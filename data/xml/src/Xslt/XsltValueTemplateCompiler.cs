using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltValueTemplateCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltValueTemplateCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltValueTemplates(XsltStylesheetPlan stylesheet)
    {
        List<XsltValueTemplate> templates = new List<XsltValueTemplate>();
        int moduleIndex = 0;
        while (moduleIndex < stylesheet.Modules.Length)
        {
            XsltStylesheetModule module = stylesheet.Modules[moduleIndex];
            CompileXsltValueTemplateTree(module.Root, module, stylesheet, templates);
            moduleIndex = moduleIndex + 1;
        }

        stylesheet.ValueTemplates = templates.ToArray();
    }

    internal void CompileXsltValueTemplateTree(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, List<XsltValueTemplate> templates)
    {
        XsltStaticContext? context = FindXsltValueTemplateStaticContext(stylesheet, node);
        if (context == null)
        {
            throw Loader.CreateXsltStaticError("A stylesheet element has no static context.", XsltErrorCodes.XTSE0010, node, module.BaseUri);
        }

        CompileXsltAttributeValueTemplates(node, module, stylesheet, context, templates);
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                CompileXsltValueTemplateTree(child, module, stylesheet, templates);
            }
            else if ((child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && context.ExpandText && IsXsltTextValueTemplateParent(node))
            {
                string source = child.Value == null ? "" : child.Value;
                XsltValueTemplate template = new XsltValueTemplateParser(Loader).ParseXsltValueTemplate(source, child, XsltValueTemplateKind.Text, module, module.BaseUri);
                template.StaticContext = context;
                CompileXsltValueTemplateExpressions(template, stylesheet, module);
                templates.Add(template);
            }

            index = index + 1;
        }
    }

    internal void CompileXsltAttributeValueTemplates(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, XsltStaticContext context, List<XsltValueTemplate> templates)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        bool xsltElement = Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt;
        string elementName = Loader.XsltNodeLocalName(node);
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            string attributeNamespace = Loader.XsltNodeNamespace(attribute);
            string attributeName = Loader.XsltNodeLocalName(attribute);
            bool designated = xsltElement ? IsXsltAttributeValueTemplate(elementName, attributeName, attributeNamespace) : attributeNamespace != XsltNamespaces.Xslt;
            if (designated && !Loader.StaticProcessor.IsXsltShadowedAttribute(attribute))
            {
                string source = attribute.Value == null ? "" : attribute.Value;
                XsltValueTemplate template = new XsltValueTemplateParser(Loader).ParseXsltValueTemplate(source, attribute, XsltValueTemplateKind.Attribute, module, module.BaseUri);
                template.StaticContext = context;
                CompileXsltValueTemplateExpressions(template, stylesheet, module);
                templates.Add(template);
            }

            index = index + 1;
        }
    }

    internal bool IsXsltAttributeValueTemplate(string elementName, string attributeName, string attributeNamespace)
    {
        if (attributeNamespace.Length != 0)
        {
            return false;
        }

        if ((elementName == "element" || elementName == "attribute") && (attributeName == "name" || attributeName == "namespace"))
        {
            return true;
        }

        if ((elementName == "namespace" || elementName == "processing-instruction") && attributeName == "name")
        {
            return true;
        }

        if ((elementName == "analyze-string") && (attributeName == "regex" || attributeName == "flags"))
        {
            return true;
        }

        if (elementName == "evaluate" && (attributeName == "base-uri" || attributeName == "schema-aware"))
        {
            return true;
        }

        if (elementName == "for-each-group" && attributeName == "collation")
        {
            return true;
        }

        if ((elementName == "message" || elementName == "assert") && (attributeName == "terminate" || attributeName == "error-code"))
        {
            return true;
        }

        if (elementName == "number" && (attributeName == "format" || attributeName == "lang" || attributeName == "letter-value" || attributeName == "ordinal" || attributeName == "grouping-separator" || attributeName == "grouping-size"))
        {
            return true;
        }

        if ((elementName == "sort" || elementName == "merge-key") && (attributeName == "lang" || attributeName == "data-type" || attributeName == "order" || attributeName == "case-order" || attributeName == "collation" || elementName == "sort" && attributeName == "stable"))
        {
            return true;
        }

        if (elementName == "value-of" && attributeName == "separator")
        {
            return true;
        }

        if ((elementName == "source-document" || elementName == "stream") && attributeName == "href")
        {
            return true;
        }

        if (elementName == "result-document")
        {
            return IsXsltResultDocumentValueTemplate(attributeName);
        }

        return false;
    }

    internal bool IsXsltResultDocumentValueTemplate(string attributeName)
    {
        return attributeName == "href" || attributeName == "format" || attributeName == "method" || attributeName == "allow-duplicate-names" || attributeName == "build-tree" || attributeName == "byte-order-mark" ||
            attributeName == "cdata-section-elements" || attributeName == "doctype-public" || attributeName == "doctype-system" || attributeName == "encoding" ||
            attributeName == "escape-uri-attributes" || attributeName == "html-version" || attributeName == "include-content-type" || attributeName == "indent" ||
            attributeName == "item-separator" || attributeName == "json-node-output-method" || attributeName == "media-type" || attributeName == "normalization-form" || attributeName == "omit-xml-declaration" ||
            attributeName == "output-version" || attributeName == "parameter-document" || attributeName == "standalone" || attributeName == "suppress-indentation" || attributeName == "undeclare-prefixes";
    }

    internal bool IsXsltTextValueTemplateParent(XmlNode node)
    {
        if (Loader.XsltNodeNamespace(node) != XsltNamespaces.Xslt)
        {
            return true;
        }

        string localName = Loader.XsltNodeLocalName(node);
        return localName != "stylesheet" && localName != "transform" && localName != "package" && localName != "use-package" && localName != "import" && localName != "include";
    }

    internal void CompileXsltValueTemplateExpressions(XsltValueTemplate template, XsltStylesheetPlan stylesheet, XsltStylesheetModule module)
    {
        XsltStaticContext? context = template.StaticContext;
        if (context == null)
        {
            throw Loader.CreateXsltStaticError("A value template has no static context.", XsltErrorCodes.XTSE0010, template.Node, module.BaseUri);
        }

        XPathStaticContext xpathContext = CreateXsltValueTemplateXPathContext(context, stylesheet, template.Node);
        int index = 0;
        while (index < template.Parts.Length)
        {
            XsltValueTemplatePart part = template.Parts[index];
            if (part.Kind == XsltValueTemplatePartKind.Expression && part.Value.Length != 0)
            {
                Task<XPathPlan> pending = Task.Run(() => new XPathPlanCompiler(xpathContext).CompileXPathSource(part.Value, module.BaseUri));
                ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
                if (pending.Status != TaskStatus.RanToCompletion)
                {
                    throw CreateXsltValueTemplateCompilationError(part, template.Node, module, pending.Exception);
                }

                part.ExpressionPlan = pending.Result;
            }

            index = index + 1;
        }
    }

    internal XPathStaticContext CreateXsltValueTemplateXPathContext(XsltStaticContext context, XsltStylesheetPlan stylesheet, XmlNode node)
    {
        XsltVariableCompiler variables = new XsltVariableCompiler(Loader);
        List<XsltVariablePlan> visible = variables.CreateXsltVisibleVariablePlans(stylesheet, node);
        XPathStaticContext result = variables.CreateXsltVariableXPathContext(context, variables.CreateXsltVariableBindingsAtNode(visible, node));
        new XsltInstructionCompiler(Loader).AddXsltInstructionFunctionBindings(result);
        return result;
    }

    internal XsltStaticContext? FindXsltValueTemplateStaticContext(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        int index = 0;
        while (index < stylesheet.StaticContexts.Length)
        {
            if (object.ReferenceEquals(stylesheet.StaticContexts[index].Node, node))
            {
                return stylesheet.StaticContexts[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltStaticException CreateXsltValueTemplateCompilationError(XsltValueTemplatePart part, XmlNode node, XsltStylesheetModule module, AggregateException? failure)
    {
        string code = Loader.StaticProcessor.ReadXsltTaskErrorCode(failure);
        XsltStaticException error = Loader.CreateXsltStaticError("A value template XPath expression could not be compiled.", code, node, module.BaseUri);
        if (error.Diagnostic != null)
        {
            error.Diagnostic.Range = part.Range;
        }

        error.Location = part.Range.Start;
        return error;
    }
}
