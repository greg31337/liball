using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal enum XmlSchemaIdentityXPathAxis
{
    Self = 0,
    Child = 1,
    Attribute = 2
}

internal class XmlSchemaIdentityNameTest
{
    internal bool AnyNamespace;
    internal string NamespaceUri = "";
    internal string? LocalName;
}

internal class XmlSchemaIdentityXPathStep
{
    internal XmlSchemaIdentityXPathAxis Axis;
    internal XmlSchemaIdentityNameTest? NameTest;
}

internal class XmlSchemaIdentityXPathPath
{
    internal bool DescendantStart;
    internal XmlSchemaIdentityXPathStep[] Steps = [];
}

internal class XmlSchemaIdentityXPathExpression
{
    internal XmlSchemaIdentityXPathPath[] Paths = [];
}

internal class XmlSchemaIdentityXPathCompiler
{
    internal XmlSchemaDeclarationCompiler DeclarationCompiler;

    internal XmlSchemaIdentityXPathCompiler(XmlSchemaDeclarationCompiler declarationCompiler)
    {
        Guard.NotNull(declarationCompiler, "declarationCompiler");
        DeclarationCompiler = declarationCompiler;
    }

    internal XmlSchemaIdentityXPathExpression CompileXmlSchemaIdentityXPath(string expression, XmlNode host, XmlSchemaModule module, string? inheritedDefaultNamespace, bool field)
    {
        string compact = RemoveXmlSchemaIdentityXPathWhitespace(expression, host);
        if (compact.Length == 0 || compact[0] == '|' || compact[compact.Length - 1] == '|')
        {
            throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "The identity-constraint XPath expression is invalid.");
        }

        string defaultNamespace = ReadXmlSchemaIdentityXPathDefaultNamespace(host, module, inheritedDefaultNamespace);
        string[] lexicalPaths = compact.Split('|');
        List<XmlSchemaIdentityXPathPath> paths = new List<XmlSchemaIdentityXPathPath>();
        int index = 0;
        while (index < lexicalPaths.Length)
        {
            if (lexicalPaths[index].Length == 0)
            {
                throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "The identity-constraint XPath expression contains an empty path.");
            }

