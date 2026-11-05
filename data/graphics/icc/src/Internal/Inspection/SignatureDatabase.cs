namespace icc;

internal static class SignatureDatabase
{
    internal static readonly System.Collections.Generic.Dictionary<uint, string> ProfileClasses = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccProfileClassSignatures.Input, "Input" },
        { IccProfileClassSignatures.Display, "Display" },
        { IccProfileClassSignatures.Output, "Output" },
        { IccProfileClassSignatures.DeviceLink, "Device Link" },
        { IccProfileClassSignatures.ColorSpace, "Color Space" },
        { IccProfileClassSignatures.Abstract, "Abstract" },
        { IccProfileClassSignatures.NamedColor, "Named Color" },
        { IccProfileClassSignatures.ColorEncodingSpace, "Color Encoding Space" },
        { IccProfileClassSignatures.MultiplexIdentification, "Multiplex Identification" },
        { IccProfileClassSignatures.MultiplexLink, "Multiplex Link" },
        { IccProfileClassSignatures.MultiplexVisualization, "Multiplex Visualization" },
    };

    internal static readonly System.Collections.Generic.Dictionary<uint, string> ColorSpaces = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccColorSpaceSignatures.Xyz, "Xyz" },
        { IccColorSpaceSignatures.Lab, "Lab" },
        { IccColorSpaceSignatures.Luv, "Luv" },
        { IccColorSpaceSignatures.YCbCr, "Y Cb Cr" },
        { IccColorSpaceSignatures.Yxy, "Yxy" },
        { IccColorSpaceSignatures.Rgb, "Rgb" },
        { IccColorSpaceSignatures.Gray, "Gray" },
        { IccColorSpaceSignatures.Hsv, "Hsv" },
        { IccColorSpaceSignatures.Hls, "Hls" },
        { IccColorSpaceSignatures.Cmyk, "Cmyk" },
        { IccColorSpaceSignatures.Cmy, "Cmy" },
        { IccColorSpaceSignatures.NamedColor, "Named Color" },
        { IccColorSpaceSignatures.OneColor, "One Color" },
        { IccColorSpaceSignatures.TwoColor, "Two Color" },
        { IccColorSpaceSignatures.ThreeColor, "Three Color" },
        { IccColorSpaceSignatures.FourColor, "Four Color" },
        { IccColorSpaceSignatures.FiveColor, "Five Color" },
        { IccColorSpaceSignatures.SixColor, "Six Color" },
        { IccColorSpaceSignatures.SevenColor, "Seven Color" },
        { IccColorSpaceSignatures.EightColor, "Eight Color" },
        { IccColorSpaceSignatures.NineColor, "Nine Color" },
        { IccColorSpaceSignatures.TenColor, "Ten Color" },
        { IccColorSpaceSignatures.ElevenColor, "Eleven Color" },
        { IccColorSpaceSignatures.TwelveColor, "Twelve Color" },
        { IccColorSpaceSignatures.ThirteenColor, "Thirteen Color" },
        { IccColorSpaceSignatures.FourteenColor, "Fourteen Color" },
        { IccColorSpaceSignatures.FifteenColor, "Fifteen Color" },
        { IccColorSpaceSignatures.ReflectanceSpectralPrefix, "Reflectance Spectral Prefix" },
        { IccColorSpaceSignatures.TransmissionSpectralPrefix, "Transmission Spectral Prefix" },
        { IccColorSpaceSignatures.RadiantSpectralPrefix, "Radiant Spectral Prefix" },
        { IccColorSpaceSignatures.BiSpectralPrefix, "Bi Spectral Prefix" },
        { IccColorSpaceSignatures.SparseMatrixSpectralPrefix, "Sparse Matrix Spectral Prefix" },
        { IccColorSpaceSignatures.MultiplexPrefix, "Multiplex Prefix" },
    };

    internal static readonly System.Collections.Generic.Dictionary<uint, string> Tags = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccTagSignatures.AToB0, "A To B 0" },
        { IccTagSignatures.AToB1, "A To B 1" },
        { IccTagSignatures.AToB2, "A To B 2" },
        { IccTagSignatures.AToB3, "A To B 3" },
        { IccTagSignatures.BToA0, "B To A 0" },
        { IccTagSignatures.BToA1, "B To A 1" },
        { IccTagSignatures.BToA2, "B To A 2" },
        { IccTagSignatures.BToA3, "B To A 3" },
        { IccTagSignatures.BToD0, "B To D 0" },
        { IccTagSignatures.BToD1, "B To D 1" },
        { IccTagSignatures.BToD2, "B To D 2" },
        { IccTagSignatures.BToD3, "B To D 3" },
        { IccTagSignatures.DToB0, "D To B 0" },
        { IccTagSignatures.DToB1, "D To B 1" },
        { IccTagSignatures.DToB2, "D To B 2" },
        { IccTagSignatures.DToB3, "D To B 3" },
        { IccTagSignatures.AToM0, "A To M 0" },
        { IccTagSignatures.MToA0, "M To A 0" },
        { IccTagSignatures.MToB0, "M To B 0" },
        { IccTagSignatures.MToB1, "M To B 1" },
        { IccTagSignatures.MToB2, "M To B 2" },
        { IccTagSignatures.MToB3, "M To B 3" },
        { IccTagSignatures.MToS0, "M To S 0" },
        { IccTagSignatures.MToS1, "M To S 1" },
        { IccTagSignatures.MToS2, "M To S 2" },
        { IccTagSignatures.MToS3, "M To S 3" },
        { IccTagSignatures.BlueMatrixColumn, "Blue Matrix Column" },
        { IccTagSignatures.BlueTrc, "Blue Trc" },
        { IccTagSignatures.GreenMatrixColumn, "Green Matrix Column" },
        { IccTagSignatures.GreenTrc, "Green Trc" },
        { IccTagSignatures.RedMatrixColumn, "Red Matrix Column" },
        { IccTagSignatures.RedTrc, "Red Trc" },
        { IccTagSignatures.GrayTrc, "Gray Trc" },
        { IccTagSignatures.CalibrationDateTime, "Calibration Date Time" },
        { IccTagSignatures.CharacterizationTarget, "Characterization Target" },
        { IccTagSignatures.ChromaticAdaptation, "Chromatic Adaptation" },
        { IccTagSignatures.Chromaticity, "Chromaticity" },
        { IccTagSignatures.Cicp, "Cicp" },
        { IccTagSignatures.ColorantOrder, "Colorant Order" },
        { IccTagSignatures.ColorantOrderOut, "Colorant Order Out" },
        { IccTagSignatures.ColorantTable, "Colorant Table" },
        { IccTagSignatures.ColorantTableOut, "Colorant Table Out" },
        { IccTagSignatures.ColorantInfo, "Colorant Info" },
        { IccTagSignatures.ColorantInfoOut, "Colorant Info Out" },
        { IccTagSignatures.ColorimetricIntentImageState, "Colorimetric Intent Image State" },
        { IccTagSignatures.Copyright, "Copyright" },
        { IccTagSignatures.DeviceManufacturerDescription, "Device Manufacturer Description" },
        { IccTagSignatures.DeviceModelDescription, "Device Model Description" },
        { IccTagSignatures.Gamut, "Gamut" },
        { IccTagSignatures.Luminance, "Luminance" },
        { IccTagSignatures.Measurement, "Measurement" },
        { IccTagSignatures.Metadata, "Metadata" },
        { IccTagSignatures.MediaBlackPoint, "Media Black Point" },
        { IccTagSignatures.MediaWhitePoint, "Media White Point" },
        { IccTagSignatures.NamedColor, "Named Color" },
        { IccTagSignatures.NamedColor2, "Named Color 2" },
        { IccTagSignatures.OutputResponse, "Output Response" },
        { IccTagSignatures.PerceptualRenderingIntentGamut, "Perceptual Rendering Intent Gamut" },
        { IccTagSignatures.SaturationRenderingIntentGamut, "Saturation Rendering Intent Gamut" },
        { IccTagSignatures.Preview0, "Preview 0" },
        { IccTagSignatures.Preview1, "Preview 1" },
        { IccTagSignatures.Preview2, "Preview 2" },
        { IccTagSignatures.ProfileDescription, "Profile Description" },
        { IccTagSignatures.ProfileSequenceDescription, "Profile Sequence Description" },
        { IccTagSignatures.ProfileSequenceIdentifier, "Profile Sequence Identifier" },
        { IccTagSignatures.Technology, "Technology" },
        { IccTagSignatures.ViewingConditionDescription, "Viewing Condition Description" },
        { IccTagSignatures.ViewingConditions, "Viewing Conditions" },
        { IccTagSignatures.CrdInfo, "Crd Info" },
        { IccTagSignatures.PostScriptCrd0, "Post Script Crd 0" },
        { IccTagSignatures.PostScriptCrd1, "Post Script Crd 1" },
        { IccTagSignatures.PostScriptCrd2, "Post Script Crd 2" },
        { IccTagSignatures.PostScriptCrd3, "Post Script Crd 3" },
        { IccTagSignatures.PostScriptCsa, "Post Script Csa" },
        { IccTagSignatures.PostScriptRenderingIntent, "Post Script Rendering Intent" },
        { IccTagSignatures.ScreeningDescription, "Screening Description" },
        { IccTagSignatures.Screening, "Screening" },
        { IccTagSignatures.UcrBg, "Ucr Bg" },
        { IccTagSignatures.DeviceSettings, "Device Settings" },
        { IccTagSignatures.DevicePcc, "Device Pcc" },
        { IccTagSignatures.DeviceSpectralRange, "Device Spectral Range" },
        { IccTagSignatures.EmbeddedIccMaxProfile, "Embedded Icc Max Profile" },
        { IccTagSignatures.AdaptiveGainCurve, "Adaptive Gain Curve" },
        { IccTagSignatures.VideoCardGammaTable, "Video Card Gamma Table" },
        { IccTagSignatures.MicrosoftHdrCalibration, "Microsoft Hdr Calibration" },
        { IccTagSignatures.MicrosoftGammaRamp, "Microsoft Gamma Ramp" },
        { IccTagSignatures.ColorEncodingParameters, "Color Encoding Parameters" },
        { IccTagSignatures.ColorSpaceName, "Color Space Name" },
        { IccTagSignatures.MeasurementInputInformation, "Measurement Input Information" },
        { IccTagSignatures.MeasurementInformation, "Measurement Information" },
        { IccTagSignatures.NamedColors, "Named Colors" },
        { IccTagSignatures.ProfileSequenceInformation, "Profile Sequence Information" },
        { IccTagSignatures.Cxf, "Cxf" },
        { IccTagSignatures.ReferenceName, "Reference Name" },
        { IccTagSignatures.SourcePcc, "Source Pcc" },
        { IccTagSignatures.CustomToStandardPcc, "Custom To Standard Pcc" },
        { IccTagSignatures.StandardToCustomPcc, "Standard To Custom Pcc" },
        { IccTagSignatures.SpectralViewingConditions, "Spectral Viewing Conditions" },
        { IccTagSignatures.SpectralWhitePoint, "Spectral White Point" },
        { IccTagSignatures.SurfaceMap, "Surface Map" },
        { IccTagSignatures.MultiplexDefaultValues, "Multiplex Default Values" },
        { IccTagSignatures.MultiplexTypeArray, "Multiplex Type Array" },
        { IccTagSignatures.GamutBoundary0, "Gamut Boundary 0" },
        { IccTagSignatures.GamutBoundary1, "Gamut Boundary 1" },
        { IccTagSignatures.GamutBoundary2, "Gamut Boundary 2" },
        { IccTagSignatures.GamutBoundary3, "Gamut Boundary 3" },
        { IccTagSignatures.HdrToSdr0, "Hdr To Sdr 0" },
        { IccTagSignatures.HdrToSdr1, "Hdr To Sdr 1" },
        { IccTagSignatures.HdrToSdr2, "Hdr To Sdr 2" },
        { IccTagSignatures.HdrToSdr3, "Hdr To Sdr 3" },
        { IccTagSignatures.BrdfColorimetricParameter0, "Brdf Colorimetric Parameter 0" },
        { IccTagSignatures.BrdfColorimetricParameter1, "Brdf Colorimetric Parameter 1" },
        { IccTagSignatures.BrdfColorimetricParameter2, "Brdf Colorimetric Parameter 2" },
        { IccTagSignatures.BrdfColorimetricParameter3, "Brdf Colorimetric Parameter 3" },
        { IccTagSignatures.BrdfSpectralParameter0, "Brdf Spectral Parameter 0" },
        { IccTagSignatures.BrdfSpectralParameter1, "Brdf Spectral Parameter 1" },
        { IccTagSignatures.BrdfSpectralParameter2, "Brdf Spectral Parameter 2" },
        { IccTagSignatures.BrdfSpectralParameter3, "Brdf Spectral Parameter 3" },
        { IccTagSignatures.BrdfAToB0, "Brdf A To B 0" },
        { IccTagSignatures.BrdfAToB1, "Brdf A To B 1" },
        { IccTagSignatures.BrdfAToB2, "Brdf A To B 2" },
        { IccTagSignatures.BrdfAToB3, "Brdf A To B 3" },
        { IccTagSignatures.BrdfBToA0, "Brdf B To A 0" },
        { IccTagSignatures.BrdfBToA1, "Brdf B To A 1" },
        { IccTagSignatures.BrdfBToA2, "Brdf B To A 2" },
        { IccTagSignatures.BrdfBToA3, "Brdf B To A 3" },
        { IccTagSignatures.BrdfBToD0, "Brdf B To D 0" },
        { IccTagSignatures.BrdfBToD1, "Brdf B To D 1" },
        { IccTagSignatures.BrdfBToD2, "Brdf B To D 2" },
        { IccTagSignatures.BrdfBToD3, "Brdf B To D 3" },
        { IccTagSignatures.BrdfDToB0, "Brdf D To B 0" },
        { IccTagSignatures.BrdfDToB1, "Brdf D To B 1" },
        { IccTagSignatures.BrdfDToB2, "Brdf D To B 2" },
        { IccTagSignatures.BrdfDToB3, "Brdf D To B 3" },
        { IccTagSignatures.BrdfMToB0, "Brdf M To B 0" },
        { IccTagSignatures.BrdfMToB1, "Brdf M To B 1" },
        { IccTagSignatures.BrdfMToB2, "Brdf M To B 2" },
        { IccTagSignatures.BrdfMToB3, "Brdf M To B 3" },
        { IccTagSignatures.BrdfMToS0, "Brdf M To S 0" },
        { IccTagSignatures.BrdfMToS1, "Brdf M To S 1" },
        { IccTagSignatures.BrdfMToS2, "Brdf M To S 2" },
        { IccTagSignatures.BrdfMToS3, "Brdf M To S 3" },
        { IccTagSignatures.DirectionalAToB0, "Directional A To B 0" },
        { IccTagSignatures.DirectionalAToB1, "Directional A To B 1" },
        { IccTagSignatures.DirectionalAToB2, "Directional A To B 2" },
        { IccTagSignatures.DirectionalAToB3, "Directional A To B 3" },
        { IccTagSignatures.DirectionalBToA0, "Directional B To A 0" },
        { IccTagSignatures.DirectionalBToA1, "Directional B To A 1" },
        { IccTagSignatures.DirectionalBToA2, "Directional B To A 2" },
        { IccTagSignatures.DirectionalBToA3, "Directional B To A 3" },
        { IccTagSignatures.DirectionalBToD0, "Directional B To D 0" },
        { IccTagSignatures.DirectionalBToD1, "Directional B To D 1" },
        { IccTagSignatures.DirectionalBToD2, "Directional B To D 2" },
        { IccTagSignatures.DirectionalBToD3, "Directional B To D 3" },
        { IccTagSignatures.DirectionalDToB0, "Directional D To B 0" },
        { IccTagSignatures.DirectionalDToB1, "Directional D To B 1" },
        { IccTagSignatures.DirectionalDToB2, "Directional D To B 2" },
        { IccTagSignatures.DirectionalDToB3, "Directional D To B 3" },
    };

    internal static readonly System.Collections.Generic.Dictionary<uint, string> Types = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccTypeSignatures.Chromaticity, "Chromaticity" },
        { IccTypeSignatures.Cicp, "Cicp" },
        { IccTypeSignatures.ColorantOrder, "Colorant Order" },
        { IccTypeSignatures.ColorantTable, "Colorant Table" },
        { IccTypeSignatures.Curve, "Curve" },
        { IccTypeSignatures.Data, "Data" },
        { IccTypeSignatures.DateTime, "Date Time" },
        { IccTypeSignatures.Dictionary, "Dictionary" },
        { IccTypeSignatures.Lut8, "Lut 8" },
        { IccTypeSignatures.Lut16, "Lut 16" },
        { IccTypeSignatures.LutAToB, "Lut A To B" },
        { IccTypeSignatures.LutBToA, "Lut B To A" },
        { IccTypeSignatures.Measurement, "Measurement" },
        { IccTypeSignatures.MultiLocalizedUnicode, "Multi Localized Unicode" },
        { IccTypeSignatures.MultiProcessElements, "Multi Process Elements" },
        { IccTypeSignatures.NamedColor2, "Named Color 2" },
        { IccTypeSignatures.ParametricCurve, "Parametric Curve" },
        { IccTypeSignatures.ProfileSequenceDescription, "Profile Sequence Description" },
        { IccTypeSignatures.ProfileSequenceIdentifier, "Profile Sequence Identifier" },
        { IccTypeSignatures.ResponseCurveSet16, "Response Curve Set 16" },
        { IccTypeSignatures.S15Fixed16Array, "S 15 Fixed 16 Array" },
        { IccTypeSignatures.Signature, "Signature" },
        { IccTypeSignatures.Text, "Text" },
        { IccTypeSignatures.U16Fixed16Array, "U 16 Fixed 16 Array" },
        { IccTypeSignatures.UInt8Array, "U Int 8 Array" },
        { IccTypeSignatures.UInt16Array, "U Int 16 Array" },
        { IccTypeSignatures.UInt32Array, "U Int 32 Array" },
        { IccTypeSignatures.UInt64Array, "U Int 64 Array" },
        { IccTypeSignatures.ViewingConditions, "Viewing Conditions" },
        { IccTypeSignatures.Xyz, "Xyz" },
        { IccTypeSignatures.TextDescription, "Text Description" },
        { IccTypeSignatures.CrdInfo, "Crd Info" },
        { IccTypeSignatures.NamedColor, "Named Color" },
        { IccTypeSignatures.Screening, "Screening" },
        { IccTypeSignatures.UcrBg, "Ucr Bg" },
        { IccTypeSignatures.DeviceSettings, "Device Settings" },
        { IccTypeSignatures.EmbeddedProfile, "Embedded Profile" },
        { IccTypeSignatures.AdaptiveGainCurve, "Adaptive Gain Curve" },
        { IccTypeSignatures.VideoCardGammaTable, "Video Card Gamma Table" },
        { IccTypeSignatures.MicrosoftHardwareCalibration, "Microsoft Hardware Calibration" },
        { IccTypeSignatures.Float16Array, "Float 16 Array" },
        { IccTypeSignatures.Float32Array, "Float 32 Array" },
        { IccTypeSignatures.Float64Array, "Float 64 Array" },
        { IccTypeSignatures.GamutBoundaryDescription, "Gamut Boundary Description" },
        { IccTypeSignatures.SparseMatrixArray, "Sparse Matrix Array" },
        { IccTypeSignatures.SpectralViewingConditions, "Spectral Viewing Conditions" },
        { IccTypeSignatures.SpectralRange, "Spectral Range" },
        { IccTypeSignatures.TagArray, "Tag Array" },
        { IccTypeSignatures.TagStructure, "Tag Structure" },
        { IccTypeSignatures.Utf8, "Utf 8" },
        { IccTypeSignatures.Utf16, "Utf 16" },
        { IccTypeSignatures.ZippedUtf8, "Zipped Utf 8" },
        { IccTypeSignatures.ZippedXml, "Zipped Xml" },
        { IccTypeSignatures.ZippedXmlUppercase, "Zipped Xml (alternate signature)" },
        { IccTypeSignatures.EmbeddedHeightImage, "Embedded Height Image" },
        { IccTypeSignatures.EmbeddedNormalImage, "Embedded Normal Image" },
    };

    internal static readonly System.Collections.Generic.Dictionary<uint, string> ProcessingElements = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccProcessingElementSignatures.CurveSet, "Curve Set" },
        { IccProcessingElementSignatures.Matrix, "Matrix" },
        { IccProcessingElementSignatures.Clut, "Clut" },
        { IccProcessingElementSignatures.BeginAcs, "Begin Acs" },
        { IccProcessingElementSignatures.EndAcs, "End Acs" },
        { IccProcessingElementSignatures.Calculator, "Calculator" },
        { IccProcessingElementSignatures.ExtendedClut, "Extended Clut" },
        { IccProcessingElementSignatures.SparseMatrix, "Sparse Matrix" },
        { IccProcessingElementSignatures.TintArray, "Tint Array" },
        { IccProcessingElementSignatures.ToneMap, "Tone Map" },
        { IccProcessingElementSignatures.XyzToJab, "Xyz To Jab" },
        { IccProcessingElementSignatures.JabToXyz, "Jab To Xyz" },
        { IccProcessingElementSignatures.EmissionClut, "Emission Clut" },
        { IccProcessingElementSignatures.EmissionMatrix, "Emission Matrix" },
        { IccProcessingElementSignatures.EmissionObserver, "Emission Observer" },
        { IccProcessingElementSignatures.InverseEmissionMatrix, "Inverse Emission Matrix" },
        { IccProcessingElementSignatures.ReflectanceClut, "Reflectance Clut" },
        { IccProcessingElementSignatures.ReflectanceObserver, "Reflectance Observer" },
    };

    internal static readonly System.Collections.Generic.Dictionary<uint, string> Arrays = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccArraySignatures.Generic, "Generic" },
        { IccArraySignatures.ColorantInformation, "Colorant Information" },
        { IccArraySignatures.NamedColor, "Named Color" },
        { IccArraySignatures.ProfileInformation, "Profile Information" },
        { IccArraySignatures.MultiplexType, "Multiplex Type" },
    };

    internal static readonly System.Collections.Generic.Dictionary<uint, string> Structures = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccStructureSignatures.BrdfTransform, "Brdf Transform" },
        { IccStructureSignatures.ColorEncodingParameters, "Color Encoding Parameters" },
        { IccStructureSignatures.ColorantInformation, "Colorant Information" },
        { IccStructureSignatures.MeasurementInformation, "Measurement Information" },
        { IccStructureSignatures.NamedColor, "Named Color" },
        { IccStructureSignatures.ProfileConnectionConditions, "Profile Connection Conditions" },
        { IccStructureSignatures.ProfileInformation, "Profile Information" },
        { IccStructureSignatures.TintZero, "Tint Zero" },
    };

    internal static readonly System.Collections.Generic.Dictionary<uint, string> Technologies = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccTechnologySignatures.DigitalCamera, "Digital Camera" },
        { IccTechnologySignatures.FilmScanner, "Film Scanner" },
        { IccTechnologySignatures.ReflectiveScanner, "Reflective Scanner" },
        { IccTechnologySignatures.InkJetPrinter, "Ink Jet Printer" },
        { IccTechnologySignatures.ThermalWaxPrinter, "Thermal Wax Printer" },
        { IccTechnologySignatures.ElectrophotographicPrinter, "Electrophotographic Printer" },
        { IccTechnologySignatures.ElectrostaticPrinter, "Electrostatic Printer" },
        { IccTechnologySignatures.DyeSublimationPrinter, "Dye Sublimation Printer" },
        { IccTechnologySignatures.PhotographicPaperPrinter, "Photographic Paper Printer" },
        { IccTechnologySignatures.FilmWriter, "Film Writer" },
        { IccTechnologySignatures.VideoMonitor, "Video Monitor" },
        { IccTechnologySignatures.VideoCamera, "Video Camera" },
        { IccTechnologySignatures.ProjectionTelevision, "Projection Television" },
        { IccTechnologySignatures.CrtDisplay, "Crt Display" },
        { IccTechnologySignatures.PassiveMatrixDisplay, "Passive Matrix Display" },
        { IccTechnologySignatures.ActiveMatrixDisplay, "Active Matrix Display" },
        { IccTechnologySignatures.PhotoCd, "Photo Cd" },
        { IccTechnologySignatures.PhotoImageSetter, "Photo Image Setter" },
        { IccTechnologySignatures.Gravure, "Gravure" },
        { IccTechnologySignatures.OffsetLithography, "Offset Lithography" },
        { IccTechnologySignatures.Silkscreen, "Silkscreen" },
        { IccTechnologySignatures.Flexography, "Flexography" },
        { IccTechnologySignatures.MotionPictureFilmScanner, "Motion Picture Film Scanner" },
        { IccTechnologySignatures.MotionPictureFilmRecorder, "Motion Picture Film Recorder" },
        { IccTechnologySignatures.DigitalMotionPictureCamera, "Digital Motion Picture Camera" },
        { IccTechnologySignatures.DigitalCinemaProjector, "Digital Cinema Projector" },
    };

    internal static readonly System.Collections.Generic.Dictionary<uint, string> ImageStates = new System.Collections.Generic.Dictionary<uint, string>
    {
        { IccImageStateSignatures.SceneColorimetryEstimates, "Scene Colorimetry Estimates" },
        { IccImageStateSignatures.SceneAppearanceEstimates, "Scene Appearance Estimates" },
        { IccImageStateSignatures.FocalPlaneColorimetryEstimates, "Focal Plane Colorimetry Estimates" },
        { IccImageStateSignatures.ReflectionHardcopyOriginalColorimetry, "Reflection Hardcopy Original Colorimetry" },
        { IccImageStateSignatures.ReflectionPrintOutputColorimetry, "Reflection Print Output Colorimetry" },
    };

    internal static string DescribeProfileClass(uint value)
    {
        return Describe(ProfileClasses, "profile class", value);
    }

    internal static string DescribeColorSpace(uint value)
    {
        uint prefix = value & 0xffff0000;
        if (prefix == IccColorSpaceSignatures.ReflectanceSpectralPrefix)
        {
            return DescribeDynamic("Reflectance Spectral", value);
        }
        if (prefix == IccColorSpaceSignatures.TransmissionSpectralPrefix)
        {
            return DescribeDynamic("Transmission Spectral", value);
        }
        if (prefix == IccColorSpaceSignatures.RadiantSpectralPrefix)
        {
            return DescribeDynamic("Radiant Spectral", value);
        }
        if (prefix == IccColorSpaceSignatures.BiSpectralPrefix)
        {
            return DescribeDynamic("Bi-Spectral", value);
        }
        if (prefix == IccColorSpaceSignatures.SparseMatrixSpectralPrefix)
        {
            return DescribeDynamic("Sparse-Matrix Spectral", value);
        }
        if (prefix == IccColorSpaceSignatures.MultiplexPrefix)
        {
            return DescribeDynamic("Multiplex", value);
        }
        return Describe(ColorSpaces, "colour space", value);
    }

    internal static string DescribeTag(uint value)
    {
        if (Tags.ContainsKey(value))
        {
            return Describe(Tags, "tag", value);
        }

        if (VendorTagCodec.IsMicrosoftPrivateSignature(value))
        {
            return DescribeDynamic("Registered Microsoft Private Tag", value);
        }

        return Describe(Tags, "tag", value);
    }

    internal static string DescribeType(uint value)
    {
        return Describe(Types, "type", value);
    }

    internal static string DescribeProcessingElement(uint value)
    {
        return Describe(ProcessingElements, "processing element", value);
    }

    internal static string DescribeArray(uint value)
    {
        return Describe(Arrays, "array", value);
    }

    internal static string DescribeStructure(uint value)
    {
        return Describe(Structures, "structure", value);
    }

    internal static string DescribeTechnology(uint value)
    {
        return Describe(Technologies, "technology", value);
    }

    internal static string DescribeImageState(uint value)
    {
        return Describe(ImageStates, "image state", value);
    }

    internal static string Describe(System.Collections.Generic.Dictionary<uint, string> entries, string category, uint value)
    {
        if (entries.ContainsKey(value))
        {
            return FormatDescription(entries[value], value);
        }
        return FormatDescription($"Unknown/private {category} signature", value);
    }

    internal static string DescribeDynamic(string name, uint value)
    {
        return FormatDescription(name, value);
    }

    internal static string FormatDescription(string name, uint value)
    {
        string formatted = FourCcCodec.Format(value);
        string hexadecimal = FourCcCodec.FormatHex(value);
        if (formatted == hexadecimal)
        {
            return $"{name} ({hexadecimal})";
        }
        return $"{name} ('{formatted}', {hexadecimal})";
    }
}
