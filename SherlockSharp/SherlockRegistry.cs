using System;
using System.Collections.Generic;
using System.Linq;
using SherlockSharp.Internal;
using SherlockSharp.Models;

namespace SherlockSharp;

/// <summary>
/// Public registry accessor for SherlockSharp service definitions.
/// </summary>
public static class SherlockRegistry
{
    /// <summary>
    /// Retrieve all known services, optionally filtered by category. NSFW flag is ignored.
    /// If category is provided, only services that include the category (case-insensitive) are returned.
    /// </summary>
    public static IReadOnlyDictionary<string, ServiceDefinition> GetServices(bool includeNsfw = false, string? category = null)
    {
        var all = DataLoader.LoadServices();
        var dict = new Dictionary<string, ServiceDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in all)
        {
            var sd = kv.Value;
            // NSFW filtering ignored; keep parameter for compatibility
            if (!string.IsNullOrWhiteSpace(category))
            {
                var cats = sd.Categories ?? Array.Empty<string>();
                if (!cats.Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase)))
                    continue;
            }
            dict[kv.Key] = sd;
        }
        return dict;
    }
}
