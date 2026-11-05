using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateSyntaxNodeTests
{
    [Test]
    public void DocumentSyntax_StoresSourceSpanChildrenAndDiagnostics()
    {
        TemplateSource source = TemplateSource.FromString("Hello {{ name }}");
        TemplateDocumentSyntax document = new TemplateDocumentSyntax(source);
        TemplateTextSyntax text = new TemplateTextSyntax(source.CreateSpan(0, 6));
        TemplateDiagnostic diagnostic = TemplateDiagnostic.Create("P001", TemplateDiagnosticSeverity.Warning, "Check node", source.CreateSpan(6, 8));

        document.AddChild(TemplateSyntaxNode.FromText(text));
        document.AddDiagnostic(diagnostic);

        Assert.That(document.Source, Is.SameAs(source));
        Assert.That(document.Span.Start, Is.EqualTo(0));
        Assert.That(document.Span.Length, Is.EqualTo(source.Length));
        Assert.That(document.HasChildren(), Is.True);
        Assert.That(document.Children.Count, Is.EqualTo(1));
        Assert.That(document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Text));
        Assert.That(document.Children[0].Text, Is.SameAs(text));
        Assert.That(document.HasDiagnostics(), Is.True);
        Assert.That(document.Diagnostics[0], Is.SameAs(diagnostic));
    }

    [Test]
    public void ConcreteSyntaxNodes_StoreTextOutputCommentRawAndErrorPayloads()
    {
        TemplateSource source = TemplateSource.FromString("Hello {{ name }} raw");
        TemplateTextSyntax text = new TemplateTextSyntax(source.CreateSpan(0, 5));
        TemplateExpressionSyntax expression = new TemplateExpressionSyntax(source.CreateSpan(9, 4), TemplateExpressionSyntaxKind.Variable, "name");
        TemplateOutputSyntax output = new TemplateOutputSyntax(source.CreateSpan(6, 10), expression, false, true);
        TemplateCommentSyntax comment = new TemplateCommentSyntax(source.CreateSpan(0, 5), "comment");
        TemplateRawSyntax raw = new TemplateRawSyntax(source.CreateSpan(17, 3), "raw");
        TemplateDiagnostic diagnostic = TemplateDiagnostic.Create("E001", TemplateDiagnosticSeverity.Error, "Bad syntax", source.CreateSpan(6, 2));
        TemplateErrorSyntax error = new TemplateErrorSyntax(source.CreateSpan(6, 2), diagnostic);

        Assert.That(text.Text, Is.EqualTo("Hello"));
        Assert.That(output.Expression, Is.SameAs(expression));
        Assert.That(output.Raw, Is.False);
        Assert.That(output.Escape, Is.True);
        Assert.That(comment.Text, Is.EqualTo("comment"));
        Assert.That(raw.Text, Is.EqualTo("raw"));
        Assert.That(error.Diagnostic, Is.SameAs(diagnostic));
        Assert.That(error.Message, Is.EqualTo("Bad syntax"));
    }

    [Test]
    public void WrapperFactories_SetKindSpanAndTypedPayload()
    {
        TemplateSource source = TemplateSource.FromString("abc");
        TemplateDocumentSyntax document = new TemplateDocumentSyntax(source);
        TemplateTextSyntax text = new TemplateTextSyntax(source.CreateSpan(0, 1));
        TemplateExpressionSyntax expression = new TemplateExpressionSyntax(source.CreateSpan(1, 1), TemplateExpressionSyntaxKind.Variable, "b");
        TemplateOutputSyntax output = new TemplateOutputSyntax(source.CreateSpan(1, 1), expression, false, true);
        TemplateCommentSyntax comment = new TemplateCommentSyntax(source.CreateSpan(0, 1), "a");
        TemplateRawSyntax raw = new TemplateRawSyntax(source.CreateSpan(2, 1), "c");
        TemplateStatementSyntax statement = new TemplateStatementSyntax(source.CreateSpan(0, 3), "if");
        TemplateBlockSyntax block = new TemplateBlockSyntax(source.CreateSpan(0, 3), "if", statement);
        TemplateDiagnostic diagnostic = TemplateDiagnostic.Create("E001", TemplateDiagnosticSeverity.Error, "Bad syntax", source.CreateSpan(2, 1));
        TemplateErrorSyntax error = new TemplateErrorSyntax(source.CreateSpan(2, 1), diagnostic);

        AssertWrapped(TemplateSyntaxNode.FromDocument(document), TemplateSyntaxKind.Document, document.Span, "abc");
        AssertWrapped(TemplateSyntaxNode.FromText(text), TemplateSyntaxKind.Text, text.Span, "a");
        AssertWrapped(TemplateSyntaxNode.FromOutput(output), TemplateSyntaxKind.Output, output.Span, "b");
        AssertWrapped(TemplateSyntaxNode.FromComment(comment), TemplateSyntaxKind.Comment, comment.Span, "a");
        AssertWrapped(TemplateSyntaxNode.FromRaw(raw), TemplateSyntaxKind.Raw, raw.Span, "c");
        AssertWrapped(TemplateSyntaxNode.FromStatement(statement), TemplateSyntaxKind.Statement, statement.Span, "abc");
        AssertWrapped(TemplateSyntaxNode.FromBlock(block), TemplateSyntaxKind.Block, block.Span, "abc");
        AssertWrapped(TemplateSyntaxNode.FromExpression(expression), TemplateSyntaxKind.Expression, expression.Span, "b");

        TemplateSyntaxNode errorNode = TemplateSyntaxNode.FromError(error);
        Assert.That(errorNode.Kind, Is.EqualTo(TemplateSyntaxKind.Error));
        Assert.That(errorNode.Error, Is.SameAs(error));
        Assert.That(errorNode.IsError(), Is.True);
    }

    [Test]
    public void StatementBlockAndExpressionSyntax_SupportNestedComposition()
    {
        TemplateSource source = TemplateSource.FromString("{% if ok %}yes{% endif %}");
        TemplateExpressionSyntax condition = new TemplateExpressionSyntax(source.CreateSpan(6, 2), TemplateExpressionSyntaxKind.Variable, "ok");
        TemplateStatementSyntax start = new TemplateStatementSyntax(source.CreateSpan(0, 11), "if");
        TemplateStatementSyntax end = new TemplateStatementSyntax(source.CreateSpan(14, 11), "endif");
        TemplateBlockSyntax block = new TemplateBlockSyntax(source.CreateSpan(0, source.Length), "if", start);
        TemplateTextSyntax text = new TemplateTextSyntax(source.CreateSpan(11, 3));
        TemplateExpressionSyntax parentExpression = new TemplateExpressionSyntax(source.CreateSpan(6, 2), TemplateExpressionSyntaxKind.Group, "ok");
        TemplateExpressionSyntax literalExpression = TemplateExpressionSyntax.Create(source.CreateSpan(6, 2), TemplateExpressionSyntaxKind.Literal, TemplateLiteralSyntaxKind.Boolean, "ok");
        TemplateExpressionSyntax pathExpression = TemplateExpressionSyntax.Create(source.CreateSpan(6, 2), TemplateExpressionSyntaxKind.Variable, TemplatePathSyntaxKind.Variable, "ok");
        TemplateExpressionSyntax operatorExpression = TemplateExpressionSyntax.Create(source.CreateSpan(6, 2), TemplateExpressionSyntaxKind.Operator, TemplateExpressionOperatorKind.UnaryNot, "ok");
        TemplateExpressionSyntax callExpression = TemplateExpressionSyntax.Create(source.CreateSpan(6, 2), TemplateExpressionSyntaxKind.Call, TemplateExpressionCallKind.Function, "fn", "ok");
        TemplatePathSegmentSyntax pathSegment = new TemplatePathSegmentSyntax(TemplatePathSegmentSyntaxKind.Name, "ok");

        start.AddArgument(condition);
        parentExpression.AddChild(condition);
        operatorExpression.AddChild(condition);
        callExpression.AddArgument(condition);
        pathExpression.AddPathSegment(pathSegment);
        block.AddChild(TemplateSyntaxNode.FromText(text));
        block.AddElseChild(TemplateSyntaxNode.FromText(TemplateTextSyntax.Create(source.CreateSpan(11, 0), "")));
        block.SetEndStatement(end);

        Assert.That(start.Arguments.Count, Is.EqualTo(1));
        Assert.That(start.Arguments[0], Is.SameAs(condition));
        Assert.That(parentExpression.HasChildren(), Is.True);
        Assert.That(parentExpression.Children[0], Is.SameAs(condition));
        Assert.That(parentExpression.LiteralKind, Is.EqualTo(TemplateLiteralSyntaxKind.None));
        Assert.That(literalExpression.LiteralKind, Is.EqualTo(TemplateLiteralSyntaxKind.Boolean));
        Assert.That(parentExpression.PathKind, Is.EqualTo(TemplatePathSyntaxKind.None));
        Assert.That(pathExpression.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Variable));
        Assert.That(pathExpression.HasPathSegments(), Is.True);
        Assert.That(pathExpression.PathSegments[0], Is.SameAs(pathSegment));
        Assert.That(parentExpression.OperatorKind, Is.EqualTo(TemplateExpressionOperatorKind.None));
        Assert.That(operatorExpression.OperatorKind, Is.EqualTo(TemplateExpressionOperatorKind.UnaryNot));
        Assert.That(operatorExpression.Children[0], Is.SameAs(condition));
        Assert.That(parentExpression.CallKind, Is.EqualTo(TemplateExpressionCallKind.None));
        Assert.That(callExpression.CallKind, Is.EqualTo(TemplateExpressionCallKind.Function));
        Assert.That(callExpression.Name, Is.EqualTo("fn"));
        Assert.That(callExpression.HasArguments(), Is.True);
        Assert.That(callExpression.Arguments[0], Is.SameAs(condition));
        Assert.That(block.Children.Count, Is.EqualTo(1));
        Assert.That(block.ElseChildren.Count, Is.EqualTo(1));
        Assert.That(block.HasEndStatement(), Is.True);
        Assert.That(block.EndStatement, Is.SameAs(end));
    }

    [Test]
    public void ConstructorsAndAddMethods_RejectMissingRequiredValues()
    {
        TemplateSource source = TemplateSource.FromString("abc");
        TemplateSourceSpan span = source.CreateSpan(0, 1);
        TemplateExpressionSyntax expression = new TemplateExpressionSyntax(span, TemplateExpressionSyntaxKind.Variable, "a");
        TemplateStatementSyntax statement = new TemplateStatementSyntax(span, "if");
        TemplateBlockSyntax block = new TemplateBlockSyntax(span, "if", statement);
        TemplateDocumentSyntax document = new TemplateDocumentSyntax(source);

        Assert.Throws<ArgumentNullException>(() => new TemplateDocumentSyntax(null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateTextSyntax(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateTextSyntax.Create(span, null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateOutputSyntax(span, null!, false, true));
        Assert.Throws<ArgumentNullException>(() => new TemplateCommentSyntax(span, null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateRawSyntax(span, null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateStatementSyntax(span, null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateBlockSyntax(span, "if", null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateExpressionSyntax(span, TemplateExpressionSyntaxKind.Variable, null!));
        Assert.Throws<ArgumentNullException>(() => TemplateExpressionSyntax.Create(span, TemplateExpressionSyntaxKind.Literal, TemplateLiteralSyntaxKind.String, null!));
        Assert.Throws<ArgumentNullException>(() => TemplateExpressionSyntax.Create(span, TemplateExpressionSyntaxKind.Path, TemplatePathSyntaxKind.Variable, null!));
        Assert.Throws<ArgumentNullException>(() => TemplateExpressionSyntax.Create(span, TemplateExpressionSyntaxKind.Operator, TemplateExpressionOperatorKind.Add, null!));
        Assert.Throws<ArgumentNullException>(() => TemplateExpressionSyntax.Create(span, TemplateExpressionSyntaxKind.Call, TemplateExpressionCallKind.Function, null!, "x"));
        Assert.Throws<ArgumentNullException>(() => TemplateExpressionSyntax.Create(span, TemplateExpressionSyntaxKind.Call, TemplateExpressionCallKind.Function, "fn", null!));
        Assert.Throws<ArgumentNullException>(() => new TemplatePathSegmentSyntax(TemplatePathSegmentSyntaxKind.Name, null!));
        Assert.Throws<ArgumentNullException>(() => TemplatePathSegmentSyntax.Create(TemplatePathSegmentSyntaxKind.Indexer, null!, expression));
        Assert.Throws<ArgumentNullException>(() => TemplatePathSegmentSyntax.Create(TemplatePathSegmentSyntaxKind.Indexer, "x", null!));
        Assert.Throws<ArgumentNullException>(() => new TemplateErrorSyntax(span, null!));
        Assert.Throws<ArgumentNullException>(() => document.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => document.AddDiagnostic(null!));
        Assert.Throws<ArgumentNullException>(() => statement.AddArgument(null!));
        Assert.Throws<ArgumentNullException>(() => block.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => block.AddElseChild(null!));
        Assert.Throws<ArgumentNullException>(() => block.SetEndStatement(null!));
        Assert.Throws<ArgumentNullException>(() => expression.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => expression.AddPathSegment(null!));
        Assert.Throws<ArgumentNullException>(() => expression.AddArgument(null!));
    }

    static void AssertWrapped(TemplateSyntaxNode node, TemplateSyntaxKind kind, TemplateSourceSpan span, string sourceText)
    {
        Assert.That(node.Kind, Is.EqualTo(kind));
        Assert.That(node.Span, Is.SameAs(span));
        Assert.That(node.GetSourceText(), Is.EqualTo(sourceText));
    }
}
