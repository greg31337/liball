using System;
using System.Collections.Generic;

namespace xml;

internal class XmlPullContext
{
    internal string? BaseUri;
    internal string? Language;
    internal XmlSpaceMode SpaceMode;

    internal XmlPullContext(string? baseUri, string? language, XmlSpaceMode spaceMode)
    {
        BaseUri = baseUri;
        Language = language;
        SpaceMode = spaceMode;
    }
}

internal class PullDriver
{
    internal XmlParserStatus Status;
    internal XmlReaderState State;
    internal XmlInput? Input;
    internal XmlParserOptions? Options;
    internal IXmlResourceResolver? Resolver;
    internal IXmlErrorReporter? ErrorReporter;
    internal IXmlCancellation? Cancellation;
    internal XmlEvent[] Events;
    internal XmlReaderState[] EventStates;
    internal int NextEventIndex;
    internal int CurrentEventIndex;
    internal bool Parsed;
    internal bool InitialInputReleased;
    internal XmlDocument? ParsedDocument;

    internal PullDriver()
    {
        Status = XmlParserStatus.Ready;
        State = CreateXmlPullIdleState(Status, null, null);
        Events = [];
        EventStates = [];
        NextEventIndex = 0;
        CurrentEventIndex = -1;
        Parsed = false;
        InitialInputReleased = false;
    }

