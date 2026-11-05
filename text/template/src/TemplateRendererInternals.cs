using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace template;

internal class TemplateForLoopSpec
{
    internal List<string> VariableNames;
    internal int IterableArgumentIndex;
    internal int LimitArgumentIndex;
    internal int OffsetArgumentIndex;
    internal int ColumnsArgumentIndex;
    internal bool Reversed;

    internal TemplateForLoopSpec()
    {
        VariableNames = new List<string>();
        IterableArgumentIndex = -1;
        LimitArgumentIndex = -1;
        OffsetArgumentIndex = -1;
        ColumnsArgumentIndex = -1;
        Reversed = false;
    }

    internal bool IsValid()
    {
        return VariableNames.Count > 0 && IterableArgumentIndex >= 0;
    }
}

internal class TemplateForLoopOptions
{
    internal int Limit;
    internal int Offset;
    internal bool HasLimit;
    internal bool Reversed;
    internal int Columns;
    internal bool HasColumns;
    internal bool ContinueOffset;

    internal TemplateForLoopOptions()
    {
        Limit = 0;
        Offset = 0;
        HasLimit = false;
        Reversed = false;
        Columns = 0;
        HasColumns = false;
        ContinueOffset = false;
    }
}

internal class TemplateLoopIteration
{
    internal TemplateValue CurrentValue;
    internal TemplateValue Key;
    internal TemplateValue Value;
    internal bool HasKeyValue;

    internal TemplateLoopIteration(TemplateValue currentValue, TemplateValue key, TemplateValue value, bool hasKeyValue)
    {
        if (currentValue == null)
        {
            throw new ArgumentNullException("currentValue");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        CurrentValue = currentValue;
        Key = key;
        Value = value;
        HasKeyValue = hasKeyValue;
    }
}

internal class TemplateStreamingRenderState
{
    internal string PendingOutput;
    internal string OutputSoFar;

    internal TemplateStreamingRenderState()
    {
        PendingOutput = "";
        OutputSoFar = "";
    }
}
