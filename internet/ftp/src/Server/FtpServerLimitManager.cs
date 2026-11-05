using System;
using System.Collections.Generic;

namespace ftp;

internal class FtpServerLimitManager
{
    internal object StateLock;
    internal int MaximumSessions;
    internal int MaximumSessionsPerAddress;
    internal int MaximumSessionsPerUser;
    internal int MaximumPendingLogins;
    internal int MaximumCommandsPerMinute;
    internal int MaximumMalformedLines;
    internal int MaximumSimultaneousTransfers;
    internal int IdleTimeoutMilliseconds;
    internal Dictionary<string, int> AddressSessions;
    internal Dictionary<string, int> UserSessions;
    internal Dictionary<string, int> HostSessions;
    internal Dictionary<string, Dictionary<string, int>> HostUserSessions;
    internal int Sessions;
    internal int PendingLogins;
    internal int Transfers;

    internal FtpServerLimitManager(FtpServerOptions settings)
    {
        StateLock = new object();
        MaximumSessions = settings.MaximumSessions;
        MaximumSessionsPerAddress = settings.MaximumSessionsPerAddress;
        MaximumSessionsPerUser = settings.MaximumSessionsPerUser;
        MaximumPendingLogins = settings.MaximumPendingConnections;
        MaximumCommandsPerMinute = settings.MaximumCommandsPerMinute;
        MaximumMalformedLines = settings.MaximumMalformedLines;
        MaximumSimultaneousTransfers =
            settings.MaximumSimultaneousTransfers;
        IdleTimeoutMilliseconds = settings.Timeouts.IdleTimeoutMilliseconds;
        AddressSessions = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);
        UserSessions = new Dictionary<string, int>(StringComparer.Ordinal);
        HostSessions = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);
        HostUserSessions =
            new Dictionary<string, Dictionary<string, int>>(
                StringComparer.OrdinalIgnoreCase);
        Sessions = 0;
        PendingLogins = 0;
        Transfers = 0;
    }

    internal bool TryRegisterSession(FtpServerSessionContext context)
    {
        lock (StateLock)
        {
            if (context.Admitted || string.IsNullOrEmpty(context.RemoteAddress))
            {
                return false;
            }

            int addressCount = GetAddressSessionCount(context.RemoteAddress);
            if (Sessions >= MaximumSessions ||
                addressCount >= MaximumSessionsPerAddress ||
                PendingLogins >= MaximumPendingLogins)
            {
                return false;
            }

            Sessions++;
            PendingLogins++;
            AddressSessions[context.RemoteAddress] = addressCount + 1;
            context.Admitted = true;
            context.PendingLogin = true;
            context.LastActivityMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    internal bool TryAuthenticateUser(
        FtpServerSessionContext context,
        string userName)
    {
        lock (StateLock)
        {
            if (!context.Admitted || !context.PendingLogin ||
                string.IsNullOrWhiteSpace(userName) ||
                FtpConfigurationValidator.HasInvalidText(userName))
            {
                return false;
            }

            int userCount = GetUserSessionCount(userName);
            int hostUserCount = GetHostUserSessionCount(
                context.SelectedHost, userName);
            if (userCount >= MaximumSessionsPerUser ||
                (context.SelectedHost != null &&
                 hostUserCount >= context.MaximumHostSessionsPerUser))
            {
                return false;
            }

            PendingLogins--;
            context.PendingLogin = false;
            context.AuthenticatedUser = userName;
            UserSessions[userName] = userCount + 1;
            IncrementHostUserSession(context.SelectedHost, userName);
            context.LastActivityMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    internal bool TryResetAuthentication(FtpServerSessionContext context)
    {
        lock (StateLock)
        {
            if (!context.Admitted)
            {
                return false;
            }
            if (context.PendingLogin)
            {
                return true;
            }
            if (PendingLogins >= MaximumPendingLogins)
            {
                return false;
            }

            string? userName = context.AuthenticatedUser;
            if (userName != null)
            {
                DecrementUserSession(userName);
                DecrementHostUserSession(context.SelectedHost, userName);
            }
            context.AuthenticatedUser = null;
            context.PendingLogin = true;
            PendingLogins++;
            context.LastActivityMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    internal bool TrySelectHost(
        FtpServerSessionContext context,
        string hostName,
        int maximumSessions,
        int maximumSessionsPerUser)
    {
        lock (StateLock)
        {
            if (!context.Admitted || !context.PendingLogin ||
                string.IsNullOrWhiteSpace(hostName) || maximumSessions <= 0 ||
                maximumSessionsPerUser <= 0 ||
                maximumSessionsPerUser > maximumSessions)
            {
                return false;
            }

            if (string.Equals(
                context.SelectedHost, hostName, StringComparison.OrdinalIgnoreCase))
            {
                context.MaximumHostSessionsPerUser = maximumSessionsPerUser;
                return true;
            }

            int hostCount = GetHostSessionCount(hostName);
            if (hostCount >= maximumSessions)
            {
                return false;
            }

            DecrementHostSession(context.SelectedHost);
            HostSessions[hostName] = hostCount + 1;
            context.SelectedHost = hostName;
            context.MaximumHostSessionsPerUser = maximumSessionsPerUser;
            context.LastActivityMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    internal void ResetHostSelection(FtpServerSessionContext context)
    {
        lock (StateLock)
        {
            if (!context.Admitted)
            {
                return;
            }
            DecrementHostSession(context.SelectedHost);
            context.SelectedHost = null;
            context.MaximumHostSessionsPerUser = 0;
            context.LastActivityMilliseconds = Environment.TickCount64;
        }
    }

    internal void ReleaseSession(FtpServerSessionContext context)
    {
        lock (StateLock)
        {
            if (!context.Admitted)
            {
                return;
            }

            Sessions--;
            DecrementAddressSession(context.RemoteAddress);
            if (context.PendingLogin)
            {
                PendingLogins--;
            }
            if (context.AuthenticatedUser != null)
            {
                DecrementUserSession(context.AuthenticatedUser);
                DecrementHostUserSession(
                    context.SelectedHost, context.AuthenticatedUser);
            }
            DecrementHostSession(context.SelectedHost);
            if (context.TransferActive)
            {
                Transfers--;
            }

            context.Admitted = false;
            context.PendingLogin = false;
            context.AuthenticatedUser = null;
            context.SelectedHost = null;
            context.MaximumHostSessionsPerUser = 0;
            context.TransferActive = false;
        }
    }

    internal bool TryRecordCommand(
        FtpServerSessionContext context,
        long currentMilliseconds)
    {
        lock (StateLock)
        {
            if (!context.Admitted)
            {
                return false;
            }

            if (context.CommandTimes.Count != 0 &&
                currentMilliseconds < context.CommandTimes.Peek())
            {
                context.CommandTimes.Clear();
            }
            while (context.CommandTimes.Count != 0 &&
                currentMilliseconds - context.CommandTimes.Peek() >= 60000)
            {
                context.CommandTimes.Dequeue();
            }
            if (context.CommandTimes.Count >= MaximumCommandsPerMinute)
            {
                return false;
            }

            context.CommandTimes.Enqueue(currentMilliseconds);
            context.LastActivityMilliseconds = currentMilliseconds;
            return true;
        }
    }

    internal bool TryRecordMalformedLine(FtpServerSessionContext context)
    {
        lock (StateLock)
        {
            if (!context.Admitted)
            {
                return false;
            }
            if (context.MalformedLines >= MaximumMalformedLines)
            {
                return false;
            }

            context.MalformedLines++;
            context.LastActivityMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    internal void RecordActivity(
        FtpServerSessionContext context,
        long currentMilliseconds)
    {
        lock (StateLock)
        {
            if (context.Admitted)
            {
                context.LastActivityMilliseconds = currentMilliseconds;
            }
        }
    }

    internal bool HasIdleExpired(
        FtpServerSessionContext context,
        long currentMilliseconds)
    {
        lock (StateLock)
        {
            if (!context.Admitted ||
                currentMilliseconds <= context.LastActivityMilliseconds)
            {
                return false;
            }
            return currentMilliseconds - context.LastActivityMilliseconds >=
                IdleTimeoutMilliseconds;
        }
    }

    internal bool TryBeginTransfer(FtpServerSessionContext context)
    {
        lock (StateLock)
        {
            if (!context.Admitted || context.TransferActive ||
                Transfers >= MaximumSimultaneousTransfers)
            {
                return false;
            }

            Transfers++;
            context.TransferActive = true;
            context.LastActivityMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    internal void EndTransfer(FtpServerSessionContext context)
    {
        lock (StateLock)
        {
            if (context.Admitted && context.TransferActive)
            {
                Transfers--;
                context.TransferActive = false;
                context.LastActivityMilliseconds = Environment.TickCount64;
            }
        }
    }

    internal int GetAddressSessionCount(string address)
    {
        lock (StateLock)
        {
            if (!AddressSessions.ContainsKey(address))
            {
                return 0;
            }
            return AddressSessions[address];
        }
    }

    internal int GetUserSessionCount(string userName)
    {
        lock (StateLock)
        {
            if (!UserSessions.ContainsKey(userName))
            {
                return 0;
            }
            return UserSessions[userName];
        }
    }

    internal int GetHostSessionCount(string hostName)
    {
        lock (StateLock)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                return 0;
            }
            int count;
            return HostSessions.TryGetValue(hostName, out count) ? count : 0;
        }
    }

    internal int GetHostUserSessionCount(
        string? hostName,
        string userName)
    {
        lock (StateLock)
        {
            if (hostName == null)
            {
                return 0;
            }
            Dictionary<string, int>? users;
            if (!HostUserSessions.TryGetValue(hostName, out users) || users == null)
            {
                return 0;
            }
            int count;
            return users.TryGetValue(userName, out count) ? count : 0;
        }
    }

    internal void IncrementHostUserSession(
        string? hostName,
        string userName)
    {
        if (hostName == null)
        {
            return;
        }
        Dictionary<string, int>? users;
        if (!HostUserSessions.TryGetValue(hostName, out users) || users == null)
        {
            users = new Dictionary<string, int>(StringComparer.Ordinal);
            HostUserSessions[hostName] = users;
        }
        int count;
        users.TryGetValue(userName, out count);
        users[userName] = count + 1;
    }

    internal void DecrementHostSession(string? hostName)
    {
        if (hostName == null)
        {
            return;
        }
        int count = GetHostSessionCount(hostName);
        if (count <= 1)
        {
            HostSessions.Remove(hostName);
        }
        else
        {
            HostSessions[hostName] = count - 1;
        }
    }

    internal void DecrementHostUserSession(
        string? hostName,
        string userName)
    {
        if (hostName == null)
        {
            return;
        }
        Dictionary<string, int>? users;
        if (!HostUserSessions.TryGetValue(hostName, out users) || users == null)
        {
            return;
        }
        int count;
        if (!users.TryGetValue(userName, out count))
        {
            return;
        }
        if (count <= 1)
        {
            users.Remove(userName);
        }
        else
        {
            users[userName] = count - 1;
        }
        if (users.Count == 0)
        {
            HostUserSessions.Remove(hostName);
        }
    }

    internal void DecrementAddressSession(string address)
    {
        int count = GetAddressSessionCount(address);
        if (count <= 1)
        {
            AddressSessions.Remove(address);
            return;
        }
        AddressSessions[address] = count - 1;
    }

    internal void DecrementUserSession(string userName)
    {
        int count = GetUserSessionCount(userName);
        if (count <= 1)
        {
            UserSessions.Remove(userName);
            return;
        }
        UserSessions[userName] = count - 1;
    }

    internal int GetSessionCount()
    {
        lock (StateLock)
        {
            return Sessions;
        }
    }

    internal int GetPendingLoginCount()
    {
        lock (StateLock)
        {
            return PendingLogins;
        }
    }

    internal int GetTransferCount()
    {
        lock (StateLock)
        {
            return Transfers;
        }
    }
}
