using System.Text;

namespace odf.Tests;

public class XmlEventReaderTests
{
    [Test]
    public void ReadsNamespaceAwareXmlEventsWithAttributesAndLocations()
    {
        using MemoryStream input = XmlInput(FeatureXml());
        XmlEventReader reader = new XmlEventReader(
            input,
            OdfPartNames.Content,
            new OdfLoadOptions { LeaveInputOpen = true });

        List<OdfStreamEvent> events = ReadAllEvents(reader);
        OdfStreamEvent root = events.Single(item =>
            item.Kind == OdfStreamEventKind.ElementStart
            && item.Name?.LocalName == "root");
        OdfStreamEvent firstItem = events.First(item =>
            item.Kind == OdfStreamEventKind.ElementStart
            && item.Name?.LocalName == "item");
        OdfStreamEvent emptyStart = events.Single(item =>
            item.Kind == OdfStreamEventKind.ElementStart
            && item.Name?.LocalName == "empty");
        OdfStreamEvent emptyEnd = events.Single(item =>
            item.Kind == OdfStreamEventKind.ElementEnd
            && item.Name?.LocalName == "empty");

        Assert.Multiple(() =>
        {
            Assert.That(events.First().Kind,
                Is.EqualTo(OdfStreamEventKind.ProcessingInstruction));
            Assert.That(events.First().Name?.LocalName, Is.EqualTo("before"));
            Assert.That(events.Last().Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(root.NamespaceDeclarations.Select(item => item.Prefix),
                Is.EqualTo(new[] { "", "a" }));
            Assert.That(root.Attributes.Select(item => item.Name.LocalName),
                Is.EqualTo(new[] { "plain", "id" }));
            Assert.That(root.Attributes[1].Name.NamespaceName,
                Is.EqualTo("urn:a"));
            Assert.That(NamespaceValue(firstItem, ""),
                Is.EqualTo("urn:root"));
            Assert.That(NamespaceValue(firstItem, "a"),
                Is.EqualTo("urn:override"));
            Assert.That(NamespaceValue(firstItem, "xml"),
                Is.EqualTo(OdfNamespaces.Xml));
            Assert.That(firstItem.NamespaceScope.Single(item =>
                    item.Prefix == ""),
                Is.Not.SameAs(root.NamespaceScope.Single(item =>
                    item.Prefix == "")));
            Assert.That(firstItem.Name?.NamespaceName,
                Is.EqualTo("urn:override"));
            Assert.That(firstItem.Attributes.Single().Value, Is.EqualTo("x"));
            Assert.That(firstItem.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(firstItem.Location.Line, Is.GreaterThan(0));
            Assert.That(firstItem.Location.Column, Is.GreaterThan(0));
            Assert.That(events.Where(item =>
                item.Kind == OdfStreamEventKind.Text).Select(item => item.Value),
                Does.Contain("alpha"));
            Assert.That(events.Where(item =>
                item.Kind == OdfStreamEventKind.Text).Select(item => item.Value),
                Does.Contain(" beta"));
            Assert.That(events.First(item =>
                    item.Kind == OdfStreamEventKind.Text).Location.TextLength,
                Is.GreaterThan(0));
            Assert.That(events.Single(item =>
                item.Kind == OdfStreamEventKind.Comment).Value,
                Is.EqualTo("note"));
            Assert.That(events.Single(item =>
                item.Kind == OdfStreamEventKind.ProcessingInstruction
                && item.Name?.LocalName == "go").Value,
                Is.EqualTo("value"));
            Assert.That(emptyStart.IsEmptyElement, Is.True);
            Assert.That(emptyEnd.Depth, Is.EqualTo(emptyStart.Depth));
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void SelectsMatchedSubtreesWhilePreservingInheritedNamespaces()
    {
        using MemoryStream input = XmlInput(FeatureXml());
        XmlEventReader reader = new XmlEventReader(
            input,
            OdfPartNames.Content,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    ElementNames =
                    [
                        new OdfQualifiedName
                        {
                            NamespaceName = "urn:override",
                            LocalName = "item"
                        }
                    ],
                    SkipUnselectedSubtrees = false
                }
            });

        List<OdfStreamEvent> events = ReadAllEvents(reader);
        OdfStreamEvent selected = events.First(item =>
            item.Kind == OdfStreamEventKind.ElementStart);

        Assert.Multiple(() =>
        {
            Assert.That(events.Count(item =>
                item.Kind == OdfStreamEventKind.ElementStart
                && item.Name?.LocalName == "item"), Is.EqualTo(2));
            Assert.That(events.Any(item => item.Name?.LocalName == "root"),
                Is.False);
            Assert.That(events.Any(item => item.Name?.LocalName == "wrapper"),
                Is.False);
            Assert.That(events.Any(item => item.Name?.LocalName == "empty"),
                Is.True);
            Assert.That(NamespaceValue(selected, ""), Is.EqualTo("urn:root"));
            Assert.That(NamespaceValue(selected, "a"),
                Is.EqualTo("urn:override"));
            Assert.That(events.Last().Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
        });
    }

    [Test]
    public void SkipsUnselectedAndExplicitlySkippedSubtrees()
    {
        using MemoryStream filteredInput = XmlInput(FeatureXml());
        XmlEventReader filtered = new XmlEventReader(
            filteredInput,
            OdfPartNames.Content,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    ElementNames =
                    [
                        new OdfQualifiedName
                        {
                            NamespaceName = "urn:override",
                            LocalName = "item"
                        }
                    ],
                    SkipUnselectedSubtrees = true
                }
            });
        OdfStreamEvent filteredEnd = filtered.ReadNextEvent();

        using MemoryStream skippedInput = XmlInput(FeatureXml());
        XmlEventReader skipped = new XmlEventReader(
            skippedInput,
            OdfPartNames.Content,
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    IncludeProcessingInstructionEvents = false
                }
            });
        OdfStreamEvent root = skipped.ReadNextEvent();
        OdfStreamEvent wrapper = skipped.ReadNextEvent();
        skipped.SkipCurrentSubtree();
        OdfStreamEvent wrapperEnd = skipped.ReadNextEvent();
        OdfStreamEvent rootEnd = skipped.ReadNextEvent();
        OdfStreamEvent finished = skipped.ReadNextEvent();

        Assert.Multiple(() =>
        {
            Assert.That(filteredEnd.Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(root.Name?.LocalName, Is.EqualTo("root"));
            Assert.That(wrapper.Name?.LocalName, Is.EqualTo("wrapper"));
            Assert.That(wrapper.WasSkipped, Is.True);
            Assert.That(wrapperEnd.Kind,
                Is.EqualTo(OdfStreamEventKind.ElementEnd));
            Assert.That(wrapperEnd.Name?.LocalName, Is.EqualTo("wrapper"));
            Assert.That(wrapperEnd.WasSkipped, Is.True);
            Assert.That(rootEnd.Name?.LocalName, Is.EqualTo("root"));
            Assert.That(finished.Kind,
                Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(() => skipped.SkipCurrentSubtree(),
                Throws.TypeOf<OdfDocumentException>());
        });
    }

    [Test]
    public void AppliesEventFiltersAndSecurityLimits()
    {
        using MemoryStream filteredInput = XmlInput(
            "<root><!--hidden--><?hidden value?><child>visible</child></root>");
        XmlEventReader filtered = new XmlEventReader(
            filteredInput,
            "flat.xml",
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                StreamSelection = new OdfStreamSelection
                {
                    IncludeElementEvents = false,
                    IncludeCommentEvents = false,
                    IncludeProcessingInstructionEvents = false
                }
            });
        List<OdfStreamEvent> filteredEvents = ReadAllEvents(filtered);

        Assert.Multiple(() =>
        {
            Assert.That(filteredEvents.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    OdfStreamEventKind.Text,
                    OdfStreamEventKind.EndOfStream
                }));
            Assert.That(filteredEvents[0].Value, Is.EqualTo("visible"));
            Assert.That(() => ReadWithLimits(
                    "<root><child><deep/></child></root>",
                    new OdfSecurityLimits { MaximumXmlDepth = 2 }),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(() => ReadWithLimits(
                    "<root first='1' second='2'/>",
                    new OdfSecurityLimits
                    {
                        MaximumXmlAttributesPerElement = 1
                    }),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(() => ReadWithLimits(
                    "<root>too long</root>",
                    new OdfSecurityLimits { MaximumXmlTextLength = 3 }),
                Throws.TypeOf<OdfDocumentException>());
        });
    }

    [Test]
    public void HonorsCancellationAndInputOwnership()
    {
        MemoryStream owned = XmlInput("<root/>");
        XmlEventReader ownedReader = new XmlEventReader(
            owned,
            "owned.xml",
            new OdfLoadOptions { LeaveInputOpen = false });
        ReadAllEvents(ownedReader);

        MemoryStream callerOwned = XmlInput("<root><child/></root>");
        OdfCancellationState cancellation = new OdfCancellationState();
        XmlEventReader cancelledReader = new XmlEventReader(
            callerOwned,
            "caller.xml",
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                Cancellation = cancellation
            });
        cancelledReader.ReadNextEvent();
        cancellation.Reason = "stop XML";
        cancellation.IsCancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(owned.CanRead, Is.False);
            Assert.That(() => cancelledReader.ReadNextEvent(),
                Throws.TypeOf<OdfDocumentException>()
                    .With.Message.Contains("stop XML"));
            Assert.That(callerOwned.CanRead, Is.True);
        });
        callerOwned.Dispose();
    }

    static List<OdfStreamEvent> ReadAllEvents(XmlEventReader reader)
    {
        List<OdfStreamEvent> result = new List<OdfStreamEvent>();
        OdfStreamEvent item = reader.ReadNextEvent();
        result.Add(item);
        while (item.Kind != OdfStreamEventKind.EndOfStream)
        {
            item = reader.ReadNextEvent();
            result.Add(item);
        }
        return result;
    }

    static void ReadWithLimits(string xml, OdfSecurityLimits limits)
    {
        using MemoryStream input = XmlInput(xml);
        XmlEventReader reader = new XmlEventReader(
            input,
            "limited.xml",
            new OdfLoadOptions
            {
                LeaveInputOpen = true,
                SecurityLimits = limits
            });
        ReadAllEvents(reader);
    }

    static string NamespaceValue(OdfStreamEvent item, string prefix)
    {
        return item.NamespaceScope.Single(value => value.Prefix == prefix)
            .NamespaceName;
    }

    static MemoryStream XmlInput(string xml)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(xml));
    }

    static string FeatureXml()
    {
        return "<?before setup?><root xmlns='urn:root' xmlns:a='urn:a' "
            + "plain='v' a:id='1'><wrapper xmlns:a='urn:override'>"
            + "<a:item a:code='x'>alpha<![CDATA[ beta]]><!--note-->"
            + "<?go value?><empty/></a:item><other><a:item>second</a:item>"
            + "</other></wrapper></root>";
    }
}
