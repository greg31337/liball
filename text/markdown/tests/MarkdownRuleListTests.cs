using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownRuleListTests
{
    [Test]
    public void RuleRegistration_StoresNameAndRule()
    {
        object rule = new object();
        MarkdownRuleRegistration registration = MarkdownRuleRegistration.Create("paragraph", rule);

        Assert.That(registration.Name, Is.EqualTo("paragraph"));
        Assert.That(registration.Rule, Is.SameAs(rule));
        Assert.That(registration.Copy().Rule, Is.SameAs(rule));
    }

    [Test]
    public void RuleList_AddInsertAndSetMaintainOrder()
    {
        object first = new object();
        object second = new object();
        object third = new object();
        object replacement = new object();
        MarkdownRuleList rules = new MarkdownRuleList();

        rules.Add("first", first);
        rules.Add("third", third);
        rules.Insert(1, "second", second);
        rules.Set("third", replacement);

        Assert.That(rules.GetCount(), Is.EqualTo(3));
        Assert.That(rules.GetAt(0).Name, Is.EqualTo("first"));
        Assert.That(rules.GetAt(1).Name, Is.EqualTo("second"));
        Assert.That(rules.GetAt(2).Name, Is.EqualTo("third"));
        Assert.That(rules.FindRule("third"), Is.SameAs(replacement));
    }

    [Test]
    public void RuleList_SetAddsMissingRuleAtEnd()
    {
        MarkdownRuleList rules = new MarkdownRuleList();

        rules.Add("first", new object());
        rules.Set("second", new object());

        Assert.That(rules.GetCount(), Is.EqualTo(2));
        Assert.That(rules.GetAt(1).Name, Is.EqualTo("second"));
    }

    [Test]
    public void RuleList_RemoveAndClearUpdateCollection()
    {
        MarkdownRuleList rules = new MarkdownRuleList();

        rules.Add("first", new object());
        rules.Add("second", new object());

        Assert.That(rules.HasRules(), Is.True);
        Assert.That(rules.Remove("first"), Is.True);
        Assert.That(rules.Remove("first"), Is.False);
        Assert.That(rules.Contains("second"), Is.True);

        rules.Clear();

        Assert.That(rules.GetCount(), Is.EqualTo(0));
        Assert.That(rules.HasRules(), Is.False);
    }

    [Test]
    public void RuleList_ReturnsCopiesForRegistrations()
    {
        MarkdownRuleList rules = new MarkdownRuleList();

        rules.Add("first", new object());
        MarkdownRuleRegistration registration = rules.GetAt(0);
        MarkdownRuleRegistration[] registrations = rules.GetRegistrations();
        MarkdownRuleList copy = rules.Copy();

        registration.Name = "mutated";
        registrations[0].Name = "also-mutated";
        copy.Set("first", new object());

        Assert.That(rules.GetAt(0).Name, Is.EqualTo("first"));
        Assert.That(rules.Find("missing"), Is.Null);
        Assert.That(rules.Find("first")!.Name, Is.EqualTo("first"));
    }

    [Test]
    public void RuleList_RejectsInvalidArguments()
    {
        MarkdownRuleList rules = new MarkdownRuleList();

        rules.Add("first", new object());

        Assert.That(() => MarkdownRuleRegistration.Create(null!, new object()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRuleRegistration.Create("", new object()), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownRuleRegistration.Create("name", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRuleRegistration.ValidateName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRuleRegistration.ValidateName(""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => rules.GetAt(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => rules.GetAt(1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => rules.IndexOf(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => rules.Add("first", new object()), Throws.TypeOf<ArgumentException>());
        Assert.That(() => rules.Add("second", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => rules.Insert(-1, "second", new object()), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => rules.Insert(2, "second", new object()), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
