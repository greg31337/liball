using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltPackageCompiler
{
    internal XsltStylesheetLoader Loader;
    internal XsltPackageVersionRules VersionRules;
    internal XsltStylesheetPlan? Stylesheet;

    internal XsltPackageCompiler(XsltStylesheetLoader loader)
    {
        Loader = loader;
        VersionRules = new XsltPackageVersionRules();
        Stylesheet = null;
    }

    internal XsltPackagePlan CompileXsltPackage(XsltStylesheetPlan stylesheet)
    {
        Stylesheet = stylesheet;
        XsltPackagePlan package = CreateXsltPackageMetadata(stylesheet);
        EnterXsltPackageCompilation(package, stylesheet.PrincipalModule.Root);
        List<XsltComponent> local = CollectXsltLocalComponents(stylesheet, package);
        ApplyXsltCompiledModeMetadata(local, stylesheet, package);
        AddImplicitXsltUnnamedMode(local, stylesheet.PrincipalModule.Root, package);
        ApplyXsltExposeDeclarations(stylesheet, package, local);
        package.LocalComponents = local.ToArray();

        List<XsltComponent> all = new List<XsltComponent>(local);
        List<XsltPackageUse> uses = ProcessXsltUsedPackages(stylesheet, package, all);
        package.Components = all.ToArray();
        package.Uses = uses.ToArray();
        BindXsltPackageReferences(package);
        package.IsExecutable = !ContainsXsltAbstractComponent(package.Components);
        if (!package.IsLibrary && !package.IsExecutable)
        {
            throw CreateXsltPackageError("A top-level package must not contain abstract components.", XsltErrorCodes.XTSE3080, stylesheet.PrincipalModule.Root, stylesheet.PrincipalModule);
        }

        LeaveXsltPackageCompilation(package);
        return package;
    }

    internal XsltPackagePlan CreateXsltPackageMetadata(XsltStylesheetPlan stylesheet)
    {
        XsltPackagePlan package = new XsltPackagePlan();
        XsltStylesheetModule principal = stylesheet.PrincipalModule;
        package.IsExplicit = principal.Kind == XsltStylesheetModuleKind.Package;
        package.IsLibrary = Loader.IsLibraryPackage;
        package.IsReusable = Loader.Options.CompilePackagesSeparately;
        if (package.IsExplicit)
        {
            string? nameValue = Loader.FindXsltAttribute(principal.Root, "name", "");
            if (nameValue != null && nameValue.Trim().Length != 0)
            {
                string name = nameValue.Trim();
                if (!Uri.IsWellFormedUriString(name, UriKind.Absolute))
                {
                    throw CreateXsltPackageError("An XSLT package name must be an absolute URI.", XsltErrorCodes.XTSE0020, principal.Root, principal);
                }

                package.Name = name;
            }

            string? versionValue = Loader.FindXsltAttribute(principal.Root, "package-version", "");
            package.Version = versionValue == null ? "1" : versionValue.Trim();
            if (VersionRules.ParseXsltPackageVersion(package.Version) == null)
            {
                throw CreateXsltPackageError("An XSLT package version is not valid.", XsltErrorCodes.XTSE0020, principal.Root, principal);
            }
        }
        else
        {
            package.Name = ReadXsltConfiguredPackageName(Loader.Options.PackageName);
            package.Version = "1";
        }

        XsltPackageMetadataPlan? metadata = stylesheet.Metadata;
        if (metadata != null)
        {
            package.Id = metadata.Id;
            package.DeclaredModes = metadata.DeclaredModes;
            package.InputTypeAnnotations = metadata.InputTypeAnnotations;
            package.EffectiveInputTypeAnnotations = metadata.EffectiveInputTypeAnnotations;
            package.DefaultMode = metadata.DefaultMode;
            package.DefaultValidation = metadata.DefaultValidation;
            package.DefaultCollation = metadata.DefaultCollation;
        }

        return package;
    }

    internal string? ReadXsltConfiguredPackageName(XmlName? name)
    {
        if (name == null)
        {
            return null;
        }

        if (name.NamespaceUri != null && name.NamespaceUri.Length != 0)
        {
            return name.NamespaceUri;
        }

        if (name.QualifiedName != null && name.QualifiedName.Length != 0)
        {
            return name.QualifiedName;
        }

        return name.LocalName;
    }

    internal void EnterXsltPackageCompilation(XsltPackagePlan package, XmlNode node)
    {
        string? name = package.Name;
        if (name == null)
        {
            return;
        }

        string identity = name + "|" + VersionRules.CreateXsltPackageVersionIdentity(package.Version);
        if (ContainsXsltPackageIdentity(Loader.PackageContext.ActivePackageIdentities, identity))
        {
            throw CreateXsltPackageError("An XSLT package must not depend on itself.", XsltErrorCodes.XTSE3005, node, Loader.Modules[0]);
        }

        Loader.PackageContext.ActivePackageIdentities.Add(identity);
    }

    internal void LeaveXsltPackageCompilation(XsltPackagePlan package)
    {
        string? name = package.Name;
        if (name == null)
        {
            return;
        }

        string identity = name + "|" + VersionRules.CreateXsltPackageVersionIdentity(package.Version);
        int index = Loader.PackageContext.ActivePackageIdentities.Count - 1;
        while (index >= 0)
        {
            if (Loader.PackageContext.ActivePackageIdentities[index] == identity)
            {
                Loader.PackageContext.ActivePackageIdentities.RemoveAt(index);
                return;
            }

            index = index - 1;
        }
    }

    internal bool ContainsXsltPackageIdentity(List<string> identities, string identity)
    {
        int index = 0;
        while (index < identities.Count)
        {
            if (identities[index] == identity)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal List<XsltComponent> CollectXsltLocalComponents(XsltStylesheetPlan stylesheet, XsltPackagePlan package)
    {
        List<XsltComponent> result = new List<XsltComponent>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "use-package" && declaration.Level != stylesheet.RootLevel)
            {
                throw CreateXsltPackageError("An xsl:use-package declaration must be in the principal stylesheet level.", XsltErrorCodes.XTSE3008, declaration.Node, declaration.Module);
            }

            XsltComponent? component = CreateXsltDeclaredComponent(declaration.Node, declaration.LocalName, package, package);
            if (component != null)
            {
                component.ImportPrecedence = declaration.ImportPrecedence;
                component.DeclarationOrder = declaration.DeclarationOrder;
                AddOrReplaceXsltLocalComponent(result, component);
            }

            index = index + 1;
        }

        return result;
    }

    internal XsltComponent? CreateXsltDeclaredComponent(XmlNode node, string localName, XsltPackagePlan declaringPackage, XsltPackagePlan containingPackage)
    {
        XsltComponentKind kind;
        string? lexicalName = Loader.FindXsltAttribute(node, "name", "");
        int arity = -1;
        if (localName == "template")
        {
            if (lexicalName == null || lexicalName.Trim().Length == 0)
            {
                return null;
            }

            kind = XsltComponentKind.Template;
        }
        else if (localName == "function")
        {
            kind = XsltComponentKind.Function;
            arity = CountXsltFunctionParameters(node);
        }
        else if (localName == "attribute-set")
        {
            kind = XsltComponentKind.AttributeSet;
        }
        else if (localName == "variable" || localName == "param")
        {
            kind = XsltComponentKind.Variable;
        }
        else if (localName == "mode")
        {
            kind = XsltComponentKind.Mode;
            if (lexicalName == null || lexicalName.Trim().Length == 0)
            {
                lexicalName = "#unnamed";
            }
        }
        else
        {
            return null;
        }

        if (kind == XsltComponentKind.Function && IsXsltFunctionOverriddenByExtension(node))
        {
            return null;
        }

        if (lexicalName == null || lexicalName.Trim().Length == 0)
        {
            throw CreateXsltPackageError("A named XSLT component requires a name.", XsltErrorCodes.XTSE0010, node, FindXsltNodeModule(node));
        }

        string name = lexicalName == "#unnamed" ? "#unnamed" : ResolveXsltComponentName(lexicalName.Trim(), node);
        string identifier = CreateXsltSymbolicIdentifier(kind, name, arity);
        XsltComponent component = new XsltComponent(kind, name, arity, identifier, node, declaringPackage, containingPackage);
        component.IsParameter = localName == "param";
        ReadXsltDeclaredVisibility(component, localName, declaringPackage.IsExplicit);
        return component;
    }

    internal bool IsXsltFunctionOverriddenByExtension(XmlNode node)
    {
        XsltStylesheetPlan? stylesheet = Stylesheet;
        if (stylesheet == null)
        {
            return false;
        }

        int index = 0;
        while (index < stylesheet.Functions.Length)
        {
            XsltFunctionPlan function = stylesheet.Functions[index];
            if (object.ReferenceEquals(function.Declaration, node))
            {
                return function.IsOverriddenByExtension;
            }

            index = index + 1;
        }

        return false;
    }

    internal int CountXsltFunctionParameters(XmlNode function)
    {
        XmlNode[] children = function.Children == null ? [] : function.Children;
        int count = 0;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "param")
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal void ReadXsltDeclaredVisibility(XsltComponent component, string declarationName, bool explicitPackage)
    {
        string? value = Loader.FindXsltAttribute(component.Declaration, "visibility", "");
        string? staticValue = Loader.FindXsltAttribute(component.Declaration, "static", "");
        component.IsStatic = (declarationName == "variable" || declarationName == "param") && staticValue != null &&
            (staticValue.Trim() == "yes" || staticValue.Trim() == "true" || staticValue.Trim() == "1");
        if (component.IsParameter)
        {
            if (value != null)
            {
                throw CreateXsltPackageError("An xsl:param declaration cannot specify visibility.", XsltErrorCodes.XTSE0020, component.Declaration, FindXsltNodeModule(component.Declaration));
            }

            component.Visibility = component.IsStatic ? XsltComponentVisibility.Private : XsltComponentVisibility.Public;
            component.DeclaredVisibility = component.Visibility;
            component.HasDeclaredVisibility = true;
            return;
        }

        if (value != null)
        {
            component.DeclaredVisibility = ParseXsltVisibility(value, false, component.Declaration);
            component.Visibility = component.DeclaredVisibility;
            component.HasDeclaredVisibility = true;
        }
        else if (!explicitPackage && (declarationName == "template" || declarationName == "mode"))
        {
            component.Visibility = XsltComponentVisibility.Public;
        }
        else
        {
            component.Visibility = XsltComponentVisibility.Private;
        }

        if (component.Kind == XsltComponentKind.Mode && (component.Name == "#unnamed" && component.Visibility != XsltComponentVisibility.Private || component.Visibility == XsltComponentVisibility.Abstract))
        {
            throw CreateXsltPackageError("An unnamed mode must be private and a named mode cannot be abstract.", XsltErrorCodes.XTSE0020, component.Declaration, FindXsltNodeModule(component.Declaration));
        }

        if (component.IsStatic && component.Visibility != XsltComponentVisibility.Private)
        {
            throw CreateXsltPackageError("A static variable or parameter must have private visibility.", XsltErrorCodes.XTSE0020, component.Declaration, FindXsltNodeModule(component.Declaration));
        }
    }

    internal void AddOrReplaceXsltLocalComponent(List<XsltComponent> components, XsltComponent component)
    {
        int index = 0;
        while (index < components.Count)
        {
            XsltComponent existing = components[index];
            if (existing.SymbolicIdentifier == component.SymbolicIdentifier)
            {
                if (component.ImportPrecedence > existing.ImportPrecedence || component.ImportPrecedence == existing.ImportPrecedence && component.DeclarationOrder >= existing.DeclarationOrder)
                {
                    components[index] = component;
                }

                return;
            }

            index = index + 1;
        }

        components.Add(component);
    }

    internal void ApplyXsltCompiledModeMetadata(List<XsltComponent> components, XsltStylesheetPlan stylesheet, XsltPackagePlan package)
    {
        int modeIndex = 0;
        while (modeIndex < stylesheet.Modes.Length)
        {
            XsltModePlan mode = stylesheet.Modes[modeIndex];
            XsltComponent? component = null;
            int componentIndex = 0;
            while (componentIndex < components.Count)
            {
                if (components[componentIndex].Kind == XsltComponentKind.Mode && components[componentIndex].Name == mode.Name)
                {
                    component = components[componentIndex];
                    break;
                }

                componentIndex = componentIndex + 1;
            }

            if (component == null)
            {
                string identifier = CreateXsltSymbolicIdentifier(XsltComponentKind.Mode, mode.Name, -1);
                component = new XsltComponent(XsltComponentKind.Mode, mode.Name, -1, identifier, mode.Origin, package, package);
                component.ImportPrecedence = mode.ImportPrecedence;
                component.DeclarationOrder = mode.DeclarationOrder;
                components.Add(component);
            }
            else
            {
                component.Declaration = mode.Origin;
                component.ImportPrecedence = mode.ImportPrecedence;
                component.DeclarationOrder = mode.DeclarationOrder;
            }

            if (mode.HasDeclaredVisibility)
            {
                component.HasDeclaredVisibility = true;
                component.DeclaredVisibility = mode.DeclaredVisibility;
                component.Visibility = mode.DeclaredVisibility;
            }

            component.Mode = mode;
            mode.Component = component;
            modeIndex = modeIndex + 1;
        }
    }

    internal void AddImplicitXsltUnnamedMode(List<XsltComponent> components, XmlNode node, XsltPackagePlan package)
    {
        string identifier = CreateXsltSymbolicIdentifier(XsltComponentKind.Mode, "#unnamed", -1);
        int index = 0;
        while (index < components.Count)
        {
            if (components[index].SymbolicIdentifier == identifier)
            {
                components[index].Visibility = XsltComponentVisibility.Private;
                return;
            }

            index = index + 1;
        }

        XsltComponent component = new XsltComponent(XsltComponentKind.Mode, "#unnamed", -1, identifier, node, package, package);
        component.Visibility = XsltComponentVisibility.Private;
        components.Add(component);
    }

    internal void ApplyXsltExposeDeclarations(XsltStylesheetPlan stylesheet, XsltPackagePlan package, List<XsltComponent> components)
    {
        List<XsltVisibilityRule> rules = new List<XsltVisibilityRule>();
        int index = 0;
        while (index < stylesheet.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.Declarations[index];
            if (declaration.LocalName == "expose")
            {
                AddXsltVisibilityRules(rules, declaration.Node, false);
            }

            index = index + 1;
        }

        index = 0;
        while (index < components.Count)
        {
            XsltComponent component = components[index];
            if (!component.IsParameter && component.Name != "#unnamed")
            {
                XsltVisibilityRule? selected = SelectXsltVisibilityRule(rules, component);
                if (selected != null)
                {
                    bool applies = selected.Rank == 3 || !component.HasDeclaredVisibility;
                    if (applies && selected.Visibility == XsltComponentVisibility.Abstract && component.Visibility != XsltComponentVisibility.Abstract)
                    {
                        throw CreateXsltPackageError("An xsl:expose rule cannot make a concrete component abstract.", XsltErrorCodes.XTSE3025, selected.Node, stylesheet.PrincipalModule);
                    }

                    if (selected.Rank == 3 && component.HasDeclaredVisibility && !IsCompatibleXsltExposedVisibility(component.DeclaredVisibility, selected.Visibility))
                    {
                        throw CreateXsltPackageError("The exposed visibility is inconsistent with the component declaration.", XsltErrorCodes.XTSE3010, selected.Node, stylesheet.PrincipalModule);
                    }
                    else if (applies)
                    {
                        component.Visibility = selected.Visibility;
                    }
                }
            }

            index = index + 1;
        }

        ValidateXsltUnmatchedVisibilityRules(rules, XsltErrorCodes.XTSE3020, stylesheet.PrincipalModule);
    }

    internal void AddXsltVisibilityRules(List<XsltVisibilityRule> rules, XmlNode node, bool accepting)
    {
        string? componentValue = Loader.FindXsltAttribute(node, "component", "");
        string? namesValue = Loader.FindXsltAttribute(node, "names", "");
        string? visibilityValue = Loader.FindXsltAttribute(node, "visibility", "");
        if (componentValue == null || namesValue == null || visibilityValue == null)
        {
            throw CreateXsltPackageError("A package visibility declaration requires component, names, and visibility attributes.", XsltErrorCodes.XTSE0010, node, FindXsltNodeModule(node));
        }

        string componentText = componentValue.Trim();
        bool allKinds = componentText == "*";
        XsltComponentKind kind = allKinds ? XsltComponentKind.Template : ParseXsltComponentKind(componentText, node);
        XsltComponentVisibility visibility = ParseXsltVisibility(visibilityValue, accepting, node);
        string[] selectors = namesValue.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (selectors.Length == 0)
        {
            throw CreateXsltPackageError("A package visibility declaration requires at least one component selector.", XsltErrorCodes.XTSE0010, node, FindXsltNodeModule(node));
        }

        int index = 0;
        while (index < selectors.Length)
        {
            XsltVisibilityRule rule = CreateXsltVisibilityRule(node, kind, allKinds, selectors[index], visibility);
            if (allKinds && !rule.IsWildcard)
            {
                string code = accepting ? XsltErrorCodes.XTSE3032 : XsltErrorCodes.XTSE3022;
                throw CreateXsltPackageError("A visibility declaration for all component kinds requires a wildcard name.", code, node, FindXsltNodeModule(node));
            }

            rules.Add(rule);
            index = index + 1;
        }
    }

    internal XsltVisibilityRule CreateXsltVisibilityRule(XmlNode node, XsltComponentKind kind, bool allKinds, string selector, XsltComponentVisibility visibility)
    {
        XsltVisibilityRule result = new XsltVisibilityRule(node);
        result.Kind = kind;
        result.AllKinds = allKinds;
        result.Visibility = visibility;
        string value = selector.Trim();
        if (value == "*")
        {
            result.Selector = "*";
            result.IsWildcard = true;
            result.Rank = 1;
            return result;
        }

        if (value.StartsWith("*:", StringComparison.Ordinal))
        {
            string local = value.Substring(2);
            if (!new XdmStringDatatype().IsValidXdmName(local, true))
            {
                throw CreateXsltPackageError("A component wildcard contains an invalid local name.", XsltErrorCodes.XTSE0010, node, FindXsltNodeModule(node));
            }

            result.Selector = "*:" + local;
            result.IsWildcard = true;
            result.Rank = 2;
            return result;
        }

        if (value.EndsWith(":*", StringComparison.Ordinal))
        {
            string prefix = value.Substring(0, value.Length - 2);
            string namespaceUri = ResolveXsltNamespacePrefix(prefix, node);
            result.Selector = "Q{" + namespaceUri + "}*";
            result.IsWildcard = true;
            result.Rank = 2;
            return result;
        }

        if (value.StartsWith("Q{", StringComparison.Ordinal) && value.EndsWith("}*", StringComparison.Ordinal))
        {
            result.Selector = value;
            result.IsWildcard = true;
            result.Rank = 2;
            return result;
        }

        int arity = -1;
        string nameText = value;
        int hash = value.LastIndexOf('#');
        if (hash >= 0)
        {
            if (!allKinds && kind != XsltComponentKind.Function)
            {
                throw CreateXsltPackageError("Only a function selector can specify an arity.", XsltErrorCodes.XTSE0010, node, FindXsltNodeModule(node));
            }

            nameText = value.Substring(0, hash);
            arity = ParseXsltNonnegativeInteger(value.Substring(hash + 1), node);
        }

        result.Selector = ResolveXsltComponentName(nameText, node);
        if (arity >= 0)
        {
            result.Selector = result.Selector + "#" + arity;
        }

        result.Rank = 3;
        return result;
    }

    internal XsltVisibilityRule? SelectXsltVisibilityRule(List<XsltVisibilityRule> rules, XsltComponent component)
    {
        XsltVisibilityRule? selected = null;
        int index = 0;
        while (index < rules.Count)
        {
            XsltVisibilityRule rule = rules[index];
            if (MatchesXsltVisibilityRule(rule, component))
            {
                rule.Matched = true;
                if (selected == null || rule.Rank >= selected.Rank)
                {
                    selected = rule;
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal bool MatchesXsltVisibilityRule(XsltVisibilityRule rule, XsltComponent component)
    {
        if (!rule.AllKinds && rule.Kind != component.Kind)
        {
            return false;
        }

        if (rule.Selector == "*")
        {
            return true;
        }

        if (rule.Selector.StartsWith("*:", StringComparison.Ordinal))
        {
            return ReadXsltExpandedLocalName(component.Name) == rule.Selector.Substring(2);
        }

        if (rule.Selector.StartsWith("Q{", StringComparison.Ordinal) && rule.Selector.EndsWith("}*", StringComparison.Ordinal))
        {
            int close = rule.Selector.LastIndexOf('}');
            string namespaceUri = rule.Selector.Substring(2, close - 2);
            return ReadXsltExpandedNamespace(component.Name) == namespaceUri;
        }

        if (rule.Selector.IndexOf('#') >= 0)
        {
            return rule.Selector == component.Name + "#" + component.Arity;
        }

        return rule.Selector == component.Name;
    }

    internal void ValidateXsltUnmatchedVisibilityRules(List<XsltVisibilityRule> rules, string code, XsltStylesheetModule module)
    {
        int index = 0;
        while (index < rules.Count)
        {
            XsltVisibilityRule rule = rules[index];
            if (!rule.IsWildcard && !rule.Matched)
            {
                throw CreateXsltPackageError("An explicit component selector does not match a component.", code, rule.Node, module);
            }

            index = index + 1;
        }
    }

    internal bool IsCompatibleXsltExposedVisibility(XsltComponentVisibility declared, XsltComponentVisibility exposed)
    {
        if (exposed == XsltComponentVisibility.Public)
        {
            return declared == XsltComponentVisibility.Public;
        }

        if (exposed == XsltComponentVisibility.Private)
        {
            return declared == XsltComponentVisibility.Public || declared == XsltComponentVisibility.Private || declared == XsltComponentVisibility.Final;
        }

        if (exposed == XsltComponentVisibility.Final)
        {
            return declared == XsltComponentVisibility.Public || declared == XsltComponentVisibility.Final;
        }

        return declared == XsltComponentVisibility.Abstract && exposed == XsltComponentVisibility.Abstract;
    }

    internal List<XsltPackageUse> ProcessXsltUsedPackages(XsltStylesheetPlan stylesheet, XsltPackagePlan package, List<XsltComponent> all)
    {
        List<XsltPackageUse> uses = new List<XsltPackageUse>();
        int index = 0;
        while (index < stylesheet.RootLevel.Declarations.Length)
        {
            XsltStylesheetDeclaration declaration = stylesheet.RootLevel.Declarations[index];
            if (declaration.LocalName == "use-package")
            {
                XsltPackageUse use = CompileXsltUsedPackage(declaration, package, all);
                uses.Add(use);
            }

            index = index + 1;
        }

        return uses;
    }

    internal XsltPackageUse CompileXsltUsedPackage(XsltStylesheetDeclaration declaration, XsltPackagePlan package, List<XsltComponent> all)
    {
        XmlNode node = declaration.Node;
        string? nameValue = Loader.FindXsltAttribute(node, "name", "");
        if (nameValue == null || nameValue.Trim().Length == 0)
        {
            throw CreateXsltPackageError("An xsl:use-package declaration requires a package name.", XsltErrorCodes.XTSE0010, node, declaration.Module);
        }

        string name = nameValue.Trim();
        if (!Uri.IsWellFormedUriString(name, UriKind.Absolute))
        {
            throw CreateXsltPackageError("An xsl:use-package name must be an absolute URI.", XsltErrorCodes.XTSE0020, node, declaration.Module);
        }

        string? rangeValue = Loader.FindXsltAttribute(node, "package-version", "");
        string range = rangeValue == null ? "*" : rangeValue.Trim();
        if (!VersionRules.IsValidXsltPackageVersionRange(range))
        {
            throw CreateXsltPackageError("An xsl:use-package package-version range is invalid.", XsltErrorCodes.XTSE0020, node, declaration.Module);
        }

        XsltExecutable executable = ResolveXsltUsedPackageExecutable(name, range, declaration);
        XsltStylesheetPlan? targetStylesheet = executable.CompiledPlan == null ? executable.Plan as XsltStylesheetPlan : executable.CompiledPlan;
        XsltPackagePlan? target = targetStylesheet == null ? null : targetStylesheet.Package;
        if (target == null || !target.IsExplicit || target.Name != name || !VersionRules.MatchesXsltPackageVersionRange(target.Version, range))
        {
            throw CreateXsltPackageError("The resolved package does not match the requested name and version.", XsltErrorCodes.XTSE3000, node, declaration.Module);
        }

        XsltPackageUse use = new XsltPackageUse(node, name, range, target);
        if (Loader.Options.CompilePackagesSeparately)
        {
            use.SeparateExecutable = executable;
        }

        List<XsltVisibilityRule> accepts = new List<XsltVisibilityRule>();
        List<XmlNode> overrides = new List<XmlNode>();
        ReadXsltUsePackageChildren(node, declaration.Module, accepts, overrides);
        List<XsltComponent> accepted = CopyXsltAcceptedComponents(target, package, accepts, declaration.Module, node);
        ApplyXsltOverrides(overrides, accepts, accepted, all, package, declaration.Module, node);
        MergeXsltAcceptedComponents(accepted, all, declaration.Module, node);
        use.AcceptedComponents = accepted.ToArray();
        return use;
    }

    internal XsltExecutable ResolveXsltUsedPackageExecutable(string name, string range, XsltStylesheetDeclaration declaration)
    {
        XsltPackageReference? selected = SelectXsltPackageReference(name, range);
        if (selected == null || selected.LocationUri == null || selected.LocationUri.Trim().Length == 0)
        {
            throw CreateXsltPackageError("No package matching the requested name and version could be located.", XsltErrorCodes.XTSE3000, declaration.Node, declaration.Module);
        }

        string selectedVersion = selected.Version == null || selected.Version.Trim().Length == 0 ? "1" : selected.Version.Trim();
        string cacheKey = name + "|" + VersionRules.CreateXsltPackageVersionIdentity(selectedVersion) + "|" + selected.LocationUri.Trim();
        if (Loader.PackageContext.Executables.ContainsKey(cacheKey))
        {
            return Loader.PackageContext.Executables[cacheKey];
        }

        XmlResolvedResource resource = ResolveXsltPackageResource(selected.LocationUri.Trim(), declaration);
        XmlInput? optionalInput = resource.Input;
        if (optionalInput == null || resource.AbsoluteUri == null || resource.AbsoluteUri.Length == 0)
        {
            throw CreateXsltPackageError("The package resolver did not provide package input and an absolute URI.", XsltErrorCodes.XTSE3000, declaration.Node, declaration.Module);
        }

        XmlInput input = Loader.CopyXsltInput(optionalInput, resource.AbsoluteUri);
        if (resource.OwnsResource)
        {
            input.OwnsResource = true;
        }

        XsltStylesheetLoader dependencyLoader = new XsltStylesheetLoader(Loader.Options, Loader.Resolver, Loader.PackageContext, true);
        XsltExecutable executable = dependencyLoader.CompileXsltStylesheet(input);
        Loader.PackageContext.Executables.Add(cacheKey, executable);
        return executable;
    }

    internal XsltPackageReference? SelectXsltPackageReference(string name, string range)
    {
        XsltPackageReference[] packages = Loader.Options.Packages == null ? [] : Loader.Options.Packages;
        XsltPackageReference? selected = null;
        XsltPackageVersion? selectedVersion = null;
        int index = 0;
        while (index < packages.Length)
        {
            XsltPackageReference candidate = packages[index];
            string? candidateName = ReadXsltConfiguredPackageName(candidate.Name);
            string candidateVersionText = candidate.Version == null || candidate.Version.Trim().Length == 0 ? "1" : candidate.Version.Trim();
            XsltPackageVersion? candidateVersion = VersionRules.ParseXsltPackageVersion(candidateVersionText);
            if (candidateName == name && candidateVersion != null && VersionRules.MatchesXsltPackageVersionRange(candidateVersionText, range))
            {
                if (selected == null || selectedVersion == null || VersionRules.CompareXsltPackageVersions(candidateVersion, selectedVersion) > 0)
                {
                    selected = candidate;
                    selectedVersion = candidateVersion;
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal XmlResolvedResource ResolveXsltPackageResource(string location, XsltStylesheetDeclaration declaration)
    {
        string? baseUri = Loader.ReadXsltNodeBaseUri(declaration.Node, declaration.Module.BaseUri);
        Task<XmlResolvedResource> pending = Task.Run(() => ResolveXsltPackageResourceCore(location, baseUri));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion || pending.Result == null)
        {
            throw CreateXsltPackageError("The referenced XSLT package could not be retrieved.", XsltErrorCodes.XTSE3000, declaration.Node, declaration.Module);
        }

        return pending.Result;
    }

    internal XmlResolvedResource ResolveXsltPackageResourceCore(string location, string? baseUri)
    {
        using (ResolverGuardScope scope = Loader.ResourceGuard.BeginXmlResource(location, baseUri))
        {
            XmlResolvedResource resource = Loader.Resolver.ResolveXmlResource(XmlResourceKind.Package, null, location, baseUri, Loader.Policy);
            Loader.ResourceGuard.ValidateXmlResolvedResource(scope, XmlResourceKind.Package, resource);
            return resource;
        }
    }

    internal void ReadXsltUsePackageChildren(XmlNode use, XsltStylesheetModule module, List<XsltVisibilityRule> accepts, List<XmlNode> overrides)
    {
        XmlNode[] children = use.Children == null ? [] : use.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                string namespaceUri = Loader.XsltNodeNamespace(child);
                string localName = Loader.XsltNodeLocalName(child);
                if (namespaceUri != XsltNamespaces.Xslt || localName != "accept" && localName != "override")
                {
                    throw CreateXsltPackageError("An xsl:use-package element may contain only xsl:accept and xsl:override elements.", XsltErrorCodes.XTSE0010, child, module);
                }

                if (localName == "accept")
                {
                    AddXsltVisibilityRules(accepts, child, true);
                }
                else
                {
                    overrides.Add(child);
                }
            }
            else if ((child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value))
            {
                throw CreateXsltPackageError("An xsl:use-package element must not contain text.", XsltErrorCodes.XTSE0010, child, module);
            }

            index = index + 1;
        }
    }

    internal List<XsltComponent> CopyXsltAcceptedComponents(XsltPackagePlan target, XsltPackagePlan package, List<XsltVisibilityRule> accepts, XsltStylesheetModule module, XmlNode useDeclaration)
    {
        List<XsltComponent> result = new List<XsltComponent>();
        int index = 0;
        while (index < target.Components.Length)
        {
            XsltComponent source = target.Components[index];
            XsltComponent copy = CopyXsltUsedComponent(source, package, useDeclaration);
            XsltVisibilityRule? rule = source.IsParameter ? null : SelectXsltAcceptedVisibilityRule(accepts, source, module);
            if (rule == null)
            {
                copy.Visibility = DefaultXsltAcceptedVisibility(source);
            }
            else
            {
                copy.Visibility = rule.Visibility;
            }

            result.Add(copy);
            index = index + 1;
        }

        ValidateXsltUnmatchedVisibilityRules(accepts, XsltErrorCodes.XTSE3030, module);
        return result;
    }

    internal XsltVisibilityRule? SelectXsltAcceptedVisibilityRule(List<XsltVisibilityRule> rules, XsltComponent component, XsltStylesheetModule module)
    {
        XsltVisibilityRule? selected = null;
        int index = 0;
        while (index < rules.Count)
        {
            XsltVisibilityRule rule = rules[index];
            if (MatchesXsltVisibilityRule(rule, component))
            {
                rule.Matched = true;
                if (IsCompatibleXsltAcceptedVisibility(component.Visibility, rule.Visibility))
                {
                    if (selected == null || rule.Rank >= selected.Rank)
                    {
                        selected = rule;
                    }
                }
                else if (!rule.IsWildcard)
                {
                    throw CreateXsltPackageError("The accepted visibility is incompatible with the used component.", XsltErrorCodes.XTSE3040, rule.Node, module);
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal XsltComponent CopyXsltUsedComponent(XsltComponent source, XsltPackagePlan containingPackage, XmlNode useDeclaration)
    {
        XsltComponent result = new XsltComponent(source.Kind, source.Name, source.Arity, source.SymbolicIdentifier, source.Declaration, source.DeclaringPackage, containingPackage);
        result.Visibility = source.Visibility;
        result.DeclaredVisibility = source.DeclaredVisibility;
        result.HasDeclaredVisibility = source.HasDeclaredVisibility;
        result.IsParameter = source.IsParameter;
        result.IsStatic = source.IsStatic;
        result.UseDeclaration = useDeclaration;
        result.CorrespondingComponent = source;
        result.ImportPrecedence = source.ImportPrecedence;
        result.DeclarationOrder = source.DeclarationOrder;
        result.Function = source.Function;
        result.Mode = source.Mode;
        result.AttributeSet = source.AttributeSet;
        result.Template = source.Template;
        result.Variable = source.Variable;
        return result;
    }

    internal XsltComponentVisibility DefaultXsltAcceptedVisibility(XsltComponent component)
    {
        if (component.IsParameter && !component.IsStatic)
        {
            return XsltComponentVisibility.Public;
        }

        if (component.Visibility == XsltComponentVisibility.Public || component.Visibility == XsltComponentVisibility.Final)
        {
            return XsltComponentVisibility.Private;
        }

        return XsltComponentVisibility.Hidden;
    }

    internal bool IsCompatibleXsltAcceptedVisibility(XsltComponentVisibility supplied, XsltComponentVisibility accepted)
    {
        if (supplied == XsltComponentVisibility.Public)
        {
            return accepted == XsltComponentVisibility.Public || accepted == XsltComponentVisibility.Private || accepted == XsltComponentVisibility.Final || accepted == XsltComponentVisibility.Hidden;
        }

        if (supplied == XsltComponentVisibility.Final)
        {
            return accepted == XsltComponentVisibility.Private || accepted == XsltComponentVisibility.Final || accepted == XsltComponentVisibility.Hidden;
        }

        if (supplied == XsltComponentVisibility.Abstract)
        {
            return accepted == XsltComponentVisibility.Abstract || accepted == XsltComponentVisibility.Hidden;
        }

        return false;
    }

    internal void ApplyXsltOverrides(List<XmlNode> overrides, List<XsltVisibilityRule> accepts, List<XsltComponent> accepted, List<XsltComponent> local, XsltPackagePlan package, XsltStylesheetModule module, XmlNode useDeclaration)
    {
        int overrideIndex = 0;
        while (overrideIndex < overrides.Count)
        {
            XmlNode container = overrides[overrideIndex];
            XmlNode[] children = container.Children == null ? [] : container.Children;
            int childIndex = 0;
            while (childIndex < children.Length)
            {
                XmlNode child = children[childIndex];
                if (child.Kind == XmlNodeKind.Element)
                {
                    string localName = Loader.XsltNodeLocalName(child);
                    bool permitted = Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && (localName == "template" || localName == "function" || localName == "variable" || localName == "param" || localName == "attribute-set");
                    if (!permitted)
                    {
                        throw CreateXsltPackageError("An xsl:override element contains an unsupported declaration.", XsltErrorCodes.XTSE0010, child, module);
                    }

                    XsltComponent? overriding = CreateXsltDeclaredComponent(child, localName, package, package);
                    if (overriding == null)
                    {
                        string? match = Loader.FindXsltAttribute(child, "match", "");
                        if (localName == "template" && match != null && match.Trim().Length != 0)
                        {
                            childIndex = childIndex + 1;
                            continue;
                        }

                        throw CreateXsltPackageError("An overriding component must be named.", XsltErrorCodes.XTSE0010, child, module);
                    }

                    XsltComponent? target = FindXsltOverriddenComponent(accepted, overriding.SymbolicIdentifier);
                    if (target == null)
                    {
                        throw CreateXsltPackageError("An overriding declaration does not match a component in the used package.", XsltErrorCodes.XTSE3058, child, module);
                    }

                    XsltComponent? originalTarget = target.CorrespondingComponent == null ? target : target.CorrespondingComponent;
                    if (originalTarget.Visibility != XsltComponentVisibility.Public && originalTarget.Visibility != XsltComponentVisibility.Abstract)
                    {
                        throw CreateXsltPackageError("Only a public or abstract component can be overridden.", XsltErrorCodes.XTSE3060, child, module);
                    }

                    if (!HaveCompatibleXsltComponentSignatures(overriding, originalTarget))
                    {
                        throw CreateXsltPackageError("The overriding component has an incompatible signature.", XsltErrorCodes.XTSE3070, child, module);
                    }

                    if (ContainsNonhiddenXsltComponent(local, overriding.SymbolicIdentifier) || ContainsXsltOverrideComponent(accepted, overriding.SymbolicIdentifier))
                    {
                        throw CreateXsltPackageError("An overriding component conflicts with another declaration in the using package.", XsltErrorCodes.XTSE3055, child, module);
                    }

                    if (MatchesExplicitXsltAccept(accepts, overriding))
                    {
                        throw CreateXsltPackageError("A component cannot be both explicitly accepted and overridden.", XsltErrorCodes.XTSE3051, child, module);
                    }

                    target.Visibility = XsltComponentVisibility.Hidden;
                    overriding.IsOverride = true;
                    overriding.UseDeclaration = useDeclaration;
                    overriding.OriginalComponent = target;
                    accepted.Add(overriding);
                }
                else if ((child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && !new XsltStylesheetAssembler(Loader).IsXsltWhitespace(child.Value))
                {
                    throw CreateXsltPackageError("An xsl:override element must not contain text.", XsltErrorCodes.XTSE0010, child, module);
                }

                childIndex = childIndex + 1;
            }

            overrideIndex = overrideIndex + 1;
        }
    }

    internal XsltComponent? FindXsltOverriddenComponent(List<XsltComponent> components, string identifier)
    {
        int index = components.Count - 1;
        while (index >= 0)
        {
            XsltComponent component = components[index];
            if (component.SymbolicIdentifier == identifier)
            {
                return component;
            }

            index = index - 1;
        }

        return null;
    }

    internal bool ContainsNonhiddenXsltComponent(List<XsltComponent> components, string identifier)
    {
        int index = 0;
        while (index < components.Count)
        {
            if (components[index].SymbolicIdentifier == identifier && components[index].Visibility != XsltComponentVisibility.Hidden)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsXsltOverrideComponent(List<XsltComponent> components, string identifier)
    {
        int index = 0;
        while (index < components.Count)
        {
            if (components[index].IsOverride && components[index].SymbolicIdentifier == identifier)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool MatchesExplicitXsltAccept(List<XsltVisibilityRule> rules, XsltComponent component)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (!rules[index].IsWildcard && MatchesXsltVisibilityRule(rules[index], component))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HaveCompatibleXsltComponentSignatures(XsltComponent overriding, XsltComponent overridden)
    {
        if (overriding.Kind != overridden.Kind || overriding.Arity != overridden.Arity)
        {
            return false;
        }

        if (overriding.Kind == XsltComponentKind.Template)
        {
            return HaveCompatibleXsltTemplateSignatures(overriding.Declaration, overridden.Declaration);
        }

        if (overriding.Kind == XsltComponentKind.Function)
        {
            return HaveCompatibleXsltFunctionSignatures(overriding.Declaration, overridden.Declaration);
        }

        if (overriding.Kind == XsltComponentKind.AttributeSet)
        {
            string originalStreamability = ReadXsltSignatureAttribute(overridden.Declaration, "streamable", "no");
            return originalStreamability != "yes" || ReadXsltSignatureAttribute(overriding.Declaration, "streamable", "no") == "yes";
        }

        string left = CreateXsltComponentSignature(overriding);
        string right = CreateXsltComponentSignature(overridden);
        return left == right;
    }

    internal bool HaveCompatibleXsltFunctionSignatures(XmlNode overriding, XmlNode overridden)
    {
        if (ReadXsltSignatureAttribute(overriding, "as", "item()*") != ReadXsltSignatureAttribute(overridden, "as", "item()*"))
        {
            return false;
        }

        if (ReadXsltSignatureAttribute(overriding, "new-each-time", "yes") != ReadXsltSignatureAttribute(overridden, "new-each-time", "yes"))
        {
            return false;
        }

        if (ReadXsltSignatureAttribute(overridden, "streamable", "no") == "yes" && ReadXsltSignatureAttribute(overriding, "streamable", "no") != "yes")
        {
            return false;
        }

        XmlNode[] replacementParameters = ReadXsltTemplateParameters(overriding);
        XmlNode[] originalParameters = ReadXsltTemplateParameters(overridden);
        int index = 0;
        while (index < originalParameters.Length)
        {
            if (ReadXsltSignatureAttribute(replacementParameters[index], "as", "item()*") != ReadXsltSignatureAttribute(originalParameters[index], "as", "item()*"))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool HaveCompatibleXsltTemplateSignatures(XmlNode overriding, XmlNode overridden)
    {
        if (ReadXsltSignatureAttribute(overriding, "as", "item()*") != ReadXsltSignatureAttribute(overridden, "as", "item()*"))
        {
            return false;
        }

        if (ReadXsltTemplateContextSignature(overriding) != ReadXsltTemplateContextSignature(overridden))
        {
            return false;
        }

        XmlNode[] originalParameters = ReadXsltTemplateParameters(overridden);
        XmlNode[] replacementParameters = ReadXsltTemplateParameters(overriding);
        int index = 0;
        while (index < originalParameters.Length)
        {
            XmlNode original = originalParameters[index];
            string? originalNameValue = Loader.FindXsltAttribute(original, "name", "");
            string originalName = originalNameValue == null ? "" : ResolveXsltComponentName(originalNameValue, original);
            XmlNode? replacement = FindXsltTemplateParameter(replacementParameters, originalName);
            bool originalTunnel = ReadXsltSignatureAttribute(original, "tunnel", "no") == "yes";
            if (!originalTunnel && replacement == null)
            {
                return false;
            }

            if (replacement != null)
            {
                bool replacementTunnel = ReadXsltSignatureAttribute(replacement, "tunnel", "no") == "yes";
                if (replacementTunnel != originalTunnel || ReadXsltSignatureAttribute(replacement, "as", "item()*") != ReadXsltSignatureAttribute(original, "as", "item()*"))
                {
                    return false;
                }

                if (!originalTunnel && ReadXsltSignatureAttribute(replacement, "required", "no") != ReadXsltSignatureAttribute(original, "required", "no"))
                {
                    return false;
                }
            }

            index = index + 1;
        }

        index = 0;
        while (index < replacementParameters.Length)
        {
            XmlNode replacement = replacementParameters[index];
            string? replacementNameValue = Loader.FindXsltAttribute(replacement, "name", "");
            string replacementName = replacementNameValue == null ? "" : ResolveXsltComponentName(replacementNameValue, replacement);
            if (FindXsltTemplateParameter(originalParameters, replacementName) == null && ReadXsltSignatureAttribute(replacement, "required", "no") != "no")
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string ReadXsltTemplateContextSignature(XmlNode template)
    {
        XmlNode[] children = template.Children == null ? [] : template.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "context-item")
            {
                return ReadXsltSignatureAttribute(child, "as", "item()") + "|" + ReadXsltSignatureAttribute(child, "use", "optional");
            }

            index = index + 1;
        }

        return "item()|optional";
    }

    internal XmlNode[] ReadXsltTemplateParameters(XmlNode template)
    {
        List<XmlNode> result = new List<XmlNode>();
        XmlNode[] children = template.Children == null ? [] : template.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "param")
            {
                result.Add(child);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XmlNode? FindXsltTemplateParameter(XmlNode[] parameters, string name)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            string? value = Loader.FindXsltAttribute(parameters[index], "name", "");
            if (value != null && ResolveXsltComponentName(value, parameters[index]) == name)
            {
                return parameters[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal string CreateXsltComponentSignature(XsltComponent component)
    {
        XmlNode node = component.Declaration;
        string signature = ReadXsltSignatureAttribute(node, "as", "item()*");
        if (component.Kind == XsltComponentKind.Function)
        {
            signature = signature + "|new=" + ReadXsltSignatureAttribute(node, "new-each-time", "yes") + "|stream=" + ReadXsltSignatureAttribute(node, "streamable", "no");
        }
        else if (component.Kind == XsltComponentKind.AttributeSet)
        {
            signature = "stream=" + ReadXsltSignatureAttribute(node, "streamable", "no");
        }

        if (component.Kind == XsltComponentKind.Function || component.Kind == XsltComponentKind.Template)
        {
            XmlNode[] children = node.Children == null ? [] : node.Children;
            int index = 0;
            while (index < children.Length)
            {
                XmlNode child = children[index];
                if (child.Kind == XmlNodeKind.Element && Loader.XsltNodeNamespace(child) == XsltNamespaces.Xslt && Loader.XsltNodeLocalName(child) == "param")
                {
                    string? parameterName = Loader.FindXsltAttribute(child, "name", "");
                    string name = parameterName == null ? "" : ResolveXsltComponentName(parameterName, child);
                    if (component.Kind == XsltComponentKind.Function)
                    {
                        signature = signature + "|param=" + ReadXsltSignatureAttribute(child, "as", "item()*");
                    }
                    else
                    {
                        signature = signature + "|param=" + name + ":" + ReadXsltSignatureAttribute(child, "as", "item()*") + ":" + ReadXsltSignatureAttribute(child, "required", "no") + ":" + ReadXsltSignatureAttribute(child, "tunnel", "no");
                    }
                }

                index = index + 1;
            }
        }

        return signature;
    }

    internal string ReadXsltSignatureAttribute(XmlNode node, string localName, string defaultValue)
    {
        string? value = Loader.FindXsltAttribute(node, localName, "");
        return value == null ? defaultValue : value.Trim();
    }

    internal void MergeXsltAcceptedComponents(List<XsltComponent> accepted, List<XsltComponent> all, XsltStylesheetModule module, XmlNode useNode)
    {
        int index = 0;
        while (index < accepted.Count)
        {
            XsltComponent component = accepted[index];
            if (component.Visibility != XsltComponentVisibility.Hidden && ContainsNonhiddenXsltComponent(all, component.SymbolicIdentifier))
            {
                throw CreateXsltPackageError("Used packages contribute conflicting visible components.", XsltErrorCodes.XTSE3050, useNode, module);
            }

            all.Add(component);
            index = index + 1;
        }
    }

    internal void BindXsltPackageReferences(XsltPackagePlan package)
    {
        int index = 0;
        while (index < package.Components.Length)
        {
            XsltComponent component = package.Components[index];
            XsltComponent? corresponding = component.CorrespondingComponent;
            if (corresponding == null)
            {
                component.References = CollectXsltSymbolicReferences(component);
            }
            else
            {
                component.References = CopyXsltCorrespondingReferences(corresponding.References);
            }

            int referenceIndex = 0;
            while (referenceIndex < component.References.Length)
            {
                XsltSymbolicReference reference = component.References[referenceIndex];
                if (corresponding != null)
                {
                    XsltComponent? sourceTarget = corresponding.References[referenceIndex].Target;
                    reference.Target = sourceTarget == null ? null : FindXsltCopiedBindingTarget(package.Components, sourceTarget, component.UseDeclaration);
                }
                else if (reference.Name == "Q{" + XsltNamespaces.Xslt + "}original")
                {
                    if (component.OriginalComponent == null || reference.Kind != component.Kind)
                    {
                        throw CreateXsltPackageError("xsl:original is available only for the overridden component kind.", XsltErrorCodes.XTSE0010, reference.Node, FindXsltNodeModule(reference.Node));
                    }

                    XsltComponent original = component.OriginalComponent;
                    XsltComponent? declared = original.CorrespondingComponent == null ? original : original.CorrespondingComponent;
                    if (declared.Visibility == XsltComponentVisibility.Abstract)
                    {
                        throw CreateXsltPackageError("xsl:original cannot refer to an abstract component.", XsltErrorCodes.XTSE3075, reference.Node, FindXsltNodeModule(reference.Node));
                    }

                    reference.Target = original;
                }
                else
                {
                    reference.Target = FindXsltBindingTarget(package.Components, reference);
                }

                referenceIndex = referenceIndex + 1;
            }

            index = index + 1;
        }
    }

    internal XsltSymbolicReference[] CopyXsltCorrespondingReferences(XsltSymbolicReference[] source)
    {
        List<XsltSymbolicReference> result = new List<XsltSymbolicReference>();
        int index = 0;
        while (index < source.Length)
        {
            XsltSymbolicReference reference = source[index];
            result.Add(new XsltSymbolicReference(reference.Kind, reference.Name, reference.Arity, reference.Node));
            index = index + 1;
        }

        return result.ToArray();
    }

    internal XsltComponent? FindXsltCopiedBindingTarget(XsltComponent[] components, XsltComponent sourceTarget, XmlNode? useDeclaration)
    {
        int index = components.Length - 1;
        while (index >= 0)
        {
            XsltComponent candidate = components[index];
            XsltComponent? original = candidate.OriginalComponent;
            if (candidate.UseDeclaration == useDeclaration && candidate.IsOverride && original != null && (original == sourceTarget || original.CorrespondingComponent == sourceTarget))
            {
                return candidate;
            }

            index = index - 1;
        }

        index = components.Length - 1;
        while (index >= 0)
        {
            XsltComponent candidate = components[index];
            if (candidate.UseDeclaration == useDeclaration && candidate.CorrespondingComponent == sourceTarget)
            {
                return candidate;
            }

            index = index - 1;
        }

        return null;
    }

    internal XsltSymbolicReference[] CollectXsltSymbolicReferences(XsltComponent component)
    {
        List<XsltSymbolicReference> result = new List<XsltSymbolicReference>();
        if (component.Kind == XsltComponentKind.Mode)
        {
            return result.ToArray();
        }

        CollectXsltSymbolicReferencesInTree(component.Declaration, result);
        AddXsltOriginalExpressionReferences(component.Declaration, component, result);
        return result.ToArray();
    }

    internal void AddXsltOriginalExpressionReferences(XmlNode node, XsltComponent component, List<XsltSymbolicReference> result)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                string? optionalValue = attributes[attributeIndex].Value;
                string value = optionalValue == null ? "" : optionalValue;
                if (ContainsXsltOriginalXPathReference(value, component, node))
                {
                    int arity = component.Kind == XsltComponentKind.Function ? component.Arity : -1;
                    result.Add(new XsltSymbolicReference(component.Kind, "Q{" + XsltNamespaces.Xslt + "}original", arity, node));
                }

                attributeIndex = attributeIndex + 1;
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            AddXsltOriginalExpressionReferences(children[index], component, result);
            index = index + 1;
        }
    }

    internal bool ContainsXsltOriginalXPathReference(string value, XsltComponent component, XmlNode node)
    {
        if (component.Kind != XsltComponentKind.Variable && component.Kind != XsltComponentKind.Function)
        {
            return false;
        }

        bool possibleVariable = component.Kind == XsltComponentKind.Variable && value.IndexOf("original", StringComparison.Ordinal) >= 0;
        bool possibleFunction = component.Kind == XsltComponentKind.Function && value.IndexOf("original", StringComparison.Ordinal) >= 0;
        if (!possibleVariable && !possibleFunction)
        {
            return false;
        }

        XPathToken[] tokens = new XPathLexer(value, Loader.ReadXsltNodeBaseUri(node, Loader.Modules[0].BaseUri)).TokenizeXPath();
        int index = 0;
        while (index < tokens.Length)
        {
            if (possibleVariable && tokens[index].Kind == XPathTokenKind.Dollar && index + 1 < tokens.Length && IsXsltOriginalXPathName(tokens[index + 1], node))
            {
                return true;
            }

            if (possibleFunction && IsXsltOriginalXPathName(tokens[index], node) && index + 1 < tokens.Length && (tokens[index + 1].Kind == XPathTokenKind.LeftParenthesis || tokens[index + 1].Kind == XPathTokenKind.Hash))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXsltOriginalXPathName(XPathToken token, XmlNode node)
    {
        if (token.LocalName != "original")
        {
            return false;
        }

        if (token.NamespaceUri != null)
        {
            return token.NamespaceUri == XsltNamespaces.Xslt;
        }

        if (token.Prefix == null || token.Prefix.Length == 0)
        {
            return false;
        }

        return ResolveXsltNamespacePrefix(token.Prefix, node) == XsltNamespaces.Xslt;
    }

    internal void CollectXsltSymbolicReferencesInTree(XmlNode node, List<XsltSymbolicReference> result)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            string namespaceUri = Loader.XsltNodeNamespace(node);
            string localName = Loader.XsltNodeLocalName(node);
            if (namespaceUri == XsltNamespaces.Xslt && localName == "call-template")
            {
                AddXsltNamedReference(result, XsltComponentKind.Template, Loader.FindXsltAttribute(node, "name", ""), -1, node);
            }
            else if (namespaceUri == XsltNamespaces.Xslt && localName == "apply-templates")
            {
                string? mode = Loader.FindXsltAttribute(node, "mode", "");
                if (mode == null || mode.Trim().Length == 0 || mode.Trim() == "#default")
                {
                    result.Add(new XsltSymbolicReference(XsltComponentKind.Mode, FindXsltDefaultMode(node), -1, node));
                }
                else if (mode.Trim() != "#current")
                {
                    AddXsltNamedReference(result, XsltComponentKind.Mode, mode, -1, node);
                }
            }

            XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                XmlNode attribute = attributes[attributeIndex];
                if (Loader.XsltNodeLocalName(attribute) == "use-attribute-sets" && (Loader.XsltNodeNamespace(attribute).Length == 0 || Loader.XsltNodeNamespace(attribute) == XsltNamespaces.Xslt))
                {
                    string[] names = (attribute.Value == null ? "" : attribute.Value).Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                    int nameIndex = 0;
                    while (nameIndex < names.Length)
                    {
                        XsltStylesheetModule module = FindXsltNodeModule(node);
                        string name = new XsltAttributeSetCompiler(Loader).ResolveXsltAttributeSetName(names[nameIndex], node, module);
                        result.Add(new XsltSymbolicReference(XsltComponentKind.AttributeSet, name, -1, node));
                        nameIndex = nameIndex + 1;
                    }
                }

                attributeIndex = attributeIndex + 1;
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            CollectXsltSymbolicReferencesInTree(children[index], result);
            index = index + 1;
        }
    }

    internal string FindXsltDefaultMode(XmlNode node)
    {
        XsltStylesheetPlan? stylesheet = Stylesheet;
        if (stylesheet == null)
        {
            return "#unnamed";
        }

        int index = 0;
        while (index < stylesheet.StaticContexts.Length)
        {
            XsltStaticContext context = stylesheet.StaticContexts[index];
            if (object.ReferenceEquals(context.Node, node))
            {
                return context.DefaultMode;
            }

            index = index + 1;
        }

        return "#unnamed";
    }

    internal void AddXsltNamedReference(List<XsltSymbolicReference> references, XsltComponentKind kind, string? lexicalName, int arity, XmlNode node)
    {
        if (lexicalName == null || lexicalName.Trim().Length == 0)
        {
            return;
        }

        string name = lexicalName.Trim() == "#unnamed" ? "#unnamed" : ResolveXsltComponentName(lexicalName.Trim(), node);
        references.Add(new XsltSymbolicReference(kind, name, arity, node));
    }

    internal XsltComponent? FindXsltBindingTarget(XsltComponent[] components, XsltSymbolicReference reference)
    {
        int index = components.Length - 1;
        while (index >= 0)
        {
            XsltComponent component = components[index];
            if (component.Visibility != XsltComponentVisibility.Hidden && component.Kind == reference.Kind && component.Name == reference.Name && (reference.Arity < 0 || component.Arity == reference.Arity))
            {
                return component;
            }

            index = index - 1;
        }

        return null;
    }

    internal bool ContainsXsltAbstractComponent(XsltComponent[] components)
    {
        int index = 0;
        while (index < components.Length)
        {
            if (components[index].Visibility == XsltComponentVisibility.Abstract)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltComponentKind ParseXsltComponentKind(string value, XmlNode node)
    {
        string normalized = value.Trim();
        if (normalized == "template")
        {
            return XsltComponentKind.Template;
        }

        if (normalized == "function")
        {
            return XsltComponentKind.Function;
        }

        if (normalized == "attribute-set")
        {
            return XsltComponentKind.AttributeSet;
        }

        if (normalized == "variable")
        {
            return XsltComponentKind.Variable;
        }

        if (normalized == "mode")
        {
            return XsltComponentKind.Mode;
        }

        throw CreateXsltPackageError("The package component kind is not recognized.", XsltErrorCodes.XTSE0020, node, FindXsltNodeModule(node));
    }

    internal XsltComponentVisibility ParseXsltVisibility(string value, bool allowHidden, XmlNode node)
    {
        string normalized = value.Trim();
        if (normalized == "public")
        {
            return XsltComponentVisibility.Public;
        }

        if (normalized == "private")
        {
            return XsltComponentVisibility.Private;
        }

        if (normalized == "final")
        {
            return XsltComponentVisibility.Final;
        }

        if (normalized == "abstract")
        {
            return XsltComponentVisibility.Abstract;
        }

        if (allowHidden && normalized == "hidden")
        {
            return XsltComponentVisibility.Hidden;
        }

        throw CreateXsltPackageError("The component visibility is not recognized.", XsltErrorCodes.XTSE0020, node, FindXsltNodeModule(node));
    }

    internal int ParseXsltNonnegativeInteger(string value, XmlNode node)
    {
        if (value.Length == 0)
        {
            throw CreateXsltPackageError("A function selector requires a nonnegative arity.", XsltErrorCodes.XTSE0010, node, FindXsltNodeModule(node));
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9' || result > 214748364)
            {
                throw CreateXsltPackageError("A function selector contains an invalid arity.", XsltErrorCodes.XTSE0010, node, FindXsltNodeModule(node));
            }

            result = result * 10 + character - '0';
            index = index + 1;
        }

        return result;
    }

    internal string ResolveXsltComponentName(string lexicalName, XmlNode node)
    {
        string value = lexicalName.Trim();
        if (value.StartsWith("Q{", StringComparison.Ordinal))
        {
            int close = value.IndexOf('}');
            if (close < 2 || close + 1 >= value.Length)
            {
                throw CreateXsltPackageError("A component name contains an invalid EQName.", XsltErrorCodes.XTSE0280, node, FindXsltNodeModule(node));
            }

            string namespaceUri = value.Substring(2, close - 2);
            string local = value.Substring(close + 1);
            if (!new XdmStringDatatype().IsValidXdmName(local, true))
            {
                throw CreateXsltPackageError("A component name contains an invalid EQName local part.", XsltErrorCodes.XTSE0280, node, FindXsltNodeModule(node));
            }

            return "Q{" + namespaceUri + "}" + local;
        }

        int colon = value.IndexOf(':');
        if (colon < 0)
        {
            if (!new XdmStringDatatype().IsValidXdmName(value, true))
            {
                throw CreateXsltPackageError("A component name must be a valid EQName.", XsltErrorCodes.XTSE0280, node, FindXsltNodeModule(node));
            }

            return "Q{}" + value;
        }

        string prefix = value.Substring(0, colon);
        string localName = value.Substring(colon + 1);
        if (!new XdmStringDatatype().IsValidXdmName(prefix, true) || !new XdmStringDatatype().IsValidXdmName(localName, true) || localName.IndexOf(':') >= 0)
        {
            throw CreateXsltPackageError("A component name must be a valid lexical QName.", XsltErrorCodes.XTSE0280, node, FindXsltNodeModule(node));
        }

        return "Q{" + ResolveXsltNamespacePrefix(prefix, node) + "}" + localName;
    }

    internal string ResolveXsltNamespacePrefix(string prefix, XmlNode node)
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
                string? optionalNamespace = bindings[index].NamespaceUri;
                if (bindingPrefix == prefix && !bindings[index].IsUndeclaration && optionalNamespace != null)
                {
                    return optionalNamespace;
                }

                index = index - 1;
            }

            current = current.Parent;
        }

        throw CreateXsltPackageError("A component name uses an undeclared namespace prefix.", XsltErrorCodes.XTSE0280, node, FindXsltNodeModule(node));
    }

    internal string CreateXsltSymbolicIdentifier(XsltComponentKind kind, string name, int arity)
    {
        string result = ((int)kind).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + name;
        if (kind == XsltComponentKind.Function)
        {
            result = result + "|" + arity.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return result;
    }

    internal string ReadXsltExpandedNamespace(string name)
    {
        if (!name.StartsWith("Q{", StringComparison.Ordinal))
        {
            return "";
        }

        int close = name.IndexOf('}');
        return close < 0 ? "" : name.Substring(2, close - 2);
    }

    internal string ReadXsltExpandedLocalName(string name)
    {
        if (!name.StartsWith("Q{", StringComparison.Ordinal))
        {
            return name;
        }

        int close = name.IndexOf('}');
        return close < 0 ? name : name.Substring(close + 1);
    }

    internal XsltStylesheetModule FindXsltNodeModule(XmlNode node)
    {
        XmlDocument? document = node.OwnerDocument;
        int index = 0;
        while (index < Loader.Modules.Count)
        {
            if (Loader.Modules[index].Document == document)
            {
                return Loader.Modules[index];
            }

            index = index + 1;
        }

        return Loader.Modules[0];
    }

    internal XsltStaticException CreateXsltPackageError(string message, string code, XmlNode node, XsltStylesheetModule module)
    {
        return new XsltStylesheetAssembler(Loader).CreateXsltAssemblyError(message, code, node, module);
    }
}
