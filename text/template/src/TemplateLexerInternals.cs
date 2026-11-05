using System;
using System.Collections.Generic;

namespace template;

internal class TemplateDelimiterMatch
{
    internal int Index;
    internal int Length;
    internal TemplateTokenKind StartKind;
    internal TemplateTokenKind EndKind;
    internal string StartText;
    internal string EndText;

    internal TemplateDelimiterMatch()
    {
        Index = -1;
        Length = 0;
        StartKind = TemplateTokenKind.Unknown;
        EndKind = TemplateTokenKind.Unknown;
        StartText = "";
        EndText = "";
    }
}

internal class TemplateBlockTagMatch
{
    internal bool Matched;
    internal int Start;
    internal int End;
    internal TemplateTrimKind LeftTrimKind;
    internal TemplateTrimKind RightTrimKind;

    internal TemplateBlockTagMatch()
    {
        Matched = false;
        Start = -1;
        End = -1;
        LeftTrimKind = TemplateTrimKind.None;
        RightTrimKind = TemplateTrimKind.None;
    }
}

internal class TemplateDelimiterChangeMatch
{
    internal bool Matched;
    internal bool Valid;
    internal int Start;
    internal int End;
    internal string OutputStartDelimiter;
    internal string OutputEndDelimiter;

    internal TemplateDelimiterChangeMatch()
    {
        Matched = false;
        Valid = false;
        Start = -1;
        End = -1;
        OutputStartDelimiter = "";
        OutputEndDelimiter = "";
    }
}
