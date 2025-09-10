using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SherlockSharp.Models;

namespace SherlockSharp.Internal;

internal static class DataLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyDictionary<string, ServiceDefinition> LoadServices()
    {
        using var stream = GetEmbeddedDataJsonStream();
        if (stream.CanSeek && stream.Length == 0)
        {
            throw new InvalidDataException("Embedded resource data.json is empty. Ensure SherlockSharp/Resources/data.json contains valid JSON.");
        }
        try
        {
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;
            var dict = new Dictionary<string, ServiceDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("$schema")) continue;
                try
                {
                    var sd = JsonSerializer.Deserialize<ServiceDefinition>(prop.Value.GetRawText(), Options);
                    if (sd != null && !string.IsNullOrWhiteSpace(sd.Url))
                    {
                        dict[prop.Name] = sd;
                    }
                }
                catch
                {
                    // Skip malformed entries in minimal MVP
                }
            }
            return dict;
        }
        catch (JsonException jex)
        {
            throw new InvalidDataException("Failed to parse embedded data.json. Ensure it contains valid JSON.", jex);
        }
    }

    private static Stream GetEmbeddedDataJsonStream()
    {
        var asm = Assembly.GetExecutingAssembly();
        // Resource name is "SherlockSharp.Resources.data.json"
        var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("Resources.data.json", StringComparison.OrdinalIgnoreCase));
        if (name == null) throw new FileNotFoundException("Embedded resource data.json not found.");
        var s = asm.GetManifestResourceStream(name);
        if (s == null) throw new FileNotFoundException("Embedded resource data.json stream is null.");
        return s;
    }
}
