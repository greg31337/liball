using System.IO;
using xml;

namespace xml.Tests;

[TestFixture]
public class CompiledArtifactIsolationTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void XPathEvaluationUsesTheCompiledPlanAfterPublicHandleMutation()
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("$value + 1", new XPathStaticContext
        {
            Variables = [new XPathVariableBinding { Name = new XmlName { LocalName = "value" } }]
        });
        expression.Source = "0";
        expression.StaticContext = new XPathStaticContext();
        expression.Plan = null;
        expression.IsThreadSafe = false;

        XPathEvaluationResult first = new XPathEvaluator().EvaluateXPath(expression, DynamicInteger(4));
        XPathEvaluationResult second = new XPathEvaluator().EvaluateXPath(expression, DynamicInteger(9));

        Assert.Multiple(() =>
        {
            Assert.That(first.Value!.AtomicValues!.Single().IntegerValue, Is.EqualTo(5));
            Assert.That(second.Value!.AtomicValues!.Single().IntegerValue, Is.EqualTo(10));
            Assert.That(first.DynamicContext, Is.Not.SameAs(second.DynamicContext));
            Assert.That(first.DynamicContext!.Variables, Is.Not.SameAs(second.DynamicContext!.Variables));
        });
    }

    [Test]
    public void SchemaValidationUsesTheCompiledSetAfterPublicHandleMutation()
    {
        XmlSchemaSet schemas = CompileSchema();
        schemas.CompiledSet = null;
        schemas.SchemaDocuments = [];
        schemas.TargetNamespaces = [];
        schemas.IsImmutable = false;

        XmlSchemaValidationResult valid = new XmlSchemaValidator().ValidateXmlSchema(Parse("<root xmlns='urn:test' code='7'/>", "instance-one.xml"), schemas, new XmlSchemaValidationOptions());
        XmlSchemaValidationResult invalid = new XmlSchemaValidator().ValidateXmlSchema(Parse("<root xmlns='urn:test' code='bad'/>", "instance-two.xml"), schemas, new XmlSchemaValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(invalid.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(invalid.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void XsltRunsKeepPackagePatternsKeysAndCharacterMapsIsolated()
    {
        string source =
            "<xsl:stylesheet xmlns:xsl='" + Xsl + "' version='3.0'>" +
            "<xsl:character-map name='mapped'><xsl:output-character character='a' string='[A]'/></xsl:character-map>" +
            "<xsl:output method='text' use-character-maps='mapped'/>" +
            "<xsl:key name='by-id' match='item' use='@id'/>" +
            "<xsl:template match='/'><xsl:apply-templates select='root/item'/></xsl:template>" +
            "<xsl:template match=\"item[@id='one']\"><xsl:value-of select=\"concat('a', count(key('by-id', @id)))\"/></xsl:template>" +
            "<xsl:template match='item'><xsl:text>a0</xsl:text></xsl:template>" +
            "</xsl:stylesheet>";
        XsltCompileOptions compileOptions = new XsltCompileOptions
        {
            BaseUri = "https://example.test/package.xsl",
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Allowed, AllowedSchemes = ["https"] }
        };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = source, BaseUri = compileOptions.BaseUri }, compileOptions, new XmlDefaultResolver());
        executable.Plan = null;
        executable.CompileOptions = new XsltCompileOptions { ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied } };
        executable.PackageName = new XmlName { LocalName = "changed" };
        executable.PackageVersion = "changed";
        executable.IsImmutable = false;
        executable.IsStreaming = true;
        executable.IsThreadSafe = false;

        string first = Transform(executable, "<root><item id='one'/></root>");
        string second = Transform(executable, "<root><item id='one'/><item id='one'/></root>");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("[A]1"));
            Assert.That(second, Is.EqualTo("[A]2[A]2"));
        });
    }

    private static XPathDynamicContext DynamicInteger(long value)
    {
        XdmAtomicValue atomic = new XdmFactory().CreateXdmIntegerAtomicValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture), XdmAtomicType.Integer);
        return new XPathDynamicContext
        {
            Variables = [new XPathVariableBinding
            {
                Name = new XmlName { LocalName = "value" },
                Value = new XdmSequenceOperations().CreateXdmAtomicSequence(atomic)
            }]
        };
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema = "<xs:schema xmlns:xs='" + Xsd + "' targetNamespace='urn:test' elementFormDefault='qualified'><xs:element name='root'><xs:complexType><xs:attribute name='code' type='xs:int' use='required'/></xs:complexType></xs:element></xs:schema>";
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Text = schema, BaseUri = "https://example.test/schema.xsd" }],
            new XmlSchemaValidationOptions(),
            new XmlDefaultResolver());
    }

    private static string Transform(XsltExecutable executable, string source)
    {
        StringWriter writer = new StringWriter();
        XsltTransformResult result = new XsltTransformer().TransformXslt(executable, new XsltTransformOptions
        {
            SourceDocument = Parse(source, "source.xml"),
            PrincipalOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }
        });
        Assert.That(result.Succeeded, Is.True);
        return writer.ToString();
    }

    private static XmlDocument Parse(string source, string name)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source, BaseUri = "https://example.test/" + name }, options, null, null, null);
    }
}
