# Project rules for Claude

## What this is

CortanaKiller is a tiny Windows Forms application that permanently kills the Cortana background
task on Windows 10. It has no user interface, it starts, hides itself and then keeps killing every
process whose name contains `searchui`. There is nothing to configure and nothing to click, the
only way to stop it is the task manager.

One solution `src/CortanaKiller.sln` with exactly one project,
`src/CortanaKiller/CortanaKiller.csproj`, `OutputType` `WinExe`. There are no tests, no class
library and no `.github` folder.

Layout inside `src/CortanaKiller`:

- `Program.cs`: `[STAThread] Main`, `Application.EnableVisualStyles`,
  `Application.SetCompatibleTextRenderingDefault(false)`, `Application.Run(new Main())`.
- `Main.cs`: the entire logic of the application. Everything happens in the constructor of the
  form.
- `Main.Designer.cs` and `Main.resx`: the untouched designer output of an empty 284x261 form. The
  form is never shown, so its contents do not matter.
- `GlobalUsings.cs`: all usings of the project, currently only `System.Diagnostics`.
- `Remove.ico`: the application icon, referenced by `ApplicationIcon` and by the installer.
- `License.txt`: copied to the output directory with `CopyToOutputDirectory=Always`, because the
  installer shows it as the license file. It is a byte identical copy of the `License.txt` in the
  repository root, keep both in sync.

`Setup` holds the installer: `CortanaKiller-Setup.iss` (Inno Setup 6),
`build-setup-files.bat` (cleans `bin` and `obj`, publishes, deletes the `*.pdb`) and the built
`CortanaKiller-Setup.exe`, which is tracked although `.gitignore` excludes `*.exe`.

Repository root: `README.md` (spelled with capital letters here, the sibling repositories use
`Readme.md`), `Changelog.md`, `License.txt` (MIT), `.gitignore`, `.gitattributes`. There is no
`Updating.md` and no `HowToUse.md`.

## Build

```powershell
dotnet build src/CortanaKiller.sln -c Release
```

There are no tests, so there is nothing to run with `dotnet test`. A behaviour change is verified
by starting the published executable and looking at the process list, not at stdout.

- Single target framework `net9.0-windows` in the one project, no multi-targeting.
  `RuntimeIdentifiers` is `win-x64`, but `build-setup-files.bat` publishes without `-r`, so the
  identifier only matters when someone passes it explicitly.
- All build properties live directly in `CortanaKiller.csproj`. There is **no**
  `Directory.Build.props` in this repository.
- `TreatWarningsAsErrors` is enabled, so every warning breaks the build, NuGet warnings (`NU****`)
  from restore included. A clean build reports zero warnings, keep it that way.
- `NU1803` (HTTP source usage during restore) is the one warning suppressed via `NoWarn`. Fix
  warnings instead of extending that list. `NuGetAudit` and `NuGetAuditMode=all` are on, so a
  vulnerable transitive package fails the build too.
- Versions come from GitVersion.MsBuild out of the git tags, for example `1.0.8-1` for the first
  commit after tag `1.0.7`. Never edit a version property or an assembly version by hand.
- Restore needs nuget.org. If a private feed is configured globally on the machine and answers 404
  for public packages, restore fails with `NU1301`. Then build with an explicit source:
  `dotnet build src/CortanaKiller.sln --source https://api.nuget.org/v3/index.json`.

## Code conventions

Follow the surrounding code, it is consistent in every hand written file:

- File header comment block with `<copyright file="..." company="Hämmer Electronics">` and a
  `<summary>`, then the file-scoped namespace.
- XML doc comments on every type and every member, private members included, no exceptions.
- `Nullable`, `ImplicitUsings` and `LangVersion latest` are enabled.
- New `using` directives go into `GlobalUsings.cs`, inside the existing `#pragma warning disable
  IDE0065` block, never at the top of a file. The editorconfig requires usings inside the namespace
  (`csharp_using_directive_placement=inside_namespace:warning`), which global usings cannot
  satisfy, that is what the pragma is for. Do not add other pragmas. The comment text in that block
  is German because Visual Studio generated it, leave it alone.
