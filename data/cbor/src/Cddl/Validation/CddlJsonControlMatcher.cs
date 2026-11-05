namespace cbor;

internal class CddlJsonControlMatcher
{
    internal CddlCoreMatcher Matcher;
    internal CddlValidationState State;

    internal CddlJsonControlMatcher(
        CddlCoreMatcher matcher,
        CddlValidationState state)
    {
        Matcher = matcher;
        State = state;
    }

    internal CddlMatchResult MatchJsonControl(
        CddlNode node,
        CborValue value)
    {
        CddlJsonControlReader reader = new CddlJsonControlReader(
            State,
            node,
            value.TextStringValue ?? ""
        );
        CborValue? converted = reader.ParseJsonValue();
        if (converted == null)
        {
            if (State.Diagnostics.HasErrors)
            {
                return Matcher.CreateNoIssueRejection();
            }
            string actual = reader.ErrorCode ?? "CDDL_JSON_INVALID";
            if (reader.ErrorOffset >= 0)
            {
                actual = actual + " at character " + reader.ErrorOffset;
            }
            if (reader.ErrorPath != null)
            {
                actual = actual + " at " + reader.ErrorPath;
            }
            CddlValidationIssue issue = State.CreateIssue(
                CddlValidationCode.ControlMismatch,
                reader.ErrorMessage ?? "The text is not one JSON value.",
                node,
                "one complete JSON value",
                actual
            );
            return Matcher.CreateRejection(issue);
        }
        int checkpoint = State.CreateControlResultCheckpoint();
        bool previousJsonNumberMode = State.JsonNumberMode;
        State.JsonNumberMode = true;
        CddlMatchResult result = Matcher.MatchEmbeddedValue(node, converted);
        State.JsonNumberMode = previousJsonNumberMode;
        if (!result.IsMatch)
        {
            State.RestoreControlResults(checkpoint);
        }
        return result;
    }
}
