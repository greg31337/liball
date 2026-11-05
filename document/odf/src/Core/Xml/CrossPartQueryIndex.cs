using System;
using System.Collections.Generic;

namespace odf;

internal class CrossPartQueryRecord
{
    internal string Category = "";
    internal string FieldName = "";
    internal string Value = "";
    internal string PartPath = "";
    internal OdfNode? Node;
    internal OdfStyle? Style;
    internal OdfTextRange? TextRange;
    internal OdfCellRange? CellRange;
    internal OdfNamedExpression? NamedExpression;
    internal OdfFormula? Formula;
    internal OdfImage? Image;
    internal OdfMediaResource? MediaResource;
    internal OdfEmbeddedObject? EmbeddedObject;
    internal OdfStyleResource? StyleResource;
    internal OdfThumbnail? Thumbnail;
    internal OdfAnnotation? Annotation;
    internal OdfChange? Change;
    internal OdfSpreadsheetChange? SpreadsheetChange;
}

internal class CrossPartQueryIndex
{
    OdfDocument Document;
    Dictionary<string, List<CrossPartQueryRecord>> Names =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> Ids =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> QualifiedNames =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> Fields =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> References =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> Styles =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> Ranges =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> Formulas =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> Resources =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> Annotations =
        NewIndex();
    Dictionary<string, List<CrossPartQueryRecord>> Changes =
        NewIndex();
    List<CrossPartQueryRecord> TextRecords =
        new List<CrossPartQueryRecord>();
    List<CrossPartQueryRecord> AllRecords =
        new List<CrossPartQueryRecord>();
    bool Valid;
    int Revision;

