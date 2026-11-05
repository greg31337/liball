namespace uri.Tests;

public class UriTemplateOperatorDataTests
{
    [Test]
    public void DefinedOperatorsMatchRfc6570()
    {
        string operators = string.Concat(
            UriTemplateOperatorData.ReservedExpansion,
            UriTemplateOperatorData.FragmentExpansion,
            UriTemplateOperatorData.LabelExpansion,
            UriTemplateOperatorData.PathSegmentExpansion,
            UriTemplateOperatorData.PathParameterExpansion,
            UriTemplateOperatorData.QueryExpansion,
            UriTemplateOperatorData.QueryContinuationExpansion);

        Assert.That(operators, Is.EqualTo("+#./;?&"));
    }

    [Test]
    public void FutureOperatorSetMatchesRfc6570()
    {
        Assert.That(UriTemplateOperatorData.ReservedFutureOperators, Is.EqualTo("=,!@|"));
    }
}