- Fields, properties, methods and events are always accessed with `this.` qualification
  (`dotnet_style_qualification_for_*` at severity `warning`).
- `src/.editorconfig` also enforces braces everywhere, no multiple blank lines, four spaces, CRLF,
  UTF-8, file scoped namespaces, `System` usings sorted first and `IDE0005` as warning. Analyzer
  warnings are fixed, not silenced.
- `Main.Designer.cs` follows none of this. It is generated code with German comments and without
  `this.` qualification in `Dispose`. Do not reformat it, the designer would undo it.

## Known quirks

Do not silently "clean up" these, they are existing behaviour:

- **All the work happens in the constructor.** `Main()` calls `InitializeComponent`, sets
  `Visible = false` and then enters an endless loop that kills the matching processes. The
  constructor never returns, so `Application.Run` never starts a message pump and the form is never
  displayed. That is why the application has no window even though it is a Windows Forms
  application, and it is also why it can only be ended through the task manager.
- **The process name is matched with `Contains("searchui")`.** The Cortana background task is
  `SearchUI.exe` on Windows 10, so the comparison is lowercased first. It is a substring match, any
  future process whose name contains that text is killed as well.
- **Killing may fail and that is expected.** `Process.Kill` throws for processes of other users and
  for processes that already exited between enumeration and kill. The `catch` around the loop
  swallows everything on purpose, the next iteration tries again.
- **The form title is still `Form1`.** It comes straight from the designer template. Since the form
  is never shown, nobody ever sees it.
- **The installer executable is tracked although `.gitignore` has `*.exe`.** Adding a new build of
  `Setup/CortanaKiller-Setup.exe` needs `git add -f`, a plain `git add -A` skips it.
- **AppVeyor badge without CI in the repository.** `README.md` links an AppVeyor build that is
  configured outside of this repository. There is no pipeline file here.
- **`src/CortanaKiller.sln.DotSettings`** is tracked and holds nothing but a ReSharper user
  dictionary (`Cortana`, `H_00E4mmer`, `searchui`). Leave it alone.
- **`.gitattributes` sets `* text=auto`** and every rule of the Visual Studio template below it is
  commented out. A binary file that must not be normalized needs its own rule.

## Releasing

1. Make the change.
2. Add an entry at the top of `Changelog.md` in the existing format:
   `* **Version 1.0.8.0 (2026-08-11)** : Short description.`
3. Set `MyAppVersion` in `Setup/CortanaKiller-Setup.iss` to the same four part version.
4. Commit that.
5. Tag the commit with the plain version number, no `v` prefix (`1.0.8`, `1.0.7`, ...). The existing
   tags are lightweight tags, create new ones the same way.
6. Run `Setup/build-setup-files.bat` and compile the installer with
   `ISCC.exe Setup/CortanaKiller-Setup.iss`. This has to happen **after** the tag, otherwise
   GitVersion burns a prerelease version into the shipped executable.
7. Commit the new `Setup/CortanaKiller-Setup.exe` with `git add -f`.
8. Push the commits and the tag.

The version in the `Changelog.md` has four parts (`1.0.8.0`), the tag has three (`1.0.8`).
GitVersion turns the tag into the assembly version, so an untagged commit produces something like
`1.0.8-1+Branch.master.Sha...`. There is no package to push, so the release ends with the push.

## Git

- **Never amend a commit.** No `git commit --amend`, not for a typo in the message, not to add a
  forgotten file, not even when the commit is still local. Write a follow-up commit instead. The
  release versions come from tags on exact commits, an amended commit leaves its tag pointing at a
  commit that no longer exists in the branch.

## Writing style

- Commit messages are written **in English only**: short, precise subject line, explanatory body
  when needed.
- Code comments and comments in project files such as `.csproj` are **always English**, regardless
  of the language used in the conversation.
- **No em dashes or en dashes** (`—`, `–`), neither in prose, commit messages, code comments nor
  documentation. Use a regular hyphen, comma, colon, parentheses or a separate sentence.
- German texts (documentation, chat replies) always use real umlauts and ß, never ASCII
  transliterations such as `ae`, `oe`, `ue` or `ss`. Identifiers, file names and configuration keys
  stay unchanged where umlauts are technically undesirable.
