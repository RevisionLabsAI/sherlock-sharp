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
        var streams = GetEmbeddedDataJsonStreams();
        if (streams.Count == 0)
            throw new FileNotFoundException("No embedded data.json or data-*.json resources found.");

        var dict = new Dictionary<string, ServiceDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var stream in streams)
        {
            try
            {
                if (stream.CanSeek && stream.Length == 0) continue;
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.NameEquals("$schema")) continue;
                    try
                    {
                        var sd = JsonSerializer.Deserialize<ServiceDefinition>(prop.Value.GetRawText(), Options);
                        if (sd != null && !string.IsNullOrWhiteSpace(sd.Url))
                        {
                            // Later files override earlier ones on name collision
                            dict[prop.Name] = sd;
                        }
                    }
                    catch
                    {
                        // Skip malformed entries
                    }
                }
            }
            catch (JsonException jex)
            {
                throw new InvalidDataException("Failed to parse embedded data*.json. Ensure it contains valid JSON.", jex);
            }
            finally
            {
                stream.Dispose();
            }
        }
        return dict;
    }

    private static List<Stream> GetEmbeddedDataJsonStreams()
    {
        var asm = Assembly.GetExecutingAssembly();
        var names = asm.GetManifestResourceNames();
        // Match main data.json and any data-*.json under Resources
        var matches = names
            .Where(n => n.EndsWith("Resources.data.json", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Resources.data-", StringComparison.OrdinalIgnoreCase) && n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var list = new List<Stream>();
        foreach (var name in matches)
        {
            var s = asm.GetManifestResourceStream(name);
            if (s != null)
                list.Add(s);
        }
        return list;
    }
}
