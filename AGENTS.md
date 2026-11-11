# Nullean.Xunit.Partitions

An xUnit test framework that adds "partitions": an `IPartitionFixture<TLifetime>` shares a long-lived object across
tests, only one partition runs at a time, tests inside a partition still run concurrently (up to
`IPartitionLifetime.MaxConcurrency`), and each lifetime is initialized just before its tests and disposed right after.

## Layout

| Path | What |
|---|---|
| `src/Nullean.Xunit.Partitions` | the xUnit v2 package (netstandard2.0, netstandard2.1, net462) |
| `src/Nullean.Xunit.Partitions.v3` | the xUnit v3 package (netstandard2.0), with its own `README.md` |
| `tests/Nullean.Xunit.Partitions.Tests` | v2 tests, on net10.0 (plus net462 on Windows) |
| `tests/Nullean.Xunit.Partitions.v3.Tests` | v3 tests, an xUnit v3 executable on net10.0 |
| `build/scripts` | the F# build the `mise` tasks call |
| `build/keys` | the strong-name key pair |

## Commands

mise installs the SDK pinned in `global.json` (plus the net10.0 runtime the tests run on). Run everything through it:

```bash
mise run build    # build the solution
mise run test     # build, then run the tests
mise run pack     # build, pack, validate the packages, report public API changes
mise run ci       # exactly what CI runs
mise run check    # workflow security checks (zizmor)
mise run clean    # delete build output
```

`mise tasks ls` lists them all. Package output and API reports go to `build/output`.

## Rules

- Public API is a contract: `mise run pack` diffs each package against its previous release
  (`breaking-changes-*.md`). A change that removes or alters public API is a breaking change.
- The v2 and v3 packages are separate implementations against different xUnit extensibility APIs; a behaviour change in
  one usually needs the same change in the other.
- `mise run test` runs the tests through `dotnet test` (VSTest). The v3 test project has no VSTest adapter, so it is
  built but not run there; run it directly with
  `dotnet tests/Nullean.Xunit.Partitions.v3.Tests/bin/Release/net10.0/Nullean.Xunit.Partitions.v3.Tests.dll`.
