using System;
using System.Collections.Generic;

namespace odf;

internal class AnimationClockValue
{
    internal double Seconds;
    internal bool IsValid;
}

internal class AnimationDurationValue
{
    internal double Seconds;
    internal bool IsIndefinite;
    internal bool IsMedia;
    internal bool IsValid;
}

internal class AnimationTimingCore
{
    internal string Core = "";
    internal double OffsetSeconds;
    internal bool IsValid;
}

internal static class AnimationTimingModel
{
    internal static void RefreshAnimations(OdfDocument document)
    {
        int index = 0;
        while (index < document.Animations.Length)
        {
            RefreshAnimationValues(
                document.Animations[index],
                document.VersionInfo);
            index = index + 1;
        }
        index = 0;
        while (index < document.Animations.Length)
        {
            ResolveAnimationReferences(
                document.Animations[index],
                document.Animations);
            index = index + 1;
        }
    }

    static void RefreshAnimationValues(
        OdfAnimation animation,
        OdfVersionInfo? version)
    {
        animation.BeginTimes = ParseTimingList(animation.Begin);
        animation.EndTimes = ParseTimingList(animation.End);

        AnimationDurationValue duration = ParseSmilDuration(
            animation.Duration,
            true,
            true);
        animation.DurationSeconds = duration.Seconds;
        animation.IsDurationIndefinite = duration.IsIndefinite;
        animation.IsDurationMedia = duration.IsMedia;
        animation.IsDurationValid = duration.IsValid;

        AnimationDurationValue minimum = ParseSmilDuration(
            animation.Minimum,
            false,
            true);
        animation.MinimumSeconds = minimum.Seconds;
        animation.IsMinimumMedia = minimum.IsMedia;
        animation.IsMinimumValid = minimum.IsValid;

        AnimationDurationValue maximum = ParseSmilDuration(
            animation.Maximum,
            true,
            true);
        animation.MaximumSeconds = maximum.Seconds;
        animation.IsMaximumIndefinite = maximum.IsIndefinite;
        animation.IsMaximumMedia = maximum.IsMedia;
        animation.IsMaximumValid = maximum.IsValid;

        RefreshRepeatValues(animation);
        RefreshFillValues(animation);
        RefreshRestartValues(animation);
        RefreshTimeManipulation(animation);
        RefreshIterateInterval(animation);
        animation.IsVersionValid = ValidateVersion(animation, version);
    }

    static void RefreshRepeatValues(OdfAnimation animation)
    {
        animation.RepeatCountValue = 0;
        animation.IsRepeatCountIndefinite = false;
        animation.IsRepeatCountValid = animation.RepeatCount.Length == 0;
        if (animation.RepeatCount == "indefinite")
        {
            animation.IsRepeatCountIndefinite = true;
            animation.IsRepeatCountValid = true;
        }
        else if (animation.RepeatCount.Length > 0)
        {
            OdfLexicalValue count = OdfLexicalParser.ParseDecimal(
                animation.RepeatCount);
            animation.IsRepeatCountValid = count.IsValid
                && count.DecimalValue >= 0;
            if (animation.IsRepeatCountValid)
            {
                animation.RepeatCountValue = count.DecimalValue;
            }
        }

        AnimationDurationValue repeatDuration = ParseSmilDuration(
            animation.RepeatDuration,
            true,
            false);
        animation.RepeatDurationSeconds = repeatDuration.Seconds;
        animation.IsRepeatDurationIndefinite =
            repeatDuration.IsIndefinite;
        animation.IsRepeatDurationValid = repeatDuration.IsValid;
    }

    static void RefreshFillValues(OdfAnimation animation)
    {
        animation.IsFillValid = IsFillValue(animation.Fill, true);
        animation.IsFillDefaultValid = IsFillDefaultValue(
            animation.FillDefault);
        string inherited = "auto";
        if (animation.Parent != null)
        {
            inherited = animation.Parent.EffectiveFillDefault;
        }
        if (animation.FillDefault.Length == 0
            || animation.FillDefault == "inherit"
            || !animation.IsFillDefaultValid)
        {
            animation.EffectiveFillDefault = inherited;
        }
        else
        {
            animation.EffectiveFillDefault = animation.FillDefault;
        }
        if (animation.Fill.Length == 0
            || animation.Fill == "default"
            || !animation.IsFillValid)
        {
            animation.EffectiveFill = animation.EffectiveFillDefault;
        }
        else
        {
            animation.EffectiveFill = animation.Fill;
        }
    }

