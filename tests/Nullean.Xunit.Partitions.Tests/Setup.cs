using Nullean.Xunit.Partitions;
using Nullean.Xunit.Partitions.Tests;
using Xunit;

[assembly: TestFramework("Nullean.Xunit.Partitions.PartitioningTestFramework", "Nullean.Xunit.Partitions")]
[assembly: PartitioningConfiguration(typeof(MyRunOptions))]

namespace Nullean.Xunit.Partitions.Tests;

/// <summary>
///     Allows us to control the custom xunit test pipeline
/// </summary>
public class MyRunOptions : PartitioningRunOptions
{
	public MyRunOptions()
	{
		PartitionFilterRegex = "LongLivedObject";
		TestFilterRegex = null;
	}
}
