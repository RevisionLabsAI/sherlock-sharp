using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SherlockSharp.Models;

public sealed class ServiceDefinition
{
    [JsonPropertyName("url")] 
    public string? Url { get; set; }
    
    [JsonPropertyName("urlMain")] 
    public string? UrlMain { get; set; }
    [JsonPropertyName("urlProbe")] 
    public string? UrlProbe { get; set; }

    [JsonPropertyName("errorType")] 
    public string? ErrorType { get; set; }

    [JsonPropertyName("errorMsg"), JsonConverter(typeof(FlexibleStringArrayConverter))] 
    public string[]? ErrorMsg { get; set; }

    [JsonPropertyName("regexCheck")] 
    public string? RegexCheck { get; set; }

    [JsonPropertyName("isNSFW")] 
    public bool? IsNsfw { get; set; }

    // Optional categorization from Sherlock data (e.g., ["popular", "social"])
    [JsonPropertyName("categories")] 
    public string[]? Categories { get; set; }

    [JsonPropertyName("request_method")] 
    public string? RequestMethod { get; set; }

    [JsonPropertyName("request_payload")] 
    public Dictionary<string, object?>? RequestPayload { get; set; }
}
