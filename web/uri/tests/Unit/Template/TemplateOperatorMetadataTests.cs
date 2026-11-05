namespace uri.Tests;

public class TemplateOperatorMetadataTests
{
    [TestCase(UriTemplateOperator.Simple, "", ",", false, "", false)]
    [TestCase(UriTemplateOperator.Reserved, "", ",", false, "", true)]
    [TestCase(UriTemplateOperator.Fragment, "#", ",", false, "", true)]
    [TestCase(UriTemplateOperator.Label, ".", ".", false, "", false)]
    [TestCase(UriTemplateOperator.PathSegment, "/", "/", false, "", false)]
    [TestCase(UriTemplateOperator.PathParameter, ";", ";", true, "", false)]
    [TestCase(UriTemplateOperator.Query, "?", "&", true, "=", false)]
    [TestCase(UriTemplateOperator.QueryContinuation, "&", "&", true, "=", false)]
    public void EveryDefinedOperatorMatchesTheRfc6570ExpansionTable(
        UriTemplateOperator value,
        string prefix,
        string separator,
        bool named,
        string emptyValue,
        bool allowReserved)
    {
        TemplateOperatorMetadata metadata = TemplateOperatorTable.Get(value);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Prefix, Is.EqualTo(prefix));
            Assert.That(metadata.Separator, Is.EqualTo(separator));
            Assert.That(metadata.Named, Is.EqualTo(named));
            Assert.That(metadata.EmptyValue, Is.EqualTo(emptyValue));
            Assert.That(metadata.AllowReserved, Is.EqualTo(allowReserved));
        });
    }

    [Test]
    public void MetadataLookupRejectsUndefinedOperatorValues()
    {
        Assert.That(
            () => TemplateOperatorTable.Get((UriTemplateOperator)99),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("value"));
    }
}
