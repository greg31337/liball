namespace uspto;

internal static class BulkDatasetPresetResolver
{
    internal static BulkDatasetPreset[] ResolvePresets(DatasetFamily[] families, BulkProduct[] products, PatentDataProvenance catalogProvenance)
    {
        ValidatePresetFamilies(families);
        if (products == null)
        {
            throw new ValidationException("Bulk catalog products are required.", "products", "REQUIRED", null);
        }

        if (catalogProvenance == null)
        {
            throw new ValidationException("Bulk catalog provenance is required.", "catalogProvenance", "REQUIRED", null);
        }

        BulkDatasetPreset[] results = new BulkDatasetPreset[families.Length];
        int familyIndex;
        for (familyIndex = 0; familyIndex < families.Length; familyIndex += 1)
        {
            DatasetFamily family = families[familyIndex];
            global::System.Collections.Generic.List<BulkProduct> matches = new global::System.Collections.Generic.List<BulkProduct>();
            global::System.Collections.Generic.List<string> identifiers = new global::System.Collections.Generic.List<string>();
            int productIndex;
            for (productIndex = 0; productIndex < products.Length; productIndex += 1)
            {
                BulkProduct product = products[productIndex];
                if (product == null)
                {
                    throw new ValidationException("Bulk catalog products cannot contain null values.", "products", "REQUIRED", null);
                }

                if (MatchesPresetFamily(family, product))
                {
                    matches.Add(product);
                    if (!global::System.String.IsNullOrWhiteSpace(product.ProductIdentifier))
                    {
                        identifiers.Add(product.ProductIdentifier);
                    }
                }
            }

            BulkDatasetPreset preset = new BulkDatasetPreset();
            preset.Family = family;
            preset.ProductIdentifiers = identifiers.ToArray();
            preset.Products = matches.ToArray();
            preset.IsAvailable = matches.Count != 0;
            preset.Provenance = CreatePresetProvenance(family, catalogProvenance);
            results[familyIndex] = preset;
        }

        return results;
    }

    internal static void ValidatePresetFamilies(DatasetFamily[] families)
    {
        if (families == null)
        {
            throw new ValidationException("Dataset families are required.", "families", "REQUIRED", null);
        }

        global::System.Collections.Generic.HashSet<DatasetFamily> seen = new global::System.Collections.Generic.HashSet<DatasetFamily>();
        int index;
        for (index = 0; index < families.Length; index += 1)
        {
            DatasetFamily family = families[index];
            if (!IsSupportedPresetFamily(family))
            {
                throw new ValidationException("Every dataset preset must select a supported family.", "families", "UNSUPPORTED_DATASET_FAMILY", null);
            }

            if (!seen.Add(family))
            {
                throw new ValidationException("Dataset preset families cannot be repeated.", "families", "DUPLICATE_DATASET_FAMILY", null);
            }
        }
    }

    internal static bool IsSupportedPresetFamily(DatasetFamily family)
    {
        switch (family)
        {
            case DatasetFamily.PatentFileWrapperComplete:
            case DatasetFamily.PatentFileWrapperDelta:
            case DatasetFamily.PatentGrantFullText:
            case DatasetFamily.PatentApplicationFullText:
            case DatasetFamily.PatentAssignment:
            case DatasetFamily.OfficeAction:
            case DatasetFamily.Ptab:
            case DatasetFamily.TrademarkApplicationAndStatus:
            case DatasetFamily.TrademarkAssignment:
            case DatasetFamily.Ttab:
            case DatasetFamily.PatentsViewGrant:
            case DatasetFamily.PatentsViewPreGrant:
            case DatasetFamily.PatentsViewAnnualized:
            case DatasetFamily.PatentsViewSorted:
                return true;
            default:
                return false;
        }
    }

