# Logging

The FIX Gateway logging module generates a persistent log of all the messages that pass through the replicated Aeron streams. This is useful for reliability, auditing and message replay.

# Archival

The archival system copies messages off the stream into log files in a directory. The log files are identified by a composite key of stream id and term id. Offsets into the archive files are identical to those on the Aeron subscription that they are consuming from, which means positions in the archive have a 1-to-1 mapping to positions in the stream. Messages can be looked up from the Archive using the stream id and position.

# Indexing

Indices are generic and built in a non-blocking fashion on a separate agent to the Archiver. In order to define an index, you just implement the Index interface.

# Replay Index

Fix Message replay requires messages be indexed by a composite key of session id and sequence number. The index files are identified by session id, and then consist of records listing the stream id, sequence number and position of the message.