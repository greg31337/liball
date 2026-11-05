using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ftp;

internal class FtpPassiveConnector
{
    internal IPAddress ControlPeerAddress;
    internal FtpAddressFamily ControlPeerFamily;
    internal bool UseExtendedPassive;
    internal bool SendExtendedPassiveAll;
    internal bool AllowPassiveFallback;
    internal bool IgnorePassiveAdvertisedAddress;
    internal bool RejectPassiveAddressMismatch;
    internal bool RequireDataPeerMatch;
    internal FtpSocketOptions SocketOptions;
    internal bool ExtendedPassiveAllAttempted;
    internal bool ExtendedPassiveAllReplyApplied;
    internal bool ExtendedPassiveOnly;

    internal FtpPassiveConnector(
        IPAddress? controlPeerAddress,
        FtpDataConnectionOptions? options)
    {
        ValidateConfiguration(controlPeerAddress, options);
        if (controlPeerAddress == null || options == null)
        {
            throw new FtpConfigurationException(
                "A control peer and data-connection options are required.", null);
        }

        ControlPeerAddress = FtpPeerPolicy.NormalizeAddress(controlPeerAddress);
        ControlPeerFamily = FtpPeerPolicy.GetAddressFamily(controlPeerAddress);
        UseExtendedPassive = options.UseExtendedPassive;
        SendExtendedPassiveAll = options.SendExtendedPassiveAll;
        AllowPassiveFallback = options.AllowPassiveFallback;
        IgnorePassiveAdvertisedAddress = options.IgnorePassiveAdvertisedAddress;
        RejectPassiveAddressMismatch = options.RejectPassiveAddressMismatch;
        RequireDataPeerMatch = options.RequireDataPeerMatch;
        SocketOptions = FtpSocketSettings.CreateSnapshot(options.Socket);
        ExtendedPassiveAllAttempted = false;
        ExtendedPassiveAllReplyApplied = false;
        ExtendedPassiveOnly = false;
    }

    internal FtpCommand? CreateExtendedPassiveAllCommand()
    {
        if (!SendExtendedPassiveAll || ExtendedPassiveAllAttempted)
        {
            return null;
        }

        ExtendedPassiveAllAttempted = true;
        FtpCommand command = CreateCommand("ALL");
        return command;
    }

    internal void ApplyExtendedPassiveAllReply(FtpReply? reply)
    {
        if (!ExtendedPassiveAllAttempted || ExtendedPassiveAllReplyApplied)
        {
            throw new FtpProtocolException(
                "An EPSV ALL reply requires one pending EPSV ALL command.", null);
        }

        if (reply == null)
        {
            throw new FtpProtocolException("An EPSV ALL reply is required.", null);
        }

        ExtendedPassiveAllReplyApplied = true;
        ExtendedPassiveOnly = reply.Code == FtpReplyCodes.CommandOkay;
    }

    internal FtpCommand CreateExtendedPassiveCommand(FtpAddressFamily requestedFamily)
    {
        if (!UseExtendedPassive)
        {
            throw new FtpConfigurationException(
                "Extended passive mode is disabled for this connector.", null);
        }

        RequireExtendedPassiveAllCompletion();
        ResolveAddressFamily(requestedFamily);
        string? argument = null;
        if (requestedFamily != FtpAddressFamily.Unspecified)
        {
            FtpExtendedPassiveRequest request = new FtpExtendedPassiveRequest();
            request.HasProtocol = true;
            request.AddressFamily = requestedFamily;
            request.Protocol = FtpEndpointCodec.FormatNetworkProtocol(requestedFamily, "EPSV");
            argument = FtpEndpointCodec.FormatExtendedPassiveArgument(request);
        }

        return CreateCommand(argument);
    }

