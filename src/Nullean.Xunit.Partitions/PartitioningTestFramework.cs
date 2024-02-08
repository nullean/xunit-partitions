// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Nullean.Xunit.Partitions.Sdk;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions;

// ReSharper disable once UnusedType.Global
/// <summary>
///
/// </summary>
public class PartitioningTestFramework
	: PartitioningTestFramework<PartitioningRunOptions, PartitioningTestRunnerFactory, TestFrameworkDiscovererFactory>
{
	public PartitioningTestFramework(IMessageSink messageSink) : base(messageSink) { }
}

public abstract class PartitioningTestFramework<TOptions, TRunnerFactory, TDiscoverFactory> : XunitTestFramework
	where TOptions : PartitioningRunOptions, new()
	where TRunnerFactory : ITestAssemblyRunnerFactory, new()
	where TDiscoverFactory : ITestFrameworkDiscovererFactory, new()
{
	protected PartitioningTestFramework(IMessageSink messageSink) : base(messageSink) =>
		DiscoverFactory = new TDiscoverFactory();

	private TDiscoverFactory DiscoverFactory { get; }

	protected override ITestFrameworkDiscoverer CreateDiscoverer(IAssemblyInfo assemblyInfo) =>
		DiscoverFactory.Create<TOptions>(assemblyInfo, SourceInformationProvider, DiagnosticMessageSink);

	protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
	{
		var assembly = Assembly.Load(assemblyName);
		var options = PartitioningConfigurationAttribute.GetOptions<TOptions>(assembly);

		return new PartitioningTestFrameworkExecutor<TOptions, TRunnerFactory>(options, assemblyName, SourceInformationProvider, DiagnosticMessageSink);
	}

}
