namespace jpeg;

internal class JpegIo
{
    internal byte[] ReadFile(string path, System.Threading.CancellationToken cancellationToken)
    {
        Jpeg.ValidateFilePath(path);
        cancellationToken.ThrowIfCancellationRequested();
        System.Threading.Tasks.Task<byte[]> task = System.Threading.Tasks.Task.Run(
            () => ReadFileCore(path, cancellationToken),
            cancellationToken);
        return CompleteReadTask(task, "JPEG input file could not be read.", cancellationToken);
    }

    internal void WriteFile(string path, byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        Jpeg.ValidateFilePath(path);
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        cancellationToken.ThrowIfCancellationRequested();
        System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Run(
            () => WriteFileCore(path, data, cancellationToken),
            cancellationToken);
        CompleteWriteTask(task, "JPEG output file could not be written.", cancellationToken);
    }

    internal byte[] ReadStream(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        ValidateReadableStream(stream);
        cancellationToken.ThrowIfCancellationRequested();
        System.Threading.Tasks.Task<byte[]> task = System.Threading.Tasks.Task.Run(
            () => ReadStreamCore(stream, cancellationToken),
            cancellationToken);
        return CompleteReadTask(task, "JPEG input stream could not be read.", cancellationToken);
    }

    internal void WriteStream(System.IO.Stream stream, byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        ValidateWritableStream(stream);
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        cancellationToken.ThrowIfCancellationRequested();
        System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Run(
            () => WriteStreamCore(stream, data, cancellationToken),
            cancellationToken);
        CompleteWriteTask(task, "JPEG output stream could not be written.", cancellationToken);
    }

    internal byte[] ReadStreamCore(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        byte[] buffer = new byte[8192];
        int count = stream.Read(buffer, 0, buffer.Length);
        while (count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.Write(buffer, 0, count);
            count = stream.Read(buffer, 0, buffer.Length);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return output.ToArray();
    }

    internal byte[] ReadFileCore(string path, System.Threading.CancellationToken cancellationToken)
    {
        using (System.IO.Stream stream = System.IO.File.OpenRead(path))
        {
            return ReadStreamCore(stream, cancellationToken);
        }
    }

    internal void WriteFileCore(string path, byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        using (System.IO.Stream stream = System.IO.File.Create(path))
        {
            WriteStreamCore(stream, data, cancellationToken);
        }
    }

    internal void WriteStreamCore(System.IO.Stream stream, byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int count = data.Length - offset;
            if (count > 8192)
            {
                count = 8192;
            }

            stream.Write(data, offset, count);
            offset = offset + count;
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    internal byte[] CompleteReadTask(
        System.Threading.Tasks.Task<byte[]> task,
        string message,
        System.Threading.CancellationToken cancellationToken)
    {
        WaitForCompletion(task);
        if (task.IsCanceled)
        {
            throw new System.OperationCanceledException(cancellationToken);
        }
        if (task.IsFaulted)
        {
            throw new JpegIOException(message, GetTaskFailure(task.Exception));
        }

        return task.Result;
    }

    internal void CompleteWriteTask(
        System.Threading.Tasks.Task task,
        string message,
        System.Threading.CancellationToken cancellationToken)
    {
        WaitForCompletion(task);
        if (task.IsCanceled)
        {
            throw new System.OperationCanceledException(cancellationToken);
        }
        if (task.IsFaulted)
        {
            throw new JpegIOException(message, GetTaskFailure(task.Exception));
        }
    }

    internal void WaitForCompletion(System.Threading.Tasks.Task task)
    {
        while (!task.IsCompleted)
        {
            System.Threading.Thread.Yield();
        }
    }

    internal System.Exception? GetTaskFailure(System.AggregateException? aggregate)
    {
        if (aggregate == null)
        {
            return null;
        }
        if (aggregate.InnerException != null)
        {
            return aggregate.InnerException;
        }

        return aggregate;
    }

    internal void ValidateReadableStream(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanRead)
        {
            throw new System.ArgumentException("Stream must be readable.", "stream");
        }
    }

    internal void ValidateWritableStream(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("Stream must be writable.", "stream");
        }
    }
}
