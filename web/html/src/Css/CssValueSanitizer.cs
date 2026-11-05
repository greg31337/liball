using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal class CssValueSanitizeResult
{
    internal bool allowed;
    internal string value;
    internal bool rewritten;
    internal bool containsUrl;

    internal CssValueSanitizeResult(bool allowedValue, string valueValue,
        bool rewrittenValue, bool containsUrlValue)
    {
        if (valueValue == null)
        {
            throw new ArgumentNullException("valueValue");
        }

        this.allowed = allowedValue;
        this.value = valueValue;
        this.rewritten = rewrittenValue;
        this.containsUrl = containsUrlValue;
    }

    internal bool IsAllowed()
    {
        return this.allowed;
    }

    internal string GetValue()
    {
        return this.value;
    }

    internal bool WasRewritten()
    {
        return this.rewritten;
    }

    internal bool ContainsUrl()
    {
        return this.containsUrl;
    }
}

internal enum CssValueNodeKind
{
    Identifier = 0,
    String = 1,
    Hash = 2,
    Number = 3,
    Percentage = 4,
    Dimension = 5,
    Delimiter = 6,
    Comma = 7,
    Group = 8,
    Function = 9,
    Url = 10
}

internal class CssValueNode
{
    internal CssValueNodeKind kind;
    internal CssToken token;
    internal string name;
    internal string url;
    internal CssValueNode[] children;

    internal CssValueNode(CssValueNodeKind kindValue, CssToken tokenValue,
        string nameValue, string urlValue, CssValueNode[] childrenValue)
    {
        if ((int)kindValue < 0 || (int)kindValue > (int)CssValueNodeKind.Url)
        {
            throw new ArgumentOutOfRangeException("kindValue");
        }

        if (tokenValue == null)
        {
            throw new ArgumentNullException("tokenValue");
        }

        if (nameValue == null)
        {
            throw new ArgumentNullException("nameValue");
        }

        if (urlValue == null)
        {
            throw new ArgumentNullException("urlValue");
        }

        if (childrenValue == null)
        {
            throw new ArgumentNullException("childrenValue");
        }

        this.kind = kindValue;
        this.token = tokenValue;
        this.name = nameValue;
        this.url = urlValue;
        this.children = Copy(childrenValue);
    }

    internal CssValueNodeKind GetKind()
    {
        return this.kind;
    }

    internal CssToken GetToken()
    {
        return this.token;
    }

    internal string GetName()
    {
        return this.name;
    }

    internal string GetUrl()
    {
        return this.url;
    }

    internal CssValueNode[] GetChildren()
    {
        return Copy(this.children);
    }

