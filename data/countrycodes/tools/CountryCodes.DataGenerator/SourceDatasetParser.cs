using System.IO;
using System.Text;
using CountryCodes.DataGenerator.Parsers;

namespace CountryCodes.DataGenerator;

internal static class SourceDatasetParser
{
    internal static RawSourceData Parse(string repositoryRoot)
    {
        DebianIsoData debian = ParseDebian(repositoryRoot);
        UnM49Data unM49 = ParseUnM49(repositoryRoot);
        CldrData cldr = ParseCldr(repositoryRoot);
        IanaRegistryData iana = ParseIana(repositoryRoot);
        return new RawSourceData(debian, unM49, cldr, iana);
    }

    private static DebianIsoData ParseDebian(string repositoryRoot)
    {
        string countryPath = Resolve(repositoryRoot, "specs/iso-codes/iso_3166-1.json");
        string subdivisionPath = Resolve(repositoryRoot, "specs/iso-codes/iso_3166-2.json");
        string historicalPath = Resolve(repositoryRoot, "specs/iso-codes/iso_3166-3.json");

        using FileStream countryStream = File.OpenRead(countryPath);
        using FileStream subdivisionStream = File.OpenRead(subdivisionPath);
        using FileStream historicalStream = File.OpenRead(historicalPath);
        return new DebianIsoData(
            DebianIsoJsonParser.ParseCountries(countryStream, countryPath),
            DebianIsoJsonParser.ParseSubdivisions(subdivisionStream, subdivisionPath),
            DebianIsoJsonParser.ParseHistoricalCountries(historicalStream, historicalPath));
    }

    private static UnM49Data ParseUnM49(string repositoryRoot)
    {
        string methodologyPath = Resolve(repositoryRoot, "specs/un/un-m49-methodology.html");
        string overviewPath = Resolve(repositoryRoot, "specs/un/un-m49-overview.html");
        using StreamReader methodologyReader = OpenText(methodologyPath);
        using StreamReader overviewReader = OpenText(overviewPath);
        UnM49MethodologyResult methodology = UnM49HtmlParser.ParseMethodology(
            methodologyReader,
            methodologyPath);
        return new UnM49Data(
            methodology.Countries,
            methodology.History,
            UnM49HtmlParser.ParseOverview(overviewReader, overviewPath));
    }

    private static CldrData ParseCldr(string repositoryRoot)
    {
        string namesPath = Resolve(repositoryRoot, "specs/unicode/cldr-48.2-en.xml");
        string aliasesPath = Resolve(
            repositoryRoot,
            "specs/unicode/cldr-48.2-supplementalMetadata.xml");
        string containmentPath = Resolve(
            repositoryRoot,
            "specs/unicode/cldr-48.2-subdivisions.xml");
        using FileStream namesStream = File.OpenRead(namesPath);
        using FileStream aliasesStream = File.OpenRead(aliasesPath);
        using FileStream containmentStream = File.OpenRead(containmentPath);
        CldrNamesResult names = CldrXmlParser.ParseNames(namesStream, namesPath);
        return new CldrData(
            names.Locale,
            names.Names,
            CldrXmlParser.ParseAliases(aliasesStream, aliasesPath),
            CldrXmlParser.ParseContainment(containmentStream, containmentPath));
    }

    private static IanaRegistryData ParseIana(string repositoryRoot)
    {
        string registryPath = Resolve(repositoryRoot, "specs/ietf/language-subtag-registry.txt");
        using StreamReader reader = OpenText(registryPath);
        return IanaRegistryParser.Parse(reader, registryPath);
    }

    private static StreamReader OpenText(string path)
    {
        return new StreamReader(
            path,
            new UTF8Encoding(false, true),
            true,
            65_536);
    }

    private static string Resolve(string repositoryRoot, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
    }
}
