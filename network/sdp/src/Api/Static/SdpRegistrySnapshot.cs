namespace sdp;

/// <summary>
/// Contains an immutable, normalized source snapshot of the checked-in IANA SDP and RTP registries.
/// </summary>
/// <remarks>
/// Each nonempty line is one registry or record. Fields are tab-delimited key/value pairs; percent, equals, tab, carriage return, and line feed are encoded as percent escapes.
/// </remarks>
public static class SdpRegistrySnapshot
{
    /// <summary>The normalized snapshot format version.</summary>
    public const int FormatVersion = 1;

    /// <summary>The update date recorded by the checked-in SDP registry.</summary>
    public const string SdpUpdated = "2026-06-01";

    /// <summary>The number of SDP registries represented in the snapshot.</summary>
    public const int SdpRegistryCount = 25;

    /// <summary>The number of SDP records represented in the snapshot.</summary>
    public const int SdpRecordCount = 460;

    /// <summary>The update date recorded by the checked-in RTP registry.</summary>
    public const string RtpUpdated = "2025-04-17";

    /// <summary>The number of RTP registries represented in the snapshot.</summary>
    public const int RtpRegistryCount = 16;

    /// <summary>The number of RTP records represented in the snapshot.</summary>
    public const int RtpRecordCount = 359;

    /// <summary>Contains normalized tab-delimited data for SdpRegistryDefinitions.</summary>
    public const string SdpRegistryDefinitions =
        "sdp-parameters-1	title=media	xref=rfc:rfc8866	registration_rule=Standards Action\n" +
        "sdp-parameters-2	title=proto	xref=rfc:rfc8866	registration_rule=RFC Required	note=Standards Track RFC preferred.	footnote=historic\n" +
        "sdp-parameters-3	title=bwtype	xref=rfc:rfc8866	xref=rfc:rfc8859	registration_rule=Standards Action\n" +
        "sdp-parameters-4	title=nettype	xref=rfc:rfc8866	registration_rule=RFC Required\n" +
        "sdp-parameters-5	title=addrtype	xref=rfc:rfc8866	registration_rule=RFC Required\n" +
        "sdp-parameters-6	title=enckey	xref=rfc:rfc8866	registration_rule=REGISTRY OBSOLETED - No further registrations accepted\n" +
        "sdp-att-field	title=attribute-name (formerly \"att-field\")	xref=rfc:rfc8866	xref=rfc:rfc8864	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Flemming Andreasen	footnote=historic; use fmtp as per\n" +
        "sdp-parameters-12	title=content SDP Parameters	xref=rfc:rfc4796	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Christer Holmberg\n" +
        "sdp-parameters-13	title=Semantics for the \"group\" SDP Attribute	xref=rfc:rfc5888	xref=rfc:rfc8859	registration_rule=Standards Action\n" +
        "sdp-parameters-14	title=\"rtcp-fb\" Attribute Values	xref=rfc:rfc4585	xref=rfc:rfc8859	registration_rule=First Come First Served with permanent, stable, and publically accessible document.\n" +
        "sdp-parameters-15	title=\"ack\" and \"nack\" Attribute Values	xref=rfc:rfc4585	xref=rfc:rfc8859	registration_rule=First Come First Served with a permanent, stable, and publically accessible document\n" +
        "sdp-parameters-16	title=\"depend\" SDP Attribute Values	xref=rfc:rfc5583	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Stephan Wenger\n" +
        "cs-correlation-values	title=\"cs-correlation\" Attribute Values	xref=rfc:rfc7195	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Bo Burman\n" +
        "sdp-parameters-17	title=Semantics for the \"ssrc-group\" SDP Attribute	xref=rfc:rfc5576	xref=rfc:rfc8859	registration_rule=Standards Action\n" +
        "sdp-parameters-18	title=SDP/RTSP key management protocol identifiers	xref=rfc:rfc4567	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Magnus Westerlund	note=This registry of the key management protocol identifier, KMPID, has a name space that is shared between the SDP \"key-mgmt\" attribute and RTSP header \"keymgmt\".\n" +
        "sdp-parameters-19	title=Codec Control Messages	xref=rfc:rfc5104	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Stephan Wenger\n" +
        "sdp-parameters-20	title=QoS Mechanism Tokens	xref=rfc:rfc5432	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Christer Holmberg\n" +
        "sdp-parameters-21	title=SDP Capability Negotiation Option Tags	xref=rfc:rfc5939	xref=rfc:rfc8859	registration_rule=IETF Review\n" +
        "sdp-parameters-22	title=SDP Capability Negotiation Configuration Parameters Registry	xref=rfc:rfc5939	xref=rfc:rfc6871	registration_rule=IETF Review\n" +
        "timestamp-ref-clock-source	title=Timestamp Reference Clock Source Parameters	xref=rfc:rfc7273	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Magnus Westerlund\n" +
        "media-clock-source	title=Media Clock Source Parameters	xref=rfc:rfc7273	xref=rfc:rfc8859	registration_rule=Specification Required	expert=Magnus Westerlund\n" +
        "multiplexing-categories	title=Multiplexing Categories	xref=rfc:rfc8859	registration_rule=Standards Action\n" +
        "association-usage-name	title=association-usage Name Registry	xref=rfc:rfc8841	registration_rule=First Come First Served\n" +
        "rid-attribute	title=RID Attribute Parameters	xref=rfc:rfc8851	registration_rule=Specification Required	expert=Adam Roach\n" +
        "candidate-attribute-extensions	title=Candidate Attribute Extensions	xref=rfc:rfc8839	registration_rule=Specification Required	expert=Christer Holmberg";

