using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeschoolManager.Infrastructure.Production;

public sealed class ProductionRuntimeSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static ProductionRuntimeSettingsStore()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly ProductionPathProvider paths;

    public ProductionRuntimeSettingsStore(ProductionPathProvider paths)
    {
        this.paths = paths;
    }

    public ProductionRuntimeSettings LoadOrCreate()
    {
        paths.EnsureDirectories();
        if (!File.Exists(paths.RuntimeSettingsPath))
        {
            var defaults = new ProductionRuntimeSettings();
            Save(defaults);
            return defaults;
        }

        var json = File.ReadAllText(paths.RuntimeSettingsPath);
        return JsonSerializer.Deserialize<ProductionRuntimeSettings>(json, JsonOptions) ?? new ProductionRuntimeSettings();
    }

    public void Save(ProductionRuntimeSettings settings)
    {
        paths.EnsureDirectories();
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(paths.RuntimeSettingsPath, json);
    }
}
