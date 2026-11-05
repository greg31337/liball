namespace svg;

/// <summary>Applies the Unicode Bidirectional Algorithm to normalized scalar text.</summary>
public class SvgBidiAnalyzer : ISvgBidiAnalyzer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgBidiAnalyzer()
    {
    }

    /// <summary>Analyzes text using automatic paragraph direction.</summary>
    /// <param name="text">The UTF-16 logical text.</param>
    /// <returns>The resolved bidirectional data and visual mappings.</returns>
    public SvgBidiResult AnalyzeWithText(string text)
    {
        return new SvgBidiAnalyzer().AnalyzeWithStringAndSvgBidiOptions(text, new SvgBidiOptions());
    }

    /// <summary>Analyzes text using explicit bounds and paragraph direction.</summary>
    /// <param name="text">The UTF-16 logical text.</param>
    /// <param name="options">The analysis bounds and direction.</param>
    /// <returns>The resolved bidirectional data and visual mappings.</returns>
    public SvgBidiResult AnalyzeWithStringAndSvgBidiOptions(string text, SvgBidiOptions options)
    {
        SvgBidiResult result = new SvgBidiResult();
        if (!new SvgBidiAnalyzer().ValidOptions(text, options)) return new SvgBidiAnalyzer().Fail(result, SvgBidiError.InvalidArgument, "Bidirectional text arguments or options are invalid.");
        SvgUnicodeTextOptions unicodeOptions = new SvgUnicodeTextOptions();
        unicodeOptions.MaxScalars = options.MaxScalars;
        unicodeOptions.MaxClusters = options.MaxScalars;
        unicodeOptions.MaxRuns = options.MaxScalars;
        unicodeOptions.CancellationToken = options.CancellationToken;
        SvgUnicodeTextResult unicode = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions(text, unicodeOptions);
        if (!unicode.Success)
        {
            if (unicode.Error == SvgUnicodeTextError.Cancelled) return new SvgBidiAnalyzer().Fail(result, SvgBidiError.Cancelled, unicode.Message);
            if (unicode.Error == SvgUnicodeTextError.LimitExceeded) return new SvgBidiAnalyzer().Fail(result, SvgBidiError.LimitExceeded, unicode.Message);
            return new SvgBidiAnalyzer().Fail(result, SvgBidiError.InvalidArgument, unicode.Message);
        }
        return new SvgBidiAnalyzer().AnalyzeWithSvgUnicodeTextAndSvgBidiOptions(unicode.UnicodeText, options);
    }

    /// <summary>Analyzes a previously normalized Unicode text model.</summary>
    /// <param name="text">The validated normalized Unicode text model.</param>
    /// <param name="options">The analysis bounds and direction.</param>
    /// <returns>The resolved bidirectional data and visual mappings.</returns>
    public SvgBidiResult AnalyzeWithSvgUnicodeTextAndSvgBidiOptions(SvgUnicodeText text, SvgBidiOptions options)
    {
        SvgBidiResult result = new SvgBidiResult();
        if (!new SvgBidiAnalyzer().ValidUnicodeText(text, options)) return new SvgBidiAnalyzer().Fail(result, SvgBidiError.InvalidArgument, "Bidirectional text model or options are invalid.");
        if (text.Scalars.Count > options.MaxScalars) return new SvgBidiAnalyzer().Fail(result, SvgBidiError.LimitExceeded, "Bidirectional analysis exceeds MaxScalars.");
        result.LogicalText = text.Text;
        int index = 0;
        while (index < text.Scalars.Count)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgBidiAnalyzer().Fail(result, SvgBidiError.Cancelled, "Bidirectional analysis was cancelled.");
            SvgBidiScalar item = new SvgBidiScalar();
            item.LogicalIndex = index;
            item.Value = text.Scalars[index].Value;
            item.DisplayValue = item.Value;
            item.OriginalClass = new SvgBidiAnalyzer().Classify(item.Value, text.Scalars[index].Category);
            item.ResolvedClass = item.OriginalClass;
            item.RemovedControl = new SvgBidiAnalyzer().FormattingControl(item.OriginalClass) || item.Value == 0x061C || item.Value == 0x200E || item.Value == 0x200F;
            result.Scalars.Add(item);
            result.LogicalToVisual.Add(-1);
            index = index + 1;
        }
        int[] matching = new SvgBidiAnalyzer().MatchIsolates(result.Scalars);
        index = 0;
        while (index < matching.Length)
        {
            result.Scalars[index].MatchingControlIndex = matching[index];
            index = index + 1;
        }
        int paragraphStart = 0;
        while (paragraphStart < result.Scalars.Count)
        {
            int paragraphEnd = paragraphStart;
            while (paragraphEnd + 1 < result.Scalars.Count && result.Scalars[paragraphEnd].OriginalClass != SvgBidiClass.ParagraphSeparator) paragraphEnd = paragraphEnd + 1;
            if (!new SvgBidiAnalyzer().ProcessParagraph(result, paragraphStart, paragraphEnd, matching, options)) return result;
            paragraphStart = paragraphEnd + 1;
        }
        if (result.Scalars.Count == 0)
        {
            result.Success = true;
            return result;
        }
        new SvgBidiAnalyzer().BuildVisualText(result, text, options);
        result.Success = true;
        return result;
    }
bool ProcessParagraph(SvgBidiResult result, int start, int end, int[] matching, SvgBidiOptions options)
    {
        if (options.CancellationToken.IsCancellationRequested)
        {
            new SvgBidiAnalyzer().Fail(result, SvgBidiError.Cancelled, "Bidirectional paragraph processing was cancelled.");
            return false;
        }
        int paragraphLevel = new SvgBidiAnalyzer().ParagraphLevel(result.Scalars, start, end, matching, options.Direction);
        SvgBidiParagraph paragraph = new SvgBidiParagraph();
        paragraph.ScalarStart = start;
        paragraph.ScalarCount = end - start + 1;
        paragraph.EmbeddingLevel = paragraphLevel;
        paragraph.VisualStart = result.VisualToLogical.Count;
        new SvgBidiAnalyzer().ExplicitLevels(result.Scalars, start, end, matching, paragraphLevel, options.MaxExplicitLevel);
        System.Collections.Generic.List<SvgBidiLevelRun> runs = new SvgBidiAnalyzer().BuildLevelRuns(result.Scalars, start, end, matching);
        bool[] processed = new bool[runs.Count];
        int runIndex = 0;
        while (runIndex < runs.Count)
        {
            if (!processed[runIndex] && runs[runIndex].Previous < 0)
            {
                System.Collections.Generic.List<int> sequence = new SvgBidiAnalyzer().CollectSequence(runs, runIndex, processed);
                new SvgBidiAnalyzer().ResolveSequence(result.Scalars, sequence, start, end, paragraphLevel);
            }
            runIndex = runIndex + 1;
        }
        runIndex = 0;
        while (runIndex < runs.Count)
        {
            if (!processed[runIndex])
            {
                System.Collections.Generic.List<int> sequence = new SvgBidiAnalyzer().CollectSequence(runs, runIndex, processed);
                new SvgBidiAnalyzer().ResolveSequence(result.Scalars, sequence, start, end, paragraphLevel);
            }
            runIndex = runIndex + 1;
        }
        new SvgBidiAnalyzer().ResetLineLevels(result.Scalars, start, end, paragraphLevel);
        System.Collections.Generic.List<int> visual = new SvgBidiAnalyzer().Reorder(result.Scalars, start, end);
        int visible = new SvgBidiAnalyzer().IndexVisual(result, visual, options);
        paragraph.VisualCount = visible;
        result.Paragraphs.Add(paragraph);
        return true;
    }
