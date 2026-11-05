using System.Text;

namespace odf.Tests;

public class ReferenceAndResourceMaintenanceTests
{
    [Test]
    public void CollectsOnlyUnusedGeneralResourcesAndMovesReferencedOnes()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        DocumentMutationModel.AddDocumentResource(
            document,
            "Resources/used.bin",
            [1, 2],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);
        DocumentMutationModel.AddDocumentResource(
            document,
            "Resources/orphan.bin",
            [3, 4],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);
        OdfNode text = document.Body!.Children[0];
        new OdfTreeEditor(document.Parts[0].Nodes).SetAttribute(
            text,
            Name(OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix),
            "Resources/used.bin");

        string[] unused = ResourceGarbageCollector.FindUnusedResources(
            document);
        string recordedUnused = document.OrphanedResourcePaths[0];
        unused[0] = "caller mutation";
        OdfEditResult collected = ResourceGarbageCollector
            .CollectUnusedResources(document);
        OdfEditResult moved = ReferenceUpdater.MoveResource(
            document,
            "Resources/used.bin",
            "Resources/moved.bin");

        Assert.Multiple(() =>
        {
            Assert.That(recordedUnused, Is.EqualTo("Resources/orphan.bin"));
            Assert.That(document.OrphanedResourcePaths, Is.Empty);
            Assert.That(collected.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(collected.AffectedCount, Is.EqualTo(1));
            Assert.That(HasEntry(document, "Resources/orphan.bin"), Is.False);
            Assert.That(moved.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(HasEntry(document, "Resources/used.bin"), Is.False);
            Assert.That(HasEntry(document, "Resources/moved.bin"), Is.True);
            Assert.That(AttributeValue(
                text,
                OdfNamespaces.XLink,
                "href"), Is.EqualTo("Resources/moved.bin"));
        });
    }

    [Test]
    public void RenamesUniqueIdentifiersAcrossReferenceForms()
    {
        OdfDocument document = ReadFlat(
            "<draw:g xml:id='target' draw:name='Target'/>"
                + "<draw:g xml:id='occupied' draw:name='Occupied'/>"
                + "<draw:connector xlink:href='#target' "
                + "draw:start-shape='target' draw:nav-order='target occupied'/>"
                + "<anim:animate smil:targetElement='target'/>");
        OdfEditResult collision = ReferenceUpdater.RenameIdentifier(
            document,
            "target",
            "occupied");
        OdfEditResult renamed = ReferenceUpdater.RenameIdentifier(
            document,
            "target",
            "renamed");
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);
        OdfNode connector = Descendant(
            document.Parts[0].Root!,
            OdfNamespaces.Drawing,
            "connector");
        OdfNode animation = Descendant(
            document.Parts[0].Root!,
            OdfNamespaces.Animation,
            "animate");

        Assert.Multiple(() =>
        {
            Assert.That(collision.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(collision.Changed, Is.False);
            Assert.That(renamed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(renamed.AffectedCount, Is.EqualTo(5));
            Assert.That(index.FindIds("target"), Is.Empty);
            Assert.That(index.FindIds("renamed"), Has.Length.EqualTo(1));
            Assert.That(AttributeValue(
                connector,
                OdfNamespaces.XLink,
                "href"), Is.EqualTo("#renamed"));
            Assert.That(AttributeValue(
                connector,
                OdfNamespaces.Drawing,
                "start-shape"), Is.EqualTo("renamed"));
            Assert.That(AttributeValue(
                connector,
                OdfNamespaces.Drawing,
                "nav-order"), Is.EqualTo("renamed occupied"));
            Assert.That(AttributeValue(
                animation,
                OdfNamespaces.Smil,
                "targetElement"), Is.EqualTo("renamed"));
        });
    }

    [Test]
    public void ResolvesCopiedSubtreeCollisionsDeterministically()
    {
        OdfDocument document = ReadFlat(
            "<draw:g draw:name='Hero' xml:id='group'>"
                + "<draw:rect draw:name='Hero' xml:id='child' "
                + "draw:start-shape='group' xlink:href='#child'/>"
                + "</draw:g>");
        OdfNode source = Descendant(
            document.Parts[0].Root!,
            OdfNamespaces.Drawing,
            "g");
        OdfNode first = new OdfTreeEditor([]).CloneSubtree(source);
        OdfNode second = new OdfTreeEditor([]).CloneSubtree(source);

        ReferenceUpdater.PrepareCopiedSubtree(document, first);
        ReferenceUpdater.PrepareCopiedSubtree(document, second);
        OdfNode firstChild = first.Children[0];
        OdfNode secondChild = second.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(
                first,
                OdfNamespaces.Xml,
                "id"), Is.EqualTo("group-2"));
            Assert.That(AttributeValue(
                first,
                OdfNamespaces.Drawing,
                "name"), Is.EqualTo("Hero Copy 2"));
            Assert.That(AttributeValue(
                firstChild,
                OdfNamespaces.Xml,
                "id"), Is.EqualTo("child-2"));
            Assert.That(AttributeValue(
                firstChild,
                OdfNamespaces.Drawing,
                "name"), Is.EqualTo("Hero Copy 3"));
            Assert.That(AttributeValue(
                firstChild,
                OdfNamespaces.Drawing,
                "start-shape"), Is.EqualTo("group-2"));
            Assert.That(AttributeValue(
                firstChild,
                OdfNamespaces.XLink,
                "href"), Is.EqualTo("#child-2"));
            Assert.That(AttributeValue(
                second,
                OdfNamespaces.Xml,
                "id"), Is.EqualTo("group-2"));
            Assert.That(AttributeValue(
                secondChild,
                OdfNamespaces.Drawing,
                "name"), Is.EqualTo("Hero Copy 3"));
        });
    }

    [Test]
    public void GeneratesStableNamesIdentifiersAndPackagePaths()
    {
        OdfDocument document = ReadFlat(
            "<draw:g draw:name='Hero' xml:id='item'/>"
                + "<draw:g xml:id='item-2'/>");
        string name = DeterministicNameModel.CreateUniqueName(
            document,
            "Hero");
        string identifier = DeterministicNameModel.CreateUniqueIdentifier(
            document,
            "item");
        string normalized = DeterministicNameModel.CreateUniqueIdentifier(
            document,
            "1 invalid");
        OdfDocument package = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        DocumentMutationModel.AddDocumentResource(
            package,
            "Resources/file.bin",
            [1],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);
        DocumentMutationModel.AddDocumentResource(
            package,
            "Resources/file-2.bin",
            [2],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);
        string path = DeterministicNameModel.CreateUniquePackagePath(
            package,
            "Resources/file.bin");
        string repeated = DeterministicNameModel.CreateUniquePackagePath(
            package,
            "Resources/file.bin");

        Assert.Multiple(() =>
        {
            Assert.That(name, Is.EqualTo("Hero Copy 2"));
            Assert.That(identifier, Is.EqualTo("item-3"));
            Assert.That(normalized, Is.EqualTo("id"));
            Assert.That(path, Is.EqualTo("Resources/file-3.bin"));
            Assert.That(repeated, Is.EqualTo(path));
        });
    }

    [Test]
    public void OrchestratesSpecializedStyleAndSheetReferenceUpdates()
    {
        OdfDocument text = ReadFlat(
            "<office:styles><style:style style:name='Body' "
                + "style:family='paragraph'/></office:styles>"
                + "<office:body><office:text>"
                + "<text:p text:style-name='Body'>Text</text:p>"
                + "</office:text></office:body>");
        OdfStyle style = text.Styles[0];
        OdfEditResult styleResult = ReferenceUpdater.RenameStyle(
            text,
            style,
            "RenamedBody");
        OdfNode paragraph = Descendant(
            text.Parts[0].Root!,
            OdfNamespaces.Text,
            "p");
        OdfDocument spreadsheet = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Spreadsheet);
        OdfTable sheet = SpreadsheetEditorModel.CreateSheet(
            spreadsheet,
            "Sheet1",
            1,
            1);
        OdfEditResult sheetResult = ReferenceUpdater.RenameSheet(
            spreadsheet,
            sheet,
            "Data");

        Assert.Multiple(() =>
        {
            Assert.That(styleResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(style.Name, Is.EqualTo("RenamedBody"));
            Assert.That(AttributeValue(
                paragraph,
                OdfNamespaces.Text,
                "style-name"), Is.EqualTo("RenamedBody"));
            Assert.That(sheetResult.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(SpreadsheetEditorModel.FindSheet(
                spreadsheet,
                "Data"), Is.Not.Null);
            Assert.That(SpreadsheetEditorModel.FindSheet(
                spreadsheet,
                "Sheet1"), Is.Null);
        });
    }

    static OdfDocument ReadFlat(string content)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:style='" + OdfNamespaces.Style
            + "' xmlns:draw='" + OdfNamespaces.Drawing
            + "' xmlns:xlink='" + OdfNamespaces.XLink
            + "' xmlns:anim='" + OdfNamespaces.Animation
            + "' xmlns:smil='" + OdfNamespaces.Smil
            + "' xmlns:xml='" + OdfNamespaces.Xml
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'>" + content + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "references.fodt",
            new OdfLoadOptions());
    }

    static OdfQualifiedName Name(
        string namespaceName,
        string localName,
        string prefix)
    {
        return new OdfQualifiedName
        {
            NamespaceName = namespaceName,
            LocalName = localName,
            Prefix = prefix
        };
    }

    static OdfNode Descendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindDescendant(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        throw new AssertionException("The expected descendant was not found.");
    }

    static OdfNode? FindDescendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindDescendant(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static string AttributeValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
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
}
