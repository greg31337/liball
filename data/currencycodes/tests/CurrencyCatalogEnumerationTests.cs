namespace currencycodes.Tests;

public class CurrencyCatalogEnumerationTests
{
    [Test]
    public void CatalogScopesContainTheCompleteExpectedPartitions()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        CurrencyInfo[] active = catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly);
        CurrencyInfo[] historical = catalog.GetCurrencyCatalog(CurrencySet.HistoricalOnly);
        CurrencyInfo[] all = catalog.GetCurrencyCatalog(CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(active, Has.Length.EqualTo(179));
            Assert.That(historical, Has.Length.EqualTo(170));
            Assert.That(all, Has.Length.EqualTo(349));
            Assert.That(active, Has.All.Matches<CurrencyInfo>(currency => currency.Status == CurrencyStatus.Active));
            Assert.That(historical, Has.All.Matches<CurrencyInfo>(currency => currency.Status == CurrencyStatus.Historical));
            Assert.That(all.Length, Is.EqualTo(active.Length + historical.Length));
        });
    }

    [Test]
    public void AllCatalogPreservesCanonicalOrderAndEveryPinnedSourceField()
    {
        ICurrencyCatalog service = new CurrencyCodeService();

        CurrencyInfo[] catalog = service.GetCurrencyCatalog(CurrencySet.All);

        Assert.That(catalog, Has.Length.EqualTo(GeneratedCurrencyRows.Rows.Length));
        for (int index = 0; index < catalog.Length; index++)
        {
            CurrencyRow expected = GeneratedCurrencyRows.Rows[index];
            AssertCurrencyMatchesRow(catalog[index], expected, index);
        }
    }

    [Test]
    public void ScopedCatalogsPreserveRelativeCanonicalOrder()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        CurrencyInfo[] active = catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly);
        CurrencyInfo[] historical = catalog.GetCurrencyCatalog(CurrencySet.HistoricalOnly);
        int activeIndex = 0;
        int historicalIndex = 0;

        foreach (CurrencyRow row in GeneratedCurrencyRows.Rows)
        {
            if (row.Status == CurrencyStatus.Active)
            {
                Assert.That(active[activeIndex].AlphabeticCode, Is.EqualTo(row.AlphabeticCode));
                Assert.That(active[activeIndex].OfficialName, Is.EqualTo(row.OfficialName));
                activeIndex++;
            }
            else
            {
                Assert.That(historical[historicalIndex].AlphabeticCode, Is.EqualTo(row.AlphabeticCode));
                Assert.That(historical[historicalIndex].OfficialName, Is.EqualTo(row.OfficialName));
                historicalIndex++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(activeIndex, Is.EqualTo(active.Length));
            Assert.That(historicalIndex, Is.EqualTo(historical.Length));
        });
    }

    [Test]
    public void CatalogEnforcesDesignationUniquenessRules()
    {
        ICurrencyCatalog service = new CurrencyCodeService();
        CurrencyInfo[] active = service.GetCurrencyCatalog(CurrencySet.ActiveOnly);
        CurrencyInfo[] historical = service.GetCurrencyCatalog(CurrencySet.HistoricalOnly);
        CurrencyInfo[] all = service.GetCurrencyCatalog(CurrencySet.All);
        HashSet<string> activeCodes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> designationIdentities = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < active.Length; index++)
        {
            Assert.That(
                activeCodes.Add(active[index].AlphabeticCode),
                Is.True,
                active[index].AlphabeticCode);
        }

        for (int index = 0; index < all.Length; index++)
        {
            string identity = CreateDesignationIdentity(all[index]);
            Assert.That(designationIdentities.Add(identity), Is.True, identity);
        }

        Assert.Multiple(() =>
        {
            Assert.That(activeCodes, Has.Count.EqualTo(active.Length));
            Assert.That(designationIdentities, Has.Count.EqualTo(all.Length));
            Assert.That(
                Array.ConvertAll(historical, currency => currency.AlphabeticCode),
                Does.Contain("RUR"));
            Assert.That(
                Array.FindAll(historical, currency => currency.AlphabeticCode == "RUR"),
                Has.Length.EqualTo(10));
        });
    }

    [Test]
    public void CatalogContainsEveryPinnedSpecialCurrencyClassification()
    {
        ICurrencyCatalog service = new CurrencyCodeService();
        CurrencyInfo[] catalog = service.GetCurrencyCatalog(CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(CountKind(catalog, CurrencyKind.MonetaryCurrency), Is.EqualTo(293));
            Assert.That(CountKind(catalog, CurrencyKind.Fund), Is.EqualTo(22));
            Assert.That(CountKind(catalog, CurrencyKind.PreciousMetal), Is.EqualTo(4));
            Assert.That(CountKind(catalog, CurrencyKind.SpecialUnit), Is.EqualTo(9));
            Assert.That(CountKind(catalog, CurrencyKind.Testing), Is.EqualTo(1));
            Assert.That(CountKind(catalog, CurrencyKind.NoCurrency), Is.EqualTo(1));
            Assert.That(CountKind(catalog, CurrencyKind.Unknown), Is.EqualTo(19));
            Assert.That(
                GetKindIdentities(catalog, CurrencyKind.Fund),
                Is.EqualTo(new[]
                {
                    "BEC:Historical",
                    "BEL:Historical",
                    "BOV:Active",
                    "CHC:Historical",
                    "CHE:Active",
                    "CHW:Active",
                    "CLF:Active",
                    "COU:Active",
                    "ECV:Historical",
                    "ESA:Historical",
                    "ESB:Historical",
                    "LUC:Historical",
                    "LUL:Historical",
                    "MXV:Active",
                    "USN:Active",
                    "USS:Historical",
                    "UYI:Active",
                    "UYW:Active",
                    "XAD:Active",
                    "XFU:Historical",
                    "XRE:Historical",
                    "ZAL:Historical"
                }));
            Assert.That(
                GetKindIdentities(catalog, CurrencyKind.PreciousMetal),
                Is.EqualTo(new[]
                {
                    "XAG:Active",
                    "XAU:Active",
                    "XPD:Active",
                    "XPT:Active"
                }));
            Assert.That(
                GetKindIdentities(catalog, CurrencyKind.SpecialUnit),
                Is.EqualTo(new[]
                {
                    "XBA:Active",
                    "XBB:Active",
                    "XBC:Active",
                    "XBD:Active",
                    "XDR:Active",
                    "XEU:Historical",
                    "XFO:Historical",
                    "XSU:Active",
                    "XUA:Active"
                }));
            Assert.That(
                GetKindIdentities(catalog, CurrencyKind.Testing),
                Is.EqualTo(new[] { "XTS:Active" }));
            Assert.That(
                GetKindIdentities(catalog, CurrencyKind.NoCurrency),
                Is.EqualTo(new[] { "XXX:Active" }));
        });
    }

    [Test]
    public void CatalogCallsReturnDetachedArraysObjectsAndDates()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        CurrencyInfo[] first = catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly);
        string expectedCode = first[0].AlphabeticCode;
        int expectedYear = first[0].IntroductionDate.Year;

        first[0].AlphabeticCode = "XXX";
        first[0].IntroductionDate.Year = 1;

        CurrencyInfo[] second = catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second[0], Is.Not.SameAs(first[0]));
            Assert.That(second[0].IntroductionDate, Is.Not.SameAs(first[0].IntroductionDate));
            Assert.That(second[0].AlphabeticCode, Is.EqualTo(expectedCode));
            Assert.That(second[0].IntroductionDate.Year, Is.EqualTo(expectedYear));
        });
    }

    [Test]
    public void InvalidCatalogScopeIsRejected()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        Assert.That(
            () => catalog.GetCurrencyCatalog((CurrencySet)99),
            Throws.TypeOf<System.ArgumentException>());
    }

    private static void AssertCurrencyMatchesRow(
        CurrencyInfo actual,
        CurrencyRow expected,
        int index)
    {
        string message = index.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(actual.AlphabeticCode, Is.EqualTo(expected.AlphabeticCode), message);
            Assert.That(actual.HasNumericCode, Is.EqualTo(expected.HasNumericCode), message);
            Assert.That(actual.NumericCode, Is.EqualTo(expected.NumericCode), message);
            Assert.That(actual.NumericCodeValue, Is.EqualTo(expected.NumericCodeValue), message);
            Assert.That(actual.OfficialName, Is.EqualTo(expected.OfficialName), message);
            Assert.That(actual.HasIsoMinorUnit, Is.EqualTo(expected.HasIsoMinorUnit), message);
            Assert.That(actual.IsoMinorUnitDigits, Is.EqualTo(expected.IsoMinorUnitDigits), message);
            Assert.That(actual.Status, Is.EqualTo(expected.Status), message);
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind), message);
            Assert.That(actual.IdentityFromIso, Is.EqualTo(expected.IdentityFromIso), message);
            Assert.That(actual.HistoricalStatusFromIso, Is.EqualTo(expected.HistoricalStatusFromIso), message);
            Assert.That(actual.FundClassificationFromIso, Is.EqualTo(expected.FundClassificationFromIso), message);
            Assert.That(actual.EnrichedFromCldr, Is.EqualTo(expected.EnrichedFromCldr), message);
            AssertPartialDate(actual.IntroductionDate, expected.IntroductionDate, message);
            AssertPartialDate(actual.WithdrawalDate, expected.WithdrawalDate, message);
        });
    }

    private static void AssertPartialDate(
        PartialDate actual,
        PartialDate expected,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.IsKnown, Is.EqualTo(expected.IsKnown), message);
            Assert.That(actual.Year, Is.EqualTo(expected.Year), message);
            Assert.That(actual.Month, Is.EqualTo(expected.Month), message);
            Assert.That(actual.Day, Is.EqualTo(expected.Day), message);
            Assert.That(actual.Precision, Is.EqualTo(expected.Precision), message);
        });
    }

    private static string CreateDesignationIdentity(CurrencyInfo currency)
    {
        return currency.AlphabeticCode
            + "\0"
            + currency.Status.ToString()
            + "\0"
            + currency.HasNumericCode.ToString()
            + "\0"
            + currency.NumericCode
            + "\0"
            + currency.OfficialName
            + "\0"
            + currency.WithdrawalDate.IsKnown.ToString()
            + "\0"
            + currency.WithdrawalDate.Year.ToString()
            + "\0"
            + currency.WithdrawalDate.Month.ToString()
            + "\0"
            + currency.WithdrawalDate.Day.ToString()
            + "\0"
            + currency.WithdrawalDate.Precision.ToString();
    }

    private static int CountKind(CurrencyInfo[] catalog, CurrencyKind kind)
    {
        int count = 0;
        for (int index = 0; index < catalog.Length; index++)
        {
            if (catalog[index].Kind == kind)
            {
                count++;
            }
        }

        return count;
    }

    private static string[] GetKindIdentities(CurrencyInfo[] catalog, CurrencyKind kind)
    {
        int count = CountKind(catalog, kind);
        string[] identities = new string[count];
        int resultIndex = 0;
        for (int index = 0; index < catalog.Length; index++)
        {
            if (catalog[index].Kind == kind)
            {
                identities[resultIndex] = catalog[index].AlphabeticCode
                    + ":"
                    + catalog[index].Status.ToString();
                resultIndex++;
            }
        }

        return identities;
    }
}
