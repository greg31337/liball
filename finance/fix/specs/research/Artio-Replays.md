The FIX protocol allows parties to send `ResendRequest` messages that request a replay of messages. Artio handles this for you automatically though this can customised and configured in a couple of ways (see below).

## Gapfills

When a FIX replay happens Artio replaces any administrative messages defined by the FIX protocol with gapfill messages. Artio sends as few gapfill messages as possible - combining gapfills from multiple sequential administrative messages. The set of messages can be customised by setting the `EngineConfiguration.gapfillOnReplayMessageTypes()` configuration option to the String values FIX MsgType (35) tag. 

## Callbacks

If you want to know what messages have been replayed then you can implement your own `ReplayHandler` callback and set that via the `EngineConfiguration.replayHandler()` gapfillOnReplayMessageTypes(). Note that this callback will be called on the Replay thread.

## Replay when requesting a `Session`

Artio has a second way in which message replay occur. When you request a `Session` as a `FixLibrary` you can pass in values for the last sequence number that you've received and the last sequence index that you've received. At that point Artio will replay, via callbacks to your `SessionHandler`, any FIX messages received by the Engine since this message.

A sequence index here is a number that represents a contiguous sequence of FIX messages. Some FIX sessions will reset their sequence number on connection, some will increment them explicitly at an end of day or end of week operation. Either way this reset process results in an increment of the sequence index.

## Message Logging Configuration Implications

It is possible to configure Artio to not log messages into an archive using the `EngineConfiguration.logInboundMessages()` and `EngineConfiguration.logOutboundMessages()` options. If you do not persistently log messages then you cannot replay them. Any `ResendRequest` messages will be gapfilled and no messages will be replayed when you request a Session.
