namespace asn1;

internal class OpenTypeDispatchMap
{
    internal AsnConstraintKind Kind;
    internal string StableId = "";
    internal DispatchKeyNormalizer Normalizer = new DispatchKeyNormalizer();
    internal System.Collections.Generic.Dictionary<string, OpenTypeDispatchBucket> Index =
        new System.Collections.Generic.Dictionary<string, OpenTypeDispatchBucket>(
            System.StringComparer.Ordinal);
    internal OpenTypeDispatchBucket[] Buckets = [];
    internal AsnOpenTypeDispatchEntry[] Entries = [];
    internal TableAtNotationResolution[] KeyAtNotations = [];

    internal OpenTypeDispatchLookupResult ResolveKey(AsnValue? key)
    {
        NormalizedDispatchKey normalized = this.Normalizer.NormalizeKey(key);
        if (!normalized.Success)
        {
            return this.CreateMissingResult(
                normalized,
                "The open-type dispatch key is absent.",
                "");
        }

        return this.ResolveNormalizedKey(normalized, "");
    }

    internal OpenTypeDispatchLookupResult ResolveStructuredValue(AsnValue? value)
    {
        if (value == null || this.KeyAtNotations.Length == 0)
        {
            return this.CreateMissingResult(
                new NormalizedDispatchKey(),
                "The component-relation dispatch key path is absent.",
                "");
        }

        NormalizedDispatchKey? selected = null;
        string selectedPath = "";
        int index = 0;
        while (index < this.KeyAtNotations.Length)
        {
            TableAtNotationResolution notation = this.KeyAtNotations[index];
            AsnValue? key = this.FindPathValue(value, notation.LookupComponents);
            string path = this.FormatPath(notation.LookupComponents);
            NormalizedDispatchKey normalized = this.Normalizer.NormalizeKey(key);
            if (!normalized.Success)
            {
                return this.CreateMissingResult(
                    normalized,
                    "The component-relation dispatch key at '" + path
                        + "' is absent.",
                    path);
            }

            if (selected != null && selected.Identity != normalized.Identity)
            {
                return this.CreateAmbiguousResult(
                    normalized,
                    "The component-relation key paths '" + selectedPath
                        + "' and '" + path + "' contain different UNIQUE values.",
                    path);
            }

            selected = normalized;
            selectedPath = path;
            index = index + 1;
        }

        return this.ResolveNormalizedKey(
            selected == null ? new NormalizedDispatchKey() : selected,
            selectedPath);
    }

    internal OpenTypeDispatchLookupResult ResolveNormalizedKey(
        NormalizedDispatchKey key,
        string valuePath)
    {
        if (!key.Success || !this.Index.ContainsKey(key.Identity))
        {
            return this.CreateMissingResult(
                key,
                "No open-type dispatch entry exists for the normalized UNIQUE value.",
                valuePath);
        }

        OpenTypeDispatchBucket bucket = this.Index[key.Identity];
        if (bucket.Entries.Length != 1)
        {
            return this.CreateAmbiguousResult(
                key,
                "The normalized UNIQUE value identifies more than one open type.",
                valuePath);
        }

        return new OpenTypeDispatchLookupResult
        {
            Key = key,
            Entry = bucket.Entries[0],
            Success = true
        };
    }

    internal AsnValue? FindPathValue(
        AsnValue value,
        ResolvedComponentSyntax[] components)
    {
        AsnValue? current = value;
        int componentIndex = 0;
        while (current != null && componentIndex < components.Length)
        {
            string name = components[componentIndex].Component == null
                ? ""
                : components[componentIndex].Component?.Name ?? "";
            AsnValue? next = null;
            int valueIndex = 0;
            while (valueIndex < current.NamedValues.Length)
            {
                if (current.NamedValues[valueIndex].Name == name)
                {
                    next = current.NamedValues[valueIndex].Value;
                    break;
                }

                valueIndex = valueIndex + 1;
            }

            current = next;
            componentIndex = componentIndex + 1;
        }

        return componentIndex == components.Length ? current : null;
    }

    internal string FormatPath(ResolvedComponentSyntax[] components)
    {
        string result = "";
        int index = 0;
        while (index < components.Length)
        {
            string name = components[index].Component == null
                ? ""
                : components[index].Component?.Name ?? "";
            result = result.Length == 0 ? name : result + "." + name;
            index = index + 1;
        }

        return result;
    }

    internal OpenTypeDispatchLookupResult CreateMissingResult(
        NormalizedDispatchKey key,
        string message,
        string valuePath)
    {
        return new OpenTypeDispatchLookupResult
        {
            Key = key,
            Diagnostics = [this.CreateDiagnostic(
                ConstraintDiagnosticCodes.MissingDispatchKey,
                message,
                valuePath)]
        };
    }

    internal OpenTypeDispatchLookupResult CreateAmbiguousResult(
        NormalizedDispatchKey key,
        string message,
        string valuePath)
    {
        return new OpenTypeDispatchLookupResult
        {
            Key = key,
            Diagnostics = [this.CreateDiagnostic(
                ConstraintDiagnosticCodes.AmbiguousDispatchKey,
                message,
                valuePath)]
        };
    }

    internal AsnDiagnostic CreateDiagnostic(
        int code,
        string message,
        string valuePath)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = AsnDiagnosticCategory.Constraint,
            Code = DiagnosticBag.CreateCode(AsnDiagnosticCategory.Constraint, code),
            Message = message,
            ConstraintId = this.StableId,
            ConstraintKind = this.Kind,
            ValuePath = valuePath
        };
    }
}