    /// <summary>Contains normalized tab-delimited data for SdpRecords.</summary>
    public const string SdpRecords =
        "sdp-parameters-1	type=media	name=audio	xref=rfc:rfc8866\n" +
        "sdp-parameters-1	type=media	name=video	xref=rfc:rfc8866\n" +
        "sdp-parameters-1	type=media	name=text	xref=rfc:rfc8866\n" +
        "sdp-parameters-1	type=media	name=application	xref=rfc:rfc8866\n" +
        "sdp-parameters-1	type=media	name=message	xref=rfc:rfc8866\n" +
        "sdp-parameters-1	type=media	name=image	xref=rfc:rfc6466\n" +
        "sdp-parameters-1	@date=2026-01-20	type=media	name=haptics	xref=rfc:rfc9993\n" +
        "sdp-parameters-2	type=proto	name=RTP/AVP	xref=rfc:rfc8866\n" +
        "sdp-parameters-2	type=proto	name=udp	xref=rfc:rfc8866\n" +
        "sdp-parameters-2	type=proto	name=vat	xref=note:1\n" +
        "sdp-parameters-2	type=proto	name=rtp	xref=note:1\n" +
        "sdp-parameters-2	type=proto	name=udptl	xref=uri:http://www.itu.int/rec/recommendation.asp?type%3Dfolders&lang%3De&parent%3DT-REC-T.38\n" +
        "sdp-parameters-2	type=proto	name=TCP	xref=rfc:rfc4145\n" +
        "sdp-parameters-2	type=proto	name=RTP/AVPF	xref=rfc:rfc4585\n" +
        "sdp-parameters-2	type=proto	name=TCP/RTP/AVP	xref=rfc:rfc4571\n" +
        "sdp-parameters-2	type=proto	name=RTP/SAVP	xref=rfc:rfc3711\n" +
        "sdp-parameters-2	type=proto	name=TCP/BFCP	xref=rfc:rfc8856\n" +
        "sdp-parameters-2	type=proto	name=TCP/TLS/BFCP	xref=rfc:rfc8856\n" +
        "sdp-parameters-2	type=proto	name=TCP/TLS	xref=rfc:rfc8122\n" +
        "sdp-parameters-2	type=proto	name=FLUTE/UDP	xref=draft:RFC-mehta-rmt-flute-sdp-05\n" +
        "sdp-parameters-2	type=proto	name=TCP/MSRP	xref=rfc:rfc4975\n" +
        "sdp-parameters-2	type=proto	name=TCP/TLS/MSRP	xref=rfc:rfc4975\n" +
        "sdp-parameters-2	type=proto	name=DCCP	xref=rfc:rfc5762\n" +
        "sdp-parameters-2	type=proto	name=DCCP/RTP/AVP	xref=rfc:rfc5762\n" +
        "sdp-parameters-2	type=proto	name=DCCP/RTP/SAVP	xref=rfc:rfc5762\n" +
        "sdp-parameters-2	type=proto	name=DCCP/RTP/AVPF	xref=rfc:rfc5762\n" +
        "sdp-parameters-2	type=proto	name=DCCP/RTP/SAVPF	xref=rfc:rfc5762\n" +
        "sdp-parameters-2	type=proto	name=RTP/SAVPF	xref=rfc:rfc5124\n" +
        "sdp-parameters-2	type=proto	name=UDP/TLS/RTP/SAVP	xref=rfc:rfc5764\n" +
        "sdp-parameters-2	type=proto	name=DCCP/TLS/RTP/SAVP	xref=rfc:rfc5764\n" +
        "sdp-parameters-2	type=proto	name=UDP/TLS/RTP/SAVPF	xref=rfc:rfc5764\n" +
        "sdp-parameters-2	type=proto	name=DCCP/TLS/RTP/SAVPF	xref=rfc:rfc5764\n" +
        "sdp-parameters-2	type=proto	name=UDP/MBMS-FEC/RTP/AVP	xref=rfc:rfc6064\n" +
        "sdp-parameters-2	type=proto	name=UDP/MBMS-FEC/RTP/SAVP	xref=rfc:rfc6064\n" +
        "sdp-parameters-2	type=proto	name=UDP/MBMS-REPAIR	xref=rfc:rfc6064\n" +
        "sdp-parameters-2	type=proto	name=FEC/UDP	xref=rfc:rfc6364\n" +
        "sdp-parameters-2	type=proto	name=UDP/FEC	xref=rfc:rfc6364\n" +
        "sdp-parameters-2	type=proto	name=TCP/MRCPv2	xref=rfc:rfc6787\n" +
        "sdp-parameters-2	type=proto	name=TCP/TLS/MRCPv2	xref=rfc:rfc6787\n" +
        "sdp-parameters-2	type=proto	name=PSTN	xref=rfc:rfc7195\n" +
        "sdp-parameters-2	@date=2014-06-23	type=proto	name=UDP/TLS/UDPTL	xref=rfc:rfc7345\n" +
        "sdp-parameters-2	@date=2015-03-20	type=proto	name=voice	xref=rfc:rfc2848\n" +
        "sdp-parameters-2	@date=2015-03-20	type=proto	name=fax	xref=rfc:rfc2848\n" +
        "sdp-parameters-2	@date=2015-03-20	type=proto	name=pager	xref=rfc:rfc2848\n" +
        "sdp-parameters-2	@date=2016-03-23	type=proto	name=TCP/RTP/AVPF	xref=rfc:rfc7850\n" +
        "sdp-parameters-2	@date=2016-03-23	type=proto	name=TCP/RTP/SAVP	xref=rfc:rfc7850\n" +
        "sdp-parameters-2	@date=2016-03-23	type=proto	name=TCP/RTP/SAVPF	xref=rfc:rfc7850\n" +
        "sdp-parameters-2	@date=2016-03-23	type=proto	name=TCP/DTLS/RTP/SAVP	xref=rfc:rfc7850\n" +
        "sdp-parameters-2	@date=2016-03-23	type=proto	name=TCP/DTLS/RTP/SAVPF	xref=rfc:rfc7850\n" +
        "sdp-parameters-2	@date=2016-03-23	type=proto	name=TCP/TLS/RTP/AVP	xref=rfc:rfc7850\n" +
        "sdp-parameters-2	@date=2016-03-23	type=proto	name=TCP/TLS/RTP/AVPF	xref=rfc:rfc7850\n" +
        "sdp-parameters-2	@date=2017-02-16	type=proto	name=TCP/WS/BFCP	xref=rfc:rfc8857\n" +
        "sdp-parameters-2	@date=2017-02-16	type=proto	name=TCP/WSS/BFCP	xref=rfc:rfc8857\n" +
        "sdp-parameters-2	@date=2017-03-24	type=proto	name=UDP/DTLS/SCTP	xref=rfc:rfc8841\n" +
        "sdp-parameters-2	@date=2017-03-24	type=proto	name=TCP/DTLS/SCTP	xref=rfc:rfc8841\n" +
        "sdp-parameters-2	@date=2018-12-28	type=proto	name=TCP/DTLS/BFCP	xref=rfc:rfc8856\n" +
        "sdp-parameters-2	@date=2018-12-28	type=proto	name=UDP/BFCP	xref=rfc:rfc8856\n" +
        "sdp-parameters-2	@date=2018-12-28	type=proto	name=UDP/TLS/BFCP	xref=rfc:rfc8856\n" +
        "sdp-parameters-3	type=bwtype	name=CT	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-parameters-3	type=bwtype	name=AS	mux=SUM	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-parameters-3	type=bwtype	name=RS	mux=SUM	xref=rfc:rfc3556	xref=rfc:rfc8859\n" +
        "sdp-parameters-3	type=bwtype	name=RR	mux=SUM	xref=rfc:rfc3556	xref=rfc:rfc8859\n" +
        "sdp-parameters-3	type=bwtype	name=TIAS	mux=SPECIAL	xref=rfc:rfc3890	xref=rfc:rfc8859\n" +
        "sdp-parameters-4	type=nettype	name=IN	usable=IP4, IP6	xref=rfc:rfc8866\n" +
        "sdp-parameters-4	@date=2014-03-03	type=nettype	name=TN	usable=RFC2543	xref=rfc:rfc2848\n" +
        "sdp-parameters-4	@date=2014-03-03	type=nettype	name=ATM	usable=NSAP, GWID, E164	xref=rfc:rfc3108\n" +
        "sdp-parameters-4	type=nettype	name=PSTN	usable=E164	xref=rfc:rfc7195\n" +
        "sdp-parameters-5	type=addrtype	name=IP4	xref=rfc:rfc8866\n" +
        "sdp-parameters-5	type=addrtype	name=IP6	xref=rfc:rfc8866\n" +
        "sdp-parameters-5	@date=2014-03-03	type=addrtype	name=NSAP	xref=rfc:rfc3108\n" +
        "sdp-parameters-5	@date=2014-03-03	type=addrtype	name=GWID	xref=rfc:rfc3108\n" +
        "sdp-parameters-5	type=addrtype	name=E164	xref=rfc:rfc7195	xref=rfc:rfc3108\n" +
        "sdp-parameters-6	type=enckey	name=clear	xref=rfc:rfc8866\n" +
        "sdp-parameters-6	type=enckey	name=base64	xref=rfc:rfc8866\n" +
        "sdp-parameters-6	type=enckey	name=uri	xref=rfc:rfc8866\n" +
        "sdp-parameters-6	type=enckey	name=prompt	xref=rfc:rfc8866\n" +
        "sdp-att-field	@date=2017-01-05	@updated=2026-01-15	type=attribute	name=3ge2ae	usage=media	mux=IDENTICAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim\n" +
        "sdp-att-field	@date=2013-09-11	type=attribute	name=3gpp_MaxRecvSDUSize	usage=session, media	mux=NORMAL	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:_3GPP_Specifications_Manager\n" +
        "sdp-att-field	@date=2019-11-06	type=attribute	name=3gpp_mtsi_app_adapt	usage=media	mux=IDENTICAL-PER-PT	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:Ozgur_Oyman\n" +
        "sdp-att-field	@date=2013-09-11	type=attribute	name=3gpp_sync_info	usage=session, media	mux=NORMAL	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:_3GPP_Specifications_Manager\n" +
        "sdp-att-field	type=attribute	name=3GPP-Adaptation-Support	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=3GPP-Asset-Information	usage=session, media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=3GPP-Integrity-Key	usage=session	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=3GPP-QoE-Metrics	usage=session, media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=3GPP-SDP-Auth	usage=session	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=3GPP-SRTP-Config	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=3gpp-videopostdecbufsize	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	@updated=2026-01-15	type=attribute	name=3gpp.iut.replication	usage=session, media	mux=TBD	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24337.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=aal2CPS	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=aal2CPSSDUrate	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=aal2sscs3661assured	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=aal2sscs3661unassured	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=aal2sscs3662	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=aal5sscop	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=aalApp	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=aalType	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=abrParms	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=abrSetup	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=acap	usage=session, media	mux=INHERIT	xref=rfc:rfc5939	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=accept-types	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc4975	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=accept-wrapped-types	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc4975	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=acfg	usage=media	mux=SPECIAL	xref=rfc:rfc5939	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=alt	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=alt-default-id	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=alt-group	usage=session	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=altc	usage=media	mux=TRANSPORT	xref=rfc:rfc6947	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2019-11-06	type=attribute	name=anbr	usage=media	mux=IDENTICAL	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:Ozgur_Oyman\n" +
        "sdp-att-field	type=attribute	name=anycast	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=atmmap	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=atmQOSparms	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=atmTrfcDesc	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2010-11-24	type=attribute	name=bc_program	usage=session	mux=NORMAL	xref=person:Sonia_Compans	xref=uri:http://docbox.etsi.org/tispan/Open/NGN_LATEST_DRAFTS/RELEASE3/03204-NGN-R3v342.pdf	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2010-11-24	type=attribute	name=bc_service	usage=session	mux=NORMAL	xref=person:Sonia_Compans	xref=uri:http://docbox.etsi.org/tispan/Open/NGN_LATEST_DRAFTS/RELEASE3/03204-NGN-R3v342.pdf	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2010-11-24	type=attribute	name=bc_service_package	usage=session	mux=NORMAL	xref=person:Sonia_Compans	xref=uri:http://docbox.etsi.org/tispan/Open/NGN_LATEST_DRAFTS/RELEASE3/03204-NGN-R3v342.pdf	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=bcap	usage=session, media	mux=INHERIT	xref=rfc:rfc7006	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=bcastversion	usage=session	mux=NORMAL	xref=rfc:rfc5159	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=bcob	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=bearerSigIE	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=bearerType	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2018-12-28	type=attribute	name=bfcpver	usage=media	mux=TBD	xref=rfc:rfc8856\n" +
        "sdp-att-field	@date=2018-06-18	type=attribute	name=bundle-only	usage=media	mux=NORMAL	xref=rfc:rfc9143\n" +
        "sdp-att-field	@date=2019-11-06	type=attribute	name=bw-info	usage=media	mux=IDENTICAL-PER-PT	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:Ozgur_Oyman\n" +
        "sdp-att-field	type=attribute	name=cache	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2014-07-11	type=attribute	name=calgextmap	usage=session, media	mux=NORMAL	xref=rfc:rfc7266	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=candidate	usage=media	mux=TRANSPORT	xref=rfc:rfc8839	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=capability	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cat	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cbrRate	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ccap	usage=session, media	mux=IDENTICAL	xref=rfc:rfc7006	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2017-06-16	type=attribute	name=ccc_list	usage=session	mux=NORMAL	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:_3GPP_Specifications_Manager\n" +
        "sdp-att-field	type=attribute	name=cdsc	usage=session, media	mux=NORMAL	xref=rfc:rfc3407	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cfw-id	usage=media	mux=NORMAL	xref=rfc:rfc6230	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=chain	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=channel	usage=media	mux=NORMAL	xref=rfc:rfc6787	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=charset	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=charset:iso8895-1	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=chatroom	usage=media	mux=TBD	xref=rfc:rfc7701	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-03-20	type=attribute	name=clir	usage=session, media	mux=TBD	xref=rfc:rfc2848	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=clkrec	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cmid	usage=media	mux=NORMAL	xref=rfc:rfc6787	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cname	usage=source	mux=NORMAL	xref=rfc:rfc5576	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=codecconfig	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=conf	usage=media	mux=CAUTION	xref=rfc:rfc3312	xref=rfc:rfc8859\n" +
        "sdp-att-field	@updated=2020-05-18	type=attribute	name=confid	usage=media	mux=TBD	xref=rfc:rfc8856	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=connection	usage=session, media	mux=TRANSPORT	xref=rfc:rfc4145	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=content	usage=media	mux=NORMAL	xref=rfc:rfc4796	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=content-desc	usage=session, media	mux=TBD	xref=draft:RFC-mehta-rmt-flute-sdp-05	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2014-02-21	type=attribute	name=control	usage=session, media	mux=CAUTION	xref=rfc:rfc2326	xref=rfc:rfc7826	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cpar	usage=session, media	mux=INHERIT	xref=rfc:rfc3407	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cparmax	usage=session, media	mux=SPECIAL	xref=rfc:rfc3407	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cparmin	usage=session, media	mux=SPECIAL	xref=rfc:rfc3407	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cpsSDUsize	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=creq	usage=session, media	mux=NORMAL	xref=rfc:rfc5939	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2022-09-30	type=attribute	name=cryptex	usage=session, media	mux=TRANSPORT	xref=rfc:rfc9335\n" +
        "sdp-att-field	type=attribute	name=crypto	usage=media	mux=TRANSPORT	xref=rfc:rfc4568	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=cs-correlation	usage=media	mux=TBD	xref=rfc:rfc7195	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=csup	usage=session, media	mux=NORMAL	xref=rfc:rfc5939	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=curr	usage=media	mux=CAUTION	xref=rfc:rfc3312	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=dccp-port	usage=media	mux=CAUTION	xref=rfc:rfc6773	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=dccp-service-code	usage=media	mux=CAUTION	xref=rfc:rfc5762	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2019-05-24	type=attribute	name=dcmap	usage=media	mux=SPECIAL	xref=rfc:rfc8864\n" +
        "sdp-att-field	@date=2019-05-24	type=attribute	name=dcsa	usage=media	mux=SPECIAL	xref=rfc:rfc8864\n" +
        "sdp-att-field	type=attribute	name=depend	usage=media	mux=IDENTICAL-PER-PT	xref=rfc:rfc5583	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=des	usage=media	mux=CAUTION	xref=rfc:rfc3312	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=dsel	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2014-01-23	type=attribute	name=duplication-delay	usage=session, media	mux=NORMAL	xref=rfc:rfc7197	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2016-11-17	type=attribute	name=ebuacip	usage=media	mux=TBD	xref=uri:https://tech.ebu.ch/publications/tech3368	xref=person:Felix_Poulin	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ecan	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ecn-capable-rtp	usage=media	mux=IDENTICAL	xref=rfc:rfc6679	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=eecid	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2018-07-05	type=attribute	name=end-of-candidates	usage=session, media	mux=IDENTICAL	xref=rfc:rfc8840\n" +
        "sdp-att-field	@date=2013-09-12	type=attribute	name=etag	usage=session, media	mux=CAUTION	xref=rfc:rfc2326	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=extmap	usage=session, media	mux=SPECIAL	xref=rfc:rfc8285	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2017-08-08	type=attribute	name=extmap-allow-mixed	usage=session, media	mux=IDENTICAL	xref=rfc:rfc8285\n" +
        "sdp-att-field	type=attribute	name=fec	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=FEC	usage=media	mux=NORMAL	xref=draft:RFC-mehta-rmt-flute-sdp-05	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=FEC-declaration	usage=session, media	mux=TBD	xref=draft:RFC-mehta-rmt-flute-sdp-05	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=FEC-OTI-extension	usage=session, media	mux=TBD	xref=draft:RFC-mehta-rmt-flute-sdp-05	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=fec-repair-flow	usage=media	mux=SPECIAL	xref=rfc:rfc6364	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=fec-source-flow	usage=media	mux=SPECIAL	xref=rfc:rfc6364	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=file-date	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc5547	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=file-disposition	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc5547	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=file-icon	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc5547	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=file-range	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc5547	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=file-selector	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc5547	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=file-transfer-id	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc5547	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=fingerprint	usage=session, media	mux=TRANSPORT	xref=rfc:rfc8122	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=floorctrl	usage=media	mux=TBD	xref=rfc:rfc8856	xref=rfc:rfc8859\n" +
        "sdp-att-field	@updated=2020-05-18	type=attribute	name=floorid	usage=media	mux=TBD	xref=rfc:rfc8856	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=flute-ch	usage=session, media	mux=TBD	xref=draft:RFC-mehta-rmt-flute-sdp-05	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=flute-tsi	usage=session, media	mux=TBD	xref=draft:RFC-mehta-rmt-flute-sdp-05	xref=rfc:rfc8859\n" +
        "sdp-att-field	@updated=2020-04-14	type=attribute	name=fmtp	usage=media, source, dcsa (t140)	mux=IDENTICAL-PER-PT	xref=rfc:rfc5576	xref=rfc:rfc8866	xref=rfc:rfc8859	xref=rfc:rfc8865\n" +
        "sdp-att-field	type=attribute	name=framerate	usage=media	mux=IDENTICAL-PER-PT	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=framesize	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=fsel	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=gc	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2019-07-30	type=attribute	name=gpmd	usage=media	mux=TBD	xref=uri:https://www.itu.int/rec/T-REC-V.150.1	xref=uri:https://www.itu.int/rec/T-REC-V.152	xref=person:Simao_Campos-Neto\n" +
        "sdp-att-field	type=attribute	name=group	usage=session	mux=NORMAL	xref=rfc:rfc5888	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=h248item	usage=session, media	mux=SPECIAL	xref=uri:http://www.itu.int/rec/T-REC-H.248.15	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2018-02-22	@updated=2020-04-14	type=attribute	name=hlang-recv	usage=media, dcsa (t140)	mux=NORMAL	xref=rfc:rfc8373	xref=rfc:rfc8865\n" +
        "sdp-att-field	@date=2018-02-22	@updated=2020-04-14	type=attribute	name=hlang-send	usage=media, dcsa (t140)	mux=NORMAL	xref=rfc:rfc8373	xref=rfc:rfc8865\n" +
        "sdp-att-field	type=attribute	name=icap	usage=session, media	mux=NORMAL	xref=rfc:rfc7006	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ice-lite	usage=session	mux=NORMAL	xref=rfc:rfc8839	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ice-mismatch	usage=media	mux=NORMAL	xref=rfc:rfc8839	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ice-options	usage=session	mux=NORMAL	xref=rfc:rfc8839	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2019-08-21	type=attribute	name=ice-pacing	usage=session	mux=NORMAL	xref=rfc:rfc8839\n" +
        "sdp-att-field	type=attribute	name=ice-pwd	usage=session, media	mux=TRANSPORT	xref=rfc:rfc8839	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ice-ufrag	usage=session, media	mux=TRANSPORT	xref=rfc:rfc8839	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2019-07-25	type=attribute	name=identity	usage=session	mux=NORMAL	xref=rfc:rfc8827\n" +
        "sdp-att-field	type=attribute	name=ike-setup	usage=session, media	mux=IDENTICAL	xref=rfc:rfc6193	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=imageattr	usage=media	mux=IDENTICAL-PER-PT	xref=rfc:rfc6236	xref=rfc:rfc8859\n" +
        "sdp-att-field	@updated=2020-04-14	type=attribute	name=inactive	usage=session, media, dcsa (t140), dcsa (msrp)	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859	xref=rfc:rfc8865	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=ipbcp	usage=session	mux=SPECIAL	xref=text:ITU-T Recommendation Q.1970, \"BICC IP bearer control protocol\" (07/2001)	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=isup_usi	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=key-mgmt	usage=session, media	mux=IDENTICAL	xref=rfc:rfc4567	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=keywds	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=label	usage=media	mux=NORMAL	xref=rfc:rfc4574	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=lang	usage=session, media	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=lcfg	usage=media	mux=SPECIAL	xref=rfc:rfc6871	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=lij	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=loopback	usage=media	mux=NORMAL	xref=rfc:rfc6849	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=loopback-mirror	usage=media	mux=NORMAL	xref=rfc:rfc6849	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=loopback-source	usage=media	mux=NORMAL	xref=rfc:rfc6849	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2017-03-24	type=attribute	name=max-message-size	usage=media	mux=CAUTION	xref=rfc:rfc8841\n" +
        "sdp-att-field	type=attribute	name=max-size	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc4975	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	@date=2017-04-11	type=attribute	name=maxmptime	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-V.152-201009-I	xref=person:Simao_Campos-Neto\n" +
        "sdp-att-field	type=attribute	name=maxprate	usage=session, media	mux=SPECIAL	xref=rfc:rfc3890	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=maxptime	usage=media	mux=IDENTICAL-PER-PT	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=mbms-flowid	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=mbms-mode	usage=session, media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=mbms-repair	usage=session, media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2014-04-07	type=attribute	name=mediaclk	usage=session, media, source	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=mfcap	usage=session, media	mux=IDENTICAL-PER-PT	xref=rfc:rfc6871	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=mid	usage=media	mux=NORMAL	xref=rfc:rfc5888	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=mscap	usage=session, media	mux=INHERIT	xref=rfc:rfc6871	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2016-07-13	@updated=2020-04-29	type=attribute	name=msid	usage=media	mux=NORMAL	xref=rfc:rfc8830\n" +
        "sdp-att-field	type=attribute	name=msrp-cema	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc6714	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	@date=2014-02-21	type=attribute	name=mtag	usage=session, media	mux=CAUTION	xref=rfc:rfc7826	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=multicast-rtcp	usage=session, media	mux=IDENTICAL	xref=rfc:rfc6128	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=omcap	usage=session, media	mux=NORMAL	xref=rfc:rfc6871	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=omr-codecs	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=omr-m-att	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=omr-m-bw	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=omr-m-cksum	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=omr-s-att	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=omr-s-bw	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=omr-s-cksum	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=onewaySel	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=orient	usage=media	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=orient:landscape	usage=media	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=orient:portrait	usage=media	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=orient:seascape	usage=media	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=path	usage=media, dcsa (msrp)	mux=TBD	xref=rfc:rfc4975	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=pcfg	usage=media	mux=SPECIAL	xref=rfc:rfc5939	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-03-20	type=attribute	name=phone-context	usage=session, media	mux=TBD	xref=rfc:rfc2848	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2017-04-11	type=attribute	name=pmft	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-V.152-201009-I	xref=person:Simao_Campos-Neto\n" +
        "sdp-att-field	type=attribute	name=portmapping-req	usage=media	mux=CAUTION	xref=rfc:rfc6284	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-09-16	type=attribute	name=predefined_ROI	usage=media	mux=IDENTICAL-PER-PT	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:_3GPP_Specifications_Manager\n" +
        "sdp-att-field	type=attribute	name=previous-ssrc	usage=source	mux=NORMAL	xref=rfc:rfc5576	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=profileDesc	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=prtfl	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2010-11-24	type=attribute	name=PSCid	usage=session	mux=NORMAL	xref=person:Sonia_Compans	xref=uri:http://docbox.etsi.org/tispan/Open/NGN_LATEST_DRAFTS/RELEASE3/03204-NGN-R3v342.pdf	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=psk-fingerprint	usage=session, media	mux=IDENTICAL	xref=rfc:rfc6193	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ptime	usage=media	mux=IDENTICAL-PER-PT	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-03-20	type=attribute	name=Q763-INN	usage=session, media	mux=TBD	xref=rfc:rfc2848	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-03-20	type=attribute	name=Q763-nature	usage=session, media	mux=TBD	xref=rfc:rfc2848	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-03-20	type=attribute	name=Q763-plan	usage=session, media	mux=TBD	xref=rfc:rfc2848	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=qos-mech-recv	usage=session, media	mux=TRANSPORT	xref=rfc:rfc5432	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=qos-mech-send	usage=session, media	mux=TRANSPORT	xref=rfc:rfc5432	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=qosClass	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=quality	usage=media	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rams-updates	usage=media	mux=CAUTION	xref=rfc:rfc6285	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2014-02-21	type=attribute	name=range	usage=session, media	mux=CAUTION	xref=rfc:rfc2326	xref=rfc:rfc7826	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-10-07	type=attribute	name=record	usage=session, media	mux=TBD	xref=rfc:rfc7866	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-10-07	type=attribute	name=recordpref	usage=session, media	mux=TBD	xref=rfc:rfc7866	xref=rfc:rfc8859\n" +
        "sdp-att-field	@updated=2020-04-20	type=attribute	name=recvonly	usage=session, media, dcsa (t140), dcsa (msrp)	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859	xref=rfc:rfc8865	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=remote-candidates	usage=media	mux=TRANSPORT	xref=rfc:rfc8839	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=repair-window	usage=media	mux=SPECIAL	xref=rfc:rfc6364	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2015-03-20	type=attribute	name=require	usage=session, media	mux=TBD	xref=rfc:rfc2848	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=resource	usage=media	mux=NORMAL	xref=rfc:rfc6787	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2018-05-17	type=attribute	name=rid	usage=media	mux=SPECIAL	xref=rfc:rfc8851\n" +
        "sdp-att-field	type=attribute	name=rmcap	usage=session, media	mux=IDENTICAL-PER-PT	xref=rfc:rfc6871	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtcp	usage=media	mux=TRANSPORT	xref=rfc:rfc3605	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtcp-fb	usage=media	mux=IDENTICAL-PER-PT	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2013-09-11	type=attribute	name=rtcp-idms	usage=media	mux=NORMAL	xref=rfc:rfc7272	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtcp-mux	usage=media	mux=IDENTICAL	xref=rfc:rfc5761	xref=rfc:rfc8035	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2016-08-10	@updated=2020-04-29	type=attribute	name=rtcp-mux-only	usage=media	mux=IDENTICAL	xref=rfc:rfc8858\n" +
        "sdp-att-field	@date=2016-03-18	type=attribute	name=rtcp-rgrp	usage=session, media	mux=IDENTICAL	xref=rfc:rfc8861	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtcp-rsize	usage=media	mux=IDENTICAL	xref=rfc:rfc5506	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtcp-unicast	usage=media	mux=IDENTICAL	xref=rfc:rfc5760	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtcp-xr	usage=session, media	mux=NORMAL	xref=rfc:rfc3611	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtpmap	usage=media	mux=IDENTICAL-PER-PT	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtpred1	usage=media	mux=CAUTION	xref=note:2	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=rtpred2	usage=media	mux=CAUTION	xref=note:2	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2014-08-18	type=attribute	name=rtsp-ice-d-m	usage=session	mux=TBD	xref=rfc:rfc7825	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2021-05-26	type=attribute	name=rtt-mixer	usage=media	mux=NORMAL	xref=rfc:rfc9071\n" +
        "sdp-att-field	type=attribute	name=sbc	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2017-03-24	type=attribute	name=sctp-port	usage=media	mux=CAUTION	xref=rfc:rfc8841\n" +
        "sdp-att-field	type=attribute	name=sdplang	usage=session, media	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=secondary-realm	usage=media	mux=TRANSPORT	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	@updated=2020-04-14	type=attribute	name=sendonly	usage=session, media, dcsa (t140), dcsa (msrp)	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859	xref=rfc:rfc8865	xref=rfc:rfc8873\n" +
        "sdp-att-field	@updated=2020-04-14	type=attribute	name=sendrecv	usage=session, media, dcsa (t140), dcsa (msrp)	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859	xref=rfc:rfc8865	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=sescap	usage=session	mux=CAUTION	xref=rfc:rfc6871	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=setup	usage=session, media, dcsa (msrp)	mux=TRANSPORT	xref=rfc:rfc4145	xref=rfc:rfc8859	xref=rfc:rfc8873\n" +
        "sdp-att-field	type=attribute	name=silenceSupp	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2018-07-10	type=attribute	name=simulcast	usage=media	mux=NORMAL	xref=rfc:rfc8853\n" +
        "sdp-att-field	type=attribute	name=source-filter	usage=session, media	mux=IDENTICAL	xref=rfc:rfc4570	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=sqn	usage=session, media	mux=NORMAL	xref=rfc:rfc3407	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=SRTPAuthentication	usage=media	mux=TBD	xref=rfc:rfc5159	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=SRTPROCTxRate	usage=media	mux=TBD	xref=rfc:rfc5159	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ssrc	usage=media	mux=NORMAL	xref=rfc:rfc5576	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=ssrc-group	usage=media	mux=NORMAL	xref=rfc:rfc5576	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=stc	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=stkmstream	usage=session, media	mux=NORMAL	xref=rfc:rfc5159	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=structure	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2016-10-11	type=attribute	name=suspended	usage=session, media	mux=TBD	xref=text:NENA STA-010.2-2016	xref=person:Brian_Rosen	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38FaxFillBitRemoval	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38FaxMaxBuffer	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38FaxMaxDatagram	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2013-06-06	type=attribute	name=T38FaxMaxIFP	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=person:Simao_Campos-Neto	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38FaxRateManagement	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38FaxTranscodingJBIG	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38FaxTranscodingMMR	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38FaxUdpEC	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2013-06-06	type=attribute	name=T38FaxUdpECDepth	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=person:Simao_Campos-Neto	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2013-06-06	type=attribute	name=T38FaxUdpFECMaxSpan	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=person:Simao_Campos-Neto	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38FaxVersion	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38MaxBitRate	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2013-06-06	type=attribute	name=T38ModemType	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-T.38/en	xref=person:Simao_Campos-Neto	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=T38VendorInfo	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/recommendation.asp?type%3Dfolders&lang%3De&parent%3DT-REC-T.38	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=tcap	usage=session, media	mux=INHERIT	xref=rfc:rfc5939	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2017-11-03	type=attribute	name=tls-id	usage=media	mux=IDENTICAL	xref=rfc:rfc8842\n" +
        "sdp-att-field	type=attribute	name=tool	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2023-03-14	type=attribute	name=tra-att	usage=session, media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24371.htm\n" +
        "sdp-att-field	@date=2023-03-14	type=attribute	name=tra-bw	usage=session, media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24371.htm\n" +
        "sdp-att-field	@date=2023-03-14	type=attribute	name=tra-contact	usage=session, media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24371.htm\n" +
        "sdp-att-field	@date=2023-03-14	type=attribute	name=tra-m-line	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24371.htm\n" +
        "sdp-att-field	@date=2023-03-14	type=attribute	name=tra-media-line-number	usage=session	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24371.htm\n" +
        "sdp-att-field	@date=2023-03-14	type=attribute	name=tra-SCTP-association	usage=media	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24371.htm\n" +
        "sdp-att-field	@date=2014-04-07	type=attribute	name=ts-refclk	usage=session, media, source	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=type	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=type:H332	usage=session	mux=NORMAL	xref=uri:http://www.itu.int/rec/T-REC-H.332-199809-I/en	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=type:broadcast	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=type:meeting	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=type:moderated	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=type:test	usage=session	mux=NORMAL	xref=rfc:rfc8866	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=uiLayer1_Prot	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=upcc	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@updated=2020-05-18	type=attribute	name=userid	usage=media	mux=TBD	xref=rfc:rfc8856	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2026-03-05	type=attribute	name=v3cfmtp	usage=session, media	mux=NORMAL	xref=draft:RFC-ietf-avtcore-rtp-v3c-17\n" +
        "sdp-att-field	@date=2017-04-11	type=attribute	name=vbd (OBSOLETE)	usage=media	mux=TBD	xref=uri:http://www.itu.int/rec/T-REC-V.152-201009-I	xref=person:Simao_Campos-Neto\n" +
        "sdp-att-field	@date=2012-06-21	@updated=2026-01-15	type=attribute	name=visited-realm	usage=media	mux=TRANSPORT	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2019-07-30	type=attribute	name=vndpar	usage=session, media	mux=TBD	xref=uri:https://www.itu.int/rec/T-REC-V.150.1	xref=uri:https://www.itu.int/rec/T-REC-V.152	xref=person:Simao_Campos-Neto\n" +
        "sdp-att-field	type=attribute	name=vsel	usage=media	mux=CAUTION	xref=rfc:rfc3108	xref=rfc:rfc8859\n" +
        "sdp-att-field	@date=2017-02-13	type=attribute	name=websocket-uri	usage=media	mux=CAUTION	xref=rfc:rfc8124\n" +
        "sdp-att-field	type=attribute	name=X-decbyterate	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=X-initpostdecbufperiod	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=X-initpredecbufperiod	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=X-predecbufsize	usage=media	mux=CAUTION	xref=rfc:rfc6064	xref=rfc:rfc8859\n" +
        "sdp-att-field	type=attribute	name=zrtp-hash	usage=media	mux=TRANSPORT	xref=rfc:rfc6189	xref=rfc:rfc8859\n" +
        "sdp-parameters-12	type=content	name=slides	description=Presentation slides	mux=NORMAL	xref=rfc:rfc4796	xref=rfc:rfc8859\n" +
        "sdp-parameters-12	type=content	name=speaker	description=Image from the speaker	mux=NORMAL	xref=rfc:rfc4796	xref=rfc:rfc8859\n" +
        "sdp-parameters-12	type=content	name=sl	description=Sign language	mux=NORMAL	xref=rfc:rfc4796	xref=rfc:rfc8859\n" +
        "sdp-parameters-12	type=content	name=main	description=Main media stream	mux=NORMAL	xref=rfc:rfc4796	xref=rfc:rfc8859\n" +
        "sdp-parameters-12	type=content	name=alt	description=Alternative media stream	mux=NORMAL	xref=rfc:rfc4796	xref=rfc:rfc8859\n" +
        "sdp-parameters-12	@date=2018-01-16	@updated=2026-01-15	type=content	name=g.3gpp.cat	description=Stream associated with the 3GPP Customized Alerting Ringtone (CAT) service	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24182.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-parameters-12	@date=2011-12-07	@updated=2026-01-15	type=content	name=g.3gpp.crs	description=Stream associated with the 3GPP Customized Ringing Signal (CRS) service	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/specs/html-info/24183.htm	xref=person:Dongwook_Kim	xref=rfc:rfc8859\n" +
        "sdp-parameters-12	@date=2023-04-04	type=content	name=g.3gpp.announcement-no-confirmation	description=Stream associated with video announcement service without user confirmation	mux=NORMAL	xref=uri:http://www.3gpp.org/ftp/Specs/html-info/24229.htm\n" +
        "sdp-parameters-13	description=Lip Synchronization	name=LS	mux=NORMAL	xref=rfc:rfc5888	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	description=Flow Identification	name=FID	mux=NORMAL	xref=rfc:rfc5888	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	description=Single Reservation flow	name=SRF	mux=NORMAL	xref=rfc:rfc3524	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	description=Alternative Network Address Types	name=ANAT	mux=CAUTION	xref=rfc:rfc4091	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	description=Forward Error Correction (Deprecated)	name=FEC	mux=NORMAL	xref=rfc:rfc5956	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	description=Forward Error Correction FR	name=FEC-FR	mux=NORMAL	xref=rfc:rfc5956	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	description=Composite Session	name=CS	mux=NORMAL	xref=draft:RFC-mehta-rmt-flute-sdp-05	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	description=Decoding Dependency	name=DDP	mux=NORMAL	xref=rfc:rfc5583	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	@date=2013-12-04	description=Duplication	name=DUP	mux=NORMAL	xref=rfc:rfc7104	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	@date=2016-08-09	description=Splice	name=SPLICE	mux=TBD	xref=rfc:rfc8286	xref=rfc:rfc8859\n" +
        "sdp-parameters-13	@date=2018-06-18	description=Media bundling	name=BUNDLE	mux=NORMAL	xref=rfc:rfc9143\n" +
        "sdp-parameters-13	@date=2019-12-19	@updated=2020-09-04	description=CLUE-controlled \"m%3D\" line	name=CLUE	mux=NORMAL	xref=rfc:rfc8848\n" +
        "sdp-parameters-13	@date=2026-03-05	description=V3C grouping	name=V3C	mux=NORMAL	xref=draft:RFC-ietf-avtcore-rtp-v3c-17\n" +
        "sdp-parameters-14	name=ack	description=Positive acknowledgement	mux=IDENTICAL-PER-PT	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-parameters-14	name=app	description=Application-defined parameter	mux=SPECIAL	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-parameters-14	name=ccm	description=Codec Control Commands and Indications	mux=IDENTICAL-PER-PT	xref=rfc:rfc5104	xref=rfc:rfc8859\n" +
        "sdp-parameters-14	name=nack	description=Negative Acknowledgement	mux=IDENTICAL-PER-PT	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-parameters-14	name=trr-int	description=Minimal receiver report interval	mux=IDENTICAL-PER-PT	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-parameters-14	@date=2015-10-19	name=3gpp-roi-arbitrary	description=Video region-of-interest (ROI) arbitrarily selected by the endpoint	mux=IDENTICAL-PER-PT	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:Ozgur_Oyman\n" +
        "sdp-parameters-14	@date=2015-10-19	name=3gpp-roi-predefined	description=Video region-of-interest (ROI) pre-defined by the sender and selected by the endpoint	mux=IDENTICAL-PER-PT	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:Ozgur_Oyman\n" +
        "sdp-parameters-14	@date=2018-10-29	name=3gpp-delay-budget	description=Available or requested delay budget specified in milliseconds	mux=IDENTICAL-PER-PT	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:Ozgur_Oyman\n" +
        "sdp-parameters-14	@date=2023-06-06	name=3gpp-viewport	description=Viewport feedback for viewport-dependent 360-degree video	mux=IDENTICAL-PER-PT	xref=uri:https://www.3gpp.org/ftp/Specs/archive/26_series/26.114\n" +
        "sdp-parameters-15	name=sli	description=Slice Loss Indication	usage=nack	mux=IDENTICAL-PER-PT	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-parameters-15	name=pli	description=Picture Loss Indication	usage=nack	mux=IDENTICAL-PER-PT	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-parameters-15	name=rpsi	description=Reference Picture Selection Indication	usage=ack, nack	mux=IDENTICAL-PER-PT	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-parameters-15	name=app	description=Application layer feedback	usage=ack, nack	mux=SPECIAL	xref=rfc:rfc4585	xref=rfc:rfc8859\n" +
        "sdp-parameters-15	name=rai	description=Rapid Acquisition Indication	usage=nack	mux=IDENTICAL-PER-PT	xref=rfc:rfc6285	xref=rfc:rfc8859\n" +
        "sdp-parameters-15	name=tllei	description=Transport-Layer Third-Party Loss Early Indication	usage=nack	mux=IDENTICAL-PER-PT	xref=rfc:rfc6642	xref=rfc:rfc8859\n" +
        "sdp-parameters-15	name=pslei	description=Payload-Specific Third-Party Loss Early Indication	usage=nack	mux=IDENTICAL-PER-PT	xref=rfc:rfc6642	xref=rfc:rfc8859\n" +
        "sdp-parameters-15	name=ecn	description=Explicit Congestion Notification	usage=nack	mux=IDENTICAL	xref=rfc:rfc6679	xref=rfc:rfc8859\n" +
        "sdp-parameters-15	@date=2020-11-19	name=ccfb	description=Congestion Control Feedback	usage=ack	mux=IDENTICAL-PER-PT	xref=rfc:rfc8888\n" +
        "sdp-parameters-16	name=lay	description=Layered decoding dependency	mux=IDENTICAL-PER-PT	xref=rfc:rfc5583	xref=rfc:rfc8859\n" +
        "sdp-parameters-16	name=mdc	description=Multi descriptive decoding dependency	mux=IDENTICAL-PER-PT	xref=rfc:rfc5583	xref=rfc:rfc8859\n" +
        "cs-correlation-values	value=callerid	description=Caller ID	mux=TBD	xref=rfc:rfc7195	xref=rfc:rfc8859\n" +
        "cs-correlation-values	value=uuie	description=User-User Information Element	mux=TBD	xref=rfc:rfc7195	xref=rfc:rfc8859\n" +
        "cs-correlation-values	value=dtmf	description=Dual-tone Multi-Frequency	mux=TBD	xref=rfc:rfc7195	xref=rfc:rfc8859\n" +
        "cs-correlation-values	@date=2014-03-03	value=external	description=External	mux=TBD	xref=rfc:rfc7195	xref=rfc:rfc8859\n" +
        "sdp-parameters-17	name=FID	description=Flow Identification	mux=NORMAL	xref=rfc:rfc5576	xref=rfc:rfc8859\n" +
        "sdp-parameters-17	name=FEC	description=Forward Error Correction	mux=NORMAL	xref=rfc:rfc5576	xref=rfc:rfc8859\n" +
        "sdp-parameters-17	name=FEC-FR	description=Forward Error Correction FR	mux=NORMAL	xref=rfc:rfc5956	xref=rfc:rfc8859\n" +
        "sdp-parameters-17	@date=2013-12-04	name=DUP	description=Duplication	mux=NORMAL	xref=rfc:rfc7104	xref=rfc:rfc8859\n" +
        "sdp-parameters-18	name=mikey	description=Multimedia Internet KEYing	mux=IDENTICAL	xref=rfc:rfc4567	xref=rfc:rfc8859\n" +
        "sdp-parameters-19	name=fir	description=Full Intra Request Command	usage=ccm	mux=IDENTICAL-PER-PT	xref=rfc:rfc5104	xref=rfc:rfc8859\n" +
        "sdp-parameters-19	name=tmmbr	description=Temporary Maximum Media Stream Bit Rate	usage=ccm	mux=IDENTICAL-PER-PT	xref=rfc:rfc5104	xref=rfc:rfc8859\n" +
        "sdp-parameters-19	name=tstr	description=Temporal-Spatial Trade-off	usage=ccm	mux=IDENTICAL-PER-PT	xref=rfc:rfc5104	xref=rfc:rfc8859\n" +
        "sdp-parameters-19	name=vbcm	description=H.271 video back channel messages	usage=ccm	mux=IDENTICAL-PER-PT	xref=rfc:rfc5104	xref=rfc:rfc8859\n" +
        "sdp-parameters-19	@date=2015-09-30	name=pause	description=Media Pause/Resume	usage=ccm	mux=TBD	xref=rfc:rfc7728	xref=rfc:rfc8859\n" +
        "sdp-parameters-19	@date=2017-07-10	name=lrr	description=Layer Refresh Request Command	usage=ccm	mux=IDENTICAL-PER-PT	xref=rfc:rfc9627\n" +
        "sdp-parameters-20	name=rsvp	mux=TRANSPORT	xref=rfc:rfc5432	xref=rfc:rfc8859\n" +
        "sdp-parameters-20	name=nsis	mux=TRANSPORT	xref=rfc:rfc5432	xref=rfc:rfc8859\n" +
        "sdp-parameters-21	name=cap-v0	mux=NORMAL	xref=rfc:rfc5939	xref=rfc:rfc8859\n" +
        "sdp-parameters-21	name=med-v0	mux=NORMAL	xref=rfc:rfc6871	xref=rfc:rfc8859\n" +
        "sdp-parameters-21	name=bcap-v0	mux=NORMAL	xref=rfc:rfc7006	xref=rfc:rfc8859\n" +
        "sdp-parameters-21	name=ccap-v0	mux=NORMAL	xref=rfc:rfc7006	xref=rfc:rfc8859\n" +
        "sdp-parameters-21	name=icap-v0	mux=NORMAL	xref=rfc:rfc7006	xref=rfc:rfc8859\n" +
        "sdp-parameters-22	name=a	description=Attribute Configuration	potential=	actual=	latent=\n" +
        "sdp-parameters-22	name=b	description=Bandwidth	potential=	actual=	latent=\n" +
        "sdp-parameters-22	name=c	description=Connection Data	potential=	actual=	latent=\n" +
        "sdp-parameters-22	name=i	description=Title	potential=	actual=	latent=\n" +
        "sdp-parameters-22	name=m	description=Media Configuration	potential=	actual=	latent=\n" +
        "sdp-parameters-22	name=mt	description=Media Type	potential=	actual=	latent=\n" +
        "sdp-parameters-22	name=pt	description=Payload Type Number Mapping	potential=	actual=	latent=\n" +
        "sdp-parameters-22	name=t	description=Transport Protocol Configuration	potential=	actual=	latent=\n" +
        "timestamp-ref-clock-source	value=ntp	description=Network Time Protocol	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "timestamp-ref-clock-source	value=ptp	description=Precision Time Protocol	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "timestamp-ref-clock-source	value=gps	description=Global Positioning System	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "timestamp-ref-clock-source	value=gal	description=Galileo	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "timestamp-ref-clock-source	value=glonass	description=Global Navigation Satellite System	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "timestamp-ref-clock-source	value=local	description=Local Clock	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "timestamp-ref-clock-source	value=private	description=Private Clock	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "media-clock-source	value=sender	description=Asynchronously Generated Media Clock	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "media-clock-source	value=direct	description=Direct-Referenced Media Clock	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "media-clock-source	value=IEEE1722	description=IEEE1722 Media Stream Identifier	mux=NORMAL	xref=rfc:rfc7273	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=NORMAL	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=CAUTION	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=IDENTICAL	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=TRANSPORT	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=SUM	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=INHERIT	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=IDENTICAL-PER-PT	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=SPECIAL	xref=rfc:rfc8859\n" +
        "multiplexing-categories	@date=2016-12-22	value=TBD	xref=rfc:rfc8859\n" +
        "association-usage-name	@date=2017-03-24	value=webrtc-datachannel	xref=rfc:rfc8841	xref=rfc:rfc8832	controller=IESG\n" +
        "rid-attribute	@date=2018-05-17	value=pt	xref=rfc:rfc8851\n" +
        "rid-attribute	@date=2018-05-17	value=max-width	xref=rfc:rfc8851\n" +
        "rid-attribute	@date=2018-05-17	value=max-height	xref=rfc:rfc8851\n" +
        "rid-attribute	@date=2018-05-17	value=max-fps	xref=rfc:rfc8851\n" +
        "rid-attribute	@date=2018-05-17	value=max-fs	xref=rfc:rfc8851\n" +
        "rid-attribute	@date=2018-05-17	value=max-br	xref=rfc:rfc8851\n" +
        "rid-attribute	@date=2018-05-17	value=max-pps	xref=rfc:rfc8851\n" +
        "rid-attribute	@date=2018-05-17	value=max-bpp	xref=rfc:rfc8851\n" +
        "rid-attribute	@date=2018-05-17	value=depend	xref=rfc:rfc8851";

