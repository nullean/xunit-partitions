using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Xunit;
using Xunit.Sdk;

namespace Nullean.Xunit.Partitions.Sdk;

// marker interface
// ReSharper disable once UnusedTypeParameter
public interface IPartitionFixture<out TLifetime> where TLifetime : IPartitionLifetime;

/// <summary>
/// The object that provides long running state to tests.
/// <para>Avoid work in constructors, state has to be initialized early to satisfy XUnit constructor injection</para>
/// <para>Utilize <see cref="IAsyncLifetime.InitializeAsync"/> to bootstrap over constructors</para>
/// <para>Utilize <see cref="IAsyncLifetime.DisposeAsync"/> to wind down state</para>
/// </summary>
public interface IPartitionLifetime : IAsyncLifetime
{
	public int? MaxConcurrency { get; }
}

public static class PartitionContext
{
	private class Context
	{
		public Exception? Exception { get; internal set; }
		public bool RanTest { get; internal set; }
	}

	private static readonly AsyncLocal<Context?> Local = new();

	internal static void StopExceptionCapture()
	{
		if (Local.Value != null) Local.Value.RanTest = true;
	}

	internal static void StartExceptionCapture()
	{
		Local.Value ??= new Context();

		AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
		{
			if (Local.Value == null)
				return;
			if (Local.Value.RanTest)
				return;

			Local.Value.Exception = e.Exception;
		};
	}

	// https://github.com/SimonCropp/XunitContext/blob/main/src/XunitContext/Context.cs#L39C3-L79C6
	// Lifted from XunitContext, awesome library do check it out!

	/// <summary>
	/// The <see cref="Exception" /> for the current test if it failed.
	/// </summary>
	public static Exception? TestException
	{
		get
		{
			var e = Local.Value?.Exception;
			switch (e)
			{
				case null: return null;
				case XunitException: return e;
			}

			var outerTrace = new StackTrace(e, false);
			var firstFrame = outerTrace.GetFrame(outerTrace.FrameCount - 1)!;
			var firstMethod = firstFrame.GetMethod()!;

			// firstMethod.DeclaringType can be null if the member was generated with reflection.
			var root = firstMethod.DeclaringType?.DeclaringType;
			if (root == null || root != typeof(ExceptionAggregator)) return null;

			if (e is TargetInvocationException targetInvocationException)
				return targetInvocationException.InnerException;

			return e;

		}
	}
}
