using System.Collections.Generic;

namespace pptx;

internal enum PptxSyntaxSampleState
{
    Empty = 0,
    Ready = 1
}

internal class PptxSyntaxSample
{
    internal const string DefaultName = "syntax-sample";

    internal string Name;
    internal List<string> Items;
    internal string? NullableName;
    internal PptxSyntaxSample? NullableSample;
    internal List<string>? NullableItems;

    internal PptxSyntaxSample(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new System.ArgumentException("Sample name is required.");
        }

        Name = name;
        Items = new List<string>();
        NullableName = null;
        NullableSample = null;
        NullableItems = null;
    }

    internal void Add(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Item value is required.");
        }

        Items.Add(value);
    }

    internal int Count()
    {
        return Items.Count;
    }

    internal PptxSyntaxSampleState State()
    {
        if (Items.Count == 0)
        {
            return PptxSyntaxSampleState.Empty;
        }

        return PptxSyntaxSampleState.Ready;
    }

    internal string JoinWith(string separator)
    {
        if (separator == null)
        {
            throw new System.ArgumentNullException("separator");
        }

        string result = "";
        for (int index = 0; index < Items.Count; index++)
        {
            if (index > 0)
            {
                result = result + separator;
            }

            result = result + Items[index];
        }

        return result;
    }

    internal string DescribeCount()
    {
        switch (Items.Count)
        {
            case 0:
                return "empty";
            case 1:
                return "single";
            default:
                return "multiple";
        }
    }

    internal static PptxSyntaxSample CreateDefault()
    {
        return new PptxSyntaxSample(DefaultName);
    }
}
