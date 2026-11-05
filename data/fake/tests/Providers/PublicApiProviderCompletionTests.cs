using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PublicApiProviderCompletionTests
{
    [Test]
    public void AddressAndCommerceConvenienceOperationsPreserveTheirContracts()
    {
        Fake value = Fake.Create(15100);
        string address = value.Address().SingleLineAddress();
        CommerceProvider commerce = value.Commerce();
        string promotion = commerce.PromotionName();
        int discount = commerce.DiscountPercent();
        CommerceLineItem yen = commerce.LineItemFromCurrencyCode("jpy");
        string sku = commerce.SkuFromMask("SKU-##-??-*");
        string order = commerce.OrderFromMask("ORD-####");
        string tracking = commerce.TrackingFromMask("TRK-????-####");

        Assert.Multiple(() =>
        {
            Assert.That(address, Is.Not.Empty);
            Assert.That(address, Does.Not.Contain("\r").And.Not.Contain("\n"));
            Assert.That(promotion, Is.Not.Empty);
            Assert.That(discount, Is.InRange(5, 50));
            Assert.That(discount % 5, Is.Zero);
            Assert.That(yen.CurrencyCode, Is.EqualTo("JPY"));
            Assert.That(yen.Quantity, Is.GreaterThan(0));
            Assert.That(yen.UnitPrice,
                Is.EqualTo(decimal.Truncate(yen.UnitPrice)));
            Assert.That(yen.Subtotal,
                Is.EqualTo(yen.UnitPrice * yen.Quantity));
            Assert.That(yen.IsSynthetic, Is.True);
            Assert.That(sku,
                Does.Match("^SKU-[0-9]{2}-[A-Za-z]{2}-[A-Za-z0-9]$"));
            Assert.That(order, Does.Match("^ORD-[0-9]{4}$"));
            Assert.That(tracking,
                Does.Match("^TRK-[A-Za-z]{4}-[0-9]{4}$"));
        });
    }

    [Test]
    public void DateIntervalConversionCopiesBoundsIntoAnIndependentRecord()
    {
        DateProvider date = Fake.Create(15101).Date();
        DateTimeOffset minimum = new DateTimeOffset(
            2026,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        DateTimeOffset maximum = minimum.AddDays(30);
        DateInterval source = date.OrderedInterval(minimum, maximum);
        DateTimeOffset originalStart = source.Start;
        DateTimeOffset originalEnd = source.End;

        IntervalRecord result = date.IntervalRecordFromDateInterval(source);
        source.Start = maximum.AddDays(1);
        source.End = maximum.AddDays(2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Start, Is.EqualTo(originalStart));
            Assert.That(result.End, Is.EqualTo(originalEnd));
            Assert.That(result.Start, Is.LessThanOrEqualTo(result.End));
        });
    }

    [Test]
    public void ExplicitMultiRowSqlDialectSerializesEveryRow()
    {
        Dictionary<string, object> first = new Dictionary<string, object>();
        first["id"] = 1;
        first["active"] = true;
        Dictionary<string, object> second = new Dictionary<string, object>();
        second["id"] = 2;
        second["active"] = false;

        string sql = Fake.Create(15102).Export()
            .SqlInsertFromTableAndRowsAndDialect(
                "items",
                new object[] { first, second },
                SqlDialect.MySql);

        Assert.That(sql, Is.EqualTo(
            "INSERT INTO `items` (`id`, `active`) VALUES " +
            "(1, 1), (2, 0);"));
    }

    [Test]
    public void FinanceConvenienceOperationsExposeCorrelatedResults()
    {
        FinanceProvider finance = Fake.Create(15103).Finance();
        decimal price = finance
            .PriceFromMinimumAndMaximumAndDecimalPlacesAndRoundingMode(
                1.001m,
                1.019m,
                2,
                MoneyRoundingMode.ToEven);
        MoneyRecord yen = finance.MoneyFromCurrencyCode("jpy");
        string display = finance
            .CurrencyDisplayFromAmountAndCurrencyCodeAndDecimalPlacesAndRoundingMode(
                1.005m,
                "USD",
                2,
                MoneyRoundingMode.AwayFromZero);
        string ibanPrint = finance.IbanPrint();
        IbanRecord iban = finance.IbanRecord();
        string accountType = finance.AccountTypeName();
        string bank = finance.BankName();
        string branch = finance.BranchName();
        CheckRecord check = finance.CheckFromCurrencyCode("EUR");
        PaymentRecord payment = finance.Payment();
        CardIssuer issuer = finance.CardIssuer();
        string invalidCard = finance.InvalidCardNumber();
        string maskedCard = finance.MaskedCard();
        string cvv = finance.Cvv();

        Assert.Multiple(() =>
        {
            Assert.That(price, Is.EqualTo(1.01m));
            Assert.That(yen.CurrencyCode, Is.EqualTo("JPY"));
            Assert.That(yen.DecimalPlaces, Is.Zero);
            Assert.That(yen.Amount, Is.EqualTo(decimal.Truncate(yen.Amount)));
            Assert.That(yen.Display, Is.Not.Empty);
            Assert.That(display, Is.EqualTo("$1.01"));
            Assert.That(ibanPrint.Replace(" ", string.Empty).Length,
                Is.InRange(15, 34));
            Assert.That(finance.IsIbanValid(
                ibanPrint.Replace(" ", string.Empty)), Is.True);
            Assert.That(iban.Print.Replace(" ", string.Empty),
                Is.EqualTo(iban.Electronic));
            Assert.That(iban.IsMod97Valid, Is.True);
            Assert.That(iban.IsSynthetic, Is.True);
            Assert.That(iban.RepresentsActiveAccount, Is.False);
            Assert.That(accountType, Is.Not.Empty);
            Assert.That(bank, Is.Not.Empty);
            Assert.That(branch, Is.Not.Empty);
            Assert.That(check.CurrencyCode, Is.EqualTo("EUR"));
            Assert.That(check.IsRoutingChecksumValid, Is.True);
            Assert.That(check.IsSynthetic, Is.True);
            Assert.That(check.IsNegotiable, Is.False);
            Assert.That(payment.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(payment.BalanceAfter,
                Is.EqualTo(payment.BalanceBefore - payment.Amount));
            Assert.That(payment.IsSynthetic, Is.True);
            Assert.That(payment.IsAuthorized, Is.False);
            Assert.That(payment.IsSettled, Is.False);
            Assert.That(Enum.IsDefined(typeof(CardIssuer), issuer), Is.True);
            Assert.That(finance.IsCardNumberValid(invalidCard), Is.False);
            Assert.That(finance.CardIssuerFromNumber(invalidCard),
                Is.TypeOf<CardIssuer>());
            Assert.That(maskedCard, Does.Match("^[*]+[0-9]{4}$"));
            Assert.That(cvv, Does.Match("^[0-9]{3,4}$"));
        });
    }

    [Test]
    public void InternetConvenienceOperationsReturnValidStructuredValues()
    {
        InternetProvider internet = Fake.Create(15104).Internet();
        string mask = internet.Ipv6Mask();
        string protocol = internet.TransportProtocol();
        HttpHeaderRecord header = internet.HttpHeaderRecord();

        Assert.Multiple(() =>
        {
            Assert.That(IPAddress.Parse(mask).AddressFamily,
                Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(internet.Protocols(), Does.Contain(protocol));
            Assert.That(header.Name, Is.Not.Empty);
            Assert.That(header.Value,
                Does.Not.Contain("\r").And.Not.Contain("\n"));
            Assert.That(header.Text,
                Is.EqualTo(header.Name + ": " + header.Value));
            Assert.That(header.IsSynthetic, Is.True);
            Assert.That(header.RepresentsLiveCredential, Is.False);
        });
    }

    [Test]
    public void ExplicitUriAndIpv6RangeOperationsRetainRequestedComponents()
    {
        InternetProvider internet = Fake.Create(15107).Internet();
        Uri uri = internet
            .UriFromIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(
                true,
                true,
                true,
                true);
        Ipv6Range loopback = internet.Ipv6RangeFromKind(
            NetworkAddressKind.Loopback);

        Assert.Multiple(() =>
        {
            Assert.That(uri.IsAbsoluteUri, Is.True);
            Assert.That(uri.Scheme, Is.EqualTo("https"));
            Assert.That(uri.UserInfo, Is.Not.Empty);
            Assert.That(uri.AbsolutePath, Is.Not.EqualTo("/"));
            Assert.That(uri.Query, Is.Not.Empty);
            Assert.That(uri.Fragment, Is.Not.Empty);
            Assert.That(loopback.Kind, Is.EqualTo(NetworkAddressKind.Loopback));
            Assert.That(loopback.PrefixLength, Is.EqualTo(128));
            Assert.That(loopback.NetworkAddress,
                Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(loopback.LastAddress,
                Is.EqualTo(loopback.NetworkAddress));
            Assert.That(loopback.AddressCount,
                Is.EqualTo(System.Numerics.BigInteger.One));
            Assert.That(loopback.Cidr, Does.EndWith("/128"));
        });
    }

    [Test]
    public void LocaleTraceAndUnnamedRegistrationExposeTheirResolvedSources()
    {
        Fake value = Fake.Create("fr-CA", 15105);
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            new LocaleFieldValue("fr", "audit.value", "parent"),
            new LocaleFieldValue("invariant", "audit.value", "base")
        };
        FallbackTrace trace = value.Locale()
            .FallbackTraceFromLocaleAndFieldAndValues(
                "fr-CA",
                "audit.value",
                values);

        const string json = "{" +
            "\"schemaVersion\":1," +
            "\"dataVersion\":\"1\"," +
            "\"locale\":\"fr-CA\"," +
            "\"fallbacks\":[\"fr\",\"invariant\"]," +
            "\"reviewStatus\":\"reviewed\"," +
            "\"provenance\":[{" +
            "\"id\":\"project\"," +
            "\"source\":\"Project authors\"," +
            "\"sourceVersion\":\"2026.08\"," +
            "\"license\":\"project\"," +
            "\"reviewer\":\"reviewer\"}]," +
            "\"fields\":[{" +
            "\"name\":\"audit.registered\"," +
            "\"provenance\":\"project\"," +
            "\"value\":\"registered\"}]}";
        using MemoryStream stream = new MemoryStream(
            Encoding.UTF8.GetBytes(json));
        value.Locale().Register(stream);
        JsonElement registered = (JsonElement)value.Locale()
            .Resolve("audit.registered");

        Assert.Multiple(() =>
        {
            Assert.That(trace.RequestedLocale, Is.EqualTo("fr-CA"));
            Assert.That(trace.Field, Is.EqualTo("audit.value"));
            Assert.That(trace.SourceLocale, Is.EqualTo("fr"));
            Assert.That(trace.AttemptedLocales,
                Is.EqualTo(new string[] { "fr-CA", "fr" }));
            Assert.That(trace.UsedFallback, Is.True);
            Assert.That(trace.Value, Is.EqualTo("parent"));
            Assert.That(registered.GetString(), Is.EqualTo("registered"));
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void PhoneConvenienceOperationsRetainSafeRangeAndShapeMetadata()
    {
        PhoneProvider phone = Fake.Create(15106).Phone();
        PhoneSafeRange[] first = phone.SafeRanges();
        PhoneSafeRange[] second = phone.SafeRanges();
        string areaCode = phone.AreaCode();
        string originalAuthority = second[0].Authority;
        first[0].Authority = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Empty);
            Assert.That(second[0].IsAuthoritative, Is.True);
            Assert.That(second[0].Authority, Is.EqualTo(originalAuthority));
            Assert.That(second[0].Authority, Is.Not.EqualTo("changed"));
            Assert.That(second[0].FirstSubscriber, Is.Not.Empty);
            Assert.That(second[0].LastSubscriber, Is.Not.Empty);
            Assert.That(areaCode, Is.Not.Empty);
            Assert.That(areaCode.All(character =>
                character >= '0' && character <= '9'), Is.True);
        });
    }

    [Test]
    public void ExplicitPhoneFormatsAndSpanValidatorsPreserveStructure()
    {
        PhoneProvider reserved = Fake.Create(15108).Phone();
        PhoneRecord reservedRecord = reserved.ReservedFictionRecordFromFormat(
            PhoneNumberFormat.E164);
        FakeOptions plausibleOptions = FakeOptions.Create(15109);
        plausibleOptions.SafetyMode = SafetyMode.SyntheticPlausible;
        PhoneProvider phone = Fake.Create(plausibleOptions).Phone();
        string e164 = phone.E164FromCategory(PhoneNumberCategory.TollFree);
        string international = phone.InternationalFromCategory(
            PhoneNumberCategory.Premium);
        string mobile = phone.MobileFromFormat(
            PhoneNumberFormat.International);
        string landline = phone.LandlineFromFormat(PhoneNumberFormat.E164);
        string iccid = phone.Iccid();
        string imsi = phone.ImsiFromMobileNetworkCodeLength(3);

        Assert.Multiple(() =>
        {
            Assert.That(reservedRecord.Number, Does.StartWith("+"));
            Assert.That(reservedRecord.SafetyMetadata.AppliedMode,
                Is.EqualTo(SafetyMode.Reserved));
            Assert.That(reservedRecord.Metadata.SafeReserved, Is.True);
            Assert.That(e164, Does.Match("^[+][0-9]{1,15}$"));
            Assert.That(international, Does.StartWith("+"));
            Assert.That(mobile, Does.StartWith("+"));
            Assert.That(landline, Does.Match("^[+][0-9]{1,15}$"));
            Assert.That(phone.ValidateIccidFromValue(iccid.AsSpan()), Is.True);
            Assert.That(phone.ValidateImsiFromValue(imsi.AsSpan()), Is.True);
            Assert.That(phone
                .ValidateImsiFromValueAndMobileNetworkCodeLengthUsingReadOnlySpanOfCharAndInt32(
                    imsi.AsSpan(),
                    3), Is.True);
        });
    }

    [Test]
    public void ChecksumSpanWeightsAndDateAliasesReturnValidResults()
    {
        ChecksumProvider checksum = Fake.Create(15110).Checksum();
        int[] modulo10Weights = new int[] { 3, 1 };
        int[] modulo11Weights = new int[] { 2, 3, 4, 5, 6, 7 };
        Modulo10Checksum modulo10 = checksum
            .Modulo10FromWeightsUsingReadOnlySpanOfInt32(
                modulo10Weights.AsSpan());
        Modulo11Checksum modulo11 = checksum
            .Modulo11FromWeightsUsingReadOnlySpanOfInt32(
                modulo11Weights.AsSpan());
        string modulo10Value = modulo10.Append("12345");
        string modulo11Value = modulo11.Append("12345");
        DateProvider date = Fake.Create("en-US", 15111).Date();
        string abbreviatedDay = date.WeekdayNameFromStyle(
            DateNameStyle.Abbreviated);
        TimeSpan offset = date.TimeZoneOffsetFromInstant(
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            Assert.That(modulo10.Validate(modulo10Value), Is.True);
            Assert.That(modulo11.Validate(modulo11Value), Is.True);
            Assert.That(abbreviatedDay, Is.Not.Empty);
            Assert.That(offset, Is.InRange(
                TimeSpan.FromHours(-14),
                TimeSpan.FromHours(14)));
        });
    }
}
