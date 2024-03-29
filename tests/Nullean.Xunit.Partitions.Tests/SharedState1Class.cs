using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nullean.Xunit.Partitions.Sdk;
using Xunit;

namespace Nullean.Xunit.Partitions.Tests;

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
	public string FailureTestOutput() => "";
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

public class NoStateClass
{
	[Fact]
	public void SimpleTest() => 1.Should().Be(1);
}

public class FailingTests : IDisposable
{
	[Fact(Skip = "Enabling this would fail CI")]
	public void OneIsNotZero() => 1.Should().Be(0);

	public void Dispose() => PartitionContext.TestException.Should().NotBeNull();
}

public class SucceedingTests : IDisposable
{
	[Fact]
	public void OneIsOne() => 1.Should().Be(1);

	public void Dispose() => PartitionContext.TestException.Should().BeNull();
}
