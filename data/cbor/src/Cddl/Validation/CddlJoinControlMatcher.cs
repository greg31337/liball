using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CddlJoinControlMatcher
{
    internal CddlCoreMatcher Matcher;
    internal CddlValidationState State;
    internal CddlNode? ControlNode;
    internal byte[] TargetBytes = [];
    internal CborValueKind TargetKind;

    internal CddlJoinControlMatcher(
        CddlCoreMatcher matcher,
        CddlValidationState state)
    {
        Matcher = matcher;
        State = state;
    }

    internal CddlMatchResult MatchJoinControl(
        CddlNode node,
        CborValue value)
    {
        ControlNode = node;
        TargetKind = value.Kind;
        if (value.Kind == CborValueKind.TextString)
        {
            string text = value.TextStringValue ?? "";
            if (!IsValidText(text))
            {
                CddlValidationIssue invalidText = State.CreateIssue(
                    CddlValidationCode.ControlMismatch,
                    "The join target is not valid Unicode text.",
                    node,
                    "a valid UTF-8 text string",
                    "invalid Unicode scalar sequence"
                );
                return Matcher.CreateRejection(invalidText);
            }
            TargetBytes = Encoding.UTF8.GetBytes(text);
        }
        else if (value.Kind == CborValueKind.ByteString)
        {
            TargetBytes = value.ByteStringValue ?? [];
        }
        else
        {
            return Matcher.RejectInvalidSchema(
                node,
                "A join target must be a text or byte string."
            );
        }
        List<CddlNode> arrays = new List<CddlNode>();
        CollectJoinArrays(
            node.Children[1],
            arrays,
            new List<CddlRule>()
        );
        if (arrays.Count == 0)
        {
            return Matcher.RejectInvalidSchema(
                node,
                "A join controller must resolve to at least one array type."
            );
        }
        int index = 0;
        while (index < arrays.Count)
        {
            CddlNode array = arrays[index];
            if (array.Children.Count != 1)
            {
                return Matcher.RejectInvalidSchema(
                    node,
                    "A join controller array must contain one group expression."
                );
            }
            if (arrays.Count > 1 && !State.TakeAlternative(node))
            {
                return Matcher.CreateNoIssueRejection();
            }
            int checkpoint = State.CreateControlResultCheckpoint();
            List<CddlJoinPosition> starts = new List<CddlJoinPosition>();
            starts.Add(new CddlJoinPosition(0, 0));
            List<CddlJoinPosition> matches = MatchJoinGroup(
                array.Children[0],
                starts
            );
            int matchIndex = 0;
            while (matchIndex < matches.Count)
            {
                if (matches[matchIndex].Offset == TargetBytes.Length)
                {
                    return Matcher.CreateMatch();
                }
                matchIndex = matchIndex + 1;
            }
            State.RestoreControlResults(checkpoint);
            if (State.Diagnostics.HasErrors)
            {
                return Matcher.CreateNoIssueRejection();
            }
            index = index + 1;
        }
        CddlValidationIssue issue = State.CreateIssue(
            CddlValidationCode.ControlMismatch,
            "The string cannot be decomposed into the CDDL join controller elements.",
            node,
            "a concatenation accepted by the controller array",
            TargetBytes.Length + " target byte(s)"
        );
        return Matcher.CreateRejection(issue);
    }

    internal void CollectJoinArrays(
        CddlNode node,
        List<CddlNode> arrays,
        List<CddlRule> visited)
    {
        if (node.Kind == CddlNodeKind.Array)
        {
            arrays.Add(node);
            return;
        }
        if (node.Kind == CddlNodeKind.ParenthesizedType
            || node.Kind == CddlNodeKind.TypeChoice)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                CollectJoinArrays(node.Children[index], arrays, visited);
                index = index + 1;
            }
            return;
        }
        if (node.Kind == CddlNodeKind.RuleReference && node.Name != null)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null
                && rule.Value != null
                && !Matcher.ContainsRule(visited, rule))
            {
                visited.Add(rule);
                CollectJoinArrays(rule.Value, arrays, visited);
                visited.RemoveAt(visited.Count - 1);
            }
        }
    }

    internal List<CddlJoinPosition> MatchJoinGroup(
        CddlNode node,
        List<CddlJoinPosition> starts)
    {
        List<CddlJoinPosition> empty = new List<CddlJoinPosition>();
        CddlNode? control = ControlNode;
        if (control == null || !State.EnterDataNesting(control))
        {
            return empty;
        }
        if (!State.TakeStep(node))
        {
            State.LeaveDataNesting();
            return empty;
        }
        List<CddlJoinPosition> result;
        if (node.Kind == CddlNodeKind.Group)
        {
            result = MatchJoinSequence(node, starts);
        }
        else if (node.Kind == CddlNodeKind.GroupChoice)
        {
            result = MatchJoinChoice(node, starts);
        }
        else if (node.Kind == CddlNodeKind.GroupEntry)
        {
            result = MatchJoinOccurrences(node, starts);
        }
        else if (node.Kind == CddlNodeKind.ParenthesizedGroup
            && node.Children.Count == 1)
        {
            result = MatchJoinGroup(node.Children[0], starts);
        }
        else if (Matcher.IsGroupProducingNode(node))
        {
            result = MatchJoinIncludedGroup(node, starts);
        }
        else
        {
            result = empty;
        }
        State.LeaveDataNesting();
        return result;
    }

    internal List<CddlJoinPosition> MatchJoinSequence(
        CddlNode group,
        List<CddlJoinPosition> starts)
    {
        List<CddlJoinPosition> current = ClonePositions(starts);
        int index = 0;
        while (index < group.Children.Count && current.Count > 0)
        {
            current = MatchJoinGroup(group.Children[index], current);
            if (State.Diagnostics.HasErrors)
            {
                return new List<CddlJoinPosition>();
            }
            index = index + 1;
        }
        return current;
    }

    internal List<CddlJoinPosition> MatchJoinChoice(
        CddlNode choice,
        List<CddlJoinPosition> starts)
    {
        List<CddlJoinPosition> result = new List<CddlJoinPosition>();
        int index = 0;
        while (index < choice.Children.Count)
        {
            if (!State.TakeAlternative(choice))
            {
                return new List<CddlJoinPosition>();
            }
            int checkpoint = State.CreateControlResultCheckpoint();
            List<CddlJoinPosition> branch = MatchJoinGroup(
                choice.Children[index],
                starts
            );
            AddPositions(result, branch);
            if (branch.Count == 0)
            {
                State.RestoreControlResults(checkpoint);
            }
            if (State.Diagnostics.HasErrors)
            {
                return new List<CddlJoinPosition>();
            }
            index = index + 1;
        }
        return result;
    }

    internal List<CddlJoinPosition> MatchJoinOccurrences(
        CddlNode entry,
        List<CddlJoinPosition> starts)
    {
        List<CddlJoinPosition> result = new List<CddlJoinPosition>();
        if (entry.Children.Count == 0)
        {
            return result;
        }
        if (entry.MinimumOccurrence == 0)
        {
            AddPositions(result, starts);
        }
        List<CddlJoinPosition> current = ClonePositions(starts);
        int maximum = JoinOccurrenceMaximum(entry);
        int count = 0;
        while (count < maximum && current.Count > 0)
        {
            if ((entry.MinimumOccurrence != entry.MaximumOccurrence
                    || current.Count > 1)
                && !State.TakeAlternative(entry))
            {
                return new List<CddlJoinPosition>();
            }
            List<CddlJoinPosition> next = MatchJoinEntryOnce(
                entry,
                current
            );
            if (count + 1 >= entry.MinimumOccurrence)
            {
                AddPositions(result, next);
            }
            current = next;
            count = count + 1;
        }
        return result;
    }

    internal int JoinOccurrenceMaximum(CddlNode entry)
    {
        if (entry.MaximumOccurrence >= 0)
        {
            return entry.MaximumOccurrence;
        }
        return TargetBytes.Length + entry.MinimumOccurrence + 1;
    }

    internal List<CddlJoinPosition> MatchJoinEntryOnce(
        CddlNode entry,
        List<CddlJoinPosition> starts)
    {
        CddlNode value = entry.Children[entry.Children.Count - 1];
        if (Matcher.IsGroupProducingNode(value))
        {
            return MatchJoinIncludedGroup(value, starts);
        }
        List<CddlJoinPosition> result = new List<CddlJoinPosition>();
        int index = 0;
        while (index < starts.Count)
        {
            List<CddlJoinPosition> matches = MatchJoinComponent(
                value,
                starts[index]
            );
            AddPositions(result, matches);
            if (State.Diagnostics.HasErrors)
            {
                return new List<CddlJoinPosition>();
            }
            index = index + 1;
        }
        return result;
    }

    internal List<CddlJoinPosition> MatchJoinIncludedGroup(
        CddlNode node,
        List<CddlJoinPosition> starts)
    {
        if (node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice
            || node.Kind == CddlNodeKind.GroupEntry
            || node.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            return MatchJoinGroup(node, starts);
        }
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null || rule.Value == null)
        {
            return new List<CddlJoinPosition>();
        }
        CddlNode? content = rule.Value;
        if (node.Kind == CddlNodeKind.Unwrap)
        {
            content = Matcher.FindUnwrappedContent(
                rule.Value,
                new List<CddlRule>()
            );
        }
        if (content == null || !State.EnterRule(rule))
        {
            return new List<CddlJoinPosition>();
        }
        List<CddlJoinPosition> result = MatchJoinGroup(content, starts);
        State.LeaveRule();
        return result;
    }

    internal List<CddlJoinPosition> MatchJoinComponent(
        CddlNode schema,
        CddlJoinPosition start)
    {
        List<CddlJoinPosition> result = new List<CddlJoinPosition>();
        CborValue? singleton = new CddlComputedLiteralMatcher(
            Matcher,
            State
        ).EvaluateSingleValue(schema);
        if (singleton != null
            && (singleton.Kind == CborValueKind.TextString
                || singleton.Kind == CborValueKind.ByteString))
        {
            MatchSingletonComponent(schema, singleton, start, result);
            return result;
        }
        int end = start.Offset;
        while (end <= TargetBytes.Length)
        {
            if (!State.TakeAlternative(schema))
            {
                return new List<CddlJoinPosition>();
            }
            if (start.Components == 0)
            {
                MatchComponentKind(schema, start, end, TargetKind, result);
            }
            else
            {
                MatchComponentKind(
                    schema,
                    start,
                    end,
                    CborValueKind.ByteString,
                    result
                );
                MatchComponentKind(
                    schema,
                    start,
                    end,
                    CborValueKind.TextString,
                    result
                );
            }
            if (State.Diagnostics.HasErrors)
            {
                return new List<CddlJoinPosition>();
            }
            end = end + 1;
        }
        return result;
    }

    internal void MatchSingletonComponent(
        CddlNode schema,
        CborValue singleton,
        CddlJoinPosition start,
        List<CddlJoinPosition> result)
    {
        if (start.Components == 0 && singleton.Kind != TargetKind)
        {
            return;
        }
        byte[] bytes = singleton.Kind == CborValueKind.ByteString
            ? singleton.ByteStringValue ?? []
            : Encoding.UTF8.GetBytes(singleton.TextStringValue ?? "");
        if (!SegmentEquals(start.Offset, bytes))
        {
            return;
        }
        AddPosition(
            result,
            new CddlJoinPosition(
                start.Offset + bytes.Length,
                start.Components + 1
            )
        );
    }

    internal void MatchComponentKind(
        CddlNode schema,
        CddlJoinPosition start,
        int end,
        CborValueKind kind,
        List<CddlJoinPosition> result)
    {
        CborValue? candidate = CreateComponent(start.Offset, end, kind);
        if (candidate == null)
        {
            return;
        }
        int checkpoint = State.CreateControlResultCheckpoint();
        State.InstancePath.PushArrayIndex(start.Components);
        CddlMatchResult match = Matcher.MatchNode(schema, candidate);
        State.InstancePath.PopSegment();
        if (match.IsMatch)
        {
            AddPosition(
                result,
                new CddlJoinPosition(end, start.Components + 1)
            );
        }
        else
        {
            State.RestoreControlResults(checkpoint);
        }
    }

    internal CborValue? CreateComponent(
        int start,
        int end,
        CborValueKind kind)
    {
        int length = end - start;
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < length)
        {
            bytes[index] = TargetBytes[start + index];
            index = index + 1;
        }
        CborValue value = new CborValue();
        value.Kind = kind;
        value.LengthKind = CborLengthKind.Definite;
        if (kind == CborValueKind.ByteString)
        {
            value.ByteStringValue = bytes;
            return value;
        }
        CborUtf8Result decoded = new CborUtf8().ProcessChunk(bytes, true);
        if (decoded.Status != CborReadStatus.Done)
        {
            return null;
        }
        value.TextStringValue = decoded.Text;
        return value;
    }

    internal bool SegmentEquals(int start, byte[] expected)
    {
        if (start > TargetBytes.Length - expected.Length)
        {
            return false;
        }
        int index = 0;
        while (index < expected.Length)
        {
            if (TargetBytes[start + index] != expected[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsValidText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length
                    || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }
                index = index + 2;
            }
            else if (char.IsLowSurrogate(value[index]))
            {
                return false;
            }
            else
            {
                index = index + 1;
            }
        }
        return true;
    }

    internal List<CddlJoinPosition> ClonePositions(
        List<CddlJoinPosition> source)
    {
        List<CddlJoinPosition> result = new List<CddlJoinPosition>();
        AddPositions(result, source);
        return result;
    }

    internal void AddPositions(
        List<CddlJoinPosition> destination,
        List<CddlJoinPosition> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            AddPosition(destination, source[index]);
            index = index + 1;
        }
    }

    internal void AddPosition(
        List<CddlJoinPosition> positions,
        CddlJoinPosition candidate)
    {
        int index = 0;
        while (index < positions.Count)
        {
            if (positions[index].Offset == candidate.Offset
                && positions[index].Components == candidate.Components)
            {
                return;
            }
            index = index + 1;
        }
        positions.Add(candidate);
    }
}
