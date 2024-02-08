using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions.Sdk;

public interface ITestFrameworkDiscovererFactory
{
	XunitTestFrameworkDiscoverer Create<TOptions>(
		IAssemblyInfo assemblyInfo, ISourceInformationProvider sourceProvider, IMessageSink diagnosticMessageSink)
		where TOptions : PartitioningRunOptions, new();
}

public class TestFrameworkDiscovererFactory : ITestFrameworkDiscovererFactory
{
	public XunitTestFrameworkDiscoverer Create<TOptions>(
		IAssemblyInfo assemblyInfo, ISourceInformationProvider sourceProvider, IMessageSink diagnosticMessageSink
	)
		where TOptions : PartitioningRunOptions, new() =>
		new PartitioningTestFrameworkDiscoverer<TOptions>(assemblyInfo, sourceProvider, diagnosticMessageSink, typeof(IPartitionFixture<>));
}

