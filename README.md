# Nullean.Xunit.Partitions

<a href="https://www.nuget.org/packages/Nullean.Xunit.Partitions/"><img src="https://img.shields.io/nuget/v/Nullean.Xunit.Partitions?color=blue&style=plastic" /></a>

<img src="https://github.com/nullean/xunit-partitions/raw/main/nuget-icon.png" align="right"
title="Logo " width="220" height="220">

An `XunitTestFramework` implementation that introduces the concept of "partitions".

A `IPartitionFixture{TLifetime}` allows tests to inject a long lived object to share.

Only a single partition will run at a time in contrast with xUnit's `ICollectionFixture{TFixture}`
However unlike `ICollectionFixture{TFixture}` this library's `IPartitionFixture{TLifetime}`
does not mark a concurrency barrier, tests belonging to a single **partition still run concurrently**

In fact each `IPartitionFixture{TLifetime}` can declare its own desired concurrency through
`IPartitionLifetime.MaxConcurrency`

If you want to share a few (say 0-20) long running objects over 1000's of tests this library will work for you. 
If you instead have many test collections each with only a few tests xUnit native collections will suit better.

Because each partititon only calls `InitializeAsync` and `DisposeAsync` just before and after its test will run this makes it more appropiate then assembly fixtures which might bootstrap too early and dispose too late.

## Setup

Provide the following Assembly level attribute anywhere in your test project.

```csharp
using Nullean.Xunit.Partitions;
using Xunit;
[assembly: TestFramework(Partition.TestFramework, Partition.Assembly)]
```

This will ensure xUnit bootstraps the partition test framework shipped with this library.


## Providing options

Options to control the parition test framework can be provided similarly through the `PartitonOptions` Assembly level
attribute.

Here you can control filters to only run certain partitions and/or tests.

#### Setup.cs
```csharp
using Nullean.Xunit.Partitions;
using My.Tests;
using Xunit;

[assembly: TestFramework(Partition.TestFramework, Partition.Assembly)]
//optional only needed if you want to specify execution options to PartitionTestFramework
[assembly: PartitionOptions(typeof(MyPartitioningOptions))]

namespace My.Tests;

/// <summary> Allows us to control the xunit partitioning test pipeline </summary>
public class MyPartitioningOptions : PartitionOptions
{
	public MyPartitioningOptions()
	{
		PartitionFilterRegex = "LongLivedObject";
		TestFilterRegex = null;
	}
}

```

## Usage

```csharp
namespace Nullean.Xunit.Partitions.Tests;


public class NoStateClass
{
	[Fact]
	public void SimpleTest() => 1.Should().Be(1);
}

public class LongLivedObject : IPartitionLifetime
{
	private static long _initialized;
	private static long _disposed;

	public long Initialized => _initialized;
	public long Disposed => _disposed;

	public Task InitializeAsync()
	{
		Interlocked.Increment(ref _initialized);
		return Task.CompletedTask;
	}

	public Task DisposeAsync()
	{
		Interlocked.Increment(ref _disposed);
		return Task.CompletedTask;
	}

	public int? MaxConcurrency => null;
}

public class SharedState1Class(LongLivedObject longLivedObject) : IPartitionFixture<LongLivedObject>
{
	[Fact]
	public void StaticInitializedShouldNotIncrease() => longLivedObject.Initialized.Should().Be(1);

	[Fact]
	public void DisposeShouldNotHaveHappened() => longLivedObject.Disposed.Should().Be(0);
}

public class SharedState2Class(LongLivedObject longLivedObject) : IPartitionFixture<LongLivedObject>
{
	[Fact]
	public void StaticInitializedShouldNotIncrease() => longLivedObject.Initialized.Should().Be(1);

	[Fact]
	public void DisposeShouldNotHaveHappened() => longLivedObject.Disposed.Should().Be(0);
}

```

`SharedState1Class` and `SharedState2Class` both depend on `LongLivedObject` and so will receive a single shared 
instance AFTER `InitializeAsync` has run. The tests of each **will** run concurrently. 

`DisposeAsync` will run before the next partition's state will `InitializeAsync`


`NoStateClass` does not belong to any partition. All tests with no partitions are treated as part of an empty partition. 
These tests will all run concurrently too.

However no partition will ever run concurrently with another **by design**. 

This allows partition's state to run expensive operations (e.g starting docker containers, processes, bootstrap playwright states),
in isolation.