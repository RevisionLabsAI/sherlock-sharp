SherlockSharp.Example

A minimal console application demonstrating how to use the SherlockSharp library to check username availability across services.

Build
- Included in the BetterNamer solution. If not, add the project to the solution.

Run examples

1) Single username (defaults to all non-NSFW services)
   dotnet run --project SherlockSharp/SherlockSharp.Example -- check alice

2) Restrict to specific services
   dotnet run --project SherlockSharp/SherlockSharp.Example -- check alice -s github,twitter

3) Include NSFW services and increase timeout
   dotnet run --project SherlockSharp/SherlockSharp.Example -- check alice --include-nsfw -t 60

4) JSON output
   dotnet run --project SherlockSharp/SherlockSharp.Example -- check alice --json

5) Check multiple usernames
   dotnet run --project SherlockSharp/SherlockSharp.Example -- check-many alice bob carol -s github

Help
   dotnet run --project SherlockSharp/SherlockSharp.Example -- --help