    internal FtpPassiveConnection OpenExtendedPassiveConnection(
        FtpReply? reply,
        FtpAddressFamily requestedFamily,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        if (!UseExtendedPassive)
        {
            throw new FtpConfigurationException(
                "Extended passive mode is disabled for this connector.", null);
        }

        RequireExtendedPassiveAllCompletion();
        FtpAddressFamily selectedFamily = ResolveAddressFamily(requestedFamily);
        FtpPassiveReply passiveReply = FtpEndpointCodec.ParseExtendedPassiveReply(reply);
        Socket socket = FtpSocketOperation.ConnectConfigured(
            ControlPeerAddress,
            passiveReply.Endpoint.Port,
            timeoutMilliseconds,
            cancellationToken,
            SocketOptions);

        FtpPassiveConnection result = new FtpPassiveConnection();
        result.Connection = socket;
        result.Endpoint.Host = ControlPeerAddress.ToString();
        result.Endpoint.Port = passiveReply.Endpoint.Port;
        result.Endpoint.AddressFamily = selectedFamily;
        result.Extended = true;
        result.UsedControlPeerAddress = true;
        return result;
    }

    internal FtpCommand CreatePassiveCommand()
    {
        RequirePassiveAvailability();
        if (UseExtendedPassive)
        {
            throw new FtpConfigurationException(
                "Direct PASV setup requires extended passive mode to be disabled.", null);
        }

        return CreatePassiveCommandValue();
    }

    internal FtpCommand CreatePassiveFallbackCommand(FtpReply? extendedPassiveReply)
    {
        RequirePassiveAvailability();
        if (!UseExtendedPassive || !AllowPassiveFallback)
        {
            throw new FtpProtocolException("PASV fallback is not enabled for this connector.", null);
        }

        if (extendedPassiveReply == null)
        {
            throw new FtpProtocolException("An EPSV failure reply is required before PASV fallback.", null);
        }

        int code = extendedPassiveReply.Code;
        bool unsupported = code == FtpReplyCodes.SyntaxError ||
            code == FtpReplyCodes.SyntaxErrorInParameters ||
            code == FtpReplyCodes.CommandNotImplemented ||
            code == FtpReplyCodes.CommandParameterNotImplemented ||
            code == FtpReplyCodes.NetworkProtocolNotSupported;
        if (!unsupported)
        {
            throw new FtpProtocolException(
                "PASV fallback requires a standards-defined EPSV unsupported reply.", null);
        }

        return CreatePassiveCommandValue();
    }

    internal FtpPassiveConnection OpenPassiveConnection(
        FtpReply? reply,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        RequirePassiveAvailability();
        FtpPassiveReply passiveReply = FtpEndpointCodec.ParsePassiveReply(reply);
        IPAddress selectedAddress = SelectPassiveAddress(passiveReply.Endpoint);
        Socket socket = FtpSocketOperation.ConnectConfigured(
            selectedAddress,
            passiveReply.Endpoint.Port,
            timeoutMilliseconds,
            cancellationToken,
            SocketOptions);

        FtpPassiveConnection result = new FtpPassiveConnection();
        result.Connection = socket;
        result.Endpoint.Host = selectedAddress.ToString();
        result.Endpoint.Port = passiveReply.Endpoint.Port;
        result.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        result.Extended = false;
        result.UsedControlPeerAddress = FtpPeerPolicy.IsSameAddress(
            selectedAddress, ControlPeerAddress);
        return result;
    }

    internal IPAddress SelectPassiveAddress(FtpEndpoint advertisedEndpoint)
    {
        int[] values = FtpEndpointCodec.ParseIpv4Address(advertisedEndpoint.Host, "PASV");
        IPAddress advertisedAddress = new IPAddress(new byte[]
        {
            (byte)values[0],
            (byte)values[1],
            (byte)values[2],
            (byte)values[3]
        });
        if (IgnorePassiveAdvertisedAddress)
        {
            return ControlPeerAddress;
        }

        if (FtpPeerPolicy.IsProhibitedPassiveTarget(advertisedAddress))
        {
            throw new FtpConnectionException(
                "The PASV reply advertised a prohibited data target.", null);
        }

        bool sameAddress = FtpPeerPolicy.IsSameAddress(advertisedAddress, ControlPeerAddress);
        if (RequireDataPeerMatch && !sameAddress)
        {
            throw new FtpConnectionException(
                "The PASV reply address does not match the control peer.", null);
        }

        bool advertisedPrivate = FtpPeerPolicy.IsPrivateOrLocalAddress(advertisedAddress);
        bool controlPrivate = FtpPeerPolicy.IsPrivateOrLocalAddress(ControlPeerAddress);
        if (RejectPassiveAddressMismatch && advertisedPrivate != controlPrivate)
        {
            throw new FtpConnectionException(
                "The PASV reply crosses the configured private/public address boundary.", null);
        }

        return advertisedAddress;
    }

