using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;

namespace RollbackSteam
{
    internal class Program
    {
        private const string SteamCommand = "BootStrapperInhibitAll=enable";
        private static List<string> _resourcesList = null!;

        public static void Main(string[] args)
        {
            var triggerVersion = new Version("7.56.33.36");
            _resourcesList = new List<string>()
            {
                "steam.exe", "steamclient.dll", "SteamUI.dll"
            };

            var steamPath = GetSteamPath();
            var steamFile = Path.Combine(steamPath, "steam.exe");
            if (!File.Exists(steamFile))
                throw new FileNotFoundException("steam.exe not found");

            AppendSteamConfig(steamPath);
            Console.WriteLine("steam.cfg appended");

            var steamVersion = GetFileVersion(steamFile);
            Console.WriteLine($"Steam Version: {steamVersion}");

            if (steamVersion >= triggerVersion)
            {
                Console.WriteLine("Steam need roll back to old version");

                CloseAllSteamProcesses();
                CopyOldSteamToPath(steamPath);
            }
            else
            {
                Console.WriteLine("Steam not need to be rolled back");
            }

            Console.WriteLine("\nPRESS ANY KEY TO EXIT");
            Console.ReadKey();
        }

        private static string GetSteamPath()
        {
            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam");
            if (key is null)
                throw new KeyNotFoundException("Steam not found in registry");

            var steamPath = key.GetValue("SteamPath").ToString();
            if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
                throw new DirectoryNotFoundException("Steam path not found");

            return steamPath;
        }

        private static Version GetFileVersion(string fileName)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(fileName);
            return new Version(versionInfo.FileVersion ?? "0.0.0");
        }

        private static void AppendSteamConfig(string steamPath)
        {
            var configFile = Path.Combine(steamPath, "steam.cfg");
            using var fs = new FileStream(configFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using var sr = new StreamReader(fs);
            var content = sr.ReadToEnd();
            if (content.Contains(SteamCommand))
                return;

            using var sw = new StreamWriter(fs);
            sw.WriteLineAsync(SteamCommand);
        }

        private static string RenameFile(string fileName)
        {
            var unixTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var newFileName = $"{fileName}.{unixTime}";
            File.Move(fileName, newFileName);
            return newFileName;
        }

        private static void CopyOldSteamToPath(string steamPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            _resourcesList.ForEach(resourceName =>
            {
                var file = Path.Combine(steamPath, resourceName);
                var newFile = RenameFile(file);
                Console.WriteLine($"File renamed: {file} -> {newFile}");

                
                Console.WriteLine($"Copying {resourceName}...");
                using var resource = assembly.GetManifestResourceStream($"RollbackSteam.Resources.{resourceName}");
                using var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write);
                resource?.CopyTo(fs);
            });
        }

        private static void CloseAllSteamProcesses()
        {
            Console.WriteLine("Closing all steam processes...");
            
            var processes = Process.GetProcesses().Where(p =>
            {
                var processName = p.ProcessName.ToLower();
                return processName == "steam" || processName.StartsWith("steam_");
            }).ToList();
            processes.ForEach(process =>
            {
                try
                {
                    process.Kill();
                    Console.WriteLine($"{process.Id} killed");
                }
                catch
                {
                }
            });
            
            if (processes.Count > 0)
                Thread.Sleep(2000);
        }
    }
}