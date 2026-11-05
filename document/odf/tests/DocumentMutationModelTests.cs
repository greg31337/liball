using System.IO;
using System.Text;

namespace odf.Tests;

public class DocumentMutationModelTests
{
    [Test]
    public void AddsCopiesAndRemovesMaterializedStandardParts()
    {
        OdfDocument source = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfDocument target = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfNode[] metadata = ReadNodes(
            "<office:document-meta xmlns:office='"
                + OdfNamespaces.Office
                + "' office:version='1.4'><office:meta/></office:document-meta>",
            OdfPartNames.Metadata);

        OdfEditResult added = DocumentMutationModel.AddDocumentPart(
            source,
            OdfPartNames.Metadata,
            metadata,
            OdfMediaTypes.Xml);
        OdfEditResult copied = DocumentMutationModel.CopyDocumentPart(
            source,
            target,
            OdfPartNames.Metadata,
            OdfPartNames.Metadata);
        OdfEditResult duplicate = DocumentMutationModel.CopyDocumentPart(
            source,
            target,
            OdfPartNames.Metadata,
            OdfPartNames.Metadata);
        OdfPart sourcePart = FindPart(source, OdfPartNames.Metadata)!;
        OdfPart targetPart = FindPart(target, OdfPartNames.Metadata)!;
        OdfEditResult required = DocumentMutationModel.RemoveDocumentPart(
            target,
            OdfPartNames.Content);
        OdfEditResult removed = DocumentMutationModel.RemoveDocumentPart(
            target,
            OdfPartNames.Metadata);

        Assert.Multiple(() =>
        {
            Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(duplicate.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(sourcePart.Root, Is.Not.SameAs(targetPart.Root));
            Assert.That(HasEntry(source, OdfPartNames.Metadata), Is.True);
            Assert.That(FindManifestMediaType(source, OdfPartNames.Metadata),
                Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(required.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(FindPart(target, OdfPartNames.Metadata), Is.Null);
            Assert.That(HasEntry(target, OdfPartNames.Metadata), Is.False);
        });
    }

    [Test]
    public void AddsCopiesAndRemovesResourcesWithManifestMetadata()
    {
        OdfDocument source = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfDocument target = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        byte[] data = [1, 2, 3, 4];

        OdfEditResult added = DocumentMutationModel.AddDocumentResource(
            source,
            "Media/source.bin",
            data,
            "application/x-test-data",
            OdfCompressionMethod.Deflated);
        data[0] = 99;
        OdfEditResult copied = DocumentMutationModel.CopyDocumentResource(
            source,
            target,
            "Media/source.bin",
            "Media/copied.bin");
        byte[] copiedData = ReadEntry(target, "Media/copied.bin");
        OdfEditResult reserved = DocumentMutationModel.AddDocumentResource(
            target,
            OdfPartNames.Content,
            [8],
            OdfMediaTypes.Xml,
            OdfCompressionMethod.Deflated);
        OdfEditResult removed = DocumentMutationModel.RemoveDocumentResource(
            source,
            "Media/source.bin");
        OdfEditResult missing = DocumentMutationModel.RemoveDocumentResource(
            source,
            "Media/source.bin");

        Assert.Multiple(() =>
        {
            Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copiedData, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(FindManifestMediaType(target, "Media/copied.bin"),
                Is.EqualTo("application/x-test-data"));
            Assert.That(reserved.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(missing.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(HasEntry(source, "Media/source.bin"), Is.False);
        });
    }

    [Test]
    public void ChecksBodyKindUsingDocumentFamilies()
    {
        OdfDocument text = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);

        OdfEditResult exact = DocumentMutationModel.CheckBodyKind(
            text,
            OdfDocumentKind.Text);
        OdfEditResult templateFamily = DocumentMutationModel.CheckBodyKind(
            text,
            OdfDocumentKind.TextTemplate);
        OdfEditResult wrong = DocumentMutationModel.CheckBodyKind(
            text,
            OdfDocumentKind.Spreadsheet);
        OdfDocument formulaTemplate = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.FormulaTemplate);
        OdfEditResult formulaFamily = DocumentMutationModel.CheckBodyKind(
            formulaTemplate,
            OdfDocumentKind.Formula);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(exact.Changed, Is.False);
            Assert.That(templateFamily.Status,
                Is.EqualTo(OdfEditStatus.Success));
            Assert.That(wrong.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(formulaFamily.Status,
                Is.EqualTo(OdfEditStatus.Success));
        });
    }

    [Test]
    public void ConvertsResourceFreeDocumentsBetweenFlatAndPackageForms()
    {
        OdfDocument original = ReadFlat(
            "<office:styles/><office:meta/><office:settings/>"
                + "<office:body><office:text><text:p>Converted</text:p>"
                + "</office:text></office:body>");

        OdfDocument package = DocumentMutationModel
            .ConvertToPackageDocument(original);
        OdfDocument flat = DocumentMutationModel
            .ConvertToFlatDocument(package);
        DocumentMutationModel.AddDocumentResource(
            package,
            "Media/loss.bin",
            [5, 6],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);

        Assert.Multiple(() =>
        {
            Assert.That(original.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(package.PackageKind,
                Is.EqualTo(OdfPackageKind.Package));
            Assert.That(FindPart(package, OdfPartNames.Content), Is.Not.Null);
            Assert.That(FindPart(package, OdfPartNames.Styles), Is.Not.Null);
            Assert.That(FindPart(package, OdfPartNames.Metadata), Is.Not.Null);
            Assert.That(FindPart(package, OdfPartNames.Settings), Is.Not.Null);
            Assert.That(package.TextBlocks[0].Text, Is.EqualTo("Converted"));
            Assert.That(flat.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(flat.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(flat.Kind, Is.EqualTo(OdfDocumentKind.Text));
            Assert.That(flat.TextBlocks[0].Text, Is.EqualTo("Converted"));
            Assert.That(
                () => DocumentMutationModel.ConvertToFlatDocument(package),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void AppliesRepairsOnlyAfterExplicitRequest()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfPackage package = document.Package!;
        new OdfPackageWriter().RemovePackageEntry(
            package,
            OdfPartNames.Mimetype);
        document.PackageEntries = package.Entries;

        OdfRepairAnalysis analysis = DocumentMutationModel
            .ProposeDocumentRepairs(document);
        OdfRepairProposal proposal = FindRepair(
            analysis,
            OdfRepairKind.PackageMimetype);
        bool absentBeforeApplication = !HasEntry(
            document,
            OdfPartNames.Mimetype);
        OdfRepairResult result = DocumentMutationModel.ApplyDocumentRepairs(
            document,
            [proposal]);

        Assert.Multiple(() =>
        {
            Assert.That(absentBeforeApplication, Is.True);
            Assert.That(proposal.IsSafe, Is.True);
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.Mutations, Has.Length.EqualTo(1));
            Assert.That(HasEntry(document, OdfPartNames.Mimetype), Is.True);
        });
    }

    static OdfNode[] ReadNodes(string xml, string partPath)
    {
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            partPath,
            new OdfLoadOptions());
    }

    static OdfDocument ReadFlat(string sections)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'>" + sections + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "mutation.fodt",
            new OdfLoadOptions());
    }

    static OdfPart? FindPart(OdfDocument document, string path)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (document.Parts[index].Path == path)
            {
                return document.Parts[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool HasEntry(OdfDocument document, string path)
    {
        int index = 0;
        while (index < document.PackageEntries.Length)
        {
            if (document.PackageEntries[index].Path == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string FindManifestMediaType(OdfDocument document, string path)
    {
        OdfManifest? manifest = document.Manifest;
        if (manifest == null)
        {
            return "";
        }
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == path)
            {
                return manifest.Entries[index].MediaType;
            }
            index = index + 1;
        }
        return "";
    }

    static byte[] ReadEntry(OdfDocument document, string path)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(
            document.Package!);
        using (Stream input = index.OpenEntry(path))
        using (MemoryStream output = new MemoryStream())
        {
            input.CopyTo(output);
            return output.ToArray();
        }
    }

    static OdfRepairProposal FindRepair(
        OdfRepairAnalysis analysis,
        OdfRepairKind kind)
    {
        int index = 0;
        while (index < analysis.Proposals.Length)
        {
            if (analysis.Proposals[index].Kind == kind)
            {
                return analysis.Proposals[index];
            }
            index = index + 1;
        }
        throw new AssertionException("The expected repair was not proposed.");
    }
}
