using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Resolves BUNDLE groups, tagged media sections, and effective shared transport state.</summary>
public class SdpBundleService : ISdpBundleService
{
    /// <summary>Initializes an SDP BUNDLE service.</summary>
    public SdpBundleService()
    {
    }

    /// <summary>Resolves valid BUNDLE groups in session attribute order.</summary>
    /// <param name="description">The session description containing BUNDLE signaling.</param>
    /// <returns>Independent BUNDLE group projections retaining their source groups.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    public List<SdpBundleGroup> ResolveBundleGroups(SdpSessionDescription description)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        return SdpBundleServiceCore.ResolveBundleGroups(description);
    }

    /// <summary>Finds the first BUNDLE group containing a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The matching BUNDLE group, or <c>null</c> when the media section is not bundled.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpBundleGroup? FindBundleGroupForMedia(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpBundleServiceCore.FindBundleGroupForMedia(description, mediaIndex);
    }

    /// <summary>Resolves the tagged media section supplying transport state for a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The tagged media index, or the selected index when no valid tagged section can be resolved.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public int ResolveBundleTaggedMediaIndex(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpBundleServiceCore.ResolveBundleTaggedMediaIndex(description, mediaIndex);
    }

    /// <summary>Resolves shared transport state for the BUNDLE group containing a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective BUNDLE transport, or <c>null</c> when no valid BUNDLE transport applies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpBundleTransport? ResolveBundleTransport(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpBundleServiceCore.ResolveBundleTransport(description, mediaIndex);
    }

    /// <summary>Constructs initial BUNDLE signaling over preconfigured media fallback transports.</summary>
    /// <param name="description">The initial offer description to update.</param>
    /// <param name="mediaIndexes">The distinct media indexes to bundle, in preferred group order.</param>
    /// <param name="taggedMediaIndex">The bundled media index suggested as the tagged transport owner.</param>
    /// <param name="bundleOnlyMediaIndexes">The bundled media indexes that may be accepted only within BUNDLE.</param>
    /// <returns>The created BUNDLE group with the tagged media identifier first.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or either index collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an index collection contains duplicates or an invalid membership relationship.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a media index does not identify a media section.</exception>
    /// <exception cref="SdpNegotiationException">Thrown when existing signaling or fallback transports cannot form a valid initial BUNDLE offer.</exception>
    public SdpBundleGroup CreateInitialBundleOffer(SdpSessionDescription description, List<int> mediaIndexes, int taggedMediaIndex, List<int> bundleOnlyMediaIndexes)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (mediaIndexes == null)
        {
            throw new ArgumentNullException("mediaIndexes");
        }
        if (bundleOnlyMediaIndexes == null)
        {
            throw new ArgumentNullException("bundleOnlyMediaIndexes");
        }
        return SdpBundleServiceCore.CreateInitialBundleOffer(description, mediaIndexes, taggedMediaIndex, bundleOnlyMediaIndexes);
    }

    /// <summary>Constructs an initial BUNDLE answer for an accepted subset of an offered group.</summary>
    /// <param name="offer">The initial offer containing the requested BUNDLE group.</param>
    /// <param name="answer">The corresponding answer description to update.</param>
    /// <param name="offeredBundleGroupIndex">The zero-based BUNDLE-group index in the offer.</param>
    /// <param name="acceptedMediaIndexes">The distinct offered media indexes retained in the answered BUNDLE group.</param>
    /// <returns>The created answer BUNDLE group, or <c>null</c> when no offered member is accepted in the group.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the offer, answer, or accepted-media collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the offer and answer media counts differ or the accepted indexes are duplicated or outside the offered group.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the BUNDLE-group index or an accepted media index is outside its valid range.</exception>
    /// <exception cref="SdpNegotiationException">Thrown when the offered group, answer media identifiers, or selected tagged transport is incompatible.</exception>
    public SdpBundleGroup? CreateInitialBundleAnswer(SdpSessionDescription offer, SdpSessionDescription answer, int offeredBundleGroupIndex, List<int> acceptedMediaIndexes)
    {
        if (offer == null)
        {
            throw new ArgumentNullException("offer");
        }
        if (answer == null)
        {
            throw new ArgumentNullException("answer");
        }
        if (acceptedMediaIndexes == null)
        {
            throw new ArgumentNullException("acceptedMediaIndexes");
        }
        return SdpBundleServiceCore.CreateInitialBundleAnswer(offer, answer, offeredBundleGroupIndex, acceptedMediaIndexes);
    }

    /// <summary>Updates a subsequent offer to the requested BUNDLE membership and disabled-media state.</summary>
    /// <param name="offer">The subsequent offer description to update.</param>
    /// <param name="bundleGroupIndex">The zero-based existing BUNDLE-group index.</param>
    /// <param name="bundledMediaIndexes">The distinct media indexes retained in or added to the group.</param>
    /// <param name="taggedMediaIndex">The bundled media index supplying the shared transport, or <c>-1</c> when dissolving the group.</param>
    /// <param name="disabledMediaIndexes">The distinct previous group members to disable and remove from the group.</param>
    /// <returns>The updated BUNDLE group, or <c>null</c> when the group is dissolved.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the offer or either media-index collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when indexes are duplicated or have incompatible group membership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the group or a media index is outside its valid range.</exception>
    /// <exception cref="SdpNegotiationException">Thrown when MID or transport state cannot represent the requested edit.</exception>
    public SdpBundleGroup? UpdateSubsequentBundleOffer(SdpSessionDescription offer, int bundleGroupIndex, List<int> bundledMediaIndexes, int taggedMediaIndex, List<int> disabledMediaIndexes)
    {
        if (offer == null)
        {
            throw new ArgumentNullException("offer");
        }
        if (bundledMediaIndexes == null)
        {
            throw new ArgumentNullException("bundledMediaIndexes");
        }
        if (disabledMediaIndexes == null)
        {
            throw new ArgumentNullException("disabledMediaIndexes");
        }
        return SdpBundleServiceCore.UpdateSubsequentBundleOffer(offer, bundleGroupIndex, bundledMediaIndexes, taggedMediaIndex, disabledMediaIndexes);
    }

    /// <summary>Updates a subsequent answer to the accepted subset of an offered BUNDLE group.</summary>
    /// <param name="offer">The subsequent offer containing the BUNDLE group.</param>
    /// <param name="answer">The corresponding subsequent answer to update.</param>
    /// <param name="offeredBundleGroupIndex">The zero-based BUNDLE-group index in the offer.</param>
    /// <param name="acceptedMediaIndexes">The distinct offered media indexes accepted in the answered BUNDLE group.</param>
    /// <returns>The updated answer BUNDLE group.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the offer, answer, or accepted-media collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when media counts or accepted membership are incompatible.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the group or an accepted media index is outside its valid range.</exception>
    /// <exception cref="SdpNegotiationException">Thrown when the established tag, MID identity, or transport state is incompatible.</exception>
    public SdpBundleGroup UpdateSubsequentBundleAnswer(SdpSessionDescription offer, SdpSessionDescription answer, int offeredBundleGroupIndex, List<int> acceptedMediaIndexes)
    {
        if (offer == null)
        {
            throw new ArgumentNullException("offer");
        }
        if (answer == null)
        {
            throw new ArgumentNullException("answer");
        }
        if (acceptedMediaIndexes == null)
        {
            throw new ArgumentNullException("acceptedMediaIndexes");
        }
        return SdpBundleServiceCore.UpdateSubsequentBundleAnswer(offer, answer, offeredBundleGroupIndex, acceptedMediaIndexes);
    }

    internal void ValidateMediaSelection(SdpSessionDescription description, int mediaIndex)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("mediaIndex");
        }
    }
}
