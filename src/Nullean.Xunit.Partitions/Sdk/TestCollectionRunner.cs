// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions.Sdk;

internal class TestCollectionRunner(
	Dictionary<Type, object> assemblyFixtureMappings,
	ITestCollection testCollection,
	IEnumerable<IXunitTestCase> testCases,
	IMessageSink diagnosticMessageSink,
	IMessageBus messageBus,
	ITestCaseOrderer testCaseOrderer,
	ExceptionAggregator aggregator,
	CancellationTokenSource ctx
)
	: XunitTestCollectionRunner(
		testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, ctx
	)
{
	private readonly IMessageSink _diagnosticMessageSink = diagnosticMessageSink;

	protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class,
		IEnumerable<IXunitTestCase> testCases)
	{
		// this ensures xunit can constructor inject our partition types
		var combinedFixtures = new Dictionary<Type, object>(assemblyFixtureMappings);
		foreach (var kvp in CollectionFixtureMappings)
			combinedFixtures[kvp.Key] = kvp.Value;

		// We've done everything we need, so hand back off to default Xunit implementation for class runner
		return new XunitTestClassRunner(testClass, @class, testCases, _diagnosticMessageSink, MessageBus,
				TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource, combinedFixtures)
			.RunAsync();
	}
}
