using System.Collections.Generic;
using xml;

namespace xml.Tests;

[TestFixture]
public class XsltPackageRuntimeTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public void PublicTransformerInvokesTheOriginalOverriddenTemplateComponent()
    {
        PackageResolver resolver = new PackageResolver();
        resolver.Resources.Add(
            "https://example.test/library.xsl",
            Package("urn:library", "<xsl:template name='hook' visibility='public'><xsl:sequence select=\"'base'\"/></xsl:template>"));
        string main = Package(
            "urn:main",
            "<xsl:use-package name='urn:library'><xsl:override>" +
            "<xsl:template name='hook' visibility='public'>" +
            "<xsl:variable name='original'><xsl:call-template name='xsl:original'/></xsl:variable>" +
            "<xsl:sequence select=\"concat($original, '!')\"/>" +
            "</xsl:template>" +
            "</xsl:override></xsl:use-package>" +
            "<xsl:template name='entry' visibility='public'><xsl:call-template name='hook'/></xsl:template>");
        XsltCompileOptions compileOptions = new XsltCompileOptions
        {
            BaseUri = "https://example.test/main.xsl",
            Packages =
            [
                new XsltPackageReference
                {
                    Name = new XmlName { LocalName = "urn:library" },
                    Version = "1",
                    LocationUri = "library.xsl"
                }
            ],
            ResolverPolicy = AllowedPolicy()
        };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = main, BaseUri = compileOptions.BaseUri }, compileOptions, resolver);
        XsltTransformResult result = new XsltTransformer().TransformXslt(executable, new XsltTransformOptions { InitialTemplate = new XmlName { LocalName = "entry" } });

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PrincipalValue!.AtomicValues!.Single().StringValue, Is.EqualTo("base!"));
        });
    }

    private static string Package(string name, string content)
    {
        return "<xsl:package xmlns:xsl='" + Xsl + "' name='" + name + "' package-version='1' version='3.0'>" + content + "</xsl:package>";
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        return new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https"],
            MaximumDepth = 16,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 1048576
        };
    }

    private sealed class PackageResolver : IXmlResourceResolver
    {
        internal Dictionary<string, string> Resources;

        internal PackageResolver()
        {
            Resources = new Dictionary<string, string>(System.StringComparer.Ordinal);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string requested = systemIdentifier == null ? "" : systemIdentifier;
            string key = baseUri == null ? requested : new System.Uri(new System.Uri(baseUri), requested).AbsoluteUri;
            if (!Resources.ContainsKey(key))
            {
                return new XmlResolvedResource { Kind = kind, Access = XmlResourceAccess.Denied, AbsoluteUri = key };
            }

            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = key,
                Input = new XmlInput { Text = Resources[key], BaseUri = key }
            };
        }
    }
}