    internal void RequirePassiveAvailability()
    {
        if (ControlPeerFamily != FtpAddressFamily.IPv4)
        {
            throw new FtpProtocolException("PASV is available only for an IPv4 control connection.", null);
        }

        if (ExtendedPassiveOnly)
        {
            throw new FtpProtocolException("EPSV ALL prohibits subsequent PASV commands.", null);
        }
    }

    internal FtpCommand CreatePassiveCommandValue()
    {
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Pasv;
        command.Verb = FtpCommandNames.Pasv;
        command.Argument = null;
        return command;
    }

    internal FtpAddressFamily ResolveAddressFamily(FtpAddressFamily requestedFamily)
    {
        if (requestedFamily != FtpAddressFamily.Unspecified &&
            requestedFamily != FtpAddressFamily.IPv4 &&
            requestedFamily != FtpAddressFamily.IPv6)
        {
            throw new FtpConfigurationException("The EPSV network protocol selection is invalid.", null);
        }

        if (requestedFamily == FtpAddressFamily.Unspecified)
        {
            return ControlPeerFamily;
        }

        if (requestedFamily != ControlPeerFamily)
        {
            throw new FtpConfigurationException(
                "An explicit EPSV protocol must match the numeric control peer address family.", null);
        }

        return requestedFamily;
    }

    internal void RequireExtendedPassiveAllCompletion()
    {
        if (SendExtendedPassiveAll && !ExtendedPassiveAllReplyApplied)
        {
            throw new FtpProtocolException(
                "EPSV ALL must receive a terminal reply before passive setup continues.", null);
        }
    }

    internal FtpCommand CreateCommand(string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Epsv;
        command.Verb = FtpCommandNames.Epsv;
        command.Argument = argument;
        return command;
    }

    internal static FtpAddressFamily MapAddressFamily(AddressFamily addressFamily)
    {
        if (addressFamily == AddressFamily.InterNetwork)
        {
            return FtpAddressFamily.IPv4;
        }

        if (addressFamily == AddressFamily.InterNetworkV6)
        {
            return FtpAddressFamily.IPv6;
        }

        throw new FtpConfigurationException("The control peer must use IPv4 or IPv6.", null);
    }

    internal static void ValidateConfiguration(
        IPAddress? controlPeerAddress,
        FtpDataConnectionOptions? options)
    {
        if (controlPeerAddress == null || options == null)
        {
            throw new FtpConfigurationException(
                "A control peer and data-connection options are required.", null);
        }

        FtpConfigurationValidator.ValidateDataConnectionOptions(options);
        FtpAddressFamily controlFamily = FtpPeerPolicy.GetAddressFamily(controlPeerAddress);
        if (FtpPeerPolicy.IsProhibitedAdvertisedAddress(controlPeerAddress))
        {
            throw new FtpConfigurationException(
                "The control peer cannot be unspecified or multicast.", null);
        }
        if (options.Mode == FtpDataConnectionMode.Active)
        {
            throw new FtpConfigurationException(
                "A passive connector cannot be used with active-only data mode.", null);
        }

        if (controlFamily == FtpAddressFamily.IPv6 && !options.UseExtendedPassive)
        {
            throw new FtpConfigurationException(
                "An IPv6 passive connection requires EPSV.", null);
        }
    }
}
