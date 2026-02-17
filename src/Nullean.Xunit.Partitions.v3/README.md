# Nullean.Xunit.Partitions.v3

<a href="https://www.nuget.org/packages/Nullean.Xunit.Partitions.v3/"><img src="https://img.shields.io/nuget/v/Nullean.Xunit.Partitions.v3?color=blue&style=plastic" /></a>

An xUnit v3 `TestFramework` implementation that introduces the concept of **partitions** for sharing long-lived state across tests.

> This is the **xUnit v3** version. For xUnit v2, use [`Nullean.Xunit.Partitions`](https://www.nuget.org/packages/Nullean.Xunit.Partitions/).

## Why partitions?

`IPartitionFixture<TLifetime>` lets tests inject a long-lived object (Docker container, database, Playwright browser, etc.) that is shared across multiple test classes.

- **Only one partition runs at a time** &mdash; partitions execute serially, so expensive resources don't compete.
- **Tests within a partition run concurrently** &mdash; unlike `ICollectionFixture<T>`, partitions are not a concurrency barrier.
- **Just-in-time lifecycle** &mdash; `InitializeAsync` is called right before a partition's tests run, and `DisposeAsync` right after. This is more efficient than assembly fixtures which bootstrap at startup and dispose at shutdown.
- **Per-partition concurrency control** &mdash; each `IPartitionLifetime` can declare its own `MaxConcurrency`.

If you want to share a few (0&ndash;20) long-running objects over thousands of tests, this library is for you.
If you have many test collections with only a few tests each, xUnit's native collection fixtures may be a better fit.

## Setup

xUnit v3 test projects are executables. Your `.csproj` should look like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="Nullean.Xunit.Partitions.v3" Version="*" />
  </ItemGroup>
</Project>
```

Register the test framework with an assembly-level attribute:

```csharp
using Nullean.Xunit.Partitions.v3;
using Xunit;

[assembly: TestFramework(typeof(PartitionTestFramework))]
```

## Usage

Define a partition lifetime &mdash; the shared state that will be initialized once and injected into all test classes that request it:

```csharp
public class LongLivedObject : IPartitionLifetime
{
    private static long _initialized;
    private static long _disposed;

    public long Initialized => _initialized;
    public long Disposed => _disposed;

    public ValueTask InitializeAsync()
    {
        Interlocked.Increment(ref _initialized);
        return default;
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposed);
        return default;
    }

    public int? MaxConcurrency => null;
    public string FailureTestOutput() => "";
}
```

Test classes receive the shared instance via constructor injection by implementing `IPartitionFixture<T>`:

```csharp
public class SharedState1Class(LongLivedObject state) : IPartitionFixture<LongLivedObject>
{
    [Fact]
    public void InitializedOnce() => state.Initialized.Should().Be(1);

    [Fact]
    public void NotDisposedYet() => state.Disposed.Should().Be(0);
}

public class SharedState2Class(LongLivedObject state) : IPartitionFixture<LongLivedObject>
{
    [Fact]
    public void InitializedOnce() => state.Initialized.Should().Be(1);

    [Fact]
    public void NotDisposedYet() => state.Disposed.Should().Be(0);
}
```

Both classes share a single `LongLivedObject` instance. Their tests run concurrently within the partition. `DisposeAsync` runs only after all tests in the partition complete.

Test classes without `IPartitionFixture<T>` are grouped into an implicit "no partition" group and run concurrently with default concurrency.

## Providing options

Control partition and test filtering with a custom options class:

```csharp
using Nullean.Xunit.Partitions.v3;
using Xunit;

[assembly: TestFramework(typeof(PartitionTestFramework))]
[assembly: PartitionOptions(typeof(MyOptions))]

public class MyOptions : PartitionOptions
{
    public MyOptions()
    {
        // Only run partitions whose type name matches this regex
        PartitionFilterRegex = "LongLivedObject";
        // Only run test classes whose name matches this regex (null = all)
        TestFilterRegex = null;
    }
}
```

You can also override `OnBeforeTestsRun()` and `OnTestsFinished(...)` for custom logging or reporting.

## Migrating from v2

| v2 | v3 |
|---|---|
| `[assembly: TestFramework(Partition.TestFramework, Partition.Assembly)]` | `[assembly: TestFramework(typeof(PartitionTestFramework))]` |
| `Task InitializeAsync()` / `Task DisposeAsync()` | `ValueTask InitializeAsync()` / `ValueTask DisposeAsync()` |
| `PartitionContext.TestException` | `TestContext.Current.TestState` (xUnit v3 built-in) |
| `return Task.CompletedTask;` | `return default;` |
| `Nullean.Xunit.Partitions` namespace | `Nullean.Xunit.Partitions.v3` namespace |
| Test project is a class library | Test project is an executable (`<OutputType>Exe</OutputType>`) |
| Requires `xunit.runner.visualstudio` + `Microsoft.NET.Test.Sdk` | Self-hosting &mdash; no runner packages needed |

## License

MIT
