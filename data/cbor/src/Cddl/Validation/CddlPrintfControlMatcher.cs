using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace cbor;

internal class CddlPrintfControlMatcher
{
    internal CddlCoreMatcher Matcher;
    internal CddlValidationState State;
    internal CddlNode? ControlNode;
    internal string Target = "";
    internal List<CddlNode> Arguments = new List<CddlNode>();
    internal List<CddlPrintfPart> Parts = new List<CddlPrintfPart>();

    internal CddlPrintfControlMatcher(
        CddlCoreMatcher matcher,
        CddlValidationState state)
    {
        Matcher = matcher;
        State = state;
    }

    internal CddlMatchResult MatchPrintfControl(
        CddlNode node,
        CborValue value)
    {
        ControlNode = node;
        Target = value.TextStringValue ?? "";
        CddlNode? array = ResolveControllerArray(
            node.Children[1],
            new List<CddlRule>()
        );
        CddlNode? group = array == null || array.Children.Count != 1
            ? null
            : ResolveControllerGroup(array.Children[0]);
        if (group == null || !CollectArguments(group))
        {
            return Matcher.RejectInvalidSchema(
                node,
                "A printf controller must be a fixed array of format and argument entries."
            );
        }
        CborValue? formatValue = new CddlComputedLiteralMatcher(
            Matcher,
            State
        ).EvaluateSingleValue(Arguments[0]);
        if (formatValue == null
            || formatValue.Kind != CborValueKind.TextString
            || formatValue.TextStringValue == null)
        {
            return Matcher.RejectInvalidSchema(
                node,
                "The first printf controller entry must be one text singleton."
            );
        }
        CddlPrintfParser parser = new CddlPrintfParser(
            formatValue.TextStringValue
        );
        if (!State.TakePatternStates(
                formatValue.TextStringValue.Length,
                node))
        {
            return Matcher.CreateNoIssueRejection();
        }
        if (!parser.ParseFormat())
        {
            return Matcher.RejectInvalidSchema(
                node,
                parser.Error ?? "The printf format is invalid."
            );
        }
        if (parser.ArgumentCount != Arguments.Count)
        {
            return Matcher.RejectInvalidSchema(
                node,
                "The printf format and controller argument count differ."
            );
        }
        Parts = parser.Parts;
        bool matches = MatchPart(0, 0);
        if (State.Diagnostics.HasErrors)
        {
            return Matcher.CreateNoIssueRejection();
        }
        if (matches)
        {
            return Matcher.CreateMatch();
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The text is not produced by the CDDL printf controller.",
            node,
            formatValue.TextStringValue,
            Target
        );
        return Matcher.CreateRejection(issue);
    }

    internal bool CollectArguments(CddlNode group)
    {
        int index = 0;
        while (index < group.Children.Count)
        {
            CddlNode entry = group.Children[index];
            if (entry.Kind != CddlNodeKind.GroupEntry
                || entry.MinimumOccurrence != 1
                || entry.MaximumOccurrence != 1
                || entry.Children.Count == 0)
            {
                return false;
            }
            Arguments.Add(entry.Children[entry.Children.Count - 1]);
            index = index + 1;
        }
        return Arguments.Count > 0;
    }

