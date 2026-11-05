using languagecodes;

namespace languagecodes.Tests;

public class LanguageDetailDataTests
{
    [Test]
    public void LanguageNameStoresSourceAndClassification()
    {
        LanguageName name = new LanguageName
        {
            Text = "anglais",
            NameLanguage = "fr",
            Kind = LanguageNameKind.French,
            SourceSet = Iso639Set.Set2,
            IsReferenceName = false
        };

        Assert.Multiple(() =>
        {
            Assert.That(name.Text, Is.EqualTo("anglais"));
            Assert.That(name.NameLanguage, Is.EqualTo("fr"));
            Assert.That(name.Kind, Is.EqualTo(LanguageNameKind.French));
            Assert.That(name.SourceSet, Is.EqualTo(Iso639Set.Set2));
            Assert.That(name.IsReferenceName, Is.False);
        });
    }

    [Test]
    public void LanguageAliasStoresPreferredCodeAndLifecycle()
    {
        LanguageAlias alias = new LanguageAlias
        {
            Value = "fre",
            Kind = LanguageAliasKind.Set2Bibliographic,
            PreferredCode = "fra",
            IsDeprecated = false,
            SourceSet = Iso639Set.Set2
        };

        Assert.Multiple(() =>
        {
            Assert.That(alias.Value, Is.EqualTo("fre"));
            Assert.That(alias.Kind, Is.EqualTo(LanguageAliasKind.Set2Bibliographic));
            Assert.That(alias.PreferredCode, Is.EqualTo("fra"));
            Assert.That(alias.IsDeprecated, Is.False);
            Assert.That(alias.SourceSet, Is.EqualTo(Iso639Set.Set2));
        });
    }

    [Test]
    public void LanguageRelationshipStoresDirectionSourceAndLifecycle()
    {
        LanguageRelationship relationship = new LanguageRelationship
        {
            ParentCode = "ara",
            ChildCode = "aao",
            Kind = LanguageRelationshipKind.MacrolanguageMember,
            SourceSet = Iso639Set.Set3,
            IsRetired = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(relationship.ParentCode, Is.EqualTo("ara"));
            Assert.That(relationship.ChildCode, Is.EqualTo("aao"));
            Assert.That(relationship.Kind, Is.EqualTo(LanguageRelationshipKind.MacrolanguageMember));
            Assert.That(relationship.SourceSet, Is.EqualTo(Iso639Set.Set3));
            Assert.That(relationship.IsRetired, Is.True);
        });
    }

    [Test]
    public void LanguageRetirementStoresSplitCandidatesAndRemedy()
    {
        LanguageRetirement retirement = new LanguageRetirement
        {
            RetiredCode = "agp",
            FormerReferenceName = "Paranan",
            Reason = LanguageRetirementReason.Split,
            ReplacementCodes = new[] { "apf", "prf" },
            Remedy = "Split into two languages",
            EffectiveDate = "2010-01-18"
        };

        Assert.Multiple(() =>
        {
            Assert.That(retirement.RetiredCode, Is.EqualTo("agp"));
            Assert.That(retirement.FormerReferenceName, Is.EqualTo("Paranan"));
            Assert.That(retirement.Reason, Is.EqualTo(LanguageRetirementReason.Split));
            Assert.That(retirement.ReplacementCodes, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(retirement.Remedy, Is.EqualTo("Split into two languages"));
            Assert.That(retirement.EffectiveDate, Is.EqualTo("2010-01-18"));
        });
    }

    [Test]
    public void LanguageRetirementInstancesDoNotShareReplacementArrays()
    {
        LanguageRetirement first = new LanguageRetirement();
        LanguageRetirement second = new LanguageRetirement();

        Assert.Multiple(() =>
        {
            Assert.That(first.ReplacementCodes, Is.Empty);
            Assert.That(second.ReplacementCodes, Is.Empty);
            Assert.That(first.ReplacementCodes, Is.Not.SameAs(second.ReplacementCodes));
        });
    }

    [Test]
    public void LanguageDetailsRowKeepsTheEntryAggregateTogether()
    {
        LanguageCodeEntry entry = new LanguageCodeEntry();
        LanguageDetailsRow details = new LanguageDetailsRow(entry);

        Assert.That(details.Entry, Is.SameAs(entry));
    }
}
