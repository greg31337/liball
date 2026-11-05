using System;
using System.Collections.Generic;

namespace xml;

internal class XmlParserState
{
    internal XmlScanner? Scanner;
    internal ParserEventQueue? EventQueue;
    internal IXmlErrorReporter? ErrorReporter;
    internal List<XmlDiagnostic> Diagnostics;
    internal XmlParserStatus Status;
    internal XmlWellFormednessException? FatalError;

    internal XmlParserState(XmlScanner scanner, ParserEventQueue eventQueue, IXmlErrorReporter? errorReporter)
    {
        Guard.NotNull(scanner, "scanner");
        Guard.NotNull(eventQueue, "eventQueue");
        Scanner = scanner;
        EventQueue = eventQueue;
        ErrorReporter = errorReporter;
        Diagnostics = new List<XmlDiagnostic>();
        Status = XmlParserStatus.Ready;
        FatalError = null;
    }

    internal XmlParserStatus CurrentXmlParserStatus()
    {
        return Status;
    }

    internal void BeginXmlParsing()
    {
        ThrowFatalXmlParserError();
        if (Status == XmlParserStatus.Closed)
        {
            throw new InvalidOperationException("A closed XML parser cannot be started.");
        }

        if (Status != XmlParserStatus.Ready)
        {
            throw new InvalidOperationException("The XML parser is not ready to start.");
        }

        Status = XmlParserStatus.Running;
    }

    internal void PauseXmlParsing()
    {
        ThrowFatalXmlParserError();
        if (Status != XmlParserStatus.Running)
        {
            throw new InvalidOperationException("Only a running XML parser can be paused.");
        }

        Status = XmlParserStatus.Paused;
    }

    internal void ResumeXmlParsing()
    {
        ThrowFatalXmlParserError();
        if (Status != XmlParserStatus.Paused && Status != XmlParserStatus.NeedInput)
        {
            throw new InvalidOperationException("The XML parser is not paused or waiting for input.");
        }

        Status = XmlParserStatus.Running;
    }

    internal void NeedXmlParserInput()
    {
        ThrowFatalXmlParserError();
        if (Status != XmlParserStatus.Running)
        {
            throw new InvalidOperationException("Only a running XML parser can wait for input.");
        }

        Status = XmlParserStatus.NeedInput;
    }

    internal void CompleteXmlParsing()
    {
        ThrowFatalXmlParserError();
        if (Status != XmlParserStatus.Running)
        {
            throw new InvalidOperationException("Only a running XML parser can complete.");
        }

        Status = XmlParserStatus.Completed;
    }

    internal void AbortXmlParsing()
    {
        ThrowFatalXmlParserError();
        if (Status == XmlParserStatus.Closed)
        {
            throw new InvalidOperationException("A closed XML parser cannot be aborted.");
        }

        if (Status == XmlParserStatus.Completed || Status == XmlParserStatus.Aborted)
        {
            throw new InvalidOperationException("The XML parser has already stopped.");
        }

        Status = XmlParserStatus.Aborted;
        if (EventQueue != null)
        {
            EventQueue.ClearParserEvents();
        }
    }

    internal void EnsureXmlParsingCanContinue()
    {
        ThrowFatalXmlParserError();
        if (Status != XmlParserStatus.Running)
        {
            throw new InvalidOperationException("The XML parser is not running.");
        }
    }

    internal void RecordXmlParserDiagnostic(XmlDiagnostic diagnostic)
    {
        Guard.NotNull(diagnostic, "diagnostic");
        ThrowFatalXmlParserError();
        if (Status == XmlParserStatus.Closed)
        {
            throw new InvalidOperationException("A closed XML parser cannot report diagnostics.");
        }

        RetainXmlParserDiagnostic(diagnostic);
    }

    internal XmlDiagnostic[] SnapshotXmlParserDiagnostics()
    {
        return Diagnostics.ToArray();
    }

    internal XmlWellFormednessException MapXmlWellFormednessException(FormatException failure, SourceLocation? location)
    {
        Guard.NotNull(failure, "failure");
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.MalformedMarkup;
        diagnostic.Category = XmlErrorCategory.Xml;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = failure.Message;
        diagnostic.RelatedLocation = null;
        diagnostic.EntityStack = null;
        diagnostic.IncludeStack = null;
        diagnostic.TemplateStack = null;
        if (location != null)
        {
            SourceRange range = new SourceRange();
            range.Start = location;
            range.End = location;
            diagnostic.Range = range;
        }

        return new XmlWellFormednessException(failure.Message, diagnostic, location);
    }

    internal XmlWellFormednessException FailXmlParsing(FormatException failure, SourceLocation? location)
    {
        Guard.NotNull(failure, "failure");
        if (Status == XmlParserStatus.Closed)
        {
            throw new InvalidOperationException("A closed XML parser cannot fail parsing.");
        }

        if (FatalError != null)
        {
            return FatalError;
        }

        XmlWellFormednessException mapped = MapXmlWellFormednessException(failure, location);
        FatalError = mapped;
        Status = XmlParserStatus.Error;
        if (EventQueue != null)
        {
            EventQueue.ClearParserEvents();
        }

        if (mapped.Diagnostic != null)
        {
            RetainXmlParserDiagnostic(mapped.Diagnostic);
        }

        return mapped;
    }

    internal void ResetXmlParser()
    {
        if (Status == XmlParserStatus.Closed)
        {
            throw new InvalidOperationException("A closed XML parser cannot be reset.");
        }

        if (Scanner != null)
        {
            Scanner.ResetXmlScanner();
        }

        if (EventQueue != null)
        {
            EventQueue.ClearParserEvents();
        }

        Diagnostics.Clear();
        FatalError = null;
        Status = XmlParserStatus.Ready;
    }

    internal void CloseXmlParser()
    {
        if (Status == XmlParserStatus.Closed)
        {
            return;
        }

        if (EventQueue != null)
        {
            EventQueue.ClearParserEvents();
        }

        Scanner = null;
        EventQueue = null;
        ErrorReporter = null;
        Status = XmlParserStatus.Closed;
    }

    internal void RetainXmlParserDiagnostic(XmlDiagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
        if (ErrorReporter != null)
        {
            ErrorReporter.ReportXmlDiagnostic(diagnostic);
        }
    }

    internal void ThrowFatalXmlParserError()
    {
        if (Status == XmlParserStatus.Error && FatalError != null)
        {
            throw FatalError;
        }
    }
}
