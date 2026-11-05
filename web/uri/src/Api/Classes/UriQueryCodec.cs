namespace uri;

/// <summary>
/// Provides bounded query parsing, composition, and explicit parameter transformations.
/// </summary>
public class UriQueryCodec : IUriQueryCodec
{
    /// <summary>Initializes a stateless URI query codec.</summary>
    public UriQueryCodec()
    {
    }

    /// <summary>Parses encoded query text into ordered decoded parameters.</summary>
    /// <param name="query">The query text without a leading question mark.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query parsing outcome.</returns>
    public UriQueryResult ParseQuery(string query, UriQueryOptions options)
    {
        UriQueryParseCoreResult result = QueryCodecCore.ParseQuery(query, options);
        if (!result.Success)
        {
            return CreateQueryFailure(result.PairDelimiter, result.Diagnostic);
        }

        UriDiagnostic? outputLimit = LimitGuard.CheckOutputCharacters(
            query.Length,
            options.Limits,
            DiagnosticDomain.Uri);
        if (outputLimit != null)
        {
            return CreateQueryFailure(result.PairDelimiter, outputLimit);
        }

        return new UriQueryResult(
            true,
            ConvertInternalParameters(result.Parameters),
            query,
            query.Length,
            result.PairDelimiter,
            null);
    }

    /// <summary>Composes ordered decoded parameters as encoded query text.</summary>
    /// <param name="parameters">The ordered decoded parameters.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query composition outcome.</returns>
    public UriQueryResult ComposeQuery(
        UriQueryParameter[] parameters,
        UriQueryOptions options)
    {
        UriQueryParameterConversionResult conversion = ConvertPublicParameters(
            parameters,
            options);
        if (!conversion.Success)
        {
            return CreateQueryFailure(
                QueryCodecCore.GetPairDelimiter(options),
                conversion.Diagnostic);
        }

        return ComposeConvertedParameters(conversion.Parameters, options);
    }

