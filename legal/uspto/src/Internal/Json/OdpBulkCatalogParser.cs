namespace uspto;

internal class BulkCatalogDateValue
{
    internal string? Text = null;
    internal global::System.DateTimeOffset Value = default(global::System.DateTimeOffset);
    internal bool HasValue = false;
}

internal class BulkCatalogElementValue
{
    internal global::System.Text.Json.JsonElement Element = default(global::System.Text.Json.JsonElement);
    internal string Path = "$";
}

internal static class OdpBulkCatalogParser
{
    internal static BulkProductResponse ParseSearch(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP bulk catalog response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("An ODP bulk catalog search request is required.", "request", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("ODP response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue count = root.ReadInt64("count");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int64Value < 0L)
            {
                throw ParseFailure("The ODP bulk catalog response has an invalid count.", count.Path);
            }

            JsonFieldValue products = ReadFirstArray(root, new string[] { "bulkDataProductBag", "productBag", "products" });
            if (!products.IsValid || products.IsNull || count.Int64Value != 0L && !products.IsPresent)
            {
                throw ParseFailure("The ODP bulk catalog response has an invalid product collection.", products.Path);
            }

            if (count.Int64Value != products.ArrayItems.LongLength)
            {
                throw ParseFailure("The ODP bulk catalog count does not match its products.", products.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, responseMetadata);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            BulkProduct[] parsedProducts = new BulkProduct[products.ArrayItems.Length];
            int index;
            for (index = 0; index < products.ArrayItems.Length; index += 1)
            {
                parsedProducts[index] = ParseProduct(products.ArrayItems[index], products.ArrayItemPaths[index], provenance);
            }

            BulkProductResponse result = new BulkProductResponse();
            result.Request = request;
            result.Products = parsedProducts;
            result.Facets = ParseFacets(root);
            result.Count = count.Int64Value;
            result.HasCount = true;
            result.ReturnedCount = parsedProducts.Length;
            result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
            result.RequestIdentifier = requestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = true;
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "count", "bulkDataProductBag", "productBag", "products", "facets", "requestIdentifier" });
            OdpPetitionDecisionParser.CopyUnknownFields(unknown, result.UnknownFields);
            return result;
        }
    }

