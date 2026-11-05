using System;
using System.Collections.Generic;

namespace xml;

internal class XmlSchemaRegexMatcher
{
    internal XmlSchemaRegexUnicodeProperties UnicodeProperties;
    internal int WorkCount;
    internal bool WorkLimitReached;

    internal XmlSchemaRegexMatcher()
    {
        UnicodeProperties = new XmlSchemaRegexUnicodeProperties();
        WorkCount = 0;
        WorkLimitReached = false;
    }

    internal bool IsXmlSchemaRegexMatch(XmlSchemaRegexProgram program, string value)
    {
        return FindXmlSchemaRegexMatch(program, value, 0).Success;
    }

    internal bool IsEntireXmlSchemaRegexMatch(XmlSchemaRegexProgram program, string value)
    {
        XmlSchemaRegexInput input = CreateXmlSchemaRegexInput(value);
        WorkCount = 0;
        WorkLimitReached = false;
        XmlSchemaRegexMatchState initial = new XmlSchemaRegexMatchState(0, program.CaptureCount);
        List<XmlSchemaRegexMatchState> matches = MatchXmlSchemaRegexNode(program.Root, initial, input, program);
        int index = 0;
        while (index < matches.Count)
        {
            if (matches[index].Position == input.Codepoints.Length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XmlSchemaRegexMatch FindXmlSchemaRegexMatch(XmlSchemaRegexProgram program, string value, int startUtf16)
    {
        XmlSchemaRegexInput input = CreateXmlSchemaRegexInput(value);
        int start = FindXmlSchemaRegexScalarBoundary(input, startUtf16);
        WorkCount = 0;
        WorkLimitReached = false;
        while (start <= input.Codepoints.Length && !WorkLimitReached)
        {
            XmlSchemaRegexMatchState initial = new XmlSchemaRegexMatchState(start, program.CaptureCount);
            List<XmlSchemaRegexMatchState> matches = MatchXmlSchemaRegexNode(program.Root, initial, input, program);
            if (matches.Count != 0)
            {
                return CreateXmlSchemaRegexMatch(matches[0], start, input, program.CaptureCount);
            }

            start = start + 1;
        }

        return new XmlSchemaRegexMatch(false, program.CaptureCount);
    }

    internal XmlSchemaRegexInput CreateXmlSchemaRegexInput(string value)
    {
        List<int> codepoints = new List<int>();
        List<int> boundaries = new List<int>();
        boundaries.Add(0);
        int offset = 0;
        while (offset < value.Length)
        {
            int first = value[offset];
            if (first >= 0xD800 && first <= 0xDBFF && offset + 1 < value.Length && value[offset + 1] >= 0xDC00 && value[offset + 1] <= 0xDFFF)
            {
                int second = value[offset + 1];
                codepoints.Add(0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00);
                offset = offset + 2;
            }
            else
            {
                codepoints.Add(first);
                offset = offset + 1;
            }

            boundaries.Add(offset);
        }

        return new XmlSchemaRegexInput(value, codepoints.ToArray(), boundaries.ToArray());
    }

    internal int FindXmlSchemaRegexScalarBoundary(XmlSchemaRegexInput input, int utf16Offset)
    {
        if (utf16Offset <= 0)
        {
            return 0;
        }

        int index = 0;
        while (index < input.Utf16Boundaries.Length && input.Utf16Boundaries[index] < utf16Offset)
        {
            index = index + 1;
        }

        return index;
    }

    internal XmlSchemaRegexMatch CreateXmlSchemaRegexMatch(XmlSchemaRegexMatchState state, int start, XmlSchemaRegexInput input, int captureCount)
    {
        XmlSchemaRegexMatch result = new XmlSchemaRegexMatch(true, captureCount);
        state.CaptureStarts[0] = start;
        state.CaptureEnds[0] = state.Position;
        int index = 0;
        while (index <= captureCount)
        {
            int captureStart = state.CaptureStarts[index];
            int captureEnd = state.CaptureEnds[index];
            if (captureStart >= 0 && captureEnd >= captureStart)
            {
                int utf16Start = input.Utf16Boundaries[captureStart];
                int utf16End = input.Utf16Boundaries[captureEnd];
                result.Captures[index] = input.Value.Substring(utf16Start, utf16End - utf16Start);
                result.CaptureIndexes[index] = utf16Start;
                result.CaptureLengths[index] = utf16End - utf16Start;
            }

            index = index + 1;
        }

        result.Index = result.CaptureIndexes[0];
        result.Length = result.CaptureLengths[0];
        return result;
    }

    internal List<XmlSchemaRegexMatchState> MatchXmlSchemaRegexNode(XmlSchemaRegexNode node, XmlSchemaRegexMatchState state, XmlSchemaRegexInput input, XmlSchemaRegexProgram program)
    {
        WorkCount = WorkCount + 1;
        if (WorkCount > 1000000)
        {
            WorkLimitReached = true;
            return new List<XmlSchemaRegexMatchState>();
        }

        if (node.Kind == XmlSchemaRegexNodeKind.Empty)
        {
            return CreateXmlSchemaRegexSingleStateList(CloneXmlSchemaRegexState(state));
        }

        if (node.Kind == XmlSchemaRegexNodeKind.Sequence)
        {
            return MatchXmlSchemaRegexSequence(node, state, input, program);
        }

        if (node.Kind == XmlSchemaRegexNodeKind.Alternation)
        {
            return MatchXmlSchemaRegexAlternation(node, state, input, program);
        }

        if (node.Kind == XmlSchemaRegexNodeKind.Repeat)
        {
            List<XmlSchemaRegexMatchState> repeated = new List<XmlSchemaRegexMatchState>();
            ExpandXmlSchemaRegexRepeat(node, state, input, program, 0, repeated);
            return repeated;
        }

        if (node.Kind == XmlSchemaRegexNodeKind.Capture)
        {
            return MatchXmlSchemaRegexCapture(node, state, input, program);
        }

        if (node.Kind == XmlSchemaRegexNodeKind.BackReference)
        {
            return MatchXmlSchemaRegexBackReference(node, state, input, program);
        }

        if (node.Kind == XmlSchemaRegexNodeKind.StartAnchor)
        {
            bool startMatches = state.Position == 0 || program.MultiLine && state.Position > 0 && state.Position < input.Codepoints.Length && input.Codepoints[state.Position - 1] == 10;
            return startMatches ? CreateXmlSchemaRegexSingleStateList(CloneXmlSchemaRegexState(state)) : new List<XmlSchemaRegexMatchState>();
        }

        if (node.Kind == XmlSchemaRegexNodeKind.EndAnchor)
        {
            bool endMatches;
            if (program.MultiLine)
            {
                endMatches = state.Position < input.Codepoints.Length && input.Codepoints[state.Position] == 10 || state.Position == input.Codepoints.Length && (state.Position == 0 || input.Codepoints[state.Position - 1] != 10);
            }
            else
            {
                endMatches = state.Position == input.Codepoints.Length;
            }

            return endMatches ? CreateXmlSchemaRegexSingleStateList(CloneXmlSchemaRegexState(state)) : new List<XmlSchemaRegexMatchState>();
        }

        if (state.Position >= input.Codepoints.Length)
        {
            return new List<XmlSchemaRegexMatchState>();
        }

        int codepoint = input.Codepoints[state.Position];
        bool matched = false;
        if (node.Kind == XmlSchemaRegexNodeKind.Literal)
        {
            matched = AreXmlSchemaRegexCodepointsEqual(node.Codepoint, codepoint, program.IgnoreCase);
        }
        else if (node.Kind == XmlSchemaRegexNodeKind.Dot)
        {
            matched = program.DotAll || codepoint != 10 && codepoint != 13;
        }
        else if (node.Kind == XmlSchemaRegexNodeKind.CharacterClass && node.CharacterSet != null)
        {
            matched = MatchesXmlSchemaRegexCharacterSet(node.CharacterSet, codepoint, program.IgnoreCase);
        }

        if (!matched)
        {
            return new List<XmlSchemaRegexMatchState>();
        }

        XmlSchemaRegexMatchState advanced = CloneXmlSchemaRegexState(state);
        advanced.Position = advanced.Position + 1;
        return CreateXmlSchemaRegexSingleStateList(advanced);
    }

    internal List<XmlSchemaRegexMatchState> MatchXmlSchemaRegexSequence(XmlSchemaRegexNode node, XmlSchemaRegexMatchState state, XmlSchemaRegexInput input, XmlSchemaRegexProgram program)
    {
        List<XmlSchemaRegexMatchState> states = CreateXmlSchemaRegexSingleStateList(CloneXmlSchemaRegexState(state));
        int childIndex = 0;
        while (childIndex < node.Children.Length && states.Count != 0 && !WorkLimitReached)
        {
            List<XmlSchemaRegexMatchState> nextStates = new List<XmlSchemaRegexMatchState>();
            int stateIndex = 0;
            while (stateIndex < states.Count && !WorkLimitReached)
            {
                List<XmlSchemaRegexMatchState> matches = MatchXmlSchemaRegexNode(node.Children[childIndex], states[stateIndex], input, program);
                nextStates.AddRange(matches);
                stateIndex = stateIndex + 1;
            }

            states = nextStates;
            childIndex = childIndex + 1;
        }

        return states;
    }

    internal List<XmlSchemaRegexMatchState> MatchXmlSchemaRegexAlternation(XmlSchemaRegexNode node, XmlSchemaRegexMatchState state, XmlSchemaRegexInput input, XmlSchemaRegexProgram program)
    {
        List<XmlSchemaRegexMatchState> result = new List<XmlSchemaRegexMatchState>();
        int index = 0;
        while (index < node.Children.Length && !WorkLimitReached)
        {
            result.AddRange(MatchXmlSchemaRegexNode(node.Children[index], state, input, program));
            index = index + 1;
        }

        return result;
    }

    internal List<XmlSchemaRegexMatchState> MatchXmlSchemaRegexCapture(XmlSchemaRegexNode node, XmlSchemaRegexMatchState state, XmlSchemaRegexInput input, XmlSchemaRegexProgram program)
    {
        XmlSchemaRegexMatchState started = CloneXmlSchemaRegexState(state);
        started.CaptureStarts[node.CaptureNumber] = state.Position;
        started.CaptureEnds[node.CaptureNumber] = -1;
        List<XmlSchemaRegexMatchState> matches = MatchXmlSchemaRegexNode(node.Children[0], started, input, program);
        int index = 0;
        while (index < matches.Count)
        {
            matches[index].CaptureEnds[node.CaptureNumber] = matches[index].Position;
            index = index + 1;
        }

        return matches;
    }

    internal List<XmlSchemaRegexMatchState> MatchXmlSchemaRegexBackReference(XmlSchemaRegexNode node, XmlSchemaRegexMatchState state, XmlSchemaRegexInput input, XmlSchemaRegexProgram program)
    {
        int start = state.CaptureStarts[node.CaptureNumber];
        int end = state.CaptureEnds[node.CaptureNumber];
        if (start < 0 || end < start)
        {
            return CreateXmlSchemaRegexSingleStateList(CloneXmlSchemaRegexState(state));
        }

        int length = end - start;
        if (state.Position + length > input.Codepoints.Length)
        {
            return new List<XmlSchemaRegexMatchState>();
        }

        int index = 0;
        while (index < length)
        {
            if (!AreXmlSchemaRegexCodepointsEqual(input.Codepoints[start + index], input.Codepoints[state.Position + index], program.IgnoreCase))
            {
                return new List<XmlSchemaRegexMatchState>();
            }

            index = index + 1;
        }

        XmlSchemaRegexMatchState advanced = CloneXmlSchemaRegexState(state);
        advanced.Position = advanced.Position + length;
        return CreateXmlSchemaRegexSingleStateList(advanced);
    }

    internal void ExpandXmlSchemaRegexRepeat(XmlSchemaRegexNode node, XmlSchemaRegexMatchState state, XmlSchemaRegexInput input, XmlSchemaRegexProgram program, int count, List<XmlSchemaRegexMatchState> result)
    {
        List<XmlSchemaRegexRepeatFrame> pending = new List<XmlSchemaRegexRepeatFrame>();
        HashSet<long> expanded = new HashSet<long>();
        pending.Add(new XmlSchemaRegexRepeatFrame(state, count, false));
        while (pending.Count != 0 && !WorkLimitReached)
        {
            int last = pending.Count - 1;
            XmlSchemaRegexRepeatFrame frame = pending[last];
            pending.RemoveAt(last);
            if (frame.Emit)
            {
                result.Add(CloneXmlSchemaRegexState(frame.State));
                continue;
            }

            long expansionKey = node.Maximum < 0 && frame.Count >= node.Minimum ? frame.State.Position : ((long)frame.State.Position << 32) | (uint)frame.Count;
            if (!program.HasBackReferences && !expanded.Add(expansionKey))
            {
                continue;
            }

            if (node.Reluctant && frame.Count >= node.Minimum)
            {
                result.Add(CloneXmlSchemaRegexState(frame.State));
            }

            if (!node.Reluctant && frame.Count >= node.Minimum)
            {
                pending.Add(new XmlSchemaRegexRepeatFrame(frame.State, frame.Count, true));
            }

            bool belowMaximum = node.Maximum < 0 || frame.Count < node.Maximum;
            bool belowSafeLimit = node.Maximum >= 0 || frame.Count < input.Codepoints.Length + node.Minimum + 1;
            if (belowMaximum && belowSafeLimit)
            {
                List<XmlSchemaRegexMatchState> matches = MatchXmlSchemaRegexNode(node.Children[0], frame.State, input, program);
                int index = matches.Count - 1;
                while (index >= 0)
                {
                    XmlSchemaRegexMatchState next = matches[index];
                    if (next.Position != frame.State.Position || frame.Count + 1 < node.Minimum)
                    {
                        pending.Add(new XmlSchemaRegexRepeatFrame(next, frame.Count + 1, false));
                    }
                    else if (frame.Count < node.Minimum && frame.Count + 1 >= node.Minimum)
                    {
                        pending.Add(new XmlSchemaRegexRepeatFrame(next, frame.Count + 1, true));
                    }

                    index = index - 1;
                }
            }
        }
    }

    internal bool MatchesXmlSchemaRegexCharacterSet(XmlSchemaRegexCharacterSet set, int codepoint, bool ignoreCase)
    {
        bool matched = MatchesXmlSchemaRegexCharacterRanges(set, codepoint);
        if (ignoreCase && !matched)
        {
            int upper = ReadXmlSchemaRegexUppercaseCodepoint(codepoint);
            int lower = ReadXmlSchemaRegexLowercaseCodepoint(codepoint);
            matched = MatchesXmlSchemaRegexCharacterRanges(set, upper);
            matched = matched || MatchesXmlSchemaRegexCharacterRanges(set, lower);
            matched = matched || MatchesXmlSchemaRegexCharacterRanges(set, ReadXmlSchemaRegexUppercaseCodepoint(lower));
            matched = matched || MatchesXmlSchemaRegexCharacterRanges(set, ReadXmlSchemaRegexLowercaseCodepoint(upper));
            int compatibilityVariant = ReadXmlSchemaRegexCompatibilityCaseVariant(lower);
            matched = matched || compatibilityVariant >= 0 && MatchesXmlSchemaRegexCharacterRanges(set, compatibilityVariant);
        }

        if (!matched)
        {
            matched = MatchesXmlSchemaRegexCharacterProperties(set, codepoint);
        }

        if (set.Negated)
        {
            matched = !matched;
        }

        if (matched && set.Subtraction != null && MatchesXmlSchemaRegexCharacterSet(set.Subtraction, codepoint, ignoreCase))
        {
            matched = false;
        }

        return matched;
    }

    internal bool MatchesXmlSchemaRegexCharacterRanges(XmlSchemaRegexCharacterSet set, int codepoint)
    {
        int index = 0;
        while (index < set.RangeStarts.Count)
        {
            if (codepoint >= set.RangeStarts[index] && codepoint <= set.RangeEnds[index])
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool MatchesXmlSchemaRegexCharacterProperties(XmlSchemaRegexCharacterSet set, int codepoint)
    {
        int index = 0;
        while (index < set.Properties.Count)
        {
            XmlSchemaRegexProperty property = set.Properties[index];
            bool matched = UnicodeProperties.MatchesXmlSchemaRegexProperty(property.Name, codepoint);
            if (property.Negated ? !matched : matched)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AreXmlSchemaRegexCodepointsEqual(int left, int right, bool ignoreCase)
    {
        if (left == right)
        {
            return true;
        }

        if (!ignoreCase)
        {
            return false;
        }

        return ReadXmlSchemaRegexLowercaseCodepoint(left) == ReadXmlSchemaRegexLowercaseCodepoint(right) || ReadXmlSchemaRegexUppercaseCodepoint(left) == ReadXmlSchemaRegexUppercaseCodepoint(right);
    }

    internal int ReadXmlSchemaRegexLowercaseCodepoint(int codepoint)
    {
        if (codepoint == 0x2126)
        {
            return 0x03C9;
        }

        if (codepoint == 0x212A)
        {
            return 0x006B;
        }

        if (codepoint == 0x212B)
        {
            return 0x00E5;
        }

        string converted = char.ConvertFromUtf32(codepoint).ToLowerInvariant();
        return CountXmlSchemaRegexScalars(converted) == 1 ? char.ConvertToUtf32(converted, 0) : codepoint;
    }

    internal int ReadXmlSchemaRegexUppercaseCodepoint(int codepoint)
    {
        string converted = char.ConvertFromUtf32(codepoint).ToUpperInvariant();
        return CountXmlSchemaRegexScalars(converted) == 1 ? char.ConvertToUtf32(converted, 0) : codepoint;
    }

    internal int ReadXmlSchemaRegexCompatibilityCaseVariant(int lowercaseCodepoint)
    {
        if (lowercaseCodepoint == 0x03C9)
        {
            return 0x2126;
        }

        if (lowercaseCodepoint == 0x006B)
        {
            return 0x212A;
        }

        if (lowercaseCodepoint == 0x00E5)
        {
            return 0x212B;
        }

        return -1;
    }

    internal int CountXmlSchemaRegexScalars(string value)
    {
        int count = 0;
        int offset = 0;
        while (offset < value.Length)
        {
            if (value[offset] >= 0xD800 && value[offset] <= 0xDBFF && offset + 1 < value.Length && value[offset + 1] >= 0xDC00 && value[offset + 1] <= 0xDFFF)
            {
                offset = offset + 2;
            }
            else
            {
                offset = offset + 1;
            }

            count = count + 1;
        }

        return count;
    }

    internal XmlSchemaRegexMatchState CloneXmlSchemaRegexState(XmlSchemaRegexMatchState state)
    {
        XmlSchemaRegexMatchState result = new XmlSchemaRegexMatchState(state.Position, state.CaptureStarts.Length - 1);
        state.CaptureStarts.CopyTo(result.CaptureStarts, 0);
        state.CaptureEnds.CopyTo(result.CaptureEnds, 0);
        return result;
    }

    internal List<XmlSchemaRegexMatchState> CreateXmlSchemaRegexSingleStateList(XmlSchemaRegexMatchState state)
    {
        List<XmlSchemaRegexMatchState> result = new List<XmlSchemaRegexMatchState>();
        result.Add(state);
        return result;
    }
}
