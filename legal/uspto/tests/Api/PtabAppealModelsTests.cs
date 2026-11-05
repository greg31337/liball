using uspto;

namespace uspto.Tests;

public class PtabAppealModelsTests
{
    [Test]
    public void AppealDecisionDefaultsAreIndependentAndReadyForUse()
    {
        PtabAppealDecision first = new PtabAppealDecision();
        PtabAppealDecision second = new PtabAppealDecision();
        first.Appellant.Counsel.Name = "Counsel";
        first.Metadata.UnknownFields["future"] = "true";
        first.Examiner.Name = "Examiner";

        Assert.Multiple(() =>
        {
            Assert.That(second.Appellant.Counsel.Name, Is.Null);
            Assert.That(second.Metadata.UnknownFields, Is.Empty);
            Assert.That(second.Examiner.Name, Is.Null);
            Assert.That(first.Document, Is.Not.SameAs(second.Document));
            Assert.That(new PtabAppealDecisionResponse().Decisions, Is.Empty);
        });
    }
}
