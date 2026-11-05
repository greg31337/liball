using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxSlideTransitionOperations"/>.
/// </summary>
public interface IPptxSlideTransitionOperations
{
    /// <summary>
    /// Reports whether the slide transition contains or enables transition.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasTransition(PptxSlideTransition target);
    /// <summary>
    /// Updates the type and the underlying presentation data.
    /// </summary>
    /// <param name="transitionType">The transition type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetType(PptxSlideTransition target, string transitionType);
    /// <summary>
    /// Updates the speed and the underlying presentation data.
    /// </summary>
    /// <param name="speed">The speed value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetSpeed(PptxSlideTransition target, string speed);
    /// <summary>
    /// Updates the direction and the underlying presentation data.
    /// </summary>
    /// <param name="direction">The direction value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetDirection(PptxSlideTransition target, string direction);
    /// <summary>
    /// Updates the advance and the underlying presentation data.
    /// </summary>
    /// <param name="advanceOnClick">The advance on click value to use.</param>
    /// <param name="advanceAfterTime">The advance after time value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetAdvance(PptxSlideTransition target, bool advanceOnClick, string advanceAfterTime);
    /// <summary>
    /// Enables or disables click-based slide advance while preserving timed advance.
    /// </summary>
    /// <param name="advanceOnClick">Whether the slide advances on click.</param>
    /// <param name="target">The slide transition to update.</param>
    void SetAdvanceOnClick(PptxSlideTransition target, bool advanceOnClick);
    /// <summary>
    /// Enables timed slide advance using a non-negative millisecond duration.
    /// </summary>
    /// <param name="milliseconds">The delay before the slide advances.</param>
    /// <param name="target">The slide transition to update.</param>
    void SetAdvanceAfterMilliseconds(PptxSlideTransition target, long milliseconds);
    /// <summary>
    /// Disables timed slide advance while preserving click advance.
    /// </summary>
    /// <param name="target">The slide transition to update.</param>
    void ClearAdvanceAfter(PptxSlideTransition target);
    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void Remove(PptxSlideTransition target);
}
