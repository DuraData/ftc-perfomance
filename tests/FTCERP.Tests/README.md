# FTCERP Tests

This folder contains automated tests for domain entities and core functionalities.

## Scope
- Entity model tests for IDP domain defaults/enums/graph persistence.
- IDP controller functionality tests for lifecycle and workflows.
- Workflow governance service tests for audit and notifications.

## Run
From repository root:

```powershell
dotnet test tests/FTCERP.Tests/FTCERP.Tests.csproj
```

The tests project sets `SkipClientBuild=true` so frontend build is not required when running backend tests.
