namespace barcode;

internal class Gs1AssociationValidationResult
{
    internal bool Success;
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
}

internal static class Gs1AssociationValidator
{
    internal static Gs1AssociationValidationResult ValidateMessage(
        Gs1Message? message)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (message == null)
        {
            AddIssue(
                issues,
                "GS1_ASSOCIATION_MESSAGE_MISSING",
                "A GS1 message is required for association validation.",
                "Message",
                -1,
                "a parsed GS1 message",
                "null");
            return CreateResult(issues);
        }

        Gs1Element[] elements = message.Elements == null
            ? new Gs1Element[0]
            : message.Elements;
        ValidateElementAssociations(elements, issues);
        if (message.Syntax == Gs1Syntax.DigitalLinkUri)
        {
            ValidateDigitalLink(elements, issues);
        }

        SortIssues(issues);
        return CreateResult(issues);
    }

    static void ValidateElementAssociations(
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element != null && element.ApplicationIdentifier != null)
            {
                ValidateRepeatedIdentifier(elements, index, issues);
                ValidateExcludedPairs(elements, index, issues);
                Gs1AiDefinition? definition =
                    Gs1AiResolver.ResolveDefinition(
                        element.ApplicationIdentifier);
                if (definition != null)
                {
                    ValidateRequiredGroups(
                        elements,
                        element,
                        definition,
                        issues);
                }
            }

            index = index + 1;
        }
    }

    static void ValidateRepeatedIdentifier(
        Gs1Element[] elements,
        int index,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1Element element = elements[index];
        int previousIndex = 0;
        while (previousIndex < index)
        {
            Gs1Element previous = elements[previousIndex];
            if (previous != null
                && previous.ApplicationIdentifier
                    == element.ApplicationIdentifier)
            {
                AddIssue(
                    issues,
                    "GS1_REPEATED_APPLICATION_IDENTIFIER",
                    "A GS1 Application Identifier must not be repeated in one message.",
                    element.ApplicationIdentifier == null
                        ? "ApplicationIdentifier"
                        : element.ApplicationIdentifier,
                    element.SourceOffset,
                    "one occurrence of the Application Identifier",
                    element.ApplicationIdentifier == null
                        ? ""
                        : element.ApplicationIdentifier);
                return;
            }

            previousIndex = previousIndex + 1;
        }
    }

    static void ValidateExcludedPairs(
        Gs1Element[] elements,
        int index,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1Element element = elements[index];
        string identifier = element.ApplicationIdentifier == null
            ? ""
            : element.ApplicationIdentifier;
        Gs1AiDefinition? definition =
            Gs1AiResolver.ResolveDefinition(identifier);
        int previousIndex = 0;
        while (previousIndex < index)
        {
            Gs1Element previous = elements[previousIndex];
            string previousIdentifier = previous == null
                || previous.ApplicationIdentifier == null
                ? ""
                : previous.ApplicationIdentifier;
            if (identifier != previousIdentifier)
            {
                Gs1AiDefinition? previousDefinition =
                    Gs1AiResolver.ResolveDefinition(previousIdentifier);
                bool excluded = definition != null
                    && ExcludesIdentifier(definition, previousIdentifier);
                if (!excluded && previousDefinition != null)
                {
                    excluded = ExcludesIdentifier(
                        previousDefinition,
                        identifier);
                }

                if (excluded)
                {
                    AddIssue(
                        issues,
                        "GS1_EXCLUDED_ASSOCIATION",
                        "The two GS1 Application Identifiers are mutually excluded.",
                        identifier,
                        element.SourceOffset,
                        "Application Identifiers that may occur together",
                        previousIdentifier + "+" + identifier);
                }
            }

            previousIndex = previousIndex + 1;
        }
    }

    static bool ExcludesIdentifier(
        Gs1AiDefinition definition,
        string identifier)
    {
        int index = 0;
        while (index < definition.Exclusions.Length)
        {
            if (Gs1AiResolver.MatchesPattern(
                identifier,
                definition.Exclusions[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void ValidateRequiredGroups(
        Gs1Element[] elements,
        Gs1Element element,
        Gs1AiDefinition definition,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        if (definition.RequiredAssociations.Length == 0)
        {
            return;
        }

        int groupIndex = 0;
        while (groupIndex < definition.RequiredAssociations.Length)
        {
            string[] group = definition.RequiredAssociations[groupIndex];
            bool groupPresent = true;
            int patternIndex = 0;
            while (patternIndex < group.Length)
            {
                if (!ContainsPattern(elements, group[patternIndex]))
                {
                    groupPresent = false;
                    break;
                }

                patternIndex = patternIndex + 1;
            }

            if (groupPresent)
            {
                return;
            }

            groupIndex = groupIndex + 1;
        }

        string identifier = element.ApplicationIdentifier == null
            ? "ApplicationIdentifier"
            : element.ApplicationIdentifier;
        AddIssue(
            issues,
            "GS1_REQUIRED_ASSOCIATION_MISSING",
            "The GS1 Application Identifier is missing a required association.",
            identifier,
            element.SourceOffset,
            FormatGroups(definition.RequiredAssociations),
            identifier);
    }

    static bool ContainsPattern(Gs1Element[] elements, string pattern)
    {
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element != null
                && Gs1AiResolver.MatchesPattern(
                    element.ApplicationIdentifier,
                    pattern))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string FormatGroups(string[][] groups)
    {
        string text = "";
        int groupIndex = 0;
        while (groupIndex < groups.Length)
        {
            if (groupIndex > 0)
            {
                text = text + " or ";
            }

            string[] group = groups[groupIndex];
            int valueIndex = 0;
            while (valueIndex < group.Length)
            {
                if (valueIndex > 0)
                {
                    text = text + "+";
                }

                text = text + group[valueIndex];
                valueIndex = valueIndex + 1;
            }

            groupIndex = groupIndex + 1;
        }

        return text;
    }

    static void ValidateDigitalLink(
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        global::System.Collections.Generic.List<Gs1Element> primaryKeys =
            new global::System.Collections.Generic.List<Gs1Element>();
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            Gs1AiDefinition? definition = element == null
                ? null
                : Gs1AiResolver.ResolveDefinition(
                    element.ApplicationIdentifier);
            if (element != null
                && definition != null
                && definition.DigitalLinkPrimaryKey)
            {
                if (!ContainsIdentifier(primaryKeys, element.ApplicationIdentifier))
                {
                    primaryKeys.Add(element);
                }
            }

            index = index + 1;
        }

        if (primaryKeys.Count == 0)
        {
            AddIssue(
                issues,
                "GS1_DIGITAL_LINK_PRIMARY_KEY_MISSING",
                "A GS1 Digital Link message requires one primary key.",
                "DigitalLinkPrimaryKey",
                -1,
                "exactly one GS1 Digital Link primary key",
                "none");
            return;
        }

        if (primaryKeys.Count > 1)
        {
            index = 1;
            while (index < primaryKeys.Count)
            {
                Gs1Element element = primaryKeys[index];
                AddIssue(
                    issues,
                    "GS1_DIGITAL_LINK_MULTIPLE_PRIMARY_KEYS",
                    "A GS1 Digital Link message contains more than one primary key.",
                    element.ApplicationIdentifier == null
                        ? "DigitalLinkPrimaryKey"
                        : element.ApplicationIdentifier,
                    element.SourceOffset,
                    "exactly one GS1 Digital Link primary key",
                    FormatPrimaryKeys(primaryKeys));
                index = index + 1;
            }

            return;
        }

        Gs1Element primaryKey = primaryKeys[0];
        Gs1AiDefinition? primaryDefinition =
            Gs1AiResolver.ResolveDefinition(
                primaryKey.ApplicationIdentifier);
        if (primaryDefinition == null)
        {
            return;
        }

        ValidateQualifierSequence(
            elements,
            primaryKey,
            primaryDefinition,
            issues);
        ValidateDigitalLinkAttributes(
            elements,
            primaryKey,
            primaryDefinition,
            issues);
    }

    static bool ContainsIdentifier(
        global::System.Collections.Generic.List<Gs1Element> elements,
        string? identifier)
    {
        int index = 0;
        while (index < elements.Count)
        {
            if (elements[index].ApplicationIdentifier == identifier)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string FormatPrimaryKeys(
        global::System.Collections.Generic.List<Gs1Element> primaryKeys)
    {
        string text = "";
        int index = 0;
        while (index < primaryKeys.Count)
        {
            if (index > 0)
            {
                text = text + "+";
            }

            string? identifier = primaryKeys[index].ApplicationIdentifier;
            text = text + (identifier == null ? "" : identifier);
            index = index + 1;
        }

        return text;
    }

    static void ValidateQualifierSequence(
        Gs1Element[] elements,
        Gs1Element primaryKey,
        Gs1AiDefinition definition,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        global::System.Collections.Generic.List<Gs1Element> qualifiers =
            CollectQualifiers(elements, definition);
        if (qualifiers.Count == 0)
        {
            return;
        }

        int index = 0;
        while (index < qualifiers.Count)
        {
            if (qualifiers[index].SourceOffset < primaryKey.SourceOffset)
            {
                string qualifierIdentifier = "";
                string? observedIdentifier =
                    qualifiers[index].ApplicationIdentifier;
                if (observedIdentifier != null)
                {
                    qualifierIdentifier = observedIdentifier;
                }
                AddIssue(
                    issues,
                    "GS1_DIGITAL_LINK_QUALIFIER_BEFORE_PRIMARY_KEY",
                    "A GS1 Digital Link qualifier appears before its primary key.",
                    qualifierIdentifier.Length == 0
                        ? "DigitalLinkQualifier"
                        : qualifierIdentifier,
                    qualifiers[index].SourceOffset,
                    "the primary key before all key qualifiers",
                    qualifierIdentifier);
                return;
            }

            index = index + 1;
        }

        int bestMatch = 0;
        int sequenceIndex = 0;
        while (sequenceIndex < definition.DigitalLinkQualifierSequences.Length)
        {
            int matched = CountQualifierMatches(
                qualifiers,
                definition.DigitalLinkQualifierSequences[sequenceIndex]);
            if (matched == qualifiers.Count)
            {
                return;
            }

            if (matched > bestMatch)
            {
                bestMatch = matched;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        Gs1Element invalid = qualifiers[bestMatch];
        AddIssue(
            issues,
            "GS1_DIGITAL_LINK_QUALIFIER_SEQUENCE_INVALID",
            "The GS1 Digital Link key qualifiers do not follow an allowed sequence.",
            invalid.ApplicationIdentifier == null
                ? "DigitalLinkQualifier"
                : invalid.ApplicationIdentifier,
            invalid.SourceOffset,
            FormatGroups(definition.DigitalLinkQualifierSequences),
            FormatElements(qualifiers));
    }

    static global::System.Collections.Generic.List<Gs1Element> CollectQualifiers(
        Gs1Element[] elements,
        Gs1AiDefinition definition)
    {
        global::System.Collections.Generic.List<Gs1Element> qualifiers =
            new global::System.Collections.Generic.List<Gs1Element>();
        int elementIndex = 0;
        while (elementIndex < elements.Length)
        {
            Gs1Element element = elements[elementIndex];
            bool qualifier = false;
            int sequenceIndex = 0;
            while (sequenceIndex < definition.DigitalLinkQualifierSequences.Length)
            {
                string[] sequence =
                    definition.DigitalLinkQualifierSequences[sequenceIndex];
                int valueIndex = 0;
                while (valueIndex < sequence.Length)
                {
                    if (element != null
                        && Gs1AiResolver.MatchesPattern(
                            element.ApplicationIdentifier,
                            sequence[valueIndex]))
                    {
                        qualifier = true;
                        break;
                    }

                    valueIndex = valueIndex + 1;
                }

                if (qualifier)
                {
                    break;
                }

                sequenceIndex = sequenceIndex + 1;
            }

            if (qualifier && element != null)
            {
                qualifiers.Add(element);
            }

            elementIndex = elementIndex + 1;
        }

        return qualifiers;
    }

    static int CountQualifierMatches(
        global::System.Collections.Generic.List<Gs1Element> qualifiers,
        string[] sequence)
    {
        int qualifierIndex = 0;
        int sequenceIndex = 0;
        while (qualifierIndex < qualifiers.Count
            && sequenceIndex < sequence.Length)
        {
            if (Gs1AiResolver.MatchesPattern(
                qualifiers[qualifierIndex].ApplicationIdentifier,
                sequence[sequenceIndex]))
            {
                qualifierIndex = qualifierIndex + 1;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        return qualifierIndex;
    }

    static string FormatElements(
        global::System.Collections.Generic.List<Gs1Element> elements)
    {
        string text = "";
        int index = 0;
        while (index < elements.Count)
        {
            if (index > 0)
            {
                text = text + "+";
            }

            string? identifier = elements[index].ApplicationIdentifier;
            text = text + (identifier == null ? "" : identifier);
            index = index + 1;
        }

        return text;
    }

    static void ValidateDigitalLinkAttributes(
        Gs1Element[] elements,
        Gs1Element primaryKey,
        Gs1AiDefinition primaryDefinition,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element != null && element != primaryKey)
            {
                Gs1AiDefinition? definition =
                    Gs1AiResolver.ResolveDefinition(
                        element.ApplicationIdentifier);
                bool anotherPrimaryKey = definition != null
                    && definition.DigitalLinkPrimaryKey;
                bool qualifier = IsQualifier(
                    element,
                    primaryDefinition.DigitalLinkQualifierSequences);
                if (definition != null
                    && !anotherPrimaryKey
                    && !qualifier
                    && !definition.DigitalLinkDataAttribute)
                {
                    string identifier = element.ApplicationIdentifier == null
                        ? "DigitalLinkDataAttribute"
                        : element.ApplicationIdentifier;
                    AddIssue(
                        issues,
                        "GS1_DIGITAL_LINK_DATA_ATTRIBUTE_NOT_PERMITTED",
                        "The Application Identifier is not permitted as GS1 Digital Link data.",
                        identifier,
                        element.SourceOffset,
                        "a Digital Link key qualifier or data attribute",
                        identifier);
                }
            }

            index = index + 1;
        }
    }

    static bool IsQualifier(Gs1Element element, string[][] sequences)
    {
        int sequenceIndex = 0;
        while (sequenceIndex < sequences.Length)
        {
            string[] sequence = sequences[sequenceIndex];
            int valueIndex = 0;
            while (valueIndex < sequence.Length)
            {
                if (Gs1AiResolver.MatchesPattern(
                    element.ApplicationIdentifier,
                    sequence[valueIndex]))
                {
                    return true;
                }

                valueIndex = valueIndex + 1;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        return false;
    }

    static void AddIssue(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        string code,
        string message,
        string field,
        int offset,
        string expected,
        string observed)
    {
        BarcodeValidationIssue issue = ValidationIssueFactory.Create(
            BarcodeValidationSeverity.Error,
            code,
            message,
            Gs1ElementStringParser.StandardName,
            field,
            offset,
            expected,
            observed);
        issues.Add(issue);
    }

    static void SortIssues(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int index = 1;
        while (index < issues.Count)
        {
            BarcodeValidationIssue current = issues[index];
            int previousIndex = index - 1;
            while (previousIndex >= 0
                && issues[previousIndex].Offset > current.Offset)
            {
                issues[previousIndex + 1] = issues[previousIndex];
                previousIndex = previousIndex - 1;
            }

            issues[previousIndex + 1] = current;
            index = index + 1;
        }
    }

    static Gs1AssociationValidationResult CreateResult(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1AssociationValidationResult result =
            new Gs1AssociationValidationResult();
        result.Success = issues.Count == 0;
        result.Issues = issues.ToArray();
        return result;
    }
}
