using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class PushPullParityTests
{
    private const string Document = "<?pre data?><!DOCTYPE root [<!ELEMENT root (#PCDATA|child)*><!ATTLIST root state CDATA 'ready'><!ENTITY ent 'entity é'><!NOTATION note SYSTEM 'urn:note'><!ELEMENT child EMPTY>]><root xmlns='urn:r' xmlns:p='urn:p' p:a='v'>text<![CDATA[cdata]]><!--comment--><?inside value?>&ent;<child/></root><?post done?>";

    [Test]
    public void WholeAndArbitrarilyChunkedCharactersMatchPullEvents()
    {
        XmlParserOptions options = ParityOptions();
        List<XmlReaderState> expected = ReadPull(Document, options);

        AssertEquivalent(expected, ReadWholePush(Document, options), "whole string input");
        int chunkSize = 1;
        while (chunkSize <= 19)
        {
            AssertEquivalent(expected, ReadCharacterPush(Document, options, chunkSize), $"character chunks of {chunkSize}");
            chunkSize = chunkSize + 1;
        }
    }

    [Test]
    public void ArbitrarilyChunkedUtf8BytesMatchPullEvents()
    {
        XmlParserOptions options = ParityOptions();
        byte[] bytes = Encoding.UTF8.GetBytes(Document);
        XmlInput input = ByteInput();
        input.Bytes = bytes;
        List<XmlReaderState> expected = ReadPullInput(input, options);

        int chunkSize = 1;
        while (chunkSize <= 19)
        {
            AssertEquivalent(expected, ReadBytePush(bytes, options, chunkSize), $"byte chunks of {chunkSize}");
            chunkSize = chunkSize + 1;
        }
    }

    private static List<XmlReaderState> ReadPull(string text, XmlParserOptions options)
    {
        return ReadPullInput(StringInput(text), options);
    }

    private static List<XmlReaderState> ReadPullInput(XmlInput input, XmlParserOptions options)
    {
        XmlPullReader reader = new XmlPullReader();
        reader.BeginXmlPull(input, options, null, null, null);
        List<XmlReaderState> states = new List<XmlReaderState>();
        XmlReaderState state = reader.AdvanceXmlPull();
        states.Add(state);
        while (state.Status != XmlParserStatus.Completed)
        {
            state = reader.AdvanceXmlPull();
            states.Add(state);
        }

        return states;
    }

    private static XmlEvent[] ReadWholePush(string text, XmlParserOptions options)
    {
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(StringInput(text), options, null, null, null, null);
        XmlEventBatch batch = parser.FinishXmlPush();
        Assert.That(batch.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(batch.IsFinal, Is.True);
        return batch.Events!;
    }

    private static XmlEvent[] ReadCharacterPush(string text, XmlParserOptions options, int chunkSize)
    {
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(CharacterInput(), options, null, null, null, null);
        List<XmlEvent> events = new List<XmlEvent>();
        int offset = 0;
        while (offset < text.Length)
        {
            int count = Math.Min(chunkSize, text.Length - offset);
            bool isFinal = offset + count == text.Length;
            XmlEventBatch batch = parser.FeedXmlPushCharacters(text.AsMemory(offset, count), isFinal);
            AddEvents(events, batch);
            offset = offset + count;
        }

        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Completed));
        return events.ToArray();
    }

    private static XmlEvent[] ReadBytePush(byte[] bytes, XmlParserOptions options, int chunkSize)
    {
        XmlPushParser parser = new XmlPushParser();
        parser.BeginXmlPush(ByteInput(), options, null, null, null, null);
        List<XmlEvent> events = new List<XmlEvent>();
        int offset = 0;
        while (offset < bytes.Length)
        {
            int count = Math.Min(chunkSize, bytes.Length - offset);
            bool isFinal = offset + count == bytes.Length;
            XmlEventBatch batch = parser.FeedXmlPushBytes(bytes.AsMemory(offset, count), isFinal);
            AddEvents(events, batch);
            offset = offset + count;
        }

        Assert.That(parser.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Completed));
        return events.ToArray();
    }

    private static void AddEvents(List<XmlEvent> events, XmlEventBatch batch)
    {
        XmlEvent[]? items = batch.Events;
        if (items == null)
        {
            return;
        }

        events.AddRange(items);
    }

    private static void AssertEquivalent(List<XmlReaderState> expected, XmlEvent[] actual, string scenario)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Count), scenario);
        int index = 0;
        while (index < actual.Length)
        {
            XmlReaderState state = expected[index];
            XmlEvent projected = new XmlEvent();
            projected.Kind = state.Kind;
            projected.NodeKind = state.NodeKind;
            projected.Name = state.Name;
            projected.Value = state.Value;
            projected.Attributes = state.Attributes;
            projected.NamespaceBindings = state.NamespaceBindings;
            projected.Depth = state.Depth;
            projected.IsEmptyElement = state.IsEmptyElement;
            projected.IsIgnorableWhitespace = state.IsIgnorableWhitespace;
            projected.Range = state.Range;
            projected.Diagnostic = state.Diagnostic;
            projected.ElementDeclaration = state.ElementDeclaration;
            projected.AttributeDeclaration = state.AttributeDeclaration;
            projected.EntityDeclaration = state.EntityDeclaration;
            projected.NotationDeclaration = state.NotationDeclaration;
            projected.PublicIdentifier = state.PublicIdentifier;
            projected.SystemIdentifier = state.SystemIdentifier;
            AssertDataEqual(projected, actual[index], $"{scenario}, event {index}");
            index = index + 1;
        }
    }

    private static void AssertDataEqual(object? expected, object? actual, string path)
    {
        if (expected == null || actual == null)
        {
            Assert.That(actual, Is.EqualTo(expected), path);
            return;
        }

        Type type = expected.GetType();
        Assert.That(actual.GetType(), Is.EqualTo(type), path);
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        {
            Assert.That(actual, Is.EqualTo(expected), path);
            return;
        }

        Array? expectedArray = expected as Array;
        Array? actualArray = actual as Array;
        if (expectedArray != null)
        {
            Assert.That(actualArray, Is.Not.Null, path);
            Assert.That(actualArray!.Length, Is.EqualTo(expectedArray.Length), path);
            int index = 0;
            while (index < expectedArray.Length)
            {
                AssertDataEqual(expectedArray.GetValue(index), actualArray.GetValue(index), $"{path}[{index}]");
                index = index + 1;
            }

            return;
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            FieldInfo field = fields[fieldIndex];
            AssertDataEqual(field.GetValue(expected), field.GetValue(actual), $"{path}.{field.Name}");
            fieldIndex = fieldIndex + 1;
        }
    }

    private static XmlInput StringInput(string text)
    {
        return new XmlInput
        {
            Kind = XmlInputKind.String,
            Text = text,
            BaseUri = "https://example.test/parity/document.xml"
        };
    }

    private static XmlInput CharacterInput()
    {
        return new XmlInput
        {
            Kind = XmlInputKind.CharacterMemory,
            Characters = ReadOnlyMemory<char>.Empty,
            BaseUri = "https://example.test/parity/document.xml"
        };
    }

    private static XmlInput ByteInput()
    {
        return new XmlInput
        {
            Kind = XmlInputKind.ByteMemory,
            Bytes = ReadOnlyMemory<byte>.Empty,
            BaseUri = "https://example.test/parity/document.xml"
        };
    }

    private static XmlParserOptions ParityOptions()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Parse,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveCData = true,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveEntityReferences = true,
            PreservePrefixes = true,
            PreserveSourceLocations = true
        };
    }
}
