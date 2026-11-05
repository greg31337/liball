using System;
using System.Collections.Generic;

namespace xml;

internal class XsltStylesheetAssembler
{
    internal XsltStylesheetLoader Loader;
    internal List<XsltStylesheetLevel> OrderedLevels;
    internal List<XsltStylesheetDeclaration> OrderedDeclarations;
    internal List<XsltFallbackPlan> FallbackPlans;
    internal int NextImportPrecedence;

    internal XsltStylesheetAssembler(XsltStylesheetLoader loader)
    {
        Loader = loader;
        OrderedLevels = new List<XsltStylesheetLevel>();
        OrderedDeclarations = new List<XsltStylesheetDeclaration>();
        FallbackPlans = new List<XsltFallbackPlan>();
        NextImportPrecedence = 0;
    }

    internal void AssembleXsltStylesheet(XsltStylesheetPlan plan)
    {
        XsltStylesheetLevel root = new XsltStylesheetLevel(null, plan.PrincipalModule);
        ExpandXsltStylesheetLevel(root, plan.PrincipalModule);
        AssignXsltImportPrecedence(root);
        ValidateXsltModuleTrees(plan.Modules);
        plan.RootLevel = root;
        plan.Levels = OrderedLevels.ToArray();
        plan.Declarations = OrderedDeclarations.ToArray();
        plan.Fallbacks = FallbackPlans.ToArray();
    }

    internal void ExpandXsltStylesheetLevel(XsltStylesheetLevel level, XsltStylesheetModule module)
    {
        List<XsltStylesheetModule> modules = new List<XsltStylesheetModule>(level.Modules);
        List<XsltStylesheetLevel> imports = new List<XsltStylesheetLevel>(level.ImportedLevels);
        List<XsltStylesheetDeclaration> declarations = new List<XsltStylesheetDeclaration>(level.Declarations);
        modules.Add(module);
        level.Modules = modules.ToArray();
        if (module.Kind == XsltStylesheetModuleKind.Simplified)
        {
            string version = ReadXsltEffectiveVersion(module.Root, module);
            declarations.Add(new XsltStylesheetDeclaration(module.Root, module, level, "template", version, ClassifyXsltCompatibilityBehavior(version), declarations.Count, true));
            level.Declarations = declarations.ToArray();
            return;
        }

        XmlNode[] children = module.Root.Children == null ? [] : module.Root.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
            {
                if (!IsXsltWhitespace(child.Value))
                {
                    throw CreateXsltAssemblyError("A stylesheet element must not contain a text node.", XsltErrorCodes.XTSE0120, child, module);
                }
            }
            else if (child.Kind == XmlNodeKind.Element)
            {
                XsltStylesheetModuleRelation relation = Loader.ClassifyXsltModuleReference(child);
                if (relation == XsltStylesheetModuleRelation.Include)
                {
                    XsltStylesheetReference reference = FindXsltModuleReference(module, child);
                    XsltStylesheetModule? target = reference.Target;
                    if (target == null)
                    {
                        throw CreateXsltAssemblyError("The included stylesheet module is unavailable.", XsltErrorCodes.XTSE0165, child, module);
                    }

                    level.Declarations = declarations.ToArray();
                    level.ImportedLevels = imports.ToArray();
                    ExpandXsltStylesheetLevel(level, target);
                    declarations = new List<XsltStylesheetDeclaration>(level.Declarations);
                    imports = new List<XsltStylesheetLevel>(level.ImportedLevels);
                }
                else if (relation == XsltStylesheetModuleRelation.Import)
                {
                    string version = ReadXsltEffectiveVersion(child, module);
                    declarations.Add(new XsltStylesheetDeclaration(child, module, level, "import", version, ClassifyXsltCompatibilityBehavior(version), declarations.Count, false));
                    XsltStylesheetReference reference = FindXsltModuleReference(module, child);
                    XsltStylesheetModule? target = reference.Target;
                    if (target == null)
                    {
                        throw CreateXsltAssemblyError("The imported stylesheet module is unavailable.", XsltErrorCodes.XTSE0165, child, module);
                    }

                    XsltStylesheetLevel imported = new XsltStylesheetLevel(level, target);
                    imports.Add(imported);
                    ExpandXsltStylesheetLevel(imported, target);
                }
                else
                {
                    AddXsltTopLevelDeclaration(level, module, child, declarations);
                }
            }

            index = index + 1;
        }

