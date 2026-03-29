# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is Chops

A native app for **macOS** (SwiftUI + SwiftData) and **Windows** (WinUI 3 + EF Core) for discovering, organizing, and editing AI coding agent skills across tools (Claude Code, Cursor, Codex, Windsurf, Copilot, Aider, Amp). Fully open source (MIT), public repo at github.com/Shpigford/chops. No sandbox — requires full filesystem access to read user dotfiles.

## Build & Run

### macOS

```bash
# Generate Xcode project (required after changing project.yml)
xcodegen generate

# Open in Xcode
open Chops.xcodeproj

# CLI build
xcodebuild -scheme Chops -configuration Release

# Local release-like build that launches cleanly from shell
xcodebuild -scheme Chops -configuration LocalRelease

# Release (needs APPLE_TEAM_ID, APPLE_ID, SIGNING_IDENTITY_NAME env vars)
./scripts/release.sh <version>
```

Requires: Xcode, `brew install xcodegen`, macOS 15+. Sparkle (>= 2.6.0) is the only external dependency (auto-updates via GitHub Releases).

### Windows

```powershell
# Restore NuGet packages
cd ChopsWindows
dotnet restore

# Build
dotnet build

# Run
dotnet run --project Chops
```

Or open `ChopsWindows\Chops.sln` in Visual Studio 2022 and press F5.

Requires: Visual Studio 2022 (17.0+), .NET 8 SDK, Windows App SDK 1.6+. NuGet dependencies (EF Core SQLite, CommunityToolkit.Mvvm, Windows App SDK) are restored automatically.

No test suite exists on either platform. Validate manually by building and running.

## Development Rules

**Always manually test.** After every change, build the app (`xcodebuild` on macOS, `dotnet build` on Windows), launch it, and exercise the feature you changed. Seeing "build succeeded" is not enough — open the app and verify the actual behavior. If it's a UI change, look at it. If it's a data change, confirm the data. No exceptions.

**No fallbacks.** Do not write fallback logic, graceful degradation, or backwards-compatibility shims. The product should work correctly via the primary code path. If something fails, fix the root cause — don't paper over it with a fallback. We are early-stage; the code should be clean and direct, not defensive.

## Architecture

### macOS (SwiftUI + SwiftData)

**Entry:** `Chops/App/ChopsApp.swift` → sets up SwiftData ModelContainer + Sparkle updater.

**State:** `AppState` is an `@Observable` singleton holding UI filters, search text, and selection state.

**Models (SwiftData):**
- `Skill` — a discovered skill file. Uniquely identified by resolved symlink path. Tracks which tools it's installed in.
- `SkillCollection` — user-created groupings of skills.

**Services:**
- `SkillScanner` — probes tool directories (~/.claude/skills/, ~/.cursor/rules/, etc.), parses frontmatter, upserts into SwiftData. Deduplicates via resolved symlink paths.
- `FileWatcher` — FSEvents via `DispatchSourceFileSystemObject`, triggers re-scan on changes.
- `SkillParser` → dispatches to `FrontmatterParser` (.md) or `MDCParser` (.mdc).
- **ACP agent hierarchy** — `BaseACPAgent` owns all transport/session logic. Vendor subclasses override three hooks:
  - `shouldFilter(JsonRpcMessage) → Bool` — drop messages before SDK decoding (e.g. Augment's `usage_update`)
  - `postProcess(String) → String` — strip vendor-specific XML tags before text is stored
  - `resolvePermission(title, options)` — present permission UI; hook for future session-wide allow-all
  - `ACPAgentFactory.make(for: ToolSource)` instantiates the right subclass; `ComposePanel` holds `BaseACPAgent?`.

**Views:** Three-column `NavigationSplitView` (Sidebar → List → Detail). Editor uses native `NSTextView` with markdown highlighting. Cmd+S save via `FocusedValues`. Agent responses render via `MarkdownMessageView` (MarkdownUI `.gitHub` theme + syntax highlighting).

**Tool sources** are defined in `ToolSource.swift` — each enum case knows its display name, icon, and filesystem paths to scan.

### Windows (WinUI 3 + EF Core)

**Entry:** `ChopsWindows/Chops/App.xaml.cs` → creates EF Core SQLite database + main window.

**State:** `AppState` is a CommunityToolkit.Mvvm `ObservableObject` holding UI filters, search text, and selection state.

**Models (EF Core SQLite):**
- `Skill` — mirrors the Swift model. Uniquely identified by resolved path. Uses JSON-encoded strings for frontmatter and installed paths.
- `SkillCollection` — user-created groupings of skills.
- `ChopsDbContext` — EF Core context with SQLite storage in `%LOCALAPPDATA%\Chops\chops.db`.

**Services:**
- `SkillScanner` — probes tool directories under `%USERPROFILE%`, parses frontmatter, upserts into SQLite. Deduplicates via resolved paths.
- `FileWatcher` — `FileSystemWatcher`-based (Windows equivalent of FSEvents), triggers re-scan with 500ms debounce.
- `SkillParser` → dispatches to `FrontmatterParser` (.md) or `MDCParser` (.mdc).

**Views:** Three-column `Grid` layout in `MainWindow.xaml` (Sidebar → List → Detail). Editor uses `TextBox` with monospace font. Ctrl+S save via keyboard accelerator. Preview uses `WebView2`.

**ViewModels:** `MainViewModel` coordinates scanning, filtering, and persistence. Uses `RelayCommand` for async operations.

**Tool sources** are defined in `ToolSource.cs` — each enum case knows its display name, icon glyph, and filesystem paths under `%USERPROFILE%`.

## Release Pipeline

### macOS
`scripts/release.sh` does: xcodegen → archive → export with Developer ID → create DMG → notarize → staple → git tag → generate Sparkle appcast.xml → GitHub Release. Appcast served at chops.md/appcast.xml.

### Windows
No automated release pipeline yet. Build with `dotnet publish -c Release` for distribution. Future work: MSIX packaging and Microsoft Store submission.

## Website

Marketing site lives in `site/` — Astro 6, built with `npm run build` from that directory. Appcast XML is in `site/public/appcast.xml`.
