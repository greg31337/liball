using uspto;

namespace uspto.Tests;

public class PatentSubEndpointEmptyFixtureTests
{
    [Test]
    public void TransactionFixtureMapsAnExplicitlyEmptyEventCollection()
    {
        PatentTransactionHistory result = OdpPatentTransactionParser.Parse(
            FixtureLoader.LoadText("patent-subendpoints/transactions-empty.json"),
            "14412875",
            Metadata("transactions"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.RequestIdentifier, Is.EqualTo("fixture-request"));
        });
    }

    [Test]
    public void DocumentFixtureMapsAnExplicitlyEmptyDocumentCollection()
    {
        PatentDocumentList result = OdpPatentDocumentParser.Parse(
            FixtureLoader.LoadText("patent-subendpoints/documents-empty.json"),
            "16123123",
            Metadata("documents"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("16123123"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Documents, Is.Empty);
            Assert.That(result.RequestIdentifier, Is.EqualTo("fixture-request"));
        });
    }

    [Test]
    public void AssociatedDocumentFixtureMapsExplicitlyEmptyFileCollections()
    {
        PatentAssociatedDocumentList result = OdpPatentAssociatedDocumentParser.Parse(
            FixtureLoader.LoadText("patent-subendpoints/associated-documents-empty.json"),
            "14412875",
            Metadata("associated-documents"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.PublicationFiles, Is.Empty);
            Assert.That(result.GrantFiles, Is.Empty);
        });
    }

    [Test]
    public void ContinuityFixtureMapsExplicitlyEmptyRelationshipCollections()
    {
        PatentContinuity result = OdpPatentContinuityPriorityParser.ParseContinuity(
            FixtureLoader.LoadText("patent-subendpoints/continuity-empty.json"),
            "14412875",
            Metadata("continuity"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Parents, Is.Empty);
            Assert.That(result.Children, Is.Empty);
        });
    }

    [Test]
    public void PriorityFixtureMapsAnExplicitlyEmptyClaimCollection()
    {
        PatentForeignPriorityList result = OdpPatentContinuityPriorityParser.ParseForeignPriorities(
            FixtureLoader.LoadText("patent-subendpoints/priority-empty.json"),
            "14412875",
            Metadata("foreign-priority"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Claims, Is.Empty);
        });
    }

    [Test]
    public void AssignmentFixtureMapsAnExplicitlyEmptyAssignmentCollection()
    {
        PatentAssignmentHistory result = OdpPatentAssignmentParser.Parse(
            FixtureLoader.LoadText("patent-subendpoints/assignments-empty.json"),
            "15000001",
            Metadata("assignment"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("15000001"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Assignments, Is.Empty);
        });
    }

    [Test]
    public void AttorneyFixtureMapsExplicitlyEmptyPractitionerCollections()
    {
        PatentAttorneyRecord result = OdpPatentAttorneyParser.Parse(
            FixtureLoader.LoadText("patent-subendpoints/attorneys-empty.json"),
            "14104993",
            Metadata("attorney"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Attorneys, Is.Empty);
            Assert.That(result.Correspondence, Is.Empty);
        });
    }

    [Test]
    public void PatentTermAdjustmentFixtureMapsAnExplicitlyEmptyHistoryCollection()
    {
        PatentTermAdjustment result = OdpPatentTermAdjustmentParser.Parse(
            FixtureLoader.LoadText("patent-subendpoints/pta-empty.json"),
            "17248024",
            Metadata("adjustment"));

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("17248024"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.History, Is.Empty);
            Assert.That(result.HasAdjustmentTotalDays, Is.False);
        });
    }

    [Test]
    public void StatusCodeFixtureMapsAnExplicitlyEmptyCatalog()
    {
        PatentStatusCodeResponse result = OdpPatentStatusCodeParser.Parse(
            FixtureLoader.LoadText("patent-subendpoints/status-codes-empty.json"),
            new SearchRequest(),
            Metadata("status-codes"),
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Total, Is.Zero);
            Assert.That(result.HasTotal, Is.True);
            Assert.That(result.StatusCodes, Is.Empty);
            Assert.That(result.DescriptionsByCode, Is.Empty);
        });
    }

    private static HttpResponseMetadata Metadata(string endpoint)
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/fixture/" + endpoint),
            RequestIdentifier = "fixture-request",
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json
        };
    }
}
