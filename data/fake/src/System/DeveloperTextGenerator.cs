using System;
using System.Globalization;
using System.Text;

namespace fake;

internal static class DeveloperTextGenerator
{
    const int DefaultMaximumStackFrames = 4;
    const int DefaultMinimumStackFrames = 2;
    const int MaximumCronLength = 13;

    internal static string Cron(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            MaximumCronLength,
            context.CapturedOptions.MaximumOutputSize,
            "cron");
        CronExpressionKind kind = (CronExpressionKind)random.IntFromMaximumExclusive(7);
        return CronUnchecked(random, kind);
    }

    internal static string Cron(
        GenerationContext context,
        FakeRandom random,
        CronExpressionKind kind)
    {
        ValidateCronKind(kind);
        int maximum = CronMaximumLength(kind);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "cron");
        return CronUnchecked(random, kind);
    }

    internal static string EnvironmentName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateEnvironmentData(context);
        int maximum = MaximumLength(
            DeveloperTextData.EnvironmentPrefixes) + 1 + MaximumLength(
                DeveloperTextData.EnvironmentSuffixes);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "environmentName");
        return DeveloperTextData.EnvironmentPrefixes[
            random.IntFromMaximumExclusive(DeveloperTextData.EnvironmentPrefixes.Length)] + "_" +
            DeveloperTextData.EnvironmentSuffixes[
                random.IntFromMaximumExclusive(DeveloperTextData.EnvironmentSuffixes.Length)];
    }

    internal static bool IsEnvironmentName(
        GenerationContext context,
        string value)
    {
        Guard.NotNull(value, "value");
        Guard.Count(
            value.Length,
            context.CapturedOptions.MaximumCount,
            "value");
        if (value.Length == 0 || !EnvironmentInitial(value[0]))
        {
            return false;
        }
        int index = 1;
        while (index < value.Length)
        {
            if (!EnvironmentCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static string CommandArgument(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateCommandData(context);
        int maximum = MaximumLength(DeveloperTextData.CommandArguments);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "commandArgument");
        return DeveloperTextData.CommandArguments[
            random.IntFromMaximumExclusive(DeveloperTextData.CommandArguments.Length)];
    }

    internal static string LogEntry(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateLogData(context);
        Guard.OutputSize(
            MaximumLogLength(null),
            context.CapturedOptions.MaximumOutputSize,
            "logEntry");
        LogSeverity severity = (LogSeverity)random.IntFromMaximumExclusive(6);
        return LogEntryUnchecked(context, random, severity);
    }

    internal static string LogEntry(
        GenerationContext context,
        FakeRandom random,
        LogSeverity severity)
    {
        ValidateSeverity(severity);
        ValidateLogData(context);
        Guard.OutputSize(
            MaximumLogLength(SeverityText(severity)),
            context.CapturedOptions.MaximumOutputSize,
            "logEntry");
        return LogEntryUnchecked(context, random, severity);
    }

    internal static string ExceptionMessage(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateExceptionData(context);
        int maximum = MaximumLength(DeveloperTextData.ExceptionMessages);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "exceptionMessage");
        return DeveloperTextData.ExceptionMessages[
            random.IntFromMaximumExclusive(DeveloperTextData.ExceptionMessages.Length)];
    }

    internal static string StackTrace(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateStackRequest(context, DefaultMaximumStackFrames);
        int frameCount = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            DefaultMinimumStackFrames,
            DefaultMaximumStackFrames,
            true);
        return StackTraceUnchecked(random, frameCount);
    }

    internal static string StackTrace(
        GenerationContext context,
        FakeRandom random,
        int frameCount)
    {
        ValidateStackRequest(context, frameCount);
        return StackTraceUnchecked(random, frameCount);
    }

    internal static string ExceptionText(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateExceptionData(context);
        Guard.Count(
            DefaultMaximumStackFrames,
            context.CapturedOptions.MaximumCount,
            "frameCount");
        long maximum = (long)MaximumLength(
            DeveloperTextData.ExceptionTypes) + 2L + (long)MaximumLength(
                DeveloperTextData.ExceptionMessages) + 1L +
            MaximumStackLength(DefaultMaximumStackFrames);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "exceptionText");
        int exceptionIndex = random.IntFromMaximumExclusive(
            DeveloperTextData.ExceptionTypes.Length);
        int frameCount = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            DefaultMinimumStackFrames,
            DefaultMaximumStackFrames,
            true);
        return DeveloperTextData.ExceptionTypes[exceptionIndex] + ": " +
            DeveloperTextData.ExceptionMessages[exceptionIndex] + "\n" +
            StackTraceUnchecked(random, frameCount);
    }

    static string CronUnchecked(
        FakeRandom random,
        CronExpressionKind kind)
    {
        switch (kind)
        {
            case CronExpressionKind.EveryMinute:
                return "* * * * *";
            case CronExpressionKind.Hourly:
                return "0 * * * *";
            case CronExpressionKind.Daily:
                return "0 0 * * *";
            case CronExpressionKind.Weekly:
                return "0 0 * * 0";
            case CronExpressionKind.Monthly:
                return "0 0 1 * *";
            case CronExpressionKind.SpecificTime:
                return random.IntFromMaximumExclusive(60).ToString(CultureInfo.InvariantCulture) +
                    " " + random.IntFromMaximumExclusive(24).ToString(
                        CultureInfo.InvariantCulture) + " * * *";
            default:
                return "*/" + random.IntFromMinimumAndMaximumAndMaximumInclusive(2, 30, true).ToString(
                    CultureInfo.InvariantCulture) + " * * * *";
        }
    }

    static string LogEntryUnchecked(
        GenerationContext context,
        FakeRandom random,
        LogSeverity severity)
    {
        string timestamp = context.ReferenceTime().ToString(
            "O",
            CultureInfo.InvariantCulture);
        string category = DeveloperTextData.LogCategories[
            random.IntFromMaximumExclusive(DeveloperTextData.LogCategories.Length)];
        string message = DeveloperTextData.LogMessages[
            random.IntFromMaximumExclusive(DeveloperTextData.LogMessages.Length)];
        string eventId = random.IntFromMinimumAndMaximumAndMaximumInclusive(1000, 9999, true).ToString(
            "D4",
            CultureInfo.InvariantCulture);
        return timestamp + " [" + SeverityText(severity) + "] " +
            category + " " + eventId + ": " + message;
    }

    static string StackTraceUnchecked(
        FakeRandom random,
        int frameCount)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < frameCount)
        {
            if (index > 0)
            {
                result.Append('\n');
            }
            result.Append("   at ");
            result.Append(DeveloperTextData.StackFrames[
                random.IntFromMaximumExclusive(DeveloperTextData.StackFrames.Length)]);
            index = index + 1;
        }
        return result.ToString();
    }

    static void ValidateStackRequest(
        GenerationContext context,
        int frameCount)
    {
        Guard.Positive(frameCount, "frameCount");
        Guard.Count(
            frameCount,
            context.CapturedOptions.MaximumCount,
            "frameCount");
        ValidateExceptionData(context);
        Guard.OutputSize(
            MaximumStackLength(frameCount),
            context.CapturedOptions.MaximumOutputSize,
            "stackTrace");
    }

    static long MaximumStackLength(int frameCount)
    {
        return (long)frameCount * (6L + (long)MaximumLength(
            DeveloperTextData.StackFrames)) + (long)frameCount - 1L;
    }

    static int MaximumLogLength(string? severity)
    {
        int severityLength = severity == null ? 11 : severity.Length;
        return 44 + severityLength + MaximumLength(
            DeveloperTextData.LogCategories) + MaximumLength(
                DeveloperTextData.LogMessages);
    }

    static string SeverityText(LogSeverity severity)
    {
        switch (severity)
        {
            case LogSeverity.Trace:
                return "TRACE";
            case LogSeverity.Debug:
                return "DEBUG";
            case LogSeverity.Information:
                return "INFORMATION";
            case LogSeverity.Warning:
                return "WARNING";
            case LogSeverity.Error:
                return "ERROR";
            default:
                return "CRITICAL";
        }
    }

    static int CronMaximumLength(CronExpressionKind kind)
    {
        if (kind == CronExpressionKind.MinuteInterval)
        {
            return MaximumCronLength;
        }
        if (kind == CronExpressionKind.SpecificTime)
        {
            return 12;
        }
        return CronFixed(kind).Length;
    }

    static string CronFixed(CronExpressionKind kind)
    {
        switch (kind)
        {
            case CronExpressionKind.EveryMinute:
                return "* * * * *";
            case CronExpressionKind.Hourly:
                return "0 * * * *";
            case CronExpressionKind.Daily:
                return "0 0 * * *";
            case CronExpressionKind.Weekly:
                return "0 0 * * 0";
            default:
                return "0 0 1 * *";
        }
    }

    static void ValidateCronKind(CronExpressionKind kind)
    {
        switch (kind)
        {
            case CronExpressionKind.EveryMinute:
            case CronExpressionKind.Hourly:
            case CronExpressionKind.Daily:
            case CronExpressionKind.Weekly:
            case CronExpressionKind.Monthly:
            case CronExpressionKind.SpecificTime:
            case CronExpressionKind.MinuteInterval:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported cron expression shape.");
        }
    }

    static void ValidateSeverity(LogSeverity severity)
    {
        switch (severity)
        {
            case LogSeverity.Trace:
            case LogSeverity.Debug:
            case LogSeverity.Information:
            case LogSeverity.Warning:
            case LogSeverity.Error:
            case LogSeverity.Critical:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "severity",
                    severity,
                    "severity is not supported.");
        }
    }

    static void ValidateEnvironmentData(GenerationContext context)
    {
        Guard.Count(
            DeveloperTextData.EnvironmentPrefixes.Length,
            context.CapturedOptions.MaximumCount,
            "environmentPrefixData");
        Guard.Count(
            DeveloperTextData.EnvironmentSuffixes.Length,
            context.CapturedOptions.MaximumCount,
            "environmentSuffixData");
        int index = 0;
        while (index < DeveloperTextData.EnvironmentPrefixes.Length)
        {
            ValidateEnvironmentFixture(
                DeveloperTextData.EnvironmentPrefixes[index]);
            index = index + 1;
        }
        index = 0;
        while (index < DeveloperTextData.EnvironmentSuffixes.Length)
        {
            ValidateEnvironmentFixture(
                DeveloperTextData.EnvironmentSuffixes[index]);
            index = index + 1;
        }
    }

    static void ValidateCommandData(GenerationContext context)
    {
        Guard.Count(
            DeveloperTextData.CommandArguments.Length,
            context.CapturedOptions.MaximumCount,
            "commandArgumentData");
        int index = 0;
        while (index < DeveloperTextData.CommandArguments.Length)
        {
            string value = DeveloperTextData.CommandArguments[index];
            if (value.Length < 3 || value[0] != '-' || value[1] != '-')
            {
                throw FakeException.Create(
                    "The project-authored command-argument table is malformed.");
            }
            int characterIndex = 0;
            while (characterIndex < value.Length)
            {
                char character = value[characterIndex];
                if (char.IsWhiteSpace(character) || character == ';' ||
                    character == '&' || character == '|' || character == '`' ||
                    character == '$' || character == '<' || character == '>')
                {
                    throw FakeException.Create(
                        "The project-authored command-argument table contains a shell control character.");
                }
                characterIndex = characterIndex + 1;
            }
            index = index + 1;
        }
    }

    static void ValidateLogData(GenerationContext context)
    {
        Guard.Count(
            DeveloperTextData.LogCategories.Length,
            context.CapturedOptions.MaximumCount,
            "logCategoryData");
        Guard.Count(
            DeveloperTextData.LogMessages.Length,
            context.CapturedOptions.MaximumCount,
            "logMessageData");
        ValidateSingleLine(DeveloperTextData.LogCategories, "log category");
        ValidateSingleLine(DeveloperTextData.LogMessages, "log message");
    }

    static void ValidateExceptionData(GenerationContext context)
    {
        Guard.Count(
            DeveloperTextData.ExceptionTypes.Length,
            context.CapturedOptions.MaximumCount,
            "exceptionTypeData");
        Guard.Count(
            DeveloperTextData.ExceptionMessages.Length,
            context.CapturedOptions.MaximumCount,
            "exceptionMessageData");
        Guard.Count(
            DeveloperTextData.StackFrames.Length,
            context.CapturedOptions.MaximumCount,
            "stackFrameData");
        if (DeveloperTextData.ExceptionTypes.Length !=
            DeveloperTextData.ExceptionMessages.Length)
        {
            throw FakeException.Create(
                "The project-authored exception type and message tables have inconsistent lengths.");
        }
        ValidateSingleLine(DeveloperTextData.ExceptionTypes, "exception type");
        ValidateSingleLine(
            DeveloperTextData.ExceptionMessages,
            "exception message");
        ValidateSingleLine(DeveloperTextData.StackFrames, "stack frame");
    }

    static void ValidateSingleLine(string[] values, string tableName)
    {
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            if (value.Length == 0 || value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0)
            {
                throw FakeException.Create(
                    "The project-authored " + tableName + " table is malformed.");
            }
            index = index + 1;
        }
    }

    static void ValidateEnvironmentFixture(string value)
    {
        if (value.Length == 0 || !EnvironmentInitial(value[0]))
        {
            throw FakeException.Create(
                "The project-authored environment-name table is malformed.");
        }
        int index = 1;
        while (index < value.Length)
        {
            if (!EnvironmentCharacter(value[index]))
            {
                throw FakeException.Create(
                    "The project-authored environment-name table is malformed.");
            }
            index = index + 1;
        }
    }

    static bool EnvironmentInitial(char value)
    {
        return value >= 'A' && value <= 'Z' ||
            value >= 'a' && value <= 'z' ||
            value == '_';
    }

    static bool EnvironmentCharacter(char value)
    {
        return EnvironmentInitial(value) || value >= '0' && value <= '9';
    }

    static int MaximumLength(string[] values)
    {
        int result = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > result)
            {
                result = values[index].Length;
            }
            index = index + 1;
        }
        return result;
    }
}
