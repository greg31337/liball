using languagecodes;

namespace languagecodes.Tests;

public class LanguageCodeEntryTests
{
    [Test]
    public void EntryRepresentsAReconciledMultiSetEntity()
    {
        LanguageName referenceName = new LanguageName
        {
            Text = "French",
            NameLanguage = "en",
            Kind = LanguageNameKind.Reference,
            SourceSet = Iso639Set.Set3,
            IsReferenceName = true
        };
        LanguageAlias bibliographicAlias = new LanguageAlias
        {
            Value = "fre",
            Kind = LanguageAliasKind.Set2Bibliographic,
            PreferredCode = "fra",
            SourceSet = Iso639Set.Set2
        };
        LanguageRelationship relationship = new LanguageRelationship
        {
            ParentCode = "roa",
            ChildCode = "fra",
            Kind = LanguageRelationshipKind.Set5ParentChild,
            SourceSet = Iso639Set.Set5
        };
        LanguageCodeEntry entry = new LanguageCodeEntry
        {
            Alpha2Code = "fr",
            Set2TerminologyCode = "fra",
            Set2BibliographicCode = "fre",
            Set3Code = "fra",
            Set5Code = null,
            InSet1 = true,
            InSet2 = true,
            InSet3 = true,
            InSet5 = false,
            Scope = LanguageScope.Individual,
            LanguageType = LanguageType.Living,
            Status = LanguageCodeStatus.Active,
            ReferenceName = "French",
            Names = new[] { referenceName },
            Aliases = new[] { bibliographicAlias },
            Relationships = new[] { relationship },
            Retirement = null,
            Comment = "Representative reconciled entry"
        };

        Assert.Multiple(() =>
        {
            Assert.That(entry.Alpha2Code, Is.EqualTo("fr"));
            Assert.That(entry.Set2TerminologyCode, Is.EqualTo("fra"));
            Assert.That(entry.Set2BibliographicCode, Is.EqualTo("fre"));
            Assert.That(entry.Set3Code, Is.EqualTo("fra"));
            Assert.That(entry.Set5Code, Is.Null);
            Assert.That(entry.InSet1, Is.True);
            Assert.That(entry.InSet2, Is.True);
            Assert.That(entry.InSet3, Is.True);
            Assert.That(entry.InSet5, Is.False);
            Assert.That(entry.Scope, Is.EqualTo(LanguageScope.Individual));
            Assert.That(entry.LanguageType, Is.EqualTo(LanguageType.Living));
            Assert.That(entry.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(entry.ReferenceName, Is.EqualTo("French"));
            Assert.That(entry.Names, Is.EqualTo(new[] { referenceName }));
            Assert.That(entry.Aliases, Is.EqualTo(new[] { bibliographicAlias }));
            Assert.That(entry.Relationships, Is.EqualTo(new[] { relationship }));
            Assert.That(entry.Retirement, Is.Null);
            Assert.That(entry.Comment, Is.EqualTo("Representative reconciled entry"));
        });
    }

    [Test]
    public void EntryRepresentsRetiredIdentifiersWithoutInventingAReplacement()
    {
        LanguageRetirement retirement = new LanguageRetirement
        {
            RetiredCode = "agp",
            FormerReferenceName = "Paranan",
            Reason = LanguageRetirementReason.Split,
            ReplacementCodes = new[] { "apf", "prf" },
            EffectiveDate = "2010-01-18"
        };
        LanguageCodeEntry entry = new LanguageCodeEntry
        {
            Set3Code = "agp",
            InSet3 = true,
            Scope = LanguageScope.Individual,
            LanguageType = LanguageType.Living,
            Status = LanguageCodeStatus.Retired,
            ReferenceName = "Paranan",
            Retirement = retirement
        };

        Assert.Multiple(() =>
        {
            Assert.That(entry.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(entry.Retirement, Is.SameAs(retirement));
            Assert.That(entry.Retirement?.ReplacementCodes, Is.EqualTo(new[] { "apf", "prf" }));
        });
    }

    [Test]
    public void EntryInstancesDoNotShareDetailArrays()
    {
        LanguageCodeEntry first = new LanguageCodeEntry();
        LanguageCodeEntry second = new LanguageCodeEntry();

        Assert.Multiple(() =>
        {
            Assert.That(first.Names, Is.Empty);
            Assert.That(first.Aliases, Is.Empty);
            Assert.That(first.Relationships, Is.Empty);
            Assert.That(first.Names, Is.Not.SameAs(second.Names));
            Assert.That(first.Aliases, Is.Not.SameAs(second.Aliases));
            Assert.That(first.Relationships, Is.Not.SameAs(second.Relationships));
        });
    }
}
