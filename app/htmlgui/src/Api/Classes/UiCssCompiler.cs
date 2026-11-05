using System;
using System.Collections.Generic;

namespace htmlgui;

/// <summary>Compiles themes, scoped styles, responsive rules, and canonical stylesheets.</summary>
public class UiCssCompiler : IUiCssCompiler
{
    internal ThemeResolver ThemeEngine;
    internal StyleResolver StyleEngine;
    internal ResponsiveRuleCompiler ResponsiveEngine;
    internal CssSerializer Serializer;

    /// <summary>Initializes a CSS compiler with bounded resolution diagnostics.</summary>
    public UiCssCompiler()
    {
        ThemeEngine = new ThemeResolver(100);
        StyleEngine = new StyleResolver(100);
        ResponsiveEngine = new ResponsiveRuleCompiler();
        Serializer = new CssSerializer();
    }

    /// <summary>Resolves a theme variant, token references, and host token overrides.</summary>
    public UiCssCompilation ResolveTheme(UiTheme? theme, UiThemeMode hostMode, string? requestedVariantKey, UiValueEntry[]? hostOverrides)
    {
        ThemeResolution resolution = ThemeEngine.ResolveTheme(theme, hostMode, requestedVariantKey, hostOverrides);
        UiCssCompilation result = new UiCssCompilation();
        result.Theme = new UiTheme
        {
            Key = resolution.ThemeKey,
            Mode = resolution.Mode,
            Tokens = CloneValueEntries(resolution.Tokens)
        };
        result.VariantKey = resolution.VariantKey;
        result.Diagnostics = ConvertDiagnostics(resolution.Diagnostics);
        result.Success = resolution.Success;
        if (!resolution.Success)
        {
            return result;
        }

        result.Declarations = Serializer.CreateCustomPropertyDeclarations(result.Theme.Tokens, result.Theme.Key);
        if (result.Declarations.Length > 0)
        {
            result.Rules = new UiCssRule[]
            {
                new UiCssRule
                {
                    Selector = ":root",
                    Declarations = CloneDeclarations(result.Declarations)
                }
            };
        }

        return result;
    }

    /// <summary>Resolves style precedence and generates rules constrained to a deterministic owner scope.</summary>
    public UiCssCompilation GenerateScopedRules(string? rootSeed, string? ownerIdentity, UiStyle[]? globalStyles, UiStyle[]? windowStyles, UiStyle[]? pageStyles, UiStyle[]? componentStyles, UiStyle[]? controlStyles, UiStyle[]? stateStyles, UiStyle? inlineStyle)
    {
        StyleResolution resolution = StyleEngine.ResolveStyles(rootSeed, ownerIdentity, globalStyles, windowStyles, pageStyles, componentStyles, controlStyles, stateStyles, inlineStyle);
        UiCssCompilation result = new UiCssCompilation();
        result.ScopeId = resolution.ScopeId;
        result.Declarations = CloneDeclarations(resolution.Declarations);
        result.ResponsiveRules = CloneResponsiveRules(resolution.ResponsiveRules);
        result.Diagnostics = ConvertDiagnostics(resolution.Diagnostics);
        result.Success = resolution.Success;
        if (!resolution.Success)
        {
            return result;
        }

        string scopeSelector = "." + CssEscaper.EscapeIdentifier(result.ScopeId);
        List<UiCssRule> rules = new List<UiCssRule>();
        if (result.Declarations.Length > 0)
        {
            AddCanonicalRule(rules, new UiCssRule { Selector = scopeSelector, Declarations = CloneDeclarations(result.Declarations) });
        }

        int index = 0;
        while (index < resolution.Rules.Length)
        {
            AddCanonicalRule(rules, CreateScopedRule(resolution.Rules[index], scopeSelector));
            index++;
        }

        UiCssRule[] responsive = ResponsiveEngine.CompileRules(result.ResponsiveRules, scopeSelector);
        index = 0;
        while (index < responsive.Length)
        {
            AddCanonicalRule(rules, CloneRule(responsive[index]));
            index++;
        }

        result.Rules = rules.ToArray();
        return result;
    }

    /// <summary>Generates canonical conditional rules for responsive descriptions.</summary>
    public UiCssCompilation GenerateResponsiveRules(UiResponsiveRule[]? responsiveRules, string? selector)
    {
        if (responsiveRules == null)
        {
            throw new ArgumentNullException("responsiveRules");
        }

        UiCssCompilation result = new UiCssCompilation();
        result.Rules = ResponsiveEngine.CompileRules(responsiveRules, selector);
        result.ResponsiveRules = CloneResponsiveRules(responsiveRules);
        result.Success = true;
        return result;
    }

    /// <summary>Serializes, interns, orders, and deduplicates a complete stylesheet.</summary>
    public UiCssCompilation SerializeStylesheet(UiCssRule[]? rules)
    {
        if (rules == null)
        {
            throw new ArgumentNullException("rules");
        }

        CssSerializationResult serialized = Serializer.SerializeStylesheet(rules);
        UiCssCompilation result = new UiCssCompilation();
        result.Css = serialized.Css;
        result.Hash = serialized.Hash;
        result.InputRuleCount = serialized.InputRuleCount;
        result.Rules = CreateCanonicalRules(rules);
        result.Success = true;
        return result;
    }

