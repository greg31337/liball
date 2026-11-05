using uspto;

namespace uspto.Tests;

public class ClassificationModelsTests
{
    [Test]
    public void ClassificationDefaultsPreserveMissingValueState()
    {
        PatentCpcClassification cpc = new PatentCpcClassification();
        PatentIpcClassification ipc = new PatentIpcClassification();
        PatentUspcClassification uspc = new PatentUspcClassification();
        PatentWipoClassification wipo = new PatentWipoClassification();

        Assert.Multiple(() =>
        {
            Assert.That(cpc.Symbol, Is.Null);
            Assert.That(cpc.Sequence, Is.Zero);
            Assert.That(cpc.HasSequence, Is.False);
            Assert.That(cpc.IsInventive, Is.False);
            Assert.That(cpc.HasIsInventive, Is.False);
            Assert.That(cpc.IsPrimary, Is.False);
            Assert.That(cpc.HasIsPrimary, Is.False);
            Assert.That(ipc.Symbol, Is.Null);
            Assert.That(ipc.Sequence, Is.Zero);
            Assert.That(ipc.HasSequence, Is.False);
            Assert.That(uspc.Symbol, Is.Null);
            Assert.That(uspc.HasSequence, Is.False);
            Assert.That(uspc.HasIsPrimary, Is.False);
            Assert.That(wipo.FieldCode, Is.Null);
            Assert.That(wipo.HasSequence, Is.False);
        });
    }

    [Test]
    public void CpcAndIpcRetainTheirHierarchicalComponents()
    {
        PatentCpcClassification cpc = new PatentCpcClassification
        {
            Symbol = "H01L29/66325",
            Section = "H",
            Class = "01",
            Subclass = "L",
            MainGroup = "29",
            Subgroup = "66325",
            Version = "2024.01",
            Sequence = 1,
            HasSequence = true,
            IsInventive = true,
            HasIsInventive = true,
            IsPrimary = true,
            HasIsPrimary = true
        };
        PatentIpcClassification ipc = new PatentIpcClassification
        {
            Symbol = "G06F 16/00",
            Section = "G",
            Class = "06",
            Subclass = "F",
            MainGroup = "16",
            Subgroup = "00",
            Version = "2024.01",
            ClassificationLevel = "A",
            PositionCode = "F",
            ValueCode = "I",
            Sequence = 2,
            HasSequence = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(cpc.Symbol, Is.EqualTo("H01L29/66325"));
            Assert.That(cpc.MainGroup, Is.EqualTo("29"));
            Assert.That(cpc.Subgroup, Is.EqualTo("66325"));
            Assert.That(cpc.IsInventive, Is.True);
            Assert.That(cpc.HasIsInventive, Is.True);
            Assert.That(cpc.IsPrimary, Is.True);
            Assert.That(ipc.Symbol, Is.EqualTo("G06F 16/00"));
            Assert.That(ipc.ClassificationLevel, Is.EqualTo("A"));
            Assert.That(ipc.PositionCode, Is.EqualTo("F"));
            Assert.That(ipc.ValueCode, Is.EqualTo("I"));
            Assert.That(ipc.HasSequence, Is.True);
        });
    }

    [Test]
    public void UspcAndWipoKeepSchemeSpecificData()
    {
        PatentUspcClassification uspc = new PatentUspcClassification
        {
            Symbol = "257/197000",
            Class = "257",
            Subclass = "197000",
            ClassificationCategory = "original",
            Sequence = 1,
            HasSequence = true,
            IsPrimary = true,
            HasIsPrimary = true
        };
        PatentWipoClassification wipo = new PatentWipoClassification
        {
            FieldCode = "10",
            FieldName = "Semiconductors",
            Sector = "Electrical engineering",
            Sequence = 3,
            HasSequence = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(uspc.Symbol, Is.EqualTo("257/197000"));
            Assert.That(uspc.Class, Is.EqualTo("257"));
            Assert.That(uspc.Subclass, Is.EqualTo("197000"));
            Assert.That(uspc.IsPrimary, Is.True);
            Assert.That(uspc.HasIsPrimary, Is.True);
            Assert.That(wipo.FieldCode, Is.EqualTo("10"));
            Assert.That(wipo.FieldName, Is.EqualTo("Semiconductors"));
            Assert.That(wipo.Sector, Is.EqualTo("Electrical engineering"));
            Assert.That(wipo.Sequence, Is.EqualTo(3));
        });
    }

    [Test]
    public void ClassificationDataKeepsSchemesAndGovernmentInterestsSeparate()
    {
        PatentClassificationData classifications = new PatentClassificationData();
        PatentGovernmentInterest interest = new PatentGovernmentInterest
        {
            Statement = "The government has certain rights in this invention.",
            AgencyName = "National Science Foundation",
            AgencyIdentifier = "NSF",
            AwardNumber = "IIP-1234567",
            RecipientOrganization = new PatentOrganization { Name = "Example University" },
            RightsCategory = "federal",
            RelatedClassifications = classifications
        };
        classifications.Cpc = new PatentCpcClassification[]
        {
            new PatentCpcClassification { Symbol = "H01L29/66325" }
        };
        classifications.Ipc = new PatentIpcClassification[]
        {
            new PatentIpcClassification { Symbol = "G06F 16/00" }
        };
        classifications.Uspc = new PatentUspcClassification[]
        {
            new PatentUspcClassification { Symbol = "257/197000" }
        };
        classifications.Wipo = new PatentWipoClassification[]
        {
            new PatentWipoClassification { FieldName = "Semiconductors" }
        };
        classifications.GovernmentInterests = new PatentGovernmentInterest[] { interest };

        Assert.Multiple(() =>
        {
            Assert.That(classifications.Cpc[0].Symbol, Is.EqualTo("H01L29/66325"));
            Assert.That(classifications.Ipc[0].Symbol, Is.EqualTo("G06F 16/00"));
            Assert.That(classifications.Uspc[0].Symbol, Is.EqualTo("257/197000"));
            Assert.That(classifications.Wipo[0].FieldName, Is.EqualTo("Semiconductors"));
            Assert.That(classifications.GovernmentInterests[0].AgencyIdentifier, Is.EqualTo("NSF"));
            Assert.That(classifications.GovernmentInterests[0].RecipientOrganization?.Name, Is.EqualTo("Example University"));
            Assert.That(classifications.GovernmentInterests[0].RelatedClassifications, Is.SameAs(classifications));
        });
    }

    [Test]
    public void MutableClassificationDefaultsAreIndependent()
    {
        PatentClassificationData first = new PatentClassificationData();
        PatentClassificationData second = new PatentClassificationData();

        first.Cpc = new PatentCpcClassification[] { new PatentCpcClassification { Symbol = "A01B1/00" } };

        Assert.Multiple(() =>
        {
            Assert.That(first.Cpc, Has.Length.EqualTo(1));
            Assert.That(second.Cpc, Is.Empty);
            Assert.That(first.GovernmentInterests, Is.Not.SameAs(second.GovernmentInterests));
        });
    }
}
