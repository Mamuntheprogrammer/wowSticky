# WowSticky

**A sticky-note app that follows folders in Windows Explorer.**

WowSticky lives in your system tray and automatically shows notes for whichever folder you're currently browsing in Explorer. Switch to a different folder — your notes switch too.

---

## Demo

[![WowSticky Demo](https://img.shields.io/badge/Watch-Demo-blue?style=for-the-badge)](https://github.com/Mamuntheprogrammer/WowSticky)

> Download the latest installer from the [Releases](https://github.com/Mamuntheprogrammer/WowSticky/releases) page.

---

## Features

| Feature | Description |
|---|---|
| **Folder-Aware** | Notes are linked to folders — switch folders in Explorer, notes follow |
| **System Tray** | Runs quietly in the notification area; right-click for all controls |
| **Drag & Resize** | Drag the title bar to move, grab any edge or corner to resize |
| **Pin & Lock** | Pin to lock position, Lock to make notes read-only |
| **Color Palette** | 9 colors to choose from |
| **Font Size** | Adjust text size from 12 to 24 |
| **Always on Top** | Toggle per-note always-on-top |
| **Auto-Save** | Content saves automatically as you type |
| **Global Hotkey** | `Ctrl+Shift+W` to show/hide all notes instantly |
| **Auto-Startup** | Launches automatically with Windows |
| **Single Instance** | Only one instance runs; double-launch shows existing notes |
| **Tutorial** | Step-by-step walkthrough on first run (skippable) |

---

## Tutorial

On first launch, WowSticky shows an 8-step tutorial covering:

1. **Folder-Aware Notes** — how notes follow folders
2. **System Tray** — the control center
3. **Pin & Lock** — position and edit locking
4. **Colors & Font Size** — customization
5. **Drag & Resize** — window management
6. **Auto-Save** — persistence
7. **Keyboard Shortcut** — `Ctrl+Shift+W`
8. **Trash** — deleting notes

The tutorial can be skipped at any time.

---

## Download & Install

### Requirements

- Windows 10 / 11
- [.NET 9 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (x64)

### Installer

1. Download `WowSticky-Setup-1.1.exe` from the [Releases](https://github.com/Mamuntheprogrammer/WowSticky/releases) page
2. Run the installer
3. WowSticky starts automatically and adds itself to Windows startup

### Portable

Download `WowSticky.exe` from `publish/` and run it directly (requires .NET 9 Runtime).

---

## Developer Info

Access the About dialog from the system tray menu:

- **Developer:** Md. Abdullah Al Mamun
- **Email:** a.a.mamunbu@gmail.com
- **LinkedIn:** [linkedin.com/in/mamuntheprogrammer](https://www.linkedin.com/in/mamuntheprogrammer)
- **GitHub:** [github.com/Mamuntheprogrammer](https://github.com/Mamuntheprogrammer)

The About dialog includes clickable links and a **Copy** button for the email address.

---

## Build from Source

```bash
# Clone
git clone https://github.com/Mamuntheprogrammer/WowSticky.git
cd WowSticky

# Build
dotnet build -c Release

# Publish single-file exe
dotnet publish -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish

# Compile installer (requires Inno Setup 6)
iscc installer.iss
```

**Tech Stack:** .NET 9, WPF, WindowsForms, SQLite (Microsoft.Data.Sqlite), Inno Setup 6.

---

## Contributing

Contributions, issues, and feature requests are welcome!

- **Found a bug?** [Open an issue](https://github.com/Mamuntheprogrammer/WowSticky/issues)
- **Want a feature?** Start a discussion or open a pull request
- **Ideas for improvement:** All suggestions welcome

Feel free to fork the repo, make changes, and submit a pull request. Whether it's a bug fix, a new feature, UI polish, or documentation — every contribution helps.

---

## License

This project is open source. Feel free to use, modify, and distribute.
