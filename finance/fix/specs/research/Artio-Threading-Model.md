_The FIX Gateway can tradeoff CPU utilisation for throughput + latency by different thread configurations_

The FIX Gateway uses an Agent based design and depends on Aeron which is a similar design. Agents execute on single threads and asynchronously react and respond to events. The Gateway controls its use of threads through two abstractions: a **Scheduler** and various **IdleStrategy** implementations. A scheduler determines how Agents are allocated to threads.

# EngineScheduler

The Engine process has its scheduler configured through the `EngineConfiguration.scheduler` option. There are three different `EngineScheduler` implementations that ship with the Gateway, but you can implement your own, see the `EngineScheduler` interface for implementation notes.

* `DefaultEngineScheduler` - Use when you want lowest latency and highest throughput. This uses a separate thread for each of the Framer, Archiver, Monitoring agents and the Aeron Client conductor.
* `LowResourceEngineScheduler` - Use when you want to minimise the amount of CPU used. This combines the Framer, Archiver, Monitoring and Aeron Client Conductor Agents onto a single thread. This can also, optionally, combine a MediaDriver in INVOKER threading mode onto the same thread.
* `LockStepFramerEngineScheduler` - Use when you want to write a deterministic test of the Framer that controls the rate of its operations. This lets the Archiver, Monitoring Agent and Aeron Client Conductor agents run on their own threads, but lets you control the way that the Framer is polled.

# LibraryScheduler

* `DefaultLibraryScheduler` - Use if you have a single `FixLibrary` instance per process or lots of CPU. This schedules both the monitoring agent and also the Aeron Client Conductor agent onto a single thread.
* `SharedLibraryScheduler` - Use if you have multiple `FixLibrary` instances per process and want to reduce CPU used. This is the only scheduler that is thread-safe and safe to share across multiple instances. It schedules a configurable number of libraries and puts all of their monitoring agents and also Aeron Client Conductor agents onto a single thread.

# IdleStrategy

Details and examples of use for the `IdleStrategy` can be found in the Aeron and Agrona projects.
