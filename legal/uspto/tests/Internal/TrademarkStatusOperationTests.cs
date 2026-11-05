using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class TrademarkStatusOperationTests
{
    [TestCase("SN 90/123,456", "sn90123456", TrademarkIdentifierKind.SerialNumber)]
    [TestCase("RN 7,654,321", "rn7654321", TrademarkIdentifierKind.RegistrationNumber)]
    [TestCase("REF A0123456", "refA0123456", TrademarkIdentifierKind.ReferenceNumber)]
    public void JsonRetrievalNormalizesPublishedIdentifierKinds(string identifier, string canonical, TrademarkIdentifierKind kind)
    {
        RecordingTrademarkStatusHandler handler = new RecordingTrademarkStatusHandler(HttpStatusCode.OK, JsonResponse(), UsptoMediaTypes.Json);
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusOperation operation = new TrademarkStatusOperation(client, Options(client));

        TrademarkCaseStatus result = operation.GetStatus(identifier, DocumentFormat.Json, new RequestContext { RetainRawResponse = true });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/casestatus/" + canonical + "/info"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(result.RequestedIdentifier, Is.EqualTo(identifier));
            Assert.That(result.CanonicalIdentifier, Is.EqualTo(canonical));
            Assert.That(result.IdentifierKind, Is.EqualTo(kind));
            Assert.That(result.RawFormat, Is.EqualTo(DocumentFormat.Json));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void JsonRetrievalParsesCaseDomainsAndPreservesUnrecognizedDates()
    {
        RecordingTrademarkStatusHandler handler = new RecordingTrademarkStatusHandler(HttpStatusCode.OK, JsonResponse(), UsptoMediaTypes.Json)
        {
            RequestIdentifier = "tsdr-request"
        };
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusOperation operation = new TrademarkStatusOperation(client, Options(client));

        TrademarkCaseStatus result = operation.GetStatus("sn90123456", DocumentFormat.Json, new RequestContext { RetainRawResponse = true });

        Assert.Multiple(() =>
        {
            Assert.That(result.SerialNumber, Is.EqualTo("90123456"));
            Assert.That(result.RegistrationNumber, Is.EqualTo("7654321"));
            Assert.That(result.Mark.VerbalElementText, Is.EqualTo("ACME ORBIT"));
            Assert.That(result.Mark.IsStandardCharacter, Is.True);
            Assert.That(result.Mark.HasIsStandardCharacter, Is.True);
            Assert.That(result.Mark.DesignSearchClassifications.Single().Code, Is.EqualTo("01.01.03"));
            Assert.That(result.Status.Code, Is.EqualTo(700));
            Assert.That(result.Status.HasCode, Is.True);
            Assert.That(result.Status.StatusDateText, Is.EqualTo("date pending"));
            Assert.That(result.Status.HasStatusDate, Is.False);
            Assert.That(result.Status.FilingDate, Is.EqualTo(new DateTimeOffset(2020, 1, 15, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(result.Status.HasFilingDate, Is.True);
            Assert.That(result.FilingBasis.Section1A, Is.True);
            Assert.That(result.FilingBasis.HasSection1A, Is.True);
            Assert.That(result.CurrentOwners.Single().Name, Is.EqualTo("Acme Holdings LLC"));
            Assert.That(result.CurrentOwners.Single().Address.City, Is.EqualTo("Alexandria"));
            Assert.That(result.PriorOwners.Single().IsCurrent, Is.False);
            Assert.That(result.Attorneys.Single().DocketNumber, Is.EqualTo("ACME-100"));
            Assert.That(result.Correspondence.EmailAddresses, Is.EqualTo(new[] { "trademarks@example.test" }));
            Assert.That(result.ProsecutionEvents.Single().Code, Is.EqualTo("R.PR"));
            Assert.That(result.ProsecutionEvents.Single().EventDate, Is.EqualTo(new DateTimeOffset(2024, 8, 12, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(result.GoodsAndServices.Single().Classes.Select(item => item.Code), Is.EqualTo(new[] { "009", "021" }));
            Assert.That(result.GoodsAndServices.Single().FirstUseDateText, Is.EqualTo("20180301"));
            Assert.That(result.GoodsAndServices.Single().HasFirstUseDate, Is.True);
            Assert.That(result.ForeignRegistrations.Single().CountryCode, Is.EqualTo("EM"));
            Assert.That(result.Madrid.Registrations.Single().RegistrationNumber, Is.EqualTo("1234567"));
            Assert.That(result.Madrid.Events.Single().ActionCode, Is.EqualTo("DESIGNATION"));
            Assert.That(result.Madrid.Relationships.Single().ReferenceNumber, Is.EqualTo("A0123456"));
            Assert.That(result.IsInternationalData, Is.True);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("tsdr-request"));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Does.Contain("[REDACTED]"));
            Assert.That(result.RawResponse, Does.Not.Contain("tsdr-secret"));
            Assert.That(result.UnknownFields["futureCase"], Is.EqualTo("{\"enabled\":true}"));
        });
    }

    [Test]
    public void XmlRetrievalUsesNamespaceAgnosticParsingAndRetainsRawDates()
    {
        RecordingTrademarkStatusHandler handler = new RecordingTrademarkStatusHandler(HttpStatusCode.OK, XmlResponse(), UsptoMediaTypes.Xml);
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusOperation operation = new TrademarkStatusOperation(client, Options(client));

        TrademarkCaseStatus result = operation.GetStatus("RN 7,654,321", DocumentFormat.Xml, new RequestContext { RetainRawResponse = true });

        Assert.Multiple(() =>
        {
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Xml));
            Assert.That(result.CanonicalIdentifier, Is.EqualTo("rn7654321"));
            Assert.That(result.RawFormat, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(result.SerialNumber, Is.EqualTo("90123456"));
            Assert.That(result.RegistrationNumber, Is.EqualTo("7654321"));
            Assert.That(result.Mark.VerbalElementText, Is.EqualTo("ACME ORBIT"));
            Assert.That(result.Status.Code, Is.EqualTo(700));
            Assert.That(result.Status.FilingDateText, Is.EqualTo("2020-01-15"));
            Assert.That(result.Status.HasFilingDate, Is.True);
            Assert.That(result.Status.StatusDateText, Is.EqualTo("0000-00-00"));
            Assert.That(result.Status.HasStatusDate, Is.False);
            Assert.That(result.CurrentOwners.Single().Name, Is.EqualTo("Acme Holdings LLC"));
            Assert.That(result.ProsecutionEvents.Single().Description, Is.EqualTo("Registered"));
            Assert.That(result.GoodsAndServices.Single().PrimeClassCode, Is.EqualTo("009"));
            Assert.That(result.Madrid.Registrations.Single().RegistrationNumber, Is.EqualTo("1234567"));
            Assert.That(result.RawResponse, Does.Contain("TrademarkTransaction"));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void JsonRetrievalMapsCompleteCaseMarkPartyEventGoodsClassAndMadridData()
    {
        string json = """
        {
          "searchId": "complete-json",
          "trademarks": [
            {
              "status": {
                "serialNumber": "90123456",
                "usRegistrationNumber": "7654321",
                "status": 700,
                "statusStr": "REGISTERED",
                "extStatusDesc": "Registered",
                "intStatusDesc": "Active registration",
                "tm5Status": 510,
                "tm5StatusDesc": "Registered/Issued",
                "tm5StatusDef": "A valid registration",
                "statusDate": "not-reported",
                "filingDate": "2020-01-15",
                "usRegistrationDate": "2021-06-05",
                "dateAbandoned": "unknown",
                "dateCancelled": "2031-06-05",
                "dateRenewal": "2030-06-05",
                "registerCategory": "PRINCIPAL",
                "lawOffAsgnCd": "101",
                "currLocationCd": "TMEG LAW OFFICE 101",
                "currentLoc": "Publication and issue section",
                "clsCtActive": 1,
                "clsTotal": 2,
                "markElement": "ACME ORBIT",
                "markType": "TRADEMARK",
                "markDrawingCd": "4",
                "markDrawDesc": "STANDARD CHARACTER MARK",
                "markFeatureCode": "F1",
                "markFeatureDescription": "Motion mark",
                "standardChar": true,
                "colorDrawingCurr": false,
                "colorClaimed": "Blue is claimed.",
                "colorLocation": "Blue appears in ORBIT.",
                "descOfMark": "The mark consists of ACME ORBIT.",
                "disclaimer": "No claim is made to ORBIT.",
                "pseudoMark": "ACME ORBIT",
                "translation": "ORBIT means orbit.",
                "transliteration": "OR-BIT",
                "namePortrait": "Consent is of record.",
                "lineStippling": "Lining indicates shading.",
                "designSearchList": [{"code":"01.01.03","descriptions":["Stars"]}],
                "filedUse": true,
                "filedItu": false,
                "filed44d": true,
                "filed44e": false,
                "filed66a": true,
                "correspondence": {
                  "name": "Jane Lawyer",
                  "organization": "Example Law LLP",
                  "emailAddresses": ["trademarks@example.test"],
                  "phoneNumbers": ["+1-202-555-0101"],
                  "address1": "200 Legal Avenue",
                  "address2": "Suite 5",
                  "city": "Washington",
                  "state": "DC",
                  "postalCode": "20001",
                  "countryCode": "US",
                  "countryName": "United States"
                }
              },
              "soundFile": "https://example.test/mark.mp3",
              "multimediaFiles": ["https://example.test/mark.mp4"],
              "parties": {
                "currentOwners": [
                  {
                    "entityNum": 3,
                    "partyType": 10,
                    "partyTypeDescription": "Owner",
                    "name": "Orbit Labs LLC",
                    "entityType": {"code":"16","name":"Limited Liability Company"},
                    "dbaAkaFormerly": "Orbit Research",
                    "composedOf": "Members",
                    "reelFrame": "1234/0567",
                    "citizenshipCountryCode": "US",
                    "citizenshipCountryName": "United States",
                    "emailList": {"addresses":["owner@example.test"]},
                    "address1": "100 Main Street",
                    "address2": "Floor 2",
                    "city": "Alexandria",
                    "state": "VA",
                    "postalCode": "22314",
                    "countryCode": "US",
                    "countryName": "United States"
                  }
                ],
                "priorOwnerList": [{"name":"Orbit Company"}]
              },
              "attorneys": [
                {
                  "employeeNumber": "12345",
                  "firstName": "Jane",
                  "middleName": "Q",
                  "lastName": "Lawyer",
                  "nameSuffix": "III",
                  "name": "Jane Q Lawyer III",
                  "organization": "Example Law LLP",
                  "docketNumber": "ORBIT-100",
                  "emailAddresses": ["jane@example.test"],
                  "phoneNumbers": ["+1-202-555-0100"],
                  "address1": "200 Legal Avenue",
                  "city": "Washington"
                }
              ],
              "prosecutionHistory": [
                {
                  "entryCode": "R.PR",
                  "entryType": "REGISTRATION",
                  "entryDesc": "Registered",
                  "entryNumber": 12,
                  "sequenceNum": 7,
                  "proceedingNum": "91234567",
                  "docId": "TM-DOC-1",
                  "documentUrl": "https://example.test/documents/1",
                  "confidential": false,
                  "entryDate": "2024-08-12",
                  "dueDate": "2024-09-12",
                  "recordalDate": "2024-08-13T10:11:12Z"
                }
              ],
              "gsList": [
                {
                  "serialNumber": "90123456",
                  "primeClassCode": "009",
                  "description": "Downloadable computer software",
                  "statusCode": "6",
                  "statusDescription": "ACTIVE",
                  "statusDate": "2024-08-12",
                  "firstUseDate": "2018-03-01",
                  "firstUseDateDescription": "At least as early as 2018",
                  "firstUseInCommerceDate": "2019-04-02",
                  "firstUseInCommerceDateDescription": "At least as early as 2019",
                  "pseudoClasses": ["100", "101"],
                  "internationalClasses": [
                    {
                      "code": "009",
                      "description": "Scientific apparatus",
                      "isPrime": true
                    }
                  ],
                  "usClasses": [{"code":"021","description":"Electrical apparatus","prime":false}],
                  "classBasis": {"flag1a":true,"flag1b":false,"flag44d":true,"flag44e":false,"section66A":true}
                }
              ],
              "foreignInfoList": [
                {
                  "frgnApplNum": "EU123456",
                  "frgnRegNum": "EU765432",
                  "frgnCountryCd": "EM",
                  "frgnCountryName": "European Union",
                  "isPriorityClaimed": true,
                  "frgnApplFilingDt": "2020-01-02",
                  "frgnRegDt": "2021-01-02",
                  "frgnExpirationDt": "2031-01-02"
                }
              ],
              "internationalData": true,
              "intlRegistrationList": [
                {
                  "registrationNum": "1234567",
                  "serialNumber": "90123456",
                  "statusCd": 10,
                  "regStatuDesc": "Protected",
                  "priorityClaimed": true,
                  "firstRefusal": false,
                  "registrationDate": "2021-06-05",
                  "regStatusDt": "2022-06-05",
                  "notificationOfDesignationDt": "2021-07-05",
                  "automaticProtectionDt": "2023-01-05",
                  "registrationCancelledDt": "2031-06-05",
                  "renewalDt": "2030-06-05"
                }
              ],
              "relationshipBundleList": [
                {"serialNumber":"90123456","usRegistrationNumber":"7654321","referenceNumber":"A0123456","internationalRegistrationNumber":"1234567"}
              ],
              "usReference": {
                "referenceNum": "A0123456",
                "intlRegistrationNum": "1234567",
                "statusCd": 10,
                "statusCdStr": "PROTECTED",
                "statusDesc": "Protection granted",
                "originalFilingDate": "2020-01-15",
                "statusDate": "2022-06-05",
                "replyByDate": "2022-12-05",
                "renewalDate": "2030-06-05",
                "relatedSerialNumbrs": ["90123456"],
                "relatedIntlRegNumbrs": ["1234567"],
                "madridHistory": [
                  {"eventSerialNum":"M-1","eventActionCode":"DESIGNATION","eventDesc":"Designation received","docId":"MADRID-DOC-1","eventDateTime":"2021-06-05","recordalDate":"2021-06-06"}
                ]
              },
              "futureCase": {"enabled":true}
            }
          ]
        }
        """;
        RecordingTrademarkStatusHandler handler = new RecordingTrademarkStatusHandler(HttpStatusCode.OK, json, UsptoMediaTypes.Json);
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusOperation operation = new TrademarkStatusOperation(client, Options(client));

        TrademarkCaseStatus result = operation.GetStatus("sn90123456", DocumentFormat.Json, new RequestContext());
        TrademarkOwner owner = result.CurrentOwners.Single();
        TrademarkAttorney attorney = result.Attorneys.Single();
        TrademarkProsecutionEvent prosecutionEvent = result.ProsecutionEvents.Single();
        TrademarkGoodsAndService goods = result.GoodsAndServices.Single();
        TrademarkForeignRegistration foreign = result.ForeignRegistrations.Single();
        TrademarkInternationalRegistration registration = result.Madrid.Registrations.Single();
        TrademarkMadridEvent madridEvent = result.Madrid.Events.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.SearchIdentifier, Is.EqualTo("complete-json"));
            Assert.That(result.SerialNumber, Is.EqualTo("90123456"));
            Assert.That(result.RegistrationNumber, Is.EqualTo("7654321"));
            Assert.That(result.ReferenceNumber, Is.EqualTo("A0123456"));
            Assert.That(result.InternationalRegistrationNumber, Is.EqualTo("1234567"));
            Assert.That(result.Mark.Type, Is.EqualTo("TRADEMARK"));
            Assert.That(result.Mark.DrawingCode, Is.EqualTo("4"));
            Assert.That(result.Mark.DrawingDescription, Is.EqualTo("STANDARD CHARACTER MARK"));
            Assert.That(result.Mark.FeatureCode, Is.EqualTo("F1"));
            Assert.That(result.Mark.FeatureDescription, Is.EqualTo("Motion mark"));
            Assert.That(result.Mark.IsStandardCharacter, Is.True);
            Assert.That(result.Mark.HasIsStandardCharacter, Is.True);
            Assert.That(result.Mark.IsColorDrawing, Is.False);
            Assert.That(result.Mark.HasIsColorDrawing, Is.True);
            Assert.That(result.Mark.ColorClaimText, Is.EqualTo("Blue is claimed."));
            Assert.That(result.Mark.ColorLocationText, Is.EqualTo("Blue appears in ORBIT."));
            Assert.That(result.Mark.Description, Is.EqualTo("The mark consists of ACME ORBIT."));
            Assert.That(result.Mark.DisclaimerText, Is.EqualTo("No claim is made to ORBIT."));
            Assert.That(result.Mark.PseudoMarkText, Is.EqualTo("ACME ORBIT"));
            Assert.That(result.Mark.TranslationText, Is.EqualTo("ORBIT means orbit."));
            Assert.That(result.Mark.TransliterationText, Is.EqualTo("OR-BIT"));
            Assert.That(result.Mark.NamePortraitConsentText, Is.EqualTo("Consent is of record."));
            Assert.That(result.Mark.LiningStipplingText, Is.EqualTo("Lining indicates shading."));
            Assert.That(result.Mark.DesignSearchClassifications.Single().Descriptions, Is.EqualTo(new[] { "Stars" }));
            Assert.That(result.Mark.SoundFile, Is.EqualTo("https://example.test/mark.mp3"));
            Assert.That(result.Mark.MultimediaFiles, Is.EqualTo(new[] { "https://example.test/mark.mp4" }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status.CodeText, Is.EqualTo("REGISTERED"));
            Assert.That(result.Status.ExternalDescription, Is.EqualTo("Registered"));
            Assert.That(result.Status.InternalDescription, Is.EqualTo("Active registration"));
            Assert.That(result.Status.Tm5Code, Is.EqualTo(510));
            Assert.That(result.Status.HasTm5Code, Is.True);
            Assert.That(result.Status.Tm5Description, Is.EqualTo("Registered/Issued"));
            Assert.That(result.Status.Tm5Definition, Is.EqualTo("A valid registration"));
            Assert.That(result.Status.StatusDateText, Is.EqualTo("not-reported"));
            Assert.That(result.Status.HasStatusDate, Is.False);
            Assert.That(result.Status.HasFilingDate, Is.True);
            Assert.That(result.Status.HasRegistrationDate, Is.True);
            Assert.That(result.Status.AbandonmentDateText, Is.EqualTo("unknown"));
            Assert.That(result.Status.HasAbandonmentDate, Is.False);
            Assert.That(result.Status.HasCancellationDate, Is.True);
            Assert.That(result.Status.HasRenewalDate, Is.True);
            Assert.That(result.Status.RegisterCategory, Is.EqualTo("PRINCIPAL"));
            Assert.That(result.Status.LawOfficeCode, Is.EqualTo("101"));
            Assert.That(result.Status.CurrentLocationCode, Is.EqualTo("TMEG LAW OFFICE 101"));
            Assert.That(result.Status.CurrentLocationDescription, Is.EqualTo("Publication and issue section"));
            Assert.That(result.Status.ActiveClassCount, Is.EqualTo(1));
            Assert.That(result.Status.HasActiveClassCount, Is.True);
            Assert.That(result.Status.TotalClassCount, Is.EqualTo(2));
            Assert.That(result.Status.HasTotalClassCount, Is.True);
            Assert.That(result.FilingBasis.Section1A, Is.True);
            Assert.That(result.FilingBasis.Section1B, Is.False);
            Assert.That(result.FilingBasis.Section44D, Is.True);
            Assert.That(result.FilingBasis.Section44E, Is.False);
            Assert.That(result.FilingBasis.Section66A, Is.True);
            Assert.That(result.FilingBasis.HasSection1A && result.FilingBasis.HasSection1B && result.FilingBasis.HasSection44D && result.FilingBasis.HasSection44E && result.FilingBasis.HasSection66A, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(owner.SequenceNumber, Is.EqualTo(3));
            Assert.That(owner.HasSequenceNumber, Is.True);
            Assert.That(owner.PartyTypeCode, Is.EqualTo(10));
            Assert.That(owner.HasPartyTypeCode, Is.True);
            Assert.That(owner.PartyTypeDescription, Is.EqualTo("Owner"));
            Assert.That(owner.EntityTypeCode, Is.EqualTo("16"));
            Assert.That(owner.EntityTypeDescription, Is.EqualTo("Limited Liability Company"));
            Assert.That(owner.AlternateNameText, Is.EqualTo("Orbit Research"));
            Assert.That(owner.CompositionText, Is.EqualTo("Members"));
            Assert.That(owner.AssignmentReelFrame, Is.EqualTo("1234/0567"));
            Assert.That(owner.CitizenshipCountryCode, Is.EqualTo("US"));
            Assert.That(owner.CitizenshipCountryName, Is.EqualTo("United States"));
            Assert.That(owner.EmailAddresses, Is.EqualTo(new[] { "owner@example.test" }));
            Assert.That(owner.Address.Line1, Is.EqualTo("100 Main Street"));
            Assert.That(owner.Address.Line2, Is.EqualTo("Floor 2"));
            Assert.That(owner.Address.City, Is.EqualTo("Alexandria"));
            Assert.That(owner.Address.StateOrProvince, Is.EqualTo("VA"));
            Assert.That(owner.Address.PostalCode, Is.EqualTo("22314"));
            Assert.That(owner.Address.CountryCode, Is.EqualTo("US"));
            Assert.That(owner.Address.CountryName, Is.EqualTo("United States"));
            Assert.That(owner.IsCurrent && owner.HasIsCurrent, Is.True);
            Assert.That(result.PriorOwners.Single().Name, Is.EqualTo("Orbit Company"));
            Assert.That(attorney.EmployeeNumber, Is.EqualTo("12345"));
            Assert.That(attorney.FirstName, Is.EqualTo("Jane"));
            Assert.That(attorney.MiddleName, Is.EqualTo("Q"));
            Assert.That(attorney.LastName, Is.EqualTo("Lawyer"));
            Assert.That(attorney.NameSuffix, Is.EqualTo("III"));
            Assert.That(attorney.Organization, Is.EqualTo("Example Law LLP"));
            Assert.That(attorney.DocketNumber, Is.EqualTo("ORBIT-100"));
            Assert.That(attorney.EmailAddresses, Is.EqualTo(new[] { "jane@example.test" }));
            Assert.That(attorney.PhoneNumbers, Is.EqualTo(new[] { "+1-202-555-0100" }));
            Assert.That(result.Correspondence.Name, Is.EqualTo("Jane Lawyer"));
            Assert.That(result.Correspondence.Organization, Is.EqualTo("Example Law LLP"));
            Assert.That(result.Correspondence.EmailAddresses, Is.EqualTo(new[] { "trademarks@example.test" }));
            Assert.That(result.Correspondence.PhoneNumbers, Is.EqualTo(new[] { "+1-202-555-0101" }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(prosecutionEvent.Code, Is.EqualTo("R.PR"));
            Assert.That(prosecutionEvent.Type, Is.EqualTo("REGISTRATION"));
            Assert.That(prosecutionEvent.Description, Is.EqualTo("Registered"));
            Assert.That(prosecutionEvent.EntryNumber, Is.EqualTo(12));
            Assert.That(prosecutionEvent.HasEntryNumber, Is.True);
            Assert.That(prosecutionEvent.SequenceNumber, Is.EqualTo(7));
            Assert.That(prosecutionEvent.HasSequenceNumber, Is.True);
            Assert.That(prosecutionEvent.ProceedingNumber, Is.EqualTo("91234567"));
            Assert.That(prosecutionEvent.DocumentIdentifier, Is.EqualTo("TM-DOC-1"));
            Assert.That(prosecutionEvent.DocumentUri?.AbsoluteUri, Is.EqualTo("https://example.test/documents/1"));
            Assert.That(prosecutionEvent.IsConfidential, Is.False);
            Assert.That(prosecutionEvent.HasIsConfidential, Is.True);
            Assert.That(prosecutionEvent.HasEventDate && prosecutionEvent.HasDueDate && prosecutionEvent.HasRecordedAt, Is.True);
            Assert.That(goods.SerialNumber, Is.EqualTo("90123456"));
            Assert.That(goods.PrimeClassCode, Is.EqualTo("009"));
            Assert.That(goods.Description, Is.EqualTo("Downloadable computer software"));
            Assert.That(goods.StatusCode, Is.EqualTo("6"));
            Assert.That(goods.StatusDescription, Is.EqualTo("ACTIVE"));
            Assert.That(goods.HasStatusDate && goods.HasFirstUseDate && goods.HasFirstUseInCommerceDate, Is.True);
            Assert.That(goods.FirstUseDateDescription, Is.EqualTo("At least as early as 2018"));
            Assert.That(goods.FirstUseInCommerceDateDescription, Is.EqualTo("At least as early as 2019"));
            Assert.That(goods.PseudoClassCodes, Is.EqualTo(new[] { "100", "101" }));
            Assert.That(goods.Classes.Select(item => item.Code), Is.EqualTo(new[] { "009", "021" }));
            Assert.That(goods.Classes.Select(item => item.IsPrime), Is.EqualTo(new[] { true, false }));
            Assert.That(goods.Classes.All(item => item.HasIsPrime), Is.True);
            Assert.That(goods.FilingBasis.HasSection1A && goods.FilingBasis.HasSection1B && goods.FilingBasis.HasSection44D && goods.FilingBasis.HasSection44E && goods.FilingBasis.HasSection66A, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(foreign.ApplicationNumber, Is.EqualTo("EU123456"));
            Assert.That(foreign.RegistrationNumber, Is.EqualTo("EU765432"));
            Assert.That(foreign.CountryCode, Is.EqualTo("EM"));
            Assert.That(foreign.CountryName, Is.EqualTo("European Union"));
            Assert.That(foreign.IsPriorityClaimed && foreign.HasIsPriorityClaimed, Is.True);
            Assert.That(foreign.HasFilingDate && foreign.HasRegistrationDate && foreign.HasExpirationDate, Is.True);
            Assert.That(result.IsInternationalData && result.HasIsInternationalData, Is.True);
            Assert.That(registration.RegistrationNumber, Is.EqualTo("1234567"));
            Assert.That(registration.SerialNumber, Is.EqualTo("90123456"));
            Assert.That(registration.StatusCode, Is.EqualTo(10));
            Assert.That(registration.HasStatusCode, Is.True);
            Assert.That(registration.StatusDescription, Is.EqualTo("Protected"));
            Assert.That(registration.IsPriorityClaimed && registration.HasIsPriorityClaimed, Is.True);
            Assert.That(registration.FirstRefusal, Is.False);
            Assert.That(registration.HasFirstRefusal, Is.True);
            Assert.That(registration.HasRegistrationDate && registration.HasStatusDate && registration.HasDesignationNotificationDate && registration.HasAutomaticProtectionDate && registration.HasCancellationDate && registration.HasRenewalDate, Is.True);
            Assert.That(result.Madrid.StatusCode, Is.EqualTo(10));
            Assert.That(result.Madrid.HasStatusCode, Is.True);
            Assert.That(result.Madrid.StatusCodeText, Is.EqualTo("PROTECTED"));
            Assert.That(result.Madrid.StatusDescription, Is.EqualTo("Protection granted"));
            Assert.That(result.Madrid.HasOriginalFilingDate && result.Madrid.HasStatusDate && result.Madrid.HasReplyByDate && result.Madrid.HasRenewalDate, Is.True);
            Assert.That(result.Madrid.RelatedSerialNumbers, Is.EqualTo(new[] { "90123456" }));
            Assert.That(result.Madrid.RelatedInternationalRegistrationNumbers, Is.EqualTo(new[] { "1234567" }));
            Assert.That(result.Madrid.Relationships.Single().RegistrationNumber, Is.EqualTo("7654321"));
            Assert.That(madridEvent.SerialNumber, Is.EqualTo("M-1"));
            Assert.That(madridEvent.ActionCode, Is.EqualTo("DESIGNATION"));
            Assert.That(madridEvent.Description, Is.EqualTo("Designation received"));
            Assert.That(madridEvent.DocumentIdentifier, Is.EqualTo("MADRID-DOC-1"));
            Assert.That(madridEvent.HasEventDate && madridEvent.HasRecordedAt, Is.True);
            Assert.That(result.UnknownFields["futureCase"], Is.EqualTo("{\"enabled\":true}"));
        });
    }

    [Test]
    public void XmlRetrievalMapsFormatSpecificFieldsAndRetainsUnknownElements()
    {
        string xml = """
        <t:TrademarkTransaction xmlns:t="urn:uspto:tsdr">
          <t:SearchId>complete-xml</t:SearchId>
          <t:Trademark>
            <t:Status>
              <t:SerialNumber>90123456</t:SerialNumber>
              <t:MarkFeatureCode>F1</t:MarkFeatureCode>
              <t:MarkFeatureDescription>Motion mark</t:MarkFeatureDescription>
              <t:NamePortrait>Consent of Jane Orbit is of record.</t:NamePortrait>
              <t:LineStippling>Lining indicates shading.</t:LineStippling>
            </t:Status>
            <t:Parties>
              <t:CurrentOwners>
                <t:Owner>
                  <t:Name>Orbit Labs LLC</t:Name>
                  <t:CitizenshipCountryCode>US</t:CitizenshipCountryCode>
                  <t:CitizenshipCountryName>United States</t:CitizenshipCountryName>
                </t:Owner>
              </t:CurrentOwners>
            </t:Parties>
            <t:Attorneys>
              <t:Attorney>
                <t:FirstName>Jane</t:FirstName>
                <t:LastName>Lawyer</t:LastName>
                <t:Suffix>III</t:Suffix>
                <t:EmailAddress>jane@example.test</t:EmailAddress>
                <t:PhoneNumber>+1-202-555-0100</t:PhoneNumber>
              </t:Attorney>
            </t:Attorneys>
            <t:ProsecutionHistory>
              <t:Prosecution>
                <t:EntryCode>R.PR</t:EntryCode>
                <t:Confidential>false</t:Confidential>
                <t:DocumentUri>https://example.test/documents/1</t:DocumentUri>
              </t:Prosecution>
            </t:ProsecutionHistory>
            <t:GsList>
              <t:GoodsAndService>
                <t:PrimeClassCode>009</t:PrimeClassCode>
                <t:InternationalClasses>
                  <t:Class>
                    <t:Code>009</t:Code>
                    <t:IsPrime>true</t:IsPrime>
                  </t:Class>
                </t:InternationalClasses>
              </t:GoodsAndService>
            </t:GsList>
            <t:FutureCase><t:Enabled>true</t:Enabled></t:FutureCase>
          </t:Trademark>
        </t:TrademarkTransaction>
        """;
        RecordingTrademarkStatusHandler handler = new RecordingTrademarkStatusHandler(HttpStatusCode.OK, xml, UsptoMediaTypes.Xml);
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusOperation operation = new TrademarkStatusOperation(client, Options(client));

        TrademarkCaseStatus result = operation.GetStatus("sn90123456", DocumentFormat.Xml, new RequestContext());
        TrademarkOwner owner = result.CurrentOwners.Single();
        TrademarkAttorney attorney = result.Attorneys.Single();
        TrademarkProsecutionEvent prosecutionEvent = result.ProsecutionEvents.Single();
        TrademarkClass classification = result.GoodsAndServices.Single().Classes.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.SearchIdentifier, Is.EqualTo("complete-xml"));
            Assert.That(result.Mark.FeatureCode, Is.EqualTo("F1"));
            Assert.That(result.Mark.FeatureDescription, Is.EqualTo("Motion mark"));
            Assert.That(result.Mark.NamePortraitConsentText, Is.EqualTo("Consent of Jane Orbit is of record."));
            Assert.That(result.Mark.LiningStipplingText, Is.EqualTo("Lining indicates shading."));
            Assert.That(owner.CitizenshipCountryCode, Is.EqualTo("US"));
            Assert.That(owner.CitizenshipCountryName, Is.EqualTo("United States"));
            Assert.That(attorney.NameSuffix, Is.EqualTo("III"));
            Assert.That(attorney.EmailAddresses, Is.EqualTo(new[] { "jane@example.test" }));
            Assert.That(attorney.PhoneNumbers, Is.EqualTo(new[] { "+1-202-555-0100" }));
            Assert.That(prosecutionEvent.IsConfidential, Is.False);
            Assert.That(prosecutionEvent.HasIsConfidential, Is.True);
            Assert.That(prosecutionEvent.DocumentUri?.AbsoluteUri, Is.EqualTo("https://example.test/documents/1"));
            Assert.That(classification.IsPrime, Is.True);
            Assert.That(classification.HasIsPrime, Is.True);
            Assert.That(result.UnknownFields["FutureCase"], Is.EqualTo("<FutureCase><Enabled>true</Enabled></FutureCase>"));
        });
    }

    [TestCase(null, DocumentFormat.Json, "REQUIRED")]
    [TestCase("", DocumentFormat.Json, "EMPTY_IDENTIFIER")]
    [TestCase("sn123", DocumentFormat.Json, "INVALID_TRADEMARK_SERIAL")]
    [TestCase("sn90123456", DocumentFormat.Html, "UNSUPPORTED_FORMAT")]
    public void InvalidRequestsFailBeforeSending(string? identifier, DocumentFormat format, string expectedCode)
    {
        RecordingTrademarkStatusHandler handler = new RecordingTrademarkStatusHandler(HttpStatusCode.OK, JsonResponse(), UsptoMediaTypes.Json);
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusOperation operation = new TrademarkStatusOperation(client, Options(client));

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.GetStatus(identifier!, format, new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void CancellationStopsBeforeSending()
    {
        RecordingTrademarkStatusHandler handler = new RecordingTrademarkStatusHandler(HttpStatusCode.OK, JsonResponse(), UsptoMediaTypes.Json);
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusOperation operation = new TrademarkStatusOperation(client, Options(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.GetStatus("sn90123456", DocumentFormat.Json, new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    [Test]
    public void RemoteAndMediaFailuresUseSharedResponsePolicies()
    {
        RecordingTrademarkStatusHandler missingHandler = new RecordingTrademarkStatusHandler(HttpStatusCode.NotFound, "{\"error\":\"missing\"}", UsptoMediaTypes.Json);
        using HttpClient missingClient = new HttpClient(missingHandler);
        TrademarkStatusOperation missingOperation = new TrademarkStatusOperation(missingClient, Options(missingClient));
        NotFoundException missing = Assert.Throws<NotFoundException>(() => missingOperation.GetStatus("sn90123456", DocumentFormat.Json, new RequestContext()));

        RecordingTrademarkStatusHandler mediaHandler = new RecordingTrademarkStatusHandler(HttpStatusCode.OK, "<Trademark />", UsptoMediaTypes.Xml);
        using HttpClient mediaClient = new HttpClient(mediaHandler);
        TrademarkStatusOperation mediaOperation = new TrademarkStatusOperation(mediaClient, Options(mediaClient));
        ApiException media = Assert.Throws<ApiException>(() => mediaOperation.GetStatus("sn90123456", DocumentFormat.Json, new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(missing.StatusCode, Is.EqualTo(404));
            Assert.That(media.ErrorCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(media.Service, Is.EqualTo(ApiService.Tsdr));
        });
    }

    private static UsptoClientOptions Options(HttpClient client)
    {
        return new UsptoClientOptions
        {
            TsdrBaseUri = new Uri("https://example.test/root/"),
            TsdrApiKey = "tsdr-secret",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000
        };
    }

    private static string JsonResponse()
    {
        return "{\"searchId\":\"search-1\",\"trademarks\":[{\"status\":{\"serialNumber\":90123456,\"usRegistrationNumber\":\"7654321\",\"status\":700,\"statusDate\":\"date pending\",\"filingDate\":\"2020-01-15\",\"markElement\":\"ACME ORBIT\",\"standardChar\":true,\"designSearchList\":[{\"code\":\"01.01.03\",\"descriptions\":[\"Stars\"]}],\"filedUse\":true,\"correspondence\":{\"name\":\"Jane Lawyer\",\"emailAddresses\":[\"trademarks@example.test\"],\"city\":\"Washington\"}},\"parties\":{\"currentOwners\":[{\"entityNum\":1,\"name\":\"Acme Holdings LLC\",\"address1\":\"100 Main\",\"city\":\"Alexandria\"}],\"priorOwnerList\":[{\"name\":\"Acme Company\"}]},\"attorneys\":[{\"name\":\"Jane Lawyer\",\"docketNumber\":\"ACME-100\"}],\"prosecutionHistory\":[{\"entryCode\":\"R.PR\",\"entryDesc\":\"Registered\",\"entryDate\":\"2024-08-12\"}],\"gsList\":[{\"primeClassCode\":\"009\",\"description\":\"Software\",\"firstUseDate\":20180301,\"internationalClasses\":[{\"code\":\"009\"}],\"usClasses\":[{\"code\":\"021\"}],\"classBasis\":{\"flag1a\":true}}],\"foreignInfoList\":[{\"frgnApplNum\":\"EU123\",\"frgnCountryCd\":\"EM\",\"frgnApplFilingDt\":\"2020-01-02\"}],\"internationalData\":true,\"intlRegistrationList\":[{\"registrationNum\":\"1234567\",\"statusCd\":10,\"registrationDate\":\"2021-06-05\"}],\"relationshipBundleList\":[{\"referenceNumber\":\"A0123456\",\"internationalRegistrationNumber\":\"1234567\"}],\"usReference\":{\"referenceNum\":\"A0123456\",\"intlRegistrationNum\":\"1234567\",\"statusCd\":10,\"madridHistory\":[{\"eventActionCode\":\"DESIGNATION\",\"eventDate\":\"2021-06-05\"}]},\"futureCase\":{\"enabled\":true},\"apiKey\":\"tsdr-secret\"}]}";
    }

    private static string XmlResponse()
    {
        return "<t:TrademarkTransaction xmlns:t=\"urn:uspto:tsdr\"><t:Trademark><t:Status><t:SerialNumber>90123456</t:SerialNumber><t:USRegistrationNumber>7654321</t:USRegistrationNumber><t:Status>700</t:Status><t:StatusDate>0000-00-00</t:StatusDate><t:FilingDate>2020-01-15</t:FilingDate><t:MarkElement>ACME ORBIT</t:MarkElement></t:Status><t:Parties><t:CurrentOwners><t:Party><t:Name>Acme Holdings LLC</t:Name><t:City>Alexandria</t:City></t:Party></t:CurrentOwners></t:Parties><t:ProsecutionHistory><t:Prosecution><t:EntryCode>R.PR</t:EntryCode><t:EntryDesc>Registered</t:EntryDesc><t:EntryDate>2024-08-12</t:EntryDate></t:Prosecution></t:ProsecutionHistory><t:GsList><t:GoodsAndService><t:PrimeClassCode>009</t:PrimeClassCode><t:Description>Software</t:Description></t:GoodsAndService></t:GsList><t:IntlRegistrationList><t:InternationalRegistration><t:RegistrationNum>1234567</t:RegistrationNum><t:RegistrationDate>2021-06-05</t:RegistrationDate></t:InternationalRegistration></t:IntlRegistrationList></t:Trademark></t:TrademarkTransaction>";
    }
}

internal class RecordingTrademarkStatusHandler : HttpMessageHandler
{
    internal HttpStatusCode StatusCode;
    internal string ResponseBody;
    internal string MediaType;
    internal string? RequestIdentifier = null;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> ApiKeys = new List<string?>();

    internal RecordingTrademarkStatusHandler(HttpStatusCode statusCode, string responseBody, string mediaType)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        MediaType = mediaType;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.TsdrApiKey).Single());
        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseBody, Encoding.UTF8, MediaType),
            RequestMessage = request
        };
        if (RequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, RequestIdentifier);
        }

        return Task.FromResult(response);
    }
}