int IndexVisual(SvgBidiResult result, System.Collections.Generic.List<int> visual, SvgBidiOptions options)
    {
        int visible = 0;
        int index = 0;
        while (index < visual.Count)
        {
            int logical = visual[index];
            SvgBidiScalar scalar = result.Scalars[logical];
            if (!scalar.RemovedControl)
            {
                scalar.VisualIndex = result.VisualToLogical.Count;
                result.LogicalToVisual[logical] = scalar.VisualIndex;
                result.VisualToLogical.Add(logical);
                visible = visible + 1;
                if (options.ApplyMirroring && scalar.Level % 2 == 1)
                {
                    int mirrored = new SvgBidiAnalyzer().Mirror(scalar.Value);
                    if (mirrored != scalar.Value)
                    {
                        scalar.DisplayValue = mirrored;
                        scalar.Mirrored = true;
                    }
                }
            }
            index = index + 1;
        }
        return visible;
    }
void BuildVisualText(SvgBidiResult result, SvgUnicodeText text, SvgBidiOptions options)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder(text.Text.Length);
        int index = 0;
        while (index < result.VisualToLogical.Count)
        {
            new SvgBidiAnalyzer().AppendScalar(builder, result.Scalars[result.VisualToLogical[index]].DisplayValue);
            index = index + 1;
        }
        result.VisualText = builder.ToString();
    }
int[] MatchIsolates(System.Collections.Generic.List<SvgBidiScalar> scalars)
    {
        int[] matching = new int[scalars.Count];
        System.Collections.Generic.List<int> stack = new System.Collections.Generic.List<int>();
        int index = 0;
        while (index < matching.Length)
        {
            matching[index] = -1;
            SvgBidiClass bidiClass = scalars[index].OriginalClass;
            if (bidiClass == SvgBidiClass.ParagraphSeparator) stack.Clear();
            else if (new SvgBidiAnalyzer().IsIsolateInitiator(bidiClass)) stack.Add(index);
            else if (bidiClass == SvgBidiClass.PopDirectionalIsolate && stack.Count > 0)
            {
                int initiator = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                matching[initiator] = index;
                matching[index] = initiator;
            }
            index = index + 1;
        }
        return matching;
    }
int ParagraphLevel(System.Collections.Generic.List<SvgBidiScalar> scalars, int start, int end, int[] matching, SvgBidiDirection direction)
    {
        if (direction == SvgBidiDirection.LeftToRight) return 0;
        if (direction == SvgBidiDirection.RightToLeft) return 1;
        int index = start;
        while (index <= end)
        {
            SvgBidiClass bidiClass = scalars[index].OriginalClass;
            if (bidiClass == SvgBidiClass.LeftToRight) return 0;
            if (bidiClass == SvgBidiClass.RightToLeft || bidiClass == SvgBidiClass.ArabicLetter) return 1;
            if (new SvgBidiAnalyzer().IsIsolateInitiator(bidiClass))
            {
                if (matching[index] >= 0 && matching[index] <= end) index = matching[index];
                else return 0;
            }
            index = index + 1;
        }
        return 0;
    }
void ExplicitLevels(System.Collections.Generic.List<SvgBidiScalar> scalars, int start, int end, int[] matching, int paragraphLevel, int maximum)
    {
        System.Collections.Generic.List<SvgBidiStatus> stack = new System.Collections.Generic.List<SvgBidiStatus>();
        stack.Add(new SvgBidiStatus(paragraphLevel, -1, false));
        int overflowIsolates = 0;
        int overflowEmbeddings = 0;
        int validIsolates = 0;
        int index = start;
        while (index <= end)
        {
            SvgBidiScalar scalar = scalars[index];
            SvgBidiStatus current = stack[stack.Count - 1];
            SvgBidiClass bidiClass = scalar.OriginalClass;
            scalar.Level = current.Level;
            if (bidiClass == SvgBidiClass.RightToLeftEmbedding || bidiClass == SvgBidiClass.RightToLeftOverride)
            {
                int level = new SvgBidiAnalyzer().NextOdd(current.Level);
                if (level <= maximum && overflowIsolates == 0 && overflowEmbeddings == 0) stack.Add(new SvgBidiStatus(level, bidiClass == SvgBidiClass.RightToLeftOverride ? 1 : -1, false));
                else if (overflowIsolates == 0) overflowEmbeddings = overflowEmbeddings + 1;
            }
            else if (bidiClass == SvgBidiClass.LeftToRightEmbedding || bidiClass == SvgBidiClass.LeftToRightOverride)
            {
                int level = new SvgBidiAnalyzer().NextEven(current.Level);
                if (level <= maximum && overflowIsolates == 0 && overflowEmbeddings == 0) stack.Add(new SvgBidiStatus(level, bidiClass == SvgBidiClass.LeftToRightOverride ? 0 : -1, false));
                else if (overflowIsolates == 0) overflowEmbeddings = overflowEmbeddings + 1;
            }
            else if (new SvgBidiAnalyzer().IsIsolateInitiator(bidiClass))
            {
                if (current.Override == 0) scalar.ResolvedClass = SvgBidiClass.LeftToRight;
                else if (current.Override == 1) scalar.ResolvedClass = SvgBidiClass.RightToLeft;
                int direction = bidiClass == SvgBidiClass.FirstStrongIsolate ? new SvgBidiAnalyzer().IsolateDirection(scalars, index, end, matching) : bidiClass == SvgBidiClass.RightToLeftIsolate ? 1 : 0;
                int level = direction == 1 ? new SvgBidiAnalyzer().NextOdd(current.Level) : new SvgBidiAnalyzer().NextEven(current.Level);
                if (level <= maximum && overflowIsolates == 0 && overflowEmbeddings == 0)
                {
                    stack.Add(new SvgBidiStatus(level, -1, true));
                    validIsolates = validIsolates + 1;
                }
                else overflowIsolates = overflowIsolates + 1;
            }
            else if (bidiClass == SvgBidiClass.PopDirectionalIsolate)
            {
                if (overflowIsolates > 0) overflowIsolates = overflowIsolates - 1;
                else if (validIsolates > 0)
                {
                    overflowEmbeddings = 0;
                    int stackIndex = stack.Count - 1;
                    while (stackIndex > 0 && !stack[stackIndex].Isolate) stackIndex = stackIndex - 1;
                    while (stack.Count - 1 >= stackIndex) stack.RemoveAt(stack.Count - 1);
                    validIsolates = validIsolates - 1;
                    scalar.Level = stack[stack.Count - 1].Level;
                    if (stack[stack.Count - 1].Override == 0) scalar.ResolvedClass = SvgBidiClass.LeftToRight;
                    else if (stack[stack.Count - 1].Override == 1) scalar.ResolvedClass = SvgBidiClass.RightToLeft;
                }
            }
            else if (bidiClass == SvgBidiClass.PopDirectionalFormat)
            {
                if (overflowIsolates == 0)
                {
                    if (overflowEmbeddings > 0) overflowEmbeddings = overflowEmbeddings - 1;
                    else if (stack.Count > 1 && !stack[stack.Count - 1].Isolate) stack.RemoveAt(stack.Count - 1);
                }
            }
            else if (bidiClass == SvgBidiClass.ParagraphSeparator) scalar.Level = paragraphLevel;
            else if (bidiClass != SvgBidiClass.ParagraphSeparator && bidiClass != SvgBidiClass.BoundaryNeutral)
            {
                if (current.Override == 0) scalar.ResolvedClass = SvgBidiClass.LeftToRight;
                else if (current.Override == 1) scalar.ResolvedClass = SvgBidiClass.RightToLeft;
            }
            index = index + 1;
        }
        index = start;
        while (index <= end)
        {
            SvgBidiClass bidiClass = scalars[index].OriginalClass;
            if (new SvgBidiAnalyzer().RemovedByX9(bidiClass)) scalars[index].Level = paragraphLevel;
            index = index + 1;
        }
    }
