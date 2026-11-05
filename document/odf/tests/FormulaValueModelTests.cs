namespace odf.Tests;

public class FormulaValueModelTests
{
    [Test]
    public void CreatesValuesFromEveryScalarLiteralKind()
    {
        OdfFormulaResult number = FormulaValueModel.FromLiteral(
            FormulaParser.Parse("=1.25", 32).Root!);
        OdfFormulaResult percentage = FormulaValueModel.FromLiteral(
            FormulaParser.Parse("=25%", 32).Root!);
        OdfFormulaResult text = FormulaValueModel.FromLiteral(
            FormulaParser.Parse("=\"a\"\"b\"", 32).Root!);
        OdfFormulaResult boolean = FormulaValueModel.FromLiteral(
            FormulaParser.Parse("=TRUE", 32).Root!);
        OdfFormulaResult error = FormulaValueModel.FromLiteral(
            FormulaParser.Parse("=#DIV/0!", 32).Root!);

        Assert.Multiple(() =>
        {
            Assert.That(number.Kind, Is.EqualTo(OdfValueKind.Float));
            Assert.That(number.NumericValue, Is.EqualTo(1.25));
            Assert.That(percentage.Kind, Is.EqualTo(OdfValueKind.Percentage));
            Assert.That(percentage.NumericValue, Is.EqualTo(0.25));
            Assert.That(text.StringValue, Is.EqualTo("a\"b"));
            Assert.That(boolean.BooleanValue, Is.True);
            Assert.That(error.Kind, Is.EqualTo(OdfValueKind.Error));
            Assert.That(error.Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(error.StringValue,
                Is.EqualTo(OdfFormulaNames.ErrorDivisionByZero));
        });
    }

    [Test]
    public void AppliesLocaleIndependentScalarConversions()
    {
        OdfFormulaResult emptyNumber = FormulaValueModel.ConvertToNumber(
            FormulaValueModel.CreateEmpty());
        OdfFormulaResult trueNumber = FormulaValueModel.ConvertToNumber(
            FormulaValueModel.CreateBoolean(true));
        OdfFormulaResult parsedNumber = FormulaValueModel.ConvertToNumber(
            FormulaValueModel.CreateText(" 12.5 "));
        OdfFormulaResult invalidNumber = FormulaValueModel.ConvertToNumber(
            FormulaValueModel.CreateText("12,5"));
        OdfFormulaResult numberText = FormulaValueModel.ConvertToText(
            FormulaValueModel.CreateNumber(12.5));
        OdfFormulaResult falseBoolean = FormulaValueModel.ConvertToBoolean(
            FormulaValueModel.CreateText("false"));
        OdfFormulaResult invalidBoolean = FormulaValueModel.ConvertToBoolean(
            FormulaValueModel.CreateText("yes"));

        Assert.Multiple(() =>
        {
            Assert.That(emptyNumber.NumericValue, Is.Zero);
            Assert.That(trueNumber.NumericValue, Is.EqualTo(1));
            Assert.That(parsedNumber.NumericValue, Is.EqualTo(12.5));
            Assert.That(invalidNumber.Error,
                Is.EqualTo(OdfFormulaError.Value));
            Assert.That(numberText.StringValue, Is.EqualTo("12.5"));
            Assert.That(falseBoolean.BooleanValue, Is.False);
            Assert.That(invalidBoolean.Error,
                Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void AppliesOperatorsAndPropagatesErrorsLeftToRight()
    {
        OdfFormulaResult sum = FormulaValueModel.ApplyBinary(
            "+",
            FormulaValueModel.CreateNumber(2),
            FormulaValueModel.CreateText("3"));
        OdfFormulaResult concatenated = FormulaValueModel.ApplyBinary(
            "&",
            FormulaValueModel.CreateText("Total: "),
            FormulaValueModel.CreateNumber(5));
        OdfFormulaResult division = FormulaValueModel.ApplyBinary(
            "/",
            FormulaValueModel.CreateNumber(1),
            FormulaValueModel.CreateNumber(0));
        OdfFormulaResult propagated = FormulaValueModel.ApplyBinary(
            "+",
            FormulaValueModel.CreateError(OdfFormulaError.Reference),
            FormulaValueModel.CreateError(OdfFormulaError.Number));
        OdfFormulaResult negated = FormulaValueModel.ApplyUnary(
            "-",
            FormulaValueModel.CreateBoolean(true));

        Assert.Multiple(() =>
        {
            Assert.That(sum.NumericValue, Is.EqualTo(5));
            Assert.That(concatenated.StringValue, Is.EqualTo("Total: 5"));
            Assert.That(division.Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(propagated.Error,
                Is.EqualTo(OdfFormulaError.Reference));
            Assert.That(negated.NumericValue, Is.EqualTo(-1));
        });
    }

    [Test]
    public void ComparesNumbersTextBooleansAndEmptyValuesDeterministically()
    {
        OdfFormulaResult numeric = FormulaValueModel.CompareValues(
            FormulaValueModel.CreateNumber(2),
            FormulaValueModel.CreateNumber(10),
            false);
        OdfFormulaResult textInsensitive = FormulaValueModel.ApplyBinary(
            "=",
            FormulaValueModel.CreateText("Alpha"),
            FormulaValueModel.CreateText("alpha"));
        OdfFormulaResult textSensitive = FormulaValueModel.CompareValues(
            FormulaValueModel.CreateText("Alpha"),
            FormulaValueModel.CreateText("alpha"),
            true);
        OdfFormulaResult emptyEqualsZero = FormulaValueModel.ApplyBinary(
            "=",
            FormulaValueModel.CreateEmpty(),
            FormulaValueModel.CreateNumber(0));
        OdfFormulaResult typeOrder = FormulaValueModel.CompareValues(
            FormulaValueModel.CreateText("0"),
            FormulaValueModel.CreateBoolean(false),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(numeric.NumericValue, Is.EqualTo(-1));
            Assert.That(textInsensitive.BooleanValue, Is.True);
            Assert.That(textSensitive.NumericValue, Is.EqualTo(-1));
            Assert.That(emptyEqualsZero.BooleanValue, Is.True);
            Assert.That(typeOrder.NumericValue, Is.EqualTo(-1));
        });
    }

    [Test]
    public void BroadcastsScalarsAcrossArraysAndPreservesElementErrors()
    {
        OdfFormulaResult array = FormulaValueModel.CreateArray(
            [
                FormulaValueModel.CreateNumber(1),
                FormulaValueModel.CreateError(OdfFormulaError.NotAvailable),
                FormulaValueModel.CreateNumber(3),
                FormulaValueModel.CreateNumber(4)
            ],
            2,
            2);
        OdfFormulaResult result = FormulaValueModel.ApplyBinary(
            "+",
            array,
            FormulaValueModel.CreateNumber(10));
        OdfFormulaResult mismatch = FormulaValueModel.ApplyBinary(
            "+",
            array,
            FormulaValueModel.CreateArray(
                [FormulaValueModel.CreateNumber(1)],
                1,
                1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(OdfValueKind.Array));
            Assert.That(result.RowCount, Is.EqualTo(2));
            Assert.That(result.ColumnCount, Is.EqualTo(2));
            Assert.That(result.Values[0].NumericValue, Is.EqualTo(11));
            Assert.That(result.Values[1].Error,
                Is.EqualTo(OdfFormulaError.NotAvailable));
            Assert.That(result.Values[3].NumericValue, Is.EqualTo(14));
            Assert.That(mismatch.Error, Is.EqualTo(OdfFormulaError.Value));
        });
    }

    [Test]
    public void RetainsReferencesUntilScalarOrArrayBehaviorIsRequested()
    {
        OdfFormulaReference reference = new OdfFormulaReference();
        reference.Kind = OdfFormulaNodeKind.Range;
        OdfFormulaResult single = FormulaValueModel.CreateReference(
            reference,
            [FormulaValueModel.CreateNumber(42)],
            1,
            1);
        OdfFormulaResult range = FormulaValueModel.CreateReference(
            reference,
            [
                FormulaValueModel.CreateNumber(1),
                FormulaValueModel.CreateNumber(2)
            ],
            1,
            2);
        OdfFormulaResult scalar = FormulaValueModel.ConvertToNumber(single);
        OdfFormulaResult invalidScalar = FormulaValueModel.ConvertToNumber(
            range);
        OdfFormulaResult broadcast = FormulaValueModel.ApplyBinary(
            "*",
            range,
            FormulaValueModel.CreateNumber(3));

        Assert.Multiple(() =>
        {
            Assert.That(single.Kind, Is.EqualTo(OdfValueKind.Reference));
            Assert.That(single.Reference, Is.SameAs(reference));
            Assert.That(scalar.NumericValue, Is.EqualTo(42));
            Assert.That(invalidScalar.Error,
                Is.EqualTo(OdfFormulaError.Reference));
            Assert.That(broadcast.Kind, Is.EqualTo(OdfValueKind.Array));
            Assert.That(broadcast.Values[0].NumericValue, Is.EqualTo(3));
            Assert.That(broadcast.Values[1].NumericValue, Is.EqualTo(6));
        });
    }

    [Test]
    public void RoundsTiesAndDirectedValuesAtPositiveAndNegativeDigits()
    {
        OdfFormulaResult positiveTie = FormulaValueModel.RoundValue(
            FormulaValueModel.CreateNumber(2.675),
            2);
        OdfFormulaResult negativeTie = FormulaValueModel.RoundValue(
            FormulaValueModel.CreateNumber(-2.5),
            0);
        OdfFormulaResult down = FormulaValueModel.RoundDownValue(
            FormulaValueModel.CreateNumber(-12.39),
            1);
        OdfFormulaResult up = FormulaValueModel.RoundUpValue(
            FormulaValueModel.CreateNumber(-12.31),
            1);
        OdfFormulaResult truncated = FormulaValueModel.TruncateValue(
            FormulaValueModel.CreateNumber(1234.5),
            -2);

        Assert.Multiple(() =>
        {
            Assert.That(positiveTie.NumericValue, Is.EqualTo(2.68));
            Assert.That(negativeTie.NumericValue, Is.EqualTo(-3));
            Assert.That(down.NumericValue, Is.EqualTo(-12.3));
            Assert.That(up.NumericValue, Is.EqualTo(-12.4));
            Assert.That(truncated.NumericValue, Is.EqualTo(1200));
        });
    }

    [Test]
    public void MapsEveryStandardErrorToItsFormulaLiteral()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FormulaValueModel.FormatError(OdfFormulaError.Null),
                Is.EqualTo(OdfFormulaNames.ErrorNull));
            Assert.That(FormulaValueModel.FormatError(
                OdfFormulaError.DivisionByZero),
                Is.EqualTo(OdfFormulaNames.ErrorDivisionByZero));
            Assert.That(FormulaValueModel.FormatError(OdfFormulaError.Value),
                Is.EqualTo(OdfFormulaNames.ErrorValue));
            Assert.That(FormulaValueModel.FormatError(
                OdfFormulaError.Reference),
                Is.EqualTo(OdfFormulaNames.ErrorReference));
            Assert.That(FormulaValueModel.FormatError(OdfFormulaError.Name),
                Is.EqualTo(OdfFormulaNames.ErrorName));
            Assert.That(FormulaValueModel.FormatError(OdfFormulaError.Number),
                Is.EqualTo(OdfFormulaNames.ErrorNumber));
            Assert.That(FormulaValueModel.FormatError(
                OdfFormulaError.NotAvailable),
                Is.EqualTo(OdfFormulaNames.ErrorNotAvailable));
        });
    }
}
