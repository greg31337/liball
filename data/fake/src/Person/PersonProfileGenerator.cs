using System.Globalization;
using System.Text;

namespace fake;

internal static class PersonProfileGenerator
{
    internal static PersonProfile Person(
        GenerationContext context,
        FakeRandom random)
    {
        PersonProfileData data = PersonProfileDataParser.Resolve(context);
        PersonProfileLocalityData locality = data.ChooseLocality(random);
        string address = Address(context, random, data, locality);
        string phone = Phone(context, random, data, locality);
        return Person(context, random, data, locality, address, phone);
    }

    internal static HouseholdProfile Household(
        GenerationContext context,
        FakeRandom random)
    {
        PersonProfileData data = PersonProfileDataParser.Resolve(context);
        PersonProfileLocalityData locality = data.ChooseLocality(random);
        string address = Address(context, random, data, locality);
        string phone = Phone(context, random, data, locality);
        int memberCount = data.ChooseHouseholdSize(random);
        PersonProfile[] members = new PersonProfile[memberCount];
        int index = 0;
        while (index < members.Length)
        {
            members[index] = Person(
                context,
                random,
                data,
                locality,
                address,
                phone);
            index++;
        }
        HouseholdProfile result = new HouseholdProfile();
        result.HouseholdType = memberCount == 1
            ? data.SingleHouseholdLabel
            : data.MultiHouseholdLabel;
        result.MemberCount = memberCount;
        result.Members = members;
        result.Address = address;
        result.Phone = phone;
        result.Locale = context.CapturedOptions.Locale;
        result.TimeZoneId = locality.TimeZoneId;
        return result;
    }

    static PersonProfile Person(
        GenerationContext context,
        FakeRandom random,
        PersonProfileData data,
        PersonProfileLocalityData locality,
        string address,
        string phone)
    {
        PersonProfileName name = PersonNameGenerator.ProfileName(context, random);
        System.DateOnly referenceDate = CalendarDateGenerator.ReferenceDate(
            context.ReferenceTime());
        System.DateOnly birthDate = CalendarDateGenerator.BirthDate(
            random,
            referenceDate,
            18,
            80);
        string username = Username(context, random, name);
        string email = Email(context, username, locality.EmailDomain);
        string employer = data.Choose(random, data.Employers).Value;
        PersonIdentifierRecord identifiers = new PersonIdentifierRecord();
        identifiers.ProfileId = UuidGenerator.GenerateVersion4Text(
            random,
            context.CapturedOptions.MaximumOutputSize);
        identifiers.Username = username;
        identifiers.Email = email;
        identifiers.EmailIsReservedExample = true;
        identifiers.IsSynthetic = true;
        identifiers.RepresentsReachableContact = false;
        PersonProfile result = new PersonProfile();
        result.FirstName = name.FirstName;
        result.MiddleName = name.MiddleName;
        result.LastName = name.LastName;
        result.MaidenName = name.MaidenName;
        result.Patronymic = name.Patronymic;
        result.Matronymic = name.Matronymic;
        result.Prefix = name.Prefix;
        result.Suffix = name.Suffix;
        result.Initials = name.Initials;
        result.FullName = name.FullName;
        result.NativeName = name.NativeName;
        result.TransliteratedName = name.TransliteratedName;
        result.Pronouns = PersonIdentityGenerator.Pronouns(context, random);
        result.BirthDate = birthDate;
        result.Age = CalendarDateGenerator.Age(birthDate, referenceDate);
        result.Username = username;
        result.Email = email;
        result.Identifiers = identifiers;
        result.AvatarDataUri = SvgDataUriGenerator.Avatar(
            random,
            128,
            context.CapturedOptions.MaximumOutputSize);
        result.FavoriteColor = ColorGenerator.Name(context, random);
        result.Phone = phone;
        result.Address = address;
        result.Employer = employer;
        result.Locale = context.CapturedOptions.Locale;
        string? language = LanguageTagParser.Parse(
            context.CapturedOptions.Locale,
            "Locale").Language;
        result.Language = language == null ? string.Empty : language;
        result.TimeZoneId = locality.TimeZoneId;
        return result;
    }

    static string Username(
        GenerationContext context,
        FakeRandom random,
        PersonProfileName name)
    {
        int limit = context.CapturedOptions.MaximumOutputSize;
        string first = TextTransforms.Slug(name.FirstName, '-', limit);
        string last = TextTransforms.Slug(name.LastName, '-', limit);
        if (first.Length == 0)
        {
            first = "user";
        }
        if (last.Length == 0)
        {
            last = "profile";
        }
        string suffix = NumericStringGenerator.Digits(random, 4, limit);
        StringBuilder result = new StringBuilder();
        Append(result, first, limit, "username");
        Append(result, ".", limit, "username");
        Append(result, last, limit, "username");
        Append(result, ".", limit, "username");
        Append(result, suffix, limit, "username");
        return result.ToString();
    }

    static string Email(
        GenerationContext context,
        string username,
        string domain)
    {
        int limit = context.CapturedOptions.MaximumOutputSize;
        StringBuilder result = new StringBuilder();
        Append(result, username, limit, "email");
        Append(result, "@", limit, "email");
        Append(result, domain, limit, "email");
        return result.ToString();
    }

    static string Address(
        GenerationContext context,
        FakeRandom random,
        PersonProfileData data,
        PersonProfileLocalityData locality)
    {
        int limit = context.CapturedOptions.MaximumOutputSize;
        string building = random
            .IntFromMinimumAndMaximumAndMaximumInclusive(1, 9999, true)
            .ToString(CultureInfo.InvariantCulture);
        string street = data.Choose(random, locality.Streets).Value;
        StringBuilder result = new StringBuilder();
        Append(result, building, limit, "address");
        Append(result, " ", limit, "address");
        Append(result, street, limit, "address");
        Append(result, ", ", limit, "address");
        Append(result, locality.City, limit, "address");
        Append(result, ", ", limit, "address");
        Append(result, locality.Region, limit, "address");
        Append(result, " ", limit, "address");
        Append(result, locality.PostalCode, limit, "address");
        return result.ToString();
    }

    static string Phone(
        GenerationContext context,
        FakeRandom random,
        PersonProfileData data,
        PersonProfileLocalityData locality)
    {
        string mask = data.Choose(random, locality.PhoneMasks).Value;
        return MaskFormatter.Format(
            random,
            mask,
            context.CapturedOptions.MaximumOutputSize);
    }

    static void Append(
        StringBuilder builder,
        string value,
        int maximumOutputSize,
        string parameterName)
    {
        Guard.OutputSize(
            (long)builder.Length + (long)value.Length,
            maximumOutputSize,
            parameterName);
        builder.Append(value);
    }
}
