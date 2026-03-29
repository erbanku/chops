<p align="center">
  <img src="site/public/favicon.png" width="128" height="128" alt="Chops icon" />
</p>

<h1 align="center">Chops</h1>

<p align="center">Your AI skills and agents, finally organized.</p>

<p align="center">
  <a href="https://github.com/Shpigford/chops/releases/latest/download/Chops.dmg">Download</a> &middot;
  <a href="https://chops.md">Website</a> &middot;
  <a href="https://x.com/Shpigford">@Shpigford</a>
</p>

<p align="center">
  <img src="site/public/screenshot.png" width="720" alt="Chops screenshot" />
</p>

A native app for **macOS** and **Windows** to discover, organize, and edit coding agent skills and agents across Claude Code, Cursor, Codex, Windsurf, and Amp. Stop digging through dotfiles.

## Features

- **Multi-tool support** — Claude Code, Cursor, Codex, Windsurf, Copilot, Aider, Amp
- **Skills + Agents** — Discovers both skills and agents from each tool's directories
- **Built-in editor** — Monospaced editor with Cmd+S save, frontmatter parsing
- **Collections** — Organize skills and agents without modifying source files
- **Real-time file watching** — FSEvents-based, instant updates on disk changes
- **Full-text search** — Search across name, description, and content
- **Create new skills & agents** — Generates correct boilerplate per tool
- **Remote servers** — Connect to servers like [OpenClaw](https://openclaw.ai) to discover, browse, and install skills

## Prerequisites

### macOS

- **macOS 15** (Sequoia) or later
- **Xcode** with command-line tools (`xcode-select --install`)
- **Homebrew** ([brew.sh](https://brew.sh))
- **xcodegen** — `brew install xcodegen`

Sparkle (auto-update framework) is the only external dependency and is pulled automatically by Xcode via Swift Package Manager. No manual setup needed.

### Windows

- **Windows 10** (1809+) or **Windows 11**
- **Visual Studio 2022** (17.0+) with the **.NET desktop development** and **Windows App SDK C# Templates** workloads
- **.NET 8 SDK**
- **Windows App SDK 1.6+** (installed via Visual Studio or NuGet)

EF Core SQLite and CommunityToolkit.Mvvm are restored automatically via NuGet on first build.

## Quick Start

### macOS

```bash
git clone https://github.com/Shpigford/chops.git
cd chops
brew install xcodegen    # skip if already installed
xcodegen generate        # generates Chops.xcodeproj from project.yml
open Chops.xcodeproj     # opens in Xcode
```

Then hit **Cmd+R** to build and run.

> **Note:** The Xcode project is generated from `project.yml`. If you change `project.yml`, re-run `xcodegen generate`. Don't edit the `.xcodeproj` directly.

### Windows

```powershell
git clone https://github.com/Shpigford/chops.git
cd chops\ChopsWindows
dotnet restore
dotnet build
dotnet run --project Chops
```

Or open `ChopsWindows\Chops.sln` in Visual Studio and press **F5** to build and run.

### CLI build (no Xcode GUI — macOS only)

```bash
xcodebuild -scheme Chops -configuration Debug build
```

## Project Structure

### macOS (SwiftUI)

```
Chops/
├── App/
│   ├── ChopsApp.swift        # @main entry — SwiftData ModelContainer + Sparkle
│   ├── AppState.swift         # @Observable singleton — filters, selection, search
│   └── ContentView.swift      # Three-column NavigationSplitView, kicks off scanning
├── Models/
│   ├── Skill.swift            # @Model — a discovered skill or agent file
│   ├── Collection.swift       # @Model — user-created skill groupings
│   └── ToolSource.swift       # Enum of supported tools, their paths and icons
├── Services/
│   ├── SkillScanner.swift     # Probes tool directories, upserts skills into SwiftData
│   ├── SkillParser.swift      # Dispatches to FrontmatterParser or MDCParser
│   ├── FileWatcher.swift      # FSEvents listener, triggers re-scan on changes
│   └── SearchService.swift    # In-memory full-text search
├── Utilities/
│   ├── FrontmatterParser.swift  # Extracts YAML frontmatter from .md files
│   └── MDCParser.swift          # Parses Cursor .mdc files
├── Views/
│   ├── Sidebar/               # Tool filters, skills/agents lists, collections
│   ├── Detail/                # Skill editor, metadata display
│   ├── Settings/              # Preferences & update UI
│   └── Shared/                # Reusable components (ToolBadge, NewSkillSheet)
├── Resources/                 # Asset catalog (tool icons, colors)
└── Chops.entitlements         # Disables sandbox (intentional)

project.yml          # xcodegen config — source of truth for Xcode project settings
scripts/             # Release pipeline (release.sh)
site/                # Marketing website (Astro 6)
```

### Windows (WinUI 3)

```
ChopsWindows/
├── Chops.sln                    # Visual Studio solution
└── Chops/
    ├── Chops.csproj             # .NET 8 + Windows App SDK project
    ├── App.xaml / App.xaml.cs   # Application entry — EF Core database setup
    ├── MainWindow.xaml / .cs    # Three-column grid layout (Sidebar → List → Detail)
    ├── Models/
    │   ├── Skill.cs             # EF Core entity — mirrors Swift Skill model
    │   ├── SkillCollection.cs   # EF Core entity — user-created groupings
    │   ├── ToolSource.cs        # Enum of tools with Windows paths (%USERPROFILE%)
    │   └── ChopsDbContext.cs    # EF Core SQLite context (replaces SwiftData)
    ├── Services/
    │   ├── SkillScanner.cs      # Filesystem scanner, deduplicates by resolved path
    │   ├── SkillParser.cs       # Routes files to frontmatter/MDC parsers
    │   └── FileWatcher.cs       # FileSystemWatcher-based (replaces FSEvents)
    ├── Utilities/
    │   ├── FrontmatterParser.cs # YAML frontmatter extraction
    │   └── MDCParser.cs         # Cursor .mdc file parsing
    ├── ViewModels/
    │   ├── AppState.cs          # Observable state (CommunityToolkit.Mvvm)
    │   └── MainViewModel.cs     # Main coordinator — scan, filter, select
    └── Views/
        ├── SidebarView.xaml     # Tool filters and collections
        ├── SkillListView.xaml   # Filtered skill/agent list
        └── SkillDetailView.xaml # Editor + preview with Ctrl+S save
```

## Architecture

Both platforms share the same core concepts and three-column layout. The macOS version uses SwiftUI + SwiftData; the Windows version uses WinUI 3 + EF Core SQLite.

### macOS — SwiftUI + SwiftData

Native macOS with zero web views.

#### App lifecycle

1. `ChopsApp` initializes a SwiftData `ModelContainer` (persists `Skill` and `SkillCollection`)
2. Sparkle updater starts in the background
3. `AppState` is created and injected into the SwiftUI environment
4. `ContentView` renders and calls `startScanning()`
5. `SkillScanner` probes all tool directories and upserts discovered skills
6. `FileWatcher` attaches FSEvents listeners — on any change, the scanner re-runs automatically

### Windows — WinUI 3 + EF Core

Native Windows desktop app using the Windows App SDK (WinUI 3) with C# and .NET 8.

#### App lifecycle

1. `App` creates the EF Core SQLite database (stored in `%LOCALAPPDATA%\Chops\chops.db`)
2. `MainWindow` renders the three-column grid layout
3. `MainViewModel` orchestrates scanning via `SkillScanner` and filesystem monitoring via `FileWatcher`
4. `SkillScanner` probes all tool directories under `%USERPROFILE%` and upserts into SQLite
5. `FileWatcher` uses `FileSystemWatcher` (Windows equivalent of FSEvents) — on any change, the scanner re-runs automatically

#### Windows-specific notes

- Tool paths use `%USERPROFILE%\.claude\`, `%USERPROFILE%\.cursor\`, etc. (same dotfile conventions as macOS)
- Data is persisted via EF Core with SQLite (equivalent to SwiftData on macOS)
- MVVM architecture using CommunityToolkit.Mvvm (`ObservableObject`, `RelayCommand`)
- Tool detection checks `PATH`, `%LOCALAPPDATA%\Programs\`, and `%PROGRAMFILES%\` for installed binaries

### Key design decisions

- **No sandbox.** The app needs unrestricted filesystem access to read dotfiles across `~/` (macOS) or `%USERPROFILE%` (Windows). This is intentional and required for core functionality.
- **Dedup via symlinks.** Skills are uniquely identified by their resolved symlink/junction path. If the same file is linked into multiple tool directories, it shows up as one skill with multiple tool badges.
- **No test suite.** Validate changes manually — build, run, trigger the feature you changed, observe the result.

### State management

- **macOS:** `AppState` is an `@Observable` class injected via `@Environment` and accessible from any view.
- **Windows:** `AppState` is an `ObservableObject` (CommunityToolkit.Mvvm) shared across views via `MainViewModel`.

### UI layout

Three-column layout on both platforms:
- **Sidebar** — tool filters and collections
- **List** — filtered/searched skill list
- **Detail** — skill editor (macOS: `NSTextView` with Cmd+S; Windows: `TextBox` with Ctrl+S)

## Supported Tools

Chops scans these directories for skills and agents:

| Tool | macOS Paths | Windows Paths |
|------|-------------|---------------|
| Claude Code | `~/.claude/skills/`, `~/.claude/agents/` | `%USERPROFILE%\.claude\skills\`, `%USERPROFILE%\.claude\agents\` |
| Cursor | `~/.cursor/skills/`, `~/.cursor/rules`, `~/.cursor/agents/` | `%USERPROFILE%\.cursor\skills\`, `%USERPROFILE%\.cursor\rules`, `%USERPROFILE%\.cursor\agents\` |
| Windsurf | `~/.codeium/windsurf/memories/`, `~/.windsurf/rules` | `%USERPROFILE%\.codeium\windsurf\memories\`, `%USERPROFILE%\.windsurf\rules` |
| Codex | `~/.codex/skills/`, `~/.codex/agents/` | `%USERPROFILE%\.codex\skills\`, `%USERPROFILE%\.codex\agents\` |
| Amp | `~/.config/amp/skills/` | `%USERPROFILE%\.config\amp\skills\` |
| Global | `~/.agents/skills/` | `%USERPROFILE%\.agents\skills\` |

Copilot and Aider are also supported but only detect project-level skills and agents (no global paths). Custom scan paths can be added for any tool.

Tool definitions live in:
- **macOS:** `Chops/Models/ToolSource.swift` — each enum case knows its display name, icon, color, and filesystem paths
- **Windows:** `ChopsWindows/Chops/Models/ToolSource.cs` — mirrors the Swift enum with `%USERPROFILE%`-based paths

## Common Dev Tasks

### Add support for a new tool

**macOS:**
1. Add a new case to the `ToolSource` enum in `Chops/Models/ToolSource.swift`
2. Fill in `displayName`, `iconName`, `color`, and `globalPaths`
3. Optionally add a logo to the asset catalog and return it from `logoAssetName`
4. Update `SkillScanner` if the new tool uses a non-standard file layout

**Windows:**
1. Add a new case to the `ToolSource` enum in `ChopsWindows/Chops/Models/ToolSource.cs`
2. Fill in `DisplayName()`, `IconGlyph()`, `GlobalPaths()`, and `IsInstalled()`
3. Update `SkillScanner` if the new tool uses a non-standard file layout

### Modify skill parsing

- **macOS:** `Chops/Utilities/FrontmatterParser.swift`, `MDCParser.swift`, `SkillParser.swift`
- **Windows:** `ChopsWindows/Chops/Utilities/FrontmatterParser.cs`, `MDCParser.cs`, `Services/SkillParser.cs`

### Change the UI

- **macOS:** Views are in `Chops/Views/`, organized by column. Main layout in `Chops/App/ContentView.swift`.
- **Windows:** Views are in `ChopsWindows/Chops/Views/` as XAML + code-behind. Main layout in `MainWindow.xaml`.

## Testing

No automated test suite on either platform. Validate manually:

**macOS:**
1. Build and run the app (Cmd+R in Xcode)
2. Trigger the exact feature you changed
3. Observe the result — check for correct behavior and error messages
4. Test edge cases (empty states, missing directories, malformed files)

**Windows:**
1. Build and run the app (F5 in Visual Studio, or `dotnet run`)
2. Trigger the exact feature you changed
3. Observe the result — check for correct behavior and error messages
4. Test edge cases (empty states, missing directories, malformed files)

## Website

The marketing site lives in `site/` and is built with [Astro](https://astro.build/).

```bash
cd site
npm install      # first time only
npm run dev      # local dev server
npm run build    # production build → site/dist/
```

## AI Agent Setup

This repo includes a Claude Code skill at `.claude/skills/setup.md` that gives AI coding agents full context on the project — architecture, key files, and common tasks. If you're using Claude Code, it'll pick this up automatically.

## License

MIT — see [LICENSE](LICENSE).
