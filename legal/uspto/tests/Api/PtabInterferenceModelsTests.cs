using uspto;

namespace uspto.Tests;

public class PtabInterferenceModelsTests
{
    [Test]
    public void InterferenceDecisionDefaultsAreIndependentAndReadyForUse()
    {
        PtabInterferenceDecision first = new PtabInterferenceDecision();
        PtabInterferenceDecision second = new PtabInterferenceDecision();
        first.SeniorParty.Counsel.Name = "Counsel";
        first.Metadata.UnknownFields["future"] = "true";
        first.AdditionalParties = new PtabInterferenceParty[] { new PtabInterferenceParty() };

        Assert.Multiple(() =>
        {
            Assert.That(second.SeniorParty.Counsel.Name, Is.Null);
            Assert.That(second.Metadata.UnknownFields, Is.Empty);
            Assert.That(second.AdditionalParties, Is.Empty);
            Assert.That(first.Document, Is.Not.SameAs(second.Document));
            Assert.That(new PtabInterferenceDecisionResponse().Decisions, Is.Empty);
        });
    }
}
