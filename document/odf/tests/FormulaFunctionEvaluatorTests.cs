namespace odf.Tests;

public class FormulaFunctionEvaluatorTests
{
    [Test]
    public void BindsConformanceAndVersionMetadataToRecognizedFunctions()
    {
        OdfFormula database = FormulaParser.Parse(
            "=DSUM(Database;\"Sales\";Criteria)",
            32);
        OdfFormula information = FormulaParser.Parse("=ISERROR(1)", 32);
        OdfFormula external = FormulaParser.Parse("=DDE(\"s\";\"t\";\"i\")", 32);
        OdfFormula uncatalogued = FormulaParser.Parse(
            "=NOT_A_FUNCTION(1;2)",
            32);

        Assert.Multiple(() =>
        {
            Assert.That(FormulaFunctionCatalog.Functions, Has.Length.EqualTo(393));
            Assert.That(database.Root!.Function, Is.Not.Null);
            Assert.That(database.Root.Function!.Name,
                Is.EqualTo(OdfFormulaNames.DSUM));
            Assert.That(database.Root.Function.Group, Is.EqualTo("Database"));
            Assert.That(database.Root.Function.MinimumArguments, Is.EqualTo(3));
            Assert.That(database.Root.Function.MaximumArguments, Is.EqualTo(3));
            Assert.That(database.Root.Function.Conformance,
                Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(database.Root.Function.IntroducedVersion,
                Is.EqualTo(OdfVersion.Version12));
            Assert.That(information.Root!.Function!.Conformance,
                Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(external.Root!.Function!.RequiresExternalAccess, Is.True);
            Assert.That(external.Root.Function.IsVolatile, Is.True);
            Assert.That(uncatalogued.Root!.Function, Is.Null);
        });
    }

    [Test]
    public void EvaluatesEveryDatabaseAggregateOverMatchingRows()
    {
        OdfFormulaResult database = CreateSalesDatabase();
        OdfFormulaResult field = FormulaValueModel.CreateText("Sales");
        OdfFormulaResult criteria = CreateArray(
            [
                "Region", "Sales",
                "West", ">=150"
            ],
            2,
            2);
        FormulaFunctionContext context = new FormulaFunctionContext();

        OdfFormulaResult average = EvaluateDatabase(
            OdfFormulaNames.DAVERAGE, database, field, criteria, context);
        OdfFormulaResult count = EvaluateDatabase(
            OdfFormulaNames.DCOUNT, database, field, criteria, context);
        OdfFormulaResult countAll = EvaluateDatabase(
            OdfFormulaNames.DCOUNTA, database, field, criteria, context);
        OdfFormulaResult records = EvaluateDatabase(
            OdfFormulaNames.DCOUNT,
            database,
            FormulaValueModel.CreateEmpty(),
            criteria,
            context);
        OdfFormulaResult maximum = EvaluateDatabase(
            OdfFormulaNames.DMAX, database, field, criteria, context);
        OdfFormulaResult minimum = EvaluateDatabase(
            OdfFormulaNames.DMIN, database, field, criteria, context);
        OdfFormulaResult product = EvaluateDatabase(
            OdfFormulaNames.DPRODUCT, database, field, criteria, context);
        OdfFormulaResult sampleDeviation = EvaluateDatabase(
            OdfFormulaNames.DSTDEV, database, field, criteria, context);
        OdfFormulaResult populationDeviation = EvaluateDatabase(
            OdfFormulaNames.DSTDEVP, database, field, criteria, context);
        OdfFormulaResult sum = EvaluateDatabase(
            OdfFormulaNames.DSUM, database, field, criteria, context);
        OdfFormulaResult sampleVariance = EvaluateDatabase(
            OdfFormulaNames.DVAR, database, field, criteria, context);
        OdfFormulaResult populationVariance = EvaluateDatabase(
            OdfFormulaNames.DVARP, database, field, criteria, context);

        Assert.Multiple(() =>
        {
            Assert.That(average.NumericValue, Is.EqualTo(350));
            Assert.That(count.NumericValue, Is.EqualTo(2));
            Assert.That(countAll.NumericValue, Is.EqualTo(2));
            Assert.That(records.NumericValue, Is.EqualTo(2));
            Assert.That(maximum.NumericValue, Is.EqualTo(400));
            Assert.That(minimum.NumericValue, Is.EqualTo(300));
            Assert.That(product.NumericValue, Is.EqualTo(120000));
            Assert.That(sampleDeviation.NumericValue,
                Is.EqualTo(global::System.Math.Sqrt(5000)).Within(0.0000001));
            Assert.That(populationDeviation.NumericValue, Is.EqualTo(50));
            Assert.That(sum.NumericValue, Is.EqualTo(700));
            Assert.That(sampleVariance.NumericValue, Is.EqualTo(5000));
            Assert.That(populationVariance.NumericValue, Is.EqualTo(2500));
        });
    }

    [Test]
    public void AppliesDatabaseCriteriaRowsWildcardsAndDgetCardinality()
    {
        OdfFormulaResult database = CreateSalesDatabase();
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult alternatives = CreateArray(
            [
                "Name", "Region",
                "A*", "",
                "", "East"
            ],
            3,
            2);
        OdfFormulaResult selected = EvaluateDatabase(
            OdfFormulaNames.DCOUNT,
            database,
            FormulaValueModel.CreateNumber(3),
            alternatives,
            context);
        OdfFormulaResult one = EvaluateDatabase(
            OdfFormulaNames.DGET,
            database,
            FormulaValueModel.CreateText("Sales"),
            CreateArray(["Name", "=Alice"], 2, 1),
            context);
        OdfFormulaResult many = EvaluateDatabase(
            OdfFormulaNames.DGET,
            database,
            FormulaValueModel.CreateText("Sales"),
            CreateArray(["Region", "West"], 2, 1),
            context);
        OdfFormulaResult none = EvaluateDatabase(
            OdfFormulaNames.DGET,
            database,
            FormulaValueModel.CreateText("Sales"),
            CreateArray(["Name", "Nobody"], 2, 1),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(selected.NumericValue, Is.EqualTo(2));
            Assert.That(one.NumericValue, Is.EqualTo(100));
            Assert.That(many.Error, Is.EqualTo(OdfFormulaError.Number));
            Assert.That(none.Error, Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void RejectsMalformedDatabaseInputsAndUnknownFields()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult scalarDatabase = FormulaValueModel.CreateNumber(1);
        OdfFormulaResult badDatabase = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.DSUM,
            [
                scalarDatabase,
                FormulaValueModel.CreateText("Sales"),
                CreateArray(["Sales", ">0"], 2, 1)
            ],
            context);
        OdfFormulaResult badField = EvaluateDatabase(
            OdfFormulaNames.DSUM,
            CreateSalesDatabase(),
            FormulaValueModel.CreateText("Missing"),
            CreateArray(["Sales", ">0"], 2, 1),
            context);
        OdfFormulaResult badCriteriaHeader = EvaluateDatabase(
            OdfFormulaNames.DSUM,
            CreateSalesDatabase(),
            FormulaValueModel.CreateText("Sales"),
            CreateArray(["Missing", ">0"], 2, 1),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(badDatabase.Error, Is.EqualTo(OdfFormulaError.Value));
            Assert.That(badField.Error, Is.EqualTo(OdfFormulaError.Value));
            Assert.That(badCriteriaHeader.Error,
                Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void EvaluatesInformationPredicatesAndConversions()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult notAvailable = FormulaValueModel.CreateError(
            OdfFormulaError.NotAvailable);
        OdfFormulaResult array = FormulaValueModel.CreateArray(
            [FormulaValueModel.CreateNumber(1)],
            1,
            1);
        OdfFormulaReference referenceData = new OdfFormulaReference();
        OdfFormulaResult reference = FormulaValueModel.CreateReference(
            referenceData,
            [FormulaValueModel.CreateNumber(2)],
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(EvaluateOne(OdfFormulaNames.ISBLANK,
                FormulaValueModel.CreateEmpty(), context).BooleanValue, Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISERR,
                notAvailable, context).BooleanValue, Is.False);
            Assert.That(EvaluateOne(OdfFormulaNames.ISERROR,
                notAvailable, context).BooleanValue, Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISNA,
                notAvailable, context).BooleanValue, Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISLOGICAL,
                FormulaValueModel.CreateBoolean(false), context).BooleanValue,
                Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISNUMBER,
                FormulaValueModel.CreateNumber(1), context).BooleanValue,
                Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISTEXT,
                FormulaValueModel.CreateText("x"), context).BooleanValue,
                Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISNONTEXT,
                FormulaValueModel.CreateNumber(1), context).BooleanValue,
                Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISREF,
                reference, context).BooleanValue, Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISEVEN,
                FormulaValueModel.CreateNumber(-4.9), context).BooleanValue,
                Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.ISODD,
                FormulaValueModel.CreateNumber(-3.9), context).BooleanValue,
                Is.True);
            Assert.That(EvaluateOne(OdfFormulaNames.N,
                FormulaValueModel.CreateBoolean(true), context).NumericValue,
                Is.EqualTo(1));
            Assert.That(EvaluateOne(OdfFormulaNames.VALUE,
                FormulaValueModel.CreateText("7.5"), context).NumericValue,
                Is.EqualTo(7.5));
            Assert.That(EvaluateOne(OdfFormulaNames.TYPE,
                array, context).NumericValue, Is.EqualTo(64));
            Assert.That(EvaluateOne(OdfFormulaNames.ERROR_TYPE,
                notAvailable, context).NumericValue, Is.EqualTo(7));
        });
    }

    [Test]
    public void EvaluatesInformationCountsAndCriteriaAcrossReferences()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaReference referenceData = new OdfFormulaReference();
        OdfFormulaResult reference = FormulaValueModel.CreateReference(
            referenceData,
            [
                FormulaValueModel.CreateNumber(1),
                FormulaValueModel.CreateEmpty(),
                FormulaValueModel.CreateText("west"),
                FormulaValueModel.CreateNumber(4)
            ],
            2,
            2);
        OdfFormulaResult second = FormulaValueModel.CreateReference(
            referenceData,
            [
                FormulaValueModel.CreateText("yes"),
                FormulaValueModel.CreateText("yes"),
                FormulaValueModel.CreateText("no"),
                FormulaValueModel.CreateText("yes")
            ],
            2,
            2);

        OdfFormulaResult count = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.COUNT,
            [reference, FormulaValueModel.CreateNumber(8)],
            context);
        OdfFormulaResult countAll = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.COUNTA,
            [reference],
            context);
        OdfFormulaResult blank = EvaluateOne(
            OdfFormulaNames.COUNTBLANK, reference, context);
        OdfFormulaResult countIf = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.COUNTIF,
            [reference, FormulaValueModel.CreateText(">2")],
            context);
        OdfFormulaResult countIfs = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.COUNTIFS,
            [
                reference, FormulaValueModel.CreateText(">0"),
                second, FormulaValueModel.CreateText("yes")
            ],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(count.NumericValue, Is.EqualTo(3));
            Assert.That(countAll.NumericValue, Is.EqualTo(3));
            Assert.That(blank.NumericValue, Is.EqualTo(1));
            Assert.That(countIf.NumericValue, Is.EqualTo(1));
            Assert.That(countIfs.NumericValue, Is.EqualTo(2));
        });
    }

    [Test]
    public void EvaluatesReferenceDimensionsCoordinatesAreasAndSheets()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.CurrentRowNumber = 8;
        context.CurrentColumnNumber = 5;
        context.CurrentSheetNumber = 2;
        context.SheetNames = ["Summary", "Data"];
        context.SheetNumbers = [1, 2];
        OdfFormulaReference referenceData = new OdfFormulaReference();
        referenceData.Start.RowNumber = 4;
        referenceData.Start.ColumnNumber = 2;
        OdfFormulaResult reference = FormulaValueModel.CreateReference(
            referenceData,
            [
                FormulaValueModel.CreateEmpty(),
                FormulaValueModel.CreateEmpty(),
                FormulaValueModel.CreateEmpty(),
                FormulaValueModel.CreateEmpty(),
                FormulaValueModel.CreateEmpty(),
                FormulaValueModel.CreateEmpty()
            ],
            2,
            3);
        OdfFormulaReference union = new OdfFormulaReference();
        union.Kind = OdfFormulaNodeKind.ReferenceConcatenation;
        union.Items =
        [
            new OdfFormulaReference(),
            new OdfFormulaReference(),
            new OdfFormulaReference()
        ];
        OdfFormulaResult unionValue = FormulaValueModel.CreateReference(
            union,
            [],
            0,
            0);

        OdfFormulaResult areas = EvaluateOne(
            OdfFormulaNames.AREAS, unionValue, context);
        OdfFormulaResult columns = EvaluateOne(
            OdfFormulaNames.COLUMNS, reference, context);
        OdfFormulaResult rows = EvaluateOne(
            OdfFormulaNames.ROWS, reference, context);
        OdfFormulaResult column = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.COLUMN, [reference], context);
        OdfFormulaResult row = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.ROW, [reference], context);
        OdfFormulaResult currentColumn = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.COLUMN, [], context);
        OdfFormulaResult sheet = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.SHEET,
            [FormulaValueModel.CreateText("Data")],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(areas.NumericValue, Is.EqualTo(3));
            Assert.That(columns.NumericValue, Is.EqualTo(3));
            Assert.That(rows.NumericValue, Is.EqualTo(2));
            Assert.That(column.Kind, Is.EqualTo(OdfValueKind.Array));
            Assert.That(column.Values.Select(item => item.NumericValue),
                Is.EqualTo(new double[] { 2, 3, 4 }));
            Assert.That(row.Values.Select(item => item.NumericValue),
                Is.EqualTo(new double[] { 4, 5 }));
            Assert.That(currentColumn.NumericValue, Is.EqualTo(5));
            Assert.That(sheet.NumericValue, Is.EqualTo(2));
        });
    }

    [Test]
    public void ParsesNumbersWithCallerSuppliedSeparatorsAndPercentages()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult explicitSeparators = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.NUMBERVALUE,
            [
                FormulaValueModel.CreateText(" 1.234,5%% "),
                FormulaValueModel.CreateText(","),
                FormulaValueModel.CreateText(".")
            ],
            context);
        context.DecimalSeparator = ",";
        context.GroupSeparator = ".";
        OdfFormulaResult defaults = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.NUMBERVALUE,
            [FormulaValueModel.CreateText("1.234,5")],
            context);
        OdfFormulaResult invalid = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.NUMBERVALUE,
            [
                FormulaValueModel.CreateText("1"),
                FormulaValueModel.CreateText("..")
            ],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(explicitSeparators.NumericValue,
                Is.EqualTo(0.12345).Within(0.0000001));
            Assert.That(defaults.NumericValue, Is.EqualTo(1234.5));
            Assert.That(invalid.Error, Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void UsesOnlyCallerSuppliedContextForInformationAndExternalAccess()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.CurrentAddress = "$Sheet1.$C$4";
        context.FileName = "file:///documents/report.ods";
        context.Directory = "/documents";
        context.OperatingSystem = "TestOS";
        context.Release = "1.4";
        context.SheetCount = 3;
        context.ReferenceSheetCount = 2;
        context.ReferencedFormula = "of:=SUM([.A1:.A2])";
        OdfFormulaReference referenceData = new OdfFormulaReference();
        OdfFormulaResult reference = FormulaValueModel.CreateReference(
            referenceData,
            [FormulaValueModel.CreateNumber(42)],
            1,
            1);

        OdfFormulaResult formula = EvaluateOne(
            OdfFormulaNames.FORMULA, reference, context);
        OdfFormulaResult isFormula = EvaluateOne(
            OdfFormulaNames.ISFORMULA, reference, context);
        OdfFormulaResult address = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.CELL,
            [FormulaValueModel.CreateText("address"), reference],
            context);
        OdfFormulaResult contents = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.CELL,
            [FormulaValueModel.CreateText("contents"), reference],
            context);
        OdfFormulaResult directory = EvaluateOne(
            OdfFormulaNames.INFO,
            FormulaValueModel.CreateText("directory"),
            context);
        OdfFormulaResult sheets = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.SHEETS,
            [],
            context);
        OdfFormulaResult referenceSheets = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.SHEETS,
            [reference],
            context);
        OdfFormulaResult hyperlink = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.HYPERLINK,
            [
                FormulaValueModel.CreateText("https://example.invalid"),
                FormulaValueModel.CreateText("Example")
            ],
            context);
        OdfFormulaResult unresolved = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.DDE,
            [
                FormulaValueModel.CreateText("service"),
                FormulaValueModel.CreateText("topic"),
                FormulaValueModel.CreateText("item")
            ],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(formula.StringValue,
                Is.EqualTo("of:=SUM([.A1:.A2])"));
            Assert.That(isFormula.BooleanValue, Is.True);
            Assert.That(address.StringValue, Is.EqualTo("$Sheet1.$C$4"));
            Assert.That(contents.NumericValue, Is.EqualTo(42));
            Assert.That(directory.StringValue, Is.EqualTo("/documents"));
            Assert.That(sheets.NumericValue, Is.EqualTo(3));
            Assert.That(referenceSheets.NumericValue, Is.EqualTo(2));
            Assert.That(hyperlink.StringValue, Is.EqualTo("Example"));
            Assert.That(unresolved.Error,
                Is.EqualTo(OdfFormulaError.NotAvailable));
        });
    }

    [Test]
    public void InvokesExplicitResolverForExternalResourceFunctions()
    {
        RecordingResolver resolver = new RecordingResolver();
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.ExternalResolver = resolver;
        OdfFormulaResult result = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.DDE,
            [
                FormulaValueModel.CreateText("service"),
                FormulaValueModel.CreateText("topic"),
                FormulaValueModel.CreateText("item")
            ],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(result.StringValue, Is.EqualTo("resolved"));
            Assert.That(resolver.CallCount, Is.EqualTo(1));
            Assert.That(resolver.FunctionName, Is.EqualTo(OdfFormulaNames.DDE));
            Assert.That(resolver.Arguments, Has.Length.EqualTo(3));
            Assert.That(resolver.Arguments[0].StringValue,
                Is.EqualTo("service"));
        });
    }

    [Test]
    public void ReportsUnsupportedFunctionsAndInvalidArgumentCounts()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult unsupported = FormulaFunctionEvaluator.Evaluate(
            "NOT_A_FUNCTION",
            [],
            context);
        OdfFormulaResult invalidCount = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.DSUM,
            [],
            context);
        OdfFormulaResult notAvailable = FormulaFunctionEvaluator.Evaluate(
            OdfFormulaNames.NA,
            [],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Error,
                Is.EqualTo(OdfFormulaError.UnsupportedFunction));
            Assert.That(invalidCount.Error, Is.EqualTo(OdfFormulaError.Value));
            Assert.That(notAvailable.Error,
                Is.EqualTo(OdfFormulaError.NotAvailable));
        });
    }

    static OdfFormulaResult EvaluateDatabase(
        string name,
        OdfFormulaResult database,
        OdfFormulaResult field,
        OdfFormulaResult criteria,
        FormulaFunctionContext context)
    {
        return FormulaFunctionEvaluator.Evaluate(
            name,
            [database, field, criteria],
            context);
    }

    static OdfFormulaResult EvaluateOne(
        string name,
        OdfFormulaResult value,
        FormulaFunctionContext context)
    {
        return FormulaFunctionEvaluator.Evaluate(name, [value], context);
    }

    static OdfFormulaResult CreateSalesDatabase()
    {
        return CreateArray(
            [
                "Name", "Region", "Sales", "Units",
                "Alice", "West", 100, 2,
                "Bob", "East", 200, 4,
                "Cara", "West", 300, 6,
                "Dan", "West", 400, 8
            ],
            5,
            4);
    }

    static OdfFormulaResult CreateArray(
        object[] values,
        int rows,
        int columns)
    {
        OdfFormulaResult[] results = new OdfFormulaResult[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] is string text)
            {
                results[index] = text.Length == 0
                    ? FormulaValueModel.CreateEmpty()
                    : FormulaValueModel.CreateText(text);
            }
            else
            {
                results[index] = FormulaValueModel.CreateNumber(
                    global::System.Convert.ToDouble(values[index]));
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateArray(results, rows, columns);
    }
}

internal class RecordingResolver : IFormulaExternalResourceResolver
{
    internal int CallCount;
    internal string FunctionName = "";
    internal OdfFormulaResult[] Arguments = [];

    public OdfFormulaResult ResolveExternalFunction(
        string functionName,
        OdfFormulaResult[] arguments)
    {
        CallCount = CallCount + 1;
        FunctionName = functionName;
        Arguments = arguments;
        return FormulaValueModel.CreateText("resolved");
    }
}
