using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace cbor;

internal class CddlCoreMatcher
{
    internal const int TypeMemoContext = 1;
    internal const int ArrayGroupMemoContext = 2;
    internal const int MapGroupMemoContext = 3;
    internal CddlValidationState State;
    internal List<CborValue> ContainerValues = new List<CborValue>();

    internal CddlCoreMatcher(CddlValidationState state)
    {
        State = state;
    }

    internal CddlMatchResult MatchSelectedRule(CborValue value)
    {
        CddlRule? rule = State.SelectedRule;
        if (rule == null || rule.Value == null)
        {
            return RejectInvalidSchema(
                null,
                "The selected rule has no validation expression."
            );
        }
        State.PushInstanceValue(value);
        CddlMatchResult result = MatchTypeRule(rule, value, rule.Value);
        State.PopInstanceValue();
        return result;
    }

    internal CddlMatchResult MatchNode(CddlNode node, CborValue value)
    {
        State.PushInstanceValue(value);
        if (!State.TakeStep(node))
        {
            State.PopInstanceValue();
            return CreateNoIssueRejection();
        }
        int nodeIndex = FindNodeIndex(node);
        if (nodeIndex >= 0)
        {
            State.SchemaPath.PushNodeIndex(nodeIndex);
        }
        CddlMatchResult result = MatchNodeKind(node, value);
        if (nodeIndex >= 0)
        {
            State.SchemaPath.PopSegment();
        }
        State.PopInstanceValue();
        return result;
    }

    internal CddlMatchResult MatchNodeKind(CddlNode node, CborValue value)
    {
        switch (node.Kind)
        {
            case CddlNodeKind.Literal:
                return MatchLiteral(node, value);
            case CddlNodeKind.TypeChoice:
                return MatchTypeChoice(node, value);
            case CddlNodeKind.RuleReference:
                return MatchReference(node, value);
            case CddlNodeKind.Range:
                return MatchRange(node, value);
            case CddlNodeKind.Tag:
                return MatchTag(node, value);
            case CddlNodeKind.Representation:
                return MatchRepresentation(node, value);
            case CddlNodeKind.Unwrap:
                return MatchUnwrap(node, value);
            case CddlNodeKind.GroupEnumeration:
                return MatchGroupEnumeration(node, value);
            case CddlNodeKind.ParenthesizedType:
                return MatchParenthesizedType(node, value);
            case CddlNodeKind.Array:
                return MatchArray(node, value);
            case CddlNodeKind.Map:
                return MatchMap(node, value);
            case CddlNodeKind.Control:
                return MatchControl(node, value);
            default:
                return RejectInvalidSchema(
                    node,
                    "The schema node cannot be used as a value type."
                );
        }
    }

    internal CddlMatchResult MatchLiteral(CddlNode node, CborValue value)
    {
        bool matches = false;
        if (State.JsonNumberMode
            && (node.HasIntegerValue || node.HasFloatValue)
            && (value.Kind == CborValueKind.UnsignedInteger
                || value.Kind == CborValueKind.NegativeInteger
                || value.Kind == CborValueKind.Float))
        {
            CddlNumericValue expected = new CddlNumericValue();
            if (node.HasIntegerValue)
            {
                expected.Kind = CddlNumericValue.Integer;
                expected.IntegerValue = node.IntegerValue;
            }
            else
            {
                expected.Kind = CddlNumericValue.Float;
                expected.FloatValue = node.FloatValue;
            }
            matches = CompareNumericValues(
                CreateNumericValue(value),
                expected
            ) == 0;
        }
        else if (node.HasIntegerValue)
        {
            matches = (value.Kind == CborValueKind.UnsignedInteger
                    || value.Kind == CborValueKind.NegativeInteger)
                && value.IntegerValue == node.IntegerValue;
        }
        else if (node.HasFloatValue)
        {
            CborFloatValue? actualFloat = value.FloatValue;
            matches = value.Kind == CborValueKind.Float
                && actualFloat != null
                && NumericFloatsEqual(actualFloat.Value, node.FloatValue);
        }
        else if (node.LiteralKind == CddlLiteralKind.TextString)
        {
            matches = value.Kind == CborValueKind.TextString
                && value.TextStringValue == node.TextValue;
        }
        else
        {
            matches = value.Kind == CborValueKind.ByteString
                && ByteArraysEqual(value.ByteStringValue, node.ByteStringValue);
        }
        if (matches)
        {
            return CreateMatch();
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.LiteralMismatch,
            "The CBOR value does not equal the required CDDL literal.",
            node,
            DescribeLiteral(node),
            DescribeValue(value)
        );
        return CreateRejection(issue);
    }

    internal CddlMatchResult MatchTypeChoice(CddlNode node, CborValue value)
    {
        List<CddlValidationIssue> details =
            new List<CddlValidationIssue>();
        int index = 0;
        while (index < node.Children.Count)
        {
            if (!State.TakeAlternative(node))
            {
                return CreateNoIssueRejection();
            }
            State.SchemaPath.PushChoiceIndex(index);
            int controlResultCheckpoint =
                State.CreateControlResultCheckpoint();
            CddlMatchResult branch = MatchNode(node.Children[index], value);
            State.SchemaPath.PopSegment();
            if (branch.IsMatch)
            {
                return CreateMatch();
            }
            State.RestoreControlResults(controlResultCheckpoint);
            if (branch.Issue != null)
            {
                AddBoundedDetail(details, branch.Issue);
            }
            if (State.Diagnostics.HasErrors)
            {
                return CreateNoIssueRejection();
            }
            index = index + 1;
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ChoiceMismatch,
            "No CDDL type-choice alternative matches the CBOR value.",
            node,
            "one matching type-choice alternative",
            DescribeValue(value)
        );
        issue.Details = details.ToArray();
        return CreateRejection(issue);
    }

    internal CddlMatchResult MatchReference(CddlNode node, CborValue value)
    {
        string? name = node.Name;
        if (name == null)
        {
            return RejectReference(
                node,
                "The rule reference has no target name."
            );
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null)
        {
            if (name.Length > 0 && name[0] == '$')
            {
                CddlValidationIssue socketIssue = State.CreateIssue(
                    CddlValidationCode.ChoiceMismatch,
                    "The empty CDDL socket has no matching plug.",
                    node,
                    "a socket plug",
                    DescribeValue(value)
                );
                return CreateRejection(socketIssue);
            }
            return RejectReference(
                node,
                "The referenced CDDL rule is unavailable."
            );
        }
        if (rule.Kind != CddlRuleKind.Type || rule.Value == null)
        {
            return RejectReference(
                node,
                "The referenced CDDL rule is not a complete type rule."
            );
        }
        return MatchTypeRule(rule, value, node);
    }

    internal CddlMatchResult MatchTypeRule(
        CddlRule rule,
        CborValue value,
        CddlNode node)
    {
        CddlValidationMemoEntry? memo = State.BeginMemo(
            rule,
            value,
            0,
            TypeMemoContext,
            node
        );
        if (memo == null)
        {
            return CreateNoIssueRejection();
        }
        if (memo.Status == CddlValidationMemoEntry.Matched)
        {
            return CreateMatch();
        }
        if (memo.Status == CddlValidationMemoEntry.Rejected)
        {
            if (memo.Issues.Length > 0)
            {
                return CreateRejection(memo.Issues[0]);
            }
            return CreateNoIssueRejection();
        }
        if (memo.IsActive)
        {
            return RejectRecursiveCycle(node, rule.Name);
        }
        if (rule.Value == null)
        {
            return RejectReference(
                node,
                "The referenced rule has no validation expression."
            );
        }
        memo.IsActive = true;
        if (!State.EnterRule(rule))
        {
            memo.IsActive = false;
            memo.CompleteRejection([]);
            return CreateNoIssueRejection();
        }
        CddlMatchResult result = MatchNode(rule.Value, value);
        State.LeaveRule();
        memo.IsActive = false;
        if (result.IsMatch)
        {
            memo.CompleteMatch();
        }
        else if (result.Issue != null)
        {
            memo.CompleteRejection([result.Issue]);
        }
        else
        {
            memo.CompleteRejection([]);
        }
        return result;
    }

    internal CddlMatchResult RejectRecursiveCycle(
        CddlNode node,
        string ruleName)
    {
        CddlValidationIssue recursion = State.CreateIssue(
            CddlValidationCode.RecursionLimit,
            "Recursive rule evaluation revisited the same instance state.",
            node,
            "a productive recursive descent",
            ruleName
        );
        return CreateRejection(recursion);
    }

    internal CddlMatchResult MatchControl(CddlNode node, CborValue value)
    {
        if (node.Children.Count != 2 || node.ControlName == null)
        {
            return RejectInvalidSchema(
                node,
                "A CDDL control requires a name, target, and controller."
            );
        }
        State.SchemaPath.PushControlName(node.ControlName);
        bool computed = node.ControlName == CddlControlNames.Plus
            || node.ControlName == CddlControlNames.Concatenate
            || node.ControlName == CddlControlNames.DedentedConcatenate;
        if (!computed)
        {
            CddlMatchResult target = MatchNode(node.Children[0], value);
            if (!target.IsMatch)
            {
                State.SchemaPath.PopSegment();
                return target;
            }
        }
        CddlMatchResult result;
        if (computed)
        {
            result = new CddlComputedLiteralMatcher(
                this,
                State
            ).MatchComputedLiteral(node, value);
        }
        else if (node.ControlName == CddlControlNames.Size)
        {
            result = MatchSizeControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.Bits)
        {
            result = MatchBitsControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.RegularExpression)
        {
            result = MatchRegularExpressionControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.Cbor)
        {
            result = MatchEmbeddedCborControl(node, value, false);
        }
        else if (node.ControlName == CddlControlNames.CborSequence)
        {
            result = MatchEmbeddedCborControl(node, value, true);
        }
        else if (node.ControlName == CddlControlNames.Within
            || node.ControlName == CddlControlNames.And)
        {
            result = MatchIntersectionControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.LessThan
            || node.ControlName == CddlControlNames.LessThanOrEqual
            || node.ControlName == CddlControlNames.GreaterThan
            || node.ControlName == CddlControlNames.GreaterThanOrEqual)
        {
            result = MatchRelationalControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.Equal
            || node.ControlName == CddlControlNames.NotEqual
            || node.ControlName == CddlControlNames.Default)
        {
            result = MatchEqualityControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.Abnf
            || node.ControlName == CddlControlNames.AbnfBytes)
        {
            result = new CddlAbnfControlMatcher(
                this,
                State
            ).MatchAbnfControl(
                node,
                value,
                node.ControlName == CddlControlNames.AbnfBytes
            );
        }
        else if (node.ControlName == CddlControlNames.Feature)
        {
            result = new CddlFeatureControlMatcher(
                this,
                State
            ).MatchFeatureControl(node, value);
        }
        else if (IsByteEncodingControl(node.ControlName))
        {
            result = new CddlTextConversionMatcher(
                this,
                State
            ).MatchByteEncodingControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.Base10)
        {
            result = new CddlTextConversionMatcher(
                this,
                State
            ).MatchBase10Control(node, value);
        }
        else if (node.ControlName == CddlControlNames.Printf)
        {
            result = new CddlPrintfControlMatcher(
                this,
                State
            ).MatchPrintfControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.Json)
        {
            result = new CddlJsonControlMatcher(
                this,
                State
            ).MatchJsonControl(node, value);
        }
        else if (node.ControlName == CddlControlNames.Join)
        {
            result = new CddlJoinControlMatcher(
                this,
                State
            ).MatchJoinControl(node, value);
        }
        else
        {
            result = new CddlCustomControlMatcher(
                this,
                State
            ).MatchCustomControl(node, value);
        }
        State.SchemaPath.PopSegment();
        return result;
    }

    internal bool IsByteEncodingControl(string name)
    {
        return name == CddlControlNames.Base64Url
            || name == CddlControlNames.Base64UrlSloppy
            || name == CddlControlNames.Base64Classic
            || name == CddlControlNames.Base64ClassicSloppy
            || name == CddlControlNames.Base45
            || name == CddlControlNames.Base32
            || name == CddlControlNames.Base32Hex
            || name == CddlControlNames.Hex
            || name == CddlControlNames.HexLower
            || name == CddlControlNames.HexUpper;
    }

