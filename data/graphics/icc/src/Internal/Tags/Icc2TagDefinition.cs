namespace icc;

internal class Icc2TagDefinition
{
    internal uint TagSignature;
    internal uint[] PermittedTypes;
    internal Icc2TagRole Role;
    internal int RenderingIntent;
    internal int ProfileClassMask;
    internal Icc2ChannelSource InputChannels;
    internal Icc2ChannelSource OutputChannels;
    internal Icc2ChannelSource AssociatedChannels;
    internal uint ContainerSignature;

    internal Icc2TagDefinition(uint tagSignature, uint[] permittedTypes, Icc2TagRole role, int renderingIntent, int profileClassMask, Icc2ChannelSource inputChannels, Icc2ChannelSource outputChannels, Icc2ChannelSource associatedChannels, uint containerSignature)
    {
        TagSignature = tagSignature;
        PermittedTypes = permittedTypes;
        Role = role;
        RenderingIntent = renderingIntent;
        ProfileClassMask = profileClassMask;
        InputChannels = inputChannels;
        OutputChannels = outputChannels;
        AssociatedChannels = associatedChannels;
        ContainerSignature = containerSignature;
    }
}
