using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltKeyCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltKeyCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltKeys(XsltStylesheetPlan stylesheet)
    {
        List<XsltKeyDeclarationPlan> declarations = new List<XsltKeyDeclarationPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "key")
            {
                declarations.Add(CompileXsltKeyDeclaration(declaration, stylesheet));
            }

            index = index + 1;
        }

        stylesheet.Keys = MergeXsltKeyDeclarations(declarations).ToArray();
    }

    internal XsltKeyDeclarationPlan CompileXsltKeyDeclaration(XsltStylesheetDeclaration declaration, XsltStylesheetPlan stylesheet)
    {
        XmlNode node = declaration.Node;
        XsltStaticContext? staticContext = declaration.StaticContext;
        if (staticContext == null)
        {
            staticContext = new XsltStaticContextCompiler(Loader).FindXsltStaticContext(stylesheet, node);
        }

        if (staticContext == null)
        {
            throw CreateXsltKeyError("An xsl:key declaration has no static context.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        if (lexicalName == null || lexicalName.Trim().Length == 0)
        {
            throw CreateXsltKeyError("An xsl:key declaration requires a name.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        string? match = Loader.FindXsltAttribute(node, "match", "");
        if (match == null || match.Trim().Length == 0)
        {
            throw CreateXsltKeyError("An xsl:key declaration requires a non-empty match pattern.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        XmlName name = Loader.StaticProcessor.ResolveXsltStaticVariableName(lexicalName, node, declaration.Module);
        string expandedName = CreateXsltKeyExpandedName(name);
        XsltKeyDeclarationPlan result = new XsltKeyDeclarationPlan(node, declaration.Module, staticContext, name, expandedName, match);
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        result.Composite = ReadXsltKeyComposite(node, declaration.Module);
        result.Collation = ReadXsltKeyCollation(node, staticContext, declaration.Module);
        CompileXsltKeyPattern(result, stylesheet);
        CompileXsltKeyValue(result, stylesheet);
        return result;
    }

    internal string CreateXsltKeyExpandedName(XmlName name)
    {
        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        string localName = name.LocalName == null ? "" : name.LocalName;
        return "Q{" + namespaceUri + "}" + localName;
    }

    internal bool ReadXsltKeyComposite(XmlNode node, XsltStylesheetModule module)
    {
        string? value = Loader.FindXsltAttribute(node, "composite", "");
        return value != null && Loader.StaticProcessor.ParseXsltStaticBoolean(value, "composite", node, module);
    }

    internal string ReadXsltKeyCollation(XmlNode node, XsltStaticContext staticContext, XsltStylesheetModule module)
    {
        string? value = Loader.FindXsltAttribute(node, "collation", "");
        if (value == null)
        {
            return staticContext.DefaultCollation;
        }

        string? resolved = new XsltStaticContextCompiler(Loader).ResolveXsltCollationUri(value.Trim(), staticContext.BaseUri);
        if (resolved != XPathNamespaces.CodepointCollation && resolved != XPathAsciiCaseInsensitiveCollation.Uri)
        {
            throw CreateXsltKeyError("The xsl:key collation does not identify a recognized collation.", XsltErrorCodes.XTSE1210, node, module);
        }

        return resolved;
    }

    internal void CompileXsltKeyPattern(XsltKeyDeclarationPlan declaration, XsltStylesheetPlan stylesheet)
    {
        XPathStaticContext context = new XsltValueTemplateCompiler(Loader).CreateXsltValueTemplateXPathContext(declaration.StaticContext, stylesheet, declaration.Declaration);
        XsltPatternCompiler compiler = new XsltPatternCompiler(Loader);
        compiler.AddXsltPatternKeyBindings(context);
        Task<bool> pending = Task.Run(() => compiler.CompileXsltPattern(declaration.Match, context, declaration.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltKeyTaskError("An xsl:key match pattern could not be compiled.", declaration.Declaration, declaration.Module, pending.Exception);
        }
    }

    internal void CompileXsltKeyValue(XsltKeyDeclarationPlan declaration, XsltStylesheetPlan stylesheet)
    {
        string? use = Loader.FindXsltAttribute(declaration.Declaration, "use", "");
        List<XmlNode> content = ReadXsltKeyContent(declaration);
        if (use != null && content.Count != 0 || use == null && content.Count == 0)
        {
            throw CreateXsltKeyError("An xsl:key declaration must contain exactly one of a use expression or a non-empty sequence constructor.", XsltErrorCodes.XTSE1205, declaration.Declaration, declaration.Module);
        }

        if (use != null)
        {
            declaration.ValueKind = XsltKeyValueKind.Expression;
            declaration.UseSource = use;
            declaration.Use = CompileXsltKeyUseExpression(use, declaration, stylesheet);
            declaration.UseType = declaration.Use.Root.StaticType;
            List<XsltVariablePlan> visible = new XsltVariableCompiler(Loader).CreateXsltVisibleVariablePlans(stylesheet, declaration.Declaration);
            declaration.Dependencies = new XsltVariableCompiler(Loader).CollectXsltVariableDependencies(declaration.Use.Root, visible);
        }
        else
        {
            declaration.ValueKind = XsltKeyValueKind.Content;
            declaration.Content = new XsltTemplateCompiler(Loader).CompileXsltSequenceConstructor(declaration.Declaration, content.ToArray(), stylesheet, false);
        }
    }

    internal List<XmlNode> ReadXsltKeyContent(XsltKeyDeclarationPlan declaration)
    {
        List<XmlNode> result = new List<XmlNode>();
        XmlNode[] children = declaration.Declaration.Children == null ? [] : declaration.Declaration.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            if (!whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                result.Add(child);
            }

            index = index + 1;
        }

        return result;
    }

    internal XPathPlan CompileXsltKeyUseExpression(string source, XsltKeyDeclarationPlan declaration, XsltStylesheetPlan stylesheet)
    {
        XPathStaticContext context = new XsltValueTemplateCompiler(Loader).CreateXsltValueTemplateXPathContext(declaration.StaticContext, stylesheet, declaration.Declaration);
        new XsltPatternCompiler(Loader).AddXsltPatternKeyBindings(context);
        Task<XPathPlan> pending = Task.Run(() => new XPathPlanCompiler(context).CompileXPathSource(source, declaration.Module.BaseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion)
        {
            throw CreateXsltKeyTaskError("An xsl:key use expression could not be compiled.", declaration.Declaration, declaration.Module, pending.Exception);
        }

        return pending.Result;
    }

    internal List<XsltKeyPlan> MergeXsltKeyDeclarations(List<XsltKeyDeclarationPlan> declarations)
    {
        List<XsltKeyPlan> result = new List<XsltKeyPlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            XsltKeyDeclarationPlan declaration = declarations[index];
            XsltKeyPlan? key = FindXsltKey(result, declaration.Name);
            if (key == null)
            {
                key = new XsltKeyPlan(declaration.ResolvedName, declaration.Name);
                result.Add(key);
            }

            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            XsltKeyPlan key = result[index];
            List<XsltKeyDeclarationPlan> matching = new List<XsltKeyDeclarationPlan>();
            int declarationIndex = 0;
            while (declarationIndex < declarations.Count)
            {
                XsltKeyDeclarationPlan declaration = declarations[declarationIndex];
                if (declaration.Name == key.Name)
                {
                    InsertXsltKeyDeclaration(matching, declaration);
                }

                declarationIndex = declarationIndex + 1;
            }

            key.Declarations = matching.ToArray();
            ValidateXsltMergedKey(key);
            index = index + 1;
        }

        return result;
    }

    internal XsltKeyPlan? FindXsltKey(List<XsltKeyPlan> keys, string name)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (keys[index].Name == name)
            {
                return keys[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void InsertXsltKeyDeclaration(List<XsltKeyDeclarationPlan> declarations, XsltKeyDeclarationPlan declaration)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            XsltKeyDeclarationPlan current = declarations[index];
            if (declaration.ImportPrecedence < current.ImportPrecedence || declaration.ImportPrecedence == current.ImportPrecedence && declaration.DeclarationOrder < current.DeclarationOrder)
            {
                declarations.Insert(index, declaration);
                return;
            }

            index = index + 1;
        }

        declarations.Add(declaration);
    }

    internal void ValidateXsltMergedKey(XsltKeyPlan key)
    {
        if (key.Declarations.Length == 0)
        {
            return;
        }

        key.Composite = key.Declarations[0].Composite;
        key.Collation = key.Declarations[0].Collation;
        int index = 1;
        while (index < key.Declarations.Length)
        {
            XsltKeyDeclarationPlan declaration = key.Declarations[index];
            if (declaration.Composite != key.Composite)
            {
                throw CreateXsltKeyError("Declarations for the same key must have the same composite value.", XsltErrorCodes.XTSE1222, declaration.Declaration, declaration.Module);
            }

            if (declaration.Collation != key.Collation)
            {
                throw CreateXsltKeyError("Declarations for the same key must use the same effective collation.", XsltErrorCodes.XTSE1220, declaration.Declaration, declaration.Module);
            }

            index = index + 1;
        }
    }

    internal XsltStaticException CreateXsltKeyTaskError(string message, XmlNode node, XsltStylesheetModule module, AggregateException? failure)
    {
        return CreateXsltKeyError(message, Loader.StaticProcessor.ReadXsltTaskErrorCode(failure), node, module);
    }

    internal XsltStaticException CreateXsltKeyError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
