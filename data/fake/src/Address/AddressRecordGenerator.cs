using System.Text;

namespace fake;

internal static class AddressRecordGenerator
{
    internal static AddressRecord AddressRecord(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        AddressCountryData country = data.ChooseCountry(random, data.Countries);
        AddressCodedData subdivision = data.ChooseSubdivision(
            random,
            country.Alpha2Code);
        GeoRecord geography = CreateGeography(
            context,
            random,
            data,
            country,
            subdivision);

        AddressRecord result = new AddressRecord();
        result.SafetyMode = geography.SafetyMode;
        result.NonDeliverabilityGuaranteed = geography.NonDeliverabilityGuaranteed;
        result.NonexistenceGuaranteed = geography.NonexistenceGuaranteed;
        result.BuildingNumber = AddressGenerator.Mask(
            context,
            random,
            data,
            data.BuildingNumberMasks);
        result.StreetName = data.Choose(random, data.StreetNames).Value;
        result.StreetType = data.ChooseType(random, data.StreetTypes).Name;
        result.Street = Join(
            context,
            result.StreetName,
            " ",
            result.StreetType);
        result.StreetAddress = Join(
            context,
            result.BuildingNumber,
            " ",
            result.Street);
        AddressTypeData unitType = data.ChooseType(random, data.UnitTypes);
        string unitNumber = AddressGenerator.Mask(
            context,
            random,
            data,
            data.UnitMasks);
        result.Unit = Join(context, unitType.Name, " ", unitNumber);
        result.District = data.Choose(random, data.Districts).Value;
        result.City = data.Choose(random, data.Cities).Value;
        result.County = data.Choose(random, data.Counties).Value;
        result.Subdivision = subdivision.Name;
        result.SubdivisionAbbreviation = subdivision.Abbreviation;
        result.SubdivisionCode = subdivision.Code;
        result.PostalCode = geography.PostalCode;
        result.Country = country.Name;
        result.CountryAbbreviation = country.Abbreviation;
        result.CountryCode = country.Alpha2Code;
        result.TimeZoneId = country.TimeZoneId;
        result.Geography = geography;
        return result;
    }

    internal static GeoRecord GeoRecord(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        AddressCountryData country = data.ChooseCountry(random, data.Countries);
        AddressCodedData subdivision = data.ChooseSubdivision(
            random,
            country.Alpha2Code);
        return CreateGeography(
            context,
            random,
            data,
            country,
            subdivision);
    }

    static GeoRecord CreateGeography(
        GenerationContext context,
        FakeRandom random,
        AddressLocaleData data,
        AddressCountryData country,
        AddressCodedData subdivision)
    {
        GeoRecord result = new GeoRecord();
        result.SafetyMode = context.SafetyModeValue;
        bool guaranteed = result.SafetyMode == SafetyMode.Reserved;
        result.NonDeliverabilityGuaranteed = guaranteed;
        result.NonexistenceGuaranteed = guaranteed;
        result.Country = country.Name;
        result.CountryCode = country.Alpha2Code;
        result.Subdivision = subdivision.Name;
        result.SubdivisionCode = subdivision.Code;
        result.PostalCode = AddressGenerator.Mask(
            context,
            random,
            data,
            country.PostalMasks);
        result.TimeZoneId = country.TimeZoneId;
        result.Coordinate = GeographyGenerator.BoundingBoxPoint(
            random,
            country.SouthLatitude,
            country.WestLongitude,
            country.NorthLatitude,
            country.EastLongitude);
        return result;
    }

    static string Join(
        GenerationContext context,
        string first,
        string separator,
        string second)
    {
        long length = (long)first.Length +
            (long)separator.Length +
            (long)second.Length;
        Guard.OutputSize(
            length,
            context.CapturedOptions.MaximumOutputSize,
            "addressRecord");
        StringBuilder result = new StringBuilder((int)length);
        result.Append(first);
        result.Append(separator);
        result.Append(second);
        return result.ToString();
    }
}
