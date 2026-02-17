// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nullean.Xunit.Partitions.v3.Extensions;
using Xunit.Sdk;
using Xunit.v3;

namespace Nullean.Xunit.Partitions.v3.Sdk;

internal record NullableKeyType
{
	public Type? Type { get; set; }
}

public class PartitionTests
{
	public Type? FixtureLifetimeType { get; set; }
	public IXunitTestCollection Collection { get; set; } = null!;
	public List<IXunitTestCase> TestCases { get; set; } = null!;
}

public class PartitionTestAssemblyRunner : XunitTestAssemblyRunner
{
	private readonly Dictionary<Type, IPartitionLifetime> _partitionFixtureInstances = new();

	//threading guess
	private static int DefaultConcurrency => Environment.ProcessorCount * 4;

	public ConcurrentBag<Tuple<string, string>> FailedCollections { get; } = new();

	public Dictionary<string, Stopwatch> ClusterTotals { get; } = new();

	// Set by the executor before calling Run
	internal Regex? PartitionRegex { get; set; }
	internal Regex? TestRegex { get; set; }

	private Dictionary<NullableKeyType, IEnumerable<PartitionTests>>? _partitions;

	private Dictionary<NullableKeyType, IEnumerable<PartitionTests>> BuildPartitions(
		XunitTestAssemblyRunnerContext ctxt)
	{
		var orderedCollections = OrderTestCollections(ctxt);

		var cases =
			from testCollection in orderedCollections
			let classes = testCollection.Item2
				.Select(tc => tc.TestClass?.Class)
				.Where(t => t != null)
				.Distinct()
			let partition = classes.Select(c => GetPartitionFixtureType(c!)).FirstOrDefault(p => p != null)
			let testcase = new PartitionTests
			{
				Collection = testCollection.Item1,
				TestCases = testCollection.Item2,
				FixtureLifetimeType = partition
			}
			select testcase;

		return cases
			.GroupBy(c => c.FixtureLifetimeType)
			.OrderBy(g => g.Count())
			.ToDictionary(k => new NullableKeyType { Type = k.Key }, v => v.Select(g => g));
	}

	protected override async ValueTask<RunSummary> RunTestCollections(
		XunitTestAssemblyRunnerContext ctxt,
		Exception? exception)
	{
		_partitions = BuildPartitions(ctxt);

		// Pre-instantiate partition fixture types for constructor injection
		foreach (var partition in _partitions)
		{
			var partitionType = partition.Key.Type;
			if (partitionType == null)
				continue;

			var state = CreatePartitionStateInstance(partitionType, ctxt);
			if (state == null) continue;

			_partitionFixtureInstances[partitionType] = state;
		}

		return await RunAllTests(ctxt).ConfigureAwait(false);
	}

