namespace icc;

/// <summary>
/// Defines registered ICC.1, ICC.2, legacy, vendor, and amendment tag signatures.
/// </summary>
public static class IccTagSignatures
{
    /// <summary>The <c>A2B0</c> transform tag.</summary>
    public const uint AToB0 = 0x41324230;
    /// <summary>The <c>A2B1</c> transform tag.</summary>
    public const uint AToB1 = 0x41324231;
    /// <summary>The <c>A2B2</c> transform tag.</summary>
    public const uint AToB2 = 0x41324232;
    /// <summary>The ICC.2 <c>A2B3</c> transform tag.</summary>
    public const uint AToB3 = 0x41324233;
    /// <summary>The <c>B2A0</c> transform tag.</summary>
    public const uint BToA0 = 0x42324130;
    /// <summary>The <c>B2A1</c> transform tag.</summary>
    public const uint BToA1 = 0x42324131;
    /// <summary>The <c>B2A2</c> transform tag.</summary>
    public const uint BToA2 = 0x42324132;
    /// <summary>The ICC.2 <c>B2A3</c> transform tag.</summary>
    public const uint BToA3 = 0x42324133;
    /// <summary>The <c>B2D0</c> transform tag.</summary>
    public const uint BToD0 = 0x42324430;
    /// <summary>The <c>B2D1</c> transform tag.</summary>
    public const uint BToD1 = 0x42324431;
    /// <summary>The <c>B2D2</c> transform tag.</summary>
    public const uint BToD2 = 0x42324432;
    /// <summary>The <c>B2D3</c> transform tag.</summary>
    public const uint BToD3 = 0x42324433;
    /// <summary>The <c>D2B0</c> transform tag.</summary>
    public const uint DToB0 = 0x44324230;
    /// <summary>The <c>D2B1</c> transform tag.</summary>
    public const uint DToB1 = 0x44324231;
    /// <summary>The <c>D2B2</c> transform tag.</summary>
    public const uint DToB2 = 0x44324232;
    /// <summary>The <c>D2B3</c> transform tag.</summary>
    public const uint DToB3 = 0x44324233;
    /// <summary>The ICC.2 <c>A2M0</c> transform tag.</summary>
    public const uint AToM0 = 0x41324d30;
    /// <summary>The ICC.2 <c>M2A0</c> transform tag.</summary>
    public const uint MToA0 = 0x4d324130;
    /// <summary>The ICC.2 <c>M2B0</c> transform tag.</summary>
    public const uint MToB0 = 0x4d324230;
    /// <summary>The ICC.2 <c>M2B1</c> transform tag.</summary>
    public const uint MToB1 = 0x4d324231;
    /// <summary>The ICC.2 <c>M2B2</c> transform tag.</summary>
    public const uint MToB2 = 0x4d324232;
    /// <summary>The ICC.2 <c>M2B3</c> transform tag.</summary>
    public const uint MToB3 = 0x4d324233;
    /// <summary>The ICC.2 <c>M2S0</c> transform tag.</summary>
    public const uint MToS0 = 0x4d325330;
    /// <summary>The ICC.2 <c>M2S1</c> transform tag.</summary>
    public const uint MToS1 = 0x4d325331;
    /// <summary>The ICC.2 <c>M2S2</c> transform tag.</summary>
    public const uint MToS2 = 0x4d325332;
    /// <summary>The ICC.2 <c>M2S3</c> transform tag.</summary>
    public const uint MToS3 = 0x4d325333;
    /// <summary>The blue matrix-column tag <c>bXYZ</c>.</summary>
    public const uint BlueMatrixColumn = 0x6258595a;
    /// <summary>The blue tone-reproduction-curve tag <c>bTRC</c>.</summary>
    public const uint BlueTrc = 0x62545243;
    /// <summary>The green matrix-column tag <c>gXYZ</c>.</summary>
    public const uint GreenMatrixColumn = 0x6758595a;
    /// <summary>The green tone-reproduction-curve tag <c>gTRC</c>.</summary>
    public const uint GreenTrc = 0x67545243;
    /// <summary>The red matrix-column tag <c>rXYZ</c>.</summary>
    public const uint RedMatrixColumn = 0x7258595a;
    /// <summary>The red tone-reproduction-curve tag <c>rTRC</c>.</summary>
    public const uint RedTrc = 0x72545243;
    /// <summary>The gray tone-reproduction-curve tag <c>kTRC</c>.</summary>
    public const uint GrayTrc = 0x6b545243;
    /// <summary>The calibration date/time tag <c>calt</c>.</summary>
    public const uint CalibrationDateTime = 0x63616c74;
    /// <summary>The characterization-target tag <c>targ</c>.</summary>
    public const uint CharacterizationTarget = 0x74617267;
    /// <summary>The chromatic-adaptation tag <c>chad</c>.</summary>
    public const uint ChromaticAdaptation = 0x63686164;
    /// <summary>The chromaticity tag <c>chrm</c>.</summary>
    public const uint Chromaticity = 0x6368726d;
    /// <summary>The CICP tag <c>cicp</c>.</summary>
    public const uint Cicp = 0x63696370;
    /// <summary>The colorant-order tag <c>clro</c>.</summary>
    public const uint ColorantOrder = 0x636c726f;
    /// <summary>The ICC.2 output colorant-order tag <c>cloo</c>.</summary>
    public const uint ColorantOrderOut = 0x636c6f6f;
    /// <summary>The colorant-table tag <c>clrt</c>.</summary>
    public const uint ColorantTable = 0x636c7274;
    /// <summary>The output colorant-table tag <c>clot</c>.</summary>
    public const uint ColorantTableOut = 0x636c6f74;
    /// <summary>The ICC.2 input colorant-information tag <c>clin</c>.</summary>
    public const uint ColorantInfo = 0x636c696e;
    /// <summary>The ICC.2 output colorant-information tag <c>clio</c>.</summary>
    public const uint ColorantInfoOut = 0x636c696f;
    /// <summary>The colorimetric-intent image-state tag <c>ciis</c>.</summary>
    public const uint ColorimetricIntentImageState = 0x63696973;
    /// <summary>The copyright tag <c>cprt</c>.</summary>
    public const uint Copyright = 0x63707274;
    /// <summary>The device-manufacturer description tag <c>dmnd</c>.</summary>
    public const uint DeviceManufacturerDescription = 0x646d6e64;
    /// <summary>The device-model description tag <c>dmdd</c>.</summary>
    public const uint DeviceModelDescription = 0x646d6464;
    /// <summary>The ICC.2 device PCC amendment tag <c>dpcc</c>.</summary>
    public const uint DevicePcc = 0x64706363;
    /// <summary>The ICC.2 device spectral-range amendment tag <c>dsrn</c>.</summary>
    public const uint DeviceSpectralRange = 0x6473726e;
    /// <summary>The gamut tag <c>gamt</c>.</summary>
    public const uint Gamut = 0x67616d74;
    /// <summary>The luminance tag <c>lumi</c>.</summary>
    public const uint Luminance = 0x6c756d69;
    /// <summary>The measurement tag <c>meas</c>.</summary>
    public const uint Measurement = 0x6d656173;
    /// <summary>The metadata tag <c>meta</c>.</summary>
    public const uint Metadata = 0x6d657461;
    /// <summary>The media-black-point tag <c>bkpt</c>.</summary>
    public const uint MediaBlackPoint = 0x626b7074;
    /// <summary>The media-white-point tag <c>wtpt</c>.</summary>
    public const uint MediaWhitePoint = 0x77747074;
    /// <summary>The legacy named-colour tag <c>ncol</c>.</summary>
    public const uint NamedColor = 0x6e636f6c;
    /// <summary>The named-colour-2 tag <c>ncl2</c>.</summary>
    public const uint NamedColor2 = 0x6e636c32;
    /// <summary>The output-response tag <c>resp</c>.</summary>
    public const uint OutputResponse = 0x72657370;
    /// <summary>The perceptual rendering-intent gamut tag <c>rig0</c>.</summary>
    public const uint PerceptualRenderingIntentGamut = 0x72696730;
    /// <summary>The saturation rendering-intent gamut tag <c>rig2</c>.</summary>
    public const uint SaturationRenderingIntentGamut = 0x72696732;
    /// <summary>The preview-zero tag <c>pre0</c>.</summary>
    public const uint Preview0 = 0x70726530;
    /// <summary>The preview-one tag <c>pre1</c>.</summary>
    public const uint Preview1 = 0x70726531;
    /// <summary>The preview-two tag <c>pre2</c>.</summary>
    public const uint Preview2 = 0x70726532;
    /// <summary>The profile-description tag <c>desc</c>.</summary>
    public const uint ProfileDescription = 0x64657363;
    /// <summary>The profile-sequence-description tag <c>pseq</c>.</summary>
    public const uint ProfileSequenceDescription = 0x70736571;
    /// <summary>The profile-sequence-identifier tag <c>psid</c>.</summary>
    public const uint ProfileSequenceIdentifier = 0x70736964;
    /// <summary>The technology tag <c>tech</c>.</summary>
    public const uint Technology = 0x74656368;
    /// <summary>The viewing-condition description tag <c>vued</c>.</summary>
    public const uint ViewingConditionDescription = 0x76756564;
    /// <summary>The viewing-conditions tag <c>view</c>.</summary>
    public const uint ViewingConditions = 0x76696577;
    /// <summary>The legacy CRD-information tag <c>crdi</c>.</summary>
    public const uint CrdInfo = 0x63726469;
    /// <summary>The legacy PostScript CRD-zero tag <c>psd0</c>.</summary>
    public const uint PostScriptCrd0 = 0x70736430;
    /// <summary>The legacy PostScript CRD-one tag <c>psd1</c>.</summary>
    public const uint PostScriptCrd1 = 0x70736431;
    /// <summary>The legacy PostScript CRD-two tag <c>psd2</c>.</summary>
    public const uint PostScriptCrd2 = 0x70736432;
    /// <summary>The legacy PostScript CRD-three tag <c>psd3</c>.</summary>
    public const uint PostScriptCrd3 = 0x70736433;
    /// <summary>The legacy PostScript CSA tag <c>ps2s</c>.</summary>
    public const uint PostScriptCsa = 0x70733273;
    /// <summary>The legacy PostScript rendering-intent tag <c>ps2i</c>.</summary>
    public const uint PostScriptRenderingIntent = 0x70733269;
    /// <summary>The legacy screening-description tag <c>scrd</c>.</summary>
    public const uint ScreeningDescription = 0x73637264;
    /// <summary>The legacy screening-data tag <c>scrn</c>.</summary>
    public const uint Screening = 0x7363726e;
    /// <summary>The legacy UCR/BG tag <c>bfd </c>.</summary>
    public const uint UcrBg = 0x62666420;
    /// <summary>The legacy device-settings tag <c>devs</c>.</summary>
    public const uint DeviceSettings = 0x64657673;
    /// <summary>The ICC.1 embedded ICC.2 profile tag <c>ICC5</c>.</summary>
    public const uint EmbeddedIccMaxProfile = 0x49434335;
    /// <summary>The 2025 adaptive-gain-curve tag <c>ADGC</c>.</summary>
    public const uint AdaptiveGainCurve = 0x41444743;
    /// <summary>The Apple video-card gamma-table tag <c>vcgt</c>.</summary>
    public const uint VideoCardGammaTable = 0x76636774;
    /// <summary>The Microsoft HDR calibration tag <c>MHC2</c>.</summary>
    public const uint MicrosoftHdrCalibration = 0x4d484332;
    /// <summary>The first registered Microsoft private calibration tag <c>MS00</c>.</summary>
    public const uint MicrosoftGammaRamp = 0x4d533030;
    /// <summary>The last signature in the registered Microsoft private range, <c>MS99</c>.</summary>
    public const uint MicrosoftPrivateLast = 0x4d533939;
    /// <summary>The ICC.2 colour-encoding parameters tag <c>cept</c>.</summary>
    public const uint ColorEncodingParameters = 0x63657074;
    /// <summary>The ICC.2 colour-space-name tag <c>csnm</c>.</summary>
    public const uint ColorSpaceName = 0x63736e6d;
    /// <summary>The ICC.2 measurement-input-information tag <c>miin</c>.</summary>
    public const uint MeasurementInputInformation = 0x6d69696e;
    /// <summary>The ICC.2 measurement-information tag <c>minf</c>.</summary>
    public const uint MeasurementInformation = 0x6d696e66;
    /// <summary>The ICC.2 named-colours tag <c>nmcl</c>.</summary>
    public const uint NamedColors = 0x6e6d636c;
    /// <summary>The ICC.2 profile-sequence-information tag <c>psin</c>.</summary>
    public const uint ProfileSequenceInformation = 0x7073696e;
    /// <summary>The ICC.2 CxF tag <c>CxF </c>.</summary>
    public const uint Cxf = 0x43784620;
    /// <summary>The ICC.2 reference-name tag <c>rfnm</c>.</summary>
    public const uint ReferenceName = 0x72666e6d;
    /// <summary>The ICC.2 source PCC tag <c>sPCC</c>.</summary>
    public const uint SourcePcc = 0x73504343;
    /// <summary>The ICC.2 custom-to-standard PCC tag <c>c2sp</c>.</summary>
    public const uint CustomToStandardPcc = 0x63327370;
    /// <summary>The ICC.2 standard-to-custom PCC tag <c>s2cp</c>.</summary>
    public const uint StandardToCustomPcc = 0x73326370;
    /// <summary>The ICC.2 spectral-viewing-conditions tag <c>svcn</c>.</summary>
    public const uint SpectralViewingConditions = 0x7376636e;
    /// <summary>The ICC.2 spectral-white-point tag <c>swpt</c>.</summary>
    public const uint SpectralWhitePoint = 0x73777074;
    /// <summary>The ICC.2 surface-map tag <c>smap</c>.</summary>
    public const uint SurfaceMap = 0x736d6170;
    /// <summary>The ICC.2 multiplex-default-values tag <c>mdv </c>.</summary>
    public const uint MultiplexDefaultValues = 0x6d647620;
    /// <summary>The ICC.2 multiplex-type-array tag <c>mcta</c>.</summary>
    public const uint MultiplexTypeArray = 0x6d637461;
    /// <summary>The ICC.2 gamut-boundary zero tag <c>gbd0</c>.</summary>
    public const uint GamutBoundary0 = 0x67626430;
    /// <summary>The ICC.2 gamut-boundary one tag <c>gbd1</c>.</summary>
    public const uint GamutBoundary1 = 0x67626431;
    /// <summary>The ICC.2 gamut-boundary two tag <c>gbd2</c>.</summary>
    public const uint GamutBoundary2 = 0x67626432;
    /// <summary>The ICC.2 gamut-boundary three tag <c>gbd3</c>.</summary>
    public const uint GamutBoundary3 = 0x67626433;
    /// <summary>The ICC.2 HDR-to-SDR zero tag <c>H2S0</c>.</summary>
    public const uint HdrToSdr0 = 0x48325330;
    /// <summary>The ICC.2 HDR-to-SDR one tag <c>H2S1</c>.</summary>
    public const uint HdrToSdr1 = 0x48325331;
    /// <summary>The ICC.2 HDR-to-SDR two tag <c>H2S2</c>.</summary>
    public const uint HdrToSdr2 = 0x48325332;
    /// <summary>The ICC.2 HDR-to-SDR three tag <c>H2S3</c>.</summary>
    public const uint HdrToSdr3 = 0x48325333;
    /// <summary>The ICC.2 BRDF colorimetric-parameter zero tag <c>bcp0</c>.</summary>
    public const uint BrdfColorimetricParameter0 = 0x62637030;
    /// <summary>The ICC.2 BRDF colorimetric-parameter one tag <c>bcp1</c>.</summary>
    public const uint BrdfColorimetricParameter1 = 0x62637031;
    /// <summary>The ICC.2 BRDF colorimetric-parameter two tag <c>bcp2</c>.</summary>
    public const uint BrdfColorimetricParameter2 = 0x62637032;
    /// <summary>The ICC.2 BRDF colorimetric-parameter three tag <c>bcp3</c>.</summary>
    public const uint BrdfColorimetricParameter3 = 0x62637033;
    /// <summary>The ICC.2 BRDF spectral-parameter zero tag <c>bsp0</c>.</summary>
    public const uint BrdfSpectralParameter0 = 0x62737030;
    /// <summary>The ICC.2 BRDF spectral-parameter one tag <c>bsp1</c>.</summary>
    public const uint BrdfSpectralParameter1 = 0x62737031;
    /// <summary>The ICC.2 BRDF spectral-parameter two tag <c>bsp2</c>.</summary>
    public const uint BrdfSpectralParameter2 = 0x62737032;
    /// <summary>The ICC.2 BRDF spectral-parameter three tag <c>bsp3</c>.</summary>
    public const uint BrdfSpectralParameter3 = 0x62737033;
    /// <summary>The ICC.2 BRDF A-to-B zero tag <c>bAB0</c>.</summary>
    public const uint BrdfAToB0 = 0x62414230;
    /// <summary>The ICC.2 BRDF A-to-B one tag <c>bAB1</c>.</summary>
    public const uint BrdfAToB1 = 0x62414231;
    /// <summary>The ICC.2 BRDF A-to-B two tag <c>bAB2</c>.</summary>
    public const uint BrdfAToB2 = 0x62414232;
    /// <summary>The ICC.2 BRDF A-to-B three tag <c>bAB3</c>.</summary>
    public const uint BrdfAToB3 = 0x62414233;
    /// <summary>The ICC.2 BRDF B-to-A zero tag <c>bBA0</c>.</summary>
    public const uint BrdfBToA0 = 0x62424130;
    /// <summary>The ICC.2 BRDF B-to-A one tag <c>bBA1</c>.</summary>
    public const uint BrdfBToA1 = 0x62424131;
    /// <summary>The ICC.2 BRDF B-to-A two tag <c>bBA2</c>.</summary>
    public const uint BrdfBToA2 = 0x62424132;
    /// <summary>The ICC.2 BRDF B-to-A three tag <c>bBA3</c>.</summary>
    public const uint BrdfBToA3 = 0x62424133;
    /// <summary>The ICC.2 BRDF B-to-D zero tag <c>bBD0</c>.</summary>
    public const uint BrdfBToD0 = 0x62424430;
    /// <summary>The ICC.2 BRDF B-to-D one tag <c>bBD1</c>.</summary>
    public const uint BrdfBToD1 = 0x62424431;
    /// <summary>The ICC.2 BRDF B-to-D two tag <c>bBD2</c>.</summary>
    public const uint BrdfBToD2 = 0x62424432;
    /// <summary>The ICC.2 BRDF B-to-D three tag <c>bBD3</c>.</summary>
    public const uint BrdfBToD3 = 0x62424433;
    /// <summary>The ICC.2 BRDF D-to-B zero tag <c>bDB0</c>.</summary>
    public const uint BrdfDToB0 = 0x62444230;
    /// <summary>The ICC.2 BRDF D-to-B one tag <c>bDB1</c>.</summary>
    public const uint BrdfDToB1 = 0x62444231;
    /// <summary>The ICC.2 BRDF D-to-B two tag <c>bDB2</c>.</summary>
    public const uint BrdfDToB2 = 0x62444232;
    /// <summary>The ICC.2 BRDF D-to-B three tag <c>bDB3</c>.</summary>
    public const uint BrdfDToB3 = 0x62444233;
    /// <summary>The ICC.2 BRDF M-to-B zero tag <c>bMB0</c>.</summary>
    public const uint BrdfMToB0 = 0x624d4230;
    /// <summary>The ICC.2 BRDF M-to-B one tag <c>bMB1</c>.</summary>
    public const uint BrdfMToB1 = 0x624d4231;
    /// <summary>The ICC.2 BRDF M-to-B two tag <c>bMB2</c>.</summary>
    public const uint BrdfMToB2 = 0x624d4232;
    /// <summary>The ICC.2 BRDF M-to-B three tag <c>bMB3</c>.</summary>
    public const uint BrdfMToB3 = 0x624d4233;
    /// <summary>The ICC.2 BRDF M-to-S zero tag <c>bMS0</c>.</summary>
    public const uint BrdfMToS0 = 0x624d5330;
    /// <summary>The ICC.2 BRDF M-to-S one tag <c>bMS1</c>.</summary>
    public const uint BrdfMToS1 = 0x624d5331;
    /// <summary>The ICC.2 BRDF M-to-S two tag <c>bMS2</c>.</summary>
    public const uint BrdfMToS2 = 0x624d5332;
    /// <summary>The ICC.2 BRDF M-to-S three tag <c>bMS3</c>.</summary>
    public const uint BrdfMToS3 = 0x624d5333;
    /// <summary>The ICC.2 directional A-to-B zero tag <c>dAB0</c>.</summary>
    public const uint DirectionalAToB0 = 0x64414230;
    /// <summary>The ICC.2 directional A-to-B one tag <c>dAB1</c>.</summary>
    public const uint DirectionalAToB1 = 0x64414231;
    /// <summary>The ICC.2 directional A-to-B two tag <c>dAB2</c>.</summary>
    public const uint DirectionalAToB2 = 0x64414232;
    /// <summary>The ICC.2 directional A-to-B three tag <c>dAB3</c>.</summary>
    public const uint DirectionalAToB3 = 0x64414233;
    /// <summary>The ICC.2 directional B-to-A zero tag <c>dBA0</c>.</summary>
    public const uint DirectionalBToA0 = 0x64424130;
    /// <summary>The ICC.2 directional B-to-A one tag <c>dBA1</c>.</summary>
    public const uint DirectionalBToA1 = 0x64424131;
    /// <summary>The ICC.2 directional B-to-A two tag <c>dBA2</c>.</summary>
    public const uint DirectionalBToA2 = 0x64424132;
    /// <summary>The ICC.2 directional B-to-A three tag <c>dBA3</c>.</summary>
    public const uint DirectionalBToA3 = 0x64424133;
    /// <summary>The ICC.2 directional B-to-D zero tag <c>dBD0</c>.</summary>
    public const uint DirectionalBToD0 = 0x64424430;
    /// <summary>The ICC.2 directional B-to-D one tag <c>dBD1</c>.</summary>
    public const uint DirectionalBToD1 = 0x64424431;
    /// <summary>The ICC.2 directional B-to-D two tag <c>dBD2</c>.</summary>
    public const uint DirectionalBToD2 = 0x64424432;
    /// <summary>The ICC.2 directional B-to-D three tag <c>dBD3</c>.</summary>
    public const uint DirectionalBToD3 = 0x64424433;
    /// <summary>The ICC.2 directional D-to-B zero tag <c>dDB0</c>.</summary>
    public const uint DirectionalDToB0 = 0x64444230;
    /// <summary>The ICC.2 directional D-to-B one tag <c>dDB1</c>.</summary>
    public const uint DirectionalDToB1 = 0x64444231;
    /// <summary>The ICC.2 directional D-to-B two tag <c>dDB2</c>.</summary>
    public const uint DirectionalDToB2 = 0x64444232;
    /// <summary>The ICC.2 directional D-to-B three tag <c>dDB3</c>.</summary>
    public const uint DirectionalDToB3 = 0x64444233;
}
