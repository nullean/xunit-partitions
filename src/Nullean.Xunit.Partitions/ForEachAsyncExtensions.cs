using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nullean.Xunit.Partitions;

internal static class ForEachAsyncExtensions
{
	internal static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body) =>
		Task.WhenAll(
			from partition in Partitioner.Create(source).GetPartitions(dop)
			select Task.Run(async delegate
			{
				using (partition)
					while (partition.MoveNext())
						await body(partition.Current).ConfigureAwait(false);
			}));
}
