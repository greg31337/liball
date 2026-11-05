using System.Collections.Generic;
using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class EmbeddedPackageActiveXCompletionTests
{
    [Test]
    public void EmbeddedObjectOperations_AddAndRemoveGenericPackageRoundTripsAndCleansParts()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxEmbeddedObjectOperations operations = new PptxEmbeddedObjectOperations();
        byte[] payload = new byte[] { 10, 20, 30, 40 };

        PptxEmbeddedObject created = operations.AddPackage(
            presentation.GetSlide(0),
            payload,
            "application/zip",
            "Package",
            "Archive",
            new PptxBounds(10L, 20L, 300L, 200L));
        string createdPartName = created.PartName;
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxEmbeddedObject actual = reopened.GetSlide(0).GetEmbeddedObject(0);

        Assert.That(actual.RelationshipType, Is.EqualTo(PptxRelationshipTypes.Package));
        Assert.That(actual.ContentType, Is.EqualTo("application/zip"));
        Assert.That(actual.ProgramId, Is.EqualTo("Package"));
        Assert.That(actual.Name, Is.EqualTo("Archive"));
        Assert.That(operations.GetBytes(actual), Is.EqualTo(payload));
        Assert.That(operations.ScanSecurity(actual).FormatDiagnostics(), Does.Contain("SEC040"));
        Assert.That(reopened.Package.HasPart(createdPartName), Is.True);

        operations.Remove(actual);
        PptxPresentation removed = SaveReopenAndLoad(reopened);

        Assert.That(removed.GetSlide(0).GetEmbeddedObjects(), Is.Empty);
        Assert.That(removed.Package.HasPart(createdPartName), Is.False);
        Assert.That(HasRelationshipType(removed.Package.GetRelationships(PptxPartNames.FirstSlide), PptxRelationshipTypes.Package), Is.False);
        Assert.That(removed.Package.Validate().Succeeded(), Is.True, removed.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ActiveXOperations_InventoryPreservesInlineAndBinaryControlData()
    {
        PptxPresentation presentation = LoadActiveXPresentation();
        PptxActiveXControlOperations operations = new PptxActiveXControlOperations();
        List<PptxActiveXControl> controls = operations.GetInventory(presentation);
        PptxActiveXControl inline = controls[0];
        PptxActiveXControl binary = controls[1];

        Assert.That(controls, Has.Count.EqualTo(2));
        Assert.That(operations.GetSlideInventory(presentation.GetSlide(0)), Has.Count.EqualTo(2));
        Assert.That(new PptxSlideOperations().GetActiveXControl(presentation.GetSlide(0), 0).Name, Is.EqualTo("InlineButton"));
        Assert.That(new PptxPresentationOperations().GetActiveXControls(presentation), Has.Count.EqualTo(2));
        Assert.That(inline.RelationshipId, Is.EqualTo("rIdInlineControl"));
        Assert.That(inline.SourcePartName, Is.EqualTo(PptxPartNames.FirstSlide));
        Assert.That(inline.Name, Is.EqualTo("InlineButton"));
        Assert.That(inline.ShapeId, Is.EqualTo("_x0000_s1025"));
        Assert.That(inline.ShowAsIcon, Is.True);
        Assert.That(inline.ImageWidth, Is.EqualTo(300L));
        Assert.That(inline.ImageHeight, Is.EqualTo(200L));
        Assert.That(inline.PartName, Is.EqualTo("/ppt/activeX/activeX1.xml"));
        Assert.That(inline.ContentType, Is.EqualTo(PptxContentTypes.ActiveX));
        Assert.That(inline.ClassId, Is.EqualTo("{INLINE-CLASS}"));
        Assert.That(inline.Persistence, Is.EqualTo("persistPropertyBag"));
        Assert.That(inline.Properties, Has.Count.EqualTo(2));
        Assert.That(inline.Properties[0].Name, Is.EqualTo("Caption"));
        Assert.That(inline.Properties[0].Value, Is.EqualTo("Run"));
        Assert.That(inline.HasPropertyPart, Is.False);
        Assert.That(binary.ClassId, Is.EqualTo("{BINARY-CLASS}"));
        Assert.That(binary.License, Is.EqualTo("license-data"));
        Assert.That(binary.Persistence, Is.EqualTo("persistStream"));
        Assert.That(binary.PropertyRelationshipId, Is.EqualTo("rIdBinaryProperties"));
        Assert.That(binary.PropertyPartName, Is.EqualTo("/ppt/activeX/activeX2.bin"));
        Assert.That(binary.PropertyContentType, Is.EqualTo(PptxContentTypes.ActiveXBinary));
        Assert.That(operations.GetPropertyBytes(binary), Is.EqualTo(CreateBinaryProperties()));
        Assert.That(operations.ScanSecurity(binary).FormatDiagnostics(), Does.Contain("SEC020"));

        presentation.Package.Cleanup();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxActiveXControl> preserved = operations.GetInventory(reopened);

        Assert.That(reopened.Package.HasPart("/ppt/activeX/activeX1.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/activeX/activeX2.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/activeX/activeX2.bin"), Is.True);
        Assert.That(operations.GetControlXml(preserved[0]), Does.Contain("<ax:unknown ax:value=\"preserve-inline\""));
        Assert.That(operations.GetControlXml(preserved[1]), Does.Contain("<ax:unknown ax:value=\"preserve-binary\""));
        Assert.That(operations.GetPropertyBytes(preserved[1]), Is.EqualTo(CreateBinaryProperties()));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ActiveXOperations_ReplaceControlAndPropertyPersistenceRoundTrips()
    {
        PptxPresentation presentation = LoadActiveXPresentation();
        PptxActiveXControlOperations operations = new PptxActiveXControlOperations();
        List<PptxActiveXControl> controls = operations.GetInventory(presentation);
        byte[] replacementBinary = new byte[] { 90, 80, 70 };

        operations.ReplaceControlXml(
            controls[0],
            "<ax:ocx xmlns:ax=\"" + PptxNamespaces.ActiveX + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" ax:classid=\"{UPDATED-CLASS}\" ax:persistence=\"persistPropertyBag\"><ax:ocxPr ax:name=\"Caption\" ax:value=\"Updated\"/><ax:unknown ax:value=\"still-preserved\"/></ax:ocx>");
        operations.ReplacePropertyBytes(controls[0], replacementBinary, PptxContentTypes.ActiveXBinary, "persistStreamInit");
        List<PptxActiveXProperty> replacementProperties = new List<PptxActiveXProperty>();
        PptxActiveXProperty caption = new PptxActiveXProperty();
        caption.Name = "Caption";
        caption.Value = "Safe replacement";
        replacementProperties.Add(caption);
        operations.ReplaceProperties(controls[1], replacementProperties);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxActiveXControl> actual = operations.GetInventory(reopened);

        Assert.That(actual[0].ClassId, Is.EqualTo("{UPDATED-CLASS}"));
        Assert.That(actual[0].Persistence, Is.EqualTo("persistStreamInit"));
        Assert.That(actual[0].Properties, Is.Empty);
        Assert.That(actual[0].HasPropertyPart, Is.True);
        Assert.That(operations.GetPropertyBytes(actual[0]), Is.EqualTo(replacementBinary));
        Assert.That(operations.GetControlXml(actual[0]), Does.Contain("still-preserved"));
        Assert.That(actual[1].Persistence, Is.EqualTo("persistPropertyBag"));
        Assert.That(actual[1].Properties, Has.Count.EqualTo(1));
        Assert.That(actual[1].Properties[0].Value, Is.EqualTo("Safe replacement"));
        Assert.That(actual[1].HasPropertyPart, Is.False);
        Assert.That(reopened.Package.HasPart("/ppt/activeX/activeX2.bin"), Is.False);
        Assert.That(reopened.Package.GetPart("/ppt/activeX/activeX2.xml").Relationships.Count(), Is.EqualTo(0));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ActiveXOperations_PresentationInventoryIncludesMasterControls()
    {
        PptxPresentation presentation = LoadActiveXPresentation();
        PptxPart masterPart = presentation.Package.GetPart(PptxPartNames.FirstSlideMaster);
        XmlDocument masterDocument = PptxXml.LoadBytes(masterPart.GetBytes());
        XmlElement commonSlideData = PptxXml.FindFirstDescendant(masterDocument.DocumentElement!, PptxNamespaces.Presentation, "cSld")!;
        XmlElement controls = masterDocument.CreateElement("p", "controls", PptxNamespaces.Presentation);
        controls.AppendChild(CreateControlElement(masterDocument, "rIdMasterControl", "MasterButton", "_x0000_s2025", false, "500", "300"));
        commonSlideData.AppendChild(controls);
        presentation.Package.ReplacePart(PptxPartNames.FirstSlideMaster, masterPart.ContentType, PptxXml.WriteBytes(masterDocument));
        presentation.Package.AddPart(
            "/ppt/activeX/activeX3.xml",
            PptxContentTypes.ActiveX,
            Encoding.UTF8.GetBytes("<ax:ocx xmlns:ax=\"" + PptxNamespaces.ActiveX + "\" ax:classid=\"{MASTER-CLASS}\" ax:persistence=\"persistPropertyBag\"><ax:ocxPr ax:name=\"Caption\" ax:value=\"Master\"/></ax:ocx>"));
        presentation.Package.AddRelationshipWithId(PptxPartNames.FirstSlideMaster, "rIdMasterControl", PptxRelationshipTypes.ActiveXControl, "../activeX/activeX3.xml", PptxRelationshipTargetMode.Internal);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxActiveXControlOperations operations = new PptxActiveXControlOperations();
        List<PptxActiveXControl> inventory = operations.GetInventory(reopened);
        PptxActiveXControl master = FindControl(inventory, "MasterButton");

        Assert.That(inventory, Has.Count.EqualTo(3));
        Assert.That(operations.GetSlideInventory(reopened.GetSlide(0)), Has.Count.EqualTo(2));
        Assert.That(master.SourcePartName, Is.EqualTo(PptxPartNames.FirstSlideMaster));
        Assert.That(master.ClassId, Is.EqualTo("{MASTER-CLASS}"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation LoadActiveXPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        XmlDocument slideDocument = PptxXml.LoadBytes(package.GetPart(PptxPartNames.FirstSlide).GetBytes());
        XmlElement commonSlideData = PptxXml.FindFirstDescendant(slideDocument.DocumentElement!, PptxNamespaces.Presentation, "cSld")!;
        XmlElement controls = slideDocument.CreateElement("p", "controls", PptxNamespaces.Presentation);
        controls.AppendChild(CreateControlElement(slideDocument, "rIdInlineControl", "InlineButton", "_x0000_s1025", true, "300", "200"));
        controls.AppendChild(CreateControlElement(slideDocument, "rIdBinaryControl", "BinaryButton", "_x0000_s1026", false, "400", "250"));
        commonSlideData.AppendChild(controls);
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, PptxXml.WriteBytes(slideDocument));
        package.AddPart("/ppt/activeX/activeX1.xml", PptxContentTypes.ActiveX, Encoding.UTF8.GetBytes(CreateInlineControlXml()));
        package.AddPart("/ppt/activeX/activeX2.xml", PptxContentTypes.ActiveX, Encoding.UTF8.GetBytes(CreateBinaryControlXml()));
        package.AddPart("/ppt/activeX/activeX2.bin", PptxContentTypes.ActiveXBinary, CreateBinaryProperties());
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdInlineControl", PptxRelationshipTypes.ActiveXControl, "../activeX/activeX1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdBinaryControl", PptxRelationshipTypes.ActiveXControl, "../activeX/activeX2.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId("/ppt/activeX/activeX2.xml", "rIdBinaryProperties", PptxRelationshipTypes.ActiveXControlBinary, "activeX2.bin", PptxRelationshipTargetMode.Internal);
        return PptxDocument.OpenBytes(package.SaveBytes()).LoadPresentation();
    }

    static XmlElement CreateControlElement(XmlDocument document, string relationshipId, string name, string shapeId, bool showAsIcon, string width, string height)
    {
        XmlElement control = document.CreateElement("p", "control", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(control, "r", PptxNamespaces.Relationships, "id", relationshipId);
        PptxXml.SetAttributeValue(control, "", "", "name", name);
        PptxXml.SetAttributeValue(control, "", "", "spid", shapeId);
        PptxXml.SetAttributeValue(control, "", "", "showAsIcon", showAsIcon ? "1" : "0");
        PptxXml.SetAttributeValue(control, "", "", "imgW", width);
        PptxXml.SetAttributeValue(control, "", "", "imgH", height);
        return control;
    }

    static string CreateInlineControlXml()
    {
        return
            "<ax:ocx xmlns:ax=\"" + PptxNamespaces.ActiveX + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" ax:classid=\"{INLINE-CLASS}\" ax:persistence=\"persistPropertyBag\">" +
            "<ax:ocxPr ax:name=\"Caption\" ax:value=\"Run\"/>" +
            "<ax:ocxPr ax:name=\"Enabled\" ax:value=\"True\"/>" +
            "<ax:unknown ax:value=\"preserve-inline\"/>" +
            "</ax:ocx>";
    }

    static string CreateBinaryControlXml()
    {
        return
            "<ax:ocx xmlns:ax=\"" + PptxNamespaces.ActiveX + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" ax:classid=\"{BINARY-CLASS}\" ax:license=\"license-data\" ax:persistence=\"persistStream\" r:id=\"rIdBinaryProperties\">" +
            "<ax:unknown ax:value=\"preserve-binary\"/>" +
            "</ax:ocx>";
    }

    static byte[] CreateBinaryProperties()
    {
        return new byte[] { 1, 3, 5, 7, 9 };
    }

    static bool HasRelationshipType(PptxRelationshipCollection relationships, string relationshipType)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == relationshipType)
            {
                return true;
            }
        }

        return false;
    }

    static PptxActiveXControl FindControl(List<PptxActiveXControl> controls, string name)
    {
        for (int index = 0; index < controls.Count; index++)
        {
            if (controls[index].Name == name)
            {
                return controls[index];
            }
        }

        throw new System.ArgumentException("ActiveX control was not found.");
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