    internal static CssValueNode[] Copy(CssValueNode[] values)
    {
        CssValueNode[] result = new CssValueNode[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}

internal static class CssValueSanitizer
{
    internal static CssValueSanitizeResult Sanitize(string propertyName,
        string value, HtmlSanitizerUrlPolicy urlPolicy)
    {
        return Sanitize(propertyName, value, urlPolicy,
            HtmlLimits.CreateDefault());
    }

    internal static CssValueSanitizeResult Sanitize(string propertyName,
        string value, HtmlSanitizerUrlPolicy urlPolicy, HtmlLimits limits)
    {
        return Sanitize(propertyName, value, urlPolicy, limits,
            CancellationToken.None);
    }

    internal static CssValueSanitizeResult Sanitize(string propertyName,
        string value, HtmlSanitizerUrlPolicy urlPolicy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (propertyName == null)
        {
            throw new ArgumentNullException("propertyName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (urlPolicy == null)
        {
            throw new ArgumentNullException("urlPolicy");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        CssTokenizeResult tokenized = CssTokenizer.Tokenize(value, limits,
            cancellationToken);
        if (!tokenized.IsSuccess() || tokenized.GetErrorCount() != 0)
        {
            return Rejected();
        }

        return SanitizeTokens(propertyName, tokenized.GetTokens(), value,
            urlPolicy, limits, cancellationToken);
    }

    internal static CssValueSanitizeResult Sanitize(CssDeclaration declaration,
        HtmlSanitizerUrlPolicy urlPolicy)
    {
        return Sanitize(declaration, urlPolicy, HtmlLimits.CreateDefault());
    }

    internal static CssValueSanitizeResult Sanitize(CssDeclaration declaration,
        HtmlSanitizerUrlPolicy urlPolicy, HtmlLimits limits)
    {
        return Sanitize(declaration, urlPolicy, limits,
            CancellationToken.None);
    }

    internal static CssValueSanitizeResult Sanitize(CssDeclaration declaration,
        HtmlSanitizerUrlPolicy urlPolicy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (declaration == null)
        {
            throw new ArgumentNullException("declaration");
        }

        if (urlPolicy == null)
        {
            throw new ArgumentNullException("urlPolicy");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        CssToken[] tokens = declaration.GetValueTokens();
        StringBuilder original = new StringBuilder();
        int index = 0;
        while (index < tokens.Length)
        {
            original.Append(tokens[index].GetRepresentation());
            index = index + 1;
        }

        return SanitizeTokens(declaration.GetCanonicalName(), tokens,
            original.ToString(), urlPolicy, limits, cancellationToken);
    }

    internal static CssValueSanitizeResult SanitizeWithPolicy(
        CssDeclaration declaration, HtmlSanitizerUrlPolicy urlPolicy,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (declaration == null)
        {
            throw new ArgumentNullException("declaration");
        }

        if (urlPolicy == null)
        {
            throw new ArgumentNullException("urlPolicy");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        CssToken[] tokens = declaration.GetValueTokens();
        StringBuilder original = new StringBuilder();
        int index = 0;
        while (index < tokens.Length)
        {
            original.Append(tokens[index].GetRepresentation());
            index = index + 1;
        }

        return SanitizeTokensWithPolicy(declaration.GetCanonicalName(),
            tokens, original.ToString(), urlPolicy, policy, limits,
            cancellationToken);
    }

    internal static CssValueSanitizeResult SanitizeTokensWithPolicy(
        string propertyName, CssToken[] tokens, string original,
        HtmlSanitizerUrlPolicy urlPolicy,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool customProperty = StartsCustomName(
            propertyName);
        CssSafePropertyProfile profile = CssSafeData.GetPropertyProfile(
            propertyName);
        if (profile == CssSafePropertyProfile.Unknown
            && (!customProperty || !policy.GetAllowCssCustomProperties()))
        {
            return Rejected();
        }

        CssValueParserState parser = new CssValueParserState(tokens,
            urlPolicy, limits, cancellationToken);
        CssValueNode[] nodes = parser.Run();
        cancellationToken.ThrowIfCancellationRequested();
        if (!parser.IsSuccess() || nodes.Length == 0
            || !AreNodesAllowedByPolicy(nodes, policy)
            || !customProperty && !ValidateProfile(profile, nodes))
        {
            return Rejected();
        }

        bool containsUrl = ContainsUrl(nodes);
        if (containsUrl && (customProperty
            || profile != CssSafePropertyProfile.UrlImage))
        {
            return Rejected();
        }

        string output = Serialize(nodes);
        return new CssValueSanitizeResult(true, output,
            output != original, containsUrl);
    }

    internal static bool AreNodesAllowedByPolicy(CssValueNode[] nodes,
        HtmlCompiledSanitizerPolicy policy)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            CssValueNode node = nodes[index];
            if (node.GetKind() == CssValueNodeKind.Function
                && !policy.IsCssFunctionAllowed(node.GetName()))
            {
                return false;
            }

            if (node.GetKind() == CssValueNodeKind.Dimension
                && !policy.IsCssUnitAllowed(node.GetToken().GetUnit()))
            {
                return false;
            }

            if (node.GetKind() == CssValueNodeKind.Identifier)
            {
                string value = node.GetToken().GetValue();
                if (CssSafeData.IsKeyword(value)
                    && !policy.IsCssKeywordAllowed(value))
                {
                    return false;
                }

                if (StartsCustomName(value)
                    && !policy.GetAllowCssCustomProperties())
                {
                    return false;
                }
            }

            if (!AreNodesAllowedByPolicy(node.GetChildren(), policy))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static CssValueSanitizeResult SanitizeTokens(string propertyName,
        CssToken[] tokens, string original,
        HtmlSanitizerUrlPolicy urlPolicy)
    {
        return SanitizeTokens(propertyName, tokens, original, urlPolicy,
            HtmlLimits.CreateDefault());
    }

    internal static CssValueSanitizeResult SanitizeTokens(string propertyName,
        CssToken[] tokens, string original,
        HtmlSanitizerUrlPolicy urlPolicy, HtmlLimits limits)
    {
        return SanitizeTokens(propertyName, tokens, original, urlPolicy,
            limits, CancellationToken.None);
    }

    internal static CssValueSanitizeResult SanitizeTokens(string propertyName,
        CssToken[] tokens, string original,
        HtmlSanitizerUrlPolicy urlPolicy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CssSafePropertyProfile profile = CssSafeData.GetPropertyProfile(
            propertyName);
        if (profile == CssSafePropertyProfile.Unknown)
        {
            return Rejected();
        }

        CssValueParserState parser = new CssValueParserState(tokens,
            urlPolicy, limits, cancellationToken);
        CssValueNode[] nodes = parser.Run();
        cancellationToken.ThrowIfCancellationRequested();
        if (!parser.IsSuccess() || nodes.Length == 0
            || !ValidateProfile(profile, nodes))
        {
            return Rejected();
        }

        bool containsUrl = ContainsUrl(nodes);
        cancellationToken.ThrowIfCancellationRequested();
        if (containsUrl && profile != CssSafePropertyProfile.UrlImage)
        {
            return Rejected();
        }

        string output = Serialize(nodes);
        cancellationToken.ThrowIfCancellationRequested();
        return new CssValueSanitizeResult(true, output,
            output != original, containsUrl);
    }

    internal static bool ValidateProfile(CssSafePropertyProfile profile,
        CssValueNode[] nodes)
    {
        if (IsCssWideKeyword(nodes))
        {
            return true;
        }

        if (nodes.Length == 1 && IsVariable(nodes[0]))
        {
            return profile != CssSafePropertyProfile.UrlImage;
        }

        switch (profile)
        {
            case CssSafePropertyProfile.Color:
                return nodes.Length == 1 && IsColor(nodes[0]);
            case CssSafePropertyProfile.ColorList:
                return IsList(nodes, 1, 4, 1);
            case CssSafePropertyProfile.UrlImage:
                return nodes.Length == 1
                    && (nodes[0].GetKind() == CssValueNodeKind.Url
                        || IsIdentifier(nodes[0], "none"));
            case CssSafePropertyProfile.BorderStyle:
                return nodes.Length == 1 && IsBorderStyle(nodes[0]);
            case CssSafePropertyProfile.BorderStyleList:
                return IsList(nodes, 1, 4, 2);
            case CssSafePropertyProfile.BorderWidth:
                return nodes.Length == 1 && IsBorderWidth(nodes[0]);
            case CssSafePropertyProfile.BorderWidthList:
                return IsList(nodes, 1, 4, 3);
            case CssSafePropertyProfile.BoxSizing:
                return nodes.Length == 1 && IsOneOf(nodes[0],
                    "border-box", "content-box", "");
            case CssSafePropertyProfile.Display:
                return nodes.Length == 1 && IsDisplay(nodes[0]);
            case CssSafePropertyProfile.FontFamily:
                return IsFontFamily(nodes);
            case CssSafePropertyProfile.FontSize:
                return nodes.Length == 1 && IsFontSize(nodes[0]);
            case CssSafePropertyProfile.FontStyle:
                return IsFontStyle(nodes);
            case CssSafePropertyProfile.FontWeight:
                return nodes.Length == 1 && IsFontWeight(nodes[0]);
            case CssSafePropertyProfile.LengthPercentage:
                return nodes.Length == 1
                    && IsLengthPercentage(nodes[0], true, true);
            case CssSafePropertyProfile.LineHeight:
                return nodes.Length == 1 && IsLineHeight(nodes[0]);
            case CssSafePropertyProfile.ListStyleType:
                return nodes.Length == 1 && IsListStyleType(nodes[0]);
            case CssSafePropertyProfile.Margin:
                return nodes.Length == 1 && IsMargin(nodes[0]);
            case CssSafePropertyProfile.MarginList:
                return IsList(nodes, 1, 4, 4);
            case CssSafePropertyProfile.MaxSize:
                return nodes.Length == 1 && (IsIdentifier(nodes[0], "none")
                    || IsLengthPercentage(nodes[0], false, true));
            case CssSafePropertyProfile.MinSize:
                return nodes.Length == 1 && (IsIdentifier(nodes[0], "auto")
                    || IsLengthPercentage(nodes[0], false, true));
            case CssSafePropertyProfile.ObjectFit:
                return nodes.Length == 1 && IsOneOf(nodes[0], "contain",
                    "cover", "fill", "none", "scale-down");
            case CssSafePropertyProfile.Opacity:
                return nodes.Length == 1 && IsOpacity(nodes[0]);
            case CssSafePropertyProfile.Overflow:
                return nodes.Length == 1 && IsOneOf(nodes[0], "auto",
                    "clip", "hidden", "scroll", "visible");
            case CssSafePropertyProfile.OverflowWrap:
                return nodes.Length == 1 && IsOneOf(nodes[0], "anywhere",
                    "break-word", "normal", "");
            case CssSafePropertyProfile.Padding:
                return nodes.Length == 1
                    && IsLengthPercentage(nodes[0], false, false);
            case CssSafePropertyProfile.PaddingList:
                return IsList(nodes, 1, 4, 5);
            case CssSafePropertyProfile.Size:
                return nodes.Length == 1 && (IsIdentifier(nodes[0], "auto")
                    || IsLengthPercentage(nodes[0], false, true));
            case CssSafePropertyProfile.Spacing:
                return nodes.Length == 1
                    && (IsIdentifier(nodes[0], "normal")
                        || IsLength(nodes[0], true, true));
            case CssSafePropertyProfile.TextAlign:
                return nodes.Length == 1 && IsOneOf(nodes[0], "center",
                    "end", "justify", "left", "match-parent", "right",
                    "start", "");
            case CssSafePropertyProfile.TextDecorationLine:
                return IsTextDecorationLine(nodes);
            case CssSafePropertyProfile.TextDecorationStyle:
                return nodes.Length == 1 && IsOneOf(nodes[0], "dashed",
                    "dotted", "double", "solid", "wavy");
            case CssSafePropertyProfile.TextTransform:
                return nodes.Length == 1 && IsOneOf(nodes[0], "capitalize",
                    "full-size-kana", "full-width", "lowercase", "none",
                    "uppercase");
            case CssSafePropertyProfile.VerticalAlign:
                return nodes.Length == 1 && IsVerticalAlign(nodes[0]);
            case CssSafePropertyProfile.WhiteSpace:
                return nodes.Length == 1 && IsOneOf(nodes[0],
                    "break-spaces", "normal", "nowrap", "pre", "pre-line",
                    "pre-wrap");
            case CssSafePropertyProfile.WordBreak:
                return nodes.Length == 1 && IsOneOf(nodes[0], "break-all",
                    "keep-all", "normal", "");
            default:
                return false;
        }
    }

    internal static bool IsList(CssValueNode[] nodes, int minimum, int maximum,
        int kind)
    {
        if (nodes.Length < minimum || nodes.Length > maximum)
        {
            return false;
        }

        int index = 0;
        while (index < nodes.Length)
        {
            bool valid = kind == 1 ? IsColor(nodes[index])
                : kind == 2 ? IsBorderStyle(nodes[index])
                : kind == 3 ? IsBorderWidth(nodes[index])
                : kind == 4 ? IsMargin(nodes[index])
                : IsLengthPercentage(nodes[index], false, false);
            if (!valid)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsColor(CssValueNode node)
    {
        if (node.GetKind() == CssValueNodeKind.Identifier)
        {
            return CssSafeData.IsColorKeyword(node.GetToken().GetValue())
                || IsVariable(node);
        }

        if (node.GetKind() == CssValueNodeKind.Hash)
        {
            string value = node.GetToken().GetValue();
            if (value.Length != 3 && value.Length != 4
                && value.Length != 6 && value.Length != 8)
            {
                return false;
            }

            int hexIndex = 0;
            while (hexIndex < value.Length)
            {
                if (!Ascii.IsHexDigit(value[hexIndex]))
                {
                    return false;
                }

                hexIndex = hexIndex + 1;
            }

            return true;
        }

        if (node.GetKind() != CssValueNodeKind.Function
            || CssSafeData.GetFunctionProfile(node.GetName())
                != CssSafeFunctionProfile.Color)
        {
            return IsVariable(node);
        }

        CssValueNode[] children = node.GetChildren();
        if (children.Length == 0)
        {
            return false;
        }

        int slashCount = 0;
        int index = 0;
        bool expectValue = true;
        while (index < children.Length)
        {
            CssValueNode child = children[index];
            if (child.GetKind() == CssValueNodeKind.Comma
                || IsDelimiter(child, "/"))
            {
                if (expectValue)
                {
                    return false;
                }

                if (IsDelimiter(child, "/"))
                {
                    slashCount = slashCount + 1;
                    if (slashCount > 1)
                    {
                        return false;
                    }
                }

                expectValue = true;
            }
            else
            {
                if (!IsColorFunctionValue(child))
                {
                    return false;
                }

                expectValue = false;
            }

            index = index + 1;
        }

        return !expectValue;
    }

    internal static bool IsColorFunctionValue(CssValueNode node)
    {
        if (node.GetKind() == CssValueNodeKind.Number
            || node.GetKind() == CssValueNodeKind.Percentage)
        {
            return IsFinite(node.GetToken().GetNumber());
        }

        if (node.GetKind() == CssValueNodeKind.Dimension)
        {
            return CssSafeData.GetUnitKind(node.GetToken().GetUnit())
                == CssSafeUnitKind.Angle;
        }

        if (IsIdentifier(node, "none") || IsVariable(node))
        {
            return true;
        }

        return IsMath(node, false, true, true, true);
    }

    internal static bool IsBorderStyle(CssValueNode node)
    {
        return IsOneOf(node, "dashed", "dotted", "double", "groove",
            "hidden", "inset", "none", "outset", "ridge", "solid");
    }

    internal static bool IsBorderWidth(CssValueNode node)
    {
        return IsOneOf(node, "medium", "thick", "thin", "")
            || IsLength(node, false, false);
    }

    internal static bool IsDisplay(CssValueNode node)
    {
        return IsOneOf(node, "block", "contents", "flex", "flow-root",
            "grid", "inline", "inline-block", "inline-flex",
            "inline-grid", "inline-table", "list-item", "none", "table",
            "table-cell", "table-row");
    }

    internal static bool IsFontFamily(CssValueNode[] nodes)
    {
        bool expectFamily = true;
        bool inNamedFamily = false;
        int index = 0;
        while (index < nodes.Length)
        {
            CssValueNode node = nodes[index];
            if (node.GetKind() == CssValueNodeKind.Comma)
            {
                if (expectFamily || !inNamedFamily)
                {
                    return false;
                }

                expectFamily = true;
                inNamedFamily = false;
            }
            else if (node.GetKind() == CssValueNodeKind.String
                || IsVariable(node))
            {
                if (!expectFamily || inNamedFamily)
                {
                    return false;
                }

                expectFamily = false;
                inNamedFamily = true;
            }
            else if (node.GetKind() == CssValueNodeKind.Identifier)
            {
                if (IsCssWideIdentifier(node))
                {
                    return false;
                }

                expectFamily = false;
                inNamedFamily = true;
            }
            else
            {
                return false;
            }

            index = index + 1;
        }

        return !expectFamily && inNamedFamily;
    }

    internal static bool IsFontSize(CssValueNode node)
    {
        return IsOneOf(node, "large", "larger", "medium", "small",
            "smaller", "x-large", "x-small", "xx-large", "xx-small",
            "xxx-large") || IsLengthPercentage(node, false, false);
    }

    internal static bool IsFontStyle(CssValueNode[] nodes)
    {
        if (nodes.Length == 1)
        {
            return IsOneOf(nodes[0], "italic", "normal", "oblique", "")
                || IsVariable(nodes[0]);
        }

        return nodes.Length == 2 && IsIdentifier(nodes[0], "oblique")
            && IsAngle(nodes[1]);
    }

    internal static bool IsFontWeight(CssValueNode node)
    {
        if (IsOneOf(node, "bold", "bolder", "lighter", "normal", ""))
        {
            return true;
        }

        if (node.GetKind() == CssValueNodeKind.Number)
        {
            double value = node.GetToken().GetNumber();
            return IsFinite(value) && value >= 1 && value <= 1000;
        }

        return IsVariable(node);
    }

    internal static bool IsLineHeight(CssValueNode node)
    {
        if (IsIdentifier(node, "normal") || IsVariable(node))
        {
            return true;
        }

        if (node.GetKind() == CssValueNodeKind.Number)
        {
            double value = node.GetToken().GetNumber();
            return IsFinite(value) && value >= 0;
        }

        return IsLengthPercentage(node, false, false);
    }

    internal static bool IsListStyleType(CssValueNode node)
    {
        return IsOneOf(node, "circle", "decimal", "decimal-leading-zero",
            "disc", "lower-alpha", "lower-roman", "none", "square",
            "upper-alpha", "upper-roman");
    }

    internal static bool IsMargin(CssValueNode node)
    {
        return IsIdentifier(node, "auto")
            || IsLengthPercentage(node, true, true);
    }

    internal static bool IsOpacity(CssValueNode node)
    {
        if (node.GetKind() == CssValueNodeKind.Number)
        {
            double value = node.GetToken().GetNumber();
            return IsFinite(value) && value >= 0 && value <= 1;
        }

        if (node.GetKind() == CssValueNodeKind.Percentage)
        {
            double value = node.GetToken().GetNumber();
            return IsFinite(value) && value >= 0 && value <= 100;
        }

        return IsVariable(node) || IsMath(node, false, true, false, true);
    }

    internal static bool IsTextDecorationLine(CssValueNode[] nodes)
    {
        if (nodes.Length < 1 || nodes.Length > 3)
        {
            return false;
        }

        if (nodes.Length > 1 && IsIdentifier(nodes[0], "none"))
        {
            return false;
        }

        int index = 0;
        while (index < nodes.Length)
        {
            if (!IsOneOf(nodes[index], "line-through", "none", "overline",
                "underline", ""))
            {
                return false;
            }

            int prior = 0;
            while (prior < index)
            {
                if (AsciiEqual(nodes[prior].GetToken().GetValue(),
                    nodes[index].GetToken().GetValue()))
                {
                    return false;
                }

                prior = prior + 1;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsVerticalAlign(CssValueNode node)
    {
        return IsOneOf(node, "baseline", "bottom", "middle", "sub",
            "super", "text-bottom", "text-top", "top")
            || IsLengthPercentage(node, true, true);
    }

    internal static bool IsLengthPercentage(CssValueNode node, bool allowNegative,
        bool allowAutoVariable)
    {
        if (node.GetKind() == CssValueNodeKind.Percentage)
        {
            double value = node.GetToken().GetNumber();
            return IsFinite(value) && (allowNegative || value >= 0);
        }

        return IsLength(node, allowNegative, allowAutoVariable)
            || IsMath(node, true, true, false, true);
    }

    internal static bool IsLength(CssValueNode node, bool allowNegative,
        bool allowVariable)
    {
        if (allowVariable && IsVariable(node))
        {
            return true;
        }

        if (node.GetKind() == CssValueNodeKind.Number)
        {
            return node.GetToken().GetNumber() == 0;
        }

        if (node.GetKind() != CssValueNodeKind.Dimension
            || CssSafeData.GetUnitKind(node.GetToken().GetUnit())
                != CssSafeUnitKind.Length)
        {
            return IsMath(node, true, false, false, allowVariable);
        }

        double value = node.GetToken().GetNumber();
        return IsFinite(value) && (allowNegative || value >= 0);
    }

    internal static bool IsAngle(CssValueNode node)
    {
        if (node.GetKind() == CssValueNodeKind.Number
            && node.GetToken().GetNumber() == 0)
        {
            return true;
        }

        return node.GetKind() == CssValueNodeKind.Dimension
            && CssSafeData.GetUnitKind(node.GetToken().GetUnit())
                == CssSafeUnitKind.Angle
            && IsFinite(node.GetToken().GetNumber());
    }

    internal static bool IsMath(CssValueNode node, bool allowLength,
        bool allowPercentage, bool allowAngle, bool allowVariable)
    {
        if (node.GetKind() != CssValueNodeKind.Function
            || CssSafeData.GetFunctionProfile(node.GetName())
                != CssSafeFunctionProfile.Math)
        {
            return false;
        }

        CssValueNode[] children = node.GetChildren();
        if (node.GetName() == "calc")
        {
            return ValidateMathExpression(children, 0, children.Length,
                allowLength, allowPercentage, allowAngle, allowVariable);
        }

        int requiredParts = node.GetName() == "clamp" ? 3 : -1;
        int partCount = 0;
        int start = 0;
        int index = 0;
        while (index <= children.Length)
        {
            if (index == children.Length
                || children[index].GetKind() == CssValueNodeKind.Comma)
            {
                if (!ValidateMathExpression(children, start, index,
                    allowLength, allowPercentage, allowAngle,
                    allowVariable))
                {
                    return false;
                }

                partCount = partCount + 1;
                start = index + 1;
            }

            index = index + 1;
        }

        return requiredParts < 0 ? partCount >= 1
            : partCount == requiredParts;
    }

    internal static bool ValidateMathExpression(CssValueNode[] nodes, int start,
        int end, bool allowLength, bool allowPercentage, bool allowAngle,
        bool allowVariable)
    {
        if (start >= end)
        {
            return false;
        }

        bool expectOperand = true;
        int index = start;
        while (index < end)
        {
            CssValueNode node = nodes[index];
            if (expectOperand)
            {
                if (!IsMathOperand(node, allowLength, allowPercentage,
                    allowAngle, allowVariable))
                {
                    return false;
                }
            }
            else if (!IsDelimiter(node, "+") && !IsDelimiter(node, "-")
                && !IsDelimiter(node, "*") && !IsDelimiter(node, "/"))
            {
                return false;
            }

            expectOperand = !expectOperand;
            index = index + 1;
        }

        return !expectOperand;
    }

    internal static bool IsMathOperand(CssValueNode node, bool allowLength,
        bool allowPercentage, bool allowAngle, bool allowVariable)
    {
        if (node.GetKind() == CssValueNodeKind.Number)
        {
            return IsFinite(node.GetToken().GetNumber());
        }

        if (node.GetKind() == CssValueNodeKind.Percentage)
        {
            return allowPercentage && IsFinite(node.GetToken().GetNumber());
        }

        if (node.GetKind() == CssValueNodeKind.Dimension)
        {
            CssSafeUnitKind kind = CssSafeData.GetUnitKind(
                node.GetToken().GetUnit());
            return IsFinite(node.GetToken().GetNumber())
                && (allowLength && kind == CssSafeUnitKind.Length
                    || allowAngle && kind == CssSafeUnitKind.Angle);
        }

        if (node.GetKind() == CssValueNodeKind.Group)
        {
            CssValueNode[] children = node.GetChildren();
            return ValidateMathExpression(children, 0, children.Length,
                allowLength, allowPercentage, allowAngle, allowVariable);
        }

        return allowVariable && IsVariable(node)
            || IsMath(node, allowLength, allowPercentage, allowAngle,
                allowVariable);
    }

    internal static bool IsVariable(CssValueNode node)
    {
        if (node.GetKind() != CssValueNodeKind.Function
            || node.GetName() != "var")
        {
            return false;
        }

        CssValueNode[] children = node.GetChildren();
        if (children.Length == 0
            || children[0].GetKind() != CssValueNodeKind.Identifier
            || !StartsCustomName(children[0].GetToken().GetValue()))
        {
            return false;
        }

        if (children.Length == 1)
        {
            return true;
        }

        return children.Length >= 3
            && children[1].GetKind() == CssValueNodeKind.Comma;
    }

    internal static bool IsCssWideKeyword(CssValueNode[] nodes)
    {
        return nodes.Length == 1 && IsCssWideIdentifier(nodes[0]);
    }

    internal static bool IsCssWideIdentifier(CssValueNode node)
    {
        return IsOneOf(node, "inherit", "initial", "revert",
            "revert-layer", "unset");
    }

    internal static bool IsOneOf(CssValueNode node, string first, string second,
        string third)
    {
        return IsIdentifier(node, first) || IsIdentifier(node, second)
            || third.Length > 0 && IsIdentifier(node, third)
            || third.Length == 0 && IsVariable(node);
    }

    internal static bool IsOneOf(CssValueNode node, string first, string second,
        string third, string fourth)
    {
        return IsIdentifier(node, first) || IsIdentifier(node, second)
            || IsIdentifier(node, third)
            || fourth.Length > 0 && IsIdentifier(node, fourth)
            || fourth.Length == 0 && IsVariable(node);
    }

    internal static bool IsOneOf(CssValueNode node, string first, string second,
        string third, string fourth, string fifth)
    {
        return IsIdentifier(node, first) || IsIdentifier(node, second)
            || IsIdentifier(node, third) || IsIdentifier(node, fourth)
            || IsIdentifier(node, fifth);
    }

    internal static bool IsOneOf(CssValueNode node, string first, string second,
        string third, string fourth, string fifth, string sixth)
    {
        return IsOneOf(node, first, second, third, fourth, fifth)
            || IsIdentifier(node, sixth);
    }

    internal static bool IsOneOf(CssValueNode node, string first, string second,
        string third, string fourth, string fifth, string sixth,
        string seventh, string eighth)
    {
        return IsOneOf(node, first, second, third, fourth, fifth, sixth)
            || IsIdentifier(node, seventh) || IsIdentifier(node, eighth);
    }

    internal static bool IsOneOf(CssValueNode node, string first, string second,
        string third, string fourth, string fifth, string sixth,
        string seventh, string eighth, string ninth, string tenth)
    {
        return IsOneOf(node, first, second, third, fourth, fifth, sixth,
            seventh, eighth) || IsIdentifier(node, ninth)
            || IsIdentifier(node, tenth);
    }

    internal static bool IsOneOf(CssValueNode node, string first, string second,
        string third, string fourth, string fifth, string sixth,
        string seventh, string eighth, string ninth, string tenth,
        string eleventh, string twelfth, string thirteenth, string fourteenth,
        string fifteenth)
    {
        return IsOneOf(node, first, second, third, fourth, fifth, sixth,
            seventh, eighth, ninth, tenth) || IsIdentifier(node, eleventh)
            || IsIdentifier(node, twelfth) || IsIdentifier(node, thirteenth)
            || IsIdentifier(node, fourteenth) || IsIdentifier(node, fifteenth);
    }

    internal static bool IsIdentifier(CssValueNode node, string value)
    {
        return node.GetKind() == CssValueNodeKind.Identifier
            && AsciiEqual(node.GetToken().GetValue(), value);
    }

    internal static bool IsDelimiter(CssValueNode node, string value)
    {
        return node.GetKind() == CssValueNodeKind.Delimiter
            && node.GetToken().GetValue() == value;
    }

    internal static bool ContainsUrl(CssValueNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index].GetKind() == CssValueNodeKind.Url
                || ContainsUrl(nodes[index].GetChildren()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string Serialize(CssValueNode[] nodes)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < nodes.Length)
        {
            CssValueNode node = nodes[index];
            if (node.GetKind() == CssValueNodeKind.Comma)
            {
                TrimSpace(result);
                result.Append(", ");
            }
            else if (node.GetKind() == CssValueNodeKind.Delimiter)
            {
                AppendSpace(result);
                result.Append(node.GetToken().GetValue());
                result.Append(' ');
            }
            else
            {
                AppendSpace(result);
                AppendNode(result, node);
            }

            index = index + 1;
        }

        TrimSpace(result);
        return result.ToString();
    }

    internal static void AppendNode(StringBuilder result, CssValueNode node)
    {
        CssToken token = node.GetToken();
        switch (node.GetKind())
        {
            case CssValueNodeKind.Identifier:
                AppendIdentifier(result, token.GetValue());
                break;
            case CssValueNodeKind.String:
                AppendString(result, token.GetValue());
                break;
            case CssValueNodeKind.Hash:
                result.Append('#');
                result.Append(AsciiLower(token.GetValue()));
                break;
            case CssValueNodeKind.Number:
                result.Append(token.GetNumberRepresentation());
                break;
            case CssValueNodeKind.Percentage:
                result.Append(token.GetNumberRepresentation());
                result.Append('%');
                break;
            case CssValueNodeKind.Dimension:
                result.Append(token.GetNumberRepresentation());
                result.Append(AsciiLower(token.GetUnit()));
                break;
            case CssValueNodeKind.Group:
                result.Append('(');
                result.Append(Serialize(node.GetChildren()));
                result.Append(')');
                break;
            case CssValueNodeKind.Function:
                result.Append(node.GetName());
                result.Append('(');
                result.Append(Serialize(node.GetChildren()));
                result.Append(')');
                break;
            case CssValueNodeKind.Url:
                result.Append("url(");
                AppendString(result, node.GetUrl());
                result.Append(')');
                break;
            default:
                break;
        }
    }

    internal static void AppendIdentifier(StringBuilder result, string value)
    {
        string normalized = StartsCustomName(value) ? value
            : AsciiLower(value);
        int index = 0;
        while (index < normalized.Length)
        {
            char current = normalized[index];
            bool safe = current >= 'a' && current <= 'z'
                || current >= 'A' && current <= 'Z'
                || current >= '0' && current <= '9' && index > 0
                || current == '-' || current == '_' || current >= 128;
            if (safe)
            {
                result.Append(current);
            }
            else
            {
                AppendHexEscape(result, current);
            }

            index = index + 1;
        }
    }

    internal static void AppendString(StringBuilder result, string value)
    {
        result.Append('"');
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '"' || current == '\\' || current < 32
                || current == 127 || current == '<' || current == '>'
                || current == '&')
            {
                AppendHexEscape(result, current);
            }
            else
            {
                result.Append(current);
            }

            index = index + 1;
        }

        result.Append('"');
    }

    internal static void AppendHexEscape(StringBuilder result, int value)
    {
        result.Append('\\');
        result.Append(value.ToString("x"));
        result.Append(' ');
    }

    internal static void AppendSpace(StringBuilder result)
    {
        if (result.Length > 0 && result[result.Length - 1] != ' ')
        {
            result.Append(' ');
        }
    }

    internal static void TrimSpace(StringBuilder result)
    {
        while (result.Length > 0 && result[result.Length - 1] == ' ')
        {
            result.Length = result.Length - 1;
        }
    }

    internal static bool StartsCustomName(string value)
    {
        return value.Length > 2 && value[0] == '-' && value[1] == '-';
    }

    internal static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal static bool AsciiEqual(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (Ascii.ToLower(left[index]) != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string AsciiLower(string value)
    {
        char[] result = value.ToCharArray();
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Ascii.ToLower(result[index]);
            index = index + 1;
        }

        return new string(result);
    }

    internal static CssValueSanitizeResult Rejected()
    {
        return new CssValueSanitizeResult(false, "", false, false);
    }
}

internal class CssValueParserState
{
    internal const int MaximumNestingDepth = 64;

    internal CssToken[] tokens = Array.Empty<CssToken>();
    internal HtmlSanitizerUrlPolicy urlPolicy;
    internal HtmlLimits limits;
    internal int position;
    internal bool success;
    internal CancellationToken cancellationToken;

    internal CssValueParserState(CssToken[] tokensValue,
        HtmlSanitizerUrlPolicy urlPolicyValue, HtmlLimits limitsValue,
        CancellationToken cancellationTokenValue)
    {
        if (tokensValue == null)
        {
            throw new ArgumentNullException("tokensValue");
        }

        if (urlPolicyValue == null)
        {
            throw new ArgumentNullException("urlPolicyValue");
        }

        if (limitsValue == null)
        {
            throw new ArgumentNullException("limitsValue");
        }

        limitsValue.Validate();

        this.tokens = Copy(tokensValue);
        this.urlPolicy = urlPolicyValue;
        this.limits = limitsValue.Copy();
        this.cancellationToken = cancellationTokenValue;
        this.position = 0;
        this.success = true;
    }

    internal CssValueNode[] Run()
    {
        this.CheckCancellation();
        CssValueNode[] result = this.ParseSequence(CssTokenKind.End, 0);
        if (this.position < this.tokens.Length
            && this.tokens[this.position].GetKind() == CssTokenKind.End)
        {
            this.position = this.position + 1;
        }

        if (this.position != this.tokens.Length)
        {
            this.success = false;
        }

        return this.success ? result : new CssValueNode[0];
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal CssValueNode[] ParseSequence(CssTokenKind closer, int depth)
    {
        List<CssValueNode> result = new List<CssValueNode>();
        if (depth > MaximumNestingDepth)
        {
            this.success = false;
            return result.ToArray();
        }

        bool complete = false;
        while (!complete && this.success)
        {
            this.CheckCancellation();
            if (this.position >= this.tokens.Length)
            {
                if (closer != CssTokenKind.End)
                {
                    this.success = false;
                }

                break;
            }

            CssToken token = this.tokens[this.position];
            CssTokenKind kind = token.GetKind();
            if (kind == CssTokenKind.Whitespace)
            {
                this.position = this.position + 1;
                continue;
            }

            if (kind == closer)
            {
                if (closer != CssTokenKind.End)
                {
                    this.position = this.position + 1;
                }

                complete = true;
                continue;
            }

            if (kind == CssTokenKind.End || kind == CssTokenKind.CloseParenthesis
                || kind == CssTokenKind.CloseSquare
                || kind == CssTokenKind.CloseCurly)
            {
                this.success = false;
                break;
            }

            CssValueNode? node = this.ParseNode(token, depth);
            if (node != null)
            {
                result.Add(node);
            }
        }

        return result.ToArray();
    }

    internal CssValueNode? ParseNode(CssToken token, int depth)
    {
        CssTokenKind kind = token.GetKind();
        this.position = this.position + 1;
        if (kind == CssTokenKind.Identifier)
        {
            return CreateNode(CssValueNodeKind.Identifier, token);
        }

        if (kind == CssTokenKind.String)
        {
            return CreateNode(CssValueNodeKind.String, token);
        }

        if (kind == CssTokenKind.Hash)
        {
            return CreateNode(CssValueNodeKind.Hash, token);
        }

        if (kind == CssTokenKind.Number)
        {
            return CreateNode(CssValueNodeKind.Number, token);
        }

        if (kind == CssTokenKind.Percentage)
        {
            return CreateNode(CssValueNodeKind.Percentage, token);
        }

        if (kind == CssTokenKind.Dimension)
        {
            return CreateNode(CssValueNodeKind.Dimension, token);
        }

        if (kind == CssTokenKind.Comma)
        {
            return CreateNode(CssValueNodeKind.Comma, token);
        }

        if (kind == CssTokenKind.Delimiter
            && (token.GetValue() == "+" || token.GetValue() == "-"
                || token.GetValue() == "*" || token.GetValue() == "/"))
        {
            return CreateNode(CssValueNodeKind.Delimiter, token);
        }

        if (kind == CssTokenKind.Url)
        {
            return this.CreateUrlNode(token, token.GetValue());
        }

        if (kind == CssTokenKind.OpenParenthesis)
        {
            CssValueNode[] children = this.ParseSequence(
                CssTokenKind.CloseParenthesis, depth + 1);
            return new CssValueNode(CssValueNodeKind.Group, token, "", "",
                children);
        }

        if (kind == CssTokenKind.Function)
        {
            string name = AsciiLower(token.GetValue());
            CssSafeFunctionProfile profile = CssSafeData.GetFunctionProfile(
                name);
            if (profile == CssSafeFunctionProfile.Unknown)
            {
                this.success = false;
                return null;
            }

            if (profile == CssSafeFunctionProfile.Url)
            {
                return this.ParseQuotedUrl(token);
            }

            CssValueNode[] children = this.ParseSequence(
                CssTokenKind.CloseParenthesis, depth + 1);
            return new CssValueNode(CssValueNodeKind.Function, token, name,
                "", children);
        }

        this.success = false;
        return null;
    }

    internal CssValueNode? ParseQuotedUrl(CssToken token)
    {
        this.SkipWhitespace();
        if (this.position >= this.tokens.Length
            || this.tokens[this.position].GetKind() != CssTokenKind.String)
        {
            this.success = false;
            return null;
        }

        string value = this.tokens[this.position].GetValue();
        this.position = this.position + 1;
        this.SkipWhitespace();
        if (this.position >= this.tokens.Length
            || this.tokens[this.position].GetKind()
                != CssTokenKind.CloseParenthesis)
        {
            this.success = false;
            return null;
        }

        this.position = this.position + 1;
        return this.CreateUrlNode(token, value);
    }

    internal CssValueNode? CreateUrlNode(CssToken token, string value)
    {
        HtmlSanitizerUrlDecision decision = HtmlSanitizerUrls.EvaluateValue(
            value, this.urlPolicy, this.limits, this.cancellationToken);
        if (!decision.IsAllowed())
        {
            this.success = false;
            return null;
        }

        return new CssValueNode(CssValueNodeKind.Url, token, "",
            decision.GetValue(), new CssValueNode[0]);
    }

    internal void SkipWhitespace()
    {
        while (this.position < this.tokens.Length
            && this.tokens[this.position].GetKind()
                == CssTokenKind.Whitespace)
        {
            this.CheckCancellation();
            this.position = this.position + 1;
        }
    }

    internal void CheckCancellation()
    {
        this.cancellationToken.ThrowIfCancellationRequested();
    }

    internal static CssValueNode CreateNode(CssValueNodeKind kind, CssToken token)
    {
        return new CssValueNode(kind, token, "", "",
            new CssValueNode[0]);
    }

    internal static CssToken[] Copy(CssToken[] values)
    {
        CssToken[] result = new CssToken[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A CSS value token collection cannot contain null.",
                    "tokensValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static string AsciiLower(string value)
    {
        char[] result = value.ToCharArray();
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Ascii.ToLower(result[index]);
            index = index + 1;
        }

        return new string(result);
    }
}
