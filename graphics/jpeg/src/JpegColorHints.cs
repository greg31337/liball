namespace jpeg;

internal class JpegIccCicpParser
{
    internal JpegCicpMetadata Parse(JpegIccTag tag)
    {
        if (tag == null)
        {
            throw new System.ArgumentNullException("tag");
        }

        JpegCicpMetadata cicp = new JpegCicpMetadata();
        cicp.IsPresent = true;
        cicp.RawPayload = new byte[tag.Payload.Length];
        System.Array.Copy(tag.Payload, cicp.RawPayload, tag.Payload.Length);
        if (tag.Payload.Length < 12)
        {
            return cicp;
        }

        cicp.ColorPrimaries = tag.Payload[8];
        cicp.TransferCharacteristics = tag.Payload[9];
        cicp.MatrixCoefficients = tag.Payload[10];
        cicp.VideoFullRangeFlag = tag.Payload[11];
        cicp.IsValid = tag.TypeSignature == "cicp"
            && tag.Payload.Length == 12
            && tag.Payload[4] == 0
            && tag.Payload[5] == 0
            && tag.Payload[6] == 0
            && tag.Payload[7] == 0
            && cicp.VideoFullRangeFlag <= 1;
        cicp.FullRange = cicp.IsValid && cicp.VideoFullRangeFlag == 1;
        return cicp;
    }
}

internal class JpegColorHintResolver
{
    internal JpegColorHintMetadata Resolve(JpegInfo info)
    {
        if (info == null)
        {
            throw new System.ArgumentNullException("info");
        }

        JpegColorHintMetadata hints = new JpegColorHintMetadata();
        CopyComponentIdentifiers(info, hints);
        JpegColorSpace componentCount = ResolveComponentCount(info.Frame.ComponentCount);
        JpegColorSpace componentIdentifiers = ResolveComponentIdentifiers(info.Frame);
        if (componentIdentifiers != JpegColorSpace.Unknown)
        {
            hints.HasComponentIdentifierColorSpace = true;
            hints.ComponentIdentifierColorSpace = componentIdentifiers;
        }

        JpegColorSpace jfif = ResolveJfif(info);
        if (jfif != JpegColorSpace.Unknown)
        {
            hints.HasJfifColorSpace = true;
            hints.JfifColorSpace = jfif;
        }

        JpegColorSpace exif = ResolveExif(info);
        if (exif != JpegColorSpace.Unknown)
        {
            hints.HasExifColorSpace = true;
            hints.ExifColorSpace = exif;
        }

        JpegColorSpace adobe = ResolveAdobe(info);
        if (adobe != JpegColorSpace.Unknown)
        {
            hints.HasAdobeColorSpace = true;
            hints.AdobeColorSpace = adobe;
        }

        JpegColorSpace cicp = ResolveCicp(info);
        if (cicp != JpegColorSpace.Unknown)
        {
            hints.HasCicpColorSpace = true;
            hints.CicpColorSpace = cicp;
        }

        JpegColorSpace icc = ResolveIcc(info);
        if (icc != JpegColorSpace.Unknown)
        {
            hints.HasIccColorSpace = true;
            hints.IccColorSpace = icc;
        }

        hints.EncodedColorSpace = componentCount;
        if (componentCount != JpegColorSpace.Unknown)
        {
            hints.EncodedColorSpaceSource = JpegColorHintSource.ComponentCount;
        }
        if (hints.HasComponentIdentifierColorSpace)
        {
            hints.EncodedColorSpace = hints.ComponentIdentifierColorSpace;
            hints.EncodedColorSpaceSource = JpegColorHintSource.ComponentIdentifiers;
        }
        if (hints.HasJfifColorSpace)
        {
            hints.EncodedColorSpace = hints.JfifColorSpace;
            hints.EncodedColorSpaceSource = JpegColorHintSource.Jfif;
        }
        if (hints.HasCicpColorSpace)
        {
            hints.EncodedColorSpace = hints.CicpColorSpace;
            hints.EncodedColorSpaceSource = JpegColorHintSource.Cicp;
        }
        if (hints.HasAdobeColorSpace)
        {
            hints.EncodedColorSpace = hints.AdobeColorSpace;
            hints.EncodedColorSpaceSource = JpegColorHintSource.Adobe;
        }

        hints.ColorCharacteristicsSource = ResolveColorCharacteristicsSource(info, hints);
        hints.HasConflict = HasConflict(hints);
        return hints;
    }

    internal void CopyComponentIdentifiers(JpegInfo info, JpegColorHintMetadata hints)
    {
        for (int i = 0; i < info.Frame.Components.Count; i++)
        {
            hints.ComponentIdentifiers.Add(info.Frame.Components[i].Identifier);
        }
    }

    internal JpegColorSpace ResolveComponentCount(int componentCount)
    {
        if (componentCount == 1)
        {
            return JpegColorSpace.Grayscale;
        }
        if (componentCount == 3)
        {
            return JpegColorSpace.YCbCr;
        }
        if (componentCount == 4)
        {
            return JpegColorSpace.CMYK;
        }
        return JpegColorSpace.Unknown;
    }

