namespace sdp.Tests.Api;

public class SdpImplementationConstructorTests
{
    [Test]
    public void PublicImplementations_ConstructAsTheirAdvertisedContracts()
    {
        ISdpParser parser = new SdpParser();
        ISdpWriter writer = new SdpWriter();
        ISdpValidator validator = new SdpValidator();
        ISdpAttributeCodec attributeCodec = new SdpAttributeCodec();
        ISdpRegistry registry = new SdpRegistry();
        ISdpSessionFactory sessionFactory = new SdpSessionFactory();
        ISdpEditor editor = new SdpEditor();
        ISdpQueryService queryService = new SdpQueryService();
        ISdpTimingService timingService = new SdpTimingService();
        ISdpCodecService codecService = new SdpCodecService();
        ISdpIceService iceService = new SdpIceService();
        ISdpDtlsService dtlsService = new SdpDtlsService();
        ISdpExtMapService extMapService = new SdpExtMapService();
        ISdpGroupService groupService = new SdpGroupService();
        ISdpBundleService bundleService = new SdpBundleService();
        ISdpStreamService streamService = new SdpStreamService();
        ISdpDataChannelService dataChannelService = new SdpDataChannelService();
        ISdpComparer comparer = new SdpComparer();
        ISdpOfferAnswerService offerAnswerService = new SdpOfferAnswerService();

        Assert.Multiple(() =>
        {
            Assert.That(parser, Is.TypeOf<SdpParser>());
            Assert.That(writer, Is.TypeOf<SdpWriter>());
            Assert.That(validator, Is.TypeOf<SdpValidator>());
            Assert.That(attributeCodec, Is.TypeOf<SdpAttributeCodec>());
            Assert.That(registry, Is.TypeOf<SdpRegistry>());
            Assert.That(sessionFactory, Is.TypeOf<SdpSessionFactory>());
            Assert.That(editor, Is.TypeOf<SdpEditor>());
            Assert.That(queryService, Is.TypeOf<SdpQueryService>());
            Assert.That(timingService, Is.TypeOf<SdpTimingService>());
            Assert.That(codecService, Is.TypeOf<SdpCodecService>());
            Assert.That(iceService, Is.TypeOf<SdpIceService>());
            Assert.That(dtlsService, Is.TypeOf<SdpDtlsService>());
            Assert.That(extMapService, Is.TypeOf<SdpExtMapService>());
            Assert.That(groupService, Is.TypeOf<SdpGroupService>());
            Assert.That(bundleService, Is.TypeOf<SdpBundleService>());
            Assert.That(streamService, Is.TypeOf<SdpStreamService>());
            Assert.That(dataChannelService, Is.TypeOf<SdpDataChannelService>());
            Assert.That(comparer, Is.TypeOf<SdpComparer>());
            Assert.That(offerAnswerService, Is.TypeOf<SdpOfferAnswerService>());
        });
    }
}
