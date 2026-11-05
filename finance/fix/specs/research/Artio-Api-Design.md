Artio has extensive Javadoc and a bunch of samples documenting its usage. This wiki page outlines common API patterns that are used throughout Artio.

# Library Polling

If you're interacting with an Artio Library then you need to regularly call its poll() method on your thread's duty-cycle. This is similar to polling an Aeron `Subscription` object. In order to help make best use of the Library then taking a look at the Agrona project's `Agent` and `IdleStrategy` patterns will help your application.

If you don't poll with regularity then the Artio Library could timeout its connection to its Engine. If your application has regular long pauses (for example Garbage Collection pauses or a under-resourced machine) then you should configure the `replyTimeoutInMs()` on both the Library and Engine configuration to be long enough not to timeout during those pauses.

# `Reply` objects

Most of the `FixLibrary` operations return a `Reply<T>` object. This is a common interface for modelling asynchronous operations and has a generic type parameter, `T` that represents its result.

Each operation can be in a variety of states. It is initially `EXECUTING` whilst the operation is happening. When the operation has finished with the resulting value it is in the `COMPLETED` state. At this point you can call the `resultIfPresent` method, otherwise it will return null. If the operation doesn't complete within some time bound then it terminates in a `TIMED_OUT` state, whilst if there's an error the state will be `ERRORED`. In this final state you can retrieve the exception from the `Reply.error()` method, otherwise that will return null.

All `Reply` objects should be handled on the single that is associated with their `FixLibrary` object. Java 8 onwards has an inbuilt `CompletableFuture` type that could have been used for these API operations. However it is designed to be used on multiple different threads in a way that do not fit will with the Artio threading model. It is also garbage heavy.