int IsolateDirection(System.Collections.Generic.List<SvgBidiScalar> scalars, int initiator, int end, int[] matching)
    {
        int stop = matching[initiator] >= 0 ? matching[initiator] : end + 1;
        int index = initiator + 1;
        while (index < stop)
        {
            SvgBidiClass bidiClass = scalars[index].OriginalClass;
            if (bidiClass == SvgBidiClass.LeftToRight) return 0;
            if (bidiClass == SvgBidiClass.RightToLeft || bidiClass == SvgBidiClass.ArabicLetter) return 1;
            if (new SvgBidiAnalyzer().IsIsolateInitiator(bidiClass) && matching[index] >= 0 && matching[index] < stop) index = matching[index];
            index = index + 1;
        }
        return 0;
    }
System.Collections.Generic.List<SvgBidiLevelRun> BuildLevelRuns(System.Collections.Generic.List<SvgBidiScalar> scalars, int start, int end, int[] matching)
    {
        System.Collections.Generic.List<SvgBidiLevelRun> runs = new System.Collections.Generic.List<SvgBidiLevelRun>();
        int[] runForIndex = new int[scalars.Count];
        int fill = 0;
        while (fill < runForIndex.Length)
        {
            runForIndex[fill] = -1;
            fill = fill + 1;
        }
        SvgBidiLevelRun? current = null;
        int previousLevel = -1;
        int index = start;
        while (index <= end)
        {
            if (!new SvgBidiAnalyzer().RemovedByX9(scalars[index].OriginalClass))
            {
                if (current == null || scalars[index].Level != previousLevel)
                {
                    current = new SvgBidiLevelRun();
                    runs.Add(current);
                    previousLevel = scalars[index].Level;
                }
                current.Indices.Add(index);
                runForIndex[index] = runs.Count - 1;
            }
            index = index + 1;
        }
        int runIndex = 0;
        while (runIndex < runs.Count)
        {
            SvgBidiLevelRun run = runs[runIndex];
            int last = run.Indices[run.Indices.Count - 1];
            if (new SvgBidiAnalyzer().IsIsolateInitiator(scalars[last].OriginalClass) && matching[last] >= start && matching[last] <= end)
            {
                int next = runForIndex[matching[last]];
                if (next >= 0 && next != runIndex)
                {
                    run.Next = next;
                    runs[next].Previous = runIndex;
                }
            }
            runIndex = runIndex + 1;
        }
        return runs;
    }
System.Collections.Generic.List<int> CollectSequence(System.Collections.Generic.List<SvgBidiLevelRun> runs, int first, bool[] processed)
    {
        System.Collections.Generic.List<int> sequence = new System.Collections.Generic.List<int>();
        int runIndex = first;
        while (runIndex >= 0 && runIndex < runs.Count && !processed[runIndex])
        {
            processed[runIndex] = true;
            int index = 0;
            while (index < runs[runIndex].Indices.Count)
            {
                sequence.Add(runs[runIndex].Indices[index]);
                index = index + 1;
            }
            runIndex = runs[runIndex].Next;
        }
        return sequence;
    }
void ResolveSequence(System.Collections.Generic.List<SvgBidiScalar> scalars, System.Collections.Generic.List<int> sequence, int paragraphStart, int paragraphEnd, int paragraphLevel)
    {
        if (sequence.Count == 0) return;
        int sequenceLevel = scalars[sequence[0]].Level;
        int before = new SvgBidiAnalyzer().PreviousActive(scalars, sequence[0] - 1, paragraphStart);
        int after = new SvgBidiAnalyzer().NextActive(scalars, sequence[sequence.Count - 1] + 1, paragraphEnd);
        int beforeLevel = before >= 0 ? scalars[before].Level : paragraphLevel;
        int afterLevel = after >= 0 ? scalars[after].Level : paragraphLevel;
        SvgBidiClass sos = new SvgBidiAnalyzer().DirectionClass(sequenceLevel > beforeLevel ? sequenceLevel : beforeLevel);
        SvgBidiClass eos = new SvgBidiAnalyzer().DirectionClass(sequenceLevel > afterLevel ? sequenceLevel : afterLevel);
        new SvgBidiAnalyzer().ResolveWeak(scalars, sequence, sos);
        new SvgBidiAnalyzer().ResolveBrackets(scalars, sequence, sos, sequenceLevel);
        new SvgBidiAnalyzer().ResolveNeutrals(scalars, sequence, sos, eos, sequenceLevel);
        new SvgBidiAnalyzer().ResolveImplicit(scalars, sequence);
    }
