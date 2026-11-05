namespace pop3;

internal class Pop3PipelineRequest
{
    internal Pop3Command Command = new Pop3Command();
    internal bool DependsOnPrevious;
    internal System.IO.Stream? MultilineDestination;
    internal int MaximumLineOctets = Pop3Defaults.DefaultMaximumMessageLineOctets;
    internal int MemoryCeilingBytes = Pop3Defaults.DefaultMessageMemoryCeilingBytes;
    internal IPop3TransferObserver? TransferObserver;
    internal Pop3TransferKind TransferKind = Pop3TransferKind.ExtensionData;
    internal int MessageNumber;
    internal long ExpectedBytes = -1;
}

internal class Pop3PipelineResult
{
    internal Pop3Command Command = new Pop3Command();
    internal Pop3Response Response = new Pop3Response();
    internal byte[] MultilineData = new byte[0];
    internal long MultilineOctets;
}

internal class Pop3ClientPipeline
{
    Pop3WireReader Reader;
    Pop3WireWriter Writer;

    internal Pop3ClientPipeline(Pop3WireReader reader, Pop3WireWriter writer)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }
        Reader = reader;
        Writer = writer;
    }

    internal bool IsEnabled(
        Pop3ClientOptions options,
        Pop3CapabilitySet capabilities)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (capabilities == null)
        {
            throw new System.ArgumentNullException("capabilities");
        }
        return options.ForcePipelining ||
            options.EnablePipelining && capabilities.SupportsPipelining;
    }

    internal bool IsPipelineSafe(Pop3Command command)
    {
        if (command == null || command.Keyword == null || command.Arguments == null)
        {
            return false;
        }
        string keyword = command.Keyword.ToUpperInvariant();
        if (keyword == Pop3Commands.Stat ||
            keyword == Pop3Commands.List ||
            keyword == Pop3Commands.Uidl ||
            keyword == Pop3Commands.Retr ||
            keyword == Pop3Commands.Top ||
            keyword == Pop3Commands.Dele ||
            keyword == Pop3Commands.Noop ||
            keyword == Pop3Commands.Capa)
        {
            return true;
        }
        return keyword == Pop3Commands.Lang && command.Arguments.Length == 0;
    }

    internal Pop3PipelineResult[] Execute(
        Pop3PipelineRequest[] requests,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateRequests(requests);
        cancellationToken.ThrowIfCancellationRequested();

        int index = 0;
        while (index < requests.Length)
        {
            Writer.WriteCommand(
                requests[index].Command,
                Pop3Defaults.MaximumCommandLineOctets,
                cancellationToken);
            index++;
        }

        Pop3PipelineResult[] results = new Pop3PipelineResult[requests.Length];
        index = 0;
        while (index < requests.Length)
        {
            Pop3PipelineRequest request = requests[index];
            Pop3Response response = Reader.ReadResponse(
                Pop3Defaults.MaximumResponseLineOctets,
                cancellationToken);
            if (response.Kind == Pop3ResponseKind.SaslContinuation)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 pipeline command returned an unexpected continuation response.");
            }

            Pop3PipelineResult result = new Pop3PipelineResult();
            result.Command = request.Command;
            result.Response = response;
            if (response.Kind == Pop3ResponseKind.Positive &&
                request.Command.ExpectsMultilineResponse)
            {
                response.IsMultiline = true;
                if (request.Command.Keyword.ToUpperInvariant() == Pop3Commands.Retr)
                {
                    response.AdvisoryOctetCount = ParseAdvisoryOctetCount(response);
                }
                ReadMultilineResult(request, result, cancellationToken);
            }
            results[index] = result;
            index++;
        }
        return results;
    }

    void ValidateRequests(Pop3PipelineRequest[] requests)
    {
        if (requests == null)
        {
            throw new System.ArgumentNullException("requests");
        }

        Pop3CommandParser parser = new Pop3CommandParser();
        int index = 0;
        while (index < requests.Length)
        {
            Pop3PipelineRequest request = requests[index];
            if (request == null)
            {
                throw new System.ArgumentException(
                    "A POP3 pipeline request cannot be null.",
                    "requests");
            }
            parser.FormatCommand(request.Command);
            if (!IsPipelineSafe(request.Command))
            {
                throw new System.InvalidOperationException(
                    "The POP3 command is a pipeline barrier or is not known to be pipeline-safe.");
            }
            if (request.DependsOnPrevious)
            {
                throw new System.InvalidOperationException(
                    "A dependent POP3 command must execute after the preceding pipeline completes successfully.");
            }

            bool expectsMultiline = ExpectsMultilineResponse(request.Command);
            if (request.Command.ExpectsMultilineResponse != expectsMultiline)
            {
                throw new System.ArgumentException(
                    "The POP3 command has incorrect multiline response metadata.",
                    "requests");
            }
            ValidateMultilineRequest(request, expectsMultiline);
            index++;
        }
    }

    bool ExpectsMultilineResponse(Pop3Command command)
    {
        string keyword = command.Keyword.ToUpperInvariant();
        if (keyword == Pop3Commands.Capa ||
            keyword == Pop3Commands.Retr ||
            keyword == Pop3Commands.Top)
        {
            return true;
        }
        if (keyword == Pop3Commands.List ||
            keyword == Pop3Commands.Uidl ||
            keyword == Pop3Commands.Lang)
        {
            return command.Arguments.Length == 0;
        }
        return false;
    }

    void ValidateMultilineRequest(
        Pop3PipelineRequest request,
        bool expectsMultiline)
    {
        if (!expectsMultiline)
        {
            if (request.MultilineDestination != null)
            {
                throw new System.ArgumentException(
                    "A single-line POP3 pipeline request cannot have a multiline destination.",
                    "requests");
            }
            return;
        }
        if (request.MaximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("requests");
        }
        if (request.MultilineDestination == null && request.MemoryCeilingBytes < 0)
        {
            throw new System.ArgumentOutOfRangeException("requests");
        }
        if (request.MultilineDestination != null &&
            !request.MultilineDestination.CanWrite)
        {
            throw new System.ArgumentException(
                "A POP3 pipeline multiline destination must be writable.",
                "requests");
        }
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(
            request.TransferObserver);
        transfer.ValidateMetadata(
            request.TransferKind,
            request.MessageNumber,
            request.ExpectedBytes);
    }

    void ReadMultilineResult(
        Pop3PipelineRequest request,
        Pop3PipelineResult result,
        System.Threading.CancellationToken cancellationToken)
    {
        long expectedBytes = request.ExpectedBytes;
        if (expectedBytes < 0 && result.Response.AdvisoryOctetCount >= 0)
        {
            expectedBytes = result.Response.AdvisoryOctetCount;
        }
        if (request.MultilineDestination == null)
        {
            result.MultilineData = Reader.MultilineReader.ReadMultilineData(
                request.MaximumLineOctets,
                request.MemoryCeilingBytes,
                request.TransferObserver,
                request.TransferKind,
                request.MessageNumber,
                expectedBytes,
                cancellationToken);
            result.MultilineOctets = result.MultilineData.LongLength;
            return;
        }

        result.MultilineOctets = Reader.MultilineReader.CopyMultilineData(
            request.MultilineDestination,
            request.MaximumLineOctets,
            request.TransferObserver,
            request.TransferKind,
            request.MessageNumber,
            expectedBytes,
            cancellationToken);
    }

    long ParseAdvisoryOctetCount(Pop3Response response)
    {
        string text = response.Text;
        int start = 0;
        if (response.ResponseCodeLevels.Length > 0)
        {
            int responseCodeEnd = text.IndexOf(']');
            start = responseCodeEnd + 1;
            if (start >= text.Length || text[start] != ' ')
            {
                return -1;
            }
            start++;
        }

        int end = start;
        while (end < text.Length && text[end] >= '0' && text[end] <= '9')
        {
            end++;
        }
        if (end == start)
        {
            return -1;
        }
        if (end < text.Length && text[end] != ' ')
        {
            return -1;
        }

        byte[] digits = System.Text.Encoding.ASCII.GetBytes(
            text.Substring(start, end - start));
        Pop3NumberParseResult parsed = new Pop3NumberParser().ParseSignedDecimal(
            digits,
            0,
            long.MaxValue);
        if (!parsed.Success)
        {
            return -1;
        }
        return parsed.Value;
    }
}