    /// <summary>Contains normalized tab-delimited data for RtpRegistryDefinitions.</summary>
    public const string RtpRegistryDefinitions =
        "rtp-parameters-1	title=RTP Payload Types (PT) for standard audio and video encodings - Closed	xref=rfc:rfc3551	registration_rule=Registry closed; see , Section 3	note=The RFC \"RTP Profile for Audio and Video Conferences with Minimal Control\" specifies an initial set \"payload types\". This list maintains and extends that list.\n" +
        "rtp-parameters-2	title=RTP Payload Format Media Types	xref=rfc:rfc4855	xref=rfc:rfc9751	registration_rule=Registry closed	note=In addition to the RTP payload formats (encodings) listed in the RTP Payload Types table, there are additional payload formats that do not have static RTP payload types assigned but instead use dynamic payload type number assignment. Each payload format is named by a registered media subtype as listed in the following table. As new payload formats are specified, their registered media subtypes should be added to this table. In addition, for the payload formats listed in the RTP Payload Types table above, the \"encoding name\" is also registered as a media subtype under the media type \"audio\" or \"video\". The clock rate and number of channels shown here are the normal values for those payload formats that have a normal value. Both type and subtype names are case-insensitive as defined in . This registry has been closed; it was considered redundant because all RTP payload formats are part of the . See for further details. It was previously stated that registration procedures and a registration template can be found in . As documented in , this is not the case.\n" +
        "rtp-parameters-4	title=RTCP Control Packet Types (PT)	xref=rfc:rfc5761	xref=rfc:rfc3550	expert=Magnus Westerlund	note=The RFC \"RTP: A Transport Protocol for Real-Time Applications\" specifies an initial set of \"control packet types\" for RTCP. This list maintains and extends that list.	range=1-191 Specification Required or Expert Review	range=194-199 Specification Required or Expert Review If 200-223 is fully occupied	range=200-223 Specification Required or Expert Review Primary Assignments range	range=224-254 Specification Required or Expert Review\n" +
        "rtp-parameters-5	title=RTP SDES Item Types	xref=rfc:rfc3550	registration_rule=Specification Required or Expert Review	expert=Magnus Westerlund	note=The RFC \"RTP: A Transport Protocol for Real-Time Applications\" specifies an initial set of \"item types\" for the RTCP SDES control packet. This list maintains and extends that list.\n" +
        "rtp-parameters-6	title=SMT Values for TOKEN Packet Type Registry	xref=rfc:rfc6284	registration_rule=IETF Review\n" +
        "rtp-parameters-7	title=RTP Profile Names	xref=rfc:rfc3550	registration_rule=Standards Action or Expert Review	expert=Unassigned\n" +
        "rtp-parameters-8	title=FMT Values for RTPFB Payload Types	xref=rfc:rfc4585	registration_rule=Specification Required	expert=Magnus Westerlund\n" +
        "rtp-parameters-9	title=FMT Values for PSFB Payload Types	xref=rfc:rfc4585	registration_rule=Specification Required	expert=Magnus Westerlund\n" +
        "rtp-parameters-10	title=RTP Compact Header Extensions	xref=rfc:rfc8285	registration_rule=Expert Review	expert=Magnus Westerlund	note=For extensions defined in RFCs, the URI is recommended to be of the form urn:ietf:params:rtp-hdrext:, and the formal reference is the RFC number of the RFC documenting the extension.\n" +
        "sdes-compact-header-extensions	title=RTP SDES Compact Header Extensions	xref=rfc:rfc7941	registration_rule=Expert Review	expert=Magnus Westerlund\n" +
        "rtp-parameters-11	title=Sub-Report Block Type (SRBT) Values for the RSI Packet	xref=rfc:rfc5760	registration_rule=Specification Required	expert=Unassigned\n" +
        "rtp-parameters-12	title=SFMT Values for RAMS Messages	xref=rfc:rfc6285	registration_rule=Specification Required	expert=Unassigned\n" +
        "rtp-parameters-13	title=RAMS TLV Space Registry	xref=rfc:rfc6285	registration_rule=Specification Required	expert=Unassigned\n" +
        "rtp-parameters-15	title=RAMS Response Code Space Registry	xref=rfc:rfc6285	registration_rule=Specification Required	expert=Unassigned	note=New response codes should be classified following the guidelines below:	range=1xx Informational	range=2xx Success	range=3xx Redirection	range=4xx RTP Receiver (RTP_Rx) Error	range=5xx Burst/Retransmission Source (BRS) Error\n" +
        "rtp-ekt-message-types	title=EKT Message Types	xref=rfc:rfc8870	registration_rule=Specification Required	expert=Unassigned\n" +
        "rtp-ekt-ciphers	title=EKT Ciphers	xref=rfc:rfc8870	registration_rule=Specification Required	expert=Unassigned";

