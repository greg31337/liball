using System;

namespace ftp;

internal class FtpResourceLifetime
{
    internal IDisposable? Resource;

    internal FtpResourceLifetime(IDisposable? resource)
    {
        Resource = resource;
    }

    internal bool HasResource()
    {
        return Resource != null;
    }

    internal IDisposable? ReleaseResource()
    {
        IDisposable? resource = Resource;
        Resource = null;
        return resource;
    }

    internal FtpFaultIsolationResult DisposeResource()
    {
        IDisposable? resource = ReleaseResource();
        if (resource == null)
        {
            FtpFaultIsolationResult emptyResult = new FtpFaultIsolationResult();
            emptyResult.Succeeded = true;
            return emptyResult;
        }

        return FtpFaultIsolationRunner.RunOperation(() =>
        {
            resource.Dispose();
            return null;
        });
    }
}
