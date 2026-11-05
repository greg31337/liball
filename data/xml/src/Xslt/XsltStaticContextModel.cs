namespace xml;

internal enum XsltDefaultValidation
{
    Strip = 0,
    Preserve = 1
}

internal class XsltStaticContext
{
    internal XmlNode Node;
    internal XsltStylesheetModule Module;
    internal XmlNamespaceBinding[] Namespaces;
    internal string XPathDefaultNamespace;
    internal string DefaultCollation;
    internal string DefaultMode;
    internal XsltDefaultValidation Validation;
    internal string[] ExcludedNamespaceUris;
    internal string[] ExtensionNamespaceUris;
    internal bool ExpandText;
    internal string EffectiveVersion;
    internal XsltCompatibilityBehavior CompatibilityBehavior;
    internal XPathCompatibilityMode XPathCompatibilityMode;
    internal string? BaseUri;
    internal XmlName[] DecimalFormats;
    internal string[] ImportedSchemaNamespaces;
    internal XPathCollationBinding[] CollationBindings;
    internal XsltExtensionBinding[] Extensions;
    internal XmlExtensionPolicy ExtensionPolicy;

    internal XsltStaticContext(XmlNode node, XsltStylesheetModule module)
    {
        Node = node;
        Module = module;
        Namespaces = [];
        XPathDefaultNamespace = "";
        DefaultCollation = XPathNamespaces.CodepointCollation;
        DefaultMode = "#unnamed";
        Validation = XsltDefaultValidation.Strip;
        ExcludedNamespaceUris = [XsltNamespaces.Xslt];
        ExtensionNamespaceUris = [];
        ExpandText = false;
        EffectiveVersion = module.Version;
        CompatibilityBehavior = XsltCompatibilityBehavior.Standard;
        XPathCompatibilityMode = XPathCompatibilityMode.Strict;
        BaseUri = module.BaseUri;
        DecimalFormats = [];
        ImportedSchemaNamespaces = [];
        CollationBindings = [];
        Extensions = [];
        ExtensionPolicy = new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(null);
    }
}

internal class XsltNamespaceAlias
{
    internal XmlNode Declaration;
    internal string LiteralNamespaceUri;
    internal string TargetNamespaceUri;
    internal string ResultPrefix;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltNamespaceAlias(XmlNode declaration, string literalNamespaceUri, string targetNamespaceUri, string resultPrefix, int importPrecedence, int declarationOrder)
    {
        Declaration = declaration;
        LiteralNamespaceUri = literalNamespaceUri;
        TargetNamespaceUri = targetNamespaceUri;
        ResultPrefix = resultPrefix;
        ImportPrecedence = importPrecedence;
        DeclarationOrder = declarationOrder;
    }
}
