namespace uspto;

internal static class ProvenanceLabeler
{
    internal static PatentDataProvenance CopyPatent(PatentDataProvenance source)
    {
        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = source.Source;
        result.SourceUri = source.SourceUri;
        result.DatasetIdentifier = source.DatasetIdentifier;
        result.DatasetVersion = source.DatasetVersion;
        result.RequestIdentifier = source.RequestIdentifier;
        result.RetrievedAt = source.RetrievedAt;
        result.HasRetrievedAt = source.HasRetrievedAt;
        result.LastUpdatedAt = source.LastUpdatedAt;
        result.HasLastUpdatedAt = source.HasLastUpdatedAt;
        result.FromCache = source.FromCache;
        result.IsAuthoritative = source.IsAuthoritative;
        result.FieldSources = CopyFieldSources(source.FieldSources);
        return result;
    }

    internal static PtabDataProvenance CopyPtab(PtabDataProvenance source)
    {
        PtabDataProvenance result = new PtabDataProvenance();
        result.Source = source.Source;
        result.Collection = source.Collection;
        result.HasCollection = source.HasCollection;
        result.SourceUri = source.SourceUri;
        result.DatasetIdentifier = source.DatasetIdentifier;
        result.DatasetVersion = source.DatasetVersion;
        result.RequestIdentifier = source.RequestIdentifier;
        result.RetrievedAt = source.RetrievedAt;
        result.HasRetrievedAt = source.HasRetrievedAt;
        result.LastModifiedAt = source.LastModifiedAt;
        result.HasLastModifiedAt = source.HasLastModifiedAt;
        result.FromCache = source.FromCache;
        result.IsAuthoritative = source.IsAuthoritative;
        result.FieldSources = CopyFieldSources(source.FieldSources);
        return result;
    }

    internal static FieldDataProvenance[] CopyFieldSources(FieldDataProvenance[] source)
    {
        FieldDataProvenance[] result = new FieldDataProvenance[source.Length];
        int index;
        for (index = 0; index < source.Length; index += 1)
        {
            result[index] = CopyFieldSource(source[index]);
        }

        return result;
    }

    internal static FieldDataProvenance CopyFieldSource(FieldDataProvenance source)
    {
        FieldDataProvenance result = new FieldDataProvenance();
        result.FieldPath = source.FieldPath;
        result.Source = source.Source;
        result.Delivery = source.Delivery;
        result.DatasetIdentifier = source.DatasetIdentifier;
        result.DatasetVersion = source.DatasetVersion;
        result.IsAuthoritative = source.IsAuthoritative;
        return result;
    }

    internal static void AddPatentField(PatentDataProvenance target, string fieldPath, PatentDataProvenance source)
    {
        FieldDataProvenance label = ResolvePatentField(fieldPath, source);
        AppendPatentField(target, label);
    }

    internal static void AddLocalPatentField(PatentDataProvenance target, string fieldPath, PatentDataProvenance source)
    {
        FieldDataProvenance label = new FieldDataProvenance();
        label.FieldPath = fieldPath;
        label.Source = DataProvenance.PatentsView;
        label.Delivery = DataProvenance.LocalMirror;
        label.DatasetIdentifier = target.DatasetIdentifier ?? source.DatasetIdentifier;
        label.DatasetVersion = target.DatasetVersion ?? source.DatasetVersion;
        label.IsAuthoritative = false;
        AppendPatentField(target, label);
    }

    internal static void AddPtabField(PtabDataProvenance target, string fieldPath, PtabDataProvenance source)
    {
        FieldDataProvenance label = ResolvePtabField(fieldPath, source);
        FieldDataProvenance[] fields = new FieldDataProvenance[target.FieldSources.Length + 1];
        global::System.Array.Copy(target.FieldSources, fields, target.FieldSources.Length);
        fields[fields.Length - 1] = label;
        target.FieldSources = fields;
    }

    internal static FieldDataProvenance ResolvePatentField(string fieldPath, PatentDataProvenance source)
    {
        FieldDataProvenance? selected = FindSourceField(source.FieldSources, fieldPath);
        FieldDataProvenance result;
        if (selected == null)
        {
            result = new FieldDataProvenance();
            result.Source = source.Source;
            result.DatasetIdentifier = source.DatasetIdentifier;
            result.DatasetVersion = source.DatasetVersion;
            result.IsAuthoritative = source.IsAuthoritative;
        }
        else
        {
            result = CopyFieldSource(selected);
            if (result.DatasetIdentifier == null)
            {
                result.DatasetIdentifier = source.DatasetIdentifier;
            }

            if (result.DatasetVersion == null)
            {
                result.DatasetVersion = source.DatasetVersion;
            }
        }

        result.FieldPath = fieldPath;
        result.Delivery = ResolveDelivery(source.Source, source.FromCache);
        return result;
    }

    internal static FieldDataProvenance ResolvePtabField(string fieldPath, PtabDataProvenance source)
    {
        FieldDataProvenance? selected = FindSourceField(source.FieldSources, fieldPath);
        FieldDataProvenance result;
        if (selected == null)
        {
            result = new FieldDataProvenance();
            result.Source = source.Source;
            result.DatasetIdentifier = source.DatasetIdentifier;
            result.DatasetVersion = source.DatasetVersion;
            result.IsAuthoritative = source.IsAuthoritative;
        }
        else
        {
            result = CopyFieldSource(selected);
            if (result.DatasetIdentifier == null)
            {
                result.DatasetIdentifier = source.DatasetIdentifier;
            }

            if (result.DatasetVersion == null)
            {
                result.DatasetVersion = source.DatasetVersion;
            }
        }

        result.FieldPath = fieldPath;
        result.Delivery = ResolveDelivery(source.Source, source.FromCache);
        return result;
    }

    internal static FieldDataProvenance? FindSourceField(FieldDataProvenance[] fields, string fieldPath)
    {
        FieldDataProvenance? wildcard = null;
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            if (global::System.String.Equals(fields[index].FieldPath, fieldPath, global::System.StringComparison.Ordinal))
            {
                return fields[index];
            }

            if (global::System.String.Equals(fields[index].FieldPath, "*", global::System.StringComparison.Ordinal))
            {
                wildcard = fields[index];
            }
        }

        return wildcard;
    }

    internal static DataProvenance ResolveDelivery(DataProvenance source, bool fromCache)
    {
        if (fromCache)
        {
            return DataProvenance.Cache;
        }

        return source;
    }

    internal static void AppendPatentField(PatentDataProvenance target, FieldDataProvenance label)
    {
        FieldDataProvenance[] fields = new FieldDataProvenance[target.FieldSources.Length + 1];
        global::System.Array.Copy(target.FieldSources, fields, target.FieldSources.Length);
        fields[fields.Length - 1] = label;
        target.FieldSources = fields;
    }
}
