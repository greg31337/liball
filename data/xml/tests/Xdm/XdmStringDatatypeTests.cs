using xml;

namespace xml.Tests;

[TestFixture]
public class XdmStringDatatypeTests
{
    [Test]
    public void AppliesPreserveReplaceAndCollapseWhitespaceFacets()
    {
        XdmStringDatatype datatype = new XdmStringDatatype();
        string lexical = " \ta\r\n  b\u00A0 ";

        XdmAtomicValue preserved = datatype.ParseXdmStringAtomic(lexical, XdmAtomicType.String);
        XdmAtomicValue replaced = datatype.ParseXdmStringAtomic(lexical, XdmAtomicType.NormalizedString);
        XdmAtomicValue collapsed = datatype.ParseXdmStringAtomic(lexical, XdmAtomicType.Token);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.StringValue, Is.EqualTo(" \ta\r\n  b\u00A0 "));
            Assert.That(replaced.StringValue, Is.EqualTo("  a    b\u00A0 "));
            Assert.That(collapsed.StringValue, Is.EqualTo("a b\u00A0"));
            Assert.That(preserved.LexicalValue, Is.EqualTo(lexical));
            Assert.That(replaced.LexicalValue, Is.EqualTo(lexical));
            Assert.That(collapsed.LexicalValue, Is.EqualTo(lexical));
            Assert.That(preserved.CanonicalValue, Is.EqualTo(preserved.StringValue));
            Assert.That(replaced.CanonicalValue, Is.EqualTo(replaced.StringValue));
            Assert.That(collapsed.CanonicalValue, Is.EqualTo(collapsed.StringValue));
            Assert.That(collapsed.IsImmutable, Is.True);
            Assert.That(datatype.IsValidXdmStringValue("a b", XdmAtomicType.Token), Is.True);
            Assert.That(datatype.IsValidXdmStringValue(" a  b ", XdmAtomicType.Token), Is.False);
            Assert.That(datatype.IsValidXdmStringValue("a\tb", XdmAtomicType.NormalizedString), Is.False);
        });
    }

    [Test]
    public void ProducesLexicalValueAndCanonicalFormsForEveryStringDerivedType()
    {
        XdmStringDatatype datatype = new XdmStringDatatype();
        XdmAtomicType[] types =
        [
            XdmAtomicType.String,
            XdmAtomicType.NormalizedString,
            XdmAtomicType.Token,
            XdmAtomicType.Language,
            XdmAtomicType.Name,
            XdmAtomicType.NcName,
            XdmAtomicType.Nmtoken,
            XdmAtomicType.Id,
            XdmAtomicType.Idref,
            XdmAtomicType.Entity
        ];

        foreach (XdmAtomicType type in types)
        {
            XdmAtomicValue value = datatype.ParseXdmStringAtomic("  Alpha  ", type);
            string expected = type == XdmAtomicType.String ? "  Alpha  " : type == XdmAtomicType.NormalizedString ? "  Alpha  " : "Alpha";
            Assert.Multiple(() =>
            {
                Assert.That(value.Type, Is.EqualTo(type));
                Assert.That(value.LexicalValue, Is.EqualTo("  Alpha  "));
                Assert.That(value.StringValue, Is.EqualTo(expected));
                Assert.That(value.CanonicalValue, Is.EqualTo(expected));
                Assert.That(value.IsImmutable, Is.True);
            });
        }

        XdmAtomicValue fromFactory = new XdmFactory().CreateXdmStringAtomicValue("\tidentifier\r", XdmAtomicType.Id);
        Assert.Multiple(() =>
        {
            Assert.That(fromFactory.Type, Is.EqualTo(XdmAtomicType.Id));
            Assert.That(fromFactory.StringValue, Is.EqualTo("identifier"));
            Assert.That(fromFactory.CanonicalValue, Is.EqualTo("identifier"));
        });
    }

    [Test]
    public void ValidatesTheXmlSchemaLanguagePatternWithoutChangingCase()
    {
        XdmStringDatatype datatype = new XdmStringDatatype();
        string[] valid = ["en", "EN-us", "x-private1", "abcdefgh-12345678", "  fr-CA  "];
        string[] invalid = ["", "1en", "abcdefghi", "en-", "-en", "en--US", "en-abcdefghi", "en_US", "é"];

        foreach (string lexical in valid)
        {
            XdmStringParseResult result = datatype.TryParseXdmStringAtomic(lexical, XdmAtomicType.Language);
            Assert.That(result.Success, Is.True, lexical);
        }

        foreach (string lexical in invalid)
        {
            XdmStringParseResult result = datatype.TryParseXdmStringAtomic(lexical, XdmAtomicType.Language);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, lexical);
                Assert.That(result.Value, Is.Null, lexical);
                Assert.That(result.ErrorMessage, Does.Contain("xs:language"), lexical);
            });
        }

        Assert.That(datatype.ParseXdmStringAtomic("EN-us", XdmAtomicType.Language).CanonicalValue, Is.EqualTo("EN-us"));
    }

    [Test]
    public void DistinguishesNameNcNameAndNmtokenLexicalSpaces()
    {
        XdmStringDatatype datatype = new XdmStringDatatype();
        string supplementaryName = "\U00010000name";

        Assert.Multiple(() =>
        {
            Assert.That(datatype.TryParseXdmStringAtomic(":qualified", XdmAtomicType.Name).Success, Is.True);
            Assert.That(datatype.TryParseXdmStringAtomic("prefix:local", XdmAtomicType.Name).Success, Is.True);
            Assert.That(datatype.TryParseXdmStringAtomic(supplementaryName, XdmAtomicType.Name).Success, Is.True);
            Assert.That(datatype.TryParseXdmStringAtomic("1token.-:part", XdmAtomicType.Nmtoken).Success, Is.True);
            Assert.That(datatype.TryParseXdmStringAtomic("\u0301", XdmAtomicType.Nmtoken).Success, Is.True);
            Assert.That(datatype.TryParseXdmStringAtomic("_local", XdmAtomicType.NcName).Success, Is.True);
            Assert.That(datatype.TryParseXdmStringAtomic("prefix:local", XdmAtomicType.NcName).Success, Is.False);
            Assert.That(datatype.TryParseXdmStringAtomic("1local", XdmAtomicType.NcName).Success, Is.False);
            Assert.That(datatype.TryParseXdmStringAtomic(".token", XdmAtomicType.Name).Success, Is.False);
            Assert.That(datatype.TryParseXdmStringAtomic("", XdmAtomicType.Name).Success, Is.False);
            Assert.That(datatype.TryParseXdmStringAtomic("", XdmAtomicType.Nmtoken).Success, Is.False);
        });
    }

    [Test]
    public void AppliesNcNameRulesToIdIdrefAndEntityFamiliesAfterCollapse()
    {
        XdmStringDatatype datatype = new XdmStringDatatype();
        XdmAtomicType[] types = [XdmAtomicType.NcName, XdmAtomicType.Id, XdmAtomicType.Idref, XdmAtomicType.Entity];

        foreach (XdmAtomicType type in types)
        {
            XdmAtomicValue valid = datatype.ParseXdmStringAtomic(" \tValid_Name-7\r\n", type);
            XdmStringParseResult colon = datatype.TryParseXdmStringAtomic("prefix:name", type);
            XdmStringParseResult internalSpace = datatype.TryParseXdmStringAtomic("two names", type);
            Assert.Multiple(() =>
            {
                Assert.That(valid.StringValue, Is.EqualTo("Valid_Name-7"));
                Assert.That(valid.CanonicalValue, Is.EqualTo("Valid_Name-7"));
                Assert.That(colon.Success, Is.False);
                Assert.That(internalSpace.Success, Is.False);
            });
        }
    }

    [Test]
    public void RejectsMalformedUnicodeAndCharactersOutsideTheXmlCharacterSet()
    {
        XdmStringDatatype datatype = new XdmStringDatatype();
        string[] invalid = ["\uD800", "\uDC00", "\uD800x", "contains\0null", "\uFFFF"];

        foreach (string lexical in invalid)
        {
            XdmStringParseResult result = datatype.TryParseXdmStringAtomic(lexical, XdmAtomicType.String);
            Assert.That(result.Success, Is.False, "U+" + ((int)lexical[0]).ToString("X"));
        }

        Assert.That(datatype.TryParseXdmStringAtomic("\U00010000", XdmAtomicType.String).Success, Is.True);
    }

    [Test]
    public void ReportsInvalidLexicalFormsAsStandardDynamicErrors()
    {
        XdmStringDatatype datatype = new XdmStringDatatype();

        XPathDynamicException language = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmStringAtomic("not_a_language", XdmAtomicType.Language))!;
        XPathDynamicException name = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmStringAtomic("two names", XdmAtomicType.NcName))!;
        Assert.Multiple(() =>
        {
            Assert.That(language.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(language.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XPathDynamic));
            Assert.That(language.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
            Assert.That(name.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.Throws<ArgumentException>(() => datatype.ParseXdmStringAtomic("1", XdmAtomicType.Integer));
            Assert.Throws<ArgumentOutOfRangeException>(() => datatype.ParseXdmStringAtomic("value", (XdmAtomicType)99));
            Assert.Throws<ArgumentNullException>(() => datatype.ParseXdmStringAtomic(null!, XdmAtomicType.String));
            Assert.That(datatype.IsValidXdmStringLexical(" \tname ", XdmAtomicType.NcName), Is.True);
            Assert.That(datatype.CanonicalizeXdmStringValue("name", XdmAtomicType.NcName), Is.EqualTo("name"));
            Assert.Throws<XPathDynamicException>(() => datatype.CanonicalizeXdmStringValue("two names", XdmAtomicType.NcName));
        });
    }

    [Test]
    public void FactoryValidationEnforcesConsistentStringRepresentations()
    {
        XdmFactory factory = new XdmFactory();
        XdmAtomicValue valid = new XdmAtomicValue { Type = XdmAtomicType.Token, LexicalValue = " a  b ", StringValue = "a b", CanonicalValue = "a b" };

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => factory.ValidateXdmAtomicValue(valid));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.Token, StringValue = " a  b " }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.Token, LexicalValue = "a b", StringValue = "different" }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.Token, StringValue = "a b", CanonicalValue = "a  b" }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.Language, LexicalValue = "not_a_language" }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.String, ExtendedValue = new object() }));
        });
    }
}
