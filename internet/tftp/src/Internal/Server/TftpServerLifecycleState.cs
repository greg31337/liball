namespace tftp;

internal enum TftpServerLifecycleState
{
    Stopped = 0,
    Binding = 1,
    Listening = 2,
    Draining = 3
}
