namespace pop3.Tests;

internal sealed class FakeProtocolLogger : IPop3ProtocolLogger
{
    internal readonly List<Pop3LogEntry> Entries = new List<Pop3LogEntry>();
    internal Action<Pop3LogEntry>? Handler = null;

    public void Log(Pop3LogEntry entry)
    {
        Entries.Add(entry);
        if (Handler != null)
        {
            Handler(entry);
        }
    }
}

internal sealed class FakeTransferObserver : IPop3TransferObserver
{
    internal readonly List<Pop3TransferProgress> Reports = new List<Pop3TransferProgress>();
    internal Action<Pop3TransferProgress>? Handler = null;

    public void Report(Pop3TransferProgress progress)
    {
        Reports.Add(progress);
        if (Handler != null)
        {
            Handler(progress);
        }
    }
}

internal sealed class FakeServerExtension : IPop3ServerExtension
{
    internal Pop3Capability[] Capabilities = Array.Empty<Pop3Capability>();
    internal string[] CommandNames = Array.Empty<string>();
    internal Pop3SessionState[] DefaultAllowedStates = Array.Empty<Pop3SessionState>();
    internal readonly Dictionary<string, Pop3SessionState[]> AllowedStates =
        new Dictionary<string, Pop3SessionState[]>(StringComparer.OrdinalIgnoreCase);
    internal Pop3ExtensionResult Result = new Pop3ExtensionResult();
    internal Func<Pop3ExtensionContext, Pop3Capability[]>? CapabilitiesHandler = null;
    internal Func<string[]>? CommandNamesHandler = null;
    internal Func<string, Pop3SessionState[]>? AllowedStatesHandler = null;
    internal Func<Pop3ExtensionContext, Pop3Command, CancellationToken, Pop3ExtensionResult>? ExecuteHandler = null;
    internal readonly List<string> Calls = new List<string>();
    internal readonly List<Pop3ExtensionContext> CapabilityContexts =
        new List<Pop3ExtensionContext>();
    internal readonly List<string> AllowedStateCommands = new List<string>();
    internal readonly List<Pop3ExtensionContext> ExecuteContexts =
        new List<Pop3ExtensionContext>();
    internal readonly List<Pop3Command> ExecutedCommands = new List<Pop3Command>();
    internal int GetCapabilitiesCallCount;
    internal int GetCommandNamesCallCount;
    internal int GetAllowedStatesCallCount;
    internal int ExecuteCallCount;
    internal CancellationToken LastCancellationToken;
    internal Stream? LastResultMultilineStream;

    public Pop3Capability[] GetCapabilities(Pop3ExtensionContext context)
    {
        GetCapabilitiesCallCount++;
        Calls.Add("capabilities");
        CapabilityContexts.Add(context);
        if (CapabilitiesHandler != null)
        {
            return CapabilitiesHandler(context);
        }
        return Capabilities;
    }

    public string[] GetCommandNames()
    {
        GetCommandNamesCallCount++;
        Calls.Add("command-names");
        if (CommandNamesHandler != null)
        {
            return CommandNamesHandler();
        }
        return CommandNames;
    }

    public Pop3SessionState[] GetAllowedStates(string commandName)
    {
        GetAllowedStatesCallCount++;
        Calls.Add("allowed-states:" + commandName);
        AllowedStateCommands.Add(commandName);
        if (AllowedStatesHandler != null)
        {
            return AllowedStatesHandler(commandName);
        }
        if (AllowedStates.TryGetValue(commandName, out Pop3SessionState[]? states))
        {
            return states;
        }
        return DefaultAllowedStates;
    }

    public Pop3ExtensionResult ExecuteCommand(
        Pop3ExtensionContext context,
        Pop3Command command,
        CancellationToken cancellationToken)
    {
        ExecuteCallCount++;
        Calls.Add("execute:" + command.Keyword);
        ExecuteContexts.Add(context);
        ExecutedCommands.Add(command);
        LastCancellationToken = cancellationToken;
        Pop3ExtensionResult returnedResult = Result;
        if (ExecuteHandler != null)
        {
            returnedResult = ExecuteHandler(context, command, cancellationToken);
        }
        LastResultMultilineStream = returnedResult.MultilineData;
        return returnedResult;
    }
}
