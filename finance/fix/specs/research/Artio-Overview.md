This page explains the key concepts and components of the FIX Gateway and how they interact with each other.

## The Big Picture

![Component Diagram](Component%20Interaction.png)

The FIX Gateway has two key components - Engine and Library. These can be run in separate processes or the same process. The Engine handles TCP Connections, Frames FIX Messages and logs them into an archive. When Library instances become unavailable the Engine manages sessions that it has accepted.

The Library sits within your Application's process and manages FIX sessions. Each Library instance is single threaded, so if you want to use multiple threads you either need to manage the handoff between those threads yourself or to run one library instance per thread.

## Aeron

Communications between the Engine and Library go over Aeron. Aeron has a separation of concerns between the "Media Driver" which replicates its log file over the network and the "Client" which you have in process in your application. In the case of the FIX Gateway you don't need to worry about the client because both the Engine instance and the Library instances have a client in them.

A media driver also needs to run in order to run the Engine or Library. Normally you should run a single media driver per machine. So if you've got multiple processes on the same machine they can share a single media driver, but if you want to have an Engine on one machine and a Library on another then you need two media drivers - one for each machine.

If the Engine and Library processes are sitting on the same machine then you can use Aeron's IPC transport to communicate between them, if you're going over different machines then you can use UDP. This is configured through the `aeronChannel` configuration option. You need to an `AeronArchiver` on the Engine's machine as well as this is used for archiving, this can be most easily be achieved using Aeron's `ArchivingMediaDriver`.

Artio maintains a single inbound (Engine to Library) Aeron stream and a single outbound (Library to Engine) stream. There is a single Aeron Session for inbound writes from the engine and a single Aeron Session per library for outbound writes. Each of these Aeron Sessions multiplexes over multiple FIX Sessions. There is a separate Replay Stream between the Replayer and the Framer.

## Diving into the Engine

![Artio Engine Diagram](Artio%20Engine%20Diagram.png)

The Artio Engine has several agents, each of which are single threaded
 * *Framer* - Responsible for initiating and accepting TCP connections. Framing messages received inbound by TCP and putting them onto an Aeron stream. Sending framed messages sent outbound into a TCP connection.
 * *Indexer* - reads the inbound and outbound messages and writes out an index of those messages that is used during message replay.
 * *Replayer* - replays the messages based upon FIX `ResendRequest` messages.

There are multiple FIX Sessions per Engine.

## Diving into the Library

Libraries are much simpler than Engines. They don't hold any persistent state and mostly just managed FIX Session protocol instances and send and receive messages to an Engine. Your application will mostly interact with Artio Libraries in terms of code.

Each Library can manage multiple Sessions and should only be used on a single thread. Each of the Session objects should also only be used on that same thread. If you want to multi-thread your application's interaction with FIX then you should use multiple library instances.

## Session Ownership

The [Session Management](Session-Management) page contains more information about FIX sessions and how they map to Artio. Each Session is owned by either a Library or an Engine. Its owner controls how it is polled and manages its state. The Library object has methods to control ownership. If you want to try to acquire a FIX Session from an Engine then you can request it. If you want to hand ownership of a Session over to an Engine then you should release it.