    static void RefreshRestartValues(OdfAnimation animation)
    {
        animation.IsRestartValid = IsRestartValue(animation.Restart);
        animation.IsRestartDefaultValid = IsRestartDefaultValue(
            animation.RestartDefault);
        string inherited = "always";
        if (animation.Parent != null)
        {
            inherited = animation.Parent.EffectiveRestartDefault;
        }
        if (animation.RestartDefault.Length == 0
            || animation.RestartDefault == "inherit"
            || !animation.IsRestartDefaultValid)
        {
            animation.EffectiveRestartDefault = inherited;
        }
        else
        {
            animation.EffectiveRestartDefault = animation.RestartDefault;
        }
        if (animation.Restart.Length == 0
            || animation.Restart == "default"
            || !animation.IsRestartValid)
        {
            animation.EffectiveRestart = animation.EffectiveRestartDefault;
        }
        else
        {
            animation.EffectiveRestart = animation.Restart;
        }
    }

    static void RefreshTimeManipulation(OdfAnimation animation)
    {
        animation.SpeedValue = 1;
        animation.IsSpeedValid = animation.Speed.Length == 0;
        if (animation.Speed.Length > 0)
        {
            OdfLexicalValue speed = OdfLexicalParser.ParseFloat(
                animation.Speed);
            animation.IsSpeedValid = speed.IsValid
                && double.IsFinite(speed.FloatValue)
                && speed.FloatValue != 0;
            if (animation.IsSpeedValid)
            {
                animation.SpeedValue = speed.FloatValue;
            }
        }

        animation.AccelerateValue = 0;
        animation.DecelerateValue = 0;
        animation.IsAccelerateValid = ParseFractionValue(
            animation.Accelerate);
        animation.IsDecelerateValid = ParseFractionValue(
            animation.Decelerate);
        if (animation.Accelerate.Length > 0
            && animation.IsAccelerateValid)
        {
            animation.AccelerateValue = OdfLexicalParser.ParseDecimal(
                animation.Accelerate).DecimalValue;
        }
        if (animation.Decelerate.Length > 0
            && animation.IsDecelerateValid)
        {
            animation.DecelerateValue = OdfLexicalParser.ParseDecimal(
                animation.Decelerate).DecimalValue;
        }
        animation.IsAccelerationValid = animation.IsAccelerateValid
            && animation.IsDecelerateValid
            && animation.AccelerateValue + animation.DecelerateValue <= 1;

        animation.AutoReverseEnabled = false;
        animation.IsAutoReverseValid = animation.AutoReverse.Length == 0;
        if (animation.AutoReverse.Length > 0)
        {
            OdfLexicalValue autoReverse = OdfLexicalParser.ParseBoolean(
                animation.AutoReverse);
            animation.IsAutoReverseValid = autoReverse.IsValid;
            animation.AutoReverseEnabled = autoReverse.IsValid
                && autoReverse.BooleanValue;
        }
    }

