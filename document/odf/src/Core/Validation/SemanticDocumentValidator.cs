using System;
using System.Collections.Generic;

namespace odf;

internal class SemanticDocumentValidator
{
    OdfValidationOptions Options;
    List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();
    List<SemanticNodeRecord> Records = new List<SemanticNodeRecord>();
    Dictionary<OdfNode, SemanticNodeRecord> RecordsByNode =
        new Dictionary<OdfNode, SemanticNodeRecord>();
    Dictionary<string, SemanticNodeRecord> Styles =
        new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
    Dictionary<string, SemanticNodeRecord> Masters =
        new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
    Dictionary<string, SemanticNodeRecord> PageLayouts =
        new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
    Dictionary<string, SemanticNodeRecord> PresentationLayouts =
        new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
    Dictionary<string, SemanticNodeRecord> Tables =
        new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
    Dictionary<string, SemanticNodeRecord> Changes =
        new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
    Dictionary<string, SemanticNodeRecord> NamedTargets =
        new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
    Dictionary<string, SemanticNodeRecord> XmlIds =
        new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
    Dictionary<string, bool> PackagePaths =
        new Dictionary<string, bool>(StringComparer.Ordinal);
    Dictionary<string, bool> MaterializedParts =
        new Dictionary<string, bool>(StringComparer.Ordinal);
    HashSet<OdfNode> MergeDiagnostics = new HashSet<OdfNode>();
    bool Stop;
    int ValidatedNodeCount;
    OdfVersion Version;
    OdfPackageKind PackageKind;

    internal SemanticDocumentValidator(OdfValidationOptions options)
    {
        if (!OptionsAreValid(options))
        {
            throw new OdfValidationException(
                "Positive bounded semantic validation limits are required.");
        }
        Options = options;
    }

