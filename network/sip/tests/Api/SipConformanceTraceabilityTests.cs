namespace sip.Tests;

public class SipConformanceTraceabilityTests
{
    [Test]
    public void EveryNormativeBehaviorHasFocusedAndIntegrationCoverage()
    {
        Trace[] traces = Traces();
        List<string> failures = new List<string>();

        foreach (Trace trace in traces)
        {
            System.Reflection.MethodInfo? focused = FindTest(
                trace.FocusedType, trace.FocusedMethod);
            System.Reflection.MethodInfo? integration = FindTest(
                trace.IntegrationType, trace.IntegrationMethod);
            if (focused == null || !IsNUnitTest(focused))
            {
                failures.Add(trace.Requirement + " has no focused test: "
                    + trace.FocusedType.Name + "." + trace.FocusedMethod);
            }
            if (integration == null || !IsNUnitTest(integration))
            {
                failures.Add(trace.Requirement + " has no integration test: "
                    + trace.IntegrationType.Name + "."
                    + trace.IntegrationMethod);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(traces, Has.Length.EqualTo(84));
            Assert.That(traces.Select(value => value.Requirement).Distinct()
                .ToArray(),
                Has.Length.EqualTo(84));
            Assert.That(failures, Is.Empty);
        });
    }

    [Test]
    public void TraceMatrixSpansEveryProtocolAreaAndCrossLayerBoundary()
    {
        Trace[] traces = Traces();
        string[] areas = new string[]
        {
            "CORE", "ROUTE", "DNS", "TRANSPORT", "TX", "DIALOG",
            "AUTH", "REG", "PROXY", "USAGE", "SAFE"
        };
        Type[] integrationSuites = new Type[]
        {
            typeof(SipConcurrentIntegrationTests),
            typeof(SipDialogUsageCallTests),
            typeof(SipHostileInputTests),
            typeof(SipInProcessHarnessTests),
            typeof(SipProxyCallTests),
            typeof(SipRegistrarClientCallTests),
            typeof(SipSecureTransportCallTests),
            typeof(SipUdpTcpCallTests)
        };

        Assert.Multiple(() =>
        {
            foreach (string area in areas)
            {
                Assert.That(traces.Count(value => value.Requirement
                    .StartsWith(area + "-", StringComparison.Ordinal)),
                    Is.GreaterThan(0), area);
            }
            Assert.That(traces.Select(value => value.FocusedType).Distinct()
                .ToArray(),
                Has.Length.GreaterThanOrEqualTo(35));
            Assert.That(traces.Select(value => value.IntegrationType).Distinct(),
                Is.EquivalentTo(integrationSuites));
            Assert.That(traces, Has.All.Matches<Trace>(value =>
                value.FocusedType != value.IntegrationType));
        });
    }

