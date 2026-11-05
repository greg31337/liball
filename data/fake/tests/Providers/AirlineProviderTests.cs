using fake;

namespace fake.Tests;

[TestFixture]
public class AirlineProviderTests
{
    static Dictionary<string, string> Airlines = new Dictionary<string, string>
    {
        ["Example Aster Air"] = "X1",
        ["Fixture Beacon Airways"] = "X2",
        ["Sample Calder Regional"] = "X3",
        ["Test Meridian Flights"] = "X4",
        ["Demo Northstar Air"] = "X5",
        ["Mock Pine Aviation"] = "X6"
    };

    static Dictionary<string, string> Airports = new Dictionary<string, string>
    {
        ["Example City Airport"] = "QAA",
        ["Sample Bay Airport"] = "QBB",
        ["Fixture Field"] = "QCC",
        ["North Test Airport"] = "QDD",
        ["South Demo Airport"] = "QEE",
        ["Harbor Example Airport"] = "QFF",
        ["Cedar Sample Airport"] = "QGG",
        ["River Fixture Airport"] = "QHH"
    };

    static Dictionary<string, string> Aircraft = new Dictionary<string, string>
    {
        ["Example Aster 100"] = "X100",
        ["Fixture Beacon 200"] = "X200",
        ["Sample Calder Regional 50"] = "X050",
        ["Test Meridian Twinjet"] = "X300",
        ["Demo Northstar Turboprop"] = "X400",
        ["Mock Pine Shuttle"] = "X500"
    };

    static string[] Cabins = new string[]
    {
        "economy", "premium economy", "business", "first"
    };

