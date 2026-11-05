using fake;

namespace fake.Tests;

[TestFixture]
public class ProfileProviderTests
{
    [Test]
    public void RootReturnsOneCachedProfileProvider()
    {
        Fake value = Create(13400);
        ProfileProvider first = value.Profile();
        ProfileProvider second = value.Profile();

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void PersonAndHouseholdProfilesAreDeterministicAndCorrelated()
    {
        PersonProfile person = Create(13401).Profile().Person();
        PersonProfile samePerson = Create(13401).Profile().Person();
        HouseholdProfile household = Create(13402).Profile().Household();
        HouseholdProfile sameHousehold = Create(13402).Profile().Household();

        Assert.Multiple(() =>
        {
            Assert.That(person.FullName, Is.EqualTo(samePerson.FullName));
            Assert.That(person.BirthDate,
                Is.EqualTo(samePerson.BirthDate));
            Assert.That(person.Username, Is.EqualTo(samePerson.Username));
            Assert.That(person.Email, Is.EqualTo(samePerson.Email));
            Assert.That(person.FullName, Does.Contain(person.FirstName));
            Assert.That(person.FullName, Does.Contain(person.LastName));
            Assert.That(person.Email,
                Does.StartWith(person.Username + "@example."));
            Assert.That(person.Age, Is.InRange(18, 80));

            Assert.That(household.HouseholdType,
                Is.EqualTo(sameHousehold.HouseholdType));
            Assert.That(household.MemberCount,
                Is.EqualTo(sameHousehold.MemberCount));
            Assert.That(household.MemberCount,
                Is.EqualTo(household.Members.Length));
            Assert.That(household.MemberCount, Is.InRange(1, 5));
        });

        int index = 0;
        while (index < household.Members.Length)
        {
            PersonProfile member = household.Members[index];
            PersonProfile sameMember = sameHousehold.Members[index];
            Assert.Multiple(() =>
            {
                Assert.That(member.FullName,
                    Is.EqualTo(sameMember.FullName));
                Assert.That(member.Address,
                    Is.EqualTo(household.Address));
                Assert.That(member.Phone, Is.EqualTo(household.Phone));
                Assert.That(member.Locale, Is.EqualTo(household.Locale));
                Assert.That(member.TimeZoneId,
                    Is.EqualTo(household.TimeZoneId));
            });
            index++;
        }
    }

    [Test]
    public void CompanyAndProductProfilesPreserveSafetyAndCorrelation()
    {
        CompanyProfile company = Create(13403).Profile().Company();
        CompanyProfile sameCompany = Create(13403).Profile().Company();
        ProductProfile product = Create(13404).Profile().Product();
        ProductProfile sameProduct = Create(13404).Profile().Product();

        Assert.Multiple(() =>
        {
            Assert.That(company.Name, Is.EqualTo(sameCompany.Name));
            Assert.That(company.Domain, Is.EqualTo(sameCompany.Domain));
            Assert.That(company.Email, Is.EqualTo(sameCompany.Email));
            Assert.That(company.Email,
                Is.EqualTo("contact@" + company.Domain));
            Assert.That(company.PhoneRegionCode,
                Is.EqualTo(company.Phone.ParsingMetadata.RegionCode));
            Assert.That(company.AddressFixtureCountryCode,
                Is.EqualTo(company.Address.CountryCode));
            Assert.That(company.DomainIsReservedExample, Is.True);
            Assert.That(company.EmailIsReservedExample, Is.True);
            Assert.That(company.IsSynthetic, Is.True);
            Assert.That(company.RepresentsRegisteredEntity, Is.False);
            Assert.That(company.RepresentsReachableContact, Is.False);

            Assert.That(product.Name, Is.EqualTo(sameProduct.Name));
            Assert.That(product.Sku, Is.EqualTo(sameProduct.Sku));
            Assert.That(product.Gtin, Is.EqualTo(sameProduct.Gtin));
            Assert.That(product.Description,
                Does.Contain(product.Component.ToLowerInvariant()));
            Assert.That(product.Description,
                Does.Contain(product.Material.ToLowerInvariant()));
            Assert.That(product.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(product.IsSynthetic, Is.True);
            Assert.That(product.RepresentsRealProduct, Is.False);
            Assert.That(product.IdentifiersAreAllocated, Is.False);
            Assert.That(product.RepresentsMarketPrice, Is.False);
        });
    }

    [Test]
    public void OrderAndPaymentProfilesPreserveArithmeticAndTimelines()
    {
        OrderProfile order = Create(13405).Profile().Order();
        OrderProfile sameOrder = Create(13405).Profile().Order();
        PaymentProfile payment = Create(13406).Profile().Payment();
        PaymentProfile samePayment = Create(13406).Profile().Payment();
        decimal lineTotal = decimal.Zero;
        int quantity = 0;
        int index = 0;
        while (index < order.Items.Length)
        {
            OrderLineRecord item = order.Items[index];
            Assert.Multiple(() =>
            {
                Assert.That(item.Sku,
                    Is.EqualTo(sameOrder.Items[index].Sku));
                Assert.That(item.CurrencyCode,
                    Is.EqualTo(order.CurrencyCode));
                Assert.That(item.Total, Is.EqualTo(
                    item.Subtotal - item.DiscountAmount + item.TaxAmount));
                Assert.That(item.IsSynthetic, Is.True);
            });
            lineTotal += item.Total;
            quantity += item.Quantity;
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(order.Number, Is.EqualTo(sameOrder.Number));
            Assert.That(order.Total, Is.EqualTo(sameOrder.Total));
            Assert.That(order.Status, Is.EqualTo(sameOrder.Status));
            Assert.That(order.LineCount, Is.EqualTo(order.Items.Length));
            Assert.That(order.Quantity, Is.EqualTo(quantity));
            Assert.That(order.Total,
                Is.EqualTo(lineTotal + order.ShippingAmount));
            Assert.That(order.IsSynthetic, Is.True);
            Assert.That(order.IsSubmitted, Is.False);

            Assert.That(payment.Reference,
                Is.EqualTo(samePayment.Reference));
            Assert.That(payment.Amount, Is.EqualTo(samePayment.Amount));
            Assert.That(payment.Type, Is.EqualTo(samePayment.Type));
            Assert.That(payment.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(payment.Amount, Is.EqualTo(
                payment.Subtotal + payment.TaxAmount +
                payment.FeeAmount - payment.DiscountAmount));
            Assert.That(payment.BalanceAfter, Is.EqualTo(
                payment.BalanceBefore - payment.Amount));
            Assert.That(payment.IsSynthetic, Is.True);
            Assert.That(payment.IsAuthorized, Is.False);
            Assert.That(payment.IsSettled, Is.False);
        });
    }

    [Test]
    public void VehicleAndFlightProfilesPreserveIdentifiersAndRoutes()
    {
        VehicleProfile vehicle = Create(13407).Profile().Vehicle();
        VehicleProfile sameVehicle = Create(13407).Profile().Vehicle();
        FlightProfile flight = Create(13408).Profile().Flight();
        FlightProfile sameFlight = Create(13408).Profile().Flight();
        IdentifierProvider identifiers = Fake.Create(1).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(vehicle.Make, Is.EqualTo(sameVehicle.Make));
            Assert.That(vehicle.Model, Is.EqualTo(sameVehicle.Model));
            Assert.That(vehicle.Vin, Is.EqualTo(sameVehicle.Vin));
            Assert.That(identifiers.ValidateVin(vehicle.Vin), Is.True);
            Assert.That(identifiers.VinModelYear(vehicle.Vin),
                Is.EqualTo(vehicle.ModelYear));
            Assert.That(vehicle.RegistrationPlate,
                Does.StartWith("TEST-"));
            Assert.That(vehicle.IsSynthetic, Is.True);
            Assert.That(vehicle.VinIsAssigned, Is.False);
            Assert.That(vehicle.RegistrationIsAssigned, Is.False);

            Assert.That(flight.FlightNumber,
                Is.EqualTo(sameFlight.FlightNumber));
            Assert.That(flight.Route, Is.EqualTo(sameFlight.Route));
            Assert.That(flight.Departure,
                Is.EqualTo(sameFlight.Departure));
            Assert.That(flight.FlightNumber,
                Does.StartWith(flight.AirlineCode + " "));
            Assert.That(flight.Route, Is.EqualTo(
                flight.OriginAirportCode + "-" +
                flight.DestinationAirportCode));
            Assert.That(flight.Arrival, Is.EqualTo(
                flight.Departure.AddMinutes(flight.DurationMinutes)));
            Assert.That(flight.IsSynthetic, Is.True);
            Assert.That(flight.AirlineCodeIsAssigned, Is.False);
            Assert.That(flight.AirportCodesAreAssigned, Is.False);
            Assert.That(flight.RecordLocatorIsBooked, Is.False);
        });
    }

