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
- `Main.cs`: the entire logic of the application. The constructor starts a timer, the timer handler
  kills the matching processes.
- `Main.Designer.cs` and `Main.resx`: the untouched designer output of an empty 284x261 form. The
  form is never shown, so its contents do not matter.
- `GlobalUsings.cs`: all usings of the project, currently `System.ComponentModel` and
  `System.Diagnostics`.
- `Remove.ico`: the application icon, referenced by `ApplicationIcon` and by the installer.
- `License.txt`: copied to the output directory with `CopyToOutputDirectory=Always`, because the
  installer shows it as the license file. It is a byte identical copy of the `License.txt` in the
  repository root, keep both in sync.

`Setup` holds the installer: `CortanaKiller-Setup.iss` (Inno Setup 6),
`build-setup-files.bat` (cleans `bin` and `obj`, publishes, deletes the `*.pdb`) and the built
`CortanaKiller-Setup.exe`, which is not tracked, `.gitignore` excludes `*.exe`.

Repository root: `README.md` (spelled with capital letters here, the sibling repositories use
`Readme.md`), `Changelog.md`, `License.txt` (MIT), `.gitignore`, `.gitattributes`. There is no
`Updating.md` and no `HowToUse.md`.

## Build

```powershell
dotnet build src/CortanaKiller.sln -c Release
```

There are no tests, so there is nothing to run with `dotnet test`. A behaviour change is verified
by starting the published executable and looking at the process list, not at stdout.

- Single target framework `net10.0-windows` in the one project, no multi-targeting.
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

- **The form exists only to keep a message loop alive.** There is no user interface. `Main`
  overrides `SetVisibleCore` and always passes `false` to the base implementation, because
  `Application.Run(Form)` shows the form it is given. The work runs on a
  `System.Windows.Forms.Timer` that fires once per second on the UI thread, so the process idles at
  almost no CPU. There is no exit command either, the application can only be ended through the
  task manager. Up to version 1.0.7.0 the kill loop was an endless `while (true)` in the
  constructor without a pause, which pinned one CPU core at 100 percent.
- **The process name is matched with `Contains("searchui", StringComparison.OrdinalIgnoreCase)`.**
  The Cortana background task is `SearchUI.exe` on Windows 10. It is a substring match, any other
  process whose name contains that text is killed as well. On Windows 11 there is no such process,
  the search host is called `SearchHost.exe`, so the application has nothing to do there.
- **Killing may fail and that is expected.** `Process.Kill` throws for processes of other users and
  for processes that already exited between enumeration and kill. The `catch` around the loop
  swallows everything on purpose, the next iteration tries again.
- **The form title is still `Form1`.** It comes straight from the designer template. Since the form
  is never shown, nobody ever sees it.
- **The installer executable belongs on the release, not into a commit.** Up to and including 1.0.8
  `Setup/CortanaKiller-Setup.exe` was tracked, added with `git add -f` against the `*.exe` rule of
  `.gitignore`. Do not add it back.
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
7. Push the commits and the tag.
8. Attach `Setup/CortanaKiller-Setup.exe` to the GitHub release of that tag. **Never commit the
   installer.** `Setup/` is the `OutputDir` of the Inno Setup script, so the file lands there during
   the build and `.gitignore` covers it afterwards.

The version in the `Changelog.md` has four parts (`1.0.8.0`), the tag has three (`1.0.8`).
GitVersion turns the tag into the assembly version, so an untagged commit produces something like
`1.0.8-1+Branch.master.Sha...`. There is no package to push, so the release ends with the asset
upload.

For step 8 there is no `gh` on this machine. The GitHub API does the job, with the token that
`git push` already uses, so nothing has to be stored anywhere:

```bash
c=$(printf "protocol=https\nhost=github.com\n\n" | git credential fill)
tok=$(printf "%s" "$c" | grep '^password=' | cut -d= -f2-)
id=$(curl -s -X POST -H "Authorization: Bearer $tok" \
  https://api.github.com/repos/SeppPenner/CortanaKiller/releases \
  -d '{"tag_name":"1.0.9","name":"1.0.9"}' | grep -m1 '"id"' | tr -dc 0-9)
curl -s -X POST -H "Authorization: Bearer $tok" -H "Content-Type: application/octet-stream" \
  --data-binary @Setup/CortanaKiller-Setup.exe \
  "https://uploads.github.com/repos/SeppPenner/CortanaKiller/releases/$id/assets?name=CortanaKiller-Setup.exe"
```

Never print that token, and never write it into a file.

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
