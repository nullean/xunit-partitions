using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nullean.Xunit.Partitions.Sdk;
using Xunit;

namespace Nullean.Xunit.Partitions.Tests;

public class InitializeThrowsState : IPartitionLifetime
{
	public readonly bool Initialized = false;
	public Task InitializeAsync() => throw new Exception("BOOM!");
	//public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => Task.CompletedTask;

	public int? MaxConcurrency => null;

	public string FailureTestOutput() => "This class always fails to run InitializeAsync";
}

public class BadStateTests(InitializeThrowsState longLivedObject) : IPartitionFixture<InitializeThrowsState>
{
	[Fact]
	public void StaticInitializedShouldNotIncrease() => longLivedObject.Initialized.Should().BeFalse();

	[Fact]
	public void DisposeShouldNotHaveHappened() => longLivedObject.Initialized.Should().BeFalse();
}
