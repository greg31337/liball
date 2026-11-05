using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML pivot table definition part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxPivotTableDefinitionPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the cache id value used by this model.
    /// </summary>
    public uint CacheId;
    /// <summary>
    /// Indicates whether cache id is present and should be written.
    /// </summary>
    public bool HasCacheId;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Stores the location value used by this model.
    /// </summary>
    public XlsxPivotTableLocation Location;
    /// <summary>
    /// Indicates whether location is present and should be written.
    /// </summary>
    public bool HasLocation;
    /// <summary>
    /// Contains fields.
    /// </summary>
    public List<XlsxPivotTableField> Fields;
    /// <summary>
    /// Contains row fields.
    /// </summary>
    public List<XlsxPivotTableFieldReference> RowFields;
    /// <summary>
    /// Contains column fields.
    /// </summary>
    public List<XlsxPivotTableFieldReference> ColumnFields;
    /// <summary>
    /// Contains page fields.
    /// </summary>
    public List<XlsxPivotTableFieldReference> PageFields;
    /// <summary>
    /// Contains data fields.
    /// </summary>
    public List<XlsxPivotTableDataField> DataFields;
    /// <summary>
    /// Contains calculated fields.
    /// </summary>
    public List<XlsxPivotTableCalculatedField> CalculatedFields;
    /// <summary>
    /// Contains filters.
    /// </summary>
    public List<XlsxPivotTableFilter> Filters;
    /// <summary>
    /// Contains formats.
    /// </summary>
    public List<XlsxPivotTableFormat> Formats;
    /// <summary>
    /// Stores the style info value used by this model.
    /// </summary>
    public XlsxPivotTableStyleInfo StyleInfo;
    /// <summary>
    /// Indicates whether style info is present and should be written.
    /// </summary>
    public bool HasStyleInfo;
    /// <summary>
    /// Stores the cache relationship value used by this model.
    /// </summary>
    public XlsxRelationship? CacheRelationship;
    /// <summary>
    /// Indicates whether cache relationship is present and should be written.
    /// </summary>
    public bool HasCacheRelationship;
    /// <summary>
    /// Stores the cache relationship id value used by this model.
    /// </summary>
    public string? CacheRelationshipId;
    /// <summary>
    /// Stores the cache target value used by this model.
    /// </summary>
    public string? CacheTarget;
    /// <summary>
    /// Stores the cache target path value used by this model.
    /// </summary>
    public string CacheTargetPath;
    /// <summary>
    /// Initializes a new pivot table definition part with its default state.
    /// </summary>
    public XlsxPivotTableDefinitionPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/pivotTables/pivotTable1.xml";
        RawXml = "";
        Name = "";
        CacheId = 0;
        HasCacheId = false;
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        Location = new XlsxPivotTableLocation();
        HasLocation = false;
        Fields = new List<XlsxPivotTableField>();
        RowFields = new List<XlsxPivotTableFieldReference>();
        ColumnFields = new List<XlsxPivotTableFieldReference>();
        PageFields = new List<XlsxPivotTableFieldReference>();
        DataFields = new List<XlsxPivotTableDataField>();
        CalculatedFields = new List<XlsxPivotTableCalculatedField>();
        Filters = new List<XlsxPivotTableFilter>();
        Formats = new List<XlsxPivotTableFormat>();
        StyleInfo = new XlsxPivotTableStyleInfo();
        HasStyleInfo = false;
        CacheRelationship = null;
        HasCacheRelationship = false;
        CacheRelationshipId = "";
        CacheTarget = "";
        CacheTargetPath = "";
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Performs the resolve cache relationship operation using the current model state.
    /// </summary>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableCacheRelationshipResult ResolveCacheRelationship(XlsxRelationshipsPart relationships)
    {
        XlsxPivotTableCacheRelationshipResult result = new XlsxPivotTableCacheRelationshipResult();
        if (relationships == null)
        {
            result.AddError("pivotTableDefinition.cacheRelationshipsMissing", "Pivot table cache link cannot be resolved because the relationships part is missing.");
            return result;
        }

        XlsxRelationship? relationship = relationships.GetFirstByType(XlsxRelationshipTypes.PivotCacheDefinition);
        if (relationship == null)
        {
            result.AddError("pivotTableDefinition.cacheRelationshipMissing", "Pivot table definition relationships must include a pivot cache definition relationship.");
            return result;
        }

        CacheRelationship = relationship;
        HasCacheRelationship = true;
        CacheRelationshipId = relationship.Id;
        CacheTarget = relationship.Target;
        CacheTargetPath = relationship.TargetPath;
        result.Relationship = relationship;
        result.HasRelationship = true;
        return result;
    }

    /// <summary>
    /// Adds row field to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableFieldOperationResult AddRowField(string fieldName)
    {
        return AddAxisField(fieldName, "axisRow", RowFields, "rowFields");
    }

    /// <summary>
    /// Adds column field to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableFieldOperationResult AddColumnField(string fieldName)
    {
        return AddAxisField(fieldName, "axisCol", ColumnFields, "colFields");
    }

    /// <summary>
    /// Adds page field to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableFieldOperationResult AddPageField(string fieldName)
    {
        return AddPageFieldWithItem(fieldName, 0);
    }

    /// <summary>
    /// Adds page field to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <param name = "itemIndex">The item index value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableFieldOperationResult AddPageFieldWithItem(string fieldName, int itemIndex)
    {
        XlsxPivotTableFieldOperationResult result = ResolveFieldForOperation(fieldName);
        if (!result.IsValid)
        {
            return result;
        }

        if (PageFields == null)
        {
            result.AddError("pivotTableField.pageFieldsNull", "Pivot table page field list must not be null.");
            return result;
        }

        if (ContainsFieldReference(PageFields, result.FieldIndex))
        {
            result.AddError("pivotTableField.duplicatePageField", "Pivot table page fields already contain the requested field.");
            return result;
        }

        RemoveFieldReference(RowFields, result.FieldIndex);
        RemoveFieldReference(ColumnFields, result.FieldIndex);
        XlsxPivotTableField field = Fields[result.FieldIndex];
        field.Axis = "axisPage";
        XlsxPivotTableFieldReference reference = new XlsxPivotTableFieldReference();
        reference.FieldIndex = result.FieldIndex;
        reference.HasFieldIndex = true;
        reference.ItemIndex = itemIndex;
        reference.HasItemIndex = true;
        reference.Name = result.FieldName;
        PageFields.Add(reference);
        RebuildRawXmlAfterFieldOperation(result);
        return result;
    }

    /// <summary>
    /// Adds data field to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableFieldOperationResult AddDataField(string fieldName)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(fieldName);
        return AddDataFieldWithOptions(fieldName, "sum", "Sum of " + normalized);
    }

    /// <summary>
    /// Adds data field to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <param name = "subtotal">The subtotal value used by the operation.</param>
    /// <param name = "displayName">The display name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableFieldOperationResult AddDataFieldWithOptions(string fieldName, string subtotal, string displayName)
    {
        XlsxPivotTableFieldOperationResult result = ResolveFieldForOperation(fieldName);
        if (!result.IsValid)
        {
            return result;
        }

        if (DataFields == null)
        {
            result.AddError("pivotTableField.dataFieldsNull", "Pivot table data field list must not be null.");
            return result;
        }

        if (ContainsDataField(DataFields, result.FieldIndex))
        {
            result.AddError("pivotTableField.duplicateDataField", "Pivot table data fields already contain the requested field.");
            return result;
        }

        XlsxPivotTableField field = Fields[result.FieldIndex];
        field.DataField = true;
        field.HasDataField = true;
        XlsxPivotTableDataField dataField = new XlsxPivotTableDataField();
        dataField.Name = XlsxCellValueConverter.NormalizeText(displayName);
        if (dataField.Name.Length == 0)
        {
            dataField.Name = "Sum of " + result.FieldName;
        }

        dataField.FieldIndex = result.FieldIndex;
        dataField.HasFieldIndex = true;
        dataField.Subtotal = XlsxCellValueConverter.NormalizeText(subtotal);
        if (dataField.Subtotal.Length == 0)
        {
            dataField.Subtotal = "sum";
        }

        DataFields.Add(dataField);
        RebuildRawXmlAfterFieldOperation(result);
        return result;
    }

    /// <summary>
    /// Performs the group field items operation using the current model state.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <param name = "groupNames">The group names value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableOperationResult GroupFieldItems(string fieldName, List<string> groupNames)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        XlsxPivotTableFieldOperationResult fieldResult = ResolveFieldForOperation(fieldName);
        CopyFieldOperationDiagnostics(result, fieldResult);
        if (!result.IsValid)
        {
            return result;
        }

        if (groupNames == null)
        {
            result.AddError("pivotTableGrouping.itemsNull", "Pivot table grouping item list must not be null.");
            return result;
        }

        if (groupNames.Count == 0)
        {
            result.AddError("pivotTableGrouping.itemsEmpty", "Pivot table grouping item list must contain at least one item.");
            return result;
        }

        XlsxPivotTableField field = Fields[fieldResult.FieldIndex];
        if (field.GroupItems == null)
        {
            result.AddError("pivotTableGrouping.groupItemsNull", "Pivot table field group item list must not be null.");
            return result;
        }

        field.GroupItems.Clear();
        int index = 0;
        foreach (string groupName in groupNames)
        {
            string normalized = XlsxCellValueConverter.NormalizeText(groupName);
            if (normalized.Length == 0)
            {
                XlsxDiagnostic diagnostic = result.AddError("pivotTableGrouping.itemNameMissing", "Pivot table grouping item names must not be empty.");
                diagnostic.SetLocation(PartPath, "groupItems", index.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                XlsxPivotTableItem item = new XlsxPivotTableItem();
                item.Type = "s";
                item.Name = normalized;
                field.GroupItems.Add(item);
            }

            index = index + 1;
        }

        if (!result.IsValid)
        {
            return result;
        }

        field.HasGrouping = true;
        RebuildRawXmlAfterOperation(result);
        return result;
    }

    /// <summary>
    /// Adds calculated field to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableOperationResult AddCalculatedField(string name, string formula)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        if (CalculatedFields == null)
        {
            result.AddError("pivotTableCalculatedFields.listNull", "Pivot table calculated field list must not be null.");
            return result;
        }

        string normalizedName = XlsxCellValueConverter.NormalizeText(name);
        string normalizedFormula = XlsxCellValueConverter.NormalizeText(formula);
        if (normalizedName.Length == 0)
        {
            result.AddError("pivotTableCalculatedFields.nameMissing", "Calculated field name must not be empty.");
        }

        if (normalizedFormula.Length == 0)
        {
            result.AddError("pivotTableCalculatedFields.formulaMissing", "Calculated field formula must not be empty.");
        }

        if (ContainsCalculatedField(normalizedName))
        {
            result.AddError("pivotTableCalculatedFields.duplicateName", "Calculated field names must be unique.");
        }

        if (!result.IsValid)
        {
            return result;
        }

        XlsxPivotTableCalculatedField calculatedField = new XlsxPivotTableCalculatedField();
        calculatedField.Name = normalizedName;
        calculatedField.Formula = normalizedFormula;
        if (Fields != null)
        {
            calculatedField.FieldIndex = Fields.Count;
            calculatedField.HasFieldIndex = true;
            XlsxPivotTableField field = new XlsxPivotTableField();
            field.Name = normalizedName;
            Fields.Add(field);
        }

        CalculatedFields.Add(calculatedField);
        RebuildRawXmlAfterOperation(result);
        return result;
    }

    /// <summary>
    /// Adds filter to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <param name = "filterType">The filter type value used by the operation.</param>
    /// <param name = "filterName">The filter name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableOperationResult AddFilter(string fieldName, string filterType, string filterName)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        XlsxPivotTableFieldOperationResult fieldResult = ResolveFieldForOperation(fieldName);
        CopyFieldOperationDiagnostics(result, fieldResult);
        if (!result.IsValid)
        {
            return result;
        }

        if (Filters == null)
        {
            result.AddError("pivotTableFilters.listNull", "Pivot table filter list must not be null.");
            return result;
        }

        string normalizedType = XlsxCellValueConverter.NormalizeText(filterType);
        if (normalizedType.Length == 0)
        {
            result.AddError("pivotTableFilters.typeMissing", "Pivot table filter type must not be empty.");
            return result;
        }

        XlsxPivotTableFilter filter = new XlsxPivotTableFilter();
        filter.FieldIndex = fieldResult.FieldIndex;
        filter.HasFieldIndex = true;
        filter.Type = normalizedType;
        filter.Name = XlsxCellValueConverter.NormalizeText(filterName);
        filter.Id = (uint)(Filters.Count + 1);
        filter.HasId = true;
        XlsxPivotTableArea area = new XlsxPivotTableArea();
        area.Type = "normal";
        area.FieldIndex = fieldResult.FieldIndex;
        area.HasFieldIndex = true;
        XlsxPivotTableAreaReference reference = new XlsxPivotTableAreaReference();
        reference.FieldIndex = fieldResult.FieldIndex;
        reference.HasFieldIndex = true;
        reference.Count = 1;
        reference.HasCount = true;
        reference.Selected = true;
        reference.HasSelected = true;
        area.References.Add(reference);
        filter.Areas.Add(area);
        Filters.Add(filter);
        RebuildRawXmlAfterOperation(result);
        return result;
    }

    /// <summary>
    /// Sets style and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "styleName">The style name value used by the operation.</param>
    /// <param name = "showRowHeaders">The show row headers value used by the operation.</param>
    /// <param name = "showColumnHeaders">The show column headers value used by the operation.</param>
    /// <param name = "showRowStripes">The show row stripes value used by the operation.</param>
    /// <param name = "showColumnStripes">The show column stripes value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableOperationResult SetStyle(string styleName, bool showRowHeaders, bool showColumnHeaders, bool showRowStripes, bool showColumnStripes)
    {
        XlsxPivotTableOperationResult result = new XlsxPivotTableOperationResult();
        string normalized = XlsxCellValueConverter.NormalizeText(styleName);
        if (normalized.Length == 0)
        {
            result.AddError("pivotTableStyle.nameMissing", "Pivot table style name must not be empty.");
            return result;
        }

        StyleInfo = new XlsxPivotTableStyleInfo();
        StyleInfo.Name = normalized;
        StyleInfo.ShowRowHeaders = showRowHeaders;
        StyleInfo.HasShowRowHeaders = true;
        StyleInfo.ShowColumnHeaders = showColumnHeaders;
        StyleInfo.HasShowColumnHeaders = true;
        StyleInfo.ShowRowStripes = showRowStripes;
        StyleInfo.HasShowRowStripes = true;
        StyleInfo.ShowColumnStripes = showColumnStripes;
        StyleInfo.HasShowColumnStripes = true;
        HasStyleInfo = true;
        RebuildRawXmlAfterOperation(result);
        return result;
    }

    /// <summary>
    /// Adds axis field to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <param name = "axis">The axis value used by the operation.</param>
    /// <param name = "references">An A1-style cell or range reference.</param>
    /// <param name = "listName">The list name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableFieldOperationResult AddAxisField(string fieldName, string axis, List<XlsxPivotTableFieldReference> references, string listName)
    {
        XlsxPivotTableFieldOperationResult result = ResolveFieldForOperation(fieldName);
        if (!result.IsValid)
        {
            return result;
        }

        if (references == null)
        {
            result.AddError("pivotTableField." + listName + "Null", "Pivot table field reference list must not be null.");
            return result;
        }

        if (ContainsFieldReference(references, result.FieldIndex))
        {
            result.AddError("pivotTableField.duplicateAxisField", "Pivot table axis field list already contains the requested field.");
            return result;
        }

        RemoveFieldReference(RowFields, result.FieldIndex);
        RemoveFieldReference(ColumnFields, result.FieldIndex);
        RemoveFieldReference(PageFields, result.FieldIndex);
        XlsxPivotTableField field = Fields[result.FieldIndex];
        field.Axis = axis;
        XlsxPivotTableFieldReference reference = new XlsxPivotTableFieldReference();
        reference.FieldIndex = result.FieldIndex;
        reference.HasFieldIndex = true;
        reference.Name = result.FieldName;
        references.Add(reference);
        RebuildRawXmlAfterFieldOperation(result);
        return result;
    }

    /// <summary>
    /// Performs the resolve field for operation operation using the current model state.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxPivotTableFieldOperationResult ResolveFieldForOperation(string fieldName)
    {
        XlsxPivotTableFieldOperationResult result = new XlsxPivotTableFieldOperationResult();
        string normalized = XlsxCellValueConverter.NormalizeText(fieldName);
        result.FieldName = normalized;
        if (normalized.Length == 0)
        {
            result.AddError("pivotTableField.nameMissing", "Pivot table field name must not be empty.");
            return result;
        }

        if (Fields == null)
        {
            result.AddError("pivotTableField.fieldsNull", "Pivot table field list must not be null.");
            return result;
        }

        int index = FindFieldIndex(normalized);
        if (index < 0)
        {
            result.AddError("pivotTableField.notFound", "Pivot table field was not found.");
            return result;
        }

        result.FieldIndex = index;
        result.HasFieldIndex = true;
        return result;
    }

    /// <summary>
    /// Performs the find field index operation using the current model state.
    /// </summary>
    /// <param name = "fieldName">The field name value used by the operation.</param>
    /// <returns>The int value produced by the operation.</returns>
    internal int FindFieldIndex(string fieldName)
    {
        if (Fields == null)
        {
            return -1;
        }

        string normalized = XlsxCellValueConverter.NormalizeText(fieldName);
        int index = 0;
        foreach (XlsxPivotTableField field in Fields)
        {
            if (field != null && XlsxCellValueConverter.NormalizeText(field.Name) == normalized)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool ContainsFieldReference(List<XlsxPivotTableFieldReference> references, int fieldIndex)
    {
        if (references == null)
        {
            return false;
        }

        foreach (XlsxPivotTableFieldReference reference in references)
        {
            if (reference != null && reference.HasFieldIndex && reference.FieldIndex == fieldIndex)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContainsDataField(List<XlsxPivotTableDataField> dataFields, int fieldIndex)
    {
        if (dataFields == null)
        {
            return false;
        }

        foreach (XlsxPivotTableDataField dataField in dataFields)
        {
            if (dataField != null && dataField.HasFieldIndex && dataField.FieldIndex == fieldIndex)
            {
                return true;
            }
        }

        return false;
    }

    internal static void RemoveFieldReference(List<XlsxPivotTableFieldReference> references, int fieldIndex)
    {
        if (references == null)
        {
            return;
        }

        int index = 0;
        while (index < references.Count)
        {
            XlsxPivotTableFieldReference reference = references[index];
            if (reference != null && reference.HasFieldIndex && reference.FieldIndex == fieldIndex)
            {
                references.RemoveAt(index);
                continue;
            }

            index = index + 1;
        }
    }

    /// <summary>
    /// Performs the rebuild raw xml after field operation operation using the current model state.
    /// </summary>
    /// <param name = "result">The result value used by the operation.</param>
    internal void RebuildRawXmlAfterFieldOperation(XlsxPivotTableFieldOperationResult result)
    {
        XlsxPivotTableDefinitionWriteResult write = XlsxPivotTableDefinitionWriter.WriteStructuredString(this);
        foreach (XlsxDiagnostic diagnostic in write.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (write.IsValid)
        {
            RawXml = write.Xml;
        }
    }

    /// <summary>
    /// Performs the rebuild raw xml after operation operation using the current model state.
    /// </summary>
    /// <param name = "result">The result value used by the operation.</param>
    internal void RebuildRawXmlAfterOperation(XlsxPivotTableOperationResult result)
    {
        XlsxPivotTableDefinitionWriteResult write = XlsxPivotTableDefinitionWriter.WriteStructuredString(this);
        foreach (XlsxDiagnostic diagnostic in write.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (write.IsValid)
        {
            RawXml = write.Xml;
        }
    }

    /// <summary>
    /// Performs the contains calculated field operation using the current model state.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsCalculatedField(string name)
    {
        if (CalculatedFields == null)
        {
            return false;
        }

        string normalized = XlsxCellValueConverter.NormalizeText(name);
        foreach (XlsxPivotTableCalculatedField calculatedField in CalculatedFields)
        {
            if (calculatedField != null && XlsxCellValueConverter.NormalizeText(calculatedField.Name) == normalized)
            {
                return true;
            }
        }

        return false;
    }

    internal static void CopyFieldOperationDiagnostics(XlsxPivotTableOperationResult target, XlsxPivotTableFieldOperationResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }
}
