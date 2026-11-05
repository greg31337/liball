# Logging

## What can be Logged

You can switch on debug logging by passing the following commandline parameter: `-Dfix.core.debug=true`. Different types of debug logging can be enabled by providing a comma separated list of different tags. The available options are in the `uk.co.real_logic.artio.LogTag` class. For example if you want to enable logging for fix messages, closing and state cleanup and nothing internal you can use:

`-Dfix.core.debug=FIX_MESSAGE,CLOSE,STATE_CLEANUP`

It is possible to filter the type of fix messages logged using the `fix.core.debug.msg_types` property. This takes a comma separated list of FIX message types, so for example to log new order single and execution reports messages you could use:

`-Dfix.core.debug=FIX_MESSAGE -Dfix.core.debug.msg_types=D,8`

There are some caveats to how this message type filter applies. Anything logged through the FIX_MESSAGE_FLOW log tag is covered and all FIX_MESSAGE tags, apart from situations where the body length is invalid are also filtered.

You can filter which threads in your system are logged using the `fix.core.debug.thread` property.

## Where can it be logged

By default the debug logger prints to your process' standard out. You can set the `fix.core.debug.file` property to print this output to a file alternatively.

You may wish to integrate Artio's debug logger with some other external logging system like slf4j or log4j in order to do this you need to extend the service provider class `uk.co.real_logic.artio.AbstractDebugAppender`. This is loaded through Java's `ServiceLoader.load` mechanism - See [the service loader javadoc](https://docs.oracle.com/en/java/javase/11/docs/api/java.base/java/util/ServiceLoader.html) for details.

# Monitoring and Statistical Counters

In order to monitor the progress of Artio in production it's helpful to be aware of a number of counters that are exported from the Artio system. These are exposed via [Aeron's well documented counter mechanism](https://github.com/aeron-io/aeron/wiki/Monitoring-and-Debugging). Generally these counters measure failure events. Each class of counter has a type ID associated with it and documented below:

## Engine-wide Counters

Failed Inbound - `10000` - Failed to successfully offer a message on the inbound Aeron stream - ie going from the `FixEngine` to a `FixLibrary`. Rapid increases in this counter indicates `FixLibrary` instances backpressuring the `FixEngine`.

Failed Outbound - `10001` - Failed to successfully offer a message on the outbound Aeron stream - ie going from a `FixLibrary` to the `FixEngine`. Rapid increases in this counter indicates the `FixEngine` backpressuring `FixLibrary` instances.

Failed Replay - `10002` - Failed to successfully offer a message on the replay Aeron stream - ie going from the `Replayer` inside the `FixEngine` to the `Framer`. Rapid increases in this counter indicates `Framer` instances backpressuring the `Replayer` - or possibly a counter-party requesting loads of replay operations.

## Once per FIX connection Counters

Messages Read - `10003` - the number of fully framed FIX messages read off of the TCP connection. This excludes the messages with an invalid checksum value.

Bytes In Buffer - `10004` - the number of bytes that have been queued up ready to be sent, but not sent yet. This number should be 0 for any FIX session that isn't a [Slow Consumer](https://github.com/artiofix/artio/wiki/Slow-Consumer).

Invalid Library Attempts - `10005` - the number of messages, attempted to be sent, that have been ignored because the wrong library sent them. This indicates that a library believes it owns a Session when it actually doesn't.

Sent message sequence number - `10006` - this is the last sent msgSeqNum for a given FIX session.

Received message sequence number - `10007` - this is the last received msgSeqNum for a given FIX session.

# Directory structure

Artio stores persistent state using both the Aeron Archiver and its own files.

The Aeron Archiver is used for a persistent log of messages that are exchanged between `FixEngine` and `FixLibrary`. Please refer to the Aeron Archiver documentation for how to read these files.

All of Artio's persistent state is held by the `FixEngine` and stored in files in a directory configured set by `EngineConfiguration.logFileDir()`.

## Session Information

FIX Sessions are assigned surrogate session ids given their unique company ids. The mapping between these surrogate session ids and company ids is stored in `session_id_buffer`. This state must persist between restarts if you want to use sequence numbers that persist over restarts.

## Sequence Numbers

Artio's Sequence Number Index persists the mapping between Session Ids and the last associated sequence number for that session. These are stored in two files called: `sequence_numbers_received` and `sequence_numbers_sent`.

## Replay Positions

Artio keeps track of a mapping between the Aeron stream position and the FIX message sequence numbers so that it can replay messages correctly. Files of the form replay-index-<fixSessionId>-<streamId> record that mapping. Here the stream-id is the Aeron Stream Id and the fixSessionId is the surrogate key assigned to each FIX session. The last position at which replays were index up to is record in the replay-positions-<streamId>. These files only need to be persisted over restarts if persistent sequence numbers are used or if a catchup replay from a previous sequence index is to be requested.

# Multiple Engine Instances with the same Media Driver

It is possible to run multiple Artio Engine instances with the same Aeron Media Driver if you wish to reduce the number of cores that you're using. In order to achieve this, however, you need to ensure that the Engine instances don't have a clash in terms of their stream configuration. There are two ways to achieve this.

1. Use different Aeron stream ids. This is the preferred solution when using the IPC channel to communicate between  `FixEngine` and `FixLibrary` instances, which most users do. In order to achieve this, you need to set `LibraryConfiguration.inboundLibraryStream()` and `LibraryConfiguration.outboundLibraryStream()` on the `FixLibrary` instances and 
`EngineConfiguration.inboundLibraryStream()`, `EngineConfiguration.outboundLibraryStream()` `EngineConfiguration.outboundReplayStream()`, `EngineConfiguration.archiveReplayStream()` for the `FixEngine`. It doesn't really matter what numbers you set those stream ids to - as long as they don't clash with another `FixEngine` instance.
1. Put the engines on different Aeron channels. Specify `EngineConfiguration.libraryAeronChannel()` when configuring the `FixEngine` and `LibraryConfiguration.libraryAeronChannels()` when configuring the `FixLibrary`. This is useful when you're running `FixLibrary` instances on different machines.