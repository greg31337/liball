using System.Collections.Generic;

namespace cbor;

/// <summary>Validates generic CBOR values against compiled CDDL schemas.</summary>
public class CddlValidator : ICddlValidator
{
    /// <summary>Creates a CDDL CBOR validator.</summary>
    public CddlValidator()
    {
    }

    /// <summary>Validates a CBOR value against a selected schema rule.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="value">The CBOR value to validate.</param>
    /// <param name="options">The validation selection and budgets.</param>
    /// <returns>The structured validation result.</returns>
    public CddlValidationResult ValidateCbor(
        CddlSchema schema,
        CborValue value,
        CddlValidationOptions options)
    {
        if (options == null)
        {
            return CreateInputFailure(
                CddlValidationCode.InvalidSchema,
                "CDDL validation options are required.",
                new CddlValidationOptions(),
                "validation options",
                "null"
            );
        }
        if (schema == null)
        {
            return CreateInputFailure(
                CddlValidationCode.InvalidSchema,
                "A compiled CDDL schema is required.",
                options,
                "a compiled schema",
                "null"
            );
        }
        if (value == null)
        {
            return CreateInputFailure(
                CddlValidationCode.TypeMismatch,
                "A CBOR value is required.",
                options,
                "a CBOR value",
                "null"
            );
        }

        CddlValidationState state = new CddlValidationState(
            schema,
            value,
            options
        );
        if (!state.InitializeValidation())
        {
            return state.CompleteValidation(false);
        }
        CddlCoreMatcher matcher = new CddlCoreMatcher(state);
        CddlMatchResult match = matcher.MatchSelectedRule(value);
        if (!match.IsMatch && match.Issue != null)
        {
            state.AddIssue(match.Issue);
        }
        return state.CompleteValidation(match.IsMatch);
    }

    /// <summary>Decodes and validates exactly one encoded CBOR data item.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="decodeOptions">The decoding policies and limits.</param>
    /// <param name="validationOptions">The CDDL validation selection and budgets.</param>
    /// <returns>The structured decoding or validation result.</returns>
    public CddlValidationResult ValidateEncodedCbor(
        CddlSchema schema,
        byte[] source,
        CborDecodeOptions decodeOptions,
        CddlValidationOptions validationOptions)
    {
        if (validationOptions == null)
        {
            return CreateInputFailure(
                CddlValidationCode.InvalidSchema,
                "CDDL validation options are required.",
                new CddlValidationOptions(),
                "validation options",
                "null"
            );
        }
        if (schema == null)
        {
            return CreateInputFailure(
                CddlValidationCode.InvalidSchema,
                "A compiled CDDL schema is required.",
                validationOptions,
                "a compiled schema",
                "null"
            );
        }
        if (decodeOptions == null)
        {
            return CreateInputFailure(
                CddlValidationCode.InvalidEncoding,
                "CBOR decoding options are required.",
                validationOptions,
                "decoding options",
                "null"
            );
        }
        if (source == null)
        {
            return CreateInputFailure(
                CddlValidationCode.InvalidEncoding,
                "Encoded CBOR bytes are required.",
                validationOptions,
                "one encoded CBOR data item",
                "null"
            );
        }

        CborDecodeOptions retainedOptions = CreateRetainedDecodeOptions(
            decodeOptions
        );
        CborReadResult decoded = new CborDecoder().Decode(
            source,
            retainedOptions
        );
        if (decoded.Status != CborReadStatus.Done || decoded.Value == null)
        {
            return CreateDecodeFailure(decoded, validationOptions);
        }
        return ValidateCbor(schema, decoded.Value, validationOptions);
    }

    internal CborDecodeOptions CreateRetainedDecodeOptions(
        CborDecodeOptions source)
    {
        CborDecodeOptions options = new CborDecodeOptions();
        options.AllowTrailingData = false;
        options.DuplicateKeyPolicy = source.DuplicateKeyPolicy;
        options.TagPolicy = source.TagPolicy;
        options.SharedValuePolicy = source.SharedValuePolicy;
        options.ValidationLevel = source.ValidationLevel;
        options.DeterministicEncodingProfile =
            source.DeterministicEncodingProfile;
        options.AllowIndefiniteLength = source.AllowIndefiniteLength;
        options.AllowUnknownSimpleValues = source.AllowUnknownSimpleValues;
        options.RejectInvalidUtf8 = source.RejectInvalidUtf8;
        options.RetainRepresentation = source.RetainRepresentation;
        options.RetainSourceRanges = true;
        options.MaxNestingDepth = source.MaxNestingDepth;
        options.MaxTotalItems = source.MaxTotalItems;
        options.MaxArrayItems = source.MaxArrayItems;
        options.MaxMapPairs = source.MaxMapPairs;
        options.MaxStringBytes = source.MaxStringBytes;
        options.MaxTotalBytes = source.MaxTotalBytes;
        options.MaxRetainedSourceBytes = source.MaxRetainedSourceBytes;
        options.MaxSequenceItems = source.MaxSequenceItems;
        options.MaxSequenceBytes = source.MaxSequenceBytes;
        options.MaxEmbeddedDepth = source.MaxEmbeddedDepth;
        options.MaxValidationIssues = source.MaxValidationIssues;
        options.CollectValidationIssues = source.CollectValidationIssues;
        options.TagRegistrations = new List<CborTagRegistration>();
        int index = 0;
        while (index < source.TagRegistrations.Count)
        {
            options.TagRegistrations.Add(source.TagRegistrations[index]);
            index = index + 1;
        }
        return options;
    }

    internal CddlValidationResult CreateDecodeFailure(
        CborReadResult decoded,
        CddlValidationOptions options)
    {
        CddlValidationIssue issue = new CddlValidationIssue();
        issue.Code = CddlValidationCode.InvalidEncoding;
        issue.Message = decoded.ErrorMessage
            ?? "The encoded bytes are not one complete CBOR data item.";
        issue.InstancePath = decoded.ErrorPath ?? "$";
        issue.ByteOffset = decoded.ErrorOffset;
        issue.Expected = "one well-formed CBOR data item";
        issue.Actual = decoded.ErrorCode ?? decoded.Status.ToString();
        CddlValidationResult result = new CddlValidationResult();
        result.Options = options;
        if (options.MaxIssues > 0)
        {
            result.Issues = [issue];
        }
        return result;
    }

    internal CddlValidationResult CreateInputFailure(
        CddlValidationCode code,
        string message,
        CddlValidationOptions options,
        string expected,
        string actual)
    {
        CddlValidationIssue issue = new CddlValidationIssue();
        issue.Code = code;
        issue.Message = message;
        issue.Expected = expected;
        issue.Actual = actual;
        CddlValidationResult result = new CddlValidationResult();
        result.Options = options;
        if (options.MaxIssues > 0)
        {
            result.Issues = [issue];
        }
        return result;
    }
}