    static Trace[] Traces()
    {
        List<Trace> result = new List<Trace>();

        Add(result, new string[]
        {
            "CORE-01 request status and header grammar",
            "CORE-04 combined compact and case-insensitive headers",
            "CORE-05 typed core headers"
        }, typeof(SipCoreHeaderComprehensiveTests),
            nameof(SipCoreHeaderComprehensiveTests.CanonicallyFormatsEveryCoreHeader),
            typeof(SipInProcessHarnessTests),
            nameof(SipInProcessHarnessTests.ExchangesParsedRequestsOverEveryImplementedTransport));
        Add(result, new string[]
        {
            "CORE-02 incremental bounded message framing",
            "CORE-03 loss-preserving message serialization"
        }, typeof(SipMessageParserComprehensiveTests),
            nameof(SipMessageParserComprehensiveTests.ParsesThreePipelinedMessagesWithBinaryAndMessageLikeBodies),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.StreamFramingRejectsSmugglingAndSeparatesAmbiguousSuffix));
        Add(result, new string[] { "CORE-06 typed extension headers" },
            typeof(SipExtensionHeaderComprehensiveTests),
            nameof(SipExtensionHeaderComprehensiveTests.CanonicallyFormatsEveryExtensionHeaderFamily),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.MessageInfoAndReferWithReplacesCrossTheDialogTransport));
        Add(result, new string[] { "CORE-07 generic extension preservation" },
            typeof(SipGenericExtensionTests),
            nameof(SipGenericExtensionTests.UnknownValuesSurviveMessageRoundTripAndReachRegisteredExtension),
            typeof(SipConcurrentIntegrationTests),
            nameof(SipConcurrentIntegrationTests.ExtensionCallbackCanReenterEndpointWithoutWaitingForItsLock));
        Add(result, new string[] { "CORE-08 SIP SIPS and TEL URI behavior" },
            typeof(SipUriComprehensiveTests),
            nameof(SipUriComprehensiveTests.ParsesAllSipUserPasswordParameterAndHeaderCharacterBranches),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatelessProxyForwardsLooseAndStrictRoutesEndToEnd));
        Add(result, new string[] { "CORE-09 MIME multipart and sipfrag bodies" },
            typeof(SipMimeComprehensiveTests),
            nameof(SipMimeComprehensiveTests.ParsesNestedMultipartWithQuotedBoundariesAndDigestDefaults),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.NestedMimeAndXmlBodiesAreRejectedBeforeStateIsStored));
        Add(result, new string[] { "CORE-10 SDP modeled and opaque fields" },
            typeof(SipSdpComprehensiveTests),
            nameof(SipSdpComprehensiveTests.ParsesEveryModeledSessionAndMediaFieldAndRoundTripsThem),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.ReliableInviteCompletesOfferAnswerPrackAndSessionRefresh));
        Add(result, new string[] { "CORE-11 secure XML event body codecs" },
            typeof(SipXmlBodyComprehensiveTests),
            nameof(SipXmlBodyComprehensiveTests.PreservesNestedNamespaceExtensionsWithoutResolvingExternalResources),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.PresencePublishSubscribeAndNotifyCompleteEndToEnd));
        Add(result, new string[] { "CORE-12 RFC torture message compatibility" },
            typeof(Rfc4475ConformanceTests),
            nameof(Rfc4475ConformanceTests.AcceptsAndCanonicallyRoundTripsValidParserMessages),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.EndpointRejectsOversizedHeadersAndBodiesBeforeDispatch));

        Add(result, new string[] { "ROUTE-01 collision-resistant client branches" },
            typeof(SipProtocolTokenGeneratorTests),
            nameof(SipProtocolTokenGeneratorTests.RequestsCollisionResistantEntropyForEveryTokenKind),
            typeof(SipUdpTcpCallTests),
            nameof(SipUdpTcpCallTests.LostUdpInviteIsRetransmittedButTcpNeverRetransmits));
        Add(result, new string[] { "ROUTE-02 Via rport and response routing" },
            typeof(SipViaRoutingTests),
            nameof(SipViaRoutingTests.CalculatesUnreliableResponseDestinationsInProtocolOrder),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatelessProxyForwardsLooseAndStrictRoutesEndToEnd));
        Add(result, new string[] { "ROUTE-03 loose strict and record routing" },
            typeof(SipRouteRoutingTests),
            nameof(SipRouteRoutingTests.AppliesStrictRouteSetAndAppendsRemoteTarget),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatelessProxyForwardsLooseAndStrictRoutesEndToEnd));
        Add(result, new string[] { "ROUTE-04 Contact registration and target refresh" },
            typeof(SipContactRoutingTests),
            nameof(SipContactRoutingTests.NormalizesWildcardAndOutboundRegistrationSemantics),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.AuthenticatedOutboundRegistrationRoundTripsFeatures));
        Add(result, new string[] { "ROUTE-05 forwarding loop and target safeguards" },
            typeof(SipRequestRoutingTests),
            nameof(SipRequestRoutingTests.DistinguishesLoopsFromRoutingSpirals),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatefulForkHandlesCancelAckRaceAndMultipleSuccesses));

        Add(result, new string[] { "DNS-01 RFC3263 decision paths" },
            typeof(SipRfc3263ResolverTests),
            nameof(SipRfc3263ResolverTests.AppliesNumericUriTransportSecurityDecisionTable),
            typeof(SipUdpTcpCallTests),
            nameof(SipUdpTcpCallTests.CompletesDnsInviteRefreshAndByeCallFlow));
        Add(result, new string[] { "DNS-02 NAPTR SRV and address ordering" },
            typeof(SipRfc3263ResolverTests),
            nameof(SipRfc3263ResolverTests.NaptrSelectsTransportAndSrvReplacement),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.AuthenticatedSipsProxySkipsInsecureCandidateAndUsesTls));
        Add(result, new string[] { "DNS-03 bounded DNS wire transport" },
            typeof(SipDnsClientTests),
            nameof(SipDnsClientTests.RetriesTruncatedUdpResponsesOverTcp),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.DnsClientRejectsCompressionPointerLoopReceivedOverUdp));
        Add(result, new string[] { "DNS-04 caching failover and deterministic ordering" },
            typeof(SipDnsOrderingAndCacheTests),
            nameof(SipDnsOrderingAndCacheTests.CachesPositiveResultsUntilTtlAndFlushesDefensiveCopies),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.PrimaryOutboundFlowFailureRefreshesOverBackupFlow));
        Add(result, new string[] { "DNS-05 ENUM routing extension" },
            typeof(SipEnumResolverTests),
            nameof(SipEnumResolverTests.ResolvesGlobalNumberInNaptrPreferenceOrderAndAppliesEnumTtl),
            typeof(SipInProcessHarnessTests),
            nameof(SipInProcessHarnessTests.SharesDeterministicClockScriptedDnsAndCertificateFixtures));

        Add(result, new string[] { "TRANSPORT-01 transport-neutral envelopes" },
            typeof(SipEndpointTests),
            nameof(SipEndpointTests.PollingTransportReceiveQueuesUnifiedRequestEvents),
            typeof(SipInProcessHarnessTests),
            nameof(SipInProcessHarnessTests.ExchangesParsedRequestsOverEveryImplementedTransport));
        Add(result, new string[] { "TRANSPORT-02 UDP datagram behavior" },
            typeof(UdpSipTransportTests),
            nameof(UdpSipTransportTests.SendsAndReceivesParsedSipDatagramWithIpv4EndpointMetadata),
            typeof(SipUdpTcpCallTests),
            nameof(SipUdpTcpCallTests.LostUdpInviteIsRetransmittedButTcpNeverRetransmits));
        Add(result, new string[] { "TRANSPORT-03 TCP framing pooling and fallback" },
            typeof(TcpSipTransportTests),
            nameof(TcpSipTransportTests.ReceivesByteWiseFragmentsAndPipelinedMessagesFromARawStream),
            typeof(SipUdpTcpCallTests),
            nameof(SipUdpTcpCallTests.TcpConnectionFailureTerminatesTheClientTransaction));
        Add(result, new string[] { "TRANSPORT-04 TLS identity and certificate policy" },
            typeof(TlsSipTransportTests),
            nameof(TlsSipTransportTests.ValidatesCustomRootAndServerName),
            typeof(SipSecureTransportCallTests),
            nameof(SipSecureTransportCallTests.SecureCallAuthenticatesBothPeersAndReusesConnections));
        Add(result, new string[] { "TRANSPORT-05 authenticated connection reuse" },
            typeof(SipTransportManagerTests),
            nameof(SipTransportManagerTests.ReusesTrackedFlowAndConnectionAffinity),
            typeof(SipSecureTransportCallTests),
            nameof(SipSecureTransportCallTests.DisconnectReconnectsByEndpointAndRejectsStaleAffinity));
        Add(result, new string[] { "TRANSPORT-06 SIP WebSocket integration" },
            typeof(WebSocketSipTransportTests),
            nameof(WebSocketSipTransportTests.AcceptsFragmentedTextAndBinaryWebSocketMessages),
            typeof(SipSecureTransportCallTests),
            nameof(SipSecureTransportCallTests.WebSocketFramingPreservesCompleteSipMessages));
        Add(result, new string[] { "TRANSPORT-07 outbound flows and keepalives" },
            typeof(SipOutboundFlowManagerTests),
            nameof(SipOutboundFlowManagerTests.AppliesFlowTimerJitterPongDeadlineAndRecoveryBackoff),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.PrimaryOutboundFlowFailureRefreshesOverBackupFlow));
        Add(result, new string[] { "TRANSPORT-08 keepalive symmetry and custom transports" },
            typeof(SipViaRoutingTests),
            nameof(SipViaRoutingTests.UpdatesReceivedAndValuelessRPortFromObservedSource),
            typeof(SipInProcessHarnessTests),
            nameof(SipInProcessHarnessTests.ExchangesParsedRequestsOverEveryImplementedTransport));
        Add(result, new string[] { "TRANSPORT-09 polled lifecycle and backpressure" },
            typeof(UdpSipTransportTests),
            nameof(UdpSipTransportTests.HonorsCancellationDeadlinesQueueLimitsAndClosedState),
            typeof(SipInProcessHarnessTests),
            nameof(SipInProcessHarnessTests.AppliesLossDuplicationReorderingAndFragmentationDeterministically));

        Add(result, new string[] { "TX-01 INVITE client transaction matrix" },
            typeof(SipInviteTransactionMatrixTests),
            nameof(SipInviteTransactionMatrixTests.ClientCallingHandlesEveryResponseClassBoundary),
            typeof(SipUdpTcpCallTests),
            nameof(SipUdpTcpCallTests.LostUdpInviteIsRetransmittedButTcpNeverRetransmits));
        Add(result, new string[] { "TX-02 INVITE server transaction matrix" },
            typeof(SipInviteTransactionMatrixTests),
            nameof(SipInviteTransactionMatrixTests.ServerProceedingHandlesEveryResponseClassBoundary),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.ReliableInviteCompletesOfferAnswerPrackAndSessionRefresh));
        Add(result, new string[] { "TX-03 non-INVITE client transaction matrix" },
            typeof(SipNonInviteTransactionMatrixTests),
            nameof(SipNonInviteTransactionMatrixTests.ClientTryingHandlesEveryResponseClassBoundary),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.LearnedServiceRouteIsUsedForRefreshAndUnregister));
        Add(result, new string[] { "TX-04 non-INVITE server transaction matrix" },
            typeof(SipNonInviteTransactionMatrixTests),
            nameof(SipNonInviteTransactionMatrixTests.ServerTryingHandlesEveryResponseClassBoundary),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatefulForkHandlesCancelAckRaceAndMultipleSuccesses));
        Add(result, new string[] { "TX-05 RFC6026 accepted INVITE states" },
            typeof(SipInviteTransactionMatrixTests),
            nameof(SipInviteTransactionMatrixTests.ClientAcceptedOnlyDeliversAdditionalSuccessResponses),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.ReliableInviteCompletesOfferAnswerPrackAndSessionRefresh));
        Add(result, new string[] { "TX-06 matching CANCEL ACK and merged requests" },
            typeof(SipTransactionMatchingTests),
            nameof(SipTransactionMatchingTests.MagicCookieAckMatchesInviteAndCancelTargetsOriginalTransaction),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatefulForkHandlesCancelAckRaceAndMultipleSuccesses));
        Add(result, new string[] { "TX-07 stateless transaction forwarding" },
            typeof(SipStatelessTransactionTests),
            nameof(SipStatelessTransactionTests.StatelessRequestForwardingDecrementsCopyAndLeavesInputUntouched),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatelessProxyForwardsLooseAndStrictRoutesEndToEnd));
        Add(result, new string[] { "TX-08 configurable monotonic transaction timers" },
            typeof(SipInviteTransactionMatrixTests),
            nameof(SipInviteTransactionMatrixTests.ClientUnreliableTimersHonorExactABAndDBoundaries),
            typeof(SipConcurrentIntegrationTests),
            nameof(SipConcurrentIntegrationTests.ConcurrentTimerExpiryAndShutdownConvergeOnce));
        Add(result, new string[] { "TX-09 safe transport failover" },
            typeof(SipTransportManagerTests),
            nameof(SipTransportManagerTests.ConvertsConnectionLossIntoFlowFailureAndRejectsFailedFlow),
            typeof(SipUdpTcpCallTests),
            nameof(SipUdpTcpCallTests.TcpConnectionFailureTerminatesTheClientTransaction));

        Add(result, new string[] { "DIALOG-01 dialog identity state and ownership" },
            typeof(SipDialogLayerTests),
            nameof(SipDialogLayerTests.UacProvisionalCreatesDetachedEarlyDialogIdentityAndSnapshot),
            typeof(SipConcurrentIntegrationTests),
            nameof(SipConcurrentIntegrationTests.ConcurrentDialogTerminationRemovesEveryIndependentDialog));
        Add(result, new string[] { "DIALOG-02 forked early and confirmed dialogs" },
            typeof(SipDialogForkingTests),
            nameof(SipDialogForkingTests.MultipleSuccessResponsesCreateDialogsWithIndependentAckAndByeCleanup),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatefulForkHandlesCancelAckRaceAndMultipleSuccesses));
        Add(result, new string[] { "DIALOG-03 in-dialog routing sequence and target refresh" },
            typeof(SipDialogRoutingTests),
            nameof(SipDialogRoutingTests.LocalRequestsUseDialogIdentityLooseRoutesAndMonotonicCSeq),
            typeof(SipUdpTcpCallTests),
            nameof(SipUdpTcpCallTests.CompletesDnsInviteRefreshAndByeCallFlow));
        Add(result, new string[] { "DIALOG-04 complete INVITE usage lifecycle" },
            typeof(SipInviteLifecycleTests),
            nameof(SipInviteLifecycleTests.TransportFailureTerminatesDialogAndClearsPendingState),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.ReliableInviteCompletesOfferAnswerPrackAndSessionRefresh));
        Add(result, new string[] { "DIALOG-05 offer answer state machine" },
            typeof(SipOfferAnswerSessionTests),
            nameof(SipOfferAnswerSessionTests.OneOfferInvariantRejectsGlareButAllowsOfferRetransmission),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.ReliableInviteCompletesOfferAnswerPrackAndSessionRefresh));
        Add(result, new string[] { "DIALOG-06 session timer negotiation" },
            typeof(SipInviteSessionTimerTests),
            nameof(SipInviteSessionTimerTests.Initial422RetriesWithLargerMinSeAndFreshTransaction),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.ReliableInviteCompletesOfferAnswerPrackAndSessionRefresh));
        Add(result, new string[] { "DIALOG-07 dialog identity extensions and reasons" },
            typeof(SipIdentityServiceTests),
            nameof(SipIdentityServiceTests.UpdatesConnectedIdentityAndRetainsDialogTag),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.MessageInfoAndReferWithReplacesCrossTheDialogTransport));
        Add(result, new string[] { "DIALOG-08 isolated back-to-back legs" },
            typeof(SipBackToBackSessionTests),
            nameof(SipBackToBackSessionTests.AuthenticationAndTransportFailureRemainLegLocal),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.BackToBackMessageMappingKeepsTransportLegsIsolated));

        Add(result, new string[] { "AUTH-01 digest client challenge policy" },
            typeof(SipAuthenticationClientTests),
            nameof(SipAuthenticationClientTests.ChoosesStrongestAlgorithmAndAuthIntQuality),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.AuthenticatedOutboundRegistrationRoundTripsFeatures));
        Add(result, new string[] { "AUTH-02 modern and compatibility digest algorithms" },
            typeof(SipAuthenticationServerTests),
            nameof(SipAuthenticationServerTests.VerifiesEverySupportedAlgorithm),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.AuthenticatedOutboundRegistrationRoundTripsFeatures));
        Add(result, new string[] { "AUTH-03 auth-int nonce count and userhash" },
            typeof(SipAuthenticationServerTests),
            nameof(SipAuthenticationServerTests.VerifiesAuthIntAndUserHashAndRejectsChangedBody),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.DigestVerificationRejectsReplayAndTamperedNonceAfterTransport));
        Add(result, new string[] { "AUTH-04 server challenge and verification" },
            typeof(SipAuthenticationServerTests),
            nameof(SipAuthenticationServerTests.CreatesOriginChallengesInStrongestFirstOrder),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.AuthenticatedOutboundRegistrationRoundTripsFeatures));
        Add(result, new string[] { "AUTH-05 nonce integrity replay and secrecy" },
            typeof(SipAuthenticationServerTests),
            nameof(SipAuthenticationServerTests.DetectsARepeatedValidAuthorization),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.DigestVerificationRejectsReplayAndTamperedNonceAfterTransport));
        Add(result, new string[] { "AUTH-06 bearer token integration" },
            typeof(SipBearerAuthenticationTests),
            nameof(SipBearerAuthenticationTests.ServerPassesRealmAndScopeToExternalValidator),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.AuthenticatedSipsProxySkipsInsecureCandidateAndUsesTls));
        Add(result, new string[] { "AUTH-07 mutual TLS trusted identity" },
            typeof(SipCertificateAuthenticationTests),
            nameof(SipCertificateAuthenticationTests.TrustedMutualTlsPeerProducesOwnedAuthenticationResult),
            typeof(SipSecureTransportCallTests),
            nameof(SipSecureTransportCallTests.SecureCallAuthenticatesBothPeersAndReusesConnections));

        Add(result, new string[] { "REG-01 registration client state machine" },
            typeof(SipRegistrationClientCoverageTests),
            nameof(SipRegistrationClientCoverageTests.ProvisionalAndSuccessfulRefreshResponsesAdvanceStates),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.RegistrationExpiryTerminatesClientAndRegistrarEvents));
        Add(result, new string[] { "REG-02 registrar validation and atomic updates" },
            typeof(SipRegistrarBranchTests),
            nameof(SipRegistrarBranchTests.RejectsMalformedExpiryExtensionPathAndContactValues),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.AuthenticatedOutboundRegistrationRoundTripsFeatures));
        Add(result, new string[] { "REG-03 pluggable location service" },
            typeof(SipLocationServiceTests),
            nameof(SipLocationServiceTests.FiltersRemovesAndExpiresBindingsAtExactDeadlines),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatefulForkHandlesCancelAckRaceAndMultipleSuccesses));
        Add(result, new string[] { "REG-04 Path GRUU outbound and push" },
            typeof(SipOutboundServiceTests),
            nameof(SipOutboundServiceTests.CreatesOutboundGruuAndPushRegistrationContact),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.LearnedServiceRouteIsUsedForRefreshAndUnregister));
        Add(result, new string[] { "REG-05 registrar policy security and limits" },
            typeof(SipRegistrarBranchTests),
            nameof(SipRegistrarBranchTests.EnforcesOutboundRegistrationIdentityRules),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.AuthenticatedOutboundRegistrationRoundTripsFeatures));

        Add(result, new string[] { "PROXY-01 stateless and stateful forwarding" },
            typeof(SipStatelessProxyTests),
            nameof(SipStatelessProxyTests.ForwardsRequestWithRouteViaRecordRouteContactAndDnsTransport),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatelessProxyForwardsLooseAndStrictRoutesEndToEnd));
        Add(result, new string[] { "PROXY-02 forking redirect and final selection" },
            typeof(SipProxyForkingTests),
            nameof(SipProxyForkingTests.RedirectTargetsRecurseOnceDeduplicateAndHonorDepthLimit),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatefulProxyFollowsRecursiveRedirectsEndToEnd));
        Add(result, new string[] { "PROXY-03 CANCEL ACK and success races" },
            typeof(SipProxyCancelAckTests),
            nameof(SipProxyCancelAckTests.FailureAckIsAbsorbedWhileSuccessAckIsForwardedStatelessly),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatefulForkHandlesCancelAckRaceAndMultipleSuccesses));
        Add(result, new string[] { "PROXY-04 secure record and service routing" },
            typeof(SipStatelessProxyTests),
            nameof(SipStatelessProxyTests.EnforcesHopValidationAndSipsTransportAndContactRules),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.AuthenticatedSipsProxySkipsInsecureCandidateAndUsesTls));
        Add(result, new string[] { "PROXY-05 proxy authentication sanitation and overload" },
            typeof(SipStatefulProxyCoverageTests),
            nameof(SipStatefulProxyCoverageTests.RollsBackPartialBranchStartAfterResolutionFailure),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.QueuePressureIsBoundedAndProxyReturnsOverloadResponse));
        Add(result, new string[] { "PROXY-06 redirect server response policy" },
            typeof(SipRedirectServerTests),
            nameof(SipRedirectServerTests.CreatesOrderedRedirectContactsWithQualityAndExpiry),
            typeof(SipProxyCallTests),
            nameof(SipProxyCallTests.StatefulProxyFollowsRecursiveRedirectsEndToEnd));
        Add(result, new string[] { "PROXY-07 OPTIONS and generic UAS dispatch" },
            typeof(SipCapabilityNegotiatorTests),
            nameof(SipCapabilityNegotiatorTests.AddsNormalizedAdvertisementsAndPrependsFeatureCaps),
            typeof(SipConcurrentIntegrationTests),
            nameof(SipConcurrentIntegrationTests.ExtensionCallbackCanReenterEndpointWithoutWaitingForItsLock));
        Add(result, new string[] { "PROXY-08 explicit B2BUA building blocks" },
            typeof(SipBackToBackSessionTests),
            nameof(SipBackToBackSessionTests.CreatesDetachedIncomingAndOutgoingLegsWithIndependentState),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.BackToBackMessageMappingKeepsTransportLegsIsolated));

        Add(result, new string[] { "USAGE-01 composed endpoint and lower layers" },
            typeof(SipEndpointTests),
            nameof(SipEndpointTests.ExposesComposedComponentsAndRunsOrderlyLifecycle),
            typeof(SipUdpTcpCallTests),
            nameof(SipUdpTcpCallTests.CompletesDnsInviteRefreshAndByeCallFlow));
        Add(result, new string[] { "USAGE-02 generic UAC and UAS operations" },
            typeof(SipEndpointTests),
            nameof(SipEndpointTests.DispatchesRawUasRequestAndSendsApplicationExtensionResponse),
            typeof(SipConcurrentIntegrationTests),
            nameof(SipConcurrentIntegrationTests.ExtensionCallbackCanReenterEndpointWithoutWaitingForItsLock));
        Add(result, new string[] { "USAGE-03 INVITE OPTIONS MESSAGE INFO and REFER" },
            typeof(SipReferUsageTests),
            nameof(SipReferUsageTests.ImplicitSubscriptionSupportsNotifyBeforeReferResponseAndFinalProgress),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.MessageInfoAndReferWithReplacesCrossTheDialogTransport));
        Add(result, new string[] { "USAGE-04 corrected subscription usages" },
            typeof(SipSubscriptionClientTests),
            nameof(SipSubscriptionClientTests.NotifyBeforeSuccessResponseEstablishesSubscription),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.PresencePublishSubscribeAndNotifyCompleteEndToEnd));
        Add(result, new string[] { "USAGE-05 conditional publication usages" },
            typeof(SipPublicationClientTests),
            nameof(SipPublicationClientTests.ConditionalFailureRetriesAsInitialPublicationWithLatestState),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.PresencePublishSubscribeAndNotifyCompleteEndToEnd));
        Add(result, new string[] { "USAGE-06 built-in and custom event packages" },
            typeof(SipEventPackageRegistryTests),
            nameof(SipEventPackageRegistryTests.ProcessesEachStandardPackage),
            typeof(SipDialogUsageCallTests),
            nameof(SipDialogUsageCallTests.PresencePublishSubscribeAndNotifyCompleteEndToEnd));
        Add(result, new string[] { "USAGE-07 capability and preference negotiation" },
            typeof(SipCapabilityNegotiatorTests),
            nameof(SipCapabilityNegotiatorTests.NegotiatesMutualCapabilitiesIncludingMediaWildcards),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.AuthenticatedOutboundRegistrationRoundTripsFeatures));
        Add(result, new string[] { "USAGE-08 identity privacy and verification hooks" },
            typeof(SipIdentityServiceTests),
            nameof(SipIdentityServiceTests.PrivacyFilteringHonorsTrustTokensAndForwardingRules),
            typeof(SipSecureTransportCallTests),
            nameof(SipSecureTransportCallTests.SecureCallAuthenticatesBothPeersAndReusesConnections));
        Add(result, new string[] { "USAGE-09 push outbound ICE and content coding" },
            typeof(SipIceServiceTests),
            nameof(SipIceServiceTests.AppliesTypedIceWhilePreservingUnrelatedSdpAttributes),
            typeof(SipRegistrarClientCallTests),
            nameof(SipRegistrarClientCallTests.PrimaryOutboundFlowFailureRefreshesOverBackupFlow));
        Add(result, new string[] { "USAGE-10 unknown extension pass-through" },
            typeof(SipGenericExtensionTests),
            nameof(SipGenericExtensionTests.UnknownValuesSurviveMessageRoundTripAndReachRegisteredExtension),
            typeof(SipConcurrentIntegrationTests),
            nameof(SipConcurrentIntegrationTests.ExtensionCallbackCanReenterEndpointWithoutWaitingForItsLock));

        Add(result, new string[] { "SAFE-01 bounded hostile-input parsers" },
            typeof(SipMessageParserComprehensiveTests),
            nameof(SipMessageParserComprehensiveTests.OneBelowEveryConfiguredMessageLimitFails),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.StreamFramingRejectsSmugglingAndSeparatesAmbiguousSuffix));
        Add(result, new string[] { "SAFE-02 cryptographic tokens and redaction" },
            typeof(SipProtocolTokenGeneratorTests),
            nameof(SipProtocolTokenGeneratorTests.CryptographicGenerationDoesNotRepeatTokens),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.DigestVerificationRejectsReplayAndTamperedNonceAfterTransport));
        Add(result, new string[] { "SAFE-03 deterministic snapshots events and correlation" },
            typeof(SipDialogManagementTests),
            nameof(SipDialogManagementTests.MessageLookupAndQueuesExposeDetachedCorrelatedSnapshots),
            typeof(SipInProcessHarnessTests),
            nameof(SipInProcessHarnessTests.AppliesLossDuplicationReorderingAndFragmentationDeterministically));
        Add(result, new string[] { "SAFE-04 thread safety ownership and callback locks" },
            typeof(SipLocationServiceTests),
            nameof(SipLocationServiceTests.ConcurrentUpdatesToOneAddressOfRecordDoNotLoseBindings),
            typeof(SipConcurrentIntegrationTests),
            nameof(SipConcurrentIntegrationTests.ExtensionCallbackCanReenterEndpointWithoutWaitingForItsLock));
        Add(result, new string[] { "SAFE-05 limits and graceful overload" },
            typeof(SipEndpointTests),
            nameof(SipEndpointTests.CancellationDeadlineAndQueueLimitFailWithoutTransportSend),
            typeof(SipHostileInputTests),
            nameof(SipHostileInputTests.QueuePressureIsBoundedAndProxyReturnsOverloadResponse));
        Add(result, new string[] { "SAFE-06 interoperability and race fixtures" },
            typeof(Rfc4475ConformanceTests),
            nameof(Rfc4475ConformanceTests.ReportsSemanticErrorsWithoutTurningThemIntoParserCrashes),
            typeof(SipInProcessHarnessTests),
            nameof(SipInProcessHarnessTests.AppliesLossDuplicationReorderingAndFragmentationDeterministically));

        return result.ToArray();
    }

    static void Add(List<Trace> result, string[] requirements,
        Type focusedType, string focusedMethod,
        Type integrationType, string integrationMethod)
    {
        foreach (string requirement in requirements)
        {
            result.Add(new Trace(requirement, focusedType, focusedMethod,
                integrationType, integrationMethod));
        }
    }

    static System.Reflection.MethodInfo? FindTest(Type type, string name)
    {
        return type.GetMethod(name, System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance);
    }

    static bool IsNUnitTest(System.Reflection.MethodInfo method)
    {
        return method.GetCustomAttributes(false).Any(value =>
            value.GetType().Namespace == "NUnit.Framework"
                && (value.GetType().Name == "TestAttribute"
                    || value.GetType().Name == "TestCaseAttribute"
                    || value.GetType().Name == "TestCaseSourceAttribute"));
    }

    sealed class Trace
    {
        public string Requirement;
        public Type FocusedType;
        public string FocusedMethod;
        public Type IntegrationType;
        public string IntegrationMethod;

        public Trace(string requirement, Type focusedType,
            string focusedMethod, Type integrationType,
            string integrationMethod)
        {
            Requirement = requirement;
            FocusedType = focusedType;
            FocusedMethod = focusedMethod;
            IntegrationType = integrationType;
            IntegrationMethod = integrationMethod;
        }
    }
}
