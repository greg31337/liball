using System;
using System.IO;

namespace fix;

/// <summary>Adapts a caller-supplied connected duplex stream to session transport operations.</summary>
public class StreamSessionTransport : ISessionTransport
{
    internal Stream ConnectedStream;
    internal TransportState CurrentState = TransportState.Connected;
    internal bool StreamClosed;

    /// <summary>Creates a connected transport over a readable and writable stream.</summary>
    /// <param name="connectedStream">The caller-supplied connected duplex stream.</param>
    /// <exception cref="FixTransportException">Thrown when the supplied stream is missing or is not duplex.</exception>
    public StreamSessionTransport(Stream connectedStream)
    {
        if (connectedStream == null)
        {
            throw new FixTransportException("The connected transport stream is missing.");
        }
        if (!connectedStream.CanRead || !connectedStream.CanWrite)
        {
            throw new FixTransportException("The connected transport stream must be readable and writable.");
        }
        ConnectedStream = connectedStream;
    }

    /// <summary>Confirms that the caller-supplied stream remains connected and usable.</summary>
    /// <param name="endpoint">The endpoint associated with the connected stream.</param>
    /// <param name="tls">The TLS settings associated with the connected stream.</param>
    /// <returns>The current transport outcome.</returns>
    public TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls)
    {
        if (CurrentState == TransportState.Closed)
        {
            return CreateWriteFailure(TransportState.Closed, "transport-closed", "The stream transport is closed.");
        }
        if (CurrentState == TransportState.Faulted)
        {
            return CreateWriteFailure(TransportState.Faulted, "transport-faulted", "The stream transport is faulted.");
        }
        if (!ConnectedStream.CanRead || !ConnectedStream.CanWrite)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(TransportState.Faulted, "stream-not-duplex", "The connected transport stream is no longer readable and writable.");
        }
        CurrentState = TransportState.Connected;
        return CreateWriteSuccess(0);
    }

    /// <summary>Performs one synchronous stream read and preserves its partial byte count.</summary>
    /// <param name="destination">The destination storage.</param>
    /// <returns>The read count, EOF indication, and resulting state.</returns>
    public TransportReadResult ReadTransport(Span<byte> destination)
    {
        if (CurrentState == TransportState.Closed)
        {
            return CreateReadFailure(TransportState.Closed, "transport-closed", "The stream transport is closed.");
        }
        if (CurrentState == TransportState.Faulted)
        {
            return CreateReadFailure(TransportState.Faulted, "transport-faulted", "The stream transport is faulted.");
        }
        if (!ConnectedStream.CanRead)
        {
            CurrentState = TransportState.Faulted;
            return CreateReadFailure(TransportState.Faulted, "stream-not-readable", "The connected transport stream is not readable.");
        }
        TransportReadResult result = new TransportReadResult();
        result.State = TransportState.Connected;
        if (destination.Length == 0)
        {
            return result;
        }
        result.BytesRead = ConnectedStream.Read(destination);
        if (result.BytesRead == 0)
        {
            ConnectedStream.Close();
            StreamClosed = true;
            CurrentState = TransportState.Closed;
            result.State = TransportState.Closed;
            result.EndOfStream = true;
        }
        return result;
    }

    /// <summary>Synchronously writes the supplied byte slice to the connected stream.</summary>
    /// <param name="source">The source bytes.</param>
    /// <returns>The consumed byte count and resulting state.</returns>
    public TransportWriteResult WriteTransport(ReadOnlySpan<byte> source)
    {
        if (CurrentState == TransportState.Closed)
        {
            return CreateWriteFailure(TransportState.Closed, "transport-closed", "The stream transport is closed.");
        }
        if (CurrentState == TransportState.Faulted)
        {
            return CreateWriteFailure(TransportState.Faulted, "transport-faulted", "The stream transport is faulted.");
        }
        if (!ConnectedStream.CanWrite)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(TransportState.Faulted, "stream-not-writable", "The connected transport stream is not writable.");
        }
        if (source.Length != 0)
        {
            ConnectedStream.Write(source);
        }
        return CreateWriteSuccess(source.Length);
    }

    /// <summary>Closes the supplied stream and makes further transport operations inert.</summary>
    /// <returns>The closed transport outcome.</returns>
    public TransportWriteResult CloseTransport()
    {
        if (!StreamClosed)
        {
            ConnectedStream.Close();
            StreamClosed = true;
        }
        CurrentState = TransportState.Closed;
        TransportWriteResult result = new TransportWriteResult();
        result.State = TransportState.Closed;
        return result;
    }

    internal TransportWriteResult CreateWriteSuccess(int bytesWritten)
    {
        TransportWriteResult result = new TransportWriteResult();
        result.State = CurrentState;
        result.BytesWritten = bytesWritten;
        return result;
    }

    internal static TransportWriteResult CreateWriteFailure(TransportState state, string code, string message)
    {
        TransportWriteResult result = new TransportWriteResult();
        result.State = state;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    internal static TransportReadResult CreateReadFailure(TransportState state, string code, string message)
    {
        TransportReadResult result = new TransportReadResult();
        result.State = state;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
