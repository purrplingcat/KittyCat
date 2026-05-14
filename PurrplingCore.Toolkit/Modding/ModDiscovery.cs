using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PurrplingCore.Toolkit.Modding;

internal class ModDiscovery(ILogger logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true 
    };

    public async Task<Dictionary<string, ModEntry>> Discover(string modsDirectory)
    {
        var discovered = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
        var directories = Directory.GetDirectories(modsDirectory);
        var tasks = directories.Select(TryReadManifestAsync);
        var results = await Task.WhenAll(tasks);

        foreach (var entry in results)
        {
            if (entry.HasValue && !discovered.TryAdd(entry.Value.Manifest.Id, entry.Value))
            {
                logger.LogWarning("Duplicate mod '{Id}' found. Skipping.", entry.Value.Manifest.Id);
            }
        }

        return discovered;
    }

    private async Task<ModEntry?> TryReadManifestAsync(string modDir)
    {
        string manifestPath = Path.Combine(modDir, "manifest.json");
        if (!File.Exists(manifestPath)) return null;

        try
        {
            var stream = File.OpenRead(manifestPath);
            var manifest = await JsonSerializer.DeserializeAsync<ModManifest>(stream, _jsonOptions);
            
            if (manifest != null) return new ModEntry(manifest, modDir);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse manifest from '{Path}'", manifestPath);
        }

        return null;
    }
}