    internal CddlMatchResult MatchSizeControl(
        CddlNode node,
        CborValue value)
    {
        CddlNode controller = node.Children[1];
        if (value.Kind == CborValueKind.ByteString
            || value.Kind == CborValueKind.TextString)
        {
            int size;
            if (value.Kind == CborValueKind.ByteString)
            {
                size = value.ByteStringValue == null
                    ? 0
                    : value.ByteStringValue.Length;
            }
            else
            {
                size = Encoding.UTF8.GetByteCount(
                    value.TextStringValue ?? ""
                );
            }
            CborValue sizeValue = CreateUnsignedValue(size);
            CddlMatchResult sizeMatch = MatchNode(controller, sizeValue);
            if (sizeMatch.IsMatch)
            {
                return CreateMatch();
            }
            CddlValidationIssue sizeIssue = State.CreateIssue(
                CddlValidationCode.SizeMismatch,
                "The CBOR string byte length does not match the CDDL size controller.",
                node,
                "a byte length accepted by the size controller",
                size + " byte(s)"
            );
            if (sizeMatch.Issue != null)
            {
                sizeIssue.Details = [sizeMatch.Issue];
            }
            return CreateRejection(sizeIssue);
        }
        if (value.Kind == CborValueKind.UnsignedInteger)
        {
            int minimum = FindUnsignedIntegerSize(value.IntegerValue);
            bool allowed = ControllerAllowsIntegerSize(
                controller,
                minimum,
                new List<CddlRule>()
            );
            if (allowed)
            {
                return CreateMatch();
            }
            CddlValidationIssue integerIssue = State.CreateIssue(
                CddlValidationCode.SizeMismatch,
                "The unsigned integer exceeds the size allowed by the CDDL controller.",
                node,
                "an integer fitting an allowed byte size",
                minimum + " required byte(s)"
            );
            return CreateRejection(integerIssue);
        }
        return RejectInvalidSchema(
            node,
            "The size control target is not a string or unsigned integer."
        );
    }

    internal CddlMatchResult MatchBitsControl(
        CddlNode node,
        CborValue value)
    {
        CddlNode controller = node.Children[1];
        if (value.Kind == CborValueKind.UnsignedInteger)
        {
            BigInteger remaining = value.IntegerValue;
            int bit = 0;
            while (remaining > BigInteger.Zero)
            {
                if ((remaining & BigInteger.One) != BigInteger.Zero)
                {
                    CddlMatchResult bitMatch = MatchBitPosition(
                        controller,
                        bit
                    );
                    if (!bitMatch.IsMatch)
                    {
                        return RejectDisallowedBit(node, bit, bitMatch);
                    }
                }
                remaining = remaining >> 1;
                bit = bit + 1;
            }
            return CreateMatch();
        }
        if (value.Kind == CborValueKind.ByteString)
        {
            byte[] bytes = value.ByteStringValue ?? [];
            int byteIndex = 0;
            while (byteIndex < bytes.Length)
            {
                int bitInByte = 0;
                while (bitInByte < 8)
                {
                    int mask = 1 << bitInByte;
                    if ((bytes[byteIndex] & mask) != 0)
                    {
                        int bit = byteIndex * 8 + bitInByte;
                        CddlMatchResult bitMatch = MatchBitPosition(
                            controller,
                            bit
                        );
                        if (!bitMatch.IsMatch)
                        {
                            return RejectDisallowedBit(
                                node,
                                bit,
                                bitMatch
                            );
                        }
                    }
                    bitInByte = bitInByte + 1;
                }
                byteIndex = byteIndex + 1;
            }
            return CreateMatch();
        }
        return RejectInvalidSchema(
            node,
            "The bits control target is not a byte string or unsigned integer."
        );
    }

    internal CddlMatchResult MatchBitPosition(
        CddlNode controller,
        int bit)
    {
        return MatchNode(controller, CreateUnsignedValue(bit));
    }

