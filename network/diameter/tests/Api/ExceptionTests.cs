namespace diameter.Tests;

public class ExceptionTests
{
    [Test]
    public void DomainExceptionConstructorsPreserveMessages()
    {
        DiameterCodecException codec = new DiameterCodecException("codec failure");
        DiameterDictionaryException dictionary = new DiameterDictionaryException("dictionary failure");
        DiameterValidationException validation = new DiameterValidationException("validation failure");
        DiameterTransportException transport = new DiameterTransportException("transport failure");
        DiameterPeerException peer = new DiameterPeerException("peer failure");
        DiameterRoutingException routing = new DiameterRoutingException("routing failure");
        DiameterSessionException session = new DiameterSessionException("session failure");
        DiameterApplicationException application = new DiameterApplicationException("application failure");
        DiameterCreditControlException creditControl = new DiameterCreditControlException("credit-control failure");

        Assert.Multiple(() =>
        {
            Assert.That(codec.Message, Is.EqualTo("codec failure"));
            Assert.That(dictionary.Message, Is.EqualTo("dictionary failure"));
            Assert.That(validation.Message, Is.EqualTo("validation failure"));
            Assert.That(transport.Message, Is.EqualTo("transport failure"));
            Assert.That(peer.Message, Is.EqualTo("peer failure"));
            Assert.That(routing.Message, Is.EqualTo("routing failure"));
            Assert.That(session.Message, Is.EqualTo("session failure"));
            Assert.That(application.Message, Is.EqualTo("application failure"));
            Assert.That(creditControl.Message, Is.EqualTo("credit-control failure"));
        });
    }
}