    internal static BulkProduct ParseExactProduct(string json, string expectedProductIdentifier, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP bulk product response is required.", "json", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(expectedProductIdentifier))
        {
            throw new ValidationException("An expected bulk product identifier is required.", "expectedProductIdentifier", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("ODP response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            global::System.Text.Json.JsonElement rootElement = document.RootElement;
            ValidateExactCount(rootElement);
            BulkCatalogElementValue selected = SelectExactElement(rootElement, new string[] { "productIdentifier", "productId", "shortName" }, new string[] { "bulkDataProductBag", "productBag", "products", "bulkDataProduct", "product" }, "bulkDataProductBag");
            string? requestIdentifier = responseMetadata.RequestIdentifier;
            if (rootElement.ValueKind == global::System.Text.Json.JsonValueKind.Object)
            {
                requestIdentifier = ResolveRequestIdentifier(new JsonFieldReader(rootElement, "$"), responseMetadata);
            }

            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            BulkProduct result = ParseProduct(selected.Element, selected.Path, provenance);
            if (!global::System.String.Equals(result.ProductIdentifier, expectedProductIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
            {
                throw ParseFailure("The ODP bulk product identifier does not match the requested identifier.", selected.Path + ".productIdentifier");
            }

            result.RawResponse = rawResponse;
            return result;
        }
    }

    internal static BulkProductFile ParseExactFile(string json, string productIdentifier, string expectedFileName, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP bulk file response is required.", "json", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(productIdentifier))
        {
            throw new ValidationException("A bulk product identifier is required.", "productIdentifier", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(expectedFileName))
        {
            throw new ValidationException("An expected bulk filename is required.", "expectedFileName", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("ODP response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            global::System.Text.Json.JsonElement rootElement = document.RootElement;
            ValidateExactCount(rootElement);
            BulkCatalogElementValue selected = SelectExactElement(rootElement, new string[] { "fileName", "filename" }, new string[] { "fileDataBag", "files", "bulkDataFile", "file" }, "fileDataBag");
            string? requestIdentifier = responseMetadata.RequestIdentifier;
            if (rootElement.ValueKind == global::System.Text.Json.JsonValueKind.Object)
            {
                requestIdentifier = ResolveRequestIdentifier(new JsonFieldReader(rootElement, "$"), responseMetadata);
            }

            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            BulkProductFile result = ParseFile(selected.Element, selected.Path, productIdentifier, provenance);
            if (!global::System.String.Equals(result.FileName, expectedFileName, global::System.StringComparison.Ordinal))
            {
                throw ParseFailure("The ODP bulk filename does not match the requested filename.", selected.Path + ".fileName");
            }

            result.RawResponse = rawResponse;
            return result;
        }
    }

    internal static void ValidateExactCount(global::System.Text.Json.JsonElement rootElement)
    {
        if (rootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            return;
        }

        JsonFieldReader root = new JsonFieldReader(rootElement, "$");
        JsonFieldValue count = root.ReadInt64("count");
        if (count.IsPresent && (!count.IsValid || count.IsNull || count.Int64Value != 1L))
        {
            throw ParseFailure("The exact ODP bulk catalog response must contain one record.", count.Path);
        }
    }

    internal static BulkCatalogElementValue SelectExactElement(global::System.Text.Json.JsonElement rootElement, string[] identityNames, string[] wrapperNames, string missingField)
    {
        BulkCatalogElementValue result = new BulkCatalogElementValue();
        if (rootElement.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            if (rootElement.GetArrayLength() != 1)
            {
                throw ParseFailure("The exact ODP bulk catalog response must contain one record.", "$");
            }

            int index = 0;
            foreach (global::System.Text.Json.JsonElement element in rootElement.EnumerateArray())
            {
                result.Element = element;
                result.Path = "$[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
                index += 1;
            }

            return result;
        }

        if (rootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The exact ODP bulk catalog response is invalid.", "$");
        }

        JsonFieldReader root = new JsonFieldReader(rootElement, "$");
        JsonFieldValue identity = ReadFirstString(root, identityNames);
        if (identity.IsPresent)
        {
            result.Element = rootElement;
            result.Path = "$";
            return result;
        }

        JsonFieldValue wrapped = ReadFirstRaw(root, wrapperNames);
        if (!wrapped.IsPresent || wrapped.IsNull)
        {
            throw ParseFailure("The exact ODP bulk catalog record is missing.", "$" + "." + missingField);
        }

        if (wrapped.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            result.Element = wrapped.ElementValue;
            result.Path = wrapped.Path;
            return result;
        }

        if (wrapped.ElementValue.ValueKind != global::System.Text.Json.JsonValueKind.Array || wrapped.ElementValue.GetArrayLength() != 1)
        {
            throw ParseFailure("The exact ODP bulk catalog response must contain one record.", wrapped.Path);
        }

        foreach (global::System.Text.Json.JsonElement element in wrapped.ElementValue.EnumerateArray())
        {
            result.Element = element;
            result.Path = wrapped.Path + "[0]";
        }

        return result;
    }

    internal static BulkProduct ParseProduct(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP bulk catalog product is invalid.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        BulkProduct result = new BulkProduct();
        result.ProductIdentifier = ReadRequiredString(reader, new string[] { "productIdentifier", "productId", "shortName" }, "The ODP bulk catalog product identifier is required.");
        result.Title = ReadOptionalString(reader, new string[] { "productTitleText", "productTitle", "title" });
        result.Description = ReadOptionalString(reader, new string[] { "productDescriptionText", "productDescription", "description" });
        result.Frequency = ReadOptionalString(reader, new string[] { "productFrequencyText", "productFrequency", "frequency" });
        result.DaysOfWeekText = ReadOptionalString(reader, new string[] { "daysOfWeekText", "productDaysOfWeekText" });
        result.DaysOfWeek = SplitText(result.DaysOfWeekText);
        result.Labels = ReadStringArray(reader, new string[] { "productLabelArrayText", "productLabels", "labels" }, "The ODP bulk catalog product labels are invalid.");
        result.DatasetTypes = ReadStringArray(reader, new string[] { "productDataSetArrayText", "productDatasetArrayText", "datasetTypes" }, "The ODP bulk catalog dataset types are invalid.");
        result.Categories = ReadStringArray(reader, new string[] { "productDataSetCategoryArrayText", "productDatasetCategoryArrayText", "productCategoryArrayText", "categories" }, "The ODP bulk catalog product categories are invalid.");
        ApplyDateRange(reader, new string[] { "productFromDate", "fromDate" }, new string[] { "productToDate", "toDate" }, result.DataDateRange);
        ApplyOptionalProductSize(reader, result);
        ApplyOptionalProductCount(reader, result);
        ApplyProductLastModified(reader, result);
        result.MimeTypeIdentifiers = ReadStringArray(reader, new string[] { "mimeTypeIdentifierArrayText", "mimeTypeIdentifiers", "mimeTypes" }, "The ODP bulk catalog MIME identifiers are invalid.");
        ParseProductFiles(reader, result, provenance);
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        result.Provenance.DatasetIdentifier = result.ProductIdentifier;
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[]
        {
            "productIdentifier", "productId", "shortName", "productTitleText", "productTitle", "title",
            "productDescriptionText", "productDescription", "description", "productFrequencyText", "productFrequency", "frequency",
            "daysOfWeekText", "productDaysOfWeekText", "productLabelArrayText", "productLabels", "labels",
            "productDataSetArrayText", "productDatasetArrayText", "datasetTypes", "productDataSetCategoryArrayText",
            "productDatasetCategoryArrayText", "productCategoryArrayText", "categories", "productFromDate", "fromDate",
            "productToDate", "toDate", "productTotalFileSize", "totalFileSize", "productFileTotalQuantity", "fileCount",
            "lastModifiedDateTime", "productLastModifiedDateTime", "mimeTypeIdentifierArrayText", "mimeTypeIdentifiers", "mimeTypes",
            "productFileBag", "files"
        });
        OdpPetitionDecisionParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static void ParseProductFiles(JsonFieldReader reader, BulkProduct result, PatentDataProvenance provenance)
    {
        JsonFieldValue bag = ReadFirstRaw(reader, new string[] { "productFileBag", "files" });
        if (!bag.IsPresent || bag.IsNull)
        {
            return;
        }

        global::System.Text.Json.JsonElement[] elements;
        string[] paths;
        if (bag.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            global::System.Collections.Generic.List<global::System.Text.Json.JsonElement> directElements = new global::System.Collections.Generic.List<global::System.Text.Json.JsonElement>();
            global::System.Collections.Generic.List<string> directPaths = new global::System.Collections.Generic.List<string>();
            int directIndex = 0;
            foreach (global::System.Text.Json.JsonElement directFile in bag.ElementValue.EnumerateArray())
            {
                directElements.Add(directFile.Clone());
                directPaths.Add(bag.Path + "[" + directIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]");
                directIndex += 1;
            }

            elements = directElements.ToArray();
            paths = directPaths.ToArray();
            result.ReturnedFileCount = elements.LongLength;
            result.HasReturnedFileCount = true;
        }
        else if (bag.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            JsonFieldReader bagReader = new JsonFieldReader(bag.ElementValue, bag.Path);
            JsonFieldValue count = ReadFirstInt64(bagReader, new string[] { "count", "fileCount" });
            if (count.IsPresent && (!count.IsValid || count.IsNull || count.Int64Value < 0L))
            {
                throw ParseFailure("The ODP bulk catalog returned file count is invalid.", count.Path);
            }

            JsonFieldValue files = ReadFirstArray(bagReader, new string[] { "fileDataBag", "files" });
            if (!files.IsValid || files.IsNull || count.IsPresent && count.Int64Value != files.ArrayItems.LongLength)
            {
                throw ParseFailure("The ODP bulk catalog product files are invalid.", files.Path);
            }

            elements = files.ArrayItems;
            paths = files.ArrayItemPaths;
            if (count.IsPresent)
            {
                result.ReturnedFileCount = count.Int64Value;
            }
            else
            {
                result.ReturnedFileCount = elements.LongLength;
            }

            result.HasReturnedFileCount = true;
            JsonUnknownField[] unknown = bagReader.CaptureUnknownFields(new string[] { "count", "fileCount", "fileDataBag", "files" });
            int unknownIndex;
            for (unknownIndex = 0; unknownIndex < unknown.Length; unknownIndex += 1)
            {
                result.UnknownFields["productFileBag." + unknown[unknownIndex].Name] = unknown[unknownIndex].RawJson;
            }
        }
        else
        {
            throw ParseFailure("The ODP bulk catalog product file bag is invalid.", bag.Path);
        }

        result.Files = new BulkProductFile[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            result.Files[index] = ParseFile(elements[index], paths[index], result.ProductIdentifier ?? string.Empty, provenance);
        }
    }

    internal static BulkProductFile ParseFile(global::System.Text.Json.JsonElement element, string path, string productIdentifier, PatentDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP bulk catalog file is invalid.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        BulkProductFile result = new BulkProductFile();
        result.ProductIdentifier = productIdentifier;
        result.FileName = ReadRequiredString(reader, new string[] { "fileName", "filename" }, "The ODP bulk catalog filename is required.");
        JsonFieldValue size = ReadFirstInt64(reader, new string[] { "fileSize", "size" });
        if (size.IsPresent)
        {
            if (!size.IsValid || size.IsNull || size.Int64Value < 0L)
            {
                throw ParseFailure("The ODP bulk catalog file size is invalid.", size.Path);
            }

            result.Size = size.Int64Value;
            result.HasSize = true;
        }

        ApplyDateRange(reader, new string[] { "fileDataFromDate", "dataFromDate" }, new string[] { "fileDataToDate", "dataToDate" }, result.DataDateRange);
        result.FileType = ReadOptionalString(reader, new string[] { "fileTypeText", "fileType", "type" });
        result.MimeTypeIdentifier = ReadOptionalString(reader, new string[] { "mimeTypeIdentifier", "mimeType", "format" });
        string? uriText = ReadOptionalString(reader, new string[] { "fileDownloadURI", "fileDownloadUri", "downloadURI", "downloadUri" });
        if (uriText != null)
        {
            global::System.Uri? parsedUri = TsdrJsonParser.ParseUri(uriText);
            if (parsedUri == null)
            {
                throw ParseFailure("The ODP bulk catalog file download URI is invalid.", path + ".fileDownloadURI");
            }

            result.CatalogUri = SecretRedactor.RedactUri(parsedUri, new string[0]);
            result.DownloadUri = result.CatalogUri;
        }

        ApplyFileReleaseDate(reader, result);
        ApplyFileDate(reader, result);
        ApplyFileLastModified(reader, result);
        ApplyFileFormat(result);
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        result.Provenance.DatasetIdentifier = productIdentifier;
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[]
        {
            "fileName", "filename", "fileSize", "size", "fileDataFromDate", "dataFromDate", "fileDataToDate", "dataToDate",
            "fileTypeText", "fileType", "type", "mimeTypeIdentifier", "mimeType", "format", "fileDownloadURI", "fileDownloadUri",
            "downloadURI", "downloadUri", "fileReleaseDate", "releaseDate", "fileDate", "fileLastModifiedDateTime", "lastModifiedDateTime"
        });
        OdpPetitionDecisionParser.CopyUnknownFields(unknown, result.UnknownFields);
        return result;
    }

    internal static BulkCatalogFacet[] ParseFacets(JsonFieldReader root)
    {
        JsonFieldValue facets = root.ReadRaw("facets");
        if (!facets.IsPresent || facets.IsNull)
        {
            return new BulkCatalogFacet[0];
        }

        global::System.Collections.Generic.List<BulkCatalogFacet> results = new global::System.Collections.Generic.List<BulkCatalogFacet>();
        if (facets.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            AddFacetObject(facets.ElementValue, facets.Path, results);
        }
        else if (facets.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            int index = 0;
            foreach (global::System.Text.Json.JsonElement item in facets.ElementValue.EnumerateArray())
            {
                string itemPath = facets.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
                AddFacetObject(item, itemPath, results);
                index += 1;
            }
        }
        else
        {
            throw ParseFailure("The ODP bulk catalog facets are invalid.", facets.Path);
        }

        return results.ToArray();
    }

    internal static void AddFacetObject(global::System.Text.Json.JsonElement element, string path, global::System.Collections.Generic.List<BulkCatalogFacet> results)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("The ODP bulk catalog facet entry is invalid.", path);
        }

        foreach (global::System.Text.Json.JsonProperty property in element.EnumerateObject())
        {
            results.Add(ParseFacet(property.Name, property.Value, JsonFieldReader.AppendPropertyPath(path, property.Name)));
        }
    }

    internal static BulkCatalogFacet ParseFacet(string field, global::System.Text.Json.JsonElement element, string path)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw ParseFailure("The ODP bulk catalog facet values are invalid.", path);
        }

        global::System.Collections.Generic.List<BulkCatalogFacetValue> values = new global::System.Collections.Generic.List<BulkCatalogFacetValue>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in element.EnumerateArray())
        {
            string itemPath = path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            JsonFieldReader reader = new JsonFieldReader(item, itemPath);
            JsonFieldValue value = reader.ReadString("value");
            JsonFieldValue count = reader.ReadInt64("count");
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object || !value.IsPresent || !value.IsValid || value.IsNull)
            {
                throw ParseFailure("The ODP bulk catalog facet value is invalid.", value.Path);
            }

            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int64Value < 0L)
            {
                throw ParseFailure("The ODP bulk catalog facet count is invalid.", count.Path);
            }

            BulkCatalogFacetValue parsed = new BulkCatalogFacetValue();
            parsed.Value = value.StringValue;
            parsed.Count = count.Int64Value;
            values.Add(parsed);
            index += 1;
        }

