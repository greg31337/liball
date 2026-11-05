namespace avif;

internal enum Av1EncoderProfile
{
    Main = 0,
    High = 1,
    Professional = 2
}

internal class Av1EncoderProfileRequest
{
    internal int BitDepth;
    internal AvifChromaFormat ChromaFormat;
    internal AvifChromaSamplePosition ChromaSamplePosition;
    internal int RequestedProfile;

    internal Av1EncoderProfileRequest()
    {
        BitDepth = 8;
        ChromaFormat = AvifChromaFormat.Yuv420;
        ChromaSamplePosition = AvifChromaSamplePosition.Unknown;
        RequestedProfile = -1;
    }
}

internal class Av1EncoderProfileDecision
{
    internal AvifResultCode Code;
    internal Av1EncoderProfile Profile;
    internal int BitDepth;
    internal bool HighBitDepth;
    internal bool TwelveBit;
    internal bool Monochrome;
    internal bool ChromaSubsamplingX;
    internal bool ChromaSubsamplingY;
    internal int ChromaSamplePosition;

    internal Av1EncoderProfileDecision(AvifResultCode code)
    {
        Code = code;
    }
}

internal static class Av1EncoderProfileSelector
{
    internal static Av1EncoderProfileDecision Choose(
        Av1EncoderProfileRequest request)
    {
        if (request == null || !ValidBitDepth(request.BitDepth)
            || !ValidChromaFormat(request.ChromaFormat)
            || !ValidChromaSamplePosition(
                request.ChromaFormat,
                request.ChromaSamplePosition)
            || request.RequestedProfile < -1
            || request.RequestedProfile > (int)Av1EncoderProfile.Professional)
        {
            return new Av1EncoderProfileDecision(
                AvifResultCode.InvalidArgument);
        }

        bool monochrome = request.ChromaFormat
            == AvifChromaFormat.Monochrome;
        bool subsamplingX = request.ChromaFormat == AvifChromaFormat.Yuv420
            || request.ChromaFormat == AvifChromaFormat.Yuv422;
        bool subsamplingY = request.ChromaFormat == AvifChromaFormat.Yuv420;
        int profile = request.RequestedProfile;
        if (profile < 0)
        {
            profile = SelectProfile(
                request.BitDepth,
                monochrome,
                subsamplingX,
                subsamplingY);
        }
        if (!Av1PixelConformance.IsLegalFormat(
            profile,
            request.BitDepth,
            monochrome,
            monochrome || subsamplingX,
            monochrome || subsamplingY))
        {
            return new Av1EncoderProfileDecision(
                AvifResultCode.InvalidArgument);
        }

        Av1EncoderProfileDecision decision =
            new Av1EncoderProfileDecision(AvifResultCode.Success);
        decision.Profile = (Av1EncoderProfile)profile;
        decision.BitDepth = request.BitDepth;
        decision.HighBitDepth = request.BitDepth > 8;
        decision.TwelveBit = request.BitDepth == 12;
        decision.Monochrome = monochrome;
        decision.ChromaSubsamplingX = monochrome || subsamplingX;
        decision.ChromaSubsamplingY = monochrome || subsamplingY;
        decision.ChromaSamplePosition = request.ChromaFormat
            == AvifChromaFormat.Yuv420
            ? (int)request.ChromaSamplePosition
            : 0;
        return decision;
    }

    internal static Av1EncoderProfileDecision Choose(
        AvifImage? image,
        int requestedProfile)
    {
        if (image == null || image.GetCode() != AvifResultCode.Success)
        {
            return new Av1EncoderProfileDecision(
                AvifResultCode.InvalidArgument);
        }
        Av1EncoderProfileRequest request = new Av1EncoderProfileRequest();
        request.BitDepth = image.GetBitDepth();
        request.ChromaFormat = image.GetChromaFormat();
        request.ChromaSamplePosition = image.GetChromaSamplePosition();
        request.RequestedProfile = requestedProfile;
        return Choose(request);
    }

    internal static AvifResultCode ValidateHeader(
        ParsedAv1SequenceHeader header)
    {
        if (header == null || header.IsTwelveBit()
            && !header.IsHighBitDepth()
            || header.GetBitDepth() == 12
                && header.GetSequenceProfile()
                    != (int)Av1EncoderProfile.Professional
            || header.IsMonochrome()
                && (!header.HasChromaSubsamplingX()
                    || !header.HasChromaSubsamplingY())
            || !Av1PixelConformance.IsLegalFormat(
                header.GetSequenceProfile(),
                header.GetBitDepth(),
                header.IsMonochrome(),
                header.HasChromaSubsamplingX(),
                header.HasChromaSubsamplingY()))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static int SelectProfile(
        int bitDepth,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY)
    {
        if (bitDepth == 12 || !monochrome
            && subsamplingX && !subsamplingY)
        {
            return (int)Av1EncoderProfile.Professional;
        }
        if (!monochrome && !subsamplingX && !subsamplingY)
        {
            return (int)Av1EncoderProfile.High;
        }
        return (int)Av1EncoderProfile.Main;
    }

    static bool ValidBitDepth(int bitDepth)
    {
        return bitDepth == 8 || bitDepth == 10 || bitDepth == 12;
    }

    static bool ValidChromaFormat(AvifChromaFormat format)
    {
        return format >= AvifChromaFormat.Monochrome
            && format <= AvifChromaFormat.Yuv444;
    }

    static bool ValidChromaSamplePosition(
        AvifChromaFormat format,
        AvifChromaSamplePosition position)
    {
        if (format != AvifChromaFormat.Yuv420)
        {
            return true;
        }
        return position >= AvifChromaSamplePosition.Unknown
            && position <= AvifChromaSamplePosition.Colocated;
    }
}
