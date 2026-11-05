# FIX vs FIXP

Artio provides support for both the commonly used test based FIX protocol and the more modern binary FIXP protocol. If you're interested in details of the FIXP protocol standards then they can be found over at https://www.fixtrading.org/standards/fixp/. This page doesn't document the protocol itself but how it is supported by Artio.

Artio supports two different FIXP Protocols: iLink3 and Binary EntryPoint. Most of the internal implementation is shared but there are significant differences between the two FIXP protocols that result in internal implementation differences.

# Differences between protocols

### Sequence Numbers
iLink3 maintains explicit sequence numbers in each message, whilst Binary EntryPoint has implicit sequence numbers. Binary EntryPoint sequences are checked when establish and sequence messages are exchanged.

### Session identification
iLink3 uses a "UUID" to unique identify connections. The UUID used is a timestamp at the time of connection. Artio considers sessions with the same primary host, and port and "access key id" to be the same logical session and continues the sequence numbers if required. Sequence numbers and session continuity are maintained whether the connection is made to the primary or the backup host.

Binary EntryPoint has an explicit session id allocated to a session, which Artio uses to treat reconnections as the same logical session. Binary EntryPoint provides a "session version id" whose incrementing is controlled by the client in order to identify different sequences of sequence numbers within the same logical session, similar to Artio's internal sequence index concept.

# Feature Support

Artio has a number of features that it supports for FIX protocols that it doesn't support for FIXP currently. These are CatchupReplays, Engine managed sessions, Explicitly releasing sessions from a library to an Engine and Sole Library mode. Additionally if you're using a FIXP protocol with Artio then an Artio instance only supports a single FIXP protocol, but can support multiple different FIX versions concurrently.

There are also differences in feature support between different FIXP protocols:

| Feature                    | iLink3   | Binary EntryPoint |
| -------------------------- | -------- | ----------------- |
| Initiator / Client         | Yes      | No                |
| Acceptor / Server          | No       | Yes               |
| Interleaved Retransmission | Yes      | No                |
| Slow Consumer Disconnect   | No       | Yes               |

# How to get started

Take a look at the pages for [iLink3](https://github.com/artiofix/artio/wiki/ilink3-support) or [Binary Entrypoint](https://github.com/artiofix/artio/wiki/binary-entrypoint-support)


# Logging and Debugging
The FIXP implementations offers two LogTags for printing out session level (FIXP_SESSION) and business level (FIXP_BUSINESS) messages. These can be configured through commandline Java properties, for example:

-Dfix.core.debug=FIXP_SESSION,FIXP_BUSINESS

In order to inspect the produced archive of messages the FixArchivePrinter has been updated to support printing out of FIXP messages.
