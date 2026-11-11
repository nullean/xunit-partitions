# Nullean.Xunit.Partitions

![nullean](https://raw.githubusercontent.com/nullean/nullean.github.io/main/assets/nullean-lockup-light-544x192.png)

Partitions for **xUnit v2**: share a few expensive, long-lived objects across thousands of tests, set up just before the tests that need them and torn down right after, one partition at a time. For xUnit v3, use [Nullean.Xunit.Partitions.v3](https://www.nuget.org/packages/Nullean.Xunit.Partitions.v3).

[![NuGet](https://img.shields.io/nuget/v/Nullean.Xunit.Partitions?style=flat-square)](https://www.nuget.org/packages/Nullean.Xunit.Partitions)

## Install

```sh
dotnet add package Nullean.Xunit.Partitions
```

## Quick start

Register the partition test framework with an assembly-level attribute, anywhere in the test project:

```csharp
using Nullean.Xunit.Partitions;
using Xunit;

[assembly: TestFramework(Partition.TestFramework, Partition.Assembly)]
```

Give the shared state an `IPartitionLifetime`, and let test classes ask for it with `IPartitionFixture<T>`:

```csharp
using AwesomeAssertions;
using Nullean.Xunit.Partitions.Sdk;
using Xunit;

// The shared state: initialised just before its partition's tests run, disposed right after.
public class Database : IPartitionLifetime
{
	private static int _starts;
	public int Starts => _starts;

	public Task InitializeAsync()
	{
		Interlocked.Increment(ref _starts);   // start a container, a server, a browser...
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

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

`OrderTests` and `CustomerTests` receive one shared `Database`, after its `InitializeAsync` has run. xUnit's analyzer
reports partition fixtures as xUnit1041; add `<NoWarn>$(NoWarn);xUnit1041</NoWarn>` to the test project.
`PartitionContext.TestException` holds the exception of the running test, if any.

## How it runs

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

## Providing options

Filter which partitions and test classes run with an options class; filtered-out tests are reported as skipped.
`OnBeforeTestsRun()` and `OnTestsFinished(...)` can be overridden for logging or reporting.

```csharp
using Nullean.Xunit.Partitions;

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

## Learn more

- Source and issues: https://github.com/nullean/xunit-partitions