	private async ValueTask<RunSummary> RunAllTests(XunitTestAssemblyRunnerContext ctxt)
	{
		var totalSummary = new RunSummary();

		foreach (var partitioning in _partitions!)
		{
			var partitionType = partitioning.Key.Type;
			if (partitionType == null)
			{
				var summary = await RunPartitionGroupConcurrently(partitioning.Value, DefaultConcurrency, ctxt).ConfigureAwait(false);
				totalSummary.Aggregate(summary);
				continue;
			}

			var state = CreatePartitionStateInstance(partitionType, ctxt);
			if (state == null)
			{
				var testClass = partitioning.Value.Select(g => g.Collection.TestCollectionDisplayName).FirstOrDefault();
				throw new Exception($"{typeof(IPartitionLifetime)} did not yield partition state for e.g: {testClass}");
			}

			var partitionName = state.GetType().Name;
			if (PartitionRegex != null && !PartitionRegex.IsMatch(partitionName))
			{
				var skipSummary = SkipAll(ctxt, partitioning.Value, $"Unmatched: '{PartitionRegex}' for partition: '{partitionName}'");
				totalSummary.Aggregate(skipSummary);
				continue;
			}

			var skipReasons = partitioning.Value.SelectMany(g => g.TestCases.Select(t => t.SkipReason)).ToList();
			var allSkipped = skipReasons.All(r => !string.IsNullOrWhiteSpace(r));
			if (allSkipped)
			{
				var s = new RunSummary { Total = skipReasons.Count, Skipped = skipReasons.Count };
				totalSummary.Aggregate(s);
				continue;
			}

			ClusterTotals.Add(partitionName, Stopwatch.StartNew());

			var partitionSummary = new RunSummary();
			await UseStateAndRun(state, ctxt, async concurrency =>
			{
				var s = await RunPartitionGroupConcurrently(partitioning.Value, concurrency, ctxt)
					.ConfigureAwait(false);
				partitionSummary.Aggregate(s);
			}, async (e, f) =>
			{
				var s = await FailAll(ctxt, partitioning.Value, e, f);
				partitionSummary.Aggregate(s);
			}).ConfigureAwait(false);
			totalSummary.Aggregate(partitionSummary);

			// Track failures for OnTestsFinished callback
			if (partitionSummary.Failed > 0)
			{
				foreach (var g in partitioning.Value)
				{
					var test = g.Collection.TestCollectionDisplayName.Replace("Test collection for", string.Empty).Trim();
					FailedCollections.Add(Tuple.Create(partitionName, test));
				}
			}

			ClusterTotals[partitionName].Stop();
		}

		return totalSummary;
	}

	private async ValueTask UseStateAndRun(
		IPartitionLifetime partition,
		XunitTestAssemblyRunnerContext ctxt,
		Func<int?, Task> runGroup,
		Func<Exception, string, Task> failAll)
	{
		var initialized = false;
		try
		{
			await partition.InitializeAsync().ConfigureAwait(false);
			initialized = true;
		}
		catch (Exception e)
		{
			ctxt.MessageBus.QueueMessage(new DiagnosticMessage(e.ToString()));
			await failAll(e, partition.FailureTestOutput());
		}

		if (initialized)
			await runGroup(partition.MaxConcurrency).ConfigureAwait(false);

		await partition.DisposeAsync().ConfigureAwait(false);
	}

	protected override async ValueTask<RunSummary> RunTestCollection(
		XunitTestAssemblyRunnerContext ctxt,
		IXunitTestCollection testCollection,
		IReadOnlyCollection<IXunitTestCase> testCases)
	{
		// Collect all fixture instances: assembly fixtures + partition fixtures
		var allFixtures = new List<object>();
		foreach (var type in ctxt.TestAssembly.AssemblyFixtureTypes)
		{
			var fixture = await ctxt.AssemblyFixtureMappings.GetFixture(type);
			if (fixture != null)
				allFixtures.Add(fixture);
		}
		foreach (var kv in _partitionFixtureInstances)
			allFixtures.Add(kv.Value);

		// Create a combined fixture manager with all fixtures pre-populated
		var combinedManager = new PartitionFixtureMappingManager(allFixtures.ToArray());

		return await XunitTestCollectionRunner.Instance.Run(
			testCollection,
			testCases,
			ctxt.ExplicitOption,
			ctxt.MessageBus,
			ctxt.AssemblyTestCaseOrderer ?? DefaultTestCaseOrderer.Instance,
			ctxt.Aggregator,
			ctxt.CancellationTokenSource,
			combinedManager
		).ConfigureAwait(false);
	}

	private async Task<RunSummary> RunPartitionGroupConcurrently(
		IEnumerable<PartitionTests> source, int? concurrency, XunitTestAssemblyRunnerContext ctxt
	)
	{
		var summaries = new ConcurrentBag<RunSummary>();

		await source
			.ForEachAsync(Math.Max(concurrency ?? 0, DefaultConcurrency),
				async g =>
				{
					var summary = await ExecutePartitionGrouping(ctxt, g).ConfigureAwait(false);
					summaries.Add(summary);
				}
			)
			.ConfigureAwait(false);

		var result = new RunSummary();
		foreach (var s in summaries)
			result.Aggregate(s);
		return result;
	}

