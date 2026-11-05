using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxSlideTransition values.
/// </summary>
public class PptxSlideTransitionOperations : IPptxSlideTransitionOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxSlideTransitionOperations()
    {
    }

    /// <summary>
    /// Reports whether the slide transition contains or enables transition.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasTransition(PptxSlideTransition target)
    {
        return target.HasTransition();
    }

    /// <summary>
    /// Updates the type and the underlying presentation data.
    /// </summary>
    /// <param name="transitionType">The transition type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetType(PptxSlideTransition target, string transitionType)
    {
        target.SetType(transitionType);
    }

    /// <summary>
    /// Updates the speed and the underlying presentation data.
    /// </summary>
    /// <param name="speed">The speed value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetSpeed(PptxSlideTransition target, string speed)
    {
        target.SetSpeed(speed);
    }

    /// <summary>
    /// Updates the direction and the underlying presentation data.
    /// </summary>
    /// <param name="direction">The direction value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetDirection(PptxSlideTransition target, string direction)
    {
        target.SetDirection(direction);
    }

    /// <summary>
    /// Updates the advance and the underlying presentation data.
    /// </summary>
    /// <param name="advanceOnClick">The advance on click value to use.</param>
    /// <param name="advanceAfterTime">The advance after time value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetAdvance(PptxSlideTransition target, bool advanceOnClick, string advanceAfterTime)
    {
        target.SetAdvance(advanceOnClick, advanceAfterTime);
    }
    /// <summary>
    /// Enables or disables click-based slide advance while preserving timed advance.
    /// </summary>
    /// <param name="advanceOnClick">Whether the slide advances on click.</param>
    /// <param name="target">The slide transition to update.</param>
    public void SetAdvanceOnClick(PptxSlideTransition target, bool advanceOnClick)
    {
        target.SetAdvanceOnClick(advanceOnClick);
    }
    /// <summary>
    /// Enables timed slide advance using a non-negative millisecond duration.
    /// </summary>
    /// <param name="milliseconds">The delay before the slide advances.</param>
    /// <param name="target">The slide transition to update.</param>
    public void SetAdvanceAfterMilliseconds(PptxSlideTransition target, long milliseconds)
    {
        target.SetAdvanceAfterMilliseconds(milliseconds);
    }
    /// <summary>
    /// Disables timed slide advance while preserving click advance.
    /// </summary>
    /// <param name="target">The slide transition to update.</param>
    public void ClearAdvanceAfter(PptxSlideTransition target)
    {
        target.ClearAdvanceAfter();
    }

    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void Remove(PptxSlideTransition target)
    {
        target.Remove();
    }
}