    [Test]
    public void DeviceAndStudentProfilesPreserveTheirCorrelations()
    {
        Fake studentFake = Create(13410);
        DeviceProfile device = Create(13409).Profile().Device();
        DeviceProfile sameDevice = Create(13409).Profile().Device();
        StudentProfile student = studentFake.Profile().Student();
        StudentProfile sameStudent = Create(13410).Profile().Student();
        EducationLocaleData education = EducationLocaleDataParser.Resolve(
            studentFake.Context);
        EducationSubjectData subject = education.Subjects.Single(
            item => item.Value == student.Subject);

        Assert.Multiple(() =>
        {
            Assert.That(device.Model, Is.EqualTo(sameDevice.Model));
            Assert.That(device.HardwareToken,
                Is.EqualTo(sameDevice.HardwareToken));
            Assert.That(device.PushToken,
                Is.EqualTo(sameDevice.PushToken));
            Assert.That(device.ScreenResolution, Is.EqualTo(
                device.ScreenWidth + "x" + device.ScreenHeight));
            Assert.That(device.UsesAndroid,
                Is.EqualTo(device.OperatingSystem == "Android"));
            Assert.That(device.HardwareToken,
                Does.Match("^[0-9a-f]{32}$"));
            Assert.That(device.PushToken,
                Does.Match("^[A-Za-z0-9_-]{64}$"));

            Assert.That(student.StudentId,
                Is.EqualTo(sameStudent.StudentId));
            Assert.That(student.Institution,
                Is.EqualTo(sameStudent.Institution));
            Assert.That(subject.Courses.Select(item => item.Value),
                Does.Contain(student.Course));
            Assert.That(subject.Degrees.Select(item => item.Value),
                Does.Contain(student.Degree));
            Assert.That(student.StudentId, Is.Not.Empty);
        });
    }