    internal XmlReaderState BeginXmlPullDriver(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
    {
        Guard.NotNull(input, "input");
        Guard.NotNull(options, "options");
        if (Status != XmlParserStatus.Ready)
        {
            throw new InvalidOperationException("The XML pull reader is not ready to begin.");
        }

        XmlParserEngine validator = new XmlParserEngine(input, options, resolver, reporter, cancellation, options.ParseFragment);
        Input = input;
        Options = options;
        Resolver = resolver;
        ErrorReporter = reporter;
        Cancellation = cancellation;
        Events = [];
        EventStates = [];
        NextEventIndex = 0;
        CurrentEventIndex = -1;
        Parsed = false;
        InitialInputReleased = false;
        ParsedDocument = null;
        Status = XmlParserStatus.Running;
        State = CreateXmlPullIdleState(Status, options, input.BaseUri);
        return State;
    }

    internal XmlReaderState AdvanceXmlPullDriver()
    {
        if (Status == XmlParserStatus.Closed)
        {
            throw new InvalidOperationException("A closed XML pull reader cannot advance.");
        }

        if (Status == XmlParserStatus.Ready)
        {
            throw new InvalidOperationException("The XML pull reader has not begun parsing.");
        }

        if (Status == XmlParserStatus.Error)
        {
            throw new InvalidOperationException("The XML pull reader cannot advance after an error.");
        }

        if (Status == XmlParserStatus.Completed)
        {
            State.Status = Status;
            return State;
        }

        EnsureXmlPullParsed();
        if (NextEventIndex >= EventStates.Length)
        {
            Status = XmlParserStatus.Completed;
            CurrentEventIndex = -1;
            State = CreateXmlPullIdleState(Status, Options, Input == null ? null : Input.BaseUri);
            return State;
        }

        CurrentEventIndex = NextEventIndex;
        State = EventStates[NextEventIndex];
        NextEventIndex = NextEventIndex + 1;
        Status = NextEventIndex == EventStates.Length ? XmlParserStatus.Completed : XmlParserStatus.Running;
        State.Status = Status;
        return State;
    }

    internal XmlReaderState InspectXmlPullDriver()
    {
        State.Status = Status;
        return State;
    }

    internal XmlReaderState SkipXmlPullDriverSubtree()
    {
        if (Status == XmlParserStatus.Closed)
        {
            throw new InvalidOperationException("A closed XML pull reader cannot skip a subtree.");
        }

        if (Status == XmlParserStatus.Ready)
        {
            throw new InvalidOperationException("The XML pull reader has not begun parsing.");
        }

        if (Status == XmlParserStatus.Error)
        {
            throw new InvalidOperationException("The XML pull reader cannot skip after an error.");
        }

        if (CurrentEventIndex < 0 || CurrentEventIndex >= Events.Length || Events[CurrentEventIndex].Kind != XmlEventKind.StartElement)
        {
            throw new InvalidOperationException("The pull cursor must be on a start-element event to skip a subtree.");
        }

        int depth = Events[CurrentEventIndex].Depth;
        int index = NextEventIndex;
        bool foundEnd = false;
        while (index < Events.Length)
        {
            XmlEvent item = Events[index];
            index = index + 1;
            if (item.Kind == XmlEventKind.EndElement && item.Depth == depth)
            {
                foundEnd = true;
                break;
            }
        }

        if (!foundEnd)
        {
            Status = XmlParserStatus.Error;
            State.Status = Status;
            throw new InvalidOperationException("The current XML element has no matching end event.");
        }

        while (index < Events.Length && Events[index].Kind == XmlEventKind.EndNamespace && Events[index].Depth == depth)
        {
            index = index + 1;
        }

        NextEventIndex = index;
        CurrentEventIndex = -1;
        Status = XmlParserStatus.Running;
        return AdvanceXmlPullDriver();
    }

    internal void CloseXmlPullDriver()
    {
        if (Status == XmlParserStatus.Closed)
        {
            return;
        }

        ReleaseXmlPullInput();
        Input = null;
        Options = null;
        Resolver = null;
        ErrorReporter = null;
        Cancellation = null;
        Events = [];
        EventStates = [];
        NextEventIndex = 0;
        CurrentEventIndex = -1;
        ParsedDocument = null;
        Status = XmlParserStatus.Closed;
        State = CreateXmlPullIdleState(Status, null, null);
    }

    internal void EnsureXmlPullParsed()
    {
        if (Parsed)
        {
            return;
        }

        XmlInput? optionalInput = Input;
        XmlParserOptions? optionalOptions = Options;
        if (optionalInput == null || optionalOptions == null)
        {
            Status = XmlParserStatus.Error;
            State.Status = Status;
            throw new InvalidOperationException("The XML pull reader has no configured input.");
        }

        Status = XmlParserStatus.Error;
        State.Status = Status;
        XmlParserEngine engine = new XmlParserEngine(optionalInput, optionalOptions, Resolver, ErrorReporter, Cancellation, optionalOptions.ParseFragment);
        if (optionalInput.Kind == XmlInputKind.Stream || optionalInput.Kind == XmlInputKind.TextReader)
        {
            InitialInputReleased = true;
        }

        ParsedDocument = engine.ParseXmlInput();
        Events = engine.SnapshotXmlParserEvents();
        EventStates = CreateXmlPullEventStates(Events, optionalOptions, optionalInput.BaseUri);
        Parsed = true;
        Status = XmlParserStatus.Running;
        State.Status = Status;
    }

    internal XmlReaderState[] CreateXmlPullEventStates(XmlEvent[] events, XmlParserOptions options, string? baseUri)
    {
        XmlReaderState[] states = new XmlReaderState[events.Length];
        List<XmlPullContext> contexts = new List<XmlPullContext>();
        XmlPullContext context = new XmlPullContext(baseUri, null, XmlSpaceMode.Default);
        int index = 0;
        while (index < events.Length)
        {
            XmlEvent item = events[index];
            if (item.Kind == XmlEventKind.StartElement)
            {
                contexts.Add(context);
                context = CreateXmlPullChildContext(context, item.Attributes);
                states[index] = CreateXmlPullEventState(item, options, context);
            }
            else if (item.Kind == XmlEventKind.EndElement)
            {
                states[index] = CreateXmlPullEventState(item, options, context);
                if (contexts.Count > 0)
                {
                    context = contexts[contexts.Count - 1];
                    contexts.RemoveAt(contexts.Count - 1);
                }
            }
            else
            {
                states[index] = CreateXmlPullEventState(item, options, context);
            }

            index = index + 1;
        }

        return states;
    }

    internal XmlPullContext CreateXmlPullChildContext(XmlPullContext parent, XmlAttributeData[]? attributes)
    {
        XmlPullContext child = new XmlPullContext(parent.BaseUri, parent.Language, parent.SpaceMode);
        if (attributes == null)
        {
            return child;
        }

        int index = 0;
        while (index < attributes.Length)
        {
            XmlAttributeData attribute = attributes[index];
            XmlName? name = attribute.Name;
            string? value = attribute.Value;
            if (name != null && value != null && IsXmlPullAttribute(name, "base"))
            {
                child.BaseUri = ResolveXmlPullBaseUri(parent.BaseUri, value);
            }
            else if (name != null && value != null && IsXmlPullAttribute(name, "lang"))
            {
                child.Language = value.Length == 0 ? null : value;
            }
            else if (name != null && value == "preserve" && IsXmlPullAttribute(name, "space"))
            {
                child.SpaceMode = XmlSpaceMode.Preserve;
            }
            else if (name != null && value == "default" && IsXmlPullAttribute(name, "space"))
            {
                child.SpaceMode = XmlSpaceMode.Default;
            }

            index = index + 1;
        }

        return child;
    }

    internal bool IsXmlPullAttribute(XmlName name, string localName)
    {
        string? eventLocalName = name.LocalName;
        bool correctLocalName = string.Equals(eventLocalName, localName, StringComparison.Ordinal);
        bool xmlNamespace = string.Equals(name.NamespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal);
        bool xmlPrefix = string.Equals(name.Prefix, "xml", StringComparison.Ordinal);
        return correctLocalName && (xmlNamespace || xmlPrefix);
    }

    internal string? ResolveXmlPullBaseUri(string? parentBaseUri, string value)
    {
        Uri reference = UriTools.CreateReference(value);
        if (reference.IsAbsoluteUri)
        {
            return UriTools.CanonicalAbsoluteUri(reference);
        }

        if (parentBaseUri == null)
        {
            return value;
        }

        Uri parent = UriTools.CreateReference(parentBaseUri);
        if (!parent.IsAbsoluteUri)
        {
            return value;
        }

        return UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(parent, value));
    }

