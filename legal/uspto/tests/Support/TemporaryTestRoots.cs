using uspto;

namespace uspto.Tests;

internal sealed class TemporaryTestRoots : IDisposable
{
    internal string Root;
    internal string CacheRoot;
    internal string MirrorRoot;
    private bool disposed;

    internal TemporaryTestRoots()
    {
        Root = Path.Combine(Path.GetTempPath(), "uspto-tests", Guid.NewGuid().ToString("N"));
        CacheRoot = Path.Combine(Root, "cache");
        MirrorRoot = Path.Combine(Root, "mirror");
        Directory.CreateDirectory(CacheRoot);
        Directory.CreateDirectory(MirrorRoot);
    }

    internal void ApplyTo(UsptoClientOptions options)
    {
        options.Cache.Directory = CacheRoot;
        options.Mirror.Directory = MirrorRoot;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
        }
    }
}
