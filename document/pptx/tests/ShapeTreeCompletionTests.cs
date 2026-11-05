using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ShapeTreeCompletionTests
{
    [Test]
    public void SlideTree_CreatesBackgroundGroupsNestedShapesAndGraphicFramesWithZOrder()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();
        PptxShape first = tree.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(10L, 10L, 50L, 50L));
        first.SetName("First");
        PptxShape second = tree.AddAutoShape(PptxShapeType.Ellipse, new PptxBounds(20L, 20L, 50L, 50L));
        second.SetName("Second");
        PptxShape background = tree.AddBackgroundShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 1000L, 500L));
        background.SetName("Background");

        tree.BringToFront(first);
        tree.MoveBackward(first);
        PptxShape groupShape = tree.AddGroup(new PptxBounds(100L, 100L, 400L, 200L), new PptxBounds(0L, 0L, 200L, 100L));
        PptxGroupShape group = groupShape.AsGroup();
        PptxShape childA = group.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 50L, 50L));
        childA.SetName("Child A");
        PptxShape childB = group.AddAutoShape(PptxShapeType.Ellipse, new PptxBounds(100L, 0L, 50L, 50L));
        childB.SetName("Child B");
        group.SendToBack(childB);
        PptxShape nested = group.AddGroup(new PptxBounds(20L, 20L, 100L, 50L), new PptxBounds(0L, 0L, 100L, 50L));
        nested.AsGroup().AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(10L, 10L, 20L, 20L)).SetName("Nested Child");
        group.AddGraphicFrame("urn:test:graphic", "<x:payload xmlns:x=\"urn:test\">frame-data</x:payload>", new PptxBounds(20L, 60L, 100L, 30L));
        PptxShape removedChild = group.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 5L, 5L));
        Assert.That(tree.RemoveById(removedChild.Id), Is.True);

        PptxPresentation reopenedPresentation = SaveReopenAndLoad(presentation);
        PptxShapeTree reopened = reopenedPresentation.GetSlide(0).GetShapeTree();
        PptxShape reopenedGroup = reopened.FindByName(groupShape.Name)!;

        Assert.That(reopened.Get(0).Name, Is.EqualTo("Background"));
        Assert.That(reopened.Get(1).Name, Is.EqualTo("First"));
        Assert.That(reopened.Get(2).Name, Is.EqualTo("Second"));
        Assert.That(reopened.Get(0).IsBackgroundShape(), Is.True);
        Assert.That(reopenedGroup.IsGroup(), Is.True);
        Assert.That(reopenedGroup.ChildCount(), Is.EqualTo(4));
        Assert.That(reopenedGroup.GetChild(0).Name, Is.EqualTo("Child B"));
        Assert.That(reopenedGroup.FindDescendantByName("Nested Child"), Is.Not.Null);
        Assert.That(reopenedGroup.GetChild(3).ElementType, Is.EqualTo("graphicFrame"));
        Assert.That(reopenedGroup.GetChild(3).Element.OuterXml, Does.Contain("frame-data"));
        Assert.That(reopenedPresentation.Package.Validate().Succeeded(), Is.True, reopenedPresentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_CustomDataAndDeepCloningPreserveExtensionsAndAssignUniqueIds()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();
        PptxShape groupShape = tree.AddGroup(new PptxBounds(0L, 0L, 200L, 100L), new PptxBounds(0L, 0L, 200L, 100L));
        PptxShape child = groupShape.AsGroup().AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 50L, 50L));
        child.SetName("Tagged Child");
        child.SetCustomData("owner", "design");
        child.SetCustomData("revision", "2");
        AddUnknownShapeExtension(child, "preserved-shape-extension");
        PptxShape clone = groupShape.Clone();
        PptxSlide secondSlide = presentation.AddSlide();
        PptxShape crossTreeClone = secondSlide.GetShapeTree().CloneInto(groupShape);

        Assert.That(child.GetCustomDataNames(), Is.EquivalentTo(new string[] { "owner", "revision" }));
        Assert.That(child.GetCustomData("owner"), Is.EqualTo("design"));
        Assert.That(child.RemoveCustomData("revision"), Is.True);
        Assert.That(child.GetExtensionXml(), Has.Some.Contains("preserved-shape-extension"));

        PptxPresentation reopenedPresentation = SaveReopenAndLoad(presentation);
        PptxShapeTree reopened = reopenedPresentation.GetSlide(0).GetShapeTree();
        PptxShape originalChild = reopened.FindByName("Tagged Child")!;
        PptxShape clonedGroup = reopened.FindById(clone.Id)!;
        PptxShape reopenedCrossTreeClone = reopenedPresentation.GetSlide(1).GetShapeTree().FindById(crossTreeClone.Id)!;
        List<PptxShape> all = reopened.GetAllShapes();
        HashSet<int> ids = new HashSet<int>();
        for (int index = 0; index < all.Count; index++)
        {
            ids.Add(all[index].Id);
        }

        Assert.That(ids.Count, Is.EqualTo(all.Count));
        Assert.That(originalChild.GetCustomData("owner"), Is.EqualTo("design"));
        Assert.That(originalChild.GetCustomData("revision"), Is.Empty);
        Assert.That(originalChild.GetExtensionXml(), Has.Some.Contains("preserved-shape-extension"));
        Assert.That(clonedGroup.FindDescendantByName("Tagged Child"), Is.Not.Null);
        Assert.That(clonedGroup.FindDescendantByName("Tagged Child")!.GetCustomData("revision"), Is.EqualTo("2"));
        Assert.That(clonedGroup.FindDescendantByName("Tagged Child")!.GetExtensionXml(), Has.Some.Contains("preserved-shape-extension"));
        Assert.That(reopenedCrossTreeClone.FindDescendantByName("Tagged Child")!.GetCustomData("revision"), Is.EqualTo("2"));
        Assert.That(reopenedCrossTreeClone.FindDescendantByName("Tagged Child")!.GetExtensionXml(), Has.Some.Contains("preserved-shape-extension"));
        Assert.That(reopenedPresentation.Package.Validate().Succeeded(), Is.True, reopenedPresentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void PartTrees_EditMastersLayoutsNotesAndHandoutsAndPreserveTreeExtensions()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxPartShapeTreeOperations operations = new PptxPartShapeTreeOperations();
        PptxPartShapeTree masterTree = presentation.GetSlideMaster(0).GetShapeTree();
        AddUnknownTreeExtension(masterTree, "preserved-master-extension");
        PptxShapeMetadata masterGroup = operations.AddGroup(masterTree, new PptxBounds(0L, 0L, 400L, 200L), new PptxBounds(0L, 0L, 400L, 200L));
        PptxShapeMetadata masterFirst = operations.AddAutoShapeToGroup(masterTree, masterGroup.Id, PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 50L, 50L));
        PptxShapeMetadata masterSecond = operations.AddAutoShapeToGroup(masterTree, masterGroup.Id, PptxShapeType.Ellipse, new PptxBounds(100L, 0L, 50L, 50L));
        operations.Rename(masterTree, masterFirst.Id, "Master First");
        operations.Rename(masterTree, masterSecond.Id, "Master Second");
        operations.SetCustomData(masterTree, masterFirst.Id, "owner", "master-team");
        operations.MoveInGroup(masterTree, masterGroup.Id, 1, 0);
        PptxShapeMetadata masterClone = operations.Clone(masterTree, masterFirst.Id);

        PptxPartShapeTree layoutTree = presentation.GetSlideLayout(0).GetShapeTree();
        operations.AddGraphicFrame(layoutTree, "urn:test:layout", "<x:item xmlns:x=\"urn:test\">layout-data</x:item>", new PptxBounds(0L, 0L, 100L, 50L));

        PptxNotesSlide notes = presentation.GetSlide(0).GetOrCreateNotes();
        PptxPartShapeTree notesTree = notes.GetShapeTree();
        PptxShapeMetadata notesShape = operations.AddAutoShape(notesTree, PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 50L));
        operations.Rename(notesTree, notesShape.Id, "Notes Decoration");

        PptxNotesMaster notesMaster = presentation.GetOrCreateNotesMaster();
        PptxPartShapeTree notesMasterTree = notesMaster.GetShapeTree();
        operations.AddAutoShape(notesMasterTree, PptxShapeType.Ellipse, new PptxBounds(0L, 0L, 100L, 50L));

        PptxHandoutMaster handoutMaster = presentation.GetOrCreateHandoutMaster();
        PptxPartShapeTree handoutTree = handoutMaster.GetShapeTree();
        PptxShapeMetadata handoutBackground = operations.AddBackgroundShape(handoutTree, PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 50L));

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxPartShapeTree reopenedMaster = reopened.GetSlideMaster(0).GetShapeTree();
        PptxPartShapeTree reopenedLayout = reopened.GetSlideLayout(0).GetShapeTree();
        PptxPartShapeTree reopenedNotes = reopened.GetSlide(0).GetNotes()!.GetShapeTree();
        PptxPartShapeTree reopenedNotesMaster = reopened.GetNotesMaster()!.GetShapeTree();
        PptxPartShapeTree reopenedHandout = reopened.GetHandoutMaster()!.GetShapeTree();
        string masterXml = Encoding.UTF8.GetString(reopened.Package.GetPart(PptxPartNames.FirstSlideMaster).GetBytes());

        Assert.That(operations.FindById(reopenedMaster, masterGroup.Id), Is.Not.Null);
        Assert.That(operations.FindById(reopenedMaster, masterGroup.Id)!.Children[0].Name, Is.EqualTo("Master Second"));
        Assert.That(operations.FindById(reopenedMaster, masterClone.Id), Is.Not.Null);
        Assert.That(operations.GetCustomData(reopenedMaster, masterFirst.Id, "owner"), Is.EqualTo("master-team"));
        Assert.That(operations.GetCustomData(reopenedMaster, masterClone.Id, "owner"), Is.EqualTo("master-team"));
        Assert.That(masterXml, Does.Contain("preserved-master-extension"));
        Assert.That(masterXml.IndexOf("preserved-master-extension", System.StringComparison.Ordinal), Is.GreaterThan(masterXml.IndexOf("Master First", System.StringComparison.Ordinal)));
        Assert.That(reopenedLayout.Shapes, Has.Some.Matches<PptxShapeMetadata>(shape => shape.ElementType == "graphicFrame" && shape.RawXml.Contains("layout-data", System.StringComparison.Ordinal)));
        Assert.That(reopenedNotes.Shapes, Has.Some.Matches<PptxShapeMetadata>(shape => shape.Name == "Notes Decoration"));
        Assert.That(reopenedNotesMaster.Shapes, Has.Some.Matches<PptxShapeMetadata>(shape => shape.ElementType == "sp"));
        Assert.That(reopenedHandout.Shapes, Has.Some.Matches<PptxShapeMetadata>(shape => shape.ElementType == "sp"));
        Assert.That(operations.GetCustomData(reopenedHandout, handoutBackground.Id, "background"), Is.EqualTo("1"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void PartTree_RemoveAndTopLevelMoveRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxPartShapeTree tree = presentation.GetSlideLayout(0).GetShapeTree();
        PptxPartShapeTreeOperations operations = new PptxPartShapeTreeOperations();
        PptxShapeMetadata first = operations.AddAutoShape(tree, PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 10L, 10L));
        PptxShapeMetadata second = operations.AddAutoShape(tree, PptxShapeType.Ellipse, new PptxBounds(0L, 0L, 10L, 10L));
        operations.Rename(tree, first.Id, "First");
        operations.Rename(tree, second.Id, "Second");
        operations.Move(tree, operations.Count(tree) - 2, operations.Count(tree) - 1);
        Assert.That(operations.RemoveById(tree, second.Id), Is.True);
        Assert.That(operations.RemoveById(tree, 999999), Is.False);

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxPartShapeTree reopenedTree = reopened.GetSlideLayout(0).GetShapeTree();

        Assert.That(operations.FindById(reopenedTree, first.Id), Is.Not.Null);
        Assert.That(operations.FindById(reopenedTree, second.Id), Is.Null);
        Assert.That(operations.FindById(reopenedTree, first.Id)!.Name, Is.EqualTo("First"));
    }

    [Test]
    public void CloneInto_CopiesShapeRelationshipsToTheDestinationSlide()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide firstSlide = presentation.GetSlide(0);
        PptxShape picture = firstSlide.AddImage(CreatePngBytes(), new PptxBounds(0L, 0L, 100L, 50L));
        PptxSlide secondSlide = presentation.AddSlide();

        PptxShape clone = secondSlide.GetShapeTree().CloneInto(picture);
        PptxRelationship? clonedRelationship = secondSlide.GetShapeTree().FindRelationshipById(clone.GetImageRelationshipId());
        Assert.That(clonedRelationship, Is.Not.Null);
        Assert.That(clonedRelationship!.TargetPartName, Is.EqualTo(picture.ExtractImage().PartName));

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedClone = reopened.GetSlide(1).GetShapeTree().Get(0);

        Assert.That(reopenedClone.IsPicture(), Is.True);
        Assert.That(reopenedClone.ExtractImage().Bytes, Is.EqualTo(CreatePngBytes()));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static void AddUnknownShapeExtension(PptxShape shape, string marker)
    {
        XmlElement properties = shape.GetNonVisualDrawingProperties();
        XmlElement list = PptxXml.GetOrCreateChild(properties, "a", "extLst", PptxNamespaces.Drawing);
        XmlElement extension = shape.Element.OwnerDocument.CreateElement("a", "ext", PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(extension, "", "", "uri", "urn:test:preserve");
        XmlElement data = shape.Element.OwnerDocument.CreateElement("x", "data", "urn:test");
        data.InnerText = marker;
        extension.AppendChild(data);
        list.AppendChild(extension);
        shape.Tree.MarkDirty();
    }

    static void AddUnknownTreeExtension(PptxPartShapeTree tree, string marker)
    {
        XmlElement parent = tree.GetRequiredShapeTree();
        XmlDocument document = tree.GetRequiredDocument();
        XmlElement list = document.CreateElement("p", "extLst", PptxNamespaces.Presentation);
        XmlElement extension = document.CreateElement("p", "ext", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(extension, "", "", "uri", "urn:test:tree-preserve");
        XmlElement data = document.CreateElement("x", "data", "urn:test");
        data.InnerText = marker;
        extension.AppendChild(data);
        list.AppendChild(extension);
        parent.AppendChild(list);
        tree.MarkDirty();
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static byte[] CreatePngBytes()
    {
        byte[] bytes = new byte[24];
        bytes[0] = 137;
        bytes[1] = 80;
        bytes[2] = 78;
        bytes[3] = 71;
        bytes[4] = 13;
        bytes[5] = 10;
        bytes[6] = 26;
        bytes[7] = 10;
        bytes[12] = 73;
        bytes[13] = 72;
        bytes[14] = 68;
        bytes[15] = 82;
        bytes[19] = 100;
        bytes[23] = 50;
        return bytes;
    }
}
