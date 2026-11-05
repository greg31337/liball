using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogScopeStorageTests
{
    [Test]
    public void Current_DefaultsToNull()
    {
        LogScopeStorage storage = new LogScopeStorage();

        Assert.That(storage.Current(), Is.Null);
        Assert.That(storage.GetCurrentFields().Length, Is.EqualTo(0));
    }

    [Test]
    public void Push_StoresCurrentFrameWithParentAndDepth()
    {
        LogScopeStorage storage = new LogScopeStorage();

        LogScopeFrame parent = storage.Push(new LogField[] { LogField.String("RequestId", "r1") });
        LogScopeFrame child = storage.Push(new LogField[] { LogField.Int("Attempt", 2) });

        Assert.That(storage.Current(), Is.SameAs(child));
        Assert.That(child.Parent, Is.SameAs(parent));
        Assert.That(parent.Depth, Is.EqualTo(1));
        Assert.That(child.Depth, Is.EqualTo(2));
    }

    [Test]
    public void GetCurrentFields_FlattensScopesFromRootToCurrent()
    {
        LogScopeStorage storage = new LogScopeStorage();

        storage.Push(new LogField[] { LogField.String("RequestId", "r1") });
        storage.Push(new LogField[] { LogField.String("Operation", "sync"), LogField.Int("Attempt", 2) });

        LogField[] fields = storage.GetCurrentFields();

        Assert.That(fields.Length, Is.EqualTo(3));
        Assert.That(fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(fields[1].Name, Is.EqualTo("Operation"));
        Assert.That(fields[2].Name, Is.EqualTo("Attempt"));
    }

    [Test]
    public async Task Current_FlowsAcrossAwait()
    {
        LogScopeStorage storage = new LogScopeStorage();

        storage.Push(new LogField[] { LogField.String("RequestId", "r1") });
        await Task.Yield();

        LogField[] fields = storage.GetCurrentFields();

        Assert.That(fields.Length, Is.EqualTo(1));
        Assert.That(fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("r1"));
    }

    [Test]
    public async Task Current_IsolatedBetweenAsyncBranches()
    {
        LogScopeStorage storage = new LogScopeStorage();

        storage.Push(new LogField[] { LogField.String("RequestId", "parent") });
        Task<LogField[]> childTask = Task.Run(
            async () =>
            {
                storage.Push(new LogField[] { LogField.String("Operation", "child") });
                await Task.Yield();
                return storage.GetCurrentFields();
            });

        LogField[] childFields = await childTask;
        LogField[] parentFields = storage.GetCurrentFields();

        Assert.That(childFields.Length, Is.EqualTo(2));
        Assert.That(childFields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(childFields[1].Name, Is.EqualTo("Operation"));
        Assert.That(parentFields.Length, Is.EqualTo(1));
        Assert.That(parentFields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(parentFields[0].Value.TextValue, Is.EqualTo("parent"));
    }

    [Test]
    public void Clear_RemovesCurrentFrame()
    {
        LogScopeStorage storage = new LogScopeStorage();

        storage.Push(new LogField[] { LogField.String("RequestId", "r1") });
        storage.Clear();

        Assert.That(storage.Current(), Is.Null);
        Assert.That(storage.GetCurrentFields().Length, Is.EqualTo(0));
    }

    [Test]
    public void Dispose_RestoresParentScope()
    {
        LogScopeStorage storage = new LogScopeStorage();

        LogScope parent = storage.Begin(new LogField[] { LogField.String("RequestId", "r1") });
        LogScope child = storage.Begin(new LogField[] { LogField.String("Operation", "sync") });

        child.Dispose();

        LogField[] fields = storage.GetCurrentFields();
        Assert.That(storage.Current(), Is.SameAs(parent.Frame));
        Assert.That(fields.Length, Is.EqualTo(1));
        Assert.That(fields[0].Name, Is.EqualTo("RequestId"));

        parent.Dispose();
        Assert.That(storage.Current(), Is.Null);
    }

    [Test]
    public void Dispose_IsIdempotent()
    {
        LogScopeStorage storage = new LogScopeStorage();
        LogScope scope = storage.Begin(new LogField[] { LogField.String("RequestId", "r1") });

        scope.Dispose();
        scope.Dispose();

        Assert.That(scope.IsDisposed, Is.True);
        Assert.That(scope.Frame!.IsDisposed, Is.True);
        Assert.That(storage.Current(), Is.Null);
    }

    [Test]
    public void Dispose_OutOfOrderStopsDisposedParentFieldsFromLeaking()
    {
        LogScopeStorage storage = new LogScopeStorage();

        LogScope parent = storage.Begin(new LogField[] { LogField.String("RequestId", "r1") });
        LogScope child = storage.Begin(new LogField[] { LogField.String("Operation", "sync") });

        parent.Dispose();
        LogField[] childFields = storage.GetCurrentFields();

        Assert.That(storage.Current(), Is.SameAs(child.Frame));
        Assert.That(childFields.Length, Is.EqualTo(1));
        Assert.That(childFields[0].Name, Is.EqualTo("Operation"));

        child.Dispose();
        Assert.That(storage.Current(), Is.Null);
    }

    [Test]
    public void BeginField_CreatesSingleFieldScope()
    {
        LogScopeStorage storage = new LogScopeStorage();

        LogScope scope = storage.BeginField("RequestId", "r1");

        LogField[] fields = storage.GetCurrentFields();
        Assert.That(scope.IsDisposed, Is.False);
        Assert.That(fields.Length, Is.EqualTo(1));
        Assert.That(fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("r1"));
    }
}
