# Slow Consumer Support

## Introduction

Artio provides **slow consumer support** to protect the engine and other sessions from a single TCP connection that cannot read data fast enough. When the outbound TCP send buffer for a FIX session fills up, Artio:

- Marks the session as **slow**.
- Buffers additional outbound messages in a dedicated **slow consumer buffer**.
- Eventually **disconnects** the session with `DisconnectReason.SLOW_CONSUMER` if it falls too far behind or remains slow for too long.

This mechanism preserves **fairness** and **latency** for healthy sessions while still allowing reasonable headroom for temporarily slow clients.

---

## Behaviour

### When a session becomes a slow consumer

For each connection, Artio tracks how many bytes are queued to be written to the TCP socket:

- When the TCP send buffer becomes full, outbound data for that session is placed into a **slow consumer buffer**.
- The current backlog for a connected session can be inspected via:
    - `ConnectedSessionInfo.bytesInBuffer()`
    - `Session.isSlowConsumer()`
    - Admin views such as `FixAdminSession.isSlow()`

The session is considered a **slow consumer** while it has any outstanding bytes in this buffer and Artio has marked it as slow.

### Disconnection thresholds

Two main conditions lead to a `SLOW_CONSUMER` disconnect:

1. **Buffer size limit exceeded**

    - Artio maintains a per-connection limit for how far a session may fall behind in the slow consumer buffer.
    - If queuing another message would make the buffered usage exceed `senderMaxBytesInBuffer`, the engine disconnects the session immediately with `DisconnectReason.SLOW_CONSUMER`.

2. **Slow for too long (timeout)**

    - Once a session is marked slow, a timer is started.
    - If the session remains a slow consumer longer than `slowConsumerTimeoutInMs`, a periodic poll will also disconnect it with `DisconnectReason.SLOW_CONSUMER`, even if the buffer is not growing further.

System tests show this behaviour explicitly, for example:

- `SlowConsumerTest` drives a client that stops reading from the socket until the buffer usage reaches or exceeds the configured maximum, then asserts that the session becomes slow and is disconnected.
- `FixSenderEndPointTest` verifies that a slow consumer is not disconnected before the timeout, but is disconnected after `DEFAULT_SLOW_CONSUMER_TIMEOUT_IN_MS` has elapsed.

### Recovery

Once a `SLOW_CONSUMER` session is disconnected, applications typically:

- Allow the client to **reconnect** the FIX session.
- Use **persistent sequence numbers** and FIX replay (Resend Requests) so the slow client can catch up on any missed messages instead of losing state.

---

## How to Configure It

Slow consumer behaviour is controlled primarily via **engine configuration**.

### Buffer size: `senderMaxBytesInBuffer`

`EngineConfiguration.senderMaxBytesInBuffer(long)` (and its default) define how many bytes Artio will buffer per connection before forcibly disconnecting a slow consumer:

- The buffer is used when the TCP socket cannot accept more data.
- If `bytesInBuffer + messageLength > senderMaxBytesInBuffer`, Artio disconnects the session with `DisconnectReason.SLOW_CONSUMER`.

### Timeout: `slowConsumerTimeoutInMs`

`EngineConfiguration.slowConsumerTimeoutInMs(long)` configures how long a session may remain in a slow state before timing out:

- Default value: `DEFAULT_SLOW_CONSUMER_TIMEOUT_IN_MS = 10_000` ms (10 seconds).
- If `isSlowConsumer()` remains true past `slowConsumerTimeoutInMs`, `FixSenderEndPoint.poll(long timeInMs)` will disconnect with `SLOW_CONSUMER`.

**Example:**

````java
final EngineConfiguration configuration = new EngineConfiguration()
.senderMaxBytesInBuffer(128 * 1024L)   // 128 KiB per-connection backlog
.slowConsumerTimeoutInMs(30_000L);     // 30s slow-consumer timeout
````


### Tuning guidelines

- **Increase `senderMaxBytesInBuffer`** to tolerate short bursts or high-latency links, but keep it bounded so a single client cannot exhaust memory or cause unacceptable latency for others.
- **Adjust `slowConsumerTimeoutInMs`** if clients sometimes stall briefly (e.g. due to GC pauses) but usually recover quickly.
- Keep both values at **per-connection** scales; they are not global limits.

## Callbacks

Artio exposes slow consumer state to applications via the **library API**.

### `SessionHandler.onSlowStatus`

Implementations of `uk.co.real_logic.artio.library.SessionHandler` can observe slow status transitions:



- `hasBecomeSlow == true`  
  The session has just been detected as a slow consumer.
