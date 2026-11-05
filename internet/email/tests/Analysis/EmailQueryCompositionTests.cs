using System.Globalization;
using System.Text;
using email;

namespace email.Tests;

public class EmailQueryCompositionTests
{
    [Test]
    public void AndOrNotComposeLeafPredicatesLeftToRight()
    {
        EmailMessage message = ParseMessage("From: alice@example.test\r\nSubject: Quarterly Report\r\n\r\nhello world");
        EmailQuery sender = new EmailQueryOperations().AddressDomainEqualsWithFieldNameAndDomain("From", "example.test");
        EmailQuery subject = new EmailQueryOperations().DecodedFieldContains("Subject", "quarterly");
        EmailQuery missing = new EmailQueryOperations().HasAttachment();

        Assert.That(new EmailQueryOperations().AndWithLeftAndRight(sender, subject).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().AndWithLeftAndRight(sender, missing).Matches(message), Is.False);
        Assert.That(new EmailQueryOperations().OrWithLeftAndRight(missing, subject).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().OrWithLeftAndRight(missing, new EmailQueryOperations().HasField("X-Missing")).Matches(message), Is.False);
        Assert.That(new EmailQueryOperations().Not(missing).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().Not(subject).Matches(message), Is.False);

        List<EmailQuery> allChildren = new List<EmailQuery> { sender, subject, new EmailQueryOperations().BodyContains("world") };
        List<EmailQuery> anyChildren = new List<EmailQuery> { missing, new EmailQueryOperations().HasField("X-Missing"), subject };
        Assert.That(new EmailQueryOperations().AllWithQueries(allChildren).Matches(message), Is.True);
        Assert.That(new EmailQueryOperations().AnyWithQueries(anyChildren).Matches(message), Is.True);
    }

    [Test]
    public void ComposedNodesDefensivelyStoreAndExposeChildren()
    {
        EmailQuery first = new EmailQueryOperations().HasField("From");
        EmailQuery second = new EmailQueryOperations().HasField("Subject");
        List<EmailQuery> supplied = new List<EmailQuery> { first, second };

        EmailQuery query = new EmailQueryOperations().AndWithQueries(supplied);
        supplied.Clear();
        List<EmailQuery> returned = query.GetChildren();
        returned.Clear();

        Assert.That(query.GetKind(), Is.EqualTo(EmailQueryKind.And));
        Assert.That(query.GetChildCount(), Is.EqualTo(2));
        Assert.That(query.GetChild(0), Is.SameAs(first));
        Assert.That(query.GetChild(1), Is.SameAs(second));
        EmailQuery negated = new EmailQueryOperations().Not(query);
        Assert.That(negated.GetKind(), Is.EqualTo(EmailQueryKind.Not));
        Assert.That(negated.GetChildCount(), Is.EqualTo(1));
        Assert.That(negated.GetChild(0), Is.SameAs(query));
        Assert.Throws<ArgumentOutOfRangeException>(() => query.GetChild(2));
    }

    [Test]
    public void AndAndOrShortCircuitBeforeCostlyOrLimitedBranches()
    {
        EmailMessage message = ParseMessage("From: alice@example.test\r\nSubject: present\r\n\r\na long body");
        EmailQuery expensive = new EmailQueryOperations().BodyContains("absent");
        EmailQuery firstTrue = new EmailQueryOperations().HasField("Subject");
        EmailQuery firstFalse = new EmailQueryOperations().HasField("X-Missing");
        EmailQueryOptions options = new EmailQueryOptions();
        options.SetMaximumBodyCharacters(1);

        Assert.That(new EmailQueryOperations().AndWithLeftAndRight(firstFalse, expensive).MatchesWithOptions(message, options), Is.False);
        Assert.That(new EmailQueryOperations().OrWithLeftAndRight(firstTrue, expensive).MatchesWithOptions(message, options), Is.True);
        AssertLimit("MaxQueryBodyCharacters", () => new EmailQueryOperations().AndWithLeftAndRight(firstTrue, expensive).MatchesWithOptions(message, options));
        AssertLimit("MaxQueryBodyCharacters", () => new EmailQueryOperations().OrWithLeftAndRight(firstFalse, expensive).MatchesWithOptions(message, options));
    }

