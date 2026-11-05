using System;
using System.Globalization;

namespace fake;

internal static class AirlineGenerator
{
    const string RecordLocatorAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    const int RecordLocatorLength = 6;
    const int MaximumFlightNumberLength = 7;
    const int MaximumSeatLength = 3;
    const int RouteLength = 7;
    const int MaximumDurationMinutes = 720;
    const int MinimumDurationMinutes = 60;

    internal static string AirlineName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAirlineData(context);
        return PairedValue(
            context,
            random,
            AirlineData.AirlineNames,
            "airlineName");
    }

    internal static string AirlineCode(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAirlineData(context);
        return PairedValue(
            context,
            random,
            AirlineData.AirlineCodes,
            "airlineCode");
    }

    internal static string AirportName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAirportData(context);
        return PairedValue(
            context,
            random,
            AirlineData.AirportNames,
            "airportName");
    }

    internal static string AirportCode(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAirportData(context);
        return PairedValue(
            context,
            random,
            AirlineData.AirportCodes,
            "airportCode");
    }

    internal static string AircraftName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAircraftData(context);
        return PairedValue(
            context,
            random,
            AirlineData.AircraftNames,
            "aircraftName");
    }

    internal static string AircraftCode(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAircraftData(context);
        return PairedValue(
            context,
            random,
            AirlineData.AircraftCodes,
            "aircraftCode");
    }

    internal static string FlightNumber(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAirlineData(context);
        Guard.OutputSize(
            MaximumFlightNumberLength,
            context.CapturedOptions.MaximumOutputSize,
            "flightNumber");
        int airlineIndex = random.IntFromMaximumExclusive(AirlineData.AirlineCodes.Length);
        return FlightNumberUnchecked(random, airlineIndex);
    }

    internal static string Seat(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateCabinData(context);
        Guard.OutputSize(
            MaximumSeatLength,
            context.CapturedOptions.MaximumOutputSize,
            "seat");
        int cabinIndex = random.IntFromMaximumExclusive(AirlineData.Cabins.Length);
        return SeatUnchecked(random, cabinIndex);
    }

    internal static string Seat(
        GenerationContext context,
        FakeRandom random,
        string cabin)
    {
        int cabinIndex = CabinIndex(context, cabin);
        Guard.OutputSize(
            MaximumSeatLength,
            context.CapturedOptions.MaximumOutputSize,
            "seat");
        return SeatUnchecked(random, cabinIndex);
    }

    internal static string RecordLocator(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            RecordLocatorLength,
            context.CapturedOptions.MaximumOutputSize,
            "recordLocator");
        return RecordLocatorUnchecked(random);
    }

    internal static string Cabin(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateCabinData(context);
        return PairedValue(
            context,
            random,
            AirlineData.Cabins,
            "cabin");
    }

    internal static string Route(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAirportData(context);
        Guard.OutputSize(
            RouteLength,
            context.CapturedOptions.MaximumOutputSize,
            "flightRoute");
        int originIndex = random.IntFromMaximumExclusive(AirlineData.AirportCodes.Length);
        int destinationIndex = DistinctAirportIndex(random, originIndex);
        return RouteUnchecked(originIndex, destinationIndex);
    }

    internal static FlightProfile Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAirlineData(context);
        ValidateAirportData(context);
        ValidateAircraftData(context);
        ValidateCabinData(context);
        Guard.OutputSize(
            MaximumProfileSize(),
            context.CapturedOptions.MaximumOutputSize,
            "flightProfile");
        int airlineIndex = random.IntFromMaximumExclusive(AirlineData.AirlineNames.Length);
        int originIndex = random.IntFromMaximumExclusive(AirlineData.AirportNames.Length);
        int destinationIndex = DistinctAirportIndex(random, originIndex);
        int aircraftIndex = random.IntFromMaximumExclusive(AirlineData.AircraftNames.Length);
        int cabinIndex = random.IntFromMaximumExclusive(AirlineData.Cabins.Length);
        int durationMinutes = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            MinimumDurationMinutes,
            MaximumDurationMinutes,
            true);
        DateTimeOffset departure = Departure(context, random);
        FlightProfile result = new FlightProfile();
        result.AirlineName = AirlineData.AirlineNames[airlineIndex];
        result.AirlineCode = AirlineData.AirlineCodes[airlineIndex];
        result.OriginAirportName = AirlineData.AirportNames[originIndex];
        result.OriginAirportCode = AirlineData.AirportCodes[originIndex];
        result.DestinationAirportName = AirlineData.AirportNames[
            destinationIndex];
        result.DestinationAirportCode = AirlineData.AirportCodes[
            destinationIndex];
        result.AircraftName = AirlineData.AircraftNames[aircraftIndex];
        result.AircraftCode = AirlineData.AircraftCodes[aircraftIndex];
        result.FlightNumber = FlightNumberUnchecked(random, airlineIndex);
        result.Seat = SeatUnchecked(random, cabinIndex);
        result.RecordLocator = RecordLocatorUnchecked(random);
        result.Cabin = AirlineData.Cabins[cabinIndex];
        result.Route = RouteUnchecked(originIndex, destinationIndex);
        result.Departure = departure;
        result.Arrival = departure.AddMinutes(durationMinutes);
        result.DurationMinutes = durationMinutes;
        result.IsSynthetic = true;
        result.AirlineCodeIsAssigned = false;
        result.AirportCodesAreAssigned = false;
        result.RecordLocatorIsBooked = false;
        return result;
    }

    static string PairedValue(
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

    static string FlightNumberUnchecked(FakeRandom random, int airlineIndex)
    {
        return AirlineData.AirlineCodes[airlineIndex] + " " + random.IntFromMinimumAndMaximumAndMaximumInclusive(
            1,
            9999,
            true).ToString(CultureInfo.InvariantCulture);
    }

    static string SeatUnchecked(FakeRandom random, int cabinIndex)
    {
        int minimumRow = 20;
        int maximumRow = 40;
        int letterCount = 6;
        if (cabinIndex == 1)
        {
            minimumRow = 10;
            maximumRow = 19;
        }
        else if (cabinIndex == 2)
        {
            minimumRow = 5;
            maximumRow = 9;
            letterCount = 4;
        }
        else if (cabinIndex == 3)
        {
            minimumRow = 1;
            maximumRow = 4;
            letterCount = 4;
        }
        int row = random.IntFromMinimumAndMaximumAndMaximumInclusive(minimumRow, maximumRow, true);
        char letter = (char)('A' + random.IntFromMaximumExclusive(letterCount));
        return row.ToString(CultureInfo.InvariantCulture) + letter;
    }

    static string RecordLocatorUnchecked(FakeRandom random)
    {
        char[] result = new char[RecordLocatorLength];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = RecordLocatorAlphabet[
                random.IntFromMaximumExclusive(RecordLocatorAlphabet.Length)];
            index = index + 1;
        }
        return new string(result);
    }

    static int DistinctAirportIndex(FakeRandom random, int originIndex)
    {
        int offset = random.IntFromMinimumAndMaximumAndMaximumInclusive(1, AirlineData.AirportCodes.Length - 1, true);
        return (originIndex + offset) % AirlineData.AirportCodes.Length;
    }

    static string RouteUnchecked(int originIndex, int destinationIndex)
    {
        return AirlineData.AirportCodes[originIndex] + "-" +
            AirlineData.AirportCodes[destinationIndex];
    }

    static DateTimeOffset Departure(
        GenerationContext context,
        FakeRandom random)
    {
        long referenceTicks = context.ReferenceTime().UtcTicks;
        long durationTicks = TimeSpan.FromMinutes(
            MaximumDurationMinutes).Ticks;
        long latestTicks = DateTimeOffset.MinValue.UtcTicks;
        if (referenceTicks >= DateTimeOffset.MinValue.UtcTicks + durationTicks)
        {
            latestTicks = referenceTicks - durationTicks;
        }
        long windowTicks = TimeSpan.FromDays(30.0).Ticks;
        long minimumTicks = DateTimeOffset.MinValue.UtcTicks;
        if (latestTicks >= DateTimeOffset.MinValue.UtcTicks + windowTicks)
        {
            minimumTicks = latestTicks - windowTicks;
        }
        if (minimumTicks == latestTicks)
        {
            return new DateTimeOffset(latestTicks, TimeSpan.Zero);
        }
        return new DateTimeOffset(
            random.LongFromMinimumAndMaximumAndMaximumInclusive(minimumTicks, latestTicks, true),
            TimeSpan.Zero);
    }

    static int CabinIndex(GenerationContext context, string cabin)
    {
        Guard.NotNull(cabin, "cabin");
        Guard.Count(
            cabin.Length,
            context.CapturedOptions.MaximumCount,
            "cabin");
        ValidateCabinData(context);
        int index = 0;
        while (index < AirlineData.Cabins.Length)
        {
            if (string.Equals(
                cabin,
                AirlineData.Cabins[index],
                StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index = index + 1;
        }
        throw new ArgumentException(
            "cabin is not a supported cabin category.",
            "cabin");
    }

    static void ValidateAirlineData(GenerationContext context)
    {
        ValidatePairs(
            context,
            AirlineData.AirlineNames,
            AirlineData.AirlineCodes,
            "airlineData");
    }

    static void ValidateAirportData(GenerationContext context)
    {
        ValidatePairs(
            context,
            AirlineData.AirportNames,
            AirlineData.AirportCodes,
            "airportData");
        if (AirlineData.AirportCodes.Length < 2)
        {
            throw FakeException.Create(
                "The project-authored airport table needs two or more entries.");
        }
    }

    static void ValidateAircraftData(GenerationContext context)
    {
        ValidatePairs(
            context,
            AirlineData.AircraftNames,
            AirlineData.AircraftCodes,
            "aircraftData");
    }

    static void ValidateCabinData(GenerationContext context)
    {
        ValidateValues(context, AirlineData.Cabins, "cabinData");
    }

    static void ValidatePairs(
        GenerationContext context,
        string[] names,
        string[] codes,
        string dataName)
    {
        if (names.Length != codes.Length)
        {
            throw FakeException.Create(
                "A project-authored aviation table has inconsistent column lengths.");
        }
        ValidateValues(context, names, dataName);
        ValidateValues(context, codes, dataName);
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
                    "A project-authored aviation data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumProfileSize()
    {
        return MaximumLength(AirlineData.AirlineNames) +
            MaximumLength(AirlineData.AirlineCodes) +
            MaximumLength(AirlineData.AirportNames) * 2 +
            MaximumLength(AirlineData.AirportCodes) * 2 +
            MaximumLength(AirlineData.AircraftNames) +
            MaximumLength(AirlineData.AircraftCodes) +
            MaximumFlightNumberLength + MaximumSeatLength +
            RecordLocatorLength + MaximumLength(AirlineData.Cabins) +
            RouteLength + 7;
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
