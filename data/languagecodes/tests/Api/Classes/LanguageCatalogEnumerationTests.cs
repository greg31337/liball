using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageCatalogEnumerationTests
{
    [Test]
    public void GetAllEntriesReturnsTheCompleteActiveCatalogInStableOrder()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry[] first = service.GetAllEntries();
        LanguageCodeEntry[] second = service.GetAllEntries();

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(8043));
            Assert.That(first.All(entry => entry.Status == LanguageCodeStatus.Active), Is.True);
            AssertStableOrder(first);
            Assert.That(first.Select(StableIdentity), Is.EqualTo(second.Select(StableIdentity)));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
        });
    }

    [TestCase(Iso639Set.None, 0)]
    [TestCase(Iso639Set.Set1, 184)]
    [TestCase(Iso639Set.Set2, 486)]
    [TestCase(Iso639Set.Set3, 7927)]
    [TestCase(Iso639Set.Set5, 115)]
    public void GetEntriesBySetCoversEverySet(Iso639Set set, int expectedCount)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry[] first = service.GetEntriesBySet(set);
        LanguageCodeEntry[] second = service.GetEntriesBySet(set);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(expectedCount));
            Assert.That(first.All(entry => entry.Status == LanguageCodeStatus.Active), Is.True);
            Assert.That(first.All(entry => BelongsToSet(entry, set)), Is.True);
            AssertStableOrder(first);
            Assert.That(first.Select(StableIdentity), Is.EqualTo(second.Select(StableIdentity)));
        });
    }

    [TestCase(LanguageScope.Unknown, 0)]
    [TestCase(LanguageScope.Individual, 7860)]
    [TestCase(LanguageScope.Macrolanguage, 63)]
    [TestCase(LanguageScope.Collection, 116)]
    [TestCase(LanguageScope.Special, 4)]
    public void GetEntriesByScopeCoversEveryScope(LanguageScope scope, int expectedCount)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry[] first = service.GetEntriesByScope(scope);
        LanguageCodeEntry[] second = service.GetEntriesByScope(scope);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(expectedCount));
            Assert.That(first.All(entry => entry.Status == LanguageCodeStatus.Active), Is.True);
            Assert.That(first.All(entry => entry.Scope == scope), Is.True);
            AssertStableOrder(first);
            Assert.That(first.Select(StableIdentity), Is.EqualTo(second.Select(StableIdentity)));
        });
    }

    [TestCase(LanguageType.Unknown, 116)]
    [TestCase(LanguageType.Living, 7082)]
    [TestCase(LanguageType.Extinct, 602)]
    [TestCase(LanguageType.Ancient, 0)]
    [TestCase(LanguageType.Historical, 215)]
    [TestCase(LanguageType.Constructed, 24)]
    [TestCase(LanguageType.Special, 4)]
    public void GetEntriesByTypeCoversEveryLanguageType(LanguageType languageType, int expectedCount)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry[] first = service.GetEntriesByType(languageType);
        LanguageCodeEntry[] second = service.GetEntriesByType(languageType);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(expectedCount));
            Assert.That(first.All(entry => entry.Status == LanguageCodeStatus.Active), Is.True);
            Assert.That(first.All(entry => entry.LanguageType == languageType), Is.True);
            AssertStableOrder(first);
            Assert.That(first.Select(StableIdentity), Is.EqualTo(second.Select(StableIdentity)));
        });
    }

    [TestCase(LanguageCodeStatus.Unknown, 0)]
    [TestCase(LanguageCodeStatus.Active, 8043)]
    [TestCase(LanguageCodeStatus.Retired, 388)]
    [TestCase(LanguageCodeStatus.Deprecated, 0)]
    [TestCase(LanguageCodeStatus.Reserved, 0)]
    [TestCase(LanguageCodeStatus.PrivateUse, 0)]
    [TestCase(LanguageCodeStatus.Unassigned, 0)]
    public void InternalStatusFilterCoversEveryCatalogStatus(
        LanguageCodeStatus status,
        int expectedCount)
    {
        LanguageCodeEntry[] first = LanguageCatalogEngine.GetEntriesByStatus(status);
        LanguageCodeEntry[] second = LanguageCatalogEngine.GetEntriesByStatus(status);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(expectedCount));
            Assert.That(first.All(entry => entry.Status == status), Is.True);
            AssertStableOrder(first);
            Assert.That(first.Select(StableIdentity), Is.EqualTo(second.Select(StableIdentity)));
        });
    }

    [TestCase("all")]
    [TestCase("set")]
    [TestCase("scope")]
    [TestCase("type")]
    public void PublicEnumerationMethodsReturnRecursiveDefensiveSnapshots(string operation)
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageCodeEntry[] first = Enumerate(service, operation);
        LanguageCodeEntry[] second = Enumerate(service, operation);
        LanguageCodeEntry firstAao = first.Single(entry => entry.Set3Code == "aao");
        LanguageCodeEntry secondAao = second.Single(entry => entry.Set3Code == "aao");
        string? expectedName = secondAao.Names[0].Text;
        string? expectedAlias = secondAao.Aliases[0].Value;
        string? expectedParent = secondAao.Relationships[0].ParentCode;

        first[0] = new LanguageCodeEntry { Set3Code = "changed-array-slot" };
        firstAao.Set3Code = "changed-entry";
        firstAao.Names[0].Text = "changed-name";
        firstAao.Aliases[0].Value = "changed-alias";
        firstAao.Relationships[0].ParentCode = "changed-parent";
        LanguageCodeEntry[] third = Enumerate(service, operation);
        LanguageCodeEntry thirdAao = third.Single(entry => entry.Set3Code == "aao");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(firstAao, Is.Not.SameAs(secondAao));
            Assert.That(firstAao.Names, Is.Not.SameAs(secondAao.Names));
            Assert.That(firstAao.Aliases, Is.Not.SameAs(secondAao.Aliases));
            Assert.That(firstAao.Relationships, Is.Not.SameAs(secondAao.Relationships));
            Assert.That(secondAao.Set3Code, Is.EqualTo("aao"));
            Assert.That(secondAao.Names[0].Text, Is.EqualTo(expectedName));
            Assert.That(secondAao.Aliases[0].Value, Is.EqualTo(expectedAlias));
            Assert.That(secondAao.Relationships[0].ParentCode, Is.EqualTo(expectedParent));
            Assert.That(thirdAao.Names[0].Text, Is.EqualTo(expectedName));
            Assert.That(thirdAao.Aliases[0].Value, Is.EqualTo(expectedAlias));
            Assert.That(thirdAao.Relationships[0].ParentCode, Is.EqualTo(expectedParent));
        });
    }

    [Test]
    public void UndefinedFilterValuesReturnEmptyResults()
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.Multiple(() =>
        {
            Assert.That(service.GetEntriesBySet((Iso639Set)int.MaxValue), Is.Empty);
            Assert.That(service.GetEntriesByScope((LanguageScope)int.MaxValue), Is.Empty);
            Assert.That(service.GetEntriesByType((LanguageType)int.MaxValue), Is.Empty);
            Assert.That(
                LanguageCatalogEngine.GetEntriesByStatus((LanguageCodeStatus)int.MaxValue),
                Is.Empty);
        });
    }

    private static LanguageCodeEntry[] Enumerate(LanguageCodeService service, string operation)
    {
        switch (operation)
        {
            case "all":
                return service.GetAllEntries();
            case "set":
                return service.GetEntriesBySet(Iso639Set.Set3);
            case "scope":
                return service.GetEntriesByScope(LanguageScope.Individual);
            case "type":
                return service.GetEntriesByType(LanguageType.Living);
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static bool BelongsToSet(LanguageCodeEntry entry, Iso639Set set)
    {
        switch (set)
        {
            case Iso639Set.Set1:
                return entry.InSet1;
            case Iso639Set.Set2:
                return entry.InSet2;
            case Iso639Set.Set3:
                return entry.InSet3;
            case Iso639Set.Set5:
                return entry.InSet5;
            default:
                return false;
        }
    }

    private static void AssertStableOrder(LanguageCodeEntry[] entries)
    {
        for (int index = 1; index < entries.Length; index++)
        {
            Assert.That(
                string.CompareOrdinal(StableCode(entries[index - 1]), StableCode(entries[index])),
                Is.LessThanOrEqualTo(0),
                $"Catalog order changed between indexes {index - 1} and {index}.");
        }
    }

    private static string StableIdentity(LanguageCodeEntry entry)
    {
        return StableCode(entry) + "\0" + entry.ReferenceName;
    }

    private static string StableCode(LanguageCodeEntry entry)
    {
        return entry.Set3Code ?? entry.Set5Code ?? entry.Set2TerminologyCode ??
            entry.Set2BibliographicCode ?? entry.Alpha2Code ?? string.Empty;
    }
}
