using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ResponsiveRuleCompiler
{
    internal CssSerializer Serializer = new CssSerializer();

    internal UiCssRule[] CompileRules(UiResponsiveRule[]? responsiveRules, string? selector)
    {
        if (responsiveRules == null)
        {
            throw new ArgumentNullException("responsiveRules");
        }

        string normalizedSelector = CssEscaper.EscapeSelector(selector);
        List<UiCssRule> compiled = new List<UiCssRule>();
        int index = 0;
        while (index < responsiveRules.Length)
        {
            UiResponsiveRule? responsiveRule = responsiveRules[index];
            if (responsiveRule == null)
            {
                throw new ArgumentException("Responsive rule collections cannot contain null.", "responsiveRules");
            }

            UiCssRule rule = CreateRule(responsiveRule, normalizedSelector);
            InsertCompiledRule(compiled, rule);
            index++;
        }

        return compiled.ToArray();
    }

    internal UiCssRule CreateRule(UiResponsiveRule? responsiveRule, string? selector)
    {
        if (responsiveRule == null)
        {
            throw new ArgumentNullException("responsiveRule");
        }

        string normalizedSelector = CssEscaper.EscapeSelector(selector);
        Serializer.SerializeDeclarations(responsiveRule.Declarations);
        return new UiCssRule
        {
            Selector = normalizedSelector,
            Declarations = CloneDeclarations(responsiveRule.Declarations),
            Condition = CreateCondition(responsiveRule)
        };
    }

    internal UiCssCondition CreateCondition(UiResponsiveRule? responsiveRule)
    {
        if (responsiveRule == null)
        {
            throw new ArgumentNullException("responsiveRule");
        }

        string kind = responsiveRule.IsContainer ? "container" : "media";
        string? containerName = null;
        string feature;
        string value;
        switch (responsiveRule.Operator)
        {
            case UiResponsiveOperator.MinWidth:
            case UiResponsiveOperator.MaxWidth:
            case UiResponsiveOperator.MinHeight:
            case UiResponsiveOperator.MaxHeight:
                feature = GetSizeFeature(responsiveRule.Operator);
                value = FormatSizeValue(responsiveRule.Length);
                if (responsiveRule.IsContainer)
                {
                    containerName = NormalizeContainerName(responsiveRule.Value);
                }
                else if (responsiveRule.Value != null)
                {
                    throw new ArgumentException("Viewport size conditions cannot specify a container name.", "responsiveRule");
                }

                break;
            case UiResponsiveOperator.Orientation:
                RejectLength(responsiveRule.Length);
                feature = CssMediaFeatures.Orientation;
                value = NormalizeFeatureToken(responsiveRule.Value, "portrait", "landscape", string.Empty, string.Empty);
                break;
            case UiResponsiveOperator.Pointer:
                RejectContainer(responsiveRule.IsContainer);
                RejectLength(responsiveRule.Length);
                feature = CssMediaFeatures.Pointer;
                value = NormalizeFeatureToken(responsiveRule.Value, "none", "coarse", "fine", string.Empty);
                break;
            case UiResponsiveOperator.Hover:
                RejectContainer(responsiveRule.IsContainer);
                RejectLength(responsiveRule.Length);
                feature = CssMediaFeatures.Hover;
                value = NormalizeFeatureToken(responsiveRule.Value, "none", "hover", string.Empty, string.Empty);
                break;
            case UiResponsiveOperator.ColorScheme:
                RejectContainer(responsiveRule.IsContainer);
                RejectLength(responsiveRule.Length);
                feature = CssMediaFeatures.PreferredColorScheme;
                value = NormalizeFeatureToken(responsiveRule.Value, "light", "dark", string.Empty, string.Empty);
                break;
            case UiResponsiveOperator.Contrast:
                RejectContainer(responsiveRule.IsContainer);
                RejectLength(responsiveRule.Length);
                feature = CssMediaFeatures.PreferredContrast;
                value = NormalizeFeatureToken(responsiveRule.Value, "no-preference", "less", "more", "custom");
                break;
            case UiResponsiveOperator.Motion:
                RejectContainer(responsiveRule.IsContainer);
                RejectLength(responsiveRule.Length);
                feature = CssMediaFeatures.PreferredReducedMotion;
                value = NormalizeFeatureToken(responsiveRule.Value, "no-preference", "reduce", string.Empty, string.Empty);
                break;
            case UiResponsiveOperator.ForcedColors:
                RejectContainer(responsiveRule.IsContainer);
                RejectLength(responsiveRule.Length);
                feature = CssMediaFeatures.ForcedColors;
                value = NormalizeFeatureToken(responsiveRule.Value, "none", "active", string.Empty, string.Empty);
                break;
            default:
                throw new ArgumentOutOfRangeException("responsiveRule", responsiveRule.Operator, "The responsive operator is not supported.");
        }

        return new UiCssCondition
        {
            Kind = kind,
            ContainerName = containerName,
            Expression = "(" + feature + ": " + value + ")"
        };
    }

    internal string GetSizeFeature(UiResponsiveOperator responsiveOperator)
    {
        switch (responsiveOperator)
        {
            case UiResponsiveOperator.MinWidth:
                return CssMediaFeatures.MinimumWidth;
            case UiResponsiveOperator.MaxWidth:
                return CssMediaFeatures.MaximumWidth;
            case UiResponsiveOperator.MinHeight:
                return CssMediaFeatures.MinimumHeight;
            case UiResponsiveOperator.MaxHeight:
                return CssMediaFeatures.MaximumHeight;
            default:
                throw new ArgumentOutOfRangeException("responsiveOperator", responsiveOperator, "The responsive operator is not a size operator.");
        }
    }

    internal string FormatSizeValue(UiLength? length)
    {
        if (length == null)
        {
            throw new ArgumentException("Responsive size conditions require a length.", "length");
        }

        if (length.HasValue && length.Value < 0.0)
        {
            throw new ArgumentOutOfRangeException("length", length.Value, "Responsive sizes cannot be negative.");
        }

        string value = CssDesignValueFormatter.FormatLength(length, false);
        if (value == "auto" || value == "min-content" || value == "max-content" || value == "fit-content")
        {
            throw new ArgumentException("Responsive sizes require a CSS length rather than a sizing keyword.", "length");
        }

        return value;
    }

    internal string? NormalizeContainerName(string? value)
    {
        if (value == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Container names cannot be empty.", "value");
        }

        string name = value.Trim();
        CssEscaper.EscapeIdentifier(name);
        return name;
    }

    internal string NormalizeFeatureToken(string? value, string first, string second, string third, string fourth)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Responsive feature values cannot be empty.", "value");
        }

        string token = value.Trim().ToLowerInvariant();
        if (token == first || token == second || third.Length > 0 && token == third || fourth.Length > 0 && token == fourth)
        {
            return token;
        }

        throw new ArgumentException("The responsive feature value is not supported.", "value");
    }

    internal void RejectLength(UiLength? length)
    {
        if (length != null)
        {
            throw new ArgumentException("Only responsive size conditions can specify a length.", "length");
        }
    }

    internal void RejectContainer(bool isContainer)
    {
        if (isContainer)
        {
            throw new ArgumentException("This responsive feature is available only as a viewport media condition.", "isContainer");
        }
    }

    internal UiCssDeclaration[] CloneDeclarations(UiCssDeclaration[] declarations)
    {
        UiCssDeclaration[] result = new UiCssDeclaration[declarations.Length];
        int index = 0;
        while (index < declarations.Length)
        {
            UiCssDeclaration declaration = declarations[index];
            result[index] = new UiCssDeclaration
            {
                Property = declaration.Property,
                Value = declaration.Value,
                Important = declaration.Important,
                IsTrusted = declaration.IsTrusted
            };
            index++;
        }

        return result;
    }

    internal void InsertCompiledRule(List<UiCssRule> rules, UiCssRule rule)
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

    internal UiCssDeclaration CreateValueOverride(string? property, string? value, bool isTrusted)
    {
        if (property == null)
        {
            throw new ArgumentNullException("property");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        CssEscaper.EscapeDeclarationValue(property, value, isTrusted);
        return new UiCssDeclaration
        {
            Property = property,
            Value = value.Trim(),
            IsTrusted = isTrusted
        };
    }

    internal UiCssDeclaration[] CreateLayoutOverride(UiLayoutKind kind, UiOrientation orientation, bool wrap)
    {
        List<UiCssDeclaration> declarations = new List<UiCssDeclaration>();
        switch (kind)
        {
            case UiLayoutKind.Flow:
            case UiLayoutKind.Scroll:
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.Display, "block");
                break;
            case UiLayoutKind.Inline:
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.Display, "inline");
                break;
            case UiLayoutKind.VerticalStack:
            case UiLayoutKind.FlexColumn:
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.Display, "flex");
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.FlexDirection, "column");
                break;
            case UiLayoutKind.HorizontalStack:
            case UiLayoutKind.FlexRow:
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.Display, "flex");
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.FlexDirection, "row");
                break;
            case UiLayoutKind.Wrap:
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.Display, "flex");
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.FlexWrap, "wrap");
                break;
            case UiLayoutKind.Grid:
            case UiLayoutKind.UniformGrid:
            case UiLayoutKind.Dock:
            case UiLayoutKind.Overlay:
            case UiLayoutKind.ResponsiveShell:
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.Display, "grid");
                break;
            case UiLayoutKind.Absolute:
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.Position, "relative");
                break;
            case UiLayoutKind.Split:
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.Display, "flex");
                CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.FlexDirection, GetOrientationValue(orientation));
                break;
            default:
                throw new ArgumentOutOfRangeException("kind", kind, "The layout kind is not supported.");
        }

        if (wrap && kind != UiLayoutKind.Wrap)
        {
            CssDesignValueFormatter.AddGeneratedDeclaration(declarations, CssProperties.FlexWrap, "wrap");
        }

        return declarations.ToArray();
    }

    internal string GetOrientationValue(UiOrientation orientation)
    {
        switch (orientation)
        {
            case UiOrientation.Horizontal:
                return "row";
            case UiOrientation.Vertical:
                return "column";
            default:
                throw new ArgumentOutOfRangeException("orientation", orientation, "The orientation is not supported.");
        }
    }

    internal UiCssDeclaration CreateVisibilityOverride(bool visible)
    {
        return new UiCssDeclaration
        {
            Property = CssProperties.Display,
            Value = visible ? "revert-layer" : "none",
            IsTrusted = true
        };
    }

    internal UiCssDeclaration CreateDirectionOverride(bool rightToLeft)
    {
        return new UiCssDeclaration
        {
            Property = CssProperties.Direction,
            Value = rightToLeft ? "rtl" : "ltr",
            IsTrusted = true
        };
    }

    internal UiCssDeclaration CreateOrderOverride(int order)
    {
        return new UiCssDeclaration
        {
            Property = CssProperties.Order,
            Value = InvariantFormatter.FormatInt32(order),
            IsTrusted = true
        };
    }
}
