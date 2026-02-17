// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Nullean.Xunit.Partitions.v3.Sdk;

internal class PartitionTestFrameworkExecutor<TOptions>(
	TOptions options,
	IXunitTestAssembly testAssembly
)
	: XunitTestFrameworkExecutor(testAssembly)
	where TOptions : PartitionOptions, new()
{
	private TOptions Options { get; } = options;

	public override async ValueTask RunTestCases(
		IReadOnlyCollection<IXunitTestCase> testCases,
		IMessageSink executionMessageSink,
		ITestFrameworkExecutionOptions executionOptions,
		CancellationToken cancellationToken)
	{
		Options.SetOptions(executionOptions);
		try
		{
			Options.OnBeforeTestsRun();
			var runner = new PartitionTestAssemblyRunner();

			// Set partition filter options directly on the runner
			if (!string.IsNullOrEmpty(Options.PartitionFilterRegex))
				runner.PartitionRegex = new Regex(Options.PartitionFilterRegex);
			if (!string.IsNullOrEmpty(Options.TestFilterRegex))
				runner.TestRegex = new Regex(Options.TestFilterRegex);

			await runner.Run(
				TestAssembly,
				testCases,
				executionMessageSink,
				executionOptions,
				cancellationToken
			).ConfigureAwait(false);
			Options.OnTestsFinished(runner.ClusterTotals, runner.FailedCollections);
		}
		catch (Exception e)
		{
			executionMessageSink.OnMessage(ErrorMessage.FromException(e));
			throw;
		}
	}
}
