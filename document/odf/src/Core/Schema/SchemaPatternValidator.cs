using System.Collections.Generic;

namespace odf;

internal class SchemaPatternValidator
{
    SchemaValidationLimits Limits;
    SchemaRuleSet Rules = new SchemaRuleSet();
    SchemaRuleIndex Index = new SchemaRuleIndex(new SchemaRuleSet());
    int Operations;
    int GreatestDepth;
    int ElementDepth;
    bool LimitExceeded;
    bool InvalidDatatype;

    internal SchemaPatternValidator(SchemaValidationLimits limits)
    {
        if (limits == null
            || limits.MaximumPatternDepth <= 0
            || limits.MaximumOperations <= 0
            || limits.MaximumStates <= 0
            || limits.MaximumElementDepth <= 0
            || limits.MaximumDatatypeLength <= 0)
        {
            throw new OdfValidationException(
                "Positive schema validation limits are required.");
        }
        Limits = limits;
    }

    internal SchemaValidationResult ValidateNode(
        OdfNode node,
        SchemaRuleSet rules)
    {
        if (node == null || rules == null)
        {
            throw new OdfValidationException(
                "A node and schema rule set are required.");
        }
        Operations = 0;
        GreatestDepth = 0;
        ElementDepth = 0;
        LimitExceeded = false;
        InvalidDatatype = false;
        Rules = rules;
        Index = new SchemaRuleIndex(rules);

        OdfNode context = new OdfNode();
        context.Kind = OdfNodeKind.Element;
        context.Children = [node];
        SchemaMatchState initial = NewElementState(context);
        List<SchemaMatchState> matches = MatchStartRules(context, initial, 1);
        bool valid = false;
        foreach (SchemaMatchState match in matches)
        {
            if (ElementStateIsComplete(context, match))
            {
                valid = true;
            }
        }

        SchemaValidationResult result = new SchemaValidationResult();
        result.IsValid = valid && !LimitExceeded;
        result.LimitExceeded = LimitExceeded;
        result.InvalidDatatype = InvalidDatatype && !valid;
        result.Operations = Operations;
        result.MaximumDepth = GreatestDepth;
        return result;
    }

    List<SchemaMatchState> MatchStartRules(
        OdfNode context,
        SchemaMatchState state,
        int depth)
    {
        SchemaRule[] starts = Index.FindStartRules();
        List<SchemaMatchState> result = NewStateList();
        if (starts.Length == 0)
        {
            return result;
        }
        bool interleave = false;
        foreach (SchemaRule rule in starts)
        {
            if (rule.Combine == "interleave")
            {
                interleave = true;
            }
        }
        if (interleave)
        {
            int[] patterns = new int[starts.Length];
            int index = 0;
            while (index < starts.Length)
            {
                patterns[index] = starts[index].PatternIndex;
                index = index + 1;
            }
            return MatchInterleavePatterns(context, state, patterns, depth + 1);
        }
        foreach (SchemaRule rule in starts)
        {
            AddStateRange(
                result,
                MatchNodePattern(context, state, rule.PatternIndex, depth + 1));
        }
        return result;
    }

