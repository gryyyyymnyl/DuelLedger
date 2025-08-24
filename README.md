# DuelLedger

This repository provides components for detecting game states.

## Runner の起動方法

Shadowverse detection can be started from the command line:

```bash
dotnet run -c Release --project ./Runner/DuelLedger.Runner.csproj
```

Use `--dry-run` to output a dummy match summary without requiring the game client.

## UI

The Avalonia-based UI starts the Shadowverse detection engine in the background on launch and writes match summaries under `out/matches` beneath the application's directory. On Windows, native OpenCV binaries are loaded from `runtimes/win-x64/native`; ensure the Microsoft Visual C++ Redistributable is installed for these binaries to load correctly.
