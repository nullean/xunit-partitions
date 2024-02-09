// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions.Sdk;

internal class PartitioningTestFrameworkDiscoverer<TOptions> : XunitTestFrameworkDiscoverer
	where TOptions : PartitionOptions, new()
{
	private readonly Type _fixtureOpenGeneric;

	public PartitioningTestFrameworkDiscoverer(
		IAssemblyInfo assemblyInfo,
		ISourceInformationProvider sourceProvider,
		IMessageSink diagnosticMessageSink,
		Type fixtureOpenGeneric
		) : base(assemblyInfo, sourceProvider, diagnosticMessageSink)
	{
		_fixtureOpenGeneric = fixtureOpenGeneric;
		var a = Assembly.Load(new AssemblyName(assemblyInfo.Name));
		Options = PartitionOptionsAttribute.GetOptions<TOptions>(a);
		PartitionRegex = Options.PartitionFilterRegex != null ? new Regex(Options.PartitionFilterRegex) : null;
	}

	private Regex? PartitionRegex { get; }

	private TOptions Options { get; }

	protected override bool FindTestsForType(ITestClass testClass, bool includeSourceInformation,
		IMessageBus messageBus, ITestFrameworkDiscoveryOptions discoveryOptions)
	{
		Options.SetOptions(discoveryOptions);
		return base.FindTestsForType(testClass, includeSourceInformation, messageBus, discoveryOptions);
	}

	protected override bool IsValidTestClass(ITypeInfo type)
	{
		if (PartitionRegex == null) return base.IsValidTestClass(type);

		var partitionFixtureType = PartitioningTestAssemblyRunner.GetPartitionFixtureType(type, _fixtureOpenGeneric);
		return partitionFixtureType == null
			? base.IsValidTestClass(type)
			: PartitionRegex.IsMatch(partitionFixtureType.Name);
	}
}
