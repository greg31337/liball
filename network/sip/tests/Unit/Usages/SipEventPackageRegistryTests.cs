namespace sip.Tests;

public class SipEventPackageRegistryTests
{
    [Test]
    public void ExposesStandardPackagesAndDetachedMediaTypes()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();

        SipEventPackageRegistration[] registrations =
            registry.GetEventPackageRegistrations();
        registrations[0].ContentTypes[0].Subtype = "changed";
        SipEventPackageRegistration[] second =
            registry.GetEventPackageRegistrations();

        Assert.Multiple(() =>
        {
            Assert.That(registrations.Select(item => item.EventPackage), Is.EqualTo(new string[]
            {
                SipEventPackages.Presence,
                SipEventPackages.WatcherInformation,
                SipEventPackages.Dialog,
                SipEventPackages.Registration,
                SipEventPackages.MessageSummary,
                SipEventPackages.ResourceList
            }));
            Assert.That(registrations.All(item => item.BuiltIn), Is.True);
            Assert.That(registrations.All(item => item.SupportsResourceLists), Is.True);
            Assert.That(second[0].ContentTypes[0].Subtype, Is.EqualTo("pidf+xml"));
            Assert.That(registry.RemoveEventPackage(SipEventPackages.Presence), Is.False);
        });
    }

    [Test]
    public void RegistersDispatchesAndRemovesCustomOpaquePackage()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        RecordingPackage handler = new RecordingPackage();
        handler.Registry = registry;
        SipMediaType contentType = MediaType("application", "widget-state");

        bool added = registry.RegisterEventPackage(
            "widget.state", handler, new SipMediaType[] { contentType });
        contentType.Subtype = "mutated";
        SipEventPackageResult result = registry.ProcessEventBody(
            Request("custom-1", "widget.state", "application", "widget-state", "opaque"));

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.Accepted));
            Assert.That(result.BuiltIn, Is.False);
            Assert.That(result.EventPackage, Is.EqualTo("widget.state"));
            Assert.That(handler.Calls, Is.EqualTo(1));
            Assert.That(handler.Received!.Body, Is.EqualTo(Bytes("opaque")));
            Assert.That(handler.RemovedDuringCallback, Is.True,
                "The custom callback must run outside the registry lock.");
            Assert.That(registry.RemoveEventPackage("widget.state"), Is.False);
        });
    }

    [Test]
    public void RejectsUnknownPackagesWrongMediaTypesAndMalformedBodies()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        SipEventPackageResult unknown = registry.ProcessEventBody(
            Request("bad-1", "unknown", "application", "pidf+xml", Pidf()));
        SipEventPackageResult media = registry.ProcessEventBody(
            Request("bad-2", SipEventPackages.Dialog,
                "application", "pidf+xml", Pidf()));
        SipEventPackageResult malformed = registry.ProcessEventBody(
            Request("bad-3", SipEventPackages.Presence,
                "application", "pidf+xml", "<presence>"));

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Accepted, Is.False);
            Assert.That(unknown.StatusCode, Is.EqualTo(SipStatusCodes.BadEvent));
            Assert.That(media.Accepted, Is.False);
            Assert.That(media.StatusCode, Is.EqualTo(SipStatusCodes.UnsupportedMediaType));
            Assert.That(malformed.Accepted, Is.False);
            Assert.That(malformed.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
        });
    }

    [TestCase("presence", "pidf+xml",
        "<?xml version=\"1.0\"?><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:alice@example.com\"><note>Ready</note></presence>")]
    [TestCase("presence.winfo", "watcherinfo+xml",
        "<?xml version=\"1.0\"?><watcherinfo xmlns=\"urn:ietf:params:xml:ns:watcherinfo\" version=\"0\" state=\"full\"/>")]
    [TestCase("dialog", "dialog-info+xml",
        "<?xml version=\"1.0\"?><dialog-info xmlns=\"urn:ietf:params:xml:ns:dialog-info\" version=\"0\" state=\"full\" entity=\"sip:alice@example.com\"><dialog id=\"one\"/></dialog-info>")]
    [TestCase("reg", "reginfo+xml",
        "<?xml version=\"1.0\"?><reginfo xmlns=\"urn:ietf:params:xml:ns:reginfo\" version=\"0\" state=\"full\"><registration aor=\"sip:alice@example.com\" id=\"one\" state=\"active\"/></reginfo>")]
    [TestCase("message-summary", "simple-message-summary",
        "Messages-Waiting: yes\r\nVoice-Message: 1/0\r\n")]
    [TestCase("resource-list", "resource-lists+xml",
        "<?xml version=\"1.0\"?><resource-lists xmlns=\"urn:ietf:params:xml:ns:resource-lists\"><list name=\"friends\"><entry uri=\"sip:bob@example.com\"/></list></resource-lists>")]
    public void ProcessesEachStandardPackage(
        string eventPackage, string subtype, string body)
    {
        SipEventPackageResult result = new SipEventPackageRegistry().ProcessEventBody(
            Request("standard-" + eventPackage, eventPackage, "application", subtype, body));

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True, result.ErrorMessage);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(result.BuiltIn, Is.True);
            Assert.That(result.XmlBody, Is.Not.Null);
            Assert.That(result.FullState, Is.True);
        });
    }

    [Test]
    public void EnforcesFullBaselineAndContiguousPartialVersions()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        SipEventPackageResult noBaseline = registry.ProcessEventBody(Dialog(3, "partial"));
        SipEventPackageResult full = registry.ProcessEventBody(Dialog(3, "full"));
        SipEventPackageResult partial = registry.ProcessEventBody(Dialog(4, "partial"));
        SipEventPackageResult gap = registry.ProcessEventBody(Dialog(6, "partial"));
        SipEventPackageResult resync = registry.ProcessEventBody(Dialog(6, "full"));
        SipEventPackageState state = registry.GetEventPackageState("dialog-state")!;
        state.Version = 99;
        SipEventPackageState stored = registry.GetEventPackageState("dialog-state")!;

        Assert.Multiple(() =>
        {
            Assert.That(noBaseline.StatusCode, Is.EqualTo(SipStatusCodes.Conflict));
            Assert.That(full.Accepted, Is.True);
            Assert.That(partial.Accepted, Is.True);
            Assert.That(partial.PartialState, Is.True);
            Assert.That(gap.StatusCode, Is.EqualTo(SipStatusCodes.Conflict));
            Assert.That(resync.Accepted, Is.True);
            Assert.That(stored.Version, Is.EqualTo(6));
            Assert.That(stored.FullState, Is.True);
            Assert.That(stored.Entity, Is.EqualTo("sip:alice@example.com"));
            Assert.That(registry.ClearEventPackageState("dialog-state"), Is.True);
            Assert.That(registry.GetEventPackageState("dialog-state"), Is.Null);
        });
    }

    [Test]
    public void ProcessesPidfDiffFullAndPartialDocumentsWithOneVersionSequence()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        string full = "<?xml version=\"1.0\"?>"
            + "<p:pidf-full xmlns:p=\"urn:ietf:params:xml:ns:pidf-diff\" "
            + "xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:alice@example.com\" version=\"10\">"
            + "<tuple id=\"phone\"><status><basic>open</basic></status></tuple></p:pidf-full>";
        string diff = "<?xml version=\"1.0\"?>"
            + "<p:pidf-diff xmlns:p=\"urn:ietf:params:xml:ns:pidf-diff\" "
            + "xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:alice@example.com\" version=\"11\">"
            + "<p:remove sel=\"presence/tuple\"/></p:pidf-diff>";

        SipEventPackageResult first = registry.ProcessEventBody(
            Request("pidf-diff", SipEventPackages.Presence,
                "application", "pidf-diff+xml", full));
        SipEventPackageResult second = registry.ProcessEventBody(
            Request("pidf-diff", SipEventPackages.Presence,
                "application", "pidf-diff+xml", diff));

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True, first.ErrorMessage);
            Assert.That(first.FullState, Is.True);
            Assert.That(first.Version, Is.EqualTo(10));
            Assert.That(second.Accepted, Is.True, second.ErrorMessage);
            Assert.That(second.PartialState, Is.True);
            Assert.That(second.Version, Is.EqualTo(11));
            Assert.That(second.Entity, Is.EqualTo("pres:alice@example.com"));
        });
    }

    [Test]
    public void AppliesRlmiListIdentityAndVersionRulesToAggregatedPackages()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        SipEventPackageResult partialFirst = registry.ProcessEventBody(
            Rlmi(2, false, "sip:list@example.com"));
        SipEventPackageResult full = registry.ProcessEventBody(
            Rlmi(2, true, "sip:list@example.com"));
        SipEventPackageResult partial = registry.ProcessEventBody(
            Rlmi(3, false, "sip:list@example.com"));
        SipEventPackageResult changedList = registry.ProcessEventBody(
            Rlmi(4, false, "sip:other@example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(partialFirst.StatusCode, Is.EqualTo(SipStatusCodes.Conflict));
            Assert.That(full.Accepted, Is.True, full.ErrorMessage);
            Assert.That(full.ResourceList, Is.True);
            Assert.That(full.Entity, Is.EqualTo("sip:list@example.com"));
            Assert.That(partial.Accepted, Is.True, partial.ErrorMessage);
            Assert.That(changedList.StatusCode, Is.EqualTo(SipStatusCodes.Conflict));
        });
    }

    [TestCase("<list xmlns=\"urn:ietf:params:xml:ns:rlmi\" version=\"1\" fullState=\"true\"/>")]
    [TestCase("<list xmlns=\"urn:ietf:params:xml:ns:rlmi\" uri=\"sip:list@example.com\" fullState=\"true\"/>")]
    [TestCase("<list xmlns=\"urn:ietf:params:xml:ns:rlmi\" uri=\"sip:list@example.com\" version=\"1\" fullState=\"sometimes\"/>")]
    public void RejectsRlmiWithoutRequiredMetadata(string xml)
    {
        SipParseResult result = new SipXmlBodyCodec().ParseXmlBody(
            Bytes(xml), MediaType("application", "rlmi+xml"), new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
    }

    [TestCase("presence.winfo", "watcherinfo+xml", "watcherinfo",
        "urn:ietf:params:xml:ns:watcherinfo")]
    [TestCase("dialog", "dialog-info+xml", "dialog-info",
        "urn:ietf:params:xml:ns:dialog-info")]
    [TestCase("reg", "reginfo+xml", "reginfo",
        "urn:ietf:params:xml:ns:reginfo")]
    public void VersionedPackagesRequireContiguousPartialUpdates(
        string eventPackage, string subtype, string root,
        string namespaceUri)
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        SipEventPackageResult full = registry.ProcessEventBody(
            Versioned("versioned-" + eventPackage, eventPackage,
                subtype, root, namespaceUri, 10, "full"));
        SipEventPackageResult partial = registry.ProcessEventBody(
            Versioned("versioned-" + eventPackage, eventPackage,
                subtype, root, namespaceUri, 11, "partial"));
        SipEventPackageResult duplicate = registry.ProcessEventBody(
            Versioned("versioned-" + eventPackage, eventPackage,
                subtype, root, namespaceUri, 11, "partial"));
        SipEventPackageState stored = registry.GetEventPackageState(
            "versioned-" + eventPackage)!;

        Assert.Multiple(() =>
        {
            Assert.That(full.Accepted, Is.True, full.ErrorMessage);
            Assert.That(full.FullState, Is.True);
            Assert.That(full.Version, Is.EqualTo(10));
            Assert.That(partial.Accepted, Is.True, partial.ErrorMessage);
            Assert.That(partial.PartialState, Is.True);
            Assert.That(partial.Version, Is.EqualTo(11));
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.StatusCode,
                Is.EqualTo(SipStatusCodes.Conflict));
            Assert.That(stored.Version, Is.EqualTo(11));
        });
    }

    [Test]
    public void PresencePreservesExtensionsAndStoresDetachedState()
    {
        string xml = "<?xml version=\"1.0\"?>"
            + "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "xmlns:r=\"urn:ietf:params:xml:ns:pidf:rpid\" "
            + "entity=\"pres:alice@example.com\">"
            + "<tuple id=\"desk\"><status><basic>open</basic></status>"
            + "<r:relationship><r:self/></r:relationship></tuple>"
            + "</presence>";
        SipEventPackageRequest request = Request(
            "presence-extension", SipEventPackages.Presence,
            "application", "pidf+xml", xml);
        SipEventPackageRegistry registry = new SipEventPackageRegistry();

        SipEventPackageResult result = registry.ProcessEventBody(request);
        request.Body[0] = (byte)'X';
        result.XmlBody!.OriginalBytes[0] = (byte)'Y';
        SipEventPackageState stored = registry.GetEventPackageState(
            "presence-extension")!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True, result.ErrorMessage);
            Assert.That(result.Entity,
                Is.EqualTo("pres:alice@example.com"));
            Assert.That(result.XmlBody.XmlText,
                Does.Contain("r:relationship"));
            Assert.That(result.Request!.Body[0], Is.EqualTo((byte)'<'));
            Assert.That(stored.XmlBody!.OriginalBytes[0],
                Is.EqualTo((byte)'<'));
            Assert.That(stored.XmlBody.XmlText,
                Does.Contain("urn:ietf:params:xml:ns:pidf:rpid"));
        });
    }

    [Test]
    public void MessageSummaryAcceptsOptionalHeadersAndRejectsBadCounts()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        string valid = "Messages-Waiting: yes\r\n"
            + "Message-Account: sip:alice@example.com\r\n"
            + "Voice-Message: 2/3 (1/1)\r\n\r\n"
            + "Subject: retained\r\n";
        string invalid = "Messages-Waiting: yes\r\n"
            + "Voice-Message: 4294967296/0\r\n";

        SipEventPackageResult accepted = registry.ProcessEventBody(
            Request("summary-valid", SipEventPackages.MessageSummary,
                "application", "simple-message-summary", valid));
        SipEventPackageResult rejected = registry.ProcessEventBody(
            Request("summary-invalid", SipEventPackages.MessageSummary,
                "application", "simple-message-summary", invalid));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True,
                accepted.ErrorMessage);
            Assert.That(accepted.XmlBody!.RootName,
                Is.EqualTo("message-summary"));
            Assert.That(accepted.XmlBody.XmlText,
                Does.Contain("Subject: retained"));
            Assert.That(accepted.Version, Is.EqualTo(-1));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
        });
    }

    [TestCase("presence")]
    [TestCase("presence.winfo")]
    [TestCase("dialog")]
    [TestCase("reg")]
    [TestCase("message-summary")]
    [TestCase("resource-list")]
    public void RlmiAggregatesEveryStandardPackage(string eventPackage)
    {
        string subscriptionId = "rlmi-" + eventPackage;
        string xml = "<list xmlns=\"urn:ietf:params:xml:ns:rlmi\" "
            + "uri=\"sip:list@example.com\" version=\"1\" "
            + "fullState=\"true\"><resource "
            + "uri=\"sip:bob@example.com\"/></list>";

        SipEventPackageResult result = new SipEventPackageRegistry()
            .ProcessEventBody(Request(subscriptionId, eventPackage,
                "application", "rlmi+xml", xml));

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True, result.ErrorMessage);
            Assert.That(result.ResourceList, Is.True);
            Assert.That(result.FullState, Is.True);
            Assert.That(result.Version, Is.EqualTo(1));
            Assert.That(result.Entity,
                Is.EqualTo("sip:list@example.com"));
        });
    }

    [Test]
    public void ResourceListsPreserveUnknownNamespacedChildren()
    {
        string xml = "<resource-lists "
            + "xmlns=\"urn:ietf:params:xml:ns:resource-lists\" "
            + "xmlns:x=\"urn:example:policy\"><list name=\"friends\">"
            + "<entry uri=\"sip:bob@example.com\"/>"
            + "<x:policy mode=\"private\"/></list></resource-lists>";

        SipEventPackageResult result = new SipEventPackageRegistry()
            .ProcessEventBody(Request("resource-extension",
                SipEventPackages.ResourceList, "application",
                "resource-lists+xml", xml));

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True, result.ErrorMessage);
            Assert.That(result.ResourceList, Is.True);
            Assert.That(result.XmlBody!.XmlText,
                Does.Contain("x:policy"));
            Assert.That(result.XmlBody.XmlText,
                Does.Contain("urn:example:policy"));
        });
    }

    [Test]
    public void CustomPackageCanDenyUnauthorizedEventBody()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        AuthorizingPackage handler = new AuthorizingPackage();
        registry.RegisterEventPackage("protected.state", handler,
            new SipMediaType[]
            {
                MediaType("application", "protected-state")
            });
        SipEventPackageRequest request = Request(
            "protected-subscription", "protected.state",
            "application", "protected-state", "secret");
        request.CorrelationId = "unauthorized";

        SipEventPackageResult denied = registry.ProcessEventBody(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Calls, Is.EqualTo(1));
            Assert.That(denied.Accepted, Is.False);
            Assert.That(denied.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(denied.ErrorMessage, Does.Contain("authorized"));
            Assert.That(denied.CorrelationId,
                Is.EqualTo("unauthorized"));
            Assert.That(registry.GetEventPackageState(
                "protected-subscription"), Is.Null);
        });
    }

    [Test]
    public void MissingOptionsAndBodyLimitsReturnMalformedBodyFailure()
    {
        SipEventPackageRequest missingOptions = Request(
            "missing-options", SipEventPackages.Presence,
            "application", "pidf+xml", Pidf());
        missingOptions.Options = null!;
        SipEventPackageRequest limited = Request(
            "limited-body", SipEventPackages.Presence,
            "application", "pidf+xml", Pidf());
        limited.Options.MaxBodyBytes = limited.Body.Length - 1;
        SipEventPackageRegistry registry = new SipEventPackageRegistry();

        SipEventPackageResult missing = registry.ProcessEventBody(
            missingOptions);
        SipEventPackageResult tooLarge = registry.ProcessEventBody(limited);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Accepted, Is.False);
            Assert.That(missing.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(missing.ErrorMessage, Does.Contain("options"));
            Assert.That(tooLarge.Accepted, Is.False);
            Assert.That(tooLarge.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(tooLarge.ErrorMessage,
                Does.Contain("maximum size"));
        });
    }

    [Test]
    public void SubscriptionStateCannotChangeEventPackage()
    {
        SipEventPackageRegistry registry = new SipEventPackageRegistry();
        SipEventPackageResult dialog = registry.ProcessEventBody(
            Dialog(1, "full"));
        SipEventPackageRequest registration = Versioned(
            "dialog-state", SipEventPackages.Registration,
            "reginfo+xml", "reginfo",
            "urn:ietf:params:xml:ns:reginfo", 2, "full");

        SipEventPackageResult changed = registry.ProcessEventBody(
            registration);

        Assert.Multiple(() =>
        {
            Assert.That(dialog.Accepted, Is.True);
            Assert.That(changed.Accepted, Is.False);
            Assert.That(changed.StatusCode,
                Is.EqualTo(SipStatusCodes.Conflict));
            Assert.That(changed.ErrorMessage,
                Does.Contain("cannot change"));
        });
    }

    static SipEventPackageRequest Dialog(int version, string state)
    {
        string xml = "<?xml version=\"1.0\"?>"
            + "<dialog-info xmlns=\"urn:ietf:params:xml:ns:dialog-info\" version=\""
            + version + "\" state=\"" + state
            + "\" entity=\"sip:alice@example.com\"><dialog id=\"one\"/></dialog-info>";
        return Request("dialog-state", SipEventPackages.Dialog,
            "application", "dialog-info+xml", xml);
    }

    static SipEventPackageRequest Versioned(
        string subscriptionId, string eventPackage, string subtype,
        string root, string namespaceUri, int version, string state)
    {
        string entity = root == "dialog-info"
            ? " entity=\"sip:alice@example.com\"" : "";
        string xml = "<?xml version=\"1.0\"?><" + root
            + " xmlns=\"" + namespaceUri + "\" version=\""
            + version + "\" state=\"" + state + "\"" + entity
            + "></" + root + ">";
        return Request(subscriptionId, eventPackage,
            "application", subtype, xml);
    }

    static SipEventPackageRequest Rlmi(int version, bool full, string uri)
    {
        string xml = "<?xml version=\"1.0\"?>"
            + "<list xmlns=\"urn:ietf:params:xml:ns:rlmi\" uri=\"" + uri
            + "\" version=\"" + version + "\" fullState=\""
            + (full ? "true" : "false") + "\"><resource uri=\"sip:bob@example.com\"/>"
            + "</list>";
        return Request("rlmi-state", SipEventPackages.Presence,
            "application", "rlmi+xml", xml);
    }

    static SipEventPackageRequest Request(string subscriptionId,
        string eventPackage, string type, string subtype, string body)
    {
        SipEventPackageRequest request = new SipEventPackageRequest();
        request.SubscriptionId = subscriptionId;
        request.EventPackage = eventPackage;
        request.Body = Bytes(body);
        request.ContentType = MediaType(type, subtype);
        request.CorrelationId = "correlation";
        return request;
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = type;
        mediaType.Subtype = subtype;
        return mediaType;
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string Pidf()
    {
        return "<?xml version=\"1.0\"?><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:alice@example.com\"/>";
    }

    class RecordingPackage : ISipEventPackage
    {
        public SipEventPackageRegistry? Registry;
        public SipEventPackageRequest? Received;
        public int Calls;
        public bool RemovedDuringCallback;

        public SipEventPackageResult ProcessEventBody(SipEventPackageRequest request)
        {
            Calls++;
            Received = request;
            RemovedDuringCallback = Registry!.RemoveEventPackage("widget.state");
            SipEventPackageResult result = new SipEventPackageResult();
            result.Accepted = true;
            result.StatusCode = SipStatusCodes.Accepted;
            return result;
        }
    }

    class AuthorizingPackage : ISipEventPackage
    {
        public int Calls;

        public SipEventPackageResult ProcessEventBody(
            SipEventPackageRequest request)
        {
            Calls = Calls + 1;
            SipEventPackageResult result = new SipEventPackageResult();
            result.StatusCode = SipStatusCodes.Forbidden;
            result.ErrorMessage =
                "The event body was not authorized.";
            return result;
        }
    }
}
