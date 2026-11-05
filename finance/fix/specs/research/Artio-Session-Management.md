# FIX Sessions

A FIX session is a bi-directional stream of ordered and numbered messages between two parties. The underlying messages are transmitted over TCP and framed using the FIX protocol. The party that starts the TCP connection is called the Initiator and the party that receives this connection is called the Acceptor.

# Sessions in Artio

Each FIX session has a corresponding `uk.co.real_logic.artio.session.Session` object that manages the protocol logic for the session and can be inspected by your application.  Artio handles session logic for you which includes logging on and off, handling sequence number resets, gapfills and resend requests. Artio also keeps a persistent archive of the messages that have been sent and received.

## Initiating Sessions

Here is an example snippet of code for initiating a connection numbered comments are referenced from the explanation below.

```java
// (1)
final SessionConfiguration sessionConfig = SessionConfiguration.builder()
    .address("localhost", 9999)
    .targetCompId(ACCEPTOR_COMP_ID)
    .senderCompId(INITIATOR_COMP_ID)
    .build();

// (2)
final Reply<Session> reply = library.initiate(sessionConfig);

// (3)
while (reply.isExecuting())
{
    idleStrategy.idle(library.poll(1));
}

// (4)
if (!reply.hasCompleted())
{
    System.err.println(
        "Unable to initiate the session, " + reply.state() + " " + reply.error());
    return;
}

// (5)
System.out.println("Replied with: " + reply.state());
final Session session = reply.resultIfPresent();
```

In order to initiate a connection you need to provide a `SessionConfiguration` object (1) that determines where you're connecting to and provides the configuration values for the Session. To start the process call the `initiate` method on the library (2) which will return you a `Reply` object. Since the Artio Library API is asynchronous and single threaded the `initiate` method can't just block your thread until the Session is setup. The reply represents the state of the Session initiation operation as it progresses. See [API Design](Api-Design) for more details on reply objects, including waiting for them to complete (3) and checking their error status (4).  Once the operation has completed successfully you can get your `Session` object from the reply (5).

## Accepting Sessions

When another Gateway connects to your system the `FixEngine` will accept this Session for you and proceed with the logon process. Any connected libraries will be notified of this by a callback on their configured `SessionExistsHandler`. At this point it is up to your application to choose how to proceed. If you only have one `FixLibrary` connected then you can just request that your library manage that Session and the `AcquiringSessionExistsHandler` will do this for you. If you have multiple threads or processes with different libraries then you will have decide whether to request it or not depending upon an appropriate load balancing or sharding strategy.

In order to request a given Session in that callback you should invoke the `FixLibrary.requestSession` method. This takes a `sessionId` parameter, for which your callback can use the `surrogateSessionId` parameter it is passed. This returns a `Reply` object which can handled in the usual way, see [API Design](Api-Design).  Once your library takes control of the session then the configured `SessionAcquireHandler` callback will be called that hands you an instance of the `Session` object, it can also be retrieved from the `Reply` object.

Sessions can be managed by both the `FixEngine` and the `FixLibrary` instances. So during the period when your session has been accepted by the `FixEngine` but not acquired by any `FixLibrary` then heartbeats, gapfills, resend requests, etc. will all be processed.

## Offline Sessions

The `FixLibrary.requestSession` method can also be used to request sessions that are offline (not currently connected but previously connected). This enables several functions:
 * Querying the state of the session, eg: last sent and received sequence numbers, `lastSequenceResetTime()` and `lastLogonTime()`.
 * Query previously received messages from a counter-party.
 * "Send" new messages. In the case of an offline session these aren't immediately sent but indexed and archived. The next time the FIX session reconnects and sequence numbers aren't reset then these messages will be received by the counter-party via FIX's usual resend request mechanism.

If a FIX session reconnects whilst it a library owns an offline `Session` then this same `Session` object owns the FIX session and that session receives the Logon message and can be active.

## Releasing Sessions

If you wish to rebalance a `Session` between multiple `FixLibrary` instances you can hand its management back over to the `FixEngine` using the `FixLibrary.releaseToGateway` method, this returns a `Reply` object to indicate the success of the operation.

If your library is closed or times out from its connection to the `FixEngine` then all the sessions that it manages will be timed out as well and managed on the `FixEngine` automatically. This enables Artio users to easily implement Continuous Deployment of their business logic whilst remaining connected to a Fix Server or without dropping connections from clients.

# Session Identification

How sessions are identified in FIX can be configured. It may be that the pair of counter parties is identified solely by the SenderCompID (49) and TargetCompID (56) fields. It might also involve SenderSubID (50), SenderLocationID (142), TargetSubID (57) and TargetLocationID (143) fields. In order to support these different requirements Artio uses a configurable `SessionIdStrategy` interface. By default Artio ships an implementation for the CompID pairs (`SessionIdStrategy.senderAndTarget()`) and also using the Sub Id fields as well (`SessionIdStrategy.senderTargetAndSub()`). You can implement your own `SessionIdStrategy` if you wish to support other strategies. The `SessionIdStrategy` should be set on both the `EngineConfiguration` and the `LibraryConfiguration`.
