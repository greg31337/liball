namespace icc;

internal class Icc1TagDefinition
{
    internal uint TagSignature;
    internal uint[] Version2Types;
    internal uint[] Version4Types;
    internal Icc1TagRole Role;
    internal int RenderingIntent;
    internal int ProfileClassMask;
    internal Icc1ChannelSource InputChannels;
    internal Icc1ChannelSource OutputChannels;
    internal Icc1ChannelSource AssociatedChannels;
    internal int RequiredDataChannels;
    internal int ExpectedValueCount;

    internal Icc1TagDefinition(uint tagSignature, uint[] version2Types, uint[] version4Types, Icc1TagRole role, int renderingIntent, int profileClassMask, Icc1ChannelSource inputChannels, Icc1ChannelSource outputChannels, Icc1ChannelSource associatedChannels, int requiredDataChannels, int expectedValueCount)
    {
        TagSignature = tagSignature;
        Version2Types = version2Types;
        Version4Types = version4Types;
        Role = role;
        RenderingIntent = renderingIntent;
        ProfileClassMask = profileClassMask;
        InputChannels = inputChannels;
        OutputChannels = outputChannels;
        AssociatedChannels = associatedChannels;
        RequiredDataChannels = requiredDataChannels;
        ExpectedValueCount = expectedValueCount;
    }
}
