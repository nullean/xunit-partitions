// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions.v3;

/// <summary> Control how execution of the partitioning test pipeline </summary>
public class PartitionOptions
{
	/// <summary> A positive regular expression to filter tests, ONLY matches will run</summary>
	public string? TestFilterRegex { get; set; }

	/// <summary> A positive regular expression to filter partitions, ONLY matches will run</summary>
	public string? PartitionFilterRegex { get; set; }

	// ReSharper disable UnusedParameter.Global
	/// <summary> Called when the tests have finished running successfully </summary>
	/// <param name="partitionTimings">Per cluster timings of the total test time, including starting Elasticsearch</param>
	/// <param name="failedPartitionTests">All collection of failed cluster, failed tests tuples</param>
	public virtual void OnTestsFinished(
		Dictionary<string, Stopwatch> partitionTimings,
		ConcurrentBag<Tuple<string, string>> failedPartitionTests)
	{
	}
	// ReSharper restore UnusedParameter.Global

	/// <summary>
	/// Called before tests run. An ideal place to perform actions such as writing information to
	/// <see cref="Console" />.
	/// </summary>
	public virtual void OnBeforeTestsRun() { }

	/// <summary> Expert option allows custom test runners to receive more options </summary>
	public virtual void SetOptions(ITestFrameworkDiscoveryOptions discoveryOptions)
	{
		discoveryOptions.SetValue(nameof(PartitionFilterRegex), PartitionFilterRegex);
		discoveryOptions.SetValue(nameof(TestFilterRegex), TestFilterRegex);
	}

	/// <summary> Expert option allows custom test runners to receive more options </summary>
	public virtual void SetOptions(ITestFrameworkExecutionOptions executionOptions)
	{
		executionOptions.SetValue(nameof(PartitionFilterRegex), PartitionFilterRegex);
		executionOptions.SetValue(nameof(TestFilterRegex), TestFilterRegex);
	}
}
