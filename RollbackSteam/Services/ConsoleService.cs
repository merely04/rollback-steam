using System;
using System.Globalization;

namespace RollbackSteam.Services;

public static class ConsoleService
{
    static ConsoleService()
    {
        Console.BackgroundColor = ConsoleColor.Black;
    }

    public static void WriteInfo(string text)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Write($"INFO | {text}");
    }

    public static void WriteSuccess(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Write($"SUCCESS | {text}");
    }

    public static void WriteError(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Write($"ERROR | {text}");
    }

    private static void Write(string text) =>
        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)} | {text}");
}