using System;
using System.Collections.Generic;

namespace xml;

internal class XsltAttributeSetCompiler
{
    internal XsltStylesheetLoader Loader;
    internal XsltStylesheetPlan? Stylesheet;

    internal XsltAttributeSetCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
        Stylesheet = null;
    }

    internal void CompileXsltAttributeSets(XsltStylesheetPlan stylesheet)
    {
        Stylesheet = stylesheet;
        List<XsltAttributeSetDeclarationPlan> declarations = CollectXsltAttributeSetDeclarations(stylesheet);
        List<XsltAttributeSetPlan> sets = MergeXsltAttributeSetDeclarations(declarations);
        BindXsltAttributeSetComponents(stylesheet, sets);

        int index = 0;
        while (index < sets.Count)
        {
            ExpandXsltAttributeSet(sets[index], sets, sets[index].Origin);
            index = index + 1;
        }

        stylesheet.AttributeSets = sets.ToArray();
        stylesheet.AttributeSetUses = CollectXsltAttributeSetUses(stylesheet, sets).ToArray();
    }

    internal List<XsltAttributeSetDeclarationPlan> CollectXsltAttributeSetDeclarations(XsltStylesheetPlan stylesheet)
    {
        List<XsltAttributeSetDeclarationPlan> result = new List<XsltAttributeSetDeclarationPlan>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "attribute-set")
            {
                result.Add(CreateXsltAttributeSetDeclaration(declaration, stylesheet));
            }

            index = index + 1;
        }

        XsltPackagePlan? package = stylesheet.Package;
        if (package != null)
        {
            index = 0;
            while (index < package.Components.Length)
            {
                XsltComponent component = package.Components[index];
                if (component.Kind == XsltComponentKind.AttributeSet && component.IsOverride && component.CorrespondingComponent == null && !ContainsXsltAttributeSetDeclaration(result, component.Declaration))
                {
                    XsltStylesheetModule module = new XsltPackageCompiler(Loader).FindXsltNodeModule(component.Declaration);
                    XsltStylesheetDeclaration declaration = new XsltStylesheetDeclaration(component.Declaration, module, stylesheet.RootLevel, "attribute-set", module.Version, XsltCompatibilityBehavior.Standard, component.DeclarationOrder, true);
                    declaration.ImportPrecedence = component.ImportPrecedence;
                    XsltAttributeSetDeclarationPlan compiled = CreateXsltAttributeSetDeclaration(declaration, stylesheet);
                    compiled.Component = component;
                    result.Add(compiled);
                }

                index = index + 1;
            }
        }

        return result;
    }

    internal bool ContainsXsltAttributeSetDeclaration(List<XsltAttributeSetDeclarationPlan> declarations, XmlNode node)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (object.ReferenceEquals(declarations[index].Declaration, node))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltAttributeSetDeclarationPlan CreateXsltAttributeSetDeclaration(XsltStylesheetDeclaration declaration, XsltStylesheetPlan stylesheet)
    {
        XmlNode node = declaration.Node;
        XsltStaticContext? staticContext = declaration.StaticContext;
        if (staticContext == null)
        {
            staticContext = new XsltStaticContextCompiler(Loader).FindXsltStaticContext(stylesheet, node);
        }

        if (staticContext == null)
        {
            throw CreateXsltAttributeSetError("An xsl:attribute-set declaration has no static context.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        if (lexicalName == null || lexicalName.Trim().Length == 0)
        {
            throw CreateXsltAttributeSetError("An xsl:attribute-set declaration requires a name.", XsltErrorCodes.XTSE0710, node, declaration.Module);
        }

        string name = new XsltPackageCompiler(Loader).ResolveXsltComponentName(lexicalName, node);
        XsltAttributeSetDeclarationPlan result = new XsltAttributeSetDeclarationPlan(node, declaration.Module, staticContext, name);
        result.ImportPrecedence = declaration.ImportPrecedence;
        result.DeclarationOrder = declaration.DeclarationOrder;
        result.UsedNames = ReadXsltUsedAttributeSetNames(node, declaration.Module);
        string? streamable = Loader.FindXsltAttribute(node, "streamable", "");
        if (streamable != null)
        {
            result.HasStreamable = true;
            result.Streamable = Loader.StaticProcessor.ParseXsltStaticBoolean(streamable, "streamable", node, declaration.Module);
        }

        string? visibility = Loader.FindXsltAttribute(node, "visibility", "");
        if (visibility != null)
        {
            result.HasVisibility = true;
            result.Visibility = new XsltPackageCompiler(Loader).ParseXsltVisibility(visibility, false, node);
        }

        result.Attributes = CompileXsltAttributeSetAttributes(result, stylesheet);
        if (result.Visibility == XsltComponentVisibility.Abstract && (result.UsedNames.Length != 0 || result.Attributes.Length != 0))
        {
            throw CreateXsltAttributeSetError("An abstract attribute set must have no used attribute sets or attribute instructions.", XsltErrorCodes.XTSE0020, node, declaration.Module);
        }

        return result;
    }

    internal string[] ReadXsltUsedAttributeSetNames(XmlNode node, XsltStylesheetModule module)
    {
        string? source = Loader.FindXsltAttribute(node, "use-attribute-sets", "");
        if (source == null)
        {
            return [];
        }

        string[] tokens = source.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        string[] result = new string[tokens.Length];
        int index = 0;
        while (index < tokens.Length)
        {
            string token = tokens[index];
            if (token == "#default" || token == "#current" || token == "#all" || token == "#unnamed")
            {
                throw CreateXsltAttributeSetError("The use-attribute-sets attribute must contain EQNames.", XsltErrorCodes.XTSE0710, node, module);
            }

            result[index] = ResolveXsltAttributeSetName(token, node, module);
            index = index + 1;
        }

        return result;
    }

    internal XsltAttributeSetAttributePlan[] CompileXsltAttributeSetAttributes(XsltAttributeSetDeclarationPlan declaration, XsltStylesheetPlan stylesheet)
    {
        List<XsltAttributeSetAttributePlan> result = new List<XsltAttributeSetAttributePlan>();
        XmlNode[] children = declaration.Declaration.Children == null ? [] : declaration.Declaration.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                bool isAttribute = Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "attribute";
                if (!isAttribute)
                {
                    throw CreateXsltAttributeSetError("An xsl:attribute-set declaration may contain only xsl:attribute instructions.", XsltErrorCodes.XTSE0010, child, declaration.Module);
                }

                XsltSequenceItemPlan item = new XsltTemplateCompiler(Loader).CompileXsltSequenceElement(child, stylesheet);
                result.Add(new XsltAttributeSetAttributePlan(child, item, declaration));
            }
            else
            {
                bool whitespace = (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value);
                if (!whitespace && child.Kind != XmlNodeKind.Comment && child.Kind != XmlNodeKind.ProcessingInstruction)
                {
                    throw CreateXsltAttributeSetError("An xsl:attribute-set declaration must not contain text.", XsltErrorCodes.XTSE0010, child, declaration.Module);
                }
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal List<XsltAttributeSetPlan> MergeXsltAttributeSetDeclarations(List<XsltAttributeSetDeclarationPlan> declarations)
    {
        List<XsltAttributeSetPlan> result = new List<XsltAttributeSetPlan>();
        int index = 0;
        while (index < declarations.Count)
        {
            XsltAttributeSetDeclarationPlan declaration = declarations[index];
            XsltAttributeSetPlan? set = FindXsltAttributeSet(result, declaration.Name);
            if (set == null)
            {
                set = new XsltAttributeSetPlan(declaration.Name, declaration.Declaration, declaration.Module);
                result.Add(set);
            }

            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            XsltAttributeSetPlan set = result[index];
            List<XsltAttributeSetDeclarationPlan> matching = new List<XsltAttributeSetDeclarationPlan>();
            int declarationIndex = 0;
            while (declarationIndex < declarations.Count)
            {
                XsltAttributeSetDeclarationPlan declaration = declarations[declarationIndex];
                if (declaration.Name == set.Name)
                {
                    InsertXsltAttributeSetDeclaration(matching, declaration);
                }

                declarationIndex = declarationIndex + 1;
            }

            set.Declarations = matching.ToArray();
            SelectXsltAttributeSetOrigin(set);
            set.Streamable = ValidateXsltAttributeSetProperties(set);
            index = index + 1;
        }

        return result;
    }

    internal void InsertXsltAttributeSetDeclaration(List<XsltAttributeSetDeclarationPlan> declarations, XsltAttributeSetDeclarationPlan declaration)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            XsltAttributeSetDeclarationPlan current = declarations[index];
            if (declaration.ImportPrecedence < current.ImportPrecedence || declaration.ImportPrecedence == current.ImportPrecedence && declaration.DeclarationOrder < current.DeclarationOrder)
            {
                declarations.Insert(index, declaration);
                return;
            }

            index = index + 1;
        }

        declarations.Add(declaration);
    }

    internal XsltAttributeSetPlan? FindXsltAttributeSet(List<XsltAttributeSetPlan> sets, string name)
    {
        int index = 0;
        while (index < sets.Count)
        {
            if (sets[index].Name == name)
            {
                return sets[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void SelectXsltAttributeSetOrigin(XsltAttributeSetPlan set)
    {
        int index = 0;
        while (index < set.Declarations.Length)
        {
            XsltAttributeSetDeclarationPlan declaration = set.Declarations[index];
            if (declaration.ImportPrecedence > set.ImportPrecedence || declaration.ImportPrecedence == set.ImportPrecedence && declaration.DeclarationOrder >= set.DeclarationOrder)
            {
                set.Origin = declaration.Declaration;
                set.Module = declaration.Module;
                set.ImportPrecedence = declaration.ImportPrecedence;
                set.DeclarationOrder = declaration.DeclarationOrder;
            }

            index = index + 1;
        }
    }

    internal bool ValidateXsltAttributeSetProperties(XsltAttributeSetPlan set)
    {
        bool anyStreamable = false;
        bool hasVisibility = false;
        XsltComponentVisibility visibility = XsltComponentVisibility.Private;
        int index = 0;
        while (index < set.Declarations.Length)
        {
            XsltAttributeSetDeclarationPlan declaration = set.Declarations[index];
            if (declaration.Streamable)
            {
                anyStreamable = true;
            }

            if (declaration.HasVisibility)
            {
                if (hasVisibility && declaration.Visibility != visibility)
                {
                    throw CreateXsltAttributeSetError("Declarations for the same attribute set must specify the same visibility.", XsltErrorCodes.XTSE0020, declaration.Declaration, declaration.Module);
                }

                hasVisibility = true;
                visibility = declaration.Visibility;
            }

            index = index + 1;
        }

        index = 0;
        while (index < set.Declarations.Length)
        {
            XsltAttributeSetDeclarationPlan declaration = set.Declarations[index];
            if (hasVisibility && (!declaration.HasVisibility || declaration.Visibility != visibility))
            {
                throw CreateXsltAttributeSetError("Every declaration for an attribute set must specify the same visibility.", XsltErrorCodes.XTSE0020, declaration.Declaration, declaration.Module);
            }

            if (anyStreamable && (!declaration.HasStreamable || !declaration.Streamable))
            {
                throw CreateXsltAttributeSetError("Every declaration for a streamable attribute set must specify streamable='yes'.", XsltErrorCodes.XTSE0020, declaration.Declaration, declaration.Module);
            }

            index = index + 1;
        }

        return anyStreamable;
    }

    internal void BindXsltAttributeSetComponents(XsltStylesheetPlan stylesheet, List<XsltAttributeSetPlan> sets)
    {
        XsltPackagePlan? package = stylesheet.Package;
        if (package == null)
        {
            return;
        }

        int index = 0;
        while (index < sets.Count)
        {
            XsltAttributeSetPlan set = sets[index];
            int componentIndex = 0;
            while (componentIndex < package.Components.Length)
            {
                XsltComponent component = package.Components[componentIndex];
                if (component.Kind == XsltComponentKind.AttributeSet && component.Name == set.Name && component.CorrespondingComponent == null && object.ReferenceEquals(component.Declaration, set.Origin))
                {
                    set.Component = component;
                    component.AttributeSet = set;
                    break;
                }

                componentIndex = componentIndex + 1;
            }

            index = index + 1;
        }
    }

    internal void ExpandXsltAttributeSet(XsltAttributeSetPlan set, List<XsltAttributeSetPlan> localSets, XmlNode referenceNode)
    {
        if (set.ExpansionState == 2)
        {
            return;
        }

        if (set.ExpansionState == 1)
        {
            throw CreateXsltAttributeSetError("An xsl:attribute-set declaration directly or indirectly uses itself.", XsltErrorCodes.XTSE0720, referenceNode, FindXsltAttributeSetModule(referenceNode));
        }

        set.ExpansionState = 1;
        List<XsltAttributeSetAttributePlan> expanded = new List<XsltAttributeSetAttributePlan>();
        int declarationIndex = 0;
        while (declarationIndex < set.Declarations.Length)
        {
            XsltAttributeSetDeclarationPlan declaration = set.Declarations[declarationIndex];
            List<XsltAttributeSetPlan> usedSets = new List<XsltAttributeSetPlan>();
            int usedIndex = 0;
            while (usedIndex < declaration.UsedNames.Length)
            {
                XsltAttributeSetPlan used = ResolveXsltUsedAttributeSet(declaration.UsedNames[usedIndex], declaration, localSets);
                if (declaration.Streamable && !used.Streamable)
                {
                    throw CreateXsltAttributeSetError("A streamable attribute set must use only streamable attribute sets.", XsltErrorCodes.XTSE0730, declaration.Declaration, declaration.Module);
                }

                usedSets.Add(used);
                ExpandXsltAttributeSet(used, localSets, declaration.Declaration);
                int attributeIndex = 0;
                while (attributeIndex < used.ExpandedAttributes.Length)
                {
                    expanded.Add(used.ExpandedAttributes[attributeIndex]);
                    attributeIndex = attributeIndex + 1;
                }

                usedIndex = usedIndex + 1;
            }

            declaration.UsedSets = usedSets.ToArray();
            int ownIndex = 0;
            while (ownIndex < declaration.Attributes.Length)
            {
                expanded.Add(declaration.Attributes[ownIndex]);
                ownIndex = ownIndex + 1;
            }

            declarationIndex = declarationIndex + 1;
        }

        set.ExpandedAttributes = expanded.ToArray();
        set.ExpansionState = 2;
    }

    internal XsltAttributeSetPlan ResolveXsltUsedAttributeSet(string name, XsltAttributeSetDeclarationPlan declaration, List<XsltAttributeSetPlan> localSets)
    {
        if (name == "Q{" + XsltNamespaces.Xslt + "}original")
        {
            XsltAttributeSetPlan? owner = FindXsltAttributeSet(localSets, declaration.Name);
            XsltComponent? ownerComponent = owner == null ? null : owner.Component;
            XsltComponent? original = ownerComponent == null ? null : ownerComponent.OriginalComponent;
            if (original != null && original.AttributeSet != null)
            {
                return original.AttributeSet;
            }
        }

        XsltAttributeSetPlan? local = FindXsltAttributeSet(localSets, name);
        if (local != null)
        {
            return local;
        }

        XsltStylesheetPlan? stylesheet = Stylesheet;
        XsltPackagePlan? package = stylesheet == null ? null : stylesheet.Package;
        if (package != null)
        {
            int index = package.Components.Length - 1;
            while (index >= 0)
            {
                XsltComponent component = package.Components[index];
                if (component.Kind == XsltComponentKind.AttributeSet && component.Name == name && component.Visibility != XsltComponentVisibility.Hidden && component.AttributeSet != null)
                {
                    return component.AttributeSet;
                }

                index = index - 1;
            }
        }

        throw CreateXsltAttributeSetError("The use-attribute-sets attribute names an attribute set that is not available.", XsltErrorCodes.XTSE0710, declaration.Declaration, declaration.Module);
    }

    internal XsltAttributeSetPlan ResolveXsltAttributeSetUse(string name, XmlNode node, XsltStylesheetModule module, List<XsltAttributeSetPlan> localSets)
    {
        if (name == "Q{" + XsltNamespaces.Xslt + "}original")
        {
            XsltAttributeSetDeclarationPlan? declaration = FindXsltAttributeSetDeclarationByNode(localSets, node);
            if (declaration != null)
            {
                return ResolveXsltUsedAttributeSet(name, declaration, localSets);
            }
        }

        XsltAttributeSetPlan? local = FindXsltAttributeSet(localSets, name);
        if (local != null)
        {
            return local;
        }

        XsltStylesheetPlan? stylesheet = Stylesheet;
        XsltPackagePlan? package = stylesheet == null ? null : stylesheet.Package;
        if (package != null)
        {
            int index = package.Components.Length - 1;
            while (index >= 0)
            {
                XsltComponent component = package.Components[index];
                if (component.Kind == XsltComponentKind.AttributeSet && component.Name == name && component.Visibility != XsltComponentVisibility.Hidden && component.AttributeSet != null)
                {
                    return component.AttributeSet;
                }

                index = index - 1;
            }
        }

        throw CreateXsltAttributeSetError("The use-attribute-sets attribute names an attribute set that is not available.", XsltErrorCodes.XTSE0710, node, module);
    }

    internal XsltAttributeSetDeclarationPlan? FindXsltAttributeSetDeclarationByNode(List<XsltAttributeSetPlan> sets, XmlNode node)
    {
        int setIndex = 0;
        while (setIndex < sets.Count)
        {
            int declarationIndex = 0;
            while (declarationIndex < sets[setIndex].Declarations.Length)
            {
                XsltAttributeSetDeclarationPlan declaration = sets[setIndex].Declarations[declarationIndex];
                if (object.ReferenceEquals(declaration.Declaration, node))
                {
                    return declaration;
                }

                declarationIndex = declarationIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        return null;
    }

    internal List<XsltAttributeSetUsePlan> CollectXsltAttributeSetUses(XsltStylesheetPlan stylesheet, List<XsltAttributeSetPlan> localSets)
    {
        List<XsltAttributeSetUsePlan> result = new List<XsltAttributeSetUsePlan>();
        int index = 0;
        while (index < stylesheet.Modules.Length)
        {
            CollectXsltAttributeSetUsesInTree(stylesheet.Modules[index].Root, stylesheet.Modules[index], localSets, result);
            index = index + 1;
        }

        return result;
    }

    internal void CollectXsltAttributeSetUsesInTree(XmlNode node, XsltStylesheetModule module, List<XsltAttributeSetPlan> localSets, List<XsltAttributeSetUsePlan> result)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            string namespaceUri = Loader.XsltNodeNamespace(node);
            string localName = Loader.XsltNodeLocalName(node);
            XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                XmlNode attribute = attributes[attributeIndex];
                string attributeNamespace = Loader.XsltNodeNamespace(attribute);
                string attributeName = Loader.XsltNodeLocalName(attribute);
                bool instructionUse = namespaceUri == XsltNamespaces.Xslt && (localName == "copy" || localName == "element" || localName == "attribute-set") && attributeNamespace.Length == 0 && attributeName == "use-attribute-sets";
                bool literalUse = namespaceUri != XsltNamespaces.Xslt && attributeNamespace == XsltNamespaces.Xslt && attributeName == "use-attribute-sets";
                if (instructionUse || literalUse)
                {
                    result.Add(CreateXsltAttributeSetUse(node, attribute, module, localSets));
                }

                attributeIndex = attributeIndex + 1;
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            CollectXsltAttributeSetUsesInTree(children[index], module, localSets, result);
            index = index + 1;
        }
    }

    internal XsltAttributeSetUsePlan CreateXsltAttributeSetUse(XmlNode node, XmlNode attribute, XsltStylesheetModule module, List<XsltAttributeSetPlan> localSets)
    {
        XsltAttributeSetUsePlan result = new XsltAttributeSetUsePlan(node, attribute, module);
        string source = attribute.Value == null ? "" : attribute.Value;
        string[] tokens = source.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        string[] names = new string[tokens.Length];
        List<XsltAttributeSetPlan> sets = new List<XsltAttributeSetPlan>();
        List<XsltAttributeSetAttributePlan> expanded = new List<XsltAttributeSetAttributePlan>();
        int index = 0;
        while (index < tokens.Length)
        {
            string name = ResolveXsltAttributeSetName(tokens[index], node, module);
            names[index] = name;
            XsltAttributeSetPlan set = ResolveXsltAttributeSetUse(name, node, module, localSets);
            sets.Add(set);
            int attributeIndex = 0;
            while (attributeIndex < set.ExpandedAttributes.Length)
            {
                expanded.Add(set.ExpandedAttributes[attributeIndex]);
                attributeIndex = attributeIndex + 1;
            }

            index = index + 1;
        }

        result.Names = names;
        result.Sets = sets.ToArray();
        result.ExpandedAttributes = expanded.ToArray();
        return result;
    }

    internal string ResolveXsltAttributeSetName(string lexicalName, XmlNode node, XsltStylesheetModule module)
    {
        string value = lexicalName.Trim();
        if (value.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = value.IndexOf('}');
            if (close < 2 || close + 1 >= value.Length)
            {
                throw CreateXsltAttributeSetError("The use-attribute-sets attribute contains an invalid EQName.", XsltErrorCodes.XTSE0710, node, module);
            }

            string local = value.Substring(close + 1);
            if (!new XdmStringDatatype().IsValidXdmName(local, true))
            {
                throw CreateXsltAttributeSetError("The use-attribute-sets attribute contains an invalid EQName.", XsltErrorCodes.XTSE0710, node, module);
            }

            return "Q{" + value.Substring(2, close - 2) + "}" + local;
        }

        int colon = value.IndexOf(':');
        if (colon < 0)
        {
            if (!new XdmStringDatatype().IsValidXdmName(value, true))
            {
                throw CreateXsltAttributeSetError("The use-attribute-sets attribute contains an invalid EQName.", XsltErrorCodes.XTSE0710, node, module);
            }

            return "Q{}" + value;
        }

        string prefix = value.Substring(0, colon);
        string localName = value.Substring(colon + 1);
        if (!new XdmStringDatatype().IsValidXdmName(prefix, true) || !new XdmStringDatatype().IsValidXdmName(localName, true) || localName.IndexOf(':') >= 0)
        {
            throw CreateXsltAttributeSetError("The use-attribute-sets attribute contains an invalid QName.", XsltErrorCodes.XTSE0710, node, module);
        }

        string? namespaceUri = FindXsltAttributeSetNamespace(prefix, node);
        if (namespaceUri == null)
        {
            throw CreateXsltAttributeSetError("The use-attribute-sets attribute uses an unbound namespace prefix.", XsltErrorCodes.XTSE0710, node, module);
        }

        return "Q{" + namespaceUri + "}" + localName;
    }

    internal string? FindXsltAttributeSetNamespace(string prefix, XmlNode node)
    {
        if (prefix == "xml")
        {
            return "http://www.w3.org/XML/1998/namespace";
        }

        XmlNode? current = node;
        while (current != null)
        {
            XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
            int index = bindings.Length - 1;
            while (index >= 0)
            {
                string? optionalPrefix = bindings[index].Prefix;
                string bindingPrefix = optionalPrefix == null ? "" : optionalPrefix;
                string? namespaceUri = bindings[index].NamespaceUri;
                if (bindingPrefix == prefix && !bindings[index].IsUndeclaration && namespaceUri != null)
                {
                    return namespaceUri;
                }

                index = index - 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal XsltStylesheetModule FindXsltAttributeSetModule(XmlNode node)
    {
        return new XsltPackageCompiler(Loader).FindXsltNodeModule(node);
    }

    internal XsltStaticException CreateXsltAttributeSetError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
