namespace uspto;

internal class BulkParserContext
{
    internal string? ProductIdentifier = null;
    internal string? FileName = null;
    internal string? MediaType = null;
    internal string? SchemaMarker = null;
    internal byte[] Prefix = new byte[0];
    internal UsptoLimits Limits = new UsptoLimits();
}

internal class BulkParserRoute
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal DocumentFormat Format = DocumentFormat.Unknown;
    internal CompressionFormat Compression = CompressionFormat.Unknown;
    internal string ParserName = string.Empty;
    internal string SelectionReason = string.Empty;
    internal bool IsTyped = false;
    internal bool FormatDetectedBySignature = false;
    internal bool FamilyDetectedBySchema = false;
    internal object? Record = null;
}

internal static class BulkParserRouter
{
    internal static BulkParserRoute SelectRoute(BulkParserContext context)
    {
        ValidateRouterContext(context);
        string prefixText = DecodeRouterPrefix(context.Prefix);
        return CreateRouterSelection(context, prefixText, DocumentFormat.Unknown);
    }

    internal static BulkParserRoute RouteXmlRecord(XmlRecord record, BulkParserContext context, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("A generic XML record is required for bulk parser routing.", "record", "REQUIRED", null);
        }

        ValidateRouterInputs(context, provenance);
        BulkParserRoute result = CreateRouterSelection(context, record.RawXml, DocumentFormat.Xml);
        if (!result.IsTyped || !SupportsRouterTypedContent(result.Family, DocumentFormat.Xml, record.RawXml))
        {
            result.IsTyped = false;
            result.ParserName = "generic-xml";
            result.Record = record;
            return result;
        }

        switch (result.Family)
        {
            case DatasetFamily.PatentFileWrapperComplete:
            case DatasetFamily.PatentFileWrapperDelta:
                result.Record = PatentFileWrapperBulkParser.ParseXmlRecord(record, provenance);
                break;
            case DatasetFamily.PatentGrantFullText:
            case DatasetFamily.PatentApplicationFullText:
                byte[] xml = global::System.Text.Encoding.UTF8.GetBytes(record.RawXml);
                using (global::System.IO.MemoryStream input = new global::System.IO.MemoryStream(xml, false))
                {
                    result.Record = PatentFullTextXmlParser.Parse(input, context.FileName, context.Limits, provenance);
                }

                break;
            case DatasetFamily.PatentAssignment:
                result.Record = PatentAssignmentXmlParser.ParseRecord(record, provenance);
                break;
            case DatasetFamily.TrademarkApplicationAndStatus:
                result.Record = TrademarkDailyXmlParser.ParseRecord(record, provenance);
                break;
            case DatasetFamily.TrademarkAssignment:
                result.Record = TrademarkAssignmentXmlParser.ParseRecord(record, provenance);
                break;
            case DatasetFamily.Ttab:
                TrademarkTtabXmlRecord ttab = TrademarkTtabXmlParser.ParseRecord(record, provenance);
                if (ttab.IsTyped)
                {
                    result.Record = ttab;
                }
                else
                {
                    result.IsTyped = false;
                    result.ParserName = "generic-xml";
                    result.Record = record;
                }

                break;
            default:
                result.IsTyped = false;
                result.ParserName = "generic-xml";
                result.Record = record;
                break;
        }

