using System.Collections.Generic;
using xml;

namespace xml.Tests;

[TestFixture]
public class DtdValidationPolicyTests
{
    [Test]
    public void NonValidatingParsingSkipsValidityErrorsAndWhitespaceClassification()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root EMPTY>]><root first='1' second='2'> </root>";

        XmlDocument document = Parse(text, XmlValidationMode.None, false, false, null);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.DocumentElement!.Attributes, Has.Length.EqualTo(2));
        Assert.That(document.DocumentElement.Children, Has.Length.EqualTo(1));
        Assert.That(document.DocumentElement.Children![0].Value, Is.EqualTo(" "));
    }

    [Test]
    public void DtdValidationClassifiesAndOptionallyPreservesIgnorableWhitespace()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root (child)><!ELEMENT child EMPTY>]><root> \n<child/>\t</root>";

        XmlDocument omitted = Parse(text, XmlValidationMode.Dtd, false, false, null);
        XmlDocument preserved = Parse(text, XmlValidationMode.Dtd, false, true, null);

        Assert.That(omitted.DocumentElement!.Children, Has.Length.EqualTo(1));
        Assert.That(omitted.DocumentElement.Children![0].Kind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(preserved.DocumentElement!.Children, Has.Length.EqualTo(3));
        Assert.That(preserved.DocumentElement.Children![0].Value, Is.EqualTo(" \n"));
        Assert.That(preserved.DocumentElement.Children[1].Kind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(preserved.DocumentElement.Children[2].Value, Is.EqualTo("\t"));
    }

    [Test]
    public void MixedContentWhitespaceRemainsOrdinaryCharacterData()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root (#PCDATA|child)*><!ELEMENT child EMPTY>]><root> <child/> </root>";

        XmlDocument document = Parse(text, XmlValidationMode.Dtd, false, false, null);

        Assert.That(document.DocumentElement!.Children, Has.Length.EqualTo(3));
        Assert.That(document.DocumentElement.Children![0].Kind, Is.EqualTo(XmlNodeKind.Text));
        Assert.That(document.DocumentElement.Children[2].Kind, Is.EqualTo(XmlNodeKind.Text));
    }

    [Test]
    public void ClassifiesWhitespaceProducedByEntityExpansion()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root (child)><!ELEMENT child EMPTY><!ENTITY gap '   '>]><root>&gap;<child/></root>";

        XmlDocument omitted = Parse(text, XmlValidationMode.Dtd, false, false, null);
        XmlDocument preserved = Parse(text, XmlValidationMode.Dtd, false, true, null);

        Assert.That(omitted.DocumentElement!.Children, Has.Length.EqualTo(1));
        Assert.That(omitted.DocumentElement.Children![0].Kind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(preserved.DocumentElement!.Children, Has.Length.EqualTo(2));
        Assert.That(preserved.DocumentElement.Children![0].Value, Is.EqualTo("   "));
    }

    [Test]
    public void AccumulatesValidityDiagnosticsAndReturnsTheInvalidDocument()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root EMPTY>]><root first='1' second='2'>text</root>";
        RecordingReporter reporter = new RecordingReporter();

        XmlDocument document = Parse(text, XmlValidationMode.Dtd, true, false, reporter);

        Assert.That(document.DocumentElement, Is.Not.Null);
        Assert.That(document.Diagnostics, Has.Length.EqualTo(3));
        Assert.That(reporter.Diagnostics, Has.Count.EqualTo(3));
        int index = 0;
        while (index < document.Diagnostics!.Length)
        {
            Assert.That(document.Diagnostics[index], Is.SameAs(reporter.Diagnostics[index]));
            Assert.That(document.Diagnostics[index].Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
            Assert.That(document.Diagnostics[index].Category, Is.EqualTo(XmlErrorCategory.Validation));
            Assert.That(document.Diagnostics[index].Severity, Is.EqualTo(XmlDiagnosticSeverity.Error));
            index = index + 1;
        }
    }

    [Test]
    public void UsesFailFastValidityReportingByDefault()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root EMPTY>]><root first='1' second='2'>text</root>";
        RecordingReporter reporter = new RecordingReporter();

        XmlValidationException failure = Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, false, false, reporter))!;

        Assert.That(reporter.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostics[0]));
    }

    [Test]
    public void AccumulationDoesNotRecoverFromWellFormednessErrors()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root ANY>]><root>";

        Assert.Throws<XmlWellFormednessException>(() => Parse(text, XmlValidationMode.Dtd, true, false, null));
    }

    private static XmlDocument Parse(string text, XmlValidationMode validationMode, bool accumulate, bool preserveIgnorableWhitespace, IXmlErrorReporter? reporter)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "urn:validation-policy";
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = validationMode;
        options.AccumulateValidationDiagnostics = accumulate;
        options.PreserveIgnorableWhitespace = preserveIgnorableWhitespace;
        return new XmlParser().ParseXmlDocument(input, options, null, reporter, null);
    }

    private sealed class RecordingReporter : IXmlErrorReporter
    {
        public List<XmlDiagnostic> Diagnostics = new List<XmlDiagnostic>();

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }
}
