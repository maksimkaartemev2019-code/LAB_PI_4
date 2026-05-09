# Team Task Board

Team Task Board is a local-network collaborative task board. A SignalR server keeps the shared board state, and Avalonia desktop clients on Windows and macOS display and edit that state through MVVM.

## Quick Start

1. Start the server:

```bash
dotnet run --project src/TaskBoard.Server
```

2. Start the client:

```bash
dotnet run --project src/TaskBoard.Client
```

3. In the client, enter the server URL. For another computer in the same local network, replace `localhost` with the server machine IP address.

## Features

- Create and delete columns.
- Add, edit, move, and delete task cards.
- See current online users.
- Keep a local JSON copy of the last synchronized board for offline viewing.
- Switch the UI between Russian and English.

## Platforms

- Windows 10/11 with .NET 8 runtime.
- macOS with .NET 8 runtime.

The client is built with Avalonia instead of WPF to avoid Windows-only UI dependencies.
