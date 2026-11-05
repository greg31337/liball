namespace pop3.Tests;

public class Pop3ImplementationContractTests
{
    [Test]
    public void PublicImplementationConstructorsProduceTheirDeclaredContracts()
    {
        MemoryStream readerStream = new MemoryStream();
        MemoryStream writerStream = new MemoryStream();
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = "alice";
        credentials.Password = "secret";

        Assert.That(new Pop3Client(), Is.InstanceOf<IPop3Client>());
        Assert.That(new Pop3Server(), Is.InstanceOf<IPop3Server>());
        Assert.That(
            new Pop3WireReader(readerStream, 1),
            Is.InstanceOf<IPop3WireReader>());
        Assert.That(
            new Pop3WireWriter(writerStream, 1),
            Is.InstanceOf<IPop3WireWriter>());
        Assert.That(
            new Pop3PlainSaslClient(credentials, true, false),
            Is.InstanceOf<IPop3SaslClient>());
        Assert.That(
            new Pop3DefaultClientTlsProvider(),
            Is.InstanceOf<IPop3ClientTlsProvider>());
        Assert.That(
            new Pop3DefaultServerTlsProvider(),
            Is.InstanceOf<IPop3ServerTlsProvider>());

        readerStream.Dispose();
        writerStream.Dispose();
    }
}
