using System.Text;
using System.Text.RegularExpressions;

namespace pop3.Tests;

public class Pop3ServerStateMachineTests
{
    [Test]
    public void OpenCreatesUniqueValidApopGreetingAndDefensiveEndpointContext()
    {
        TrackingDuplexStream firstStream = new TrackingDuplexStream();
        TrackingDuplexStream secondStream = new TrackingDuplexStream();
        Pop3ServerConnection firstConnection = CreateConnection(firstStream, true);
        firstConnection.IsSecure = true;
        firstConnection.LocalEndPoint = "127.0.0.1:110";
        firstConnection.RemoteEndPoint = "192.0.2.10:42000";
        Pop3ServerConnection secondConnection = CreateConnection(secondStream, true);
        Pop3ServerOptions options = CreateOptions();
        options.EnableApop = true;
        options.GreetingText = "mail ready";
        Pop3ServerStateMachine first = new Pop3ServerStateMachine();
        Pop3ServerStateMachine second = new Pop3ServerStateMachine();

        Pop3Response firstGreeting = first.OpenConnection(
            firstConnection,
            options,
            1000);
        Pop3Response secondGreeting = second.OpenConnection(
            secondConnection,
            options,
            1000);

        Assert.That(firstGreeting.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(firstGreeting.Text, Is.EqualTo("mail ready"));
        Assert.That(firstGreeting.GreetingTimestamp, Does.Match(
            new Regex("^<[0-9]+\\.[0-9a-f]{32}@localhost>$")));
        Assert.That(secondGreeting.GreetingTimestamp,
            Is.Not.EqualTo(firstGreeting.GreetingTimestamp));
        Assert.That(first.GetCurrentState(), Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(first.GetIsSecure(), Is.True);
        AssertGreetingRoundTrip(firstGreeting);

        firstConnection.Stream = null;
        firstConnection.IsSecure = false;
        firstConnection.LocalEndPoint = "changed";
        firstConnection.RemoteEndPoint = "changed";
        firstConnection.LeaveOpen = false;
        Pop3ServerConnection context = first.GetConnectionContext();
        Assert.That(context.Stream, Is.SameAs(firstStream));
        Assert.That(context.IsSecure, Is.True);
        Assert.That(context.LocalEndPoint, Is.EqualTo("127.0.0.1:110"));
        Assert.That(context.RemoteEndPoint, Is.EqualTo("192.0.2.10:42000"));
        Assert.That(context.LeaveOpen, Is.True);
        context.LocalEndPoint = "also changed";
        Assert.That(first.GetConnectionContext().LocalEndPoint,
            Is.EqualTo("127.0.0.1:110"));

        firstGreeting.Text = "changed";
        firstGreeting.GreetingTimestamp = null;
        Assert.That(first.GetGreeting().Text, Is.EqualTo("mail ready"));
        Assert.That(first.GetGreeting().GreetingTimestamp, Is.Not.Null);

        first.CloseConnection();
        second.CloseConnection();
        firstStream.Dispose();
        secondStream.Dispose();
    }

    [Test]
    public void DisabledApopProducesPlainPositiveGreeting()
    {
        TrackingDuplexStream stream = new TrackingDuplexStream();
        Pop3ServerOptions options = CreateOptions();
        options.GreetingText = "plain greeting";
        Pop3ServerStateMachine state = new Pop3ServerStateMachine();

        Pop3Response greeting = state.OpenConnection(
            CreateConnection(stream, true),
            options,
            0);

        Assert.That(greeting.GreetingTimestamp, Is.Null);
        Assert.That(FormatResponse(greeting), Is.EqualTo("+OK plain greeting\r\n"));

        state.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void ActivityTrackingUsesTheConfiguredBoundaryAndMonotonicResets()
    {
        TrackingDuplexStream stream = new TrackingDuplexStream();
        Pop3ServerOptions options = CreateOptions();
        Pop3ServerStateMachine state = new Pop3ServerStateMachine();
        state.OpenConnection(CreateConnection(stream, true), options, 10000);

        Assert.That(state.GetInactivityCancellationToken().CanBeCanceled,
            Is.True);
        Assert.That(state.GetRemainingInactivityMilliseconds(10000),
            Is.EqualTo(600000));
        Assert.That(state.HasInactivityExpired(609999), Is.False);
        Assert.That(state.GetRemainingInactivityMilliseconds(609999), Is.EqualTo(1));
        Assert.That(state.HasInactivityExpired(610000), Is.True);
        Assert.That(state.GetRemainingInactivityMilliseconds(610000), Is.Zero);

        state.RecordActivity(610000);

        Assert.That(state.HasInactivityExpired(1209999), Is.False);
        Assert.That(state.HasInactivityExpired(1210000), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => state.RecordActivity(609999));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            state.HasInactivityExpired(609999));

        state.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void DisabledActivityTimeoutNeverExpires()
    {
        TrackingDuplexStream stream = new TrackingDuplexStream();
        Pop3ServerOptions options = CreateOptions();
        options.InactivityTimeoutMilliseconds = 0;
        Pop3ServerStateMachine state = new Pop3ServerStateMachine();
        state.OpenConnection(CreateConnection(stream, true), options, 4);

        Assert.That(state.GetInactivityCancellationToken().CanBeCanceled,
            Is.False);
        Assert.That(state.HasInactivityExpired(long.MaxValue), Is.False);
        Assert.That(state.GetRemainingInactivityMilliseconds(long.MaxValue),
            Is.EqualTo(-1));

        state.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void CloseIsIdempotentAndHonorsCallerOwnership()
    {
        TrackingDuplexStream ownedStream = new TrackingDuplexStream();
        TrackingDuplexStream callerStream = new TrackingDuplexStream();
        Pop3ServerStateMachine owned = new Pop3ServerStateMachine();
        Pop3ServerStateMachine callerOwned = new Pop3ServerStateMachine();
        ownedStream.StateAtDispose = owned;
        callerStream.StateAtDispose = callerOwned;
        owned.OpenConnection(
            CreateConnection(ownedStream, false),
            CreateOptions(),
            0);
        callerOwned.OpenConnection(
            CreateConnection(callerStream, true),
            CreateOptions(),
            0);

        owned.CloseConnection();
        owned.CloseConnection();
        callerOwned.CloseConnection();
        callerOwned.CloseConnection();

        Assert.That(owned.GetCurrentState(), Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(callerOwned.GetCurrentState(), Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(ownedStream.DisposeCount, Is.EqualTo(1));
        Assert.That(ownedStream.ObservedStateAtDispose,
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(callerStream.DisposeCount, Is.Zero);
        Assert.That(callerStream.CanWrite, Is.True);
        Assert.That(owned.GetConnectionContext().Stream, Is.Null);
        Assert.That(callerOwned.GetConnectionContext().Stream, Is.Null);
        Assert.Throws<Pop3StateException>(() => owned.GetTransportStream());
        Assert.Throws<Pop3StateException>(() => callerOwned.GetGreeting());

        callerStream.Dispose();
    }

    [Test]
    public void OpenValidatesConnectionOptionsGreetingAndTimestampBeforeStateChange()
    {
        TrackingDuplexStream stream = new TrackingDuplexStream();
        Pop3ServerConnection connection = CreateConnection(stream, true);
        Pop3ServerOptions options = CreateOptions();
        Pop3ServerStateMachine state = new Pop3ServerStateMachine();

        Assert.Throws<ArgumentNullException>(() => state.OpenConnection(
            null!, options, 0));
        Assert.Throws<ArgumentNullException>(() => state.OpenConnection(
            connection, null!, 0));
        Pop3ServerConnection missingStream = new Pop3ServerConnection();
        Assert.Throws<ArgumentException>(() => state.OpenConnection(
            missingStream, options, 0));
        MemoryStream readOnly = new MemoryStream(new byte[0], false);
        Pop3ServerConnection invalidStream = CreateConnection(readOnly, true);
        Assert.Throws<ArgumentException>(() => state.OpenConnection(
            invalidStream, options, 0));
        options.InactivityTimeoutMilliseconds = 599999;
        Assert.Throws<ArgumentOutOfRangeException>(() => state.OpenConnection(
            connection, options, 0));
        options.InactivityTimeoutMilliseconds = 600000;
        Assert.Throws<ArgumentOutOfRangeException>(() => state.OpenConnection(
            connection, options, -1));
        options.GreetingText = null!;
        Assert.Throws<ArgumentException>(() => state.OpenConnection(
            connection, options, 0));
        options.GreetingText = "bad\r\ngreeting";
        Assert.Throws<InvalidDataException>(() => state.OpenConnection(
            connection, options, 0));
        options.GreetingText = new string('x', 600);
        Assert.Throws<InvalidDataException>(() => state.OpenConnection(
            connection, options, 0));

        Assert.That(state.GetCurrentState(), Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(stream.DisposeCount, Is.Zero);

        options.GreetingText = "ready";
        state.OpenConnection(connection, options, 0);
        TrackingDuplexStream replacementStream = new TrackingDuplexStream();
        Assert.Throws<Pop3StateException>(() => state.OpenConnection(
            CreateConnection(replacementStream, false),
            options,
            0));
        Assert.That(replacementStream.DisposeCount, Is.Zero);

        state.CloseConnection();
        stream.Dispose();
        replacementStream.Dispose();
        readOnly.Dispose();
    }

    static void AssertGreetingRoundTrip(Pop3Response greeting)
    {
        string formatted = FormatResponse(greeting);
        Pop3WireReader reader = new Pop3WireReader(
            new MemoryStream(Encoding.ASCII.GetBytes(formatted)),
            8);

        Pop3Response parsed = reader.ReadResponse(
            Pop3Defaults.MaximumResponseLineOctets,
            CancellationToken.None);

        Assert.That(parsed.Text, Is.EqualTo(
            "mail ready " + greeting.GreetingTimestamp));
        Assert.That(parsed.GreetingTimestamp, Is.EqualTo(greeting.GreetingTimestamp));
    }

    static string FormatResponse(Pop3Response greeting)
    {
        MemoryStream stream = new MemoryStream();
        Pop3WireWriter writer = new Pop3WireWriter(stream, 8);
        writer.WriteResponse(
            greeting,
            Pop3Defaults.MaximumResponseLineOctets,
            CancellationToken.None);
        return Encoding.ASCII.GetString(stream.ToArray());
    }

    static Pop3ServerConnection CreateConnection(
        Stream stream,
        bool leaveOpen)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = leaveOpen;
        return connection;
    }

    static Pop3ServerOptions CreateOptions()
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.InactivityTimeoutMilliseconds = 600000;
        return options;
    }

    class TrackingDuplexStream : MemoryStream
    {
        public int DisposeCount;
        public Pop3ServerStateMachine? StateAtDispose;
        public Pop3SessionState ObservedStateAtDispose;

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            if (StateAtDispose != null)
            {
                ObservedStateAtDispose = StateAtDispose.GetCurrentState();
            }
            base.Dispose(disposing);
        }
    }
}
