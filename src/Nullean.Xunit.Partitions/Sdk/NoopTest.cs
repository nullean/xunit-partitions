using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions.Sdk;

[Serializable]
internal class NoopTest(ITestCase xunitTestCase) : ITest
{
	public string DisplayName { get; } = xunitTestCase.DisplayName;
	public ITestCase TestCase { get; } = xunitTestCase;
}
