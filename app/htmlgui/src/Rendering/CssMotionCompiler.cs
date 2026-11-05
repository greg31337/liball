using System;
using System.Collections.Generic;
using System.Text;

namespace htmlgui;

internal class CssTransitionValue
{
    internal string[] Properties = Array.Empty<string>();
    internal int DurationMilliseconds;
    internal int DelayMilliseconds;
    internal string TimingFunction = "ease";
}

internal class CssKeyframeValue
{
    internal int OffsetPercent;
    internal UiCssDeclaration[] Declarations = Array.Empty<UiCssDeclaration>();
}

internal class AnimationLifecycleMessage
{
    internal UiBridgeMessageKind Kind = UiBridgeMessageKind.Lifecycle;
    internal string Key = string.Empty;
    internal string Name = string.Empty;
    internal UiAnimationStage Stage;
    internal bool ReducedMotion;
}

internal class CssAnimationPlan
{
    internal string KeyframesCss = string.Empty;
    internal UiCssDeclaration[] Declarations = Array.Empty<UiCssDeclaration>();
    internal AnimationLifecycleMessage[] Messages = Array.Empty<AnimationLifecycleMessage>();
    internal string StateToken = string.Empty;
    internal bool RequiresCompletion;
}

internal class ActiveCssAnimation
{
    internal string Key = string.Empty;
    internal string Name = string.Empty;
    internal UiAnimationStage Stage;
}

internal class CssMotionCompiler
{
    internal CssSerializer Serializer = new CssSerializer();
    internal List<ActiveCssAnimation> ActiveAnimations = new List<ActiveCssAnimation>();

    internal UiCssDeclaration CreateTransitionDeclaration(CssTransitionValue? transition, bool prefersReducedMotion)
    {
        if (transition == null)
        {
            throw new ArgumentNullException("transition");
        }

        if (transition.DurationMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException("transition", transition.DurationMilliseconds, "Transition durations cannot be negative.");
        }

        string timingFunction = NormalizeTimingFunction(transition.TimingFunction);
        string[] properties = NormalizeTransitionProperties(transition.Properties);
        string value = "none";
        if (!prefersReducedMotion && transition.DurationMilliseconds > 0)
        {
            StringBuilder builder = new StringBuilder();
            int index = 0;
            while (index < properties.Length)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(properties[index]);
                builder.Append(' ');
                builder.Append(InvariantFormatter.FormatInt32(transition.DurationMilliseconds));
                builder.Append(CssUnits.Millisecond);
                builder.Append(' ');
                builder.Append(timingFunction);
                builder.Append(' ');
                builder.Append(InvariantFormatter.FormatInt32(transition.DelayMilliseconds));
                builder.Append(CssUnits.Millisecond);
                index++;
            }

            value = builder.ToString();
        }

