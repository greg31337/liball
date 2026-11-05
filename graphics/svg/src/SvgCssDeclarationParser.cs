namespace svg;

internal class SvgShorthandToken
{
    internal SvgCssTokenKind Kind;
    internal string Value;
    internal string RawText;
    internal double NumberValue;
    internal string Unit;
    internal bool IsFunction;

    internal SvgShorthandToken(SvgCssTokenKind kind, string value, string rawText, double numberValue, string unit, bool isFunction)
    {
        Kind = kind;
        Value = value;
        RawText = rawText;
        NumberValue = numberValue;
        Unit = unit;
        IsFunction = isFunction;
    }
}
