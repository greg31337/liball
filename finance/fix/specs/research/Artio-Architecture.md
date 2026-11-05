
# The Big Picture

![Big Picture](FIX-Gateway%20Architecture.png)

# Modules

* The **Framer Module** handles incoming requests from TCP connections and identifies individual messages from a constant stream of TCP stream data.
* The **Parser Module** takes an individual message and calls application handler methods in order to inform them of the values of messages.
* The **Builder Module** is called by application code to generate valid FIX messages.
* The **Logger Module** ensures that messages and any relevant business logic is completely persisted.

![Modules](FIX-Gateway%20Modules.png)

# Message Flow

*Message from a client*

1. A message is sent from a client via TCP to **Framing** where it is read off the wire and identified as a valid message.
1. The **Parser** parses the message, calling callbacks into application code.

*Message to a client*

1. The application code writes a message using the **Builder**.
1. The **Parser** parses the message and calls application callbacks to inform them of the contents of the message.

*Historical analysis of a message*

1. The **Logger** gets queried by a user wanting to find a set of messages.
1. The **Logger** looks up the appropriate message from its persistent log.

# Dependencies

* The **Framer** can be used with the **Parser** independent of **Logging** to deal with client connections.
* The **Parser** can be independently used to parse FIX messages as long as they are correctly framed and valid.
* The **Builder** can be independently used to build FIX messages.
