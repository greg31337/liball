using System;
using System.Globalization;

namespace fake;

internal class CardFixture
{
    internal CardIssuer Issuer;
    internal string Name = string.Empty;
    internal string[] PrefixPatterns = new string[0];
    internal int[] PanLengths = new int[0];
    internal int CvvLength;
    internal string TestPan = string.Empty;
}

internal static class CardGenerator
{
    const string Marker = "fake.synthetic-card.v1";
    static readonly CardFixture[] Fixtures = new CardFixture[]
    {
        Fixture(
            CardIssuer.Visa,
            "Visa",
            new string[] { "4" },
            new int[] { 13, 16, 19 },
            3,
            "4242424242424242"),
        Fixture(
            CardIssuer.Mastercard,
            "Mastercard",
            new string[] { "51-55", "2221-2720" },
            new int[] { 16 },
            3,
            "5555555555554444"),
        Fixture(
            CardIssuer.AmericanExpress,
            "American Express",
            new string[] { "34", "37" },
            new int[] { 15 },
            4,
            "378282246310005"),
        Fixture(
            CardIssuer.Discover,
            "Discover",
            new string[] { "6011", "644-649", "65" },
            new int[] { 16, 19 },
            3,
            "6011111111111117")
    };
    static readonly string[] Cardholders = new string[]
    {
        "EXAMPLE CARDHOLDER",
        "SAMPLE CARDHOLDER",
        "TEST CARDHOLDER"
    };

    internal static CardIssuer Issuer(FakeRandom random)
    {
        return Fixtures[random.IntFromMaximumExclusive(Fixtures.Length)].Issuer;
    }

    internal static CardIssuer Issuer(string number)
    {
        ValidatePanText(number, "number");
        int index = 0;
        while (index < Fixtures.Length)
        {
            if (Matches(Fixtures[index], number))
            {
                return Fixtures[index].Issuer;
            }
            index = index + 1;
        }
        throw new ArgumentException(
            "number does not match a supported card issuer prefix and length.",
            "number");
    }

    internal static CardIssuerPattern Pattern(
        CardIssuer issuer,
        int maximumOutputSize)
    {
        CardFixture fixture = Find(issuer);
        CardIssuerPattern result = new CardIssuerPattern();
        result.Issuer = fixture.Issuer;
        result.Name = fixture.Name;
        result.PrefixPatterns = Copy(fixture.PrefixPatterns);
        result.PanLengths = Copy(fixture.PanLengths);
        result.CvvLength = fixture.CvvLength;
        result.TestPan = fixture.TestPan;
        result.IsSyntheticTestPattern = true;
        result.TestMarker = Marker;
        Guard.OutputSize(Size(result), maximumOutputSize, "cardPattern");
        return result;
    }

    internal static CardIssuerPattern[] Patterns(
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(Fixtures.Length, maximumCount, "cardPatterns");
        CardIssuerPattern[] result = new CardIssuerPattern[Fixtures.Length];
        int total = 0;
        int index = 0;
        while (index < Fixtures.Length)
        {
            result[index] = Pattern(
                Fixtures[index].Issuer,
                FakeLimits.DefaultMaximumOutputSize);
            total = total + Size(result[index]);
            index = index + 1;
        }
        Guard.OutputSize(total, maximumOutputSize, "cardPatterns");
        return result;
    }

    internal static string Number(
        CardIssuer issuer,
        int maximumOutputSize)
    {
        string result = Find(issuer).TestPan;
        Guard.OutputSize(result.Length, maximumOutputSize, "cardNumber");
        return result;
    }

    internal static string InvalidNumber(
        CardIssuer issuer,
        int maximumOutputSize)
    {
        string value = Find(issuer).TestPan;
        char[] result = value.ToCharArray();
        int final = result.Length - 1;
        result[final] = (char)('0' + (result[final] - '0' + 1) % 10);
        Guard.OutputSize(result.Length, maximumOutputSize, "cardNumber");
        return new string(result);
    }

