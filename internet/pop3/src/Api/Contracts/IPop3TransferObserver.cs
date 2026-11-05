namespace pop3;

/// <summary>Receives progress reports for POP3 multiline transfers.</summary>
public interface IPop3TransferObserver
{
    /// <summary>Reports the current state of a transfer.</summary>
    /// <param name="progress">The transfer progress to report.</param>
    void Report(Pop3TransferProgress progress);
}