    internal XmlReaderState CreateXmlPullEventState(XmlEvent item, XmlParserOptions options, XmlPullContext context)
    {
        XmlReaderState state = new XmlReaderState();
        state.Status = XmlParserStatus.Running;
        state.Kind = item.Kind;
        state.NodeKind = item.NodeKind;
        state.Name = item.Name;
        state.Value = item.Value;
        state.Attributes = item.Attributes;
        state.NamespaceBindings = item.NamespaceBindings;
        state.Depth = item.Depth;
        state.BaseUri = context.BaseUri;
        state.Language = context.Language;
        state.SpaceMode = context.SpaceMode;
        state.IsEmptyElement = item.IsEmptyElement;
        state.IsIgnorableWhitespace = item.IsIgnorableWhitespace;
        state.Range = item.Range;
        state.Diagnostic = item.Diagnostic;
        state.ElementDeclaration = item.ElementDeclaration;
        state.AttributeDeclaration = item.AttributeDeclaration;
        state.EntityDeclaration = item.EntityDeclaration;
        state.NotationDeclaration = item.NotationDeclaration;
        state.PublicIdentifier = item.PublicIdentifier;
        state.SystemIdentifier = item.SystemIdentifier;
        state.Options = options;
        return state;
    }

    internal XmlReaderState CreateXmlPullIdleState(XmlParserStatus status, XmlParserOptions? options, string? baseUri)
    {
        XmlReaderState state = new XmlReaderState();
        state.Status = status;
        state.BaseUri = baseUri;
        state.SpaceMode = XmlSpaceMode.Default;
        state.Options = options;
        return state;
    }

    internal void ReleaseXmlPullInput()
    {
        XmlInput? input = Input;
        if (InitialInputReleased || input == null || !input.OwnsResource)
        {
            return;
        }

        if (input.Kind == XmlInputKind.Stream && input.ByteStream != null)
        {
            input.ByteStream.Dispose();
        }
        else if (input.Kind == XmlInputKind.TextReader && input.TextReader != null)
        {
            input.TextReader.Dispose();
        }

        InitialInputReleased = true;
    }
}
