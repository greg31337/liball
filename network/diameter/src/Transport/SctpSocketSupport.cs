using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal static class SctpSocketSupport
{
    internal const int SctpProtocolNumber = 132;
    internal const int SctpInitMessageOption = 2;
    internal const int SctpNoDelayOption = 3;
    internal const int SctpDefaultSendParametersOption = 10;

    internal static SctpProbeResult ProbeSctpAddressFamily(
        AddressFamily family,
        int streamCount)
    {
        SctpProbeResult result = new SctpProbeResult();
        if (!OperatingSystem.IsLinux())
        {
            result.Message = "Built-in SCTP sockets are available only on supported Linux hosts.";
            return result;
        }

        Task<Socket> probeTask = Task.Run(() => CreateConfiguredSctpSocket(
            family,
            streamCount,
            true,
            65536,
            65536));
        bool completed = SpinWait.SpinUntil(() => probeTask.IsCompleted, 2000);
        if (!completed)
        {
            result.Message = "SCTP capability probing timed out.";
            return result;
        }

        if (!probeTask.IsCompletedSuccessfully)
        {
            result.Message = TlsStreamDiameterConnection.ReadTlsTaskFailure(
                probeTask,
                "The host does not expose usable SCTP sockets.");
            return result;
        }

        Socket socket = probeTask.Result;
        socket.Close();
        result.Supported = true;
        return result;
    }

    internal static Socket CreateConfiguredSctpSocket(
        AddressFamily family,
        int streamCount,
        bool noDelay,
        int receiveBufferSize,
        int sendBufferSize)
    {
        Socket socket = new Socket(
            family,
            SocketType.Stream,
            (ProtocolType)SctpProtocolNumber);
        socket.ReceiveBufferSize = receiveBufferSize;
        socket.SendBufferSize = sendBufferSize;
        ConfigureSctpInitialization(socket, streamCount);
        ConfigureSctpNoDelay(socket, noDelay);
        ConfigureSctpDefaultSend(socket, 0, DiameterProtocolConstants.SctpPayloadProtocolIdentifier);
        return socket;
    }

    internal static void ConfigureSctpInitialization(Socket socket, int streamCount)
    {
        socket.SetSocketOption(
            (SocketOptionLevel)SctpProtocolNumber,
            (SocketOptionName)SctpInitMessageOption,
            CreateSctpInitializationBytes(streamCount));
    }

    internal static void ConfigureSctpNoDelay(Socket socket, bool noDelay)
    {
        socket.SetSocketOption(
            (SocketOptionLevel)SctpProtocolNumber,
            (SocketOptionName)SctpNoDelayOption,
            noDelay ? 1 : 0);
    }

    internal static void ConfigureSctpDefaultSend(
        Socket socket,
        int stream,
        uint payloadProtocolIdentifier)
    {
        socket.SetSocketOption(
            (SocketOptionLevel)SctpProtocolNumber,
            (SocketOptionName)SctpDefaultSendParametersOption,
            CreateSctpSendParameterBytes(stream, payloadProtocolIdentifier));
    }

    internal static byte[] CreateSctpInitializationBytes(int streamCount)
    {
        byte[] result = new byte[8];
        WriteHostUInt16(result, 0, streamCount);
        WriteHostUInt16(result, 2, streamCount);
        return result;
    }

    internal static byte[] CreateSctpSendParameterBytes(
        int stream,
        uint payloadProtocolIdentifier)
    {
        byte[] result = new byte[32];
        WriteHostUInt16(result, 0, stream);
        result[8] = (byte)(payloadProtocolIdentifier >> 24);
        result[9] = (byte)(payloadProtocolIdentifier >> 16);
        result[10] = (byte)(payloadProtocolIdentifier >> 8);
        result[11] = (byte)payloadProtocolIdentifier;
        return result;
    }

    internal static void WriteHostUInt16(byte[] destination, int offset, int value)
    {
        if (BitConverter.IsLittleEndian)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
        }
        else
        {
            destination[offset] = (byte)(value >> 8);
            destination[offset + 1] = (byte)value;
        }
    }
}

internal interface ISctpSocketCapabilityProbe
{
    SctpProbeResult ProbeSctpAddressFamily(
        AddressFamily family,
        int streamCount);
}

internal class SctpSocketCapabilityProbe : ISctpSocketCapabilityProbe
{
    /// <summary>Probes native SCTP socket support for one address family.</summary>
    /// <param name="family">The socket address family.</param>
    /// <param name="streamCount">The requested SCTP stream count.</param>
    /// <returns>The native SCTP capability outcome.</returns>
    public SctpProbeResult ProbeSctpAddressFamily(
        AddressFamily family,
        int streamCount)
    {
        return SctpSocketSupport.ProbeSctpAddressFamily(family, streamCount);
    }
}

internal class SctpProbeResult
{
    internal bool Supported;
    internal string Message = string.Empty;
}
