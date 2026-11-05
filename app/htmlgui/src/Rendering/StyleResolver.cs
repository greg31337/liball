using System;
using System.Collections.Generic;

namespace htmlgui;

internal enum StyleOrigin
{
    Global = 0,
    Window = 1,
    Page = 2,
    Component = 3,
    Control = 4,
    State = 5,
    Inline = 6
}

internal class ResolvedStyleEntry
{
    internal UiStyle Style = new UiStyle();
    internal StyleOrigin Origin;
    internal string ScopeId = string.Empty;
}

internal class StyleDeclarationState
{
    internal UiCssDeclaration Declaration;
    internal StyleOrigin Origin;

    internal StyleDeclarationState(UiCssDeclaration declaration, StyleOrigin origin)
    {
        Declaration = declaration;
        Origin = origin;
    }
}

internal class StyleResolution
{
    internal string ScopeId = string.Empty;
    internal ResolvedStyleEntry[] Styles = Array.Empty<ResolvedStyleEntry>();
    internal UiCssDeclaration[] Declarations = Array.Empty<UiCssDeclaration>();
    internal UiCssRule[] Rules = Array.Empty<UiCssRule>();
    internal UiResponsiveRule[] ResponsiveRules = Array.Empty<UiResponsiveRule>();
    internal DiagnosticEntry[] Diagnostics = Array.Empty<DiagnosticEntry>();
    internal bool Success;
}

internal class StyleResolver
{
    internal DiagnosticSink Diagnostics;
    internal List<ResolvedStyleEntry> ResolvedStyles;
    internal List<StyleDeclarationState> DeclarationStates;
    internal List<UiCssRule> ResolvedRules;
    internal List<UiResponsiveRule> ResolvedResponsiveRules;
    internal string RootSeed;
    internal string OwnerIdentity;

    internal StyleResolver(int maximumDiagnosticCount)
    {
        Diagnostics = new DiagnosticSink(maximumDiagnosticCount);
        ResolvedStyles = new List<ResolvedStyleEntry>();
        DeclarationStates = new List<StyleDeclarationState>();
        ResolvedRules = new List<UiCssRule>();
        ResolvedResponsiveRules = new List<UiResponsiveRule>();
        RootSeed = string.Empty;
        OwnerIdentity = string.Empty;
    }

    internal StyleResolution ResolveStyles(string? rootSeed, string? ownerIdentity, UiStyle[]? globalStyles, UiStyle[]? windowStyles, UiStyle[]? pageStyles, UiStyle[]? componentStyles, UiStyle[]? controlStyles, UiStyle[]? stateStyles, UiStyle? inlineStyle)
    {
        Diagnostics.Clear();
        ResolvedStyles.Clear();
        DeclarationStates.Clear();
        ResolvedRules.Clear();
        ResolvedResponsiveRules.Clear();
        RootSeed = CheckIdentityPart(rootSeed, "style.invalid-root-seed", "The style root seed cannot be empty.", "/root-seed", "root");
        OwnerIdentity = CheckIdentityPart(ownerIdentity, "style.invalid-owner", "The style owner identity cannot be empty.", "/owner-identity", "owner");

        AddStyleLayer(globalStyles, StyleOrigin.Global, "/styles/global");
        AddStyleLayer(windowStyles, StyleOrigin.Window, "/styles/window");
        AddStyleLayer(pageStyles, StyleOrigin.Page, "/styles/page");
        AddStyleLayer(componentStyles, StyleOrigin.Component, "/styles/component");
        AddStyleLayer(controlStyles, StyleOrigin.Control, "/styles/control");
        AddStyleLayer(stateStyles, StyleOrigin.State, "/styles/state");
        if (inlineStyle != null)
        {
            AddSingleStyle(inlineStyle, StyleOrigin.Inline, "/styles/inline");
        }

        StyleResolution result = new StyleResolution();
        result.Styles = ResolvedStyles.ToArray();
        result.Declarations = CreateCanonicalDeclarations();
        result.Rules = ResolvedRules.ToArray();
        result.ResponsiveRules = ResolvedResponsiveRules.ToArray();
        result.ScopeId = CreateScopeId(RootSeed, OwnerIdentity, "owner");
        if (ResolvedStyles.Count > 0)
        {
            result.ScopeId = ResolvedStyles[ResolvedStyles.Count - 1].ScopeId;
        }

        result.Diagnostics = Diagnostics.CreateSnapshot();
        result.Success = !Diagnostics.HasErrors();
        return result;
    }

