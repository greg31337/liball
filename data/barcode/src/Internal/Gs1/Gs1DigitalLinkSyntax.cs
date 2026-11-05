namespace barcode;

internal class Gs1DigitalLinkParseResult
{
    internal bool Success;
    internal Gs1Message Message = new Gs1Message();
    internal string BaseUri = "";
    internal string[] IgnoredQueryParameters = new string[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
}

internal class Gs1DigitalLinkBuildResult
{
    internal bool Success;
    internal string Text = "";
    internal Gs1Element[] Elements = new Gs1Element[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
}

internal class Gs1DigitalLinkSegment
{
    internal string Text = "";
    internal int Offset;
}

internal class Gs1DigitalLinkUriParts
{
    internal bool Success;
    internal int PathStart;
    internal int PathEnd;
    internal int QueryStart = -1;
    internal int QueryEnd;
}

internal class Gs1DigitalLinkDecodedValue
{
    internal bool Success;
    internal string Text = "";
    internal int[] SourceOffsets = new int[0];
}

internal static class Gs1DigitalLinkSyntax
{
    internal const string CanonicalBaseUri = "https://id.gs1.org";

    internal static Gs1DigitalLinkParseResult ParseDigitalLink(
        string? source)
    {
        return ParseDigitalLinkWithAssociationPolicy(source, true);
    }

    internal static Gs1DigitalLinkParseResult ParseDigitalLinkWithAssociationPolicy(
        string? source,
        bool validateAssociations)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        global::System.Collections.Generic.List<Gs1Element> elements =
            new global::System.Collections.Generic.List<Gs1Element>();
        global::System.Collections.Generic.List<string> ignoredParameters =
            new global::System.Collections.Generic.List<string>();
        if (source == null || source.Length == 0)
        {
            AddDigitalLinkIssue(
                issues,
                source == null
                    ? "GS1_DIGITAL_LINK_NULL"
                    : "GS1_DIGITAL_LINK_EMPTY",
                "A GS1 Digital Link URI must not be null or empty.",
                "DigitalLinkUri",
                source == null ? -1 : 0,
                "an absolute HTTP or HTTPS GS1 Digital Link URI",
                source == null ? "null" : "empty");
            return CreateParseResult(
                source,
                "",
                elements,
                ignoredParameters,
                issues);
        }

        Gs1DigitalLinkUriParts parts = ReadUriParts(source, issues);
        if (!parts.Success)
        {
            return CreateParseResult(
                source,
                "",
                elements,
                ignoredParameters,
                issues);
        }

        global::System.Collections.Generic.List<Gs1DigitalLinkSegment> segments =
            ReadPathSegments(source, parts.PathStart, parts.PathEnd);
        int primarySegmentIndex = FindPrimarySegment(segments);
        if (primarySegmentIndex < 0)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_PRIMARY_KEY_MISSING",
                "The URI path does not contain a registered GS1 Digital Link primary key.",
                "DigitalLinkPrimaryKey",
                parts.PathStart,
                "a primary-key AI and value pair in the URI path",
                source.Substring(parts.PathStart, parts.PathEnd - parts.PathStart));
            return CreateParseResult(
                source,
                "",
                elements,
                ignoredParameters,
                issues);
        }

