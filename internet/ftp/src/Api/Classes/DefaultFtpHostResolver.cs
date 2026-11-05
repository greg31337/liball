using System;
using System.Net;

namespace ftp;

/// <summary>
/// Resolves configured FTP virtual hosts using case-insensitive DNS names and canonical address literals.
/// </summary>
public class DefaultFtpHostResolver : IFtpHostResolver
{
    internal FtpVirtualHostOptions[] Hosts;

    /// <summary>
    /// Creates a resolver from a validated snapshot of the supplied virtual hosts.
    /// </summary>
    /// <param name="virtualHosts">The virtual hosts available for HOST and TLS SNI routing.</param>
    /// <exception cref="System.ArgumentNullException">The virtual-host collection is null.</exception>
    /// <exception cref="FtpConfigurationException">A virtual-host entry is invalid or duplicated.</exception>
    public DefaultFtpHostResolver(FtpVirtualHostOptions[] virtualHosts)
    {
        if (virtualHosts == null)
        {
            throw new ArgumentNullException("virtualHosts");
        }

        int hostIndex = 0;
        while (hostIndex < virtualHosts.Length)
        {
            FtpVirtualHostOptions host = virtualHosts[hostIndex];
            FtpConfigurationValidator.ValidateVirtualHostOptions(host);
            FtpFaultIsolationResult hostNameValidation =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return NormalizeHostName(host.HostName);
                });
            if (!hostNameValidation.Succeeded)
            {
                throw new FtpConfigurationException(
                    "A configured FTP virtual-host name is invalid.",
                    hostNameValidation.Failure);
            }

            string? normalizedHostName = hostNameValidation.Value as string;
            if (normalizedHostName == null)
            {
                throw new FtpConfigurationException(
                    "A configured FTP virtual-host name could not be normalized.", null);
            }
            string hostName = normalizedHostName;
            int comparisonIndex = hostIndex + 1;
            while (comparisonIndex < virtualHosts.Length)
            {
                FtpVirtualHostOptions comparison = virtualHosts[comparisonIndex];
                FtpConfigurationValidator.ValidateVirtualHostOptions(comparison);
                FtpFaultIsolationResult comparisonValidation =
                    FtpFaultIsolationRunner.RunOperation(() =>
                    {
                        return NormalizeHostName(comparison.HostName);
                    });
                if (!comparisonValidation.Succeeded)
                {
                    throw new FtpConfigurationException(
                        "A configured FTP virtual-host name is invalid.",
                        comparisonValidation.Failure);
                }
                if (string.Equals(
                    hostName,
                    comparisonValidation.Value as string,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new FtpConfigurationException(
                        "FTP virtual-host names must be unique.", null);
                }
                comparisonIndex++;
            }
            hostIndex++;
        }

        Hosts = FtpServerOptionsSnapshot.CopyVirtualHosts(virtualHosts);
    }

    /// <summary>
    /// Resolves one HOST or TLS SNI request without throwing for invalid or unknown input.
    /// </summary>
    /// <param name="request">The requested host, endpoints, and TLS context.</param>
    /// <returns>A handled result containing a detached host snapshot when a match exists.</returns>
    public FtpVirtualHostResult ResolveVirtualHost(FtpVirtualHostRequest request)
    {
        FtpVirtualHostResult result = new FtpVirtualHostResult();
        result.Handled = true;
        if (request == null)
        {
            result.FailureMessage = "A virtual-host request is required.";
            return result;
        }

        string requestedName = request.HostName;
        if (string.IsNullOrWhiteSpace(requestedName) && request.ServerName != null)
        {
            requestedName = request.ServerName;
        }
        FtpFaultIsolationResult normalization =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return NormalizeHostName(requestedName);
            });
        if (!normalization.Succeeded)
        {
            result.FailureMessage = "The requested FTP virtual-host name is invalid.";
            return result;
        }

        string? normalizedRequestName = normalization.Value as string;
        if (normalizedRequestName == null)
        {
            result.FailureMessage =
                "The requested FTP virtual-host name could not be normalized.";
            return result;
        }
        string normalizedName = normalizedRequestName;
        int index = 0;
        while (index < Hosts.Length)
        {
            string configuredName = NormalizeHostName(Hosts[index].HostName);
            if (string.Equals(
                normalizedName, configuredName, StringComparison.OrdinalIgnoreCase))
            {
                result.Found = true;
                result.Host = FtpServerOptionsSnapshot.CopyVirtualHosts(
                    new FtpVirtualHostOptions[] { Hosts[index] })[0];
                return result;
            }
            index++;
        }

        result.FailureMessage = "The requested FTP virtual host was not found.";
        return result;
    }

    internal static string NormalizeHostName(string hostName)
    {
        string parsed = FtpHostCodec.ParseHostArgument(hostName);
        if (parsed[0] != '[')
        {
            return parsed.ToLowerInvariant();
        }

        string addressText = parsed.Substring(1, parsed.Length - 2);
        IPAddress address = IPAddress.Parse(addressText);
        return "[" + address.ToString() + "]";
    }
}
