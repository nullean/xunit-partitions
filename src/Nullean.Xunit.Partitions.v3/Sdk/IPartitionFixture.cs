// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Xunit;

namespace Nullean.Xunit.Partitions.v3.Sdk;

// marker interface
// ReSharper disable once UnusedTypeParameter
public interface IPartitionFixture<out TLifetime> where TLifetime : IPartitionLifetime;

/// <summary>
/// The object that provides long running state to tests.
/// <para>Avoid work in constructors, state has to be initialized early to satisfy XUnit constructor injection</para>
/// <para>Utilize <see cref="IAsyncLifetime.InitializeAsync"/> to bootstrap over constructors</para>
/// <para>Utilize <see cref="IAsyncLifetime.DisposeAsync"/> to wind down state</para>
/// <para>In v3, use <c>TestContext.Current.TestState</c> to access test exception state
/// instead of the v2 <c>PartitionContext.TestException</c></para>
/// </summary>
public interface IPartitionLifetime : IAsyncLifetime
{
	int? MaxConcurrency { get; }

	/// <summary>
	/// Allows a partition to report output to tests if <see cref="IAsyncLifetime.InitializeAsync"/>
	/// throws an exception.
	/// </summary>
	string FailureTestOutput();
}
