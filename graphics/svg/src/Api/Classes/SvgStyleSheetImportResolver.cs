namespace svg;

/// <summary>Resolves CSS import rules exclusively through a caller resource provider.</summary>
public class SvgStyleSheetImportResolver : ISvgStyleSheetImportResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgStyleSheetImportResolver()
    {
    }

    /// <summary>Expands matching imports before their containing stylesheets.</summary>
    /// <param name="collection">The collected root stylesheets.</param>
    /// <param name="options">The resource provider, policy, cancellation, and limits.</param>
    /// <param name="mediaMatches">A media-condition evaluator, or <c>null</c> to match every condition.</param>
    /// <returns>A new import-expanded collection with resource diagnostics and totals.</returns>
    public SvgStyleSheetCollection Resolve(SvgStyleSheetCollection collection, SvgLoadOptions options, System.Func<string, bool>? mediaMatches)
    {
        if (collection == null) throw new System.ArgumentNullException("collection");
        if (!new SvgStyleSheetImportResolver().ValidOptions(options)) throw new System.ArgumentException("Import resolution options are invalid.", "options");
        SvgStyleImportState state = new SvgStyleImportState(options, mediaMatches, collection.Diagnostics);
        state.ReserveExisting(collection.StyleSheets);
        int index = 0;
        while (index < collection.StyleSheets.Count)
        {
            SvgCollectedStyleSheet entry = collection.StyleSheets[index];
            System.Collections.Generic.List<string> activeUris = new System.Collections.Generic.List<string>();
            string rootUri = new SvgStyleSheetImportResolver().SourceUri(entry.StyleSheet);
            if (rootUri.Length != 0) activeUris.Add(new SvgStyleSheetImportResolver().NormalizeUri("", rootUri));
            state.Expand(entry, 0, activeUris);
            index = index + 1;
        }
        SvgStyleSheetCollection result = new SvgStyleSheetCollection(state.StyleSheets, state.Diagnostics);
        result.Resources.RequestCount = state.RequestCount;
        result.Resources.LoadedCount = state.LoadedCount;
        result.Resources.EncodedBytes = state.EncodedBytes;
        return result;
    }

    internal string NormalizeUri(string baseUri, string reference)
    {
        SvgUriResult result = new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(baseUri, reference, 268435455);
        return result.Success ? result.NormalizedIri : reference;
    }

    internal bool PolicyAllows(SvgResourcePolicy policy, string parentUri, string authoredUri, string normalizedUri)
    {
        SvgResourcePolicyRequest request = new SvgResourcePolicyRequest(authoredUri, parentUri, normalizedUri, SvgResourceKind.StyleSheet);
        request.MediaType = SvgMimeTypes.Css;
        return policy.Evaluate(request).Allowed;
    }

    internal bool ValidUriText(string? value)
    {
        if (value == null || value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= 0x20 || character == 0x7f || character == '\\') return false;
            index = index + 1;
        }
        return true;
    }
bool ValidOptions(SvgLoadOptions? options)
    {
        if (options == null || options.Limits == null || options.ResourcePolicy == null) return false;
        return options.Limits.Validate().Success;
    }
string SourceUri(SvgStyleSheet? sheet)
    {
        if (sheet == null || sheet.Source == null || sheet.Source.Source == null) return "";
        return sheet.Source.Source.Uri;
    }

}