    /// <summary>Appends one parameter and composes the resulting query.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="parameter">The parameter to append.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    public UriQueryResult AddQueryParameter(
        UriQueryParameter[] parameters,
        UriQueryParameter parameter,
        UriQueryOptions options)
    {
        UriQueryParameterConversionResult source = ConvertPublicParameters(
            parameters,
            options);
        if (!source.Success)
        {
            return CreateQueryFailure(
                QueryCodecCore.GetPairDelimiter(options),
                source.Diagnostic);
        }

        UriQueryParameter[] addedParameters = new UriQueryParameter[] { parameter };
        UriQueryParameterConversionResult added = ConvertPublicParameters(
            addedParameters,
            options);
        if (!added.Success)
        {
            return CreateQueryFailure(
                QueryCodecCore.GetPairDelimiter(options),
                added.Diagnostic);
        }

        UriDiagnostic? itemLimit = CheckOperationItemCount(
            (long)source.Parameters.Length + 1,
            options);
        if (itemLimit != null)
        {
            return CreateQueryFailure(options.PairDelimiter, itemLimit);
        }

        UriQueryParameterValue[] result = QueryCodecCore.AddQueryParameter(
            source.Parameters,
            added.Parameters[0]);
        return ComposeConvertedParameters(result, options);
    }

    /// <summary>Replaces all ordinal name matches and composes the resulting query.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="name">The decoded name to match ordinally.</param>
    /// <param name="replacements">The parameters inserted at the first match, or appended when absent.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    public UriQueryResult ReplaceQueryParameters(
        UriQueryParameter[] parameters,
        string name,
        UriQueryParameter[] replacements,
        UriQueryOptions options)
    {
        UriQueryParameterConversionResult source = ConvertPublicParameters(
            parameters,
            options);
        if (!source.Success)
        {
            return CreateQueryFailure(
                QueryCodecCore.GetPairDelimiter(options),
                source.Diagnostic);
        }

        UriDiagnostic? nameFailure = GetQueryNameDiagnostic(name, options);
        if (nameFailure != null)
        {
            return CreateQueryFailure(options.PairDelimiter, nameFailure);
        }

        UriQueryParameterConversionResult convertedReplacements = ConvertPublicParameters(
            replacements,
            options);
        if (!convertedReplacements.Success)
        {
            return CreateQueryFailure(
                options.PairDelimiter,
                convertedReplacements.Diagnostic);
        }

        int matchCount = QueryCodecCore.CountQueryParameterNames(
            source.Parameters,
            name);
        long resultCount = (long)source.Parameters.Length - matchCount +
            convertedReplacements.Parameters.Length;
        UriDiagnostic? itemLimit = CheckOperationItemCount(resultCount, options);
        if (itemLimit != null)
        {
            return CreateQueryFailure(options.PairDelimiter, itemLimit);
        }

        UriQueryParameterValue[] result = QueryCodecCore.ReplaceQueryParameters(
            source.Parameters,
            name,
            convertedReplacements.Parameters);
        return ComposeConvertedParameters(result, options);
    }

    /// <summary>Removes every ordinal name match and composes the resulting query.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="name">The decoded name to remove ordinally.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    public UriQueryResult RemoveQueryParameters(
        UriQueryParameter[] parameters,
        string name,
        UriQueryOptions options)
    {
        UriQueryParameterConversionResult source = ConvertPublicParameters(
            parameters,
            options);
        if (!source.Success)
        {
            return CreateQueryFailure(
                QueryCodecCore.GetPairDelimiter(options),
                source.Diagnostic);
        }

        UriDiagnostic? nameFailure = GetQueryNameDiagnostic(name, options);
        if (nameFailure != null)
        {
            return CreateQueryFailure(options.PairDelimiter, nameFailure);
        }

        UriQueryParameterValue[] result = QueryCodecCore.RemoveQueryParameters(
            source.Parameters,
            name);
        return ComposeConvertedParameters(result, options);
    }

    /// <summary>Stably sorts parameters by ordinal name, value state, and value.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    public UriQueryResult SortQueryParameters(
        UriQueryParameter[] parameters,
        UriQueryOptions options)
    {
        UriQueryParameterConversionResult source = ConvertPublicParameters(
            parameters,
            options);
        if (!source.Success)
        {
            return CreateQueryFailure(
                QueryCodecCore.GetPairDelimiter(options),
                source.Diagnostic);
        }

        UriQueryParameterValue[] result = QueryCodecCore.SortQueryParameters(
            source.Parameters);
        return ComposeConvertedParameters(result, options);
    }

    /// <summary>Removes exact semantic pair duplicates while keeping each first occurrence.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    public UriQueryResult DeduplicateQueryParameters(
        UriQueryParameter[] parameters,
        UriQueryOptions options)
    {
        UriQueryParameterConversionResult source = ConvertPublicParameters(
            parameters,
            options);
        if (!source.Success)
        {
            return CreateQueryFailure(
                QueryCodecCore.GetPairDelimiter(options),
                source.Diagnostic);
        }

        UriQueryParameterValue[] result = QueryCodecCore.DeduplicateQueryParameters(
            source.Parameters);
        return ComposeConvertedParameters(result, options);
    }

    internal UriQueryParameterConversionResult ConvertPublicParameters(
        UriQueryParameter[]? parameters,
        UriQueryOptions? options)
    {
        if (parameters == null ||
            options == null ||
            options.Limits == null ||
            !QueryCodecCore.IsSupportedPairDelimiter(options.PairDelimiter))
        {
            return CreateConversionFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriDiagnostic? itemLimit = LimitGuard.CheckQueryItems(
            parameters.Length,
            options.Limits,
            DiagnosticDomain.Uri,
            0);
        if (itemLimit != null)
        {
            return CreateConversionFailure(itemLimit);
        }

        UriQueryParameterValue[] converted = new UriQueryParameterValue[parameters.Length];
        int offset = 0;
        while (offset < parameters.Length)
        {
            UriQueryParameter? parameter = parameters[offset];
            if (parameter == null ||
                parameter.Name == null ||
                parameter.Value == null)
            {
                return CreateConversionFailure(
                    DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
            }

            converted[offset] = new UriQueryParameterValue(
                parameter.Name,
                parameter.HasValue,
                parameter.Value);
            offset = offset + 1;
        }

        UriDiagnostic? inputFailure = QueryCodecCore.GetComposeInputDiagnostic(
            converted,
            options);
        if (inputFailure != null)
        {
            return CreateConversionFailure(inputFailure);
        }

        return new UriQueryParameterConversionResult(true, converted, null);
    }

    internal UriQueryParameter[] ConvertInternalParameters(
        UriQueryParameterValue[] parameters)
    {
        UriQueryParameter[] converted = new UriQueryParameter[parameters.Length];
        int offset = 0;
        while (offset < parameters.Length)
        {
            UriQueryParameterValue parameter = parameters[offset];
            converted[offset] = new UriQueryParameter(
                parameter.Name,
                parameter.HasValue,
                parameter.Value);
            offset = offset + 1;
        }

        return converted;
    }

    internal UriQueryResult ComposeConvertedParameters(
        UriQueryParameterValue[] parameters,
        UriQueryOptions options)
    {
        UriQueryComposeCoreResult result = QueryCodecCore.ComposeQuery(
            parameters,
            options);
        if (!result.Success)
        {
            return CreateQueryFailure(result.PairDelimiter, result.Diagnostic);
        }

        return new UriQueryResult(
            true,
            ConvertInternalParameters(parameters),
            result.Text,
            result.Length,
            result.PairDelimiter,
            null);
    }

    internal UriDiagnostic? GetQueryNameDiagnostic(
        string? name,
        UriQueryOptions? options)
    {
        if (name == null || options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            name.Length,
            options.Limits,
            DiagnosticDomain.Uri);
        if (inputLimit != null)
        {
            return inputLimit;
        }

        return LimitGuard.CheckComponentCharacters(
            name.Length,
            options.Limits,
            DiagnosticDomain.Uri,
            UriComponent.Query,
            0);
    }

    internal UriDiagnostic? CheckOperationItemCount(
        long itemCount,
        UriQueryOptions options)
    {
        if (itemCount > int.MaxValue)
        {
            return DiagnosticFactory.CreateLimitExceeded(
                DiagnosticDomain.Uri,
                UriComponent.Query,
                0,
                0);
        }

        return LimitGuard.CheckQueryItems(
            (int)itemCount,
            options.Limits,
            DiagnosticDomain.Uri,
            0);
    }

    internal UriQueryParameterConversionResult CreateConversionFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriQueryParameterConversionResult(
            false,
            new UriQueryParameterValue[0],
            failure);
    }

    internal UriQueryResult CreateQueryFailure(
        char pairDelimiter,
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriQueryResult(
            false,
            new UriQueryParameter[0],
            "",
            0,
            pairDelimiter,
            failure);
    }
}
