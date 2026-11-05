using System.Threading;

namespace tftp;

internal interface ITftpServerSessionHandler
{
    TftpSessionOutcome ExecuteSession(
        TftpServerSession session,
        CancellationToken cancellationToken);
}
