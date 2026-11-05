Even though FIX is a popular and useful standard, some aspects of implementation are ambiguous, poorly documented or inconsistent between implementations. This page documents our decisions where those choices come up.

## Connections
A session can span multiple connections, so what happens if you have a new connection arrive at your gateway when there's already a connection?

We allow a new connection that logs correctly in to hijack the session of the previously authenticated connection and disconnect it.