    internal SemanticValidationResult ValidateDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfValidationException(
                "A document is required for semantic validation.");
        }
        ResetState(document);
        if (Options.ValidateSemantics)
        {
            ValidateCancellation();
            IndexDocument(document);
            ValidateStyleRelationshipCycles(document);
            ValidateRecords();
            ValidateTableMerges();
            ValidateTypedFormulas(document);
        }
        SemanticValidationResult result = new SemanticValidationResult();
        result.Diagnostics = Diagnostics.ToArray();
        result.ValidatedNodeCount = ValidatedNodeCount;
        result.StoppedEarly = Stop;
        result.IsValid = !HasErrors();
        return result;
    }

    void ValidateStyleRelationshipCycles(OdfDocument document)
    {
        OdfDiagnostic[] diagnostics =
            StyleResolver.ValidateRelationships(document);
        int index = 0;
        while (index < diagnostics.Length && !Stop)
        {
            Diagnostics.Add(diagnostics[index]);
            if (Options.Mode == OdfValidationMode.Strict)
            {
                Stop = true;
            }
            index = index + 1;
        }
    }

    void ResetState(OdfDocument document)
    {
        Diagnostics = new List<OdfDiagnostic>();
        Records = new List<SemanticNodeRecord>();
        RecordsByNode = new Dictionary<OdfNode, SemanticNodeRecord>();
        Styles = new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        Masters = new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        PageLayouts = new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        PresentationLayouts = new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        Tables = new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        Changes = new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        NamedTargets = new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        XmlIds = new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        PackagePaths = new Dictionary<string, bool>(StringComparer.Ordinal);
        MaterializedParts = new Dictionary<string, bool>(StringComparer.Ordinal);
        MergeDiagnostics = new HashSet<OdfNode>();
        Stop = false;
        ValidatedNodeCount = 0;
        Version = SelectVersion(document);
        PackageKind = document.PackageKind;
    }

    void IndexDocument(OdfDocument document)
    {
        int entryIndex = 0;
        while (entryIndex < document.PackageEntries.Length)
        {
            OdfPackageEntry entry = document.PackageEntries[entryIndex];
            string path = entry.CanonicalPath;
            if (path.Length == 0)
            {
                path = entry.Path;
            }
            if (path.Length > 0 && !PackagePaths.ContainsKey(path))
            {
                PackagePaths.Add(path, true);
            }
            entryIndex = entryIndex + 1;
        }
        int partIndex = 0;
        while (partIndex < document.Parts.Length && !Stop)
        {
            OdfPart part = document.Parts[partIndex];
            if (!MaterializedParts.ContainsKey(part.Path))
            {
                MaterializedParts.Add(part.Path, true);
            }
            int nodeIndex = 0;
            while (nodeIndex < part.Nodes.Length && !Stop)
            {
                OdfNode node = part.Nodes[nodeIndex];
                IndexNode(
                    node,
                    part.Path,
                    SemanticPath("", node, nodeIndex + 1));
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
    }

    void IndexNode(OdfNode node, string partPath, string nodePath)
    {
        if (RecordsByNode.ContainsKey(node))
        {
            return;
        }
        SemanticNodeRecord record = new SemanticNodeRecord();
        record.Node = node;
        record.PartPath = partPath;
        record.NodePath = nodePath;
        Records.Add(record);
        RecordsByNode.Add(node, record);
        if (node.Kind == OdfNodeKind.Element && node.Name != null)
        {
            IndexDeclarations(record);
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length && !Stop)
        {
            OdfNode child = node.Children[childIndex];
            IndexNode(
                child,
                partPath,
                SemanticPath(
                    nodePath,
                    child,
                    SemanticSiblingNumber(node, childIndex)));
            childIndex = childIndex + 1;
        }
    }

    void IndexDeclarations(SemanticNodeRecord record)
    {
        OdfNode node = record.Node;
        OdfAttribute? styleName = FindAttribute(
            node,
            OdfNamespaces.Style,
            "name");
        if (styleName != null && styleName.Value.Length > 0)
        {
            AddFirstSymbol(Styles, styleName.Value, record);
        }
        if (IsElement(node, OdfNamespaces.Style, "master-page"))
        {
            AddNamedSymbol(Masters, styleName, record);
        }
        else if (IsElement(node, OdfNamespaces.Style, "page-layout"))
        {
            AddNamedSymbol(PageLayouts, styleName, record);
        }
        else if (IsElement(
            node,
            OdfNamespaces.Presentation,
            "presentation-page-layout"))
        {
            AddNamedSymbol(PresentationLayouts, styleName, record);
        }
        if (IsElement(node, OdfNamespaces.Table, "table"))
        {
            AddNamedSymbol(
                Tables,
                FindAttribute(node, OdfNamespaces.Table, "name"),
                record);
        }
        if (IsElement(node, OdfNamespaces.Text, "changed-region"))
        {
            OdfAttribute? changeId = FindAttribute(
                node,
                OdfNamespaces.Text,
                "id");
            if (changeId == null)
            {
                changeId = FindAttribute(node, OdfNamespaces.Xml, "id");
            }
            AddNamedSymbol(Changes, changeId, record);
        }
        OdfAttribute? xmlId = FindAttribute(node, OdfNamespaces.Xml, "id");
        if (xmlId != null && xmlId.Value.Length > 0)
        {
            AddFirstSymbol(
                XmlIds,
                PartSymbolKey(record.PartPath, xmlId.Value),
                record);
            AddFirstSymbol(NamedTargets, xmlId.Value, record);
        }
        IndexGenericNames(record);
    }

    void IndexGenericNames(SemanticNodeRecord record)
    {
        foreach (OdfAttribute attribute in record.Node.Attributes)
        {
            string localName = attribute.Name.LocalName;
            if ((localName == "name"
                    || localName == "id"
                    || localName == "control-id")
                && attribute.Value.Length > 0)
            {
                AddFirstSymbol(NamedTargets, attribute.Value, record);
            }
        }
    }

    void ValidateRecords()
    {
        int index = 0;
        while (index < Records.Count && !Stop)
        {
            ValidateCancellation();
            if (Stop)
            {
                return;
            }
            SemanticNodeRecord record = Records[index];
            if (record.Node.Kind == OdfNodeKind.Element
                && record.Node.Name != null)
            {
                ValidatedNodeCount = ValidatedNodeCount + 1;
                ValidateNodeAttributes(record);
                ValidateCellValue(record);
                ValidateChartClass(record);
            }
            index = index + 1;
        }
        ValidateChangeBoundaries();
    }

    void ValidateNodeAttributes(SemanticNodeRecord record)
    {
        int index = 0;
        while (index < record.Node.Attributes.Length && !Stop)
        {
            OdfAttribute attribute = record.Node.Attributes[index];
            string localName = attribute.Name.LocalName;
            if (IsMasterReference(localName))
            {
                ValidateMasterReference(record, attribute);
            }
            else if (IsPageLayoutReference(localName))
            {
                ValidateLayoutReference(record, attribute);
            }
            else if (IsStyleReference(localName))
            {
                ValidateStyleReference(record, attribute);
            }
            else if (localName == "change-id"
                && attribute.Name.NamespaceName == OdfNamespaces.Text)
            {
                ValidateChangeReference(record, attribute);
            }
            else if (IsGenericIdReference(localName))
            {
                ValidateIdReference(record, attribute);
            }
            if (!Stop && IsRangeAttribute(localName))
            {
                ValidateRangeAttribute(record, attribute);
            }
            if (!Stop
                && localName == "formula"
                && attribute.Name.NamespaceName == OdfNamespaces.Table
                && Options.ValidateFormulas)
            {
                ValidateFormulaAttribute(record, attribute);
            }
            if (!Stop
                && attribute.Name.NamespaceName == OdfNamespaces.XLink
                && localName == "href")
            {
                ValidateHref(record, attribute);
            }
            index = index + 1;
        }
    }

    void ValidateStyleReference(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        OdfStyleNameValue parsed =
            OdfStyleNameRules.ParseStyleNameReference(attribute.Value);
        if (!parsed.IsValid)
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.InvalidStyleReference,
                "The style reference is not a valid style name.",
                record,
                attribute,
                null);
            return;
        }
        if (attribute.Value.Length > 0 && !Styles.ContainsKey(attribute.Value))
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.InvalidStyleReference,
                "The referenced style is not declared in the document.",
                record,
                attribute,
                null);
        }
    }

    void ValidateMasterReference(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        ValidateNamedReference(
            record,
            attribute,
            Masters,
            "The referenced master page is not declared.");
    }

    void ValidateLayoutReference(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        Dictionary<string, SemanticNodeRecord> targets = PageLayouts;
        if (attribute.Name.LocalName == "presentation-page-layout-name")
        {
            targets = PresentationLayouts;
        }
        ValidateNamedReference(
            record,
            attribute,
            targets,
            "The referenced page layout is not declared.");
    }

    void ValidateNamedReference(
        SemanticNodeRecord record,
        OdfAttribute attribute,
        Dictionary<string, SemanticNodeRecord> targets,
        string message)
    {
        if (attribute.Value.Length == 0 || !targets.ContainsKey(attribute.Value))
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.BrokenReference,
                message,
                record,
                attribute,
                null);
        }
    }

    void ValidateChangeReference(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        if (attribute.Value.Length == 0 || !Changes.ContainsKey(attribute.Value))
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.BrokenReference,
                "The tracked-change boundary references an undeclared changed region.",
                record,
                attribute,
                null);
        }
    }

    void ValidateIdReference(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        string[] targets = attribute.Value.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries);
        if (targets.Length == 0)
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.BrokenReference,
                "The XML target reference is empty.",
                record,
                attribute,
                null);
            return;
        }
        int index = 0;
        while (index < targets.Length && !Stop)
        {
            string target = targets[index];
            bool found = NamedTargets.ContainsKey(target)
                || XmlIds.ContainsKey(PartSymbolKey(record.PartPath, target));
            if (!found)
            {
                AddRecordDiagnostic(
                    OdfDiagnosticCodes.BrokenReference,
                    "The XML target reference cannot be resolved.",
                    record,
                    attribute,
                    null);
            }
            index = index + 1;
        }
    }

    void ValidateRangeAttribute(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        string localName = attribute.Name.LocalName;
        if (localName == "print-ranges")
        {
            OdfCellRangeListValue ranges =
                OdfCellAddressParser.ParseRangeList(attribute.Value);
            if (!ranges.IsValid)
            {
                AddInvalidRange(record, attribute);
                return;
            }
            int index = 0;
            while (index < ranges.Ranges.Length && !Stop)
            {
                ValidateRangeTables(record, attribute, ranges.Ranges[index]);
                index = index + 1;
            }
            return;
        }
        OdfCellRangeValue range;
        if (localName.EndsWith("cell-address", StringComparison.Ordinal)
            && !localName.Contains("range", StringComparison.Ordinal))
        {
            OdfCellAddressValue address =
                OdfCellAddressParser.ParseCellAddress(attribute.Value);
            if (!address.IsValid)
            {
                AddInvalidRange(record, attribute);
                return;
            }
            ValidateAddressTable(record, attribute, address);
            return;
        }
        range = OdfCellAddressParser.ParseCellRange(attribute.Value);
        if (!range.IsValid)
        {
            AddInvalidRange(record, attribute);
            return;
        }
        ValidateRangeTables(record, attribute, range);
    }

    void ValidateRangeTables(
        SemanticNodeRecord record,
        OdfAttribute attribute,
        OdfCellRangeValue range)
    {
        ValidateAddressTable(record, attribute, range.Start);
        if (!Stop && range.HasEnd)
        {
            ValidateAddressTable(record, attribute, range.End);
        }
    }

    void ValidateAddressTable(
        SemanticNodeRecord record,
        OdfAttribute attribute,
        OdfCellAddressValue address)
    {
        if (address.HasSource || !address.HasSheet)
        {
            return;
        }
        if (!Tables.ContainsKey(address.SheetName))
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.InvalidRangeReference,
                "The cell or range reference names a table that is not declared.",
                record,
                attribute,
                null);
        }
    }

    void AddInvalidRange(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        AddRecordDiagnostic(
            OdfDiagnosticCodes.InvalidRangeReference,
            "The cell or range reference is not valid ODF address syntax.",
            record,
            attribute,
            null);
    }

    void ValidateCellValue(SemanticNodeRecord record)
    {
        OdfAttribute? valueType = FindAttribute(
            record.Node,
            OdfNamespaces.Office,
            "value-type");
        bool hasValue = HasOfficeValueAttribute(record.Node);
        if (valueType == null)
        {
            if (hasValue)
            {
                AddRecordDiagnostic(
                    OdfDiagnosticCodes.ValueTypeMismatch,
                    "A typed value attribute is present without office:value-type.",
                    record,
                    null,
                    null);
            }
            return;
        }
        if (!ValueMatchesType(record.Node, valueType.Value))
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.ValueTypeMismatch,
                "The value attributes do not agree with office:value-type.",
                record,
                valueType,
                null);
        }
    }

    void ValidateFormulaAttribute(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        SemanticFormulaStatus status = CheckFormula(record, attribute.Value);
        if (status.IsValid)
        {
            ValidateFormulaRanges(record, attribute);
            return;
        }
        if (status.LimitExceeded)
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.FormulaDepthLimitExceeded,
                "The formula exceeds the configured nesting depth.",
                record,
                attribute,
                null);
        }
        else
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.InvalidFormula,
                "The formula has invalid namespace, delimiter, or expression syntax.",
                record,
                attribute,
                null);
        }
    }

    SemanticFormulaStatus CheckFormula(
        SemanticNodeRecord record,
        string lexical)
    {
        SemanticFormulaStatus result = new SemanticFormulaStatus();
        if (lexical.Length == 0)
        {
            return result;
        }
        string expression = lexical;
        int separator = lexical.IndexOf(":=", StringComparison.Ordinal);
        if (separator >= 0)
        {
            string prefix = lexical.Substring(0, separator);
            if (!OdfNameRules.IsNcName(prefix))
            {
                return result;
            }
            string namespaceName = ResolvePrefix(record.Node, prefix);
            if (namespaceName != OdfNamespaces.OpenFormula
                && namespaceName != OdfNamespaces.LegacyFormula)
            {
                return result;
            }
            expression = lexical.Substring(separator + 2);
        }
        else if (lexical[0] == '=')
        {
            expression = lexical.Substring(1);
        }
        else
        {
            return result;
        }
        if (expression.Trim().Length == 0)
        {
            return result;
        }
        return CheckFormulaDelimiters(expression);
    }

    void ValidateFormulaRanges(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        string lexical = attribute.Value;
        int expressionStart = 1;
        int assignment = lexical.IndexOf(":=", StringComparison.Ordinal);
        if (assignment >= 0)
        {
            expressionStart = assignment + 2;
        }
        bool quoted = false;
        int index = expressionStart;
        while (index < lexical.Length && !Stop)
        {
            char current = lexical[index];
            if (current == '"')
            {
                if (quoted
                    && index + 1 < lexical.Length
                    && lexical[index + 1] == '"')
                {
                    index = index + 2;
                    continue;
                }
                quoted = !quoted;
            }
            else if (!quoted && current == '[')
            {
                int end = FindFormulaReferenceEnd(lexical, index + 1);
                if (end < 0)
                {
                    return;
                }
                string reference = lexical.Substring(
                    index,
                    end - index + 1);
                OdfCellRangeValue range =
                    OdfCellAddressParser.ParseFormulaReference(reference);
                if (!range.IsValid)
                {
                    AddInvalidRange(record, attribute);
                    return;
                }
                ValidateRangeTables(record, attribute, range);
                index = end;
            }
            index = index + 1;
        }
    }

    int FindFormulaReferenceEnd(string lexical, int start)
    {
        bool quoted = false;
        int index = start;
        while (index < lexical.Length)
        {
            if (lexical[index] == '\'')
            {
                if (quoted
                    && index + 1 < lexical.Length
                    && lexical[index + 1] == '\'')
                {
                    index = index + 2;
                    continue;
                }
                quoted = !quoted;
            }
            else if (!quoted && lexical[index] == ']')
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    SemanticFormulaStatus CheckFormulaDelimiters(string expression)
    {
        SemanticFormulaStatus result = new SemanticFormulaStatus();
        List<char> stack = new List<char>();
        bool quoted = false;
        int index = 0;
        while (index < expression.Length)
        {
            char current = expression[index];
            if (current == '"')
            {
                if (quoted
                    && index + 1 < expression.Length
                    && expression[index + 1] == '"')
                {
                    index = index + 2;
                    continue;
                }
                quoted = !quoted;
            }
            else if (!quoted
                && (current == '(' || current == '{' || current == '['))
            {
                stack.Add(current);
                if (stack.Count > Options.SecurityLimits.MaximumFormulaDepth)
                {
                    result.LimitExceeded = true;
                    return result;
                }
            }
            else if (!quoted
                && (current == ')' || current == '}' || current == ']'))
            {
                if (stack.Count == 0
                    || !DelimitersMatch(stack[stack.Count - 1], current))
                {
                    return result;
                }
                stack.RemoveAt(stack.Count - 1);
            }
            index = index + 1;
        }
        result.IsValid = !quoted && stack.Count == 0;
        return result;
    }

    void ValidateChartClass(SemanticNodeRecord record)
    {
        if (!IsElement(record.Node, OdfNamespaces.Chart, "chart"))
        {
            return;
        }
        OdfAttribute? chartClass = FindAttribute(
            record.Node,
            OdfNamespaces.Chart,
            "class");
        if (chartClass == null)
        {
            return;
        }
        string value = chartClass.Value;
        int separator = value.IndexOf(':');
        if (!OdfNameRules.IsQName(value) || separator <= 0)
        {
            AddInvalidChartClass(record, chartClass);
            return;
        }
        string prefix = value.Substring(0, separator);
        string localName = value.Substring(separator + 1);
        string namespaceName = ResolvePrefix(record.Node, prefix);
        if (namespaceName.Length == 0
            || namespaceName == OdfNamespaces.Chart
                && !IsKnownChartClass(localName))
        {
            AddInvalidChartClass(record, chartClass);
        }
    }

    void AddInvalidChartClass(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        AddRecordDiagnostic(
            OdfDiagnosticCodes.InvalidDatatype,
            "The chart class is not a recognized namespaced chart type.",
            record,
            attribute,
            null);
    }

    void ValidateHref(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        string value = attribute.Value.Trim();
        if (!OdfIriResolver.IsIriReference(value))
        {
            AddResourceDiagnostic(record, attribute);
            return;
        }
        int fragmentIndex = value.IndexOf('#');
        string fragment = "";
        if (fragmentIndex >= 0 && fragmentIndex + 1 < value.Length)
        {
            fragment = value.Substring(fragmentIndex + 1);
        }
        string pathText = fragmentIndex >= 0
            ? value.Substring(0, fragmentIndex)
            : value;
        int queryIndex = pathText.IndexOf('?');
        if (queryIndex >= 0)
        {
            pathText = pathText.Substring(0, queryIndex);
        }
        if (pathText.Length == 0)
        {
            if (fragment.Length == 0
                || !XmlIds.ContainsKey(
                    PartSymbolKey(record.PartPath, fragment)))
            {
                AddRecordDiagnostic(
                    OdfDiagnosticCodes.BrokenReference,
                    "The XML fragment reference cannot be resolved.",
                    record,
                    attribute,
                    null);
            }
            return;
        }
        if (OdfIriResolver.IsAbsoluteIri(value)
            || value.StartsWith("//", StringComparison.Ordinal)
            || PackageKind == OdfPackageKind.Flat)
        {
            return;
        }
        if (!IsResourceElement(record.Node))
        {
            return;
        }
        string resolved = PackagePathRules.ResolveRelativeReference(
            record.PartPath,
            pathText);
        if (resolved.Length == 0 || !PackageTargetExists(resolved))
        {
            AddResourceDiagnostic(record, attribute);
            return;
        }
        if (fragment.Length > 0
            && MaterializedParts.ContainsKey(resolved)
            && !XmlIds.ContainsKey(PartSymbolKey(resolved, fragment)))
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.BrokenReference,
                "The referenced package XML fragment cannot be resolved.",
                record,
                attribute,
                null);
        }
    }

    void AddResourceDiagnostic(
        SemanticNodeRecord record,
        OdfAttribute attribute)
    {
        AddRecordDiagnostic(
            OdfDiagnosticCodes.InvalidResourceReference,
            "The referenced package resource does not exist or has an invalid path.",
            record,
            attribute,
            null);
    }

    void ValidateChangeBoundaries()
    {
        Dictionary<string, int> starts =
            new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<string, int> ends =
            new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<string, SemanticNodeRecord> locations =
            new Dictionary<string, SemanticNodeRecord>(StringComparer.Ordinal);
        int index = 0;
        while (index < Records.Count && !Stop)
        {
            SemanticNodeRecord record = Records[index];
            if (IsElement(record.Node, OdfNamespaces.Text, "change-start"))
            {
                CountChangeBoundary(record, starts, locations);
            }
            else if (IsElement(record.Node, OdfNamespaces.Text, "change-end"))
            {
                CountChangeBoundary(record, ends, locations);
            }
            index = index + 1;
        }
        foreach (string changeId in starts.Keys)
        {
            int endCount = 0;
            if (ends.ContainsKey(changeId))
            {
                endCount = ends[changeId];
            }
            if (starts[changeId] != endCount && !Stop)
            {
                AddRecordDiagnostic(
                    OdfDiagnosticCodes.BrokenReference,
                    "Tracked-change start and end boundaries are not paired.",
                    locations[changeId],
                    null,
                    null);
            }
        }
        foreach (string changeId in ends.Keys)
        {
            if (!starts.ContainsKey(changeId) && !Stop)
            {
                AddRecordDiagnostic(
                    OdfDiagnosticCodes.BrokenReference,
                    "A tracked-change end boundary has no matching start boundary.",
                    locations[changeId],
                    null,
                    null);
            }
        }
    }

    void CountChangeBoundary(
        SemanticNodeRecord record,
        Dictionary<string, int> counts,
        Dictionary<string, SemanticNodeRecord> locations)
    {
        OdfAttribute? attribute = FindAttribute(
            record.Node,
            OdfNamespaces.Text,
            "change-id");
        if (attribute == null || attribute.Value.Length == 0)
        {
            return;
        }
        if (!counts.ContainsKey(attribute.Value))
        {
            counts.Add(attribute.Value, 0);
            if (!locations.ContainsKey(attribute.Value))
            {
                locations.Add(attribute.Value, record);
            }
        }
        counts[attribute.Value] = counts[attribute.Value] + 1;
    }

    void ValidateTableMerges()
    {
        int index = 0;
        while (index < Records.Count && !Stop)
        {
            SemanticNodeRecord record = Records[index];
            if (IsElement(record.Node, OdfNamespaces.Table, "table"))
            {
                ValidateOneTableMerge(record);
            }
            index = index + 1;
        }
    }

    void ValidateOneTableMerge(SemanticNodeRecord table)
    {
        List<OdfNode> rows = new List<OdfNode>();
        CollectTableRows(table.Node, table.Node, rows);
        List<SemanticMergeClaim> claims = new List<SemanticMergeClaim>();
        List<SemanticMergeClaim> matrixClaims =
            new List<SemanticMergeClaim>();
        int rowIndex = 0;
        while (rowIndex < rows.Count && !Stop)
        {
            OdfNode row = rows[rowIndex];
            int column = 0;
            int childIndex = 0;
            while (childIndex < row.Children.Length && !Stop)
            {
                OdfNode cell = row.Children[childIndex];
                if (IsTableCell(cell))
                {
                    int repeat = PositiveAttribute(
                        cell,
                        OdfNamespaces.Table,
                        "number-columns-repeated",
                        1);
                    if (repeat <= 0
                        || repeat > Options.SecurityLimits.MaximumPackageEntries)
                    {
                        AddMergeDiagnostic(cell, "A repeated table cell count is invalid or exceeds the validation bound.");
                        repeat = 1;
                    }
                    int repeatIndex = 0;
                    while (repeatIndex < repeat && !Stop)
                    {
                        ValidateMergeSlot(
                            cell,
                            rowIndex,
                            column,
                            claims,
                            matrixClaims);
                        column = column + 1;
                        repeatIndex = repeatIndex + 1;
                    }
                }
                childIndex = childIndex + 1;
            }
            ValidateMissingMergeSlots(row, rowIndex, column, claims);
            ValidateMissingMatrixSlots(
                row,
                rowIndex,
                column,
                matrixClaims);
            rowIndex = rowIndex + 1;
        }
        int claimIndex = 0;
        while (claimIndex < claims.Count && !Stop)
        {
            SemanticMergeClaim claim = claims[claimIndex];
            if (claim.LastRow >= rows.Count)
            {
                AddMergeDiagnostic(
                    claim.Origin,
                    "A vertically merged cell extends beyond the available table rows.");
            }
            claimIndex = claimIndex + 1;
        }
        claimIndex = 0;
        while (claimIndex < matrixClaims.Count && !Stop)
        {
            SemanticMergeClaim claim = matrixClaims[claimIndex];
            if (claim.LastRow >= rows.Count)
            {
                AddMergeDiagnostic(
                    claim.Origin,
                    "A matrix formula range extends beyond the available table rows.");
            }
            claimIndex = claimIndex + 1;
        }
    }

    void CollectTableRows(
        OdfNode node,
        OdfNode table,
        List<OdfNode> rows)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.Table, "table")
                && !object.ReferenceEquals(child, table))
            {
                index = index + 1;
                continue;
            }
            if (IsElement(child, OdfNamespaces.Table, "table-row"))
            {
                int repeat = PositiveAttribute(
                    child,
                    OdfNamespaces.Table,
                    "number-rows-repeated",
                    1);
                if (repeat <= 0
                    || repeat > Options.SecurityLimits.MaximumPackageEntries)
                {
                    AddMergeDiagnostic(child, "A repeated table row count is invalid or exceeds the validation bound.");
                    repeat = 1;
                }
                int repeatIndex = 0;
                while (repeatIndex < repeat)
                {
                    rows.Add(child);
                    repeatIndex = repeatIndex + 1;
                }
            }
            else
            {
                CollectTableRows(child, table, rows);
            }
            index = index + 1;
        }
    }

    void ValidateMergeSlot(
        OdfNode cell,
        int row,
        int column,
        List<SemanticMergeClaim> claims,
        List<SemanticMergeClaim> matrixClaims)
    {
        bool covered = IsElement(
            cell,
            OdfNamespaces.Table,
            "covered-table-cell");
        bool expectedCovered = SlotIsClaimed(row, column, claims);
        bool expectedMatrix = SlotIsClaimed(
            row,
            column,
            matrixClaims);
        if (covered != expectedCovered)
        {
            AddMergeDiagnostic(
                cell,
                covered
                    ? "A covered table cell is not associated with a spanning cell."
                    : "A spanning table cell is not followed by the required covered cell.");
        }
        if (covered)
        {
            if (ContainsNestedTable(cell))
            {
                AddMergeDiagnostic(
                    cell,
                    "A covered table cell cannot contain a nested table.");
            }
            return;
        }
        int columns = PositiveAttribute(
            cell,
            OdfNamespaces.Table,
            "number-columns-spanned",
            1);
        int rows = PositiveAttribute(
            cell,
            OdfNamespaces.Table,
            "number-rows-spanned",
            1);
        OdfAttribute? matrixColumns = FindAttribute(
            cell,
            OdfNamespaces.Table,
            "number-matrix-columns-spanned");
        OdfAttribute? matrixRows = FindAttribute(
            cell,
            OdfNamespaces.Table,
            "number-matrix-rows-spanned");
        if (matrixColumns != null || matrixRows != null)
        {
            if (matrixColumns == null
                || matrixRows == null
                || FindAttribute(
                    cell,
                    OdfNamespaces.Table,
                    "formula") == null)
            {
                AddMergeDiagnostic(
                    cell,
                    "A matrix span requires both dimensions and a formula.");
                return;
            }
            int matrixColumnCount = PositiveAttribute(
                cell,
                OdfNamespaces.Table,
                "number-matrix-columns-spanned",
                -1);
            int matrixRowCount = PositiveAttribute(
                cell,
                OdfNamespaces.Table,
                "number-matrix-rows-spanned",
                -1);
            if (matrixColumnCount <= 0 || matrixRowCount <= 0)
            {
                AddMergeDiagnostic(
                    cell,
                    "A matrix formula range must use positive integer dimensions.");
                return;
            }
            if (matrixColumnCount
                    > Options.SecurityLimits.MaximumPackageEntries
                || matrixRowCount
                    > Options.SecurityLimits.MaximumPackageEntries)
            {
                AddMergeDiagnostic(
                    cell,
                    "A matrix formula range exceeds the semantic validation bound.");
                return;
            }
            if (FindAttribute(
                    cell,
                    OdfNamespaces.Table,
                    "number-columns-spanned") != null
                || FindAttribute(
                    cell,
                    OdfNamespaces.Table,
                    "number-rows-spanned") != null
                || expectedCovered
                || expectedMatrix)
            {
                AddMergeDiagnostic(
                    cell,
                    "A matrix formula range overlaps another matrix or merged range.");
                return;
            }
            SemanticMergeClaim matrixClaim = new SemanticMergeClaim();
            matrixClaim.Origin = cell;
            matrixClaim.FirstRow = row;
            matrixClaim.LastRow = row + matrixRowCount - 1;
            matrixClaim.FirstColumn = column;
            matrixClaim.LastColumn = column + matrixColumnCount - 1;
            matrixClaims.Add(matrixClaim);
            return;
        }
        if (columns <= 0 || rows <= 0)
        {
            AddMergeDiagnostic(
                cell,
                "A table cell span must use positive integer dimensions.");
            return;
        }
        if (columns > Options.SecurityLimits.MaximumPackageEntries
            || rows > Options.SecurityLimits.MaximumPackageEntries)
        {
            AddMergeDiagnostic(
                cell,
                "A table cell span exceeds the semantic validation bound.");
            return;
        }
        if (columns > 1 || rows > 1)
        {
            if (expectedMatrix)
            {
                AddMergeDiagnostic(
                    cell,
                    "A merged range overlaps a matrix formula range.");
                return;
            }
            SemanticMergeClaim claim = new SemanticMergeClaim();
            claim.Origin = cell;
            claim.FirstRow = row;
            claim.LastRow = row + rows - 1;
            claim.FirstColumn = column;
            claim.LastColumn = column + columns - 1;
            claims.Add(claim);
        }
    }

    void ValidateMissingMatrixSlots(
        OdfNode row,
        int rowIndex,
        int columnCount,
        List<SemanticMergeClaim> claims)
    {
        int claimIndex = 0;
        while (claimIndex < claims.Count && !Stop)
        {
            SemanticMergeClaim claim = claims[claimIndex];
            if (rowIndex >= claim.FirstRow
                && rowIndex <= claim.LastRow
                && claim.LastColumn >= columnCount)
            {
                AddMergeDiagnostic(
                    claim.Origin,
                    "A matrix formula range extends beyond the available table cells.");
            }
            claimIndex = claimIndex + 1;
        }
    }

    bool ContainsNestedTable(OdfNode cell)
    {
        int index = 0;
        while (index < cell.Children.Length)
        {
            OdfNode child = cell.Children[index];
            if (IsElement(child, OdfNamespaces.Table, "table")
                || ContainsNestedTable(child))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    void ValidateMissingMergeSlots(
        OdfNode row,
        int rowIndex,
        int columnCount,
        List<SemanticMergeClaim> claims)
    {
        int claimIndex = 0;
        while (claimIndex < claims.Count && !Stop)
        {
            SemanticMergeClaim claim = claims[claimIndex];
            if (rowIndex >= claim.FirstRow
                && rowIndex <= claim.LastRow
                && claim.LastColumn >= columnCount)
            {
                int firstMissing = columnCount;
                if (firstMissing < claim.FirstColumn)
                {
                    firstMissing = claim.FirstColumn;
                }
                if (!(rowIndex == claim.FirstRow
                    && firstMissing == claim.FirstColumn))
                {
                    AddMergeDiagnostic(
                        claim.Origin,
                        "A spanning table cell is missing one or more covered cells.");
                }
            }
            claimIndex = claimIndex + 1;
        }
    }

    bool SlotIsClaimed(
        int row,
        int column,
        List<SemanticMergeClaim> claims)
    {
        int index = 0;
        while (index < claims.Count)
        {
            SemanticMergeClaim claim = claims[index];
            if (row >= claim.FirstRow
                && row <= claim.LastRow
                && column >= claim.FirstColumn
                && column <= claim.LastColumn
                && !(row == claim.FirstRow
                    && column == claim.FirstColumn))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    void AddMergeDiagnostic(OdfNode node, string message)
    {
        if (MergeDiagnostics.Contains(node))
        {
            return;
        }
        MergeDiagnostics.Add(node);
        SemanticNodeRecord? record = null;
        if (RecordsByNode.ContainsKey(node))
        {
            record = RecordsByNode[node];
        }
        if (record != null)
        {
            AddRecordDiagnostic(
                OdfDiagnosticCodes.InvalidMerge,
                message,
                record,
                null,
                null);
        }
    }

    void ValidateTypedFormulas(OdfDocument document)
    {
        if (!Options.ValidateFormulas || Stop)
        {
            return;
        }
        int index = 0;
        while (index < document.Formulas.Length && !Stop)
        {
            OdfFormula formula = document.Formulas[index];
            bool invalid = formula.Root != null
                && formula.Root.Kind == OdfFormulaNodeKind.Unknown;
            int tokenIndex = 0;
            while (tokenIndex < formula.Tokens.Length)
            {
                if (formula.Tokens[tokenIndex].Kind
                    == OdfFormulaTokenKind.Unknown)
                {
                    invalid = true;
                }
                tokenIndex = tokenIndex + 1;
            }
            if (invalid)
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.InvalidFormula,
                    "A materialized formula contains an unknown syntax node or token.",
                    new OdfLocation(),
                    null);
            }
            if (formula.Result != null
                && formula.Result.Error == OdfFormulaError.CircularReference)
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.CircularReference,
                    "A materialized formula result reports a circular dependency.",
                    new OdfLocation(),
                    null);
            }
            else if (formula.Result != null
                && formula.Result.Error == OdfFormulaError.UnsupportedFunction)
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.UnsupportedFunction,
                    "A materialized formula result reports an unsupported function.",
                    new OdfLocation(),
                    null);
            }
            index = index + 1;
        }
    }

    bool ValueMatchesType(OdfNode node, string valueType)
    {
        OdfAttribute? numberValue = FindAttribute(
            node,
            OdfNamespaces.Office,
            "value");
        OdfAttribute? dateValue = FindAttribute(
            node,
            OdfNamespaces.Office,
            "date-value");
        OdfAttribute? timeValue = FindAttribute(
            node,
            OdfNamespaces.Office,
            "time-value");
        OdfAttribute? booleanValue = FindAttribute(
            node,
            OdfNamespaces.Office,
            "boolean-value");
        OdfAttribute? stringValue = FindAttribute(
            node,
            OdfNamespaces.Office,
            "string-value");
        OdfAttribute? currency = FindAttribute(
            node,
            OdfNamespaces.Office,
            "currency");
        if (valueType == "string")
        {
            return numberValue == null
                && dateValue == null
                && timeValue == null
                && booleanValue == null
                && currency == null;
        }
        if (valueType == "float" || valueType == "percentage")
        {
            return numberValue != null
                && OdfLexicalParser.ParseFloat(numberValue.Value).IsValid
                && dateValue == null
                && timeValue == null
                && booleanValue == null
                && stringValue == null
                && currency == null;
        }
        if (valueType == "currency")
        {
            return numberValue != null
                && OdfLexicalParser.ParseFloat(numberValue.Value).IsValid
                && currency != null
                && currency.Value.Length > 0
                && dateValue == null
                && timeValue == null
                && booleanValue == null
                && stringValue == null;
        }
        if (valueType == "date")
        {
            return dateValue != null
                && DateValueIsValid(dateValue.Value)
                && numberValue == null
                && timeValue == null
                && booleanValue == null
                && stringValue == null
                && currency == null;
        }
        if (valueType == "time")
        {
            return timeValue != null
                && OdfLexicalParser.ParseDuration(timeValue.Value).IsValid
                && numberValue == null
                && dateValue == null
                && booleanValue == null
                && stringValue == null
                && currency == null;
        }
        if (valueType == "boolean")
        {
            return booleanValue != null
                && OdfLexicalParser.ParseBoolean(booleanValue.Value).IsValid
                && numberValue == null
                && dateValue == null
                && timeValue == null
                && stringValue == null
                && currency == null;
        }
        if (valueType == "void")
        {
            return numberValue == null
                && dateValue == null
                && timeValue == null
                && booleanValue == null
                && stringValue == null
                && currency == null;
        }
        return false;
    }

    bool HasOfficeValueAttribute(OdfNode node)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && (attribute.Name.LocalName == "value"
                    || attribute.Name.LocalName == "date-value"
                    || attribute.Name.LocalName == "time-value"
                    || attribute.Name.LocalName == "boolean-value"
                    || attribute.Name.LocalName == "string-value"
                    || attribute.Name.LocalName == "currency"))
            {
                return true;
            }
        }
        return false;
    }

    bool DateValueIsValid(string value)
    {
        return OdfLexicalParser.ParseDate(value).IsValid
            || OdfLexicalParser.ParseDateTime(value).IsValid;
    }

    string ResolvePrefix(OdfNode node, string prefix)
    {
        OdfNode? current = node;
        while (current != null)
        {
            int index = current.NamespaceDeclarations.Length - 1;
            while (index >= 0)
            {
                OdfNamespaceDeclaration declaration =
                    current.NamespaceDeclarations[index];
                if (declaration.Prefix == prefix)
                {
                    return declaration.NamespaceName;
                }
                index = index - 1;
            }
            current = current.Parent;
        }
        if (prefix == "xml")
        {
            return OdfNamespaces.Xml;
        }
        return "";
    }

    bool PackageTargetExists(string path)
    {
        if (PackagePaths.ContainsKey(path)
            || MaterializedParts.ContainsKey(path))
        {
            return true;
        }
        string directory = path;
        if (!directory.EndsWith("/", StringComparison.Ordinal))
        {
            directory = directory + "/";
        }
        foreach (string entry in PackagePaths.Keys)
        {
            if (entry.StartsWith(directory, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    void AddRecordDiagnostic(
        string code,
        string message,
        SemanticNodeRecord record,
        OdfAttribute? attribute,
        SemanticNodeRecord? related)
    {
        OdfLocation location = RecordLocation(record, attribute);
        OdfLocation? relatedLocation = null;
        if (related != null)
        {
            relatedLocation = RecordLocation(related, null);
        }
        AddDiagnostic(code, message, location, relatedLocation);
    }

    void AddDiagnostic(
        string code,
        string message,
        OdfLocation location,
        OdfLocation? related)
    {
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            code,
            message,
            OdfValidationSeverity.Error,
            location,
            related,
            Version);
        Diagnostics.Add(diagnostic);
        if (Options.Mode == OdfValidationMode.Strict)
        {
            Stop = true;
        }
    }

    void ValidateCancellation()
    {
        if (Options.Cancellation.IsCancellationRequested && !Stop)
        {
            string message = "Semantic validation was cancelled.";
            if (Options.Cancellation.Reason.Length > 0)
            {
                message = message + " " + Options.Cancellation.Reason;
            }
            OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
                OdfDiagnosticCodes.OperationCancelled,
                message,
                OdfValidationSeverity.Fatal,
                null,
                null,
                Version);
            Diagnostics.Add(diagnostic);
            Stop = true;
        }
    }

    bool HasErrors()
    {
        return Diagnostics.Count > 0;
    }

    void AddNamedSymbol(
        Dictionary<string, SemanticNodeRecord> symbols,
        OdfAttribute? attribute,
        SemanticNodeRecord record)
    {
        if (attribute != null && attribute.Value.Length > 0)
        {
            AddFirstSymbol(symbols, attribute.Value, record);
        }
    }

    void AddFirstSymbol(
        Dictionary<string, SemanticNodeRecord> symbols,
        string key,
        SemanticNodeRecord record)
    {
        if (!symbols.ContainsKey(key))
        {
            symbols.Add(key, record);
        }
    }

    int PositiveAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        int defaultValue)
    {
        OdfAttribute? attribute = FindAttribute(
            node,
            namespaceName,
            localName);
        if (attribute == null)
        {
            return defaultValue;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(attribute.Value);
        if (!parsed.IsValid
            || parsed.IntegerValue <= 0
            || parsed.IntegerValue > int.MaxValue)
        {
            return -1;
        }
        return (int)parsed.IntegerValue;
    }

    OdfLocation RecordLocation(
        SemanticNodeRecord record,
        OdfAttribute? attribute)
    {
        OdfLocation source = record.Node.Location;
        if (attribute != null)
        {
            source = attribute.Location;
        }
        OdfLocation result = new OdfLocation();
        result.PartPath = record.PartPath;
        result.NodePath = source.NodePath.Length > 0
            ? source.NodePath
            : record.NodePath;
        result.XmlId = source.XmlId;
        result.Line = source.Line;
        result.Column = source.Column;
        result.ByteOffset = source.ByteOffset;
        result.TextOffset = source.TextOffset;
        result.TextLength = source.TextLength;
        return result;
    }

    static bool OptionsAreValid(OdfValidationOptions? options)
    {
        return options != null
            && options.SecurityLimits != null
            && options.Cancellation != null
            && options.SecurityLimits.MaximumFormulaDepth > 0
            && options.SecurityLimits.MaximumPackageEntries > 0;
    }

    OdfVersion SelectVersion(OdfDocument document)
    {
        if (Options.Version != OdfVersion.Unknown)
        {
            return Options.Version;
        }
        if (document.VersionInfo != null)
        {
            return document.VersionInfo.Version;
        }
        return OdfVersion.Unknown;
    }

    static OdfAttribute? FindAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute;
            }
        }
        return null;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static bool IsTableCell(OdfNode node)
    {
        return IsElement(node, OdfNamespaces.Table, "table-cell")
            || IsElement(
                node,
                OdfNamespaces.Table,
                "covered-table-cell");
    }

    static bool IsStyleReference(string localName)
    {
        return localName == "style-name"
            || localName == "apply-style-name"
            || localName.EndsWith("-style-name", StringComparison.Ordinal);
    }

    static bool IsMasterReference(string localName)
    {
        return localName == "master-page-name";
    }

    static bool IsPageLayoutReference(string localName)
    {
        return localName == "page-layout-name"
            || localName == "presentation-page-layout-name";
    }

    static bool IsGenericIdReference(string localName)
    {
        return localName == "start-shape"
            || localName == "end-shape"
            || localName == "shape-id"
            || localName == "target-id"
            || localName == "idref"
            || localName == "for"
            || localName == "control"
            || localName == "start-page"
            || localName == "ref-name";
    }

    static bool IsRangeAttribute(string localName)
    {
        return localName == "print-ranges"
            || localName == "buttons"
            || localName == "repeat-row-range"
            || localName == "repeat-column-range"
            || localName == "scenario-ranges"
            || localName == "source-cell-range-addresses"
            || localName.EndsWith("cell-address", StringComparison.Ordinal)
            || localName.EndsWith(
                "cell-range-address",
                StringComparison.Ordinal)
            || localName.EndsWith("range-address", StringComparison.Ordinal);
    }

    static bool IsResourceElement(OdfNode node)
    {
        if (node.Name == null)
        {
            return false;
        }
        string namespaceName = node.Name.NamespaceName;
        string localName = node.Name.LocalName;
        if (namespaceName == OdfNamespaces.Drawing)
        {
            return localName == "image"
                || localName == "object"
                || localName == "object-ole"
                || localName == "plugin"
                || localName == "floating-frame"
                || localName == "fill-image";
        }
        if (namespaceName == OdfNamespaces.Style
            && localName == "background-image")
        {
            return true;
        }
        if (namespaceName == OdfNamespaces.Svg
            && (localName == "font-face-uri"
                || localName == "definition-src"))
        {
            return true;
        }
        return namespaceName == OdfNamespaces.Presentation
                && localName == "sound"
            || namespaceName == OdfNamespaces.Animation
                && localName == "audio";
    }

    static bool IsKnownChartClass(string localName)
    {
        return localName == "bar"
            || localName == "line"
            || localName == "area"
            || localName == "circle"
            || localName == "ring"
            || localName == "scatter"
            || localName == "radar"
            || localName == "filled-radar"
            || localName == "stock"
            || localName == "bubble"
            || localName == "surface"
            || localName == "gantt"
            || localName == "add-in";
    }

    static bool DelimitersMatch(char opening, char closing)
    {
        return opening == '(' && closing == ')'
            || opening == '{' && closing == '}'
            || opening == '[' && closing == ']';
    }

    static string PartSymbolKey(string partPath, string value)
    {
        return partPath + "\u001f" + value;
    }

    static string SemanticPath(
        string parentPath,
        OdfNode node,
        int number)
    {
        string segment = "node()";
        if (node.Kind == OdfNodeKind.Element)
        {
            segment = "element()";
            if (node.Name != null)
            {
                segment = node.Name.LocalName;
                if (node.Name.Prefix.Length > 0)
                {
                    segment = node.Name.Prefix + ":" + segment;
                }
            }
        }
        else if (node.Kind == OdfNodeKind.Text)
        {
            segment = "text()";
        }
        return parentPath + "/" + segment + "[" + number + "]";
    }

    static int SemanticSiblingNumber(OdfNode parent, int childIndex)
    {
        OdfNode child = parent.Children[childIndex];
        int number = 1;
        int index = 0;
        while (index < childIndex)
        {
            OdfNode prior = parent.Children[index];
            if (prior.Kind == child.Kind
                && NamesAreEqual(prior, child))
            {
                number = number + 1;
            }
            index = index + 1;
        }
        return number;
    }

    static bool NamesAreEqual(OdfNode left, OdfNode right)
    {
        if (left.Name == null || right.Name == null)
        {
            return left.Name == null && right.Name == null;
        }
        return left.Name.NamespaceName == right.Name.NamespaceName
            && left.Name.LocalName == right.Name.LocalName;
    }
}
