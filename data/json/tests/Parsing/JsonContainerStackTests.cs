namespace json.Tests;

public class JsonContainerStackTests
{
    [Test]
    public void RootAcceptsExactlyOneValueAndCanBeReset()
    {
        JsonContainerStack stack = new JsonContainerStack(4);

        Assert.Multiple(() =>
        {
            Assert.That(stack.GetFrameCount(), Is.EqualTo(1));
            Assert.That(stack.GetContainerDepth(), Is.Zero);
            Assert.That(stack.GetCurrentFrame().GetContainerKind(), Is.EqualTo(JsonContainerKind.Root));
            Assert.That(stack.GetCurrentFrame().GetContainerState(), Is.EqualTo(JsonContainerState.RootValue));
            Assert.That(stack.IsDocumentComplete(), Is.False);
        });

        stack.AcceptScalarValue();
        stack.CompleteInput();

        Assert.Multiple(() =>
        {
            Assert.That(stack.IsDocumentComplete(), Is.True);
            Assert.That(stack.GetCurrentFrame().GetValueCount(), Is.EqualTo(1));
            Assert.That(stack.GetCurrentFrame().GetContainerState(), Is.EqualTo(JsonContainerState.RootComplete));
        });

        AssertFailure(() => stack.AcceptScalarValue(), JsonDiagnosticCode.TrailingContent);

        stack.ResetStack();

        Assert.Multiple(() =>
        {
            Assert.That(stack.IsDocumentComplete(), Is.False);
            Assert.That(stack.GetCurrentFrame().GetValueCount(), Is.Zero);
            Assert.That(stack.GetCurrentFrame().GetContainerState(), Is.EqualTo(JsonContainerState.RootValue));
        });
    }

    [Test]
    public void ArraysDistinguishEmptyClosureMissingCommasAndTrailingCommas()
    {
        JsonContainerStack stack = new JsonContainerStack(4);
        stack.OpenArray();
        JsonContainerFrame array = stack.GetCurrentFrame();

        Assert.Multiple(() =>
        {
            Assert.That(array.GetContainerKind(), Is.EqualTo(JsonContainerKind.Array));
            Assert.That(array.GetContainerState(), Is.EqualTo(JsonContainerState.ArrayFirstValueOrEnd));
            Assert.That(array.CanAcceptValue(), Is.True);
            Assert.That(array.CanCloseContainer(), Is.True);
        });

        stack.AcceptScalarValue();
        Assert.That(array.GetValueCount(), Is.EqualTo(1));
        AssertFailure(() => stack.AcceptScalarValue(), JsonDiagnosticCode.MissingCommaOrEnd);

        stack.AcceptValueSeparator();
        Assert.Multiple(() =>
        {
            Assert.That(array.GetContainerState(), Is.EqualTo(JsonContainerState.ArrayValue));
            Assert.That(array.CanCloseContainer(), Is.False);
        });
        AssertFailure(() => stack.CloseArray(), JsonDiagnosticCode.TrailingComma);

        stack.AcceptScalarValue();
        stack.CloseArray();

        Assert.Multiple(() =>
        {
            Assert.That(array.GetValueCount(), Is.EqualTo(2));
            Assert.That(array.IsComplete(), Is.True);
            Assert.That(stack.IsDocumentComplete(), Is.True);
        });

        stack.ResetStack();
        stack.OpenArray();
        stack.CloseArray();
        Assert.That(stack.IsDocumentComplete(), Is.True);
    }

    [Test]
    public void ObjectsEnforceNameColonValueAndSeparatorTransitions()
    {
        JsonContainerStack stack = new JsonContainerStack(6);
        stack.OpenObject();
        JsonContainerFrame objectFrame = stack.GetCurrentFrame();

        Assert.Multiple(() =>
        {
            Assert.That(objectFrame.CanAcceptPropertyName(), Is.True);
            Assert.That(objectFrame.CanCloseContainer(), Is.True);
            Assert.That(objectFrame.GetMemberCount(), Is.Zero);
        });

        stack.AcceptPropertyName();
        Assert.That(objectFrame.GetContainerState(), Is.EqualTo(JsonContainerState.ObjectColon));
        AssertFailure(() => stack.AcceptScalarValue(), JsonDiagnosticCode.MissingColon);

        stack.AcceptNameSeparator();
        Assert.That(objectFrame.GetContainerState(), Is.EqualTo(JsonContainerState.ObjectValue));
        stack.OpenArray();
        JsonContainerFrame nestedArray = stack.GetCurrentFrame();
        stack.AcceptScalarValue();
        stack.CloseArray();

        Assert.Multiple(() =>
        {
            Assert.That(nestedArray.GetValueCount(), Is.EqualTo(1));
            Assert.That(objectFrame.GetMemberCount(), Is.EqualTo(1));
            Assert.That(objectFrame.GetContainerState(), Is.EqualTo(JsonContainerState.ObjectCommaOrEnd));
        });

        AssertFailure(() => stack.AcceptPropertyName(), JsonDiagnosticCode.MissingCommaOrEnd);
        stack.AcceptValueSeparator();
        AssertFailure(() => stack.CloseObject(), JsonDiagnosticCode.TrailingComma);

        stack.AcceptPropertyName();
        stack.AcceptNameSeparator();
        stack.AcceptScalarValue();
        stack.CloseObject();

        Assert.Multiple(() =>
        {
            Assert.That(objectFrame.GetMemberCount(), Is.EqualTo(2));
            Assert.That(stack.IsDocumentComplete(), Is.True);
        });

        stack.ResetStack();
        stack.OpenObject();
        stack.CloseObject();
        Assert.That(stack.IsDocumentComplete(), Is.True);
    }