    internal CddlMatchResult RejectDisallowedBit(
        CddlNode node,
        int bit,
        CddlMatchResult bitMatch)
    {
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The CBOR value has a bit set outside the CDDL bits controller.",
            node,
            "only allowed bit positions set",
            "bit " + bit + " is set"
        );
        if (bitMatch.Issue != null)
        {
            issue.Details = [bitMatch.Issue];
        }
        return CreateRejection(issue);
    }

    internal CddlMatchResult MatchRegularExpressionControl(
        CddlNode node,
        CborValue value)
    {
        string? pattern = EvaluateTextNode(
            node.Children[1],
            new List<CddlRule>()
        );
        if (pattern == null || value.TextStringValue == null)
        {
            return RejectInvalidSchema(
                node,
                "The regexp controller is not a text-string singleton."
            );
        }
        CddlXsdRegexParser parser = new CddlXsdRegexParser(pattern);
        CddlXsdRegexNode? expression = parser.ParsePattern();
        if (expression == null)
        {
            return RejectInvalidSchema(
                node,
                "The regexp controller is not a valid XML Schema regular expression: "
                    + (parser.Error ?? "invalid pattern")
            );
        }
        if (!State.TakePatternStates(parser.NodeCount, node))
        {
            return CreateNoIssueRejection();
        }
        CddlXsdRegexMatcher matcher = new CddlXsdRegexMatcher(
            State,
            node
        );
        bool matches = matcher.Matches(expression, value.TextStringValue);
        if (State.Diagnostics.HasErrors)
        {
            return CreateNoIssueRejection();
        }
        if (matches)
        {
            return CreateMatch();
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The CBOR text string does not match the XML Schema regular expression.",
            node,
            pattern,
            value.TextStringValue
        );
        return CreateRejection(issue);
    }

    internal CddlMatchResult MatchEmbeddedCborControl(
        CddlNode node,
        CborValue value,
        bool sequence)
    {
        byte[] bytes = value.ByteStringValue ?? [];
        if (!State.TakeEmbeddedBytes(bytes.Length, node))
        {
            return CreateNoIssueRejection();
        }
        CborDecodeOptions options = CreateEmbeddedDecodeOptions(bytes.Length);
        CborDecoder decoder = new CborDecoder();
        if (!sequence)
        {
            CborReadResult decoded = decoder.Decode(bytes, options);
            if (decoded.Status != CborReadStatus.Done
                || decoded.Value == null
                || decoded.Consumed != bytes.Length
                || decoded.Remaining != 0)
            {
                return RejectEmbeddedDecode(node, decoded, -1);
            }
            return MatchEmbeddedValue(node, decoded.Value);
        }

        List<CborReadResult> decodedItems = decoder.DecodeSequence(
            bytes,
            options
        );
        List<CborValue> items = new List<CborValue>();
        int consumed = 0;
        int index = 0;
        while (index < decodedItems.Count)
        {
            CborReadResult decoded = decodedItems[index];
            if (decoded.Status != CborReadStatus.Done
                || decoded.Value == null
                || decoded.Consumed <= 0)
            {
                int itemIndex = decoded.ItemIndex < 0
                    ? index
                    : decoded.ItemIndex;
                return RejectEmbeddedDecode(node, decoded, itemIndex);
            }
            if (decoded.Consumed > bytes.Length - consumed)
            {
                return RejectEmbeddedDecode(node, decoded, index);
            }
            consumed = consumed + decoded.Consumed;
            items.Add(decoded.Value);
            index = index + 1;
        }
        if (consumed != bytes.Length)
        {
            CborReadResult incomplete = new CborReadResult();
            incomplete.Status = CborReadStatus.Error;
            incomplete.ErrorCode = "CBOR_SEQUENCE_NOT_FULLY_CONSUMED";
            incomplete.ErrorMessage =
                "The embedded CBOR Sequence was not fully consumed.";
            incomplete.ErrorOffset = consumed;
            return RejectEmbeddedDecode(node, incomplete, items.Count);
        }

        CborValue sequenceValue = new CborValue();
        sequenceValue.Kind = CborValueKind.Array;
        sequenceValue.ArrayItems = items;
        sequenceValue.LengthKind = CborLengthKind.Definite;
        return MatchEmbeddedValue(node, sequenceValue);
    }

    internal CddlMatchResult MatchEmbeddedValue(
        CddlNode node,
        CborValue value)
    {
        if (!State.EnterDataNesting(node))
        {
            return CreateNoIssueRejection();
        }
        State.InstancePath.PushEmbeddedValue();
        CddlMatchResult result = MatchNode(node.Children[1], value);
        State.InstancePath.PopSegment();
        State.LeaveDataNesting();
        return result;
    }

    internal CddlMatchResult RejectEmbeddedDecode(
        CddlNode node,
        CborReadResult decoded,
        int itemIndex)
    {
        State.InstancePath.PushEmbeddedValue();
        if (itemIndex >= 0)
        {
            State.InstancePath.PushArrayIndex(itemIndex);
        }
        string code = decoded.ErrorCode ?? "CBOR_EMBEDDED_DATA_INVALID";
        string actual = code;
        if (decoded.ErrorOffset >= 0)
        {
            actual = actual + " at embedded byte " + decoded.ErrorOffset;
        }
        if (decoded.ErrorPath != null)
        {
            actual = actual + " at " + decoded.ErrorPath;
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            decoded.ErrorMessage
                ?? "The embedded bytes are not valid CBOR data.",
            node,
            itemIndex < 0
                ? "exactly one complete CBOR data item"
                : "a complete CBOR Sequence",
            actual
        );
        if (itemIndex >= 0)
        {
            State.InstancePath.PopSegment();
        }
        State.InstancePath.PopSegment();
        return CreateRejection(issue);
    }

    internal CborDecodeOptions CreateEmbeddedDecodeOptions(int byteCount)
    {
        CborDecodeOptions options = new CborDecodeOptions();
        options.AllowTrailingData = false;
        options.DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve;
        options.TagPolicy = CborTagPolicy.Preserve;
        options.SharedValuePolicy = CborSharedValuePolicy.Preserve;
        options.ValidationLevel = CborValidationLevel.WellFormedness;
        options.AllowIndefiniteLength = true;
        options.AllowUnknownSimpleValues = true;
        options.RejectInvalidUtf8 = true;
        int remainingDepth = State.Options.MaxNestingDepth
            - State.Budget.NestingDepth;
        if (remainingDepth < 0)
        {
            remainingDepth = 0;
        }
        int itemLimit = byteCount;
        if (itemLimit < 1)
        {
            itemLimit = 1;
        }
        options.MaxNestingDepth = remainingDepth;
        options.MaxTotalItems = itemLimit;
        options.MaxArrayItems = itemLimit;
        options.MaxMapPairs = itemLimit;
        options.MaxStringBytes = byteCount;
        options.MaxTotalBytes = byteCount;
        options.MaxRetainedSourceBytes = 0;
        options.MaxSequenceItems = byteCount;
        options.MaxSequenceBytes = byteCount;
        options.MaxEmbeddedDepth = 0;
        options.MaxValidationIssues = 1;
        options.CollectValidationIssues = false;
        return options;
    }

    internal CddlMatchResult MatchIntersectionControl(
        CddlNode node,
        CborValue value)
    {
        CddlMatchResult controller = MatchNode(node.Children[1], value);
        if (controller.IsMatch)
        {
            return CreateMatch();
        }
        if (State.Diagnostics.HasErrors)
        {
            return CreateNoIssueRejection();
        }
        string name = node.ControlName ?? "intersection";
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The CBOR value does not satisfy both operands of the "
                + name + " control.",
            node,
            "a value matching both control operands",
            DescribeValue(value)
        );
        if (controller.Issue != null)
        {
            issue.Details = [controller.Issue];
        }
        return CreateRejection(issue);
    }

    internal CddlMatchResult MatchRelationalControl(
        CddlNode node,
        CborValue value)
    {
        CddlNumericValue actual = CreateNumericValue(value);
        CddlNumericValue controller = EvaluateNumericNode(
            node.Children[1],
            new List<CddlRule>()
        );
        if (actual.Kind == CddlNumericValue.None
            || controller.Kind == CddlNumericValue.None)
        {
            return RejectInvalidSchema(
                node,
                "A relational control requires numeric values."
            );
        }
        int comparison = CompareNumericValues(actual, controller);
        bool matches = false;
        if (comparison != int.MinValue)
        {
            if (node.ControlName == CddlControlNames.LessThan)
            {
                matches = comparison < 0;
            }
            else if (node.ControlName == CddlControlNames.LessThanOrEqual)
            {
                matches = comparison <= 0;
            }
            else if (node.ControlName == CddlControlNames.GreaterThan)
            {
                matches = comparison > 0;
            }
            else
            {
                matches = comparison >= 0;
            }
        }
        if (matches)
        {
            return CreateMatch();
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The CBOR number does not satisfy the relational control.",
            node,
            (node.ControlName ?? "numeric comparison") + " "
                + DescribeNumericValue(controller),
            DescribeValue(value)
        );
        return CreateRejection(issue);
    }

    internal CddlMatchResult MatchEqualityControl(
        CddlNode node,
        CborValue value)
    {
        CddlMatchResult controller = MatchNode(node.Children[1], value);
        if (State.Diagnostics.HasErrors)
        {
            return CreateNoIssueRejection();
        }
        bool requiresEquality = node.ControlName == CddlControlNames.Equal;
        bool matches = requiresEquality
            ? controller.IsMatch
            : !controller.IsMatch;
        if (matches)
        {
            return CreateMatch();
        }
        string name = node.ControlName ?? "equality";
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The CBOR value does not satisfy the " + name + " control.",
            node,
            requiresEquality
                ? "the exact controller value and CBOR type"
                : "a value different from the exact controller value",
            DescribeValue(value)
        );
        if (requiresEquality && controller.Issue != null)
        {
            issue.Details = [controller.Issue];
        }
        return CreateRejection(issue);
    }

    internal CddlNumericValue CreateNumericValue(CborValue value)
    {
        CddlNumericValue numeric = new CddlNumericValue();
        if (value.Kind == CborValueKind.UnsignedInteger
            || value.Kind == CborValueKind.NegativeInteger)
        {
            numeric.Kind = CddlNumericValue.Integer;
            numeric.IntegerValue = value.IntegerValue;
        }
        else if (value.Kind == CborValueKind.Float
            && value.FloatValue != null)
        {
            numeric.Kind = CddlNumericValue.Float;
            numeric.FloatValue = value.FloatValue.Value;
        }
        return numeric;
    }

    internal int CompareNumericValues(
        CddlNumericValue left,
        CddlNumericValue right)
    {
        if (left.Kind == CddlNumericValue.Integer
            && right.Kind == CddlNumericValue.Integer)
        {
            return left.IntegerValue.CompareTo(right.IntegerValue);
        }
        if (left.Kind == CddlNumericValue.Float
            && right.Kind == CddlNumericValue.Float)
        {
            if (double.IsNaN(left.FloatValue)
                || double.IsNaN(right.FloatValue))
            {
                return int.MinValue;
            }
            return left.FloatValue.CompareTo(right.FloatValue);
        }
        if (left.Kind == CddlNumericValue.Integer
            && right.Kind == CddlNumericValue.Float)
        {
            return CompareIntegerAndFloat(
                left.IntegerValue,
                right.FloatValue
            );
        }
        if (left.Kind == CddlNumericValue.Float
            && right.Kind == CddlNumericValue.Integer)
        {
            int reverse = CompareIntegerAndFloat(
                right.IntegerValue,
                left.FloatValue
            );
            if (reverse == int.MinValue)
            {
                return int.MinValue;
            }
            return -reverse;
        }
        return int.MinValue;
    }

    internal int CompareIntegerAndFloat(BigInteger integer, double floating)
    {
        if (double.IsNaN(floating))
        {
            return int.MinValue;
        }
        if (double.IsPositiveInfinity(floating))
        {
            return -1;
        }
        if (double.IsNegativeInfinity(floating))
        {
            return 1;
        }
        BigInteger truncated = new BigInteger(floating);
        int comparison = integer.CompareTo(truncated);
        if (comparison != 0)
        {
            return comparison;
        }
        double integral = Math.Truncate(floating);
        if (floating > integral)
        {
            return -1;
        }
        if (floating < integral)
        {
            return 1;
        }
        return 0;
    }

    internal string DescribeNumericValue(CddlNumericValue value)
    {
        if (value.Kind == CddlNumericValue.Integer)
        {
            return value.IntegerValue.ToString();
        }
        return value.FloatValue.ToString(
            "R",
            System.Globalization.CultureInfo.InvariantCulture
        );
    }

    internal CborValue CreateUnsignedValue(int value)
    {
        CborValue result = new CborValue();
        result.Kind = CborValueKind.UnsignedInteger;
        result.IntegerValue = new BigInteger(value);
        return result;
    }

    internal int FindUnsignedIntegerSize(BigInteger value)
    {
        int size = 0;
        BigInteger remaining = value;
        while (remaining > BigInteger.Zero)
        {
            size = size + 1;
            remaining = remaining >> 8;
        }
        return size;
    }

    internal bool ControllerAllowsIntegerSize(
        CddlNode node,
        int minimum,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.Control)
        {
            CborValue? computed = new CddlComputedLiteralMatcher(
                this,
                State
            ).EvaluateSingleValue(node);
            if (computed != null
                && (computed.Kind == CborValueKind.UnsignedInteger
                    || computed.Kind == CborValueKind.NegativeInteger))
            {
                return computed.IntegerValue >= new BigInteger(minimum);
            }
            return false;
        }
        if (node.Kind == CddlNodeKind.Literal && node.HasIntegerValue)
        {
            return node.IntegerValue >= new BigInteger(minimum);
        }
        if (node.Kind == CddlNodeKind.Range && node.Children.Count == 2)
        {
            CddlNumericValue lower = EvaluateNumericNode(
                node.Children[0],
                new List<CddlRule>()
            );
            CddlNumericValue upper = EvaluateNumericNode(
                node.Children[1],
                new List<CddlRule>()
            );
            if (lower.Kind != CddlNumericValue.Integer
                || upper.Kind != CddlNumericValue.Integer)
            {
                return false;
            }
            BigInteger candidate = lower.IntegerValue;
            if (candidate < new BigInteger(minimum))
            {
                candidate = new BigInteger(minimum);
            }
            return node.IsRangeUpperExclusive
                ? candidate < upper.IntegerValue
                : candidate <= upper.IntegerValue;
        }
        if (node.Kind == CddlNodeKind.TypeChoice)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                if (ControllerAllowsIntegerSize(
                    node.Children[index],
                    minimum,
                    visited))
                {
                    return true;
                }
                index = index + 1;
            }
            return false;
        }
        if (node.Kind == CddlNodeKind.ParenthesizedType
            && node.Children.Count == 1)
        {
            return ControllerAllowsIntegerSize(
                node.Children[0],
                minimum,
                visited
            );
        }
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null
                && rule.Value != null
                && !ContainsRule(visited, rule))
            {
                visited.Add(rule);
                bool allowed = ControllerAllowsIntegerSize(
                    rule.Value,
                    minimum,
                    visited
                );
                visited.RemoveAt(visited.Count - 1);
                return allowed;
            }
            return false;
        }
        if (node.Kind == CddlNodeKind.Representation
            && node.MajorType == 0
            && !node.HasArgumentValue)
        {
            return true;
        }
        if (node.Kind == CddlNodeKind.GroupEnumeration)
        {
            List<CddlNode> candidates = new List<CddlNode>();
            if (node.Name != null)
            {
                CddlRule? groupRule = State.FindReferencedRule(node);
                if (groupRule != null && groupRule.Value != null)
                {
                    CollectEnumerationCandidates(
                        groupRule.Value,
                        candidates,
                        visited
                    );
                }
            }
            else if (node.Children.Count > 0)
            {
                CollectEnumerationCandidates(
                    node.Children[0],
                    candidates,
                    visited
                );
            }
            int index = 0;
            while (index < candidates.Count)
            {
                if (ControllerAllowsIntegerSize(
                    candidates[index],
                    minimum,
                    visited))
                {
                    return true;
                }
                index = index + 1;
            }
        }
        return false;
    }

    internal string? EvaluateTextNode(
        CddlNode node,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.Control)
        {
            CborValue? computed = new CddlComputedLiteralMatcher(
                this,
                State
            ).EvaluateSingleValue(node);
            if (computed != null
                && computed.Kind == CborValueKind.TextString)
            {
                return computed.TextStringValue;
            }
            return null;
        }
        if (node.Kind == CddlNodeKind.Literal
            && node.LiteralKind == CddlLiteralKind.TextString)
        {
            return node.TextValue;
        }
        if (node.Kind == CddlNodeKind.ParenthesizedType
            && node.Children.Count == 1)
        {
            return EvaluateTextNode(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.TypeChoice
            && node.Children.Count == 1)
        {
            return EvaluateTextNode(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null
                && rule.Value != null
                && !ContainsRule(visited, rule))
            {
                visited.Add(rule);
                return EvaluateTextNode(rule.Value, visited);
            }
        }
        return null;
    }

    internal CddlMatchResult MatchRange(CddlNode node, CborValue value)
    {
        if (node.Children.Count != 2)
        {
            return RejectInvalidSchema(
                node,
                "A CDDL range must have two numeric endpoints."
            );
        }
        CddlNumericValue lower = EvaluateNumericNode(
            node.Children[0],
            new List<CddlRule>()
        );
        CddlNumericValue upper = EvaluateNumericNode(
            node.Children[1],
            new List<CddlRule>()
        );
        bool matches = false;
        if (State.JsonNumberMode)
        {
            CddlNumericValue actual = CreateNumericValue(value);
            int lowerComparison = CompareNumericValues(actual, lower);
            int upperComparison = CompareNumericValues(actual, upper);
            matches = lowerComparison != int.MinValue
                && upperComparison != int.MinValue
                && lowerComparison >= 0
                && (node.IsRangeUpperExclusive
                    ? upperComparison < 0
                    : upperComparison <= 0);
        }
        else if (lower.Kind == CddlNumericValue.Integer
            && upper.Kind == CddlNumericValue.Integer
            && (value.Kind == CborValueKind.UnsignedInteger
                || value.Kind == CborValueKind.NegativeInteger))
        {
            bool belowUpper = node.IsRangeUpperExclusive
                ? value.IntegerValue < upper.IntegerValue
                : value.IntegerValue <= upper.IntegerValue;
            matches = value.IntegerValue >= lower.IntegerValue && belowUpper;
        }
        else if (lower.Kind == CddlNumericValue.Float
            && upper.Kind == CddlNumericValue.Float
            && value.Kind == CborValueKind.Float
            && value.FloatValue != null
            && !double.IsNaN(value.FloatValue.Value))
        {
            double actual = value.FloatValue.Value;
            bool belowUpper = node.IsRangeUpperExclusive
                ? actual < upper.FloatValue
                : actual <= upper.FloatValue;
            matches = actual >= lower.FloatValue && belowUpper;
        }
        else if (lower.Kind == CddlNumericValue.None
            || upper.Kind == CddlNumericValue.None)
        {
            return RejectInvalidSchema(
                node,
                "The CDDL range endpoints are not numeric singletons."
            );
        }
        if (matches)
        {
            return CreateMatch();
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.RangeMismatch,
            "The CBOR number lies outside the required CDDL range.",
            node,
            DescribeRange(lower, upper, node.IsRangeUpperExclusive),
            DescribeValue(value)
        );
        return CreateRejection(issue);
    }

    internal CddlMatchResult MatchTag(CddlNode node, CborValue value)
    {
        if (value.Kind != CborValueKind.Tag || value.TaggedValue == null)
        {
            CddlValidationIssue typeIssue = State.CreateIssue(
                CddlValidationCode.TypeMismatch,
                "The CBOR value is not a semantic tag.",
                node,
                "tag",
                DescribeValue(value)
            );
            return CreateRejection(typeIssue);
        }
        if (node.Children.Count == 0)
        {
            return RejectInvalidSchema(
                node,
                "The CDDL tag has no content type."
            );
        }
        if (node.HasArgumentValue)
        {
            BigInteger actualTag = new BigInteger(value.TagNumber);
            if (actualTag != node.ArgumentValue)
            {
                CddlValidationIssue tagIssue = State.CreateIssue(
                    CddlValidationCode.LiteralMismatch,
                    "The CBOR semantic tag number does not match.",
                    node,
                    node.ArgumentValue.ToString(),
                    value.TagNumber.ToString()
                );
                return CreateRejection(tagIssue);
            }
        }
        else if (node.Children.Count == 2)
        {
            CborValue tagNumber = new CborValue();
            tagNumber.Kind = CborValueKind.UnsignedInteger;
            tagNumber.IntegerValue = new BigInteger(value.TagNumber);
            CddlMatchResult tagMatch = MatchNode(node.Children[0], tagNumber);
            if (!tagMatch.IsMatch)
            {
                return tagMatch;
            }
        }
        if (!State.EnterDataNesting(node))
        {
            return CreateNoIssueRejection();
        }
        State.InstancePath.PushTaggedValue();
        CddlMatchResult content = MatchNode(
            node.Children[node.Children.Count - 1],
            value.TaggedValue
        );
        State.InstancePath.PopSegment();
        State.LeaveDataNesting();
        return content;
    }

    internal CddlMatchResult MatchRepresentation(
        CddlNode node,
        CborValue value)
    {
        if (State.JsonNumberMode
            && node.MajorType == 7
            && node.HasArgumentValue
            && node.ArgumentValue >= new BigInteger(25)
            && node.ArgumentValue <= new BigInteger(27)
            && (value.Kind == CborValueKind.UnsignedInteger
                || value.Kind == CborValueKind.NegativeInteger
                || value.Kind == CborValueKind.Float))
        {
            int width = (int)node.ArgumentValue;
            if (JsonNumberFitsFloatRepresentation(value, width))
            {
                return CreateMatch();
            }
            CddlValidationIssue jsonFloatIssue = State.CreateIssue(
                CddlValidationCode.RepresentationMismatch,
                "The JSON number is not exactly representable at the required floating-point width.",
                node,
                "a number representable at the requested binary floating-point width",
                DescribeValue(value)
            );
            return CreateRejection(jsonFloatIssue);
        }
        int actualMajor = FindMajorType(value);
        if (node.MajorType >= 0 && actualMajor != node.MajorType)
        {
            CddlValidationIssue typeIssue = State.CreateIssue(
                CddlValidationCode.TypeMismatch,
                "The CBOR major type does not match the CDDL representation.",
                node,
                "major type " + node.MajorType,
                "major type " + actualMajor
            );
            return CreateRejection(typeIssue);
        }
        if (actualMajor < 0)
        {
            CddlValidationIssue invalidValue = State.CreateIssue(
                CddlValidationCode.TypeMismatch,
                "The value has no valid CBOR data-model kind.",
                node,
                "a CBOR value",
                DescribeValue(value)
            );
            return CreateRejection(invalidValue);
        }
        if (node.Children.Count == 1)
        {
            int simpleArgument = FindSimpleArgument(value);
            if (simpleArgument < 0)
            {
                CddlValidationIssue simpleIssue = State.CreateIssue(
                    CddlValidationCode.RepresentationMismatch,
                    "The CBOR simple or float argument is unavailable.",
                    node,
                    "a simple or float argument",
                    DescribeValue(value)
                );
                return CreateRejection(simpleIssue);
            }
            CborValue argument = new CborValue();
            argument.Kind = CborValueKind.UnsignedInteger;
            argument.IntegerValue = new BigInteger(simpleArgument);
            return MatchNode(node.Children[0], argument);
        }
        if (node.HasArgumentValue)
        {
            int expected = (int)node.ArgumentValue;
            if (node.MajorType == 7 && expected == 24)
            {
                if (value.Kind == CborValueKind.SimpleValue
                    && value.SimpleValue >= 32)
                {
                    return CreateMatch();
                }
                CddlValidationIssue simpleWidthIssue = State.CreateIssue(
                    CddlValidationCode.RepresentationMismatch,
                    "The CBOR value is not a two-byte simple value.",
                    node,
                    "a simple value from 32 through 255",
                    DescribeValue(value)
                );
                return CreateRejection(simpleWidthIssue);
            }
            if (node.MajorType == 7 && expected > 31)
            {
                if (value.Kind == CborValueKind.SimpleValue
                    && value.SimpleValue == expected)
                {
                    return CreateMatch();
                }
                CddlValidationIssue simpleIssue = State.CreateIssue(
                    CddlValidationCode.RepresentationMismatch,
                    "The CBOR simple value number does not match.",
                    node,
                    "simple value " + expected,
                    DescribeValue(value)
                );
                return CreateRejection(simpleIssue);
            }
            int actual = FindAdditionalInformation(value);
            if (actual != expected)
            {
                CddlValidationIssue representationIssue = State.CreateIssue(
                    CddlValidationCode.RepresentationMismatch,
                    "The CBOR additional-information representation does not match.",
                    node,
                    "additional information " + expected,
                    actual < 0
                        ? "unavailable"
                        : "additional information " + actual
                );
                return CreateRejection(representationIssue);
            }
        }
        return CreateMatch();
    }

    internal bool JsonNumberFitsFloatRepresentation(
        CborValue value,
        int additionalInformation)
    {
        CddlJsonNumber? jsonNumber = State.FindJsonNumber(value);
        if (jsonNumber != null)
        {
            return jsonNumber.IsExactlyRepresentable(additionalInformation);
        }
        double number;
        if (value.Kind == CborValueKind.Float && value.FloatValue != null)
        {
            number = value.FloatValue.Value;
        }
        else
        {
            number = (double)value.IntegerValue;
            if (double.IsInfinity(number)
                || new BigInteger(number) != value.IntegerValue)
            {
                return false;
            }
        }
        if (additionalInformation == 25)
        {
            return CborHalfPrecision.IsExactlyRepresentableAsHalf(number);
        }
        if (additionalInformation == 26)
        {
            return CborHalfPrecision.IsExactlyRepresentableAsSingle(number);
        }
        return !double.IsNaN(number) && !double.IsInfinity(number);
    }

    internal CddlMatchResult MatchUnwrap(CddlNode node, CborValue value)
    {
        string? name = node.Name;
        if (name == null)
        {
            return RejectReference(node, "The unwrap has no target rule.");
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null || rule.Value == null)
        {
            return RejectReference(node, "The unwrap target is unavailable.");
        }
        if (State.RuleStack.ContainsRule(rule))
        {
            return RejectReference(
                node,
                "The unwrap target forms an active recursive cycle."
            );
        }
        CddlNode? content = FindUnwrappedContent(
            rule.Value,
            new List<CddlRule>()
        );
        if (content == null)
        {
            return RejectInvalidSchema(
                node,
                "The unwrap target has no exposed content."
            );
        }
        if (!State.EnterRule(rule))
        {
            return CreateNoIssueRejection();
        }
        CddlMatchResult result;
        if (content.Kind == CddlNodeKind.Group
            || content.Kind == CddlNodeKind.GroupChoice
            || content.Kind == CddlNodeKind.GroupEntry)
        {
            result = RejectUnsupported(
                node,
                "Unwrapped container groups require group matching."
            );
        }
        else
        {
            result = MatchNode(content, value);
        }
        State.LeaveRule();
        return result;
    }

    internal CddlMatchResult MatchGroupEnumeration(
        CddlNode node,
        CborValue value)
    {
        List<CddlNode> candidates = new List<CddlNode>();
        CddlRule? groupRule = null;
        if (node.Name != null)
        {
            groupRule = State.FindReferencedRule(node);
            if (groupRule == null
                || groupRule.Kind != CddlRuleKind.Group
                || groupRule.Value == null)
            {
                return RejectReference(
                    node,
                    "The group-enumeration target is unavailable."
                );
            }
            CollectEnumerationCandidates(
                groupRule.Value,
                candidates,
                new List<CddlRule>()
            );
        }
        else if (node.Children.Count > 0)
        {
            CollectEnumerationCandidates(
                node.Children[0],
                candidates,
                new List<CddlRule>()
            );
        }
        if (candidates.Count == 0)
        {
            CddlValidationIssue empty = State.CreateIssue(
                CddlValidationCode.ChoiceMismatch,
                "The group enumeration contains no value alternatives.",
                node,
                "an enumerated group value",
                DescribeValue(value)
            );
            return CreateRejection(empty);
        }
        bool entered = false;
        if (groupRule != null)
        {
            entered = State.EnterRule(groupRule);
            if (!entered)
            {
                return CreateNoIssueRejection();
            }
        }
        CddlMatchResult result = MatchEnumerationCandidates(
            node,
            candidates,
            value
        );
        if (entered)
        {
            State.LeaveRule();
        }
        return result;
    }

    internal CddlMatchResult MatchEnumerationCandidates(
        CddlNode enumeration,
        List<CddlNode> candidates,
        CborValue value)
    {
        List<CddlValidationIssue> details =
            new List<CddlValidationIssue>();
        int index = 0;
        while (index < candidates.Count)
        {
            if (!State.TakeAlternative(enumeration))
            {
                return CreateNoIssueRejection();
            }
            State.SchemaPath.PushChoiceIndex(index);
            CddlMatchResult candidate = MatchNode(candidates[index], value);
            State.SchemaPath.PopSegment();
            if (candidate.IsMatch)
            {
                return CreateMatch();
            }
            if (candidate.Issue != null)
            {
                AddBoundedDetail(details, candidate.Issue);
            }
            if (State.Diagnostics.HasErrors)
            {
                return CreateNoIssueRejection();
            }
            index = index + 1;
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ChoiceMismatch,
            "The CBOR value is not present in the CDDL group enumeration.",
            enumeration,
            "an enumerated group value",
            DescribeValue(value)
        );
        issue.Details = details.ToArray();
        return CreateRejection(issue);
    }

    internal CddlMatchResult MatchParenthesizedType(
        CddlNode node,
        CborValue value)
    {
        if (node.Children.Count != 1)
        {
            return RejectInvalidSchema(
                node,
                "A parenthesized type must contain one type."
            );
        }
        return MatchNode(node.Children[0], value);
    }

    internal CddlMatchResult MatchArray(CddlNode node, CborValue value)
    {
        if (value.Kind != CborValueKind.Array)
        {
            CddlValidationIssue typeIssue = State.CreateIssue(
                CddlValidationCode.TypeMismatch,
                "The CBOR value is not an array.",
                node,
                "array",
                DescribeValue(value)
            );
            return CreateRejection(typeIssue);
        }
        if (node.Children.Count != 1)
        {
            return RejectInvalidSchema(
                node,
                "A CDDL array must contain one group expression."
            );
        }
        List<CborValue> items = value.ArrayItems
            ?? new List<CborValue>();
        if (!State.EnterDataNesting(node))
        {
            return CreateNoIssueRejection();
        }
        ContainerValues.Add(value);
        CddlGroupMatchResult group = MatchGroupAt(
            node.Children[0],
            items,
            0
        );
        bool complete = ContainsPosition(group.Positions, items.Count);
        ContainerValues.RemoveAt(ContainerValues.Count - 1);
        State.LeaveDataNesting();
        if (complete)
        {
            return CreateMatch();
        }
        if (State.Diagnostics.HasErrors)
        {
            return CreateNoIssueRejection();
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.OccurrenceMismatch,
            "The ordered CDDL group does not consume the complete CBOR array.",
            node,
            "an array matching the complete ordered group",
            items.Count + " array item(s)"
        );
        issue.Details = group.Issues.ToArray();
        return CreateRejection(issue);
    }

    internal CddlGroupMatchResult MatchGroupAt(
        CddlNode node,
        List<CborValue> items,
        int start)
    {
        CddlGroupMatchResult result = new CddlGroupMatchResult();
        if (!State.TakeStep(node))
        {
            return result;
        }
        int nodeIndex = FindNodeIndex(node);
        if (nodeIndex >= 0)
        {
            State.SchemaPath.PushNodeIndex(nodeIndex);
        }
        result = MatchGroupNodeKind(node, items, start);
        if (nodeIndex >= 0)
        {
            State.SchemaPath.PopSegment();
        }
        return result;
    }

    internal CddlGroupMatchResult MatchGroupNodeKind(
        CddlNode node,
        List<CborValue> items,
        int start)
    {
        if (node.Kind == CddlNodeKind.Group)
        {
            return MatchGroupSequence(node, items, start);
        }
        if (node.Kind == CddlNodeKind.GroupChoice)
        {
            return MatchOrderedGroupChoice(node, items, start);
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            return MatchGroupEntryOccurrences(node, items, start);
        }
        if (node.Kind == CddlNodeKind.ParenthesizedGroup
            && node.Children.Count == 1)
        {
            return MatchGroupAt(node.Children[0], items, start);
        }
        if (IsGroupProducingNode(node))
        {
            return MatchIncludedGroup(node, items, start);
        }
        CddlGroupMatchResult invalid = new CddlGroupMatchResult();
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.InvalidSchema,
            "The schema node cannot be evaluated as an ordered group.",
            node,
            "an ordered group expression",
            node.Kind.ToString()
        );
        AddGroupIssue(invalid, issue);
        return invalid;
    }

    internal CddlGroupMatchResult MatchGroupSequence(
        CddlNode node,
        List<CborValue> items,
        int start)
    {
        CddlGroupMatchResult result = new CddlGroupMatchResult();
        result.Positions.Add(start);
        int childIndex = 0;
        while (childIndex < node.Children.Count
            && result.Positions.Count > 0)
        {
            CddlGroupMatchResult next = new CddlGroupMatchResult();
            int positionIndex = 0;
            while (positionIndex < result.Positions.Count)
            {
                if (result.Positions.Count > 1
                    && !State.TakeAlternative(node))
                {
                    return new CddlGroupMatchResult();
                }
                CddlGroupMatchResult child = MatchGroupAt(
                    node.Children[childIndex],
                    items,
                    result.Positions[positionIndex]
                );
                AddPositions(next.Positions, child.Positions);
                AddGroupIssues(next, child.Issues);
                if (State.Diagnostics.HasErrors)
                {
                    return new CddlGroupMatchResult();
                }
                positionIndex = positionIndex + 1;
            }
            result = next;
            childIndex = childIndex + 1;
        }
        return result;
    }

    internal CddlGroupMatchResult MatchOrderedGroupChoice(
        CddlNode node,
        List<CborValue> items,
        int start)
    {
        CddlGroupMatchResult result = new CddlGroupMatchResult();
        int index = 0;
        while (index < node.Children.Count)
        {
            if (!State.TakeAlternative(node))
            {
                return new CddlGroupMatchResult();
            }
            State.SchemaPath.PushChoiceIndex(index);
            CddlGroupMatchResult branch = MatchGroupAt(
                node.Children[index],
                items,
                start
            );
            State.SchemaPath.PopSegment();
            AddPositions(result.Positions, branch.Positions);
            AddGroupIssues(result, branch.Issues);
            if (State.Diagnostics.HasErrors)
            {
                return new CddlGroupMatchResult();
            }
            index = index + 1;
        }
        if (result.Positions.Count == 0 && result.Issues.Count == 0)
        {
            CddlValidationIssue issue = State.CreateIssue(
                CddlValidationCode.ChoiceMismatch,
                "No ordered group-choice alternative matches the array.",
                node,
                "one matching group-choice alternative",
                "array position " + start
            );
            AddGroupIssue(result, issue);
        }
        return result;
    }

    internal CddlGroupMatchResult MatchGroupEntryOccurrences(
        CddlNode entry,
        List<CborValue> items,
        int start)
    {
        CddlGroupMatchResult result = new CddlGroupMatchResult();
        if (entry.Children.Count == 0)
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.InvalidSchema,
                "The ordered group entry has no value expression.",
                entry,
                "a group-entry value",
                null
            );
            AddGroupIssue(result, invalid);
            return result;
        }
        int minimum = entry.MinimumOccurrence;
        int maximum = FindOccurrenceMaximum(entry, items.Count - start);
        if (minimum == 0)
        {
            result.Positions.Add(start);
        }
        List<int> current = new List<int>();
        current.Add(start);
        int count = 0;
        while (count < maximum && current.Count > 0)
        {
            List<int> next = new List<int>();
            int positionIndex = 0;
            while (positionIndex < current.Count)
            {
                if ((entry.MinimumOccurrence != entry.MaximumOccurrence
                        || current.Count > 1)
                    && !State.TakeAlternative(entry))
                {
                    return new CddlGroupMatchResult();
                }
                int position = current[positionIndex];
                CddlGroupMatchResult occurrence = MatchGroupEntryOnce(
                    entry,
                    items,
                    position
                );
                AddGroupIssues(result, occurrence.Issues);
                int endIndex = 0;
                while (endIndex < occurrence.Positions.Count)
                {
                    int end = occurrence.Positions[endIndex];
                    if (count + 1 >= minimum)
                    {
                        AddPosition(result.Positions, end);
                    }
                    if ((end != position && end < items.Count)
                        || count + 1 < minimum)
                    {
                        AddPosition(next, end);
                    }
                    endIndex = endIndex + 1;
                }
                if (State.Diagnostics.HasErrors)
                {
                    return new CddlGroupMatchResult();
                }
                positionIndex = positionIndex + 1;
            }
            current = next;
            count = count + 1;
        }
        if (result.Positions.Count == 0)
        {
            PushArrayPath(start);
            CddlValidationIssue occurrenceIssue = State.CreateIssue(
                CddlValidationCode.OccurrenceMismatch,
                "The ordered group entry does not satisfy its occurrence bounds.",
                entry,
                minimum + " through "
                    + (entry.MaximumOccurrence < 0
                        ? "unbounded"
                        : entry.MaximumOccurrence.ToString())
                    + " occurrence(s)",
                count + " matched occurrence(s)"
            );
            PopArrayPath(start);
            AddGroupIssue(result, occurrenceIssue);
        }
        return result;
    }

    internal CddlGroupMatchResult MatchGroupEntryOnce(
        CddlNode entry,
        List<CborValue> items,
        int start)
    {
        CddlNode value = entry.Children[entry.Children.Count - 1];
        if (IsGroupProducingNode(value))
        {
            return MatchIncludedGroup(value, items, start);
        }
        CddlGroupMatchResult result = new CddlGroupMatchResult();
        if (start >= items.Count)
        {
            PushArrayPath(start);
            CddlValidationIssue missing = State.CreateIssue(
                CddlValidationCode.OccurrenceMismatch,
                "A required ordered array item is missing.",
                entry,
                "an array item matching the group entry",
                "end of array"
            );
            PopArrayPath(start);
            AddGroupIssue(result, missing);
            return result;
        }
        State.InstancePath.PushArrayIndex(start);
        CddlMatchResult match = MatchNode(value, items[start]);
        State.InstancePath.PopSegment();
        if (match.IsMatch)
        {
            result.Positions.Add(start + 1);
        }
        else if (match.Issue != null)
        {
            AddGroupIssue(result, match.Issue);
        }
        return result;
    }

    internal CddlGroupMatchResult MatchIncludedGroup(
        CddlNode node,
        List<CborValue> items,
        int start)
    {
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.GroupEntry
            || node.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            return MatchGroupAt(node, items, start);
        }
        if (node.Kind == CddlNodeKind.Unwrap)
        {
            return MatchUnwrappedGroupAt(node, items, start);
        }
        CddlGroupMatchResult result = new CddlGroupMatchResult();
        string? name = node.Name;
        if (name == null)
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.ReferenceFailure,
                "The included group has no target name.",
                node,
                "a group rule",
                null
            );
            AddGroupIssue(result, invalid);
            return result;
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null)
        {
            if (name.Length > 1 && name[0] == '$' && name[1] == '$')
            {
                result.Positions.Add(start);
                return result;
            }
            CddlValidationIssue missing = State.CreateIssue(
                CddlValidationCode.ReferenceFailure,
                "The included group rule is unavailable.",
                node,
                "a group rule",
                name
            );
            AddGroupIssue(result, missing);
            return result;
        }
        if (rule.Kind != CddlRuleKind.Group || rule.Value == null)
        {
            CddlValidationIssue wrongKind = State.CreateIssue(
                CddlValidationCode.ReferenceFailure,
                "The included rule is not a complete group rule.",
                node,
                "a group rule",
                name
            );
            AddGroupIssue(result, wrongKind);
            return result;
        }
        return MatchArrayGroupRule(rule, rule.Value, node, items, start);
    }

    internal CddlGroupMatchResult MatchArrayGroupRule(
        CddlRule rule,
        CddlNode content,
        CddlNode node,
        List<CborValue> items,
        int start)
    {
        CddlGroupMatchResult result = new CddlGroupMatchResult();
        CborValue container = CurrentContainerValue();
        CddlValidationMemoEntry? memo = State.BeginMemo(
            rule,
            container,
            start,
            ArrayGroupMemoContext,
            node
        );
        if (memo == null)
        {
            return result;
        }
        if (memo.Status == CddlValidationMemoEntry.Matched)
        {
            AddStoredPositions(result.Positions, memo.Positions);
            AddStoredIssues(result.Issues, memo.Issues);
            return result;
        }
        if (memo.Status == CddlValidationMemoEntry.Rejected)
        {
            AddStoredIssues(result.Issues, memo.Issues);
            return result;
        }
        if (memo.IsActive)
        {
            CddlValidationIssue recursion = State.CreateIssue(
                CddlValidationCode.RecursionLimit,
                "Recursive group evaluation revisited the same array position.",
                node,
                "a productive recursive group descent",
                rule.Name + " at array position " + start
            );
            AddGroupIssue(result, recursion);
            return result;
        }
        memo.IsActive = true;
        if (!State.EnterRule(rule))
        {
            memo.IsActive = false;
            memo.CompleteGroupMatch(result);
            return result;
        }
        result = MatchGroupAt(content, items, start);
        State.LeaveRule();
        memo.IsActive = false;
        memo.CompleteGroupMatch(result);
        return result;
    }

    internal CddlGroupMatchResult MatchUnwrappedGroupAt(
        CddlNode node,
        List<CborValue> items,
        int start)
    {
        CddlGroupMatchResult result = new CddlGroupMatchResult();
        if (node.Name == null)
        {
            return result;
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null || rule.Value == null)
        {
            CddlValidationIssue missing = State.CreateIssue(
                CddlValidationCode.ReferenceFailure,
                "The unwrapped group target is unavailable.",
                node,
                "an array or map type",
                node.Name
            );
            AddGroupIssue(result, missing);
            return result;
        }
        CddlNode? content = FindUnwrappedContent(
            rule.Value,
            new List<CddlRule>()
        );
        if (content == null || !IsDirectGroupNode(content))
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.InvalidSchema,
                "The unwrap target does not expose a container group.",
                node,
                "an exposed container group",
                node.Name
            );
            AddGroupIssue(result, invalid);
            return result;
        }
        return MatchArrayGroupRule(rule, content, node, items, start);
    }

    internal int FindOccurrenceMaximum(CddlNode entry, int remaining)
    {
        if (entry.MaximumOccurrence >= 0)
        {
            return entry.MaximumOccurrence;
        }
        if (remaining < 0)
        {
            remaining = 0;
        }
        if (entry.MinimumOccurrence > remaining)
        {
            return entry.MinimumOccurrence;
        }
        return remaining;
    }

    internal CborValue CurrentContainerValue()
    {
        if (ContainerValues.Count == 0)
        {
            return State.RootValue;
        }
        return ContainerValues[ContainerValues.Count - 1];
    }

    internal void AddStoredPositions(List<int> positions, int[] stored)
    {
        int index = 0;
        while (index < stored.Length)
        {
            AddPosition(positions, stored[index]);
            index = index + 1;
        }
    }

    internal void AddStoredIssues(
        List<CddlValidationIssue> issues,
        CddlValidationIssue[] stored)
    {
        int index = 0;
        while (index < stored.Length)
        {
            AddBoundedDetail(issues, stored[index]);
            index = index + 1;
        }
    }

    internal void AddStoredMapStates(
        List<CddlMapMatchState> states,
        List<CddlMapMatchState> stored)
    {
        int index = 0;
        while (index < stored.Count)
        {
            AddMapState(states, CloneMapState(stored[index]));
            index = index + 1;
        }
    }

    internal bool IsGroupProducingNode(CddlNode node)
    {
        if (IsDirectGroupNode(node))
        {
            return true;
        }
        if (node.Kind == CddlNodeKind.Unwrap)
        {
            CddlNode? content = FindNamedUnwrapContent(node);
            return content != null && IsDirectGroupNode(content);
        }
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null)
            {
                return rule.Kind == CddlRuleKind.Group;
            }
            return node.Name.Length > 1
                && node.Name[0] == '$'
                && node.Name[1] == '$';
        }
        return false;
    }

    internal bool IsDirectGroupNode(CddlNode node)
    {
        return node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.GroupEntry
            || node.Kind == CddlNodeKind.ParenthesizedGroup;
    }

    internal CddlNode? FindNamedUnwrapContent(CddlNode node)
    {
        if (node.Name == null)
        {
            return null;
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null || rule.Value == null)
        {
            return null;
        }
        return FindUnwrappedContent(rule.Value, new List<CddlRule>());
    }

    internal void AddPositions(List<int> destination, List<int> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            AddPosition(destination, source[index]);
            index = index + 1;
        }
    }

    internal void AddPosition(List<int> positions, int position)
    {
        int index = 0;
        while (index < positions.Count)
        {
            if (positions[index] == position)
            {
                return;
            }
            index = index + 1;
        }
        positions.Add(position);
    }

    internal bool ContainsPosition(List<int> positions, int position)
    {
        int index = 0;
        while (index < positions.Count)
        {
            if (positions[index] == position)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void AddGroupIssues(
        CddlGroupMatchResult destination,
        List<CddlValidationIssue> issues)
    {
        int index = 0;
        while (index < issues.Count)
        {
            AddGroupIssue(destination, issues[index]);
            index = index + 1;
        }
    }

    internal void AddGroupIssue(
        CddlGroupMatchResult result,
        CddlValidationIssue issue)
    {
        AddBoundedDetail(result.Issues, issue);
    }

    internal void AddBoundedDetail(
        List<CddlValidationIssue> details,
        CddlValidationIssue issue)
    {
        int maximum = State.Options.MaxIssues - 1;
        if (!State.Options.CollectIssues)
        {
            maximum = 0;
        }
        if (maximum > 0 && details.Count < maximum)
        {
            details.Add(issue);
        }
    }

    internal void PushArrayPath(int index)
    {
        State.InstancePath.PushArrayIndex(index);
    }

    internal void PopArrayPath(int index)
    {
        State.InstancePath.PopSegment();
    }

    internal CddlMatchResult MatchMap(CddlNode node, CborValue value)
    {
        if (value.Kind != CborValueKind.Map)
        {
            CddlValidationIssue typeIssue = State.CreateIssue(
                CddlValidationCode.TypeMismatch,
                "The CBOR value is not a map.",
                node,
                "map",
                DescribeValue(value)
            );
            return CreateRejection(typeIssue);
        }
        if (node.Children.Count != 1)
        {
            return RejectInvalidSchema(
                node,
                "A CDDL map must contain one group expression."
            );
        }
        List<CborMapEntry> entries = value.MapEntries
            ?? new List<CborMapEntry>();
        if (!State.EnterDataNesting(node))
        {
            return CreateNoIssueRejection();
        }
        ContainerValues.Add(value);
        CddlMapMatchState initial = new CddlMapMatchState(entries.Count);
        initial.ControlResults = State.CreateControlResultSnapshot();
        CddlMapMatchResult group = MatchMapGroupAt(
            node.Children[0],
            entries,
            initial
        );
        CddlMapMatchState? complete = FindCompleteMapState(group.States);
        ContainerValues.RemoveAt(ContainerValues.Count - 1);
        State.LeaveDataNesting();
        if (complete != null)
        {
            State.RestoreControlResultSnapshot(complete.ControlResults);
            return CreateMatch();
        }
        State.RestoreControlResultSnapshot(initial.ControlResults);
        if (State.Diagnostics.HasErrors)
        {
            return CreateNoIssueRejection();
        }
        CddlMapMatchState? best = FindBestMapState(group.States);
        if (best != null && CountUsedMapEntries(best) < entries.Count)
        {
            CddlValidationIssue unexpected = CreateUnexpectedMapIssue(
                node,
                entries,
                best
            );
            unexpected.Details = group.Issues.ToArray();
            return CreateRejection(unexpected);
        }
        CddlValidationCode code = ContainsMapIssueCode(
            group.Issues,
            CddlValidationCode.MissingMember)
            ? CddlValidationCode.MissingMember
            : CddlValidationCode.OccurrenceMismatch;
        CddlValidationIssue issue = State.CreateIssue(
            code,
            "The unordered CDDL group does not match the complete CBOR map.",
            node,
            "a map matching the complete unordered group",
            entries.Count + " map entr" + (entries.Count == 1 ? "y" : "ies")
        );
        issue.Details = group.Issues.ToArray();
        return CreateRejection(issue);
    }

    internal CddlMapMatchResult MatchMapGroupAt(
        CddlNode node,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        CddlMapMatchResult result = new CddlMapMatchResult();
        if (!State.TakeStep(node))
        {
            return result;
        }
        int nodeIndex = FindNodeIndex(node);
        if (nodeIndex >= 0)
        {
            State.SchemaPath.PushNodeIndex(nodeIndex);
        }
        result = MatchMapGroupNodeKind(node, entries, start);
        if (nodeIndex >= 0)
        {
            State.SchemaPath.PopSegment();
        }
        return result;
    }

    internal CddlMapMatchResult MatchMapGroupNodeKind(
        CddlNode node,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        if (node.Kind == CddlNodeKind.Group)
        {
            return MatchMapGroupSequence(node, entries, start);
        }
        if (node.Kind == CddlNodeKind.GroupChoice)
        {
            return MatchUnorderedGroupChoice(node, entries, start);
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            return MatchMapEntryOccurrences(node, entries, start);
        }
        if (node.Kind == CddlNodeKind.ParenthesizedGroup
            && node.Children.Count == 1)
        {
            return MatchMapGroupAt(node.Children[0], entries, start);
        }
        if (IsGroupProducingNode(node))
        {
            return MatchIncludedMapGroup(node, entries, start);
        }
        CddlMapMatchResult invalid = new CddlMapMatchResult();
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.InvalidSchema,
            "The schema node cannot be evaluated as an unordered group.",
            node,
            "an unordered group expression",
            node.Kind.ToString()
        );
        AddMapIssue(invalid, issue);
        return invalid;
    }

    internal CddlMapMatchResult MatchMapGroupSequence(
        CddlNode node,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        CddlMapMatchResult result = new CddlMapMatchResult();
        result.States.Add(start);
        int childIndex = 0;
        while (childIndex < node.Children.Count
            && result.States.Count > 0)
        {
            CddlMapMatchResult next = new CddlMapMatchResult();
            int stateIndex = 0;
            while (stateIndex < result.States.Count)
            {
                State.RestoreControlResultSnapshot(
                    result.States[stateIndex].ControlResults
                );
                if (result.States.Count > 1
                    && !State.TakeAlternative(node))
                {
                    return new CddlMapMatchResult();
                }
                CddlMapMatchResult child = MatchMapGroupAt(
                    node.Children[childIndex],
                    entries,
                    result.States[stateIndex]
                );
                AddMapStates(next.States, child.States);
                AddMapIssues(next, child.Issues);
                next.CutMatched = next.CutMatched || child.CutMatched;
                if (State.Diagnostics.HasErrors)
                {
                    return new CddlMapMatchResult();
                }
                stateIndex = stateIndex + 1;
            }
            result = next;
            childIndex = childIndex + 1;
        }
        return result;
    }

    internal CddlMapMatchResult MatchUnorderedGroupChoice(
        CddlNode node,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        CddlMapMatchResult result = new CddlMapMatchResult();
        int index = 0;
        while (index < node.Children.Count)
        {
            State.RestoreControlResultSnapshot(start.ControlResults);
            if (!State.TakeAlternative(node))
            {
                return new CddlMapMatchResult();
            }
            State.SchemaPath.PushChoiceIndex(index);
            CddlMapMatchResult branch = MatchMapGroupAt(
                node.Children[index],
                entries,
                start
            );
            State.SchemaPath.PopSegment();
            if (branch.CutMatched)
            {
                return branch;
            }
            AddMapStates(result.States, branch.States);
            AddMapIssues(result, branch.Issues);
            if (State.Diagnostics.HasErrors)
            {
                return new CddlMapMatchResult();
            }
            index = index + 1;
        }
        if (result.States.Count == 0 && result.Issues.Count == 0)
        {
            CddlValidationIssue issue = State.CreateIssue(
                CddlValidationCode.ChoiceMismatch,
                "No unordered group-choice alternative matches the map.",
                node,
                "one matching group-choice alternative",
                "map"
            );
            AddMapIssue(result, issue);
        }
        return result;
    }

    internal CddlMapMatchResult MatchMapEntryOccurrences(
        CddlNode entry,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        CddlMapMatchResult result = new CddlMapMatchResult();
        if (entry.Children.Count == 0)
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.InvalidSchema,
                "The unordered group entry has no value expression.",
                entry,
                "a map-entry value",
                null
            );
            AddMapIssue(result, invalid);
            return result;
        }
        int minimum = entry.MinimumOccurrence;
        int maximum = FindMapOccurrenceMaximum(entry, entries, start);
        List<CddlMapMatchState> current =
            new List<CddlMapMatchState>();
        current.Add(start);
        int count = 0;
        while (count < maximum && current.Count > 0)
        {
            List<CddlMapMatchState> next =
                new List<CddlMapMatchState>();
            int stateIndex = 0;
            while (stateIndex < current.Count)
            {
                State.RestoreControlResultSnapshot(
                    current[stateIndex].ControlResults
                );
                if ((entry.MinimumOccurrence != entry.MaximumOccurrence
                        || current.Count > 1)
                    && !State.TakeAlternative(entry))
                {
                    return new CddlMapMatchResult();
                }
                CddlMapMatchResult occurrence = MatchMapEntryOnce(
                    entry,
                    entries,
                    current[stateIndex]
                );
                AddMapIssues(result, occurrence.Issues);
                result.CutMatched = result.CutMatched
                    || occurrence.CutMatched;
                int endIndex = 0;
                while (endIndex < occurrence.States.Count)
                {
                    CddlMapMatchState end = occurrence.States[endIndex];
                    if (count + 1 >= minimum)
                    {
                        AddMapState(result.States, end);
                    }
                    if ((!MapStatesEqual(end, current[stateIndex])
                            && CountUsedMapEntries(end) < entries.Count)
                        || count + 1 < minimum)
                    {
                        AddMapState(next, end);
                    }
                    endIndex = endIndex + 1;
                }
                if (State.Diagnostics.HasErrors)
                {
                    return new CddlMapMatchResult();
                }
                stateIndex = stateIndex + 1;
            }
            current = next;
            count = count + 1;
        }
        if (minimum == 0)
        {
            AddMapState(result.States, start);
        }
        if (result.States.Count == 0)
        {
            PushMissingMapPath(entry);
            CddlValidationIssue missing = State.CreateIssue(
                CddlValidationCode.MissingMember,
                "A required CDDL map member is missing.",
                entry,
                DescribeMapEntry(entry) + " occurring " + minimum
                    + " through "
                    + (entry.MaximumOccurrence < 0
                        ? "unbounded"
                        : entry.MaximumOccurrence.ToString())
                    + " time(s)",
                count + " matched occurrence(s)"
            );
            PopMissingMapPath(entry);
            AddMapIssue(result, missing);
        }
        return result;
    }

    internal CddlMapMatchResult MatchMapEntryOnce(
        CddlNode entry,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        CddlNode valueNode = entry.Children[entry.Children.Count - 1];
        bool keyed = entry.Name != null || entry.Children.Count == 2;
        if (!keyed && IsGroupProducingNode(valueNode))
        {
            return MatchIncludedMapGroup(valueNode, entries, start);
        }
        CddlMapMatchResult result = new CddlMapMatchResult();
        if (!keyed)
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.InvalidSchema,
                "A CDDL map member requires a key or included group.",
                entry,
                "a keyed map entry",
                null
            );
            AddMapIssue(result, invalid);
            return result;
        }
        int index = 0;
        while (index < entries.Count)
        {
            if (!start.UsedEntries[index])
            {
                State.RestoreControlResultSnapshot(start.ControlResults);
                CborMapEntry mapEntry = entries[index];
                PushMapEntryPath(mapEntry, index);
                CddlMatchResult key = MatchMapEntryKey(entry, mapEntry);
                if (key.IsMatch)
                {
                    if (entry.IsCut)
                    {
                        result.CutMatched = true;
                    }
                    CddlMatchResult memberValue = MatchMapEntryValue(
                        valueNode,
                        mapEntry,
                        entry
                    );
                    if (memberValue.IsMatch)
                    {
                        CddlMapMatchState matched = CloneMapState(start);
                        matched.UsedEntries[index] = true;
                        matched.ControlResults =
                            State.CreateControlResultSnapshot();
                        if (result.States.Count > 0
                            && !State.TakeAlternative(entry))
                        {
                            PopMapEntryPath();
                            return new CddlMapMatchResult();
                        }
                        AddMapState(result.States, matched);
                    }
                    else if (memberValue.Issue != null)
                    {
                        AddMapIssue(result, memberValue.Issue);
                    }
                }
                else if (key.Issue != null)
                {
                    AddMapIssue(result, key.Issue);
                }
                PopMapEntryPath();
                if (State.Diagnostics.HasErrors)
                {
                    return new CddlMapMatchResult();
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal CddlMatchResult MatchMapEntryKey(
        CddlNode entry,
        CborMapEntry mapEntry)
    {
        if (mapEntry == null || mapEntry.Key == null)
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.TypeMismatch,
                "The CBOR map entry has no key.",
                entry,
                DescribeMapEntry(entry),
                "missing key"
            );
            return CreateRejection(invalid);
        }
        if (entry.Name != null)
        {
            bool matches = mapEntry.Key.Kind == CborValueKind.TextString
                && mapEntry.Key.TextStringValue == entry.Name;
            if (matches)
            {
                return CreateMatch();
            }
            CddlValidationIssue mismatch = State.CreateIssue(
                CddlValidationCode.LiteralMismatch,
                "The CBOR map key does not equal the required member name.",
                entry,
                "text \"" + entry.Name + "\"",
                DescribeValue(mapEntry.Key)
            );
            return CreateRejection(mismatch);
        }
        return MatchNode(entry.Children[0], mapEntry.Key);
    }

    internal CddlMatchResult MatchMapEntryValue(
        CddlNode valueNode,
        CborMapEntry mapEntry,
        CddlNode entry)
    {
        if (mapEntry == null || mapEntry.Value == null)
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.TypeMismatch,
                "The CBOR map entry has no value.",
                entry,
                "a CBOR value",
                "missing value"
            );
            return CreateRejection(invalid);
        }
        return MatchNode(valueNode, mapEntry.Value);
    }

    internal CddlMapMatchResult MatchIncludedMapGroup(
        CddlNode node,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        if (IsDirectGroupNode(node))
        {
            return MatchMapGroupAt(node, entries, start);
        }
        if (node.Kind == CddlNodeKind.Unwrap)
        {
            return MatchUnwrappedMapGroup(node, entries, start);
        }
        CddlMapMatchResult result = new CddlMapMatchResult();
        string? name = node.Name;
        if (name == null)
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.ReferenceFailure,
                "The included map group has no target name.",
                node,
                "a group rule",
                null
            );
            AddMapIssue(result, invalid);
            return result;
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null)
        {
            if (name.Length > 1 && name[0] == '$' && name[1] == '$')
            {
                result.States.Add(start);
                return result;
            }
            CddlValidationIssue missing = State.CreateIssue(
                CddlValidationCode.ReferenceFailure,
                "The included map group rule is unavailable.",
                node,
                "a group rule",
                name
            );
            AddMapIssue(result, missing);
            return result;
        }
        if (rule.Kind != CddlRuleKind.Group || rule.Value == null)
        {
            CddlValidationIssue wrongKind = State.CreateIssue(
                CddlValidationCode.ReferenceFailure,
                "The included rule is not a complete group rule.",
                node,
                "a group rule",
                name
            );
            AddMapIssue(result, wrongKind);
            return result;
        }
        return MatchMapGroupRule(
            rule,
            rule.Value,
            node,
            entries,
            start
        );
    }

    internal CddlMapMatchResult MatchMapGroupRule(
        CddlRule rule,
        CddlNode content,
        CddlNode node,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        CddlMapMatchResult result = new CddlMapMatchResult();
        CborValue container = CurrentContainerValue();
        CddlValidationMemoEntry? memo = State.BeginMapMemo(
            rule,
            container,
            start.UsedEntries,
            start.ControlResults,
            MapGroupMemoContext,
            node
        );
        if (memo == null)
        {
            return result;
        }
        if (memo.Status == CddlValidationMemoEntry.Matched)
        {
            AddStoredMapStates(result.States, memo.MapStates);
            AddStoredIssues(result.Issues, memo.Issues);
            result.CutMatched = memo.CutMatched;
            return result;
        }
        if (memo.Status == CddlValidationMemoEntry.Rejected)
        {
            AddStoredIssues(result.Issues, memo.Issues);
            result.CutMatched = memo.CutMatched;
            return result;
        }
        if (memo.IsActive)
        {
            CddlValidationIssue recursion = State.CreateIssue(
                CddlValidationCode.RecursionLimit,
                "Recursive group evaluation revisited the same map state.",
                node,
                "a productive recursive group descent",
                rule.Name
            );
            AddMapIssue(result, recursion);
            return result;
        }
        memo.IsActive = true;
        if (!State.EnterRule(rule))
        {
            memo.IsActive = false;
            memo.CompleteMapMatch(result);
            return result;
        }
        result = MatchMapGroupAt(content, entries, start);
        State.LeaveRule();
        memo.IsActive = false;
        memo.CompleteMapMatch(result);
        return result;
    }

    internal CddlMapMatchResult MatchUnwrappedMapGroup(
        CddlNode node,
        List<CborMapEntry> entries,
        CddlMapMatchState start)
    {
        CddlMapMatchResult result = new CddlMapMatchResult();
        if (node.Name == null)
        {
            return result;
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null || rule.Value == null)
        {
            CddlValidationIssue missing = State.CreateIssue(
                CddlValidationCode.ReferenceFailure,
                "The unwrapped map group target is unavailable.",
                node,
                "a map type",
                node.Name
            );
            AddMapIssue(result, missing);
            return result;
        }
        CddlNode? content = FindUnwrappedContent(
            rule.Value,
            new List<CddlRule>()
        );
        if (content == null || !IsDirectGroupNode(content))
        {
            CddlValidationIssue invalid = State.CreateIssue(
                CddlValidationCode.InvalidSchema,
                "The unwrap target does not expose a map group.",
                node,
                "an exposed map group",
                node.Name
            );
            AddMapIssue(result, invalid);
            return result;
        }
        return MatchMapGroupRule(
            rule,
            content,
            node,
            entries,
            start
        );
    }

    internal int FindMapOccurrenceMaximum(
        CddlNode entry,
        List<CborMapEntry> entries,
        CddlMapMatchState state)
    {
        if (entry.MaximumOccurrence >= 0)
        {
            return entry.MaximumOccurrence;
        }
        int remaining = entries.Count - CountUsedMapEntries(state);
        if (entry.MinimumOccurrence > remaining)
        {
            return entry.MinimumOccurrence;
        }
        return remaining;
    }

    internal CddlMapMatchState CloneMapState(CddlMapMatchState source)
    {
        CddlMapMatchState clone = new CddlMapMatchState(
            source.UsedEntries.Length
        );
        int index = 0;
        while (index < clone.UsedEntries.Length)
        {
            clone.UsedEntries[index] = source.UsedEntries[index];
            index = index + 1;
        }
        clone.ControlResults = (CddlControlResult[])
            source.ControlResults.Clone();
        return clone;
    }

    internal void AddMapStates(
        List<CddlMapMatchState> destination,
        List<CddlMapMatchState> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            AddMapState(destination, source[index]);
            index = index + 1;
        }
    }

    internal void AddMapState(
        List<CddlMapMatchState> states,
        CddlMapMatchState state)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (MapStatesEqual(states[index], state))
            {
                return;
            }
            index = index + 1;
        }
        states.Add(state);
    }

    internal bool MapStatesEqual(
        CddlMapMatchState left,
        CddlMapMatchState right)
    {
        if (left.UsedEntries.Length != right.UsedEntries.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.UsedEntries.Length)
        {
            if (left.UsedEntries[index] != right.UsedEntries[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal int CountUsedMapEntries(CddlMapMatchState state)
    {
        int count = 0;
        int index = 0;
        while (index < state.UsedEntries.Length)
        {
            if (state.UsedEntries[index])
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    internal bool ContainsCompleteMapState(List<CddlMapMatchState> states)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (CountUsedMapEntries(states[index])
                == states[index].UsedEntries.Length)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal CddlMapMatchState? FindCompleteMapState(
        List<CddlMapMatchState> states)
    {
        int index = 0;
        while (index < states.Count)
        {
            if (CountUsedMapEntries(states[index])
                == states[index].UsedEntries.Length)
            {
                return states[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlMapMatchState? FindBestMapState(
        List<CddlMapMatchState> states)
    {
        CddlMapMatchState? best = null;
        int bestCount = -1;
        int index = 0;
        while (index < states.Count)
        {
            int count = CountUsedMapEntries(states[index]);
            if (count > bestCount)
            {
                best = states[index];
                bestCount = count;
            }
            index = index + 1;
        }
        return best;
    }

    internal CddlValidationIssue CreateUnexpectedMapIssue(
        CddlNode node,
        List<CborMapEntry> entries,
        CddlMapMatchState state)
    {
        int unexpectedIndex = FindUnusedMapEntry(state);
        bool duplicate = IsDuplicateMapEntry(entries, state, unexpectedIndex);
        CborMapEntry entry = entries[unexpectedIndex];
        PushMapEntryPath(entry, unexpectedIndex);
        CddlValidationIssue issue = State.CreateIssue(
            duplicate
                ? CddlValidationCode.DuplicateMember
                : CddlValidationCode.UnknownMember,
            duplicate
                ? "The CBOR map contains a duplicate member not allowed by the CDDL group."
                : "The CBOR map contains a member not allowed by the CDDL group.",
            node,
            "only members consumed by the unordered group",
            entry == null || entry.Key == null
                ? "map entry without a key"
                : DescribeValue(entry.Key)
        );
        PopMapEntryPath();
        return issue;
    }

    internal int FindUnusedMapEntry(CddlMapMatchState state)
    {
        int index = 0;
        while (index < state.UsedEntries.Length)
        {
            if (!state.UsedEntries[index])
            {
                return index;
            }
            index = index + 1;
        }
        return 0;
    }

    internal bool IsDuplicateMapEntry(
        List<CborMapEntry> entries,
        CddlMapMatchState state,
        int candidateIndex)
    {
        CborMapEntry candidate = entries[candidateIndex];
        if (candidate == null || candidate.Key == null)
        {
            return false;
        }
        int index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index];
            if (state.UsedEntries[index]
                && entry != null
                && CborValueComparer.AreEqual(candidate.Key, entry.Key))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void AddMapIssues(
        CddlMapMatchResult destination,
        List<CddlValidationIssue> issues)
    {
        int index = 0;
        while (index < issues.Count)
        {
            AddMapIssue(destination, issues[index]);
            index = index + 1;
        }
    }

    internal void AddMapIssue(
        CddlMapMatchResult result,
        CddlValidationIssue issue)
    {
        AddBoundedDetail(result.Issues, issue);
    }

    internal bool ContainsMapIssueCode(
        List<CddlValidationIssue> issues,
        CddlValidationCode code)
    {
        int index = 0;
        while (index < issues.Count)
        {
            if (issues[index].Code == code
                || ContainsIssueCode(issues[index].Details, code))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsIssueCode(
        CddlValidationIssue[] issues,
        CddlValidationCode code)
    {
        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index].Code == code
                || ContainsIssueCode(issues[index].Details, code))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void PushMapEntryPath(CborMapEntry entry, int index)
    {
        if (entry != null
            && entry.Key != null
            && entry.Key.Kind == CborValueKind.TextString
            && entry.Key.TextStringValue != null)
        {
            State.InstancePath.PushMapTextKey(entry.Key.TextStringValue);
        }
        else
        {
            State.InstancePath.PushMapEntryIndex(index);
        }
    }

    internal void PopMapEntryPath()
    {
        State.InstancePath.PopSegment();
    }

    internal void PushMissingMapPath(CddlNode entry)
    {
        if (entry.Name != null)
        {
            State.InstancePath.PushMapTextKey(entry.Name);
        }
    }

    internal void PopMissingMapPath(CddlNode entry)
    {
        if (entry.Name != null)
        {
            State.InstancePath.PopSegment();
        }
    }

    internal string DescribeMapEntry(CddlNode entry)
    {
        if (entry.Name != null)
        {
            return "member \"" + entry.Name + "\"";
        }
        return "a matching map member";
    }

    internal void CollectEnumerationCandidates(
        CddlNode node,
        List<CddlNode> candidates,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.ParenthesizedGroup
            && node.Children.Count == 1)
        {
            CollectEnumerationCandidates(node.Children[0], candidates, visited);
            return;
        }
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                CollectEnumerationCandidates(
                    node.Children[index],
                    candidates,
                    visited
                );
                index = index + 1;
            }
            return;
        }
        if (node.Kind == CddlNodeKind.GroupEntry)
        {
            if (node.Children.Count > 0)
            {
                CollectEnumerationEntry(
                    node.Children[node.Children.Count - 1],
                    candidates,
                    visited
                );
            }
            return;
        }
        CollectEnumerationEntry(node, candidates, visited);
    }

    internal void CollectEnumerationEntry(
        CddlNode node,
        List<CddlNode> candidates,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.GroupEntry
            || node.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            CollectEnumerationCandidates(node, candidates, visited);
            return;
        }
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null
                && rule.Kind == CddlRuleKind.Group
                && rule.Value != null
                && !ContainsRule(visited, rule))
            {
                visited.Add(rule);
                CollectEnumerationCandidates(rule.Value, candidates, visited);
                visited.RemoveAt(visited.Count - 1);
                return;
            }
        }
        candidates.Add(node);
    }

    internal CddlNode? FindUnwrappedContent(
        CddlNode node,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule == null
                || rule.Value == null
                || ContainsRule(visited, rule))
            {
                return null;
            }
            visited.Add(rule);
            return FindUnwrappedContent(rule.Value, visited);
        }
        if (node.Kind == CddlNodeKind.Tag && node.Children.Count > 0)
        {
            return node.Children[node.Children.Count - 1];
        }
        if ((node.Kind == CddlNodeKind.Array || node.Kind == CddlNodeKind.Map)
            && node.Children.Count == 1)
        {
            return node.Children[0];
        }
        return null;
    }

    internal CddlNumericValue EvaluateNumericNode(
        CddlNode node,
        List<CddlRule> visited)
    {
        CddlNumericValue value = new CddlNumericValue();
        if (node.Kind == CddlNodeKind.Control)
        {
            CborValue? computed = new CddlComputedLiteralMatcher(
                this,
                State
            ).EvaluateSingleValue(node);
            if (computed != null)
            {
                return CreateNumericValue(computed);
            }
            return value;
        }
        if (node.Kind == CddlNodeKind.Literal && node.HasIntegerValue)
        {
            value.Kind = CddlNumericValue.Integer;
            value.IntegerValue = node.IntegerValue;
            return value;
        }
        if (node.Kind == CddlNodeKind.Literal && node.HasFloatValue)
        {
            value.Kind = CddlNumericValue.Float;
            value.FloatValue = node.FloatValue;
            return value;
        }
        if (node.Kind == CddlNodeKind.Representation
            && node.HasArgumentValue
            && node.ArgumentValue >= BigInteger.Zero
            && node.ArgumentValue <= new BigInteger(23))
        {
            if (node.MajorType == 0)
            {
                value.Kind = CddlNumericValue.Integer;
                value.IntegerValue = node.ArgumentValue;
            }
            else if (node.MajorType == 1)
            {
                value.Kind = CddlNumericValue.Integer;
                value.IntegerValue = -BigInteger.One - node.ArgumentValue;
            }
            return value;
        }
        if (node.Kind == CddlNodeKind.ParenthesizedType
            && node.Children.Count == 1)
        {
            return EvaluateNumericNode(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.TypeChoice
            && node.Children.Count == 1)
        {
            return EvaluateNumericNode(node.Children[0], visited);
        }
        if ((node.Kind == CddlNodeKind.RuleReference
                || node.Kind == CddlNodeKind.Unwrap)
            && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null
                && rule.Value != null
                && !ContainsRule(visited, rule))
            {
                visited.Add(rule);
                return EvaluateNumericNode(rule.Value, visited);
            }
        }
        return value;
    }

    internal int FindNodeIndex(CddlNode node)
    {
        int index = 0;
        while (index < State.Schema.NormalizedNodes.Length)
        {
            if (State.Schema.NormalizedNodes[index] == node)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal int FindMajorType(CborValue value)
    {
        switch (value.Kind)
        {
            case CborValueKind.UnsignedInteger:
                return 0;
            case CborValueKind.NegativeInteger:
                return 1;
            case CborValueKind.ByteString:
                return 2;
            case CborValueKind.TextString:
                return 3;
            case CborValueKind.Array:
                return 4;
            case CborValueKind.Map:
                return 5;
            case CborValueKind.Tag:
                return 6;
            case CborValueKind.Float:
            case CborValueKind.SimpleValue:
                return 7;
            default:
                return -1;
        }
    }

    internal int FindAdditionalInformation(CborValue value)
    {
        int major = FindMajorType(value);
        if (major < 0)
        {
            return -1;
        }
        if (major >= 2 && major <= 5 && IsIndefinite(value))
        {
            return 31;
        }
        if (value.Kind == CborValueKind.Float)
        {
            return FindFloatAdditionalInformation(value);
        }
        if (value.Kind == CborValueKind.SimpleValue)
        {
            return value.SimpleValue <= 23 ? value.SimpleValue : 24;
        }
        BigInteger argument = FindHeadArgument(value);
        if (argument.Sign < 0)
        {
            return -1;
        }
        int retainedWidth = FindRetainedHeadWidth(value);
        if (retainedWidth >= 0)
        {
            return AdditionalInformationForWidth(argument, retainedWidth);
        }
        int preferredWidth = FindPreferredHeadWidth(argument);
        return AdditionalInformationForWidth(argument, preferredWidth);
    }

    internal int FindSimpleArgument(CborValue value)
    {
        if (value.Kind == CborValueKind.SimpleValue)
        {
            return value.SimpleValue;
        }
        if (value.Kind == CborValueKind.Float)
        {
            return FindFloatAdditionalInformation(value);
        }
        return -1;
    }

    internal int FindFloatAdditionalInformation(CborValue value)
    {
        CborFloatWidth width = value.FloatWidth;
        if (width == CborFloatWidth.None && value.FloatValue != null)
        {
            width = value.FloatValue.Width;
        }
        if (width == CborFloatWidth.None && value.Representation != null)
        {
            width = value.Representation.FloatWidth;
        }
        if (width == CborFloatWidth.Half)
        {
            return 25;
        }
        if (width == CborFloatWidth.Single)
        {
            return 26;
        }
        if (width == CborFloatWidth.Double)
        {
            return 27;
        }
        return -1;
    }

    internal BigInteger FindHeadArgument(CborValue value)
    {
        if (value.Kind == CborValueKind.UnsignedInteger)
        {
            return value.IntegerValue;
        }
        if (value.Kind == CborValueKind.NegativeInteger)
        {
            return -BigInteger.One - value.IntegerValue;
        }
        if (value.Kind == CborValueKind.ByteString)
        {
            return new BigInteger(value.ByteStringValue == null
                ? 0
                : value.ByteStringValue.Length);
        }
        if (value.Kind == CborValueKind.TextString)
        {
            return new BigInteger(value.TextStringValue == null
                ? 0
                : Encoding.UTF8.GetByteCount(value.TextStringValue));
        }
        if (value.Kind == CborValueKind.Array)
        {
            return new BigInteger(value.ArrayItems == null
                ? 0
                : value.ArrayItems.Count);
        }
        if (value.Kind == CborValueKind.Map)
        {
            return new BigInteger(value.MapEntries == null
                ? 0
                : value.MapEntries.Count);
        }
        if (value.Kind == CborValueKind.Tag)
        {
            return new BigInteger(value.TagNumber);
        }
        return new BigInteger(-1);
    }

    internal int FindRetainedHeadWidth(CborValue value)
    {
        if (value.Representation == null)
        {
            return -1;
        }
        return value.Representation.HeadArgumentWidth;
    }

    internal int FindPreferredHeadWidth(BigInteger argument)
    {
        if (argument <= new BigInteger(23))
        {
            return 0;
        }
        if (argument <= new BigInteger(byte.MaxValue))
        {
            return 1;
        }
        if (argument <= new BigInteger(ushort.MaxValue))
        {
            return 2;
        }
        if (argument <= new BigInteger(uint.MaxValue))
        {
            return 4;
        }
        if (argument <= new BigInteger(ulong.MaxValue))
        {
            return 8;
        }
        return -1;
    }

    internal int AdditionalInformationForWidth(
        BigInteger argument,
        int width)
    {
        if (width == 0)
        {
            if (argument <= new BigInteger(23))
            {
                return (int)argument;
            }
            return -1;
        }
        if (width == 1)
        {
            return 24;
        }
        if (width == 2)
        {
            return 25;
        }
        if (width == 4)
        {
            return 26;
        }
        if (width == 8)
        {
            return 27;
        }
        return -1;
    }

    internal bool IsIndefinite(CborValue value)
    {
        if (value.LengthKind == CborLengthKind.Indefinite)
        {
            return true;
        }
        return value.Representation != null
            && value.Representation.LengthKind == CborLengthKind.Indefinite;
    }

    internal bool ByteArraysEqual(byte[]? left, byte[]? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool NumericFloatsEqual(double left, double right)
    {
        if (double.IsNaN(left) || double.IsNaN(right))
        {
            return double.IsNaN(left) && double.IsNaN(right);
        }
        return left == right;
    }

    internal bool ContainsRule(List<CddlRule> rules, CddlRule rule)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index] == rule)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal string DescribeLiteral(CddlNode node)
    {
        if (node.HasIntegerValue)
        {
            return node.IntegerValue.ToString();
        }
        if (node.HasFloatValue)
        {
            return node.FloatValue.ToString(
                "R",
                System.Globalization.CultureInfo.InvariantCulture
            );
        }
        if (node.LiteralKind == CddlLiteralKind.TextString)
        {
            return "text \"" + (node.TextValue ?? "") + "\"";
        }
        return "byte string";
    }

    internal string DescribeValue(CborValue value)
    {
        switch (value.Kind)
        {
            case CborValueKind.UnsignedInteger:
            case CborValueKind.NegativeInteger:
                return "integer " + value.IntegerValue;
            case CborValueKind.ByteString:
                return "byte string";
            case CborValueKind.TextString:
                return "text \"" + (value.TextStringValue ?? "") + "\"";
            case CborValueKind.Array:
                return "array";
            case CborValueKind.Map:
                return "map";
            case CborValueKind.Tag:
                return "tag " + value.TagNumber;
            case CborValueKind.Float:
                if (value.FloatValue != null)
                {
                    return "float " + value.FloatValue.Value.ToString(
                        "R",
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                }
                return "float";
            case CborValueKind.SimpleValue:
                return "simple " + value.SimpleValue;
            default:
                return "invalid CBOR value";
        }
    }

    internal string DescribeRange(
        CddlNumericValue lower,
        CddlNumericValue upper,
        bool upperExclusive)
    {
        string separator = upperExclusive ? "..." : "..";
        if (lower.Kind == CddlNumericValue.Integer)
        {
            return lower.IntegerValue + separator + upper.IntegerValue;
        }
        return lower.FloatValue.ToString(
                "R",
                System.Globalization.CultureInfo.InvariantCulture)
            + separator
            + upper.FloatValue.ToString(
                "R",
                System.Globalization.CultureInfo.InvariantCulture);
    }

    internal CddlMatchResult RejectReference(CddlNode node, string message)
    {
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ReferenceFailure,
            message,
            node,
            "a resolvable type rule",
            node.Name
        );
        return CreateRejection(issue);
    }

    internal CddlMatchResult RejectInvalidSchema(
        CddlNode? node,
        string message)
    {
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.InvalidSchema,
            message,
            node,
            "a valid compiled schema node",
            null
        );
        return CreateRejection(issue);
    }

    internal CddlMatchResult RejectUnsupported(CddlNode node, string message)
    {
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.UnsupportedFeature,
            message,
            node,
            "a supported core CDDL type",
            node.ControlName
        );
        return CreateRejection(issue);
    }

    internal CddlMatchResult CreateMatch()
    {
        return new CddlMatchResult(true, null);
    }

    internal CddlMatchResult CreateRejection(CddlValidationIssue issue)
    {
        return new CddlMatchResult(false, issue);
    }

    internal CddlMatchResult CreateNoIssueRejection()
    {
        return new CddlMatchResult(false, null);
    }
}
