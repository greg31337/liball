namespace sip.Tests;

public class StatefulFoundationTests
{
    [Test]
    public void BoundedQueuePreservesFifoOrderAndEnforcesCapacity()
    {
        SipBoundedQueue queue = new SipBoundedQueue(2);
        object first = new object();
        object second = new object();

        Assert.That(queue.GetCapacity(), Is.EqualTo(2));
        Assert.That(queue.TryEnqueue(null), Is.False);
        Assert.That(queue.TryEnqueue(first), Is.True);
        Assert.That(queue.TryEnqueue(second), Is.True);
        Assert.That(queue.TryEnqueue(new object()), Is.False);
        Assert.That(queue.GetCount(), Is.EqualTo(2));
        Assert.That(queue.TryPeek(), Is.SameAs(first));

        object[] copiedItems = queue.CopyItems();
        copiedItems[0] = new object();

        Assert.That(queue.TryDequeue(), Is.SameAs(first));
        Assert.That(queue.TryDequeue(), Is.SameAs(second));
        Assert.That(queue.TryDequeue(), Is.Null);
        Assert.That(queue.ClearItems(), Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SipBoundedQueue(0));
    }

    [Test]
    public void BoundedQueueAdmissionIsAtomicAcrossThreads()
    {
        SipBoundedQueue queue = new SipBoundedQueue(40);
        int accepted = 0;

        Parallel.For(0, 400, index =>
        {
            if (queue.TryEnqueue(index))
            {
                Interlocked.Increment(ref accepted);
            }
        });

        Assert.That(accepted, Is.EqualTo(40));
        Assert.That(queue.GetCount(), Is.EqualTo(40));
        Assert.That(queue.ClearItems(), Is.EqualTo(40));
        Assert.That(queue.GetCount(), Is.Zero);
    }

    [Test]
    public void LimitCounterAccountsAtomicallyWithoutOvercommit()
    {
        SipLimitCounter counter = new SipLimitCounter(25);
        int accepted = 0;

        Parallel.For(0, 250, index =>
        {
            if (counter.TryAcquire(1))
            {
                Interlocked.Increment(ref accepted);
            }
        });

        Assert.That(accepted, Is.EqualTo(25));
        Assert.That(counter.GetLimit(), Is.EqualTo(25));
        Assert.That(counter.GetUsedCount(), Is.EqualTo(25));
        Assert.That(counter.GetAvailableCount(), Is.Zero);
        Assert.That(counter.TryAcquire(1), Is.False);

        counter.Release(10);

        Assert.That(counter.GetUsedCount(), Is.EqualTo(15));
        Assert.That(counter.GetAvailableCount(), Is.EqualTo(10));
        Assert.That(counter.TryAcquire(10), Is.True);
        Assert.That(counter.TryAcquire(0), Is.False);
        Assert.Throws<InvalidOperationException>(() => counter.Release(26));
        Assert.Throws<ArgumentOutOfRangeException>(() => counter.Release(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SipLimitCounter(-1));
    }

    [Test]
    public void CorrelationIdsUseDeterministicRandomSeamAndTokenPrefixes()
    {
        SipCorrelationIdGenerator generator = new SipCorrelationIdGenerator(new SequentialRandomSource(0));

        string first = generator.CreateCorrelationId("tx-");
        string second = generator.CreateCorrelationId("tx-");

        Assert.That(first, Is.EqualTo("tx-000102030405060708090a0b0c0d0e0f"));
        Assert.That(second, Is.EqualTo("tx-101112131415161718191a1b1c1d1e1f"));
        Assert.That(second, Is.Not.EqualTo(first));
        Assert.Throws<ArgumentException>(() => generator.CreateCorrelationId("bad prefix"));
        Assert.Throws<ArgumentNullException>(() => generator.CreateCorrelationId(null));
    }

    [Test]
    public void TransactionSnapshotCopyDetachesNestedMutableState()
    {
        SipTransactionSnapshot source = new SipTransactionSnapshot();
        source.Id = "transaction-1";
        source.Key = new SipTransactionKey();
        source.Key.Branch = "z9hG4bK-source";
        source.Key.CSeq = 4;
        source.Request = CreateMessage("request.example", 1);
        source.Response = CreateMessage("response.example", 2);
        source.TimerNames = new string[] { "A", "B" };
        source.TimerDeadlines = new long[] { 100, 200 };
        source.CreatedAtMilliseconds = 10;
        source.UpdatedAtMilliseconds = 20;

        SipTransactionSnapshot copy = SipSnapshotCopy.CopyTransactionSnapshot(source)!;

        source.Key.Branch = "changed";
        source.Request.RequestUri!.Host = "changed.example";
        source.Request.RequestUri.Parameters[0].Value = "changed";
        source.Request.Headers[0].OriginalBytes[0] = 99;
        source.Request.ViaValues[0].Parameters[0].Value = "changed";
        source.Request.ContactValues[0].Address!.Uri!.Host = "changed.example";
        source.Request.From!.Uri!.Host = "changed.example";
        source.Request.ContentType!.Parameters[0].Value = "changed";
        source.Request.Body[0] = 99;
        source.TimerNames[0] = "changed";
        source.TimerDeadlines[0] = 999;

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(copy.Key, Is.Not.SameAs(source.Key));
            Assert.That(copy.Key!.Branch, Is.EqualTo("z9hG4bK-source"));
            Assert.That(copy.Request!.RequestUri!.Host, Is.EqualTo("request.example"));
            Assert.That(copy.Request.RequestUri.Parameters[0].Value, Is.EqualTo("transport=tcp"));
            Assert.That(copy.Request.Headers[0].OriginalBytes, Is.EqualTo(new byte[] { 1, 10 }));
            Assert.That(copy.Request.ViaValues[0].Parameters[0].Value, Is.EqualTo("branch-value"));
            Assert.That(copy.Request.ContactValues[0].Address!.Uri!.Host, Is.EqualTo("contact.example"));
            Assert.That(copy.Request.From!.Uri!.Host, Is.EqualTo("from.example"));
            Assert.That(copy.Request.ContentType!.Parameters[0].Value, Is.EqualTo("utf-8"));
            Assert.That(copy.Request.Body, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(copy.TimerNames, Is.EqualTo(new string[] { "A", "B" }));
            Assert.That(copy.TimerDeadlines, Is.EqualTo(new long[] { 100, 200 }));
        });
    }