void ResolveWeak(System.Collections.Generic.List<SvgBidiScalar> scalars, System.Collections.Generic.List<int> sequence, SvgBidiClass sos)
    {
        SvgBidiClass previous = sos;
        int offset = 0;
        while (offset < sequence.Count)
        {
            SvgBidiScalar scalar = scalars[sequence[offset]];
            if (scalar.ResolvedClass == SvgBidiClass.NonspacingMark) scalar.ResolvedClass = previous;
            if (new SvgBidiAnalyzer().IsIsolateInitiator(scalar.ResolvedClass) || scalar.ResolvedClass == SvgBidiClass.PopDirectionalIsolate) previous = SvgBidiClass.OtherNeutral;
            else previous = scalar.ResolvedClass;
            offset = offset + 1;
        }
        SvgBidiClass strong = sos;
        offset = 0;
        while (offset < sequence.Count)
        {
            SvgBidiScalar scalar = scalars[sequence[offset]];
            if (scalar.ResolvedClass == SvgBidiClass.LeftToRight || scalar.ResolvedClass == SvgBidiClass.RightToLeft || scalar.ResolvedClass == SvgBidiClass.ArabicLetter) strong = scalar.ResolvedClass;
            else if (scalar.ResolvedClass == SvgBidiClass.EuropeanNumber && strong == SvgBidiClass.ArabicLetter) scalar.ResolvedClass = SvgBidiClass.ArabicNumber;
            offset = offset + 1;
        }
        offset = 0;
        while (offset < sequence.Count)
        {
            if (scalars[sequence[offset]].ResolvedClass == SvgBidiClass.ArabicLetter) scalars[sequence[offset]].ResolvedClass = SvgBidiClass.RightToLeft;
            offset = offset + 1;
        }
        offset = 1;
        while (offset + 1 < sequence.Count)
        {
            SvgBidiClass before = scalars[sequence[offset - 1]].ResolvedClass;
            SvgBidiClass current = scalars[sequence[offset]].ResolvedClass;
            SvgBidiClass after = scalars[sequence[offset + 1]].ResolvedClass;
            if (current == SvgBidiClass.EuropeanSeparator && before == SvgBidiClass.EuropeanNumber && after == SvgBidiClass.EuropeanNumber) scalars[sequence[offset]].ResolvedClass = SvgBidiClass.EuropeanNumber;
            else if (current == SvgBidiClass.CommonSeparator && before == after && (before == SvgBidiClass.EuropeanNumber || before == SvgBidiClass.ArabicNumber)) scalars[sequence[offset]].ResolvedClass = before;
            offset = offset + 1;
        }
        offset = 0;
        while (offset < sequence.Count)
        {
            if (scalars[sequence[offset]].ResolvedClass == SvgBidiClass.EuropeanTerminator)
            {
                int first = offset;
                while (offset + 1 < sequence.Count && scalars[sequence[offset + 1]].ResolvedClass == SvgBidiClass.EuropeanTerminator) offset = offset + 1;
                bool adjacent = first > 0 && scalars[sequence[first - 1]].ResolvedClass == SvgBidiClass.EuropeanNumber;
                if (offset + 1 < sequence.Count && scalars[sequence[offset + 1]].ResolvedClass == SvgBidiClass.EuropeanNumber) adjacent = true;
                if (adjacent)
                {
                    int change = first;
                    while (change <= offset)
                    {
                        scalars[sequence[change]].ResolvedClass = SvgBidiClass.EuropeanNumber;
                        change = change + 1;
                    }
                }
            }
            offset = offset + 1;
        }
        offset = 0;
        while (offset < sequence.Count)
        {
            SvgBidiClass bidiClass = scalars[sequence[offset]].ResolvedClass;
            if (bidiClass == SvgBidiClass.EuropeanSeparator || bidiClass == SvgBidiClass.EuropeanTerminator || bidiClass == SvgBidiClass.CommonSeparator) scalars[sequence[offset]].ResolvedClass = SvgBidiClass.OtherNeutral;
            offset = offset + 1;
        }
        strong = sos;
        offset = 0;
        while (offset < sequence.Count)
        {
            SvgBidiScalar scalar = scalars[sequence[offset]];
            if (scalar.ResolvedClass == SvgBidiClass.LeftToRight || scalar.ResolvedClass == SvgBidiClass.RightToLeft) strong = scalar.ResolvedClass;
            else if (scalar.ResolvedClass == SvgBidiClass.EuropeanNumber && strong == SvgBidiClass.LeftToRight) scalar.ResolvedClass = SvgBidiClass.LeftToRight;
            offset = offset + 1;
        }
    }
