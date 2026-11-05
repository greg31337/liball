using System.Text;
using uspto;

namespace uspto.Tests;

public class ConfiguredBulkDataReaderTests
{
    [Test]
    public void XmlCancellationIsHonoredBetweenBufferedCompoundDocuments()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        using CancellingBulkReaderStream input = new CancellingBulkReaderStream(Encoding.UTF8.GetBytes("<first/><second/>"), cancellation);
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(new UsptoClientOptions());

        try
        {
            Assert.Throws<OperationCanceledException>(() => clients.BulkDataReader!.ReadBulkXmlRecords(input, new RequestContext
            {
                CancellationToken = cancellation.Token
            }));

            Assert.That(input.ReadCount, Is.EqualTo(1));
        }
        finally
        {
            clients.Lifetime!.Dispose();
        }
    }
}

internal class CancellingBulkReaderStream : MemoryStream
{
    internal CancellationTokenSource Cancellation;
    internal int ReadCount = 0;

    internal CancellingBulkReaderStream(byte[] body, CancellationTokenSource cancellation)
        : base(body, false)
    {
        Cancellation = cancellation;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = base.Read(buffer, offset, count);
        ReadCount += 1;
        Cancellation.Cancel();
        return read;
    }
}
