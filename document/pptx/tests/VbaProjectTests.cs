using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class VbaProjectTests
{
    [Test]
    public void Presentation_ReportsMacroPresenceAndLoadsVbaProject()
    {
        PptxPresentation presentation = LoadMacroPresentation();
        PptxVbaProject? project = presentation.GetVbaProject();
        string diagnostics = project!.ScanSecurity().FormatDiagnostics();

        Assert.That(presentation.IsMacroEnabled(), Is.True);
        Assert.That(presentation.HasMacros(), Is.True);
        Assert.That(project, Is.Not.Null);
        Assert.That(project!.Present, Is.True);
        Assert.That(project.RelationshipId, Is.EqualTo("rIdVba"));
        Assert.That(project.PartName, Is.EqualTo("/ppt/vbaProject.bin"));
        Assert.That(project.ContentType, Is.EqualTo(PptxContentTypes.VbaProject));
        Assert.That(project.Bytes, Is.EqualTo(CreateVbaBytes()));
        Assert.That(project.GetBytes(), Is.EqualTo(CreateVbaBytes()));
        Assert.That(diagnostics, Does.Contain("SEC010"));
        Assert.That(diagnostics, Does.Contain("/ppt/vbaProject.bin"));
    }

    [Test]
    public void VbaProjectPreservesAndReplacesBytesOnRoundTrip()
    {
        PptxPresentation presentation = LoadMacroPresentation();
        PptxVbaProject project = presentation.GetVbaProject()!;

        project.ReplaceBytes(CreateReplacementVbaBytes());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxVbaProject? reopenedProject = reopened.GetVbaProject();
        PptxRelationship relationship = FindRelationship(reopened.Package.GetRelationships(PptxPartNames.Presentation), PptxRelationshipTypes.VbaProject);
        string diagnostics = reopened.Package.ScanSecurity().FormatDiagnostics();

        Assert.That(reopened.IsMacroEnabled(), Is.True);
        Assert.That(reopened.HasMacros(), Is.True);
        Assert.That(reopenedProject, Is.Not.Null);
        Assert.That(reopenedProject!.GetBytes(), Is.EqualTo(CreateReplacementVbaBytes()));
        Assert.That(reopened.Package.GetPart(PptxPartNames.Presentation).ContentType, Is.EqualTo(PptxContentTypes.MacroEnabledPresentation));
        Assert.That(reopened.Package.GetPart("/ppt/vbaProject.bin").ContentType, Is.EqualTo(PptxContentTypes.VbaProject));
        Assert.That(relationship.TargetPartName, Is.EqualTo("/ppt/vbaProject.bin"));
        Assert.That(diagnostics, Does.Contain("SEC010"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void SetVbaProjectBytesAddsMacroProjectAndEnablesPresentationContentType()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();

        Assert.That(presentation.IsMacroEnabled(), Is.False);
        Assert.That(presentation.HasMacros(), Is.False);
        Assert.That(presentation.GetVbaProject(), Is.Null);

        PptxVbaProject project = presentation.SetVbaProjectBytes(CreateVbaBytes());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxVbaProject? reopenedProject = reopened.GetVbaProject();

        Assert.That(project.PartName, Is.EqualTo("/ppt/vbaProject.bin"));
        Assert.That(reopened.IsMacroEnabled(), Is.True);
        Assert.That(reopened.HasMacros(), Is.True);
        Assert.That(reopenedProject, Is.Not.Null);
        Assert.That(reopenedProject!.GetBytes(), Is.EqualTo(CreateVbaBytes()));
        Assert.That(reopened.Package.GetPart(PptxPartNames.Presentation).ContentType, Is.EqualTo(PptxContentTypes.MacroEnabledPresentation));
        Assert.That(FindRelationship(reopened.Package.GetRelationships(PptxPartNames.Presentation), PptxRelationshipTypes.VbaProject).TargetPartName, Is.EqualTo("/ppt/vbaProject.bin"));
        Assert.That(reopened.Package.ScanSecurity().FormatDiagnostics(), Does.Contain("SEC010"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadMacroPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.GetPart(PptxPartNames.Presentation).SetContentType(PptxContentTypes.MacroEnabledPresentation);
        package.EnsureContentTypeOverride(PptxPartNames.Presentation, PptxContentTypes.MacroEnabledPresentation);
        package.AddPart("/ppt/vbaProject.bin", PptxContentTypes.VbaProject, CreateVbaBytes());
        package.AddRelationshipWithId(PptxPartNames.Presentation, "rIdVba", PptxRelationshipTypes.VbaProject, "vbaProject.bin", PptxRelationshipTargetMode.Internal);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxRelationship FindRelationship(PptxRelationshipCollection relationships, string type)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == type)
            {
                return relationships.Relationships[index];
            }
        }

        throw new System.ArgumentException("Relationship was not found.");
    }

    static byte[] CreateVbaBytes()
    {
        return Encoding.UTF8.GetBytes("VBA-PROJECT-BYTES");
    }

    static byte[] CreateReplacementVbaBytes()
    {
        return Encoding.UTF8.GetBytes("REPLACED-VBA-PROJECT-BYTES");
    }
}
