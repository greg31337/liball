namespace cbor;

/// <summary>Validates JSON documents against the CDDL JSON-compatible subset.</summary>
public class CddlJsonValidator : ICddlJsonValidator
{
    /// <summary>Creates a CDDL JSON validator.</summary>
    public CddlJsonValidator()
    {
    }

    /// <summary>Validates a JSON document against a selected schema rule.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="json">The JSON document text.</param>
    /// <param name="options">The validation selection and budgets.</param>
    /// <returns>The structured validation result.</returns>
    public CddlValidationResult ValidateJson(
        CddlSchema schema,
        string json,
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
        if (json == null)
        {
            return CreateInputFailure(
                CddlValidationCode.InvalidEncoding,
                "A JSON document is required.",
                options,
                "one JSON value",
                "null"
            );
        }

        CborValue placeholder = new CborValue();
        CddlValidationState state = new CddlValidationState(
            schema,
            placeholder,
            options
        );
        if (!state.InitializeValidation())
        {
            return state.CompleteValidation(false);
        }
        CddlRule? selected = state.SelectedRule;
        if (selected == null || selected.Value == null)
        {
            state.AddInvalidSchemaIssue(
                "The selected CDDL validation rule has no expression."
            );
            return state.CompleteValidation(false);
        }
        CddlJsonControlReader reader = new CddlJsonControlReader(
            state,
            selected.Value,
            json
        );
        CborValue? value = reader.ParseJsonValue();
        if (value == null)
        {
            if (!state.Diagnostics.HasErrors)
            {
                CddlValidationIssue issue = state.CreateIssue(
                    CddlValidationCode.InvalidEncoding,
                    reader.ErrorMessage ?? "The text is not one JSON value.",
                    selected.Value,
                    "one complete JSON value",
                    reader.ErrorCode ?? "CDDL_JSON_INVALID"
                );
                issue.InstancePath = reader.ErrorPath ?? "$";
                issue.ByteOffset = reader.ErrorOffset;
                state.AddIssue(issue);
            }
            return state.CompleteValidation(false);
        }
        state.RootValue = value;
        state.JsonNumberMode = true;
        CddlMatchResult match = new CddlCoreMatcher(state).MatchSelectedRule(
            value
        );
        state.JsonNumberMode = false;
        if (!match.IsMatch && match.Issue != null)
        {
            state.AddIssue(match.Issue);
        }
        return state.CompleteValidation(match.IsMatch);
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
