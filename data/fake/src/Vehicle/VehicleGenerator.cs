using System;
using System.Globalization;

namespace fake;

internal static class VehicleGenerator
{
    const int RegistrationPlateLength = 9;

    internal static string Make(
        GenerationContext context,
        FakeRandom random)
    {
        return FixtureValue(context, random, 0, "vehicleMake");
    }

    internal static string Model(
        GenerationContext context,
        FakeRandom random)
    {
        return FixtureValue(context, random, 1, "vehicleModel");
    }

    internal static string Type(
        GenerationContext context,
        FakeRandom random)
    {
        return FixtureValue(context, random, 2, "vehicleType");
    }

    internal static string Fuel(
        GenerationContext context,
        FakeRandom random)
    {
        return FixtureValue(context, random, 3, "vehicleFuel");
    }

    internal static string Bicycle(
        GenerationContext context,
        FakeRandom random)
    {
        return ReviewedValue(
            context,
            random,
            VehicleData.Bicycles,
            "bicycleData",
            "bicycle");
    }

    internal static string RegistrationPlate(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            RegistrationPlateLength,
            context.CapturedOptions.MaximumOutputSize,
            "registrationPlate");
        return RegistrationPlateUnchecked(random);
    }

    internal static string Vin(
        GenerationContext context,
        FakeRandom random)
    {
        return Vin(
            context,
            random,
            ReferenceModelYear(context.ReferenceTime().Year));
    }

    internal static string Vin(
        GenerationContext context,
        FakeRandom random,
        int modelYear)
    {
        return VinAlgorithms.Generate(
            random,
            modelYear,
            context.CapturedOptions.MaximumCount,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static string Route(
        GenerationContext context,
        FakeRandom random)
    {
        return ReviewedValue(
            context,
            random,
            VehicleData.Routes,
            "routeData",
            "route");
    }

    internal static string TransitMode(
        GenerationContext context,
        FakeRandom random)
    {
        return ReviewedValue(
            context,
            random,
            VehicleData.TransitModes,
            "transitModeData",
            "transitMode");
    }

    internal static VehicleProfile Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateFixtureData(context);
        ValidateValues(context, VehicleData.Colors, "vehicleColorData");
        ValidateValues(context, VehicleData.Routes, "routeData");
        ValidateValues(context, VehicleData.TransitModes, "transitModeData");
        Guard.Count(
            VinAlgorithms.Length,
            context.CapturedOptions.MaximumCount,
            "vin");
        Guard.OutputSize(
            MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "vehicleProfile");
        int fixtureIndex = random.IntFromMaximumExclusive(VehicleData.Makes.Length);
        int modelYear = RecentModelYear(
            random,
            ReferenceModelYear(context.ReferenceTime().Year));
        VehicleProfile result = new VehicleProfile();
        result.Make = VehicleData.Makes[fixtureIndex];
        result.Model = VehicleData.Models[fixtureIndex];
        result.Type = VehicleData.Types[fixtureIndex];
        result.Fuel = VehicleData.Fuels[fixtureIndex];
        result.ModelYear = modelYear;
        result.Vin = VinAlgorithms.Generate(
            random,
            modelYear,
            context.CapturedOptions.MaximumCount,
            context.CapturedOptions.MaximumOutputSize);
        result.RegistrationPlate = RegistrationPlateUnchecked(random);
        result.Color = ChooseValue(random, VehicleData.Colors);
        result.Route = ChooseValue(random, VehicleData.Routes);
        result.TransitMode = ChooseValue(random, VehicleData.TransitModes);
        result.IsSynthetic = true;
        result.VinIsAssigned = false;
        result.RegistrationIsAssigned = false;
        return result;
    }

    static string FixtureValue(
        GenerationContext context,
        FakeRandom random,
        int field,
        string outputName)
    {
        ValidateFixtureData(context);
        Guard.OutputSize(
            MaximumFixtureLength(field),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        int index = random.IntFromMaximumExclusive(VehicleData.Makes.Length);
        return FixtureField(index, field);
    }

    static string ReviewedValue(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string dataName,
        string outputName)
    {
        ValidateValues(context, values, dataName);
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return ChooseValue(random, values);
    }

    static string RegistrationPlateUnchecked(FakeRandom random)
    {
        return "TEST-" + random.IntFromMaximumExclusive(10000).ToString(
            "D4",
            CultureInfo.InvariantCulture);
    }

    static int ReferenceModelYear(int value)
    {
        if (value < VinAlgorithms.MinimumModelYear)
        {
            return VinAlgorithms.MinimumModelYear;
        }
        if (value > VinAlgorithms.MaximumModelYear)
        {
            return VinAlgorithms.MaximumModelYear;
        }
        return value;
    }

    static int RecentModelYear(FakeRandom random, int referenceModelYear)
    {
        int maximumAge = referenceModelYear - VinAlgorithms.MinimumModelYear;
        if (maximumAge > 15)
        {
            maximumAge = 15;
        }
        return referenceModelYear - random.IntFromMaximumExclusive(maximumAge + 1);
    }

    static string ChooseValue(FakeRandom random, string[] values)
    {
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static string FixtureField(int index, int field)
    {
        switch (field)
        {
            case 0:
                return VehicleData.Makes[index];
            case 1:
                return VehicleData.Models[index];
            case 2:
                return VehicleData.Types[index];
            default:
                return VehicleData.Fuels[index];
        }
    }

    static int MaximumFixtureLength(int field)
    {
        switch (field)
        {
            case 0:
                return MaximumLength(VehicleData.Makes);
            case 1:
                return MaximumLength(VehicleData.Models);
            case 2:
                return MaximumLength(VehicleData.Types);
            default:
                return MaximumLength(VehicleData.Fuels);
        }
    }

    static void ValidateFixtureData(GenerationContext context)
    {
        if (VehicleData.Makes.Length != VehicleData.Models.Length ||
            VehicleData.Makes.Length != VehicleData.Types.Length ||
            VehicleData.Makes.Length != VehicleData.Fuels.Length)
        {
            throw FakeException.Create(
                "The project-authored vehicle table has inconsistent column lengths.");
        }
        ValidateValues(context, VehicleData.Makes, "vehicleMakeData");
        ValidateValues(context, VehicleData.Models, "vehicleModelData");
        ValidateValues(context, VehicleData.Types, "vehicleTypeData");
        ValidateValues(context, VehicleData.Fuels, "vehicleFuelData");
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
                    "A project-authored vehicle data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumRecordSize()
    {
        return MaximumLength(VehicleData.Makes) +
            MaximumLength(VehicleData.Models) +
            MaximumLength(VehicleData.Types) +
            MaximumLength(VehicleData.Fuels) + VinAlgorithms.Length +
            RegistrationPlateLength + MaximumLength(VehicleData.Colors) +
            MaximumLength(VehicleData.Routes) +
            MaximumLength(VehicleData.TransitModes) + 4;
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