    static bool ParseFractionValue(string value)
    {
        if (value.Length == 0)
        {
            return true;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseDecimal(value);
        return parsed.IsValid
            && parsed.DecimalValue >= 0
            && parsed.DecimalValue <= 1;
    }

    static void RefreshIterateInterval(OdfAnimation animation)
    {
        animation.IterateIntervalSeconds = 0;
        animation.IsIterateIntervalValid =
            animation.IterateInterval.Length == 0;
        if (animation.IterateInterval.Length == 0)
        {
            return;
        }
        OdfDurationValue duration = OdfLexicalParser.ParseDuration(
            animation.IterateInterval);
        if (!duration.IsValid
            || duration.IsNegative
            || duration.Years != 0
            || duration.Months != 0)
        {
            return;
        }
        double seconds = (double)duration.Days * 86400
            + (double)duration.Hours * 3600
            + (double)duration.Minutes * 60
            + (double)duration.Seconds;
        if (duration.FractionalSecond.Length > 0)
        {
            OdfLexicalValue fraction = OdfLexicalParser.ParseDecimal(
                "0." + duration.FractionalSecond);
            if (!fraction.IsValid)
            {
                return;
            }
            seconds = seconds + (double)fraction.DecimalValue;
        }
        animation.IsIterateIntervalValid = double.IsFinite(seconds);
        if (animation.IsIterateIntervalValid)
        {
            animation.IterateIntervalSeconds = seconds;
        }
    }

    static void ResolveAnimationReferences(
        OdfAnimation animation,
        OdfAnimation[] animations)
    {
        ResolveTarget(animation);
        ResolveMaster(animation, animations);
        ResolveTimingList(
            animation.BeginTimes,
            animation,
            animations);
        ResolveTimingList(
            animation.EndTimes,
            animation,
            animations);
        animation.IsBeginValid = TimingsAreValid(animation.BeginTimes);
        animation.IsEndValid = TimingsAreValid(animation.EndTimes);
    }

    static void ResolveTarget(OdfAnimation animation)
    {
        animation.TargetObject = null;
        animation.IsTargetAmbiguous = false;
        animation.IsTargetValid = animation.TargetElement.Length == 0;
        if (animation.TargetElement.Length == 0
            || animation.Slide == null)
        {
            return;
        }
        int count = 0;
        OdfDrawingObject? page = animation.Slide.Drawing;
        if (page != null && DrawingMatches(page, animation.TargetElement))
        {
            animation.TargetObject = page;
            count = count + 1;
        }
        int index = 0;
        while (index < animation.Slide.DrawingObjects.Length)
        {
            OdfDrawingObject candidate =
                animation.Slide.DrawingObjects[index];
            if (candidate != page
                && DrawingMatches(candidate, animation.TargetElement))
            {
                animation.TargetObject = candidate;
                count = count + 1;
            }
            index = index + 1;
        }
        animation.IsTargetValid = count == 1;
        animation.IsTargetAmbiguous = count > 1;
        if (count != 1)
        {
            animation.TargetObject = null;
        }
    }

    static void ResolveMaster(
        OdfAnimation animation,
        OdfAnimation[] animations)
    {
        animation.MasterAnimation = null;
        animation.IsMasterElementAmbiguous = false;
        animation.IsMasterElementValid = animation.MasterElement.Length == 0;
        if (animation.MasterElement.Length == 0)
        {
            return;
        }
        int count = 0;
        int index = 0;
        while (index < animations.Length)
        {
            OdfAnimation candidate = animations[index];
            if (candidate != animation
                && candidate.Slide == animation.Slide
                && AnimationMatches(candidate, animation.MasterElement))
            {
                animation.MasterAnimation = candidate;
                count = count + 1;
            }
            index = index + 1;
        }
        animation.IsMasterElementValid = count == 1;
        animation.IsMasterElementAmbiguous = count > 1;
        if (count != 1)
        {
            animation.MasterAnimation = null;
        }
    }

    static void ResolveTimingList(
        OdfAnimationTiming[] timings,
        OdfAnimation owner,
        OdfAnimation[] animations)
    {
        int index = 0;
        while (index < timings.Length)
        {
            ResolveTimingReference(timings[index], owner, animations);
            index = index + 1;
        }
    }

    static void ResolveTimingReference(
        OdfAnimationTiming timing,
        OdfAnimation owner,
        OdfAnimation[] animations)
    {
        timing.ReferencedAnimation = null;
        timing.ReferencedDrawingObject = null;
        timing.IsReferenceAmbiguous = false;
        timing.IsReferenceValid = timing.ReferenceId.Length == 0;
        if (timing.ReferenceId.Length == 0 || owner.Slide == null)
        {
            return;
        }
        int count = 0;
        int index = 0;
        while (index < animations.Length)
        {
            OdfAnimation candidate = animations[index];
            if (candidate.Slide == owner.Slide
                && AnimationMatches(candidate, timing.ReferenceId))
            {
                timing.ReferencedAnimation = candidate;
                count = count + 1;
            }
            index = index + 1;
        }
        bool animationOnly = timing.Kind == OdfAnimationTimingKind.Syncbase
            || timing.Kind == OdfAnimationTimingKind.Repeat;
        if (!animationOnly)
        {
            OdfDrawingObject? page = owner.Slide.Drawing;
            if (page != null && DrawingMatches(page, timing.ReferenceId))
            {
                timing.ReferencedDrawingObject = page;
                count = count + 1;
            }
            index = 0;
            while (index < owner.Slide.DrawingObjects.Length)
            {
                OdfDrawingObject candidate =
                    owner.Slide.DrawingObjects[index];
                if (candidate != page
                    && DrawingMatches(candidate, timing.ReferenceId))
                {
                    timing.ReferencedDrawingObject = candidate;
                    count = count + 1;
                }
                index = index + 1;
            }
        }
        timing.IsReferenceValid = count == 1;
        timing.IsReferenceAmbiguous = count > 1;
        if (count != 1)
        {
            timing.ReferencedAnimation = null;
            timing.ReferencedDrawingObject = null;
        }
    }

    static bool TimingsAreValid(OdfAnimationTiming[] timings)
    {
        int index = 0;
        while (index < timings.Length)
        {
            if (!timings[index].IsValid
                || !timings[index].IsReferenceValid)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static OdfAnimationTiming[] ParseTimingList(string value)
    {
        if (value.Length == 0)
        {
            return [];
        }
        string[] expressions = value.Split(';');
        List<OdfAnimationTiming> result =
            new List<OdfAnimationTiming>();
        int index = 0;
        while (index < expressions.Length)
        {
            result.Add(ParseTimingExpression(expressions[index].Trim()));
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfAnimationTiming ParseTimingExpression(string expression)
    {
        OdfAnimationTiming result = new OdfAnimationTiming();
        result.Expression = expression;
        if (expression == "indefinite")
        {
            result.Kind = OdfAnimationTimingKind.Indefinite;
            result.IsValid = true;
            return result;
        }
        AnimationClockValue clock = ParseClockValue(expression, true);
        if (clock.IsValid)
        {
            result.Kind = OdfAnimationTimingKind.Offset;
            result.OffsetSeconds = clock.Seconds;
            result.IsValid = true;
            return result;
        }
        if (StartsFunction(expression, "wallclock"))
        {
            string inner = FunctionInner(expression, "wallclock");
            OdfTemporalValue dateTime = OdfLexicalParser.ParseDateTime(inner);
            result.Kind = OdfAnimationTimingKind.WallClock;
            result.EventName = inner;
            result.IsValid = dateTime.IsValid;
            return result;
        }
        if (BeginsFunction(expression, "accessKey")
            || BeginsFunction(expression, "accesskey"))
        {
            string name = BeginsFunction(expression, "accessKey")
                ? "accessKey"
                : "accesskey";
            AnimationTimingCore access = ParseFunctionCore(
                expression,
                name);
            result.Kind = OdfAnimationTimingKind.AccessKey;
            result.EventName = access.Core;
            result.OffsetSeconds = access.OffsetSeconds;
            result.IsValid = access.IsValid
                && access.Core.Length > 0;
            return result;
        }
        AnimationTimingCore separated = SeparateTimingOffset(expression);
        if (!separated.IsValid)
        {
            return result;
        }
        result.OffsetSeconds = separated.OffsetSeconds;
        int dot = separated.Core.LastIndexOf('.');
        if (dot < 0)
        {
            result.Kind = OdfAnimationTimingKind.Event;
            result.EventName = separated.Core;
            result.IsValid = IsTimingToken(separated.Core);
            return result;
        }
        result.ReferenceId = separated.Core.Substring(0, dot);
        string suffix = separated.Core.Substring(dot + 1);
        if (!IsTimingToken(result.ReferenceId))
        {
            return result;
        }
        if (suffix == "begin" || suffix == "end")
        {
            result.Kind = OdfAnimationTimingKind.Syncbase;
            result.EventName = suffix;
            result.IsValid = true;
            return result;
        }
        if (suffix.StartsWith("repeat(", StringComparison.Ordinal)
            && suffix.EndsWith(")", StringComparison.Ordinal))
        {
            string repeatText = suffix.Substring(7, suffix.Length - 8);
            OdfLexicalValue repeat = OdfLexicalParser.ParseInteger(
                repeatText);
            result.Kind = OdfAnimationTimingKind.Repeat;
            result.EventName = "repeat";
            result.IsValid = repeat.IsValid
                && repeat.IntegerValue >= 0
                && repeat.IntegerValue <= int.MaxValue;
            if (result.IsValid)
            {
                result.RepeatIndex = (int)repeat.IntegerValue;
                result.IsRepeatIndexSpecified = true;
            }
            return result;
        }
        if (suffix.StartsWith("marker(", StringComparison.Ordinal)
            && suffix.EndsWith(")", StringComparison.Ordinal))
        {
            result.Kind = OdfAnimationTimingKind.MediaMarker;
            result.EventName = suffix.Substring(7, suffix.Length - 8);
            result.IsValid = result.EventName.Length > 0;
            return result;
        }
        result.Kind = OdfAnimationTimingKind.Event;
        result.EventName = suffix;
        result.IsValid = IsTimingToken(suffix);
        return result;
    }

    static AnimationTimingCore ParseFunctionCore(
        string expression,
        string functionName)
    {
        AnimationTimingCore result = new AnimationTimingCore();
        int close = expression.IndexOf(')');
        int start = functionName.Length + 1;
        if (close < start || close != expression.LastIndexOf(')'))
        {
            return result;
        }
        result.Core = expression.Substring(start, close - start);
        if (close == expression.Length - 1)
        {
            result.IsValid = true;
            return result;
        }
        string offset = expression.Substring(close + 1);
        AnimationClockValue clock = ParseClockValue(offset, true);
        result.IsValid = clock.IsValid
            && (offset[0] == '+' || offset[0] == '-');
        if (result.IsValid)
        {
            result.OffsetSeconds = clock.Seconds;
        }
        return result;
    }

    static AnimationTimingCore SeparateTimingOffset(string expression)
    {
        AnimationTimingCore result = new AnimationTimingCore();
        result.Core = expression;
        result.IsValid = expression.Length > 0;
        int index = expression.Length - 1;
        while (index > 0)
        {
            if (expression[index] == '+' || expression[index] == '-')
            {
                string suffix = expression.Substring(index);
                AnimationClockValue clock = ParseClockValue(suffix, true);
                if (clock.IsValid)
                {
                    result.Core = expression.Substring(0, index);
                    result.OffsetSeconds = clock.Seconds;
                    result.IsValid = result.Core.Length > 0;
                    return result;
                }
            }
            index = index - 1;
        }
        return result;
    }

    static AnimationDurationValue ParseSmilDuration(
        string value,
        bool allowIndefinite,
        bool allowMedia)
    {
        AnimationDurationValue result = new AnimationDurationValue();
        if (value.Length == 0)
        {
            result.IsValid = true;
            return result;
        }
        if (value == "indefinite")
        {
            result.IsIndefinite = true;
            result.IsValid = allowIndefinite;
            return result;
        }
        if (value == "media")
        {
            result.IsMedia = true;
            result.IsValid = allowMedia;
            return result;
        }
        AnimationClockValue clock = ParseClockValue(value, false);
        result.Seconds = clock.Seconds;
        result.IsValid = clock.IsValid;
        return result;
    }

    static AnimationClockValue ParseClockValue(
        string value,
        bool allowSign)
    {
        AnimationClockValue result = new AnimationClockValue();
        if (value.Length == 0)
        {
            return result;
        }
        bool negative = false;
        int start = 0;
        if (value[0] == '+' || value[0] == '-')
        {
            if (!allowSign)
            {
                return result;
            }
            negative = value[0] == '-';
            start = 1;
        }
        if (start == value.Length)
        {
            return result;
        }
        string core = value.Substring(start);
        double seconds = ParseClockCore(core);
        if (!double.IsFinite(seconds) || seconds < 0)
        {
            return result;
        }
        result.Seconds = negative ? -seconds : seconds;
        result.IsValid = true;
        return result;
    }

    static double ParseClockCore(string value)
    {
        if (value.IndexOf(':') >= 0)
        {
            return ParseColonClock(value);
        }
        double multiplier = 1;
        int unitLength = 0;
        if (value.EndsWith("ms", StringComparison.Ordinal))
        {
            multiplier = 0.001;
            unitLength = 2;
        }
        else if (value.EndsWith("min", StringComparison.Ordinal))
        {
            multiplier = 60;
            unitLength = 3;
        }
        else if (value.EndsWith("h", StringComparison.Ordinal))
        {
            multiplier = 3600;
            unitLength = 1;
        }
        else if (value.EndsWith("s", StringComparison.Ordinal))
        {
            unitLength = 1;
        }
        string numeric = value.Substring(0, value.Length - unitLength);
        OdfLexicalValue parsed = OdfLexicalParser.ParseDecimal(numeric);
        if (!parsed.IsValid || parsed.DecimalValue < 0)
        {
            return double.NaN;
        }
        return (double)parsed.DecimalValue * multiplier;
    }

    static double ParseColonClock(string value)
    {
        string[] parts = value.Split(':');
        if (parts.Length != 2 && parts.Length != 3)
        {
            return double.NaN;
        }
        int minuteIndex = parts.Length == 3 ? 1 : 0;
        int secondIndex = parts.Length - 1;
        OdfLexicalValue minutes = OdfLexicalParser.ParseInteger(
            parts[minuteIndex]);
        OdfLexicalValue seconds = OdfLexicalParser.ParseDecimal(
            parts[secondIndex]);
        if (!minutes.IsValid || minutes.IntegerValue < 0
            || minutes.IntegerValue > 59
            || !seconds.IsValid || seconds.DecimalValue < 0
            || seconds.DecimalValue >= 60)
        {
            return double.NaN;
        }
        double result = minutes.IntegerValue * 60
            + (double)seconds.DecimalValue;
        if (parts.Length == 3)
        {
            OdfLexicalValue hours = OdfLexicalParser.ParseInteger(parts[0]);
            if (!hours.IsValid || hours.IntegerValue < 0)
            {
                return double.NaN;
            }
            result = result + hours.IntegerValue * 3600;
        }
        return result;
    }

    static bool StartsFunction(string expression, string name)
    {
        return expression.StartsWith(
            name + "(",
            StringComparison.Ordinal)
            && expression.EndsWith(")", StringComparison.Ordinal);
    }

    static bool BeginsFunction(string expression, string name)
    {
        return expression.StartsWith(
            name + "(",
            StringComparison.Ordinal);
    }

    static string FunctionInner(string expression, string name)
    {
        return expression.Substring(
            name.Length + 1,
            expression.Length - name.Length - 2);
    }

    static bool IsTimingToken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= ' '
                || character == ';'
                || character == '(' 
                || character == ')')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsFillValue(string value, bool allowDefault)
    {
        return value.Length == 0
            || value == "remove"
            || value == "freeze"
            || value == "hold"
            || value == "auto"
            || value == "transition"
            || allowDefault && value == "default";
    }

    static bool IsFillDefaultValue(string value)
    {
        return value.Length == 0
            || value == "remove"
            || value == "freeze"
            || value == "hold"
            || value == "auto"
            || value == "transition"
            || value == "inherit";
    }

    static bool IsRestartValue(string value)
    {
        return value.Length == 0
            || value == "never"
            || value == "always"
            || value == "whenNotActive"
            || value == "default";
    }

    static bool IsRestartDefaultValue(string value)
    {
        return value.Length == 0
            || value == "never"
            || value == "always"
            || value == "whenNotActive"
            || value == "inherit";
    }

    static bool ValidateVersion(
        OdfAnimation animation,
        OdfVersionInfo? version)
    {
        if (version == null || !version.IsValid || version.IsFuture)
        {
            return true;
        }
        bool beforeVersion12 = version.Major < 1
            || version.Major == 1 && version.Minor < 2;
        if (beforeVersion12)
        {
            if (animation.XmlId.Length > 0)
            {
                return false;
            }
            if (animation.RepeatCount.Length > 0
                && !animation.IsRepeatCountIndefinite
                && animation.IsRepeatCountValid
                && animation.RepeatCountValue % 1 != 0)
            {
                return false;
            }
            return true;
        }
        return animation.Id.Length == 0 || animation.XmlId.Length > 0;
    }

    static bool DrawingMatches(OdfDrawingObject drawing, string id)
    {
        return drawing.Id == id || drawing.XmlId == id;
    }

    static bool AnimationMatches(OdfAnimation animation, string id)
    {
        return animation.Id == id || animation.XmlId == id;
    }
}
