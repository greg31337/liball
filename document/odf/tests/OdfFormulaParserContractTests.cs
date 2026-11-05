namespace odf.Tests;

public class OdfFormulaParserContractTests
{
    [Test]
    public void ExposesEveryTokenKindWithExactSourceLocations()
    {
        string source = "of:= SUM (12.5E-2; 25%; \"a\"\"b\"; TRUE; #N/A; "
            + "[.A1]; [.$B$2:.C3]; ['file.ods'#Sheet1.D4]; $$Tax; "
            + "'Row Label'; {1;2|3;4}) + 1";
        IOdfFormulaParser parser = new OdfFormulaParser();

        OdfFormula formula = parser.ParseFormula(source, 64);
        OdfFormula malformed = parser.ParseFormula("=@", 64);
        HashSet<OdfFormulaTokenKind> kinds = new HashSet<OdfFormulaTokenKind>();
        AddTokenKinds(kinds, formula.Tokens);
        AddTokenKinds(kinds, malformed.Tokens);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(kinds, Is.EquivalentTo(
                Enum.GetValues<OdfFormulaTokenKind>()));
            Assert.That(formula.Tokens[0].Kind,
                Is.EqualTo(OdfFormulaTokenKind.Prefix));
            Assert.That(formula.Tokens[0].Text, Is.EqualTo("of:="));
            Assert.That(formula.Tokens[^1].Kind,
                Is.EqualTo(OdfFormulaTokenKind.End));
            Assert.That(formula.Tokens[^1].Start, Is.EqualTo(source.Length));
            Assert.That(formula.Tokens[^1].Length, Is.Zero);
        });

        for (int index = 0; index < formula.Tokens.Length - 1; index++)
        {
            OdfFormulaToken token = formula.Tokens[index];
            Assert.That(token.Start, Is.GreaterThanOrEqualTo(0));
            Assert.That(token.Length, Is.EqualTo(token.Text.Length));
            Assert.That(source.Substring(token.Start, token.Length),
                Is.EqualTo(token.Text));
        }
    }

    [Test]
    public void ProducesEverySyntaxNodeKindAcrossGrammarProductions()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        string[] sources =
        [
            "=1",
            "=#N/A",
            "=Tax",
            "=[.A1]",
            "=[.A1:.B2]",
            "=['book.ods'#Sheet1.A1]",
            "=-1%",
            "=1+2",
            "=SUM(1)",
            "={1;2|3;4}",
            "=[.A1]![.B2]",
            "=[.A1]~[.B2]",
            "=@"
        ];
        HashSet<OdfFormulaNodeKind> kinds = new HashSet<OdfFormulaNodeKind>();

        for (int index = 0; index < sources.Length; index++)
        {
            OdfFormula formula = parser.ParseFormula(sources[index], 64);
            CollectNodeKinds(formula.Root!, kinds);
        }

        Assert.That(kinds, Is.EquivalentTo(
            Enum.GetValues<OdfFormulaNodeKind>()));
    }

    [Test]
    public void AppliesTheCompleteOperatorPrecedenceHierarchy()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        OdfFormula formula = parser.ParseFormula(
            "=[.A1]:[.B2]![.B1]~[.C1]^(2)%*3+4&\"x\"=5",
            64);
        OdfFormulaNode root = formula.Root!;

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(root.Text, Is.EqualTo("="));
            Assert.That(root.Children[0].Text, Is.EqualTo("&"));
            Assert.That(root.Children[0].Children[0].Text, Is.EqualTo("+"));
            Assert.That(root.Children[0].Children[0].Children[0].Text,
                Is.EqualTo("*"));
            Assert.That(root.Children[0].Children[0].Children[0]
                .Children[0].Text, Is.EqualTo("^"));
            Assert.That(root.Children[0].Children[0].Children[0]
                .Children[0].Children[0].Text, Is.EqualTo("~"));
            Assert.That(root.Children[0].Children[0].Children[0]
                .Children[0].Children[0].Children[0].Text, Is.EqualTo("!"));
            Assert.That(root.Children[0].Children[0].Children[0]
                .Children[0].Children[0].Children[0].Children[0].Text,
                Is.EqualTo(":"));
            Assert.That(root.Children[0].Children[0].Children[0]
                .Children[0].Children[1].Text, Is.EqualTo("%"));
        });
    }

    [TestCase("=1<>2", "<>")]
    [TestCase("=1<2", "<")]
    [TestCase("=1>2", ">")]
    [TestCase("=1<=2", "<=")]
    [TestCase("=1>=2", ">=")]
    [TestCase("=5-2", "-")]
    [TestCase("=6/3", "/")]
    [TestCase("=+1", "+")]
    public void ParsesEveryRemainingOperatorProduction(
        string source,
        string rootOperator)
    {
        IOdfFormulaParser parser = new OdfFormulaParser();

        OdfFormula formula = parser.ParseFormula(source, 32);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(formula.Root!.Text, Is.EqualTo(rootOperator));
        });
    }

    [Test]
    public void ResolvesEveryReferenceFormThroughThePublicParser()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        OdfFormula absoluteRange = parser.ParseFormula(
            "=[$'Sheet 1'.$A$2:Sheet2.C4]",
            64);
        OdfFormula wholeColumns = parser.ParseFormula("=[.A:.C]", 64);
        OdfFormula wholeRows = parser.ParseFormula("=[.$1:.$10]", 64);
        OdfFormula external = parser.ParseFormula(
            "=['file.ods'#$'Data'.$A1:.$B$2]",
            64);
        OdfFormula globalName = parser.ParseFormula("=$$'Gross Profit'", 64);
        OdfFormula localName = parser.ParseFormula("='Sheet 1'.$$Local", 64);
        OdfFormula externalName = parser.ParseFormula(
            "='book.ods'#$$Remote",
            64);
        OdfFormula label = parser.ParseFormula("='Row Label'", 64);
        OdfFormula invalid = parser.ParseFormula("=[#REF!]", 64);
        OdfFormulaReference range = absoluteRange.Root!.Reference!;

        Assert.Multiple(() =>
        {
            Assert.That(range.HasEnd, Is.True);
            Assert.That(range.IsThreeDimensional, Is.True);
            Assert.That(range.Start.SheetName, Is.EqualTo("Sheet 1"));
            Assert.That(range.Start.SheetAbsolute, Is.True);
            Assert.That(range.Start.ColumnAbsolute, Is.True);
            Assert.That(range.Start.RowAbsolute, Is.True);
            Assert.That(range.End.SheetName, Is.EqualTo("Sheet2"));
            Assert.That(wholeColumns.Root!.Reference!.Start.HasRow, Is.False);
            Assert.That(wholeRows.Root!.Reference!.Start.HasColumn, Is.False);
            Assert.That(external.Root!.Reference!.Source,
                Is.EqualTo("file.ods"));
            Assert.That(external.Root.Reference.End.EffectiveSheetName,
                Is.EqualTo("Data"));
            Assert.That(globalName.Root!.Reference!.Name,
                Is.EqualTo("Gross Profit"));
            Assert.That(globalName.Root.Reference.IsExplicitName, Is.True);
            Assert.That(localName.Root!.Reference!.NameSheet,
                Is.EqualTo("Sheet 1"));
            Assert.That(externalName.Root!.Reference!.HasSource, Is.True);
            Assert.That(externalName.Root.Reference.Name, Is.EqualTo("Remote"));
            Assert.That(label.Root!.Reference!.IsLabel, Is.True);
            Assert.That(invalid.Root!.Reference!.IsReferenceError, Is.True);
        });
    }

    [Test]
    public void PreservesArraysNamespacesVendorFunctionsAndLexicalText()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        string source = "acme:==  com.microsoft.cubemember "
            + "( { 1 ; \"a\"\"b\" | TRUE ; #N/A } ; org.vendor.foo( ; 2 ; ) ) ";

        OdfFormula formula = parser.ParseFormula(source, 64);
        string preserved = parser.FormatFormulaPreserved(formula);
        string canonical = parser.FormatFormulaCanonical(formula);
        OdfFormula reparsed = parser.ParseFormula(canonical, 64);

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(formula.NamespacePrefix, Is.EqualTo("acme"));
            Assert.That(formula.ForcedRecalculation, Is.True);
            Assert.That(formula.Root!.Text,
                Is.EqualTo("com.microsoft.cubemember"));
            Assert.That(formula.Root.Children[0].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Array));
            Assert.That(formula.Root.Children[0].Separators,
                Is.EqualTo(new[] { ";", "|", ";" }));
            Assert.That(formula.Root.Children[1].Children[0].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Unknown));
            Assert.That(formula.Root.Children[1].Children[2].Kind,
                Is.EqualTo(OdfFormulaNodeKind.Unknown));
            Assert.That(preserved, Is.EqualTo(source));
            Assert.That(canonical, Is.EqualTo(
                "acme:==COM.MICROSOFT.CUBEMEMBER({1;\"a\"\"b\"|TRUE;#N/A};"
                + "ORG.VENDOR.FOO(;2;))"));
            Assert.That(reparsed.IsValid, Is.True);
            Assert.That(parser.FormatFormulaCanonical(reparsed),
                Is.EqualTo(canonical));
        });
    }

    [Test]
    public void ReportsMalformedGrammarAndDepthAtExactLocations()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        OdfFormula missingSeparator = parser.ParseFormula("=SUM(1 2)", 32);
        OdfFormula missingParenthesis = parser.ParseFormula("=SUM(1", 32);
        OdfFormula badArray = parser.ParseFormula("={1;}", 32);
        OdfFormula badLexeme = parser.ParseFormula("=\"unterminated", 32);
        OdfFormula depth = parser.ParseFormula("=((((1))))", 3);
        OdfDiagnostic separatorDiagnostic = missingSeparator.Diagnostics[0];

        Assert.Multiple(() =>
        {
            Assert.That(missingSeparator.IsValid, Is.False);
            Assert.That(separatorDiagnostic.Code,
                Is.EqualTo(OdfDiagnosticCodes.InvalidFormula));
            Assert.That(separatorDiagnostic.Location.TextOffset, Is.EqualTo(7));
            Assert.That(separatorDiagnostic.Location.TextLength, Is.EqualTo(1));
            Assert.That(missingParenthesis.Diagnostics.Any(item =>
                item.Code == OdfDiagnosticCodes.InvalidFormula), Is.True);
            Assert.That(badArray.Diagnostics.Any(item =>
                item.Location.TextOffset == 4), Is.True);
            Assert.That(badLexeme.Tokens.Any(item =>
                item.Kind == OdfFormulaTokenKind.Unknown), Is.True);
            Assert.That(depth.Diagnostics.Count(item =>
                item.Code == OdfDiagnosticCodes.FormulaDepthLimitExceeded),
                Is.EqualTo(1));
            Assert.That(depth.Diagnostics.Single(item =>
                item.Code == OdfDiagnosticCodes.FormulaDepthLimitExceeded)
                .Severity, Is.EqualTo(OdfValidationSeverity.Fatal));
            Assert.That(
                () => parser.ParseFormula(null!, 32),
                Throws.TypeOf<OdfFormulaException>());
            Assert.That(
                () => parser.ParseFormula("=1", 0),
                Throws.TypeOf<OdfFormulaException>());
        });
    }

    [Test]
    public void FormatsAstEditsAndRejectsInvalidFormatterInput()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        OdfFormula edited = parser.ParseFormula("of:= 1 + 2 * ( 3 + 4 )", 32);
        edited.Root!.Text = "-";

        string preserved = parser.FormatFormulaPreserved(edited);
        string canonical = parser.FormatFormulaCanonical(edited);

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Is.EqualTo("of:=1-2*(3+4)"));
            Assert.That(canonical, Is.EqualTo("of:=1-2*(3+4)"));
            Assert.That(
                () => parser.FormatFormulaPreserved(null!),
                Throws.TypeOf<OdfFormulaException>());
            Assert.That(
                () => parser.FormatFormulaCanonical(new OdfFormula()),
                Throws.TypeOf<OdfFormulaException>());
        });
    }

    [Test]
    public void RebasesEveryReferenceEditThroughThePublicContract()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();

        string copied = parser.RebaseFormulaCopy(
            "of:=[.A1]+[.$B2]+[.C$3]+[.$D$4]+['Other'.E5]",
            "Sheet1",
            2,
            1);
        string moved = parser.RebaseFormulaMovedRange(
            "of:=[.A1]+[.B2]+[.$B$2]+['Other'.B2]",
            "Sheet1",
            "Sheet1",
            0,
            0,
            2,
            2,
            2,
            3);
        string insertedRows = parser.RebaseFormulaRows(
            "of:=[.A1:.C3]",
            "Sheet1",
            "Sheet1",
            1,
            0,
            2);
        string deletedRow = parser.RebaseFormulaRows(
            "of:=[.A2]",
            "Sheet1",
            "Sheet1",
            1,
            1,
            0);
        string insertedColumns = parser.RebaseFormulaColumns(
            insertedRows,
            "Sheet1",
            "Sheet1",
            1,
            0,
            1);
        string renamed = parser.RebaseFormulaSheetRename(
            "of:=['Old Sheet'.A1]+'Old Sheet'.$$Local"
            + "+['book.ods'#'Old Sheet'.A1]",
            "Calc",
            "Old Sheet",
            "New Sheet");
        string deleted = parser.RebaseFormulaSheetDeletion(
            renamed,
            "Calc",
            "New Sheet");
        string stable = "of:=[First.A1:Third.B2]";
        string insertedSheet = parser.PreserveFormulaSheetInsertion(
            stable,
            "Second",
            1);
        string reorderedSheet = parser.PreserveFormulaSheetReorder(
            insertedSheet,
            "Third",
            0);

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.EqualTo(
                "of:=[.B3]+[.$B4]+[.D$3]+[.$D$4]+['Other'.E5]"));
            Assert.That(moved, Is.EqualTo(
                "of:=[.D3]+[.E4]+[.$E$4]+['Other'.B2]"));
            Assert.That(insertedRows, Is.EqualTo("of:=[.A1:.C5]"));
            Assert.That(deletedRow, Is.EqualTo("of:=#REF!"));
            Assert.That(insertedColumns, Is.EqualTo("of:=[.A1:.D5]"));
            Assert.That(renamed, Is.EqualTo(
                "of:=['New Sheet'.A1]+'New Sheet'.$$Local"
                + "+['book.ods'#'Old Sheet'.A1]"));
            Assert.That(deleted, Is.EqualTo(
                "of:=#REF!+#REF!+['book.ods'#'Old Sheet'.A1]"));
            Assert.That(insertedSheet, Is.EqualTo(stable));
            Assert.That(reorderedSheet, Is.EqualTo(stable));
        });
    }

    private static void AddTokenKinds(
        HashSet<OdfFormulaTokenKind> kinds,
        OdfFormulaToken[] tokens)
    {
        for (int index = 0; index < tokens.Length; index++)
        {
            kinds.Add(tokens[index].Kind);
        }
    }

    private static void CollectNodeKinds(
        OdfFormulaNode node,
        HashSet<OdfFormulaNodeKind> kinds)
    {
        kinds.Add(node.Kind);
        for (int index = 0; index < node.Children.Length; index++)
        {
            CollectNodeKinds(node.Children[index], kinds);
        }
    }
}
