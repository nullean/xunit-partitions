using System;
using System.Threading.Tasks;
using FluentAssertions;
using Nullean.Xunit.Partitions.v3.Sdk;
using Xunit;

namespace Nullean.Xunit.Partitions.v3.Tests;

public class InitializeThrowsState : IPartitionLifetime
{
	public readonly bool Initialized = false;
	public ValueTask InitializeAsync() => throw new Exception("BOOM!");

	public ValueTask DisposeAsync() => default;

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