    [Test]
    public void ProfileGenerationDoesNotPerturbDomainProviderStreams()
    {
        Fake inspected = Create(13411);
        Fake untouched = Create(13411);

        inspected.Profile().Person();
        inspected.Profile().Household();
        inspected.Profile().Company();
        inspected.Profile().Product();
        inspected.Profile().Order();
        inspected.Profile().Payment();
        inspected.Profile().Vehicle();
        inspected.Profile().Flight();
        inspected.Profile().Device();
        inspected.Profile().Student();

        Assert.Multiple(() =>
        {
            Assert.That(inspected.Person().FullName(),
                Is.EqualTo(untouched.Person().FullName()));
            Assert.That(inspected.Company().Name(),
                Is.EqualTo(untouched.Company().Name()));
            Assert.That(inspected.Commerce().ProductName(),
                Is.EqualTo(untouched.Commerce().ProductName()));
            Assert.That(inspected.Finance().TransactionReference(),
                Is.EqualTo(untouched.Finance().TransactionReference()));
            Assert.That(inspected.Vehicle().Make(),
                Is.EqualTo(untouched.Vehicle().Make()));
            Assert.That(inspected.Airline().AirlineName(),
                Is.EqualTo(untouched.Airline().AirlineName()));
            Assert.That(inspected.Device().Model(),
                Is.EqualTo(untouched.Device().Model()));
            Assert.That(inspected.Education().Institution(),
                Is.EqualTo(untouched.Education().Institution()));
        });
    }

    [Test]
    public void ProfileMethodsHonorConfiguredOutputLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Tiny(13412).Profile().Person(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13413).Profile().Household(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13414).Profile().Company(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13415).Profile().Product(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13416).Profile().Order(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13417).Profile().Payment(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13418).Profile().Vehicle(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13419).Profile().Flight(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13420).Profile().Device(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Tiny(13421).Profile().Student(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    static Fake Create(long seed)
    {
        FakeOptions options = FakeOptions.Create(seed);
        options.ReferenceTime = new DateTimeOffset(
            2045,
            6,
            7,
            8,
            9,
            10,
            TimeSpan.Zero);
        return Fake.Create(options);
    }

    static Fake Tiny(long seed)
    {
        FakeOptions options = FakeOptions.Create(seed);
        options.ReferenceTime = new DateTimeOffset(
            2045,
            6,
            7,
            8,
            9,
            10,
            TimeSpan.Zero);
        options.MaximumOutputSize = 1;
        return Fake.Create(options);
    }
}
