using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CddlAbnfControlMatcher
{
    internal CddlCoreMatcher Matcher;
    internal CddlValidationState State;

    internal CddlAbnfControlMatcher(
        CddlCoreMatcher matcher,
        CddlValidationState state)
    {
        Matcher = matcher;
        State = state;
    }

    internal CddlMatchResult MatchAbnfControl(
        CddlNode node,
        CborValue value,
        bool byteMode)
    {
        CborValue? controller = new CddlComputedLiteralMatcher(
            Matcher,
            State
        ).EvaluateSingleValue(node.Children[1]);
        string? source = DecodeController(controller);
        if (source == null)
        {
            return Matcher.RejectInvalidSchema(
                node,
                "An ABNF controller must be one valid UTF-8 string literal."
            );
        }
        CddlAbnfParser parser = new CddlAbnfParser(source, byteMode);
        CddlAbnfGrammar? grammar = parser.ParseGrammar();
        if (grammar == null)
        {
            return Matcher.RejectInvalidSchema(
                node,
                "The ABNF controller is invalid: "
                    + (parser.Error ?? "invalid ABNF grammar")
            );
        }
        if (!State.TakePatternStates(grammar.NodeCount, node))
        {
            return Matcher.CreateNoIssueRejection();
        }
        int[]? units = CreateInputUnits(value, byteMode);
        if (units == null)
        {
            return RejectInputEncoding(node, value, byteMode);
        }
        bool matches = new CddlAbnfMatcher(
            grammar,
            units,
            State,
            node
        ).Matches();
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
            "The string does not match the embedded ABNF grammar.",
            node,
            byteMode
                ? "a byte sequence matching the .abnfb grammar"
                : "Unicode scalar values matching the .abnf grammar",
            Matcher.DescribeValue(value)
        );
        return Matcher.CreateRejection(issue);
    }

    internal string? DecodeController(CborValue? controller)
    {
        if (controller == null)
        {
            return null;
        }
        if (controller.Kind == CborValueKind.TextString)
        {
            string text = controller.TextStringValue ?? "";
            return CreateUnicodeUnits(text) == null ? null : text;
        }
        if (controller.Kind == CborValueKind.ByteString)
        {
            byte[] bytes = controller.ByteStringValue ?? [];
            CborUtf8Result decoded = new CborUtf8().ProcessChunk(bytes, true);
            if (decoded.Status == CborReadStatus.Done)
            {
                return decoded.Text;
            }
        }
        return null;
    }

    internal int[]? CreateInputUnits(CborValue value, bool byteMode)
    {
        if (byteMode)
        {
            byte[]? bytes = CreateInputBytes(value);
            if (bytes == null)
            {
                return null;
            }
            int[] units = new int[bytes.Length];
            int index = 0;
            while (index < bytes.Length)
            {
                units[index] = bytes[index];
                index = index + 1;
            }
            return units;
        }
        if (value.Kind == CborValueKind.TextString)
        {
            return CreateUnicodeUnits(value.TextStringValue ?? "");
        }
        if (value.Kind == CborValueKind.ByteString)
        {
            byte[] bytes = value.ByteStringValue ?? [];
            CborUtf8Result decoded = new CborUtf8().ProcessChunk(bytes, true);
            if (decoded.Status == CborReadStatus.Done)
            {
                return CreateUnicodeUnits(decoded.Text);
            }
        }
        return null;
    }

    internal byte[]? CreateInputBytes(CborValue value)
    {
        if (value.Kind == CborValueKind.ByteString)
        {
            return value.ByteStringValue ?? [];
        }
        if (value.Kind == CborValueKind.TextString)
        {
            string text = value.TextStringValue ?? "";
            if (CreateUnicodeUnits(text) == null)
            {
                return null;
            }
            return Encoding.UTF8.GetBytes(text);
        }
        return null;
    }

    internal int[]? CreateUnicodeUnits(string text)
    {
        List<int> units = new List<int>();
        int index = 0;
        while (index < text.Length)
        {
            int current = text[index];
            if (current >= 0xd800 && current <= 0xdbff)
            {
                if (index + 1 >= text.Length)
                {
                    return null;
                }
                int low = text[index + 1];
                if (low < 0xdc00 || low > 0xdfff)
                {
                    return null;
                }
                int scalar = 0x10000
                    + ((current - 0xd800) << 10)
                    + (low - 0xdc00);
                units.Add(scalar);
                index = index + 2;
                continue;
            }
            if (current >= 0xdc00 && current <= 0xdfff)
            {
                return null;
            }
            units.Add(current);
            index = index + 1;
        }
        return units.ToArray();
    }

    internal CddlMatchResult RejectInputEncoding(
        CddlNode node,
        CborValue value,
        bool byteMode)
    {
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            byteMode
                ? "The string cannot be interpreted as ABNF bytes."
                : "The string is not valid UTF-8 Unicode scalar data.",
            node,
            byteMode ? "a byte string" : "valid UTF-8 text",
            Matcher.DescribeValue(value)
        );
        return Matcher.CreateRejection(issue);
    }
}