    /// <summary>Contains normalized tab-delimited data for RtpRecords.</summary>
    public const string RtpRecords =
        "rtp-parameters-1	value=0	name=PCMU	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=1	name=Reserved\n" +
        "rtp-parameters-1	value=2	name=Reserved\n" +
        "rtp-parameters-1	value=3	name=GSM	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	@date=1997-01	value=4	name=G723	av=A	clock_rate=8000	channels=1	xref=person:Vineet_Kumar	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=5	name=DVI4	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=6	name=DVI4	av=A	clock_rate=16000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=7	name=LPC	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=8	name=PCMA	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=9	name=G722	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=10	name=L16	av=A	clock_rate=44100	channels=2	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=11	name=L16	av=A	clock_rate=44100	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=12	name=QCELP	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=13	name=CN	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3389\n" +
        "rtp-parameters-1	value=14	name=MPA	av=A	clock_rate=90000	xref=rfc:rfc3551	xref=rfc:rfc2250\n" +
        "rtp-parameters-1	value=15	name=G728	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	@date=1996-05	value=16	name=DVI4	av=A	clock_rate=11025	channels=1	xref=person:Joseph_Di_Pol\n" +
        "rtp-parameters-1	@date=1996-05	value=17	name=DVI4	av=A	clock_rate=22050	channels=1	xref=person:Joseph_Di_Pol\n" +
        "rtp-parameters-1	value=18	name=G729	av=A	clock_rate=8000	channels=1	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=19	name=Reserved	av=A\n" +
        "rtp-parameters-1	value=20	name=Unassigned	av=A\n" +
        "rtp-parameters-1	value=21	name=Unassigned	av=A\n" +
        "rtp-parameters-1	value=22	name=Unassigned	av=A\n" +
        "rtp-parameters-1	value=23	name=Unassigned	av=A\n" +
        "rtp-parameters-1	value=24	name=Unassigned	av=V\n" +
        "rtp-parameters-1	value=25	name=CelB	av=V	clock_rate=90000	xref=rfc:rfc2029\n" +
        "rtp-parameters-1	value=26	name=JPEG	av=V	clock_rate=90000	xref=rfc:rfc2435\n" +
        "rtp-parameters-1	value=27	name=Unassigned	av=V\n" +
        "rtp-parameters-1	value=28	name=nv	av=V	clock_rate=90000	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=29	name=Unassigned	av=V\n" +
        "rtp-parameters-1	value=30	name=Unassigned	av=V\n" +
        "rtp-parameters-1	value=31	name=H261	av=V	clock_rate=90000	xref=rfc:rfc4587\n" +
        "rtp-parameters-1	value=32	name=MPV	av=V	clock_rate=90000	xref=rfc:rfc2250\n" +
        "rtp-parameters-1	value=33	name=MP2T	av=AV	clock_rate=90000	xref=rfc:rfc2250\n" +
        "rtp-parameters-1	@date=1996-06	value=34	name=H263	av=V	clock_rate=90000	xref=person:Chunrong_Zhu\n" +
        "rtp-parameters-1	value=35-71	name=Unassigned	av=?\n" +
        "rtp-parameters-1	value=72-76	name=Reserved for RTCP conflict avoidance	xref=rfc:rfc3551\n" +
        "rtp-parameters-1	value=77-95	name=Unassigned	av=?\n" +
        "rtp-parameters-1	value=96-127	name=dynamic	av=?	xref=rfc:rfc3551\n" +
        "rtp-parameters-2	type=application	subtype=1d-interleaved-parityfec	xref=rfc:rfc6015\n" +
        "rtp-parameters-2	@date=2024-11-04	type=application	subtype=flexfec	clock_rate=	channels=	xref=rfc:rfc8627\n" +
        "rtp-parameters-2	type=application	subtype=h224	clock_rate=4800	xref=rfc:rfc4573\n" +
        "rtp-parameters-2	type=application	subtype=parityfec	xref=rfc:rfc3009\n" +
        "rtp-parameters-2	type=application	subtype=raptorfec	xref=rfc:rfc6682\n" +
        "rtp-parameters-2	type=application	subtype=rtx	xref=rfc:rfc4588\n" +
        "rtp-parameters-2	type=application	subtype=smpte336m	xref=rfc:rfc6597\n" +
        "rtp-parameters-2	type=application	subtype=ulpfec	xref=rfc:rfc5109\n" +
        "rtp-parameters-2	type=audio	subtype=1d-interleaved-parityfec	xref=rfc:rfc6015\n" +
        "rtp-parameters-2	type=audio	subtype=32kadpcm	clock_rate=8000	xref=rfc:rfc3802	xref=rfc:rfc2421\n" +
        "rtp-parameters-2	type=audio	subtype=ac3	xref=rfc:rfc4184\n" +
        "rtp-parameters-2	type=audio	subtype=AMR	clock_rate=8000	xref=rfc:rfc4867	xref=rfc:rfc3267\n" +
        "rtp-parameters-2	type=audio	subtype=AMR-WB	clock_rate=16000	xref=rfc:rfc4867	xref=rfc:rfc3267\n" +
        "rtp-parameters-2	type=audio	subtype=amr-wb+	clock_rate=72000	xref=rfc:rfc4352\n" +
        "rtp-parameters-2	@date=2024-11-04	type=audio	subtype=aptx	clock_rate=	channels=	xref=rfc:rfc7310\n" +
        "rtp-parameters-2	type=audio	subtype=ATRAC-ADVANCED-LOSSLESS	xref=rfc:rfc5584\n" +
        "rtp-parameters-2	type=audio	subtype=atrac-x	xref=rfc:rfc5584\n" +
        "rtp-parameters-2	type=audio	subtype=atrac3	clock_rate=44100	xref=rfc:rfc5584\n" +
        "rtp-parameters-2	type=audio	subtype=BV16	clock_rate=8000	xref=rfc:rfc4298\n" +
        "rtp-parameters-2	type=audio	subtype=BV32	clock_rate=16000	xref=rfc:rfc4298\n" +
        "rtp-parameters-2	type=audio	subtype=clearmode	clock_rate=8000	channels=1	xref=rfc:rfc4040\n" +
        "rtp-parameters-2	type=audio	subtype=CN	xref=rfc:rfc3389\n" +
        "rtp-parameters-2	type=audio	subtype=DAT12	xref=rfc:rfc3190\n" +
        "rtp-parameters-2	type=audio	subtype=dsr-es201108	xref=rfc:rfc3557\n" +
        "rtp-parameters-2	type=audio	subtype=dsr-es202050	clock_rate=8000	xref=rfc:rfc4060\n" +
        "rtp-parameters-2	type=audio	subtype=dsr-es202211	clock_rate=8000	xref=rfc:rfc4060\n" +
        "rtp-parameters-2	type=audio	subtype=dsr-es202212	clock_rate=8000	xref=rfc:rfc4060\n" +
        "rtp-parameters-2	type=audio	subtype=DV	xref=rfc:rfc6469\n" +
        "rtp-parameters-2	type=audio	subtype=DVI4	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=eac3	xref=rfc:rfc4598\n" +
        "rtp-parameters-2	type=audio	subtype=EVRC	clock_rate=8000	channels=1	xref=rfc:rfc4788\n" +
        "rtp-parameters-2	type=audio	subtype=EVRC0	clock_rate=8000	channels=1	xref=rfc:rfc4788\n" +
        "rtp-parameters-2	type=audio	subtype=EVRC1	clock_rate=8000	channels=1	xref=rfc:rfc4788\n" +
        "rtp-parameters-2	type=audio	subtype=EVRCB	clock_rate=8000	channels=1	xref=rfc:rfc4788\n" +
        "rtp-parameters-2	type=audio	subtype=EVRCB0	clock_rate=8000	channels=1	xref=rfc:rfc4788\n" +
        "rtp-parameters-2	type=audio	subtype=EVRCB1	clock_rate=8000	channels=1	xref=rfc:rfc4788\n" +
        "rtp-parameters-2	@date=2024-11-04	type=audio	subtype=EVRCNW	clock_rate=16000	channels=	xref=rfc:rfc6884\n" +
        "rtp-parameters-2	@date=2024-11-04	type=audio	subtype=EVRCNW0	clock_rate=16000	channels=	xref=rfc:rfc6884\n" +
        "rtp-parameters-2	@date=2024-11-04	type=audio	subtype=EVRCNW1	clock_rate=16000	channels=	xref=rfc:rfc6884\n" +
        "rtp-parameters-2	type=audio	subtype=EVRCWB	xref=rfc:rfc5188\n" +
        "rtp-parameters-2	type=audio	subtype=EVRCWB0	xref=rfc:rfc5188\n" +
        "rtp-parameters-2	type=audio	subtype=EVRCWB1	xref=rfc:rfc5188\n" +
        "rtp-parameters-2	@date=2024-11-04	type=audio	subtype=flexfec	clock_rate=	channels=	xref=rfc:rfc8627\n" +
        "rtp-parameters-2	type=audio	subtype=fwdred	xref=rfc:rfc6354\n" +
        "rtp-parameters-2	@date=2024-11-04	type=audio	subtype=G711-0	clock_rate=	channels=	xref=rfc:rfc7650\n" +
        "rtp-parameters-2	type=audio	subtype=g719	clock_rate=48000	xref=rfc:rfc5404\n" +
        "rtp-parameters-2	type=audio	subtype=G722	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G7221	clock_rate=16000	channels=1	xref=rfc:rfc5577\n" +
        "rtp-parameters-2	type=audio	subtype=G723	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G726-16	clock_rate=8000	channels=1	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G726-24	clock_rate=8000	channels=1	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G726-32	clock_rate=8000	channels=1	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G726-40	clock_rate=8000	channels=1	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G728	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G729	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G7291	clock_rate=16000	xref=rfc:rfc4749	xref=rfc:rfc5459\n" +
        "rtp-parameters-2	type=audio	subtype=G729D	clock_rate=8000	channels=1	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=G729E	clock_rate=8000	channels=1	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=GSM	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=GSM-EFR	clock_rate=8000	channels=1	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=GSM-HR-08	clock_rate=8000	xref=rfc:rfc5993\n" +
        "rtp-parameters-2	type=audio	subtype=iLBC	clock_rate=8000	xref=rfc:rfc3952\n" +
        "rtp-parameters-2	type=audio	subtype=ip-mr_v2.5	clock_rate=16000	xref=rfc:rfc6262\n" +
        "rtp-parameters-2	type=audio	subtype=L8	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=L16	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=L20	xref=rfc:rfc3190\n" +
        "rtp-parameters-2	type=audio	subtype=L24	xref=rfc:rfc3190\n" +
        "rtp-parameters-2	type=audio	subtype=LPC	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	@date=2017-02-15	type=audio	subtype=MELP	clock_rate=8000	channels=1	xref=rfc:rfc8130\n" +
        "rtp-parameters-2	@date=2017-02-15	type=audio	subtype=MELP600	clock_rate=8000	channels=1	xref=rfc:rfc8130\n" +
        "rtp-parameters-2	@date=2017-02-15	type=audio	subtype=MELP1200	clock_rate=8000	channels=1	xref=rfc:rfc8130\n" +
        "rtp-parameters-2	@date=2017-02-15	type=audio	subtype=MELP2400	clock_rate=8000	channels=1	xref=rfc:rfc8130\n" +
        "rtp-parameters-2	@updated=2024-11-04	type=audio	subtype=MP4A-LATM	xref=rfc:rfc6416\n" +
        "rtp-parameters-2	type=audio	subtype=MPA	clock_rate=90000	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=audio	subtype=mpa-robust	clock_rate=90000	xref=rfc:rfc5219\n" +
        "rtp-parameters-2	type=audio	subtype=mpeg4-generic	xref=rfc:rfc3640	xref=rfc:rfc5691	xref=rfc:rfc6295\n" +
        "rtp-parameters-2	@date=2024-11-04	type=audio	subtype=opus	clock_rate=48000	channels=	xref=rfc:rfc7587\n" +
        "rtp-parameters-2	type=audio	subtype=parityfec	xref=rfc:rfc5109\n" +
        "rtp-parameters-2	type=audio	subtype=PCMA	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=PCMA-WB	clock_rate=16000	xref=rfc:rfc5391\n" +
        "rtp-parameters-2	type=audio	subtype=PCMU	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=PCMU-WB	clock_rate=16000	xref=rfc:rfc5391\n" +
        "rtp-parameters-2	type=audio	subtype=QCELP	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=audio	subtype=raptorfec	xref=rfc:rfc6682\n" +
        "rtp-parameters-2	type=audio	subtype=RED	xref=rfc:rfc2198	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=audio	subtype=rtp-midi	xref=rfc:rfc6295\n" +
        "rtp-parameters-2	type=audio	subtype=rtx	xref=rfc:rfc4588\n" +
        "rtp-parameters-2	@date=2021-03-15	type=audio	subtype=scip	clock_rate=8000	channels=1	xref=person:SCIP	xref=person:Michael_Faller	xref=person:Daniel_Hanson\n" +
        "rtp-parameters-2	type=audio	subtype=SMV	clock_rate=8000	channels=1	xref=rfc:rfc3558\n" +
        "rtp-parameters-2	type=audio	subtype=SMV0	clock_rate=8000	channels=1	xref=rfc:rfc3558\n" +
        "rtp-parameters-2	type=audio	subtype=speex	xref=rfc:rfc5574\n" +
        "rtp-parameters-2	type=audio	subtype=t140c	xref=rfc:rfc4351\n" +
        "rtp-parameters-2	type=audio	subtype=t38	xref=rfc:rfc4612\n" +
        "rtp-parameters-2	type=audio	subtype=telephone-event	xref=rfc:rfc4733\n" +
        "rtp-parameters-2	type=audio	subtype=tone	xref=rfc:rfc4733\n" +
        "rtp-parameters-2	@date=2020-06-19	type=audio	subtype=TSVCIS	clock_rate=8000	channels=1	xref=rfc:rfc8817\n" +
        "rtp-parameters-2	type=audio	subtype=uemclip	xref=rfc:rfc5686\n" +
        "rtp-parameters-2	type=audio	subtype=ulpfec	xref=rfc:rfc5109\n" +
        "rtp-parameters-2	type=audio	subtype=VDVI	channels=1	xref=rfc:rfc3551	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=audio	subtype=VMR-WB	clock_rate=16000	xref=rfc:rfc4348	xref=rfc:rfc4424\n" +
        "rtp-parameters-2	type=audio	subtype=vorbis	xref=rfc:rfc5215\n" +
        "rtp-parameters-2	type=audio	subtype=vorbis-config	xref=rfc:rfc5215\n" +
        "rtp-parameters-2	type=text	subtype=1d-interleaved-parityfec	xref=rfc:rfc6015\n" +
        "rtp-parameters-2	@date=2024-11-04	type=text	subtype=flexfec	clock_rate=	channels=	xref=rfc:rfc8627\n" +
        "rtp-parameters-2	type=text	subtype=fwdred	xref=rfc:rfc6354\n" +
        "rtp-parameters-2	type=text	subtype=parityfec	xref=rfc:rfc3009\n" +
        "rtp-parameters-2	type=text	subtype=raptorfec	xref=rfc:rfc6682\n" +
        "rtp-parameters-2	type=text	subtype=red	clock_rate=1000	xref=rfc:rfc4102\n" +
        "rtp-parameters-2	type=text	subtype=rtx	xref=rfc:rfc4588\n" +
        "rtp-parameters-2	type=text	subtype=t140	clock_rate=1000	xref=rfc:rfc4103\n" +
        "rtp-parameters-2	@date=2024-11-04	type=text	subtype=ttml+xml	clock_rate=	channels=	xref=rfc:rfc8759\n" +
        "rtp-parameters-2	type=text	subtype=ulpfec	xref=rfc:rfc5109\n" +
        "rtp-parameters-2	type=video	subtype=BMPEG	clock_rate=90000	xref=rfc:rfc2343	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=video	subtype=1d-interleaved-parityfec	xref=rfc:rfc6015\n" +
        "rtp-parameters-2	type=video	subtype=3gpp-tt	xref=rfc:rfc4396\n" +
        "rtp-parameters-2	@date=2024-11-04	type=video	subtype=AV1	clock_rate=90000	channels=	xref=uri:https://www.iana.org/assignments/media-types/video/AV1\n" +
        "rtp-parameters-2	type=video	subtype=BT656	clock_rate=90000	xref=rfc:rfc2431	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=video	subtype=celB	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=video	subtype=DV	clock_rate=90000	xref=rfc:rfc6469\n" +
        "rtp-parameters-2	@date=2024-11-04	type=video	subtype=EVC	clock_rate=90000	channels=	xref=rfc:rfc9584\n" +
        "rtp-parameters-2	@date=2024-11-04	type=video	subtype=flexfec	clock_rate=	channels=	xref=rfc:rfc8627\n" +
        "rtp-parameters-2	type=video	subtype=H261	xref=rfc:rfc4587\n" +
        "rtp-parameters-2	type=video	subtype=H263	clock_rate=90000	xref=rfc:rfc4628\n" +
        "rtp-parameters-2	type=video	subtype=H263-1998	clock_rate=90000	xref=rfc:rfc4629\n" +
        "rtp-parameters-2	type=video	subtype=H263-2000	clock_rate=90000	xref=rfc:rfc4629\n" +
        "rtp-parameters-2	type=video	subtype=H264	xref=rfc:rfc6184\n" +
        "rtp-parameters-2	type=video	subtype=H264-RCDO	clock_rate=90000	xref=rfc:rfc6185\n" +
        "rtp-parameters-2	type=video	subtype=H264-SVC	xref=rfc:rfc6190\n" +
        "rtp-parameters-2	@date=2024-11-04	type=video	subtype=HEVC	clock_rate=90000	channels=	xref=rfc:rfc7798\n" +
        "rtp-parameters-2	type=video	subtype=JPEG	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=video	subtype=JPEG2000	xref=rfc:rfc5371\n" +
        "rtp-parameters-2	@date=2021-07-28	type=video	subtype=jxsv	clock_rate=90000	xref=rfc:rfc9134\n" +
        "rtp-parameters-2	type=video	subtype=MP1S	clock_rate=90000	xref=rfc:rfc2250	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=video	subtype=MP2P	clock_rate=90000	xref=rfc:rfc2250	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=video	subtype=MP2T	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	@updated=2024-11-04	type=video	subtype=MP4V-ES	clock_rate=90000	xref=rfc:rfc6416\n" +
        "rtp-parameters-2	type=video	subtype=mpeg4-generic	xref=rfc:rfc3640\n" +
        "rtp-parameters-2	type=video	subtype=MPV	xref=rfc:rfc3555\n" +
        "rtp-parameters-2	type=video	subtype=nv	xref=rfc:rfc4856\n" +
        "rtp-parameters-2	type=video	subtype=parityfec	xref=rfc:rfc5109\n" +
        "rtp-parameters-2	type=video	subtype=pointer	clock_rate=90000	xref=rfc:rfc2862\n" +
        "rtp-parameters-2	type=video	subtype=raptorfec	xref=rfc:rfc6682\n" +
        "rtp-parameters-2	type=video	subtype=raw	clock_rate=90000	xref=rfc:rfc4175\n" +
        "rtp-parameters-2	type=video	subtype=rtx	xref=rfc:rfc4588\n" +
        "rtp-parameters-2	@date=2021-03-15	type=video	subtype=scip	clock_rate=90000	channels=1	xref=person:SCIP	xref=person:Michael_Faller	xref=person:Daniel_Hanson\n" +
        "rtp-parameters-2	@date=2024-11-04	type=video	subtype=smpte291	clock_rate=	channels=	xref=rfc:rfc8331\n" +
        "rtp-parameters-2	type=video	subtype=SMPTE292M	xref=rfc:rfc3497\n" +
        "rtp-parameters-2	type=video	subtype=ulpfec	xref=rfc:rfc5109\n" +
        "rtp-parameters-2	type=video	subtype=vc1	clock_rate=90000	xref=rfc:rfc4425\n" +
        "rtp-parameters-2	@date=2018-08-29	type=video	subtype=vc2	clock_rate=90000	xref=rfc:rfc8450\n" +
        "rtp-parameters-2	@date=2024-11-04	type=video	subtype=VP8	clock_rate=90000	channels=	xref=rfc:rfc7741\n" +
        "rtp-parameters-2	@date=2021-06-11	type=video	subtype=VP9	clock_rate=90000	xref=rfc:rfc9628\n" +
        "rtp-parameters-2	@date=2024-11-04	type=video	subtype=VVC	clock_rate=90000	channels=	xref=rfc:rfc9328\n" +
        "rtp-parameters-4	value=0	name=Reserved\n" +
        "rtp-parameters-4	value=1-191	name=Unassigned\n" +
        "rtp-parameters-4	value=192	name=Reserved (Historic-FIR)	xref=rfc:rfc2032\n" +
        "rtp-parameters-4	value=193	name=Reserved (Historic-NACK)	xref=rfc:rfc2032\n" +
        "rtp-parameters-4	value=194	abbrev=SMPTETC	name=SMPTE time-code mapping	xref=rfc:rfc5484\n" +
        "rtp-parameters-4	value=195	abbrev=IJ	name=Extended inter-arrival jitter report	xref=rfc:rfc5450\n" +
        "rtp-parameters-4	value=196-199	name=Unassigned\n" +
        "rtp-parameters-4	value=200	abbrev=SR	name=sender report	xref=rfc:rfc3550\n" +
        "rtp-parameters-4	value=201	abbrev=RR	name=receiver report	xref=rfc:rfc3550\n" +
        "rtp-parameters-4	value=202	abbrev=SDES	name=source description	xref=rfc:rfc3550\n" +
        "rtp-parameters-4	value=203	abbrev=BYE	name=goodbye	xref=rfc:rfc3550\n" +
        "rtp-parameters-4	value=204	abbrev=APP	name=application-defined	xref=rfc:rfc3550\n" +
        "rtp-parameters-4	value=205	abbrev=RTPFB	name=Generic RTP Feedback	xref=rfc:rfc4585\n" +
        "rtp-parameters-4	value=206	abbrev=PSFB	name=Payload-specific	xref=rfc:rfc4585\n" +
        "rtp-parameters-4	value=207	abbrev=XR	name=extended report	xref=rfc:rfc3611\n" +
        "rtp-parameters-4	@updated=2024-04-11	value=208	abbrev=AVB	name=AVB RTCP packet	xref=uri:https://standards.ieee.org/ieee/1733/4748/\n" +
        "rtp-parameters-4	value=209	abbrev=RSI	name=Receiver Summary Information	xref=rfc:rfc5760\n" +
        "rtp-parameters-4	value=210	abbrev=TOKEN	name=Port Mapping	xref=rfc:rfc6284\n" +
        "rtp-parameters-4	@date=2013-09-11	value=211	abbrev=IDMS	name=IDMS Settings	xref=rfc:rfc7272\n" +
        "rtp-parameters-4	@date=2016-03-18	value=212	abbrev=RGRS	name=Reporting Group Reporting Sources	xref=rfc:rfc8861\n" +
        "rtp-parameters-4	@date=2016-08-09	value=213	abbrev=SNM	name=Splicing Notification Message	xref=rfc:rfc8286\n" +
        "rtp-parameters-4	value=214-254	name=Unassigned\n" +
        "rtp-parameters-4	value=255	name=Reserved\n" +
        "rtp-parameters-5	value=0	abbrev=END	name=end of SDES list	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	value=1	abbrev=CNAME	name=canonical name	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	value=2	abbrev=NAME	name=user name	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	value=3	abbrev=EMAIL	name=user's electronic mail address	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	value=4	abbrev=PHONE	name=user's phone number	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	value=5	abbrev=LOC	name=geographic user location	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	value=6	abbrev=TOOL	name=name of application or tool	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	value=7	abbrev=NOTE	name=notice about the source	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	value=8	abbrev=PRIV	name=private extensions	xref=rfc:rfc3550\n" +
        "rtp-parameters-5	@date=1997-01	value=9	abbrev=H323-CADDR	name=H.323 callable address	xref=person:Vineet_Kumar\n" +
        "rtp-parameters-5	value=10	abbrev=APSI	name=Application Specific Identifier	xref=rfc:rfc6776\n" +
        "rtp-parameters-5	@date=2016-03-18	value=11	abbrev=RGRP	name=Reporting Group Identifier	xref=rfc:rfc8861\n" +
        "rtp-parameters-5	@date=2016-10-20	value=12	abbrev=RtpStreamId	name=RTP Stream Identifier	xref=rfc:rfc8852\n" +
        "rtp-parameters-5	@date=2016-10-20	value=13	abbrev=RepairedRtpStreamId	name=Repaired RTP Stream Identifier	xref=rfc:rfc8852\n" +
        "rtp-parameters-5	@date=2017-03-03	value=14	abbrev=CCID	name=CLUE CaptId	xref=rfc:rfc8849\n" +
        "rtp-parameters-5	@date=2018-06-18	value=15	abbrev=MID	name=Media Identification	xref=rfc:rfc9143\n" +
        "rtp-parameters-5	value=16-255	name=Unassigned\n" +
        "rtp-parameters-6	value=0	name=Reserved	xref=rfc:rfc6284\n" +
        "rtp-parameters-6	value=1	name=Port Mapping Request	xref=rfc:rfc6284\n" +
        "rtp-parameters-6	value=2	name=Port Mapping Response	xref=rfc:rfc6284\n" +
        "rtp-parameters-6	value=3	name=Token Verification Request	xref=rfc:rfc6284\n" +
        "rtp-parameters-6	value=4	name=Token Verification Failure	xref=rfc:rfc6284\n" +
        "rtp-parameters-6	value=5-30	name=Unassigned\n" +
        "rtp-parameters-6	value=31	name=Reserved	xref=rfc:rfc6284\n" +
        "rtp-parameters-7	name=RTP/AVP	xref=rfc:rfc3551\n" +
        "rtp-parameters-7	name=RTP/SAVP	xref=rfc:rfc3711\n" +
        "rtp-parameters-7	name=RTP/AVPF	xref=rfc:rfc4585\n" +
        "rtp-parameters-7	name=RTP/SAVPF	xref=rfc:rfc5124\n" +
        "rtp-parameters-8	value=1	name=Generic NACK	long_name=Generic negative acknowledgement	xref=rfc:rfc4585\n" +
        "rtp-parameters-8	value=2	long_name=Reserved	xref=rfc:rfc5104\n" +
        "rtp-parameters-8	value=3	name=TMMBR	long_name=Temporary Maximum Media Stream Bit Rate Request	xref=rfc:rfc5104\n" +
        "rtp-parameters-8	value=4	name=TMMBN	long_name=Temporary Maximum Media Stream Bit Rate Notification	xref=rfc:rfc5104\n" +
        "rtp-parameters-8	value=5	name=RTCP-SR-REQ	long_name=RTCP Rapid Resynchronisation Request	xref=rfc:rfc6051\n" +
        "rtp-parameters-8	value=6	name=RAMS	long_name=Rapid Acquisition of Multicast Sessions	xref=rfc:rfc6285\n" +
        "rtp-parameters-8	value=7	name=TLLEI	long_name=Transport-Layer Third-Party Loss Early Indication	xref=rfc:rfc6642\n" +
        "rtp-parameters-8	value=8	name=RTCP-ECN-FB	long_name=RTCP ECN Feedback	xref=rfc:rfc6679\n" +
        "rtp-parameters-8	@date=2015-09-30	value=9	name=PAUSE-RESUME	long_name=Media Pause/Resume	xref=rfc:rfc7728\n" +
        "rtp-parameters-8	@date=2018-10-29	value=10	name=DBI	long_name=Delay Budget Information (DBI)	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:Ozgur_Oyman\n" +
        "rtp-parameters-8	@date=2020-11-19	value=11	name=CCFB	long_name=RTP Congestion Control Feedback	xref=rfc:rfc8888\n" +
        "rtp-parameters-8	value=12-30	long_name=Unassigned\n" +
        "rtp-parameters-8	value=31	name=Extension	long_name=Reserved for future extensions	xref=rfc:rfc4585\n" +
        "rtp-parameters-9	value=1	name=PLI	long_name=Picture Loss Indication	xref=rfc:rfc4585\n" +
        "rtp-parameters-9	value=2	name=SLI	long_name=Slice Loss Indication	xref=rfc:rfc4585\n" +
        "rtp-parameters-9	value=3	name=RPSI	long_name=Reference Picture Selection Indication	xref=rfc:rfc4585\n" +
        "rtp-parameters-9	value=4	name=FIR	long_name=Full Intra Request Command	xref=rfc:rfc5104\n" +
        "rtp-parameters-9	value=5	name=TSTR	long_name=Temporal-Spatial Trade-off Request	xref=rfc:rfc5104\n" +
        "rtp-parameters-9	value=6	name=TSTN	long_name=Temporal-Spatial Trade-off Notification	xref=rfc:rfc5104\n" +
        "rtp-parameters-9	value=7	name=VBCM	long_name=Video Back Channel Message	xref=rfc:rfc5104\n" +
        "rtp-parameters-9	value=8	name=PSLEI	long_name=Payload-Specific Third-Party Loss Early Indication	xref=rfc:rfc6642\n" +
        "rtp-parameters-9	@date=2015-10-19	value=9	name=ROI	long_name=Video region-of-interest (ROI)	xref=uri:https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId%3D1404	xref=person:Ozgur_Oyman\n" +
        "rtp-parameters-9	@date=2017-07-10	value=10	name=LRR	long_name=Layer Refresh Request Command	xref=rfc:rfc9627\n" +
        "rtp-parameters-9	@date=2023-06-12	value=11	name=VP	long_name=Viewport (VP)	xref=uri:https://www.3gpp.org/ftp/Specs/archive/26_series/26.114\n" +
        "rtp-parameters-9	value=12-14	long_name=Unassigned\n" +
        "rtp-parameters-9	value=15	name=AFB	long_name=Application Layer Feedback	xref=rfc:rfc4585\n" +
        "rtp-parameters-9	value=16-30	long_name=Unassigned\n" +
        "rtp-parameters-9	value=31	name=Extension	long_name=Reserved for future extensions	xref=rfc:rfc4585\n" +
        "rtp-parameters-10	name=urn:ietf:params:rtp-hdrext:toffset	description=Transmission Time offsets	contact=	xref=rfc:rfc5450\n" +
        "rtp-parameters-10	name=urn:ietf:params:rtp-hdrext:smpte-tc	description=SMPTE time-code mapping	contact=	xref=rfc:rfc5484\n" +
        "rtp-parameters-10	name=urn:ietf:params:rtp-hdrext:ntp-64	description=Synchronisation metadata: 64-bit timestamp format	contact=	xref=uri:http://www.ietf.org/wg/concluded/avt.html	xref=rfc:rfc6051\n" +
        "rtp-parameters-10	name=urn:ietf:params:rtp-hdrext:ntp-56	description=Synchronisation metadata: 56-bit timestamp format	contact=	xref=uri:http://www.ietf.org/wg/concluded/avt.html	xref=rfc:rfc6051\n" +
        "rtp-parameters-10	name=urn:ietf:params:rtp-hdrext:ssrc-audio-level	description=Audio Level	contact=	xref=rfc:rfc6464\n" +
        "rtp-parameters-10	name=urn:ietf:params:rtp-hdrext:csrc-audio-level	description=Mixer-to-client audio level indicators	contact=	xref=rfc:rfc6465\n" +
        "rtp-parameters-10	name=urn:ietf:params:rtp-hdrext:encrypt	description=Encrypted extension header element	contact=	xref=rfc:rfc6904\n" +
        "rtp-parameters-10	@date=2014-05-01	name=urn:3gpp:video-orientation	description=Coordination of video orientation (CVO) feature, see clause 6.2.3	contact=	xref=uri:http://www.3gpp.org/DynaReport/26114.htm\n" +
        "rtp-parameters-10	@date=2014-05-01	name=urn:3gpp:video-orientation:6	description=Higher granularity (6-bit) coordination of video orientation (CVO) feature, see clause 6.2.3	contact=	xref=uri:http://www.3gpp.org/DynaReport/26114.htm\n" +
        "rtp-parameters-10	@date=2015-09-16	name=urn:3gpp:roi-sent	description=Signalling of the arbitrary region-of-interest (ROI) information for the sent video, see clause 6.2.3.4	contact=	xref=uri:http://www.3gpp.org/DynaReport/26114.htm\n" +
        "rtp-parameters-10	@date=2015-09-16	name=urn:3gpp:predefined-roi-sent	description=Signalling of the predefined region-of-interest (ROI) information for the sent video, see clause 6.2.3.4	contact=	xref=uri:http://www.3gpp.org/DynaReport/26114.htm\n" +
        "rtp-parameters-10	@date=2016-06-23	name=urn:ietf:params:rtp-hdrext:sdes	description=Reserved as base URN for RTCP SDES items that are also defined as RTP compact header extensions.	contact=Authors of	xref=rfc:rfc7941\n" +
        "rtp-parameters-10	@date=2016-06-23	name=urn:ietf:params:rtp-hdrext:splicing-interval	description=Splicing Interval	contact=	xref=rfc:rfc8286\n" +
        "rtp-parameters-10	@date=2024-03-18	@updated=2025-03-20	name=urn:ietf:params:rtp-hdrext:framemarking	description=Frame marking information for video streams	contact=	xref=rfc:rfc9626\n" +
        "sdes-compact-header-extensions	@date=2016-06-23	name=urn:ietf:params:rtp-hdrext:sdes:cname	description=Source Description: Canonical End-Point Identifier (SDES CNAME)	contact=Authors of	xref=rfc:rfc7941\n" +
        "sdes-compact-header-extensions	@date=2016-10-20	name=urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id	description=RTP Stream Identifier	contact=	xref=rfc:rfc8852\n" +
        "sdes-compact-header-extensions	@date=2016-10-20	name=urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id	description=RTP Repaired Stream Identifier	contact=	xref=rfc:rfc8852\n" +
        "sdes-compact-header-extensions	@date=2017-03-03	name=urn:ietf:params:rtp-hdrext:sdes:CaptId	description=CLUE CaptId	contact=	xref=rfc:rfc8849\n" +
        "sdes-compact-header-extensions	@date=2018-06-18	name=urn:ietf:params:rtp-hdrext:sdes:mid	description=Media identification	contact=	xref=rfc:rfc9143\n" +
        "rtp-parameters-11	value=0	name=IPv4 Address	long_name=IPv4 Feedback Target Address	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=1	name=IPv6 Address	long_name=IPv6 Feedback Target Address	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=2	name=DNS Name	long_name=DNS Name indicating Feedback Target Address	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=3	name=Reserved	long_name=Reserved for Assignment by Standards Action	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=4	name=Loss	long_name=Loss distribution	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=5	name=Jitter	long_name=Jitter distribution	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=6	name=RTT	long_name=Round-trip time distribution	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=7	name=Cumulative loss	long_name=Cumulative loss distribution	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=8	name=Collisions	long_name=SSRC Collision list	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=9	name=Reserved	long_name=Reserved for Assignment by Standards Action	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=10	name=Stats	long_name=General statistics	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=11	name=RTCP BW	long_name=RTCP Bandwidth indication	xref=rfc:rfc5760\n" +
        "rtp-parameters-11	value=12	name=Group Info	long_name=RTCP Group and Average Packet size	xref=rfc:rfc5760\n" +
        "rtp-parameters-12	value=0	name=Reserved	xref=rfc:rfc6285\n" +
        "rtp-parameters-12	value=1	name=RAMS Request	xref=rfc:rfc6285\n" +
        "rtp-parameters-12	value=2	name=RAMS Information	xref=rfc:rfc6285\n" +
        "rtp-parameters-12	value=3	name=RAMS Termination	xref=rfc:rfc6285\n" +
        "rtp-parameters-12	value=4-254	name=Unassigned\n" +
        "rtp-parameters-12	value=255	name=Reserved	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=0	name=Reserved	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=1	name=Requested Media Sender SSRC(s)	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=2	name=Min RAMS Buffer Fill Requirement	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=3	name=Max RAMS Buffer Fill Requirement	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=4	name=Max Receive Bitrate	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=5	name=Request for Preamble Only	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=6	name=Supported Enterprise Number(s)	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=7-30	name=Unassigned\n" +
        "rtp-parameters-13	value=31	name=Media Sender SSRC	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=32	name=RTP Seqnum of the First Packet	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=33	name=Earliest Multicast Join Time	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=34	name=Burst Duration	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=35	name=Max Transmit Bitrate	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=36-60	name=Unassigned\n" +
        "rtp-parameters-13	value=61	name=Extended RTP Seqnum of First Multicast Packet	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=62-127	name=Unassigned\n" +
        "rtp-parameters-13	value=128-254	name=Reserved for Private Use	xref=rfc:rfc6285\n" +
        "rtp-parameters-13	value=255	name=Reserved	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=0	description=A private response code is included in the message	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=100	description=Parameter update for RAMS session	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=200	description=RAMS request has been accepted	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=201	description=Unicast burst has been completed	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=400	description=Invalid RAMS-R message syntax	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=401	description=Invalid min buffer requirement in RAMS-R message	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=402	description=Invalid max buffer requirement in RAMS-R message	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=403	description=Insufficient max bitrate requirement in RAMS-R message	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=404	description=Invalid RAMS-T message syntax	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=405	description=Invalid Token	xref=rfc:rfc6284\n" +
        "rtp-parameters-15	value=500	description=An unspecified BRS internal error has occurred	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=501	description=BRS has insufficient bandwidth to start RAMS session	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=502	description=Burst is terminated due to network congestion	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=503	description=BRS has insufficient CPU cycles to start RAMS session	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=504	description=RAMS functionality is not available on BRS	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=505	description=RAMS functionality is not available for RTP_Rx	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=506	description=RAMS functionality is not available for the requested multicast stream	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=507	description=BRS has no valid starting point available for the requested multicast stream	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=508	description=BRS has no reference information available for the requested multicast stream	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=509	description=BRS has no RTP stream matching the requested SSRC	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=510	description=RAMS request to acquire the entire session has been denied	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=511	description=Only the preamble information is sent	xref=rfc:rfc6285\n" +
        "rtp-parameters-15	value=512	description=RAMS request has been denied due to a policy	xref=rfc:rfc6285\n" +
        "rtp-ekt-message-types	@date=2020-06-26	value=0	description=Short	xref=rfc:rfc8870\n" +
        "rtp-ekt-message-types	value=1	description=Unassigned\n" +
        "rtp-ekt-message-types	@date=2020-06-26	value=2	description=Full	xref=rfc:rfc8870\n" +
        "rtp-ekt-message-types	value=3-254	description=Unassigned\n" +
        "rtp-ekt-message-types	@date=2020-06-26	value=255	description=Reserved	xref=rfc:rfc8870\n" +
        "rtp-ekt-ciphers	@date=2020-06-26	value=0	description=AESKW128	xref=rfc:rfc8870\n" +
        "rtp-ekt-ciphers	@date=2020-06-26	value=1	description=AESKW256	xref=rfc:rfc8870\n" +
        "rtp-ekt-ciphers	value=2-254	description=Unassigned\n" +
        "rtp-ekt-ciphers	@date=2020-06-26	value=255	description=Reserved	xref=rfc:rfc8870";

}
