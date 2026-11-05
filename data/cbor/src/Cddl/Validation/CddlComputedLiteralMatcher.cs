using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace cbor;

internal class CddlComputedLiteralMatcher
{
    internal CddlCoreMatcher Matcher;
    internal CddlValidationState State;

    internal CddlComputedLiteralMatcher(
        CddlCoreMatcher matcher,
        CddlValidationState state)
    {
        Matcher = matcher;
        State = state;
    }

    internal CddlMatchResult MatchComputedLiteral(
        CddlNode node,
        CborValue value)
    {
        List<CborValue> candidates = new List<CborValue>();
        bool complete = CollectValues(
            node,
            candidates,
            new List<CddlRule>()
        );
        if (State.Diagnostics.HasErrors)
        {
            return Matcher.CreateNoIssueRejection();
        }
        if (!complete || candidates.Count == 0)
        {
            return Matcher.RejectInvalidSchema(
                node,
                "A computed-literal control requires finite literal operands."
            );
        }
        int index = 0;
        while (index < candidates.Count)
        {
            if (ComputedValuesEqual(candidates[index], value))
            {
                return Matcher.CreateMatch();
            }
            index = index + 1;
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The CBOR value does not equal a computed CDDL literal.",
            node,
            "a value computed by " + (node.ControlName ?? "the control"),
            Matcher.DescribeValue(value)
        );
        return Matcher.CreateRejection(issue);
    }

    internal CborValue? EvaluateSingleValue(CddlNode node)
    {
        List<CborValue> values = new List<CborValue>();
        bool complete = CollectValues(
            node,
            values,
            new List<CddlRule>()
        );
        if (!complete || values.Count != 1)
        {
            return null;
        }
        return values[0];
    }

    internal bool CollectValues(
        CddlNode node,
        List<CborValue> values,
        List<CddlRule> visited)
    {
        if (!State.TakeStep(node))
        {
            return false;
        }
        if (node.Kind == CddlNodeKind.Literal)
        {
            CborValue? literal = CreateLiteralValue(node);
            if (literal == null)
            {
                return false;
            }
            values.Add(literal);
            return true;
        }
        if (node.Kind == CddlNodeKind.Representation)
        {
            CborValue? representation = CreateRepresentationValue(node);
            if (representation == null)
            {
                return false;
            }
            values.Add(representation);
            return true;
        }
        if (node.Kind == CddlNodeKind.TypeChoice)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                if (!State.TakeAlternative(node))
                {
                    return false;
                }
                if (!CollectValues(node.Children[index], values, visited))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        if (node.Kind == CddlNodeKind.ParenthesizedType
            && node.Children.Count == 1)
        {
            return CollectValues(node.Children[0], values, visited);
        }
        if ((node.Kind == CddlNodeKind.RuleReference
                || node.Kind == CddlNodeKind.Unwrap)
            && node.Name != null)
        {
            return CollectReferenceValues(node, values, visited);
        }
        if (node.Kind == CddlNodeKind.Control
            && IsComputedControl(node.ControlName))
        {
            return CollectControlValues(node, values, visited);
        }
        return false;
    }

