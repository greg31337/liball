using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PublicPackageWorkflowCompletionTests
{
    [Test]
    public void PackageOperations_PublicWorkflowCoversSaveValidationRepairCleanupInspectionAndComparison()
    {
        PptxPackageOperations operations = new PptxPackageOperations();
        PptxPackage package = new PptxDocumentFactory().CreatePresentation().GetPackage();

        Assert.That(operations.Succeeded(package), Is.True);
        Assert.That(operations.Failed(package), Is.False);
        Assert.That(operations.Validate(package).Succeeded(), Is.True);
        Assert.That(operations.Repair(package).Succeeded(), Is.True);
        Assert.That(operations.Cleanup(package).Succeeded(), Is.True);
        Assert.That(operations.Normalize(package).Succeeded(), Is.True);
        Assert.That(operations.PartCount(package), Is.GreaterThan(0));
        Assert.That(operations.RawEntryCount(package), Is.GreaterThanOrEqualTo(0));
        Assert.That(operations.HasPart(package, PptxPartNames.Presentation), Is.True);
        Assert.That(operations.IndexOfPart(package, PptxPartNames.Presentation), Is.GreaterThanOrEqualTo(0));
        Assert.That(operations.GetPart(package, PptxPartNames.Presentation).ContentType, Is.EqualTo(PptxContentTypes.Presentation));
        Assert.That(operations.GetRelationships(package, PptxPartNames.Presentation).Relationships, Is.Not.Empty);
        Assert.That(operations.ToSummaryJson(package), Does.Contain(PptxPartNames.Presentation));
        Assert.That(operations.ScanSecurity(package).Diagnostics, Is.Empty);

        PptxPackage clone = operations.ClonePackage(package);
        PptxPackageManifest manifest = operations.InspectManifest(package);
        PptxPackageManifestComparison equalManifest = operations.CompareManifests(package, clone);
        PptxEditableModelComparison equalModel = operations.CompareEditableModels(package, clone);
        Assert.That(manifest.Entries, Is.Not.Empty);
        Assert.That(equalManifest.Equal, Is.True);
        Assert.That(equalModel.Equal, Is.True);
        Assert.That(operations.SummarizeEditableModel(package).SlideCount, Is.EqualTo(1));

        operations.AddPart(clone, "/custom/facade.bin", "application/vnd.example.facade", new byte[] { 1, 2, 3 });
        operations.ReplacePart(clone, "/custom/facade.bin", "application/vnd.example.facade", new byte[] { 4, 5, 6 });
        operations.RenamePart(clone, "/custom/facade.bin", "/custom/renamed.bin");
        PptxRelationship allocated = operations.AddRelationship(clone, PptxPartNames.Presentation, "urn:example:allocated", "../custom/renamed.bin", PptxRelationshipTargetMode.Internal);
        operations.SetRelationshipTarget(clone, PptxPartNames.Presentation, allocated.Id, "../custom/renamed.bin#fragment", PptxRelationshipTargetMode.Internal);
        operations.RemoveRelationship(clone, PptxPartNames.Presentation, allocated.Id);
        operations.AddRelationshipWithId(clone, PptxPartNames.Presentation, "rIdFacade", "urn:example:facade", "../custom/renamed.bin", PptxRelationshipTargetMode.Internal);
        operations.RemoveRelationship(clone, PptxPartNames.Presentation, "rIdFacade");
        operations.RemoveContentTypeOverride(clone, "/custom/renamed.bin");
        operations.RemovePart(clone, "/custom/renamed.bin");
        Assert.That(operations.CompareManifests(package, clone).Equal, Is.True);

        using MemoryStream stream = new MemoryStream();
        operations.Save(package, stream);
        stream.Position = 0;
        PptxPackage reopened = operations.OpenWithOptions(stream, new PptxZipReadOptionsOperations().CreateDefault());
        Assert.That(operations.Validate(reopened).Succeeded(), Is.True);
        Assert.That(operations.OpenBytesWithOptions(operations.SaveBytes(reopened), PptxZipReadOptions.CreateDefault()).Failed(), Is.False);
    }

    [Test]
    public void InspectCompatibility_PublicFacadeIsReadOnlyAndUnknownContentRoundTrips()
    {
        PptxPackageOperations operations = new PptxPackageOperations();
        PptxPackage package = new PptxDocumentFactory().CreatePresentation().GetPackage();
        byte[] unknownBytes = new byte[] { 0, 8, 16, 32, 255 };
        operations.AddPart(package, "/custom/opaque.bin", "application/vnd.example.opaque", unknownBytes);
        operations.AddRelationshipWithId(package, PptxPartNames.Presentation, "rIdOpaque", "urn:example:opaque", "../custom/opaque.bin", PptxRelationshipTargetMode.Internal);
        operations.AddRelationshipWithId(package, PptxPartNames.FirstSlide, "rIdRemote", PptxRelationshipTypes.Hyperlink, "https://example.test/remote", PptxRelationshipTargetMode.External);
        byte[] beforeInspection = operations.SaveBytes(package);

        PptxCompatibilityInspection inspection = operations.InspectCompatibility(package);
        byte[] afterInspection = operations.SaveBytes(package);

        Assert.That(FindFeature(inspection, PptxCompatibilityLevel.PreservedOnly, "/custom/opaque.bin", ""), Is.Not.Null);
        Assert.That(FindFeature(inspection, PptxCompatibilityLevel.Unsafe, PptxPartNames.FirstSlide, "rIdRemote"), Is.Not.Null);
        Assert.That(inspection.SecurityDiagnostics.FormatDiagnostics(), Does.Contain("SEC001"));
        Assert.That(afterInspection, Is.EqualTo(beforeInspection));

        PptxPackage reopened = operations.OpenBytes(afterInspection);
        Assert.That(operations.GetPart(reopened, "/custom/opaque.bin").GetBytes(), Is.EqualTo(unknownBytes));
        Assert.That(operations.GetRelationships(reopened, PptxPartNames.Presentation).ContainsId("rIdOpaque"), Is.True);
    }

    [Test]
    public void PackageOperations_NullPackagesFailAtThePublicBoundary()
    {
        PptxPackageOperations operations = new PptxPackageOperations();
        PptxPackage package = new PptxDocumentFactory().CreatePresentation().GetPackage();

        AssertTargetNull(() => operations.SaveBytes(null!));
        AssertTargetNull(() => operations.Save(null!, Stream.Null));
        AssertTargetNull(() => operations.Succeeded(null!));
        AssertTargetNull(() => operations.Failed(null!));
        AssertTargetNull(() => operations.Validate(null!));
        AssertTargetNull(() => operations.Repair(null!));
        AssertTargetNull(() => operations.Cleanup(null!));
        AssertTargetNull(() => operations.Normalize(null!));
        AssertTargetNull(() => operations.ClonePackage(null!));
        AssertTargetNull(() => operations.InspectManifest(null!));
        AssertTargetNull(() => operations.InspectCompatibility(null!));
        AssertTargetNull(() => operations.CompareManifests(null!, package));
        AssertTargetNull(() => operations.CompareManifests(package, null!));
        AssertTargetNull(() => operations.SummarizeEditableModel(null!));
        AssertTargetNull(() => operations.CompareEditableModels(null!, package));
        AssertTargetNull(() => operations.CompareEditableModels(package, null!));
        AssertTargetNull(() => operations.ToSummaryJson(null!));
        AssertTargetNull(() => operations.ScanSecurity(null!));
        AssertTargetNull(() => operations.PartCount(null!));
        AssertTargetNull(() => operations.RawEntryCount(null!));
        AssertTargetNull(() => operations.HasPart(null!, "/part.bin"));
        AssertTargetNull(() => operations.IndexOfPart(null!, "/part.bin"));
        AssertTargetNull(() => operations.GetPart(null!, "/part.bin"));
        AssertTargetNull(() => operations.AddPart(null!, "/part.bin", "application/octet-stream", new byte[] { 1 }));
        AssertTargetNull(() => operations.ReplacePart(null!, "/part.bin", "application/octet-stream", new byte[] { 1 }));
        AssertTargetNull(() => operations.RemovePart(null!, "/part.bin"));
        AssertTargetNull(() => operations.RenamePart(null!, "/part.bin", "/renamed.bin"));
        AssertTargetNull(() => operations.GetRelationships(null!, ""));
        AssertTargetNull(() => operations.AddRelationship(null!, "", "urn:example:type", "part.bin", PptxRelationshipTargetMode.Internal));
        AssertTargetNull(() => operations.AddRelationshipWithId(null!, "", "rId1", "urn:example:type", "part.bin", PptxRelationshipTargetMode.Internal));
        AssertTargetNull(() => operations.RemoveRelationship(null!, "", "rId1"));
        AssertTargetNull(() => operations.SetRelationshipTarget(null!, "", "rId1", "part.bin", PptxRelationshipTargetMode.Internal));
        AssertTargetNull(() => operations.RemoveContentTypeOverride(null!, "/part.bin"));
    }

    static void AssertTargetNull(TestDelegate operation)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(operation)!;
        Assert.That(exception.ParamName, Is.EqualTo("target"));
    }

    static PptxCompatibilityFeature? FindFeature(PptxCompatibilityInspection inspection, PptxCompatibilityLevel level, string partName, string relationshipId)
    {
        for (int index = 0; index < inspection.Features.Count; index++)
        {
            PptxCompatibilityFeature feature = inspection.Features[index];
            if (feature.Level == level && feature.PartName == partName && feature.RelationshipId == relationshipId)
            {
                return feature;
            }
        }

        return null;
    }
}
