using email;

namespace email.Tests;

public class EmailPathTests
{
    [Test]
    public void AppendBuildsImmutableNestedPath()
    {
        EmailPath root = new EmailPathOperations().CreateWith();
        EmailPath child = root.Append(0);
        EmailPath nested = child.Append(2);

        Assert.That(root.IsRoot(), Is.True);
        Assert.That(root.GetDepth(), Is.Zero);
        Assert.That(root.GetDisplayText(), Is.EqualTo("root"));
        Assert.That(child.GetDisplayText(), Is.EqualTo("root/1"));
        Assert.That(nested.GetDepth(), Is.EqualTo(2));
        Assert.That(nested.GetAt(0), Is.Zero);
        Assert.That(nested.GetAt(1), Is.EqualTo(2));
        Assert.That(nested.GetDisplayText(), Is.EqualTo("root/1/3"));
    }

    [Test]
    public void ConstructionAndReturnedSegmentsUseSnapshots()
    {
        List<int> source = new List<int> { 1, 4 };
        EmailPath path = new EmailPathOperations().CreateWithSegments(source);
        source[0] = 9;
        List<int> returned = path.GetSegments();
        returned[1] = 8;

        Assert.That(path.GetAt(0), Is.EqualTo(1));
        Assert.That(path.GetAt(1), Is.EqualTo(4));
        Assert.That(path.EqualsPath(new EmailPathOperations().CreateWithSegments(new List<int> { 1, 4 })), Is.True);
        Assert.That(path.EqualsPath(new EmailPathOperations().CreateWithSegments(new List<int> { 1, 5 })), Is.False);
        Assert.That(path.EqualsPath(null!), Is.False);
    }

    [Test]
    public void PathRejectsInvalidSegmentsAndIndexes()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailPathOperations().CreateWithSegments(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailPathOperations().CreateWithSegments(new List<int> { 0, -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailPathOperations().CreateRoot().Append(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailPathOperations().CreateRoot().GetAt(0));
        Assert.Throws<InvalidOperationException>(() => new EmailPathOperations().CreateRoot().GetLastChildIndex());
    }

    [Test]
    public void ParentAndPrefixOperationsDescribeNestedEntities()
    {
        EmailPath root = new EmailPathOperations().CreateRoot();
        EmailPath first = root.Append(1);
        EmailPath nested = first.Append(3);

        Assert.That(root.HasParent(), Is.False);
        Assert.That(root.GetParent(), Is.Null);
        Assert.That(first.HasParent(), Is.True);
        Assert.That(first.GetParent()!.EqualsPath(root), Is.True);
        Assert.That(nested.GetParent()!.EqualsPath(first), Is.True);
        Assert.That(nested.GetLastChildIndex(), Is.EqualTo(3));
        Assert.That(root.IsPrefixOf(nested), Is.True);
        Assert.That(first.IsPrefixOf(nested), Is.True);
        Assert.That(nested.IsPrefixOf(nested), Is.True);
        Assert.That(nested.IsPrefixOf(first), Is.False);
        Assert.That(first.IsPrefixOf(root.Append(2)), Is.False);
        Assert.That(first.IsPrefixOf(null!), Is.False);
    }
}