    internal static bool IsValid(string number, int maximumCount)
    {
        Guard.NotNull(number, "number");
        if (number.Length != 0)
        {
            Guard.Count(number.Length - 1, maximumCount, "number");
        }
        if (!Digits(number))
        {
            return false;
        }
        if (!Supported(number))
        {
            return false;
        }
        return ChecksumAlgorithms.IsLuhn(number.AsSpan(), maximumCount);
    }

    internal static string Mask(
        string number,
        int maximumOutputSize)
    {
        ValidatePanText(number, "number");
        if (!Supported(number))
        {
            throw new ArgumentException(
                "number does not match a supported card issuer prefix and length.",
                "number");
        }
        char[] result = new char[number.Length];
        int visible = number.Length - 4;
        int index = 0;
        while (index < result.Length)
        {
            result[index] = index < visible ? '*' : number[index];
            index = index + 1;
        }
        Guard.OutputSize(result.Length, maximumOutputSize, "maskedCard");
        return new string(result);
    }

    internal static string Cvv(
        FakeRandom random,
        CardIssuer issuer,
        int maximumOutputSize)
    {
        int length = Find(issuer).CvvLength;
        return NumericStringGenerator.Digits(
            random,
            length,
            maximumOutputSize);
    }

    internal static string Pin(
        FakeRandom random,
        int maximumOutputSize)
    {
        return NumericStringGenerator.Digits(
            random,
            4,
            maximumOutputSize);
    }

    internal static string Cardholder(
        FakeRandom random,
        int maximumOutputSize)
    {
        string result = Cardholders[random.IntFromMaximumExclusive(Cardholders.Length)];
        Guard.OutputSize(result.Length, maximumOutputSize, "cardholder");
        return result;
    }

