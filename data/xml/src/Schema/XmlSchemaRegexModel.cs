using System.Collections.Generic;

namespace xml;

internal enum XmlSchemaRegexNodeKind
{
    Empty = 0,
    Sequence = 1,
    Alternation = 2,
    Literal = 3,
    Dot = 4,
    CharacterClass = 5,
    Capture = 6,
    BackReference = 7,
    Repeat = 8,
    StartAnchor = 9,
    EndAnchor = 10
}

internal class XmlSchemaRegexProperty
{
    internal string Name;
    internal bool Negated;

    internal XmlSchemaRegexProperty(string name, bool negated)
    {
        Name = name;
        Negated = negated;
    }
}

internal class XmlSchemaRegexCharacterSet
{
    internal List<int> RangeStarts;
    internal List<int> RangeEnds;
    internal List<XmlSchemaRegexProperty> Properties;
    internal bool Negated;
    internal XmlSchemaRegexCharacterSet? Subtraction;

    internal XmlSchemaRegexCharacterSet(bool negated)
    {
        RangeStarts = new List<int>();
        RangeEnds = new List<int>();
        Properties = new List<XmlSchemaRegexProperty>();
        Negated = negated;
        Subtraction = null;
    }
}

internal class XmlSchemaRegexClassAtom
{
    internal bool Single;
    internal int Codepoint;
    internal XmlSchemaRegexProperty? Property;

    internal XmlSchemaRegexClassAtom(bool single, int codepoint, XmlSchemaRegexProperty? property)
    {
        Single = single;
        Codepoint = codepoint;
        Property = property;
    }
}

internal class XmlSchemaRegexNode
{
    internal XmlSchemaRegexNodeKind Kind;
    internal XmlSchemaRegexNode[] Children;
    internal int Codepoint;
    internal XmlSchemaRegexCharacterSet? CharacterSet;
    internal int Minimum;
    internal int Maximum;
    internal bool Reluctant;
    internal int CaptureNumber;

    internal XmlSchemaRegexNode(XmlSchemaRegexNodeKind kind)
    {
        Kind = kind;
        Children = [];
        Codepoint = 0;
        CharacterSet = null;
        Minimum = 0;
        Maximum = 0;
        Reluctant = false;
        CaptureNumber = 0;
    }
}

internal class XmlSchemaRegexProgram
{
    internal XmlSchemaRegexNode Root;
    internal int CaptureCount;
    internal bool DotAll;
    internal bool MultiLine;
    internal bool IgnoreCase;
    internal bool HasBackReferences;

    internal XmlSchemaRegexProgram(XmlSchemaRegexNode root, int captureCount, bool dotAll, bool multiLine, bool ignoreCase, bool hasBackReferences)
    {
        Root = root;
        CaptureCount = captureCount;
        DotAll = dotAll;
        MultiLine = multiLine;
        IgnoreCase = ignoreCase;
        HasBackReferences = hasBackReferences;
    }
}

internal class XmlSchemaRegexCompilationResult
{
    internal bool Success;
    internal XmlSchemaRegexProgram? Program;
    internal string? ErrorMessage;

    internal XmlSchemaRegexCompilationResult(bool success, XmlSchemaRegexProgram? program, string? errorMessage)
    {
        Success = success;
        Program = program;
        ErrorMessage = errorMessage;
    }
}

internal class XmlSchemaRegexMatch
{
    internal bool Success;
    internal int Index;
    internal int Length;
    internal string?[] Captures;
    internal int[] CaptureIndexes;
    internal int[] CaptureLengths;

    internal XmlSchemaRegexMatch(bool success, int captureCount)
    {
        Success = success;
        Index = -1;
        Length = 0;
        Captures = new string?[captureCount + 1];
        CaptureIndexes = new int[captureCount + 1];
        CaptureLengths = new int[captureCount + 1];
        int index = 0;
        while (index < CaptureIndexes.Length)
        {
            CaptureIndexes[index] = -1;
            CaptureLengths[index] = -1;
            index = index + 1;
        }
    }
}

internal class XmlSchemaRegexMatchState
{
    internal int Position;
    internal int[] CaptureStarts;
    internal int[] CaptureEnds;

    internal XmlSchemaRegexMatchState(int position, int captureCount)
    {
        Position = position;
        CaptureStarts = new int[captureCount + 1];
        CaptureEnds = new int[captureCount + 1];
        int index = 0;
        while (index < CaptureStarts.Length)
        {
            CaptureStarts[index] = -1;
            CaptureEnds[index] = -1;
            index = index + 1;
        }
    }
}

internal class XmlSchemaRegexRepeatFrame
{
    internal XmlSchemaRegexMatchState State;
    internal int Count;
    internal bool Emit;

    internal XmlSchemaRegexRepeatFrame(XmlSchemaRegexMatchState state, int count, bool emit)
    {
        State = state;
        Count = count;
        Emit = emit;
    }
}

internal class XmlSchemaRegexInput
{
    internal string Value;
    internal int[] Codepoints;
    internal int[] Utf16Boundaries;

    internal XmlSchemaRegexInput(string value, int[] codepoints, int[] utf16Boundaries)
    {
        Value = value;
        Codepoints = codepoints;
        Utf16Boundaries = utf16Boundaries;
    }
}
