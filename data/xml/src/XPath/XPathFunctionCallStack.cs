using System;
using System.Collections.Generic;

namespace xml;

internal class XPathFunctionCallFrame
{
    internal XmlName? Name;
    internal XdmSequence[] Arguments;
    internal SourceRange Range;

    internal XPathFunctionCallFrame(XmlName? name, XdmSequence[] arguments, SourceRange range)
    {
        Name = name;
        Arguments = arguments;
        Range = range;
    }
}

internal class XPathFunctionCallStack
{
    internal List<XPathFunctionCallFrame> Frames;
    internal int MaximumDepth;

    internal XPathFunctionCallStack(int maximumDepth)
    {
        if (maximumDepth <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        Frames = new List<XPathFunctionCallFrame>();
        MaximumDepth = maximumDepth;
    }

    internal void PushXPathFunctionCall(XmlName? name, XdmSequence[] arguments, SourceRange range)
    {
        if (Frames.Count >= MaximumDepth)
        {
            throw CreateXPathFunctionDepthLimit(range);
        }

        XdmSequence[] snapshots = new XdmSequence[arguments.Length];
        int index = 0;
        while (index < arguments.Length)
        {
            snapshots[index] = new XdmSequenceOperations().FreezeXdmSequenceStorage(arguments[index]);
            index = index + 1;
        }

        Frames.Add(new XPathFunctionCallFrame(CopyXPathFunctionName(name), snapshots, CopyXPathFunctionRange(range)));
    }

    internal XPathFunctionCallFrame ReadXPathFunctionCall()
    {
        if (Frames.Count == 0)
        {
            throw new InvalidOperationException("No XPath function call frame is active.");
        }

        return Frames[Frames.Count - 1];
    }

    internal void PopXPathFunctionCall()
    {
        if (Frames.Count == 0)
        {
            throw new InvalidOperationException("No XPath function call frame is active.");
        }

        Frames.RemoveAt(Frames.Count - 1);
    }

    internal XmlLimitException CreateXPathFunctionDepthLimit(SourceRange range)
    {
        string message = "The configured XPath function call depth limit of " + MaximumDepth + " was exceeded.";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.LimitExceeded;
        diagnostic.Category = XmlErrorCategory.Limit;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathFunctionRange(range);
        return new XmlLimitException(message, diagnostic, diagnostic.Range.Start);
    }

    internal XmlName? CopyXPathFunctionName(XmlName? name)
    {
        if (name == null)
        {
            return null;
        }

        XmlName result = new XmlName();
        result.Prefix = name.Prefix;
        result.LocalName = name.LocalName;
        result.NamespaceUri = name.NamespaceUri;
        result.QualifiedName = name.QualifiedName;
        return result;
    }

    internal SourceRange CopyXPathFunctionRange(SourceRange range)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathFunctionLocation(range.Start);
        result.End = CopyXPathFunctionLocation(range.End);
        return result;
    }

    internal SourceLocation? CopyXPathFunctionLocation(SourceLocation? location)
    {
        if (location == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = location.SourceUri;
        result.CharacterOffset = location.CharacterOffset;
        result.ByteOffset = location.ByteOffset;
        result.Line = location.Line;
        result.Column = location.Column;
        return result;
    }
}
