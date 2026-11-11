---
name: xunit-partitions
description: >-
  Use when xUnit v2 tests need expensive shared state (a container, a cluster, a server, a browser) used by many test
  classes, started just before those tests and stopped right after. Nullean.Xunit.Partitions is a test framework
  that groups test classes into partitions: one partition at a time, its tests concurrently.
---

# Nullean.Xunit.Partitions

For **xUnit v2**. For xUnit v3 use `Nullean.Xunit.Partitions.v3`. Partitions suit a few long-lived resources shared
by many tests; for many small groups, xUnit's own collection fixtures fit better.

## The calls you need

Register the framework once per test assembly:

```csharp
using Nullean.Xunit.Partitions;
using Xunit;

[assembly: TestFramework(Partition.TestFramework, Partition.Assembly)]
```

A partition is an `IPartitionLifetime`; a test class joins it by implementing `IPartitionFixture<T>` and taking `T`
in its constructor:

```csharp
using Nullean.Xunit.Partitions.Sdk;

public class DatabaseLifetime : IPartitionLifetime
{
    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync() => ConnectionString = await StartDatabaseAsync(); // just before its tests
    public Task DisposeAsync() => StopDatabaseAsync();                                  // right after them
    public int? MaxConcurrency => null;
    public string FailureTestOutput() => "the database container did not start";       // shown on every test if InitializeAsync throws
}

public class QueryTests(DatabaseLifetime db) : IPartitionFixture<DatabaseLifetime>
{
    [Fact]
    public void HasConnection() => Assert.NotEmpty(db.ConnectionString);
}
```

Optional filters and hooks, through an assembly attribute and a `PartitionOptions` subclass:

```csharp
[assembly: PartitionOptions(typeof(MyPartitionOptions))]

public class MyPartitionOptions : PartitionOptions
{
    public MyPartitionOptions() => PartitionFilterRegex = "^DatabaseLifetime$";

    public override void OnBeforeTestsRun() => Console.WriteLine("starting");
}
```

## Gotchas

- `IPartitionFixture<T>` and `IPartitionLifetime` are in `Nullean.Xunit.Partitions.Sdk`; the attribute,
  `Partition` and `PartitionOptions` are in `Nullean.Xunit.Partitions`.
- The lifetime needs a public parameterless constructor and is created when the run starts, before any test runs:
  do the work in `InitializeAsync`, never in the constructor.
- xUnit's analyzer does not know the partition supplies the constructor argument and reports xUnit1041 on it
  (an error under warnings-as-errors). Suppress it in the test project: `<NoWarn>$(NoWarn);xUnit1041</NoWarn>`.
- If `InitializeAsync` throws, every test in that partition is reported failed with `FailureTestOutput()` as output.
- `MaxConcurrency` cannot lower parallelism: the runner uses `Math.Max(MaxConcurrency ?? 0, ProcessorCount * 4)`
  test classes at a time. Do not rely on it to serialize tests.
- `PartitionFilterRegex` matches the lifetime's short type name; `TestFilterRegex` matches the test class's full
  name. Tests that do not match are reported skipped, not hidden.
- Test classes without `IPartitionFixture<T>` form one more partition of their own, run concurrently.
