# DuelLedger

This repository provides components for detecting game states.

## Runner の起動方法

Shadowverse detection can be started from the command line:

```bash
dotnet run -c Release --project ./Runner/DuelLedger.Runner.csproj
```

Use `--dry-run` to output a dummy match summary without requiring the game client.
