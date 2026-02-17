// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Xunit.v3;

namespace Nullean.Xunit.Partitions.v3.Sdk;

/// <summary>
/// A <see cref="FixtureMappingManager"/> that pre-populates with partition fixture instances
/// using the protected constructor that accepts cached fixture values.
/// </summary>
internal class PartitionFixtureMappingManager : FixtureMappingManager
{
	public PartitionFixtureMappingManager(object[] cachedFixtureValues)
		: base("Partition", cachedFixtureValues)
	{
	}
}
