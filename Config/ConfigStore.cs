using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PortPilot_Project.Config;

public sealed class ConfigStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string ConfigPath { get; }

    public ConfigStore(string? configPath = null)
    {
        ConfigPath = configPath ?? GetDefaultConfigPath();
    }

    public async Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        await using var stream = File.OpenRead(ConfigPath);
        var config = await JsonSerializer.DeserializeAsync<AppConfig>(stream, _jsonOptions, cancellationToken);
        return config ?? new AppConfig();
    }

    public async Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        await using var stream = File.Create(ConfigPath);
        await JsonSerializer.SerializeAsync(stream, config, _jsonOptions, cancellationToken);
    }

    private static string GetDefaultConfigPath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(baseDir, "PortPilot", "config.json");
    }
}
