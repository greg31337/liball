using System;

namespace html;

internal class HtmlNthExpressionParseResult
{
    internal HtmlNthExpression? expression;
    internal bool overflow;

    internal HtmlNthExpressionParseResult(HtmlNthExpression? expression,
        bool overflow)
    {
        this.expression = expression;
        this.overflow = overflow;
    }

    internal bool IsSuccess()
    {
        return this.expression != null;
    }

    internal bool WasOverflow()
    {
        return this.overflow;
    }

    internal HtmlNthExpression? GetExpression()
    {
        return this.expression;
    }
}

internal class HtmlNthExpression
{
    internal int step;
    internal int offset;

    internal HtmlNthExpression(int step, int offset)
    {
        this.step = step;
        this.offset = offset;
    }

    internal static HtmlNthExpressionParseResult Parse(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        AnPlusBScanResult parsed = NumberParser.ParseAnPlusB(value.AsSpan());
        if (!parsed.IsSuccess())
        {
            return new HtmlNthExpressionParseResult(null,
                parsed.WasOverflow());
        }

        return new HtmlNthExpressionParseResult(new HtmlNthExpression(
            parsed.GetStep(), parsed.GetOffset()), false);
    }

    internal int GetStep()
    {
        return this.step;
    }

    internal int GetOffset()
    {
        return this.offset;
    }

    internal bool Matches(int index)
    {
        if (index <= 0)
        {
            return false;
        }

        long difference = (long)index - this.offset;
        if (this.step == 0)
        {
            return difference == 0;
        }

        if (this.step > 0 && difference < 0
            || this.step < 0 && difference > 0)
        {
            return false;
        }

        return difference % this.step == 0;
    }
}
