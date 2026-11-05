namespace uri.Tests;

public class LimitGuardTests
{
    [Test]
    public void CharacterGuardsAcceptCountsAtTheirLimits()
    {
        OperationLimits limits = CreateSmallLimits();

        UriDiagnostic? input = LimitGuard.CheckInputCharacters(5, limits, DiagnosticDomain.Uri);
        UriDiagnostic? component = LimitGuard.CheckComponentCharacters(
            4,
            limits,
            DiagnosticDomain.Uri,
            UriComponent.Host,
            10);
        UriDiagnostic? output = LimitGuard.CheckOutputCharacters(6, limits, DiagnosticDomain.Uri);

        Assert.Multiple(() =>
        {
            Assert.That(input, Is.Null);
            Assert.That(component, Is.Null);
            Assert.That(output, Is.Null);
        });
    }

    [Test]
    public void InputGuardReportsOnlyCharactersBeyondLimit()
    {
        OperationLimits limits = CreateSmallLimits();

        UriDiagnostic? diagnostic = LimitGuard.CheckInputCharacters(8, limits, DiagnosticDomain.Iri);

        AssertLimitDiagnostic(
            diagnostic,
            DiagnosticDomain.Iri,
            UriComponent.Input,
            5,
            3,
            LimitGuard.InputLimitMessage);
    }

    [Test]
    public void ComponentGuardUsesAbsoluteInputOffset()
    {
        OperationLimits limits = CreateSmallLimits();

        UriDiagnostic? diagnostic = LimitGuard.CheckComponentCharacters(
            7,
            limits,
            DiagnosticDomain.Uri,
            UriComponent.Host,
            10);

        AssertLimitDiagnostic(
            diagnostic,
            DiagnosticDomain.Uri,
            UriComponent.Host,
            14,
            3,
            LimitGuard.ComponentLimitMessage);
    }

    [Test]
    public void OutputGuardReportsOnlyCharactersBeyondLimit()
    {
        OperationLimits limits = CreateSmallLimits();

        UriDiagnostic? diagnostic = LimitGuard.CheckOutputCharacters(
            9,
            limits,
            DiagnosticDomain.UriTemplate);

        AssertLimitDiagnostic(
            diagnostic,
            DiagnosticDomain.UriTemplate,
            UriComponent.Output,
            6,
            3,
            LimitGuard.OutputLimitMessage);
    }

    [Test]
    public void CollectionGuardUsesTheSuppliedLimitAndLocation()
    {
        UriDiagnostic? accepted = LimitGuard.CheckCollectionItems(
            2,
            2,
            DiagnosticDomain.Uri,
            UriComponent.Query,
            20);
        UriDiagnostic? rejected = LimitGuard.CheckCollectionItems(
            3,
            2,
            DiagnosticDomain.Uri,
            UriComponent.Query,
            20);

        Assert.That(accepted, Is.Null);
        AssertLimitDiagnostic(
            rejected,
            DiagnosticDomain.Uri,
            UriComponent.Query,
            20,
            0,
            LimitGuard.CollectionLimitMessage);
    }

    [Test]
    public void SpecializedCollectionGuardsUseTheirConfiguredLimitsAndComponents()
    {
        OperationLimits limits = CreateSmallLimits();

        UriDiagnostic? path = LimitGuard.CheckPathSegments(3, limits, DiagnosticDomain.Uri, 1);
        UriDiagnostic? query = LimitGuard.CheckQueryItems(4, limits, DiagnosticDomain.Iri, 2);
        UriDiagnostic? expressions = LimitGuard.CheckTemplateExpressions(5, limits, 3);
        UriDiagnostic? variables = LimitGuard.CheckTemplateVariables(6, limits, 4);
        UriDiagnostic? members = LimitGuard.CheckTemplateCompositeMembers(7, limits, 5);
        UriDiagnostic? links = LimitGuard.CheckWebLinks(8, limits, 6);
        UriDiagnostic? parameters = LimitGuard.CheckWebLinkParameters(9, limits, 7);

        Assert.Multiple(() =>
        {
            AssertCollectionContext(path, DiagnosticDomain.Uri, UriComponent.Path, 1);
            AssertCollectionContext(query, DiagnosticDomain.Iri, UriComponent.Query, 2);
            AssertCollectionContext(expressions, DiagnosticDomain.UriTemplate, UriComponent.TemplateExpression, 3);
            AssertCollectionContext(variables, DiagnosticDomain.UriTemplate, UriComponent.TemplateVariable, 4);
            AssertCollectionContext(members, DiagnosticDomain.UriTemplate, UriComponent.TemplateVariable, 5);
            AssertCollectionContext(links, DiagnosticDomain.WebLink, UriComponent.WebLink, 6);
            AssertCollectionContext(parameters, DiagnosticDomain.WebLink, UriComponent.LinkParameter, 7);
        });
    }