        level.Declarations = declarations.ToArray();
        level.ImportedLevels = imports.ToArray();
    }

    internal void AddXsltTopLevelDeclaration(XsltStylesheetLevel level, XsltStylesheetModule module, XmlNode node, List<XsltStylesheetDeclaration> declarations)
    {
        string namespaceUri = Loader.XsltNodeNamespace(node);
        if (namespaceUri.Length == 0)
        {
            throw CreateXsltAssemblyError("A user-defined top-level data element must have a non-empty namespace URI.", XsltErrorCodes.XTSE0130, node, module);
        }

        if (namespaceUri != XsltNamespaces.Xslt)
        {
            return;
        }

        string version = ReadXsltEffectiveVersion(node, module);
        string localName = Loader.XsltNodeLocalName(node);
        if (!IsXsltTopLevelDeclarationName(localName, module.Kind))
        {
            if (ClassifyXsltCompatibilityBehavior(version) == XsltCompatibilityBehavior.Forwards)
            {
                return;
            }

            throw CreateXsltAssemblyError("The XSLT element is not permitted as a top-level declaration.", XsltErrorCodes.XTSE0010, node, module);
        }

        declarations.Add(new XsltStylesheetDeclaration(node, module, level, localName, version, ClassifyXsltCompatibilityBehavior(version), declarations.Count, false));
    }

    internal XsltStylesheetReference FindXsltModuleReference(XsltStylesheetModule module, XmlNode node)
    {
        int index = 0;
        while (index < module.References.Length)
        {
            if (module.References[index].Node == node)
            {
                return module.References[index];
            }

            index = index + 1;
        }

        throw CreateXsltAssemblyError("The stylesheet module reference was not loaded.", XsltErrorCodes.XTSE0165, node, module);
    }

    internal void AssignXsltImportPrecedence(XsltStylesheetLevel level)
    {
        int index = 0;
        while (index < level.ImportedLevels.Length)
        {
            AssignXsltImportPrecedence(level.ImportedLevels[index]);
            index = index + 1;
        }

        level.ImportPrecedence = NextImportPrecedence;
        NextImportPrecedence = NextImportPrecedence + 1;
        index = 0;
        while (index < level.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = level.Declarations[index];
            declaration.ImportPrecedence = level.ImportPrecedence;
            OrderedDeclarations.Add(declaration);
            index = index + 1;
        }

        OrderedLevels.Add(level);
    }

    internal void ValidateXsltModuleTrees(XsltStylesheetModule[] modules)
    {
        int index = 0;
        while (index < modules.Length)
        {
            XsltStylesheetModule module = modules[index];
            if (module.Kind != XsltStylesheetModuleKind.Simplified)
            {
                ValidateXsltDescendants(module.Root, module, true);
            }
            else
            {
                ValidateXsltDescendants(module.Root, module, false);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltDescendants(XmlNode parent, XsltStylesheetModule module, bool parentIsModuleRoot)
    {
        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                string namespaceUri = Loader.XsltNodeNamespace(child);
                string localName = Loader.XsltNodeLocalName(child);
                if (parentIsModuleRoot && namespaceUri != XsltNamespaces.Xslt)
                {
                    index = index + 1;
                    continue;
                }

                string effectiveVersion = ReadXsltEffectiveVersion(child, module);
                if (namespaceUri == XsltNamespaces.Xslt)
                {
                    if (!parentIsModuleRoot && localName == "include")
                    {
                        throw CreateXsltAssemblyError("An xsl:include element must be top-level.", XsltErrorCodes.XTSE0170, child, module);
                    }

                    if (!parentIsModuleRoot && localName == "import")
                    {
                        throw CreateXsltAssemblyError("An xsl:import element must be top-level.", XsltErrorCodes.XTSE0190, child, module);
                    }

                    if (parentIsModuleRoot && !IsXsltTopLevelDeclarationName(localName, module.Kind))
                    {
                        if (ClassifyXsltCompatibilityBehavior(effectiveVersion) == XsltCompatibilityBehavior.Forwards)
                        {
                            index = index + 1;
                            continue;
                        }
                    }
                    else if (!parentIsModuleRoot && !IsXsltKnownElementName(localName))
                    {
                        XmlNode[] fallbacks = FindXsltFallbackChildren(child);
                        if (ClassifyXsltCompatibilityBehavior(effectiveVersion) != XsltCompatibilityBehavior.Forwards || fallbacks.Length == 0)
                        {
                            throw CreateXsltAssemblyError("An unrecognized XSLT instruction requires forwards-compatible behavior and an xsl:fallback child.", XsltErrorCodes.XTSE0010, child, module);
                        }

                        FallbackPlans.Add(new XsltFallbackPlan(child, fallbacks, effectiveVersion));
                        ValidateXsltFallbackTrees(fallbacks, module);
                        index = index + 1;
                        continue;
                    }

                    if (parentIsModuleRoot && (localName == "include" || localName == "import"))
                    {
                        ValidateXsltModuleReferenceContent(child, module);
                    }
                }

                ValidateXsltDescendants(child, module, false);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltModuleReferenceContent(XmlNode reference, XsltStylesheetModule module)
    {
        XmlNode[] children = reference.Children == null ? [] : reference.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element || ((child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !IsXsltWhitespace(child.Value)))
            {
                throw CreateXsltAssemblyError("An xsl:include or xsl:import declaration must be empty.", XsltErrorCodes.XTSE0010, child, module);
            }

            index = index + 1;
        }
    }

    internal void ValidateXsltFallbackTrees(XmlNode[] fallbacks, XsltStylesheetModule module)
    {
        int index = 0;
        while (index < fallbacks.Length)
        {
            ValidateXsltDescendants(fallbacks[index], module, false);
            index = index + 1;
        }
    }

    internal XmlNode[] FindXsltFallbackChildren(XmlNode instruction)
    {
        List<XmlNode> result = new List<XmlNode>();
        XmlNode[] children = instruction.Children == null ? [] : instruction.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "fallback")
            {
                result.Add(child);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal string ReadXsltEffectiveVersion(XmlNode node, XsltStylesheetModule module)
    {
        XmlNode? current = node;
        while (current != null)
        {
            string currentNamespace = Loader.XsltNodeNamespace(current);
            string attributeNamespace = currentNamespace == XsltNamespaces.Xslt ? "" : XsltNamespaces.Xslt;
            string? value = currentNamespace == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(current) == "output" ? null : Loader.FindXsltAttribute(current, "version", attributeNamespace);
            if (value != null)
            {
                string normalized = value.Trim();
                if (!Loader.IsValidXsltDecimal(normalized))
                {
                    throw CreateXsltAssemblyError("The effective XSLT version must be a decimal value.", XsltErrorCodes.XTSE0110, current, module);
                }

                return normalized;
            }

            current = current.Parent;
        }

        return module.Version;
    }

    internal XsltCompatibilityBehavior ClassifyXsltCompatibilityBehavior(string version)
    {
        int comparison = CompareXsltDecimalVersionToThree(version);
        if (comparison < 0)
        {
            return XsltCompatibilityBehavior.Backwards;
        }

        if (comparison > 0)
        {
            return XsltCompatibilityBehavior.Forwards;
        }

        return XsltCompatibilityBehavior.Standard;
    }

    internal int CompareXsltDecimalVersionToThree(string version)
    {
        string value = version.Trim();
        int index = 0;
        bool negative = false;
        if (value.Length > 0 && (value[0] == '+' || value[0] == '-'))
        {
            negative = value[0] == '-';
            index = 1;
        }

        if (negative)
        {
            return -1;
        }

        int point = value.IndexOf('.', index);
        int integerEnd = point < 0 ? value.Length : point;
        while (index < integerEnd && value[index] == '0')
        {
            index = index + 1;
        }

        int integerLength = integerEnd - index;
        if (integerLength > 1)
        {
            return 1;
        }

        char integerDigit = integerLength == 0 ? '0' : value[index];
        if (integerDigit < '3')
        {
            return -1;
        }

        if (integerDigit > '3')
        {
            return 1;
        }

        if (point < 0)
        {
            return 0;
        }

        index = point + 1;
        while (index < value.Length)
        {
            if (value[index] != '0')
            {
                return 1;
            }

            index = index + 1;
        }

        return 0;
    }

    internal bool IsXsltWhitespace(string? value)
    {
        if (value == null)
        {
            return true;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' ' && character != '\t' && character != '\r' && character != '\n')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXsltTopLevelDeclarationName(string localName, XsltStylesheetModuleKind kind)
    {
        if (kind == XsltStylesheetModuleKind.Package && localName == "expose")
        {
            return true;
        }

        return localName == "accumulator" || localName == "attribute-set" || localName == "character-map" || localName == "decimal-format" ||
            localName == "function" || localName == "global-context-item" || localName == "import" || localName == "import-schema" ||
            localName == "include" || localName == "key" || localName == "mode" || localName == "namespace-alias" || localName == "output" ||
            localName == "param" || localName == "preserve-space" || localName == "strip-space" || localName == "template" ||
            localName == "use-package" || localName == "variable";
    }

    internal bool IsXsltKnownElementName(string localName)
    {
        return IsXsltTopLevelDeclarationName(localName, XsltStylesheetModuleKind.Package) || localName == "accept" || localName == "accumulator-rule" ||
            localName == "analyze-string" || localName == "apply-imports" || localName == "apply-templates" || localName == "array" || localName == "array-member" ||
            localName == "assert" || localName == "attribute" || localName == "break" || localName == "call-template" || localName == "catch" ||
            localName == "choose" || localName == "comment" || localName == "context-item" || localName == "copy" || localName == "copy-of" ||
            localName == "document" || localName == "element" || localName == "evaluate" || localName == "fallback" || localName == "for-each" ||
            localName == "for-each-group" || localName == "fork" || localName == "if" || localName == "iterate" || localName == "map" ||
            localName == "map-entry" || localName == "matching-substring" || localName == "merge" || localName == "merge-action" ||
            localName == "merge-key" || localName == "merge-source" || localName == "message" || localName == "namespace" ||
            localName == "next-iteration" || localName == "next-match" || localName == "non-matching-substring" || localName == "number" ||
            localName == "on-completion" || localName == "on-empty" || localName == "on-non-empty" || localName == "otherwise" ||
            localName == "output-character" || localName == "perform-sort" || localName == "processing-instruction" || localName == "result-document" ||
            localName == "sequence" || localName == "sort" || localName == "source-document" || localName == "stylesheet" || localName == "text" ||
            localName == "transform" || localName == "try" || localName == "override" || localName == "value-of" || localName == "when" || localName == "where-populated" ||
            localName == "with-param";
    }

    internal XsltStaticException CreateXsltAssemblyError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        Loader.ActiveLocations.Clear();
        AddXsltModuleLocation(module);
        return Loader.CreateXsltStaticError(message, code, node, module.BaseUri);
    }

    internal void AddXsltModuleLocation(XsltStylesheetModule module)
    {
        XsltStylesheetReference? origin = module.Origin;
        if (origin != null)
        {
            AddXsltModuleLocation(origin.Source);
        }

        if (module.BaseUri != null)
        {
            Loader.ActiveLocations.Add(module.BaseUri);
        }
    }
}
