namespace odf.Tests;

public class FormulaParserTests
{
    [Test]
    public void AppliesOpenFormulaOperatorPrecedence()
    {
        OdfFormula formula = FormulaParser.Parse("=1+2*3^4", 32);
        OdfFormulaNode root = formula.Root!;

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(root.Kind, Is.EqualTo(OdfFormulaNodeKind.BinaryOperation));
            Assert.That(root.Text, Is.EqualTo("+"));
            Assert.That(root.Children[0].Text, Is.EqualTo("1"));
            Assert.That(root.Children[1].Text, Is.EqualTo("*"));
            Assert.That(root.Children[1].Children[0].Text, Is.EqualTo("2"));
            Assert.That(root.Children[1].Children[1].Text, Is.EqualTo("^"));
            Assert.That(root.Children[1].Children[1].Children[0].Text,
                Is.EqualTo("3"));
            Assert.That(root.Children[1].Children[1].Children[1].Text,
                Is.EqualTo("4"));
            Assert.That(root.Start, Is.EqualTo(1));
            Assert.That(root.Length, Is.EqualTo(7));
        });
    }

    [Test]
    public void AppliesReferenceOperatorPrecedenceAndKinds()
    {
        OdfFormula formula = FormulaParser.Parse(
            "=[.A1]:[.B2]![.B1]~[.C1]",
            32);
        OdfFormulaNode root = formula.Root!;

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(root.Kind,
                Is.EqualTo(OdfFormulaNodeKind.ReferenceConcatenation));
            Assert.That(root.Text, Is.EqualTo("~"));
            Assert.That(root.Children[0].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Intersection));
            Assert.That(root.Children[0].Children[0].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Range));
            Assert.That(root.Children[0].Children[0].Children[0].Text,
                Is.EqualTo("[.A1]"));
            Assert.That(root.Children[1].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Reference));
        });
    }

    [Test]
    public void ParsesAutomaticIntersection()
    {
        OdfFormula formula = FormulaParser.Parse(
            "='Row Label'!!'Column Label'",
            32);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(formula.Root!.Kind,
                Is.EqualTo(OdfFormulaNodeKind.Intersection));
            Assert.That(formula.Root.Text, Is.EqualTo("!!"));
            Assert.That(formula.Root.Children[0].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Name));
            Assert.That(formula.Root.Children[1].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Name));
        });
    }

    [Test]
    public void PreservesNamespacePrefixForcedRecalculationAndVendorFunctions()
    {
        string source =
            "acme:==COM.MICROSOFT.CUBEMEMBER(1;SUM(2;3))";

        OdfFormula formula = FormulaParser.Parse(source, 32);
        OdfFormulaNode root = formula.Root!;

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(formula.Source, Is.EqualTo(source));
            Assert.That(formula.NamespacePrefix, Is.EqualTo("acme"));
            Assert.That(formula.ForcedRecalculation, Is.True);
            Assert.That(formula.Tokens[0].Text, Is.EqualTo("acme:=="));
            Assert.That(root.Kind, Is.EqualTo(OdfFormulaNodeKind.FunctionCall));
            Assert.That(root.Text, Is.EqualTo("COM.MICROSOFT.CUBEMEMBER"));
            Assert.That(root.Children, Has.Length.EqualTo(2));
            Assert.That(root.Children[1].Kind,
                Is.EqualTo(OdfFormulaNodeKind.FunctionCall));
            Assert.That(root.Children[1].Text, Is.EqualTo("SUM"));
            Assert.That(root.Start, Is.EqualTo(7));
            Assert.That(root.Length, Is.EqualTo(source.Length - 7));
        });
    }

    [Test]
    public void ParsesArraysAndLegitimateEmptyFunctionArguments()
    {
        OdfFormula array = FormulaParser.Parse("={1;2|3;4}", 32);
        OdfFormula function = FormulaParser.Parse("=SUM(;1;)", 32);

        Assert.Multiple(() =>
        {
            Assert.That(array.IsValid, Is.True);
            Assert.That(array.Root!.Kind, Is.EqualTo(OdfFormulaNodeKind.Array));
            Assert.That(array.Root.Children.Select(item => item.Text),
                Is.EqualTo(new[] { "1", "2", "3", "4" }));
            Assert.That(function.IsValid, Is.True);
            Assert.That(function.Root!.Kind,
                Is.EqualTo(OdfFormulaNodeKind.FunctionCall));
            Assert.That(function.Root.Children, Has.Length.EqualTo(3));
            Assert.That(function.Root.Children[0].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Unknown));
            Assert.That(function.Root.Children[1].Text, Is.EqualTo("1"));
            Assert.That(function.Root.Children[2].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Unknown));
        });
    }

    [Test]
    public void ProducesStructuredDiagnosticsAtExactSourceLocations()
    {
        OdfFormula formula = FormulaParser.Parse("=SUM(1 2)", 32);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.False);
            Assert.That(formula.Diagnostics, Is.Not.Empty);
            Assert.That(formula.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.InvalidFormula));
            Assert.That(formula.Diagnostics[0].Severity,
                Is.EqualTo(OdfValidationSeverity.Error));
            Assert.That(formula.Diagnostics[0].Location.TextOffset,
                Is.EqualTo(7));
            Assert.That(formula.Diagnostics[0].Location.TextLength,
                Is.EqualTo(1));
            Assert.That(formula.Diagnostics[0].SpecificationSection,
                Does.Contain("Formula"));
            Assert.That(formula.Diagnostics[0].IsRecoverable, Is.True);
        });
    }

    [TestCase("of:=")]
    [TestCase("=SUM(1")]
    [TestCase("=#BAD")]
    [TestCase("={1;}")]
    public void RejectsIncompleteOrMalformedExpressions(string source)
    {
        OdfFormula formula = FormulaParser.Parse(source, 32);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.False);
            Assert.That(formula.Diagnostics.Any(item =>
                item.Code == OdfDiagnosticCodes.InvalidFormula), Is.True);
        });
    }

    [Test]
    public void StopsAtTheConfiguredNestingLimit()
    {
        OdfFormula formula = FormulaParser.Parse("=((((1))))", 3);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.False);
            Assert.That(formula.Diagnostics.Count(item =>
                item.Code == OdfDiagnosticCodes.FormulaDepthLimitExceeded),
                Is.EqualTo(1));
            Assert.That(formula.Diagnostics.Single(item =>
                item.Code == OdfDiagnosticCodes.FormulaDepthLimitExceeded)
                .Severity, Is.EqualTo(OdfValidationSeverity.Fatal));
        });
    }

    [Test]
    public void RejectsInvalidParserInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => FormulaParser.Parse(null!, 32),
                Throws.TypeOf<OdfFormulaException>());
            Assert.That(
                () => FormulaParser.Parse("=1", 0),
                Throws.TypeOf<OdfFormulaException>());
        });
    }
}