void ResolveBrackets(System.Collections.Generic.List<SvgBidiScalar> scalars, System.Collections.Generic.List<int> sequence, SvgBidiClass sos, int level)
    {
        System.Collections.Generic.List<int> stackPositions = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> stackPairs = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<SvgBidiBracketPair> pairs = new System.Collections.Generic.List<SvgBidiBracketPair>();
        int offset = 0;
        while (offset < sequence.Count)
        {
            SvgBidiScalar scalar = scalars[sequence[offset]];
            if (scalar.ResolvedClass == SvgBidiClass.OtherNeutral)
            {
                SvgBidiBracket? bracket = new SvgBidiAnalyzer().Bracket(scalar.Value);
                if (bracket != null && bracket.Open && stackPositions.Count < 63)
                {
                    stackPositions.Add(offset);
                    stackPairs.Add(bracket.Pair);
                }
                else if (bracket != null && !bracket.Open)
                {
                    int stackIndex = stackPairs.Count - 1;
                    while (stackIndex >= 0 && stackPairs[stackIndex] != bracket.Pair) stackIndex = stackIndex - 1;
                    if (stackIndex >= 0)
                    {
                        pairs.Add(new SvgBidiBracketPair(stackPositions[stackIndex], offset));
                        while (stackPairs.Count > stackIndex)
                        {
                            stackPairs.RemoveAt(stackPairs.Count - 1);
                            stackPositions.RemoveAt(stackPositions.Count - 1);
                        }
                    }
                }
            }
            offset = offset + 1;
        }
        SvgBidiClass embedding = new SvgBidiAnalyzer().DirectionClass(level);
        int sortIndex = 1;
        while (sortIndex < pairs.Count)
        {
            SvgBidiBracketPair value = pairs[sortIndex];
            int insertion = sortIndex - 1;
            while (insertion >= 0 && pairs[insertion].Open > value.Open)
            {
                pairs[insertion + 1] = pairs[insertion];
                insertion = insertion - 1;
            }
            pairs[insertion + 1] = value;
            sortIndex = sortIndex + 1;
        }
        int pairIndex = 0;
        while (pairIndex < pairs.Count)
        {
            SvgBidiBracketPair pair = pairs[pairIndex];
            int openLogical = sequence[pair.Open];
            int closeLogical = sequence[pair.Close];
            scalars[openLogical].PairedBracketIndex = closeLogical;
            scalars[closeLogical].PairedBracketIndex = openLogical;
            bool embeddingFound = false;
            bool oppositeFound = false;
            int inner = pair.Open + 1;
            while (inner < pair.Close)
            {
                SvgBidiClass strong = new SvgBidiAnalyzer().StrongForNeutral(scalars[sequence[inner]].ResolvedClass);
                if (strong == embedding) embeddingFound = true;
                else if (strong == new SvgBidiAnalyzer().Opposite(embedding)) oppositeFound = true;
                inner = inner + 1;
            }
            SvgBidiClass resolved = SvgBidiClass.OtherNeutral;
            if (embeddingFound) resolved = embedding;
            else if (oppositeFound)
            {
                SvgBidiClass preceding = sos;
                int before = pair.Open - 1;
                while (before >= 0)
                {
                    SvgBidiClass strong = new SvgBidiAnalyzer().StrongForNeutral(scalars[sequence[before]].ResolvedClass);
                    if (strong == SvgBidiClass.LeftToRight || strong == SvgBidiClass.RightToLeft)
                    {
                        preceding = strong;
                        break;
                    }
                    before = before - 1;
                }
                resolved = preceding == new SvgBidiAnalyzer().Opposite(embedding) ? preceding : embedding;
            }
            if (resolved != SvgBidiClass.OtherNeutral)
            {
                scalars[openLogical].ResolvedClass = resolved;
                scalars[closeLogical].ResolvedClass = resolved;
                int following = pair.Open + 1;
                while (following < sequence.Count && scalars[sequence[following]].OriginalClass == SvgBidiClass.NonspacingMark)
                {
                    scalars[sequence[following]].ResolvedClass = resolved;
                    following = following + 1;
                }
                following = pair.Close + 1;
                while (following < sequence.Count && scalars[sequence[following]].OriginalClass == SvgBidiClass.NonspacingMark)
                {
                    scalars[sequence[following]].ResolvedClass = resolved;
                    following = following + 1;
                }
            }
            pairIndex = pairIndex + 1;
        }
    }
void ResolveNeutrals(System.Collections.Generic.List<SvgBidiScalar> scalars, System.Collections.Generic.List<int> sequence, SvgBidiClass sos, SvgBidiClass eos, int level)
    {
        int offset = 0;
        while (offset < sequence.Count)
        {
            if (new SvgBidiAnalyzer().Neutral(scalars[sequence[offset]].ResolvedClass))
            {
                int first = offset;
                while (offset + 1 < sequence.Count && new SvgBidiAnalyzer().Neutral(scalars[sequence[offset + 1]].ResolvedClass)) offset = offset + 1;
                SvgBidiClass before = first == 0 ? sos : new SvgBidiAnalyzer().StrongForNeutral(scalars[sequence[first - 1]].ResolvedClass);
                SvgBidiClass after = offset + 1 == sequence.Count ? eos : new SvgBidiAnalyzer().StrongForNeutral(scalars[sequence[offset + 1]].ResolvedClass);
                SvgBidiClass resolved = before == after ? before : new SvgBidiAnalyzer().DirectionClass(level);
                int change = first;
                while (change <= offset)
                {
                    scalars[sequence[change]].ResolvedClass = resolved;
                    change = change + 1;
                }
            }
            offset = offset + 1;
        }
    }
void ResolveImplicit(System.Collections.Generic.List<SvgBidiScalar> scalars, System.Collections.Generic.List<int> sequence)
    {
        int offset = 0;
        while (offset < sequence.Count)
        {
            SvgBidiScalar scalar = scalars[sequence[offset]];
            if (scalar.Level % 2 == 0)
            {
                if (scalar.ResolvedClass == SvgBidiClass.RightToLeft) scalar.Level = scalar.Level + 1;
                else if (scalar.ResolvedClass == SvgBidiClass.EuropeanNumber || scalar.ResolvedClass == SvgBidiClass.ArabicNumber) scalar.Level = scalar.Level + 2;
            }
            else if (scalar.ResolvedClass == SvgBidiClass.LeftToRight || scalar.ResolvedClass == SvgBidiClass.EuropeanNumber || scalar.ResolvedClass == SvgBidiClass.ArabicNumber) scalar.Level = scalar.Level + 1;
            offset = offset + 1;
        }
    }
void ResetLineLevels(System.Collections.Generic.List<SvgBidiScalar> scalars, int start, int end, int paragraphLevel)
    {
        int trailing = end;
        while (trailing >= start)
        {
            SvgBidiClass bidiClass = scalars[trailing].OriginalClass;
            if (bidiClass == SvgBidiClass.ParagraphSeparator || bidiClass == SvgBidiClass.SegmentSeparator || bidiClass == SvgBidiClass.Whitespace || new SvgBidiAnalyzer().FormattingControl(bidiClass))
            {
                scalars[trailing].Level = paragraphLevel;
                trailing = trailing - 1;
            }
            else break;
        }
        int index = start;
        while (index <= end)
        {
            SvgBidiClass bidiClass = scalars[index].OriginalClass;
            if (bidiClass == SvgBidiClass.ParagraphSeparator || bidiClass == SvgBidiClass.SegmentSeparator)
            {
                scalars[index].Level = paragraphLevel;
                int before = index - 1;
                while (before >= start && (scalars[before].OriginalClass == SvgBidiClass.Whitespace || new SvgBidiAnalyzer().FormattingControl(scalars[before].OriginalClass)))
                {
                    scalars[before].Level = paragraphLevel;
                    before = before - 1;
                }
            }
            index = index + 1;
        }
    }
