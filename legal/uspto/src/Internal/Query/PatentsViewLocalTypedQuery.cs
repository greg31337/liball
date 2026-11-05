namespace uspto;

internal class PatentsViewLocalTypedQueryResult
{
    internal object[] Records = new object[0];
    internal SearchResponseMetadata Metadata = new SearchResponseMetadata();
    internal int ScannedRowCount = 0;
    internal int MatchedRowCount = 0;
    internal bool ReachedScanLimit = false;
    internal bool ReachedResultLimit = false;
    internal bool IsComplete = false;
}

internal static class PatentsViewLocalTypedQuery
{
    internal static PatentsViewLocalTypedQueryResult QueryPatents(PatentsViewLocalRowQueryRequest request)
    {
        return ExecuteTyped(request, 1);
    }

    internal static PatentsViewLocalTypedQueryResult QueryPublications(PatentsViewLocalRowQueryRequest request)
    {
        return ExecuteTyped(request, 2);
    }

    internal static PatentsViewLocalTypedQueryResult QueryInventors(PatentsViewLocalRowQueryRequest request)
    {
        return ExecuteTyped(request, 3);
    }

    internal static PatentsViewLocalTypedQueryResult QueryAssignees(PatentsViewLocalRowQueryRequest request)
    {
        return ExecuteTyped(request, 4);
    }

    internal static PatentsViewLocalTypedQueryResult QueryClassifications(PatentsViewLocalRowQueryRequest request)
    {
        return ExecuteTyped(request, 5);
    }

    internal static PatentsViewLocalTypedQueryResult QueryCitations(PatentsViewLocalRowQueryRequest request)
    {
        return ExecuteTyped(request, 6);
    }

    internal static PatentsViewLocalTypedQueryResult ExecuteTyped(PatentsViewLocalRowQueryRequest request, int queryKind)
    {
        ValidateRequest(request, queryKind);
        PatentsViewLocalRowQueryResult local = PatentsViewLocalRowQuery.Execute(request);
        PatentsViewLocalTypedQueryResult result = new PatentsViewLocalTypedQueryResult();
        result.Records = new object[local.Rows.Length];
        result.Metadata = local.Metadata;
        result.ScannedRowCount = local.ScannedRowCount;
        result.MatchedRowCount = local.MatchedRowCount;
        result.ReachedScanLimit = local.ReachedScanLimit;
        result.ReachedResultLimit = local.ReachedResultLimit;
        result.IsComplete = local.IsComplete;
        int index;
        for (index = 0; index < local.Rows.Length; index += 1)
        {
            result.Records[index] = ParseTypedRecord(local.Rows[index], request.Table, queryKind);
        }

        return result;
    }

    internal static object ParseTypedRecord(PatentsViewDerivedTableRecord source, PatentsViewLocalTableInfo table, int queryKind)
    {
        DelimitedRow row = CreateDelimitedRow(source);
        PatentDataProvenance provenance = PatentsViewLocalRowQuery.CopyProvenance(source.Provenance, table.TableIdentifier);
        if (queryKind == 1)
        {
            if (table.Family != DatasetFamily.PatentsViewGrant || PatentsViewGrantBulkParser.ResolveTable(table.TableIdentifier, row) != PatentsViewGrantTable.Patent)
            {
                throw WrongTable("The selected local table does not contain patent records.");
            }

            PatentsViewPatent value = PatentsViewGrantBulkParser.ParsePatent(row, table.TableIdentifier, provenance);
            value.Provenance = provenance;
            return value;
        }

        if (queryKind == 2)
        {
            if (table.Family != DatasetFamily.PatentsViewPreGrant || PatentsViewPreGrantBulkParser.ResolveTable(table.TableIdentifier, row) != PatentsViewPreGrantTable.Publication)
            {
                throw WrongTable("The selected local table does not contain publication records.");
            }

            PatentsViewPublication value = PatentsViewPreGrantBulkParser.ParsePublication(row, table.TableIdentifier, provenance);
            value.Provenance = provenance;
            return value;
        }