    [Test]
    public void SpecializedCollectionGuardsAcceptCountsAtConfiguredLimits()
    {
        OperationLimits limits = CreateSmallLimits();

        Assert.Multiple(() =>
        {
            Assert.That(LimitGuard.CheckPathSegments(2, limits, DiagnosticDomain.Uri, 1), Is.Null);
            Assert.That(LimitGuard.CheckQueryItems(3, limits, DiagnosticDomain.Uri, 2), Is.Null);
            Assert.That(LimitGuard.CheckTemplateExpressions(4, limits, 3), Is.Null);
            Assert.That(LimitGuard.CheckTemplateVariables(5, limits, 4), Is.Null);
            Assert.That(LimitGuard.CheckTemplateCompositeMembers(6, limits, 5), Is.Null);
            Assert.That(LimitGuard.CheckWebLinks(7, limits, 6), Is.Null);
            Assert.That(LimitGuard.CheckWebLinkParameters(8, limits, 7), Is.Null);
        });
    }

    [Test]
    public void NestingGuardReportsTheOffendingInputUnit()
    {
        OperationLimits limits = CreateSmallLimits();

        UriDiagnostic? accepted = LimitGuard.CheckNestingDepth(
            10,
            limits,
            DiagnosticDomain.UriTemplate,
            UriComponent.TemplateExpression,
            30);
        UriDiagnostic? rejected = LimitGuard.CheckNestingDepth(
            11,
            limits,
            DiagnosticDomain.UriTemplate,
            UriComponent.TemplateExpression,
            31);

        Assert.That(accepted, Is.Null);
        AssertLimitDiagnostic(
            rejected,
            DiagnosticDomain.UriTemplate,
            UriComponent.TemplateExpression,
            31,
            1,
            LimitGuard.NestingLimitMessage);
    }

    [Test]
    public void NegativeConfiguredLimitProducesDeterministicEmptyRange()
    {
        OperationLimits limits = CreateSmallLimits();
        limits.MaxInputCharacters = -1;

        UriDiagnostic? diagnostic = LimitGuard.CheckInputCharacters(0, limits, DiagnosticDomain.Uri);

        AssertLimitDiagnostic(
            diagnostic,
            DiagnosticDomain.Uri,
            UriComponent.Input,
            0,
            0,
            LimitGuard.InputLimitMessage);
    }

    private static OperationLimits CreateSmallLimits()
    {
        OperationLimits limits = new OperationLimits();
        limits.MaxInputCharacters = 5;
        limits.MaxComponentCharacters = 4;
        limits.MaxPathSegments = 2;
        limits.MaxQueryItems = 3;
        limits.MaxTemplateExpressions = 4;
        limits.MaxTemplateVariables = 5;
        limits.MaxTemplateCompositeMembers = 6;
        limits.MaxWebLinks = 7;
        limits.MaxWebLinkParameters = 8;
        limits.MaxNestingDepth = 10;
        limits.MaxOutputCharacters = 6;
        return limits;
    }

    private static void AssertCollectionContext(
        UriDiagnostic? diagnostic,
        DiagnosticDomain domain,
        UriComponent component,
        int offset)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Domain, Is.EqualTo(domain));
        Assert.That(diagnostic.Component, Is.EqualTo(component));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.Range.Length, Is.Zero);
        Assert.That(diagnostic.Message, Is.EqualTo(LimitGuard.CollectionLimitMessage));
    }

    private static void AssertLimitDiagnostic(
        UriDiagnostic? diagnostic,
        DiagnosticDomain domain,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain, Is.EqualTo(domain));
            Assert.That(diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
            Assert.That(diagnostic.Message, Is.EqualTo(message));
        });
    }
}
