using System;

namespace rtp;

/// <summary>
/// Stores validated signaling-supplied RTP payload-type definitions.
/// </summary>
public class RtpPayloadTypeRegistry : IRtpPayloadTypeRegistry
{
    internal object syncRoot = new object();
    internal RtpPayloadTypeDefinition[] definitions = [];
    internal RtpSessionTransportMode transportMode;

    /// <summary>
    /// Initializes an RTP payload-type registry for one transport arrangement.
    /// </summary>
    /// <param name="transportMode">The transport mode controlling mux-safe assignments.</param>
    public RtpPayloadTypeRegistry(RtpSessionTransportMode transportMode)
    {
        RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
            definitions,
            transportMode);
        this.transportMode = transportMode;
    }

    /// <summary>Adds or replaces a payload-type definition.</summary>
    /// <param name="definition">The definition to validate and store independently.</param>
    public void SetPayloadTypeDefinition(RtpPayloadTypeDefinition definition)
    {
        RtpPayloadTypeDefinition definitionCopy =
            RtpApiCopy.CopyValidatedPayloadTypeDefinition(definition);
        lock (syncRoot)
        {
            int replacementIndex = FindStoredDefinitionIndex(definitionCopy.PayloadType);
            int candidateLength = definitions.Length;
            if (replacementIndex < 0)
            {
                candidateLength = candidateLength + 1;
            }

            RtpPayloadTypeDefinition[] candidate =
                new RtpPayloadTypeDefinition[candidateLength];
            int definitionIndex = 0;
            while (definitionIndex < definitions.Length)
            {
                candidate[definitionIndex] = definitions[definitionIndex];
                definitionIndex = definitionIndex + 1;
            }

            if (replacementIndex >= 0)
            {
                candidate[replacementIndex] = definitionCopy;
            }
            else
            {
                candidate[candidate.Length - 1] = definitionCopy;
            }

            RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
                candidate,
                transportMode);
            definitions = OrderDefinitions(candidate);
        }
    }

    /// <summary>Finds a payload-type definition.</summary>
    /// <param name="payloadType">The RTP payload type.</param>
    /// <returns>The definition copy, or <c>null</c> when no definition exists.</returns>
    public RtpPayloadTypeDefinition? FindPayloadTypeDefinition(byte payloadType)
    {
        ValidatePayloadType(payloadType);
        lock (syncRoot)
        {
            int definitionIndex = FindStoredDefinitionIndex(payloadType);
            if (definitionIndex < 0)
            {
                return null;
            }

            return definitions[definitionIndex].Copy();
        }
    }

    /// <summary>Removes a payload-type definition.</summary>
    /// <param name="payloadType">The RTP payload type to remove.</param>
    /// <returns><c>true</c> when a definition was removed.</returns>
    public bool RemovePayloadTypeDefinition(byte payloadType)
    {
        ValidatePayloadType(payloadType);
        lock (syncRoot)
        {
            int removalIndex = FindStoredDefinitionIndex(payloadType);
            if (removalIndex < 0)
            {
                return false;
            }

            RtpPayloadTypeDefinition[] remaining =
                new RtpPayloadTypeDefinition[definitions.Length - 1];
            int sourceIndex = 0;
            int destinationIndex = 0;
            while (sourceIndex < definitions.Length)
            {
                if (sourceIndex != removalIndex)
                {
                    remaining[destinationIndex] = definitions[sourceIndex];
                    destinationIndex = destinationIndex + 1;
                }

                sourceIndex = sourceIndex + 1;
            }

            RtpConfigurationValidator.ValidatePayloadTypeDefinitions(
                remaining,
                transportMode);
            definitions = remaining;
            return true;
        }
    }

    /// <summary>Returns independent copies of all registered payload definitions.</summary>
    /// <returns>The registered definitions in payload-type order.</returns>
    public RtpPayloadTypeDefinition[] GetPayloadTypeDefinitions()
    {
        lock (syncRoot)
        {
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
    }

    internal int FindStoredDefinitionIndex(byte payloadType)
    {
        int definitionIndex = 0;
        while (definitionIndex < definitions.Length)
        {
            if (definitions[definitionIndex].PayloadType == payloadType)
            {
                return definitionIndex;
            }

            definitionIndex = definitionIndex + 1;
        }

        return -1;
    }

    internal static void ValidatePayloadType(byte payloadType)
    {
        if (payloadType > RtpProtocolConstants.MaximumPayloadType)
        {
            throw new RtpConfigurationException(
                "Payload type must fit the seven-bit RTP field.");
        }
    }

    internal static RtpPayloadTypeDefinition[] OrderDefinitions(
        RtpPayloadTypeDefinition[] unordered)
    {
        RtpPayloadTypeDefinition[] ordered =
            new RtpPayloadTypeDefinition[unordered.Length];
        int definitionIndex = 0;
        while (definitionIndex < unordered.Length)
        {
            ordered[definitionIndex] = unordered[definitionIndex];
            definitionIndex = definitionIndex + 1;
        }

        definitionIndex = 1;
        while (definitionIndex < ordered.Length)
        {
            RtpPayloadTypeDefinition definition = ordered[definitionIndex];
            int insertionIndex = definitionIndex;
            while (insertionIndex > 0 &&
                ordered[insertionIndex - 1].PayloadType > definition.PayloadType)
            {
                ordered[insertionIndex] = ordered[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }

            ordered[insertionIndex] = definition;
            definitionIndex = definitionIndex + 1;
        }

        return ordered;
    }
}
