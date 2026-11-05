using System.Diagnostics;
using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LoggerTests
{
    [Test]
    public void Root_CreatesValidRootLoggerWithInfoMinimumLevel()
    {
        Logger logger = Logger.Root();

        Assert.That(logger.Name, Is.EqualTo(""));
        Assert.That(logger.IsNoOp, Is.False);
        Assert.That(logger.IsValid, Is.True);
        Assert.That(logger.GetMinimumLevel(), Is.EqualTo(LogLevel.Info));
        Assert.That(logger.IsEnabled(LogLevel.Debug), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Info), Is.True);
    }

    [Test]
    public void RootWithSwitch_UsesRuntimeLevelSwitch()
    {
        LogLevelSwitch levelSwitch = LogLevelSwitch.Create(LogLevel.Error);
        Logger logger = Logger.RootWithSwitch(levelSwitch);

        Assert.That(logger.IsEnabled(LogLevel.Warn), Is.False);

        levelSwitch.SetLevel(LogLevel.Debug);

        Assert.That(logger.IsEnabled(LogLevel.Warn), Is.True);
    }

    [Test]
    public void NoOp_DisablesEveryLevel()
    {
        Logger logger = Logger.NoOp();

        Assert.That(logger.IsNoOp, Is.True);
        Assert.That(logger.GetMinimumLevel(), Is.EqualTo(LogLevel.Off));
        Assert.That(logger.IsEnabled(LogLevel.Trace), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Fatal), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Off), Is.False);
    }

    [Test]
    public void Constructor_NormalizesNullDependencies()
    {
        Logger logger = new Logger(null, null, false, null);

        Assert.That(logger.Name, Is.EqualTo(""));
        Assert.That(logger.LevelSwitch.GetLevel(), Is.EqualTo(LogLevel.Info));
        Assert.That(logger.Clock, Is.Not.Null);
        Assert.That(logger.IsEnabled(LogLevel.Info), Is.True);
    }

    [Test]
    public void Constructor_MarksInvalidLoggerName()
    {
        Logger logger = new Logger("bad logger", LogLevelSwitch.Create(LogLevel.Trace), false, LogClock.SystemClock());

        Assert.That(logger.IsValid, Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Fatal), Is.False);
        Assert.That(logger.ErrorMessage, Is.EqualTo("logger name can contain only letters, digits, underscores, and dots"));
    }

    [Test]
    public void CreateEventBuilder_SeedsLoggerNameLevelAndClock()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 19, 0, 0, TimeSpan.Zero);
        Logger logger = new Logger("Application.Worker", LogLevelSwitch.Create(LogLevel.Trace), false, LogClock.Fixed(timestamp));

        LogEvent logEvent = logger.CreateEventBuilder(LogLevel.Warn).SetMessageTemplate("message").Build();

        Assert.That(logEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(logEvent.Level, Is.EqualTo(LogLevel.Warn));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("message"));
    }

    [Test]
    public void For_CreatesNamedLoggerSharingSwitchAndClock()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 19, 30, 0, TimeSpan.Zero);
        LogLevelSwitch levelSwitch = LogLevelSwitch.Create(LogLevel.Warn);
        Logger root = new Logger("", levelSwitch, false, LogClock.Fixed(timestamp));

        Logger logger = root.For("Application.Worker");

        Assert.That(logger.Name, Is.EqualTo("Application.Worker"));
        Assert.That(logger.IsNoOp, Is.False);
        Assert.That(logger.GetMinimumLevel(), Is.EqualTo(LogLevel.Warn));

        levelSwitch.SetLevel(LogLevel.Debug);

        Assert.That(logger.IsEnabled(LogLevel.Info), Is.True);
        Assert.That(logger.CreateEventBuilder(LogLevel.Info).Build().Timestamp, Is.EqualTo(timestamp));
    }

    [Test]
    public void For_CreatesHierarchicalChildLoggerNames()
    {
        Logger application = Logger.Root().For("Application");
        Logger worker = application.For("Worker");

        Assert.That(application.Name, Is.EqualTo("Application"));
        Assert.That(worker.Name, Is.EqualTo("Application.Worker"));
    }

    [Test]
    public void For_KeepsNoOpBehaviorForChildLoggers()
    {
        Logger logger = Logger.NoOp().For("Application.Worker");

        Assert.That(logger.Name, Is.EqualTo("Application.Worker"));
        Assert.That(logger.IsNoOp, Is.True);
        Assert.That(logger.IsEnabled(LogLevel.Fatal), Is.False);
    }

    [Test]
    public void SetCategoryLevel_UsesMostSpecificHierarchyMatch()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Warn);

        root.SetCategoryLevel("Application", LogLevel.Debug);
        root.SetCategoryLevel("Application.Worker", LogLevel.Error);

        Logger application = root.For("Application.Service");
        Logger worker = root.For("Application.Worker.Job");
        Logger other = root.For("Other");

        Assert.That(application.GetMinimumLevel(), Is.EqualTo(LogLevel.Debug));
        Assert.That(worker.GetMinimumLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(other.GetMinimumLevel(), Is.EqualTo(LogLevel.Warn));
    }

    [Test]
    public void SetCategoryLevel_UpdatesExistingCategorySwitch()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Warn);

        root.SetCategoryLevel("Application", LogLevel.Error);
        Assert.That(root.For("Application.Worker").IsEnabled(LogLevel.Warn), Is.False);

        root.SetCategoryLevel("Application", LogLevel.Debug);
        Assert.That(root.For("Application.Worker").IsEnabled(LogLevel.Warn), Is.True);
    }

    [Test]
    public void SetCategoryLevel_RejectsInvalidCategoryName()
    {
        Logger root = Logger.Root();

        LogResult result = root.SetCategoryLevel("bad logger", LogLevel.Debug);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(root.CategoryLevelSwitches.Count, Is.EqualTo(0));
    }

    [Test]
    public void IsEnabled_RequiresLoggerSwitchAndSinkThreshold()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Debug);

        logger.SetSinkMinimumLevel(LogLevel.Warn);

        Assert.That(logger.IsEnabled(LogLevel.Info), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Warn), Is.True);

        logger.LevelSwitch.SetLevel(LogLevel.Error);

        Assert.That(logger.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Error), Is.True);
    }

    [Test]
    public void IsEnabled_UsesCategorySwitchBeforeSinkThreshold()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);

        root.SetCategoryLevel("Application.Worker", LogLevel.Error);
        root.SetSinkMinimumLevel(LogLevel.Warn);

        Logger worker = root.For("Application.Worker.Job");

        Assert.That(worker.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(worker.IsEnabled(LogLevel.Error), Is.True);
    }

    [Test]
    public void For_SharesSinkThresholdsWithChildLoggers()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger child = root.For("Application.Worker");

        root.SetSinkMinimumLevel(LogLevel.Error);

        Assert.That(child.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(child.IsEnabled(LogLevel.Error), Is.True);

        child.ClearSinkMinimumLevel();

        Assert.That(root.IsEnabled(LogLevel.Debug), Is.True);
    }

    [Test]
    public void Write_BuildsRenderedEventWithBoundFields()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 20, 0, 0, TimeSpan.Zero);
        Logger logger = new Logger("Application.Worker", LogLevelSwitch.Create(LogLevel.Trace), false, LogClock.Fixed(timestamp));

        LogResult result = logger.WriteWithValues(LogLevel.Info, "Processed {Count} items for {User}", new object[] { 3, "Ada" });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.WrittenCount, Is.EqualTo(1));
        Assert.That(logger.LastEvent, Is.Not.Null);

        LogEvent logEvent = logger.LastEvent!;
        Assert.That(logEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(logEvent.Level, Is.EqualTo(LogLevel.Info));
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(logEvent.MessageTemplate, Is.EqualTo("Processed {Count} items for {User}"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Processed 3 items for Ada"));
        Assert.That(logEvent.Fields.Length, Is.EqualTo(2));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("Count"));
        Assert.That(logEvent.Fields[0].Value.IntegerValue, Is.EqualTo(3));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("User"));
        Assert.That(logEvent.Fields[1].Value.TextValue, Is.EqualTo("Ada"));
    }

    [Test]
    public void Write_ReturnsDroppedAndDoesNotReplaceEventWhenDisabled()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.Info("accepted");
        LogEvent firstEvent = logger.LastEvent!;

        logger.SetSinkMinimumLevel(LogLevel.Error);
        LogResult result = logger.Warn("blocked");

        Assert.That(result.IsDropped, Is.True);
        Assert.That(logger.WrittenCount, Is.EqualTo(1));
        Assert.That(logger.LastEvent, Is.SameAs(firstEvent));
    }

    [Test]
    public void Write_ReturnsFailureForInvalidLogger()
    {
        Logger logger = new Logger("bad logger", LogLevelSwitch.Create(LogLevel.Trace), false, LogClock.SystemClock());

        LogResult result = logger.Write(LogLevel.Fatal, "will not build");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("logger.invalid"));
        Assert.That(logger.LastEvent, Is.Null);
        Assert.That(logger.WrittenCount, Is.EqualTo(0));
    }

    [Test]
    public void WriteEvent_StoresPrebuiltEventWhenEnabled()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 20, 30, 0, TimeSpan.Zero);
        Logger logger = Logger.RootWithLevel(LogLevel.Debug);
        LogEvent logEvent = LogEvent.CreateAt(timestamp, LogLevel.Error, "Application", 0, "", "failed", "failed", new LogField[0], null);

        LogResult result = logger.WriteEvent(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.SameAs(logEvent));
        Assert.That(logger.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void ConvenienceMethods_WriteExpectedLevels()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.Trace("trace");
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Trace));

        logger.Debug("debug");
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Debug));

        logger.Info("info");
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Info));

        logger.Warn("warn");
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Warn));

        logger.Error("error");
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Error));

        logger.Fatal("fatal");
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Fatal));
        Assert.That(logger.WrittenCount, Is.EqualTo(6));
    }

    [Test]
    public void Write_WithExceptionStoresExceptionOnEvent()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        InvalidOperationException exceptionValue = new InvalidOperationException("worker failed");

        LogResult result = logger.WriteWithExceptionAndValues(LogLevel.Error, exceptionValue, "Failed {Operation}", new object[] { "sync" });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);

        LogEvent logEvent = logger.LastEvent!;
        Assert.That(logEvent.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(logEvent.ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(logEvent.MessageTemplate, Is.EqualTo("Failed {Operation}"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Failed sync"));
        Assert.That(logEvent.Fields.Length, Is.EqualTo(1));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("Operation"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo("sync"));
    }

    [Test]
    public void Error_WithExceptionUsesErrorLevel()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        ApplicationException exceptionValue = new ApplicationException("write failed");

        LogResult result = logger.ErrorWithException(exceptionValue, "Unable to write");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(logger.LastEvent!.ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(logger.LastEvent!.RenderedMessage, Is.EqualTo("Unable to write"));
    }

    [Test]
    public void Fatal_WithExceptionAndValuesUsesFatalLevel()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        Exception exceptionValue = new Exception("shutdown");

        LogResult result = logger.FatalWithExceptionAndValues(exceptionValue, "Fatal {Code}", new object[] { 503 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Fatal));
        Assert.That(logger.LastEvent!.ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(logger.LastEvent!.RenderedMessage, Is.EqualTo("Fatal 503"));
    }

    [Test]
    public void Write_WithExceptionDoesNotBuildWhenDisabled()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        Exception exceptionValue = new Exception("ignored");

        logger.SetSinkMinimumLevel(LogLevel.Fatal);
        LogResult result = logger.WriteWithExceptionAndValues(LogLevel.Error, exceptionValue, "Will not write {Value}", new object[] { 1 });

        Assert.That(result.IsDropped, Is.True);
        Assert.That(logger.LastEvent, Is.Null);
        Assert.That(logger.WrittenCount, Is.EqualTo(0));
    }

    [Test]
    public void ForField_CreatesChildLoggerWithBoundField()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace).For("Application.Worker").ForField("TenantId", "tenant-1");

        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);

        LogEvent logEvent = logger.LastEvent!;
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(logEvent.Fields.Length, Is.EqualTo(2));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo("tenant-1"));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("Count"));
        Assert.That(logEvent.Fields[1].Value.IntegerValue, Is.EqualTo(3));
    }

    [Test]
    public void ForFields_InheritsParentBoundFieldsAndKeepsParentUnchanged()
    {
        Logger parent = Logger.RootWithLevel(LogLevel.Trace).ForField("Application", "api");
        Logger child = parent.ForFields(new LogField[] { LogField.Int("WorkerId", 7) });

        parent.Info("parent");
        child.Info("child");

        Assert.That(parent.BoundFields.Length, Is.EqualTo(1));
        Assert.That(child.BoundFields.Length, Is.EqualTo(2));
        Assert.That(parent.LastEvent!.Fields.Length, Is.EqualTo(1));
        Assert.That(parent.LastEvent!.Fields[0].Name, Is.EqualTo("Application"));
        Assert.That(child.LastEvent!.Fields.Length, Is.EqualTo(2));
        Assert.That(child.LastEvent!.Fields[0].Name, Is.EqualTo("Application"));
        Assert.That(child.LastEvent!.Fields[1].Name, Is.EqualTo("WorkerId"));
        Assert.That(child.LastEvent!.Fields[1].Value.IntegerValue, Is.EqualTo(7));
    }

    [Test]
    public void For_WithFieldsCreatesNamedChildWithBoundFields()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger child = root.ForWithFields("Application.Worker", new LogField[] { LogField.String("Component", "queue") });

        child.WarnWithValues("Lag {Count}", new object[] { 12 });

        Assert.That(child.Name, Is.EqualTo("Application.Worker"));
        Assert.That(child.LastEvent, Is.Not.Null);
        Assert.That(child.LastEvent!.Fields.Length, Is.EqualTo(2));
        Assert.That(child.LastEvent!.Fields[0].Name, Is.EqualTo("Component"));
        Assert.That(child.LastEvent!.Fields[0].Value.TextValue, Is.EqualTo("queue"));
        Assert.That(child.LastEvent!.Fields[1].Name, Is.EqualTo("Count"));
        Assert.That(child.LastEvent!.Fields[1].Value.IntegerValue, Is.EqualTo(12));
    }

    [Test]
    public void BoundFields_PreserveInvalidFieldCounts()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace).ForLogField(new LogField("bad name", LogValue.String("value")));

        logger.Info("message");

        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Fields.Length, Is.EqualTo(0));
        Assert.That(logger.LastEvent!.InvalidFieldCount, Is.EqualTo(1));
    }

    [Test]
    public void For_SharesScopeStorageWithChildLoggers()
    {
        Logger root = Logger.Root();
        Logger child = root.For("Application.Worker");

        root.ScopeStorage.Push(new LogField[] { LogField.String("RequestId", "r1") });

        Assert.That(child.ScopeStorage, Is.SameAs(root.ScopeStorage));
        Assert.That(child.ScopeStorage.GetCurrentFields().Length, Is.EqualTo(1));
        Assert.That(child.ScopeStorage.GetCurrentFields()[0].Name, Is.EqualTo("RequestId"));
    }

    [Test]
    public void BeginScope_UsesSharedLoggerScopeStorage()
    {
        Logger root = Logger.Root();
        Logger child = root.For("Application.Worker");

        LogScope scope = child.BeginScopeField("RequestId", "r1");

        Assert.That(root.ScopeStorage.Current(), Is.SameAs(scope.Frame));
        Assert.That(root.ScopeStorage.GetCurrentFields().Length, Is.EqualTo(1));
        Assert.That(root.ScopeStorage.GetCurrentFields()[0].Name, Is.EqualTo("RequestId"));

        scope.Dispose();
        Assert.That(root.ScopeStorage.Current(), Is.Null);
    }

    [Test]
    public void Write_MergesBoundFieldsScopeFieldsAndTemplateFields()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace).ForField("TenantId", "tenant-1");

        LogScope requestScope = logger.BeginScopeField("RequestId", "r1");
        LogScope operationScope = logger.BeginScopeLogField(LogField.String("Operation", "sync"));

        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);

        LogEvent logEvent = logger.LastEvent!;
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(logEvent.Fields.Length, Is.EqualTo(4));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("RequestId"));
        Assert.That(logEvent.Fields[2].Name, Is.EqualTo("Operation"));
        Assert.That(logEvent.Fields[3].Name, Is.EqualTo("Count"));

        operationScope.Dispose();
        requestScope.Dispose();
    }

    [Test]
    public void Write_DoesNotMergeDisposedScopeFields()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        LogScope requestScope = logger.BeginScopeField("RequestId", "r1");
        LogScope operationScope = logger.BeginScopeField("Operation", "sync");
        operationScope.Dispose();

        logger.Info("message");

        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Fields.Length, Is.EqualTo(1));
        Assert.That(logger.LastEvent!.Fields[0].Name, Is.EqualTo("RequestId"));

        requestScope.Dispose();
        logger.Info("after");

        Assert.That(logger.LastEvent!.Fields.Length, Is.EqualTo(0));
    }

    [Test]
    public void CreateEventBuilder_MergesCurrentScopeFields()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        LogScope scope = logger.BeginScopeField("RequestId", "r1");
        LogEvent logEvent = logger.CreateEventBuilder(LogLevel.Warn).SetMessageTemplate("message").Build();

        Assert.That(logEvent.Fields.Length, Is.EqualTo(1));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo("r1"));

        scope.Dispose();
    }

    [Test]
    public async Task Write_MergesScopeFieldsAcrossAwait()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        LogScope scope = logger.BeginScopeField("RequestId", "r1");
        await Task.Yield();

        logger.Info("async");

        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Fields.Length, Is.EqualTo(1));
        Assert.That(logger.LastEvent!.Fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(logger.LastEvent!.Fields[0].Value.TextValue, Is.EqualTo("r1"));

        scope.Dispose();
    }

    [Test]
    public void Write_EnrichesEventWithCurrentActivity()
    {
        Activity.Current = null;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        Activity activity = new Activity("operation");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        logger.Info("message");

        activity.Stop();

        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.TraceId, Is.EqualTo(activity.TraceId.ToString()));
        Assert.That(logger.LastEvent!.SpanId, Is.EqualTo(activity.SpanId.ToString()));
        Assert.That(logger.LastEvent!.ParentSpanId, Is.EqualTo(""));
    }

    [Test]
    public void Write_EnrichesEventWithParentSpanIdForNestedActivity()
    {
        Activity.Current = null;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        Activity parent = new Activity("parent");
        parent.SetIdFormat(ActivityIdFormat.W3C);
        parent.Start();
        Activity child = new Activity("child");
        child.SetIdFormat(ActivityIdFormat.W3C);
        child.Start();

        logger.Warn("message");

        child.Stop();
        parent.Stop();

        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.TraceId, Is.EqualTo(child.TraceId.ToString()));
        Assert.That(logger.LastEvent!.SpanId, Is.EqualTo(child.SpanId.ToString()));
        Assert.That(logger.LastEvent!.ParentSpanId, Is.EqualTo(parent.SpanId.ToString()));
    }

    [Test]
    public void CreateEventBuilder_LeavesActivityFieldsEmptyWhenNoCurrentActivity()
    {
        Activity.Current = null;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        LogEvent logEvent = logger.CreateEventBuilder(LogLevel.Info).SetMessageTemplate("message").Build();

        Assert.That(logEvent.TraceId, Is.EqualTo(""));
        Assert.That(logEvent.SpanId, Is.EqualTo(""));
        Assert.That(logEvent.ParentSpanId, Is.EqualTo(""));
    }

    [Test]
    public void For_SharesActivityEnricherWithChildLoggers()
    {
        Activity.Current = null;
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger child = root.For("Application.Worker");
        Activity activity = new Activity("operation");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        root.ActivityEnricher.IsEnabled = false;
        child.Info("message");

        activity.Stop();

        Assert.That(child.ActivityEnricher, Is.SameAs(root.ActivityEnricher));
        Assert.That(child.LastEvent, Is.Not.Null);
        Assert.That(child.LastEvent!.TraceId, Is.EqualTo(""));
        Assert.That(child.LastEvent!.SpanId, Is.EqualTo(""));
    }

    [Test]
    public void Write_UsesBuiltInRuntimeOverride()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.BuiltInEnricher.SetRuntime(12, 34, "machine-a", "app-a");
        logger.Info("message");

        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.ThreadId, Is.EqualTo(12));
        Assert.That(logger.LastEvent!.ProcessId, Is.EqualTo(34));
        Assert.That(logger.LastEvent!.MachineName, Is.EqualTo("machine-a"));
        Assert.That(logger.LastEvent!.ApplicationName, Is.EqualTo("app-a"));
    }

    [Test]
    public void Write_AppendsBuiltInStaticFieldsAfterTemplateFields()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.BuiltInEnricher.AddStaticField("Service", "billing");
        logger.BuiltInEnricher.AddStaticLogField(LogField.Int("Shard", 2));
        logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Fields.Length, Is.EqualTo(3));
        Assert.That(logger.LastEvent!.Fields[0].Name, Is.EqualTo("Count"));
        Assert.That(logger.LastEvent!.Fields[0].Value.IntegerValue, Is.EqualTo(3));
        Assert.That(logger.LastEvent!.Fields[1].Name, Is.EqualTo("Service"));
        Assert.That(logger.LastEvent!.Fields[1].Value.TextValue, Is.EqualTo("billing"));
        Assert.That(logger.LastEvent!.Fields[2].Name, Is.EqualTo("Shard"));
        Assert.That(logger.LastEvent!.Fields[2].Value.IntegerValue, Is.EqualTo(2));
    }

    [Test]
    public void For_SharesBuiltInEnricherWithChildLoggers()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger child = root.For("Application.Worker");

        root.BuiltInEnricher.AddStaticField("Service", "billing");
        child.Info("message");

        Assert.That(child.BuiltInEnricher, Is.SameAs(root.BuiltInEnricher));
        Assert.That(child.LastEvent, Is.Not.Null);
        Assert.That(child.LastEvent!.Fields.Length, Is.EqualTo(1));
        Assert.That(child.LastEvent!.Fields[0].Name, Is.EqualTo("Service"));
    }

    [Test]
    public void Write_DoesNotAppendBuiltInStaticFieldsWhenEnricherDisabled()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.BuiltInEnricher.AddStaticField("Service", "billing");
        logger.BuiltInEnricher.IsEnabled = false;
        logger.Info("message");

        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Fields.Length, Is.EqualTo(0));
    }

    [Test]
    public void Write_DropsEventsExcludedByMinimumLevelFilter()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.Info("accepted");
        LogEvent acceptedEvent = logger.LastEvent!;

        logger.AddFilter(LogFilter.MinimumLevel(LogLevel.Error));
        LogResult result = logger.Warn("filtered");

        Assert.That(result.IsDropped, Is.True);
        Assert.That(result.Message, Is.EqualTo("log event filtered"));
        Assert.That(logger.WrittenCount, Is.EqualTo(1));
        Assert.That(logger.LastEvent, Is.SameAs(acceptedEvent));
    }

    [Test]
    public void Write_AcceptsEventsIncludedByMinimumLevelFilter()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.AddFilter(LogFilter.MinimumLevel(LogLevel.Warn));
        LogResult result = logger.Error("accepted");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Level, Is.EqualTo(LogLevel.Error));
    }

    [Test]
    public void For_SharesFilterPipelineWithChildLoggers()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger child = root.For("Application.Worker");

        root.AddFilter(LogFilter.MinimumLevel(LogLevel.Fatal));
        LogResult result = child.Error("filtered");

        Assert.That(child.FilterPipeline, Is.SameAs(root.FilterPipeline));
        Assert.That(result.IsDropped, Is.True);
        Assert.That(child.LastEvent, Is.Null);
    }

    [Test]
    public void Write_DropsEventsExcludedByMatchingCategoryPrefixFilter()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger application = root.For("Application.Worker");
        Logger other = root.For("Other");

        root.AddFilter(LogFilter.CategoryPrefix("Application", LogLevel.Error));
        LogResult applicationResult = application.Warn("filtered");
        LogResult otherResult = other.Warn("accepted");

        Assert.That(applicationResult.IsDropped, Is.True);
        Assert.That(application.LastEvent, Is.Null);
        Assert.That(otherResult.IsSuccess, Is.True);
        Assert.That(other.LastEvent, Is.Not.Null);
        Assert.That(other.LastEvent!.LoggerName, Is.EqualTo("Other"));
    }

    [Test]
    public void Write_AcceptsEventsIncludedByMatchingCategoryPrefixFilter()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger application = root.For("Application.Worker");

        root.AddFilter(LogFilter.CategoryPrefix("Application", LogLevel.Warn));
        LogResult result = application.Error("accepted");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(application.LastEvent, Is.Not.Null);
        Assert.That(application.LastEvent!.Level, Is.EqualTo(LogLevel.Error));
    }

    [Test]
    public void Write_DropsEventsWhenRequiredFieldIsMissing()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult missingResult = logger.Info("message");
        LogResult presentResult = logger.ForField("TenantId", "tenant-1").Info("message");

        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(presentResult.IsSuccess, Is.True);
    }

    [Test]
    public void Write_DropsEventsWhenFieldValueDoesNotMatch()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.AddFilter(LogFilter.FieldEquals("Count", 3));
        LogResult droppedResult = logger.InfoWithValues("Processed {Count}", new object[] { 2 });
        LogResult acceptedResult = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(droppedResult.IsDropped, Is.True);
        Assert.That(acceptedResult.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.RenderedMessage, Is.EqualTo("Processed 3"));
    }

    [Test]
    public void Write_AppliesSampleEveryFilter()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.AddFilter(LogFilter.SampleEvery(2));
        LogResult firstResult = logger.Info("first");
        LogResult secondResult = logger.Info("second");
        LogResult thirdResult = logger.Info("third");

        Assert.That(firstResult.IsDropped, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(thirdResult.IsDropped, Is.True);
        Assert.That(logger.WrittenCount, Is.EqualTo(1));
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.RenderedMessage, Is.EqualTo("second"));
    }

    [Test]
    public void Write_AppliesRateLimitFilterAndResetsAfterWindow()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 8, 2, 21, 30, 0, TimeSpan.Zero);
        LogClock clock = LogClock.Fixed(start);
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);

        logger.AddFilter(LogFilter.RateLimitWithClock(1, TimeSpan.FromSeconds(10), clock));
        LogResult firstResult = logger.Info("first");
        LogResult secondResult = logger.Info("second");
        clock.SetFixedUtcNow(start.AddSeconds(11));
        LogResult thirdResult = logger.Info("third");

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsDropped, Is.True);
        Assert.That(thirdResult.IsSuccess, Is.True);
        Assert.That(logger.WrittenCount, Is.EqualTo(2));
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.RenderedMessage, Is.EqualTo("third"));
    }

    [Test]
    public void Write_FansOutToConfiguredSinks()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink first = LogSink.Named("first");
        LogSink second = LogSink.Named("second");

        logger.AddSink(first);
        logger.AddSink(second);
        LogResult result = logger.Info("message");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(first.LastEvent, Is.SameAs(logger.LastEvent));
        Assert.That(second.LastEvent, Is.SameAs(logger.LastEvent));
        Assert.That(first.WrittenCount, Is.EqualTo(1));
        Assert.That(second.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_ReturnsPipelineFailureWithoutAcceptingLoggerEvent()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = LogSink.Named("failure");

        sink.SetResult(LogResult.FailureCode("sink.failure", "failed"));
        logger.AddSink(sink);
        LogResult result = logger.Info("message");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(logger.LastEvent, Is.Null);
        Assert.That(logger.WrittenCount, Is.EqualTo(0));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void For_SharesPipelineWithChildLoggers()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger child = root.For("Application.Worker");
        LogSink sink = LogSink.Named("shared");

        root.AddSink(sink);
        LogResult result = child.Info("message");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(child.Pipeline, Is.SameAs(root.Pipeline));
        Assert.That(sink.LastEvent, Is.SameAs(child.LastEvent));
        Assert.That(sink.LastEvent!.LoggerName, Is.EqualTo("Application.Worker"));
    }

    [Test]
    public void IsEnabled_UsesConfiguredSinkThresholds()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = LogSink.Named("error");

        sink.SetMinimumLevel(LogLevel.Error);
        logger.AddSink(sink);

        Assert.That(logger.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Error), Is.True);
    }

    [Test]
    public void Write_SucceedsWhenAtLeastOneSinkAcceptsAfterSinkSpecificFilter()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink filtered = LogSink.Named("filtered");
        LogSink unfiltered = LogSink.Named("unfiltered");

        filtered.AddFilter(LogFilter.FieldExists("TenantId"));
        logger.SetSinks(new LogSink[] { filtered, unfiltered });
        LogResult result = logger.Info("message");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DroppedCount, Is.EqualTo(1));
        Assert.That(filtered.WrittenCount, Is.EqualTo(0));
        Assert.That(unfiltered.WrittenCount, Is.EqualTo(1));
        Assert.That(logger.LastEvent, Is.SameAs(unfiltered.LastEvent));
    }

    [Test]
    public void Write_DropsWhenEverySinkFilterExcludesEvent()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = LogSink.Named("filtered");

        sink.AddFilter(LogFilter.FieldExists("TenantId"));
        logger.AddSink(sink);
        LogResult result = logger.Info("message");

        Assert.That(result.IsDropped, Is.True);
        Assert.That(logger.LastEvent, Is.Null);
        Assert.That(sink.WrittenCount, Is.EqualTo(0));
    }

    [Test]
    public void Write_FansOutToConfiguredSubPipeline()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogPipeline branch = new LogPipeline();
        LogSink nested = LogSink.Named("nested");

        branch.AddSink(nested);
        logger.AddPipeline("branch", branch);
        LogResult result = logger.Info("message");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(branch.LastEvent, Is.SameAs(logger.LastEvent));
        Assert.That(nested.LastEvent, Is.SameAs(logger.LastEvent));
        Assert.That(nested.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void IsEnabled_UsesNestedPipelineSinkThresholds()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogPipeline branch = new LogPipeline();
        LogSink nested = LogSink.Named("nested");

        nested.SetMinimumLevel(LogLevel.Error);
        branch.AddSink(nested);
        logger.AddPipeline("branch", branch);

        Assert.That(logger.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Error), Is.True);
    }
}
