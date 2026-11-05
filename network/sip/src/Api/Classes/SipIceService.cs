namespace sip;

/// <summary>Inspects and transforms ICE and Trickle ICE signaling without connectivity checks.</summary>
public class SipIceService : ISipIceService
{
    /// <summary>Initializes a signaling-only ICE helper.</summary>
    public SipIceService()
    {
    }

    /// <summary>Inspects ICE attributes in an ordinary SDP description.</summary>
    /// <param name="description">The SDP description.</param>
    /// <returns>The detached typed ICE signaling.</returns>
    public SipIceResult InspectIceDescription(SipSessionDescription description)
    {
        return SipIceServiceCore.InspectDescription(description);
    }

    /// <summary>Replaces ICE attributes in a detached SDP description.</summary>
    /// <param name="description">The SDP description to copy.</param>
    /// <param name="ice">The ICE signaling to apply by media order.</param>
    /// <returns>The detached updated SDP description.</returns>
    public SipIceResult ApplyIceDescription(
        SipSessionDescription description, SipIceDescription ice)
    {
        return SipIceServiceCore.ApplyDescription(description, ice);
    }

    /// <summary>Creates a Trickle ICE INFO package request.</summary>
    /// <param name="request">The dialog, ICE generation, and candidates.</param>
    /// <returns>The encoded fragment and INFO request.</returns>
    public SipIceResult CreateTrickleInfo(SipIceTrickleRequest request)
    {
        return SipIceServiceCore.CreateTrickleInfo(request);
    }

    /// <summary>Parses one Trickle ICE SDP fragment.</summary>
    /// <param name="fragment">The fragment bytes.</param>
    /// <returns>The typed signaling fragment.</returns>
    public SipIceResult ParseTrickleFragment(byte[] fragment)
    {
        return SipIceServiceCore.ParseTrickleFragment(fragment);
    }

    /// <summary>Merges a Trickle ICE fragment into a detached SDP description.</summary>
    /// <param name="description">The current ICE generation.</param>
    /// <param name="fragment">The received SDP fragment.</param>
    /// <returns>The merged detached SDP.</returns>
    public SipIceResult ApplyTrickleFragment(
        SipSessionDescription description, byte[] fragment)
    {
        return SipIceServiceCore.ApplyTrickleFragment(
            description, fragment);
    }

    /// <summary>Inspects SIP ICE and Trickle ICE option tags.</summary>
    /// <param name="message">The SIP message to inspect.</param>
    /// <returns>The option-tag support and requirement flags.</returns>
    public SipIceResult InspectIceMessageOptions(SipMessage message)
    {
        return SipIceServiceCore.InspectMessageOptions(message);
    }

    /// <summary>Adds ICE support and optional Trickle ICE requirement tags.</summary>
    /// <param name="message">The SIP message to copy and augment.</param>
    /// <param name="supportTraditionalIce">Whether to advertise the traditional ICE tag.</param>
    /// <param name="requireTrickleIce">Whether Trickle ICE is required rather than merely supported.</param>
    /// <returns>The detached augmented SIP message.</returns>
    public SipIceResult AddIceMessageOptions(SipMessage message,
        bool supportTraditionalIce, bool requireTrickleIce)
    {
        return SipIceServiceCore.AddMessageOptions(
            message, supportTraditionalIce, requireTrickleIce);
    }
}