    List<SchemaMatchState> MatchNodePattern(
        OdfNode context,
        SchemaMatchState state,
        int patternIndex,
        int depth)
    {
        List<SchemaMatchState> none = NewStateList();
        if (!EnterOperation(depth) || !PatternIndexIsValid(patternIndex))
        {
            return none;
        }
        SchemaPattern pattern = Rules.Patterns[patternIndex];
        if (pattern.Kind == SchemaPatternKind.Empty)
        {
            AddUniqueState(none, state);
            return none;
        }
        if (pattern.Kind == SchemaPatternKind.NotAllowed
            || pattern.Kind == SchemaPatternKind.Param
            || pattern.Kind == SchemaPatternKind.Unknown
            || pattern.Kind == SchemaPatternKind.ExternalRef
            || pattern.Kind == SchemaPatternKind.Include)
        {
            return none;
        }
        if (pattern.Kind == SchemaPatternKind.Choice)
        {
            return MatchChoicePattern(context, state, pattern, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Group)
        {
            return MatchGroupPattern(context, state, pattern, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Interleave)
        {
            return MatchInterleavePattern(context, state, pattern, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Optional)
        {
            AddUniqueState(none, state);
            AddStateRange(
                none,
                MatchPatternChildren(context, state, pattern, depth + 1));
            return none;
        }
        if (pattern.Kind == SchemaPatternKind.ZeroOrMore)
        {
            return MatchRepeatedPattern(context, state, pattern, false, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.OneOrMore)
        {
            return MatchRepeatedPattern(context, state, pattern, true, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Mixed)
        {
            return MatchMixedPattern(context, state, pattern, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Element)
        {
            return MatchElementPattern(context, state, pattern, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Attribute)
        {
            return MatchAttributePattern(context, state, pattern, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Text)
        {
            return MatchTextPattern(context, state);
        }
        if (pattern.Kind == SchemaPatternKind.Data
            || pattern.Kind == SchemaPatternKind.Value
            || pattern.Kind == SchemaPatternKind.List)
        {
            return MatchTextValuePattern(
                context,
                state,
                patternIndex,
                depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Ref
            || pattern.Kind == SchemaPatternKind.ParentRef)
        {
            return MatchReferencePattern(context, state, pattern, depth + 1);
        }
        return none;
    }

    List<SchemaMatchState> MatchChoicePattern(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        int depth)
    {
        List<SchemaMatchState> result = NewStateList();
        int childNumber = 0;
        while (childNumber < pattern.ChildCount && !LimitExceeded)
        {
            int childIndex = ChildPatternIndex(pattern, childNumber);
            AddStateRange(
                result,
                MatchNodePattern(context, state, childIndex, depth + 1));
            childNumber = childNumber + 1;
        }
        return result;
    }

    List<SchemaMatchState> MatchGroupPattern(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        int depth)
    {
        return MatchPatternChildren(context, state, pattern, depth + 1);
    }

    List<SchemaMatchState> MatchPatternChildren(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        int depth)
    {
        List<SchemaMatchState> current = NewStateList();
        AddUniqueState(current, state);
        int childNumber = 0;
        while (childNumber < pattern.ChildCount
            && current.Count > 0
            && !LimitExceeded)
        {
            int childIndex = ChildPatternIndex(pattern, childNumber);
            List<SchemaMatchState> next = NewStateList();
            foreach (SchemaMatchState candidate in current)
            {
                AddStateRange(
                    next,
                    MatchNodePattern(
                        context,
                        candidate,
                        childIndex,
                        depth + 1));
            }
            current = next;
            childNumber = childNumber + 1;
        }
        return current;
    }

    List<SchemaMatchState> MatchInterleavePattern(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        int depth)
    {
        List<int> attributes = new List<int>();
        List<int> content = new List<int>();
        int childNumber = 0;
        while (childNumber < pattern.ChildCount)
        {
            int childIndex = ChildPatternIndex(pattern, childNumber);
            if (PatternUsesOnlyAttributes(childIndex, depth + 1))
            {
                attributes.Add(childIndex);
            }
            else
            {
                content.Add(childIndex);
            }
            childNumber = childNumber + 1;
        }

        List<SchemaMatchState> current = NewStateList();
        AddUniqueState(current, state);
        foreach (int attributePattern in attributes)
        {
            List<SchemaMatchState> next = NewStateList();
            foreach (SchemaMatchState candidate in current)
            {
                AddStateRange(
                    next,
                    MatchNodePattern(
                        context,
                        candidate,
                        attributePattern,
                        depth + 1));
            }
            current = next;
        }
        if (content.Count == 0 || current.Count == 0)
        {
            return current;
        }

        List<SchemaMatchState> result = NewStateList();
        int[] contentPatterns = content.ToArray();
        foreach (SchemaMatchState candidate in current)
        {
            AddStateRange(
                result,
                MatchInterleavePatterns(
                    context,
                    candidate,
                    contentPatterns,
                    depth + 1));
        }
        return result;
    }

    List<SchemaMatchState> MatchInterleavePatterns(
        OdfNode context,
        SchemaMatchState state,
        int[] patterns,
        int depth)
    {
        int[][] branches = ExpandInterleaveBranches(patterns, depth + 1);
        int[] positions = new int[branches.Length];
        return ExploreInterleave(
            context,
            state,
            branches,
            positions,
            depth + 1);
    }

    List<SchemaMatchState> ExploreInterleave(
        OdfNode context,
        SchemaMatchState state,
        int[][] branches,
        int[] positions,
        int depth)
    {
        List<SchemaMatchState> result = NewStateList();
        if (!EnterOperation(depth))
        {
            return result;
        }
        bool complete = true;
        int branch = 0;
        while (branch < branches.Length)
        {
            if (positions[branch] < branches[branch].Length)
            {
                complete = false;
            }
            branch = branch + 1;
        }
        if (complete)
        {
            AddUniqueState(result, state);
            return result;
        }
        branch = 0;
        while (branch < branches.Length && !LimitExceeded)
        {
            if (positions[branch] < branches[branch].Length)
            {
                int[] nextPositions = CopyIntegers(positions);
                int patternIndex = branches[branch][positions[branch]];
                nextPositions[branch] = nextPositions[branch] + 1;
                List<SchemaMatchState> matches = MatchNodePattern(
                    context,
                    state,
                    patternIndex,
                    depth + 1);
                foreach (SchemaMatchState match in matches)
                {
                    AddStateRange(
                        result,
                        ExploreInterleave(
                            context,
                            match,
                            branches,
                            nextPositions,
                            depth + 1));
                }
            }
            branch = branch + 1;
        }
        return result;
    }

    int[][] ExpandInterleaveBranches(int[] patterns, int depth)
    {
        int[][] result = new int[patterns.Length][];
        int index = 0;
        while (index < patterns.Length)
        {
            List<int> branch = new List<int>();
            AppendInterleaveUnits(branch, patterns[index], depth + 1);
            result[index] = branch.ToArray();
            index = index + 1;
        }
        return result;
    }

    void AppendInterleaveUnits(
        List<int> branch,
        int patternIndex,
        int depth)
    {
        if (!EnterOperation(depth) || !PatternIndexIsValid(patternIndex))
        {
            return;
        }
        SchemaPattern pattern = Rules.Patterns[patternIndex];
        if (pattern.Kind != SchemaPatternKind.Group)
        {
            branch.Add(patternIndex);
            return;
        }
        int childNumber = 0;
        while (childNumber < pattern.ChildCount)
        {
            AppendInterleaveUnits(
                branch,
                ChildPatternIndex(pattern, childNumber),
                depth + 1);
            childNumber = childNumber + 1;
        }
    }

    List<SchemaMatchState> MatchRepeatedPattern(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        bool requireOne,
        int depth)
    {
        List<SchemaMatchState> accepted = NewStateList();
        List<SchemaMatchState> frontier = NewStateList();
        AddUniqueState(frontier, state);
        if (!requireOne)
        {
            AddUniqueState(accepted, state);
        }
        while (frontier.Count > 0 && !LimitExceeded)
        {
            List<SchemaMatchState> next = NewStateList();
            foreach (SchemaMatchState candidate in frontier)
            {
                List<SchemaMatchState> matches = MatchPatternChildren(
                    context,
                    candidate,
                    pattern,
                    depth + 1);
                foreach (SchemaMatchState match in matches)
                {
                    if (!StatesAreEqual(candidate, match)
                        && !ContainsState(accepted, match))
                    {
                        AddUniqueState(next, match);
                    }
                    AddUniqueState(accepted, match);
                }
            }
            frontier = next;
        }
        return accepted;
    }

    List<SchemaMatchState> MatchMixedPattern(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        int depth)
    {
        bool previous = state.AllowMixedText;
        SchemaMatchState mixed = CopyElementState(state);
        mixed.AllowMixedText = true;
        mixed.ChildIndex = NextRelevantChild(context, mixed);
        List<SchemaMatchState> matches = MatchPatternChildren(
            context,
            mixed,
            pattern,
            depth + 1);
        List<SchemaMatchState> result = NewStateList();
        foreach (SchemaMatchState match in matches)
        {
            SchemaMatchState restored = CopyElementState(match);
            restored.ChildIndex = NextRelevantChild(context, restored);
            restored.AllowMixedText = previous;
            AddUniqueState(result, restored);
        }
        return result;
    }

    List<SchemaMatchState> MatchElementPattern(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        int depth)
    {
        List<SchemaMatchState> result = NewStateList();
        SchemaMatchState normalized = CopyElementState(state);
        normalized.ChildIndex = NextRelevantChild(context, normalized);
        if (normalized.ChildIndex >= context.Children.Length)
        {
            return result;
        }
        OdfNode child = context.Children[normalized.ChildIndex];
        if (child.Kind != OdfNodeKind.Element
            || child.Name == null
            || !ElementNameMatches(pattern, child.Name, depth + 1))
        {
            return result;
        }
        if (!ValidateElementContent(child, pattern, depth + 1))
        {
            return result;
        }
        SchemaMatchState consumed = CopyElementState(normalized);
        consumed.ChildIndex = normalized.ChildIndex + 1;
        AddUniqueState(result, consumed);
        return result;
    }

    List<SchemaMatchState> MatchAttributePattern(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        int depth)
    {
        List<SchemaMatchState> result = NewStateList();
        int index = 0;
        while (index < context.Attributes.Length && !LimitExceeded)
        {
            OdfAttribute attribute = context.Attributes[index];
            if (!state.UsedAttributes[index]
                && AttributeNameMatches(pattern, attribute.Name, depth + 1)
                && AttributeValueMatches(pattern, attribute.Value, depth + 1))
            {
                SchemaMatchState consumed = CopyElementState(state);
                consumed.UsedAttributes[index] = true;
                AddUniqueState(result, consumed);
            }
            index = index + 1;
        }
        return result;
    }

    List<SchemaMatchState> MatchTextPattern(
        OdfNode context,
        SchemaMatchState state)
    {
        List<SchemaMatchState> result = NewStateList();
        AddUniqueState(result, state);
        SchemaMatchState normalized = CopyElementState(state);
        normalized.ChildIndex = NextRelevantChild(context, normalized);
        if (normalized.ChildIndex < context.Children.Length
            && context.Children[normalized.ChildIndex].Kind == OdfNodeKind.Text)
        {
            normalized.ChildIndex = normalized.ChildIndex + 1;
            AddUniqueState(result, normalized);
        }
        return result;
    }

    List<SchemaMatchState> MatchTextValuePattern(
        OdfNode context,
        SchemaMatchState state,
        int patternIndex,
        int depth)
    {
        List<SchemaMatchState> result = NewStateList();
        SchemaMatchState normalized = CopyElementState(state);
        normalized.ChildIndex = NextRelevantChild(context, normalized);
        AddTextValueCandidate(
            result,
            normalized,
            "",
            false,
            patternIndex,
            depth + 1);
        if (normalized.ChildIndex < context.Children.Length
            && context.Children[normalized.ChildIndex].Kind == OdfNodeKind.Text)
        {
            AddTextValueCandidate(
                result,
                normalized,
                context.Children[normalized.ChildIndex].Value,
                true,
                patternIndex,
                depth + 1);
        }
        return result;
    }

    void AddTextValueCandidate(
        List<SchemaMatchState> result,
        SchemaMatchState state,
        string value,
        bool consumesText,
        int patternIndex,
        int depth)
    {
        string[] values = [value];
        List<SchemaScalarState> scalar = MatchScalarPattern(
            values,
            0,
            patternIndex,
            depth + 1);
        foreach (SchemaScalarState match in scalar)
        {
            if (match.Position == values.Length)
            {
                SchemaMatchState consumed = CopyElementState(state);
                if (consumesText)
                {
                    consumed.ChildIndex = consumed.ChildIndex + 1;
                }
                AddUniqueState(result, consumed);
            }
        }
    }

    List<SchemaMatchState> MatchReferencePattern(
        OdfNode context,
        SchemaMatchState state,
        SchemaPattern pattern,
        int depth)
    {
        SchemaRule[] definitions = Index.FindDefinitionRules(pattern.Name);
        List<SchemaMatchState> result = NewStateList();
        if (definitions.Length == 0)
        {
            return result;
        }
        bool interleave = false;
        foreach (SchemaRule definition in definitions)
        {
            if (definition.Combine == "interleave")
            {
                interleave = true;
            }
        }
        if (interleave)
        {
            int[] patterns = new int[definitions.Length];
            int index = 0;
            while (index < definitions.Length)
            {
                patterns[index] = definitions[index].PatternIndex;
                index = index + 1;
            }
            return MatchInterleavePatterns(context, state, patterns, depth + 1);
        }
        foreach (SchemaRule definition in definitions)
        {
            AddStateRange(
                result,
                MatchNodePattern(
                    context,
                    state,
                    definition.PatternIndex,
                    depth + 1));
        }
        return result;
    }

    bool ValidateElementContent(
        OdfNode element,
        SchemaPattern pattern,
        int depth)
    {
        ElementDepth = ElementDepth + 1;
        if (ElementDepth > Limits.MaximumElementDepth)
        {
            LimitExceeded = true;
            ElementDepth = ElementDepth - 1;
            return false;
        }
        SchemaMatchState initial = NewElementState(element);
        List<SchemaMatchState> current = NewStateList();
        AddUniqueState(current, initial);
        int childNumber = 0;
        while (childNumber < pattern.ChildCount
            && current.Count > 0
            && !LimitExceeded)
        {
            int childIndex = ChildPatternIndex(pattern, childNumber);
            if (!IsDirectNameClass(childIndex))
            {
                List<SchemaMatchState> next = NewStateList();
                foreach (SchemaMatchState candidate in current)
                {
                    AddStateRange(
                        next,
                        MatchNodePattern(
                            element,
                            candidate,
                            childIndex,
                            depth + 1));
                }
                current = next;
            }
            childNumber = childNumber + 1;
        }
        bool valid = false;
        foreach (SchemaMatchState match in current)
        {
            if (ElementStateIsComplete(element, match))
            {
                valid = true;
            }
        }
        ElementDepth = ElementDepth - 1;
        return valid;
    }

    bool AttributeValueMatches(
        SchemaPattern pattern,
        string value,
        int depth)
    {
        List<SchemaScalarState> current = NewScalarList();
        AddUniqueScalar(current, 0);
        int contentCount = 0;
        int childNumber = 0;
        string[] values = [value];
        while (childNumber < pattern.ChildCount && current.Count > 0)
        {
            int childIndex = ChildPatternIndex(pattern, childNumber);
            if (!IsDirectNameClass(childIndex))
            {
                contentCount = contentCount + 1;
                List<SchemaScalarState> next = NewScalarList();
                foreach (SchemaScalarState candidate in current)
                {
                    AddScalarRange(
                        next,
                        MatchScalarPattern(
                            values,
                            candidate.Position,
                            childIndex,
                            depth + 1));
                }
                current = next;
            }
            childNumber = childNumber + 1;
        }
        if (contentCount == 0)
        {
            return true;
        }
        foreach (SchemaScalarState match in current)
        {
            if (match.Position == values.Length)
            {
                return true;
            }
        }
        InvalidDatatype = true;
        return false;
    }

    List<SchemaScalarState> MatchScalarPattern(
        string[] values,
        int position,
        int patternIndex,
        int depth)
    {
        List<SchemaScalarState> result = NewScalarList();
        if (!EnterOperation(depth) || !PatternIndexIsValid(patternIndex))
        {
            return result;
        }
        SchemaPattern pattern = Rules.Patterns[patternIndex];
        if (pattern.Kind == SchemaPatternKind.Empty)
        {
            AddUniqueScalar(result, position);
            return result;
        }
        if (pattern.Kind == SchemaPatternKind.Text)
        {
            AddUniqueScalar(result, position);
            if (position < values.Length)
            {
                AddUniqueScalar(result, position + 1);
            }
            return result;
        }
        if (pattern.Kind == SchemaPatternKind.Data)
        {
            if (position < values.Length
                && DataValueMatches(values[position], pattern, depth + 1))
            {
                AddUniqueScalar(result, position + 1);
            }
            return result;
        }
        if (pattern.Kind == SchemaPatternKind.Value)
        {
            if (position < values.Length
                && SchemaDatatypeValidator.IsValueMatch(
                    values[position],
                    pattern))
            {
                AddUniqueScalar(result, position + 1);
            }
            else
            {
                InvalidDatatype = true;
            }
            return result;
        }
        if (pattern.Kind == SchemaPatternKind.List)
        {
            return MatchScalarList(values, position, pattern, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Choice)
        {
            int choice = 0;
            while (choice < pattern.ChildCount)
            {
                AddScalarRange(
                    result,
                    MatchScalarPattern(
                        values,
                        position,
                        ChildPatternIndex(pattern, choice),
                        depth + 1));
                choice = choice + 1;
            }
            return result;
        }
        if (pattern.Kind == SchemaPatternKind.Group
            || pattern.Kind == SchemaPatternKind.Interleave)
        {
            return MatchScalarChildren(values, position, pattern, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Optional)
        {
            AddUniqueScalar(result, position);
            AddScalarRange(
                result,
                MatchScalarChildren(values, position, pattern, depth + 1));
            return result;
        }
        if (pattern.Kind == SchemaPatternKind.ZeroOrMore
            || pattern.Kind == SchemaPatternKind.OneOrMore)
        {
            return MatchScalarRepetition(
                values,
                position,
                pattern,
                pattern.Kind == SchemaPatternKind.OneOrMore,
                depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Ref
            || pattern.Kind == SchemaPatternKind.ParentRef)
        {
            return MatchScalarReference(values, position, pattern, depth + 1);
        }
        return result;
    }

    List<SchemaScalarState> MatchScalarChildren(
        string[] values,
        int position,
        SchemaPattern pattern,
        int depth)
    {
        List<SchemaScalarState> current = NewScalarList();
        AddUniqueScalar(current, position);
        int childNumber = 0;
        while (childNumber < pattern.ChildCount && current.Count > 0)
        {
            int childIndex = ChildPatternIndex(pattern, childNumber);
            SchemaPattern child = Rules.Patterns[childIndex];
            if (child.Kind != SchemaPatternKind.Param
                && child.Kind != SchemaPatternKind.Except)
            {
                List<SchemaScalarState> next = NewScalarList();
                foreach (SchemaScalarState candidate in current)
                {
                    AddScalarRange(
                        next,
                        MatchScalarPattern(
                            values,
                            candidate.Position,
                            childIndex,
                            depth + 1));
                }
                current = next;
            }
            childNumber = childNumber + 1;
        }
        return current;
    }

    List<SchemaScalarState> MatchScalarList(
        string[] values,
        int position,
        SchemaPattern pattern,
        int depth)
    {
        List<SchemaScalarState> result = NewScalarList();
        if (position >= values.Length)
        {
            return result;
        }
        string[] tokens = SchemaDatatypeValidator.SplitList(values[position]);
        List<SchemaScalarState> matches = MatchScalarChildren(
            tokens,
            0,
            pattern,
            depth + 1);
        foreach (SchemaScalarState match in matches)
        {
            if (match.Position == tokens.Length)
            {
                AddUniqueScalar(result, position + 1);
            }
        }
        return result;
    }

    List<SchemaScalarState> MatchScalarRepetition(
        string[] values,
        int position,
        SchemaPattern pattern,
        bool requireOne,
        int depth)
    {
        List<SchemaScalarState> accepted = NewScalarList();
        List<SchemaScalarState> frontier = NewScalarList();
        AddUniqueScalar(frontier, position);
        if (!requireOne)
        {
            AddUniqueScalar(accepted, position);
        }
        while (frontier.Count > 0 && !LimitExceeded)
        {
            List<SchemaScalarState> next = NewScalarList();
            foreach (SchemaScalarState candidate in frontier)
            {
                List<SchemaScalarState> matches = MatchScalarChildren(
                    values,
                    candidate.Position,
                    pattern,
                    depth + 1);
                foreach (SchemaScalarState match in matches)
                {
                    if (match.Position != candidate.Position
                        && !ContainsScalar(accepted, match.Position))
                    {
                        AddUniqueScalar(next, match.Position);
                    }
                    AddUniqueScalar(accepted, match.Position);
                }
            }
            frontier = next;
        }
        return accepted;
    }

    List<SchemaScalarState> MatchScalarReference(
        string[] values,
        int position,
        SchemaPattern pattern,
        int depth)
    {
        List<SchemaScalarState> result = NewScalarList();
        SchemaRule[] definitions = Index.FindDefinitionRules(pattern.Name);
        foreach (SchemaRule definition in definitions)
        {
            AddScalarRange(
                result,
                MatchScalarPattern(
                    values,
                    position,
                    definition.PatternIndex,
                    depth + 1));
        }
        return result;
    }

    bool DataValueMatches(
        string value,
        SchemaPattern pattern,
        int depth)
    {
        bool valid = SchemaDatatypeValidator.IsValid(
            value,
            pattern,
            Rules,
            Limits.MaximumDatatypeLength);
        if (!valid)
        {
            InvalidDatatype = true;
            return false;
        }
        int childNumber = 0;
        while (childNumber < pattern.ChildCount)
        {
            int childIndex = ChildPatternIndex(pattern, childNumber);
            SchemaPattern child = Rules.Patterns[childIndex];
            if (child.Kind == SchemaPatternKind.Except)
            {
                int exceptNumber = 0;
                while (exceptNumber < child.ChildCount)
                {
                    int exceptIndex = ChildPatternIndex(child, exceptNumber);
                    string[] values = [value];
                    List<SchemaScalarState> exceptions = MatchScalarPattern(
                        values,
                        0,
                        exceptIndex,
                        depth + 1);
                    foreach (SchemaScalarState exception in exceptions)
                    {
                        if (exception.Position == 1)
                        {
                            InvalidDatatype = true;
                            return false;
                        }
                    }
                    exceptNumber = exceptNumber + 1;
                }
            }
            childNumber = childNumber + 1;
        }
        return true;
    }

    bool ElementNameMatches(
        SchemaPattern elementPattern,
        OdfQualifiedName name,
        int depth)
    {
        if (elementPattern.Name.Length > 0)
        {
            return elementPattern.Name == name.LocalName
                && elementPattern.NamespaceName == name.NamespaceName;
        }
        int childNumber = 0;
        while (childNumber < elementPattern.ChildCount)
        {
            int childIndex = ChildPatternIndex(elementPattern, childNumber);
            if (IsDirectNameClass(childIndex)
                && NameClassMatches(childIndex, name, depth + 1))
            {
                return true;
            }
            childNumber = childNumber + 1;
        }
        return false;
    }

    bool AttributeNameMatches(
        SchemaPattern attributePattern,
        OdfQualifiedName name,
        int depth)
    {
        if (attributePattern.Name.Length > 0)
        {
            return attributePattern.Name == name.LocalName
                && attributePattern.NamespaceName == name.NamespaceName;
        }
        int childNumber = 0;
        while (childNumber < attributePattern.ChildCount)
        {
            int childIndex = ChildPatternIndex(attributePattern, childNumber);
            if (IsDirectNameClass(childIndex)
                && NameClassMatches(childIndex, name, depth + 1))
            {
                return true;
            }
            childNumber = childNumber + 1;
        }
        return false;
    }

    bool NameClassMatches(
        int patternIndex,
        OdfQualifiedName name,
        int depth)
    {
        if (!EnterOperation(depth) || !PatternIndexIsValid(patternIndex))
        {
            return false;
        }
        SchemaPattern pattern = Rules.Patterns[patternIndex];
        if (pattern.Kind == SchemaPatternKind.Name)
        {
            return pattern.Name == name.LocalName
                && pattern.NamespaceName == name.NamespaceName;
        }
        if (pattern.Kind == SchemaPatternKind.AnyName)
        {
            return !NameClassHasException(pattern, name, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.NamespaceName)
        {
            return pattern.NamespaceName == name.NamespaceName
                && !NameClassHasException(pattern, name, depth + 1);
        }
        if (pattern.Kind == SchemaPatternKind.Except)
        {
            int childNumber = 0;
            while (childNumber < pattern.ChildCount)
            {
                if (NameClassMatches(
                    ChildPatternIndex(pattern, childNumber),
                    name,
                    depth + 1))
                {
                    return true;
                }
                childNumber = childNumber + 1;
            }
        }
        return false;
    }

    bool NameClassHasException(
        SchemaPattern pattern,
        OdfQualifiedName name,
        int depth)
    {
        int childNumber = 0;
        while (childNumber < pattern.ChildCount)
        {
            int childIndex = ChildPatternIndex(pattern, childNumber);
            SchemaPattern child = Rules.Patterns[childIndex];
            if (child.Kind == SchemaPatternKind.Except
                && NameClassMatches(childIndex, name, depth + 1))
            {
                return true;
            }
            childNumber = childNumber + 1;
        }
        return false;
    }

    bool PatternUsesOnlyAttributes(int patternIndex, int depth)
    {
        if (!EnterOperation(depth) || !PatternIndexIsValid(patternIndex))
        {
            return false;
        }
        SchemaPattern pattern = Rules.Patterns[patternIndex];
        if (pattern.Kind == SchemaPatternKind.Attribute
            || pattern.Kind == SchemaPatternKind.Empty
            || pattern.Kind == SchemaPatternKind.NotAllowed)
        {
            return true;
        }
        if (pattern.Kind == SchemaPatternKind.Ref
            || pattern.Kind == SchemaPatternKind.ParentRef)
        {
            SchemaRule[] definitions = Index.FindDefinitionRules(pattern.Name);
            if (definitions.Length == 0)
            {
                return false;
            }
            foreach (SchemaRule definition in definitions)
            {
                if (!PatternUsesOnlyAttributes(
                    definition.PatternIndex,
                    depth + 1))
                {
                    return false;
                }
            }
            return true;
        }
        if (pattern.Kind == SchemaPatternKind.Choice
            || pattern.Kind == SchemaPatternKind.Group
            || pattern.Kind == SchemaPatternKind.Interleave
            || pattern.Kind == SchemaPatternKind.Optional
            || pattern.Kind == SchemaPatternKind.ZeroOrMore
            || pattern.Kind == SchemaPatternKind.OneOrMore)
        {
            int childNumber = 0;
            while (childNumber < pattern.ChildCount)
            {
                if (!PatternUsesOnlyAttributes(
                    ChildPatternIndex(pattern, childNumber),
                    depth + 1))
                {
                    return false;
                }
                childNumber = childNumber + 1;
            }
            return true;
        }
        return false;
    }

    bool IsDirectNameClass(int patternIndex)
    {
        if (!PatternIndexIsValid(patternIndex))
        {
            return false;
        }
        SchemaPatternKind kind = Rules.Patterns[patternIndex].Kind;
        return kind == SchemaPatternKind.Name
            || kind == SchemaPatternKind.AnyName
            || kind == SchemaPatternKind.NamespaceName;
    }

    SchemaMatchState NewElementState(OdfNode element)
    {
        SchemaMatchState result = new SchemaMatchState();
        result.UsedAttributes = new bool[element.Attributes.Length];
        return result;
    }

    SchemaMatchState CopyElementState(SchemaMatchState state)
    {
        SchemaMatchState result = new SchemaMatchState();
        result.UsedAttributes = CopyBooleans(state.UsedAttributes);
        result.ChildIndex = state.ChildIndex;
        result.AllowMixedText = state.AllowMixedText;
        return result;
    }

    bool[] CopyBooleans(bool[] values)
    {
        bool[] result = new bool[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }
        return result;
    }

    int[] CopyIntegers(int[] values)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index = index + 1;
        }
        return result;
    }

    int NextRelevantChild(OdfNode context, SchemaMatchState state)
    {
        int index = state.ChildIndex;
        while (index < context.Children.Length)
        {
            OdfNode child = context.Children[index];
            if (child.Kind == OdfNodeKind.Comment
                || child.Kind == OdfNodeKind.ProcessingInstruction)
            {
                index = index + 1;
                continue;
            }
            if (child.Kind == OdfNodeKind.Text)
            {
                if (state.AllowMixedText
                    || (!child.IsSignificantWhitespace
                        && string.IsNullOrWhiteSpace(child.Value)))
                {
                    index = index + 1;
                    continue;
                }
            }
            break;
        }
        return index;
    }

    bool ElementStateIsComplete(OdfNode context, SchemaMatchState state)
    {
        int childIndex = NextRelevantChild(context, state);
        if (childIndex != context.Children.Length)
        {
            return false;
        }
        int index = 0;
        while (index < state.UsedAttributes.Length)
        {
            if (!state.UsedAttributes[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    int ChildPatternIndex(SchemaPattern pattern, int childNumber)
    {
        return Rules.PatternChildIndexes[
            pattern.FirstChildIndex + childNumber];
    }

    bool PatternIndexIsValid(int patternIndex)
    {
        return patternIndex >= 0 && patternIndex < Rules.Patterns.Length;
    }

    bool EnterOperation(int depth)
    {
        if (depth > GreatestDepth)
        {
            GreatestDepth = depth;
        }
        if (Operations >= Limits.MaximumOperations
            || depth > Limits.MaximumPatternDepth)
        {
            LimitExceeded = true;
            return false;
        }
        Operations = Operations + 1;
        return true;
    }

    List<SchemaMatchState> NewStateList()
    {
        return new List<SchemaMatchState>();
    }

    List<SchemaScalarState> NewScalarList()
    {
        return new List<SchemaScalarState>();
    }

    void AddUniqueState(
        List<SchemaMatchState> states,
        SchemaMatchState candidate)
    {
        if (LimitExceeded || ContainsState(states, candidate))
        {
            return;
        }
        if (states.Count >= Limits.MaximumStates)
        {
            LimitExceeded = true;
            return;
        }
        states.Add(candidate);
    }

    void AddStateRange(
        List<SchemaMatchState> states,
        List<SchemaMatchState> candidates)
    {
        foreach (SchemaMatchState candidate in candidates)
        {
            AddUniqueState(states, candidate);
        }
    }

    bool ContainsState(
        List<SchemaMatchState> states,
        SchemaMatchState candidate)
    {
        foreach (SchemaMatchState state in states)
        {
            if (StatesAreEqual(state, candidate))
            {
                return true;
            }
        }
        return false;
    }

    bool StatesAreEqual(
        SchemaMatchState left,
        SchemaMatchState right)
    {
        if (left.ChildIndex != right.ChildIndex
            || left.AllowMixedText != right.AllowMixedText
            || left.UsedAttributes.Length != right.UsedAttributes.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.UsedAttributes.Length)
        {
            if (left.UsedAttributes[index] != right.UsedAttributes[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    void AddUniqueScalar(
        List<SchemaScalarState> states,
        int position)
    {
        if (LimitExceeded || ContainsScalar(states, position))
        {
            return;
        }
        if (states.Count >= Limits.MaximumStates)
        {
            LimitExceeded = true;
            return;
        }
        SchemaScalarState state = new SchemaScalarState();
        state.Position = position;
        states.Add(state);
    }

    void AddScalarRange(
        List<SchemaScalarState> states,
        List<SchemaScalarState> candidates)
    {
        foreach (SchemaScalarState candidate in candidates)
        {
            AddUniqueScalar(states, candidate.Position);
        }
    }

    bool ContainsScalar(
        List<SchemaScalarState> states,
        int position)
    {
        foreach (SchemaScalarState state in states)
        {
            if (state.Position == position)
            {
                return true;
            }
        }
        return false;
    }
}
