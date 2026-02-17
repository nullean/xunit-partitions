using Nullean.Xunit.Partitions.v3;
using Nullean.Xunit.Partitions.v3.Tests;
using Xunit;

[assembly: TestFramework(typeof(PartitionTestFramework))]
//optional only needed if you want to specify execution options to PartitionTestFramework
[assembly: PartitionOptions(typeof(MyPartitioningOptions))]

namespace Nullean.Xunit.Partitions.v3.Tests;

/// <summary> Allows us to control the xunit partitioning test pipeline </summary>
public class MyPartitioningOptions : PartitionOptions
{
	public MyPartitioningOptions()
	{
		PartitionFilterRegex = "^(?!InitializeThrowsState$).*";
		TestFilterRegex = null;
	}
}
