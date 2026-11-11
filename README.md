<p align="center">
  <a href="https://github.com/nullean">
    <picture>
      <source media="(prefers-color-scheme: light)" srcset="https://raw.githubusercontent.com/nullean/nullean.github.io/main/assets/nullean-lockup-light-544x192.png">
      <img src="https://raw.githubusercontent.com/nullean/nullean.github.io/main/assets/nullean-lockup-544x192.png" alt="nullean" width="272" height="96">
    </picture>
  </a>
</p>

<p align="center">Partitions for xUnit: share a few expensive, long-lived objects (a container, a server, a browser) across thousands of tests, set up just before the tests that need them and torn down right after, one partition at a time.</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Nullean.Xunit.Partitions.v3"><img src="https://img.shields.io/nuget/v/Nullean.Xunit.Partitions.v3?style=flat-square" alt="NuGet"></a>
  <a href="https://github.com/nullean/xunit-partitions/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/nullean/xunit-partitions/ci.yml?branch=main&style=flat-square&label=ci" alt="CI"></a>
</p>

```csharp
using AwesomeAssertions;
using Nullean.Xunit.Partitions.v3.Sdk;
using Xunit;

// The shared state: initialised just before its partition's tests run, disposed right after.
public class Database : IPartitionLifetime
{
	private static int _starts;
	public int Starts => _starts;

	public ValueTask InitializeAsync()
	{
		Interlocked.Increment(ref _starts);   // start a container, a server, a browser...
		return default;
	}

	public ValueTask DisposeAsync() => default;

	public int? MaxConcurrency => null;        // test classes run at once; null for the default
	public string FailureTestOutput() => "";   // reported on every test of the partition if InitializeAsync throws
}

public class OrderTests(Database db) : IPartitionFixture<Database>
{
	[Fact]
	public void StartedOnce() => db.Starts.Should().Be(1);
}

public class CustomerTests(Database db) : IPartitionFixture<Database>
{
	[Fact]
	public void SharesTheInstance() => db.Starts.Should().Be(1);
}
```

## Install

```sh
dotnet add package Nullean.Xunit.Partitions.v3   # xUnit v3
dotnet add package Nullean.Xunit.Partitions      # xUnit v2
```

## Quick start

Register the partition test framework once, anywhere in the test project (xUnit v3):

```csharp
using Nullean.Xunit.Partitions.v3;
using Xunit;

[assembly: TestFramework(typeof(PartitionTestFramework))]
```

Then give the shared state an `IPartitionLifetime` and let test classes ask for it with `IPartitionFixture<T>`, as
above: `OrderTests` and `CustomerTests` receive one shared `Database`, after its `InitializeAsync` has run. xUnit's
analyzer reports partition fixtures as xUnit1041; add `<NoWarn>$(NoWarn);xUnit1041</NoWarn>` to the test project.

- **One partition at a time.** No partition runs concurrently with another, by design, so each partition's state
  can run expensive operations (start Docker containers or processes, bootstrap Playwright) in isolation.
- **Concurrent inside a partition.** Unlike xUnit's `ICollectionFixture<T>`, a partition is not a concurrency barrier:
  the test classes that share a partition run concurrently.
- **Just in time.** `InitializeAsync` runs right before a partition's tests and `DisposeAsync` right after, before the
  next partition initialises. Assembly fixtures bootstrap too early and dispose too late.
- **No partition is a partition too.** Test classes without `IPartitionFixture<T>` run together, concurrently, as one
  more partition.
- If `InitializeAsync` throws, every test of the partition fails with the exception and the text of
  `FailureTestOutput()`, such as a server's log.
- `MaxConcurrency` is how many of the partition's test classes run at once. The runner uses the larger of it and its
  default, 4 x the processor count, so only a higher value changes anything; `null` keeps the default.

If you want to share a few (say 0 to 20) long-running objects over thousands of tests, this library will work for
you. If you instead have many test collections each with only a few tests, xUnit's own collections suit better.

Filter which partitions and test classes run with an options class; filtered-out tests are reported as skipped.
`OnBeforeTestsRun()` and `OnTestsFinished(...)` can be overridden for logging or reporting.

```csharp
using Nullean.Xunit.Partitions.v3;

[assembly: PartitionOptions(typeof(MyPartitioningOptions))]

public class MyPartitioningOptions : PartitionOptions
{
	public MyPartitioningOptions()
	{
		PartitionFilterRegex = "Database";   // only run partitions whose lifetime type name matches
		TestFilterRegex = null;              // only run test classes whose name matches (null = all)
	}
}
```

For xUnit v2 the framework is registered with `[assembly: TestFramework(Partition.TestFramework, Partition.Assembly)]`
and the lifetime methods return `Task`; see the [v2 package README](src/Nullean.Xunit.Partitions/README.md) and the
[migration table](src/Nullean.Xunit.Partitions.v3/README.md#migrating-from-v2).

## Packages

| Package | What it is | |
|---|---|---|
| [`Nullean.Xunit.Partitions.v3`](src/Nullean.Xunit.Partitions.v3/README.md) | Partitions for xUnit v3 | [![NuGet](https://img.shields.io/nuget/v/Nullean.Xunit.Partitions.v3?style=flat-square)](https://www.nuget.org/packages/Nullean.Xunit.Partitions.v3) |
| [`Nullean.Xunit.Partitions`](src/Nullean.Xunit.Partitions/README.md) | Partitions for xUnit v2 | [![NuGet](https://img.shields.io/nuget/v/Nullean.Xunit.Partitions?style=flat-square)](https://www.nuget.org/packages/Nullean.Xunit.Partitions) |

## Learn more

- [Releases](https://github.com/nullean/xunit-partitions/releases)
- [Contributing](https://github.com/nullean/.github/blob/main/CONTRIBUTING.md)

MIT licensed. Copyright Nullean and contributors.