    internal JpegColorSpace ResolveComponentIdentifiers(JpegFrameHeader frame)
    {
        if (frame.ComponentCount == 1)
        {
            return JpegColorSpace.Grayscale;
        }
        if (frame.ComponentCount == 3 && frame.Components.Count == 3)
        {
            int first = frame.Components[0].Identifier;
            int second = frame.Components[1].Identifier;
            int third = frame.Components[2].Identifier;
            if (first == 82 && second == 71 && third == 66)
            {
                return JpegColorSpace.RGB;
            }
            if (first == 1 && second == 2 && third == 3)
            {
                return JpegColorSpace.YCbCr;
            }
        }
        if (frame.ComponentCount == 4 && frame.Components.Count == 4)
        {
            int first = frame.Components[0].Identifier;
            int second = frame.Components[1].Identifier;
            int third = frame.Components[2].Identifier;
            int fourth = frame.Components[3].Identifier;
            if (first == 67 && second == 77 && third == 89 && fourth == 75)
            {
                return JpegColorSpace.CMYK;
            }
        }
        return JpegColorSpace.Unknown;
    }

    internal JpegColorSpace ResolveJfif(JpegInfo info)
    {
        if (!info.Metadata.HasJfif)
        {
            return JpegColorSpace.Unknown;
        }
        if (info.Frame.ComponentCount == 1)
        {
            return JpegColorSpace.Grayscale;
        }
        if (info.Frame.ComponentCount == 3)
        {
            return JpegColorSpace.YCbCr;
        }
        return JpegColorSpace.Unknown;
    }

    internal JpegColorSpace ResolveExif(JpegInfo info)
    {
        if (!info.Metadata.HasExif || !info.Metadata.ExifMetadata.HasColorSpace)
        {
            return JpegColorSpace.Unknown;
        }
        if (info.Metadata.ExifMetadata.ColorSpace == 1 || info.Metadata.ExifMetadata.ColorSpace == 2)
        {
            return JpegColorSpace.RGB;
        }
        return JpegColorSpace.Unknown;
    }

    internal JpegColorSpace ResolveAdobe(JpegInfo info)
    {
        if (!info.Metadata.HasAdobe)
        {
            return JpegColorSpace.Unknown;
        }
        return info.Metadata.Adobe.ColorSpace;
    }

    internal JpegColorSpace ResolveCicp(JpegInfo info)
    {
        if (!info.Metadata.HasCicp || !info.Metadata.Cicp.IsValid || info.Frame.ComponentCount != 3)
        {
            return JpegColorSpace.Unknown;
        }
        int matrix = info.Metadata.Cicp.MatrixCoefficients;
        if (matrix == 0)
        {
            return JpegColorSpace.RGB;
        }
        if (matrix == 1 || matrix == 4 || matrix == 5 || matrix == 6 || matrix == 7 || matrix == 9 || matrix == 10 || matrix == 12 || matrix == 13)
        {
            return JpegColorSpace.YCbCr;
        }
        return JpegColorSpace.Unknown;
    }

    internal JpegColorSpace ResolveIcc(JpegInfo info)
    {
        if (!info.Metadata.Icc.IsPresent)
        {
            return JpegColorSpace.Unknown;
        }
        if (info.Metadata.Icc.Conversion.SourceColorSpace != JpegColorSpace.Unknown)
        {
            return info.Metadata.Icc.Conversion.SourceColorSpace;
        }
        string signature = info.Metadata.Icc.ColorSpace;
        if (signature == "RGB ")
        {
            return JpegColorSpace.RGB;
        }
        if (signature == "GRAY")
        {
            return JpegColorSpace.Grayscale;
        }
        if (signature == "CMYK")
        {
            return JpegColorSpace.CMYK;
        }
        return JpegColorSpace.Unknown;
    }

    internal JpegColorHintSource ResolveColorCharacteristicsSource(JpegInfo info, JpegColorHintMetadata hints)
    {
        if (info.Metadata.Icc.IsPresent)
        {
            return JpegColorHintSource.IccProfile;
        }
        if (info.Metadata.HasCicp && info.Metadata.Cicp.IsValid && (info.Metadata.Cicp.ColorPrimaries != 2 || info.Metadata.Cicp.TransferCharacteristics != 2))
        {
            return JpegColorHintSource.Cicp;
        }
        if (hints.HasExifColorSpace)
        {
            return JpegColorHintSource.Exif;
        }
        if (hints.HasJfifColorSpace)
        {
            return JpegColorHintSource.Jfif;
        }
        return JpegColorHintSource.Unknown;
    }

    internal bool HasConflict(JpegColorHintMetadata hints)
    {
        JpegColorSpace selected = hints.EncodedColorSpace;
        if (hints.HasComponentIdentifierColorSpace && hints.ComponentIdentifierColorSpace != selected)
        {
            return true;
        }
        if (hints.HasJfifColorSpace && hints.JfifColorSpace != selected)
        {
            return true;
        }
        if (hints.HasCicpColorSpace && hints.CicpColorSpace != selected)
        {
            return true;
        }
        if (hints.HasAdobeColorSpace && hints.AdobeColorSpace != selected)
        {
            return true;
        }
        return false;
    }
}