        if (queryKind == 3)
        {
            ValidateEntityTable(table, row, true);
            PatentsViewInventor value = PatentsViewGrantBulkParser.ParseInventor(row, table.TableIdentifier, provenance);
            value.Provenance = provenance;
            return value;
        }

        if (queryKind == 4)
        {
            ValidateEntityTable(table, row, false);
            PatentsViewAssignee value = PatentsViewGrantBulkParser.ParseAssignee(row, table.TableIdentifier, provenance);
            value.Provenance = provenance;
            return value;
        }

        if (queryKind == 5)
        {
            return ParseClassification(row, table, provenance);
        }

        return ParseCitation(row, table, provenance);
    }

    internal static object ParseClassification(DelimitedRow row, PatentsViewLocalTableInfo table, PatentDataProvenance provenance)
    {
        int classificationKind = ResolveClassificationKind(row, table);
        if (classificationKind == 1)
        {
            PatentsViewCpcClassification value = PatentsViewGrantBulkParser.ParseCpc(row, table.TableIdentifier, provenance);
            value.Provenance = provenance;
            return value;
        }

        if (classificationKind == 2)
        {
            PatentsViewIpcClassification value = PatentsViewGrantBulkParser.ParseIpc(row, table.TableIdentifier, provenance);
            value.Provenance = provenance;
            return value;
        }

        if (classificationKind == 3)
        {
            PatentsViewUspcClassification value = PatentsViewGrantBulkParser.ParseUspc(row, table.TableIdentifier, provenance);
            value.Provenance = provenance;
            return value;
        }

        if (classificationKind == 4)
        {
            PatentsViewWipoClassification value = PatentsViewGrantBulkParser.ParseWipo(row, table.TableIdentifier, provenance);
            value.Provenance = provenance;
            return value;
        }

        throw WrongTable("The selected local table does not contain supported classification records.");
    }

    internal static object ParseCitation(DelimitedRow row, PatentsViewLocalTableInfo table, PatentDataProvenance provenance)
    {
        string citationType = ResolveCitationType(row, table);
        if (citationType.Length == 0)
        {
            throw WrongTable("The selected local table does not contain citation records.");
        }

        PatentsViewCitation value = PatentsViewGrantBulkParser.ParseCitation(row, table.TableIdentifier, provenance, citationType);
        value.Provenance = provenance;
        return value;
    }

    internal static DelimitedRow CreateDelimitedRow(PatentsViewDerivedTableRecord source)
    {
        DelimitedRow result = new DelimitedRow();
        result.RowNumber = source.RowNumber;
        result.StartLineNumber = source.StartLineNumber;
        result.EndLineNumber = source.EndLineNumber;
        result.StartOffset = source.StartOffset;
        result.Headers = source.Schema.Headers;
        result.Values = new string[result.Headers.Length];
        result.ExpectedColumnCount = result.Headers.Length;
        result.HasMissingColumns = source.HasMissingColumns;
        result.HasExtraColumns = source.HasExtraColumns;
        result.ExtraColumnCount = source.ExtraValues.Length;
        int index;
        for (index = 0; index < result.Headers.Length; index += 1)
        {
            result.Values[index] = source.ReadValue(result.Headers[index]) ?? string.Empty;
        }

        return result;
    }

    internal static void ValidateEntityTable(PatentsViewLocalTableInfo table, DelimitedRow row, bool inventor)
    {
        bool valid = false;
        if (table.Family == DatasetFamily.PatentsViewGrant)
        {
            PatentsViewGrantTable kind = PatentsViewGrantBulkParser.ResolveTable(table.TableIdentifier, row);
            valid = inventor && kind == PatentsViewGrantTable.Inventor || !inventor && kind == PatentsViewGrantTable.Assignee;
        }
        else if (table.Family == DatasetFamily.PatentsViewPreGrant)
        {
            PatentsViewPreGrantTable kind = PatentsViewPreGrantBulkParser.ResolveTable(table.TableIdentifier, row);
            valid = inventor && kind == PatentsViewPreGrantTable.Inventor || !inventor && kind == PatentsViewPreGrantTable.Assignee;
        }

        if (!valid)
        {
            string name = "assignee";
            if (inventor)
            {
                name = "inventor";
            }

            throw WrongTable("The selected local table does not contain " + name + " records.");
        }
    }

