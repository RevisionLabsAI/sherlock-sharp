using System.CommandLine;
using SherlockSharp;
using System.Text.Json;

var root = new RootCommand("SherlockSharp CLI: check username availability across services");

var usernameArg = new Argument<string>(name: "username", description: "Username to check");

var servicesOption = new Option<string?>(
    aliases: new[] {"--services", "-s"},
    description: "Comma-separated list of services to check (defaults to all non-NSFW)");

var timeoutOption = new Option<int?>(
    aliases: new[] {"--timeout", "-t"},
    description: "Timeout in seconds (default 30)");

var includeNsfwOption = new Option<bool>(
    aliases: new[] {"--include-nsfw"},
    description: "Include NSFW services", getDefaultValue: () => false);

var jsonOption = new Option<bool>(
    aliases: new[] {"--json", "-j"},
    description: "Output JSON", getDefaultValue: () => false);

var checkCmd = new Command("check", "Check a single username across services");
checkCmd.AddArgument(usernameArg);
checkCmd.AddOption(servicesOption);
checkCmd.AddOption(timeoutOption);
checkCmd.AddOption(includeNsfwOption);
checkCmd.AddOption(jsonOption);

checkCmd.SetHandler(async (username, services, timeout, includeNsfw, json) =>
{
    var servicesList = string.IsNullOrWhiteSpace(services)
        ? null
        : services!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var timeoutSpan = timeout.HasValue ? TimeSpan.FromSeconds(Math.Max(1, timeout.Value)) : (TimeSpan?)null;

    using var client = new SherlockClient(servicesList, timeoutSpan, includeNsfw);

    try
    {
        var results = await client.CheckAsync(username);
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"Results for '{username}':");
            foreach (var r in results.OrderBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase))
            {
                var status = r.Found ? "FOUND" : "NOT FOUND";
                var note = string.IsNullOrEmpty(r.Note) ? string.Empty : $" ({r.Note})";
                Console.WriteLine($" - {r.ServiceName,-20} {status}{note} {r.ProfileUrl}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
}, usernameArg, servicesOption, timeoutOption, includeNsfwOption, jsonOption);

var manyCmd = new Command("check-many", "Check multiple usernames (space-separated) across services");
var usernamesArg = new Argument<string[]>(name: "usernames", description: "Usernames to check");
manyCmd.AddArgument(usernamesArg);
manyCmd.AddOption(servicesOption);
manyCmd.AddOption(timeoutOption);
manyCmd.AddOption(includeNsfwOption);
manyCmd.AddOption(jsonOption);

manyCmd.SetHandler(async (usernames, services, timeout, includeNsfw, json) =>
{
    var servicesList = string.IsNullOrWhiteSpace(services)
        ? null
        : services!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var timeoutSpan = timeout.HasValue ? TimeSpan.FromSeconds(Math.Max(1, timeout.Value)) : (TimeSpan?)null;

    using var client = new SherlockClient(servicesList, timeoutSpan, includeNsfw);

    try
    {
        foreach (var u in usernames)
        {
            var results = await client.CheckAsync(u);
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { username = u, results }, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine($"Results for '{u}':");
                foreach (var r in results.OrderBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase))
                {
                    var status = r.Found ? "FOUND" : "NOT FOUND";
                    var note = string.IsNullOrEmpty(r.Note) ? string.Empty : $" ({r.Note})";
                    Console.WriteLine($" - {r.ServiceName,-20} {status}{note} {r.ProfileUrl}");
                }
                Console.WriteLine();
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
}, usernamesArg, servicesOption, timeoutOption, includeNsfwOption, jsonOption);

var listCmd = new Command("list-sites", "Output a comma-separated list of available services (sites)");
var includeNsfwListOption = new Option<bool>(aliases: new[] {"--include-nsfw"}, description: "Include NSFW services", getDefaultValue: () => false);
listCmd.AddOption(includeNsfwListOption);
listCmd.SetHandler((bool includeNsfw) =>
{
    try
    {
        var names = SherlockClient.GetServiceNames(includeNsfw);
        Console.WriteLine(string.Join(",", names));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
}, includeNsfwListOption);

root.AddCommand(checkCmd);
root.AddCommand(manyCmd);
root.AddCommand(listCmd);

return await root.InvokeAsync(args);
