namespace ftp;

internal class FtpPassivePortPool
{
    internal int StartPort;
    internal int EndPort;
    internal int Capacity;
    internal bool[] Reserved;
    internal int ReservedCount;
    internal int NextIndex;
    internal object SyncRoot;

    internal FtpPassivePortPool(int startPort, int endPort)
    {
        FtpConfigurationValidator.ValidatePortRange(startPort, endPort, false, "passive pool");
        StartPort = startPort;
        EndPort = endPort;
        Capacity = endPort - startPort + 1;
        Reserved = new bool[Capacity];
        ReservedCount = 0;
        NextIndex = 0;
        SyncRoot = new object();
    }

    internal int ReserveNextPort()
    {
        lock (SyncRoot)
        {
            if (ReservedCount == Capacity)
            {
                return 0;
            }

            int scanned = 0;
            while (scanned < Capacity)
            {
                int index = (NextIndex + scanned) % Capacity;
                if (!Reserved[index])
                {
                    Reserved[index] = true;
                    ReservedCount++;
                    NextIndex = (index + 1) % Capacity;
                    return StartPort + index;
                }

                scanned++;
            }
        }

        return 0;
    }

    internal void ReleasePort(int port)
    {
        if (port < StartPort || port > EndPort)
        {
            throw new FtpConfigurationException(
                "A released passive port must belong to its pool.", null);
        }

        lock (SyncRoot)
        {
            int index = port - StartPort;
            if (Reserved[index])
            {
                Reserved[index] = false;
                ReservedCount--;
            }
        }
    }

    internal bool IsReserved(int port)
    {
        if (port < StartPort || port > EndPort)
        {
            return false;
        }

        lock (SyncRoot)
        {
            return Reserved[port - StartPort];
        }
    }
}