	private async Task<RunSummary> ExecutePartitionGrouping(
		XunitTestAssemblyRunnerContext ctxt,
		PartitionTests g
	)
	{
		var test = g.Collection.TestCollectionDisplayName.Replace("Test collection for", string.Empty).Trim();

		if (TestRegex != null && !TestRegex.IsMatch(test))
		{
			var cases = g.TestCases.Cast<ITestCase>().ToList();
			XunitRunnerHelper.SkipTestCases(
				ctxt.MessageBus, ctxt.CancellationTokenSource, cases,
				$"Unmatched: '{TestRegex}', test class: '{test}'",
				sendTestCollectionMessages: true,
				sendTestClassMessages: true,
				sendTestMethodMessages: true,
				sendTestCaseMessages: true,
				sendTestMessages: true
			);
			return new RunSummary { Total = cases.Count, Skipped = cases.Count };
		}

		try
		{
			var summary = await RunTestCollection(ctxt, g.Collection, g.TestCases).ConfigureAwait(false);
			if (summary.Failed > 0)
			{
				var type = g.FixtureLifetimeType;
				var partitionName = type?.Name ?? "UNKNOWN";
				FailedCollections.Add(Tuple.Create(partitionName, test));
			}
			return summary;
		}
		catch (TaskCanceledException)
		{
			return new RunSummary();
		}
	}

	private RunSummary SkipAll(XunitTestAssemblyRunnerContext ctxt, IEnumerable<PartitionTests> source, string skipText)
	{
		var testCases = source.SelectMany(t => t.TestCases).Cast<ITestCase>().ToList();
		// Send skip messages for console display
		XunitRunnerHelper.SkipTestCases(
			ctxt.MessageBus, ctxt.CancellationTokenSource, testCases, skipText,
			sendTestCollectionMessages: true,
			sendTestClassMessages: true,
			sendTestMethodMessages: true,
			sendTestCaseMessages: true,
			sendTestMessages: true
		);
		// Return our own summary — XunitRunnerHelper may count skips as failures
		// when FailSkips is enabled, so we manage our own count
		return new RunSummary { Total = testCases.Count, Skipped = testCases.Count };
	}

	private Task<RunSummary> FailAll(
		XunitTestAssemblyRunnerContext ctxt,
		IEnumerable<PartitionTests> source,
		Exception e,
		string failureText)
	{
		var testCases = source.SelectMany(t => t.TestCases).Cast<ITestCase>().ToList();
		var summary = XunitRunnerHelper.FailTestCases(
			ctxt.MessageBus, ctxt.CancellationTokenSource, testCases, e,
			sendTestCollectionMessages: false,
			sendTestClassMessages: false,
			sendTestMethodMessages: false,
			sendTestCaseMessages: true,
			sendTestMessages: true
		);
		return Task.FromResult(summary);
	}

	private IPartitionLifetime? CreatePartitionStateInstance(Type partitionType, XunitTestAssemblyRunnerContext ctxt)
	{
		if (_partitionFixtureInstances.TryGetValue(partitionType, out var partition)) return partition;
		ctxt.Aggregator.Run(() =>
		{
			var o = Activator.CreateInstance(partitionType);
			partition = o as IPartitionLifetime;
		});
		if (partition != null)
			_partitionFixtureInstances[partitionType] = partition;
		return partition;
	}

	public static Type? GetPartitionFixtureType(Type testClass) =>
		testClass.GetInterfaces()
			.Where(i => i.IsGenericType)
			.Where(t => t.GetGenericTypeDefinition() == typeof(IPartitionFixture<>))
			.SelectMany(t => t.GetGenericArguments(), (_, a) => a)
			.FirstOrDefault();
}
