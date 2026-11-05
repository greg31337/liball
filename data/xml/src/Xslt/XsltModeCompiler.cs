using System;
using System.Collections.Generic;

namespace xml;

internal class XsltModeCompiler
{
    internal XsltStylesheetLoader Loader;

    internal XsltModeCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal void CompileXsltModes(XsltStylesheetPlan stylesheet)
    {
        stylesheet.DeclaredModes = ReadXsltDeclaredModes(stylesheet);
        List<XsltModeDeclarationPlan> declarations = new List<XsltModeDeclarationPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "mode")
            {
                declarations.Add(CreateXsltModeDeclaration(declaration, stylesheet));
            }

            index = index + 1;
        }

        List<XsltModePlan> modes = MergeXsltModeDeclarations(declarations);
        List<XsltModeReferencePlan> references = CollectXsltModeReferences(stylesheet);
        index = 0;
        while (index < references.Count)
        {
            XsltModeReferencePlan reference = references[index];
            if (FindXsltMode(modes, reference.Name) == null)
            {
                if (stylesheet.DeclaredModes)
                {
                    throw CreateXsltModeError("The package contains a reference to an undeclared mode.", XsltErrorCodes.XTSE3085, reference.Node, reference.Module);
                }

                modes.Add(CreateXsltImplicitMode(reference.Name, reference.Node, reference.Module));
            }

            index = index + 1;
        }

        if (!stylesheet.DeclaredModes && FindXsltMode(modes, "#unnamed") == null)
        {
            modes.Add(CreateXsltImplicitMode("#unnamed", stylesheet.PrincipalModule.Root, stylesheet.PrincipalModule));
        }

        stylesheet.Modes = modes.ToArray();
    }

    internal bool ReadXsltDeclaredModes(XsltStylesheetPlan stylesheet)
    {
        XsltStylesheetModule module = stylesheet.PrincipalModule;
        if (module.Kind != XsltStylesheetModuleKind.Package)
        {
            return false;
        }

        string? value = Loader.FindXsltAttribute(module.Root, "declared-modes", "");
        return value == null || Loader.StaticProcessor.ParseXsltStaticBoolean(value, "declared-modes", module.Root, module);
    }

    internal XsltModeDeclarationPlan CreateXsltModeDeclaration(XsltStylesheetDeclaration declaration, XsltStylesheetPlan stylesheet)
    {
        XmlNode node = declaration.Node;
        XsltStaticContext? staticContext = declaration.StaticContext;
        if (staticContext == null)
        {
            staticContext = new XsltStaticContextCompiler(Loader).FindXsltStaticContext(stylesheet, node);
        }

        if (staticContext == null)
        {
            throw CreateXsltModeError("An xsl:mode declaration has no static context.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        ValidateXsltModeContent(node, declaration.Module);
        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        string name = lexicalName == null ? "#unnamed" : new XsltPackageCompiler(Loader).ResolveXsltComponentName(lexicalName, node);
        XsltModeDeclarationPlan result = new XsltModeDeclarationPlan(node, declaration.Module, staticContext, name);
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        ReadXsltModeAttributes(result);
        return result;
    }

    internal void ValidateXsltModeContent(XmlNode node, XsltStylesheetModule module)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
            if (child.Kind == XmlNodeKind.Element || !whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
            {
                throw CreateXsltModeError("An xsl:mode declaration must be empty.", XsltErrorCodes.XTSE0010, child, module);
            }

            index = index + 1;
        }
    }

    internal void ReadXsltModeAttributes(XsltModeDeclarationPlan declaration)
    {
        string? streamable = Loader.FindXsltAttribute(declaration.Declaration, "streamable", "");
        if (streamable != null)
        {
            declaration.HasStreamable = true;
            declaration.Streamable = Loader.StaticProcessor.ParseXsltStaticBoolean(streamable, "streamable", declaration.Declaration, declaration.Module);
        }

        string? accumulators = Loader.FindXsltAttribute(declaration.Declaration, "use-accumulators", "");
        if (accumulators != null)
        {
            declaration.HasUseAccumulators = true;
            ReadXsltModeAccumulators(declaration, accumulators);
        }

        string? onNoMatch = Loader.FindXsltAttribute(declaration.Declaration, "on-no-match", "");
        if (onNoMatch != null)
        {
            declaration.HasOnNoMatch = true;
            declaration.OnNoMatch = ParseXsltOnNoMatch(onNoMatch, declaration.Declaration, declaration.Module);
        }

        string? onMultipleMatch = Loader.FindXsltAttribute(declaration.Declaration, "on-multiple-match", "");
        if (onMultipleMatch != null)
        {
            declaration.HasOnMultipleMatch = true;
            declaration.OnMultipleMatch = ParseXsltOnMultipleMatch(onMultipleMatch, declaration.Declaration, declaration.Module);
        }

        string? warningNoMatch = Loader.FindXsltAttribute(declaration.Declaration, "warning-on-no-match", "");
        if (warningNoMatch != null)
        {
            declaration.HasWarningOnNoMatch = true;
            declaration.WarningOnNoMatch = Loader.StaticProcessor.ParseXsltStaticBoolean(warningNoMatch, "warning-on-no-match", declaration.Declaration, declaration.Module);
        }

        string? warningMultipleMatch = Loader.FindXsltAttribute(declaration.Declaration, "warning-on-multiple-match", "");
        if (warningMultipleMatch != null)
        {
            declaration.HasWarningOnMultipleMatch = true;
            declaration.WarningOnMultipleMatch = Loader.StaticProcessor.ParseXsltStaticBoolean(warningMultipleMatch, "warning-on-multiple-match", declaration.Declaration, declaration.Module);
        }

        string? typed = Loader.FindXsltAttribute(declaration.Declaration, "typed", "");
        if (typed != null)
        {
            declaration.HasTyped = true;
            declaration.Typed = ParseXsltTypedMode(typed, declaration.Declaration, declaration.Module);
            if (Loader.Options.ConformanceLevel != XsltConformanceLevel.SchemaAware && (declaration.Typed == XsltTypedMode.Yes || declaration.Typed == XsltTypedMode.Strict))
            {
                throw CreateXsltModeError("The typed mode requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1660, declaration.Declaration, declaration.Module);
            }
        }

        string? visibility = Loader.FindXsltAttribute(declaration.Declaration, "visibility", "");
        if (visibility != null)
        {
            declaration.HasVisibility = true;
            declaration.Visibility = ParseXsltModeVisibility(visibility, declaration.Declaration, declaration.Module);
            if (declaration.Name == "#unnamed" && declaration.Visibility != XsltComponentVisibility.Private)
            {
                throw CreateXsltModeError("The unnamed mode must have private visibility.", XsltErrorCodes.XTSE0020, declaration.Declaration, declaration.Module);
            }
        }
    }

    internal void ReadXsltModeAccumulators(XsltModeDeclarationPlan declaration, string source)
    {
        string[] tokens = source.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        List<string> names = new List<string>();
        int index = 0;
        while (index < tokens.Length)
        {
            string token = tokens[index];
            if (token == "#all")
            {
                if (tokens.Length != 1)
                {
                    throw CreateXsltModeError("#all must be the only use-accumulators token.", XsltErrorCodes.XTSE3300, declaration.Declaration, declaration.Module);
                }

                declaration.UseAllAccumulators = true;
            }
            else
            {
                if (token.Length == 0 || token[0] == '#')
                {
                    throw CreateXsltModeError("The use-accumulators attribute contains an invalid token.", XsltErrorCodes.XTSE3300, declaration.Declaration, declaration.Module);
                }

                string name = ResolveXsltAccumulatorName(token, declaration);

                if (names.Contains(name))
                {
                    throw CreateXsltModeError("The use-accumulators attribute contains a duplicate name.", XsltErrorCodes.XTSE3300, declaration.Declaration, declaration.Module);
                }

                names.Add(name);
            }

            index = index + 1;
        }

        declaration.AccumulatorNames = names.ToArray();
    }

    internal string ResolveXsltAccumulatorName(string token, XsltModeDeclarationPlan declaration)
    {
        if (token.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = token.IndexOf('}');
            string local = close < 0 ? "" : token.Substring(close + 1);
            if (close < 2 || !new XdmStringDatatype().IsValidXdmName(local, true))
            {
                throw CreateXsltModeError("The use-accumulators attribute contains an invalid EQName.", XsltErrorCodes.XTSE3300, declaration.Declaration, declaration.Module);
            }

            return "Q{" + token.Substring(2, close - 2) + "}" + local;
        }

        int colon = token.IndexOf(':');
        if (colon < 0)
        {
            if (!new XdmStringDatatype().IsValidXdmName(token, true))
            {
                throw CreateXsltModeError("The use-accumulators attribute contains an invalid EQName.", XsltErrorCodes.XTSE3300, declaration.Declaration, declaration.Module);
            }

            return "Q{}" + token;
        }

        string prefix = token.Substring(0, colon);
        string localName = token.Substring(colon + 1);
        if (colon == 0 || colon != token.LastIndexOf(':') || !new XdmStringDatatype().IsValidXdmName(prefix, true) || !new XdmStringDatatype().IsValidXdmName(localName, true))
        {
            throw CreateXsltModeError("The use-accumulators attribute contains an invalid EQName.", XsltErrorCodes.XTSE3300, declaration.Declaration, declaration.Module);
        }

        string? namespaceUri = FindXsltModeNamespace(declaration.StaticContext.Namespaces, prefix);
        if (namespaceUri == null)
        {
            throw CreateXsltModeError("The use-accumulators attribute uses an unbound prefix.", XsltErrorCodes.XTSE3300, declaration.Declaration, declaration.Module);
        }

        return "Q{" + namespaceUri + "}" + localName;
    }

    internal string? FindXsltModeNamespace(XmlNamespaceBinding[] bindings, string prefix)
    {
        if (prefix == "xml")
        {
            return XmlNamespaces.Xml;
        }

        int index = bindings.Length - 1;
        while (index >= 0)
        {
            string? optionalPrefix = bindings[index].Prefix;
            string bindingPrefix = optionalPrefix == null ? "" : optionalPrefix;
            if (bindingPrefix == prefix)
            {
                return bindings[index].IsUndeclaration ? null : bindings[index].NamespaceUri;
            }

            index = index - 1;
        }

        return null;
    }

    internal XsltOnNoMatchBehavior ParseXsltOnNoMatch(string source, XmlNode node, XsltStylesheetModule module)
    {
        string value = source.Trim();
        if (value == "deep-copy")
        {
            return XsltOnNoMatchBehavior.DeepCopy;
        }

        if (value == "shallow-copy")
        {
            return XsltOnNoMatchBehavior.ShallowCopy;
        }

        if (value == "deep-skip")
        {
            return XsltOnNoMatchBehavior.DeepSkip;
        }

        if (value == "shallow-skip")
        {
            return XsltOnNoMatchBehavior.ShallowSkip;
        }

        if (value == "text-only-copy")
        {
            return XsltOnNoMatchBehavior.TextOnlyCopy;
        }

        if (value == "fail")
        {
            return XsltOnNoMatchBehavior.Fail;
        }

        throw CreateXsltModeError("The on-no-match attribute has an invalid value.", XsltErrorCodes.XTSE0020, node, module);
    }

    internal XsltOnMultipleMatchBehavior ParseXsltOnMultipleMatch(string source, XmlNode node, XsltStylesheetModule module)
    {
        string value = source.Trim();
        if (value == "use-last")
        {
            return XsltOnMultipleMatchBehavior.UseLast;
        }

        if (value == "fail")
        {
            return XsltOnMultipleMatchBehavior.Fail;
        }

        throw CreateXsltModeError("The on-multiple-match attribute has an invalid value.", XsltErrorCodes.XTSE0020, node, module);
    }

    internal XsltTypedMode ParseXsltTypedMode(string source, XmlNode node, XsltStylesheetModule module)
    {
        string value = source.Trim();
        if (value == "yes" || value == "true" || value == "1")
        {
            return XsltTypedMode.Yes;
        }

        if (value == "no" || value == "false" || value == "0")
        {
            return XsltTypedMode.No;
        }

        if (value == "strict")
        {
            return XsltTypedMode.Strict;
        }

        if (value == "lax")
        {
            return XsltTypedMode.Lax;
        }

        if (value == "unspecified")
        {
            return XsltTypedMode.Unspecified;
        }

        throw CreateXsltModeError("The typed attribute has an invalid value.", XsltErrorCodes.XTSE0020, node, module);
    }

    internal XsltComponentVisibility ParseXsltModeVisibility(string source, XmlNode node, XsltStylesheetModule module)
    {
        string value = source.Trim();
        if (value == "public")
        {
            return XsltComponentVisibility.Public;
        }

        if (value == "private")
        {
            return XsltComponentVisibility.Private;
        }

        if (value == "final")
        {
            return XsltComponentVisibility.Final;
        }

        throw CreateXsltModeError("A mode visibility must be public, private, or final.", XsltErrorCodes.XTSE0020, node, module);
    }

    internal List<XsltModePlan> MergeXsltModeDeclarations(List<XsltModeDeclarationPlan> declarations)
    {
        List<XsltModePlan> result = new List<XsltModePlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            XsltModeDeclarationPlan declaration = declarations[index];
            XsltModePlan? mode = FindXsltMode(result, declaration.Name);
            if (mode == null)
            {
                mode = new XsltModePlan(declaration.Name, declaration.Declaration, declaration.Module);
                result.Add(mode);
            }

            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            XsltModePlan mode = result[index];
            List<XsltModeDeclarationPlan> matching = new List<XsltModeDeclarationPlan>();
            int declarationIndex = 0;
            while (declarationIndex < declarations.Count)
            {
                if (declarations[declarationIndex].Name == mode.Name)
                {
                    matching.Add(declarations[declarationIndex]);
                }

                declarationIndex = declarationIndex + 1;
            }

            mode.Declarations = matching.ToArray();
            SelectXsltModeOrigin(mode);
            MergeXsltModeProperties(mode);
            index = index + 1;
        }

        return result;
    }

    internal void SelectXsltModeOrigin(XsltModePlan mode)
    {
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.ImportPrecedence > mode.ImportPrecedence || declaration.ImportPrecedence == mode.ImportPrecedence && declaration.DeclarationOrder >= mode.DeclarationOrder)
            {
                mode.Origin = declaration.Declaration;
                mode.Module = declaration.Module;
                mode.ImportPrecedence = declaration.ImportPrecedence;
                mode.DeclarationOrder = declaration.DeclarationOrder;
            }

            index = index + 1;
        }
    }

    internal void MergeXsltModeProperties(XsltModePlan mode)
    {
        MergeXsltModeStreamable(mode);
        MergeXsltModeAccumulators(mode);
        MergeXsltModeOnNoMatch(mode);
        MergeXsltModeOnMultipleMatch(mode);
        MergeXsltModeWarningOnNoMatch(mode);
        MergeXsltModeWarningOnMultipleMatch(mode);
        MergeXsltModeTyped(mode);
        MergeXsltModeVisibility(mode);
    }

    internal void MergeXsltModeStreamable(XsltModePlan mode)
    {
        int precedence = FindXsltModeAttributePrecedence(mode, "streamable");
        bool found = false;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.HasStreamable && declaration.ImportPrecedence == precedence)
            {
                if (found && mode.Streamable != declaration.Streamable)
                {
                    ThrowXsltModeConflict("streamable", declaration);
                }

                mode.Streamable = declaration.Streamable;
                found = true;
            }

            index = index + 1;
        }
    }

    internal void MergeXsltModeAccumulators(XsltModePlan mode)
    {
        int precedence = FindXsltModeAttributePrecedence(mode, "use-accumulators");
        bool found = false;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.HasUseAccumulators && declaration.ImportPrecedence == precedence)
            {
                if (found && (mode.UseAllAccumulators != declaration.UseAllAccumulators || !SameXsltAccumulatorNames(mode.AccumulatorNames, declaration.AccumulatorNames)))
                {
                    ThrowXsltModeConflict("use-accumulators", declaration);
                }

                mode.UseAllAccumulators = declaration.UseAllAccumulators;
                mode.AccumulatorNames = declaration.AccumulatorNames;
                found = true;
            }

            index = index + 1;
        }
    }

    internal void MergeXsltModeOnNoMatch(XsltModePlan mode)
    {
        int precedence = FindXsltModeAttributePrecedence(mode, "on-no-match");
        bool found = false;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.HasOnNoMatch && declaration.ImportPrecedence == precedence)
            {
                if (found && mode.OnNoMatch != declaration.OnNoMatch)
                {
                    ThrowXsltModeConflict("on-no-match", declaration);
                }

                mode.OnNoMatch = declaration.OnNoMatch;
                found = true;
            }

            index = index + 1;
        }
    }

    internal void MergeXsltModeOnMultipleMatch(XsltModePlan mode)
    {
        int precedence = FindXsltModeAttributePrecedence(mode, "on-multiple-match");
        bool found = false;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.HasOnMultipleMatch && declaration.ImportPrecedence == precedence)
            {
                if (found && mode.OnMultipleMatch != declaration.OnMultipleMatch)
                {
                    ThrowXsltModeConflict("on-multiple-match", declaration);
                }

                mode.OnMultipleMatch = declaration.OnMultipleMatch;
                found = true;
            }

            index = index + 1;
        }
    }

    internal void MergeXsltModeWarningOnNoMatch(XsltModePlan mode)
    {
        int precedence = FindXsltModeAttributePrecedence(mode, "warning-on-no-match");
        bool found = false;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.HasWarningOnNoMatch && declaration.ImportPrecedence == precedence)
            {
                if (found && mode.WarningOnNoMatch != declaration.WarningOnNoMatch)
                {
                    ThrowXsltModeConflict("warning-on-no-match", declaration);
                }

                mode.WarningOnNoMatch = declaration.WarningOnNoMatch;
                found = true;
            }

            index = index + 1;
        }
    }

    internal void MergeXsltModeWarningOnMultipleMatch(XsltModePlan mode)
    {
        int precedence = FindXsltModeAttributePrecedence(mode, "warning-on-multiple-match");
        bool found = false;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.HasWarningOnMultipleMatch && declaration.ImportPrecedence == precedence)
            {
                if (found && mode.WarningOnMultipleMatch != declaration.WarningOnMultipleMatch)
                {
                    ThrowXsltModeConflict("warning-on-multiple-match", declaration);
                }

                mode.WarningOnMultipleMatch = declaration.WarningOnMultipleMatch;
                found = true;
            }

            index = index + 1;
        }
    }

    internal void MergeXsltModeTyped(XsltModePlan mode)
    {
        int precedence = FindXsltModeAttributePrecedence(mode, "typed");
        bool found = false;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.HasTyped && declaration.ImportPrecedence == precedence)
            {
                if (found && mode.Typed != declaration.Typed)
                {
                    ThrowXsltModeConflict("typed", declaration);
                }

                mode.Typed = declaration.Typed;
                found = true;
            }

            index = index + 1;
        }
    }

    internal void MergeXsltModeVisibility(XsltModePlan mode)
    {
        int precedence = FindXsltModeAttributePrecedence(mode, "visibility");
        bool found = false;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            if (declaration.HasVisibility && declaration.ImportPrecedence == precedence)
            {
                if (found && mode.DeclaredVisibility != declaration.Visibility)
                {
                    ThrowXsltModeConflict("visibility", declaration);
                }

                mode.DeclaredVisibility = declaration.Visibility;
                mode.HasDeclaredVisibility = true;
                found = true;
            }

            index = index + 1;
        }
    }

    internal int FindXsltModeAttributePrecedence(XsltModePlan mode, string attribute)
    {
        int result = -1;
        int index = 0;
        while (index < mode.Declarations.Length)
        {
            XsltModeDeclarationPlan declaration = mode.Declarations[index];
            bool present = attribute == "streamable" && declaration.HasStreamable ||
                attribute == "use-accumulators" && declaration.HasUseAccumulators ||
                attribute == "on-no-match" && declaration.HasOnNoMatch ||
                attribute == "on-multiple-match" && declaration.HasOnMultipleMatch ||
                attribute == "warning-on-no-match" && declaration.HasWarningOnNoMatch ||
                attribute == "warning-on-multiple-match" && declaration.HasWarningOnMultipleMatch ||
                attribute == "typed" && declaration.HasTyped ||
                attribute == "visibility" && declaration.HasVisibility;
            if (present && declaration.ImportPrecedence > result)
            {
                result = declaration.ImportPrecedence;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool SameXsltAccumulatorNames(string[] left, string[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (!ContainsXsltModeName(right, left[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void ThrowXsltModeConflict(string attribute, XsltModeDeclarationPlan declaration)
    {
        throw CreateXsltModeError("Mode declarations at the same import precedence specify conflicting " + attribute + " values.", XsltErrorCodes.XTSE0545, declaration.Declaration, declaration.Module);
    }

    internal List<XsltModeReferencePlan> CollectXsltModeReferences(XsltStylesheetPlan stylesheet)
    {
        List<XsltModeReferencePlan> result = new List<XsltModeReferencePlan>();
        int index = 0;
        while (index < stylesheet.Modules.Length)
        {
            XsltStylesheetModule module = stylesheet.Modules[index];
            CollectXsltModeReferencesInTree(module.Root, module, stylesheet, result);
            index = index + 1;
        }

        return result;
    }

    internal void CollectXsltModeReferencesInTree(XmlNode node, XsltStylesheetModule module, XsltStylesheetPlan stylesheet, List<XsltModeReferencePlan> references)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            string namespaceUri = Loader.XsltNodeNamespace(node);
            string localName = Loader.XsltNodeLocalName(node);
            XsltStaticContext? context = new XsltStaticContextCompiler(Loader).FindXsltStaticContext(stylesheet, node);
            string? declaredDefault = Loader.FindXsltAttribute(node, "default-mode", namespaceUri == XsltNamespaces.Xslt ? "" : XsltNamespaces.Xslt);
            if (declaredDefault != null && context != null)
            {
                AddXsltModeReference(references, context.DefaultMode, node, module);
            }

            if (namespaceUri == XsltNamespaces.Xslt && localName == "template" && Loader.FindXsltAttribute(node, "match", "") != null && context != null)
            {
                string? source = Loader.FindXsltAttribute(node, "mode", "");
                XsltTemplateMode[] modes = new XsltTemplateCompiler(Loader).ParseXsltTemplateModes(source, context, node, module);
                int modeIndex = 0;
                while (modeIndex < modes.Length)
                {
                    if (!modes[modeIndex].IsAll)
                    {
                        AddXsltModeReference(references, modes[modeIndex].Name, node, module);
                    }

                    modeIndex = modeIndex + 1;
                }
            }
            else if (namespaceUri == XsltNamespaces.Xslt && localName == "apply-templates" && context != null)
            {
                string? source = Loader.FindXsltAttribute(node, "mode", "");
                string token = source == null ? "#default" : source.Trim();
                if (token == "#default")
                {
                    AddXsltModeReference(references, context.DefaultMode, node, module);
                }
                else if (token == "#unnamed")
                {
                    AddXsltModeReference(references, "#unnamed", node, module);
                }
                else if (token != "#current")
                {
                    string name = new XsltPackageCompiler(Loader).ResolveXsltComponentName(token, node);
                    AddXsltModeReference(references, name, node, module);
                }
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                CollectXsltModeReferencesInTree(children[index], module, stylesheet, references);
            }

            index = index + 1;
        }
    }

    internal void AddXsltModeReference(List<XsltModeReferencePlan> references, string name, XmlNode node, XsltStylesheetModule module)
    {
        int index = 0;
        while (index < references.Count)
        {
            if (references[index].Name == name)
            {
                return;
            }

            index = index + 1;
        }

        references.Add(new XsltModeReferencePlan(name, node, module));
    }

    internal XsltModePlan CreateXsltImplicitMode(string name, XmlNode node, XsltStylesheetModule module)
    {
        XsltModePlan result = new XsltModePlan(name, node, module);
        result.IsImplicit = true;
        return result;
    }

    internal XsltModePlan? FindXsltMode(List<XsltModePlan> modes, string name)
    {
        int index = 0;
        while (index < modes.Count)
        {
            if (modes[index].Name == name)
            {
                return modes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool ContainsXsltModeName(string[] names, string name)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void BindXsltModes(XsltStylesheetPlan stylesheet)
    {
        XsltPackagePlan? package = stylesheet.Package;
        if (package == null)
        {
            return;
        }

        int index = 0;
        while (index < stylesheet.Modes.Length)
        {
            XsltModePlan mode = stylesheet.Modes[index];
            int componentIndex = 0;
            while (componentIndex < package.LocalComponents.Length)
            {
                XsltComponent component = package.LocalComponents[componentIndex];
                if (component.Kind == XsltComponentKind.Mode && component.Name == mode.Name)
                {
                    mode.Component = component;
                    component.Mode = mode;
                    break;
                }

                componentIndex = componentIndex + 1;
            }

            index = index + 1;
        }
    }

    internal XsltStaticException CreateXsltModeError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