System.Collections.Generic.List<int> Reorder(System.Collections.Generic.List<SvgBidiScalar> scalars, int start, int end)
    {
        System.Collections.Generic.List<int> visual = new System.Collections.Generic.List<int>();
        int separator = -1;
        if (end >= start && scalars[end].OriginalClass == SvgBidiClass.ParagraphSeparator)
        {
            separator = end;
            end = end - 1;
        }
        int index = start;
        int maximum = 0;
        int lowestOdd = 126;
        while (index <= end)
        {
            visual.Add(index);
            int level = scalars[index].Level;
            if (level > maximum) maximum = level;
            if (level % 2 == 1 && level < lowestOdd) lowestOdd = level;
            index = index + 1;
        }
        if (lowestOdd == 126)
        {
            if (separator >= 0) visual.Add(separator);
            return visual;
        }
        int levelToReverse = maximum;
        while (levelToReverse >= lowestOdd)
        {
            int offset = 0;
            while (offset < visual.Count)
            {
                if (scalars[visual[offset]].Level >= levelToReverse)
                {
                    int first = offset;
                    while (offset + 1 < visual.Count && scalars[visual[offset + 1]].Level >= levelToReverse) offset = offset + 1;
                    visual.Reverse(first, offset - first + 1);
                }
                offset = offset + 1;
            }
            levelToReverse = levelToReverse - 1;
        }
        if (separator >= 0) visual.Add(separator);
        return visual;
    }
SvgBidiClass Classify(int value, System.Globalization.UnicodeCategory category)
    {
        if (value == 0x200E) return SvgBidiClass.LeftToRight;
        if (value == 0x200F) return SvgBidiClass.RightToLeft;
        if (value == 0x061C) return SvgBidiClass.ArabicLetter;
        if (value == 0x202A) return SvgBidiClass.LeftToRightEmbedding;
        if (value == 0x202B) return SvgBidiClass.RightToLeftEmbedding;
        if (value == 0x202D) return SvgBidiClass.LeftToRightOverride;
        if (value == 0x202E) return SvgBidiClass.RightToLeftOverride;
        if (value == 0x202C) return SvgBidiClass.PopDirectionalFormat;
        if (value == 0x2066) return SvgBidiClass.LeftToRightIsolate;
        if (value == 0x2067) return SvgBidiClass.RightToLeftIsolate;
        if (value == 0x2068) return SvgBidiClass.FirstStrongIsolate;
        if (value == 0x2069) return SvgBidiClass.PopDirectionalIsolate;
        if (value == 0x000A || value == 0x000D || value == 0x001C || value == 0x001D || value == 0x001E || value == 0x0085 || value == 0x2029) return SvgBidiClass.ParagraphSeparator;
        if (value == 0x0009 || value == 0x000B || value == 0x001F) return SvgBidiClass.SegmentSeparator;
        if (value == 0x00AD || new SvgBidiAnalyzer().Between(value, 0x200B, 0x200D) || new SvgBidiAnalyzer().Between(value, 0x2060, 0x2065) || value == 0xFEFF) return SvgBidiClass.BoundaryNeutral;
        if (category == System.Globalization.UnicodeCategory.NonSpacingMark || category == System.Globalization.UnicodeCategory.SpacingCombiningMark || category == System.Globalization.UnicodeCategory.EnclosingMark) return SvgBidiClass.NonspacingMark;
        if (new SvgBidiAnalyzer().Between(value, 0x0030, 0x0039) || new SvgBidiAnalyzer().Between(value, 0xFF10, 0xFF19)) return SvgBidiClass.EuropeanNumber;
        if (new SvgBidiAnalyzer().Between(value, 0x0660, 0x0669) || new SvgBidiAnalyzer().Between(value, 0x06F0, 0x06F9)) return SvgBidiClass.ArabicNumber;
        if (value == 0x002B || value == 0x002D || value == 0x207A || value == 0x207B || value == 0x208A || value == 0x208B || value == 0x2212) return SvgBidiClass.EuropeanSeparator;
        if (value == 0x002C || value == 0x002E || value == 0x002F || value == 0x003A || value == 0x00A0 || value == 0x060C || value == 0x202F || value == 0x2044) return SvgBidiClass.CommonSeparator;
        if (value == 0x0023 || value == 0x0024 || value == 0x0025 || category == System.Globalization.UnicodeCategory.CurrencySymbol) return SvgBidiClass.EuropeanTerminator;
        if (new SvgBidiAnalyzer().Between(value, 0x0590, 0x05FF) || new SvgBidiAnalyzer().Between(value, 0x07C0, 0x085F) || new SvgBidiAnalyzer().Between(value, 0xFB1D, 0xFB4F) || new SvgBidiAnalyzer().Between(value, 0x10800, 0x10FFF) || new SvgBidiAnalyzer().Between(value, 0x1E800, 0x1EDFF)) return SvgBidiClass.RightToLeft;
        if (new SvgBidiAnalyzer().Between(value, 0x0600, 0x07BF) || new SvgBidiAnalyzer().Between(value, 0x0860, 0x08FF) || new SvgBidiAnalyzer().Between(value, 0xFB50, 0xFDFF) || new SvgBidiAnalyzer().Between(value, 0xFE70, 0xFEFF) || new SvgBidiAnalyzer().Between(value, 0x1EE00, 0x1EEFF)) return SvgBidiClass.ArabicLetter;
        if (category == System.Globalization.UnicodeCategory.UppercaseLetter || category == System.Globalization.UnicodeCategory.LowercaseLetter || category == System.Globalization.UnicodeCategory.TitlecaseLetter || category == System.Globalization.UnicodeCategory.ModifierLetter || category == System.Globalization.UnicodeCategory.OtherLetter) return SvgBidiClass.LeftToRight;
        if (category == System.Globalization.UnicodeCategory.SpaceSeparator || category == System.Globalization.UnicodeCategory.LineSeparator || value == 0x000C) return SvgBidiClass.Whitespace;
        if (category == System.Globalization.UnicodeCategory.Control || category == System.Globalization.UnicodeCategory.Format) return SvgBidiClass.BoundaryNeutral;
        return SvgBidiClass.OtherNeutral;
    }
