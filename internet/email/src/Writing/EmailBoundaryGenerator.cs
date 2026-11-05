using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
internal class EmailBoundaryCollisionScan
{
    bool collision;
    long scannedByteCount;

    internal EmailBoundaryCollisionScan(bool collision, long scannedByteCount)
    {
        this.collision = collision;
        this.scannedByteCount = scannedByteCount;
    }

    internal bool HasCollision()
    {
        return collision;
    }

    internal long GetScannedByteCount()
    {
        return scannedByteCount;
    }
}
