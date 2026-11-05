using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxAnimationEffectOperations"/>.
/// </summary>
public interface IPptxAnimationEffectOperations
{
    /// <summary>
    /// Updates the timing and the underlying presentation data.
    /// </summary>
    /// <param name="trigger">The trigger value to use.</param>
    /// <param name="duration">The duration value to use.</param>
    /// <param name="delay">The delay value to use.</param>
    /// <param name="repeatCount">The repeat count value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTiming(PptxAnimationEffect target, string trigger, string duration, string delay, string repeatCount);
    /// <summary>
    /// Updates the trigger and the underlying presentation data.
    /// </summary>
    /// <param name="trigger">The trigger value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTrigger(PptxAnimationEffect target, string trigger);
    /// <summary>
    /// Updates the duration and the underlying presentation data.
    /// </summary>
    /// <param name="duration">The duration value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetDuration(PptxAnimationEffect target, string duration);
    /// <summary>
    /// Updates the delay and the underlying presentation data.
    /// </summary>
    /// <param name="delay">The delay value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetDelay(PptxAnimationEffect target, string delay);
    /// <summary>
    /// Updates the repeat count and the underlying presentation data.
    /// </summary>
    /// <param name="repeatCount">The repeat count value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetRepeatCount(PptxAnimationEffect target, string repeatCount);
    /// <summary>
    /// Updates the target shape id and the underlying presentation data.
    /// </summary>
    /// <param name="shapeId">The shape id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTargetShapeId(PptxAnimationEffect target, string shapeId);
    /// <summary>
    /// Changes the animation category and effect while preserving its target and timing behavior.
    /// </summary>
    /// <param name="type">One of the values defined by <see cref="PptxAnimationType"/>.</param>
    /// <param name="effect">The filter, path, or media command for the selected category.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetEffect(PptxAnimationEffect target, string type, string effect);
    /// <summary>
    /// Moves the animation to a new zero-based position in its slide sequence.
    /// </summary>
    /// <param name="index">The zero-based destination index.</param>
    /// <param name="target">The data value to operate on.</param>
    void MoveTo(PptxAnimationEffect target, int index);
    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void Remove(PptxAnimationEffect target);
}
