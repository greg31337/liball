function run() {
    function assert(condition, message) {
        if (!condition) {
            throw new Error(message);
        }
    }

    function integer(value) {
        return { kind: "integer", value: value };
    }

    function text(value) {
        return { kind: "string", value: value };
    }

    function booleanValue(value) {
        return { kind: "boolean", value: value };
    }

    function array(value) {
        return { kind: "array", value: value };
    }

    function object(value) {
        return { kind: "object", value: value };
    }

    function portable(value) {
        if (value === null || value === undefined) {
            return { kind: "null" };
        }
        if (Array.isArray(value)) {
            return array(value.map(portable));
        }
        if (typeof value === "boolean") {
            return booleanValue(value);
        }
        if (typeof value === "number") {
            return integer(value);
        }
        if (typeof value === "string") {
            return text(value);
        }
        var members = {};
        var names = Object.keys(value);
        var index = 0;
        while (index < names.length) {
            members[names[index]] = portable(value[names[index]]);
            index += 1;
        }
        return object(members);
    }

    function capabilities(binary, chunking) {
        return {
            state: "starting",
            version: 1,
            capabilities: [
                { name: "binary", supported: binary, version: "1" },
                { name: "chunking", supported: chunking, version: "1" }
            ],
            supportsBinary: binary,
            supportsChunking: chunking
        };
    }

    function handshake(nonce, binary, chunking) {
        return object({
            nonce: text(nonce),
            version: integer(1),
            capabilities: array([
                object({ name: text("binary"), supported: booleanValue(binary), version: text("1") }),
                object({ name: text("chunking"), supported: booleanValue(chunking), version: text("1") })
            ]),
            supportsBinary: booleanValue(binary),
            supportsChunking: booleanValue(chunking)
        });
    }

    function envelope(session, sequence, kind, payload) {
        var message = {
            version: 1,
            session: session,
            sequence: sequence,
            origin: "host",
            kind: kind
        };
        if (payload !== undefined) {
            message.payload = payload;
        }
        return message;
    }

    function FakeNode(documentObject, nodeType, name, value) {
        this.ownerDocument = documentObject;
        this.nodeType = nodeType;
        this.nodeName = name || "";
        this.localName = nodeType === 1 ? String(name).toLowerCase() : "";
        this.namespaceURI = nodeType === 1 ? "http://www.w3.org/1999/xhtml" : null;
        this.nodeValue = value || "";
        this.childNodes = [];
        this.parentNode = null;
        this.attributes = [];
        this._attributes = {};
        this._listeners = {};
        this.style = {
            values: {},
            setProperty: function (propertyName, propertyValue) { this.values[propertyName] = propertyValue; },
            removeProperty: function (propertyName) { delete this.values[propertyName]; }
        };
        this.scrollLeft = 0;
        this.scrollTop = 0;
        this.clientWidth = 0;
        this.clientHeight = 0;
        this.open = false;
    }

    FakeNode.prototype._refreshAttributes = function () {
        var names = Object.keys(this._attributes).sort();
        this.attributes = [];
        var index = 0;
        while (index < names.length) {
            this.attributes.push({ name: names[index], value: this._attributes[names[index]], namespaceURI: null });
            index += 1;
        }
    };
    FakeNode.prototype.setAttribute = function (name, value) {
        this._attributes[name] = String(value);
        this._refreshAttributes();
    };
    FakeNode.prototype.getAttribute = function (name) {
        return Object.prototype.hasOwnProperty.call(this._attributes, name) ? this._attributes[name] : null;
    };
    FakeNode.prototype.hasAttribute = function (name) {
        return Object.prototype.hasOwnProperty.call(this._attributes, name);
    };
    FakeNode.prototype.removeAttribute = function (name) {
        delete this._attributes[name];
        this._refreshAttributes();
    };
    FakeNode.prototype.appendChild = function (child) {
        return this.insertBefore(child, null);
    };
    FakeNode.prototype.insertBefore = function (child, reference) {
        if (child.parentNode) {
            child.parentNode.removeChild(child);
        }
        var index = reference ? this.childNodes.indexOf(reference) : this.childNodes.length;
        if (index < 0) {
            index = this.childNodes.length;
        }
        this.childNodes.splice(index, 0, child);
        child.parentNode = this;
        return child;
    };
    FakeNode.prototype.removeChild = function (child) {
        var index = this.childNodes.indexOf(child);
        if (index < 0) {
            throw new Error("missing child");
        }
        this.childNodes.splice(index, 1);
        child.parentNode = null;
        return child;
    };
    FakeNode.prototype.replaceChild = function (next, previous) {
        var index = this.childNodes.indexOf(previous);
        if (index < 0) {
            throw new Error("missing child");
        }
        if (next.parentNode) {
            next.parentNode.removeChild(next);
        }
        this.childNodes[index] = next;
        next.parentNode = this;
        previous.parentNode = null;
        return previous;
    };
    FakeNode.prototype.cloneNode = function (deep) {
        var copy = new FakeNode(this.ownerDocument, this.nodeType, this.nodeName, this.nodeValue);
        var names = Object.keys(this._attributes);
        var attributeIndex = 0;
        while (attributeIndex < names.length) {
            copy.setAttribute(names[attributeIndex], this._attributes[names[attributeIndex]]);
            attributeIndex += 1;
        }
        copy.value = this.value;
        copy.selectionStart = this.selectionStart;
        copy.selectionEnd = this.selectionEnd;
        copy.scrollLeft = this.scrollLeft;
        copy.scrollTop = this.scrollTop;
        copy.clientWidth = this.clientWidth;
        copy.clientHeight = this.clientHeight;
        copy.open = this.open;
        if (deep) {
            var childIndex = 0;
            while (childIndex < this.childNodes.length) {
                copy.appendChild(this.childNodes[childIndex].cloneNode(true));
                childIndex += 1;
            }
        }
        return copy;
    };
    FakeNode.prototype.focus = function () {
        this.ownerDocument.activeElement = this;
    };
    FakeNode.prototype.blur = function () {
        if (this.ownerDocument.activeElement === this) {
            this.ownerDocument.activeElement = null;
        }
    };
    FakeNode.prototype.setSelectionRange = function (start, end, direction) {
        this.selectionStart = start;
        this.selectionEnd = end;
        this.selectionDirection = direction || "none";
    };
    FakeNode.prototype.scrollIntoView = function (options) { this.scrollIntoViewOptions = options; };
    FakeNode.prototype.scrollTo = function (options, y) {
        if (typeof options === "object") {
            this.scrollLeft = options.left || 0;
            this.scrollTop = options.top || 0;
        } else {
            this.scrollLeft = options || 0;
            this.scrollTop = y || 0;
        }
    };
    FakeNode.prototype.addEventListener = function (name, listener, capture) {
        if (!this._listeners[name]) {
            this._listeners[name] = [];
        }
        this._listeners[name].push({ listener: listener, capture: capture === true || (capture && capture.capture === true) });
    };
    FakeNode.prototype.removeEventListener = function (name, listener, capture) {
        var expectedCapture = capture === true || (capture && capture.capture === true);
        var listeners = this._listeners[name] || [];
        var index = listeners.length - 1;
        while (index >= 0) {
            if (listeners[index].listener === listener && listeners[index].capture === expectedCapture) {
                listeners.splice(index, 1);
            }
            index -= 1;
        }
    };
    FakeNode.prototype.dispatchEvent = function (event) {
        event.target = event.target || this;
        event.defaultPrevented = false;
        event.propagationStopped = false;
        event.preventDefault = event.preventDefault || function () { this.defaultPrevented = true; };
        event.stopPropagation = event.stopPropagation || function () { this.propagationStopped = true; };
        var path = [];
        var current = event.target;
        while (current) {
            path.push(current);
            current = current.parentNode;
        }
        var pathIndex = path.length - 1;
        while (pathIndex >= 0 && !event.propagationStopped) {
            var captureListeners = path[pathIndex]._listeners[event.type] || [];
            var captureIndex = 0;
            while (captureIndex < captureListeners.length) {
                if (captureListeners[captureIndex].capture) {
                    captureListeners[captureIndex].listener(event);
                }
                captureIndex += 1;
            }
            pathIndex -= 1;
        }
        pathIndex = 0;
        while (pathIndex < path.length && !event.propagationStopped) {
            var bubbleListeners = path[pathIndex]._listeners[event.type] || [];
            var bubbleIndex = 0;
            while (bubbleIndex < bubbleListeners.length) {
                if (!bubbleListeners[bubbleIndex].capture) {
                    bubbleListeners[bubbleIndex].listener(event);
                }
                bubbleIndex += 1;
            }
            pathIndex += 1;
        }
        return !event.defaultPrevented;
    };

    function FakeDocument() {
        this.activeElement = null;
        this.body = new FakeNode(this, 1, "body", "");
    }
    FakeDocument.prototype.createElement = function (name) { return new FakeNode(this, 1, name, ""); };
    FakeDocument.prototype.createElementNS = function (namespaceUri, name) {
        var node = new FakeNode(this, 1, name, "");
        node.namespaceURI = namespaceUri;
        return node;
    };
    FakeDocument.prototype.createTextNode = function (value) { return new FakeNode(this, 3, "#text", value); };
    FakeDocument.prototype.createComment = function (value) { return new FakeNode(this, 8, "#comment", value); };
    FakeDocument.prototype.createDocumentFragment = function () { return new FakeNode(this, 11, "#document-fragment", ""); };

    var resizeObservers = [];
    function FakeResizeObserver(callback) {
        this.callback = callback;
        this.targets = [];
        this.disconnected = false;
        resizeObservers.push(this);
    }
    FakeResizeObserver.prototype.observe = function (target) { this.targets.push(target); };
    FakeResizeObserver.prototype.unobserve = function (target) {
        var index = this.targets.indexOf(target);
        if (index >= 0) { this.targets.splice(index, 1); }
    };
    FakeResizeObserver.prototype.disconnect = function () { this.targets = []; this.disconnected = true; };
    FakeResizeObserver.prototype.trigger = function (entries) { this.callback(entries); };

    var intersectionObservers = [];
    function FakeIntersectionObserver(callback) {
        this.callback = callback;
        this.targets = [];
        this.disconnected = false;
        intersectionObservers.push(this);
    }
    FakeIntersectionObserver.prototype.observe = function (target) { this.targets.push(target); };
    FakeIntersectionObserver.prototype.unobserve = FakeResizeObserver.prototype.unobserve;
    FakeIntersectionObserver.prototype.disconnect = FakeResizeObserver.prototype.disconnect;
    FakeIntersectionObserver.prototype.trigger = FakeResizeObserver.prototype.trigger;

    function FakeMessageEndpoint() {
        this.listeners = [];
        this.posted = [];
        this.startCount = 0;
        this.closeCount = 0;
    }
    FakeMessageEndpoint.prototype.addEventListener = function (name, listener) {
        if (name === "message") { this.listeners.push(listener); }
    };
    FakeMessageEndpoint.prototype.removeEventListener = function (name, listener) {
        var index = this.listeners.indexOf(listener);
        if (name === "message" && index >= 0) { this.listeners.splice(index, 1); }
    };
    FakeMessageEndpoint.prototype.postMessage = function (message) { this.posted.push(message); };
    FakeMessageEndpoint.prototype.start = function () { this.startCount += 1; };
    FakeMessageEndpoint.prototype.close = function () { this.closeCount += 1; };
    FakeMessageEndpoint.prototype.emit = function (event) {
        var listeners = this.listeners.slice();
        var index = 0;
        while (index < listeners.length) {
            listeners[index](event);
            index += 1;
        }
    };

    var detected = htmlgui.detectCapabilities({
        Uint8Array: function () {},
        MessageChannel: function () {},
        Promise: function () {}
    });
    assert(htmlgui.version === 1, "runtime version");
    assert(detected.supportsBinary && detected.supportsChunking, "binary capability detection");
    assert(detected.capabilities[0].name === "binary", "deterministic capability order");

    var adapterInput = envelope("adapter", 0, "heartbeat");
    var webViewEndpoint = new FakeMessageEndpoint();
    var webViewTransport = htmlgui.detectTransport({ chrome: { webview: webViewEndpoint } });
    var webViewReceived = [];
    assert(webViewTransport && webViewTransport.kind === "webview2", "WebView2 feature detection");
    assert(webViewTransport.start(function (message, metadata) { webViewReceived.push({ message: message, metadata: metadata }); }) && webViewTransport.send(adapterInput) && JSON.parse(webViewEndpoint.posted[0]).session === "adapter", "WebView2 serialized send");
    webViewEndpoint.emit({ data: JSON.stringify(adapterInput), isMainFrame: false });
    assert(webViewReceived.length === 1 && webViewReceived[0].metadata.isMainFrame === false, "WebView2 inbound metadata");
    webViewTransport.stop();
    webViewEndpoint.emit({ data: JSON.stringify(adapterInput), isMainFrame: true });
    assert(webViewEndpoint.listeners.length === 0 && webViewReceived.length === 1, "WebView2 listener cleanup");

    var webKitPosted = [];
    var previousWebKitCalls = 0;
    var previousWebKitReceiver = function () { previousWebKitCalls += 1; };
    var webKitEnvironment = {
        webkit: { messageHandlers: { htmlgui: { postMessage: function (message) { webKitPosted.push(message); } } } },
        htmlguiReceive: previousWebKitReceiver
    };
    var webKitReceived = [];
    var webKitTransport = htmlgui.detectTransport(webKitEnvironment);
    assert(webKitTransport && webKitTransport.kind === "webkit" && webKitTransport.start(function (message) { webKitReceived.push(message); }), "WKWebView feature detection and start");
    assert(webKitTransport.send(adapterInput) && JSON.parse(webKitPosted[0]).kind === "heartbeat", "WKWebView message handler send");
    webKitEnvironment.htmlguiReceive(JSON.stringify(adapterInput), { isMainFrame: true });
    assert(webKitReceived.length === 1 && previousWebKitCalls === 1, "WKWebView host callback and existing handler chaining");
    webKitTransport.stop();
    assert(webKitEnvironment.htmlguiReceive === previousWebKitReceiver, "WKWebView callback restoration");

    var androidEndpoint = new FakeMessageEndpoint();
    var androidEnvironment = { htmlguiAndroid: androidEndpoint };
    var androidReceived = [];
    var androidTransport = htmlgui.detectTransport(androidEnvironment);
    assert(androidTransport && androidTransport.kind === "android", "Android injected bridge feature detection");
    assert(androidTransport.start(function (message) { androidReceived.push(message); }) && androidTransport.send(adapterInput) && JSON.parse(androidEndpoint.posted[0]).session === "adapter", "Android injected bridge send");
    androidEndpoint.emit({ data: JSON.stringify(adapterInput) });
    androidEnvironment.htmlguiAndroidReceive(JSON.stringify(adapterInput), { isMainFrame: true });
    assert(androidReceived.length === 2, "Android message listener and reply proxy receive");
    androidTransport.stop();
    assert(androidEndpoint.listeners.length === 0 && androidEnvironment.htmlguiAndroidReceive === undefined, "Android listener cleanup");

    var portEndpoint = new FakeMessageEndpoint();
    var portEnvironment = { chrome: { webview: new FakeMessageEndpoint() } };
    var portReceived = [];
    var portTransport = htmlgui.detectTransport(portEnvironment, { messagePort: portEndpoint });
    assert(portTransport && portTransport.kind === "message-port", "explicit MessagePort detection precedence");
    assert(portTransport.start(function (message) { portReceived.push(message); }) && portEndpoint.startCount === 1 && portTransport.send(adapterInput), "MessagePort start and send");
    portEndpoint.emit({ data: JSON.stringify(adapterInput) });
    assert(portReceived.length === 1, "MessagePort inbound event");
    portTransport.stop();
    assert(portEndpoint.closeCount === 1 && portEndpoint.listeners.length === 0, "MessagePort close and cleanup");

    var postMessageTarget = new FakeMessageEndpoint();
    postMessageTarget.postMessage = function (message, origin) { this.posted.push({ message: message, origin: origin }); };
    var postMessageSource = new FakeMessageEndpoint();
    postMessageSource.parent = postMessageTarget;
    postMessageSource.location = { origin: "https://app.test" };
    var postMessageReceived = [];
    var postMessageTransport = htmlgui.detectTransport(postMessageSource);
    assert(postMessageTransport && postMessageTransport.kind === "post-message", "window postMessage feature detection");
    assert(postMessageTransport.start(function (message) { postMessageReceived.push(message); }) && postMessageTransport.send(adapterInput) && postMessageTarget.posted[0].origin === "https://app.test", "window postMessage target-origin send");
    postMessageSource.emit({ data: JSON.stringify(adapterInput), source: postMessageTarget, origin: "https://hostile.test" });
    assert(postMessageReceived.length === 0, "window postMessage origin rejection");
    postMessageSource.emit({ data: JSON.stringify(adapterInput), source: postMessageTarget, origin: "https://app.test" });
    assert(postMessageReceived.length === 1, "window postMessage trusted receive");
    postMessageTransport.stop();
    assert(postMessageSource.listeners.length === 0, "window postMessage listener cleanup");

    var loopbackSent = [];
    var loopbackReceived = [];
    var loopbackTransport = htmlgui.detectTransport({}, { loopback: true, onLoopbackSend: function (message) { loopbackSent.push(message); return true; } });
    assert(loopbackTransport.kind === "loopback" && loopbackTransport.start(function (message) { loopbackReceived.push(message); }) && loopbackTransport.send(adapterInput), "loopback feature detection and send");
    loopbackTransport.receive(adapterInput, { isMainFrame: true });
    adapterInput.session = "changed";
    assert(loopbackReceived[0].session === "adapter" && loopbackSent[0].session === "adapter", "loopback detached receive and callback");
    var storedLoopbackMessages = loopbackTransport.getMessages();
    storedLoopbackMessages[0].session = "mutated";
    assert(loopbackTransport.getMessages()[0].session === "adapter", "loopback detached message history");
    assert(htmlgui.detectTransport({}) === null, "missing adapter detection");
    loopbackTransport.stop();

    var automaticRuntime = htmlgui.bootstrap(null, { environment: {}, loopback: true, capabilities: capabilities(false, false) });
    assert(automaticRuntime.getState().state === "starting" && automaticRuntime.stop().success, "bootstrap feature-detected transport");

    var sent = [];
    var delivered = [];
    var receiver = null;
    var stopCount = 0;
    var transport = {
        start: function (callback) {
            receiver = callback;
            return true;
        },
        send: function (message) {
            sent.push(message);
            return true;
        },
        stop: function () {
            stopCount += 1;
            return true;
        }
    };
    var runtime = htmlgui.createRuntime({
        capabilities: capabilities(true, true),
        onMessage: function (message) {
            delivered.push(message);
            if (message.payload && message.payload.kind === "string") {
                message.payload.value = "mutated";
            }
            return true;
        }
    });
    assert(runtime.start(transport).success, "transport start");
    assert(typeof receiver === "function", "transport receiver callback");

    var initialized = receiver(JSON.stringify(envelope("session-1", 0, "initialize", handshake("nonce-1", true, false))), { isMainFrame: true });
    assert(initialized.success, "handshake accepted");
    assert(sent.length === 1 && sent[0].kind === "ready" && sent[0].sequence === 0, "ready response");
    var readyCapabilities = runtime.getState().capabilities;
    assert(readyCapabilities.supportsBinary && !readyCapabilities.supportsChunking, "capability negotiation");
    assert(runtime.getState().nonce === "nonce-1", "nonce handoff");

    var outgoing = runtime.send("log", text("browser-log"), null);
    assert(outgoing.success && sent[1].kind === "log" && sent[1].sequence === 1, "outbound notification");
    assert(runtime.getState().pendingAcknowledgementCount === 1, "pending acknowledgement");
    var acknowledgement = envelope("session-1", 1, "acknowledgement", object({ acknowledgement: integer(1) }));
    assert(runtime.receive(acknowledgement, { isMainFrame: true }).success, "acknowledgement accepted");
    assert(runtime.getState().lastAcknowledgedSequence === 1, "acknowledgement recorded");
    assert(runtime.getState().pendingAcknowledgementCount === 0, "acknowledgement window released");

    var snapshotPayload = object({ revision: integer(4), state: text("saved"), route: text("/products/7"), focus: text("field") });
    assert(runtime.receive(envelope("session-1", 2, "renderSnapshot", snapshotPayload), { isMainFrame: true }).success, "snapshot accepted");
    assert(runtime.getState().revision === 4 && runtime.getState().lastAcknowledgedRevision === 4, "snapshot revision");
    assert(runtime.getState().handoff.value.route.value === "/products/7", "snapshot handoff");
    assert(delivered[0].payload.value.state.value === "saved", "detached handler payload");
    assert(snapshotPayload.value.state.value === "saved", "source payload unchanged");

    var stalePatch = object({ baseRevision: integer(3), revision: integer(5) });
    var resync = runtime.receive(envelope("session-1", 3, "domPatch", stalePatch), { isMainFrame: true });
    assert(resync.success && resync.requiresResync, "revision mismatch requests resync");
    assert(runtime.getState().state === "resyncing", "resync state");
    assert(sent[sent.length - 2].kind === "request" && sent[sent.length - 2].request.operation === "resync", "resync request");
    var fallback = runtime.receive(envelope("session-1", 4, "renderSnapshot", object({ revision: integer(7) })), { isMainFrame: true });
    assert(fallback.success && runtime.getState().state === "ready" && runtime.getState().revision === 7, "full snapshot fallback");
    var deliveredCount = delivered.length;
    var duplicate = runtime.receive(envelope("session-1", 4, "renderSnapshot", object({ revision: integer(7) })), { isMainFrame: true });
    assert(duplicate.success && duplicate.duplicate && delivered.length === deliveredCount, "duplicate suppression");
    var gap = runtime.receive(envelope("session-1", 6, "log", text("gap")), { isMainFrame: true });
    assert(!gap.success && gap.code === "bridge.message-out-of-order", "sequence gap rejection");

    var reconnect = runtime.receive(envelope("session-2", 0, "initialize", handshake("nonce-2", true, true)), { isMainFrame: true });
    assert(reconnect.success && runtime.getState().state === "resyncing", "reconnect handshake");
    assert(sent[sent.length - 2].kind === "ready" && sent[sent.length - 1].request.operation === "resync", "reconnect resync request");
    var staleSession = runtime.receive(envelope("session-1", 5, "log", text("stale")), { isMainFrame: true });
    assert(!staleSession.success && staleSession.code === "bridge.stale-session", "retired session isolation");
    assert(runtime.receive(envelope("session-2", 1, "renderSnapshot", object({ revision: integer(8) })), { isMainFrame: true }).success, "reconnect snapshot");
    assert(runtime.receive(envelope("session-2", 2, "shutdown"), { isMainFrame: true }).success, "orderly shutdown");
    assert(runtime.getState().state === "stopped" && stopCount === 1, "transport stopped");

    var invalidSent = [];
    var invalidRuntime = htmlgui.bootstrap({
        start: function () { return true; },
        send: function (message) { invalidSent.push(message); return true; },
        stop: function () { return true; }
    }, { capabilities: capabilities(false, false), maximumPendingAcknowledgements: 1 });
    var badFrame = invalidRuntime.receive(envelope("session-x", 0, "initialize", handshake("nonce-x", false, false)), { isMainFrame: false });
    assert(!badFrame.success && badFrame.code === "bridge.frame-rejected", "main frame validation");
    var malformed = envelope("session-x", 0, "initialize", handshake("nonce-x", false, false));
    malformed.extra = true;
    assert(invalidRuntime.receive(malformed, { isMainFrame: true }).code === "bridge.unknown-envelope-member", "unknown member validation");
    assert(invalidRuntime.receive(envelope("session-x", 0, "initialize", handshake("nonce-x", false, false)), { isMainFrame: true }).success, "second handshake");
    assert(invalidRuntime.send("log", text("one"), null).success, "bounded first send");
    assert(invalidRuntime.send("log", text("two"), null).code === "bridge.acknowledgement-backpressure", "bounded acknowledgement window");

    var domDocument = new FakeDocument();
    var domSent = [];
    var domTransport = {
        start: function () { return true; },
        send: function (message) { domSent.push(message); return true; },
        stop: function () { return true; }
    };
    var serverRoot = domDocument.createElement("div");
    serverRoot.setAttribute("data-htmlgui-id", "root");
    serverRoot.setAttribute("zeta", "last");
    serverRoot.setAttribute("alpha", "first");
    var serverField = domDocument.createElement("input");
    serverField.setAttribute("data-htmlgui-id", "field");
    serverField.value = "initial";
    var serverLabel = domDocument.createTextNode("Before");
    serverLabel.__htmlguiId = "label";
    var serverScroller = domDocument.createElement("div");
    serverScroller.setAttribute("data-htmlgui-id", "scroller");
    serverScroller.clientHeight = 100;
    serverScroller.clientWidth = 200;
    var serverDialog = domDocument.createElement("dialog");
    serverDialog.setAttribute("data-htmlgui-id", "dialog");
    serverRoot.appendChild(serverField);
    serverRoot.appendChild(serverLabel);
    serverRoot.appendChild(serverScroller);
    serverRoot.appendChild(serverDialog);
    domDocument.body.appendChild(serverRoot);
    var expectedSnapshot = {
        revision: 0,
        lifecycleNotifications: [
            { componentId: "component", stage: 9, generation: 0 },
            { componentId: "component", stage: 10, generation: 0 }
        ],
        roots: [{
            kind: "element",
            id: "root",
            name: "div",
            attributes: [{ name: "alpha", value: "first" }, { name: "zeta", value: "last" }],
            eventSubscriptions: [
                { id: "root-capture", handlerId: "capture-handler", kind: 0, eventName: "click", phase: 0, preventDefault: true },
                { id: "root-bubble", handlerId: "root-handler", kind: 0, eventName: "click", phase: 2 }
            ],
            children: [
                { kind: "element", id: "field", name: "input", eventSubscriptions: [
                    { id: "field-click", handlerId: "field-handler", kind: 0, eventName: "click", phase: 2, stopPropagation: true, once: true },
                    { id: "field-input", handlerId: "input-handler", kind: 22, eventName: "input", phase: 2 },
                    { id: "field-composition-start", handlerId: "composition-handler", kind: 24, eventName: "compositionstart", phase: 2 },
                    { id: "field-composition-end", handlerId: "composition-handler", kind: 26, eventName: "compositionend", phase: 2 }
                ], children: [] },
                { kind: "text", id: "label", text: "Before", children: [] },
                { kind: "element", id: "scroller", name: "div", children: [] },
                { kind: "element", id: "dialog", name: "dialog", children: [] }
            ]
        }]
    };
    var lifecycleCalls = [];
    var domRuntime = htmlgui.createRuntime({
        document: domDocument,
        root: domDocument.body,
        capabilities: capabilities(false, false),
        ResizeObserver: FakeResizeObserver,
        IntersectionObserver: FakeIntersectionObserver,
        onLifecycle: function (notification) { lifecycleCalls.push(notification); return true; }
    });
    assert(domRuntime.start(domTransport).success, "DOM transport start");
    assert(domRuntime.receive(envelope("dom-session", 0, "initialize", handshake("dom-nonce", false, false)), { isMainFrame: true }).success, "DOM handshake");
    assert(domRuntime.receive(envelope("dom-session", 1, "renderSnapshot", portable(expectedSnapshot)), { isMainFrame: true }).success, "tagged snapshot hydration");
    var hydrated = domRuntime.hydrate(expectedSnapshot, "exact");
    assert(hydrated.success && hydrated.matched && domRuntime.getState().revision === 0, "exact hydration");
    assert(lifecycleCalls.length === 4 && lifecycleCalls[0].stage === 9 && lifecycleCalls[1].stage === 10, "hydration lifecycle callbacks");
    var firstSnapshot = domRuntime.createSnapshot();
    assert(firstSnapshot.roots[0].attributes[0].name === "alpha" && firstSnapshot.roots[0].attributes[1].name === "zeta", "deterministic snapshot attributes");
    assert(firstSnapshot.roots[0].children[1].sequence === 2, "deterministic snapshot sequence");
    assert(firstSnapshot.roots[0].eventSubscriptions[0].id === "root-bubble" && firstSnapshot.roots[0].eventSubscriptions[1].id === "root-capture", "deterministic snapshot subscriptions");
    var bridgePatch = {
        baseRevision: 0,
        revision: 1,
        mutations: [
            { kind: "setAttribute", sequence: 0, targetId: "root", name: "data-bridge", value: "applied" },
            { kind: "subscribeEvent", sequence: 1, targetId: "field", name: "field-key", eventSubscription: { id: "field-key", handlerId: "key-handler", kind: 15, eventName: "keydown", phase: 2 } }
        ]
    };
    assert(domRuntime.receive(envelope("dom-session", 2, "domPatch", portable(bridgePatch)), { isMainFrame: true }).success && serverRoot.getAttribute("data-bridge") === "applied", "tagged bridge patch");

    var inboundFocus = domRuntime.receive(envelope("dom-session", 3, "focus", portable({ operation: 0, targetId: "field", reveal: true, selectionStart: 1, selectionEnd: 3 })), { isMainFrame: true });
    assert(inboundFocus.success && domDocument.activeElement === serverField && serverField.scrollIntoViewOptions.block === "nearest", "inbound focus and reveal");
    assert(domRuntime.setSelection({ targetId: "field", start: 2, end: 5, direction: "forward" }).success && domRuntime.scroll({ targetId: "scroller", x: 3, y: 60 }).success && serverField.selectionEnd === 5 && serverScroller.scrollTop === 60, "selection and scroll operations");
    assert(domRuntime.setOverlay({ targetId: "dialog", open: true, initialFocusId: "dialog", restoreFocusId: "field", trapFocus: true, focusTargetIds: ["dialog"] }).success && domRuntime.getState().overlayCount === 1, "overlay open and focus trap registration");
    var trappedTab = { type: "keydown", target: serverDialog, key: "Tab" };
    serverDialog.dispatchEvent(trappedTab);
    assert(trappedTab.defaultPrevented && trappedTab.propagationStopped && domDocument.activeElement === serverDialog, "overlay tab focus trap");
    var escapedOverlay = { type: "keydown", target: serverDialog, key: "Escape" };
    serverDialog.dispatchEvent(escapedOverlay);
    assert(escapedOverlay.defaultPrevented && domRuntime.getState().overlayCount === 0 && domDocument.activeElement === serverField && domSent[domSent.length - 1].payload.value.reason.value === "escape", "overlay escape dismissal and focus restoration");
    domRuntime.setOverlay({ targetId: "dialog", open: true, initialFocusId: "dialog", restoreFocusId: "field" });
    serverScroller.dispatchEvent({ type: "click", target: serverScroller });
    assert(domRuntime.getState().overlayCount === 0 && domSent[domSent.length - 1].payload.value.reason.value === "outside-click", "overlay outside-click dismissal");

    assert(domRuntime.observeResize("scroller").success && domRuntime.observeIntersection("scroller").success && domRuntime.getState().resizeObservationCount === 1 && domRuntime.getState().intersectionObservationCount === 1, "observer registration");
    resizeObservers[0].trigger([{ target: serverScroller, contentRect: { x: 1, y: 2, width: 200, height: 100 } }]);
    var resizeMessage = domSent[domSent.length - 1];
    assert(resizeMessage.kind === "measurement" && resizeMessage.payload.value.kind.value === "resize" && resizeMessage.payload.value.width.value === 200, "resize measurement message");
    intersectionObservers[0].trigger([{ target: serverScroller, isIntersecting: true, intersectionRatio: 0.75, boundingClientRect: { x: 1, y: 2, width: 200, height: 100 } }]);
    var intersectionMessage = domSent[domSent.length - 1];
    assert(intersectionMessage.kind === "measurement" && intersectionMessage.payload.value.isIntersecting.value && intersectionMessage.payload.value.ratio.value === 0.75, "intersection measurement message");
    assert(domRuntime.registerVirtualizer("scroller", { orientation: 1, itemExtent: 20, overscan: 1, totalCount: 10 }).success && domRuntime.requestVirtualRange("scroller").success, "virtualizer registration and request");
    var virtualMessage = domSent[domSent.length - 1];
    assert(virtualMessage.kind === "virtualizationRange" && virtualMessage.payload.value.start.value === 2 && virtualMessage.payload.value.count.value === 7, "deterministic virtual range message");
    serverScroller.scrollTop = 80;
    serverScroller.dispatchEvent({ type: "scroll", target: serverScroller });
    assert(domSent[domSent.length - 1].kind === "virtualizationRange" && domSent[domSent.length - 1].payload.value.start.value === 3, "scroll-driven virtual range message");
    assert(domRuntime.notifyLifecycle({ componentId: "component", stage: 4, generation: 1 }).success && domSent[domSent.length - 1].kind === "lifecycle", "browser lifecycle notification");

    var clickEvent = { type: "click", target: serverField, isTrusted: true, clientX: 12.5, clientY: 7, button: 0, buttons: 1, ctrlKey: true, shiftKey: true };
    serverField.dispatchEvent(clickEvent);
    var firstEventMessage = domSent[domSent.length - 1];
    assert(clickEvent.defaultPrevented && clickEvent.propagationStopped, "subscription event decisions");
    assert(firstEventMessage.kind === "event" && firstEventMessage.payload.value.kind.value === 0 && firstEventMessage.payload.value.pointer.value.clientX.value === 12.5, "semantic pointer payload");
    assert(firstEventMessage.payload.value.subscriptionIds.value.length === 2 && firstEventMessage.payload.value.subscriptionIds.value[1].value === "field-click", "delegated propagation order");
    var repeatedClick = { type: "click", target: serverField };
    serverField.dispatchEvent(repeatedClick);
    var repeatedMessage = domSent[domSent.length - 1];
    assert(!repeatedClick.propagationStopped && repeatedMessage.payload.value.subscriptionIds.value.length === 2 && repeatedMessage.payload.value.subscriptionIds.value[1].value === "root-bubble", "once subscription removal");
    serverField.dispatchEvent({ type: "keydown", target: serverField, key: "Enter", code: "Enter", metaKey: true, isTrusted: true });
    var keyboardMessage = domSent[domSent.length - 1];
    assert(keyboardMessage.payload.value.keyboard.value.key.value === "Enter" && keyboardMessage.payload.value.keyboard.value.primaryModifier.value, "semantic keyboard payload");

    serverField.value = "user typed";
    serverField.selectionStart = 2;
    serverField.selectionEnd = 7;
    serverField.focus();
    serverScroller.scrollLeft = 4;
    serverScroller.scrollTop = 30;
    serverField.dispatchEvent({ type: "input", target: serverField, inputType: "insertText", data: "typed", isComposing: false });
    var inputMessage = domSent[domSent.length - 1];
    assert(inputMessage.kind === "event" && inputMessage.payload.value.input.value.value.value === "user typed" && inputMessage.payload.value.input.value.selectionStart.value === 2, "controlled input synchronization payload");
    assert(domRuntime.getState().controlledInputCount === 1 && domRuntime.getState().subscriptionCount === 6, "runtime event state");
    var patch = {
        baseRevision: 1,
        revision: 2,
        lifecycleNotifications: [
            { componentId: "component", stage: 3, generation: 2 },
            { componentId: "component", stage: 4, generation: 2 }
        ],
        preservation: {
            preserveFocus: true,
            preserveSelection: true,
            preserveScroll: true,
            preserveUserEditedFields: true
        },
        mutations: [
            { kind: "setAttribute", targetId: "root", name: "data-state", value: "updated" },
            { kind: "setText", targetId: "label", value: "After" },
            { kind: "setProperty", targetId: "field", name: "value", value: "server value" },
            { kind: "setProperty", targetId: "scroller", name: "scrollTop", value: 0 },
            { kind: "insert", parentId: "root", index: 2, node: { kind: "element", id: "extra", name: "span", children: [{ kind: "text", id: "extra-text", text: "Extra", children: [] }] } },
            { kind: "unsubscribeEvent", targetId: "field", name: "field-key" }
        ]
    };
    var patched = domRuntime.applyPatch(patch);
    assert(patched.success && domRuntime.getState().revision === 2, "transactional patch " + JSON.stringify(patched));
    assert(serverRoot.getAttribute("data-state") === "updated" && serverLabel.nodeValue === "After", "attribute and text mutations");
    assert(serverRoot.childNodes[2].localName === "span" && serverRoot.childNodes[2].childNodes[0].nodeValue === "Extra", "insert mutation");
    assert(serverField.value === "user typed" && domDocument.activeElement === serverField, "field and focus preservation");
    assert(serverField.selectionStart === 2 && serverField.selectionEnd === 7 && serverScroller.scrollTop === 30, "selection and scroll preservation");
    assert(domRuntime.getState().subscriptionCount === 5 && (domDocument.body._listeners.keydown || []).length === 0, "patched subscription removal");
    assert(lifecycleCalls[lifecycleCalls.length - 2].stage === 3 && lifecycleCalls[lifecycleCalls.length - 1].stage === 4, "patch lifecycle callbacks");

    serverField.dispatchEvent({ type: "compositionstart", target: serverField, data: "draft", isComposing: true });
    var imePatch = domRuntime.applyPatch({
        baseRevision: 2,
        revision: 3,
        mutations: [{ kind: "setProperty", targetId: "field", name: "value", value: "host after composition" }]
    });
    assert(imePatch.success && serverField.value === "user typed" && domRuntime.getState().compositionCount === 1, "controlled value deferred during composition");
    serverField.dispatchEvent({ type: "compositionend", target: serverField, data: "typed", isComposing: false });
    assert(serverField.value === "host after composition" && domRuntime.getState().compositionCount === 0, "deferred value applied after composition");

    var rollback = domRuntime.applyPatch({
        baseRevision: 3,
        revision: 4,
        mutations: [
            { kind: "setAttribute", targetId: "root", name: "rolled-back", value: "yes" },
            { kind: "unsubscribeEvent", targetId: "field", name: "field-input" },
            { kind: "setText", targetId: "missing", value: "failure" }
        ]
    });
    var rollbackSnapshot = domRuntime.createSnapshot();
    assert(!rollback.success && rollback.requiresResync && rollback.resyncRequested, "rollback requests resync");
    assert(domRuntime.getState().revision === 3 && domRuntime.getState().state === "resyncing" && domRuntime.getState().subscriptionCount === 5 && domRuntime.getState().virtualizerCount === 1, "rollback revision, subscription, and observer isolation");
    assert(rollbackSnapshot.roots[0].attributes.every(function (item) { return item.name !== "rolled-back"; }), "rollback restores DOM");
    assert(domSent[domSent.length - 1].kind === "request" && domSent[domSent.length - 1].request.operation === "resync", "rollback resync message");

    var afterRollback = domRuntime.applyPatch({
        baseRevision: 3,
        revision: 4,
        mutations: [{ kind: 4, sequence: 0, targetId: "label", value: "After rollback" }]
    });
    assert(afterRollback.success && domRuntime.createSnapshot().roots[0].children[1].text === "After rollback", "rollback restores text identity");

    var replacement = {
        revision: 5,
        roots: [{ kind: 0, id: "replacement", name: "main", children: [{ kind: 1, id: "replacement-text", text: "Recovered", children: [] }] }]
    };
    var replaced = domRuntime.hydrate(replacement, 2);
    assert(replaced.success && domRuntime.getState().state === "ready" && domRuntime.getState().revision === 5, "full replacement recovery");
    assert(domDocument.body.childNodes[0].localName === "main" && domDocument.body.childNodes[0].childNodes[0].nodeValue === "Recovered", "replacement DOM");
    assert(domRuntime.getState().resizeObservationCount === 0 && domRuntime.getState().intersectionObservationCount === 0 && domRuntime.getState().virtualizerCount === 0, "replacement observer cleanup");
    var beforeStaleEvent = domSent.length;
    var staleEvent = domRuntime.dispatchEvent({ type: "input", target: serverField });
    assert(!staleEvent.success && staleEvent.code === "bridge.event-stale-node" && domSent.length === beforeStaleEvent, "stale node event suppression");

    var mismatchDocument = new FakeDocument();
    var mismatchRoot = mismatchDocument.createElement("p");
    mismatchRoot.setAttribute("data-htmlgui-id", "mismatch-root");
    var mismatchText = mismatchDocument.createTextNode("Client");
    mismatchText.__htmlguiId = "mismatch-text";
    mismatchRoot.appendChild(mismatchText);
    mismatchDocument.body.appendChild(mismatchRoot);
    var mismatchRuntime = htmlgui.createRuntime({ document: mismatchDocument, root: mismatchDocument.body });
    var serverSnapshot = {
        revision: 9,
        roots: [{ kind: "element", id: "mismatch-root", name: "p", children: [{ kind: "text", id: "mismatch-text", text: "Server", children: [] }] }]
    };
    var exactMismatch = mismatchRuntime.hydrate(serverSnapshot, "exact");
    assert(!exactMismatch.success && mismatchText.nodeValue === "Client", "exact hydration mismatch is non-mutating");
    var repairedMismatch = mismatchRuntime.hydrate(serverSnapshot, "repair");
    assert(repairedMismatch.success && repairedMismatch.applied && mismatchDocument.body.childNodes[0].childNodes[0].nodeValue === "Server", "repair hydration replaces mismatch");

    var mutationDocument = new FakeDocument();
    var mutationSent = [];
    var mutationRuntime = htmlgui.createRuntime({ document: mutationDocument, root: mutationDocument.body, capabilities: capabilities(false, false) });
    assert(mutationRuntime.start({
        start: function () { return true; },
        send: function (message) { mutationSent.push(message); return true; },
        stop: function () { return true; }
    }).success && mutationRuntime.receive(envelope("mutation-session", 0, "initialize", handshake("mutation-nonce", false, false)), { isMainFrame: true }).success, "mutation runtime bootstrap");
    var mutationSnapshot = {
        revision: 0,
        roots: [{
            kind: "element",
            id: "mutation-root",
            name: "section",
            children: [
                { kind: "text", id: "mutation-text", text: "Before", children: [] },
                { kind: "element", id: "mutation-first", name: "span", attributes: { "class": "old-class", "data-remove": "yes" }, properties: { oldProperty: "remove" }, children: [] },
                { kind: "element", id: "mutation-second", name: "span", children: [] },
                { kind: "element", id: "mutation-container", name: "div", children: [] },
                { kind: "element", id: "mutation-replace", name: "aside", children: [] },
                { kind: "element", id: "mutation-remove", name: "small", children: [] },
                { kind: "element", id: "mutation-ime", name: "input", properties: { value: "initial" }, eventSubscriptions: [
                    { id: "mutation-composition-start", handlerId: "composition", kind: 24, eventName: "compositionstart", phase: 2 },
                    { id: "mutation-composition-end", handlerId: "composition", kind: 26, eventName: "compositionend", phase: 2 }
                ], children: [] }
            ]
        }]
    };
    assert(mutationRuntime.hydrate(mutationSnapshot, "replace").success, "mutation fixture hydration");
    var mutationRoot = mutationDocument.body.childNodes[0];
    var mutationFirst = mutationRoot.childNodes[1];
    mutationFirst.style.setProperty("old-style", "remove");
    var everyMutationPatch = mutationRuntime.applyPatch({
        baseRevision: 0,
        revision: 1,
        mutations: [
            { kind: "setText", sequence: 0, targetId: "mutation-text", value: "After" },
            { kind: "setAttribute", sequence: 1, targetId: "mutation-first", name: "title", value: "added" },
            { kind: "removeAttribute", sequence: 2, targetId: "mutation-first", name: "data-remove" },
            { kind: "setProperty", sequence: 3, targetId: "mutation-first", name: "newProperty", value: "set" },
            { kind: "removeProperty", sequence: 4, targetId: "mutation-first", name: "oldProperty" },
            { kind: "addClass", sequence: 5, targetId: "mutation-first", name: "active" },
            { kind: "removeClass", sequence: 6, targetId: "mutation-first", name: "old-class" },
            { kind: "setStyle", sequence: 7, targetId: "mutation-first", name: "color", value: "red" },
            { kind: "removeStyle", sequence: 8, targetId: "mutation-first", name: "old-style" },
            { kind: "subscribeEvent", sequence: 9, targetId: "mutation-first", eventSubscription: { id: "mutation-click", handlerId: "mutation-handler", kind: 0, eventName: "click", phase: 2 } },
            { kind: "insert", sequence: 10, parentId: "mutation-root", referenceId: "mutation-container", node: { kind: "element", id: "mutation-inserted", name: "strong", children: [] } },
            { kind: "move", sequence: 11, targetId: "mutation-second", parentId: "mutation-container", index: 0 },
            { kind: "replace", sequence: 12, targetId: "mutation-replace", node: { kind: "element", id: "mutation-replacement", name: "nav", children: [] } },
            { kind: "remove", sequence: 13, targetId: "mutation-remove" }
        ]
    });
    assert(everyMutationPatch.success && mutationRuntime.getState().revision === 1, "all mutation kinds apply transactionally");
    assert(mutationRoot.childNodes[0].nodeValue === "After" && mutationFirst.getAttribute("title") === "added" && !mutationFirst.hasAttribute("data-remove") && mutationFirst.newProperty === "set" && mutationFirst.oldProperty === undefined, "text attribute and property mutation matrix");
    assert(mutationFirst.getAttribute("class") === "active" && mutationFirst.style.values.color === "red" && mutationFirst.style.values["old-style"] === undefined, "class and style mutation matrix");
    assert(mutationRoot.childNodes[2].localName === "strong" && mutationRoot.childNodes[3].childNodes[0].__htmlguiId === "mutation-second" && mutationRoot.childNodes[4].localName === "nav" && mutationRoot.childNodes.every(function (node) { return node.__htmlguiId !== "mutation-remove"; }), "insert move replace and remove mutation matrix");
    mutationFirst.dispatchEvent({ type: "click", target: mutationFirst });
    assert(mutationSent[mutationSent.length - 1].kind === "event" && mutationSent[mutationSent.length - 1].payload.value.subscriptionIds.value[0].value === "mutation-click", "subscribed mutation event delivery");
    var unsubscribePatch = mutationRuntime.applyPatch({ baseRevision: 1, revision: 2, mutations: [{ kind: "unsubscribeEvent", sequence: 0, targetId: "mutation-first", name: "mutation-click" }] });
    assert(unsubscribePatch.success && mutationRuntime.getState().subscriptionCount === 2, "unsubscribe mutation cleanup");
    var mutationMessageCount = mutationSent.length;
    mutationFirst.dispatchEvent({ type: "click", target: mutationFirst });
    assert(mutationSent.length === mutationMessageCount, "unsubscribed mutation event suppression");

    var oldIme = mutationRoot.childNodes[5];
    oldIme.value = "draft";
    oldIme.selectionStart = 1;
    oldIme.selectionEnd = 4;
    oldIme.focus();
    oldIme.dispatchEvent({ type: "compositionstart", target: oldIme, data: "dra", isComposing: true });
    assert(mutationRuntime.getState().compositionCount === 1, "IME composition starts before replacement");
    var imeReplacement = mutationRuntime.applyPatch({
        baseRevision: 2,
        revision: 3,
        preservation: { preserveFocus: true, preserveSelection: true, preserveComposition: true, preserveUserEditedFields: true },
        mutations: [{ kind: "replace", sequence: 0, targetId: "mutation-ime", node: {
            kind: "element",
            id: "mutation-ime",
            name: "input",
            properties: { value: "host" },
            eventSubscriptions: [
                { id: "mutation-composition-start", handlerId: "composition", kind: 24, eventName: "compositionstart", phase: 2 },
                { id: "mutation-composition-end", handlerId: "composition", kind: 26, eventName: "compositionend", phase: 2 }
            ],
            children: []
        } }]
    });
    var newIme = mutationRoot.childNodes[5];
    assert(imeReplacement.success && newIme !== oldIme && mutationDocument.activeElement === newIme && newIme.value === "draft" && newIme.selectionStart === 1 && newIme.selectionEnd === 4 && mutationRuntime.getState().compositionCount === 1, "focus selection value and IME preservation across replacement");
    var deferredImePatch = mutationRuntime.applyPatch({ baseRevision: 3, revision: 4, mutations: [{ kind: "setProperty", sequence: 0, targetId: "mutation-ime", name: "value", value: "committed" }] });
    assert(deferredImePatch.success && newIme.value === "draft", "preserved IME defers controlled value");
    newIme.dispatchEvent({ type: "compositionend", target: newIme, data: "draft", isComposing: false });
    assert(newIme.value === "committed" && mutationRuntime.getState().compositionCount === 0, "preserved IME commits deferred value");

    return JSON.stringify({ success: true, checks: 113, sent: sent.length, delivered: delivered.length, domMessages: domSent.length });
}