            paths.Add(CompileXmlSchemaIdentityXPathPath(lexicalPaths[index], host, defaultNamespace, field));
            index = index + 1;
        }

        XmlSchemaIdentityXPathExpression result = new XmlSchemaIdentityXPathExpression();
        result.Paths = paths.ToArray();
        return result;
    }

    internal XmlSchemaIdentityXPathPath CompileXmlSchemaIdentityXPathPath(string lexicalPath, XmlNode host, string defaultNamespace, bool field)
    {
        XmlSchemaIdentityXPathPath result = new XmlSchemaIdentityXPathPath();
        string path = lexicalPath;
        if (path.StartsWith(".//", StringComparison.Ordinal))
        {
            result.DescendantStart = true;
            path = path.Substring(3);
        }

        if (path.Length == 0 || path.StartsWith("/", StringComparison.Ordinal) || path.EndsWith("/", StringComparison.Ordinal) || path.Contains("//", StringComparison.Ordinal))
        {
            throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "The identity-constraint XPath path is outside the supported selector or field subset.");
        }

        string[] lexicalSteps = path.Split('/');
        List<XmlSchemaIdentityXPathStep> steps = new List<XmlSchemaIdentityXPathStep>();
        int index = 0;
        while (index < lexicalSteps.Length)
        {
            bool last = index == lexicalSteps.Length - 1;
            steps.Add(CompileXmlSchemaIdentityXPathStep(lexicalSteps[index], host, defaultNamespace, field, last));
            index = index + 1;
        }

        result.Steps = steps.ToArray();
        return result;
    }

    internal XmlSchemaIdentityXPathStep CompileXmlSchemaIdentityXPathStep(string token, XmlNode host, string defaultNamespace, bool field, bool last)
    {
        XmlSchemaIdentityXPathStep result = new XmlSchemaIdentityXPathStep();
        if (token == ".")
        {
            result.Axis = XmlSchemaIdentityXPathAxis.Self;
            return result;
        }

        bool attribute = token.StartsWith("@", StringComparison.Ordinal);
        string nameToken = attribute ? token.Substring(1) : token;
        if (attribute && (!field || !last) || nameToken.Length == 0)
        {
            throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "Only the final step of an identity-constraint field may select an attribute.");
        }

        result.Axis = attribute ? XmlSchemaIdentityXPathAxis.Attribute : XmlSchemaIdentityXPathAxis.Child;
        result.NameTest = CompileXmlSchemaIdentityNameTest(nameToken, host, attribute ? "" : defaultNamespace);
        return result;
    }

    internal XmlSchemaIdentityNameTest CompileXmlSchemaIdentityNameTest(string token, XmlNode host, string defaultNamespace)
    {
        XmlSchemaIdentityNameTest result = new XmlSchemaIdentityNameTest();
        if (token == "*")
        {
            result.AnyNamespace = true;
            result.LocalName = null;
            return result;
        }

        int separator = token.IndexOf(':');
        if (separator >= 0 && token.IndexOf(':', separator + 1) >= 0)
        {
            throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "An identity-constraint name test contains an invalid QName.");
        }

        string prefix = separator < 0 ? "" : token.Substring(0, separator);
        string localName = separator < 0 ? token : token.Substring(separator + 1);
        XdmStringDatatype strings = new XdmStringDatatype();
        bool prefixValid = prefix.Length == 0 || strings.IsValidXdmStringLexical(prefix, XdmAtomicType.NcName);
        bool localValid = localName == "*" || strings.IsValidXdmStringLexical(localName, XdmAtomicType.NcName);
        if (!prefixValid || !localValid || separator == 0 || localName.Length == 0)
        {
            throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "An identity-constraint name test contains an invalid QName.");
        }

        result.AnyNamespace = false;
        result.NamespaceUri = prefix.Length == 0 ? defaultNamespace : ResolveXmlSchemaIdentityXPathPrefix(host, prefix);
        result.LocalName = localName == "*" ? null : localName;
        return result;
    }

    internal string ResolveXmlSchemaIdentityXPathPrefix(XmlNode host, string prefix)
    {
        if (prefix == "xml")
        {
            return XmlNamespaces.Xml;
        }

        XmlNamespaceBinding[] bindings = new TreeNavigation().SelectXmlNamespacesOperation(host);
        int index = bindings.Length - 1;
        while (index >= 0)
        {
            string? optionalCandidate = bindings[index].Prefix;
            string candidate = optionalCandidate == null ? "" : optionalCandidate;
            if (candidate == prefix && !bindings[index].IsUndeclaration)
            {
                string? optionalNamespace = bindings[index].NamespaceUri;
                return optionalNamespace == null ? "" : optionalNamespace;
            }

            index = index - 1;
        }

        throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "An identity-constraint XPath prefix is not declared.");
    }

    internal string ReadXmlSchemaIdentityXPathDefaultNamespace(XmlNode host, XmlSchemaModule module, string? inheritedDefaultNamespace)
    {
        string? value = DeclarationCompiler.ComplexCompiler.FindXmlSchemaAttribute(host, "xpathDefaultNamespace", "");
        if (value == null)
        {
            value = inheritedDefaultNamespace;
        }

        string normalized = value == null ? "##local" : new XdmStringDatatype().CollapseXdmStringWhitespace(value);
        if (normalized == "##local")
        {
            return "";
        }

        if (normalized == "##targetNamespace")
        {
            return module.EffectiveTargetNamespace == null ? "" : module.EffectiveTargetNamespace;
        }

        if (normalized == "##defaultNamespace")
        {
            XPathStaticContext context = DeclarationCompiler.SimpleCompiler.CreateXmlSchemaQNameContext(host);
            return context.DefaultElementNamespace == null ? "" : context.DefaultElementNamespace;
        }

        if (normalized.Length == 0 || normalized.StartsWith("##", StringComparison.Ordinal))
        {
            throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "The identity-constraint XPath default namespace is invalid.");
        }

        return normalized;
    }

    internal string RemoveXmlSchemaIdentityXPathWhitespace(string value, XmlNode host)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool whitespace = character == ' ' || character == '\t' || character == '\r' || character == '\n';
            if (!whitespace)
            {
                result.Append(character);
                index = index + 1;
                continue;
            }

            int nextIndex = index + 1;
            while (nextIndex < value.Length && (value[nextIndex] == ' ' || value[nextIndex] == '\t' || value[nextIndex] == '\r' || value[nextIndex] == '\n'))
            {
                nextIndex = nextIndex + 1;
            }

            if (result.Length != 0 && nextIndex < value.Length)
            {
                char previous = result[result.Length - 1];
                char next = value[nextIndex];
                bool previousSeparator = previous == '/' || previous == '|' || previous == '@';
                bool nextSeparator = next == '/' || next == '|' || next == '@';
                if (previous == '/' && next == '/' || !previousSeparator && !nextSeparator)
                {
                    throw DeclarationCompiler.CreateXmlSchemaDeclarationError(host, "Whitespace cannot split a token in an identity-constraint XPath expression.");
                }
            }

            index = nextIndex;
        }

        return result.ToString();
    }
}