    internal CddlNode? ResolveControllerArray(
        CddlNode node,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.Array)
        {
            return node;
        }
        if ((node.Kind == CddlNodeKind.ParenthesizedType
                || node.Kind == CddlNodeKind.TypeChoice)
            && node.Children.Count == 1)
        {
            return ResolveControllerArray(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null
                && rule.Value != null
                && !Matcher.ContainsRule(visited, rule))
            {
                visited.Add(rule);
                CddlNode? resolved = ResolveControllerArray(
                    rule.Value,
                    visited
                );
                visited.RemoveAt(visited.Count - 1);
                return resolved;
            }
        }
        return null;
    }

    internal CddlNode? ResolveControllerGroup(CddlNode node)
    {
        CddlNode current = node;
        while ((current.Kind == CddlNodeKind.ParenthesizedGroup
                || current.Kind == CddlNodeKind.GroupChoice)
            && current.Children.Count == 1)
        {
            current = current.Children[0];
        }
        return current.Kind == CddlNodeKind.Group ? current : null;
    }

    internal bool MatchPart(int partIndex, int targetOffset)
    {
        CddlNode? node = ControlNode;
        if (node == null || !State.EnterDataNesting(node))
        {
            return false;
        }
        bool result;
        if (partIndex >= Parts.Count)
        {
            result = targetOffset == Target.Length;
        }
        else
        {
            CddlPrintfPart part = Parts[partIndex];
            if (part.Specifier == '\0')
            {
                result = MatchLiteralPart(part, partIndex, targetOffset);
            }
            else
            {
                result = MatchDirectivePart(part, partIndex, targetOffset);
            }
        }
        State.LeaveDataNesting();
        return result;
    }

    internal bool MatchLiteralPart(
        CddlPrintfPart part,
        int partIndex,
        int targetOffset)
    {
        if (targetOffset > Target.Length - part.Literal.Length
            || string.CompareOrdinal(
                Target,
                targetOffset,
                part.Literal,
                0,
                part.Literal.Length) != 0)
        {
            return false;
        }
        return MatchPart(
            partIndex + 1,
            targetOffset + part.Literal.Length
        );
    }

    internal bool MatchDirectivePart(
        CddlPrintfPart part,
        int partIndex,
        int targetOffset)
    {
        int targetBytes = Encoding.UTF8.GetByteCount(Target);
        bool integer = part.Specifier != 's' && part.Specifier != 'c';
        if (part.Width > targetBytes
            || integer && part.Precision > Target.Length)
        {
            return false;
        }
        CborValue? singleton = new CddlComputedLiteralMatcher(
            Matcher,
            State
        ).EvaluateSingleValue(Arguments[part.ArgumentIndex]);
        if (singleton != null)
        {
            string? formatted = FormatValue(part, singleton);
            if (formatted == null
                || targetOffset > Target.Length - formatted.Length
                || string.CompareOrdinal(
                    Target,
                    targetOffset,
                    formatted,
                    0,
                    formatted.Length) != 0)
            {
                return false;
            }
            return MatchPart(
                partIndex + 1,
                targetOffset + formatted.Length
            );
        }
        int end = targetOffset;
        while (end <= Target.Length)
        {
            if (IsTextBoundary(end)
                && CandidateEndIsViable(partIndex, end)
                && State.TakeAlternative(ControlNode))
            {
                string field = Target.Substring(
                    targetOffset,
                    end - targetOffset
                );
                CborValue? candidate = ParseField(part, field);
                if (candidate != null)
                {
                    int checkpoint = State.CreateControlResultCheckpoint();
                    CddlMatchResult argument = Matcher.MatchNode(
                        Arguments[part.ArgumentIndex],
                        candidate
                    );
                    string? formatted = FormatValue(part, candidate);
                    if (argument.IsMatch
                        && formatted == field
                        && MatchPart(partIndex + 1, end))
                    {
                        return true;
                    }
                    State.RestoreControlResults(checkpoint);
                    if (State.Diagnostics.HasErrors)
                    {
                        return false;
                    }
                }
            }
            else if (State.Diagnostics.HasErrors)
            {
                return false;
            }
            end = end + 1;
        }
        return false;
    }

    internal bool CandidateEndIsViable(int partIndex, int end)
    {
        if (partIndex + 1 >= Parts.Count)
        {
            return end == Target.Length;
        }
        CddlPrintfPart next = Parts[partIndex + 1];
        if (next.Specifier != '\0')
        {
            return true;
        }
        return end <= Target.Length - next.Literal.Length
            && string.CompareOrdinal(
                Target,
                end,
                next.Literal,
                0,
                next.Literal.Length) == 0;
    }

    internal bool IsTextBoundary(int offset)
    {
        if (offset <= 0 || offset >= Target.Length)
        {
            return true;
        }
        return !char.IsHighSurrogate(Target[offset - 1])
            || !char.IsLowSurrogate(Target[offset]);
    }

    internal CborValue? ParseField(CddlPrintfPart part, string field)
    {
        if (!State.TakePatternStates(
                field.Length == 0 ? 1 : field.Length,
                ControlNode))
        {
            return null;
        }
        if (part.Specifier == 's')
        {
            CborValue text = new CborValue();
            text.Kind = CborValueKind.TextString;
            text.TextStringValue = field;
            return text;
        }
        string unpadded = RemoveFieldPadding(part, field);
        if (part.Specifier == 'c')
        {
            CborValue? character = ParseCharacter(field);
            return character ?? ParseCharacter(unpadded);
        }
        return ParseInteger(part, unpadded);
    }

    internal string RemoveFieldPadding(CddlPrintfPart part, string field)
    {
        int start = 0;
        int end = field.Length;
        if (part.LeftAligned)
        {
            while (end > start && field[end - 1] == ' ')
            {
                end = end - 1;
            }
        }
        else
        {
            while (start < end && field[start] == ' ')
            {
                start = start + 1;
            }
        }
        return field.Substring(start, end - start);
    }

    internal CborValue? ParseCharacter(string text)
    {
        if (text.Length == 0 || text.Length > 2)
        {
            return null;
        }
        int scalar;
        if (text.Length == 1)
        {
            if (char.IsSurrogate(text[0]))
            {
                return null;
            }
            scalar = text[0];
        }
        else
        {
            if (!char.IsHighSurrogate(text[0])
                || !char.IsLowSurrogate(text[1]))
            {
                return null;
            }
            scalar = char.ConvertToUtf32(text[0], text[1]);
        }
        CborValue value = new CborValue();
        value.Kind = CborValueKind.UnsignedInteger;
        value.IntegerValue = new BigInteger(scalar);
        return value;
    }

    internal CborValue? ParseInteger(
        CddlPrintfPart part,
        string text)
    {
        bool signed = part.Specifier == 'd' || part.Specifier == 'i';
        bool negative = false;
        int offset = 0;
        if (signed && offset < text.Length
            && (text[offset] == '-'
                || text[offset] == '+'
                || text[offset] == ' '))
        {
            negative = text[offset] == '-';
            offset = offset + 1;
        }
        int numberBase = IntegerBase(part.Specifier);
        if ((part.Specifier == 'x' || part.Specifier == 'X')
            && offset + 1 < text.Length
            && text[offset] == '0'
            && (text[offset + 1] == 'x' || text[offset + 1] == 'X'))
        {
            offset = offset + 2;
        }
        if ((part.Specifier == 'b' || part.Specifier == 'B')
            && offset + 1 < text.Length
            && text[offset] == '0'
            && (text[offset + 1] == 'b' || text[offset + 1] == 'B'))
        {
            offset = offset + 2;
        }
        BigInteger integer = BigInteger.Zero;
        int digitCount = 0;
        while (offset < text.Length)
        {
            int digit = IntegerDigit(text[offset]);
            if (digit < 0 || digit >= numberBase)
            {
                return null;
            }
            integer = integer * numberBase + digit;
            digitCount = digitCount + 1;
            offset = offset + 1;
        }
        if (digitCount == 0)
        {
            if (part.Precision != 0)
            {
                return null;
            }
            integer = BigInteger.Zero;
        }
        if (negative)
        {
            integer = -integer;
        }
        CborValue value = new CborValue();
        value.Kind = integer.Sign < 0
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        value.IntegerValue = integer;
        return value;
    }

    internal int IntegerDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    internal int IntegerBase(char specifier)
    {
        if (specifier == 'o')
        {
            return 8;
        }
        if (specifier == 'x' || specifier == 'X')
        {
            return 16;
        }
        if (specifier == 'b' || specifier == 'B')
        {
            return 2;
        }
        return 10;
    }

    internal string? FormatValue(CddlPrintfPart part, CborValue value)
    {
        if (part.Specifier == 's')
        {
            if (value.Kind != CborValueKind.TextString)
            {
                return null;
            }
            string text = value.TextStringValue ?? "";
            if (part.Precision >= 0)
            {
                text = TruncateUtf8(text, part.Precision);
            }
            return ApplyTextWidth(part, text);
        }
        if (part.Specifier == 'c')
        {
            if ((value.Kind != CborValueKind.UnsignedInteger
                    && value.Kind != CborValueKind.NegativeInteger)
                || value.IntegerValue < BigInteger.Zero
                || value.IntegerValue > new BigInteger(1114111))
            {
                return null;
            }
            int scalar = (int)value.IntegerValue;
            if (scalar >= 55296 && scalar <= 57343)
            {
                return null;
            }
            return ApplyTextWidth(part, char.ConvertFromUtf32(scalar));
        }
        return FormatInteger(part, value);
    }

    internal string? FormatInteger(
        CddlPrintfPart part,
        CborValue value)
    {
        if (value.Kind != CborValueKind.UnsignedInteger
            && value.Kind != CborValueKind.NegativeInteger)
        {
            return null;
        }
        bool signed = part.Specifier == 'd' || part.Specifier == 'i';
        if (!signed && value.IntegerValue.Sign < 0)
        {
            return null;
        }
        BigInteger magnitude = value.IntegerValue;
        string sign = "";
        if (signed && magnitude.Sign < 0)
        {
            sign = "-";
            magnitude = BigInteger.Abs(magnitude);
        }
        else if (signed && part.ShowSign)
        {
            sign = "+";
        }
        else if (signed && part.SpaceSign)
        {
            sign = " ";
        }
        int numberBase = IntegerBase(part.Specifier);
        bool upper = part.Specifier == 'X' || part.Specifier == 'B';
        string digits = FormatDigits(magnitude, numberBase, upper);
        if (part.Precision == 0 && magnitude.IsZero)
        {
            digits = "";
        }
        if (part.Precision > digits.Length)
        {
            digits = new string('0', part.Precision - digits.Length)
                + digits;
        }
        string prefix = "";
        if (part.Alternate && part.Specifier == 'o'
            && (digits.Length == 0 || digits[0] != '0'))
        {
            prefix = "0";
        }
        else if (part.Alternate && !magnitude.IsZero
            && (part.Specifier == 'x' || part.Specifier == 'X'))
        {
            prefix = part.Specifier == 'X' ? "0X" : "0x";
        }
        else if (part.Alternate && !magnitude.IsZero
            && (part.Specifier == 'b' || part.Specifier == 'B'))
        {
            prefix = part.Specifier == 'B' ? "0B" : "0b";
        }
        int contentLength = sign.Length + prefix.Length + digits.Length;
        if (part.Width > contentLength
            && part.ZeroPadded
            && !part.LeftAligned
            && part.Precision < 0)
        {
            digits = new string('0', part.Width - contentLength) + digits;
        }
        string formatted = sign + prefix + digits;
        return ApplyAsciiWidth(part, formatted);
    }

    internal string FormatDigits(
        BigInteger value,
        int numberBase,
        bool upper)
    {
        if (numberBase == 10)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        if (value.IsZero)
        {
            return "0";
        }
        const string lowerDigits = "0123456789abcdef";
        const string upperDigits = "0123456789ABCDEF";
        string alphabet = upper ? upperDigits : lowerDigits;
        StringBuilder reversed = new StringBuilder();
        BigInteger remaining = value;
        while (remaining > BigInteger.Zero)
        {
            BigInteger remainder = remaining % numberBase;
            reversed.Append(alphabet[(int)remainder]);
            remaining = remaining / numberBase;
        }
        StringBuilder result = new StringBuilder(reversed.Length);
        int index = reversed.Length - 1;
        while (index >= 0)
        {
            result.Append(reversed[index]);
            index = index - 1;
        }
        return result.ToString();
    }

    internal string ApplyAsciiWidth(CddlPrintfPart part, string value)
    {
        if (part.Width <= value.Length)
        {
            return value;
        }
        string padding = new string(' ', part.Width - value.Length);
        return part.LeftAligned ? value + padding : padding + value;
    }

    internal string ApplyTextWidth(CddlPrintfPart part, string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (part.Width <= byteCount)
        {
            return value;
        }
        string padding = new string(' ', part.Width - byteCount);
        return part.LeftAligned ? value + padding : padding + value;
    }

    internal string TruncateUtf8(string value, int maximumBytes)
    {
        int offset = 0;
        int bytes = 0;
        while (offset < value.Length)
        {
            int characterLength = 1;
            int characterBytes;
            char current = value[offset];
            if (char.IsHighSurrogate(current)
                && offset + 1 < value.Length
                && char.IsLowSurrogate(value[offset + 1]))
            {
                characterLength = 2;
                characterBytes = 4;
            }
            else if (current <= 127)
            {
                characterBytes = 1;
            }
            else if (current <= 2047)
            {
                characterBytes = 2;
            }
            else
            {
                characterBytes = 3;
            }
            if (characterBytes > maximumBytes - bytes)
            {
                break;
            }
            bytes = bytes + characterBytes;
            offset = offset + characterLength;
        }
        return value.Substring(0, offset);
    }
}