        return result;
    }

    internal static BulkParserRoute RouteJsonRecord(JsonRecord record, BulkParserContext context, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("A generic JSON record is required for bulk parser routing.", "record", "REQUIRED", null);
        }

        ValidateRouterInputs(context, provenance);
        BulkParserRoute result = CreateRouterSelection(context, record.RawJson, DocumentFormat.Json);
        if (!result.IsTyped || !SupportsRouterTypedContent(result.Family, DocumentFormat.Json, record.RawJson))
        {
            result.IsTyped = false;
            result.ParserName = "generic-json";
            result.Record = record;
            return result;
        }

        switch (result.Family)
        {
            case DatasetFamily.PatentFileWrapperComplete:
            case DatasetFamily.PatentFileWrapperDelta:
                result.Record = PatentFileWrapperBulkParser.ParseJsonRecord(record, provenance);
                break;
            case DatasetFamily.OfficeAction:
                result.Record = OfficeActionArchiveParser.ParseJsonRecord(record, ResolveOfficeActionHint(context), provenance);
                break;
            default:
                result.IsTyped = false;
                result.ParserName = "generic-json";
                result.Record = record;
                break;
        }

        return result;
    }

    internal static BulkParserRoute RouteDelimitedRow(DelimitedRow row, char delimiter, BulkParserContext context, PatentDataProvenance provenance)
    {
        if (row == null)
        {
            throw new ValidationException("A generic delimited row is required for bulk parser routing.", "row", "REQUIRED", null);
        }

        if (delimiter != ',' && delimiter != '\t')
        {
            throw new ValidationException("The routed delimiter must be a comma or tab.", "delimiter", "UNSUPPORTED_DELIMITER", null);
        }

        ValidateRouterInputs(context, provenance);
        global::System.Text.StringBuilder schema = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < row.Headers.Length; index += 1)
        {
            schema.Append(row.Headers[index]);
            schema.Append(' ');
        }

        DocumentFormat format = DocumentFormat.Csv;
        if (delimiter == '\t')
        {
            format = DocumentFormat.Text;
        }

        BulkParserRoute result = CreateRouterSelection(context, schema.ToString(), format);
        if (result.IsTyped && result.Family == DatasetFamily.OfficeAction && SupportsRouterTypedContent(result.Family, format, schema.ToString()))
        {
            result.Record = OfficeActionArchiveParser.ParseDelimitedRow(row, ResolveOfficeActionHint(context), provenance);
        }
        else if (result.IsTyped && result.Family == DatasetFamily.PatentsViewGrant && delimiter == '\t' && PatentsViewGrantBulkParser.CanParseRow(row, ResolvePatentsViewGrantHint(context)))
        {
            result.Record = PatentsViewGrantBulkParser.ParseRow(row, ResolvePatentsViewGrantHint(context), provenance);
        }
        else if (result.IsTyped && result.Family == DatasetFamily.PatentsViewPreGrant && delimiter == '\t' && PatentsViewPreGrantBulkParser.CanParseRow(row, ResolvePatentsViewPreGrantHint(context)))
        {
            result.Record = PatentsViewPreGrantBulkParser.ParseRow(row, ResolvePatentsViewPreGrantHint(context), provenance);
        }
        else if (result.IsTyped && (result.Family == DatasetFamily.PatentsViewAnnualized || result.Family == DatasetFamily.PatentsViewSorted))
        {
            result.Record = PatentsViewDerivedTableParser.ParseRow(row, ResolvePatentsViewDerivedHint(context), result.Family, provenance);
        }
        else
        {
            result.IsTyped = false;
            result.ParserName = "generic-delimited";
            result.Record = row;
        }

        return result;
    }

    internal static BulkParserRoute CreateRouterSelection(BulkParserContext context, string content, DocumentFormat forcedFormat)
    {
        BulkParserRoute result = new BulkParserRoute();
        result.Compression = DetectRouterCompression(context.Prefix, context.FileName);
        result.Format = forcedFormat;
        if (result.Format == DocumentFormat.Unknown)
        {
            result.Format = DetectRouterFormat(context, result.Compression);
            result.FormatDetectedBySignature = HasRouterContentSignature(context.Prefix);
        }

        DatasetFamily contentFamily = DetectRouterFamily(content);
        if (contentFamily != DatasetFamily.Unknown)
        {
            result.Family = contentFamily;
            result.SelectionReason = "content-schema";
            result.FamilyDetectedBySchema = true;
        }
        else
        {
            DatasetFamily markerFamily = DetectRouterFamily(context.SchemaMarker ?? string.Empty);
            if (markerFamily != DatasetFamily.Unknown)
            {
                result.Family = markerFamily;
                result.SelectionReason = "schema-marker";
                result.FamilyDetectedBySchema = true;
            }
            else
            {
                result.Family = DetectRouterProduct(context.ProductIdentifier);
                if (result.Family != DatasetFamily.Unknown)
                {
                    result.SelectionReason = "product-identifier";
                }
                else
                {
                    result.Family = DetectRouterFileName(context.FileName);
                    if (result.Family != DatasetFamily.Unknown)
                    {
                        result.SelectionReason = "filename";
                    }
                    else
                    {
                        result.SelectionReason = "generic";
                    }
                }
            }
        }

        if (result.Format == DocumentFormat.Unknown)
        {
            result.Format = InferRouterFamilyFormat(result.Family);
        }

        result.IsTyped = HasRouterTypedParser(result.Family, result.Format);
        result.ParserName = ResolveRouterParserName(result.Family, result.Format, result.IsTyped);
        if (forcedFormat == DocumentFormat.Unknown && result.Compression != CompressionFormat.None && result.Compression != CompressionFormat.Unknown)
        {
            result.IsTyped = false;
            result.ParserName = "archive";
        }

        return result;
    }

    internal static DatasetFamily DetectRouterFamily(string evidence)
    {
        string value = NormalizeRouterEvidence(evidence);
        if (value.Length == 0)
        {
            return DatasetFamily.Unknown;
        }

        if (value.IndexOf("patentsviewgrant", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("gpatent", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentsViewGrant;
        }

        if (value.IndexOf("patentsviewpregrant", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("pgpublication", global::System.StringComparison.Ordinal) || value.StartsWith("pgpub", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentsViewPreGrant;
        }

        if (value.IndexOf("patentsviewannualized", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("patentsviewannualised", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.PatentsViewAnnualized;
        }

        if (value.IndexOf("patentsviewsorted", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.PatentsViewSorted;
        }

        if (value.IndexOf("trademarkassignment", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("trtdxfag", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.TrademarkAssignment;
        }

        if (value.IndexOf("patentassignment", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("pasdl", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.PatentAssignment;
        }

        if (value.IndexOf("uspatentgrant", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("ptgrxml", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("ptblxml", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.PatentGrantFullText;
        }

        if (value.IndexOf("uspatentapplication", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("appxml", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("appdt", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.PatentApplicationFullText;
        }

        if (value.IndexOf("patentfilewrapper", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("patentfilewrapperdatabag", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("ptfwpre", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.PatentFileWrapperComplete;
        }

        if (value.IndexOf("ttabproceedings", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("ttabtdxf", global::System.StringComparison.Ordinal) >= 0 || value.Equals("ttab", global::System.StringComparison.Ordinal) || value.IndexOf("proceedingentry", global::System.StringComparison.Ordinal) >= 0 && value.IndexOf("prosecutionhistory", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.Ttab;
        }

        if (value.IndexOf("trademarktransaction", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("trademarkcasefile", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("trtdxfap", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("casefile", global::System.StringComparison.Ordinal) >= 0 && value.IndexOf("serialnumber", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.TrademarkApplicationAndStatus;
        }

        if (value.IndexOf("officeaction", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("oaactions", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("oacitation", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("oarejection", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("enrichedcitedreference", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("hasrej", global::System.StringComparison.Ordinal) >= 0 || value.Equals("oact", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.OfficeAction;
        }

        return DatasetFamily.Unknown;
    }

    internal static DatasetFamily DetectRouterProduct(string? productIdentifier)
    {
        string value = NormalizeRouterEvidence(productIdentifier ?? string.Empty);
        if (value.Equals("pvgrant", global::System.StringComparison.Ordinal) || value.Equals("patentsviewgrant", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentsViewGrant;
        }

        if (value.Equals("pvpregrant", global::System.StringComparison.Ordinal) || value.Equals("patentsviewpregrant", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentsViewPreGrant;
        }

        if (value.Equals("pvannual", global::System.StringComparison.Ordinal) || value.Equals("pvannualized", global::System.StringComparison.Ordinal) || value.Equals("patentsviewannualized", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentsViewAnnualized;
        }

        if (value.Equals("pvsorted", global::System.StringComparison.Ordinal) || value.Equals("patentsviewsorted", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentsViewSorted;
        }

        if (value.Equals("ptfwpre", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentFileWrapperComplete;
        }

        if (value.Equals("ptgrxml", global::System.StringComparison.Ordinal) || value.Equals("ptblxml", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentGrantFullText;
        }

        if (value.Equals("appxml", global::System.StringComparison.Ordinal) || value.Equals("appdt", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentApplicationFullText;
        }

        if (value.Equals("pasdl", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentAssignment;
        }

        if (value.Equals("oact", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.OfficeAction;
        }

        if (value.Equals("trtdxfap", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.TrademarkApplicationAndStatus;
        }

        if (value.Equals("trtdxfag", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.TrademarkAssignment;
        }

        if (value.Equals("ttab", global::System.StringComparison.Ordinal) || value.Equals("ttabtdxf", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.Ttab;
        }

        if (value.StartsWith("ptab", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.Ptab;
        }

        return DatasetFamily.Unknown;
    }

    internal static DatasetFamily DetectRouterFileName(string? fileName)
    {
        string value = NormalizeRouterEvidence(fileName ?? string.Empty);
        if (value.IndexOf("annualized", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("annualised", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.PatentsViewAnnualized;
        }

        if (value.IndexOf("sorted", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.PatentsViewSorted;
        }

        if (value.StartsWith("gpatent", global::System.StringComparison.Ordinal) || value.StartsWith("gapplication", global::System.StringComparison.Ordinal) || value.StartsWith("ginventor", global::System.StringComparison.Ordinal) || value.StartsWith("gassignee", global::System.StringComparison.Ordinal) || value.StartsWith("gapplicant", global::System.StringComparison.Ordinal) || value.StartsWith("glocation", global::System.StringComparison.Ordinal) || value.StartsWith("glawyer", global::System.StringComparison.Ordinal) || value.StartsWith("gattorney", global::System.StringComparison.Ordinal) || value.StartsWith("gcpc", global::System.StringComparison.Ordinal) || value.StartsWith("gipc", global::System.StringComparison.Ordinal) || value.StartsWith("guspc", global::System.StringComparison.Ordinal) || value.StartsWith("gwipo", global::System.StringComparison.Ordinal) || value.StartsWith("gclaim", global::System.StringComparison.Ordinal) || value.StartsWith("gbrfsum", global::System.StringComparison.Ordinal) || value.StartsWith("gdetaildesc", global::System.StringComparison.Ordinal) || value.StartsWith("gdrawdesc", global::System.StringComparison.Ordinal) || value.StartsWith("ggovinterest", global::System.StringComparison.Ordinal) || value.StartsWith("grelapp", global::System.StringComparison.Ordinal) || value.StartsWith("gforeigncitation", global::System.StringComparison.Ordinal) || value.StartsWith("gotherreference", global::System.StringComparison.Ordinal) || value.StartsWith("guspatentcitation", global::System.StringComparison.Ordinal) || value.StartsWith("gusapplicationcitation", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentsViewGrant;
        }

        if (value.StartsWith("pgpublication", global::System.StringComparison.Ordinal) || value.StartsWith("pgpublished", global::System.StringComparison.Ordinal) || value.StartsWith("pgpub", global::System.StringComparison.Ordinal) || value.StartsWith("pginventor", global::System.StringComparison.Ordinal) || value.StartsWith("pgassignee", global::System.StringComparison.Ordinal) || value.StartsWith("pgapplicant", global::System.StringComparison.Ordinal) || value.StartsWith("pgusparty", global::System.StringComparison.Ordinal) || value.StartsWith("pglocation", global::System.StringComparison.Ordinal) || value.StartsWith("pgcpc", global::System.StringComparison.Ordinal) || value.StartsWith("pgipc", global::System.StringComparison.Ordinal) || value.StartsWith("pguspc", global::System.StringComparison.Ordinal) || value.StartsWith("pgwipo", global::System.StringComparison.Ordinal) || value.StartsWith("pgclaim", global::System.StringComparison.Ordinal) || value.StartsWith("pgbrfsum", global::System.StringComparison.Ordinal) || value.StartsWith("pgdetaildesc", global::System.StringComparison.Ordinal) || value.StartsWith("pgdrawdesc", global::System.StringComparison.Ordinal) || value.StartsWith("pgrelapp", global::System.StringComparison.Ordinal) || value.StartsWith("pgforeigncitation", global::System.StringComparison.Ordinal) || value.StartsWith("pgotherreference", global::System.StringComparison.Ordinal) || value.StartsWith("pguspatentcitation", global::System.StringComparison.Ordinal) || value.StartsWith("pgusapplicationcitation", global::System.StringComparison.Ordinal) || value.StartsWith("grantedpregrantcrosswalk", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentsViewPreGrant;
        }

        if (value.IndexOf("patentfilewrapper", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("ptfw", global::System.StringComparison.Ordinal))
        {
            if (value.IndexOf("delta", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("update", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("weekly", global::System.StringComparison.Ordinal) >= 0)
            {
                return DatasetFamily.PatentFileWrapperDelta;
            }

            return DatasetFamily.PatentFileWrapperComplete;
        }

        if (value.IndexOf("trademarkassignment", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("tmassign", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("asb", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.TrademarkAssignment;
        }

        if (value.IndexOf("patentassignment", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("pasdl", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentAssignment;
        }

        if (value.IndexOf("ttab", global::System.StringComparison.Ordinal) >= 0 || StartsWithRouterLettersAndDigit(value, "tt"))
        {
            return DatasetFamily.Ttab;
        }

        if (value.IndexOf("trademarkapplication", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("trademarkstatus", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("apc", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.TrademarkApplicationAndStatus;
        }

        if (value.IndexOf("officeaction", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("oaactions", global::System.StringComparison.Ordinal) || value.StartsWith("oacitations", global::System.StringComparison.Ordinal) || value.StartsWith("oarejections", global::System.StringComparison.Ordinal) || value.IndexOf("enrichedcitedreference", global::System.StringComparison.Ordinal) >= 0)
        {
            return DatasetFamily.OfficeAction;
        }

        if (value.IndexOf("patentgrant", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("ptgr", global::System.StringComparison.Ordinal) || value.StartsWith("ipg", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentGrantFullText;
        }

        if (value.IndexOf("patentapplication", global::System.StringComparison.Ordinal) >= 0 || value.StartsWith("appxml", global::System.StringComparison.Ordinal) || value.StartsWith("ipa", global::System.StringComparison.Ordinal))
        {
            return DatasetFamily.PatentApplicationFullText;
        }

        return DatasetFamily.Unknown;
    }

    internal static bool StartsWithRouterLettersAndDigit(string value, string letters)
    {
        return value.Length > letters.Length && value.StartsWith(letters, global::System.StringComparison.Ordinal) && global::System.Char.IsDigit(value[letters.Length]);
    }

    internal static CompressionFormat DetectRouterCompression(byte[] prefix, string? fileName)
    {
        if (ArchiveFormatDetector.HasZipSignature(prefix))
        {
            return CompressionFormat.Zip;
        }

        if (ArchiveFormatDetector.HasGzipSignature(prefix))
        {
            return CompressionFormat.Gzip;
        }

        if (ArchiveFormatDetector.HasTarSignature(prefix))
        {
            return CompressionFormat.Tar;
        }

        string name = fileName ?? string.Empty;
        if (name.EndsWith(".tar.gz", global::System.StringComparison.OrdinalIgnoreCase) || name.EndsWith(".tgz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.TarGzip;
        }

        if (name.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.Zip;
        }

        if (name.EndsWith(".gz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.Gzip;
        }

        if (name.EndsWith(".tar", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return CompressionFormat.Tar;
        }

        return CompressionFormat.None;
    }

    internal static DocumentFormat DetectRouterFormat(BulkParserContext context, CompressionFormat compression)
    {
        if (compression == CompressionFormat.Zip)
        {
            return DocumentFormat.Zip;
        }

        DocumentFormat signature = DetectRouterContentFormat(context.Prefix);
        if (signature != DocumentFormat.Unknown)
        {
            return signature;
        }

        DocumentFormat media = DetectRouterMediaFormat(context.MediaType);
        if (media != DocumentFormat.Unknown)
        {
            return media;
        }

        return DetectRouterFileFormat(context.FileName);
    }

    internal static DocumentFormat DetectRouterContentFormat(byte[] prefix)
    {
        if (prefix.Length >= 4 && prefix[0] == 0x25 && prefix[1] == 0x50 && prefix[2] == 0x44 && prefix[3] == 0x46)
        {
            return DocumentFormat.Pdf;
        }

        int index = FindRouterSignificantByte(prefix);
        if (index >= 0)
        {
            if (prefix[index] == 0x3c)
            {
                return DocumentFormat.Xml;
            }

            if (prefix[index] == 0x7b || prefix[index] == 0x5b)
            {
                return DocumentFormat.Json;
            }
        }

        if (prefix.Length >= 4 && prefix[0] == 0xff && prefix[1] == 0xfe && prefix[2] == 0x3c && prefix[3] == 0x00 || prefix.Length >= 4 && prefix[0] == 0xfe && prefix[1] == 0xff && prefix[2] == 0x00 && prefix[3] == 0x3c)
        {
            return DocumentFormat.Xml;
        }

        return DocumentFormat.Unknown;
    }

    internal static int FindRouterSignificantByte(byte[] prefix)
    {
        int index = 0;
        if (prefix.Length >= 3 && prefix[0] == 0xef && prefix[1] == 0xbb && prefix[2] == 0xbf)
        {
            index = 3;
        }

        while (index < prefix.Length)
        {
            byte value = prefix[index];
            if (value != 0x20 && value != 0x09 && value != 0x0a && value != 0x0d)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static bool HasRouterContentSignature(byte[] prefix)
    {
        return ArchiveFormatDetector.HasZipSignature(prefix) || ArchiveFormatDetector.HasGzipSignature(prefix) || ArchiveFormatDetector.HasTarSignature(prefix) || DetectRouterContentFormat(prefix) != DocumentFormat.Unknown;
    }

    internal static DocumentFormat DetectRouterMediaFormat(string? mediaType)
    {
        string value = (mediaType ?? string.Empty).ToLowerInvariant();
        if (value.IndexOf("json", global::System.StringComparison.Ordinal) >= 0)
        {
            return DocumentFormat.Json;
        }

        if (value.IndexOf("xml", global::System.StringComparison.Ordinal) >= 0)
        {
            return DocumentFormat.Xml;
        }

        if (value.IndexOf("csv", global::System.StringComparison.Ordinal) >= 0)
        {
            return DocumentFormat.Csv;
        }

        if (value.IndexOf("tab-separated", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("text/plain", global::System.StringComparison.Ordinal) >= 0)
        {
            return DocumentFormat.Text;
        }

        if (value.IndexOf("pdf", global::System.StringComparison.Ordinal) >= 0)
        {
            return DocumentFormat.Pdf;
        }

        if (value.IndexOf("zip", global::System.StringComparison.Ordinal) >= 0)
        {
            return DocumentFormat.Zip;
        }

        return DocumentFormat.Unknown;
    }

    internal static DocumentFormat DetectRouterFileFormat(string? fileName)
    {
        string value = fileName ?? string.Empty;
        if (value.EndsWith(".xml", global::System.StringComparison.OrdinalIgnoreCase) || value.EndsWith(".xml.gz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Xml;
        }

        if (value.EndsWith(".json", global::System.StringComparison.OrdinalIgnoreCase) || value.EndsWith(".jsonl", global::System.StringComparison.OrdinalIgnoreCase) || value.EndsWith(".ndjson", global::System.StringComparison.OrdinalIgnoreCase) || value.EndsWith(".json.gz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Json;
        }

        if (value.EndsWith(".csv", global::System.StringComparison.OrdinalIgnoreCase) || value.EndsWith(".csv.gz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Csv;
        }

        if (value.EndsWith(".tsv", global::System.StringComparison.OrdinalIgnoreCase) || value.EndsWith(".txt", global::System.StringComparison.OrdinalIgnoreCase) || value.EndsWith(".tsv.gz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Text;
        }

        if (value.EndsWith(".pdf", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Pdf;
        }

        if (value.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Zip;
        }

        return DocumentFormat.Unknown;
    }

    internal static DocumentFormat InferRouterFamilyFormat(DatasetFamily family)
    {
        switch (family)
        {
            case DatasetFamily.PatentGrantFullText:
            case DatasetFamily.PatentApplicationFullText:
            case DatasetFamily.PatentAssignment:
            case DatasetFamily.TrademarkApplicationAndStatus:
            case DatasetFamily.TrademarkAssignment:
            case DatasetFamily.Ttab:
                return DocumentFormat.Xml;
            case DatasetFamily.PatentsViewGrant:
            case DatasetFamily.PatentsViewPreGrant:
            case DatasetFamily.PatentsViewAnnualized:
            case DatasetFamily.PatentsViewSorted:
                return DocumentFormat.Text;
            default:
                return DocumentFormat.Unknown;
        }
    }

    internal static bool HasRouterTypedParser(DatasetFamily family, DocumentFormat format)
    {
        if (format == DocumentFormat.Xml)
        {
            return family == DatasetFamily.PatentFileWrapperComplete || family == DatasetFamily.PatentFileWrapperDelta || family == DatasetFamily.PatentGrantFullText || family == DatasetFamily.PatentApplicationFullText || family == DatasetFamily.PatentAssignment || family == DatasetFamily.TrademarkApplicationAndStatus || family == DatasetFamily.TrademarkAssignment || family == DatasetFamily.Ttab;
        }

        if (format == DocumentFormat.Json)
        {
            return family == DatasetFamily.PatentFileWrapperComplete || family == DatasetFamily.PatentFileWrapperDelta || family == DatasetFamily.OfficeAction;
        }

        if (format == DocumentFormat.Csv || format == DocumentFormat.Text)
        {
            return family == DatasetFamily.OfficeAction || format == DocumentFormat.Text && (family == DatasetFamily.PatentsViewGrant || family == DatasetFamily.PatentsViewPreGrant) || family == DatasetFamily.PatentsViewAnnualized || family == DatasetFamily.PatentsViewSorted;
        }

        return false;
    }

    internal static bool SupportsRouterTypedContent(DatasetFamily family, DocumentFormat format, string content)
    {
        string value = NormalizeRouterEvidence(content);
        if (format == DocumentFormat.Xml)
        {
            switch (family)
            {
                case DatasetFamily.PatentFileWrapperComplete:
                case DatasetFamily.PatentFileWrapperDelta:
                    return value.IndexOf("patentfilewrapper", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("applicationnumbertext", global::System.StringComparison.Ordinal) >= 0;
                case DatasetFamily.PatentGrantFullText:
                    return value.IndexOf("uspatentgrant", global::System.StringComparison.Ordinal) >= 0;
                case DatasetFamily.PatentApplicationFullText:
                    return value.IndexOf("uspatentapplication", global::System.StringComparison.Ordinal) >= 0;
                case DatasetFamily.PatentAssignment:
                case DatasetFamily.TrademarkAssignment:
                    return value.IndexOf("assignment", global::System.StringComparison.Ordinal) >= 0 && value.IndexOf("reel", global::System.StringComparison.Ordinal) >= 0 && value.IndexOf("frame", global::System.StringComparison.Ordinal) >= 0;
                case DatasetFamily.TrademarkApplicationAndStatus:
                    return value.IndexOf("casefile", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("trademarktransaction", global::System.StringComparison.Ordinal) >= 0;
                case DatasetFamily.Ttab:
                    return value.IndexOf("ttabproceedings", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("proceedingentry", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("ttabproceeding", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("proceedingnumber", global::System.StringComparison.Ordinal) >= 0;
                default:
                    return false;
            }
        }

        if (format == DocumentFormat.Json)
        {
            if (family == DatasetFamily.PatentFileWrapperComplete || family == DatasetFamily.PatentFileWrapperDelta)
            {
                return value.IndexOf("applicationnumbertext", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("patentfilewrapperdatabag", global::System.StringComparison.Ordinal) >= 0;
            }

            if (family == DatasetFamily.OfficeAction)
            {
                return value.IndexOf("applicationnumber", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("bodytext", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("referenceidentifier", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("claimnumber", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("citeddocumentidentifier", global::System.StringComparison.Ordinal) >= 0;
            }

            return false;
        }

        if ((format == DocumentFormat.Csv || format == DocumentFormat.Text) && family == DatasetFamily.OfficeAction)
        {
            return value.IndexOf("applicationnumber", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("bodytext", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("referenceidentifier", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("claimnumber", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("citeddocumentidentifier", global::System.StringComparison.Ordinal) >= 0;
        }

        if (format == DocumentFormat.Text && family == DatasetFamily.PatentsViewGrant)
        {
            return value.IndexOf("patentid", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("inventorid", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("assigneeid", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("locationid", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("citation", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("classification", global::System.StringComparison.Ordinal) >= 0;
        }

        if (format == DocumentFormat.Text && family == DatasetFamily.PatentsViewPreGrant)
        {
            return value.IndexOf("documentnumber", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("inventorid", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("assigneeid", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("locationid", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("citation", global::System.StringComparison.Ordinal) >= 0 || value.IndexOf("classification", global::System.StringComparison.Ordinal) >= 0;
        }

        if ((format == DocumentFormat.Csv || format == DocumentFormat.Text) && (family == DatasetFamily.PatentsViewAnnualized || family == DatasetFamily.PatentsViewSorted))
        {
            return value.Length > 0;
        }

        return false;
    }

    internal static string ResolveRouterParserName(DatasetFamily family, DocumentFormat format, bool typed)
    {
        if (!typed)
        {
            if (format == DocumentFormat.Xml)
            {
                return "generic-xml";
            }

            if (format == DocumentFormat.Json)
            {
                return "generic-json";
            }

            if (format == DocumentFormat.Csv || format == DocumentFormat.Text)
            {
                return "generic-delimited";
            }

            return "generic-binary";
        }

        switch (family)
        {
            case DatasetFamily.PatentFileWrapperComplete:
            case DatasetFamily.PatentFileWrapperDelta:
                return "patent-file-wrapper";
            case DatasetFamily.PatentGrantFullText:
            case DatasetFamily.PatentApplicationFullText:
                return "patent-full-text";
            case DatasetFamily.PatentAssignment:
                return "patent-assignment";
            case DatasetFamily.OfficeAction:
                return "office-action";
            case DatasetFamily.TrademarkApplicationAndStatus:
                return "trademark-daily";
            case DatasetFamily.TrademarkAssignment:
                return "trademark-assignment";
            case DatasetFamily.Ttab:
                return "trademark-ttab";
            case DatasetFamily.PatentsViewGrant:
                return "patentsview-grant-tsv";
            case DatasetFamily.PatentsViewPreGrant:
                return "patentsview-pregrant-tsv";
            case DatasetFamily.PatentsViewAnnualized:
                return "patentsview-annualized-delimited";
            case DatasetFamily.PatentsViewSorted:
                return "patentsview-sorted-delimited";
            default:
                return "generic-binary";
        }
    }

    internal static string ResolveOfficeActionHint(BulkParserContext context)
    {
        if (!global::System.String.IsNullOrWhiteSpace(context.SchemaMarker))
        {
            return context.SchemaMarker;
        }

        if (!global::System.String.IsNullOrWhiteSpace(context.FileName))
        {
            return context.FileName;
        }

        return context.ProductIdentifier ?? string.Empty;
    }

    internal static string ResolvePatentsViewGrantHint(BulkParserContext context)
    {
        if (!global::System.String.IsNullOrWhiteSpace(context.SchemaMarker))
        {
            return context.SchemaMarker;
        }

        if (!global::System.String.IsNullOrWhiteSpace(context.FileName))
        {
            return context.FileName;
        }

        return context.ProductIdentifier ?? string.Empty;
    }

    internal static string ResolvePatentsViewPreGrantHint(BulkParserContext context)
    {
        if (!global::System.String.IsNullOrWhiteSpace(context.SchemaMarker))
        {
            return context.SchemaMarker;
        }

        if (!global::System.String.IsNullOrWhiteSpace(context.FileName))
        {
            return context.FileName;
        }

        return context.ProductIdentifier ?? string.Empty;
    }

    internal static string ResolvePatentsViewDerivedHint(BulkParserContext context)
    {
        if (!global::System.String.IsNullOrWhiteSpace(context.FileName))
        {
            return context.FileName;
        }

        if (!global::System.String.IsNullOrWhiteSpace(context.SchemaMarker))
        {
            return context.SchemaMarker;
        }

        return context.ProductIdentifier ?? string.Empty;
    }

    internal static string DecodeRouterPrefix(byte[] prefix)
    {
        if (prefix.Length == 0 || ArchiveFormatDetector.HasZipSignature(prefix) || ArchiveFormatDetector.HasGzipSignature(prefix) || ArchiveFormatDetector.HasTarSignature(prefix))
        {
            return string.Empty;
        }

        int length = prefix.Length;
        if (length > 8192)
        {
            length = 8192;
        }

        return global::System.Text.Encoding.UTF8.GetString(prefix, 0, length);
    }

    internal static string NormalizeRouterEvidence(string value)
    {
        global::System.Text.StringBuilder result = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (global::System.Char.IsLetterOrDigit(character))
            {
                result.Append(global::System.Char.ToLowerInvariant(character));
            }
        }

        return result.ToString();
    }

    internal static void ValidateRouterContext(BulkParserContext context)
    {
        if (context == null)
        {
            throw new ValidationException("Bulk parser routing context is required.", "context", "REQUIRED", null);
        }

        if (context.Prefix == null)
        {
            throw new ValidationException("The bulk parser prefix cannot be null.", "Prefix", "REQUIRED", null);
        }

        if (context.Limits == null)
        {
            throw new ValidationException("Bulk parser limits are required.", "Limits", "REQUIRED", null);
        }
    }

    internal static void ValidateRouterInputs(BulkParserContext context, PatentDataProvenance provenance)
    {
        ValidateRouterContext(context);
        if (provenance == null)
        {
            throw new ValidationException("Bulk parser provenance is required.", "provenance", "REQUIRED", null);
        }
    }
}
