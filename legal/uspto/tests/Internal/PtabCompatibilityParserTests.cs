using uspto;

namespace uspto.Tests;

public class PtabCompatibilityParserTests
{
    [Test]
    public void TrialDecisionAcceptsLiveBagAliasesAndDocumentCoercions()
    {
        string json = "{\"count\":1,\"patentTrialDocumentDataBag\":[{\"trialNumber\":\"IPR2025-00001\",\"patentOwnerData\":{\"realPartyName\":\"Owner Group\",\"techCenterNumber\":3600,\"futureOwner\":true},\"documentData\":{\"documentIdentifier\":170224750,\"documentNumber\":\"12\",\"documentTypeCategory\":\"Final Decision\",\"downloadURI\":\"https://ptab.uspto.gov/doc/170224750\",\"futureDocument\":9},\"decisionData\":{\"statuteAndRuleBag\":\"35 USC 103\",\"issueTypeBag\":\"Obviousness\",\"futureDecision\":true}}]}";

        PtabTrialDecisionResponse response = OdpPtabTrialDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null);

        PtabTrialDecision decision = response.Decisions[0];
        Assert.Multiple(() =>
        {
            Assert.That(decision.PatentOwner.RealPartyInInterestName, Is.EqualTo("Owner Group"));
            Assert.That(decision.PatentOwner.TechnologyCenter, Is.EqualTo("3600"));
            Assert.That(decision.PatentOwner.UnknownFields.Keys, Is.EqualTo(new[] { "futureOwner" }));
            Assert.That(decision.Document.DocumentIdentifier, Is.EqualTo("170224750"));
            Assert.That(decision.Document.DocumentNumber, Is.EqualTo(12));
            Assert.That(decision.Document.HasDocumentNumber, Is.True);
            Assert.That(decision.Document.TypeDescription, Is.EqualTo("Final Decision"));
            Assert.That(decision.Document.DownloadUri?.AbsoluteUri, Is.EqualTo("https://ptab.uspto.gov/doc/170224750"));
            Assert.That(decision.Document.UnknownFields.Keys, Is.EqualTo(new[] { "futureDocument" }));
            Assert.That(decision.Decision.StatutesAndRules, Is.EqualTo(new[] { "35 USC 103" }));
            Assert.That(decision.Decision.IssueTypes, Is.EqualTo(new[] { "Obviousness" }));
            Assert.That(decision.Decision.UnknownFields.Keys, Is.EqualTo(new[] { "futureDecision" }));
        });
    }

    [Test]
    public void TrialDocumentAcceptsLiveAliasesAndScalarCoercions()
    {
        string json = "{\"count\":\"1\",\"totalRecordCount\":\"9\",\"patentTrialDocumentDataBag\":[{\"trialNumber\":202500002,\"patentOwnerData\":{\"realPartyName\":\"Owner Alias\",\"techCenterNumber\":3600},\"documentData\":{\"documentIdentifier\":170224751,\"documentNumber\":\"7\",\"documentSizeQuantity\":\"45678\",\"documentTypeCategory\":\"Petition\",\"downloadURI\":\"https://ptab.uspto.gov/doc/170224751\",\"documentOCRText\":true,\"mimeTypeIdentifier\":\"PDF\"},\"decisionData\":{\"statuteAndRuleBag\":\"37 CFR 42.100\",\"issueTypeBag\":\"Institution\"}}],\"facets\":[{\"documentData.documentCategory\":[{\"name\":2025,\"quantity\":\"4\"}]}]}";

        PtabTrialDocumentResponse response = OdpPtabTrialDocumentParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null);

        PtabTrialDocument document = response.Documents[0];
        Assert.Multiple(() =>
        {
            Assert.That(response.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(9L));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Facets[0].Values[0].Name, Is.EqualTo("2025"));
            Assert.That(response.Facets[0].Values[0].Count, Is.EqualTo(4L));
            Assert.That(document.TrialNumber, Is.EqualTo("202500002"));
            Assert.That(document.PatentOwner.RealPartyInInterestName, Is.EqualTo("Owner Alias"));
            Assert.That(document.PatentOwner.TechnologyCenter, Is.EqualTo("3600"));
            Assert.That(document.Document.DocumentIdentifier, Is.EqualTo("170224751"));
            Assert.That(document.Document.DocumentNumber, Is.EqualTo(7));
            Assert.That(document.Document.Size, Is.EqualTo(45678L));
            Assert.That(document.Document.TypeDescription, Is.EqualTo("Petition"));
            Assert.That(document.Document.DownloadUri?.AbsoluteUri, Is.EqualTo("https://ptab.uspto.gov/doc/170224751"));
            Assert.That(document.Document.OcrText, Is.EqualTo("true"));
            Assert.That(document.Document.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(document.Decision.StatutesAndRules, Is.EqualTo(new[] { "37 CFR 42.100" }));
            Assert.That(document.Decision.IssueTypes, Is.EqualTo(new[] { "Institution" }));
        });
    }

    [Test]
    public void AppealAcceptsLegacyAppellantRequesterAndDocumentNames()
    {
        string json = "{\"count\":1,\"patentAppealDataBag\":[{\"appealNumber\":\"2025-100001\",\"appealMetaData\":{\"downloadURI\":\"/download\",\"futureMetadata\":true},\"appelantData\":{\"realPartyName\":\"Appellant LLC\",\"techCenterNumber\":2100,\"futureAppellant\":7},\"requestorData\":{\"thirdPartyName\":\"Requester Inc.\",\"futureRequester\":false},\"examinerData\":{\"techCenterNumber\":2100,\"futureExaminer\":1},\"documentData\":{\"documentIdentifier\":\"A-1\",\"documentNumber\":\"8\",\"documentTypeCategory\":\"Appeal Decision\",\"downloadURI\":\"https://ptab.uspto.gov/doc/A-1\"},\"decisionData\":{\"statuteAndRuleBag\":\"35 USC 101\",\"issueTypeBag\":\"Eligibility\"},\"futureRecord\":{\"value\":1}}]}";

        PtabAppealDecisionResponse response = OdpPtabAppealDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null);

        PtabAppealDecision decision = response.Decisions[0];
        Assert.Multiple(() =>
        {
            Assert.That(decision.Metadata.FileDownloadUri?.ToString(), Is.EqualTo("/download"));
            Assert.That(decision.Metadata.UnknownFields.Keys, Is.EqualTo(new[] { "futureMetadata" }));
            Assert.That(decision.Appellant.RealPartyInInterestName, Is.EqualTo("Appellant LLC"));
            Assert.That(decision.Appellant.TechnologyCenter, Is.EqualTo("2100"));
            Assert.That(decision.Appellant.UnknownFields.Keys, Is.EqualTo(new[] { "futureAppellant" }));
            Assert.That(decision.ThirdPartyRequester.Name, Is.EqualTo("Requester Inc."));
            Assert.That(decision.ThirdPartyRequester.UnknownFields.Keys, Is.EqualTo(new[] { "futureRequester" }));
            Assert.That(decision.Examiner.TechnologyCenter, Is.EqualTo("2100"));
            Assert.That(decision.Examiner.UnknownFields.Keys, Is.EqualTo(new[] { "futureExaminer" }));
            Assert.That(decision.Document.DocumentNumber, Is.EqualTo(8));
            Assert.That(decision.Document.TypeDescription, Is.EqualTo("Appeal Decision"));
            Assert.That(decision.Document.DownloadUri?.AbsoluteUri, Is.EqualTo("https://ptab.uspto.gov/doc/A-1"));
            Assert.That(decision.Decision.StatutesAndRules, Is.EqualTo(new[] { "35 USC 101" }));
            Assert.That(decision.Decision.IssueTypes, Is.EqualTo(new[] { "Eligibility" }));
            Assert.That(decision.UnknownFields.Keys, Is.EqualTo(new[] { "futureRecord" }));
        });
    }

    [Test]
    public void InterferenceAcceptsLiveDocumentContainerAndScalarLists()
    {
        string json = "{\"count\":1,\"patentInterferenceDataBag\":[{\"interferenceNumber\":\"105000\",\"interferenceMetaData\":{\"downloadURI\":\"/download\",\"futureMetadata\":2},\"seniorPartyData\":{\"realPartyName\":\"Senior Party\",\"techCenterNumber\":1700,\"futureSenior\":true},\"additionalPartyDataBag\":[{\"additionalPartyName\":\"Additional Party\",\"futureAdditional\":9}],\"documentData\":{\"documentIdentifier\":\"I-1\",\"documentSizeQuantity\":45224,\"documentTypeCategory\":\"Judgment\",\"mimeTypeIdentifier\":\"application/pdf\",\"downloadURI\":\"https://ptab.uspto.gov/doc/I-1\",\"statuteAndRuleBag\":\"37 CFR 41.203\",\"issueTypeBag\":\"Priority\",\"futureDocument\":false},\"futureRecord\":3}]}";

        PtabInterferenceDecisionResponse response = OdpPtabInterferenceDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null);

        PtabInterferenceDecision decision = response.Decisions[0];
        Assert.Multiple(() =>
        {
            Assert.That(decision.Metadata.FileDownloadUri?.ToString(), Is.EqualTo("/download"));
            Assert.That(decision.Metadata.UnknownFields.Keys, Is.EqualTo(new[] { "futureMetadata" }));
            Assert.That(decision.SeniorParty.RealPartyInInterestName, Is.EqualTo("Senior Party"));
            Assert.That(decision.SeniorParty.TechnologyCenter, Is.EqualTo("1700"));
            Assert.That(decision.SeniorParty.UnknownFields.Keys, Is.EqualTo(new[] { "futureSenior" }));
            Assert.That(decision.AdditionalParties[0].Name, Is.EqualTo("Additional Party"));
            Assert.That(decision.AdditionalParties[0].UnknownFields.Keys, Is.EqualTo(new[] { "futureAdditional" }));
            Assert.That(decision.Document.Size, Is.EqualTo(45224L));
            Assert.That(decision.Document.TypeDescription, Is.EqualTo("Judgment"));
            Assert.That(decision.Document.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(decision.Document.DownloadUri?.AbsoluteUri, Is.EqualTo("https://ptab.uspto.gov/doc/I-1"));
            Assert.That(decision.Document.UnknownFields.Keys, Is.EqualTo(new[] { "futureDocument" }));
            Assert.That(decision.Decision.StatutesAndRules, Is.EqualTo(new[] { "37 CFR 41.203" }));
            Assert.That(decision.Decision.IssueTypes, Is.EqualTo(new[] { "Priority" }));
            Assert.That(decision.UnknownFields.Keys, Is.EqualTo(new[] { "futureRecord" }));
        });
    }

    [Test]
    public void CanonicalFieldsTakePrecedenceOverAliases()
    {
        string json = "{\"count\":1,\"patentAppealDataBag\":[{\"appealNumber\":\"A-1\",\"appellantData\":{\"realPartyInInterestName\":\"Canonical Party\",\"realPartyName\":\"Alias Party\",\"technologyCenterNumber\":\"2100\",\"techCenterNumber\":\"2200\"},\"appelantData\":{\"realPartyName\":\"Alternate Container\"},\"thirdPartyRequesterData\":{\"thirdPartyName\":\"Canonical Requester\"},\"requestorData\":{\"thirdPartyName\":\"Alias Requester\"},\"documentData\":{\"documentIdentifier\":\"A-1\",\"documentTypeDescriptionText\":\"Canonical Type\",\"documentTypeCategory\":\"Alias Type\",\"fileDownloadURI\":\"https://ptab.uspto.gov/doc/canonical\",\"downloadURI\":\"https://ptab.uspto.gov/doc/alias\"}}]}";

        PtabAppealDecision decision = OdpPtabAppealDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null).Decisions[0];

        Assert.Multiple(() =>
        {
            Assert.That(decision.Appellant.RealPartyInInterestName, Is.EqualTo("Canonical Party"));
            Assert.That(decision.Appellant.TechnologyCenter, Is.EqualTo("2100"));
            Assert.That(decision.ThirdPartyRequester.Name, Is.EqualTo("Canonical Requester"));
            Assert.That(decision.Document.TypeDescription, Is.EqualTo("Canonical Type"));
            Assert.That(decision.Document.DownloadUri?.AbsoluteUri, Is.EqualTo("https://ptab.uspto.gov/doc/canonical"));
        });
    }

    [Test]
    public void InvalidCanonicalFieldDoesNotFallThroughToValidAlias()
    {
        string json = "{\"count\":1,\"patentAppealDataBag\":[{\"appellantData\":[],\"appelantData\":{\"realPartyName\":\"Alias Party\"}}]}";

        ParseException exception = Assert.Throws<ParseException>(() => OdpPtabAppealDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentAppealDataBag[0].appellantData"));
    }

    [Test]
    public void NonStringScalarDecisionListStillFailsAtItsSourcePath()
    {
        string json = "{\"count\":1,\"patentInterferenceDataBag\":[{\"decisionDocumentData\":{\"statuteAndRuleBag\":7}}]}";

        ParseException exception = Assert.Throws<ParseException>(() => OdpPtabInterferenceDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP PTAB interference decision"));
            Assert.That(exception.DataPath, Is.EqualTo("$.patentInterferenceDataBag[0].decisionDocumentData.statuteAndRuleBag"));
        });
    }

    private static HttpResponseMetadata CreateMetadata()
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/trials/decisions/search"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "compatibility-request"
        };
    }
}
