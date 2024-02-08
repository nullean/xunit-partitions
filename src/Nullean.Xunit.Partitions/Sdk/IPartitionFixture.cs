using Xunit;

namespace Nullean.Xunit.Partitions.Sdk;

// marker interface
// ReSharper disable once UnusedTypeParameter
public interface IPartitionFixture<out TPartition> where TPartition : IPartitionLifetime;

public interface IPartitionLifetime : IAsyncLifetime
{
	public int? MaxConcurrency { get; }
}
