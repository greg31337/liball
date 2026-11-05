namespace pop3;

internal class Pop3ServerMessageTransfer
{
    internal long WriteMessage(
        Pop3MailboxTransaction transaction,
        Pop3WireWriter writer,
        Pop3ServerOptions options,
        int messageNumber,
        bool useUtf8Representation,
        long expectedBytes,
        System.Threading.CancellationToken cancellationToken)
    {
        if (transaction == null)
        {
            throw new System.ArgumentNullException("transaction");
        }
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        cancellationToken.ThrowIfCancellationRequested();

        using (System.IO.Pipes.AnonymousPipeServerStream providerSource =
            new System.IO.Pipes.AnonymousPipeServerStream(
                System.IO.Pipes.PipeDirection.In,
                System.IO.HandleInheritability.None))
        using (System.IO.Pipes.AnonymousPipeClientStream providerDestination =
            new System.IO.Pipes.AnonymousPipeClientStream(
                System.IO.Pipes.PipeDirection.Out,
                providerSource.GetClientHandleAsString()))
        {
            System.Threading.Tasks.Task providerTask =
                System.Threading.Tasks.Task.Run(() =>
                    transaction.WriteMessage(
                        messageNumber,
                        useUtf8Representation,
                        providerDestination,
                        cancellationToken));
            System.Threading.Tasks.Task completionTask = providerTask.ContinueWith(
                task => CompleteProviderWrite(
                    task,
                    providerDestination,
                    providerSource),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            long transmittedOctets = writer.WriteServerMessageContent(
                providerSource,
                options.MaximumMessageLineOctets,
                options.TransferObserver,
                messageNumber,
                expectedBytes,
                useUtf8Representation,
                cancellationToken);
            completionTask.GetAwaiter().GetResult();
            if (transmittedOctets != expectedBytes)
            {
                throw new System.IO.InvalidDataException(
                    "The POP3 message representation does not match its LIST octet count.");
            }
            writer.WriteServerMessageTerminator(
                options.TransferObserver,
                messageNumber,
                transmittedOctets,
                expectedBytes,
                cancellationToken);
            return transmittedOctets;
        }
    }

    internal long WriteTop(
        Pop3MailboxTransaction transaction,
        Pop3WireWriter writer,
        Pop3ServerOptions options,
        int messageNumber,
        int bodyLineCount,
        bool useUtf8Representation,
        System.Threading.CancellationToken cancellationToken)
    {
        if (transaction == null)
        {
            throw new System.ArgumentNullException("transaction");
        }
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (bodyLineCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("bodyLineCount");
        }
        cancellationToken.ThrowIfCancellationRequested();

        using (System.IO.Pipes.AnonymousPipeServerStream providerSource =
            new System.IO.Pipes.AnonymousPipeServerStream(
                System.IO.Pipes.PipeDirection.In,
                System.IO.HandleInheritability.None))
        using (System.IO.Pipes.AnonymousPipeClientStream providerDestination =
            new System.IO.Pipes.AnonymousPipeClientStream(
                System.IO.Pipes.PipeDirection.Out,
                providerSource.GetClientHandleAsString()))
        {
            System.Threading.Tasks.Task providerTask =
                System.Threading.Tasks.Task.Run(() =>
                    transaction.WriteTop(
                        messageNumber,
                        bodyLineCount,
                        useUtf8Representation,
                        providerDestination,
                        cancellationToken));
            System.Threading.Tasks.Task completionTask = providerTask.ContinueWith(
                task => CompleteProviderWrite(
                    task,
                    providerDestination,
                    providerSource),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            long transmittedOctets = writer.WriteServerTopContent(
                providerSource,
                bodyLineCount,
                options.MaximumMessageLineOctets,
                options.TransferObserver,
                messageNumber,
                useUtf8Representation,
                cancellationToken);
            completionTask.GetAwaiter().GetResult();
            writer.WriteServerTopTerminator(
                options.TransferObserver,
                messageNumber,
                transmittedOctets,
                cancellationToken);
            return transmittedOctets;
        }
    }

    void CompleteProviderWrite(
        System.Threading.Tasks.Task task,
        System.IO.Stream providerDestination,
        System.IO.Pipes.AnonymousPipeServerStream providerSource)
    {
        providerDestination.Dispose();
        providerSource.DisposeLocalCopyOfClientHandle();
        task.GetAwaiter().GetResult();
    }
}
