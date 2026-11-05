using System.Threading;
using System.Threading.Tasks;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class RuntimeCatalogTests
{
    [Test]
    public void PublishedStateSnapshotsEveryGeneratedTableArray()
    {
        RuntimeCatalogState state = RuntimeCatalog.GetState();

        Assert.That(state.Countries, Is.Not.SameAs(CountryTable.Rows));
        Assert.That(state.ReservedCodes, Is.Not.SameAs(ReservedCodeTable.Rows));
        Assert.That(state.UserAssignedRanges, Is.Not.SameAs(ReservedCodeTable.UserAssignedRanges));
        Assert.That(state.Subdivisions, Is.Not.SameAs(SubdivisionTable.Rows));
        Assert.That(state.SubdivisionAliases, Is.Not.SameAs(SubdivisionTable.Aliases));
        Assert.That(state.SubdivisionReplacements, Is.Not.SameAs(SubdivisionTable.Replacements));
        Assert.That(state.HistoricalCountries, Is.Not.SameAs(HistoricalCountryTable.Rows));
        Assert.That(state.CountryChanges, Is.Not.SameAs(CountryChangeTable.Rows));
        Assert.That(state.CountryChangePredecessors, Is.Not.SameAs(CountryChangeTable.Predecessors));
        Assert.That(state.CountryChangeSuccessors, Is.Not.SameAs(CountryChangeTable.Successors));
        Assert.That(state.CountryNames, Is.Not.SameAs(CountryNameTable.Rows));
        Assert.That(state.SubdivisionNames, Is.Not.SameAs(SubdivisionNameTable.Rows));
        Assert.That(state.Regions, Is.Not.SameAs(GeographicRegionTable.Rows));
        Assert.That(state.RegionNames, Is.Not.SameAs(GeographicRegionTable.Names));
        Assert.That(state.CountryRegionMappings, Is.Not.SameAs(GeographicRegionTable.CountryMappings));
        Assert.That(state.ExactIndexRows, Is.Not.SameAs(GeneratedIndexTable.ExactRows));
        Assert.That(state.SearchIndexRows, Is.Not.SameAs(GeneratedIndexTable.SearchRows));

        Assert.That(state.Countries, Has.Length.EqualTo(CountryTable.Rows.Length));
        Assert.That(state.Subdivisions, Has.Length.EqualTo(SubdivisionTable.Rows.Length));
        Assert.That(state.ExactIndexRows, Has.Length.EqualTo(GeneratedIndexTable.ExactRows.Length));
        Assert.That(state.SearchIndexRows, Has.Length.EqualTo(GeneratedIndexTable.SearchRows.Length));
        Assert.That(state.DataIdentity, Is.EqualTo(GeneratedDataMetadata.AggregateContentChecksum));
    }

    [Test]
    public void ConcurrentReadersObserveOneFullyPublishedState()
    {
        const int readerCount = 64;
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        Task<RuntimeCatalogState>[] readers = new Task<RuntimeCatalogState>[readerCount];
        int index = 0;
        while (index < readers.Length)
        {
            readers[index] = Task.Run(() =>
            {
                start.Wait();
                return RuntimeCatalog.GetState();
            });
            index += 1;
        }

        start.Set();
        Task.WaitAll(readers);
        RuntimeCatalogState expected = readers[0].Result;
        index = 0;
        while (index < readers.Length)
        {
            RuntimeCatalogState actual = readers[index].Result;
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(actual.Countries[0].Alpha2, Is.EqualTo("AD"));
            Assert.That(actual.Subdivisions[actual.Subdivisions.Length - 1].Code, Is.EqualTo("ZW-MW"));
            Assert.That(actual.DataIdentity, Is.EqualTo(expected.DataIdentity));
            index += 1;
        }
    }

    [Test]
    public void ConcurrentLookupsAndDetachedMutationRemainDeterministic()
    {
        const int readerCount = 48;
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        Task<string>[] readers = new Task<string>[readerCount];
        int index = 0;
        while (index < readers.Length)
        {
            readers[index] = Task.Run(() =>
            {
                start.Wait();
                GeneratedCountryRow countryRow = CountryIndex.FindCountryByAlpha2("CI")!;
                Country country = CountryMapper.MapCountry(countryRow);
                country.Aliases[0] = "changed";
                country.LocalizedNames[0].Value = "changed";
                country.RegionM49Codes[0] = "999";

                GeneratedSubdivisionRow subdivisionRow =
                    SubdivisionIndex.FindSubdivisionByCode("GB-WLS")!;
                Subdivision subdivision = SubdivisionMapper.MapSubdivision(subdivisionRow);
                subdivision.LocalizedNames[0].Value = "changed";

                HistoricalCountry historical = HistoricalMapper.MapHistoricalCountry(
                    HistoricalIndex.FindHistoricalCountryByAlpha4("ANHH")!);
                historical.SuccessorIdentifiers[0] = "changed";

                GeographicRegion region = RegionMapper.MapRegion(
                    RegionIndex.FindRegionByM49Code("150")!);
                region.CountryCodes[0] = "ZZ";

                Country freshCountry = CountryMapper.MapCountry(countryRow);
                Subdivision freshSubdivision = SubdivisionMapper.MapSubdivision(subdivisionRow);
                HistoricalCountry freshHistorical = HistoricalMapper.MapHistoricalCountry(
                    HistoricalIndex.FindHistoricalCountryByAlpha4("ANHH")!);
                GeographicRegion freshRegion = RegionMapper.MapRegion(
                    RegionIndex.FindRegionByM49Code("150")!);
                return freshCountry.Alpha2 + ":" +
                       freshCountry.Aliases[0] + ":" +
                       freshCountry.RegionM49Codes[0] + ":" +
                       freshSubdivision.LocalizedNames[0].Value + ":" +
                       freshHistorical.SuccessorIdentifiers[0] + ":" +
                       freshRegion.M49Code;
            });
            index += 1;
        }

        start.Set();
        Task.WaitAll(readers);
        index = 0;
        while (index < readers.Length)
        {
            Assert.That(
                readers[index].Result,
                Is.EqualTo("CI:Ivory Coast:001:Wales:BQ:150"));
            index += 1;
        }
    }
}
