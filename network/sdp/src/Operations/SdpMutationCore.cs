using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpMutationCore
{
    internal static SdpAttribute ReplaceAttribute(List<SdpAttribute> attributes, int attributeIndex, SdpAttribute replacement)
    {
        SdpAttribute previous = attributes[attributeIndex];
        attributes[attributeIndex] = replacement;
        return previous;
    }

    internal static SdpAttribute RemoveAttributeAt(List<SdpAttribute> attributes, int attributeIndex)
    {
        SdpAttribute removed = attributes[attributeIndex];
        attributes.RemoveAt(attributeIndex);
        return removed;
    }

    internal static int RemoveAttributesByName(List<SdpAttribute> attributes, string attributeName)
    {
        int removedCount = 0;
        int index = attributes.Count - 1;
        while (index >= 0)
        {
            SdpAttribute attribute = attributes[index];
            if (attribute != null && string.Equals(attribute.Name, attributeName, StringComparison.OrdinalIgnoreCase))
            {
                attributes.RemoveAt(index);
                removedCount = removedCount + 1;
            }
            index = index - 1;
        }
        return removedCount;
    }

    internal static void MoveMedia(List<SdpMediaDescription> mediaDescriptions, int sourceMediaIndex, int destinationMediaIndex)
    {
        if (sourceMediaIndex == destinationMediaIndex)
        {
            return;
        }
        SdpMediaDescription media = mediaDescriptions[sourceMediaIndex];
        mediaDescriptions.RemoveAt(sourceMediaIndex);
        mediaDescriptions.Insert(destinationMediaIndex, media);
    }

    internal static SdpMediaDescription RemoveMediaAt(List<SdpMediaDescription> mediaDescriptions, int mediaIndex)
    {
        SdpMediaDescription removed = mediaDescriptions[mediaIndex];
        mediaDescriptions.RemoveAt(mediaIndex);
        return removed;
    }

    internal static void IncrementOriginVersion(SdpSessionDescription description)
    {
        if (description.Origin == null || description.Origin.SessionVersion == null)
        {
            throw new InvalidOperationException("The origin session version is absent or invalid.");
        }
        SdpNumberParseResult parsed = SdpNumberParser.ParseCharacterUnsignedDecimal(description.Origin.SessionVersion.AsSpan());
        if (!parsed.Success || parsed.UnsignedValue == ulong.MaxValue)
        {
            throw new InvalidOperationException("The origin session version is invalid or cannot be incremented.");
        }
        description.Origin.SessionVersion = (parsed.UnsignedValue + 1UL).ToString(CultureInfo.InvariantCulture);
    }
}
