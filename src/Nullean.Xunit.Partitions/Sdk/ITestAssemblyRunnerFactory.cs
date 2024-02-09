using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions.Sdk;

public interface ITestAssemblyRunnerFactory
{
	public XunitTestAssemblyRunner Create(
		ITestAssembly testAssembly,
		IEnumerable<IXunitTestCase> testCases,
		IMessageSink diagnosticMessageSink,
		IMessageSink executionMessageSink,
		ITestFrameworkExecutionOptions executionOptions
	);
}

public class PartitionTestRunnerFactory : ITestAssemblyRunnerFactory
{
	public XunitTestAssemblyRunner Create(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
		IMessageSink diagnosticMessageSink,
		IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions) =>
		new PartitionTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink,
			executionOptions);
}

