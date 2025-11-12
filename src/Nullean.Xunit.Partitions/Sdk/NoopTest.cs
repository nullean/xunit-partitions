using System;
using Xunit.Abstractions;

namespace Nullean.Xunit.Partitions.Sdk;

[Serializable]
internal class NoopTest(ITestCase xunitTestCase) : global::Xunit.LongLivedMarshalByRefObject, ITest
{
	public string DisplayName { get; } = xunitTestCase.DisplayName;
	public ITestCase TestCase { get; } = xunitTestCase;
}
