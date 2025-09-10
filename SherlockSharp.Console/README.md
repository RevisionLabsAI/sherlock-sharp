SherlockSharp.Console

A command-line application for checking username availability across many services using the SherlockSharp library.

Build
- Included in the BetterNamer solution. You can build from the solution or via CLI:
  dotnet build SherlockSharp/SherlockSharp.Console/SherlockSharp.Console.csproj

Run examples

1) Single username (defaults to all non-NSFW services)
   dotnet run --project SherlockSharp/SherlockSharp.Console -- check alice

2) Restrict to specific services
   dotnet run --project SherlockSharp/SherlockSharp.Console -- check alice -s github,twitter

3) Include NSFW services and increase timeout
   dotnet run --project SherlockSharp/SherlockSharp.Console -- check alice --include-nsfw -t 60

4) JSON output
   dotnet run --project SherlockSharp/SherlockSharp.Console -- check alice --json

5) Check multiple usernames
   dotnet run --project SherlockSharp/SherlockSharp.Console -- check-many alice bob carol -s github

Help
   dotnet run --project SherlockSharp/SherlockSharp.Console -- --help
