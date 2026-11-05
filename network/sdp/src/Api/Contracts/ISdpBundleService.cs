using System.Collections.Generic;

namespace sdp;

/// <summary>Resolves BUNDLE groups, tagged media sections, and effective shared transport state.</summary>
public interface ISdpBundleService
{
    /// <summary>Resolves valid BUNDLE groups in session attribute order.</summary>
    /// <param name="description">The session description containing BUNDLE signaling.</param>
    /// <returns>Independent BUNDLE group projections retaining their source groups.</returns>
    List<SdpBundleGroup> ResolveBundleGroups(SdpSessionDescription description);

    /// <summary>Finds the first BUNDLE group containing a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The matching BUNDLE group, or <c>null</c> when the media section is not bundled.</returns>
    SdpBundleGroup? FindBundleGroupForMedia(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves the tagged media section supplying transport state for a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The tagged media index, or the selected index when no valid tagged section can be resolved.</returns>
    int ResolveBundleTaggedMediaIndex(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves shared transport state for the BUNDLE group containing a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective BUNDLE transport, or <c>null</c> when no valid BUNDLE transport applies.</returns>
    SdpBundleTransport? ResolveBundleTransport(SdpSessionDescription description, int mediaIndex);

    /// <summary>Constructs initial BUNDLE signaling over preconfigured media fallback transports.</summary>
    /// <param name="description">The initial offer description to update.</param>
    /// <param name="mediaIndexes">The distinct media indexes to bundle, in preferred group order.</param>
    /// <param name="taggedMediaIndex">The bundled media index suggested as the tagged transport owner.</param>
    /// <param name="bundleOnlyMediaIndexes">The bundled media indexes that may be accepted only within BUNDLE.</param>
    /// <returns>The created BUNDLE group with the tagged media identifier first.</returns>
    SdpBundleGroup CreateInitialBundleOffer(SdpSessionDescription description, List<int> mediaIndexes, int taggedMediaIndex, List<int> bundleOnlyMediaIndexes);

    /// <summary>Constructs an initial BUNDLE answer for an accepted subset of an offered group.</summary>
    /// <param name="offer">The initial offer containing the requested BUNDLE group.</param>
    /// <param name="answer">The corresponding answer description to update.</param>
    /// <param name="offeredBundleGroupIndex">The zero-based BUNDLE-group index in the offer.</param>
    /// <param name="acceptedMediaIndexes">The distinct offered media indexes retained in the answered BUNDLE group.</param>
    /// <returns>The created answer BUNDLE group, or <c>null</c> when no offered member is accepted in the group.</returns>
    SdpBundleGroup? CreateInitialBundleAnswer(SdpSessionDescription offer, SdpSessionDescription answer, int offeredBundleGroupIndex, List<int> acceptedMediaIndexes);

    /// <summary>Updates a subsequent offer to the requested BUNDLE membership and disabled-media state.</summary>
    /// <param name="offer">The subsequent offer description to update.</param>
    /// <param name="bundleGroupIndex">The zero-based existing BUNDLE-group index.</param>
    /// <param name="bundledMediaIndexes">The distinct media indexes retained in or added to the group.</param>
    /// <param name="taggedMediaIndex">The bundled media index supplying the shared transport, or <c>-1</c> when dissolving the group.</param>
    /// <param name="disabledMediaIndexes">The distinct previous group members to disable and remove from the group.</param>
    /// <returns>The updated BUNDLE group, or <c>null</c> when the group is dissolved.</returns>
    SdpBundleGroup? UpdateSubsequentBundleOffer(SdpSessionDescription offer, int bundleGroupIndex, List<int> bundledMediaIndexes, int taggedMediaIndex, List<int> disabledMediaIndexes);

    /// <summary>Updates a subsequent answer to the accepted subset of an offered BUNDLE group.</summary>
    /// <param name="offer">The subsequent offer containing the BUNDLE group.</param>
    /// <param name="answer">The corresponding subsequent answer to update.</param>
    /// <param name="offeredBundleGroupIndex">The zero-based BUNDLE-group index in the offer.</param>
    /// <param name="acceptedMediaIndexes">The distinct offered media indexes accepted in the answered BUNDLE group.</param>
    /// <returns>The updated answer BUNDLE group.</returns>
    SdpBundleGroup UpdateSubsequentBundleAnswer(SdpSessionDescription offer, SdpSessionDescription answer, int offeredBundleGroupIndex, List<int> acceptedMediaIndexes);
}
