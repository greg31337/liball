using System;
using System.IO;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class TransportContractTests
{
    [TestCase("stream")]
    [TestCase("initiator")]
    [TestCase("acceptor")]
    [TestCase("tls")]
    public void ClosedTransportsShareExplicitLifecycleOutcomes(string transportKind)
    {
        ISessionTransport transport = CreateTransport(transportKind);

        TransportWriteResult closed = transport.CloseTransport();
        TransportReadResult read = transport.ReadTransport(new byte[1]);
        TransportWriteResult write = transport.WriteTransport([1]);

        Assert.Multiple(() =>
        {
            Assert.That(closed.State, Is.EqualTo(TransportState.Closed));
            Assert.That(closed.ErrorCode, Is.Empty);
            Assert.That(read.State, Is.EqualTo(TransportState.Closed));
            Assert.That(read.ErrorCode, Is.EqualTo("transport-closed"));
            Assert.That(read.BytesRead, Is.Zero);
            Assert.That(read.EndOfStream, Is.False);
            Assert.That(write.State, Is.EqualTo(TransportState.Closed));
            Assert.That(write.ErrorCode, Is.EqualTo("transport-closed"));
            Assert.That(write.BytesWritten, Is.Zero);
        });
    }

    [TestCase("stream")]
    [TestCase("initiator")]
    [TestCase("acceptor")]
    [TestCase("tls")]
    public void FaultedTransportsShareExplicitLifecycleOutcomes(string transportKind)
    {
        ISessionTransport transport = CreateFaultedTransport(transportKind);

        TransportReadResult read = transport.ReadTransport(new byte[1]);
        TransportWriteResult write = transport.WriteTransport([1]);

        Assert.Multiple(() =>
        {
            Assert.That(read.State, Is.EqualTo(TransportState.Faulted));
            Assert.That(read.ErrorCode, Is.EqualTo("transport-faulted"));
            Assert.That(read.BytesRead, Is.Zero);
            Assert.That(write.State, Is.EqualTo(TransportState.Faulted));
            Assert.That(write.ErrorCode, Is.EqualTo("transport-faulted"));
            Assert.That(write.BytesWritten, Is.Zero);
        });
    }

    internal static ISessionTransport CreateTransport(string transportKind)
    {
        if (transportKind == "stream") return new StreamSessionTransport(new MemoryStream());
        if (transportKind == "initiator") return new TcpInitiatorTransport(new SessionSettings());
        if (transportKind == "acceptor") return new TcpAcceptorTransport(new SessionSettings());
        return new TlsSessionTransport(new MemoryStream(), SessionRole.Initiator);
    }

    internal static ISessionTransport CreateFaultedTransport(string transportKind)
    {
        if (transportKind == "stream")
        {
            MutableCapabilityStream stream = new MutableCapabilityStream();
            StreamSessionTransport transport = new StreamSessionTransport(stream);
            stream.AllowWrite = false;
            transport.WriteTransport([1]);
            return transport;
        }
        if (transportKind == "initiator")
        {
            TcpInitiatorTransport transport = new TcpInitiatorTransport(new SessionSettings());
            transport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
            return transport;
        }
        if (transportKind == "acceptor")
        {
            TcpAcceptorTransport transport = new TcpAcceptorTransport(new SessionSettings());
            transport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
            return transport;
        }
        TlsSessionTransport tls = new TlsSessionTransport(new MemoryStream(), SessionRole.Initiator);
        tls.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
        return tls;
    }

    internal class MutableCapabilityStream : MemoryStream
    {
        internal bool AllowWrite = true;

        public override bool CanWrite
        {
            get { return AllowWrite && base.CanWrite; }
        }
    }
}
