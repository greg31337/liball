namespace cbor;

internal class CddlCustomControlMatcher
{
    internal CddlCoreMatcher Core;
    internal CddlValidationState State;

    internal CddlCustomControlMatcher(
        CddlCoreMatcher core,
        CddlValidationState state)
    {
        Core = core;
        State = state;
    }

    internal CddlMatchResult MatchCustomControl(
        CddlNode node,
        CborValue value)
    {
        string name = node.ControlName ?? "";
        CddlControlRegistration? registration =
            State.Controls.FindControl(name);
        if (registration == null || registration.Handler == null)
        {
            if (State.Options.UnknownControlPolicy
                == CddlUnknownControlPolicy.Ignore)
            {
                return Core.CreateMatch();
            }
            return Core.RejectUnsupported(
                node,
                $"Control {name} is not registered or is disabled."
            );
        }

        CddlControlResult? supplied = registration.Handler.EvaluateControl(
            node.Children[0],
            node.Children[1],
            value,
            State.CloneOptions(State.Options)
        );
        if (supplied == null)
        {
            CddlControlResult missing = new CddlControlResult();
            missing.Status = CddlControlStatus.Error;
            missing.ErrorCode = "CDDL_CONTROL_RESULT_MISSING";
            missing.ErrorMessage =
                "The custom control handler did not return a result.";
            State.AddControlResult(missing);
            return RejectResult(
                node,
                CddlValidationCode.ControlMismatch,
                missing.ErrorMessage,
                missing.ErrorCode,
                []
            );
        }

        CddlControlResult result = WrapResult(supplied, node);
        State.AddControlResult(result);
        if (result.Status == CddlControlStatus.Applied)
        {
            return Core.CreateMatch();
        }
        if (result.Status == CddlControlStatus.NotApplicable
            && State.Options.UnknownControlPolicy
                == CddlUnknownControlPolicy.Ignore)
        {
            return Core.CreateMatch();
        }
        CddlValidationCode code = CddlValidationCode.ControlMismatch;
        if (result.Status == CddlControlStatus.Unsupported
            || result.Status == CddlControlStatus.NotApplicable)
        {
            code = CddlValidationCode.UnsupportedFeature;
        }
        string message = result.ErrorMessage
            ?? $"Custom control {name} returned {result.Status}.";
        string actual = result.ErrorCode ?? result.Status.ToString();
        return RejectResult(node, code, message, actual, result.Issues);
    }

    internal CddlControlResult WrapResult(
        CddlControlResult source,
        CddlNode node)
    {
        CddlControlResult result = new CddlControlResult();
        result.Status = source.Status;
        result.Value = source.Value;
        result.ErrorCode = source.ErrorCode;
        result.ErrorMessage = source.ErrorMessage;
        result.FeatureName = source.FeatureName;
        result.FeatureDetail = source.FeatureDetail;
        CddlValidationIssue[] sourceIssues = source.Issues ?? [];
        result.Issues = new CddlValidationIssue[sourceIssues.Length];
        System.Collections.Generic.List<CddlValidationIssue> visited =
            new System.Collections.Generic.List<CddlValidationIssue>();
        int index = 0;
        while (index < sourceIssues.Length)
        {
            CddlValidationIssue? issue = sourceIssues[index];
            if (issue == null)
            {
                issue = new CddlValidationIssue();
            }
            result.Issues[index] = WrapIssueTree(issue, node, visited, 0);
            index = index + 1;
        }
        return result;
    }

    internal CddlValidationIssue WrapIssueTree(
        CddlValidationIssue source,
        CddlNode node,
        System.Collections.Generic.List<CddlValidationIssue> visited,
        int depth)
    {
        CddlValidationIssue issue = WrapIssue(source, node);
        if (ContainsIdentity(visited, source)
            || depth >= State.Options.MaxNestingDepth)
        {
            return issue;
        }
        visited.Add(source);
        CddlValidationIssue[] sourceDetails = source.Details ?? [];
        issue.Details = new CddlValidationIssue[sourceDetails.Length];
        int index = 0;
        while (index < sourceDetails.Length)
        {
            CddlValidationIssue? detail = sourceDetails[index];
            if (detail == null)
            {
                detail = new CddlValidationIssue();
            }
            issue.Details[index] = WrapIssueTree(
                detail,
                node,
                visited,
                depth + 1
            );
            index = index + 1;
        }
        visited.RemoveAt(visited.Count - 1);
        return issue;
    }

    internal CddlValidationIssue WrapIssue(
        CddlValidationIssue source,
        CddlNode node)
    {
        CddlValidationIssue issue = new CddlValidationIssue();
        issue.Code = source.Code == CddlValidationCode.None
            ? CddlValidationCode.ControlMismatch
            : source.Code;
        issue.Severity = source.Severity;
        issue.Message = source.Message == ""
            ? "The custom control handler reported a validation issue."
            : source.Message;
        issue.InstancePath = source.InstancePath == ""
            || source.InstancePath == "$"
            ? State.InstancePath.CreatePath()
            : source.InstancePath;
        issue.ByteOffset = source.ByteOffset < 0
            ? State.FindInstanceOffset()
            : source.ByteOffset;
        issue.SchemaPath = source.SchemaPath == ""
            ? State.SchemaPath.CreatePath()
            : source.SchemaPath;
        issue.RuleName = source.RuleName ?? State.CurrentRuleName();
        issue.Expected = source.Expected;
        issue.Actual = source.Actual;
        issue.Span = IsEmptySpan(source.Span)
            ? State.FindSpan(node)
            : State.CloneSpan(source.Span);
        issue.Details = [];
        return issue;
    }

    internal bool ContainsIdentity(
        System.Collections.Generic.List<CddlValidationIssue> issues,
        CddlValidationIssue candidate)
    {
        int index = 0;
        while (index < issues.Count)
        {
            if (issues[index] == candidate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool IsEmptySpan(CddlSourceSpan span)
    {
        return span == null
            || (span.SourceName == null
                && span.Start == 0
                && span.Length == 0
                && span.StartLine == 1
                && span.StartColumn == 1
                && span.EndLine == 1
                && span.EndColumn == 1);
    }

    internal CddlMatchResult RejectResult(
        CddlNode node,
        CddlValidationCode code,
        string message,
        string actual,
        CddlValidationIssue[] details)
    {
        CddlValidationIssue issue = State.CreateIssue(
            code,
            message,
            node,
            "a successfully applied custom control",
            actual
        );
        issue.Details = details;
        return Core.CreateRejection(issue);
    }
}
