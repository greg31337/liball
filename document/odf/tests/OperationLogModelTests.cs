using System.Text;

namespace odf.Tests;

public class OperationLogModelTests
{
    [Test]
    public void StableNodeIdentifiersAreUniqueAndPersistOnRoundTrip()
    {
        OdfDocument document = ReadText("<text:p>One</text:p><text:p>Two</text:p>");
        OdfNode first = document.TextBlocks[0].Node!;
        OdfNode second = document.TextBlocks[1].Node!;

        string firstId = StableNodeIdModel.EnsureStableNodeId(document, first);
        string repeatedId = StableNodeIdModel.EnsureStableNodeId(document, first);
        string secondId = StableNodeIdModel.EnsureStableNodeId(document, second);
        OdfDocument reread = ReadTextBytes(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(firstId, Is.Not.Empty);
            Assert.That(repeatedId, Is.EqualTo(firstId));
            Assert.That(secondId, Is.Not.EqualTo(firstId));
            Assert.That(StableNodeIdModel.FindStableNode(reread, firstId),
                Is.Not.Null);
            Assert.That(StableNodeIdModel.FindStableNode(reread, secondId),
                Is.Not.Null);
        });
    }

    [Test]
    public void ReplaysBySequenceAndInvertsInReverseOrder()
    {
        OdfDocument document = ReadText("<text:p>A</text:p>");
        OdfNode paragraph = document.TextBlocks[0].Node!;
        OdfOperation first = OperationLogModel.CreateSetTextOperation(
            document, paragraph, "B", 10);
        first.Id = "first";
        OdfOperation second = OperationLogModel.CreateSetTextOperation(
            document, paragraph, "C", 20);
        second.Id = "second";
        second.PreviousValue = "B";
        second.Preconditions[1].ExpectedValue = "B";

        OdfEditResult replay = OperationLogModel.ReplayOperations(
            document, [second, first]);
        OdfOperation[] inverse = OperationLogModel.InvertOperations(
            [first, second]);
        OdfEditResult undo = OperationLogModel.ReplayOperations(
            document, inverse);

        Assert.Multiple(() =>
        {
            Assert.That(replay.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(replay.AffectedCount, Is.EqualTo(2));
            Assert.That(undo.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(NodeText(document, paragraph), Is.EqualTo("A"));
            Assert.That(inverse[0].Id, Is.EqualTo("second-inverse"));
            Assert.That(inverse[1].Id, Is.EqualTo("first-inverse"));
        });
    }

    [Test]
    public void FailedPreconditionRollsBackEarlierOperations()
    {
        OdfDocument document = ReadText("<text:p>A</text:p>");
        OdfNode paragraph = document.TextBlocks[0].Node!;
        OdfOperation first = OperationLogModel.CreateSetTextOperation(
            document, paragraph, "B", 1);
        OdfOperation conflict = OperationLogModel.CreateSetTextOperation(
            document, paragraph, "C", 2);
        conflict.Preconditions[1].ExpectedValue = "not-B";

        OdfEditResult result = OperationLogModel.ReplayOperations(
            document, [first, conflict]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(result.Changed, Is.False);
            Assert.That(result.AffectedCount, Is.Zero);
            Assert.That(NodeText(document, paragraph), Is.EqualTo("A"));
        });
    }

    [Test]
    public void ReplaysAndInvertsNodeRemoval()
    {
        OdfDocument document = ReadText("<text:p>One</text:p><text:p>Two</text:p>");
        OdfNode second = document.TextBlocks[1].Node!;
        OdfNode parent = second.Parent!;
        OdfOperation removal = OperationLogModel.CreateRemoveNodeOperation(
            document, second, 1);

        OdfEditResult removed = OperationLogModel.ReplayOperations(
            document, [removal]);
        OdfOperation inverse = OperationLogModel.InvertOperation(removal)!;
        OdfEditResult restored = OperationLogModel.ReplayOperations(
            document, [inverse]);

        Assert.Multiple(() =>
        {
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(restored.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(parent.Children, Has.Length.EqualTo(2));
            Assert.That(NodeText(document, parent.Children[1]),
                Is.EqualTo("Two"));
        });
    }

    [Test]
    public void ReplaysAndInvertsNodeInsertionAndAttributeRemoval()
    {
        OdfDocument document = ReadText(
            "<text:p text:style-name='Old'>One</text:p>");
        OdfNode paragraph = document.TextBlocks[0].Node!;
        OdfNode parent = paragraph.Parent!;
        OdfQualifiedName styleName = TextName("style-name");
        OdfOperation removeAttribute =
            OperationLogModel.CreateRemoveAttributeOperation(
                document, paragraph, styleName, 1);
        OdfOperation insert = OperationLogModel.CreateInsertNodeOperation(
            document, parent, 1, TextParagraph("Two"), 2);

        OdfEditResult replay = OperationLogModel.ReplayOperations(
            document, [insert, removeAttribute]);
        OdfOperation[] inverse = OperationLogModel.InvertOperations(
            [removeAttribute, insert]);
        OdfEditResult undo = OperationLogModel.ReplayOperations(
            document, inverse);

        Assert.Multiple(() =>
        {
            Assert.That(replay.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(undo.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(parent.Children, Has.Length.EqualTo(1));
            Assert.That(ReadAttribute(paragraph, styleName), Is.EqualTo("Old"));
        });
    }

    [Test]
    public void ReplaysAndInvertsAttributeAndMoveOperations()
    {
        string body = "<text:section text:name='left'><text:p "
            + "text:style-name='Old'>One</text:p></text:section>"
            + "<text:section text:name='right'><text:p>Two</text:p>"
            + "</text:section>";
        OdfDocument document = ReadText(body);
        OdfNode paragraph = document.TextBlocks[0].Node!;
        OdfNode left = paragraph.Parent!;
        OdfNode right = document.TextBlocks[1].Node!.Parent!;
        OdfQualifiedName styleName = TextName("style-name");
        OdfOperation attribute = OperationLogModel.CreateSetAttributeOperation(
            document, paragraph, styleName, "New", 1);
        OdfOperation move = OperationLogModel.CreateMoveNodeOperation(
            document, paragraph, right, 1, 2);

        OdfEditResult replay = OperationLogModel.ReplayOperations(
            document, [move, attribute]);
        OdfOperation[] inverse = OperationLogModel.InvertOperations(
            [attribute, move]);
        OdfEditResult undo = OperationLogModel.ReplayOperations(
            document, inverse);

        Assert.Multiple(() =>
        {
            Assert.That(replay.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(undo.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(paragraph.Parent, Is.SameAs(left));
            Assert.That(left.Children[0], Is.SameAs(paragraph));
            Assert.That(ReadAttribute(paragraph, styleName), Is.EqualTo("Old"));
        });
    }

    [Test]
    public void SerializesOperationsAndPreservesUnknownExtensionChildren()
    {
        OdfDocument document = ReadText("<text:p text:style-name='Old'>A</text:p>");
        OdfNode paragraph = document.TextBlocks[0].Node!;
        OdfOperation operation = OperationLogModel.CreateSetAttributeOperation(
            document, paragraph, TextName("style-name"), "New", 7);
        operation.Id = "operation-7";
        operation.Author = "Ada";
        operation.SessionId = "session-1";
        operation.Date = "2026-09-02T12:00:00Z";
        OdfOperation unknown = new OdfOperation();
        unknown.Kind = OdfOperationKind.Unknown;
        unknown.ExtensionNode = ForeignElement();

        OdfNode extension = OperationExtensionModel.SerializeOperationExtension(
            [operation, unknown]);
        byte[] xml = OdfTreeWriter.WriteBytes(
            [extension], new OdfSaveOptions());
        OdfNode reparsed = OdfTreeBuilder.ReadBytes(
            xml, "operations.xml", new OdfLoadOptions())[0];
        OdfOperation[] result =
            OperationExtensionModel.DeserializeOperationExtension(reparsed);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(xml),
                Does.Contain("urn:odf-sdk:collaboration:1.0"));
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0].Kind,
                Is.EqualTo(OdfOperationKind.SetAttribute));
            Assert.That(result[0].Sequence, Is.EqualTo(7));
            Assert.That(result[0].Author, Is.EqualTo("Ada"));
            Assert.That(result[0].AttributeName!.LocalName,
                Is.EqualTo("style-name"));
            Assert.That(result[0].Preconditions, Has.Length.EqualTo(2));
            Assert.That(result[1].Kind, Is.EqualTo(OdfOperationKind.Unknown));
            Assert.That(result[1].ExtensionNode!.Name!.NamespaceName,
                Is.EqualTo("urn:example:foreign"));
        });
    }

    static OdfDocument ReadText(string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text>"
            + body + "</office:text></office:body></office:document>";
        return ReadTextBytes(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument ReadTextBytes(byte[] xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            xml, "operations.fodt", new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static string NodeText(OdfDocument document, OdfNode node)
    {
        OdfPart part = document.Parts[0];
        return new OdfTreeEditor(part.Nodes).GetTextContent(node);
    }

    static OdfQualifiedName TextName(string localName)
    {
        return new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Text,
            LocalName = localName,
            Prefix = OdfNamespaces.TextPrefix
        };
    }

    static string ReadAttribute(OdfNode node, OdfQualifiedName name)
    {
        OdfAttribute? attribute = new OdfTreeEditor([node])
            .FindAttribute(node, name);
        return attribute == null ? "" : attribute.Value;
    }

    static OdfNode ForeignElement()
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = "urn:example:foreign",
                LocalName = "future-operation",
                Prefix = "future"
            },
            WasEmptyElement = true
        };
    }

    static OdfNode TextParagraph(string value)
    {
        OdfNode paragraph = new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = TextName("p")
        };
        OdfNode text = new OdfNode
        {
            Kind = OdfNodeKind.Text,
            Parent = paragraph,
            Value = value
        };
        paragraph.Children = [text];
        return paragraph;
    }
}
