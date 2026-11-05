using System.Globalization;

namespace fake;

internal static class ScienceGenerator
{
    const int MinimumMeasurementHundredths = 1;
    const int MaximumMeasurementHundredths = 10000;
    const int MaximumFormattedMeasurementLength = 12;
    const int ExperimentNumberMaximum = 9999;

    internal static string Field(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateFieldData(context);
        return Value(context, random, ScienceData.Fields, "scienceField");
    }

    internal static string Element(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateElementData(context);
        return Value(context, random, ScienceData.Elements, "element");
    }

    internal static string Unit(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateUnitData(context);
        return Value(context, random, ScienceData.Units, "unit");
    }

    internal static string SiPrefix(
        GenerationContext context,
        FakeRandom random)
    {
        ValidatePrefixData(context);
        return Value(context, random, ScienceData.SiPrefixes, "siPrefix");
    }

    internal static MeasurementRecord Measurement(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateUnitData(context);
        ValidatePrefixData(context);
        Guard.OutputSize(
            MaximumMeasurementSize(),
            context.CapturedOptions.MaximumOutputSize,
            "measurement");
        return MeasurementUnchecked(random);
    }

    internal static string Planet(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, ScienceData.Planets, "planetData");
        return Value(context, random, ScienceData.Planets, "planet");
    }

    internal static string Space(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, ScienceData.SpaceTerms, "spaceData");
        return Value(context, random, ScienceData.SpaceTerms, "space");
    }

    internal static string Experiment(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateFieldData(context);
        ValidateValues(
            context,
            ScienceData.ExperimentActions,
            "experimentData");
        Guard.OutputSize(
            MaximumExperimentLength(),
            context.CapturedOptions.MaximumOutputSize,
            "experiment");
        int fieldIndex = random.IntFromMaximumExclusive(ScienceData.Fields.Length);
        return ExperimentUnchecked(random, fieldIndex);
    }

    internal static ScienceRecord Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateFieldData(context);
        ValidateElementData(context);
        ValidateUnitData(context);
        ValidatePrefixData(context);
        ValidateValues(context, ScienceData.Planets, "planetData");
        ValidateValues(context, ScienceData.SpaceTerms, "spaceData");
        ValidateValues(
            context,
            ScienceData.ExperimentActions,
            "experimentData");
        Guard.OutputSize(
            MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "scienceRecord");
        int fieldIndex = random.IntFromMaximumExclusive(ScienceData.Fields.Length);
        int elementIndex = random.IntFromMaximumExclusive(ScienceData.Elements.Length);
        ScienceRecord result = new ScienceRecord();
        result.Field = ScienceData.Fields[fieldIndex];
        result.Element = ScienceData.Elements[elementIndex];
        result.ElementSymbol = ScienceData.ElementSymbols[elementIndex];
        result.AtomicNumber = ScienceData.AtomicNumbers[elementIndex];
        result.Measurement = MeasurementUnchecked(random);
        result.Planet = ScienceData.Planets[
            random.IntFromMaximumExclusive(ScienceData.Planets.Length)];
        result.Space = ScienceData.SpaceTerms[
            random.IntFromMaximumExclusive(ScienceData.SpaceTerms.Length)];
        result.Experiment = ExperimentUnchecked(random, fieldIndex);
        result.IsSynthetic = true;
        result.RepresentsExperiment = false;
        return result;
    }

    static MeasurementRecord MeasurementUnchecked(FakeRandom random)
    {
        int unitIndex = random.IntFromMaximumExclusive(ScienceData.Units.Length);
        int prefixIndex = random.IntFromMaximumExclusive(ScienceData.SiPrefixes.Length);
        decimal value = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            MinimumMeasurementHundredths,
            MaximumMeasurementHundredths,
            true) / 100m;
        MeasurementRecord result = new MeasurementRecord();
        result.Quantity = ScienceData.Quantities[unitIndex];
        result.Value = value;
        result.SiPrefix = ScienceData.SiPrefixes[prefixIndex];
        result.PrefixSymbol = ScienceData.SiPrefixSymbols[prefixIndex];
        result.PrefixExponent = ScienceData.SiPrefixExponents[prefixIndex];
        result.Unit = ScienceData.Units[unitIndex];
        result.UnitSymbol = ScienceData.UnitSymbols[unitIndex];
        result.Formatted = value.ToString(
            "0.00",
            CultureInfo.InvariantCulture) + " " +
            result.PrefixSymbol + result.UnitSymbol;
        result.IsSynthetic = true;
        result.IsMeasured = false;
        return result;
    }

    static string ExperimentUnchecked(FakeRandom random, int fieldIndex)
    {
        int actionIndex = random.IntFromMaximumExclusive(ScienceData.ExperimentActions.Length);
        string number = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            0,
            ExperimentNumberMaximum,
            true).ToString("D4", CultureInfo.InvariantCulture);
        return "TEST-" + number + ": " +
            ScienceData.ExperimentActions[actionIndex] + " " +
            ScienceData.ExperimentSubjects[fieldIndex];
    }

    static string Value(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string outputName)
    {
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static void ValidateFieldData(GenerationContext context)
    {
        ValidatePairs(
            context,
            ScienceData.Fields,
            ScienceData.ExperimentSubjects,
            "scienceFieldData");
    }

    static void ValidateElementData(GenerationContext context)
    {
        if (ScienceData.Elements.Length != ScienceData.ElementSymbols.Length ||
            ScienceData.Elements.Length != ScienceData.AtomicNumbers.Length)
        {
            throw FakeException.Create(
                "The project-authored element table has inconsistent column lengths.");
        }
        ValidateValues(context, ScienceData.Elements, "elementData");
        ValidateValues(context, ScienceData.ElementSymbols, "elementData");
        Guard.Count(
            ScienceData.AtomicNumbers.Length,
            context.CapturedOptions.MaximumCount,
            "elementData");
    }

    static void ValidateUnitData(GenerationContext context)
    {
        if (ScienceData.Quantities.Length != ScienceData.Units.Length ||
            ScienceData.Units.Length != ScienceData.UnitSymbols.Length)
        {
            throw FakeException.Create(
                "The project-authored unit table has inconsistent column lengths.");
        }
        ValidateValues(context, ScienceData.Quantities, "unitData");
        ValidateValues(context, ScienceData.Units, "unitData");
        ValidateValues(context, ScienceData.UnitSymbols, "unitData");
    }

    static void ValidatePrefixData(GenerationContext context)
    {
        if (ScienceData.SiPrefixes.Length !=
                ScienceData.SiPrefixSymbols.Length ||
            ScienceData.SiPrefixes.Length !=
                ScienceData.SiPrefixExponents.Length)
        {
            throw FakeException.Create(
                "The project-authored SI prefix table has inconsistent column lengths.");
        }
        ValidateValues(context, ScienceData.SiPrefixes, "siPrefixData");
        ValidateValues(
            context,
            ScienceData.SiPrefixSymbols,
            "siPrefixData");
        Guard.Count(
            ScienceData.SiPrefixExponents.Length,
            context.CapturedOptions.MaximumCount,
            "siPrefixData");
    }

    static void ValidatePairs(
        GenerationContext context,
        string[] first,
        string[] second,
        string dataName)
    {
        if (first.Length != second.Length)
        {
            throw FakeException.Create(
                "A project-authored science table has inconsistent column lengths.");
        }
        ValidateValues(context, first, dataName);
        ValidateValues(context, second, dataName);
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string dataName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            dataName);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || values[index].Length == 0)
            {
                throw FakeException.Create(
                    "A project-authored science data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumMeasurementSize()
    {
        return MaximumLength(ScienceData.Quantities) +
            MaximumLength(ScienceData.SiPrefixes) +
            MaximumLength(ScienceData.SiPrefixSymbols) +
            MaximumLength(ScienceData.Units) +
            MaximumLength(ScienceData.UnitSymbols) +
            MaximumFormattedMeasurementLength;
    }

    static int MaximumExperimentLength()
    {
        return 11 +
            MaximumLength(ScienceData.ExperimentActions) + 1 +
            MaximumLength(ScienceData.ExperimentSubjects);
    }

    static int MaximumRecordSize()
    {
        return MaximumLength(ScienceData.Fields) +
            MaximumLength(ScienceData.Elements) +
            MaximumLength(ScienceData.ElementSymbols) +
            MaximumMeasurementSize() + MaximumLength(ScienceData.Planets) +
            MaximumLength(ScienceData.SpaceTerms) +
            MaximumExperimentLength();
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }
}
