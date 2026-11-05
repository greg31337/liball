using iso20022;

namespace iso20022.Tests;

public class BundledRepositoryEndToEndTests
{
    private RepositoryImportResult importResult = new RepositoryImportResult();
    private RepositorySnapshot snapshot = new RepositorySnapshot();

    [OneTimeSetUp]
    public void ImportBundledRepository()
    {
        string archivePath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022/eRepository_ISO20022_20260626.zip"));
        RepositoryImportOptions options = new RepositoryImportOptions
        {
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.Repository,
                Edition = "2026-06",
                Identifier = "iso20022-e-repository",
                Version = "20260626"
            },
            IsApproved = true
        };

        importResult = new RepositoryImporter().ImportZipFile(archivePath, options);
        Assert.That(importResult.Snapshot, Is.Not.Null, importResult.ErrorMessage);
        snapshot = importResult.Snapshot!;
    }

    [Test]
    public void BundledRepositoryMatchesArtifactAndNormalizedCounts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(importResult.Success, Is.True, importResult.ErrorMessage);
            Assert.That(importResult.Artifact.HashAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(importResult.Artifact.Hash.LexicalValue, Is.EqualTo(
                "9A9D060DD94B6858A4EF4836CA36A9A67308E96B69D7D184FF97F28295B96039"));
            Assert.That(importResult.Provenance.SourceEntryName,
                Is.EqualTo("eRepository_ISO20022_20260626.iso20022"));
            Assert.That(importResult.Provenance.AdapterIdentifier,
                Is.EqualTo("iso20022.ecore.2013"));
            Assert.That(importResult.SourceEntityCount, Is.EqualTo(376665));
            Assert.That(importResult.SourceReferenceCount, Is.EqualTo(975582));
            Assert.That(importResult.NormalizedReferenceCount, Is.EqualTo(975582));
            Assert.That(importResult.IndexedEntityCount, Is.EqualTo(226994));
            Assert.That(snapshot.Identifier, Is.EqualTo("_9DmuQENJEeGHJ_bHJRPaIQ"));
            Assert.That(snapshot.Edition, Is.EqualTo("2026-06"));
            Assert.That(snapshot.Constraints, Has.Count.EqualTo(0));
            Assert.That(snapshot.BusinessAreas, Has.Count.EqualTo(36));
            Assert.That(snapshot.BusinessProcesses, Has.Count.EqualTo(0));
            Assert.That(snapshot.BusinessRoles, Has.Count.EqualTo(0));
            Assert.That(snapshot.Participants, Has.Count.EqualTo(0));
            Assert.That(snapshot.BusinessTransactions, Has.Count.EqualTo(0));
            Assert.That(snapshot.Conversations, Has.Count.EqualTo(0));
            Assert.That(snapshot.MessageChoreographies, Has.Count.EqualTo(0));
            Assert.That(snapshot.MessageTransmissions, Has.Count.EqualTo(0));
            Assert.That(snapshot.MessagingEndpoints, Has.Count.EqualTo(0));
            Assert.That(snapshot.Transports, Has.Count.EqualTo(0));
            Assert.That(snapshot.BusinessComponents, Has.Count.EqualTo(791));
            Assert.That(snapshot.BusinessElements, Has.Count.EqualTo(4656));
            Assert.That(snapshot.MessageSets, Has.Count.EqualTo(183));
            Assert.That(snapshot.MessageDefinitions, Has.Count.EqualTo(3329));
            Assert.That(snapshot.MessageBuildingBlocks, Has.Count.EqualTo(23345));
            Assert.That(snapshot.MessageComponents, Has.Count.EqualTo(14594));
            Assert.That(snapshot.MessageElements, Has.Count.EqualTo(122106));
            Assert.That(snapshot.Choices, Has.Count.EqualTo(4661));
            Assert.That(snapshot.DataTypes, Has.Count.EqualTo(356));
            Assert.That(snapshot.CodeSets, Has.Count.EqualTo(4162));
            Assert.That(snapshot.Syntaxes, Has.Count.EqualTo(0));
            Assert.That(snapshot.Encodings, Has.Count.EqualTo(0));
            Assert.That(snapshot.ExternalSchemas, Has.Count.EqualTo(10));
            Assert.That(snapshot.SyntaxMessageSchemes, Has.Count.EqualTo(0));
            Assert.That(snapshot.ConvergenceDefinitions, Has.Count.EqualTo(0));
            Assert.That(snapshot.UnknownContent, Has.Count.EqualTo(0));
        });
    }

    [Test]
    public void BundledRepositoryContainsRepresentativeMessageFromEveryBusinessArea()
    {
        string[] expected =
        {
            "catp|ATM Card Transaction|44|catp.009.001.01",
            "caam|ATM Management|40|caam.014.001.01",
            "caaa|Acceptor to Acquirer Card Transaction|295|caaa.001.001.01",
            "acmt|Account Management|158|acmt.012.001.01",
            "cain|Acquirer to Issuer Card Transaction|76|cain.001.001.01",
            "admi|Administration|15|admi.002.001.01",
            "auth|Authorities|177|auth.068.001.01",
            "head|Business Application Header|5|head.001.001.01",
            "caad|Card Administration|27|caad.008.001.02",
            "camt|Cash Management|378|camt.060.001.02",
            "colr|Collateral Management|56|colr.016.001.02",
            "nvlp|Envelope|3|nvlp.002.001.02",
            "cafc|Fee collection|6|cafc.001.001.01",
            "cafm|File Management|6|cafm.001.001.01",
            "fxtr|Foreign Exchange Trade|48|fxtr.030.001.02",
            "cafr|Fraud Reporting and Disposition|12|cafr.003.001.01",
            "canm|Network Management|12|canm.003.001.02",
            "|OtherMessages|0|",
            "pacs|Payments Clearing and Settlement|98|pacs.008.001.01",
            "pain|Payments Initiation|116|pain.014.001.01",
            "remt|Payments Remittance Advice|10|remt.001.001.01",
            "reda|Reference Data|89|reda.049.001.01",
            "casp|Sale to POI Card Transactions|130|casp.011.001.01",
            "secl|Securities Clearing|23|secl.009.001.02",
            "seev|Securities Events|432|seev.029.001.01",
            "semt|Securities Management|206|semt.003.001.01",
            "sese|Securities Settlement|472|sese.019.001.02",
            "setr|Securities Trade|114|setr.017.001.02",
            "casr|Settlement Reporting|6|casr.001.001.01",
            "supl|Supplementary Data|71|supl.006.001.08",
            "catm|Terminal Management|84|catm.003.001.01",
            "tsrv|Trade Services|19|tsrv.016.001.01",
            "tsin|Trade Services Initiation|13|tsin.004.001.01",
            "tsmt|Trade Services Management|69|tsmt.001.001.03",
            "trck|Transaction Tracker|6|trck.004.001.03",
            "trea|Treasury|13|trea.010.001.02"
        };

        Assert.That(snapshot.BusinessAreas.Select(CreateBusinessAreaSignature),
            Is.EqualTo(expected));
    }

    [Test]
    public void BundledRepositoryHasOnlyDocumentedSourceIdentityDefects()
    {
        Assert.Multiple(() =>
        {
            Assert.That(importResult.SourceDuplicateIdentifierCount, Is.EqualTo(103));
            Assert.That(importResult.SourceUnresolvedReferenceCount, Is.EqualTo(0));
            Assert.That(snapshot.References, Has.Count.EqualTo(975582));
            Assert.That(snapshot.References.All(reference => reference.IsResolved), Is.True);
            Assert.That(importResult.DiagnosticCodes, Is.Empty);
            Assert.That(importResult.DiagnosticMessages, Is.Empty);
            Assert.That(importResult.Artifact.IsApproved, Is.True);
        });
    }

    private string CreateBusinessAreaSignature(BusinessAreaDefinition area)
    {
        List<MessageDefinitionMetadata> messages = snapshot.MessageDefinitions.Where(
            message => message.MessageIdentifier.BusinessArea == area.Code).ToList();
        string firstIdentifier = string.Empty;
        if (messages.Count > 0)
        {
            firstIdentifier = messages[0].MessageIdentifier.Value;
        }

        return area.Code + "|" + area.Name + "|" + messages.Count + "|" + firstIdentifier;
    }
}
