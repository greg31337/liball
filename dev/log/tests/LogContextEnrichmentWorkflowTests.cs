using System.Diagnostics;
using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogContextEnrichmentWorkflowTests
{
    [Test]
    public void Write_OrdersBoundScopeTemplateAndEnricherFields()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        root.AddSink(sink);
        root.BuiltInEnricher.SetRuntime(101, 202, "machine-a", "app-a");
        root.BuiltInEnricher.AddStaticField("Service", "billing");
        Logger logger = root.For("Application.Worker").ForField("TenantId", "tenant-1");
        LogScope request = logger.BeginScopeField("RequestId", "req-1");
        LogScope operation = logger.BeginScopeField("Operation", "sync");

        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });
        LogEvent logEvent = MemoryLogSink.Events(sink)[0];

        operation.Dispose();
        request.Dispose();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logEvent.ThreadId, Is.EqualTo(101));
        Assert.That(logEvent.ProcessId, Is.EqualTo(202));
        Assert.That(logEvent.MachineName, Is.EqualTo("machine-a"));
        Assert.That(logEvent.ApplicationName, Is.EqualTo("app-a"));
        Assert.That(logEvent.Fields.Length, Is.EqualTo(5));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("RequestId"));
        Assert.That(logEvent.Fields[2].Name, Is.EqualTo("Operation"));
        Assert.That(logEvent.Fields[3].Name, Is.EqualTo("Count"));
        Assert.That(logEvent.Fields[4].Name, Is.EqualTo("Service"));
    }

    [Test]
    public void ChildLoggers_KeepBoundFieldsIndependentWhileSharingEnrichers()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace).ForField("Root", "root");
        Logger first = root.ForField("Child", "first");
        Logger second = root.ForField("Child", "second");
        root.BuiltInEnricher.AddStaticField("Service", "api");

        root.Info("root");
        first.Info("first");
        second.Info("second");

        Assert.That(root.LastEvent!.Fields.Length, Is.EqualTo(2));
        Assert.That(root.LastEvent!.Fields[0].Name, Is.EqualTo("Root"));
        Assert.That(root.LastEvent!.Fields[1].Name, Is.EqualTo("Service"));
        Assert.That(first.LastEvent!.Fields.Length, Is.EqualTo(3));
        Assert.That(first.LastEvent!.Fields[0].Name, Is.EqualTo("Root"));
        Assert.That(first.LastEvent!.Fields[1].Name, Is.EqualTo("Child"));
        Assert.That(first.LastEvent!.Fields[1].Value.TextValue, Is.EqualTo("first"));
        Assert.That(first.LastEvent!.Fields[2].Name, Is.EqualTo("Service"));
        Assert.That(second.LastEvent!.Fields[1].Value.TextValue, Is.EqualTo("second"));
        Assert.That(first.BuiltInEnricher, Is.SameAs(root.BuiltInEnricher));
        Assert.That(second.BuiltInEnricher, Is.SameAs(root.BuiltInEnricher));
    }

    [Test]
    public void ConfigurationEnrichers_EnableRuntimeActivityAndStaticFields()
    {
        Activity.Current = null;
        LogSink sink = MemoryLogSink.Create();
        Activity activity = new Activity("operation");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        Logger logger = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .RuntimeMetadata(false)
            .Activity(false)
            .AddEnricher(LogEnricher.Runtime())
            .AddEnricher(LogEnricher.Activity())
            .AddEnricher(LogEnricher.Field("Service", "api"))
            .AddSink(sink)
            .CreateLogger();

        LogResult result = logger.Info("message");
        LogEvent logEvent = MemoryLogSink.Events(sink)[0];

        activity.Stop();
        Activity.Current = null;

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logEvent.ThreadId, Is.GreaterThan(0));
        Assert.That(logEvent.ProcessId, Is.GreaterThan(0));
        Assert.That(logEvent.MachineName.Length, Is.GreaterThan(0));
        Assert.That(logEvent.TraceId, Is.EqualTo(activity.TraceId.ToString()));
        Assert.That(logEvent.SpanId, Is.EqualTo(activity.SpanId.ToString()));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("Service"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo("api"));
    }

    [Test]
    public async Task Scopes_FlowToAsyncBranchWithoutLeakingChildScopeBackToParent()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddSink(sink);
        LogScope parent = logger.BeginScopeField("RequestId", "parent");

        LogEvent childEvent = await Task.Run(
            async () =>
            {
                LogScope child = logger.BeginScopeField("Operation", "child");
                await Task.Yield();
                logger.Info("child");
                LogEvent captured = logger.LastEvent!;
                child.Dispose();
                return captured;
            });

        logger.Info("parent");
        LogEvent parentEvent = logger.LastEvent!;
        parent.Dispose();

        Assert.That(childEvent.Fields.Length, Is.EqualTo(2));
        Assert.That(childEvent.Fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(childEvent.Fields[0].Value.TextValue, Is.EqualTo("parent"));
        Assert.That(childEvent.Fields[1].Name, Is.EqualTo("Operation"));
        Assert.That(parentEvent.Fields.Length, Is.EqualTo(1));
        Assert.That(parentEvent.Fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(parentEvent.Fields[0].Value.TextValue, Is.EqualTo("parent"));
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(2));
    }

    [Test]
    public void DisabledActivityEnricher_SuppressesCorrelationAcrossChildren()
    {
        Activity.Current = null;
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger child = root.For("Application.Worker");
        root.ActivityEnricher.IsEnabled = false;
        Activity activity = new Activity("operation");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        child.Info("message");

        activity.Stop();
        Activity.Current = null;

        Assert.That(child.ActivityEnricher, Is.SameAs(root.ActivityEnricher));
        Assert.That(child.LastEvent!.TraceId, Is.EqualTo(""));
        Assert.That(child.LastEvent!.SpanId, Is.EqualTo(""));
    }
}