    internal static string Expiry(
        GenerationContext context,
        FakeRandom random)
    {
        DateTimeOffset value = ExpiryDate(
            context.CapturedOptions.ReferenceTime,
            random);
        string result = ExpiryText(value);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "cardExpiry");
        return result;
    }

    internal static CardRecord Card(
        GenerationContext context,
        FakeRandom random,
        CardIssuer issuer)
    {
        CardFixture fixture = Find(issuer);
        DateTimeOffset expiry = ExpiryDate(
            context.CapturedOptions.ReferenceTime,
            random);
        CardRecord result = new CardRecord();
        result.Issuer = fixture.Issuer;
        result.IssuerName = fixture.Name;
        result.Number = fixture.TestPan;
        result.MaskedNumber = Mask(
            result.Number,
            FakeLimits.DefaultMaximumOutputSize);
        result.Cvv = Cvv(
            random,
            issuer,
            FakeLimits.DefaultMaximumOutputSize);
        result.Pin = Pin(
            random,
            FakeLimits.DefaultMaximumOutputSize);
        result.Expiry = ExpiryText(expiry);
        result.ExpiryMonth = expiry.Month;
        result.ExpiryYear = expiry.Year;
        result.Cardholder = Cardholder(
            random,
            FakeLimits.DefaultMaximumOutputSize);
        result.IsLuhnValid = ChecksumAlgorithms.IsLuhn(
            result.Number.AsSpan(),
            FakeLimits.DefaultMaximumCount);
        result.IsSynthetic = true;
        result.IsTestCard = true;
        result.TestMarker = Marker;
        result.IsAuthorized = false;
        result.RepresentsActiveAccount = false;
        Guard.OutputSize(
            Size(result),
            context.CapturedOptions.MaximumOutputSize,
            "card");
        return result;
    }

    internal static CardRecord Card(
        GenerationContext context,
        FakeRandom random)
    {
        return Card(context, random, Issuer(random));
    }

    static DateTimeOffset ExpiryDate(
        DateTimeOffset reference,
        FakeRandom random)
    {
        int available = (DateTimeOffset.MaxValue.Year - reference.Year) * 12 +
            DateTimeOffset.MaxValue.Month - reference.Month;
        if (available < 1)
        {
            throw FakeException.Create(
                "No future card-expiry month is representable after the captured reference time.");
        }
        int maximum = available < 60 ? available : 60;
        int months = random.IntFromMinimumAndMaximumAndMaximumInclusive(1, maximum, true);
        return reference.AddMonths(months);
    }

    static string ExpiryText(DateTimeOffset value)
    {
        return value.Month.ToString("D2", CultureInfo.InvariantCulture) + "/" +
            (value.Year % 100).ToString("D2", CultureInfo.InvariantCulture);
    }

    static CardFixture Find(CardIssuer issuer)
    {
        int index = 0;
        while (index < Fixtures.Length)
        {
            if (Fixtures[index].Issuer == issuer)
            {
                return Fixtures[index];
            }
            index = index + 1;
        }
        throw new ArgumentOutOfRangeException(
            "issuer",
            issuer,
            "issuer is not supported.");
    }

    static bool Supported(string number)
    {
        int index = 0;
        while (index < Fixtures.Length)
        {
            if (Matches(Fixtures[index], number))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool Matches(CardFixture fixture, string number)
    {
        if (!Contains(fixture.PanLengths, number.Length))
        {
            return false;
        }
        switch (fixture.Issuer)
        {
            case CardIssuer.Visa:
                return number[0] == '4';
            case CardIssuer.Mastercard:
                int mastercardTwo = Integer(number, 0, 2);
                int mastercardFour = Integer(number, 0, 4);
                return mastercardTwo >= 51 && mastercardTwo <= 55 ||
                    mastercardFour >= 2221 && mastercardFour <= 2720;
            case CardIssuer.AmericanExpress:
                int americanExpress = Integer(number, 0, 2);
                return americanExpress == 34 || americanExpress == 37;
            case CardIssuer.Discover:
                int discoverTwo = Integer(number, 0, 2);
                int discoverThree = Integer(number, 0, 3);
                int discoverFour = Integer(number, 0, 4);
                return discoverFour == 6011 ||
                    discoverThree >= 644 && discoverThree <= 649 ||
                    discoverTwo == 65;
            default:
                return false;
        }
    }

    static int Integer(string value, int start, int length)
    {
        int result = 0;
        int index = 0;
        while (index < length)
        {
            result = result * 10 + value[start + index] - '0';
            index = index + 1;
        }
        return result;
    }

    static bool Contains(int[] values, int value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void ValidatePanText(string number, string parameterName)
    {
        Guard.NotNullOrWhiteSpace(number, parameterName);
        if (!Digits(number))
        {
            throw new ArgumentException(
                $"{parameterName} must contain ASCII digits only.",
                parameterName);
        }
    }

    static bool Digits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length != 0;
    }

    static CardFixture Fixture(
        CardIssuer issuer,
        string name,
        string[] prefixPatterns,
        int[] panLengths,
        int cvvLength,
        string testPan)
    {
        CardFixture result = new CardFixture();
        result.Issuer = issuer;
        result.Name = name;
        result.PrefixPatterns = prefixPatterns;
        result.PanLengths = panLengths;
        result.CvvLength = cvvLength;
        result.TestPan = testPan;
        return result;
    }

    static string[] Copy(string[] values)
    {
        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static int[] Copy(int[] values)
    {
        int[] result = new int[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static int Size(CardIssuerPattern value)
    {
        int result = value.Name.Length + value.TestPan.Length +
            value.TestMarker.Length;
        int index = 0;
        while (index < value.PrefixPatterns.Length)
        {
            result = result + value.PrefixPatterns[index].Length;
            index = index + 1;
        }
        return result;
    }

    static int Size(CardRecord value)
    {
        return value.IssuerName.Length +
            value.Number.Length +
            value.MaskedNumber.Length +
            value.Cvv.Length +
            value.Pin.Length +
            value.Expiry.Length +
            value.Cardholder.Length +
            value.TestMarker.Length;
    }
}