SvgBidiBracket? Bracket(int value)
    {
        switch (value)
        {
            case 0x0028: return new SvgBidiBracket(0x0028, true);
            case 0x0029: return new SvgBidiBracket(0x0028, false);
            case 0x005B: return new SvgBidiBracket(0x005B, true);
            case 0x005D: return new SvgBidiBracket(0x005B, false);
            case 0x007B: return new SvgBidiBracket(0x007B, true);
            case 0x007D: return new SvgBidiBracket(0x007B, false);
            case 0x0F3A: return new SvgBidiBracket(0x0F3A, true);
            case 0x0F3B: return new SvgBidiBracket(0x0F3A, false);
            case 0x0F3C: return new SvgBidiBracket(0x0F3C, true);
            case 0x0F3D: return new SvgBidiBracket(0x0F3C, false);
            case 0x169B: return new SvgBidiBracket(0x169B, true);
            case 0x169C: return new SvgBidiBracket(0x169B, false);
            case 0x2045: return new SvgBidiBracket(0x2045, true);
            case 0x2046: return new SvgBidiBracket(0x2045, false);
            case 0x207D: return new SvgBidiBracket(0x207D, true);
            case 0x207E: return new SvgBidiBracket(0x207D, false);
            case 0x208D: return new SvgBidiBracket(0x208D, true);
            case 0x208E: return new SvgBidiBracket(0x208D, false);
            case 0x2308: return new SvgBidiBracket(0x2308, true);
            case 0x2309: return new SvgBidiBracket(0x2308, false);
            case 0x230A: return new SvgBidiBracket(0x230A, true);
            case 0x230B: return new SvgBidiBracket(0x230A, false);
            case 0x2329: return new SvgBidiBracket(0x2329, true);
            case 0x232A: return new SvgBidiBracket(0x2329, false);
            case 0x2768: return new SvgBidiBracket(0x2768, true);
            case 0x2769: return new SvgBidiBracket(0x2768, false);
            case 0x276A: return new SvgBidiBracket(0x276A, true);
            case 0x276B: return new SvgBidiBracket(0x276A, false);
            case 0x276C: return new SvgBidiBracket(0x276C, true);
            case 0x276D: return new SvgBidiBracket(0x276C, false);
            case 0x276E: return new SvgBidiBracket(0x276E, true);
            case 0x276F: return new SvgBidiBracket(0x276E, false);
            case 0x2770: return new SvgBidiBracket(0x2770, true);
            case 0x2771: return new SvgBidiBracket(0x2770, false);
            case 0x2772: return new SvgBidiBracket(0x2772, true);
            case 0x2773: return new SvgBidiBracket(0x2772, false);
            case 0x2774: return new SvgBidiBracket(0x2774, true);
            case 0x2775: return new SvgBidiBracket(0x2774, false);
            case 0x27E6: return new SvgBidiBracket(0x27E6, true);
            case 0x27E7: return new SvgBidiBracket(0x27E6, false);
            case 0x27E8: return new SvgBidiBracket(0x27E8, true);
            case 0x27E9: return new SvgBidiBracket(0x27E8, false);
            case 0x27EA: return new SvgBidiBracket(0x27EA, true);
            case 0x27EB: return new SvgBidiBracket(0x27EA, false);
            case 0x27EC: return new SvgBidiBracket(0x27EC, true);
            case 0x27ED: return new SvgBidiBracket(0x27EC, false);
            case 0x27EE: return new SvgBidiBracket(0x27EE, true);
            case 0x27EF: return new SvgBidiBracket(0x27EE, false);
            case 0x3008: return new SvgBidiBracket(0x3008, true);
            case 0x3009: return new SvgBidiBracket(0x3008, false);
            case 0x300A: return new SvgBidiBracket(0x300A, true);
            case 0x300B: return new SvgBidiBracket(0x300A, false);
            case 0x300C: return new SvgBidiBracket(0x300C, true);
            case 0x300D: return new SvgBidiBracket(0x300C, false);
            case 0x300E: return new SvgBidiBracket(0x300E, true);
            case 0x300F: return new SvgBidiBracket(0x300E, false);
            case 0x3010: return new SvgBidiBracket(0x3010, true);
            case 0x3011: return new SvgBidiBracket(0x3010, false);
            case 0x3014: return new SvgBidiBracket(0x3014, true);
            case 0x3015: return new SvgBidiBracket(0x3014, false);
            case 0x3016: return new SvgBidiBracket(0x3016, true);
            case 0x3017: return new SvgBidiBracket(0x3016, false);
            case 0x3018: return new SvgBidiBracket(0x3018, true);
            case 0x3019: return new SvgBidiBracket(0x3018, false);
            case 0x301A: return new SvgBidiBracket(0x301A, true);
            case 0x301B: return new SvgBidiBracket(0x301A, false);
            case 0xFE59: return new SvgBidiBracket(0xFE59, true);
            case 0xFE5A: return new SvgBidiBracket(0xFE59, false);
            case 0xFE5B: return new SvgBidiBracket(0xFE5B, true);
            case 0xFE5C: return new SvgBidiBracket(0xFE5B, false);
            case 0xFE5D: return new SvgBidiBracket(0xFE5D, true);
            case 0xFE5E: return new SvgBidiBracket(0xFE5D, false);
            case 0xFF08: return new SvgBidiBracket(0xFF08, true);
            case 0xFF09: return new SvgBidiBracket(0xFF08, false);
            case 0xFF3B: return new SvgBidiBracket(0xFF3B, true);
            case 0xFF3D: return new SvgBidiBracket(0xFF3B, false);
            case 0xFF5B: return new SvgBidiBracket(0xFF5B, true);
            case 0xFF5D: return new SvgBidiBracket(0xFF5B, false);
            case 0xFF5F: return new SvgBidiBracket(0xFF5F, true);
            case 0xFF60: return new SvgBidiBracket(0xFF5F, false);
            case 0xFF62: return new SvgBidiBracket(0xFF62, true);
            case 0xFF63: return new SvgBidiBracket(0xFF62, false);
            default: return null;
        }
    }
