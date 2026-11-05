using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace template;

internal class TemplateRuntimeQuotaState
{
    internal Stopwatch Clock;
    internal int LoopIterationCount;
    internal int TemplateCount;
    internal int RecursionDepth;

    internal TemplateRuntimeQuotaState()
    {
        Clock = new Stopwatch();
        LoopIterationCount = 0;
        TemplateCount = 0;
        RecursionDepth = 0;
    }

    internal void Reset()
    {
        Clock.Reset();
        LoopIterationCount = 0;
        TemplateCount = 0;
        RecursionDepth = 0;
    }
}