    [Test]
    public void CompositionDepthNodeAndFanoutLimitsAreEnforcedBeforeEvaluation()
    {
        EmailMessage message = ParseMessage("From: alice@example.test\r\n\r\nbody");
        EmailQuery leaf = new EmailQueryOperations().HasField("From");
        EmailQuery deep = new EmailQueryOperations().Not(new EmailQueryOperations().Not(leaf));
        EmailQuery threeNodes = new EmailQueryOperations().AndWithLeftAndRight(leaf, new EmailQueryOperations().HasField("Subject"));
        EmailQuery fanout = new EmailQueryOperations().OrWithQueries(new List<EmailQuery> { leaf, new EmailQueryOperations().HasField("To"), new EmailQueryOperations().HasField("Cc") });
        EmailQueryOptions options = new EmailQueryOptions();

        options.SetMaximumQueryDepth(2);
        AssertLimit("MaxQueryNestingDepth", () => deep.MatchesWithOptions(message, options));
        options.SetMaximumQueryDepth(10);
        options.SetMaximumQueryNodeCount(2);
        AssertLimit("MaxQueryNodeCount", () => threeNodes.MatchesWithOptions(message, options));
        options.SetMaximumQueryNodeCount(10);
        options.SetMaximumChildrenPerNode(2);
        AssertLimit("MaxQueryChildren", () => fanout.MatchesWithOptions(message, options));

        List<EmailQuery> excessive = new List<EmailQuery>();
        int index = 0;
        while (index <= EmailQueryOptionsConstants.DefaultMaximumChildrenPerNode)
        {
            excessive.Add(leaf);
            index++;
        }

        AssertLimit("MaxQueryChildren", () => new EmailQueryOperations().AndWithQueries(excessive));
        EmailQueryOptions expanded = new EmailQueryOptions();
        expanded.SetMaximumChildrenPerNode(excessive.Count);
        Assert.That(new EmailQueryOperations().AndWithQueriesAndOptions(excessive, expanded).GetChildCount(), Is.EqualTo(excessive.Count));
    }

    [Test]
    public void DefaultTextComparisonRemainsOrdinalUnderNonEnglishCulture()
    {
        EmailMessage message = ParseMessage("From: alice@example.test\r\nSubject: FILE I\r\n\r\nbody");
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            EmailQuery query = new EmailQueryOperations().AndWithLeftAndRight(new EmailQueryOperations().HasField("subject"), new EmailQueryOperations().DecodedFieldContains("Subject", "file i"));
            Assert.That(query.Matches(message), Is.True);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void CompositionInputsAndOptionsAreValidatedAndCloned()
    {
        EmailQuery leaf = new EmailQueryOperations().HasField("From");
        Assert.Throws<ArgumentNullException>(() => new EmailQueryOperations().AndWithQueries((List<EmailQuery>)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailQueryOperations().AndWithQueriesAndOptions(new List<EmailQuery> { leaf, leaf }, null!));
        Assert.Throws<ArgumentNullException>(() => new EmailQueryOperations().AndWithLeftAndRight(null!, leaf));
        Assert.Throws<ArgumentNullException>(() => new EmailQueryOperations().OrWithLeftAndRight(leaf, null!));
        Assert.Throws<ArgumentNullException>(() => new EmailQueryOperations().Not(null!));
        Assert.Throws<ArgumentException>(() => new EmailQueryOperations().AndWithQueries(new List<EmailQuery>()));
        Assert.Throws<ArgumentException>(() => new EmailQueryOperations().OrWithQueries(new List<EmailQuery> { leaf }));
        Assert.Throws<ArgumentException>(() => new EmailQueryOperations().AndWithQueries(new List<EmailQuery> { leaf, null! }));

        EmailQueryOptions options = new EmailQueryOptions();
        options.SetMaximumQueryDepth(4);
        options.SetMaximumQueryNodeCount(5);
        options.SetMaximumChildrenPerNode(6);
        EmailQueryOptions clone = options.Clone();
        options.SetMaximumQueryDepth(7);
        options.SetMaximumQueryNodeCount(8);
        options.SetMaximumChildrenPerNode(9);
        Assert.That(clone.GetMaximumQueryDepth(), Is.EqualTo(4));
        Assert.That(clone.GetMaximumQueryNodeCount(), Is.EqualTo(5));
        Assert.That(clone.GetMaximumChildrenPerNode(), Is.EqualTo(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumQueryDepth(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumQueryNodeCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumChildrenPerNode(0));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMessage()!;
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
