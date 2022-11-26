using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using RollbackSteam.Services;

namespace RollbackSteam
{
    internal static class Program
    {
        private const string SteamCommand = "BootStrapperInhibitAll=enable";
        private const string SteamTriggerVersionText = "7.56.33.36";

        public static void Main(string[] args)
        {
            var triggerVersion = new Version(SteamTriggerVersionText);
            
            try
            {
                var steamPath = GetSteamPath();
                var steamFile = Path.Combine(steamPath, "steam.exe");
                if (!File.Exists(steamFile))
                    throw new FileNotFoundException($"{steamFile} not found");

                AppendSteamConfig(steamPath);

                var steamVersion = GetFileVersion(steamFile);
                ConsoleService.WriteInfo($"Steam Version: {steamVersion}");

                if (steamVersion >= triggerVersion)
                {
                    ConsoleService.WriteInfo("Steam need roll back to old version");

                    CloseAllSteamProcesses();
                    CopyOldSteamToPath(steamPath);
                }
                else
                {
                    ConsoleService.WriteInfo("Steam not need to be rolled back");
                }
            }
            catch (Exception ex)
            {
                ConsoleService.WriteError(ex.Message);
            }

            Console.ResetColor();
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
            ConsoleService.WriteSuccess("steam.cfg appended");
        }

        private static void BackupFile(string fileName)
        {
            var unixTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var newFileName = $"{fileName}.{unixTime}";
            
            File.Move(fileName, newFileName);
            ConsoleService.WriteSuccess($"File renamed: {fileName} -> {newFileName}");
        }

        private static void CopyOldSteamToPath(string steamPath)
        {
            var resources = ResourcesService.GetResourcesList();

            resources.ForEach(resourceName =>
            {
                Console.WriteLine();
                
                var tempFileName = resourceName
                    .Replace("RollbackSteam.Resources.", "")
                    .Replace("bin.", "bin\\");
                var fileName = Path.Combine(steamPath, tempFileName);
                BackupFile(fileName);

                ConsoleService.WriteInfo($"Extracting resource {tempFileName}...");
                ResourcesService.ExtractResource(resourceName, fileName);
                ConsoleService.WriteSuccess($"Extracted to {fileName}");
            });
        }

        private static void CloseAllSteamProcesses()
        {
            ConsoleService.WriteInfo("Closing all steam processes...");

            // var processes = Process.GetProcesses().Where(p =>
            // {
            //     var processName = p.ProcessName.ToLower();
            //     return processName == "steam" || processName.StartsWith("steam_");
            // }).ToList();
            var processes = Process
                .GetProcesses()
                .Where(p => p.ProcessName.ToLower().StartsWith("steam"))
                .ToList();
            processes.ForEach(process =>
            {
                try
                {
                    process.Kill();
                    ConsoleService.WriteSuccess($"{process.Id} killed");
                }
                catch
                {
                    ConsoleService.WriteError($"{process.Id} not killed");
                }
            });

            if (processes.Count <= 0)
                return;
            
            ConsoleService.WriteInfo("Waiting...");
            Thread.Sleep(2500);
        }
    }
}