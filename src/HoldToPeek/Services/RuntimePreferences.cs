using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace HoldToPeek.Services;

public sealed class RuntimePreferences
{
    private readonly string _registryPath;
    public RuntimePreferences(string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Run") => _registryPath = registryPath;

    public bool StartAtLoginEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_registryPath);
        return key?.GetValue("HoldToPeek") is string value && !string.IsNullOrWhiteSpace(value);
    }

    public static string StartupCommand(string executable)
    {
        if (!Path.IsPathFullyQualified(executable) || executable.Contains('"') ||
            !string.Equals(Path.GetExtension(executable), ".exe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(executable), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("请从解压后的 HoldToPeek.exe 启用自启。");
        string command = $"\"{executable}\" --autostart";
        if (command.Length > 260) throw new InvalidOperationException("程序路径过长，请移到较短路径后启用自启。");
        return command;
    }

    public void SetStartAtLogin(bool enabled, string executable)
    {
        string? command = enabled ? StartupCommand(executable) : null;
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(_registryPath, writable: true);
        if (enabled) key.SetValue("HoldToPeek", command!, RegistryValueKind.String);
        else key.DeleteValue("HoldToPeek", throwOnMissingValue: false);
    }

    public static void SetHighPriority(bool enabled)
    {
        using Process process = Process.GetCurrentProcess();
        process.PriorityClass = enabled ? ProcessPriorityClass.High : ProcessPriorityClass.Normal;
    }
}