    internal string CheckIdentityPart(string? value, string code, string message, string location, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddStyleError(code, message, location);
            return fallback;
        }

        return value;
    }

    internal void AddStyleLayer(UiStyle[]? styles, StyleOrigin origin, string location)
    {
        if (styles == null || styles.Length == 0)
        {
            return;
        }

        List<UiStyle> ordered = new List<UiStyle>();
        int index = 0;
        while (index < styles.Length)
        {
            UiStyle? style = styles[index];
            string styleLocation = location + "/" + InvariantFormatter.FormatInt32(index);
            if (style == null)
            {
                AddStyleError("style.null-style", "A style collection cannot contain null.", styleLocation);
            }
            else if (string.IsNullOrWhiteSpace(style.Key))
            {
                AddStyleError("style.invalid-key", "Style keys cannot be empty.", styleLocation + "/key");
            }
            else if (CountStyleKey(styles, style.Key) > 1)
            {
                if (IsFirstStyleKey(styles, index, style.Key))
                {
                    AddStyleError("style.duplicate-key", "Style keys must be unique within a precedence layer.", styleLocation + "/key");
                }
            }
            else
            {
                InsertStyleByKey(ordered, style);
            }

            index++;
        }

        index = 0;
        while (index < ordered.Count)
        {
            AddSingleStyle(ordered[index], origin, location + "/" + ordered[index].Key);
            index++;
        }
    }

    internal int CountStyleKey(UiStyle[] styles, string key)
    {
        int count = 0;
        int index = 0;
        while (index < styles.Length)
        {
            UiStyle? style = styles[index];
            if (style != null && string.Equals(style.Key, key, StringComparison.Ordinal))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal bool IsFirstStyleKey(UiStyle[] styles, int candidateIndex, string key)
    {
        int index = 0;
        while (index < candidateIndex)
        {
            UiStyle? style = styles[index];
            if (style != null && string.Equals(style.Key, key, StringComparison.Ordinal))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal void InsertStyleByKey(List<UiStyle> styles, UiStyle style)
    {
        int index = styles.Count;
        styles.Add(style);
        while (index > 0 && string.CompareOrdinal(styles[index - 1].Key, style.Key) > 0)
        {
            styles[index] = styles[index - 1];
            index--;
        }

        styles[index] = style;
    }

    internal void AddSingleStyle(UiStyle style, StyleOrigin origin, string location)
    {
        string scopeKey = style.Key;
        if (style.Scope != null)
        {
            if (string.IsNullOrWhiteSpace(style.Scope))
            {
                AddStyleError("style.invalid-scope", "An explicit style scope cannot be empty.", location + "/scope");
            }
            else
            {
                scopeKey = style.Scope;
            }
        }

        UiStyle copied = new UiStyle();
        copied.Key = style.Key;
        copied.Scope = style.Scope;
        copied.Declarations = CopyDeclarations(style.Declarations, origin, location + "/declarations");
        copied.Rules = CopyRules(style.Rules, location + "/rules");
        copied.ResponsiveRules = CopyResponsiveRules(style.ResponsiveRules, location + "/responsiveRules");
        ResolvedStyleEntry resolved = new ResolvedStyleEntry();
        resolved.Style = copied;
        resolved.Origin = origin;
        resolved.ScopeId = CreateScopeId(RootSeed, OwnerIdentity, GetStyleOriginToken(origin) + ":" + scopeKey);
        ResolvedStyles.Add(resolved);

        int index = 0;
        while (index < copied.Declarations.Length)
        {
            ApplyDeclaration(copied.Declarations[index], origin);
            index++;
        }

        index = 0;
        while (index < copied.Rules.Length)
        {
            ResolvedRules.Add(copied.Rules[index]);
            index++;
        }

        index = 0;
        while (index < copied.ResponsiveRules.Length)
        {
            ResolvedResponsiveRules.Add(copied.ResponsiveRules[index]);
            index++;
        }
    }

    internal UiCssDeclaration[] CopyDeclarations(UiCssDeclaration[]? declarations, StyleOrigin origin, string location)
    {
        if (declarations == null)
        {
            AddStyleError("style.null-declarations", "A style declaration collection cannot be null.", location);
            return Array.Empty<UiCssDeclaration>();
        }

        List<UiCssDeclaration> result = new List<UiCssDeclaration>();
        int index = 0;
        while (index < declarations.Length)
        {
            UiCssDeclaration? declaration = declarations[index];
            string declarationLocation = location + "/" + InvariantFormatter.FormatInt32(index);
            if (declaration == null)
            {
                AddStyleError("style.null-declaration", "A declaration collection cannot contain null.", declarationLocation);
            }
            else if (string.IsNullOrWhiteSpace(declaration.Property))
            {
                AddStyleError("style.invalid-property", "A declaration property cannot be empty.", declarationLocation + "/property");
            }
            else if (CountDeclarationProperty(declarations, declaration.Property) > 1)
            {
                if (IsFirstDeclarationProperty(declarations, index, declaration.Property))
                {
                    AddStyleError("style.duplicate-property", "Declaration properties must be unique within a style.", declarationLocation + "/property");
                }
            }
            else
            {
                UiCssDeclaration copied = CloneDeclaration(declaration);
                InsertDeclarationByProperty(result, copied);
            }

            index++;
        }

        return result.ToArray();
    }

    internal int CountDeclarationProperty(UiCssDeclaration[] declarations, string property)
    {
        int count = 0;
        int index = 0;
        while (index < declarations.Length)
        {
            UiCssDeclaration? declaration = declarations[index];
            if (declaration != null && string.Equals(declaration.Property, property, StringComparison.Ordinal))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal bool IsFirstDeclarationProperty(UiCssDeclaration[] declarations, int candidateIndex, string property)
    {
        int index = 0;
        while (index < candidateIndex)
        {
            UiCssDeclaration? declaration = declarations[index];
            if (declaration != null && string.Equals(declaration.Property, property, StringComparison.Ordinal))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal UiCssDeclaration CloneDeclaration(UiCssDeclaration declaration)
    {
        return new UiCssDeclaration
        {
            Property = declaration.Property,
            Value = declaration.Value,
            Important = declaration.Important,
            IsTrusted = declaration.IsTrusted
        };
    }

    internal void InsertDeclarationByProperty(List<UiCssDeclaration> declarations, UiCssDeclaration declaration)
    {
        int index = declarations.Count;
        declarations.Add(declaration);
        while (index > 0 && string.CompareOrdinal(declarations[index - 1].Property, declaration.Property) > 0)
        {
            declarations[index] = declarations[index - 1];
            index--;
        }

        declarations[index] = declaration;
    }

    internal void ApplyDeclaration(UiCssDeclaration declaration, StyleOrigin origin)
    {
        int index = 0;
        while (index < DeclarationStates.Count)
        {
            StyleDeclarationState existing = DeclarationStates[index];
            if (string.Equals(existing.Declaration.Property, declaration.Property, StringComparison.Ordinal))
            {
                if (!existing.Declaration.Important || declaration.Important)
                {
                    existing.Declaration = CloneDeclaration(declaration);
                    existing.Origin = origin;
                }

                return;
            }

            index++;
        }

        DeclarationStates.Add(new StyleDeclarationState(CloneDeclaration(declaration), origin));
    }

    internal UiCssDeclaration[] CreateCanonicalDeclarations()
    {
        List<UiCssDeclaration> result = new List<UiCssDeclaration>();
        int index = 0;
        while (index < DeclarationStates.Count)
        {
            InsertDeclarationByProperty(result, CloneDeclaration(DeclarationStates[index].Declaration));
            index++;
        }

        return result.ToArray();
    }

    internal UiCssRule[] CopyRules(UiCssRule[]? rules, string location)
    {
        if (rules == null)
        {
            AddStyleError("style.null-rules", "A style rule collection cannot be null.", location);
            return Array.Empty<UiCssRule>();
        }

        List<UiCssRule> result = new List<UiCssRule>();
        int index = 0;
        while (index < rules.Length)
        {
            UiCssRule? rule = rules[index];
            if (rule == null)
            {
                AddStyleError("style.null-rule", "A style rule collection cannot contain null.", location + "/" + InvariantFormatter.FormatInt32(index));
            }
            else
            {
                result.Add(CloneRule(rule));
            }

            index++;
        }

        return result.ToArray();
    }

    internal UiCssRule CloneRule(UiCssRule rule)
    {
        UiCssRule result = new UiCssRule();
        result.Selector = rule.Selector;
        result.Layer = rule.Layer;
        result.Declarations = CloneDeclarationArray(rule.Declarations);
        if (rule.Condition != null)
        {
            result.Condition = new UiCssCondition
            {
                Kind = rule.Condition.Kind,
                ContainerName = rule.Condition.ContainerName,
                Expression = rule.Condition.Expression
            };
        }

        return result;
    }

    internal UiCssDeclaration[] CloneDeclarationArray(UiCssDeclaration[]? declarations)
    {
        if (declarations == null)
        {
            return Array.Empty<UiCssDeclaration>();
        }

        UiCssDeclaration[] result = new UiCssDeclaration[declarations.Length];
        int index = 0;
        while (index < declarations.Length)
        {
            UiCssDeclaration? declaration = declarations[index];
            if (declaration == null)
            {
                result[index] = new UiCssDeclaration();
            }
            else
            {
                result[index] = CloneDeclaration(declaration);
            }

            index++;
        }

        return result;
    }

    internal UiResponsiveRule[] CopyResponsiveRules(UiResponsiveRule[]? rules, string location)
    {
        if (rules == null)
        {
            AddStyleError("style.null-responsive-rules", "A responsive rule collection cannot be null.", location);
            return Array.Empty<UiResponsiveRule>();
        }

        List<UiResponsiveRule> result = new List<UiResponsiveRule>();
        int index = 0;
        while (index < rules.Length)
        {
            UiResponsiveRule? rule = rules[index];
            if (rule == null)
            {
                AddStyleError("style.null-responsive-rule", "A responsive rule collection cannot contain null.", location + "/" + InvariantFormatter.FormatInt32(index));
            }
            else
            {
                result.Add(CloneResponsiveRule(rule));
            }

            index++;
        }

        return result.ToArray();
    }

    internal UiResponsiveRule CloneResponsiveRule(UiResponsiveRule rule)
    {
        UiResponsiveRule result = new UiResponsiveRule();
        result.Operator = rule.Operator;
        result.Value = rule.Value;
        result.IsContainer = rule.IsContainer;
        result.Declarations = CloneDeclarationArray(rule.Declarations);
        if (rule.Length != null)
        {
            result.Length = new UiLength
            {
                HasValue = rule.Length.HasValue,
                Value = rule.Length.Value,
                Unit = rule.Length.Unit,
                Expression = rule.Length.Expression
            };
        }

        return result;
    }

    internal string CreateScopeId(string? rootSeed, string? ownerIdentity, string? scopeKey)
    {
        string seed = Guard.ArgumentNotWhiteSpace(rootSeed, "rootSeed");
        string owner = Guard.ArgumentNotWhiteSpace(ownerIdentity, "ownerIdentity");
        string scope = Guard.ArgumentNotWhiteSpace(scopeKey, "scopeKey");
        string hash = StableHash.HashParts(new string[] { seed, owner, scope });
        return "htmlgui-scope-" + hash.Substring(0, 24);
    }

    internal string GetStyleOriginToken(StyleOrigin origin)
    {
        switch (origin)
        {
            case StyleOrigin.Global:
                return "global";
            case StyleOrigin.Window:
                return "window";
            case StyleOrigin.Page:
                return "page";
            case StyleOrigin.Component:
                return "component";
            case StyleOrigin.Control:
                return "control";
            case StyleOrigin.State:
                return "state";
            case StyleOrigin.Inline:
                return "inline";
            default:
                throw new ArgumentOutOfRangeException("origin", origin, "The style origin is not supported.");
        }
    }

    internal void AddStyleError(string code, string message, string location)
    {
        Diagnostics.Add(DiagnosticLevel.Error, code, message, location);
    }
}
