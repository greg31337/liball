
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlNthExpressionTests
{
    [TestCase("odd", 2, 1)]
    [TestCase("EVEN", 2, 0)]
    [TestCase("n", 1, 0)]
    [TestCase("+n", 1, 0)]
    [TestCase("-n+3", -1, 3)]
    [TestCase("+3n - 2", 3, -2)]
    [TestCase("0n+5", 0, 5)]
    [TestCase("-2147483648n+2147483647", int.MinValue, int.MaxValue)]
    public void ParsesCompleteSignedAnPlusBExpressions(string value,
        int step, int offset)
    {
        HtmlNthExpressionParseResult result = HtmlNthExpression.Parse(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.WasOverflow(), Is.False);
            Assert.That(result.GetExpression().GetStep(), Is.EqualTo(step));
            Assert.That(result.GetExpression().GetOffset(),
                Is.EqualTo(offset));
        });
    }

    [Test]
    public void MatchesOnlyPositiveOneBasedIndicesWithoutOverflow()
    {
        HtmlNthExpression odd = Parse("2n+1");
        HtmlNthExpression descending = Parse("-2n+5");
        HtmlNthExpression exact = Parse("0n+4");
        HtmlNthExpression extreme = new HtmlNthExpression(int.MinValue,
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(Enumerable.Range(0, 8).Where(odd.Matches),
                Is.EqualTo(new[] { 1, 3, 5, 7 }));
            Assert.That(Enumerable.Range(0, 8).Where(descending.Matches),
                Is.EqualTo(new[] { 1, 3, 5 }));
            Assert.That(Enumerable.Range(0, 8).Where(exact.Matches),
                Is.EqualTo(new[] { 4 }));
            Assert.That(extreme.Matches(int.MaxValue), Is.True);
            Assert.That(extreme.Matches(-1), Is.False);
            Assert.That(odd.Matches(0), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("3 n")]
    [TestCase("n+")]
    [TestCase("3n + -1")]
    [TestCase("oddly")]
    public void RejectsMalformedExpressions(string value)
    {
        HtmlNthExpressionParseResult result = HtmlNthExpression.Parse(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasOverflow(), Is.False);
            Assert.That(result.GetExpression(), Is.Null);
        });
    }

    [TestCase("2147483648n")]
    [TestCase("n-2147483649")]
    public void ReportsExpressionOverflow(string value)
    {
        HtmlNthExpressionParseResult result = HtmlNthExpression.Parse(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasOverflow(), Is.True);
        });
    }

    static HtmlNthExpression Parse(string value)
    {
        HtmlNthExpressionParseResult result = HtmlNthExpression.Parse(value);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetExpression();
    }
}
