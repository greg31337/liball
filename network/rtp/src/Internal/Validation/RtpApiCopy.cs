using System;

namespace rtp;

internal static class RtpApiCopy
{
    internal static RtpSessionConfiguration CopyValidatedSessionConfiguration(
        RtpSessionConfiguration configuration)
    {
        RtpConfigurationValidator.ValidateSessionConfiguration(configuration);
        return configuration.Copy();
    }

    internal static RtpPayloadTypeDefinition CopyValidatedPayloadTypeDefinition(
        RtpPayloadTypeDefinition definition)
    {
        RtpConfigurationValidator.ValidatePayloadTypeDefinition(definition);
        return definition.Copy();
    }

    internal static RtpPayloadTypeDefinition[] CopyValidatedPayloadTypeDefinitions(
        RtpPayloadTypeDefinition[] definitions,
        RtpSessionTransportMode transportMode)
    {
        RtpConfigurationValidator.ValidatePayloadTypeDefinitions(definitions, transportMode);
        RtpPayloadTypeDefinition[] copies =
            new RtpPayloadTypeDefinition[definitions.Length];
        int definitionIndex = 0;
        while (definitionIndex < definitions.Length)
        {
            copies[definitionIndex] = definitions[definitionIndex].Copy();
            definitionIndex = definitionIndex + 1;
        }

        return copies;
    }

    internal static RtpHeaderExtensionMapping CopyValidatedHeaderExtensionMapping(
        RtpHeaderExtensionMapping mapping)
    {
        RtpConfigurationValidator.ValidateHeaderExtensionMapping(mapping);
        return mapping.Copy();
    }

    internal static RtpHeaderExtensionMapping[] CopyValidatedHeaderExtensionMappings(
        RtpHeaderExtensionMapping[] mappings)
    {
        RtpConfigurationValidator.ValidateHeaderExtensionMappings(mappings);
        RtpHeaderExtensionMapping[] copies =
            new RtpHeaderExtensionMapping[mappings.Length];
        int mappingIndex = 0;
        while (mappingIndex < mappings.Length)
        {
            copies[mappingIndex] = mappings[mappingIndex].Copy();
            mappingIndex = mappingIndex + 1;
        }

        return copies;
    }

    internal static RtpPacket CopyRtpPacket(RtpPacket packet)
    {
        if (packet == null)
        {
            throw new ArgumentNullException("packet");
        }

        return packet.Copy();
    }

    internal static RtcpPacket CopyRtcpPacket(RtcpPacket packet)
    {
        if (packet == null)
        {
            throw new ArgumentNullException("packet");
        }

        return packet.Copy();
    }

    internal static RtcpCompoundPacket CopyRtcpCompoundPacket(RtcpCompoundPacket packet)
    {
        if (packet == null)
        {
            throw new ArgumentNullException("packet");
        }

        return packet.Copy();
    }

    internal static RtpPacketContext CopyRtpPacketContext(RtpPacketContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return context.Copy();
    }

    internal static RtpFeedbackRequest CopyRtpFeedbackRequest(RtpFeedbackRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        return request.Copy();
    }

    internal static RtpRepairRequest CopyRtpRepairRequest(RtpRepairRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        return request.Copy();
    }

    internal static RtpRepairResult CopyRtpRepairResult(RtpRepairResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException("result");
        }

        return result.Copy();
    }

    internal static RtpSessionSnapshot CopyRtpSessionSnapshot(RtpSessionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException("snapshot");
        }

        return snapshot.Copy();
    }

    internal static RtpSessionUpdate CopyRtpSessionUpdate(RtpSessionUpdate update)
    {
        if (update == null)
        {
            throw new ArgumentNullException("update");
        }

        return update.Copy();
    }
}
