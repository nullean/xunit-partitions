// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Reflection;

namespace Nullean.Xunit.Partitions;

/// <summary>
///     An assembly attribute that specifies the <see cref="PartitionOptions" />
///     for Xunit tests within the assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class PartitionOptionsAttribute : Attribute
{
	private readonly Type _type;

	/// <summary>
	///     Creates a new instance of <see cref="PartitionOptionsAttribute" />
	/// </summary>
	/// <param name="type">
	///     A type deriving from <see cref="PartitionOptions" /> that specifies the run options
	/// </param>
	public PartitionOptionsAttribute(Type type) => _type = type;

	private TOptions GetOptions<TOptions>() where TOptions : PartitionOptions, new()
	{
		 var options = Activator.CreateInstance(_type) as TOptions;
		 return options ?? new TOptions();
	}

	public static TOptions GetOptions<TOptions>(Assembly assembly) where TOptions : PartitionOptions, new()
	{
		var options = assembly
			.GetCustomAttributes()
			.OfType<PartitionOptionsAttribute?>()
			.FirstOrDefault()
			?.GetOptions<TOptions>()
			?? new TOptions();

		return options;
	}
}