        BulkCatalogFacet result = new BulkCatalogFacet();
        result.Field = field;
        result.Values = values.ToArray();
        return result;
    }

    internal static void ApplyOptionalProductSize(JsonFieldReader reader, BulkProduct result)
    {
        JsonFieldValue value = ReadFirstInt64(reader, new string[] { "productTotalFileSize", "totalFileSize" });
        if (!value.IsPresent)
        {
            return;
        }

        if (!value.IsValid || value.IsNull || value.Int64Value < 0L)
        {
            throw ParseFailure("The ODP bulk catalog product size is invalid.", value.Path);
        }

        result.TotalFileSize = value.Int64Value;
        result.HasTotalFileSize = true;
    }

    internal static void ApplyOptionalProductCount(JsonFieldReader reader, BulkProduct result)
    {
        JsonFieldValue value = ReadFirstInt64(reader, new string[] { "productFileTotalQuantity", "fileCount" });
        if (!value.IsPresent)
        {
            return;
        }

        if (!value.IsValid || value.IsNull || value.Int64Value < 0L)
        {
            throw ParseFailure("The ODP bulk catalog product file count is invalid.", value.Path);
        }

        result.FileCount = value.Int64Value;
        result.HasFileCount = true;
    }

    internal static void ApplyDateRange(JsonFieldReader reader, string[] fromNames, string[] toNames, BulkDateRange result)
    {
        BulkCatalogDateValue from = ReadDateValue(reader, fromNames);
        result.FromDateText = from.Text;
        result.FromDate = from.Value;
        result.HasFromDate = from.HasValue;
        BulkCatalogDateValue to = ReadDateValue(reader, toNames);
        result.ToDateText = to.Text;
        result.ToDate = to.Value;
        result.HasToDate = to.HasValue;
    }

    internal static void ApplyProductLastModified(JsonFieldReader reader, BulkProduct result)
    {
        BulkCatalogDateValue value = ReadDateValue(reader, new string[] { "lastModifiedDateTime", "productLastModifiedDateTime" });
        result.LastModifiedText = value.Text;
        result.LastModified = value.Value;
        result.HasLastModified = value.HasValue;
    }

    internal static void ApplyFileReleaseDate(JsonFieldReader reader, BulkProductFile result)
    {
        BulkCatalogDateValue value = ReadDateValue(reader, new string[] { "fileReleaseDate", "releaseDate" });
        result.ReleaseDateText = value.Text;
        result.ReleaseDate = value.Value;
        result.HasReleaseDate = value.HasValue;
    }

    internal static void ApplyFileDate(JsonFieldReader reader, BulkProductFile result)
    {
        BulkCatalogDateValue value = ReadDateValue(reader, new string[] { "fileDate" });
        result.FileDateText = value.Text;
        result.FileDate = value.Value;
        result.HasFileDate = value.HasValue;
    }

    internal static void ApplyFileLastModified(JsonFieldReader reader, BulkProductFile result)
    {
        BulkCatalogDateValue value = ReadDateValue(reader, new string[] { "fileLastModifiedDateTime", "lastModifiedDateTime" });
        result.LastModifiedText = value.Text;
        result.LastModified = value.Value;
        result.HasLastModified = value.HasValue;
    }

    internal static BulkCatalogDateValue ReadDateValue(JsonFieldReader reader, string[] names)
    {
        BulkCatalogDateValue result = new BulkCatalogDateValue();
        string? text = ReadOptionalString(reader, names);
        result.Text = text;
        if (text == null)
        {
            return result;
        }

        JsonDateParseResult parsed = JsonFieldReader.ParseFlexibleDate(text.Trim());
        if (parsed.IsValid)
        {
            result.Value = parsed.Value;
            result.HasValue = true;
        }

        return result;
    }

    internal static void ApplyFileFormat(BulkProductFile result)
    {
        string name = result.FileName ?? string.Empty;
        string identifier = result.MimeTypeIdentifier ?? string.Empty;
        if (name.EndsWith(".tar.gz", global::System.StringComparison.OrdinalIgnoreCase) || name.EndsWith(".tgz", global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Compression = CompressionFormat.TarGzip;
            result.MediaType = UsptoMediaTypes.Gzip;
        }
        else if (name.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("ZIP", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals(UsptoMediaTypes.Zip, global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Zip;
            result.Compression = CompressionFormat.Zip;
            result.MediaType = UsptoMediaTypes.Zip;
        }
        else if (name.EndsWith(".gz", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("GZIP", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals(UsptoMediaTypes.Gzip, global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Compression = CompressionFormat.Gzip;
            result.MediaType = UsptoMediaTypes.Gzip;
        }
        else if (name.EndsWith(".tar", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("TAR", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals(UsptoMediaTypes.Tar, global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Compression = CompressionFormat.Tar;
            result.MediaType = UsptoMediaTypes.Tar;
        }
        else if (name.EndsWith(".json", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("JSON", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals(UsptoMediaTypes.Json, global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Json;
            result.Compression = CompressionFormat.None;
            result.MediaType = UsptoMediaTypes.Json;
        }
        else if (name.EndsWith(".xml", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("XML", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals(UsptoMediaTypes.Xml, global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Xml;
            result.Compression = CompressionFormat.None;
            result.MediaType = UsptoMediaTypes.Xml;
        }
        else if (name.EndsWith(".csv", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("CSV", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals(UsptoMediaTypes.Csv, global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Csv;
            result.Compression = CompressionFormat.None;
            result.MediaType = UsptoMediaTypes.Csv;
        }
        else if (name.EndsWith(".pdf", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals("PDF", global::System.StringComparison.OrdinalIgnoreCase) || identifier.Equals(UsptoMediaTypes.Pdf, global::System.StringComparison.OrdinalIgnoreCase))
        {
            result.Format = DocumentFormat.Pdf;
            result.Compression = CompressionFormat.None;
            result.MediaType = UsptoMediaTypes.Pdf;
        }
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, HttpResponseMetadata responseMetadata)
    {
        JsonFieldValue value = root.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP bulk catalog request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return responseMetadata.RequestIdentifier;
    }

    internal static string ReadRequiredString(JsonFieldReader reader, string[] names, string message)
    {
        JsonFieldValue value = ReadFirstString(reader, names);
        if (!value.IsPresent || !value.IsValid || value.IsNull || global::System.String.IsNullOrWhiteSpace(value.StringValue))
        {
            throw ParseFailure(message, value.Path);
        }

        return value.StringValue ?? string.Empty;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string[] names)
    {
        JsonFieldValue value = ReadFirstString(reader, names);
        ValidateOptional(value, "The ODP bulk catalog field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string[] ReadStringArray(JsonFieldReader reader, string[] names, string message)
    {
        JsonFieldValue value = ReadFirstArray(reader, names);
        if (!value.IsValid)
        {
            throw ParseFailure(message, value.Path);
        }

        if (!value.IsPresent || value.IsNull)
        {
            return new string[0];
        }

        string[] result = new string[value.ArrayItems.Length];
        int index;
        for (index = 0; index < value.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement item = value.ArrayItems[index];
            if (item.ValueKind == global::System.Text.Json.JsonValueKind.String)
            {
                result[index] = item.GetString() ?? string.Empty;
            }
            else if (item.ValueKind == global::System.Text.Json.JsonValueKind.Number || item.ValueKind == global::System.Text.Json.JsonValueKind.True || item.ValueKind == global::System.Text.Json.JsonValueKind.False)
            {
                result[index] = item.GetRawText();
            }
            else
            {
                throw ParseFailure(message, value.ArrayItemPaths[index]);
            }
        }

        return result;
    }

    internal static string[] SplitText(string? text)
    {
        if (global::System.String.IsNullOrWhiteSpace(text))
        {
            return new string[0];
        }

        string[] parts = text.Split(',');
        global::System.Collections.Generic.List<string> result = new global::System.Collections.Generic.List<string>();
        int index;
        for (index = 0; index < parts.Length; index += 1)
        {
            string value = parts[index].Trim();
            if (value.Length != 0)
            {
                result.Add(value);
            }
        }

        return result.ToArray();
    }

    internal static JsonFieldValue ReadFirstString(JsonFieldReader reader, string[] names)
    {
        JsonFieldValue result = reader.ReadString(names[0]);
        int index = 1;
        while (!result.IsPresent && index < names.Length)
        {
            JsonFieldValue alternative = reader.ReadString(names[index]);
            if (alternative.IsPresent)
            {
                return alternative;
            }

            index += 1;
        }

        return result;
    }

    internal static JsonFieldValue ReadFirstInt64(JsonFieldReader reader, string[] names)
    {
        JsonFieldValue result = reader.ReadInt64(names[0]);
        int index = 1;
        while (!result.IsPresent && index < names.Length)
        {
            JsonFieldValue alternative = reader.ReadInt64(names[index]);
            if (alternative.IsPresent)
            {
                return alternative;
            }

            index += 1;
        }

        return result;
    }

    internal static JsonFieldValue ReadFirstArray(JsonFieldReader reader, string[] names)
    {
        JsonFieldValue result = reader.ReadArray(names[0]);
        int index = 1;
        while (!result.IsPresent && index < names.Length)
        {
            JsonFieldValue alternative = reader.ReadArray(names[index]);
            if (alternative.IsPresent)
            {
                return alternative;
            }

            index += 1;
        }

        return result;
    }

    internal static JsonFieldValue ReadFirstRaw(JsonFieldReader reader, string[] names)
    {
        JsonFieldValue result = reader.ReadRaw(names[0]);
        int index = 1;
        while (!result.IsPresent && index < names.Length)
        {
            JsonFieldValue alternative = reader.ReadRaw(names[index]);
            if (alternative.IsPresent)
            {
                return alternative;
            }

            index += 1;
        }

        return result;
    }

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw ParseFailure(message, value.Path);
        }
    }

    internal static ParseException ParseFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP bulk catalog", path, 0L, false, 0, false, null);
    }
}