- `hasBecomeSlow == false`  
  The session has caught up and is no longer considered slow.

This callback is invoked whenever the slow flag changes, allowing applications to:

- Log or alert when a particular client becomes slow.
- Apply business-level throttling or temporarily stop sending certain traffic.
- Track how often individual sessions hit slow-consumer conditions.

### `Session.isSlowConsumer`

At the session level:



- Returns `true` while the session is marked as a slow consumer.
- Can be queried by the application (e.g. in custom monitoring or decision logic).

### Admin / offline views

For admin tools and offline inspection, Artio provides:

- `FixAdminSession.isSlow()` – returns whether the session is considered slow (or not connected).
- `ConnectedSessionInfo.bytesInBuffer()` – number of bytes currently outstanding in the slow consumer buffer for a connected session.

These are typically used by operational tooling for diagnostics and reporting.

## Monitoring

Effective slow consumer monitoring usually combines:

1. **Programmatic metrics**, and
2. **Logging**.

### Programmatic metrics

- **Per-session backlog**

    - Use `ConnectedSessionInfo.bytesInBuffer()` to see how many bytes are currently queued for a session.
    - Track this over time to identify clients that are frequently close to the configured limit.

- **Slow state flags**

    - `Session.isSlowConsumer()` at runtime.
    - `FixAdminSession.isSlow()` for admin views (including disconnected sessions that were previously slow).

- **Callbacks**

    - Count or log transitions in `SessionHandler.onSlowStatus(...)` to measure how often and for how long sessions are slow.

### Aeron counter for slow consumer buffer usage

Internally, Artio also tracks the slow consumer buffer usage in an **Aeron counter**:

- The engine allocates a counter via `FixCounters.bytesInBuffer(connectionId, address)`.<br>
  This uses `FixCountersId.BYTES_IN_BUFFER_TYPE_ID` and a label of the form:

```text
"Quarantined bytes for {address} id = {connectionId}"
```
- The returned `AtomicCounter` is passed into sender end-points (e.g. `FixSenderEndPoint`, `ImplicitFixPSenderEndPoint`) and updated whenever bytes are enqueued or drained from the slow consumer buffer, so its value reflects the current **bytes outstanding in the slow consumer buffer** for that connection.

Because this is a standard Aeron counter:

- It can be **exported and scraped** using:
    - `Aeron.countersReader()` within the same JVM, or
    - external tools that read the CnC file (for example AeronStat or Aeron Insights, using `CountersReader` over the counters metadata and values buffers).
- You can identify it by:
    - `typeId == FixCountersId.BYTES_IN_BUFFER_TYPE_ID.id()` (10004), and
    - a label starting with `"Quarantined bytes for "`.

This gives you a low-level, implementation-agnostic metric that can be fed directly into metrics systems such as Prometheus, Grafana, or DataDog (via AeronStat or custom code) alongside higher-level Artio metrics.

### Logging

Artio includes a dedicated **log tag** for slow consumer events:

- `LogTag.SLOW_CONSUMER` – specifically for logging the cause of slow consumer disconnects.

To enable detailed logs:

- Include `SLOW_CONSUMER` in `fix.core.debug` / `DEBUG_PRINT_MESSAGES_PROPERTY`, along with any other tags you want (for example `LIBRARY_MANAGEMENT,FIX_CONNECTION,SLOW_CONSUMER,STATE_CLEANUP,CLOSE`).
- When a session is disconnected due to slow consumer behaviour, Artio logs a message containing:
    - Connection id and session id.
    - Buffer usage vs `maxBytesInBuffer`.
    - Whether this was a timeout-based or buffer-size-based disconnect.

These logs are extremely helpful to answer **“why was this client disconnected?”** in production.

### Operational practices

Recommended operational checks:

- **Alerting on repeated slow status**  
  If `onSlowStatus(..., hasBecomeSlow = true)` fires frequently for a given session, investigate the client’s read-side processing and network path.

- **Correlating with disconnects**  
  Correlate `SLOW_CONSUMER` disconnect logs with:
    - `onSlowStatus` transitions.
    - Application-level logs on the client.
    - `bytesInBuffer()` peaks.

- **Capacity planning**  
  Use historical distributions of:
    - Maximum `bytesInBuffer()` per session.
    - Time spent in slow state.

  to refine `senderMaxBytesInBuffer` and `slowConsumerTimeoutInMs` for your deployment.

---

Slow consumer support is designed to be **automatic and safe by default**, while still giving you the hooks (configuration, callbacks, and metrics) needed to adapt behaviour to your latency and throughput requirements.