    internal static int ResolveClassificationKind(DelimitedRow row, PatentsViewLocalTableInfo table)
    {
        if (table.Family == DatasetFamily.PatentsViewGrant)
        {
            PatentsViewGrantTable kind = PatentsViewGrantBulkParser.ResolveTable(table.TableIdentifier, row);
            if (kind == PatentsViewGrantTable.Cpc)
            {
                return 1;
            }

            if (kind == PatentsViewGrantTable.Ipc)
            {
                return 2;
            }

            if (kind == PatentsViewGrantTable.Uspc)
            {
                return 3;
            }

            if (kind == PatentsViewGrantTable.Wipo)
            {
                return 4;
            }
        }
        else if (table.Family == DatasetFamily.PatentsViewPreGrant)
        {
            PatentsViewPreGrantTable kind = PatentsViewPreGrantBulkParser.ResolveTable(table.TableIdentifier, row);
            if (kind == PatentsViewPreGrantTable.Cpc)
            {
                return 1;
            }

            if (kind == PatentsViewPreGrantTable.Ipc)
            {
                return 2;
            }

            if (kind == PatentsViewPreGrantTable.Uspc)
            {
                return 3;
            }

            if (kind == PatentsViewPreGrantTable.Wipo)
            {
                return 4;
            }
        }

        return 0;
    }

    internal static string ResolveCitationType(DelimitedRow row, PatentsViewLocalTableInfo table)
    {
        int kind = 0;
        if (table.Family == DatasetFamily.PatentsViewGrant)
        {
            PatentsViewGrantTable grantKind = PatentsViewGrantBulkParser.ResolveTable(table.TableIdentifier, row);
            if (grantKind == PatentsViewGrantTable.UsPatentCitation)
            {
                kind = 1;
            }
            else if (grantKind == PatentsViewGrantTable.UsApplicationCitation)
            {
                kind = 2;
            }
            else if (grantKind == PatentsViewGrantTable.ForeignCitation)
            {
                kind = 3;
            }
            else if (grantKind == PatentsViewGrantTable.OtherReference)
            {
                kind = 4;
            }
        }
        else if (table.Family == DatasetFamily.PatentsViewPreGrant)
        {
            PatentsViewPreGrantTable publicationKind = PatentsViewPreGrantBulkParser.ResolveTable(table.TableIdentifier, row);
            if (publicationKind == PatentsViewPreGrantTable.UsPatentCitation)
            {
                kind = 1;
            }
            else if (publicationKind == PatentsViewPreGrantTable.UsApplicationCitation)
            {
                kind = 2;
            }
            else if (publicationKind == PatentsViewPreGrantTable.ForeignCitation)
            {
                kind = 3;
            }
            else if (publicationKind == PatentsViewPreGrantTable.OtherReference)
            {
                kind = 4;
            }
        }

        if (kind == 1)
        {
            return "us_patent";
        }

        if (kind == 2)
        {
            return "us_application";
        }

        if (kind == 3)
        {
            return "foreign";
        }

        if (kind == 4)
        {
            return "other_reference";
        }

        return string.Empty;
    }

    internal static void ValidateRequest(PatentsViewLocalRowQueryRequest request, int queryKind)
    {
        if (request == null || request.Table == null)
        {
            throw new ValidationException("A typed local query and table are required.", "request", "REQUIRED", null);
        }

        if (request.Table.Family != DatasetFamily.PatentsViewGrant && request.Table.Family != DatasetFamily.PatentsViewPreGrant)
        {
            throw new ValidationException("Typed local queries require a PatentsView grant or pre-grant mirror table.", "Table.Family", "UNSUPPORTED_DATASET_FAMILY", null);
        }

        if (queryKind < 1 || queryKind > 6)
        {
            throw new ValidationException("The typed local query kind is unsupported.", "queryKind", "UNSUPPORTED_LOCAL_QUERY", null);
        }
    }

    internal static ValidationException WrongTable(string message)
    {
        return new ValidationException(message, "Table.TableIdentifier", "LOCAL_TABLE_TYPE_MISMATCH", null);
    }
}
