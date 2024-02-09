using Xunit;

namespace Nullean.Xunit.Partitions.Sdk;

// marker interface
// ReSharper disable once UnusedTypeParameter
public interface IPartitionFixture<out TLifetime> where TLifetime : IPartitionLifetime;

/// <summary>
/// The object that provides long running state to tests.
/// <para>Avoid work in constructors, state has to be initialized early to satisfy XUnit constructor injection</para>
/// <para>Utilize <see cref="IAsyncLifetime.InitializeAsync"/> to bootstrap over constructors</para>
/// <para>Utilize <see cref="IAsyncLifetime.DisposeAsync"/> to wind down state</para>
/// </summary>
public interface IPartitionLifetime : IAsyncLifetime
{
	public int? MaxConcurrency { get; }
}