int Mirror(int value)
    {
        switch (value)
        {
            case 0x0028: return 0x0029;
            case 0x0029: return 0x0028;
            case 0x003C: return 0x003E;
            case 0x003E: return 0x003C;
            case 0x005B: return 0x005D;
            case 0x005D: return 0x005B;
            case 0x007B: return 0x007D;
            case 0x007D: return 0x007B;
            case 0x00AB: return 0x00BB;
            case 0x00BB: return 0x00AB;
            case 0x2039: return 0x203A;
            case 0x203A: return 0x2039;
            case 0x2045: return 0x2046;
            case 0x2046: return 0x2045;
            case 0x2208: return 0x220B;
            case 0x2209: return 0x220C;
            case 0x220A: return 0x220D;
            case 0x220B: return 0x2208;
            case 0x220C: return 0x2209;
            case 0x220D: return 0x220A;
            case 0x223C: return 0x223D;
            case 0x223D: return 0x223C;
            case 0x2264: return 0x2265;
            case 0x2265: return 0x2264;
            case 0x2266: return 0x2267;
            case 0x2267: return 0x2266;
            case 0x226A: return 0x226B;
            case 0x226B: return 0x226A;
            case 0x227A: return 0x227B;
            case 0x227B: return 0x227A;
            case 0x227C: return 0x227D;
            case 0x227D: return 0x227C;
            case 0x2308: return 0x2309;
            case 0x2309: return 0x2308;
            case 0x230A: return 0x230B;
            case 0x230B: return 0x230A;
            case 0x2329: return 0x232A;
            case 0x232A: return 0x2329;
            case 0x2768: return 0x2769;
            case 0x2769: return 0x2768;
            case 0x276A: return 0x276B;
            case 0x276B: return 0x276A;
            case 0x276C: return 0x276D;
            case 0x276D: return 0x276C;
            case 0x276E: return 0x276F;
            case 0x276F: return 0x276E;
            case 0x2770: return 0x2771;
            case 0x2771: return 0x2770;
            case 0x2772: return 0x2773;
            case 0x2773: return 0x2772;
            case 0x2774: return 0x2775;
            case 0x2775: return 0x2774;
            case 0x27E6: return 0x27E7;
            case 0x27E7: return 0x27E6;
            case 0x27E8: return 0x27E9;
            case 0x27E9: return 0x27E8;
            case 0x27EA: return 0x27EB;
            case 0x27EB: return 0x27EA;
            case 0x3008: return 0x3009;
            case 0x3009: return 0x3008;
            case 0x300A: return 0x300B;
            case 0x300B: return 0x300A;
            case 0x300C: return 0x300D;
            case 0x300D: return 0x300C;
            case 0x300E: return 0x300F;
            case 0x300F: return 0x300E;
            case 0x3010: return 0x3011;
            case 0x3011: return 0x3010;
            case 0x3014: return 0x3015;
            case 0x3015: return 0x3014;
            case 0x3016: return 0x3017;
            case 0x3017: return 0x3016;
            case 0x3018: return 0x3019;
            case 0x3019: return 0x3018;
            case 0x301A: return 0x301B;
            case 0x301B: return 0x301A;
            case 0xFF08: return 0xFF09;
            case 0xFF09: return 0xFF08;
            case 0xFF1C: return 0xFF1E;
            case 0xFF1E: return 0xFF1C;
            case 0xFF3B: return 0xFF3D;
            case 0xFF3D: return 0xFF3B;
            case 0xFF5B: return 0xFF5D;
            case 0xFF5D: return 0xFF5B;
            default: return value;
        }
    }
bool Neutral(SvgBidiClass bidiClass)
    {
        return bidiClass == SvgBidiClass.ParagraphSeparator || bidiClass == SvgBidiClass.SegmentSeparator || bidiClass == SvgBidiClass.Whitespace || bidiClass == SvgBidiClass.OtherNeutral || bidiClass == SvgBidiClass.LeftToRightIsolate || bidiClass == SvgBidiClass.RightToLeftIsolate || bidiClass == SvgBidiClass.FirstStrongIsolate || bidiClass == SvgBidiClass.PopDirectionalIsolate;
    }
SvgBidiClass StrongForNeutral(SvgBidiClass bidiClass)
    {
        if (bidiClass == SvgBidiClass.EuropeanNumber || bidiClass == SvgBidiClass.ArabicNumber || bidiClass == SvgBidiClass.ArabicLetter) return SvgBidiClass.RightToLeft;
        return bidiClass;
    }
SvgBidiClass DirectionClass(int level)
    {
        return level % 2 == 0 ? SvgBidiClass.LeftToRight : SvgBidiClass.RightToLeft;
    }
SvgBidiClass Opposite(SvgBidiClass bidiClass)
    {
        return bidiClass == SvgBidiClass.LeftToRight ? SvgBidiClass.RightToLeft : SvgBidiClass.LeftToRight;
    }
int PreviousActive(System.Collections.Generic.List<SvgBidiScalar> scalars, int index, int minimum)
    {
        while (index >= minimum)
        {
            if (!new SvgBidiAnalyzer().RemovedByX9(scalars[index].OriginalClass)) return index;
            index = index - 1;
        }
        return -1;
    }
int NextActive(System.Collections.Generic.List<SvgBidiScalar> scalars, int index, int maximum)
    {
        while (index <= maximum)
        {
            if (!new SvgBidiAnalyzer().RemovedByX9(scalars[index].OriginalClass)) return index;
            index = index + 1;
        }
        return -1;
    }
bool IsIsolateInitiator(SvgBidiClass bidiClass)
    {
        return bidiClass == SvgBidiClass.LeftToRightIsolate || bidiClass == SvgBidiClass.RightToLeftIsolate || bidiClass == SvgBidiClass.FirstStrongIsolate;
    }
bool RemovedByX9(SvgBidiClass bidiClass)
    {
        return bidiClass == SvgBidiClass.LeftToRightEmbedding || bidiClass == SvgBidiClass.RightToLeftEmbedding || bidiClass == SvgBidiClass.LeftToRightOverride || bidiClass == SvgBidiClass.RightToLeftOverride || bidiClass == SvgBidiClass.PopDirectionalFormat || bidiClass == SvgBidiClass.BoundaryNeutral;
    }
bool FormattingControl(SvgBidiClass bidiClass)
    {
        return new SvgBidiAnalyzer().RemovedByX9(bidiClass) || new SvgBidiAnalyzer().IsIsolateInitiator(bidiClass) || bidiClass == SvgBidiClass.PopDirectionalIsolate;
    }
int NextOdd(int level)
    {
        return level % 2 == 0 ? level + 1 : level + 2;
    }
int NextEven(int level)
    {
        return level % 2 == 0 ? level + 2 : level + 1;
    }
bool Between(int value, int minimum, int maximum)
    {
        return value >= minimum && value <= maximum;
    }
void AppendScalar(System.Text.StringBuilder builder, int value)
    {
        if (value <= 0xFFFF) builder.Append((char)value);
        else
        {
            int adjusted = value - 0x10000;
            builder.Append((char)(0xD800 + (adjusted >> 10)));
            builder.Append((char)(0xDC00 + (adjusted & 0x3FF)));
        }
    }
bool ValidOptions(string text, SvgBidiOptions options)
    {
        return text != null && options != null && options.MaxScalars > 0 && options.MaxExplicitLevel >= 0 && options.MaxExplicitLevel <= 125;
    }
bool ValidUnicodeText(SvgUnicodeText? text, SvgBidiOptions? options)
    {
        if (text == null || text.Text == null || text.Scalars == null || options == null || options.MaxScalars < 1 || options.MaxExplicitLevel < 0 || options.MaxExplicitLevel > 125) return false;
        int index = 0;
        while (index < text.Scalars.Count)
        {
            if (text.Scalars[index] == null || text.Scalars[index].Index != index) return false;
            index = index + 1;
        }
        return true;
    }
SvgBidiResult Fail(SvgBidiResult result, SvgBidiError error, string message)
    {
        if (result.Error == SvgBidiError.None) result.Error = error;
        result.Message = message;
        return result;
    }
}
