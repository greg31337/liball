using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltTemplateCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltTemplateCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltTemplates(XsltStylesheetPlan stylesheet)
    {
        List<XsltTemplatePlan> templates = new List<XsltTemplatePlan>();
        int moduleIndex = 0;
        while (moduleIndex < stylesheet.Modules.Length)
        {
            XsltStylesheetModule module = stylesheet.Modules[moduleIndex];
            if (module.Kind == XsltStylesheetModuleKind.Simplified)
            {
                templates.Add(CreateXsltSimplifiedTemplate(module, stylesheet));
            }
            else
            {
                CollectXsltTemplateDeclarations(module.Root, module, stylesheet, templates);
            }

            moduleIndex = moduleIndex + 1;
        }

        stylesheet.Templates = templates.ToArray();
    }

    internal void CollectXsltTemplateDeclarations(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, List<XsltTemplatePlan> templates)
    {
        if (node.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(node) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(node) == "template")
        {
            if (!IsXsltTemplateDeclarationPosition(node, module))
            {
                throw CreateXsltTemplateError("An xsl:template element must be a top-level or overriding declaration.", XsltErrorCodes.XTSE0010, node, module);
            }

            templates.Add(CreateXsltTemplate(node, module, stylesheet));
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                CollectXsltTemplateDeclarations(children[index], module, stylesheet, templates);
            }

            index = index + 1;
        }
    }

    internal bool IsXsltTemplateDeclarationPosition(XmlNode node, XsltStylesheetModule module)
    {
        if (object.ReferenceEquals(node.Parent, module.Root))
        {
            return true;
        }

        XmlNode? parent = node.Parent;
        if (parent == null || Loader.XsltNodeNamespace(parent) != XsltNamespaces.Xslt || Loader.XsltNodeLocalName(parent) != "override")
        {
            return false;
        }

        XmlNode? usePackage = parent.Parent;
        return usePackage != null && Loader.XsltNodeNamespace(usePackage) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(usePackage) == "use-package" &&
            object.ReferenceEquals(usePackage.Parent, module.Root);
    }

    internal XsltTemplatePlan CreateXsltTemplate(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet)
    {
        XsltStaticContext? context = FindXsltTemplateStaticContext(stylesheet, node);
        if (context == null)
        {
            throw CreateXsltTemplateError("A template declaration has no static context.", XsltErrorCodes.XTSE0010, node, module);
        }

        XsltTemplatePlan result = new XsltTemplatePlan(node, module, context);
        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        string? match = Loader.FindXsltAttribute(node, "match", "");
        string? mode = Loader.FindXsltAttribute(node, "mode", "");
        string? priority = Loader.FindXsltAttribute(node, "priority", "");
        string? visibility = Loader.FindXsltAttribute(node, "visibility", "");
        result.IsNamed = lexicalName != null;
        result.IsTemplateRule = match != null;
        if (!result.IsNamed && !result.IsTemplateRule)
        {
            throw CreateXsltTemplateError("An xsl:template declaration requires a name or match attribute.", XsltErrorCodes.XTSE0500, node, module);
        }

        if (!result.IsTemplateRule && (mode != null || priority != null))
        {
            throw CreateXsltTemplateError("A named template without a match attribute must not specify mode or priority.", XsltErrorCodes.XTSE0500, node, module);
        }

        if (!result.IsNamed && visibility != null)
        {
            throw CreateXsltTemplateError("A template rule without a name must not specify visibility.", XsltErrorCodes.XTSE0500, node, module);
        }

        if (result.IsNamed)
        {
            XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName == null ? "" : lexicalName, node, module);
            result.ResolvedName = name;
            result.Name = CreateXsltTemplateExpandedName(name);
        }

        if (result.IsTemplateRule)
        {
            string source = match == null ? "" : match;
            if (source.Trim().Length == 0)
            {
                throw CreateXsltTemplateError("A template match pattern must not be empty.", XsltErrorCodes.XTSE0340, node, module);
            }

            result.Match = source;
            result.MatchPlan = new XsltMatchPatternPlan(source, FindXsltTemplateAttributeRange(node, "match"));
            result.Modes = ParseXsltTemplateModes(mode, context, node, module);
            ReadXsltTemplatePriority(result, priority);
        }

        result.ImportPrecedence = new XsltVariableCompiler(Loader).ReadXsltVariableImportPrecedence(stylesheet, node);
        result.DeclarationOrder = new XsltVariableCompiler(Loader).ReadXsltVariableDeclarationOrder(stylesheet, node);
        ReadXsltTemplateReturnType(result);
        ReadXsltTemplateHeaderAndBody(result, stylesheet);
        AttachXsltTemplateComponent(result, stylesheet);
        ValidateXsltAbstractTemplate(result);
        return result;
    }

    internal XsltTemplatePlan CreateXsltSimplifiedTemplate(XsltStylesheetModule module, XsltStylesheetPlan stylesheet)
    {
        XsltStaticContext? context = FindXsltTemplateStaticContext(stylesheet, module.Root);
        if (context == null)
        {
            throw CreateXsltTemplateError("A simplified stylesheet has no static context.", XsltErrorCodes.XTSE0010, module.Root, module);
        }

        XsltTemplatePlan result = new XsltTemplatePlan(module.Root, module, context);
        result.IsTemplateRule = true;
        result.IsSynthetic = true;
        result.Match = "/";
        result.MatchPlan = new XsltMatchPatternPlan("/", module.Root.Range);
        result.Modes = [new XsltTemplateMode("#default", context.DefaultMode, false)];
        result.ImportPrecedence = new XsltVariableCompiler(Loader).ReadXsltVariableImportPrecedence(stylesheet, module.Root);
        result.DeclarationOrder = new XsltVariableCompiler(Loader).ReadXsltVariableDeclarationOrder(stylesheet, module.Root);
        XsltSequenceItemPlan item = CompileXsltSequenceElement(module.Root, stylesheet);
        result.Body = new XsltSequenceConstructorPlan(module.Root, [item]);
        return result;
    }

    internal string CreateXsltTemplateExpandedName(XmlName name)
    {
        return "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
    }

    internal XsltTemplateMode[] ParseXsltTemplateModes(string? source, XsltStaticContext context, XmlNode node, XsltStylesheetModule module)
    {
        if (source == null)
        {
            if (IsXsltTemplateInsideOverride(node) && context.DefaultMode == "#unnamed")
            {
                throw CreateXsltTemplateError("An overriding template rule cannot use the unnamed default mode.", XsltErrorCodes.XTSE3440, node, module);
            }

            return [new XsltTemplateMode("#default", context.DefaultMode, false)];
        }

        string[] tokens = source.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw CreateXsltTemplateError("A template mode list must not be empty.", XsltErrorCodes.XTSE0550, node, module);
        }

        List<XsltTemplateMode> result = new List<XsltTemplateMode>();
        int index = 0;
        while (index < tokens.Length)
        {
            string token = tokens[index];
            string name;
            bool all = false;
            if (token == "#default")
            {
                name = context.DefaultMode;
            }
            else if (token == "#unnamed")
            {
                name = "#unnamed";
            }
            else if (token == "#all")
            {
                name = "#all";
                all = true;
            }
            else if (token.Length > 0 && token[0] == '#')
            {
                throw CreateXsltTemplateError("A template mode list contains an invalid special token.", XsltErrorCodes.XTSE0550, node, module);
            }
            else
            {
                name = CreateXsltTemplateExpandedName(Loader.StaticProcessor.ResolveXsltStaticVariableName(token, node, module));
            }

            if (ContainsXsltTemplateMode(result, name) || all && tokens.Length != 1)
            {
                throw CreateXsltTemplateError("A template mode list contains a duplicate mode or combines #all with another mode.", XsltErrorCodes.XTSE0550, node, module);
            }

            if (IsXsltTemplateInsideOverride(node) && (all || name == "#unnamed"))
            {
                throw CreateXsltTemplateError("An overriding template rule cannot apply to #all or the unnamed mode.", XsltErrorCodes.XTSE3440, node, module);
            }

            result.Add(new XsltTemplateMode(token, name, all));
            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool ContainsXsltTemplateMode(List<XsltTemplateMode> modes, string name)
    {
        int index = 0;
        while (index < modes.Count)
        {
            if (modes[index].Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXsltTemplateInsideOverride(XmlNode node)
    {
        XmlNode? parent = node.Parent;
        return parent != null && Loader.XsltNodeNamespace(parent) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(parent) == "override";
    }

    internal void ReadXsltTemplatePriority(XsltTemplatePlan template, string? source)
    {
        if (source == null)
        {
            return;
        }

        XdmNumericParseResult parsed = new XdmNumericDatatype().TryParseXdmDecimal(source);
        if (!parsed.Success || parsed.Value == null)
        {
            throw CreateXsltTemplateError("A template priority must be a valid xs:decimal value.", XsltErrorCodes.XTSE0530, template.Declaration, template.Module);
        }

        template.HasExplicitPriority = true;
        template.PrioritySource = source;
        template.Priority = parsed.Value.CanonicalValue;
    }

    internal void ReadXsltTemplateReturnType(XsltTemplatePlan template)
    {
        string? source = Loader.FindXsltAttribute(template.Declaration, "as", "");
        if (source == null)
        {
            return;
        }

        template.ReturnTypeSource = source;
        XPathStaticContext context = new XsltVariableCompiler(Loader).CreateXsltVariableXPathContext(template.StaticContext, []);
        Task<XPathTypeSyntax> pending = Task.Run(() => new XsltVariableCompiler(Loader).ParseXsltVariableSequenceType(source, context, template.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltTemplateTaskError("A template has an invalid return sequence type.", template.Declaration, template.Module, pending.Exception);
        }

        template.ReturnType = pending.Result;
    }

    internal void ReadXsltTemplateHeaderAndBody(XsltTemplatePlan template, XsltStylesheetPlan stylesheet)
    {
        List<XsltVariablePlan> parameters = new List<XsltVariablePlan>();
        List<XmlNode> body = new List<XmlNode>();
        XmlNode[] children = template.Declaration.Children == null ? [] : template.Declaration.Children;
        bool sawParameter = false;
        bool sawBody = false;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            bool contextItem = child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "context-item";
            XsltVariablePlan? parameter = FindXsltTemplateParameter(stylesheet, child);
            if (contextItem)
            {
                if (template.ContextItem != null || sawParameter || sawBody)
                {
                    throw CreateXsltTemplateError("xsl:context-item must be the first declaration in a template.", XsltErrorCodes.XTSE0010, child, template.Module);
                }

                template.ContextItem = ParseXsltTemplateContextItem(child, template);
            }
            else if (parameter != null)
            {
                if (sawBody)
                {
                    throw CreateXsltTemplateError("Template parameters must precede the template sequence constructor.", XsltErrorCodes.XTSE0010, child, template.Module);
                }

                sawParameter = true;
                parameters.Add(parameter);
            }
            else if (!whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                sawBody = true;
                body.Add(child);
            }

            index = index + 1;
        }

        template.Parameters = parameters.ToArray();
        template.Body = CompileXsltSequenceConstructor(template.Declaration, body.ToArray(), stylesheet, false);
    }

    internal XsltVariablePlan? FindXsltTemplateParameter(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        XsltVariablePlan? variable = new XsltVariableCompiler(Loader).FindXsltVariableByNode(stylesheet, node);
        return variable != null && variable.Scope == XsltVariableScope.TemplateParameter ? variable : null;
    }

    internal XsltContextItemPlan ParseXsltTemplateContextItem(XmlNode node, XsltTemplatePlan template)
    {
        if (HasXsltTemplateSignificantChildren(node))
        {
            throw CreateXsltTemplateError("An xsl:context-item declaration must be empty.", XsltErrorCodes.XTSE0010, node, template.Module);
        }

        XsltContextItemPlan result = new XsltContextItemPlan(node);
        string? use = Loader.FindXsltAttribute(node, "use", "");
        string normalized = use == null ? "optional" : use.Trim();
        if (normalized == "required")
        {
            result.Use = XsltContextItemUse.Required;
        }
        else if (normalized == "optional")
        {
            result.Use = XsltContextItemUse.Optional;
        }
        else if (normalized == "absent")
        {
            result.Use = XsltContextItemUse.Absent;
        }
        else
        {
            throw CreateXsltTemplateError("The use attribute of xsl:context-item must be required, optional, or absent.", XsltErrorCodes.XTSE0020, node, template.Module);
        }

        string? type = Loader.FindXsltAttribute(node, "as", "");
        if (result.Use == XsltContextItemUse.Absent && type != null)
        {
            throw CreateXsltTemplateError("A context item declared absent must not specify a type.", XsltErrorCodes.XTSE3089, node, template.Module);
        }

        if (!template.IsNamed && result.Use != XsltContextItemUse.Required)
        {
            throw CreateXsltTemplateError("A match-only template can declare only a required context item.", XsltErrorCodes.XTSE0020, node, template.Module);
        }

        if (type != null)
        {
            result.TypeSource = type;
            XPathStaticContext context = new XsltVariableCompiler(Loader).CreateXsltVariableXPathContext(template.StaticContext, []);
            Task<XPathTypeSyntax> pending = Task.Run(() => ParseXsltContextItemType(type, context, template.Module.BaseUri));
            ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
            if (pending.Status != TaskStatus.RanToCompletion)
            {
                throw CreateXsltTemplateTaskError("A context item declaration has an invalid item type.", node, template.Module, pending.Exception);
            }

            result.Type = pending.Result;
        }

        return result;
    }

    internal XPathTypeSyntax ParseXsltContextItemType(string source, XPathStaticContext context, string? sourceUri)
    {
        XPathParser parser = new XPathParser(source, sourceUri);
        XPathTypeSyntax result = parser.ParseXPathItemType();
        XPathToken remaining = parser.CurrentXPathToken();
        if (remaining.Kind != XPathTokenKind.EndOfInput)
        {
            throw parser.CreateXPathParserError("The context item type contains an unexpected terminal.", remaining);
        }

        XPathStaticAnalyzer analyzer = new XPathStaticAnalyzer(context);
        analyzer.Environment.ResolveXPathType(result);
        analyzer.ValidateXPathTypeSyntax(result, XPathErrorCodes.XPST0051, result.Range);
        return result;
    }

    internal bool HasXsltTemplateSignificantChildren(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element || (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AttachXsltTemplateComponent(XsltTemplatePlan template, XsltStylesheetPlan stylesheet)
    {
        XsltPackagePlan? package = stylesheet.Package;
        if (!template.IsNamed || package == null)
        {
            return;
        }

        int index = 0;
        while (index < package.Components.Length)
        {
            XsltComponent component = package.Components[index];
            if (component.Kind == XsltComponentKind.Template && object.ReferenceEquals(component.Declaration, template.Declaration))
            {
                template.Component = component;
                template.Visibility = component.Visibility;
                template.DeclaredVisibility = component.DeclaredVisibility;
                template.HasDeclaredVisibility = component.HasDeclaredVisibility;
                template.IsAbstract = component.DeclaredVisibility == XsltComponentVisibility.Abstract;
                return;
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltAbstractTemplate(XsltTemplatePlan template)
    {
        if (!template.IsAbstract)
        {
            return;
        }

        if (template.IsTemplateRule || template.Body.Items.Length != 0)
        {
            throw CreateXsltTemplateError("An abstract template must be named, have no match pattern, and have an empty body.", XsltErrorCodes.XTSE0020, template.Declaration, template.Module);
        }
    }

    internal XsltSequenceConstructorPlan CompileXsltSequenceConstructor(XmlNode owner, XmlNode[] nodes, XsltStylesheetPlan stylesheet, bool preserveWhitespace)
    {
        List<XsltSequenceItemPlan> items = new List<XsltSequenceItemPlan>();
        int index = 0;
        while (index < nodes.Length)
        {
            XmlNode node = nodes[index];
            if (node.Kind == XmlNodeKind.Element)
            {
                items.Add(CompileXsltSequenceElement(node, stylesheet));
            }
            else if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
            {
                string value = node.Value == null ? "" : node.Value;
                if (preserveWhitespace || !new XsltStylesheetAssembler(Loader).IsXsltWhitespace(value))
                {
                    XsltSequenceItemPlan text = new XsltSequenceItemPlan(node, XsltSequenceItemKind.Text, "", "#text");
                    text.TextTemplate = FindXsltTextValueTemplate(stylesheet, node);
                    items.Add(text);
                }
            }

            index = index + 1;
        }

        return new XsltSequenceConstructorPlan(owner, items.ToArray());
    }

    internal XsltSequenceItemPlan CompileXsltSequenceElement(XmlNode node, XsltStylesheetPlan stylesheet)
    {
        string namespaceUri = Loader.XsltNodeNamespace(node);
        string localName = Loader.XsltNodeLocalName(node);
        XsltVariablePlan? variable = new XsltVariableCompiler(Loader).FindXsltVariableByNode(stylesheet, node);
        XsltStaticContext? staticContext = FindXsltTemplateStaticContext(stylesheet, node);
        XsltExtensionBinding? extension = FindXsltInstructionExtension(namespaceUri, localName, staticContext);
        bool extensionElement = staticContext != null && ContainsXsltExtensionNamespace(staticContext.ExtensionNamespaceUris, namespaceUri);
        XsltSequenceItemKind kind = variable != null ? XsltSequenceItemKind.Variable : namespaceUri == XsltNamespaces.Xslt ? XsltSequenceItemKind.Instruction : extensionElement ? XsltSequenceItemKind.Extension : XsltSequenceItemKind.LiteralResultElement;
        XsltSequenceItemPlan result = new XsltSequenceItemPlan(node, kind, namespaceUri, localName);
        result.Variable = variable;
        result.Instruction = FindXsltInstructionPlan(stylesheet, node);
        result.Extension = extension;
        result.ExtensionPolicy = staticContext == null ? null : staticContext.ExtensionPolicy;
        result.AttributeTemplates = FindXsltElementValueTemplates(stylesheet, node);
        XmlNode[] children = node.Children == null ? [] : node.Children;
        result.Content = CompileXsltSequenceConstructor(node, children, stylesheet, namespaceUri == XsltNamespaces.Xslt && localName == "text");
        return result;
    }

    internal XsltExtensionBinding? FindXsltInstructionExtension(string namespaceUri, string localName, XsltStaticContext? context)
    {
        if (context == null || !ContainsXsltExtensionNamespace(context.ExtensionNamespaceUris, namespaceUri))
        {
            return null;
        }

        XsltExtensionBinding[] bindings = Loader.Options.Extensions == null ? [] : Loader.Options.Extensions;
        int index = 0;
        while (index < bindings.Length)
        {
            XmlName? name = bindings[index].Name;
            if (bindings[index].IsInstruction && bindings[index].InstructionImplementation != null && name != null && name.LocalName == localName && (name.NamespaceUri == null ? "" : name.NamespaceUri) == namespaceUri)
            {
                return bindings[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool ContainsXsltExtensionNamespace(string[] namespaces, string namespaceUri)
    {
        int index = 0;
        while (index < namespaces.Length)
        {
            if (namespaces[index] == namespaceUri)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltInstructionPlan? FindXsltInstructionPlan(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        int index = 0;
        while (index < stylesheet.Instructions.Length)
        {
            if (object.ReferenceEquals(stylesheet.Instructions[index].Instruction, node))
            {
                return stylesheet.Instructions[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltValueTemplate? FindXsltTextValueTemplate(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        int index = 0;
        while (index < stylesheet.ValueTemplates.Length)
        {
            XsltValueTemplate template = stylesheet.ValueTemplates[index];
            if (template.Kind == XsltValueTemplateKind.Text && object.ReferenceEquals(template.Node, node))
            {
                return template;
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltValueTemplate[] FindXsltElementValueTemplates(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        List<XsltValueTemplate> result = new List<XsltValueTemplate>();
        int index = 0;
        while (index < stylesheet.ValueTemplates.Length)
        {
            XsltValueTemplate template = stylesheet.ValueTemplates[index];
            if (template.Kind == XsltValueTemplateKind.Attribute && object.ReferenceEquals(template.Node.Parent, node))
            {
                result.Add(template);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal SourceRange? FindXsltTemplateAttributeRange(XmlNode node, string localName)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            if (Loader.XsltNodeNamespace(attributes[index]).Length == 0 && Loader.XsltNodeLocalName(attributes[index]) == localName)
            {
                return attributes[index].Range;
            }

            index = index + 1;
        }

        return node.Range;
    }

    internal XsltStaticContext? FindXsltTemplateStaticContext(XsltStylesheetPlan stylesheet, XmlNode node)
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

    internal XsltStaticException CreateXsltTemplateTaskError(string message, XmlNode node, XsltStylesheetModule module, AggregateException? failure)
    {
        return CreateXsltTemplateError(message, Loader.StaticProcessor.ReadXsltTaskErrorCode(failure), node, module);
    }

    internal XsltStaticException CreateXsltTemplateError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
