# back-hr

Backend (.NET / C#) for an AI-powered HR platform: automated resume screening via a matching algorithm, a candidate-facing HR chatbot, and an employee well-being reporting tool. Built with a 4-person team.

## Stack

C#, .NET Core, SQL Server, Entity Framework

## Getting started

```bash
dotnet restore
dotnet ef database update
dotnet run
```

Configure your connection string and secrets via your local `appsettings.Development.json` or environment variables — do not commit real secrets to this repository.
