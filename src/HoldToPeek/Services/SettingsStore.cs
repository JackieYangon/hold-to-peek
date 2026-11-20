using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HoldToPeek.Core;

namespace HoldToPeek.Services;

/// <summary>Preferences only. Reference images and reading positions are never serialized.</summary>
public sealed record AppSettings
{
    public PeekTrigger Trigger { get; init; } = PeekTrigger.MouseBack;
    public InputBinding? PeekBinding { get; init; }
    public InputBinding CaptureBinding { get; init; } = new(0x70);
    [JsonIgnore]
    public InputBinding EffectivePeekBinding => PeekBinding ?? InputBinding.FromLegacy(Trigger);
    public double LineHeight { get; init; } = 28;
    public bool HasSeenWelcome { get; init; }
    public bool StartAtLogin { get; init; }
    public bool HighPriority { get; init; }
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        MaxDepth = 8,
        Converters = { new JsonStringEnumConverter<PeekTrigger>() }
    };

    public SettingsStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HoldToPeek", "settings.json");
    }

    public string FilePath { get; }
    public string? LoadWarning { get; private set; }

    public AppSettings Load()
    {
        LoadWarning = null;
        string json;
        try
        {
            json = File.ReadAllText(FilePath, Encoding.UTF8);
        }
        catch (FileNotFoundException)
        {
            return new AppSettings();
        }
        catch (DirectoryNotFoundException)
        {
            return new AppSettings();
        }

        // I/O failures are left to the caller so inaccessible preferences are not
        // silently mistaken for a fresh installation.
        try
        {
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null || !IsValid(settings))
            {
                LoadWarning = "设置内容不完整或超出范围，已使用默认设置。";
                return new AppSettings();
            }

            return settings;
        }
        catch (JsonException)
        {
            LoadWarning = "设置文件无法读取，已使用默认设置。";
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!IsValid(settings))
            throw new ArgumentOutOfRangeException(nameof(settings), "行距必须为 8–120 像素，触发方式必须有效。");

        string fullPath = Path.GetFullPath(FilePath);
        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = fullPath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath,
                JsonSerializer.Serialize(settings, JsonOptions), new UTF8Encoding(false));
            // Replace only after the full new document has been written.
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static bool IsValid(AppSettings settings) =>
        Enum.IsDefined(settings.Trigger) &&
        settings.EffectivePeekBinding.IsValid && settings.CaptureBinding is { IsValid: true, IsMouse: false } &&
        settings.EffectivePeekBinding.Key != settings.CaptureBinding.Key &&
        double.IsFinite(settings.LineHeight) &&
        settings.LineHeight >= 8 && settings.LineHeight <= 120;
}