    internal bool CollectReferenceValues(
        CddlNode node,
        List<CborValue> values,
        List<CddlRule> visited)
    {
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null
            || rule.Value == null
            || Matcher.ContainsRule(visited, rule))
        {
            return false;
        }
        visited.Add(rule);
        bool complete = CollectValues(rule.Value, values, visited);
        visited.RemoveAt(visited.Count - 1);
        return complete;
    }

    internal bool CollectControlValues(
        CddlNode node,
        List<CborValue> values,
        List<CddlRule> visited)
    {
        if (node.Children.Count != 2)
        {
            return false;
        }
        List<CborValue> targets = new List<CborValue>();
        List<CborValue> controllers = new List<CborValue>();
        if (!CollectValues(node.Children[0], targets, visited)
            || !CollectValues(node.Children[1], controllers, visited))
        {
            return false;
        }
        int targetIndex = 0;
        while (targetIndex < targets.Count)
        {
            int controllerIndex = 0;
            while (controllerIndex < controllers.Count)
            {
                if (!State.TakeAlternative(node))
                {
                    return false;
                }
                CborValue? computed = CreateComputedResult(
                    node.ControlName,
                    targets[targetIndex],
                    controllers[controllerIndex]
                );
                if (computed != null)
                {
                    values.Add(computed);
                }
                controllerIndex = controllerIndex + 1;
            }
            targetIndex = targetIndex + 1;
        }
        return true;
    }

    internal bool IsComputedControl(string? name)
    {
        return name == CddlControlNames.Plus
            || name == CddlControlNames.Concatenate
            || name == CddlControlNames.DedentedConcatenate;
    }

    internal CborValue? CreateComputedResult(
        string? name,
        CborValue target,
        CborValue controller)
    {
        if (name == CddlControlNames.Plus)
        {
            return CreateNumericResult(target, controller);
        }
        if (name == CddlControlNames.Concatenate
            || name == CddlControlNames.DedentedConcatenate)
        {
            return CreateStringResult(
                target,
                controller,
                name == CddlControlNames.DedentedConcatenate
            );
        }
        return null;
    }

    internal CborValue? CreateNumericResult(
        CborValue target,
        CborValue controller)
    {
        bool targetInteger = IsIntegerValue(target);
        bool controllerInteger = IsIntegerValue(controller);
        bool targetFloat = target.Kind == CborValueKind.Float
            && target.FloatValue != null;
        bool controllerFloat = controller.Kind == CborValueKind.Float
            && controller.FloatValue != null;
        if (targetInteger && controllerInteger)
        {
            return CreateIntegerValue(
                target.IntegerValue + controller.IntegerValue
            );
        }
        if (targetInteger && controllerFloat)
        {
            double controllerValue = controller.FloatValue?.Value
                ?? double.NaN;
            if (double.IsNaN(controllerValue)
                || double.IsInfinity(controllerValue))
            {
                return null;
            }
            BigInteger adjustment = new BigInteger(
                Math.Floor(controllerValue)
            );
            return CreateIntegerValue(target.IntegerValue + adjustment);
        }
        if (targetFloat && controllerInteger)
        {
            double targetValue = target.FloatValue?.Value ?? double.NaN;
            double controllerValue = (double)controller.IntegerValue;
            return CreateFloatValue(targetValue + controllerValue);
        }
        if (targetFloat && controllerFloat)
        {
            double targetValue = target.FloatValue?.Value ?? double.NaN;
            double controllerValue = controller.FloatValue?.Value
                ?? double.NaN;
            return CreateFloatValue(targetValue + controllerValue);
        }
        return null;
    }

    internal CborValue? CreateStringResult(
        CborValue target,
        CborValue controller,
        bool dedent)
    {
        byte[]? targetBytes = GetStringBytes(target);
        byte[]? controllerBytes = GetStringBytes(controller);
        if (targetBytes == null || controllerBytes == null)
        {
            return null;
        }
        if (dedent)
        {
            targetBytes = DedentBytes(targetBytes);
            controllerBytes = DedentBytes(controllerBytes);
        }
        if (targetBytes.Length > int.MaxValue - controllerBytes.Length)
        {
            return null;
        }
        byte[] combined = new byte[
            targetBytes.Length + controllerBytes.Length
        ];
        System.Buffer.BlockCopy(
            targetBytes,
            0,
            combined,
            0,
            targetBytes.Length
        );
        System.Buffer.BlockCopy(
            controllerBytes,
            0,
            combined,
            targetBytes.Length,
            controllerBytes.Length
        );
        if (target.Kind == CborValueKind.ByteString)
        {
            CborValue bytes = new CborValue();
            bytes.Kind = CborValueKind.ByteString;
            bytes.ByteStringValue = combined;
            return bytes;
        }
        CborUtf8Result decoded = new CborUtf8().ProcessChunk(combined, true);
        if (decoded.Status != CborReadStatus.Done)
        {
            return null;
        }
        CborValue text = new CborValue();
        text.Kind = CborValueKind.TextString;
        text.TextStringValue = decoded.Text;
        return text;
    }

    internal byte[]? GetStringBytes(CborValue value)
    {
        if (value.Kind == CborValueKind.ByteString)
        {
            return value.ByteStringValue ?? Array.Empty<byte>();
        }
        if (value.Kind == CborValueKind.TextString)
        {
            return Encoding.UTF8.GetBytes(value.TextStringValue ?? "");
        }
        return null;
    }

    internal byte[] DedentBytes(byte[] source)
    {
        int width = FindDedentWidth(source);
        List<byte> result = new List<byte>(source.Length);
        int index = 0;
        bool lineStart = true;
        while (index < source.Length)
        {
            if (lineStart)
            {
                int spaces = 0;
                while (index + spaces < source.Length
                    && source[index + spaces] == 0x20)
                {
                    spaces = spaces + 1;
                }
                int content = index + spaces;
                bool blank = content >= source.Length
                    || source[content] == 0x0a
                    || (source[content] == 0x0d
                        && content + 1 < source.Length
                        && source[content + 1] == 0x0a);
                int remove = spaces;
                if (!blank && width != int.MaxValue && remove > width)
                {
                    remove = width;
                }
                index = index + remove;
                lineStart = false;
                if (index >= source.Length)
                {
                    break;
                }
            }
            byte current = source[index];
            result.Add(current);
            index = index + 1;
            if (current == 0x0a)
            {
                lineStart = true;
            }
        }
        return result.ToArray();
    }

    internal int FindDedentWidth(byte[] source)
    {
        int minimum = int.MaxValue;
        int start = 0;
        while (start < source.Length)
        {
            int end = start;
            while (end < source.Length && source[end] != 0x0a)
            {
                end = end + 1;
            }
            int contentEnd = end;
            if (contentEnd > start && source[contentEnd - 1] == 0x0d)
            {
                contentEnd = contentEnd - 1;
            }
            int spaces = 0;
            while (start + spaces < contentEnd
                && source[start + spaces] == 0x20)
            {
                spaces = spaces + 1;
            }
            if (start + spaces < contentEnd && spaces < minimum)
            {
                minimum = spaces;
            }
            if (end == source.Length)
            {
                break;
            }
            start = end + 1;
        }
        return minimum;
    }

    internal CborValue? CreateLiteralValue(CddlNode node)
    {
        if (node.HasIntegerValue)
        {
            return CreateIntegerValue(node.IntegerValue);
        }
        if (node.HasFloatValue)
        {
            return CreateFloatValue(node.FloatValue);
        }
        if (node.LiteralKind == CddlLiteralKind.TextString)
        {
            CborValue text = new CborValue();
            text.Kind = CborValueKind.TextString;
            text.TextStringValue = node.TextValue ?? "";
            return text;
        }
        if (node.LiteralKind == CddlLiteralKind.Utf8ByteString
            || node.LiteralKind == CddlLiteralKind.HexByteString
            || node.LiteralKind == CddlLiteralKind.Base64UrlByteString)
        {
            CborValue bytes = new CborValue();
            bytes.Kind = CborValueKind.ByteString;
            bytes.ByteStringValue = node.ByteStringValue
                ?? Array.Empty<byte>();
            return bytes;
        }
        return null;
    }

    internal CborValue? CreateRepresentationValue(CddlNode node)
    {
        if (!node.HasArgumentValue
            || node.ArgumentValue < BigInteger.Zero
            || node.ArgumentValue > new BigInteger(23))
        {
            return null;
        }
        if (node.MajorType == 0)
        {
            return CreateIntegerValue(node.ArgumentValue);
        }
        if (node.MajorType == 1)
        {
            return CreateIntegerValue(-BigInteger.One - node.ArgumentValue);
        }
        return null;
    }

    internal CborValue CreateIntegerValue(BigInteger value)
    {
        CborValue result = new CborValue();
        result.Kind = value.Sign < 0
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        result.IntegerValue = value;
        return result;
    }

    internal CborValue CreateFloatValue(double value)
    {
        CborFloatValue floating = new CborFloatValue();
        floating.Value = value;
        floating.Width = CborFloatWidth.Double;
        CborValue result = new CborValue();
        result.Kind = CborValueKind.Float;
        result.FloatWidth = CborFloatWidth.Double;
        result.FloatValue = floating;
        return result;
    }

    internal bool IsIntegerValue(CborValue value)
    {
        return value.Kind == CborValueKind.UnsignedInteger
            || value.Kind == CborValueKind.NegativeInteger;
    }

    internal bool ComputedValuesEqual(CborValue expected, CborValue actual)
    {
        if (expected.Kind != actual.Kind)
        {
            return false;
        }
        if (IsIntegerValue(expected))
        {
            return expected.IntegerValue == actual.IntegerValue;
        }
        if (expected.Kind == CborValueKind.Float)
        {
            return expected.FloatValue != null
                && actual.FloatValue != null
                && Matcher.NumericFloatsEqual(
                    expected.FloatValue.Value,
                    actual.FloatValue.Value
                );
        }
        if (expected.Kind == CborValueKind.TextString)
        {
            return expected.TextStringValue == actual.TextStringValue;
        }
        if (expected.Kind == CborValueKind.ByteString)
        {
            return Matcher.ByteArraysEqual(
                expected.ByteStringValue,
                actual.ByteStringValue
            );
        }
        return false;
    }
}
