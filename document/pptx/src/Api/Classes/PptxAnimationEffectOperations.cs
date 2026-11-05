using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxAnimationEffect values.
/// </summary>
public class PptxAnimationEffectOperations : IPptxAnimationEffectOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxAnimationEffectOperations()
    {
    }

    /// <summary>
    /// Updates the timing and the underlying presentation data.
    /// </summary>
    /// <param name="trigger">The trigger value to use.</param>
    /// <param name="duration">The duration value to use.</param>
    /// <param name="delay">The delay value to use.</param>
    /// <param name="repeatCount">The repeat count value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTiming(PptxAnimationEffect target, string trigger, string duration, string delay, string repeatCount)
    {
        target.SetTiming(trigger, duration, delay, repeatCount);
    }

    /// <summary>
    /// Updates the trigger and the underlying presentation data.
    /// </summary>
    /// <param name="trigger">The trigger value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTrigger(PptxAnimationEffect target, string trigger)
    {
        target.SetTrigger(trigger);
    }

    /// <summary>
    /// Updates the duration and the underlying presentation data.
    /// </summary>
    /// <param name="duration">The duration value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetDuration(PptxAnimationEffect target, string duration)
    {
        target.SetDuration(duration);
    }

    /// <summary>
    /// Updates the delay and the underlying presentation data.
    /// </summary>
    /// <param name="delay">The delay value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetDelay(PptxAnimationEffect target, string delay)
    {
        target.SetDelay(delay);
    }

    /// <summary>
    /// Updates the repeat count and the underlying presentation data.
    /// </summary>
    /// <param name="repeatCount">The repeat count value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetRepeatCount(PptxAnimationEffect target, string repeatCount)
    {
        target.SetRepeatCount(repeatCount);
    }

    /// <summary>
    /// Updates the target shape id and the underlying presentation data.
    /// </summary>
    /// <param name="shapeId">The shape id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTargetShapeId(PptxAnimationEffect target, string shapeId)
    {
        target.SetTargetShapeId(shapeId);
    }
    /// <summary>
    /// Changes the animation category and effect while preserving its target and timing behavior.
    /// </summary>
    /// <param name="type">One of the values defined by <see cref="PptxAnimationType"/>.</param>
    /// <param name="effect">The filter, path, or media command for the selected category.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetEffect(PptxAnimationEffect target, string type, string effect)
    {
        target.SetEffect(type, effect);
    }
    /// <summary>
    /// Moves the animation to a new zero-based position in its slide sequence.
    /// </summary>
    /// <param name="index">The zero-based destination index.</param>
    /// <param name="target">The data value to operate on.</param>
    public void MoveTo(PptxAnimationEffect target, int index)
    {
        target.MoveTo(index);
    }

    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void Remove(PptxAnimationEffect target)
    {
        target.Remove();
    }
}
