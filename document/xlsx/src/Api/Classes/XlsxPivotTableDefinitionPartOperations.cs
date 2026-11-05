using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxPivotTableDefinitionPart"/> data.
/// </summary>
public class XlsxPivotTableDefinitionPartOperations : IXlsxPivotTableDefinitionPartOperations
{
    /// <summary>
    /// Initializes a new xlsx pivot table definition part operations instance.
    /// </summary>
    public XlsxPivotTableDefinitionPartOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.ResolveCacheRelationship through the ResolveCacheRelationship operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableCacheRelationshipResult ResolveCacheRelationship(XlsxPivotTableDefinitionPart data, XlsxRelationshipsPart relationships)
    {
        return data.ResolveCacheRelationship(relationships);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddRowField through the AddRowField operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableFieldOperationResult AddRowField(XlsxPivotTableDefinitionPart data, string fieldName)
    {
        return data.AddRowField(fieldName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddColumnField through the AddColumnField operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableFieldOperationResult AddColumnField(XlsxPivotTableDefinitionPart data, string fieldName)
    {
        return data.AddColumnField(fieldName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddPageField through the AddPageField operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableFieldOperationResult AddPageField(XlsxPivotTableDefinitionPart data, string fieldName)
    {
        return data.AddPageField(fieldName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddPageFieldWithItem through the AddPageFieldWithItem operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <param name="itemIndex">The item index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableFieldOperationResult AddPageFieldWithItem(XlsxPivotTableDefinitionPart data, string fieldName, int itemIndex)
    {
        return data.AddPageFieldWithItem(fieldName, itemIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddDataField through the AddDataField operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableFieldOperationResult AddDataField(XlsxPivotTableDefinitionPart data, string fieldName)
    {
        return data.AddDataField(fieldName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddDataFieldWithOptions through the AddDataFieldWithOptions operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <param name="subtotal">The subtotal value used by the operation.</param>
    /// <param name="displayName">The display name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableFieldOperationResult AddDataFieldWithOptions(XlsxPivotTableDefinitionPart data, string fieldName, string subtotal, string displayName)
    {
        return data.AddDataFieldWithOptions(fieldName, subtotal, displayName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.GroupFieldItems through the GroupFieldItems operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <param name="groupNames">The group names value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableOperationResult GroupFieldItems(XlsxPivotTableDefinitionPart data, string fieldName, List<string> groupNames)
    {
        return data.GroupFieldItems(fieldName, groupNames);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddCalculatedField through the AddCalculatedField operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableOperationResult AddCalculatedField(XlsxPivotTableDefinitionPart data, string name, string formula)
    {
        return data.AddCalculatedField(name, formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddFilter through the AddFilter operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <param name="filterType">The filter type value used by the operation.</param>
    /// <param name="filterName">The filter name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableOperationResult AddFilter(XlsxPivotTableDefinitionPart data, string fieldName, string filterType, string filterName)
    {
        return data.AddFilter(fieldName, filterType, filterName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.SetStyle through the SetStyle operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="styleName">The style name value used by the operation.</param>
    /// <param name="showRowHeaders">The show row headers value used by the operation.</param>
    /// <param name="showColumnHeaders">The show column headers value used by the operation.</param>
    /// <param name="showRowStripes">The show row stripes value used by the operation.</param>
    /// <param name="showColumnStripes">The show column stripes value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableOperationResult SetStyle(XlsxPivotTableDefinitionPart data, string styleName, bool showRowHeaders, bool showColumnHeaders, bool showRowStripes, bool showColumnStripes)
    {
        return data.SetStyle(styleName, showRowHeaders, showColumnHeaders, showRowStripes, showColumnStripes);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.AddAxisField through the AddAxisField operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <param name="axis">The axis value used by the operation.</param>
    /// <param name="references">The references value used by the operation.</param>
    /// <param name="listName">The list name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableFieldOperationResult AddAxisField(XlsxPivotTableDefinitionPart data, string fieldName, string axis, List<XlsxPivotTableFieldReference> references, string listName)
    {
        return data.AddAxisField(fieldName, axis, references, listName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.ResolveFieldForOperation through the ResolveFieldForOperation operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxPivotTableFieldOperationResult ResolveFieldForOperation(XlsxPivotTableDefinitionPart data, string fieldName)
    {
        return data.ResolveFieldForOperation(fieldName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.FindFieldIndex through the FindFieldIndex operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="fieldName">The field name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public int FindFieldIndex(XlsxPivotTableDefinitionPart data, string fieldName)
    {
        return data.FindFieldIndex(fieldName);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.RebuildRawXmlAfterFieldOperation through the RebuildRawXmlAfterFieldOperation operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="result">The result value used by the operation.</param>
    public void RebuildRawXmlAfterFieldOperation(XlsxPivotTableDefinitionPart data, XlsxPivotTableFieldOperationResult result)
    {
        data.RebuildRawXmlAfterFieldOperation(result);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.RebuildRawXmlAfterOperation through the RebuildRawXmlAfterOperation operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="result">The result value used by the operation.</param>
    public void RebuildRawXmlAfterOperation(XlsxPivotTableDefinitionPart data, XlsxPivotTableOperationResult result)
    {
        data.RebuildRawXmlAfterOperation(result);
    }

    /// <summary>
    /// Invokes <see cref="XlsxPivotTableDefinitionPart"/>.ContainsCalculatedField through the ContainsCalculatedField operation.
    /// </summary>
    /// <param name="data">The xlsx pivot table definition part data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool ContainsCalculatedField(XlsxPivotTableDefinitionPart data, string name)
    {
        return data.ContainsCalculatedField(name);
    }
}