    internal UiCssRule[] CreateCanonicalRules(UiCssRule[] rules)
    {
        List<UiCssRule> result = new List<UiCssRule>();
        int index = 0;
        while (index < rules.Length)
        {
            AddCanonicalRule(result, CloneRule(rules[index]));
            index++;
        }

        return result.ToArray();
    }

    internal UiCssRule CreateScopedRule(UiCssRule rule, string scopeSelector)
    {
        UiCssRule result = CloneRule(rule);
        string nestedSelector = CssEscaper.EscapeSelector(result.Selector);
        result.Selector = ":where(" + scopeSelector + ") :is(" + nestedSelector + ")";
        return result;
    }

    internal void AddCanonicalRule(List<UiCssRule> rules, UiCssRule rule)
    {
        string serialized = Serializer.SerializeRule(rule);
        int index = 0;
        while (index < rules.Count)
        {
            int comparison = string.CompareOrdinal(Serializer.SerializeRule(rules[index]), serialized);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                break;
            }

            index++;
        }

        rules.Insert(index, rule);
    }

    internal UiCssRule CloneRule(UiCssRule rule)
    {
        UiCssRule result = new UiCssRule();
        result.Selector = CssEscaper.EscapeSelector(rule.Selector);
        result.Layer = rule.Layer;
        result.Declarations = CloneDeclarations(rule.Declarations);
        if (rule.Condition != null)
        {
            result.Condition = new UiCssCondition
            {
                Kind = rule.Condition.Kind,
                ContainerName = rule.Condition.ContainerName,
                Expression = rule.Condition.Expression.Trim()
            };
        }

        return result;
    }

    internal UiCssDeclaration[] CloneDeclarations(UiCssDeclaration[] declarations)
    {
        Serializer.SerializeDeclarations(declarations);
        List<UiCssDeclaration> result = new List<UiCssDeclaration>();
        int index = 0;
        while (index < declarations.Length)
        {
            UiCssDeclaration declaration = declarations[index];
            if (FindCanonicalDeclaration(result, declaration.Property) == null)
            {
                AddCanonicalDeclaration(result, new UiCssDeclaration
                {
                    Property = declaration.Property,
                    Value = declaration.Value.Trim(),
                    Important = declaration.Important,
                    IsTrusted = declaration.IsTrusted
                });
            }

            index++;
        }

        return result.ToArray();
    }

    internal UiCssDeclaration? FindCanonicalDeclaration(List<UiCssDeclaration> declarations, string property)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (string.Equals(declarations[index].Property, property, StringComparison.Ordinal))
            {
                return declarations[index];
            }

            index++;
        }

        return null;
    }

    internal void AddCanonicalDeclaration(List<UiCssDeclaration> declarations, UiCssDeclaration declaration)
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

    internal UiResponsiveRule[] CloneResponsiveRules(UiResponsiveRule[] rules)
    {
        UiResponsiveRule[] result = new UiResponsiveRule[rules.Length];
        int index = 0;
        while (index < rules.Length)
        {
            UiResponsiveRule rule = rules[index];
            UiResponsiveRule copied = new UiResponsiveRule();
            copied.Operator = rule.Operator;
            copied.Value = rule.Value;
            copied.IsContainer = rule.IsContainer;
            copied.Declarations = CloneDeclarations(rule.Declarations);
            if (rule.Length != null)
            {
                copied.Length = new UiLength
                {
                    HasValue = rule.Length.HasValue,
                    Value = rule.Length.Value,
                    Unit = rule.Length.Unit,
                    Expression = rule.Length.Expression
                };
            }

            result[index] = copied;
            index++;
        }

        return result;
    }

    internal UiValueEntry[] CloneValueEntries(UiValueEntry[] entries)
    {
        UiValueEntry[] result = new UiValueEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            result[index] = new UiValueEntry
            {
                Key = entries[index].Key,
                Value = ValueJsonCodec.CloneNode(entries[index].Value)
            };
            index++;
        }

        return result;
    }

    internal UiDiagnostic[] ConvertDiagnostics(DiagnosticEntry[] entries)
    {
        UiDiagnostic[] result = new UiDiagnostic[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            DiagnosticEntry entry = entries[index];
            result[index] = new UiDiagnostic
            {
                Severity = ConvertDiagnosticSeverity(entry.Level),
                Code = entry.Code,
                Message = entry.Message,
                Location = entry.Location
            };
            index++;
        }

        return result;
    }

    internal UiDiagnosticSeverity ConvertDiagnosticSeverity(DiagnosticLevel level)
    {
        switch (level)
        {
            case DiagnosticLevel.Information:
                return UiDiagnosticSeverity.Information;
            case DiagnosticLevel.Warning:
                return UiDiagnosticSeverity.Warning;
            case DiagnosticLevel.Error:
                return UiDiagnosticSeverity.Error;
            default:
                throw new ArgumentOutOfRangeException("level", level, "The diagnostic level is not supported.");
        }
    }
}
