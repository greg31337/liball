using xml;

namespace xml.Tests;

[TestFixture]
public class DtdContentValidatorTests
{
    [Test]
    public void CompilesDeterministicPositionTransitions()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);
        parser.LoadDtdSubsets("<!ELEMENT root (head,(section|note)+,tail?)>", null, null, null);

        DtdCompiledContentModel model = parser.FindDtdCompiledContentModel("root")!;

        Assert.That(model.PositionNames, Is.EqualTo(new string[] { "head", "section", "note", "tail" }));
        Assert.That(model.InitialPositions.Select(position => model.PositionNames[position]), Is.EqualTo(new string[] { "head" }));
        Assert.That(model.FollowPositions[0].Select(position => model.PositionNames[position]), Is.EquivalentTo(new string[] { "section", "note" }));
        Assert.That(model.FollowPositions[1].Select(position => model.PositionNames[position]), Is.EquivalentTo(new string[] { "section", "note", "tail" }));
        Assert.That(model.FinalPositions.Select(position => model.PositionNames[position]), Is.EquivalentTo(new string[] { "section", "note", "tail" }));
        Assert.That(model.AcceptsEmpty, Is.False);
    }

    [Test]
    public void AdvancesACompiledModelIncrementally()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);
        parser.LoadDtdSubsets("<!ELEMENT root (head,(section|note)+,tail?)>", null, null, null);
        DtdContentValidator validator = new DtdContentValidator(parser, new DtdValidationContext(null, null, false));

        validator.BeginDtdElementContent("root", 0, false);
        validator.BeginDtdElementContent("head", 1, true);
        validator.BeginDtdElementContent("section", 2, true);
        validator.BeginDtdElementContent("note", 3, true);
        validator.BeginDtdElementContent("tail", 4, true);
        validator.EndDtdElementContent("root", 5);

        Assert.That(validator.States, Is.Empty);
    }

    [TestCase("<root><b/></root>")]
    [TestCase("<root/>")]
    [TestCase("<root><a/><b/><b/></root>")]
    [TestCase("<root><a/>text</root>")]
    public void RejectsChildrenThatDoNotMatchTheDeclaredSequence(string documentContent)
    {
        string text = "<!DOCTYPE root [<!ELEMENT root (a,b?)><!ELEMENT a EMPTY><!ELEMENT b EMPTY>]>" + documentContent;

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, null));
    }

    [Test]
    public void AcceptsOptionalRepeatedAndWhitespaceSeparatedChildren()
    {
        string subset = "<!ELEMENT root (a,(b|c)*,d?)><!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY><!ELEMENT d EMPTY>";

        XmlDocument first = Parse("<!DOCTYPE root [" + subset + "]><root>\n<a/><b/><c/><b/>\n</root>", XmlValidationMode.Dtd, null);
        XmlDocument second = Parse("<!DOCTYPE root [" + subset + "]><root><a/><d/></root>", XmlValidationMode.Dtd, null);

        Assert.That(first.DocumentElement, Is.Not.Null);
        Assert.That(second.DocumentElement, Is.Not.Null);
    }

    [Test]
    public void EnforcesEmptyMixedAndAnyContentCategories()
    {
        string emptySubset = "<!ELEMENT root EMPTY>";
        Assert.That(Parse("<!DOCTYPE root [" + emptySubset + "]><root/>", XmlValidationMode.Dtd, null).DocumentElement, Is.Not.Null);
        Assert.Throws<XmlValidationException>(() => Parse("<!DOCTYPE root [" + emptySubset + "]><root><!--not-allowed--><?pi not-allowed?></root>", XmlValidationMode.Dtd, null));
        Assert.Throws<XmlValidationException>(() => Parse("<!DOCTYPE root [" + emptySubset + "]><root> </root>", XmlValidationMode.Dtd, null));
        Assert.Throws<XmlValidationException>(() => Parse("<!DOCTYPE root [" + emptySubset + "]><root><child/></root>", XmlValidationMode.Dtd, null));

        string mixedSubset = "<!ELEMENT root (#PCDATA|a)*><!ELEMENT a EMPTY>";
        Assert.That(Parse("<!DOCTYPE root [" + mixedSubset + "]><root>before<a/>after</root>", XmlValidationMode.Dtd, null).DocumentElement, Is.Not.Null);
        Assert.Throws<XmlValidationException>(() => Parse("<!DOCTYPE root [" + mixedSubset + "]><root><b/></root>", XmlValidationMode.Dtd, null));

        string anySubset = "<!ELEMENT root ANY><!ELEMENT undeclared ANY><!ELEMENT nested EMPTY>";
        Assert.That(Parse("<!DOCTYPE root [" + anySubset + "]><root>text<undeclared><nested/></undeclared></root>", XmlValidationMode.Dtd, null).DocumentElement, Is.Not.Null);
    }

    [Test]
    public void DoesNotApplyContentValidationWhenDtdValidationIsDisabled()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root (required)>]><root><different/></root>";

        XmlDocument document = Parse(text, XmlValidationMode.None, null);

        Assert.That(document.DocumentElement!.Children![0].Name!.QualifiedName, Is.EqualTo("different"));
    }

    [Test]
    public void ValidatesElementContentSuppliedByAnEntityReplacement()
    {
        string valid = "<!DOCTYPE root [<!ELEMENT root (child)><!ELEMENT child EMPTY><!ENTITY content '<child/>'>]><root>&content;</root>";
        string invalid = "<!DOCTYPE root [<!ELEMENT root (expected)><!ELEMENT actual EMPTY><!ENTITY content '<actual/>'>]><root>&content;</root>";

        Assert.That(Parse(valid, XmlValidationMode.Dtd, null).DocumentElement, Is.Not.Null);
        Assert.Throws<XmlValidationException>(() => Parse(invalid, XmlValidationMode.Dtd, null));
    }

    [Test]
    public void ReportsAValidityDiagnosticAtTheFailingChild()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root (expected)>]><root><actual/></root>";
        RecordingValidationReporter reporter = new RecordingValidationReporter();

        XmlValidationException failure = Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, reporter))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Validation));
        Assert.That(failure.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Error));
        Assert.That(failure.Location!.CharacterOffset, Is.EqualTo(text.IndexOf("<actual", StringComparison.Ordinal)));
    }

    private static XmlDocument Parse(string text, XmlValidationMode validationMode, IXmlErrorReporter? reporter)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "urn:document";
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = validationMode;
        return new XmlParser().ParseXmlDocument(input, options, null, reporter, null);
    }

    private sealed class RecordingValidationReporter : IXmlErrorReporter
    {
        public XmlDiagnostic? Diagnostic;

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }
}
