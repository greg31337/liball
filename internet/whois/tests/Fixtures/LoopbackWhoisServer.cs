using whois;

namespace whois.Tests;

internal class LoopbackWhoisServer : IDisposable
{
    private System.Net.Sockets.TcpListener listener;
    private CancellationTokenSource cancellation = new CancellationTokenSource();
    private byte[][] responseChunks;
    private TimeSpan delayBetweenChunks;
    private bool resetAfterResponse;
    private bool disposed;
    private Task completion;
    private System.Net.Sockets.Socket? acceptedSocket;

    public WhoisEndpoint Endpoint;
    public byte[] ReceivedQuery = Array.Empty<byte>();
    public Exception? Error;
    public int ConnectionsAccepted;

    public LoopbackWhoisServer(byte[][] chunks, TimeSpan chunkDelay, bool resetConnection)
    {
        responseChunks = chunks;
        delayBetweenChunks = chunkDelay;
        resetAfterResponse = resetConnection;
        listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        System.Net.IPEndPoint localEndpoint = (System.Net.IPEndPoint)listener.LocalEndpoint;
        Endpoint = new WhoisEndpoint();
        Endpoint.Host = System.Net.IPAddress.Loopback.ToString();
        Endpoint.Port = localEndpoint.Port;
        completion = RunServer();
    }

    public Task Completion
    {
        get
        {
            return completion;
        }
    }

    private async Task RunServer()
    {
        try
        {
            using System.Net.Sockets.Socket socket = await listener.AcceptSocketAsync(cancellation.Token);
            acceptedSocket = socket;
            ConnectionsAccepted = ConnectionsAccepted + 1;
            using System.Net.Sockets.NetworkStream stream = new System.Net.Sockets.NetworkStream(socket, false);
            using MemoryStream query = new MemoryStream();
            byte[] buffer = new byte[256];
            bool terminated = false;
            while (!terminated)
            {
                int count = await stream.ReadAsync(buffer, cancellation.Token);
                if (count == 0)
                {
                    break;
                }
                query.Write(buffer, 0, count);
                byte[] current = query.ToArray();
                int index = 1;
                while (index < current.Length)
                {
                    if (current[index - 1] == 13 && current[index] == 10)
                    {
                        terminated = true;
                        break;
                    }
                    index = index + 1;
                }
            }
            ReceivedQuery = query.ToArray();

            int chunkIndex = 0;
            while (chunkIndex < responseChunks.Length)
            {
                if (chunkIndex > 0 && delayBetweenChunks > TimeSpan.Zero)
                {
                    await Task.Delay(delayBetweenChunks, cancellation.Token);
                }
                byte[] chunk = responseChunks[chunkIndex];
                await stream.WriteAsync(chunk, cancellation.Token);
                await stream.FlushAsync(cancellation.Token);
                chunkIndex = chunkIndex + 1;
            }

            if (resetAfterResponse)
            {
                socket.LingerState = new System.Net.Sockets.LingerOption(true, 0);
            }
            else
            {
                socket.Shutdown(System.Net.Sockets.SocketShutdown.Send);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception exception)
        {
            Error = exception;
        }
        finally
        {
            acceptedSocket = null;
            listener.Stop();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        cancellation.Cancel();
        acceptedSocket?.Dispose();
        listener.Stop();
        try
        {
            completion.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }
        cancellation.Dispose();
    }
}
