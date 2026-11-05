namespace odf.Tests;

public class FormulaLookupMatrixFunctionTests
{
    [Test]
    public void PublishesCompleteLookupAndMatrixMetadata()
    {
        string[] matrixNames =
        [
            OdfFormulaNames.MDETERM,
            OdfFormulaNames.MINVERSE,
            OdfFormulaNames.MMULT,
            OdfFormulaNames.MUNIT,
            OdfFormulaNames.TRANSPOSE
        ];
        string[] lookupNames =
        [
            OdfFormulaNames.ADDRESS,
            OdfFormulaNames.CHOOSE,
            OdfFormulaNames.GETPIVOTDATA,
            OdfFormulaNames.HLOOKUP,
            OdfFormulaNames.INDEX,
            OdfFormulaNames.INDIRECT,
            OdfFormulaNames.LOOKUP,
            OdfFormulaNames.MATCH,
            OdfFormulaNames.MULTIPLE_OPERATIONS,
            OdfFormulaNames.OFFSET,
            OdfFormulaNames.VLOOKUP
        ];

        Assert.Multiple(() =>
        {
            Assert.That(FormulaFunctionCatalog.Functions, Has.Length.EqualTo(393));
            Assert.That(matrixNames.All(name =>
                FormulaFunctionCatalog.Find(name)?.Group == "Matrix"), Is.True);
            Assert.That(lookupNames.All(name =>
                FormulaFunctionCatalog.Find(name)?.Group == "Lookup"), Is.True);
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.MDETERM)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Large));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.CHOOSE)!
                .Conformance, Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.INDIRECT)!
                .IsVolatile, Is.True);
            Assert.That(FormulaFunctionCatalog.Find(OdfFormulaNames.OFFSET)!
                .IsVolatile, Is.True);
        });
    }

    [Test]
    public void EvaluatesDeterminantsInversesProductsUnitsAndTransposes()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult matrix = Array(2, 2, 4, 7, 2, 6);
        OdfFormulaResult right = Array(2, 2, 1, 2, 3, 4);

        OdfFormulaResult determinant = Evaluate(
            OdfFormulaNames.MDETERM, [matrix], context);
        OdfFormulaResult inverse = Evaluate(
            OdfFormulaNames.MINVERSE, [matrix], context);
        OdfFormulaResult product = Evaluate(
            OdfFormulaNames.MMULT, [matrix, right], context);
        OdfFormulaResult unit = Evaluate(
            OdfFormulaNames.MUNIT, [Number(3)], context);
        OdfFormulaResult transpose = Evaluate(
            OdfFormulaNames.TRANSPOSE,
            [Array(2, 3, 1, 2, 3, 4, 5, 6)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(determinant.NumericValue, Is.EqualTo(10).Within(0.000001));
            Assert.That(Numbers(inverse), Is.EqualTo(new[] { 0.6, -0.7, -0.2, 0.4 })
                .Within(0.000001));
            Assert.That(Numbers(product), Is.EqualTo(new[] { 25.0, 36.0, 20.0, 28.0 }));
            Assert.That(unit.RowCount, Is.EqualTo(3));
            Assert.That(Numbers(unit), Is.EqualTo(new[]
                { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }));
            Assert.That(transpose.RowCount, Is.EqualTo(3));
            Assert.That(transpose.ColumnCount, Is.EqualTo(2));
            Assert.That(Numbers(transpose), Is.EqualTo(new[]
                { 1.0, 4.0, 2.0, 5.0, 3.0, 6.0 }));
        });
    }

    [Test]
    public void ReportsInvalidMatrixDimensionsAndSingularMatrices()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult nonSquare = Array(1, 2, 1, 2);
        OdfFormulaResult singular = Array(2, 2, 1, 2, 2, 4);

        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(OdfFormulaNames.MDETERM,
                [nonSquare], context).Error, Is.EqualTo(OdfFormulaError.Value));
            Assert.That(Evaluate(OdfFormulaNames.MINVERSE,
                [singular], context).Error, Is.EqualTo(OdfFormulaError.Number));
            Assert.That(Evaluate(OdfFormulaNames.MMULT,
                [nonSquare, nonSquare], context).Error,
                Is.EqualTo(OdfFormulaError.Value));
            Assert.That(Evaluate(OdfFormulaNames.MUNIT,
                [Number(0)], context).Error, Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void EvaluatesAddressChooseAndIndexForms()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.CurrentRowNumber = 3;
        context.CurrentColumnNumber = 2;
        OdfFormulaResult source = Array(2, 3, 1, 2, 3, 4, 5, 6);

        OdfFormulaResult address = Evaluate(
            OdfFormulaNames.ADDRESS, [Number(12), Number(28)], context);
        OdfFormulaResult relative = Evaluate(
            OdfFormulaNames.ADDRESS,
            [Number(5), Number(4), Number(4), Boolean(false)],
            context);
        OdfFormulaResult chosen = Evaluate(
            OdfFormulaNames.CHOOSE,
            [Number(2), Text("first"), Text("second")],
            context);
        OdfFormulaResult element = Evaluate(
            OdfFormulaNames.INDEX,
            [source, Number(2), Number(3)],
            context);
        OdfFormulaResult column = Evaluate(
            OdfFormulaNames.INDEX,
            [source, FormulaValueModel.CreateEmpty(), Number(2)],
            context);
        OdfFormulaResult referenceColumn = Evaluate(
            OdfFormulaNames.INDEX,
            [Reference(2, 3, 1, 2, 3, 4, 5, 6),
                FormulaValueModel.CreateEmpty(), Number(2)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(address.StringValue, Is.EqualTo("$AB$12"));
            Assert.That(relative.StringValue, Is.EqualTo("R[5]C[4]"));
            Assert.That(chosen.StringValue, Is.EqualTo("second"));
            Assert.That(element.NumericValue, Is.EqualTo(6));
            Assert.That(Numbers(column), Is.EqualTo(new[] { 2.0, 5.0 }));
            Assert.That(referenceColumn.Kind,
                Is.EqualTo(OdfValueKind.Reference));
            Assert.That(Numbers(referenceColumn),
                Is.EqualTo(new[] { 2.0, 5.0 }));
        });
    }

    [Test]
    public void EvaluatesExactAndApproximateLookupForms()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        OdfFormulaResult vertical = MixedArray(
            4,
            2,
            Number(1), Text("one"),
            Number(3), Text("three"),
            Number(5), Text("five"),
            Number(7), Text("seven"));
        OdfFormulaResult horizontal = MixedArray(
            2,
            4,
            Number(1), Number(3), Number(5), Number(7),
            Text("one"), Text("three"), Text("five"), Text("seven"));
        OdfFormulaResult searched = Array(4, 1, 1, 3, 5, 7);
        OdfFormulaResult results = MixedArray(
            4, 1, Text("one"), Text("three"), Text("five"), Text("seven"));

        OdfFormulaResult verticalExact = Evaluate(
            OdfFormulaNames.VLOOKUP,
            [Number(5), vertical, Number(2), Boolean(false)],
            context);
        OdfFormulaResult horizontalApproximate = Evaluate(
            OdfFormulaNames.HLOOKUP,
            [Number(6), horizontal, Number(2)],
            context);
        OdfFormulaResult lookup = Evaluate(
            OdfFormulaNames.LOOKUP,
            [Number(4), searched, results],
            context);
        OdfFormulaResult exactMatch = Evaluate(
            OdfFormulaNames.MATCH,
            [Number(5), searched, Number(0)],
            context);
        OdfFormulaResult approximateMatch = Evaluate(
            OdfFormulaNames.MATCH,
            [Number(6), searched],
            context);
        OdfFormulaResult duplicateMatch = Evaluate(
            OdfFormulaNames.MATCH,
            [Number(3), Array(4, 1, 1, 3, 3, 5)],
            context);
        context.SpreadsheetSettings.UseWildcards = true;
        OdfFormulaResult wildcardMatch = Evaluate(
            OdfFormulaNames.MATCH,
            [Text("t*"), MixedArray(2, 1, Text("one"), Text("three")),
                Number(0)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(verticalExact.StringValue, Is.EqualTo("five"));
            Assert.That(horizontalApproximate.StringValue, Is.EqualTo("five"));
            Assert.That(lookup.StringValue, Is.EqualTo("three"));
            Assert.That(exactMatch.NumericValue, Is.EqualTo(3));
            Assert.That(approximateMatch.NumericValue, Is.EqualTo(3));
            Assert.That(duplicateMatch.NumericValue, Is.EqualTo(3));
            Assert.That(wildcardMatch.NumericValue, Is.EqualTo(2));
            Assert.That(Evaluate(OdfFormulaNames.VLOOKUP,
                [Number(4), vertical, Number(2), Boolean(false)],
                context).Error, Is.EqualTo(OdfFormulaError.NotAvailable));
        });
    }

    [Test]
    public void DelegatesWorkbookDependentLookupFunctionsExplicitly()
    {
        LookupResolver resolver = new LookupResolver();
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.LookupResolver = resolver;

        string[] names =
        [
            OdfFormulaNames.GETPIVOTDATA,
            OdfFormulaNames.INDIRECT,
            OdfFormulaNames.MULTIPLE_OPERATIONS
        ];
        int index = 0;
        while (index < names.Length)
        {
            OdfFormulaResult result = Evaluate(
                names[index],
                ResolverArguments(names[index]),
                context);
            Assert.That(result.StringValue, Is.EqualTo(names[index]));
            index++;
        }

        OdfFormulaResult reference = Reference(2, 2, 1, 2, 3, 4);
        OdfFormulaResult localOffset = Evaluate(
            OdfFormulaNames.OFFSET,
            [reference, Number(1), Number(1), Number(1), Number(1)],
            context);
        OdfFormulaResult resolvedOffset = Evaluate(
            OdfFormulaNames.OFFSET,
            [reference, Number(2), Number(0)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(localOffset.Values[0].NumericValue, Is.EqualTo(4));
            Assert.That((int)localOffset.Reference!.Start.RowNumber,
                Is.EqualTo(2));
            Assert.That((int)localOffset.Reference.Start.ColumnNumber,
                Is.EqualTo(2));
            Assert.That(resolvedOffset.StringValue,
                Is.EqualTo(OdfFormulaNames.OFFSET));
        });

        context.LookupResolver = null;
        Assert.That(Evaluate(OdfFormulaNames.INDIRECT,
            [Text("[.A1]")], context).Error,
            Is.EqualTo(OdfFormulaError.NotAvailable));
    }

    static OdfFormulaResult[] ResolverArguments(string name)
    {
        if (name == OdfFormulaNames.INDIRECT)
        {
            return [Text("[.A1]")];
        }
        if (name == OdfFormulaNames.GETPIVOTDATA)
        {
            return [Text("Sales"), Text("Pivot")];
        }
        if (name == OdfFormulaNames.MULTIPLE_OPERATIONS)
        {
            return [Text("Formula"), Text("Input"), Number(2)];
        }
        return [Text("Reference"), Number(1), Number(2)];
    }

    static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        return FormulaFunctionEvaluator.Evaluate(name, arguments, context);
    }

    static OdfFormulaResult Array(
        int rows,
        int columns,
        params double[] values)
    {
        OdfFormulaResult[] items = new OdfFormulaResult[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            items[index] = Number(values[index]);
            index++;
        }
        return FormulaValueModel.CreateArray(items, rows, columns);
    }

    static OdfFormulaResult MixedArray(
        int rows,
        int columns,
        params OdfFormulaResult[] values)
    {
        return FormulaValueModel.CreateArray(values, rows, columns);
    }

    static OdfFormulaResult Reference(
        int rows,
        int columns,
        params double[] values)
    {
        OdfFormulaReference reference = new OdfFormulaReference
        {
            Kind = rows == 1 && columns == 1
                ? OdfFormulaNodeKind.Reference
                : OdfFormulaNodeKind.Range,
            HasEnd = rows != 1 || columns != 1,
            Start = new OdfFormulaReferenceEndpoint
            {
                RowNumber = 1,
                ColumnNumber = 1,
                HasRow = true,
                HasColumn = true
            },
            End = new OdfFormulaReferenceEndpoint
            {
                RowNumber = rows,
                ColumnNumber = columns,
                HasRow = true,
                HasColumn = true
            }
        };
        return FormulaValueModel.CreateReference(
            reference,
            Array(rows, columns, values).Values,
            rows,
            columns);
    }

    static OdfFormulaResult Number(double value)
    {
        return FormulaValueModel.CreateNumber(value);
    }

    static OdfFormulaResult Text(string value)
    {
        return FormulaValueModel.CreateText(value);
    }

    static OdfFormulaResult Boolean(bool value)
    {
        return FormulaValueModel.CreateBoolean(value);
    }

    static double[] Numbers(OdfFormulaResult value)
    {
        return value.Values.Select(item => item.NumericValue).ToArray();
    }
}

public class LookupResolver : IFormulaLookupResourceResolver
{
    public OdfFormulaResult ResolveLookupFunction(
        string functionName,
        OdfFormulaResult[] arguments)
    {
        return FormulaValueModel.CreateText(functionName);
    }
}
