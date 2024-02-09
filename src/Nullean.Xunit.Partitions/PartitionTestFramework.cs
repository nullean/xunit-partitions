// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Nullean.Xunit.Partitions.Sdk;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions;


public static class Partition
{
	public const string Assembly = $"{nameof(Nullean)}.{nameof(Xunit)}.{nameof(Partitions)}";
	public const string TestFramework = $"{Assembly}.{nameof(PartitionTestFramework)}";
}

// ReSharper disable once UnusedType.Global
/// <summary>
/// An <see cref="XunitTestFramework"/> implementation that introduces the concept of partitions.
/// <para>A <see cref="IPartitionFixture{TLifetime}"/> allows tests to inject a long lived object.</para>
/// <para>Only a single partition will run at a time in contrast with <see cref="ICollectionFixture{TFixture}"/></para>
/// <para>However unlike <see cref="ICollectionFixture{TFixture}"/> <see cref="IPartitionFixture{TLifetime}"/>
/// does not mark a concurrency barrier, tests belonging to a single partition still run concurrently</para>
/// <para>In fact each <see cref="IPartitionFixture{TLifetime}"/> can declare its own desired concurrency through
/// <see cref="IPartitionLifetime.MaxConcurrency"/> </para>
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class PartitionTestFramework(IMessageSink sink)
	: PartitionTestFramework<PartitionOptions, PartitioningTestRunnerFactory, TestFrameworkDiscovererFactory>
		(sink);

public abstract class PartitionTestFramework<TOptions, TRunnerFactory, TDiscoverFactory>(IMessageSink sink)
	: XunitTestFramework(sink)
	where TOptions : PartitionOptions, new()
	where TRunnerFactory : ITestAssemblyRunnerFactory, new()
	where TDiscoverFactory : ITestFrameworkDiscovererFactory, new()
{
	private TDiscoverFactory DiscoverFactory { get; } = new();

	protected override ITestFrameworkDiscoverer CreateDiscoverer(IAssemblyInfo assemblyInfo) =>
		DiscoverFactory.Create<TOptions>(assemblyInfo, SourceInformationProvider, DiagnosticMessageSink);

	protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
	{
		var assembly = Assembly.Load(assemblyName);
		var options = PartitionOptionsAttribute.GetOptions<TOptions>(assembly);

		return new PartitioningTestFrameworkExecutor<TOptions, TRunnerFactory>(options, assemblyName, SourceInformationProvider, DiagnosticMessageSink);
	}

}