    [Test]
    public void AviationComponentsAreDeterministicReviewedFixtures()
    {
        const long seed = 12400;
        AirlineProvider first = Fake.Create(seed).Airline();
        AirlineProvider second = Fake.Create(seed).Airline();
        HashSet<string> airlineNames = new HashSet<string>();
        HashSet<string> airlineCodes = new HashSet<string>();
        HashSet<string> airportNames = new HashSet<string>();
        HashSet<string> airportCodes = new HashSet<string>();
        HashSet<string> aircraftNames = new HashSet<string>();
        HashSet<string> aircraftCodes = new HashSet<string>();
        HashSet<string> cabins = new HashSet<string>();

        int index = 0;
        while (index < 512)
        {
            string airline = first.AirlineName();
            string airlineCode = first.AirlineCode();
            string airport = first.AirportName();
            string airportCode = first.AirportCode();
            string aircraft = first.AircraftName();
            string aircraftCode = first.AircraftCode();
            string cabin = first.Cabin();

            Assert.Multiple(() =>
            {
                Assert.That(airline, Is.EqualTo(second.AirlineName()));
                Assert.That(airlineCode, Is.EqualTo(second.AirlineCode()));
                Assert.That(airport, Is.EqualTo(second.AirportName()));
                Assert.That(airportCode, Is.EqualTo(second.AirportCode()));
                Assert.That(aircraft, Is.EqualTo(second.AircraftName()));
                Assert.That(aircraftCode, Is.EqualTo(second.AircraftCode()));
                Assert.That(cabin, Is.EqualTo(second.Cabin()));
                Assert.That(Airlines.ContainsKey(airline), Is.True);
                Assert.That(Airlines.ContainsValue(airlineCode), Is.True);
                Assert.That(Airports.ContainsKey(airport), Is.True);
                Assert.That(Airports.ContainsValue(airportCode), Is.True);
                Assert.That(Aircraft.ContainsKey(aircraft), Is.True);
                Assert.That(Aircraft.ContainsValue(aircraftCode), Is.True);
                Assert.That(cabin, Is.AnyOf(Cabins));
            });
            airlineNames.Add(airline);
            airlineCodes.Add(airlineCode);
            airportNames.Add(airport);
            airportCodes.Add(airportCode);
            aircraftNames.Add(aircraft);
            aircraftCodes.Add(aircraftCode);
            cabins.Add(cabin);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(airlineNames, Is.EquivalentTo(Airlines.Keys));
            Assert.That(airlineCodes, Is.EquivalentTo(Airlines.Values));
            Assert.That(airportNames, Is.EquivalentTo(Airports.Keys));
            Assert.That(airportCodes, Is.EquivalentTo(Airports.Values));
            Assert.That(aircraftNames, Is.EquivalentTo(Aircraft.Keys));
            Assert.That(aircraftCodes, Is.EquivalentTo(Aircraft.Values));
            Assert.That(cabins, Is.EquivalentTo(Cabins));
        });
    }

    [Test]
    public void PairedNamesAndCodesCorrelateAtTheSameSeed()
    {
        int seed = 12401;
        while (seed < 12501)
        {
            string airlineName = Fake.Create(seed).Airline().AirlineName();
            string airlineCode = Fake.Create(seed).Airline().AirlineCode();
            string airportName = Fake.Create(seed).Airline().AirportName();
            string airportCode = Fake.Create(seed).Airline().AirportCode();
            string aircraftName = Fake.Create(seed).Airline().AircraftName();
            string aircraftCode = Fake.Create(seed).Airline().AircraftCode();

            Assert.Multiple(() =>
            {
                Assert.That(airlineCode, Is.EqualTo(Airlines[airlineName]));
                Assert.That(airportCode, Is.EqualTo(Airports[airportName]));
                Assert.That(aircraftCode, Is.EqualTo(Aircraft[aircraftName]));
                Assert.That(Fake.Create(seed).Airline().Airline(),
                    Is.EqualTo(airlineName));
                Assert.That(Fake.Create(seed).Airline().Airport(),
                    Is.EqualTo(airportName));
                Assert.That(Fake.Create(seed).Airline().Aircraft(),
                    Is.EqualTo(aircraftName));
            });
            seed++;
        }
    }

    [Test]
    public void FlightsRoutesAndLocatorsHaveSyntheticShapes()
    {
        const long seed = 12402;
        AirlineProvider first = Fake.Create(seed).Airline();
        AirlineProvider second = Fake.Create(seed).Airline();
        HashSet<string> locators = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string flightNumber = first.FlightNumber();
            string route = first.Route();
            string seat = first.Seat();
            string locator = first.RecordLocator();
            string[] endpoints = route.Split('-');

            Assert.Multiple(() =>
            {
                Assert.That(flightNumber,
                    Is.EqualTo(second.FlightNumber()));
                Assert.That(route, Is.EqualTo(second.Route()));
                Assert.That(seat, Is.EqualTo(second.Seat()));
                Assert.That(locator, Is.EqualTo(second.RecordLocator()));
                Assert.That(flightNumber,
                    Does.Match("^X[1-6] [1-9][0-9]{0,3}$"));
                Assert.That(route,
                    Does.Match("^Q[A-H]{2}-Q[A-H]{2}$"));
                Assert.That(endpoints, Has.Length.EqualTo(2));
                Assert.That(endpoints[0], Is.Not.EqualTo(endpoints[1]));
                Assert.That(Airports.ContainsValue(endpoints[0]), Is.True);
                Assert.That(Airports.ContainsValue(endpoints[1]), Is.True);
                Assert.That(seat, Does.Match(
                    "^(?:[1-9]|[1-3][0-9]|40)[A-F]$"));
                Assert.That(locator, Does.Match(
                    "^[A-HJ-NP-Z2-9]{6}$"));
            });
            locators.Add(locator);
            index++;
        }

        Assert.That(locators, Has.Count.EqualTo(256));
    }

    [Test]
    public void RequestedCabinsConstrainSeatRowsAndLetters()
    {
        AirlineProvider airline = Fake.Create(12403).Airline();

        int index = 0;
        while (index < 128)
        {
            AssertSeat(airline.SeatFromCabin("economy"), 20, 40, 'F');
            AssertSeat(airline.SeatFromCabin("Premium Economy"), 10, 19, 'F');
            AssertSeat(airline.SeatFromCabin("business"), 5, 9, 'D');
            AssertSeat(airline.SeatFromCabin("FIRST"), 1, 4, 'D');
            index++;
        }
    }

    [Test]
    public void FlightProfilesKeepCodesRouteSeatAndScheduleCorrelated()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2036,
            7,
            8,
            9,
            10,
            11,
            TimeSpan.FromHours(5.5));
        FakeOptions firstOptions = FakeOptions.Create(12404);
        firstOptions.ReferenceTime = reference;
        FakeOptions secondOptions = FakeOptions.Create(12404);
        secondOptions.ReferenceTime = reference;
        AirlineProvider first = Fake.Create(firstOptions).Airline();
        AirlineProvider second = Fake.Create(secondOptions).Airline();

        int index = 0;
        while (index < 512)
        {
            FlightProfile value = first.Record();
            FlightProfile same = second.Record();

            Assert.Multiple(() =>
            {
                AssertProfileEqual(value, same);
                Assert.That(value.AirlineCode,
                    Is.EqualTo(Airlines[value.AirlineName]));
                Assert.That(value.OriginAirportCode,
                    Is.EqualTo(Airports[value.OriginAirportName]));
                Assert.That(value.DestinationAirportCode,
                    Is.EqualTo(Airports[value.DestinationAirportName]));
                Assert.That(value.OriginAirportCode,
                    Is.Not.EqualTo(value.DestinationAirportCode));
                Assert.That(value.AircraftCode,
                    Is.EqualTo(Aircraft[value.AircraftName]));
                Assert.That(value.FlightNumber,
                    Does.StartWith(value.AirlineCode + " "));
                Assert.That(value.Route, Is.EqualTo(
                    value.OriginAirportCode + "-" +
                    value.DestinationAirportCode));
                Assert.That(value.Cabin, Is.AnyOf(Cabins));
                Assert.That(value.RecordLocator,
                    Does.Match("^[A-HJ-NP-Z2-9]{6}$"));
                Assert.That(value.DurationMinutes, Is.InRange(60, 720));
                Assert.That(value.Arrival, Is.EqualTo(
                    value.Departure.AddMinutes(value.DurationMinutes)));
                Assert.That(value.Departure.Offset,
                    Is.EqualTo(TimeSpan.Zero));
                Assert.That(value.Arrival, Is.LessThanOrEqualTo(reference));
                Assert.That(value.Departure,
                    Is.GreaterThanOrEqualTo(reference.AddDays(-31.0)));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.AirlineCodeIsAssigned, Is.False);
                Assert.That(value.AirportCodesAreAssigned, Is.False);
                Assert.That(value.RecordLocatorIsBooked, Is.False);
            });
            AssertSeatForCabin(value.Seat, value.Cabin);
            index++;
        }

        AssertProfileEqual(
            Fake.Create(firstOptions).Airline().Flight(),
            Fake.Create(firstOptions).Airline().Record());
    }

    [Test]
    public void ExtremeReferenceTimesStillProduceRepresentableSchedules()
    {
        FakeOptions minimumOptions = FakeOptions.Create(12405);
        minimumOptions.ReferenceTime = DateTimeOffset.MinValue;
        FlightProfile minimum = Fake.Create(minimumOptions).Airline().Record();
        FakeOptions maximumOptions = FakeOptions.Create(12405);
        maximumOptions.ReferenceTime = DateTimeOffset.MaxValue;
        FlightProfile maximum = Fake.Create(maximumOptions).Airline().Record();

        Assert.Multiple(() =>
        {
            Assert.That(minimum.Departure,
                Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(minimum.Arrival,
                Is.GreaterThan(minimum.Departure));
            Assert.That(maximum.Arrival,
                Is.LessThanOrEqualTo(DateTimeOffset.MaxValue));
            Assert.That(maximum.Arrival,
                Is.GreaterThan(maximum.Departure));
        });
    }

    [Test]
    public void LimitsAndInvalidCabinsFailBeforeAirlineEntropy()
    {
        const long seed = 12406;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        AirlineProvider tinyInspected = Fake.Create(tinyOptions).Airline();
        AirlineProvider tinyUntouched = Fake.Create(tinyOptions).Airline();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 3;
        AirlineProvider countInspected = Fake.Create(countOptions).Airline();
        AirlineProvider countUntouched = Fake.Create(countOptions).Airline();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.AirlineName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.AirlineCode(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.AirportName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.AirportCode(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.AircraftName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.AircraftCode(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.FlightNumber(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Seat(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.RecordLocator(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Cabin(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Route(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.AirlineName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.AirportName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.AircraftName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Cabin(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Route(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.SeatFromCabin(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => tinyInspected.SeatFromCabin("crew"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void AirlineGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12407);
        Fake untouched = Fake.Create(12407);

        int index = 0;
        while (index < 32)
        {
            inspected.Airline().AirlineName();
            inspected.Airline().AirlineCode();
            inspected.Airline().AirportName();
            inspected.Airline().AirportCode();
            inspected.Airline().AircraftName();
            inspected.Airline().AircraftCode();
            inspected.Airline().FlightNumber();
            inspected.Airline().Seat();
            inspected.Airline().RecordLocator();
            inspected.Airline().Cabin();
            inspected.Airline().Route();
            inspected.Airline().Record();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertSeatForCabin(string seat, string cabin)
    {
        if (cabin == "economy")
        {
            AssertSeat(seat, 20, 40, 'F');
        }
        else if (cabin == "premium economy")
        {
            AssertSeat(seat, 10, 19, 'F');
        }
        else if (cabin == "business")
        {
            AssertSeat(seat, 5, 9, 'D');
        }
        else
        {
            AssertSeat(seat, 1, 4, 'D');
        }
    }

    static void AssertSeat(
        string seat,
        int minimumRow,
        int maximumRow,
        char maximumLetter)
    {
        int row = int.Parse(seat.Substring(0, seat.Length - 1));
        char letter = seat[seat.Length - 1];
        Assert.Multiple(() =>
        {
            Assert.That(row, Is.InRange(minimumRow, maximumRow));
            Assert.That(letter, Is.InRange('A', maximumLetter));
        });
    }

    static void AssertProfileEqual(
        FlightProfile first,
        FlightProfile second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.AirlineName, Is.EqualTo(second.AirlineName));
            Assert.That(first.AirlineCode, Is.EqualTo(second.AirlineCode));
            Assert.That(first.OriginAirportName,
                Is.EqualTo(second.OriginAirportName));
            Assert.That(first.OriginAirportCode,
                Is.EqualTo(second.OriginAirportCode));
            Assert.That(first.DestinationAirportName,
                Is.EqualTo(second.DestinationAirportName));
            Assert.That(first.DestinationAirportCode,
                Is.EqualTo(second.DestinationAirportCode));
            Assert.That(first.AircraftName, Is.EqualTo(second.AircraftName));
            Assert.That(first.AircraftCode, Is.EqualTo(second.AircraftCode));
            Assert.That(first.FlightNumber, Is.EqualTo(second.FlightNumber));
            Assert.That(first.Seat, Is.EqualTo(second.Seat));
            Assert.That(first.RecordLocator,
                Is.EqualTo(second.RecordLocator));
            Assert.That(first.Cabin, Is.EqualTo(second.Cabin));
            Assert.That(first.Route, Is.EqualTo(second.Route));
            Assert.That(first.Departure, Is.EqualTo(second.Departure));
            Assert.That(first.Arrival, Is.EqualTo(second.Arrival));
            Assert.That(first.DurationMinutes,
                Is.EqualTo(second.DurationMinutes));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.AirlineCodeIsAssigned,
                Is.EqualTo(second.AirlineCodeIsAssigned));
            Assert.That(first.AirportCodesAreAssigned,
                Is.EqualTo(second.AirportCodesAreAssigned));
            Assert.That(first.RecordLocatorIsBooked,
                Is.EqualTo(second.RecordLocatorIsBooked));
        });
    }
}