        int gs1SegmentCount = segments.Count - primarySegmentIndex;
        if (gs1SegmentCount < 2 || gs1SegmentCount % 2 != 0)
        {
            Gs1DigitalLinkSegment incomplete =
                segments[segments.Count - 1];
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_PATH_PAIR_INCOMPLETE",
                "The GS1 Digital Link path must contain alternating Application Identifier and value segments.",
                "Path",
                incomplete.Offset,
                "complete AI/value path pairs",
                incomplete.Text);
        }

        int usableSegmentCount = gs1SegmentCount - gs1SegmentCount % 2;
        int segmentIndex = primarySegmentIndex;
        int segmentLimit = primarySegmentIndex + usableSegmentCount;
        while (segmentIndex < segmentLimit)
        {
            Gs1DigitalLinkSegment identifierSegment = segments[segmentIndex];
            Gs1DigitalLinkSegment valueSegment = segments[segmentIndex + 1];
            string identifier = identifierSegment.Text;
            Gs1AiDefinition? definition =
                IsAllDigits(identifier)
                    ? Gs1AiResolver.ResolveDefinition(identifier)
                    : null;
            if (definition == null)
            {
                AddDigitalLinkIssue(
                    issues,
                    "GS1_DIGITAL_LINK_PATH_AI_INVALID",
                    "A GS1 Digital Link path identifier must be a registered numeric Application Identifier.",
                    "ApplicationIdentifier",
                    identifierSegment.Offset,
                    "a registered numeric GS1 Application Identifier",
                    identifier);
                segmentIndex = segmentIndex + 2;
                continue;
            }

            Gs1DigitalLinkDecodedValue decoded = DecodeUriValue(
                valueSegment.Text,
                valueSegment.Offset,
                issues);
            if (decoded.Success)
            {
                Gs1Element? element = ParseElementPair(
                    identifier,
                    decoded,
                    identifierSegment.Offset,
                    valueSegment.Offset,
                    valueSegment.Text.Length,
                    issues);
                if (element != null)
                {
                    elements.Add(element);
                }
            }

            segmentIndex = segmentIndex + 2;
        }

        Gs1AiDefinition? primaryDefinition =
            Gs1AiResolver.ResolveDefinition(
                segments[primarySegmentIndex].Text);
        if (primaryDefinition != null)
        {
            ValidateParsedPathQualifiers(
                elements,
                primaryDefinition,
                issues);
            if (parts.QueryStart >= 0)
            {
                ParseQueryAttributes(
                    source,
                    parts.QueryStart + 1,
                    parts.QueryEnd,
                    primaryDefinition,
                    elements,
                    ignoredParameters,
                    issues);
            }
        }

        Gs1Message message = CreateDigitalLinkMessage(
            source,
            elements,
            issues);
        if (issues.Count == 0 && validateAssociations)
        {
            Gs1AssociationValidationResult association =
                Gs1AssociationValidator.ValidateMessage(message);
            AppendIssues(issues, association.Issues);
        }

        SortDigitalLinkIssues(issues);
        message.Issues = issues.ToArray();
        string baseUri = source.Substring(
            0,
            segments[primarySegmentIndex].Offset - 1);
        return CreateParseResult(
            source,
            baseUri,
            elements,
            ignoredParameters,
            issues);
    }

    internal static Gs1DigitalLinkBuildResult BuildDigitalLink(
        Gs1Element[]? elements,
        string? baseUri)
    {
        return BuildDigitalLinkWithAssociationPolicy(
            elements,
            baseUri,
            true);
    }

    internal static Gs1DigitalLinkBuildResult BuildDigitalLinkWithAssociationPolicy(
        Gs1Element[]? elements,
        string? baseUri,
        bool validateAssociations)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (elements == null || elements.Length == 0)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_ELEMENTS_MISSING",
                "At least one GS1 element is required to build a Digital Link URI.",
                "Elements",
                -1,
                "one or more GS1 elements",
                elements == null ? "null" : "empty");
            return CreateBuildResult("", new Gs1Element[0], issues);
        }

        ValidateBuildElements(elements, issues);
        Gs1Element? primary = FindBuildPrimary(elements, issues);
        if (primary == null)
        {
            SortDigitalLinkIssues(issues);
            return CreateBuildResult("", new Gs1Element[0], issues);
        }

        Gs1AiDefinition? primaryDefinition =
            Gs1AiResolver.ResolveDefinition(primary.ApplicationIdentifier);
        if (primaryDefinition == null)
        {
            return CreateBuildResult("", new Gs1Element[0], issues);
        }

        global::System.Collections.Generic.List<Gs1Element> qualifiers =
            OrderBuildQualifiers(
                elements,
                primary,
                primaryDefinition,
                issues);
        global::System.Collections.Generic.List<Gs1Element> attributes =
            CollectBuildAttributes(
                elements,
                primary,
                qualifiers,
                issues);
        SortBuildAttributes(attributes);

        global::System.Collections.Generic.List<Gs1Element> ordered =
            new global::System.Collections.Generic.List<Gs1Element>();
        ordered.Add(primary);
        AppendElements(ordered, qualifiers);
        AppendElements(ordered, attributes);
        Gs1Message message = new Gs1Message();
        message.Elements = ordered.ToArray();
        message.Syntax = Gs1Syntax.DigitalLinkUri;
        if (validateAssociations)
        {
            Gs1AssociationValidationResult association =
                Gs1AssociationValidator.ValidateMessage(message);
            AppendIssues(issues, association.Issues);
        }

        string normalizedBaseUri = NormalizeBaseUri(baseUri, issues);
        SortDigitalLinkIssues(issues);
        if (issues.Count > 0)
        {
            return CreateBuildResult("", ordered.ToArray(), issues);
        }

        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        text.Append(normalizedBaseUri);
        AppendPathElement(text, primary);
        int index = 0;
        while (index < qualifiers.Count)
        {
            AppendPathElement(text, qualifiers[index]);
            index = index + 1;
        }

        index = 0;
        while (index < attributes.Count)
        {
            text.Append(index == 0 ? '?' : '&');
            string? sourceIdentifier =
                attributes[index].ApplicationIdentifier;
            string identifier = sourceIdentifier == null
                ? ""
                : sourceIdentifier;
            string? sourceValue = attributes[index].Value;
            string value = sourceValue == null
                ? ""
                : sourceValue;
            text.Append(identifier);
            text.Append('=');
            text.Append(EncodeUriValue(value));
            index = index + 1;
        }

        return CreateBuildResult(text.ToString(), ordered.ToArray(), issues);
    }

    internal static string EncodeUriValue(string value)
    {
        const string hexadecimal = "0123456789ABCDEF";
        global::System.Text.StringBuilder encoded =
            new global::System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (IsDirectUriValueCharacter(character))
            {
                encoded.Append(character);
            }
            else
            {
                int numeric = character;
                encoded.Append('%');
                encoded.Append(hexadecimal[(numeric >> 4) & 15]);
                encoded.Append(hexadecimal[numeric & 15]);
            }

            index = index + 1;
        }

        return encoded.ToString();
    }

    static Gs1DigitalLinkUriParts ReadUriParts(
        string source,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1DigitalLinkUriParts parts = new Gs1DigitalLinkUriParts();
        int schemeEnd = source.IndexOf("://");
        if (schemeEnd < 0)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_SCHEME_MISSING",
                "A GS1 Digital Link URI must use an absolute HTTP or HTTPS scheme.",
                "Scheme",
                0,
                "http:// or https://",
                source);
            return parts;
        }

        string scheme = source.Substring(0, schemeEnd);
        if (scheme != "http"
            && scheme != "https"
            && scheme != "HTTP"
            && scheme != "HTTPS")
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_SCHEME_INVALID",
                "A GS1 Digital Link URI must use HTTP or HTTPS.",
                "Scheme",
                0,
                "http or https",
                scheme);
            return parts;
        }

        int authorityStart = schemeEnd + 3;
        int fragmentStart = source.IndexOf('#', authorityStart);
        int uriEnd = fragmentStart < 0 ? source.Length : fragmentStart;
        int queryStart = source.IndexOf('?', authorityStart);
        if (queryStart >= uriEnd)
        {
            queryStart = -1;
        }

        int pathEnd = queryStart < 0 ? uriEnd : queryStart;
        int pathStart = source.IndexOf('/', authorityStart);
        int authorityEnd = pathStart < 0 ? pathEnd : pathStart;
        if (authorityEnd == authorityStart)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_HOST_MISSING",
                "A GS1 Digital Link URI must contain a host name.",
                "Host",
                authorityStart,
                "a non-empty URI host",
                "empty");
            return parts;
        }

        if (pathStart < 0 || pathStart >= pathEnd)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_PATH_MISSING",
                "A GS1 Digital Link URI must contain GS1 path information.",
                "Path",
                authorityEnd,
                "a primary-key AI and value path",
                "empty");
            return parts;
        }

        parts.Success = true;
        parts.PathStart = pathStart;
        parts.PathEnd = pathEnd;
        parts.QueryStart = queryStart;
        parts.QueryEnd = uriEnd;
        return parts;
    }

    static global::System.Collections.Generic.List<Gs1DigitalLinkSegment> ReadPathSegments(
        string source,
        int pathStart,
        int pathEnd)
    {
        global::System.Collections.Generic.List<Gs1DigitalLinkSegment> segments =
            new global::System.Collections.Generic.List<Gs1DigitalLinkSegment>();
        int segmentStart = pathStart + 1;
        int offset = segmentStart;
        while (offset <= pathEnd)
        {
            if (offset == pathEnd || source[offset] == '/')
            {
                Gs1DigitalLinkSegment segment =
                    new Gs1DigitalLinkSegment();
                segment.Text = source.Substring(
                    segmentStart,
                    offset - segmentStart);
                segment.Offset = segmentStart;
                segments.Add(segment);
                segmentStart = offset + 1;
            }

            offset = offset + 1;
        }

        return segments;
    }

    static int FindPrimarySegment(
        global::System.Collections.Generic.List<Gs1DigitalLinkSegment> segments)
    {
        int index = 0;
        while (index < segments.Count - 1)
        {
            string identifier = segments[index].Text;
            Gs1AiDefinition? definition = IsAllDigits(identifier)
                ? Gs1AiResolver.ResolveDefinition(identifier)
                : null;
            if (definition != null && definition.DigitalLinkPrimaryKey)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static Gs1DigitalLinkDecodedValue DecodeUriValue(
        string rawValue,
        int sourceOffset,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1DigitalLinkDecodedValue result =
            new Gs1DigitalLinkDecodedValue();
        global::System.Text.StringBuilder decoded =
            new global::System.Text.StringBuilder();
        global::System.Collections.Generic.List<int> sourceOffsets =
            new global::System.Collections.Generic.List<int>();
        int index = 0;
        while (index < rawValue.Length)
        {
            char character = rawValue[index];
            if (character == '%')
            {
                if (index + 2 >= rawValue.Length
                    || HexadecimalValue(rawValue[index + 1]) < 0
                    || HexadecimalValue(rawValue[index + 2]) < 0)
                {
                    AddDigitalLinkIssue(
                        issues,
                        "GS1_DIGITAL_LINK_PERCENT_ENCODING_INVALID",
                        "A percent-encoded URI character must contain two hexadecimal digits.",
                        "PercentEncoding",
                        sourceOffset + index,
                        "% followed by two hexadecimal digits",
                        rawValue.Substring(index));
                    return result;
                }

                int numeric = HexadecimalValue(rawValue[index + 1]) * 16
                    + HexadecimalValue(rawValue[index + 2]);
                if (numeric > 127)
                {
                    AddDigitalLinkIssue(
                        issues,
                        "GS1_DIGITAL_LINK_NON_ASCII_VALUE",
                        "GS1 Digital Link AI values must use the permitted ISO/IEC 646 repertoire.",
                        "Value",
                        sourceOffset + index,
                        "an ASCII percent-encoded character",
                        rawValue.Substring(index, 3));
                    return result;
                }

                decoded.Append((char)numeric);
                sourceOffsets.Add(sourceOffset + index);
                index = index + 3;
                continue;
            }

            if (character > 127)
            {
                AddDigitalLinkIssue(
                    issues,
                    "GS1_DIGITAL_LINK_NON_ASCII_VALUE",
                    "GS1 Digital Link AI values must use the permitted ISO/IEC 646 repertoire.",
                    "Value",
                    sourceOffset + index,
                    "an ASCII URI character",
                    ((int)character).ToString());
                return result;
            }

            if (!IsDirectUriValueCharacter(character))
            {
                AddDigitalLinkIssue(
                    issues,
                    "GS1_DIGITAL_LINK_VALUE_NOT_PERCENT_ENCODED",
                    "A reserved GS1 value character must be percent-encoded in a Digital Link URI.",
                    "PercentEncoding",
                    sourceOffset + index,
                    "a percent-encoded reserved character",
                    character.ToString());
                return result;
            }

            decoded.Append(character);
            sourceOffsets.Add(sourceOffset + index);
            index = index + 1;
        }

        result.Success = true;
        result.Text = decoded.ToString();
        result.SourceOffsets = sourceOffsets.ToArray();
        return result;
    }

    static int HexadecimalValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    static Gs1Element? ParseElementPair(
        string identifier,
        Gs1DigitalLinkDecodedValue decoded,
        int identifierOffset,
        int valueOffset,
        int rawValueLength,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString(
                identifier + decoded.Text);
        int issueIndex = 0;
        while (issueIndex < parsed.Issues.Length)
        {
            BarcodeValidationIssue sourceIssue = parsed.Issues[issueIndex];
            int mappedOffset = MapParsedIssueOffset(
                sourceIssue.Offset,
                identifier.Length,
                identifierOffset,
                valueOffset,
                rawValueLength,
                decoded.SourceOffsets);
            CopyDigitalLinkIssue(issues, sourceIssue, mappedOffset);
            issueIndex = issueIndex + 1;
        }

        if (parsed.Elements.Length != 1
            || parsed.Elements[0].ApplicationIdentifier != identifier)
        {
            return null;
        }

        Gs1Element element = parsed.Elements[0];
        element.SourceOffset = identifierOffset;
        element.SourceLength = valueOffset + rawValueLength
            - identifierOffset;
        return element;
    }

    static int MapParsedIssueOffset(
        int issueOffset,
        int identifierLength,
        int identifierOffset,
        int valueOffset,
        int rawValueLength,
        int[] decodedOffsets)
    {
        if (issueOffset < 0)
        {
            return issueOffset;
        }

        if (issueOffset < identifierLength)
        {
            return identifierOffset + issueOffset;
        }

        int valueIndex = issueOffset - identifierLength;
        if (valueIndex >= 0 && valueIndex < decodedOffsets.Length)
        {
            return decodedOffsets[valueIndex];
        }

        return valueOffset + rawValueLength;
    }

    static void ValidateParsedPathQualifiers(
        global::System.Collections.Generic.List<Gs1Element> elements,
        Gs1AiDefinition primaryDefinition,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        if (elements.Count == 0)
        {
            return;
        }

        global::System.Collections.Generic.List<Gs1Element> qualifiers =
            new global::System.Collections.Generic.List<Gs1Element>();
        int index = 1;
        while (index < elements.Count)
        {
            qualifiers.Add(elements[index]);
            index = index + 1;
        }

        if (QualifierSequenceMatches(qualifiers, primaryDefinition))
        {
            return;
        }

        Gs1Element invalid = qualifiers.Count == 0
            ? elements[0]
            : qualifiers[qualifiers.Count - 1];
        AddDigitalLinkIssue(
            issues,
            "GS1_DIGITAL_LINK_PATH_QUALIFIERS_INVALID",
            "The URI path qualifiers do not form an allowed sequence for the primary key.",
            invalid.ApplicationIdentifier == null
                ? "Path"
                : invalid.ApplicationIdentifier,
            invalid.SourceOffset,
            FormatQualifierSequences(primaryDefinition.DigitalLinkQualifierSequences),
            FormatElementIdentifiers(qualifiers));
    }

    static bool QualifierSequenceMatches(
        global::System.Collections.Generic.List<Gs1Element> qualifiers,
        Gs1AiDefinition definition)
    {
        if (qualifiers.Count == 0)
        {
            return true;
        }

        int sequenceIndex = 0;
        while (sequenceIndex < definition.DigitalLinkQualifierSequences.Length)
        {
            string[] sequence =
                definition.DigitalLinkQualifierSequences[sequenceIndex];
            int qualifierIndex = 0;
            int patternIndex = 0;
            while (qualifierIndex < qualifiers.Count
                && patternIndex < sequence.Length)
            {
                if (Gs1AiResolver.MatchesPattern(
                    qualifiers[qualifierIndex].ApplicationIdentifier,
                    sequence[patternIndex]))
                {
                    qualifierIndex = qualifierIndex + 1;
                }

                patternIndex = patternIndex + 1;
            }

            if (qualifierIndex == qualifiers.Count)
            {
                return true;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        return false;
    }

    static void ParseQueryAttributes(
        string source,
        int queryStart,
        int queryEnd,
        Gs1AiDefinition primaryDefinition,
        global::System.Collections.Generic.List<Gs1Element> elements,
        global::System.Collections.Generic.List<string> ignoredParameters,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int parameterStart = queryStart;
        int offset = queryStart;
        while (offset <= queryEnd)
        {
            if (offset == queryEnd
                || source[offset] == '&'
                || source[offset] == ';')
            {
                ParseQueryParameter(
                    source,
                    parameterStart,
                    offset,
                    primaryDefinition,
                    elements,
                    ignoredParameters,
                    issues);
                parameterStart = offset + 1;
            }

            offset = offset + 1;
        }
    }

    static void ParseQueryParameter(
        string source,
        int parameterStart,
        int parameterEnd,
        Gs1AiDefinition primaryDefinition,
        global::System.Collections.Generic.List<Gs1Element> elements,
        global::System.Collections.Generic.List<string> ignoredParameters,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        string parameter = source.Substring(
            parameterStart,
            parameterEnd - parameterStart);
        if (parameter.Length == 0)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_QUERY_PARAMETER_EMPTY",
                "A GS1 Digital Link query string must not contain an empty parameter.",
                "Query",
                parameterStart,
                "a key=value query parameter",
                "empty");
            return;
        }

        int equalsOffset = parameter.IndexOf('=');
        string key = equalsOffset < 0
            ? parameter
            : parameter.Substring(0, equalsOffset);
        if (!IsAllDigits(key))
        {
            ignoredParameters.Add(parameter);
            return;
        }

        if (equalsOffset < 0 || equalsOffset == parameter.Length - 1)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_QUERY_VALUE_MISSING",
                "A numeric GS1 query parameter must contain a non-empty value.",
                key,
                parameterStart,
                "AI=value",
                parameter);
            return;
        }

        Gs1AiDefinition? definition =
            Gs1AiResolver.ResolveDefinition(key);
        if (definition == null)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_QUERY_AI_UNKNOWN",
                "A numeric GS1 query key must be a registered Application Identifier.",
                "ApplicationIdentifier",
                parameterStart,
                "a registered GS1 data-attribute AI",
                key);
            return;
        }

        if (IsQualifierIdentifier(key, primaryDefinition))
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_QUALIFIER_IN_QUERY",
                "A key qualifier for the selected primary key must appear in the URI path.",
                key,
                parameterStart,
                "the key qualifier in path information",
                key);
            return;
        }

        if (!definition.DigitalLinkDataAttribute)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_QUERY_ATTRIBUTE_NOT_PERMITTED",
                "The Application Identifier is not permitted as a Digital Link query attribute.",
                key,
                parameterStart,
                "a GS1 Digital Link data-attribute AI",
                key);
            return;
        }

        int rawValueOffset = parameterStart + equalsOffset + 1;
        string rawValue = parameter.Substring(equalsOffset + 1);
        Gs1DigitalLinkDecodedValue decoded = DecodeUriValue(
            rawValue,
            rawValueOffset,
            issues);
        if (!decoded.Success)
        {
            return;
        }

        Gs1Element? element = ParseElementPair(
            key,
            decoded,
            parameterStart,
            rawValueOffset,
            rawValue.Length,
            issues);
        if (element != null)
        {
            elements.Add(element);
        }
    }

    static bool IsQualifierIdentifier(
        string identifier,
        Gs1AiDefinition primaryDefinition)
    {
        int sequenceIndex = 0;
        while (sequenceIndex < primaryDefinition.DigitalLinkQualifierSequences.Length)
        {
            string[] sequence =
                primaryDefinition.DigitalLinkQualifierSequences[sequenceIndex];
            int index = 0;
            while (index < sequence.Length)
            {
                if (Gs1AiResolver.MatchesPattern(
                    identifier,
                    sequence[index]))
                {
                    return true;
                }

                index = index + 1;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        return false;
    }

    static void ValidateBuildElements(
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element == null)
            {
                AddDigitalLinkIssue(
                    issues,
                    "GS1_DIGITAL_LINK_ELEMENT_NULL",
                    "A GS1 Digital Link element must not be null.",
                    "Elements",
                    -1,
                    "a GS1 element",
                    "null");
                index = index + 1;
                continue;
            }

            string identifier = element.ApplicationIdentifier == null
                ? ""
                : element.ApplicationIdentifier;
            string value = element.Value == null ? "" : element.Value;
            Gs1ElementStringParseResult parsed =
                Gs1ElementStringParser.ParseElementString(identifier + value);
            AppendIssues(issues, parsed.Issues);
            if (Gs1AiResolver.ResolveDefinition(identifier) == null)
            {
                AddDigitalLinkIssue(
                    issues,
                    "GS1_DIGITAL_LINK_BUILD_AI_INVALID",
                    "A GS1 Digital Link element must contain a registered Application Identifier.",
                    "ApplicationIdentifier",
                    element.SourceOffset,
                    "a registered GS1 Application Identifier",
                    identifier);
            }

            index = index + 1;
        }
    }

    static Gs1Element? FindBuildPrimary(
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1Element? primary = null;
        int count = 0;
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
                if (primary == null)
                {
                    primary = element;
                }

                count = count + 1;
            }

            index = index + 1;
        }

        if (count == 0)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_PRIMARY_KEY_MISSING",
                "A GS1 Digital Link URI requires one primary key.",
                "DigitalLinkPrimaryKey",
                -1,
                "exactly one GS1 Digital Link primary key",
                "none");
            return null;
        }

        if (count > 1)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_MULTIPLE_PRIMARY_KEYS",
                "A GS1 Digital Link URI cannot be built with multiple primary keys.",
                "DigitalLinkPrimaryKey",
                primary == null ? -1 : primary.SourceOffset,
                "exactly one GS1 Digital Link primary key",
                count.ToString());
            return null;
        }

        return primary;
    }

    static global::System.Collections.Generic.List<Gs1Element> OrderBuildQualifiers(
        Gs1Element[] elements,
        Gs1Element primary,
        Gs1AiDefinition primaryDefinition,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        global::System.Collections.Generic.List<Gs1Element> candidates =
            new global::System.Collections.Generic.List<Gs1Element>();
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element != null
                && element != primary
                && IsQualifierIdentifier(
                    element.ApplicationIdentifier == null
                        ? ""
                        : element.ApplicationIdentifier,
                    primaryDefinition))
            {
                candidates.Add(element);
            }

            index = index + 1;
        }

        global::System.Collections.Generic.List<Gs1Element> ordered =
            new global::System.Collections.Generic.List<Gs1Element>();
        if (candidates.Count == 0)
        {
            return ordered;
        }

        string[]? selectedSequence = null;
        int sequenceIndex = 0;
        while (sequenceIndex < primaryDefinition.DigitalLinkQualifierSequences.Length)
        {
            string[] sequence =
                primaryDefinition.DigitalLinkQualifierSequences[sequenceIndex];
            if (SequenceContainsCandidates(sequence, candidates))
            {
                selectedSequence = sequence;
                break;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        if (selectedSequence == null)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_QUALIFIER_SET_INVALID",
                "The supplied key qualifiers do not belong to one allowed qualifier sequence.",
                "DigitalLinkQualifier",
                candidates[0].SourceOffset,
                FormatQualifierSequences(
                    primaryDefinition.DigitalLinkQualifierSequences),
                FormatElementIdentifiers(candidates));
            return ordered;
        }

        index = 0;
        while (index < selectedSequence.Length)
        {
            int candidateIndex = 0;
            while (candidateIndex < candidates.Count)
            {
                if (Gs1AiResolver.MatchesPattern(
                    candidates[candidateIndex].ApplicationIdentifier,
                    selectedSequence[index]))
                {
                    ordered.Add(candidates[candidateIndex]);
                }

                candidateIndex = candidateIndex + 1;
            }

            index = index + 1;
        }

        return ordered;
    }

    static bool SequenceContainsCandidates(
        string[] sequence,
        global::System.Collections.Generic.List<Gs1Element> candidates)
    {
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            bool found = false;
            int sequenceIndex = 0;
            while (sequenceIndex < sequence.Length)
            {
                if (Gs1AiResolver.MatchesPattern(
                    candidates[candidateIndex].ApplicationIdentifier,
                    sequence[sequenceIndex]))
                {
                    found = true;
                    break;
                }

                sequenceIndex = sequenceIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            candidateIndex = candidateIndex + 1;
        }

        return true;
    }

    static global::System.Collections.Generic.List<Gs1Element> CollectBuildAttributes(
        Gs1Element[] elements,
        Gs1Element primary,
        global::System.Collections.Generic.List<Gs1Element> qualifiers,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        global::System.Collections.Generic.List<Gs1Element> attributes =
            new global::System.Collections.Generic.List<Gs1Element>();
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element == null
                || element == primary
                || ContainsElementReference(qualifiers, element))
            {
                index = index + 1;
                continue;
            }

            Gs1AiDefinition? definition =
                Gs1AiResolver.ResolveDefinition(
                    element.ApplicationIdentifier);
            if (definition == null || !definition.DigitalLinkDataAttribute)
            {
                string identifier = element.ApplicationIdentifier == null
                    ? ""
                    : element.ApplicationIdentifier;
                AddDigitalLinkIssue(
                    issues,
                    "GS1_DIGITAL_LINK_QUERY_ATTRIBUTE_NOT_PERMITTED",
                    "The Application Identifier is not permitted as a Digital Link query attribute.",
                    identifier.Length == 0 ? "ApplicationIdentifier" : identifier,
                    element.SourceOffset,
                    "a GS1 Digital Link data-attribute AI",
                    identifier);
            }
            else
            {
                attributes.Add(element);
            }

            index = index + 1;
        }

        return attributes;
    }

    static bool ContainsElementReference(
        global::System.Collections.Generic.List<Gs1Element> elements,
        Gs1Element candidate)
    {
        int index = 0;
        while (index < elements.Count)
        {
            if (elements[index] == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void SortBuildAttributes(
        global::System.Collections.Generic.List<Gs1Element> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            Gs1Element current = attributes[index];
            string currentIdentifier = current.ApplicationIdentifier == null
                ? ""
                : current.ApplicationIdentifier;
            int previousIndex = index - 1;
            while (previousIndex >= 0)
            {
                string? sourcePreviousIdentifier =
                    attributes[previousIndex].ApplicationIdentifier;
                string previousIdentifier = sourcePreviousIdentifier == null
                    ? ""
                    : sourcePreviousIdentifier;
                if (string.CompareOrdinal(
                    previousIdentifier,
                    currentIdentifier) <= 0)
                {
                    break;
                }

                attributes[previousIndex + 1] = attributes[previousIndex];
                previousIndex = previousIndex - 1;
            }

            attributes[previousIndex + 1] = current;
            index = index + 1;
        }
    }

    static string NormalizeBaseUri(
        string? baseUri,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        string normalized = baseUri == null || baseUri.Length == 0
            ? CanonicalBaseUri
            : baseUri;
        while (normalized.Length > 0
            && normalized[normalized.Length - 1] == '/')
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }

        int schemeEnd = normalized.IndexOf("://");
        string scheme = schemeEnd < 0
            ? ""
            : normalized.Substring(0, schemeEnd);
        int authorityStart = schemeEnd + 3;
        int authorityEnd = schemeEnd < 0
            ? -1
            : normalized.IndexOf('/', authorityStart);
        if (authorityEnd < 0)
        {
            authorityEnd = normalized.Length;
        }

        bool validScheme = scheme == "http"
            || scheme == "https"
            || scheme == "HTTP"
            || scheme == "HTTPS";
        bool validAuthority = schemeEnd >= 0
            && authorityEnd > authorityStart;
        bool validSuffix = normalized.IndexOf('?') < 0
            && normalized.IndexOf('#') < 0;
        if (!validScheme || !validAuthority || !validSuffix)
        {
            AddDigitalLinkIssue(
                issues,
                "GS1_DIGITAL_LINK_BASE_URI_INVALID",
                "The Digital Link base URI must be an HTTP or HTTPS URI stem without a query or fragment.",
                "DigitalLinkBaseUri",
                -1,
                "an HTTP or HTTPS URI stem",
                normalized);
            return "";
        }

        return normalized;
    }

    static void AppendPathElement(
        global::System.Text.StringBuilder text,
        Gs1Element element)
    {
        string identifier = element.ApplicationIdentifier == null
            ? ""
            : element.ApplicationIdentifier;
        string value = element.Value == null ? "" : element.Value;
        text.Append('/');
        text.Append(identifier);
        text.Append('/');
        text.Append(EncodeUriValue(value));
    }

    static bool IsDirectUriValueCharacter(char character)
    {
        return (character >= '0' && character <= '9')
            || (character >= 'A' && character <= 'Z')
            || (character >= 'a' && character <= 'z')
            || character == '-'
            || character == '.'
            || character == '_';
    }

    static bool IsAllDigits(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static string FormatQualifierSequences(string[][] sequences)
    {
        if (sequences.Length == 0)
        {
            return "no key qualifiers";
        }

        string text = "";
        int sequenceIndex = 0;
        while (sequenceIndex < sequences.Length)
        {
            if (sequenceIndex > 0)
            {
                text = text + " or ";
            }

            int valueIndex = 0;
            while (valueIndex < sequences[sequenceIndex].Length)
            {
                if (valueIndex > 0)
                {
                    text = text + "+";
                }

                text = text + sequences[sequenceIndex][valueIndex];
                valueIndex = valueIndex + 1;
            }

            sequenceIndex = sequenceIndex + 1;
        }

        return text;
    }

    static string FormatElementIdentifiers(
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

        return text.Length == 0 ? "none" : text;
    }

    static void AppendElements(
        global::System.Collections.Generic.List<Gs1Element> destination,
        global::System.Collections.Generic.List<Gs1Element> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static void AppendIssues(
        global::System.Collections.Generic.List<BarcodeValidationIssue> destination,
        BarcodeValidationIssue[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static void CopyDigitalLinkIssue(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        BarcodeValidationIssue source,
        int offset)
    {
        BarcodeValidationIssue issue = ValidationIssueFactory.CopyWithOffset(
            source,
            offset);
        issues.Add(issue);
    }

    static void AddDigitalLinkIssue(
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
            BarcodeStandards.Gs1DigitalLinkUriSyntax,
            field,
            offset,
            expected,
            observed);
        issues.Add(issue);
    }

    static void SortDigitalLinkIssues(
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

    static Gs1Message CreateDigitalLinkMessage(
        string source,
        global::System.Collections.Generic.List<Gs1Element> elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1Message message = new Gs1Message();
        message.Elements = elements.ToArray();
        message.Syntax = Gs1Syntax.DigitalLinkUri;
        message.SourceText = source;
        message.Issues = issues.ToArray();
        return message;
    }

    static Gs1DigitalLinkParseResult CreateParseResult(
        string? source,
        string baseUri,
        global::System.Collections.Generic.List<Gs1Element> elements,
        global::System.Collections.Generic.List<string> ignoredParameters,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1DigitalLinkParseResult result =
            new Gs1DigitalLinkParseResult();
        result.Success = issues.Count == 0;
        result.Message = CreateDigitalLinkMessage(
            source == null ? "" : source,
            elements,
            issues);
        result.BaseUri = baseUri;
        result.IgnoredQueryParameters = ignoredParameters.ToArray();
        result.Issues = issues.ToArray();
        return result;
    }

    static Gs1DigitalLinkBuildResult CreateBuildResult(
        string text,
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1DigitalLinkBuildResult result =
            new Gs1DigitalLinkBuildResult();
        result.Success = issues.Count == 0;
        result.Text = text;
        result.Elements = elements;
        result.Issues = issues.ToArray();
        return result;
    }
}
