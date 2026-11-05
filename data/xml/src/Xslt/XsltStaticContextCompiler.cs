using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltStaticContextCompiler
{
    internal XsltStylesheetLoader Loader;
    internal XsltStylesheetAssembler Assembler;

    internal XsltStaticContextCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
        Assembler = new XsltStylesheetAssembler(loader);
    }

    internal void CompileXsltStaticContexts(XsltStylesheetPlan stylesheet)
    {
        List<XsltStaticContext> contexts = new List<XsltStaticContext>();
        int moduleIndex = 0;
        while (moduleIndex < stylesheet.Modules.Length)
        {
            XsltStylesheetModule module = stylesheet.Modules[moduleIndex];
            XmlNamespaceBinding[] declarations = module.Root.NamespaceBindings == null ? [] : module.Root.NamespaceBindings;
            NamespaceContext namespaces = new NamespaceContext(null, declarations);
            CompileXsltStaticContextTree(module.Root, module, null, namespaces, contexts);
            moduleIndex = moduleIndex + 1;
        }

        stylesheet.StaticContexts = contexts.ToArray();
        AttachXsltDeclarationStaticContexts(stylesheet);
        XsltStaticContext? principal = FindXsltStaticContext(stylesheet, stylesheet.PrincipalModule.Root);
        if (principal != null)
        {
            stylesheet.InitialMode = principal.DefaultMode;
        }

        stylesheet.NamespaceAliases = CompileXsltNamespaceAliases(stylesheet);
    }

    internal void CompileXsltStaticContextTree(XmlNode node, XsltStylesheetModule module, XsltStaticContext? parent, NamespaceContext namespaces, List<XsltStaticContext> contexts)
    {
        XsltStaticContext context = CreateXsltStaticContext(node, module, parent, namespaces);
        contexts.Add(context);
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                XmlNamespaceBinding[] declarations = child.NamespaceBindings == null ? [] : child.NamespaceBindings;
                CompileXsltStaticContextTree(child, module, context, namespaces.PushXmlNamespaceFrame(declarations), contexts);
            }

            index = index + 1;
        }
    }

    internal XsltStaticContext CreateXsltStaticContext(XmlNode node, XsltStylesheetModule module, XsltStaticContext? parent, NamespaceContext namespaces)
    {
        XsltStaticContext result = new XsltStaticContext(node, module);
        result.CollationBindings = Loader.Options.Collations == null ? [] : Loader.Options.Collations;
        result.Extensions = Loader.Options.Extensions == null ? [] : Loader.Options.Extensions;
        result.ExtensionPolicy = Loader.Options.ExtensionPolicy == null ? new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(null) : Loader.Options.ExtensionPolicy;
        if (parent != null)
        {
            result.XPathDefaultNamespace = parent.XPathDefaultNamespace;
            result.DefaultCollation = parent.DefaultCollation;
            result.DefaultMode = parent.DefaultMode;
            result.Validation = parent.Validation;
            result.ExcludedNamespaceUris = CopyXsltStrings(parent.ExcludedNamespaceUris);
            result.ExtensionNamespaceUris = CopyXsltStrings(parent.ExtensionNamespaceUris);
            result.ExpandText = parent.ExpandText;
        }

        result.Namespaces = CopyXsltNamespaceBindings(namespaces.EnumerateInScopeXmlNamespaces());
        result.BaseUri = Loader.ReadXsltNodeBaseUri(node, module.BaseUri);
        result.EffectiveVersion = Assembler.ReadXsltEffectiveVersion(node, module);
        result.CompatibilityBehavior = Assembler.ClassifyXsltCompatibilityBehavior(result.EffectiveVersion);
        result.XPathCompatibilityMode = IsXsltVersionOne(result.EffectiveVersion) ? XPathCompatibilityMode.BackwardsCompatible : XPathCompatibilityMode.Strict;

        string? xpathDefaultNamespace = ReadXsltStandardAttribute(node, "xpath-default-namespace");
        if (xpathDefaultNamespace != null)
        {
            result.XPathDefaultNamespace = xpathDefaultNamespace.Trim();
        }

        string? defaultCollation = ReadXsltStandardAttribute(node, "default-collation");
        if (defaultCollation != null)
        {
            result.DefaultCollation = ResolveXsltDefaultCollation(defaultCollation, result.BaseUri, node, module);
        }

        string? defaultMode = ReadXsltStandardAttribute(node, "default-mode");
        if (defaultMode != null)
        {
            result.DefaultMode = ResolveXsltDefaultMode(defaultMode, namespaces, node, module);
        }

        string? defaultValidation = ReadXsltStandardAttribute(node, "default-validation");
        if (defaultValidation != null)
        {
            result.Validation = ParseXsltDefaultValidation(defaultValidation, node, module);
        }

        string? expandText = ReadXsltStandardAttribute(node, "expand-text");
        if (expandText != null)
        {
            result.ExpandText = ParseXsltExpandText(expandText, node, module);
        }

        string? extensionPrefixes = ReadXsltStandardAttribute(node, "extension-element-prefixes");
        if (extensionPrefixes != null)
        {
            AddXsltExtensionNamespaces(result, extensionPrefixes, namespaces, node, module);
        }

        string? excludedPrefixes = ReadXsltStandardAttribute(node, "exclude-result-prefixes");
        if (excludedPrefixes != null)
        {
            AddXsltExcludedNamespaces(result, excludedPrefixes, namespaces, node, module);
        }

        return result;
    }

    internal string? ReadXsltStandardAttribute(XmlNode node, string localName)
    {
        string namespaceUri = Loader.XsltNodeNamespace(node);
        if (namespaceUri == XsltNamespaces.Xslt)
        {
            if (localName == "version" && Loader.XsltNodeLocalName(node) == "output")
            {
                return null;
            }

            return Loader.FindXsltAttribute(node, localName, "");
        }

        return Loader.FindXsltAttribute(node, localName, XsltNamespaces.Xslt);
    }

    internal string ResolveXsltDefaultCollation(string value, string? baseUri, XmlNode node, XsltStylesheetModule module)
    {
        string[] candidates = value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < candidates.Length)
        {
            string? resolved = ResolveXsltCollationUri(candidates[index], baseUri);
            if (resolved != null && (resolved == XPathNamespaces.CodepointCollation || resolved == XPathAsciiCaseInsensitiveCollation.Uri || IsRegisteredXsltCollation(resolved)))
            {
                return resolved;
            }

            index = index + 1;
        }

        throw CreateXsltStaticContextError("No URI in default-collation identifies an available collation.", XsltErrorCodes.XTSE0125, node, module);
    }

    internal bool IsRegisteredXsltCollation(string? uri)
    {
        XPathCollationBinding[] bindings = Loader.Options.Collations == null ? [] : Loader.Options.Collations;
        int index = 0;
        while (index < bindings.Length)
        {
            if (bindings[index].Uri == uri && bindings[index].Implementation != null)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string? ResolveXsltCollationUri(string value, string? baseUri)
    {
        if (value == XPathNamespaces.CodepointCollation || value == XPathAsciiCaseInsensitiveCollation.Uri)
        {
            return value;
        }

        if (baseUri == null && !Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            return null;
        }

        Task<string> pending = Task.Run(() => ResolveXsltCollationUriCore(value, baseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        return pending.Status == TaskStatus.RanToCompletion ? pending.Result : null;
    }

    internal string ResolveXsltCollationUriCore(string value, string? baseUri)
    {
        Uri uri;
        if (Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            uri = UriTools.CreateAbsolute(value);
        }
        else
        {
            if (baseUri == null)
            {
                return "";
            }

            uri = UriTools.ResolveReference(UriTools.CreateAbsolute(baseUri), value);
        }

        return UriTools.CanonicalAbsoluteUri(uri);
    }

    internal string ResolveXsltDefaultMode(string value, NamespaceContext namespaces, XmlNode node, XsltStylesheetModule module)
    {
        string name = value.Trim();
        if (name == "#unnamed")
        {
            return name;
        }

        if (name.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = name.IndexOf('}');
            string localName = close < 0 ? "" : name.Substring(close + 1);
            if (close < 2 || !IsValidXsltNcName(localName))
            {
                throw CreateXsltStaticContextError("The default mode must be an EQName or #unnamed.", XsltErrorCodes.XTSE0020, node, module);
            }

            return "Q{" + name.Substring(2, close - 2) + "}" + localName;
        }

        int colon = name.IndexOf(':');
        if (colon < 0)
        {
            if (!IsValidXsltNcName(name))
            {
                throw CreateXsltStaticContextError("The default mode must be an EQName or #unnamed.", XsltErrorCodes.XTSE0020, node, module);
            }

            return "Q{}" + name;
        }

        if (colon == 0 || colon != name.LastIndexOf(':') || !IsValidXsltNcName(name.Substring(0, colon)) || !IsValidXsltNcName(name.Substring(colon + 1)))
        {
            throw CreateXsltStaticContextError("The default mode must be an EQName or #unnamed.", XsltErrorCodes.XTSE0020, node, module);
        }

        string? namespaceUri = namespaces.LookupXmlNamespaceUri(name.Substring(0, colon));
        if (namespaceUri == null)
        {
            throw CreateXsltStaticContextError("The default mode uses an unbound namespace prefix.", XsltErrorCodes.XTSE0280, node, module);
        }

        return "Q{" + namespaceUri + "}" + name.Substring(colon + 1);
    }

    internal XsltDefaultValidation ParseXsltDefaultValidation(string value, XmlNode node, XsltStylesheetModule module)
    {
        string normalized = value.Trim();
        if (normalized == "strip")
        {
            return XsltDefaultValidation.Strip;
        }

        if (normalized == "preserve")
        {
            return XsltDefaultValidation.Preserve;
        }

        throw CreateXsltStaticContextError("The default-validation value must be preserve or strip.", XsltErrorCodes.XTSE0020, node, module);
    }

    internal bool ParseXsltExpandText(string value, XmlNode node, XsltStylesheetModule module)
    {
        string normalized = value.Trim();
        if (normalized == "yes" || normalized == "true" || normalized == "1")
        {
            return true;
        }

        if (normalized == "no" || normalized == "false" || normalized == "0")
        {
            return false;
        }

        throw CreateXsltStaticContextError("The expand-text value must be a valid XML boolean.", XsltErrorCodes.XTSE0020, node, module);
    }

    internal void AddXsltExtensionNamespaces(XsltStaticContext context, string value, NamespaceContext namespaces, XmlNode node, XsltStylesheetModule module)
    {
        string[] prefixes = value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < prefixes.Length)
        {
            string namespaceUri = ResolveXsltPrefixNamespace(prefixes[index], namespaces, XsltErrorCodes.XTSE1430, node, module);
            if (IsXsltReservedNamespace(namespaceUri))
            {
                throw CreateXsltStaticContextError("An extension namespace must not be a reserved namespace.", XsltErrorCodes.XTSE0085, node, module);
            }

            context.ExtensionNamespaceUris = AddUniqueXsltString(context.ExtensionNamespaceUris, namespaceUri);
            context.ExcludedNamespaceUris = AddUniqueXsltString(context.ExcludedNamespaceUris, namespaceUri);
            index = index + 1;
        }
    }

    internal void AddXsltExcludedNamespaces(XsltStaticContext context, string value, NamespaceContext namespaces, XmlNode node, XsltStylesheetModule module)
    {
        string[] prefixes = value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (prefixes.Length == 1 && prefixes[0] == "#all")
        {
            XmlNamespaceBinding[] bindings = namespaces.EnumerateInScopeXmlNamespaces();
            int bindingIndex = 0;
            while (bindingIndex < bindings.Length)
            {
                string? optionalNamespaceUri = bindings[bindingIndex].NamespaceUri;
                string namespaceUri = optionalNamespaceUri == null ? "" : optionalNamespaceUri;
                if (namespaceUri.Length > 0)
                {
                    context.ExcludedNamespaceUris = AddUniqueXsltString(context.ExcludedNamespaceUris, namespaceUri);
                }

                bindingIndex = bindingIndex + 1;
            }

            return;
        }

        int index = 0;
        while (index < prefixes.Length)
        {
            if (prefixes[index] == "#all")
            {
                throw CreateXsltStaticContextError("#all must be the only exclude-result-prefixes token.", XsltErrorCodes.XTSE0020, node, module);
            }

            string namespaceUri = ResolveXsltPrefixNamespace(prefixes[index], namespaces, XsltErrorCodes.XTSE0808, node, module);
            context.ExcludedNamespaceUris = AddUniqueXsltString(context.ExcludedNamespaceUris, namespaceUri);
            index = index + 1;
        }
    }

    internal string ResolveXsltPrefixNamespace(string prefixToken, NamespaceContext namespaces, string unboundCode, XmlNode node, XsltStylesheetModule module)
    {
        if (prefixToken == "#default")
        {
            string? defaultNamespace = namespaces.DefaultXmlNamespaceUri();
            if (defaultNamespace == null)
            {
                string code = unboundCode == XsltErrorCodes.XTSE0808 ? XsltErrorCodes.XTSE0809 : unboundCode;
                throw CreateXsltStaticContextError("The #default token requires a default namespace binding.", code, node, module);
            }

            return defaultNamespace;
        }

        if (!IsValidXsltNcName(prefixToken))
        {
            throw CreateXsltStaticContextError("A namespace prefix token is not a valid NCName.", XsltErrorCodes.XTSE0020, node, module);
        }

        string? namespaceUri = namespaces.LookupXmlNamespaceUri(prefixToken);
        if (namespaceUri == null)
        {
            throw CreateXsltStaticContextError("A namespace prefix token is not bound.", unboundCode, node, module);
        }

        return namespaceUri;
    }

    internal bool IsXsltReservedNamespace(string namespaceUri)
    {
        return namespaceUri == XsltNamespaces.Xslt || namespaceUri == XmlNamespaces.Xml || namespaceUri == XmlNamespaces.Xmlns ||
            namespaceUri == XmlNamespaces.XmlSchema || namespaceUri == XmlNamespaces.XmlSchemaInstance;
    }

    internal void AttachXsltDeclarationStaticContexts(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            stylesheet.Declarations[index].StaticContext = FindXsltStaticContext(stylesheet, stylesheet.Declarations[index].Node);
            index = index + 1;
        }
    }

    internal XsltStaticContext? FindXsltStaticContext(XsltStylesheetPlan stylesheet, XmlNode node)
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

    internal XsltNamespaceAlias[] CompileXsltNamespaceAliases(XsltStylesheetPlan stylesheet)
    {
        List<XsltNamespaceAlias> candidates = new List<XsltNamespaceAlias>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "namespace-alias")
            {
                candidates.Add(CreateXsltNamespaceAlias(declaration, index));
            }

            index = index + 1;
        }

        List<XsltNamespaceAlias> result = new List<XsltNamespaceAlias>();
        index = 0;
        while (index < candidates.Count)
        {
            XsltNamespaceAlias candidate = candidates[index];
            if (!ContainsXsltNamespaceAlias(result, candidate.LiteralNamespaceUri))
            {
                result.Add(SelectXsltNamespaceAlias(candidates, candidate.LiteralNamespaceUri, candidate.Declaration, FindXsltDeclarationModule(stylesheet, candidate.Declaration)));
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltNamespaceAlias CreateXsltNamespaceAlias(XsltStylesheetDeclaration declaration, int order)
    {
        XsltStaticContext? context = declaration.StaticContext;
        string? stylesheetPrefix = Loader.FindXsltAttribute(declaration.Node, "stylesheet-prefix", "");
        string? resultPrefix = Loader.FindXsltAttribute(declaration.Node, "result-prefix", "");
        if (context == null || stylesheetPrefix == null || resultPrefix == null)
        {
            throw CreateXsltStaticContextError("xsl:namespace-alias requires stylesheet-prefix and result-prefix.", XsltErrorCodes.XTSE0010, declaration.Node, declaration.Module);
        }

        string literalNamespace = ResolveXsltAliasNamespace(stylesheetPrefix, context, declaration);
        string targetNamespace = ResolveXsltAliasNamespace(resultPrefix, context, declaration);
        string targetPrefix = resultPrefix.Trim() == "#default" ? "" : resultPrefix.Trim();
        return new XsltNamespaceAlias(declaration.Node, literalNamespace, targetNamespace, targetPrefix, declaration.ImportPrecedence, order);
    }

    internal string ResolveXsltAliasNamespace(string value, XsltStaticContext context, XsltStylesheetDeclaration declaration)
    {
        string prefix = value.Trim();
        if (prefix == "#default")
        {
            string? defaultNamespace = LookupXsltSnapshotNamespace(context.Namespaces, "");
            return defaultNamespace == null ? "" : defaultNamespace;
        }

        if (!IsValidXsltNcName(prefix))
        {
            throw CreateXsltStaticContextError("A namespace alias prefix must be #default or an NCName.", XsltErrorCodes.XTSE0812, declaration.Node, declaration.Module);
        }

        string? namespaceUri = LookupXsltSnapshotNamespace(context.Namespaces, prefix);
        if (namespaceUri == null)
        {
            throw CreateXsltStaticContextError("A namespace alias prefix is not bound.", XsltErrorCodes.XTSE0812, declaration.Node, declaration.Module);
        }

        return namespaceUri;
    }

    internal XsltNamespaceAlias SelectXsltNamespaceAlias(List<XsltNamespaceAlias> candidates, string literalNamespaceUri, XmlNode node, XsltStylesheetModule module)
    {
        XsltNamespaceAlias? selected = null;
        int highestPrecedence = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            XsltNamespaceAlias candidate = candidates[index];
            if (candidate.LiteralNamespaceUri == literalNamespaceUri && candidate.ImportPrecedence > highestPrecedence)
            {
                highestPrecedence = candidate.ImportPrecedence;
                selected = candidate;
            }

            index = index + 1;
        }

        index = 0;
        while (index < candidates.Count)
        {
            XsltNamespaceAlias candidate = candidates[index];
            if (candidate.LiteralNamespaceUri == literalNamespaceUri && candidate.ImportPrecedence == highestPrecedence)
            {
                if (selected != null && candidate.TargetNamespaceUri != selected.TargetNamespaceUri)
                {
                    throw CreateXsltStaticContextError("Namespace aliases at the same import precedence have conflicting result namespaces.", XsltErrorCodes.XTSE0810, candidate.Declaration, module);
                }

                if (selected == null || candidate.DeclarationOrder > selected.DeclarationOrder)
                {
                    selected = candidate;
                }
            }

            index = index + 1;
        }

        if (selected == null)
        {
            throw CreateXsltStaticContextError("A namespace alias could not be selected.", XsltErrorCodes.XTSE0812, node, module);
        }

        return selected;
    }

    internal bool ContainsXsltNamespaceAlias(List<XsltNamespaceAlias> aliases, string literalNamespaceUri)
    {
        int index = 0;
        while (index < aliases.Count)
        {
            if (aliases[index].LiteralNamespaceUri == literalNamespaceUri)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltStylesheetModule FindXsltDeclarationModule(XsltStylesheetPlan stylesheet, XmlNode node)
    {
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            if (object.ReferenceEquals(stylesheet.Declarations[index].Node, node))
            {
                return stylesheet.Declarations[index].Module;
            }

            index = index + 1;
        }

        return stylesheet.PrincipalModule;
    }

    internal string? LookupXsltSnapshotNamespace(XmlNamespaceBinding[] bindings, string prefix)
    {
        int index = 0;
        while (index < bindings.Length)
        {
            string? optionalPrefix = bindings[index].Prefix;
            string bindingPrefix = optionalPrefix == null ? "" : optionalPrefix;
            if (bindingPrefix == prefix)
            {
                return bindings[index].NamespaceUri;
            }

            index = index + 1;
        }

        return prefix == "xml" ? XmlNamespaces.Xml : null;
    }

    internal XmlNamespaceBinding[] CopyXsltNamespaceBindings(XmlNamespaceBinding[] source)
    {
        XmlNamespaceBinding[] result = new XmlNamespaceBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlNamespaceBinding binding = new XmlNamespaceBinding();
            binding.Prefix = source[index].Prefix;
            binding.NamespaceUri = source[index].NamespaceUri;
            binding.IsUndeclaration = source[index].IsUndeclaration;
            binding.Range = source[index].Range;
            result[index] = binding;
            index = index + 1;
        }

        return result;
    }

    internal string[] CopyXsltStrings(string[] source)
    {
        string[] result = new string[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal string[] AddUniqueXsltString(string[] source, string value)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == value)
            {
                return source;
            }

            index = index + 1;
        }

        string[] result = new string[source.Length + 1];
        Array.Copy(source, result, source.Length);
        result[source.Length] = value;
        return result;
    }

    internal bool IsValidXsltNcName(string value)
    {
        return new XdmStringDatatype().IsValidXdmStringLexical(value, XdmAtomicType.NcName);
    }

    internal bool IsXsltVersionOne(string version)
    {
        string value = version.Trim();
        int index = value.Length > 0 && value[0] == '+' ? 1 : 0;
        int decimalPoint = value.IndexOf('.', index);
        string integer = decimalPoint < 0 ? value.Substring(index) : value.Substring(index, decimalPoint - index);
        int first = 0;
        while (first < integer.Length - 1 && integer[first] == '0')
        {
            first = first + 1;
        }

        if (integer.Substring(first) != "1")
        {
            return false;
        }

        if (decimalPoint < 0)
        {
            return true;
        }

        index = decimalPoint + 1;
        while (index < value.Length)
        {
            if (value[index] != '0')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XsltStaticException CreateXsltStaticContextError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return Assembler.CreateXsltAssemblyError(message, code, node, module);
    }
}