    internal CrossPartQueryIndex(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for cross-part indexing.");
        }
        Document = document;
    }

    internal void Invalidate()
    {
        Valid = false;
    }

    internal void UpdateDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for cross-part indexing.");
        }
        Document = document;
        Valid = false;
    }

    internal CrossPartQueryRecord[] FindNames(string value)
    {
        EnsureBuilt();
        return FindExact(Names, value);
    }

    internal CrossPartQueryRecord[] FindIds(string value)
    {
        EnsureBuilt();
        return FindExact(Ids, value);
    }

    internal CrossPartQueryRecord[] FindQualifiedNames(
        OdfQualifiedName name)
    {
        if (!OdfNameRules.IsQualifiedNameValue(name))
        {
            throw new OdfXmlException(
                "A valid expanded XML name is required for lookup.");
        }
        EnsureBuilt();
        return FindExact(
            QualifiedNames,
            ExpandedName(name.NamespaceName, name.LocalName));
    }

    internal CrossPartQueryRecord[] FindText(
        string value,
        bool matchCase)
    {
        EnsureBuilt();
        if (value == null || value.Length == 0)
        {
            return [];
        }
        StringComparison comparison = matchCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        List<CrossPartQueryRecord> result =
            new List<CrossPartQueryRecord>();
        int index = 0;
        while (index < TextRecords.Count)
        {
            CrossPartQueryRecord record = TextRecords[index];
            if (record.Value.IndexOf(value, comparison) >= 0)
            {
                result.Add(record);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal CrossPartQueryRecord[] FindFields(OdfQualifiedName name)
    {
        if (!OdfNameRules.IsQualifiedNameValue(name))
        {
            throw new OdfXmlException(
                "A valid expanded XML field name is required for lookup.");
        }
        EnsureBuilt();
        return FindExact(
            Fields,
            ExpandedName(name.NamespaceName, name.LocalName));
    }

    internal CrossPartQueryRecord[] FindReferences(string value)
    {
        EnsureBuilt();
        return FindExact(References, value);
    }

    internal CrossPartQueryRecord[] FindStyles(string value)
    {
        EnsureBuilt();
        return FindExact(Styles, value);
    }

    internal CrossPartQueryRecord[] FindRanges(string value)
    {
        EnsureBuilt();
        return FindExact(Ranges, value);
    }

    internal CrossPartQueryRecord[] FindFormulas(string value)
    {
        EnsureBuilt();
        return FindExact(Formulas, value);
    }

    internal CrossPartQueryRecord[] FindResources(string value)
    {
        EnsureBuilt();
        return FindExact(Resources, value);
    }

    internal CrossPartQueryRecord[] FindAnnotations(string value)
    {
        EnsureBuilt();
        return FindExact(Annotations, value);
    }

    internal CrossPartQueryRecord[] FindChanges(string value)
    {
        EnsureBuilt();
        return FindExact(Changes, value);
    }

    internal CrossPartQueryRecord[] GetAllRecords()
    {
        EnsureBuilt();
        return AllRecords.ToArray();
    }

    internal int GetRevision()
    {
        EnsureBuilt();
        return Revision;
    }

    void EnsureBuilt()
    {
        if (Valid)
        {
            return;
        }
        Rebuild();
    }

    void Rebuild()
    {
        ClearIndexes();
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        IndexPartNodes(visited);
        IndexDocumentNodes(visited);
        IndexTypedStyles();
        IndexTypedRanges();
        IndexTypedFormulas();
        IndexTypedResources();
        IndexTypedAnnotations();
        IndexTypedChanges();
        Revision = Revision + 1;
        Valid = true;
    }

    void ClearIndexes()
    {
        Names.Clear();
        Ids.Clear();
        QualifiedNames.Clear();
        Fields.Clear();
        References.Clear();
        Styles.Clear();
        Ranges.Clear();
        Formulas.Clear();
        Resources.Clear();
        Annotations.Clear();
        Changes.Clear();
        TextRecords.Clear();
        AllRecords.Clear();
    }

    void IndexPartNodes(HashSet<OdfNode> visited)
    {
        if (Document.Parts == null)
        {
            return;
        }
        int partIndex = 0;
        while (partIndex < Document.Parts.Length)
        {
            OdfPart? part = Document.Parts[partIndex];
            if (part != null && part.Nodes != null)
            {
                int nodeIndex = 0;
                while (nodeIndex < part.Nodes.Length)
                {
                    VisitNode(part.Nodes[nodeIndex], part.Path, visited);
                    nodeIndex = nodeIndex + 1;
                }
            }
            partIndex = partIndex + 1;
        }
    }

    void IndexDocumentNodes(HashSet<OdfNode> visited)
    {
        if (Document.Nodes == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.Nodes.Length)
        {
            OdfNode? node = Document.Nodes[index];
            string partPath = "";
            if (node != null && node.Location != null)
            {
                partPath = node.Location.PartPath;
            }
            VisitNode(node, partPath, visited);
            index = index + 1;
        }
    }

    void VisitNode(
        OdfNode? node,
        string partPath,
        HashSet<OdfNode> visited)
    {
        if (node == null || visited.Contains(node))
        {
            return;
        }
        visited.Add(node);
        string actualPartPath = partPath;
        if (node.Location != null && node.Location.PartPath.Length > 0)
        {
            actualPartPath = node.Location.PartPath;
        }
        if (node.Kind == OdfNodeKind.Element && node.Name != null)
        {
            IndexElementNode(node, actualPartPath);
        }
        else if (node.Kind == OdfNodeKind.Text
            && node.Value.Length > 0)
        {
            CrossPartQueryRecord text = NodeRecord(
                "text",
                "value",
                node.Value,
                actualPartPath,
                node);
            TextRecords.Add(text);
            AllRecords.Add(text);
        }
        if (node.Children != null)
        {
            int index = 0;
            while (index < node.Children.Length)
            {
                VisitNode(node.Children[index], actualPartPath, visited);
                index = index + 1;
            }
        }
    }

    void IndexElementNode(OdfNode node, string partPath)
    {
        OdfQualifiedName name = node.Name ?? new OdfQualifiedName();
        CrossPartQueryRecord qualifiedName = NodeRecord(
            "qualified-name",
            "element",
            ExpandedName(name.NamespaceName, name.LocalName),
            partPath,
            node);
        AddRecord(
            QualifiedNames,
            qualifiedName.Value,
            qualifiedName);
        if (node.XmlId.Length > 0
            && NodeAttributeValue(node, "id").Length == 0)
        {
            AddNodeCategory(
                Ids,
                "id",
                "xml:id",
                node.XmlId,
                partPath,
                node);
        }
        if (node.Attributes != null)
        {
            int index = 0;
            while (index < node.Attributes.Length)
            {
                IndexNodeAttribute(
                    node,
                    node.Attributes[index],
                    partPath);
                index = index + 1;
            }
        }
        IndexSpecialElement(node, partPath);
    }

    void IndexNodeAttribute(
        OdfNode node,
        OdfAttribute? attribute,
        string partPath)
    {
        if (attribute == null || attribute.Name == null
            || attribute.Value.Length == 0)
        {
            return;
        }
        string localName = attribute.Name.LocalName;
        string fieldName = ExpandedName(
            attribute.Name.NamespaceName,
            localName);
        CrossPartQueryRecord record = NodeRecord(
            "field",
            fieldName,
            attribute.Value,
            partPath,
            node);
        AddRecord(Fields, fieldName, record);
        if (localName == "name")
        {
            AddExistingRecord(Names, attribute.Value, record);
        }
        if (localName == "id")
        {
            AddExistingRecord(Ids, attribute.Value, record);
        }
        if (IsStyleAttribute(attribute.Name))
        {
            AddExistingRecord(Styles, attribute.Value, record);
        }
        if (IsRangeAttribute(localName))
        {
            AddExistingRecord(Ranges, attribute.Value, record);
        }
        if (localName == "formula")
        {
            AddExistingRecord(Formulas, attribute.Value, record);
        }
        if (localName == "href")
        {
            AddExistingRecord(Resources, attribute.Value, record);
        }
        if (IsReferenceAttribute(attribute.Name))
        {
            AddExistingRecord(References, attribute.Value, record);
            string target = ReferenceFragment(attribute.Name, attribute.Value);
            if (target != attribute.Value)
            {
                AddExistingRecord(References, target, record);
            }
        }
    }

    void IndexSpecialElement(OdfNode node, string partPath)
    {
        OdfQualifiedName name = node.Name ?? new OdfQualifiedName();
        if (name.NamespaceName == OdfNamespaces.Office
            && (name.LocalName == "annotation"
                || name.LocalName == "annotation-end"))
        {
            string value = NodeAttributeValue(node, "name");
            if (value.Length == 0)
            {
                value = node.XmlId;
            }
            if (value.Length > 0)
            {
                AddNodeLookup(
                    Annotations,
                    "annotation",
                    name.LocalName,
                    value,
                    partPath,
                    node);
            }
        }
        if (IsChangeElement(name))
        {
            string value = NodeAttributeValue(node, "change-id");
            if (value.Length == 0)
            {
                value = NodeAttributeValue(node, "id");
            }
            if (value.Length > 0)
            {
                AddNodeLookup(
                    Changes,
                    "change",
                    name.LocalName,
                    value,
                    partPath,
                    node);
            }
        }
    }

    void IndexTypedStyles()
    {
        if (Document.Styles == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.Styles.Length)
        {
            OdfStyle? style = Document.Styles[index];
            if (style != null && style.Name.Length > 0)
            {
                CrossPartQueryRecord record = TypedRecord(
                    "style",
                    "name",
                    style.Name,
                    style.PartPath,
                    null);
                record.Style = style;
                AddRecord(Styles, style.Name, record);
                AddExistingRecord(Names, style.Name, record);
            }
            index = index + 1;
        }
    }

    void IndexTypedRanges()
    {
        IndexTextRanges();
        IndexNamedRanges();
        IndexTableRanges();
        IndexLabelRanges();
        IndexDatabaseRanges();
        IndexDetectiveRanges();
        IndexConsolidationRanges();
        IndexScenarioRanges();
        IndexPivotRanges();
    }

    void IndexTextRanges()
    {
        if (Document.TextRanges == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.TextRanges.Length)
        {
            OdfTextRange? range = Document.TextRanges[index];
            if (range != null && range.Name.Length > 0)
            {
                CrossPartQueryRecord record = TypedRecord(
                    "range",
                    "name",
                    range.Name,
                    range.PartPath,
                    range.StartNode);
                record.TextRange = range;
                AddRecord(Ranges, range.Name, record);
            }
            index = index + 1;
        }
    }

    void IndexNamedRanges()
    {
        if (Document.NamedExpressions == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.NamedExpressions.Length)
        {
            OdfNamedExpression? expression =
                Document.NamedExpressions[index];
            if (expression != null && expression.IsRange)
            {
                string partPath = NodePartPath(expression.Node);
                CrossPartQueryRecord record = TypedRecord(
                    "range",
                    "range-address",
                    expression.RangeAddress,
                    partPath,
                    expression.Node);
                record.NamedExpression = expression;
                AddRecord(Ranges, expression.RangeAddress, record);
                if (expression.Name.Length > 0
                    && expression.Name != expression.RangeAddress)
                {
                    CrossPartQueryRecord nameRecord = CopyRecordValue(
                        record,
                        "name",
                        expression.Name);
                    AddRecord(Ranges, expression.Name, nameRecord);
                    AddExistingRecord(
                        Names,
                        expression.Name,
                        nameRecord);
                }
            }
            index = index + 1;
        }
    }

    void IndexTableRanges()
    {
        if (Document.Tables == null)
        {
            return;
        }
        int tableIndex = 0;
        while (tableIndex < Document.Tables.Length)
        {
            OdfTable? table = Document.Tables[tableIndex];
            if (table != null)
            {
                if (table.Name.Length > 0)
                {
                    CrossPartQueryRecord nameRecord = TypedRecord(
                        "name",
                        "name",
                        table.Name,
                        table.PartPath,
                        table.Node);
                    AddRecord(Names, table.Name, nameRecord);
                }
                if (table.PrintRanges != null)
                {
                    int rangeIndex = 0;
                    while (rangeIndex < table.PrintRanges.Length)
                    {
                        OdfCellRange? range =
                            table.PrintRanges[rangeIndex];
                        if (range != null
                            && range.LexicalValue.Length > 0)
                        {
                            CrossPartQueryRecord record = TypedRecord(
                                "range",
                                "print-range",
                                range.LexicalValue,
                                table.PartPath,
                                table.Node);
                            record.CellRange = range;
                            AddRecord(Ranges, range.LexicalValue, record);
                        }
                        rangeIndex = rangeIndex + 1;
                    }
                }
                IndexTableCellFormulas(table);
            }
            tableIndex = tableIndex + 1;
        }
    }

    void IndexLabelRanges()
    {
        if (Document.LabelRanges == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.LabelRanges.Length)
        {
            OdfLabelRange? range = Document.LabelRanges[index];
            if (range != null)
            {
                AddTypedRangeValue(
                    "label-range-address",
                    range.LabelRangeAddress,
                    range.Node);
                AddTypedRangeValue(
                    "data-range-address",
                    range.DataRangeAddress,
                    range.Node);
            }
            index = index + 1;
        }
    }

    void IndexDatabaseRanges()
    {
        if (Document.DatabaseRanges == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.DatabaseRanges.Length)
        {
            OdfDatabaseRange? range = Document.DatabaseRanges[index];
            if (range != null)
            {
                AddTypedRangeValue(
                    "target-range-address",
                    range.TargetRangeAddress,
                    range.Node);
                AddTypedRangeValue(
                    "sort-target-range-address",
                    range.SortTargetRangeAddress,
                    range.Node);
            }
            index = index + 1;
        }
    }

    void IndexDetectiveRanges()
    {
        if (Document.DetectiveRanges == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.DetectiveRanges.Length)
        {
            OdfDetectiveRange? range = Document.DetectiveRanges[index];
            if (range != null)
            {
                AddTypedRangeValue(
                    "range-address",
                    range.RangeAddress,
                    range.Node);
            }
            index = index + 1;
        }
    }

    void IndexConsolidationRanges()
    {
        if (Document.Consolidations == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.Consolidations.Length)
        {
            OdfConsolidation? value = Document.Consolidations[index];
            if (value != null)
            {
                AddTypedRangeValue(
                    "source-range-addresses",
                    value.SourceRangeAddresses,
                    value.Node);
                AddTypedRangeValue(
                    "target-cell-address",
                    value.TargetCellAddress,
                    value.Node);
            }
            index = index + 1;
        }
    }

    void IndexScenarioRanges()
    {
        if (Document.Scenarios == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.Scenarios.Length)
        {
            OdfScenario? value = Document.Scenarios[index];
            if (value != null)
            {
                AddTypedRangeValue(
                    "scenario-ranges",
                    value.RangeAddresses,
                    value.Node);
            }
            index = index + 1;
        }
    }

    void IndexPivotRanges()
    {
        if (Document.PivotTables == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.PivotTables.Length)
        {
            OdfPivotTable? value = Document.PivotTables[index];
            if (value != null)
            {
                AddTypedRangeValue(
                    "target-range-address",
                    value.TargetRangeAddress,
                    value.Node);
                AddTypedRangeValue(
                    "source-range-address",
                    value.SourceRangeAddress,
                    value.Node);
                AddTypedRangeValue(
                    "button-range-addresses",
                    value.ButtonRangeAddresses,
                    value.Node);
            }
            index = index + 1;
        }
    }

    void IndexTypedFormulas()
    {
        if (Document.Formulas != null)
        {
            int index = 0;
            while (index < Document.Formulas.Length)
            {
                OdfFormula? formula = Document.Formulas[index];
                if (formula != null && formula.Source.Length > 0)
                {
                    CrossPartQueryRecord record = TypedRecord(
                        "formula",
                        "source",
                        formula.Source,
                        "",
                        null);
                    record.Formula = formula;
                    AddRecord(Formulas, formula.Source, record);
                }
                index = index + 1;
            }
        }
        if (Document.NamedExpressions != null)
        {
            int index = 0;
            while (index < Document.NamedExpressions.Length)
            {
                OdfNamedExpression? expression =
                    Document.NamedExpressions[index];
                if (expression != null && !expression.IsRange
                    && expression.Expression.Length > 0)
                {
                    CrossPartQueryRecord record = TypedRecord(
                        "formula",
                        "expression",
                        expression.Expression,
                        NodePartPath(expression.Node),
                        expression.Node);
                    record.NamedExpression = expression;
                    AddRecord(Formulas, expression.Expression, record);
                }
                index = index + 1;
            }
        }
    }

    void IndexTableCellFormulas(OdfTable table)
    {
        if (table.Rows == null)
        {
            return;
        }
        int rowIndex = 0;
        while (rowIndex < table.Rows.Length)
        {
            OdfRow? row = table.Rows[rowIndex];
            if (row != null && row.Cells != null)
            {
                int cellIndex = 0;
                while (cellIndex < row.Cells.Length)
                {
                    OdfCell? cell = row.Cells[cellIndex];
                    if (cell != null && cell.Formula.Length > 0)
                    {
                        CrossPartQueryRecord record = TypedRecord(
                            "formula",
                            "cell-formula",
                            cell.Formula,
                            table.PartPath,
                            cell.Node);
                        AddRecord(Formulas, cell.Formula, record);
                    }
                    cellIndex = cellIndex + 1;
                }
            }
            rowIndex = rowIndex + 1;
        }
    }

    void IndexTypedResources()
    {
        IndexImageResources();
        IndexMediaResources();
        IndexEmbeddedResources();
        IndexStyleResources();
        IndexThumbnailResources();
    }

    void IndexImageResources()
    {
        if (Document.Images == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.Images.Length)
        {
            OdfImage? value = Document.Images[index];
            if (value != null)
            {
                CrossPartQueryRecord record = ResourceRecord(
                    value.Href,
                    value.PackagePath,
                    value.Node);
                record.Image = value;
                AddResourceKeys(record, value.Href, value.PackagePath);
            }
            index = index + 1;
        }
    }

    void IndexMediaResources()
    {
        if (Document.MediaResources == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.MediaResources.Length)
        {
            OdfMediaResource? value = Document.MediaResources[index];
            if (value != null)
            {
                CrossPartQueryRecord record = ResourceRecord(
                    value.Href,
                    value.PackagePath,
                    value.Node);
                record.MediaResource = value;
                AddResourceKeys(record, value.Href, value.PackagePath);
            }
            index = index + 1;
        }
    }

    void IndexEmbeddedResources()
    {
        if (Document.EmbeddedObjects == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.EmbeddedObjects.Length)
        {
            OdfEmbeddedObject? value = Document.EmbeddedObjects[index];
            if (value != null)
            {
                CrossPartQueryRecord record = ResourceRecord(
                    value.Href,
                    value.PackagePath,
                    value.Node);
                record.EmbeddedObject = value;
                AddResourceKeys(record, value.Href, value.PackagePath);
            }
            index = index + 1;
        }
    }

    void IndexStyleResources()
    {
        if (Document.StyleResources == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.StyleResources.Length)
        {
            OdfStyleResource? value = Document.StyleResources[index];
            if (value != null)
            {
                CrossPartQueryRecord record = ResourceRecord(
                    value.Href,
                    "",
                    value.Node);
                record.PartPath = value.PartPath;
                record.StyleResource = value;
                AddResourceKeys(record, value.Href, "");
                if (value.Name.Length > 0)
                {
                    CrossPartQueryRecord nameRecord = CopyRecordValue(
                        record,
                        "name",
                        value.Name);
                    AddRecord(Resources, value.Name, nameRecord);
                    AddExistingRecord(Names, value.Name, nameRecord);
                }
            }
            index = index + 1;
        }
    }

    void IndexThumbnailResources()
    {
        if (Document.Thumbnails == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.Thumbnails.Length)
        {
            OdfThumbnail? value = Document.Thumbnails[index];
            if (value != null && value.Path.Length > 0)
            {
                CrossPartQueryRecord record = TypedRecord(
                    "resource",
                    "path",
                    value.Path,
                    value.Path,
                    null);
                record.Thumbnail = value;
                AddRecord(Resources, value.Path, record);
            }
            index = index + 1;
        }
    }

    void IndexTypedAnnotations()
    {
        if (Document.Annotations == null)
        {
            return;
        }
        int index = 0;
        while (index < Document.Annotations.Length)
        {
            OdfAnnotation? value = Document.Annotations[index];
            if (value != null)
            {
                AddTypedAnnotationField(value, "name", value.Name);
                AddTypedAnnotationField(value, "xml-id", value.XmlId);
                AddTypedAnnotationField(value, "creator", value.Creator);
                AddTypedAnnotationField(value, "text", value.Text);
            }
            index = index + 1;
        }
    }

    void IndexTypedChanges()
    {
        if (Document.Changes != null)
        {
            int index = 0;
            while (index < Document.Changes.Length)
            {
                OdfChange? value = Document.Changes[index];
                if (value != null)
                {
                    AddTypedTextChangeField(value, "id", value.Id);
                    AddTypedTextChangeField(
                        value,
                        "xml-id",
                        value.XmlId);
                    AddTypedTextChangeField(
                        value,
                        "creator",
                        value.Creator);
                    AddTypedTextChangeField(
                        value,
                        "comment",
                        value.Comment);
                    AddTypedTextChangeField(
                        value,
                        "changed-text",
                        value.ChangedText);
                    AddTypedTextChangeField(
                        value,
                        "deleted-text",
                        value.DeletedText);
                }
                index = index + 1;
            }
        }
        if (Document.SpreadsheetChanges != null)
        {
            int index = 0;
            while (index < Document.SpreadsheetChanges.Length)
            {
                OdfSpreadsheetChange? value =
                    Document.SpreadsheetChanges[index];
                if (value != null)
                {
                    AddTypedSpreadsheetChangeField(
                        value,
                        "id",
                        value.Id);
                    AddTypedSpreadsheetChangeField(
                        value,
                        "creator",
                        value.Creator);
                    AddTypedSpreadsheetChangeField(
                        value,
                        "comment",
                        value.Comment);
                    AddTypedSpreadsheetChangeField(
                        value,
                        "kind",
                        value.Kind);
                }
                index = index + 1;
            }
        }
    }

    void AddTypedRangeValue(
        string fieldName,
        string value,
        OdfNode? node)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }
        CrossPartQueryRecord record = TypedRecord(
            "range",
            fieldName,
            value,
            NodePartPath(node),
            node);
        AddRecord(Ranges, value, record);
    }

    void AddResourceKeys(
        CrossPartQueryRecord record,
        string first,
        string second)
    {
        string primary = first.Length > 0 ? first : second;
        if (primary.Length == 0)
        {
            return;
        }
        record.Value = primary;
        AddRecord(Resources, primary, record);
        AddExistingRecord(References, primary, record);
        if (second != primary)
        {
            CrossPartQueryRecord secondRecord = CopyRecordValue(
                record,
                "path",
                second);
            AddRecord(Resources, second, secondRecord);
            AddExistingRecord(References, second, secondRecord);
        }
    }

    void AddTypedAnnotationField(
        OdfAnnotation annotation,
        string fieldName,
        string value)
    {
        if (value.Length == 0)
        {
            return;
        }
        CrossPartQueryRecord record = TypedRecord(
            "annotation",
            fieldName,
            value,
            annotation.PartPath,
            annotation.Node);
        record.Annotation = annotation;
        AddRecord(Annotations, value, record);
        if (fieldName == "name")
        {
            AddExistingRecord(Names, value, record);
        }
        else if (fieldName == "xml-id")
        {
            AddExistingRecord(Ids, value, record);
        }
    }

    void AddTypedTextChangeField(
        OdfChange change,
        string fieldName,
        string value)
    {
        if (value.Length == 0)
        {
            return;
        }
        CrossPartQueryRecord record = TypedRecord(
            "change",
            fieldName,
            value,
            change.PartPath,
            change.Node);
        record.Change = change;
        AddRecord(Changes, value, record);
        if (fieldName == "xml-id")
        {
            AddExistingRecord(Ids, value, record);
        }
    }

    void AddTypedSpreadsheetChangeField(
        OdfSpreadsheetChange change,
        string fieldName,
        string value)
    {
        if (value.Length == 0)
        {
            return;
        }
        CrossPartQueryRecord record = TypedRecord(
            "change",
            fieldName,
            value,
            NodePartPath(change.Node),
            change.Node);
        record.SpreadsheetChange = change;
        AddRecord(Changes, value, record);
    }

    void AddNodeCategory(
        Dictionary<string, List<CrossPartQueryRecord>> index,
        string category,
        string fieldName,
        string value,
        string partPath,
        OdfNode node)
    {
        CrossPartQueryRecord record = NodeRecord(
            category,
            fieldName,
            value,
            partPath,
            node);
        AddRecord(index, value, record);
    }

    void AddNodeLookup(
        Dictionary<string, List<CrossPartQueryRecord>> index,
        string category,
        string fieldName,
        string value,
        string partPath,
        OdfNode node)
    {
        CrossPartQueryRecord record = NodeRecord(
            category,
            fieldName,
            value,
            partPath,
            node);
        AddExistingRecord(index, value, record);
    }

    void AddRecord(
        Dictionary<string, List<CrossPartQueryRecord>> index,
        string key,
        CrossPartQueryRecord record)
    {
        if (key != null && key.Length > 0)
        {
            AddExistingRecord(index, key, record);
        }
        AllRecords.Add(record);
    }

    static void AddExistingRecord(
        Dictionary<string, List<CrossPartQueryRecord>> index,
        string key,
        CrossPartQueryRecord record)
    {
        if (key == null || key.Length == 0)
        {
            return;
        }
        if (!index.ContainsKey(key))
        {
            index.Add(key, new List<CrossPartQueryRecord>());
        }
        List<CrossPartQueryRecord> records = index[key];
        int itemIndex = 0;
        while (itemIndex < records.Count)
        {
            if (records[itemIndex] == record)
            {
                return;
            }
            itemIndex = itemIndex + 1;
        }
        records.Add(record);
    }

    static CrossPartQueryRecord NodeRecord(
        string category,
        string fieldName,
        string value,
        string partPath,
        OdfNode node)
    {
        CrossPartQueryRecord result = new CrossPartQueryRecord();
        result.Category = category;
        result.FieldName = fieldName;
        result.Value = value;
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static CrossPartQueryRecord TypedRecord(
        string category,
        string fieldName,
        string value,
        string partPath,
        OdfNode? node)
    {
        CrossPartQueryRecord result = new CrossPartQueryRecord();
        result.Category = category;
        result.FieldName = fieldName;
        result.Value = value;
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static CrossPartQueryRecord ResourceRecord(
        string reference,
        string fallback,
        OdfNode? node)
    {
        return TypedRecord(
            "resource",
            "href",
            reference.Length > 0 ? reference : fallback,
            NodePartPath(node),
            node);
    }

    static CrossPartQueryRecord CopyRecordValue(
        CrossPartQueryRecord source,
        string fieldName,
        string value)
    {
        CrossPartQueryRecord result = TypedRecord(
            source.Category,
            fieldName,
            value,
            source.PartPath,
            source.Node);
        result.Style = source.Style;
        result.TextRange = source.TextRange;
        result.CellRange = source.CellRange;
        result.NamedExpression = source.NamedExpression;
        result.Formula = source.Formula;
        result.Image = source.Image;
        result.MediaResource = source.MediaResource;
        result.EmbeddedObject = source.EmbeddedObject;
        result.StyleResource = source.StyleResource;
        result.Thumbnail = source.Thumbnail;
        result.Annotation = source.Annotation;
        result.Change = source.Change;
        result.SpreadsheetChange = source.SpreadsheetChange;
        return result;
    }

    static CrossPartQueryRecord[] FindExact(
        Dictionary<string, List<CrossPartQueryRecord>> index,
        string value)
    {
        if (value == null || !index.ContainsKey(value))
        {
            return [];
        }
        return index[value].ToArray();
    }

    static Dictionary<string, List<CrossPartQueryRecord>> NewIndex()
    {
        return new Dictionary<string, List<CrossPartQueryRecord>>(
            StringComparer.Ordinal);
    }

    static string ExpandedName(
        string namespaceName,
        string localName)
    {
        return namespaceName + "\u001F" + localName;
    }

    static bool IsStyleAttribute(OdfQualifiedName name)
    {
        return name.LocalName == "style-name"
            || name.LocalName.EndsWith(
                "-style-name",
                StringComparison.Ordinal)
            || (name.NamespaceName == OdfNamespaces.Style
                && name.LocalName == "name");
    }

    static bool IsRangeAttribute(string localName)
    {
        return localName == "range"
            || localName == "range-address"
            || localName == "cell-range-address"
            || localName == "target-range-address"
            || localName == "source-range-address"
            || localName == "source-range-addresses"
            || localName == "button-range-addresses"
            || localName == "print-ranges"
            || localName == "label-cell-range-address"
            || localName == "data-cell-range-address"
            || localName == "end-cell-address"
            || localName == "cell-address";
    }

    static bool IsReferenceAttribute(OdfQualifiedName name)
    {
        string localName = name.LocalName;
        if (localName == "href"
            || localName == "style-name"
            || localName.EndsWith(
                "-style-name",
                StringComparison.Ordinal)
            || IsRangeAttribute(localName))
        {
            return true;
        }
        return localName == "ref-name"
            || localName == "start-shape"
            || localName == "end-shape"
            || localName == "shape-id"
            || localName == "target-id"
            || localName == "idref"
            || localName == "for"
            || localName == "control"
            || localName == "start-page";
    }

    static string ReferenceFragment(
        OdfQualifiedName name,
        string value)
    {
        if (name.LocalName == "href")
        {
            int fragment = value.LastIndexOf('#');
            if (fragment >= 0 && fragment + 1 < value.Length)
            {
                return value.Substring(fragment + 1);
            }
        }
        return value;
    }

    static bool IsChangeElement(OdfQualifiedName name)
    {
        string localName = name.LocalName;
        return localName == "changed-region"
            || localName == "change"
            || localName == "change-start"
            || localName == "change-end"
            || localName == "cell-content-change"
            || localName == "insertion"
            || localName == "deletion"
            || localName == "movement";
    }

    static string NodeAttributeValue(
        OdfNode node,
        string localName)
    {
        if (node.Attributes == null)
        {
            return "";
        }
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute? attribute = node.Attributes[index];
            if (attribute != null
                && attribute.Name != null
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static string NodePartPath(OdfNode? node)
    {
        if (node == null || node.Location == null)
        {
            return "";
        }
        return node.Location.PartPath;
    }
}
