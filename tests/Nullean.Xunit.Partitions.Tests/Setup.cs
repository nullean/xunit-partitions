using Nullean.Xunit.Partitions;
using Nullean.Xunit.Partitions.Tests;
using Xunit;

[assembly: TestFramework(Partition.TestFramework, Partition.Assembly)]
//optional only needed if you want to specify execution options to PartitionTestFramework
[assembly: PartitionOptions(typeof(MyPartitioningOptions))]

namespace Nullean.Xunit.Partitions.Tests;

/// <summary> Allows us to control the xunit partitioning test pipeline </summary>
public class MyPartitioningOptions : PartitionOptions
{
	public MyPartitioningOptions()
	{
		PartitionFilterRegex = "^(?!InitializeThrowsState$).*";
		TestFilterRegex = null;
	}
}
