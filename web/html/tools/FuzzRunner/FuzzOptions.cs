using System.Globalization;

namespace UniversalHtml.FuzzRunner;

internal sealed class FuzzUsageException : Exception
{
    public FuzzUsageException(string message) : base(message)
    {
    }
}

internal static class FuzzTargetNames
{
    public static string Format(FuzzTarget target)
    {
        return target.ToString().ToLowerInvariant();
    }

    public static FuzzTarget Parse(string value)
    {
        return value switch
        {
            "html" => FuzzTarget.Html,
            "selector" => FuzzTarget.Selector,
            "css" => FuzzTarget.Css,
            "url" => FuzzTarget.Url,
            _ => throw new FuzzUsageException("Unknown fuzz target: " + value)
        };
    }
}

internal sealed class FuzzOptions
{
    public ulong Seed { get; private set; } = 0x554E495645525341UL;
    public int Cases { get; private set; } = 256;
    public long WorkBudget { get; private set; } = 200000;
    public int TimeBudgetMilliseconds { get; private set; } = 15000;
    public int CaseTimeoutMilliseconds { get; private set; } = 500;
    public int MaximumInputLength { get; private set; } = 2048;
    public int MaximumDepth { get; private set; } = 6;
    public int ShrinkSteps { get; private set; } = 256;
    public string OutputDirectory { get; private set; }
        = "artifacts/fuzz-failures";
    public string ReplayPath { get; private set; }
    public string TargetName { get; private set; } = "all";
    public bool Shrink { get; private set; } = true;
    public bool ShowHelp { get; private set; }

    public FuzzTarget[] GetTargets()
    {
        return TargetName == "all"
            ? Enum.GetValues<FuzzTarget>()
            : [FuzzTargetNames.Parse(TargetName)];
    }

    public static FuzzOptions Parse(string[] args)
    {
        FuzzOptions options = new FuzzOptions();
        int index = 0;
        while (index < args.Length)
        {
            string argument = args[index++];
            switch (argument)
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                case "--no-shrink":
                    options.Shrink = false;
                    break;
                case "--seed":
                    options.Seed = ParseSeed(ReadValue(args, ref index,
                        argument));
                    break;
                case "--cases":
                    options.Cases = ParsePositiveInt(ReadValue(args,
                        ref index, argument), argument, 1000000);
                    break;
                case "--work-budget":
                    options.WorkBudget = ParsePositiveLong(ReadValue(args,
                        ref index, argument), argument, 1000000000L);
                    break;
                case "--time-budget-ms":
                    options.TimeBudgetMilliseconds = ParsePositiveInt(
                        ReadValue(args, ref index, argument), argument,
                        3600000);
                    break;
                case "--case-timeout-ms":
                    options.CaseTimeoutMilliseconds = ParsePositiveInt(
                        ReadValue(args, ref index, argument), argument,
                        60000);
                    break;
                case "--max-input-length":
                    options.MaximumInputLength = ParsePositiveInt(ReadValue(
                        args, ref index, argument), argument, 1000000);
                    break;
                case "--max-depth":
                    options.MaximumDepth = ParsePositiveInt(ReadValue(args,
                        ref index, argument), argument, 128);
                    break;
                case "--shrink-steps":
                    options.ShrinkSteps = ParseNonNegativeInt(ReadValue(args,
                        ref index, argument), argument, 1000000);
                    break;
                case "--output":
                    options.OutputDirectory = ReadValue(args, ref index,
                        argument);
                    break;
                case "--replay":
                    options.ReplayPath = ReadValue(args, ref index, argument);
                    break;
                case "--target":
                    options.TargetName = ReadValue(args, ref index, argument)
                        .ToLowerInvariant();
                    if (options.TargetName != "all")
                    {
                        FuzzTargetNames.Parse(options.TargetName);
                    }

                    break;
                default:
                    throw new FuzzUsageException(
                        "Unknown argument: " + argument);
            }
        }

        if (options.OutputDirectory.Length == 0)
        {
            throw new FuzzUsageException("--output cannot be empty.");
        }

        if (options.ReplayPath != null && args.Any(value =>
            value == "--seed" || value == "--cases" || value == "--target"))
        {
            throw new FuzzUsageException(
                "--replay cannot be combined with generation selection options.");
        }

        return options;
    }

    static string ReadValue(string[] args, ref int index, string option)
    {
        if (index >= args.Length)
        {
            throw new FuzzUsageException(option + " requires a value.");
        }

        return args[index++];
    }

    static ulong ParseSeed(string value)
    {
        bool hexadecimal = value.StartsWith("0x",
            StringComparison.OrdinalIgnoreCase);
        string digits = hexadecimal ? value.Substring(2) : value;
        NumberStyles style = hexadecimal ? NumberStyles.AllowHexSpecifier
            : NumberStyles.None;
        if (!ulong.TryParse(digits, style, CultureInfo.InvariantCulture,
            out ulong result))
        {
            throw new FuzzUsageException("Invalid --seed value: " + value);
        }

        return result;
    }

    static int ParsePositiveInt(string value, string option, int maximum)
    {
        int result = ParseNonNegativeInt(value, option, maximum);
        if (result == 0)
        {
            throw new FuzzUsageException(option + " must be positive.");
        }

        return result;
    }

    static int ParseNonNegativeInt(string value, string option, int maximum)
    {
        if (!int.TryParse(value, NumberStyles.None,
            CultureInfo.InvariantCulture, out int result)
            || result < 0 || result > maximum)
        {
            throw new FuzzUsageException("Invalid " + option + " value: "
                + value);
        }

        return result;
    }

    static long ParsePositiveLong(string value, string option, long maximum)
    {
        if (!long.TryParse(value, NumberStyles.None,
            CultureInfo.InvariantCulture, out long result)
            || result < 1 || result > maximum)
        {
            throw new FuzzUsageException("Invalid " + option + " value: "
                + value);
        }

        return result;
    }
}
