// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Nullean.Xunit.Partitions.v3.Sdk;
using Xunit.Sdk;
using Xunit.v3;

namespace Nullean.Xunit.Partitions.v3;

// ReSharper disable once UnusedType.Global
/// <summary>
/// An <see cref="XunitTestFramework"/> implementation that introduces the concept of partitions.
/// <para>A <see cref="IPartitionFixture{TLifetime}"/> allows tests to inject a long lived object.</para>
/// <para>Only a single partition will run at a time in contrast with collection fixtures</para>
/// <para>However unlike collection fixtures, <see cref="IPartitionFixture{TLifetime}"/>
/// does not mark a concurrency barrier, tests belonging to a single partition still run concurrently</para>
/// <para>Each <see cref="IPartitionFixture{TLifetime}"/> can declare its own desired concurrency through
/// <see cref="IPartitionLifetime.MaxConcurrency"/> </para>
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class PartitionTestFramework : PartitionTestFramework<PartitionOptions>;

public class PartitionTestFramework<TOptions> : XunitTestFramework
	where TOptions : PartitionOptions, new()
{
	protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly)
	{
		var options = PartitionOptionsAttribute.GetOptions<TOptions>(assembly);
		var testAssembly = new XunitTestAssembly(assembly);
		return new PartitionTestFrameworkExecutor<TOptions>(options, testAssembly);
	}
}
