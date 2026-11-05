function run() {
    var checks = 0;

    function assert(condition, message) {
        checks += 1;
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

    function capabilities() {
        return {
            state: "starting",
            version: 1,
            capabilities: [],
            supportsBinary: false,
            supportsChunking: false
        };
    }

    function handshake(nonce) {
        return object({
            nonce: text(nonce),
            version: integer(1),
            capabilities: array([]),
            supportsBinary: booleanValue(false),
            supportsChunking: booleanValue(false)
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

    function FakeEndpoint() {
        this.listeners = [];
        this.posted = [];
        this.started = 0;
        this.closed = 0;
        this.disconnected = false;
    }

    FakeEndpoint.prototype.addEventListener = function (name, listener) {
        if (name === "message") {
            this.listeners.push(listener);
        }
    };

    FakeEndpoint.prototype.removeEventListener = function (name, listener) {
        var index = this.listeners.indexOf(listener);
        if (name === "message" && index >= 0) {
            this.listeners.splice(index, 1);
        }
    };

    FakeEndpoint.prototype.postMessage = function (message) {
        if (this.disconnected) {
            throw new Error("disconnected");
        }
        this.posted.push(message);
    };

    FakeEndpoint.prototype.start = function () {
        this.started += 1;
    };

    FakeEndpoint.prototype.close = function () {
        this.closed += 1;
    };

    FakeEndpoint.prototype.emit = function (event) {
        var listeners = this.listeners.slice();
        var result = false;
        var index = 0;
        while (index < listeners.length) {
            result = listeners[index](event);
            index += 1;
        }
        return result;
    };

    function exerciseAdapter(kind, transport, deliver, posted, deliverAfterStop) {
        var runtime = htmlgui.createRuntime({ capabilities: capabilities() });
        assert(transport && transport.kind === kind, kind + " detection");
        assert(runtime.start(transport).success, kind + " start");
        var initialized = deliver(JSON.stringify(envelope(kind + "-session", 0, "initialize", handshake(kind + "-nonce"))), { isMainFrame: true });
        assert(initialized && initialized.success && runtime.getState().state === "ready", kind + " handshake");
        var ready = JSON.parse(posted[0]);
        assert(ready.kind === "ready" && ready.session === kind + "-session" && ready.origin === "browser", kind + " ready response");
        var logged = deliver(JSON.stringify(envelope(kind + "-session", 1, "log", text("message"))), { isMainFrame: true });
        var acknowledgement = JSON.parse(posted[posted.length - 1]);
        assert(logged && logged.success && acknowledgement.kind === "acknowledgement" && acknowledgement.payload.value.acknowledgement.value === 1, kind + " inbound acknowledgement");
        assert(runtime.stop().success && runtime.getState().state === "stopped" && deliverAfterStop() === false, kind + " deterministic stop");
    }

    var webViewEndpoint = new FakeEndpoint();
    var webViewEnvironment = { chrome: { webview: webViewEndpoint } };
    exerciseAdapter(
        "webview2",
        htmlgui.detectTransport(webViewEnvironment),
        function (message, metadata) { return webViewEndpoint.emit({ data: message, isMainFrame: metadata.isMainFrame }); },
        webViewEndpoint.posted,
        function () { return webViewEndpoint.emit({ data: "ignored", isMainFrame: true }); });
    assert(webViewEndpoint.listeners.length === 0, "WebView2 listener detached");

    var webKitPosted = [];
    var webKitEnvironment = {
        webkit: { messageHandlers: { htmlgui: { postMessage: function (message) { webKitPosted.push(message); } } } }
    };
    exerciseAdapter(
        "webkit",
        htmlgui.detectTransport(webKitEnvironment),
        function (message, metadata) { return webKitEnvironment.htmlguiReceive(message, metadata); },
        webKitPosted,
        function () { return typeof webKitEnvironment.htmlguiReceive === "function" ? webKitEnvironment.htmlguiReceive("ignored", { isMainFrame: true }) : false; });
    assert(webKitEnvironment.htmlguiReceive === undefined, "WKWebView callback detached");

    var androidEndpoint = new FakeEndpoint();
    var androidEnvironment = { htmlguiAndroid: androidEndpoint };
    exerciseAdapter(
        "android",
        htmlgui.detectTransport(androidEnvironment),
        function (message, metadata) { return androidEnvironment.htmlguiAndroidReceive(message, metadata); },
        androidEndpoint.posted,
        function () { return typeof androidEnvironment.htmlguiAndroidReceive === "function" ? androidEnvironment.htmlguiAndroidReceive("ignored", { isMainFrame: true }) : false; });
    assert(androidEndpoint.listeners.length === 0 && androidEnvironment.htmlguiAndroidReceive === undefined, "Android listener and reply callback detached");

    var messagePort = new FakeEndpoint();
    exerciseAdapter(
        "message-port",
        htmlgui.detectTransport({}, { messagePort: messagePort }),
        function (message, metadata) { return messagePort.emit({ data: message, isMainFrame: metadata.isMainFrame }); },
        messagePort.posted,
        function () { return messagePort.emit({ data: "ignored", isMainFrame: true }); });
    assert(messagePort.started === 1 && messagePort.closed === 1 && messagePort.listeners.length === 0, "MessagePort lifecycle contract");

    var delayedTransport = htmlgui.createLoopbackTransport();
    var delayedRuntime = htmlgui.createRuntime({ capabilities: capabilities() });
    assert(delayedRuntime.start(delayedTransport).success && delayedTransport.receive(envelope("delayed", 0, "initialize", handshake("delayed-nonce")), { isMainFrame: true }).success, "delayed transport handshake");
    var delayedSecond = envelope("delayed", 2, "log", text("second"));
    var delayedFirst = envelope("delayed", 1, "log", text("first"));
    var outOfOrder = delayedTransport.receive(delayedSecond, { isMainFrame: true });
    assert(!outOfOrder.success && outOfOrder.code === "bridge.message-out-of-order" && delayedRuntime.getState().lastInboundSequence === 0, "out-of-order delivery rejected without advancement");
    assert(delayedTransport.receive(delayedFirst, { isMainFrame: true }).success, "delayed predecessor accepted");
    assert(delayedTransport.receive(delayedSecond, { isMainFrame: true }).success && delayedRuntime.getState().lastInboundSequence === 2, "out-of-order message succeeds when retried in order");
    assert(delayedTransport.getMessages().filter(function (message) { return message.kind === "acknowledgement"; }).length === 2, "ordered retries acknowledged exactly once");
    delayedRuntime.stop();

    var disconnectedEndpoint = new FakeEndpoint();
    var disconnectedRuntime = htmlgui.createRuntime({ capabilities: capabilities() });
    assert(disconnectedRuntime.start(htmlgui.createWebView2Transport({ chrome: { webview: disconnectedEndpoint } })).success && disconnectedEndpoint.emit({ data: JSON.stringify(envelope("disconnect", 0, "initialize", handshake("disconnect-nonce"))), isMainFrame: true }).success, "disconnect fixture handshake");
    disconnectedEndpoint.disconnected = true;
    var failedSend = disconnectedRuntime.send("log", text("cannot-send"), null);
    assert(!failedSend.success && failedSend.code === "bridge.transport-send-failed" && disconnectedRuntime.getState().state === "faulted", "disconnect faults failed sends");
    assert(disconnectedRuntime.stop().success && disconnectedEndpoint.listeners.length === 0, "disconnect cleanup remains deterministic");

    var trustedHost = new FakeEndpoint();
    trustedHost.postMessage = function (message, origin) { this.posted.push({ message: message, origin: origin }); };
    var page = new FakeEndpoint();
    page.parent = trustedHost;
    page.top = page;
    page.location = { origin: "https://trusted.test" };
    var hostileRuntime = htmlgui.createRuntime({ capabilities: capabilities() });
    assert(hostileRuntime.start(htmlgui.createPostMessageTransport(page, { targetOrigin: "https://trusted.test" })).success, "hostile page fixture start");
    var initializeJson = JSON.stringify(envelope("trusted", 0, "initialize", handshake("trusted-nonce")));
    assert(page.emit({ data: initializeJson, source: new FakeEndpoint(), origin: "https://trusted.test", isMainFrame: true }) === false && hostileRuntime.getState().state === "starting", "foreign postMessage source rejected");
    assert(page.emit({ data: initializeJson, source: trustedHost, origin: "https://hostile.test", isMainFrame: true }) === false && hostileRuntime.getState().state === "starting", "foreign postMessage origin rejected");
    assert(page.emit({ data: initializeJson, source: trustedHost, origin: "https://trusted.test", isMainFrame: true }).success, "trusted postMessage initialization accepted");
    var forgedOrigin = envelope("trusted", 1, "log", text("forged"));
    forgedOrigin.origin = "browser";
    var forgedResult = page.emit({ data: JSON.stringify(forgedOrigin), source: trustedHost, origin: "https://trusted.test", isMainFrame: true });
    assert(!forgedResult.success && forgedResult.code === "bridge.origin-mismatch" && hostileRuntime.getState().lastInboundSequence === 0, "forged envelope origin rejected");
    var subframeResult = page.emit({ data: JSON.stringify(envelope("trusted", 1, "log", text("frame"))), source: trustedHost, origin: "https://trusted.test", isMainFrame: false });
    assert(!subframeResult.success && subframeResult.code === "bridge.frame-rejected" && hostileRuntime.getState().lastInboundSequence === 0, "subframe message rejected");
    assert(page.emit({ data: JSON.stringify(envelope("trusted", 1, "log", text("trusted"))), source: trustedHost, origin: "https://trusted.test", isMainFrame: true }).success, "trusted main-frame message accepted after hostile attempts");
    hostileRuntime.stop();

    return JSON.stringify({ success: true, checks: checks, adapters: 4 });
}
