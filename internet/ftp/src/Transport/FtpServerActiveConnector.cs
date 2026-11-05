using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ftp;

internal class FtpServerActiveConnector
{
    internal IPAddress ControlPeerAddress;
    internal FtpAddressFamily ControlFamily;
    internal bool RequireDataPeerMatch;
    internal bool AllowForeignDataConnections;
    internal bool RejectPrivilegedActivePorts;
    internal FtpSocketOptions SocketOptions;
    internal bool ExtendedPassiveOnly;
    internal bool Configured;
    internal bool Extended;
    internal IPAddress? TargetAddress;
    internal FtpEndpoint TargetEndpoint;

    internal FtpServerActiveConnector(
        IPAddress? controlPeerAddress,
        FtpDataConnectionOptions? options)
    {
        ValidateConfiguration(controlPeerAddress, options);
        if (controlPeerAddress == null || options == null)
        {
            throw new FtpConfigurationException(
                "A control peer and server active options are required.", null);
        }

        ControlPeerAddress = FtpPeerPolicy.NormalizeAddress(controlPeerAddress);
        ControlFamily = FtpPeerPolicy.GetAddressFamily(controlPeerAddress);
        RequireDataPeerMatch = options.RequireDataPeerMatch;
        AllowForeignDataConnections = options.AllowForeignDataConnections;
        RejectPrivilegedActivePorts = options.RejectPrivilegedActivePorts;
        SocketOptions = FtpSocketSettings.CreateSnapshot(options.Socket);
        ExtendedPassiveOnly = false;
        Configured = false;
        Extended = false;
        TargetAddress = null;
        TargetEndpoint = new FtpEndpoint();
    }

    internal FtpReply ConfigurePort(string? argument)
    {
        ClearDataSetup();
        ValidateActiveSetupAllowed("PORT");
        FtpEndpoint endpoint = FtpEndpointCodec.ParsePortArgument(argument);
        ConfigureTarget(endpoint, false);
        return FtpEndpointCodec.CreateEndpointReply(
            FtpReplyCodes.CommandOkay, "PORT command successful");
    }

    internal FtpReply ConfigureExtendedPort(string? argument)
    {
        ClearDataSetup();
        ValidateActiveSetupAllowed("EPRT");
        FtpExtendedPortRequest request = FtpEndpointCodec.ParseExtendedPortRequest(argument);
        if (!request.Supported)
        {
            return FtpEndpointCodec.CreateNetworkProtocolReply(GetSupportedProtocols());
        }

        ConfigureTarget(request.Endpoint, true);
        return FtpEndpointCodec.CreateEndpointReply(
            FtpReplyCodes.CommandOkay, "EPRT command successful");
    }

    internal Socket OpenOutboundConnection(
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        if (!Configured || TargetAddress == null)
        {
            throw new FtpConfigurationException(
                "A validated PORT or EPRT endpoint is required.", null);
        }

        IPAddress address = TargetAddress;
        int port = TargetEndpoint.Port;
        ClearDataSetup();
        return FtpSocketOperation.ConnectConfigured(
            address, port, timeoutMilliseconds, cancellationToken, SocketOptions);
    }

    internal void EnableExtendedPassiveOnly()
    {
        ClearDataSetup();
        ExtendedPassiveOnly = true;
    }

    internal void ResetSession()
    {
        ClearDataSetup();
        ExtendedPassiveOnly = false;
    }

    internal void ClearDataSetup()
    {
        Configured = false;
        Extended = false;
        TargetAddress = null;
        TargetEndpoint = new FtpEndpoint();
    }

    internal void ConfigureTarget(FtpEndpoint endpoint, bool extended)
    {
        IPAddress address = FtpPeerPolicy.NormalizeAddress(
            ParseEndpointAddress(endpoint));
        if (FtpPeerPolicy.GetAddressFamily(address) != endpoint.AddressFamily)
        {
            throw new FtpProtocolException(
                "The active data target does not match its network protocol.", null);
        }
        if (FtpPeerPolicy.IsProhibitedActiveTarget(address))
        {
            throw new FtpProtocolException(
                "The active data target address is prohibited.", null);
        }

        if (RejectPrivilegedActivePorts && endpoint.Port < 1024)
        {
            throw new FtpProtocolException(
                "The active data target port is privileged and prohibited.", null);
        }

        bool samePeer = FtpPeerPolicy.IsSameAddress(address, ControlPeerAddress);
        if (RequireDataPeerMatch && !samePeer)
        {
            throw new FtpProtocolException(
                "The active data target does not match the control peer.", null);
        }

        if (!samePeer && !AllowForeignDataConnections)
        {
            throw new FtpProtocolException(
                "Foreign active data targets require explicit FXP enablement.", null);
        }

        TargetAddress = address;
        TargetEndpoint.Host = address.ToString();
        TargetEndpoint.Port = endpoint.Port;
        TargetEndpoint.AddressFamily = FtpPeerPolicy.GetAddressFamily(address);
        Extended = extended;
        Configured = true;
    }

    internal IPAddress ParseEndpointAddress(FtpEndpoint endpoint)
    {
        if (endpoint.AddressFamily == FtpAddressFamily.IPv4)
        {
            int[] values = FtpEndpointCodec.ParseIpv4Address(
                endpoint.Host, "active data target");
            return new IPAddress(new byte[]
            {
                (byte)values[0],
                (byte)values[1],
                (byte)values[2],
                (byte)values[3]
            });
        }

        if (endpoint.AddressFamily == FtpAddressFamily.IPv6)
        {
            FtpEndpointCodec.ValidateNumericAddress(
                endpoint.Host, endpoint.AddressFamily, "active data target");
            return IPAddress.Parse(endpoint.Host);
        }

        throw new FtpProtocolException(
            "The active data target requires IPv4 or IPv6.", null);
    }

    internal int[] GetSupportedProtocols()
    {
        if (AllowForeignDataConnections)
        {
            return new int[] { 1, 2 };
        }

        int protocol = FtpEndpointCodec.FormatNetworkProtocol(
            ControlFamily, "EPRT");
        return new int[] { protocol };
    }

    internal void ValidateActiveSetupAllowed(string commandName)
    {
        if (ExtendedPassiveOnly)
        {
            throw new FtpProtocolException(
                $"EPSV ALL prohibits {commandName} data setup.", null);
        }
    }

    internal static void ValidateConfiguration(
        IPAddress? controlPeerAddress,
        FtpDataConnectionOptions? options)
    {
        if (controlPeerAddress == null || options == null)
        {
            throw new FtpConfigurationException(
                "A control peer and server active options are required.", null);
        }

        FtpConfigurationValidator.ValidateDataConnectionOptions(options);
        FtpPeerPolicy.GetAddressFamily(controlPeerAddress);
        if (FtpPeerPolicy.IsProhibitedActiveTarget(controlPeerAddress))
        {
            throw new FtpConfigurationException(
                "The control peer cannot be unspecified or multicast.", null);
        }

        if (options.Mode == FtpDataConnectionMode.Passive)
        {
            throw new FtpConfigurationException(
                "A server active connector cannot use passive-only data mode.", null);
        }
    }
}