    [Test]
    public void MismatchedClosuresDoNotChangeTheStack()
    {
        JsonContainerStack stack = new JsonContainerStack(4);
        stack.OpenArray();

        AssertFailure(() => stack.CloseObject(), JsonDiagnosticCode.UnexpectedToken);
        Assert.Multiple(() =>
        {
            Assert.That(stack.GetContainerDepth(), Is.EqualTo(1));
            Assert.That(stack.GetCurrentFrame().GetContainerKind(), Is.EqualTo(JsonContainerKind.Array));
            Assert.That(stack.GetCurrentFrame().GetContainerState(), Is.EqualTo(JsonContainerState.ArrayFirstValueOrEnd));
        });

        stack.CloseArray();
        AssertFailure(() => stack.CloseArray(), JsonDiagnosticCode.UnexpectedToken);
        Assert.That(stack.IsDocumentComplete(), Is.True);
    }

    [Test]
    public void DepthLimitFailureLeavesTheParentStateUnchanged()
    {
        JsonContainerStack stack = new JsonContainerStack(2);
        stack.OpenArray();
        stack.OpenArray();
        JsonContainerFrame deepest = stack.GetCurrentFrame();

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => stack.OpenObject())
            ?? throw new InvalidOperationException("Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.NestingDepthLimitExceeded));
            Assert.That(exception.Diagnostic.Depth, Is.EqualTo(2));
            Assert.That(stack.GetContainerDepth(), Is.EqualTo(2));
            Assert.That(deepest.GetContainerState(), Is.EqualTo(JsonContainerState.ArrayFirstValueOrEnd));
            Assert.That(deepest.GetValueCount(), Is.Zero);
        });

        stack.CloseArray();
        stack.CloseArray();
        Assert.That(stack.IsDocumentComplete(), Is.True);
    }

    [Test]
    public void StackGrowsForDeepInputWithoutRecursiveState()
    {
        JsonContainerStack stack = new JsonContainerStack(40);

        for (int depth = 1; depth <= 32; depth++)
        {
            stack.OpenArray();
            Assert.That(stack.GetContainerDepth(), Is.EqualTo(depth));
        }

        Assert.That(stack.GetFrameCount(), Is.EqualTo(33));

        for (int depth = 32; depth >= 1; depth--)
        {
            stack.CloseArray();
            Assert.That(stack.GetContainerDepth(), Is.EqualTo(depth - 1));
        }

        stack.CompleteInput();
        Assert.That(stack.IsDocumentComplete(), Is.True);
    }

    [Test]
    public void CompletionAndConfigurationFailuresHaveStableDiagnostics()
    {
        JsonContainerStack empty = new JsonContainerStack(1);
        AssertFailure(() => empty.CompleteInput(), JsonDiagnosticCode.UnexpectedEndOfInput);

        empty.OpenObject();
        AssertFailure(() => empty.CompleteInput(), JsonDiagnosticCode.UnexpectedEndOfInput);

        JsonLimitException invalidLimit = Assert.Throws<JsonLimitException>(() => new JsonContainerStack(-1))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        Assert.That(invalidLimit.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [Test]
    public void ResetDropsPartialNestedGrammarState()
    {
        JsonContainerStack stack = new JsonContainerStack(5);
        stack.OpenObject();
        stack.AcceptPropertyName();
        stack.AcceptNameSeparator();
        stack.OpenArray();
        stack.AcceptScalarValue();

        stack.ResetStack();

        Assert.Multiple(() =>
        {
            Assert.That(stack.GetFrameCount(), Is.EqualTo(1));
            Assert.That(stack.GetContainerDepth(), Is.Zero);
            Assert.That(stack.GetCurrentFrame().GetContainerKind(), Is.EqualTo(JsonContainerKind.Root));
            Assert.That(stack.GetCurrentFrame().GetContainerState(), Is.EqualTo(JsonContainerState.RootValue));
        });

        stack.OpenObject();
        stack.CloseObject();
        stack.CompleteInput();
        Assert.That(stack.IsDocumentComplete(), Is.True);
    }

    static void AssertFailure(TestDelegate action, JsonDiagnosticCode expectedCode)
    {
        JsonParseException exception = Assert.Throws<JsonParseException>(action)
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode));
    }
}