        return new UiCssDeclaration
        {
            Property = CssProperties.Transition,
            Value = value,
            IsTrusted = true
        };
    }

    internal string[] NormalizeTransitionProperties(string[]? properties)
    {
        if (properties == null)
        {
            throw new ArgumentNullException("properties");
        }

        if (properties.Length == 0)
        {
            throw new ArgumentException("Transitions require at least one property.", "properties");
        }

        List<string> result = new List<string>();
        int index = 0;
        while (index < properties.Length)
        {
            string? property = properties[index];
            if (string.IsNullOrWhiteSpace(property))
            {
                throw new ArgumentException("Transition property names cannot be empty.", "properties");
            }

            string normalized = property.Trim();
            if (!CssEscaper.IsValidPropertyName(normalized) || normalized == CssProperties.Transition || normalized == CssProperties.Animation)
            {
                throw new ArgumentException("The transition property name is not supported.", "properties");
            }

            InsertUniqueProperty(result, normalized);
            index++;
        }

        return result.ToArray();
    }

    internal void InsertUniqueProperty(List<string> properties, string property)
    {
        int index = 0;
        while (index < properties.Count)
        {
            int comparison = string.CompareOrdinal(properties[index], property);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                break;
            }

            index++;
        }

        properties.Insert(index, property);
    }

    internal string NormalizeTimingFunction(string? timingFunction)
    {
        if (string.IsNullOrWhiteSpace(timingFunction))
        {
            throw new ArgumentException("Animation timing functions cannot be empty.", "timingFunction");
        }

        string value = timingFunction.Trim().ToLowerInvariant();
        switch (value)
        {
            case "linear":
            case "ease":
            case "ease-in":
            case "ease-out":
            case "ease-in-out":
            case "step-start":
            case "step-end":
                return value;
            default:
                throw new ArgumentException("The animation timing function is not supported.", "timingFunction");
        }
    }

    internal string SerializeKeyframes(string? name, CssKeyframeValue[]? keyframes)
    {
        string normalizedName = NormalizeAnimationName(name);
        if (keyframes == null)
        {
            throw new ArgumentNullException("keyframes");
        }

        if (keyframes.Length < 2)
        {
            throw new ArgumentException("Keyframe animations require at least two frames.", "keyframes");
        }

        List<CssKeyframeValue> ordered = new List<CssKeyframeValue>();
        int index = 0;
        while (index < keyframes.Length)
        {
            CssKeyframeValue? keyframe = keyframes[index];
            if (keyframe == null)
            {
                throw new ArgumentException("Keyframe collections cannot contain null.", "keyframes");
            }

            if (keyframe.OffsetPercent < 0 || keyframe.OffsetPercent > 100)
            {
                throw new ArgumentOutOfRangeException("keyframes", keyframe.OffsetPercent, "Keyframe offsets must be between zero and one hundred percent.");
            }

            Serializer.SerializeDeclarations(keyframe.Declarations);
            InsertKeyframe(ordered, keyframe);
            index++;
        }

        if (ordered[0].OffsetPercent != 0 || ordered[ordered.Count - 1].OffsetPercent != 100)
        {
            throw new ArgumentException("Keyframe animations require zero and one hundred percent frames.", "keyframes");
        }

        StringBuilder result = new StringBuilder();
        result.Append("@keyframes ");
        result.Append(CssEscaper.EscapeIdentifier(normalizedName));
        result.Append('{');
        index = 0;
        while (index < ordered.Count)
        {
            CssKeyframeValue keyframe = ordered[index];
            result.Append(InvariantFormatter.FormatInt32(keyframe.OffsetPercent));
            result.Append("%{");
            result.Append(Serializer.SerializeDeclarations(keyframe.Declarations));
            result.Append('}');
            index++;
        }

        result.Append('}');
        return result.ToString();
    }

    internal void InsertKeyframe(List<CssKeyframeValue> keyframes, CssKeyframeValue keyframe)
    {
        int index = 0;
        while (index < keyframes.Count)
        {
            if (keyframes[index].OffsetPercent == keyframe.OffsetPercent)
            {
                throw new ArgumentException("Keyframe offsets must be unique.", "keyframe");
            }

            if (keyframes[index].OffsetPercent > keyframe.OffsetPercent)
            {
                break;
            }

            index++;
        }

        keyframes.Insert(index, keyframe);
    }

    internal CssAnimationPlan StartAnimation(UiAnimation? animation, CssKeyframeValue[]? keyframes, bool prefersReducedMotion)
    {
        if (animation == null)
        {
            throw new ArgumentNullException("animation");
        }

        string key = NormalizeAnimationKey(animation.Key);
        string name = NormalizeAnimationName(animation.Name);
        string stateToken = GetActiveStageToken(animation.Stage);
        if (animation.DurationMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException("animation", animation.DurationMilliseconds, "Animation durations cannot be negative.");
        }

        string keyframesCss = SerializeKeyframes(name, keyframes);
        List<AnimationLifecycleMessage> messages = new List<AnimationLifecycleMessage>();
        ActiveCssAnimation? existing = FindActiveAnimation(key);
        if (existing != null)
        {
            messages.Add(CreateLifecycleMessage(existing.Key, existing.Name, UiAnimationStage.Cancelled, false));
            ActiveAnimations.Remove(existing);
        }

        bool reduce = prefersReducedMotion && animation.DisableForReducedMotion;
        messages.Add(CreateLifecycleMessage(key, name, animation.Stage, reduce));
        CssAnimationPlan result = new CssAnimationPlan();
        result.StateToken = stateToken;
        if (reduce || animation.DurationMilliseconds == 0)
        {
            result.Declarations = new UiCssDeclaration[] { CreateDisabledAnimationDeclaration() };
            result.Messages = AppendCompletedMessage(messages, key, name, reduce);
            return result;
        }

        result.KeyframesCss = keyframesCss;
        result.Declarations = new UiCssDeclaration[] { CreateActiveAnimationDeclaration(name, animation.DurationMilliseconds) };
        result.Messages = messages.ToArray();
        result.RequiresCompletion = true;
        ActiveAnimations.Add(new ActiveCssAnimation { Key = key, Name = name, Stage = animation.Stage });
        return result;
    }

    internal UiCssDeclaration CreateActiveAnimationDeclaration(string name, int durationMilliseconds)
    {
        return new UiCssDeclaration
        {
            Property = CssProperties.Animation,
            Value = CssEscaper.EscapeIdentifier(name) + " " + InvariantFormatter.FormatInt32(durationMilliseconds) + CssUnits.Millisecond + " ease both",
            IsTrusted = true
        };
    }

    internal UiCssDeclaration CreateDisabledAnimationDeclaration()
    {
        return new UiCssDeclaration
        {
            Property = CssProperties.Animation,
            Value = "none",
            IsTrusted = true
        };
    }

    internal AnimationLifecycleMessage[] AppendCompletedMessage(List<AnimationLifecycleMessage> messages, string key, string name, bool reducedMotion)
    {
        messages.Add(CreateLifecycleMessage(key, name, UiAnimationStage.Completed, reducedMotion));
        return messages.ToArray();
    }

    internal AnimationLifecycleMessage CompleteAnimation(string? key, string? name)
    {
        ActiveCssAnimation animation = RequireActiveAnimation(key);
        string normalizedName = NormalizeAnimationName(name);
        if (!string.Equals(animation.Name, normalizedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The completion message does not match the active animation.");
        }

        ActiveAnimations.Remove(animation);
        return CreateLifecycleMessage(animation.Key, animation.Name, UiAnimationStage.Completed, false);
    }

    internal AnimationLifecycleMessage CancelAnimation(string? key)
    {
        ActiveCssAnimation animation = RequireActiveAnimation(key);
        ActiveAnimations.Remove(animation);
        return CreateLifecycleMessage(animation.Key, animation.Name, UiAnimationStage.Cancelled, false);
    }

    internal ActiveCssAnimation RequireActiveAnimation(string? key)
    {
        string normalizedKey = NormalizeAnimationKey(key);
        ActiveCssAnimation? animation = FindActiveAnimation(normalizedKey);
        if (animation == null)
        {
            throw new InvalidOperationException("The animation is not active.");
        }

        return animation;
    }

    internal ActiveCssAnimation? FindActiveAnimation(string key)
    {
        int index = 0;
        while (index < ActiveAnimations.Count)
        {
            if (string.Equals(ActiveAnimations[index].Key, key, StringComparison.Ordinal))
            {
                return ActiveAnimations[index];
            }

            index++;
        }

        return null;
    }

    internal int GetActiveAnimationCount()
    {
        return ActiveAnimations.Count;
    }

    internal AnimationLifecycleMessage CreateLifecycleMessage(string key, string name, UiAnimationStage stage, bool reducedMotion)
    {
        return new AnimationLifecycleMessage
        {
            Key = key,
            Name = name,
            Stage = stage,
            ReducedMotion = reducedMotion
        };
    }

    internal string NormalizeAnimationKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Animation keys cannot be empty.", "key");
        }

        return key.Trim();
    }

    internal string NormalizeAnimationName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Animation names cannot be empty.", "name");
        }

        string value = name.Trim();
        string lower = value.ToLowerInvariant();
        if (lower == "none" || lower == "initial" || lower == "inherit" || lower == "unset" || lower == "revert" || lower == "revert-layer")
        {
            throw new ArgumentException("CSS-wide keywords cannot be used as animation names.", "name");
        }

        CssEscaper.EscapeIdentifier(value);
        return value;
    }

    internal string GetActiveStageToken(UiAnimationStage stage)
    {
        switch (stage)
        {
            case UiAnimationStage.Enter:
                return "enter";
            case UiAnimationStage.Leave:
                return "leave";
            case UiAnimationStage.Move:
                return "move";
            default:
                throw new ArgumentException("Only enter, leave, and move stages can start an animation.", "stage");
        }
    }
}
