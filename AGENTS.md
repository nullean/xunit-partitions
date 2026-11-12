# Nullean.Xunit.Partitions

An xUnit test framework that adds "partitions": an `IPartitionFixture<TLifetime>` shares a long-lived object across
tests, only one partition runs at a time, tests inside a partition still run concurrently (up to
`IPartitionLifetime.MaxConcurrency`), and each lifetime is initialized just before its tests and disposed right after.

## Layout

| Path | What |
|---|---|
| `src/Nullean.Xunit.Partitions` | the xUnit v2 package (netstandard2.0, netstandard2.1, net462) |
| `src/Nullean.Xunit.Partitions.v3` | the xUnit v3 package (netstandard2.0), with its own `README.md` |
| `src/*/skills` | the agent skill packed into each package (`skills/<name>/SKILL.md`) |
| `tests/Nullean.Xunit.Partitions.Tests` | v2 tests, on net11.0 (plus net462 on Windows) |
| `tests/Nullean.Xunit.Partitions.v3.Tests` | v3 tests, an xUnit v3 executable on net11.0 |
| `build/scripts` | the F# build ([Nullean.Make.Fs](https://www.nuget.org/packages/Nullean.Make.Fs), net11.0) the `mise` tasks call |
| `build/keys` | the strong-name key pair |

## Commands

mise installs the SDK pinned in `global.json`. Run everything through it:

```bash
mise run build    # build the solution
mise run test     # build, then run the tests
mise run pack     # build, pack, validate the packages, report public API changes
mise run ci       # exactly what CI runs: the curb check, test, pack
mise run format   # format the repository with curb
mise run check    # formatting drift (curb) and workflow security checks (zizmor)
mise run clean    # delete build output
mise run release  # show the release draft; `mise run release publish` releases
```

`mise tasks ls` lists them all.

## Releasing

release-drafter keeps a draft release up to date from the labels of merged pull requests. **Publishing the draft**
releases both packages: it creates the tag, and the `release` job in `ci.yml` packs at that version and pushes to
nuget.org. `mise run release` shows the draft. The [releasing](.claude/skills/releasing/SKILL.md) and
[breaking-changes](.claude/skills/breaking-changes/SKILL.md) skills have the details. Every pull request carries
exactly one release label. Package output and API reports go to `build/output`.

## Rules

- Public API is a contract: `mise run pack` diffs each package against its previous release
  (`breaking-changes-*.md`). A change that removes or alters public API is a breaking change.
- The v2 and v3 packages are separate implementations against different xUnit extensibility APIs; a behaviour change in
  one usually needs the same change in the other.
- `mise run test` runs the tests through `dotnet test` (VSTest). The v3 test project has no VSTest adapter, so it is
  built but not run there; run it directly with
  `dotnet tests/Nullean.Xunit.Partitions.v3.Tests/bin/Release/net11.0/Nullean.Xunit.Partitions.v3.Tests.dll`.
- Package versions live in `Directory.Packages.props` (Central Package Management), never on a `PackageReference`.
  Package metadata lives in `Directory.Build.props`; a project file keeps only id, title, description and tags.
- The packed skills are written from the code, and every snippet in them must compile: update them with the public
  API, in both packages.
- Build targets are Make.Fs `target { }` blocks in `build/scripts/Program.fs` whose commands are Proc.Fs `sh $"…"`
  lines; values come from `text`/`lines` inside `shell { }`. Do not call the C# `ProcNet` API from the build.
- Formatting is curb's, from `.editorconfig` (the nullean template, identical in every repo): run `mise run format`
  before committing C#. CI fails on drift. See the [format](.claude/skills/format/SKILL.md) skill.