    internal static bool MatchesPresetFamily(DatasetFamily family, BulkProduct product)
    {
        string text = BuildPresetSearchText(product);
        bool patentFileWrapper = HasPresetText(text, new string[] { "patent file wrapper", "patent-file-wrapper", "file wrapper" });
        bool patentsView = IsPatentsViewProduct(product, text);
        switch (family)
        {
            case DatasetFamily.PatentFileWrapperComplete:
                return HasPresetIdentifier(product, new string[] { "PTFWPRE" }) || patentFileWrapper && HasPresetText(text, new string[] { " complete ", "complete-", " full ", "full-", "historical", "backfile" });
            case DatasetFamily.PatentFileWrapperDelta:
                return HasPresetIdentifier(product, new string[] { "PTFWPRE" }) || patentFileWrapper && HasPresetText(text, new string[] { " delta ", "delta-", "incremental", " weekly ", "weekly-", " daily ", "daily-", " update " });
            case DatasetFamily.PatentGrantFullText:
                return HasPresetIdentifier(product, new string[] { UsptoDatasetIdentifiers.PatentGrantFullTextXml, "PTBLXML", "PTGRAPS" }) || HasPresetText(text, new string[] { "patent grant full text", "patent grant data/xml", "patent grant xml" });
            case DatasetFamily.PatentApplicationFullText:
                return HasPresetIdentifier(product, new string[] { UsptoDatasetIdentifiers.PatentApplicationFullTextXml, "APPDT" }) || HasPresetText(text, new string[] { "patent application full text", "published patent application", "patent application data/xml", "patent application xml" });
            case DatasetFamily.PatentAssignment:
                return HasPresetIdentifier(product, new string[] { "PASDL" }) || HasPresetText(text, new string[] { "patent assignment" });
            case DatasetFamily.OfficeAction:
                return HasPresetIdentifier(product, new string[] { UsptoDatasetIdentifiers.OfficeActionWeekly }) || HasPresetText(text, new string[] { "office action weekly archive", "office actions weekly archive", "office action archive" });
            case DatasetFamily.Ptab:
                return HasPresetIdentifierPrefix(product, "PTAB") || HasPresetText(text, new string[] { "patent trial and appeal board", " ptab ", "ptab-", "ptab_" });
            case DatasetFamily.TrademarkApplicationAndStatus:
                return HasPresetIdentifier(product, new string[] { "TRTDXFAP" }) || HasPresetText(text, new string[] { "trademark application and status", "trademark applications and status", "trademark status data" }) || HasPresetText(text, new string[] { "trademark" }) && HasPresetText(text, new string[] { "application", "status" }) && !HasPresetText(text, new string[] { "assignment", "trial and appeal board", " ttab " });
            case DatasetFamily.TrademarkAssignment:
                return HasPresetIdentifier(product, new string[] { "TRTDXFAG" }) || HasPresetText(text, new string[] { "trademark assignment" });
            case DatasetFamily.Ttab:
                return HasPresetIdentifier(product, new string[] { UsptoDatasetIdentifiers.TrademarkTrialAndAppealBoard, "TTABTDXF" }) || HasPresetText(text, new string[] { "trademark trial and appeal board", " ttab ", "ttab-", "ttab_" });
            case DatasetFamily.PatentsViewGrant:
                return patentsView && !HasPresetText(text, new string[] { "pregrant", "pre-grant", "pre grant" }) && HasPresetText(text, new string[] { " grant ", "grant-", "grant_", "patent data", "patent tables", "g_patent" });
            case DatasetFamily.PatentsViewPreGrant:
                return patentsView && HasPresetText(text, new string[] { "pregrant", "pre-grant", "pre grant", "publication data", "publication tables", "pgpub" });
            case DatasetFamily.PatentsViewAnnualized:
                return patentsView && HasPresetText(text, new string[] { "annualized", "annualised", " annual " });
            case DatasetFamily.PatentsViewSorted:
                return patentsView && HasPresetText(text, new string[] { " sorted ", "sorted-", "sorted_" });
            default:
                return false;
        }
    }

    internal static string BuildPresetSearchText(BulkProduct product)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        AppendPresetText(builder, product.ProductIdentifier);
        AppendPresetText(builder, product.Title);
        AppendPresetText(builder, product.Description);
        AppendPresetText(builder, product.Frequency);
        int index;
        for (index = 0; index < product.Labels.Length; index += 1)
        {
            AppendPresetText(builder, product.Labels[index]);
        }

        for (index = 0; index < product.DatasetTypes.Length; index += 1)
        {
            AppendPresetText(builder, product.DatasetTypes[index]);
        }

        for (index = 0; index < product.Categories.Length; index += 1)
        {
            AppendPresetText(builder, product.Categories[index]);
        }

        for (index = 0; index < product.Files.Length; index += 1)
        {
            BulkProductFile file = product.Files[index];
            if (file != null)
            {
                AppendPresetText(builder, file.FileName);
                AppendPresetText(builder, file.FileType);
            }
        }

        return builder.ToString().ToLowerInvariant();
    }

    internal static void AppendPresetText(global::System.Text.StringBuilder builder, string? value)
    {
        if (!global::System.String.IsNullOrWhiteSpace(value))
        {
            builder.Append(' ');
            builder.Append(value.Trim());
            builder.Append(' ');
        }
    }

    internal static bool HasPresetText(string text, string[] candidates)
    {
        int index;
        for (index = 0; index < candidates.Length; index += 1)
        {
            if (text.IndexOf(candidates[index], global::System.StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasPresetIdentifier(BulkProduct product, string[] identifiers)
    {
        int index;
        for (index = 0; index < identifiers.Length; index += 1)
        {
            if (global::System.String.Equals(product.ProductIdentifier, identifiers[index], global::System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasPresetIdentifierPrefix(BulkProduct product, string prefix)
    {
        return product.ProductIdentifier != null && product.ProductIdentifier.StartsWith(prefix, global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsPatentsViewProduct(BulkProduct product, string text)
    {
        return HasPresetIdentifierPrefix(product, "PV") || HasPresetIdentifierPrefix(product, "PATENTSVIEW") || HasPresetText(text, new string[] { "patentsview", "patents view" });
    }

    internal static PatentDataProvenance CreatePresetProvenance(DatasetFamily family, PatentDataProvenance catalogProvenance)
    {
        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = DataProvenance.ClientDerived;
        result.SourceUri = catalogProvenance.SourceUri;
        result.DatasetIdentifier = "bulk-dataset-preset/" + family.ToString();
        result.RequestIdentifier = catalogProvenance.RequestIdentifier;
        result.RetrievedAt = catalogProvenance.RetrievedAt;
        result.HasRetrievedAt = catalogProvenance.HasRetrievedAt;
        if (!result.HasRetrievedAt)
        {
            result.RetrievedAt = global::System.DateTimeOffset.UtcNow;
            result.HasRetrievedAt = true;
        }

        result.FromCache = catalogProvenance.FromCache;
        result.IsAuthoritative = false;
        return result;
    }
}