    [Test]
    public void DialogSnapshotCopyDetachesIdentifiersRoutesAndUris()
    {
        SipDialogSnapshot source = new SipDialogSnapshot();
        source.Id = new SipDialogId();
        source.Id.CallId = "call-1";
        source.LocalUri = CreateUri("local.example");
        source.RemoteUri = CreateUri("remote.example");
        source.LocalTarget = CreateUri("local-target.example");
        source.RemoteTarget = CreateUri("remote-target.example");
        source.RouteSet = new SipRouteValue[] { CreateRoute("route.example") };
        source.UsageIds = new string[] { "invite-1" };
        source.LocalCSeq = 7;

        SipDialogSnapshot copy = SipSnapshotCopy.CopyDialogSnapshot(source)!;

        source.Id.CallId = "changed";
        source.LocalUri.Host = "changed.example";
        source.RouteSet[0].Address!.Uri!.Host = "changed.example";
        source.RouteSet[0].Address!.Parameters[0].Value = "changed";
        source.UsageIds[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(copy.Id, Is.Not.SameAs(source.Id));
            Assert.That(copy.Id!.CallId, Is.EqualTo("call-1"));
            Assert.That(copy.LocalUri!.Host, Is.EqualTo("local.example"));
            Assert.That(copy.RouteSet, Is.Not.SameAs(source.RouteSet));
            Assert.That(copy.RouteSet[0].Address!.Uri!.Host, Is.EqualTo("route.example"));
            Assert.That(copy.RouteSet[0].Address!.Parameters[0].Value, Is.EqualTo("tag-value"));
            Assert.That(copy.UsageIds, Is.EqualTo(new string[] { "invite-1" }));
            Assert.That(copy.LocalCSeq, Is.EqualTo(7));
        });
    }

    [Test]
    public void RegistrationSnapshotCopyDetachesBindingFlowAndResponseState()
    {
        SipRegistrationSnapshot source = new SipRegistrationSnapshot();
        source.AddressOfRecord = CreateUri("aor.example");
        source.Response = CreateMessage("registrar.example", 3);
        source.Bindings = new SipRegistrationBinding[] { CreateBinding() };
        source.RefreshAtMilliseconds = 500;

        SipRegistrationSnapshot copy = SipSnapshotCopy.CopyRegistrationSnapshot(source)!;

        source.AddressOfRecord.Host = "changed.example";
        source.Response.Body[0] = 99;
        source.Response.ContentType!.QualityThousandths = 100;
        source.Response.ContactValues[0].PushProvider = "changed";
        source.Bindings[0].Contact!.Address!.Uri!.Host = "changed.example";
        source.Bindings[0].ReceivedAddress!.Host = "changed.example";
        source.Bindings[0].Path[0].Address!.Uri!.Host = "changed.example";
        source.Bindings[0].Flow!.Connection!.RemoteAddress!.Host = "changed.example";

        SipRegistrationBinding copiedBinding = copy.Bindings[0];
        Assert.Multiple(() =>
        {
            Assert.That(copy.AddressOfRecord!.Host, Is.EqualTo("aor.example"));
            Assert.That(copy.Response!.Body, Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(copy.Response.ContentType!.QualityThousandths, Is.EqualTo(700));
            Assert.That(copy.Response.ContactValues[0].PushProvider, Is.EqualTo("apns"));
            Assert.That(copy.Bindings, Is.Not.SameAs(source.Bindings));
            Assert.That(copiedBinding.Contact!.Address!.Uri!.Host, Is.EqualTo("contact.example"));
            Assert.That(copiedBinding.ReceivedAddress!.Host, Is.EqualTo("received.example"));
            Assert.That(copiedBinding.Path[0].Address!.Uri!.Host, Is.EqualTo("path.example"));
            Assert.That(copiedBinding.Flow!.Connection!.RemoteAddress!.Host, Is.EqualTo("remote.example"));
            Assert.That(copiedBinding.ExpiresAtMilliseconds, Is.EqualTo(900));
        });
    }

    static SipMessage CreateMessage(string host, byte firstBodyByte)
    {
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.Method = SipMethod.Invite;
        message.MethodToken = "INVITE";
        message.RequestUri = CreateUri(host);
        SipHeader header = new SipHeader();
        header.Name = "X-Test";
        header.Value = "value";
        header.OriginalBytes = new byte[] { firstBodyByte, 10 };
        message.Headers = new SipHeader[] { header };
        SipViaValue via = new SipViaValue();
        via.Host = "via.example";
        SipUriParameter viaParameter = new SipUriParameter();
        viaParameter.Name = "branch";
        viaParameter.Value = "branch-value";
        via.Parameters = new SipUriParameter[] { viaParameter };
        message.ViaValues = new SipViaValue[] { via };
        message.RouteValues = new SipRouteValue[] { CreateRoute("route.example") };
        message.RecordRouteValues = new SipRouteValue[] { CreateRoute("record-route.example") };
        SipContactValue contact = new SipContactValue();
        contact.Address = new SipNameAddress();
        contact.Address.Uri = CreateUri("contact.example");
        contact.InstanceId = "<urn:uuid:copy-test>";
        contact.RegistrationId = 2;
        contact.Outbound = true;
        contact.PublicGruu = "sip:public@example.com;gr";
        contact.TemporaryGruu = "sip:temporary@example.com;gr";
        contact.PushProvider = "apns";
        contact.PushParameter = "topic";
        contact.PushResourceId = "device";
        contact.PushRegistrationReference = "reference";
        message.ContactValues = new SipContactValue[] { contact };
        message.From = new SipNameAddress();
        message.From.Uri = CreateUri("from.example");
        message.To = new SipNameAddress();
        message.To.Uri = CreateUri("to.example");
        message.CSeq = new SipCSeqValue();
        message.CSeq.SequenceNumber = 1;
        message.ContentType = new SipMediaType();
        message.ContentType.Type = "text";
        message.ContentType.Subtype = "plain";
        message.ContentType.QualityThousandths = 700;
        SipUriParameter charset = new SipUriParameter();
        charset.Name = "charset";
        charset.Value = "utf-8";
        message.ContentType.Parameters = new SipUriParameter[] { charset };
        message.ContentDisposition = new SipContentDisposition();
        message.ContentDisposition.Disposition = SipBodyDisposition.Render;
        message.ContentDisposition.Parameters = new SipUriParameter[] { charset };
        message.Body = new byte[] { firstBodyByte, (byte)(firstBodyByte + 1) };
        message.OriginalBytes = new byte[] { firstBodyByte, 13, 10 };
        return message;
    }

    static SipUri CreateUri(string host)
    {
        SipUri uri = new SipUri();
        uri.Scheme = SipUriScheme.Sip;
        uri.SchemeToken = "sip";
        uri.Host = host;
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = "transport";
        parameter.Value = "transport=tcp";
        parameter.HasValue = true;
        uri.Parameters = new SipUriParameter[] { parameter };
        SipUriHeader header = new SipUriHeader();
        header.Name = "Subject";
        header.Value = "test";
        uri.Headers = new SipUriHeader[] { header };
        return uri;
    }

    static SipRouteValue CreateRoute(string host)
    {
        SipRouteValue route = new SipRouteValue();
        route.Address = new SipNameAddress();
        route.Address.Uri = CreateUri(host);
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = "tag";
        parameter.Value = "tag-value";
        route.Address.Parameters = new SipUriParameter[] { parameter };
        route.LooseRouting = true;
        return route;
    }

    static SipRegistrationBinding CreateBinding()
    {
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.AddressOfRecord = CreateUri("aor.example");
        binding.Contact = new SipContactValue();
        binding.Contact.Address = new SipNameAddress();
        binding.Contact.Address.Uri = CreateUri("contact.example");
        binding.ExpiresAtMilliseconds = 900;
        binding.ReceivedAddress = new SipEndpointAddress();
        binding.ReceivedAddress.Host = "received.example";
        binding.Path = new SipRouteValue[] { CreateRoute("path.example") };
        binding.ServiceRoute = new SipRouteValue[] { CreateRoute("service-route.example") };
        binding.Flow = new SipFlow();
        binding.Flow.Id = "flow-1";
        binding.Flow.Connection = new SipConnectionInfo();
        binding.Flow.Connection.Id = "connection-1";
        binding.Flow.Connection.LocalAddress = new SipEndpointAddress();
        binding.Flow.Connection.LocalAddress.Host = "local.example";
        binding.Flow.Connection.RemoteAddress = new SipEndpointAddress();
        binding.Flow.Connection.RemoteAddress.Host = "remote.example";
        return binding;
    }
}

class SequentialRandomSource : ISipRandomSource
{
    int nextValue;

    public SequentialRandomSource(int firstValue)
    {
        nextValue = firstValue;
    }

    public void FillBytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)nextValue;
            nextValue = nextValue + 1;
            index = index + 1;
        }
    }
}
