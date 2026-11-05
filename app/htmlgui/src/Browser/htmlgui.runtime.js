"use strict";
(function (root) {
    var VERSION = 1;
    var MINIMUM_VERSION = 1;
    var HOST_ORIGIN = "host";
    var BROWSER_ORIGIN = "browser";
    var kinds = {
        initialize: true,
        ready: true,
        renderSnapshot: true,
        domPatch: true,
        event: true,
        command: true,
        stateMutation: true,
        validation: true,
        navigation: true,
        focus: true,
        measurement: true,
        virtualizationRange: true,
        virtualizationResult: true,
        lifecycle: true,
        log: true,
        diagnostic: true,
        hostCall: true,
        hostResult: true,
        request: true,
        response: true,
        error: true,
        cancel: true,
        acknowledgement: true,
        heartbeat: true,
        shutdown: true,
        chunk: true
    };
    var notificationKinds = {
        renderSnapshot: true,
        domPatch: true,
        event: true,
        command: true,
        stateMutation: true,
        validation: true,
        navigation: true,
        focus: true,
        measurement: true,
        virtualizationRange: true,
        virtualizationResult: true,
        lifecycle: true,
        log: true,
        diagnostic: true
    };
    var domNodeKinds = ["element", "text", "comment", "fragment", "raw"];
    var mutationKinds = ["insert", "remove", "move", "replace", "setText", "setAttribute", "removeAttribute", "setProperty", "removeProperty", "addClass", "removeClass", "setStyle", "removeStyle", "subscribeEvent", "unsubscribeEvent"];
    var hydrationModes = ["exact", "repair", "replace"];
    var eventNames = ["click", "dblclick", "contextmenu", "pointerdown", "pointerup", "pointermove", "pointerenter", "pointerleave", "pointercancel", "mousedown", "mouseup", "mousemove", "mouseenter", "mouseleave", "wheel", "keydown", "keyup", "focus", "blur", "focusin", "focusout", "beforeinput", "input", "change", "compositionstart", "compositionupdate", "compositionend", "selectionchange", "submit", "reset", "dragstart", "drag", "dragenter", "dragover", "dragleave", "drop", "dragend", "copy", "cut", "paste", "scroll", "resize", "visibilitychange", "toggle"];
    var eventPhases = ["capture", "target", "bubble"];

    function own(value, name) {
        return Object.prototype.hasOwnProperty.call(value, name);
    }

    function isObject(value) {
        return value !== null && typeof value === "object" && !Array.isArray(value);
    }

    function isInteger(value) {
        return typeof value === "number" && isFinite(value) && Math.floor(value) === value;
    }

    function isBoundedText(value, maximum, optional) {
        if (optional && value === null) {
            return true;
        }
        return typeof value === "string" && value.length > 0 && value.length <= maximum;
    }

    function utf8Length(value) {
        return unescape(encodeURIComponent(value)).length;
    }

    function clone(value) {
        if (value === undefined) {
            return undefined;
        }
        return JSON.parse(JSON.stringify(value));
    }

    function enumToken(value, tokens, fallback) {
        if (isInteger(value) && value >= 0 && value < tokens.length) {
            return tokens[value];
        }
        if (typeof value === "string") {
            return value;
        }
        return fallback;
    }

    function result(success, applied, code) {
        return {
            success: success,
            applied: applied,
            duplicate: false,
            requiresResync: false,
            code: code || ""
        };
    }

    function failure(code) {
        return result(false, false, code);
    }

    function taggedNull() {
        return { kind: "null" };
    }

    function taggedBoolean(value) {
        return { kind: "boolean", value: value === true };
    }

    function taggedInteger(value) {
        return { kind: "integer", value: value };
    }

    function taggedFloatingPoint(value) {
        return { kind: "floatingPoint", value: value };
    }

    function taggedString(value) {
        return { kind: "string", value: value };
    }

    function taggedArray(value) {
        return { kind: "array", value: value };
    }

    function taggedObject(value) {
        return { kind: "object", value: value };
    }

    function encodeTaggedValue(value) {
        if (value === null || value === undefined) {
            return taggedNull();
        }
        if (typeof value === "boolean") {
            return taggedBoolean(value);
        }
        if (typeof value === "number") {
            return isInteger(value) ? taggedInteger(value) : taggedFloatingPoint(value);
        }
        if (typeof value === "string") {
            return taggedString(value);
        }
        if (Array.isArray(value)) {
            var items = [];
            var itemIndex = 0;
            while (itemIndex < value.length) {
                items.push(encodeTaggedValue(value[itemIndex]));
                itemIndex += 1;
            }
            return taggedArray(items);
        }
        var encoded = {};
        var names = Object.keys(value).sort();
        var nameIndex = 0;
        while (nameIndex < names.length) {
            encoded[names[nameIndex]] = encodeTaggedValue(value[names[nameIndex]]);
            nameIndex += 1;
        }
        return taggedObject(encoded);
    }

    function sortCapabilities(capabilities) {
        capabilities.sort(function (left, right) {
            if (left.name < right.name) {
                return -1;
            }
            if (left.name > right.name) {
                return 1;
            }
            return 0;
        });
        return capabilities;
    }

    function detectCapabilities(environment) {
        var source = environment || root;
        var binary = typeof source.Uint8Array === "function";
        var capabilities = [
            { name: "binary", supported: binary, version: "1" },
            { name: "chunking", supported: binary, version: "1" },
            { name: "message-channel", supported: typeof source.MessageChannel === "function", version: "1" },
            { name: "promise", supported: typeof source.Promise === "function", version: "1" }
        ];
        return {
            state: "starting",
            version: VERSION,
            capabilities: sortCapabilities(capabilities),
            supportsBinary: binary,
            supportsChunking: binary
        };
    }

    function normalizeOptions(options) {
        var source = options || {};
        return {
            version: isInteger(source.version) ? source.version : VERSION,
            expectedOrigin: source.expectedOrigin || HOST_ORIGIN,
            localOrigin: source.localOrigin || BROWSER_ORIGIN,
            requireMainFrame: source.requireMainFrame !== false,
            maximumMessageLength: isInteger(source.maximumMessageLength) && source.maximumMessageLength > 0 ? source.maximumMessageLength : 1048576,
            maximumDepth: isInteger(source.maximumDepth) && source.maximumDepth > 0 ? source.maximumDepth : 64,
            maximumCollectionCount: isInteger(source.maximumCollectionCount) && source.maximumCollectionCount > 0 ? source.maximumCollectionCount : 10000,
            maximumStringLength: isInteger(source.maximumStringLength) && source.maximumStringLength > 0 ? source.maximumStringLength : 1048576,
            maximumPendingAcknowledgements: isInteger(source.maximumPendingAcknowledgements) && source.maximumPendingAcknowledgements > 0 ? source.maximumPendingAcknowledgements : 128,
            maximumRetiredSessions: isInteger(source.maximumRetiredSessions) && source.maximumRetiredSessions > 0 ? source.maximumRetiredSessions : 32,
            onMessage: typeof source.onMessage === "function" ? source.onMessage : null,
            capabilities: source.capabilities || null,
            document: source.document || root.document || null,
            root: source.root || null,
            hydrationMode: source.hydrationMode || "repair",
            ResizeObserver: source.ResizeObserver || root.ResizeObserver || null,
            IntersectionObserver: source.IntersectionObserver || root.IntersectionObserver || null,
            onLifecycle: typeof source.onLifecycle === "function" ? source.onLifecycle : null
        };
    }

    function adapterMessage(input) {
        if (isObject(input) && own(input, "data")) {
            return input.data;
        }
        return input;
    }

    function adapterMetadata(input, metadata, environment) {
        var supplied = isObject(metadata) ? clone(metadata) : {};
        if (!own(supplied, "isMainFrame")) {
            if (isObject(input) && typeof input.isMainFrame === "boolean") {
                supplied.isMainFrame = input.isMainFrame;
            } else {
                supplied.isMainFrame = !environment || !environment.top || environment.top === environment;
            }
        }
        if (isObject(input) && typeof input.origin === "string" && !own(supplied, "origin")) {
            supplied.origin = input.origin;
        }
        return supplied;
    }

    function createAdapterTransport(kind, endpoint, sender, environment, settings) {
        var configuration = settings || {};
        var receiver = null;
        var listener = null;
        var previousOnMessage = null;
        var previousGlobalReceiver = null;
        var hadGlobalReceiver = false;
        var started = false;

        function deliver(input, metadata) {
            if (!receiver) {
                return false;
            }
            if (configuration.accept && !configuration.accept(input)) {
                return false;
            }
            return receiver(adapterMessage(input), adapterMetadata(input, metadata, environment));
        }

        function start(callback) {
            if (started || typeof callback !== "function") {
                return false;
            }
            receiver = callback;
            listener = function (event) {
                return deliver(event, null);
            };
            if (endpoint && typeof endpoint.addEventListener === "function") {
                endpoint.addEventListener("message", listener);
            } else if (endpoint) {
                previousOnMessage = endpoint.onmessage;
                endpoint.onmessage = function (event) {
                    if (typeof previousOnMessage === "function") {
                        previousOnMessage.call(endpoint, event);
                    }
                    return listener(event);
                };
            }
            if (configuration.receiverRoot && configuration.receiverName) {
                hadGlobalReceiver = own(configuration.receiverRoot, configuration.receiverName);
                previousGlobalReceiver = configuration.receiverRoot[configuration.receiverName];
                configuration.receiverRoot[configuration.receiverName] = function (message, metadata) {
                    if (typeof previousGlobalReceiver === "function") {
                        previousGlobalReceiver.call(configuration.receiverRoot, message, metadata);
                    }
                    return deliver(message, metadata);
                };
            }
            if (configuration.startEndpoint && endpoint && typeof endpoint.start === "function") {
                endpoint.start();
            }
            started = true;
            return true;
        }

        function send(message) {
            if (!started) {
                return false;
            }
            try {
                var response = sender(JSON.stringify(message));
                return response !== false && (!isObject(response) || response.success !== false);
            } catch (error) {
                return false;
            }
        }

        function stop() {
            if (!started) {
                return true;
            }
            if (endpoint && typeof endpoint.removeEventListener === "function" && listener) {
                endpoint.removeEventListener("message", listener);
            } else if (endpoint) {
                endpoint.onmessage = previousOnMessage;
            }
            if (configuration.receiverRoot && configuration.receiverName) {
                if (hadGlobalReceiver) {
                    configuration.receiverRoot[configuration.receiverName] = previousGlobalReceiver;
                } else {
                    delete configuration.receiverRoot[configuration.receiverName];
                }
            }
            if (configuration.closeEndpoint && endpoint && typeof endpoint.close === "function") {
                endpoint.close();
            }
            receiver = null;
            listener = null;
            previousOnMessage = null;
            previousGlobalReceiver = null;
            hadGlobalReceiver = false;
            started = false;
            return true;
        }

        return {
            kind: kind,
            start: start,
            send: send,
            stop: stop
        };
    }

    function createWebView2Transport(environment) {
        var source = environment || root;
        var endpoint = source.chrome && source.chrome.webview;
        if (!endpoint || typeof endpoint.postMessage !== "function") {
            return null;
        }
        return createAdapterTransport("webview2", endpoint, function (message) {
            return endpoint.postMessage(message);
        }, source, null);
    }

    function createWebKitTransport(environment, settings) {
        var source = environment || root;
        var configuration = settings || {};
        var handlerName = configuration.webKitHandlerName || "htmlgui";
        var receiverName = configuration.webKitReceiverName || "htmlguiReceive";
        var handlers = source.webkit && source.webkit.messageHandlers;
        var endpoint = handlers && handlers[handlerName];
        if (!endpoint || typeof endpoint.postMessage !== "function") {
            return null;
        }
        return createAdapterTransport("webkit", null, function (message) {
            return endpoint.postMessage(message);
        }, source, { receiverRoot: source, receiverName: receiverName });
    }

    function resolveAndroidEndpoint(environment, settings) {
        var configuration = settings || {};
        if (configuration.androidBridge && typeof configuration.androidBridge.postMessage === "function") {
            return configuration.androidBridge;
        }
        var source = environment || root;
        var configuredName = configuration.androidBridgeName;
        if (configuredName && source[configuredName] && typeof source[configuredName].postMessage === "function") {
            return source[configuredName];
        }
        var names = ["htmlguiAndroid", "HtmlGuiAndroid", "htmlguiBridge"];
        var index = 0;
        while (index < names.length) {
            if (source[names[index]] && typeof source[names[index]].postMessage === "function") {
                return source[names[index]];
            }
            index += 1;
        }
        return null;
    }

    function createAndroidTransport(environment, settings) {
        var source = environment || root;
        var configuration = settings || {};
        var endpoint = resolveAndroidEndpoint(source, configuration);
        if (!endpoint) {
            return null;
        }
        return createAdapterTransport("android", endpoint, function (message) {
            return endpoint.postMessage(message);
        }, source, {
            receiverRoot: source,
            receiverName: configuration.androidReceiverName || "htmlguiAndroidReceive"
        });
    }

    function createMessagePortTransport(port, environment, settings) {
        if (!port || typeof port.postMessage !== "function") {
            return null;
        }
        var configuration = settings || {};
        return createAdapterTransport("message-port", port, function (message) {
            return port.postMessage(message);
        }, environment || root, {
            startEndpoint: true,
            closeEndpoint: configuration.closeMessagePortOnStop !== false
        });
    }

    function createPostMessageTransport(environment, settings) {
        var source = environment || root;
        var configuration = settings || {};
        var target = configuration.targetWindow || (source.parent && source.parent !== source ? source.parent : null) || source.opener;
        if (!target || typeof target.postMessage !== "function" || typeof source.addEventListener !== "function") {
            return null;
        }
        var targetOrigin = configuration.targetOrigin || (source.location && source.location.origin) || "*";
        var allowedOrigin = configuration.allowedOrigin || targetOrigin;
        return createAdapterTransport("post-message", source, function (message) {
            return target.postMessage(message, targetOrigin);
        }, source, {
            accept: function (event) {
                if (!isObject(event)) {
                    return false;
                }
                if (event.source && event.source !== target) {
                    return false;
                }
                return allowedOrigin === "*" || event.origin === allowedOrigin;
            }
        });
    }

    function createLoopbackTransport(settings) {
        var configuration = settings || {};
        var receiver = null;
        var messages = [];
        var started = false;
        return {
            kind: "loopback",
            start: function (callback) {
                if (started || typeof callback !== "function") {
                    return false;
                }
                receiver = callback;
                started = true;
                return true;
            },
            send: function (message) {
                if (!started) {
                    return false;
                }
                var detached = clone(message);
                messages.push(detached);
                return !configuration.onLoopbackSend || configuration.onLoopbackSend(clone(detached)) !== false;
            },
            stop: function () {
                receiver = null;
                started = false;
                return true;
            },
            receive: function (message, metadata) {
                return receiver ? receiver(clone(message), metadata || { isMainFrame: true }) : false;
            },
            getMessages: function () {
                return clone(messages);
            }
        };
    }

    function detectTransport(environment, settings) {
        var source = environment || root;
        var configuration = settings || {};
        if (configuration.loopback === true) {
            return createLoopbackTransport(configuration);
        }
        var explicitPort = configuration.messagePort || source.htmlguiMessagePort;
        var adapter = createMessagePortTransport(explicitPort, source, configuration);
        if (adapter) {
            return adapter;
        }
        adapter = createWebView2Transport(source);
        if (adapter) {
            return adapter;
        }
        adapter = createWebKitTransport(source, configuration);
        if (adapter) {
            return adapter;
        }
        adapter = createAndroidTransport(source, configuration);
        if (adapter) {
            return adapter;
        }
        return createPostMessageTransport(source, configuration);
    }

    function decodeTaggedValue(value) {
        if (!isObject(value) || typeof value.kind !== "string") {
            return value;
        }
        if (value.kind === "null") {
            return null;
        }
        if (value.kind === "array") {
            var items = [];
            var itemIndex = 0;
            while (itemIndex < value.value.length) {
                items.push(decodeTaggedValue(value.value[itemIndex]));
                itemIndex += 1;
            }
            return items;
        }
        if (value.kind === "object") {
            var decoded = {};
            var keys = Object.keys(value.value);
            var keyIndex = 0;
            while (keyIndex < keys.length) {
                decoded[keys[keyIndex]] = decodeTaggedValue(value.value[keys[keyIndex]]);
                keyIndex += 1;
            }
            return decoded;
        }
        return value.value;
    }

    function validateTaggedValue(value, depth, state) {
        if (!isObject(value) || typeof value.kind !== "string" || depth > state.maximumDepth) {
            return false;
        }
        var names = Object.keys(value);
        if (value.kind === "null") {
            return names.length === 1 && names[0] === "kind";
        }
        if (names.length !== 2 || !own(value, "value")) {
            return false;
        }
        if (value.kind === "boolean") {
            return typeof value.value === "boolean";
        }
        if (value.kind === "integer") {
            return isInteger(value.value) && Math.abs(value.value) <= 9007199254740991;
        }
        if (value.kind === "floatingPoint") {
            return typeof value.value === "number" && isFinite(value.value);
        }
        if (value.kind === "decimal" || value.kind === "string" || value.kind === "dateTime" || value.kind === "binary") {
            return typeof value.value === "string" && value.value.length <= state.maximumStringLength;
        }
        if (value.kind === "array") {
            if (!Array.isArray(value.value)) {
                return false;
            }
            state.collectionCount += value.value.length;
            if (state.collectionCount > state.maximumCollectionCount) {
                return false;
            }
            var arrayIndex = 0;
            while (arrayIndex < value.value.length) {
                if (!validateTaggedValue(value.value[arrayIndex], depth + 1, state)) {
                    return false;
                }
                arrayIndex += 1;
            }
            return true;
        }
        if (value.kind === "object") {
            if (!isObject(value.value)) {
                return false;
            }
            var keys = Object.keys(value.value);
            state.collectionCount += keys.length;
            if (state.collectionCount > state.maximumCollectionCount) {
                return false;
            }
            var objectIndex = 0;
            while (objectIndex < keys.length) {
                if (keys[objectIndex].length > state.maximumStringLength || !validateTaggedValue(value.value[keys[objectIndex]], depth + 1, state)) {
                    return false;
                }
                objectIndex += 1;
            }
            return true;
        }
        return false;
    }

    function validatePayload(value, options) {
        if (value === undefined) {
            return true;
        }
        return validateTaggedValue(value, 0, {
            maximumDepth: options.maximumDepth,
            maximumCollectionCount: options.maximumCollectionCount,
            maximumStringLength: options.maximumStringLength,
            collectionCount: 0
        });
    }

    function validateTaggedMap(value, options) {
        if (!isObject(value)) {
            return false;
        }
        var keys = Object.keys(value);
        if (keys.length > options.maximumCollectionCount) {
            return false;
        }
        var index = 0;
        while (index < keys.length) {
            if (keys[index].length > options.maximumStringLength || !validatePayload(value[keys[index]], options)) {
                return false;
            }
            index += 1;
        }
        return true;
    }

    function hasOnlyNames(value, names) {
        var actual = Object.keys(value);
        var index = 0;
        while (index < actual.length) {
            if (names.indexOf(actual[index]) < 0) {
                return false;
            }
            index += 1;
        }
        return true;
    }

    function validateErrorBody(error, correlationId) {
        return isObject(error)
            && hasOnlyNames(error, ["code", "message", "correlationId", "retryable"])
            && isBoundedText(error.code, 256, false)
            && isBoundedText(error.message, 1048576, false)
            && (!own(error, "correlationId") || error.correlationId === correlationId)
            && typeof error.retryable === "boolean";
    }

    function validateEnvelopeBody(message, options) {
        var hasPayload = own(message, "payload");
        var hasRequest = own(message, "request");
        var hasResponse = own(message, "response");
        var hasError = own(message, "error");
        var hasCorrelation = own(message, "correlationId");
        if (message.kind === "initialize" || message.kind === "ready") {
            return hasPayload && !hasRequest && !hasResponse && !hasError && !hasCorrelation;
        }
        if (message.kind === "heartbeat" || message.kind === "shutdown") {
            return !hasPayload && !hasRequest && !hasResponse && !hasError && !hasCorrelation;
        }
        if (message.kind === "request" || message.kind === "hostCall") {
            if (!hasRequest || hasPayload || hasResponse || hasError || !hasCorrelation || !isObject(message.request)) {
                return false;
            }
            return hasOnlyNames(message.request, ["correlationId", "operation", "arguments", "timeoutMilliseconds"])
                && message.request.correlationId === message.correlationId
                && isBoundedText(message.request.operation, 256, false)
                && validateTaggedMap(message.request.arguments, options)
                && isInteger(message.request.timeoutMilliseconds)
                && message.request.timeoutMilliseconds >= 0;
        }
        if (message.kind === "response" || message.kind === "hostResult") {
            if (!hasResponse || hasPayload || hasRequest || hasError || !hasCorrelation || !isObject(message.response)) {
                return false;
            }
            if (!hasOnlyNames(message.response, ["correlationId", "success", "value", "error"])
                || message.response.correlationId !== message.correlationId
                || typeof message.response.success !== "boolean"
                || (own(message.response, "value") && !validatePayload(message.response.value, options))) {
                return false;
            }
            return !own(message.response, "error") || validateErrorBody(message.response.error, message.correlationId);
        }
        if (message.kind === "error") {
            return hasError && !hasPayload && !hasRequest && !hasResponse && hasCorrelation && validateErrorBody(message.error, message.correlationId);
        }
        if (message.kind === "cancel") {
            return hasCorrelation && !hasPayload && !hasRequest && !hasResponse && !hasError;
        }
        if (message.kind === "acknowledgement") {
            return hasPayload && !hasCorrelation && !hasRequest && !hasResponse && !hasError;
        }
        return !hasRequest && !hasResponse && !hasError;
    }

    function validateEnvelopeShape(message, options) {
        if (!isObject(message)) {
            return "bridge.invalid-envelope";
        }
        var allowed = {
            version: true,
            session: true,
            sequence: true,
            correlationId: true,
            origin: true,
            kind: true,
            payload: true,
            request: true,
            response: true,
            error: true
        };
        var names = Object.keys(message);
        var index = 0;
        while (index < names.length) {
            if (!allowed[names[index]]) {
                return "bridge.unknown-envelope-member";
            }
            index += 1;
        }
        if (!isInteger(message.version) || message.version < MINIMUM_VERSION || message.version > options.version) {
            return "bridge.version-mismatch";
        }
        if (!isBoundedText(message.session, 256, false)) {
            return "bridge.invalid-session";
        }
        if (!isInteger(message.sequence) || message.sequence < 0 || message.sequence > 2147483647) {
            return "bridge.invalid-sequence";
        }
        if (!isBoundedText(message.origin, 256, false)) {
            return "bridge.invalid-origin";
        }
        if (!own(kinds, message.kind)) {
            return "bridge.invalid-kind";
        }
        if (own(message, "correlationId") && !isBoundedText(message.correlationId, 256, true)) {
            return "bridge.invalid-correlation";
        }
        if (!validatePayload(message.payload, options)) {
            return "bridge.invalid-payload";
        }
        if (!validateEnvelopeBody(message, options)) {
            return "bridge.invalid-message-body";
        }
        var encoded;
        try {
            encoded = JSON.stringify(message);
        } catch (error) {
            return "bridge.invalid-envelope";
        }
        if (utf8Length(encoded) > options.maximumMessageLength) {
            return "bridge.message-too-large";
        }
        return "";
    }

    function validateCapabilities(value) {
        if (!isObject(value) || !isInteger(value.version) || value.version < MINIMUM_VERSION || value.version > VERSION || !Array.isArray(value.capabilities)) {
            return false;
        }
        if (value.capabilities.length > 256 || typeof value.supportsBinary !== "boolean" || typeof value.supportsChunking !== "boolean") {
            return false;
        }
        var seen = {};
        var index = 0;
        while (index < value.capabilities.length) {
            var capability = value.capabilities[index];
            if (!isObject(capability) || !isBoundedText(capability.name, 128, false) || typeof capability.supported !== "boolean") {
                return false;
            }
            if (capability.version !== null && !isBoundedText(capability.version, 128, false)) {
                return false;
            }
            if (own(seen, capability.name)) {
                return false;
            }
            seen[capability.name] = true;
            index += 1;
        }
        return true;
    }

    function readTaggedField(payload, name, kind) {
        if (!isObject(payload) || payload.kind !== "object" || !isObject(payload.value) || !own(payload.value, name)) {
            return null;
        }
        var value = payload.value[name];
        if (!isObject(value) || value.kind !== kind || !own(value, "value")) {
            return null;
        }
        return value.value;
    }

    function parseHandshake(payload) {
        var nonce = readTaggedField(payload, "nonce", "string");
        var version = readTaggedField(payload, "version", "integer");
        var binary = readTaggedField(payload, "supportsBinary", "boolean");
        var chunking = readTaggedField(payload, "supportsChunking", "boolean");
        if (!isBoundedText(nonce, 256, false) || !isInteger(version) || typeof binary !== "boolean" || typeof chunking !== "boolean") {
            return null;
        }
        var rawCapabilities = readTaggedField(payload, "capabilities", "array");
        if (!Array.isArray(rawCapabilities)) {
            return null;
        }
        var capabilities = [];
        var index = 0;
        while (index < rawCapabilities.length) {
            var raw = rawCapabilities[index];
            var name = readTaggedField(raw, "name", "string");
            var supported = readTaggedField(raw, "supported", "boolean");
            var capabilityVersion = null;
            if (isObject(raw) && raw.kind === "object" && isObject(raw.value) && own(raw.value, "version")) {
                if (raw.value.version.kind === "string") {
                    capabilityVersion = raw.value.version.value;
                } else if (raw.value.version.kind !== "null") {
                    return null;
                }
            }
            if (!isBoundedText(name, 128, false) || typeof supported !== "boolean") {
                return null;
            }
            capabilities.push({ name: name, supported: supported, version: capabilityVersion });
            index += 1;
        }
        var parsed = {
            nonce: nonce,
            version: version,
            capabilities: capabilities,
            supportsBinary: binary,
            supportsChunking: chunking
        };
        if (!validateCapabilities(parsed)) {
            return null;
        }
        return parsed;
    }

    function negotiateCapabilities(local, remote) {
        var remoteByName = {};
        var remoteIndex = 0;
        while (remoteIndex < remote.capabilities.length) {
            remoteByName[remote.capabilities[remoteIndex].name] = remote.capabilities[remoteIndex];
            remoteIndex += 1;
        }
        var items = [];
        var localIndex = 0;
        while (localIndex < local.capabilities.length) {
            var localItem = local.capabilities[localIndex];
            var remoteItem = remoteByName[localItem.name];
            var compatibleVersion = !remoteItem || localItem.version === null || remoteItem.version === null || localItem.version === remoteItem.version;
            items.push({
                name: localItem.name,
                supported: !!remoteItem && localItem.supported && remoteItem.supported && compatibleVersion,
                version: localItem.version === null && remoteItem ? remoteItem.version : localItem.version
            });
            localIndex += 1;
        }
        return {
            state: "ready",
            version: Math.min(local.version, remote.version),
            capabilities: sortCapabilities(items),
            supportsBinary: local.supportsBinary && remote.supportsBinary,
            supportsChunking: local.supportsChunking && remote.supportsChunking
        };
    }

    function createHandshakePayload(nonce, capabilities) {
        var items = [];
        var index = 0;
        while (index < capabilities.capabilities.length) {
            var capability = capabilities.capabilities[index];
            items.push(taggedObject({
                name: taggedString(capability.name),
                supported: taggedBoolean(capability.supported),
                version: capability.version === null ? taggedNull() : taggedString(capability.version)
            }));
            index += 1;
        }
        return taggedObject({
            nonce: taggedString(nonce),
            version: taggedInteger(capabilities.version),
            capabilities: taggedArray(items),
            supportsBinary: taggedBoolean(capabilities.supportsBinary),
            supportsChunking: taggedBoolean(capabilities.supportsChunking)
        });
    }

    function createRuntime(runtimeOptions) {
        var options = normalizeOptions(runtimeOptions);
        var state = "stopped";
        var transport = null;
        var session = "";
        var nonce = "";
        var nextOutboundSequence = 0;
        var lastInboundSequence = -1;
        var lastAcknowledgedSequence = -1;
        var revision = -1;
        var lastAcknowledgedRevision = -1;
        var pendingAcknowledgements = [];
        var retiredSessions = [];
        var localCapabilities = options.capabilities ? clone(options.capabilities) : detectCapabilities(root);
        var negotiatedCapabilities = null;
        var readyEnvelope = null;
        var handoff = null;
        var lastError = "";

        if (!validateCapabilities(localCapabilities)) {
            localCapabilities = detectCapabilities(root);
        }

        var documentObject = options.document;
        var domRoot = options.root || (documentObject ? (documentObject.body || documentObject.documentElement) : null);
        var nodesById = {};
        var subscriptionsById = {};
        var delegatedListenersByName = {};
        var controlledInputsById = {};
        var compositionsById = {};
        var pendingControlledValuesById = {};
        var overlaysById = {};
        var resizeObservedById = {};
        var intersectionObservedById = {};
        var virtualizersById = {};
        var resizeObserver = null;
        var intersectionObserver = null;
        var overlayKeyListener = null;
        var overlayPointerListener = null;
        var overlayOrder = 0;
        var focusOperations = ["focus", "blur", "move", "restore", "setActiveDescendant"];
        var focusDirections = ["none", "first", "last", "next", "previous", "up", "down", "left", "right"];

        function invokeLifecycleNotifications(notifications, beforeMutation) {
            var values = Array.isArray(notifications) ? notifications : [];
            var index = 0;
            while (index < values.length) {
                var notification = values[index];
                var stage = isInteger(notification.stage) ? notification.stage : -1;
                var isBefore = stage === 3 || stage === 9;
                if (isBefore === beforeMutation && options.onLifecycle) {
                    var handled = options.onLifecycle(clone(notification));
                    if (handled === false || (isObject(handled) && handled.success === false)) {
                        return false;
                    }
                }
                index += 1;
            }
            return true;
        }

        function notifyLifecycle(notification) {
            if (!isObject(notification) || !isInteger(notification.stage) || notification.stage < 0 || notification.stage > 11) {
                return failure("bridge.invalid-lifecycle-notification");
            }
            if (!invokeLifecycleNotifications([notification], notification.stage === 3 || notification.stage === 9)) {
                return failure("bridge.lifecycle-handler-failed");
            }
            return send("lifecycle", encodeTaggedValue(notification), null);
        }

        function focusNode(node, reveal) {
            if (!node || typeof node.focus !== "function") {
                return false;
            }
            node.focus();
            if (reveal && typeof node.scrollIntoView === "function") {
                node.scrollIntoView({ block: "nearest", inline: "nearest" });
            }
            return true;
        }

        function resolveFocusMove(request) {
            var targetIds = Array.isArray(request.targetIds) ? request.targetIds : [];
            if (targetIds.length === 0) {
                return "";
            }
            var activeId = readNodeIdentifier(documentObject ? documentObject.activeElement : null);
            var currentIndex = targetIds.indexOf(activeId);
            var direction = enumToken(request.direction, focusDirections, "none");
            if (direction === "first") {
                return targetIds[0];
            }
            if (direction === "last") {
                return targetIds[targetIds.length - 1];
            }
            var delta = direction === "previous" || direction === "up" || direction === "left" ? -1 : 1;
            var nextIndex = currentIndex < 0 ? (delta < 0 ? targetIds.length - 1 : 0) : currentIndex + delta;
            if (request.wrap === true) {
                nextIndex = (nextIndex + targetIds.length) % targetIds.length;
            }
            if (nextIndex < 0) {
                nextIndex = 0;
            }
            if (nextIndex >= targetIds.length) {
                nextIndex = targetIds.length - 1;
            }
            return targetIds[nextIndex];
        }

        function applyFocusRequest(request) {
            if (!isObject(request)) {
                return failure("bridge.invalid-focus-request");
            }
            var operation = enumToken(request.operation, focusOperations, "focus");
            var targetId = request.targetId || "";
            if (operation === "move") {
                targetId = resolveFocusMove(request);
            }
            if (operation === "blur") {
                var active = documentObject ? documentObject.activeElement : null;
                if (active && typeof active.blur === "function") {
                    active.blur();
                }
                return result(true, !!active, "");
            }
            if (operation === "setActiveDescendant") {
                var owner = nodesById[request.scopeId || ""];
                if (!owner || !targetId) {
                    return failure("bridge.focus-target-unavailable");
                }
                owner.setAttribute("aria-activedescendant", targetId);
                focusNode(owner, request.reveal === true);
                return result(true, true, "");
            }
            var target = nodesById[targetId];
            if (!target || !focusNode(target, request.reveal === true)) {
                return failure("bridge.focus-target-unavailable");
            }
            if (isInteger(request.selectionStart) && isInteger(request.selectionEnd) && request.selectionEnd >= request.selectionStart && typeof target.setSelectionRange === "function") {
                target.setSelectionRange(request.selectionStart, request.selectionEnd, request.selectionDirection || "none");
            }
            return result(true, true, "");
        }

        function applyScrollRequest(request) {
            if (!isObject(request) || !request.targetId || !nodesById[request.targetId]) {
                return failure("bridge.scroll-target-unavailable");
            }
            var target = nodesById[request.targetId];
            if (request.reveal === true && typeof target.scrollIntoView === "function") {
                target.scrollIntoView({ behavior: request.behavior || "auto", block: request.block || "nearest", inline: request.inline || "nearest" });
            } else if (typeof target.scrollTo === "function") {
                target.scrollTo({ left: finiteNumber(request.x), top: finiteNumber(request.y), behavior: request.behavior || "auto" });
            } else {
                target.scrollLeft = finiteNumber(request.x);
                target.scrollTop = finiteNumber(request.y);
            }
            return result(true, true, "");
        }

        function applySelectionRequest(request) {
            if (!isObject(request) || !request.targetId || !nodesById[request.targetId] || !isInteger(request.start) || !isInteger(request.end) || request.start < 0 || request.end < request.start) {
                return failure("bridge.invalid-selection-request");
            }
            var target = nodesById[request.targetId];
            if (typeof target.setSelectionRange !== "function") {
                return failure("bridge.selection-unavailable");
            }
            target.setSelectionRange(request.start, request.end, request.direction || "none");
            if (request.focus === true) {
                focusNode(target, false);
            }
            return result(true, true, "");
        }

        function applyOverlayRequest(request) {
            if (!isObject(request) || !request.targetId || !nodesById[request.targetId]) {
                return failure("bridge.overlay-target-unavailable");
            }
            var target = nodesById[request.targetId];
            if (request.open === true) {
                overlayOrder += 1;
                overlaysById[request.targetId] = {
                    targetId: request.targetId,
                    restoreFocusId: request.restoreFocusId || readNodeIdentifier(documentObject ? documentObject.activeElement : null),
                    trapFocus: request.trapFocus === true,
                    dismissOnEscape: request.dismissOnEscape !== false,
                    dismissOnOutsideClick: request.dismissOnOutsideClick !== false,
                    focusTargetIds: Array.isArray(request.focusTargetIds) ? clone(request.focusTargetIds) : [],
                    stackOrder: isInteger(request.stackOrder) ? request.stackOrder : overlayOrder
                };
                if (request.modal === true && typeof target.showModal === "function") {
                    target.showModal();
                } else if (typeof target.show === "function") {
                    target.show();
                } else {
                    target.open = true;
                    if (typeof target.setAttribute === "function") {
                        target.setAttribute("open", "");
                    }
                }
                if (request.initialFocusId && nodesById[request.initialFocusId]) {
                    focusNode(nodesById[request.initialFocusId], false);
                }
                refreshOverlayListeners();
            } else {
                var stateToRestore = overlaysById[request.targetId] || {};
                if (typeof target.close === "function") {
                    target.close();
                } else {
                    target.open = false;
                    if (typeof target.removeAttribute === "function") {
                        target.removeAttribute("open");
                    }
                }
                delete overlaysById[request.targetId];
                var restoreId = request.restoreFocusId || stateToRestore.restoreFocusId;
                if (restoreId && nodesById[restoreId]) {
                    focusNode(nodesById[restoreId], false);
                }
                refreshOverlayListeners();
            }
            return result(true, true, "");
        }

        function topOverlayState() {
            var identifiers = Object.keys(overlaysById);
            var top = null;
            var index = 0;
            while (index < identifiers.length) {
                var candidate = overlaysById[identifiers[index]];
                if (!top || candidate.stackOrder > top.stackOrder || (candidate.stackOrder === top.stackOrder && candidate.targetId > top.targetId)) {
                    top = candidate;
                }
                index += 1;
            }
            return top;
        }

        function nodeIsInside(node, ancestor) {
            var current = node;
            while (current) {
                if (current === ancestor) {
                    return true;
                }
                current = current.parentNode;
            }
            return false;
        }

        function emitOverlayDismissed(overlay, reason) {
            if (state !== "ready") {
                return;
            }
            send("event", encodeTaggedValue({
                kind: 43,
                targetId: overlay.targetId,
                path: [overlay.targetId],
                isTrusted: true,
                input: { value: false, inputType: null, data: null, isComposing: false, hasSelectionStart: false, selectionStart: 0, hasSelectionEnd: false, selectionEnd: 0 },
                reason: reason,
                revision: revision
            }), null);
        }

        function overlayKeydownHandler(nativeEvent) {
            var overlay = topOverlayState();
            if (!overlay) {
                return;
            }
            if (nativeEvent.key === "Escape" && overlay.dismissOnEscape) {
                if (typeof nativeEvent.preventDefault === "function") {
                    nativeEvent.preventDefault();
                }
                if (typeof nativeEvent.stopPropagation === "function") {
                    nativeEvent.stopPropagation();
                }
                applyOverlayRequest({ targetId: overlay.targetId, open: false, restoreFocusId: overlay.restoreFocusId });
                emitOverlayDismissed(overlay, "escape");
                return;
            }
            if (nativeEvent.key === "Tab" && overlay.trapFocus) {
                var targetIds = overlay.focusTargetIds.length > 0 ? overlay.focusTargetIds : [overlay.targetId];
                applyFocusRequest({ operation: "move", targetIds: targetIds, direction: nativeEvent.shiftKey ? "previous" : "next", wrap: true });
                if (typeof nativeEvent.preventDefault === "function") {
                    nativeEvent.preventDefault();
                }
                if (typeof nativeEvent.stopPropagation === "function") {
                    nativeEvent.stopPropagation();
                }
            }
        }

        function overlayPointerHandler(nativeEvent) {
            var overlay = topOverlayState();
            var overlayNode = overlay ? nodesById[overlay.targetId] : null;
            if (overlay && overlay.dismissOnOutsideClick && overlayNode && !nodeIsInside(nativeEvent.target, overlayNode)) {
                applyOverlayRequest({ targetId: overlay.targetId, open: false, restoreFocusId: overlay.restoreFocusId });
                emitOverlayDismissed(overlay, "outside-click");
            }
        }

        function refreshOverlayListeners() {
            if (!domRoot || typeof domRoot.addEventListener !== "function") {
                return;
            }
            var hasOverlays = Object.keys(overlaysById).length > 0;
            if (hasOverlays && !overlayKeyListener) {
                overlayKeyListener = overlayKeydownHandler;
                overlayPointerListener = overlayPointerHandler;
                domRoot.addEventListener("keydown", overlayKeyListener, true);
                domRoot.addEventListener("click", overlayPointerListener, true);
            } else if (!hasOverlays && overlayKeyListener) {
                domRoot.removeEventListener("keydown", overlayKeyListener, true);
                domRoot.removeEventListener("click", overlayPointerListener, true);
                overlayKeyListener = null;
                overlayPointerListener = null;
            }
        }

        function detachOverlayListeners() {
            overlaysById = {};
            refreshOverlayListeners();
        }

        function readNodeIdentifier(node) {
            if (!node) {
                return "";
            }
            if (node.__htmlguiId) {
                return node.__htmlguiId;
            }
            if (typeof node.getAttribute === "function") {
                return node.getAttribute("data-htmlgui-id") || node.getAttribute("id") || "";
            }
            return "";
        }

        function registerDomNode(node, identifier) {
            if (identifier) {
                node.__htmlguiId = identifier;
                nodesById[identifier] = node;
            }
        }

        function normalizeSubscription(subscription, nodeId) {
            var source = clone(subscription || {});
            var eventName = source.eventName || enumToken(source.kind, eventNames, "");
            var identifier = source.id || (nodeId + ":" + eventName);
            if (!isBoundedText(identifier, options.maximumStringLength, false) || eventNames.indexOf(eventName) < 0) {
                throw new Error("bridge.invalid-event-subscription");
            }
            source.id = identifier;
            source.nodeId = nodeId;
            source.eventName = eventName;
            source.kind = eventNames.indexOf(eventName);
            source.phase = enumToken(source.phase, eventPhases, "bubble");
            if (eventPhases.indexOf(source.phase) < 0) {
                throw new Error("bridge.invalid-event-phase");
            }
            source.handlerId = source.handlerId || "";
            source.preventDefault = source.preventDefault === true;
            source.stopPropagation = source.stopPropagation === true;
            source.once = source.once === true;
            source.passive = source.passive === true;
            return source;
        }

        function registerDomSubscriptions(node, definition) {
            var nodeId = definition.id || readNodeIdentifier(node);
            var subscriptions = Array.isArray(definition.eventSubscriptions) ? definition.eventSubscriptions : [];
            var index = 0;
            while (index < subscriptions.length) {
                var subscription = normalizeSubscription(subscriptions[index], nodeId);
                subscriptionsById[subscription.id] = subscription;
                index += 1;
            }
        }

        function createDelegatedListener() {
            return function (nativeEvent) {
                dispatchNativeEvent(nativeEvent);
            };
        }

        function refreshDelegatedListeners() {
            if (!domRoot || typeof domRoot.addEventListener !== "function") {
                return;
            }
            var needed = {};
            var subscriptionIds = Object.keys(subscriptionsById);
            var subscriptionIndex = 0;
            while (subscriptionIndex < subscriptionIds.length) {
                needed[subscriptionsById[subscriptionIds[subscriptionIndex]].eventName] = true;
                subscriptionIndex += 1;
            }
            var attachedNames = Object.keys(delegatedListenersByName);
            var attachedIndex = 0;
            while (attachedIndex < attachedNames.length) {
                var attachedName = attachedNames[attachedIndex];
                if (!needed[attachedName]) {
                    domRoot.removeEventListener(attachedName, delegatedListenersByName[attachedName], true);
                    delete delegatedListenersByName[attachedName];
                }
                attachedIndex += 1;
            }
            var neededNames = Object.keys(needed).sort();
            var neededIndex = 0;
            while (neededIndex < neededNames.length) {
                var neededName = neededNames[neededIndex];
                if (!delegatedListenersByName[neededName]) {
                    var listener = createDelegatedListener();
                    delegatedListenersByName[neededName] = listener;
                    domRoot.addEventListener(neededName, listener, true);
                }
                neededIndex += 1;
            }
        }

        function detachDelegatedListeners() {
            if (domRoot && typeof domRoot.removeEventListener === "function") {
                var names = Object.keys(delegatedListenersByName);
                var index = 0;
                while (index < names.length) {
                    domRoot.removeEventListener(names[index], delegatedListenersByName[names[index]], true);
                    index += 1;
                }
            }
            delegatedListenersByName = {};
        }

        function createDomNode(definition) {
            if (!documentObject || !isObject(definition)) {
                throw new Error("bridge.invalid-node");
            }
            var kind = enumToken(definition.kind, domNodeKinds, "element");
            var node;
            if (kind === "text") {
                node = documentObject.createTextNode(definition.text || "");
            } else if (kind === "comment") {
                node = documentObject.createComment(definition.text || "");
            } else if (kind === "fragment") {
                node = documentObject.createDocumentFragment();
            } else if (kind === "raw") {
                node = documentObject.createTextNode(definition.text || "");
            } else {
                if (!isBoundedText(definition.name, 256, false)) {
                    throw new Error("bridge.invalid-node-name");
                }
                node = definition.namespaceUri && typeof documentObject.createElementNS === "function"
                    ? documentObject.createElementNS(definition.namespaceUri, definition.name)
                    : documentObject.createElement(definition.name);
                applyDomAttributes(node, definition.attributes);
                applyDomProperties(node, definition.properties);
            }
            registerDomNode(node, definition.id || "");
            registerDomSubscriptions(node, definition);
            var children = Array.isArray(definition.children) ? definition.children : [];
            var childIndex = 0;
            while (childIndex < children.length) {
                node.appendChild(createDomNode(children[childIndex]));
                childIndex += 1;
            }
            return node;
        }

        function applyDomAttributes(node, attributes) {
            if (!node || typeof node.setAttribute !== "function" || !attributes) {
                return;
            }
            if (Array.isArray(attributes)) {
                var itemIndex = 0;
                while (itemIndex < attributes.length) {
                    var item = attributes[itemIndex];
                    if (item && item.name) {
                        if (item.namespaceUri && typeof node.setAttributeNS === "function") {
                            node.setAttributeNS(item.namespaceUri, item.name, item.value === undefined ? "" : String(item.value));
                        } else {
                            node.setAttribute(item.name, item.value === undefined ? "" : String(item.value));
                        }
                    }
                    itemIndex += 1;
                }
                return;
            }
            var names = Object.keys(attributes).sort();
            var nameIndex = 0;
            while (nameIndex < names.length) {
                node.setAttribute(names[nameIndex], String(attributes[names[nameIndex]]));
                nameIndex += 1;
            }
        }

        function applyDomProperties(node, properties) {
            if (!node || !properties) {
                return;
            }
            if (Array.isArray(properties)) {
                var itemIndex = 0;
                while (itemIndex < properties.length) {
                    if (properties[itemIndex] && properties[itemIndex].name) {
                        node[properties[itemIndex].name] = decodeTaggedValue(properties[itemIndex].value);
                    }
                    itemIndex += 1;
                }
                return;
            }
            var names = Object.keys(properties).sort();
            var nameIndex = 0;
            while (nameIndex < names.length) {
                node[names[nameIndex]] = properties[names[nameIndex]];
                nameIndex += 1;
            }
        }

        function clearDomRoot() {
            if (!domRoot) {
                return;
            }
            while (domRoot.childNodes && domRoot.childNodes.length > 0) {
                domRoot.removeChild(domRoot.childNodes[0]);
            }
        }

        function replaceDomRoots(roots) {
            if (!domRoot || !documentObject || !Array.isArray(roots)) {
                return false;
            }
            disconnectObservers();
            detachOverlayListeners();
            clearDomRoot();
            nodesById = {};
            subscriptionsById = {};
            controlledInputsById = {};
            compositionsById = {};
            pendingControlledValuesById = {};
            var index = 0;
            while (index < roots.length) {
                domRoot.appendChild(createDomNode(roots[index]));
                index += 1;
            }
            refreshDelegatedListeners();
            return true;
        }

        function nodeKind(node) {
            if (!node) {
                return "unknown";
            }
            if (node.nodeType === 3) {
                return "text";
            }
            if (node.nodeType === 8) {
                return "comment";
            }
            if (node.nodeType === 11) {
                return "fragment";
            }
            return "element";
        }

        function normalizedDefinitionAttributes(attributes) {
            var values = [];
            if (Array.isArray(attributes)) {
                var arrayIndex = 0;
                while (arrayIndex < attributes.length) {
                    if (attributes[arrayIndex] && attributes[arrayIndex].name !== "data-htmlgui-id") {
                        values.push({ name: attributes[arrayIndex].name, value: String(attributes[arrayIndex].value === undefined ? "" : attributes[arrayIndex].value) });
                    }
                    arrayIndex += 1;
                }
            } else if (attributes) {
                var names = Object.keys(attributes);
                var nameIndex = 0;
                while (nameIndex < names.length) {
                    if (names[nameIndex] !== "data-htmlgui-id") {
                        values.push({ name: names[nameIndex], value: String(attributes[names[nameIndex]]) });
                    }
                    nameIndex += 1;
                }
            }
            values.sort(function (left, right) { return left.name < right.name ? -1 : (left.name > right.name ? 1 : 0); });
            return values;
        }

        function domAttributesMatch(node, definition) {
            var actual = serializeDomAttributes(node);
            var expected = normalizedDefinitionAttributes(definition.attributes);
            if (actual.length !== expected.length) {
                return false;
            }
            var index = 0;
            while (index < actual.length) {
                if (actual[index].name !== expected[index].name || String(actual[index].value) !== expected[index].value) {
                    return false;
                }
                index += 1;
            }
            return true;
        }

        function nodeMatchesDefinition(node, definition) {
            if (!node || !definition || nodeKind(node) !== enumToken(definition.kind, domNodeKinds, "element")) {
                return false;
            }
            if (node.nodeType === 3 || node.nodeType === 8) {
                var textIdentifier = readNodeIdentifier(node);
                if (definition.id && textIdentifier && definition.id !== textIdentifier) {
                    return false;
                }
                return String(node.nodeValue || "") === String(definition.text || "");
            }
            if (node.nodeType === 1 && String(node.localName || node.nodeName || "").toLowerCase() !== String(definition.name || "").toLowerCase()) {
                return false;
            }
            if (node.nodeType === 1 && !domAttributesMatch(node, definition)) {
                return false;
            }
            var identifier = readNodeIdentifier(node);
            if (definition.id && identifier && definition.id !== identifier) {
                return false;
            }
            var expectedChildren = Array.isArray(definition.children) ? definition.children : [];
            var actualChildren = node.childNodes || [];
            if (actualChildren.length !== expectedChildren.length) {
                return false;
            }
            var index = 0;
            while (index < expectedChildren.length) {
                if (!nodeMatchesDefinition(actualChildren[index], expectedChildren[index])) {
                    return false;
                }
                index += 1;
            }
            return true;
        }

        function indexDomDefinition(node, definition) {
            if (!node || !definition) {
                return;
            }
            registerDomNode(node, definition.id || readNodeIdentifier(node));
            registerDomSubscriptions(node, definition);
            var children = node.childNodes || [];
            var definitions = Array.isArray(definition.children) ? definition.children : [];
            var index = 0;
            while (index < children.length && index < definitions.length) {
                indexDomDefinition(children[index], definitions[index]);
                index += 1;
            }
        }

        function hydrateDom(snapshot, mode) {
            if (!isObject(snapshot) || !Array.isArray(snapshot.roots) || !isInteger(snapshot.revision) || snapshot.revision < 0 || !domRoot) {
                return failure("bridge.invalid-snapshot");
            }
            if (!invokeLifecycleNotifications(snapshot.lifecycleNotifications, true)) {
                return failure("bridge.lifecycle-handler-failed");
            }
            var hydrationMode = enumToken(mode, hydrationModes, "repair");
            var actualRoots = domRoot.childNodes || [];
            var matched = actualRoots.length === snapshot.roots.length;
            var previousNodesById = nodesById;
            var previousSubscriptionsById = subscriptionsById;
            nodesById = {};
            subscriptionsById = {};
            var index = 0;
            while (matched && index < snapshot.roots.length) {
                matched = nodeMatchesDefinition(actualRoots[index], snapshot.roots[index]);
                index += 1;
            }
            if (!matched && hydrationMode === "exact") {
                nodesById = previousNodesById;
                subscriptionsById = previousSubscriptionsById;
                return failure("bridge.hydration-mismatch");
            }
            if (!matched) {
                nodesById = previousNodesById;
                subscriptionsById = previousSubscriptionsById;
                var preserved = captureDomPreservation(snapshot.preservation || {});
                if (!replaceDomRoots(snapshot.roots)) {
                    return failure("bridge.hydration-failed");
                }
                restoreDomPreservation(preserved);
            } else {
                index = 0;
                while (index < snapshot.roots.length) {
                    indexDomDefinition(actualRoots[index], snapshot.roots[index]);
                    index += 1;
                }
                refreshDelegatedListeners();
            }
            revision = snapshot.revision;
            lastAcknowledgedRevision = snapshot.revision;
            if (!invokeLifecycleNotifications(snapshot.lifecycleNotifications, false)) {
                return failure("bridge.lifecycle-handler-failed");
            }
            var hydrated = result(true, !matched, matched ? "bridge.hydration-matched" : (hydrationMode === "replace" ? "bridge.hydration-replaced" : "bridge.hydration-repaired"));
            hydrated.matched = matched;
            return hydrated;
        }

        function serializeDomAttributes(node) {
            var resultAttributes = [];
            var attributes = node && node.attributes ? node.attributes : [];
            var index = 0;
            while (index < attributes.length) {
                var attribute = attributes[index];
                if (attribute && attribute.name !== "data-htmlgui-id") {
                    resultAttributes.push({ name: attribute.name, value: attribute.value, namespaceUri: attribute.namespaceURI || null });
                }
                index += 1;
            }
            resultAttributes.sort(function (left, right) {
                return left.name < right.name ? -1 : (left.name > right.name ? 1 : 0);
            });
            return resultAttributes;
        }

        function serializeDomNode(node, sequenceState) {
            var kind = nodeKind(node);
            var definition = {
                sequence: sequenceState.value,
                kind: kind,
                id: readNodeIdentifier(node),
                key: readNodeIdentifier(node),
                children: []
            };
            sequenceState.value += 1;
            if (kind === "text" || kind === "comment") {
                definition.text = String(node.nodeValue || "");
            } else if (kind === "element") {
                definition.name = String(node.localName || node.nodeName || "").toLowerCase();
                definition.namespaceUri = node.namespaceURI || null;
                definition.attributes = serializeDomAttributes(node);
                definition.properties = [];
                if ("value" in node && node.value !== undefined) {
                    definition.properties.push({ name: "value", value: node.value });
                }
                if ("checked" in node && node.checked !== undefined) {
                    definition.properties.push({ name: "checked", value: node.checked === true });
                }
                if ("selectedIndex" in node && isInteger(node.selectedIndex)) {
                    definition.properties.push({ name: "selectedIndex", value: node.selectedIndex });
                }
            }
            definition.eventSubscriptions = serializeDomSubscriptions(definition.id);
            var children = node.childNodes || [];
            var index = 0;
            while (index < children.length) {
                definition.children.push(serializeDomNode(children[index], sequenceState));
                index += 1;
            }
            return definition;
        }

        function serializeDomSubscriptions(nodeId) {
            var resultSubscriptions = [];
            var identifiers = Object.keys(subscriptionsById).sort();
            var index = 0;
            while (index < identifiers.length) {
                var subscription = subscriptionsById[identifiers[index]];
                if (subscription.nodeId === nodeId) {
                    var detached = clone(subscription);
                    delete detached.nodeId;
                    resultSubscriptions.push(detached);
                }
                index += 1;
            }
            return resultSubscriptions;
        }

        function finiteNumber(value) {
            return typeof value === "number" && isFinite(value) ? value : 0;
        }

        function readControlValue(node) {
            var type = String(node && (node.type || (typeof node.getAttribute === "function" ? node.getAttribute("type") : "")) || "").toLowerCase();
            if (type === "checkbox" || type === "radio") {
                return node.checked === true;
            }
            if (node && node.multiple === true && node.options) {
                var selected = [];
                var optionIndex = 0;
                while (optionIndex < node.options.length) {
                    if (node.options[optionIndex].selected) {
                        selected.push(String(node.options[optionIndex].value));
                    }
                    optionIndex += 1;
                }
                return selected;
            }
            if (node && "value" in node && node.value !== undefined) {
                return node.value;
            }
            if (node && "checked" in node) {
                return node.checked === true;
            }
            if (node && "open" in node) {
                return node.open === true;
            }
            return null;
        }

        function extractTransferValues(transfer) {
            var values = [];
            var types = transfer && transfer.types ? transfer.types : [];
            var names = [];
            var index = 0;
            while (index < types.length && names.length < options.maximumCollectionCount) {
                var name = String(types[index]);
                if (name.length <= options.maximumStringLength && names.indexOf(name) < 0) {
                    names.push(name);
                }
                index += 1;
            }
            names.sort();
            index = 0;
            while (index < names.length) {
                var transferValue = "";
                try {
                    transferValue = typeof transfer.getData === "function" ? String(transfer.getData(names[index]) || "") : "";
                } catch (error) {
                    transferValue = "";
                }
                if (transferValue.length <= options.maximumStringLength) {
                    values.push({ key: names[index], value: transferValue });
                }
                index += 1;
            }
            return values;
        }

        function extractSemanticEventData(nativeEvent, eventKind, targetId, path, target) {
            var data = {
                kind: eventKind,
                targetId: targetId,
                path: path,
                isTrusted: nativeEvent.isTrusted === true
            };
            if (eventKind >= 0 && eventKind <= 14) {
                var pointerType = nativeEvent.pointerType || ((eventKind >= 9 && eventKind <= 14) || eventKind <= 2 ? "mouse" : "");
                data.pointer = {
                    pointerType: String(pointerType),
                    pointerId: isInteger(nativeEvent.pointerId) ? nativeEvent.pointerId : 0,
                    clientX: finiteNumber(nativeEvent.clientX),
                    clientY: finiteNumber(nativeEvent.clientY),
                    button: isInteger(nativeEvent.button) ? nativeEvent.button : 0,
                    buttons: isInteger(nativeEvent.buttons) && nativeEvent.buttons >= 0 ? nativeEvent.buttons : 0,
                    primaryModifier: nativeEvent.metaKey === true || nativeEvent.ctrlKey === true,
                    shift: nativeEvent.shiftKey === true
                };
            } else if (eventKind === 15 || eventKind === 16) {
                data.keyboard = {
                    key: String(nativeEvent.key || ""),
                    code: String(nativeEvent.code || ""),
                    primaryModifier: nativeEvent.metaKey === true || nativeEvent.ctrlKey === true,
                    alt: nativeEvent.altKey === true,
                    shift: nativeEvent.shiftKey === true,
                    repeat: nativeEvent.repeat === true,
                    isComposing: nativeEvent.isComposing === true
                };
            }
            if ((eventKind >= 21 && eventKind <= 29) || eventKind === 43) {
                var controlValue = readControlValue(target);
                data.input = {
                    value: controlValue,
                    inputType: nativeEvent.inputType === undefined ? null : String(nativeEvent.inputType),
                    data: nativeEvent.data === undefined || nativeEvent.data === null ? null : String(nativeEvent.data),
                    isComposing: nativeEvent.isComposing === true || eventKind === 24 || eventKind === 25,
                    hasSelectionStart: isInteger(target.selectionStart),
                    selectionStart: isInteger(target.selectionStart) ? target.selectionStart : 0,
                    hasSelectionEnd: isInteger(target.selectionEnd),
                    selectionEnd: isInteger(target.selectionEnd) ? target.selectionEnd : 0
                };
                if (eventKind === 22 || eventKind === 23 || eventKind === 26 || eventKind === 43) {
                    controlledInputsById[targetId] = clone(controlValue);
                }
            }
            if (eventKind >= 30 && eventKind <= 36) {
                var dragTransfer = nativeEvent.dataTransfer || null;
                data.drag = {
                    sourceId: nativeEvent.sourceId || null,
                    targetId: targetId,
                    effect: dragTransfer ? String(dragTransfer.dropEffect || "") : "",
                    allowedEffect: dragTransfer ? String(dragTransfer.effectAllowed || "") : "",
                    data: extractTransferValues(dragTransfer),
                    isKeyboard: nativeEvent.isKeyboard === true
                };
            }
            if (eventKind >= 37 && eventKind <= 39) {
                data.clipboard = {
                    operation: eventNames[eventKind],
                    data: extractTransferValues(nativeEvent.clipboardData || null),
                    isAllowed: nativeEvent.clipboardData !== null && nativeEvent.clipboardData !== undefined
                };
            }
            return data;
        }

        function subscriptionsForNode(nodeId, eventName, phase) {
            var matches = [];
            var identifiers = Object.keys(subscriptionsById).sort();
            var index = 0;
            while (index < identifiers.length) {
                var subscription = subscriptionsById[identifiers[index]];
                if (subscription.nodeId === nodeId && subscription.eventName === eventName && subscription.phase === phase) {
                    matches.push(subscription);
                }
                index += 1;
            }
            return matches;
        }

        function appendEventInvocations(invocations, nodeId, eventName, phase, decision) {
            var matches = subscriptionsForNode(nodeId, eventName, phase);
            var index = 0;
            while (index < matches.length) {
                var subscription = matches[index];
                invocations.push(subscription);
                if (subscription.preventDefault && !subscription.passive) {
                    decision.preventDefault = true;
                }
                if (subscription.stopPropagation) {
                    decision.stopPropagation = true;
                }
                index += 1;
            }
            return matches.length > 0;
        }

        function dispatchNativeEvent(nativeEvent) {
            if (!nativeEvent || !isBoundedText(nativeEvent.type, 64, false)) {
                return failure("bridge.invalid-browser-event");
            }
            if (state !== "ready") {
                return failure("bridge.invalid-state");
            }
            var eventName = String(nativeEvent.type).toLowerCase();
            var eventKind = eventNames.indexOf(eventName);
            if (eventKind < 0) {
                return failure("bridge.unsupported-browser-event");
            }
            var target = nativeEvent.target || nativeEvent.srcElement;
            if (target && target.nodeType === 3) {
                target = target.parentNode;
            }
            var path = [];
            var current = target;
            var reachedRoot = false;
            while (current) {
                var currentId = readNodeIdentifier(current);
                if (currentId) {
                    if (nodesById[currentId] !== current) {
                        return failure("bridge.event-stale-node");
                    }
                    path.push(currentId);
                }
                if (current === domRoot) {
                    reachedRoot = true;
                    break;
                }
                current = current.parentNode;
            }
            if (!reachedRoot || path.length === 0) {
                return failure("bridge.event-stale-node");
            }
            var targetId = path[0];
            var endsComposition = eventKind === 26;
            if (eventKind === 24 || eventKind === 25) {
                compositionsById[targetId] = {
                    data: nativeEvent.data === undefined || nativeEvent.data === null ? null : String(nativeEvent.data),
                    value: clone(readControlValue(target))
                };
            }
            var invocations = [];
            var decision = { preventDefault: false, stopPropagation: false };
            var pathIndex = path.length - 1;
            while (pathIndex >= 1) {
                appendEventInvocations(invocations, path[pathIndex], eventName, "capture", decision);
                if (decision.stopPropagation) {
                    break;
                }
                pathIndex -= 1;
            }
            if (!decision.stopPropagation) {
                appendEventInvocations(invocations, path[0], eventName, "capture", decision);
                appendEventInvocations(invocations, path[0], eventName, "target", decision);
                appendEventInvocations(invocations, path[0], eventName, "bubble", decision);
            }
            if (!decision.stopPropagation) {
                pathIndex = 1;
                while (pathIndex < path.length) {
                    appendEventInvocations(invocations, path[pathIndex], eventName, "bubble", decision);
                    if (decision.stopPropagation) {
                        break;
                    }
                    pathIndex += 1;
                }
            }
            if (invocations.length === 0) {
                if (endsComposition) {
                    completeComposition(targetId, target);
                }
                return result(true, false, "bridge.event-unhandled");
            }
            if (decision.preventDefault && typeof nativeEvent.preventDefault === "function") {
                nativeEvent.preventDefault();
            }
            if (decision.stopPropagation && typeof nativeEvent.stopPropagation === "function") {
                nativeEvent.stopPropagation();
            }
            var subscriptionIds = [];
            var handlerIds = [];
            var invocationIndex = 0;
            while (invocationIndex < invocations.length) {
                subscriptionIds.push(invocations[invocationIndex].id);
                handlerIds.push(invocations[invocationIndex].handlerId);
                if (invocations[invocationIndex].once) {
                    delete subscriptionsById[invocations[invocationIndex].id];
                }
                invocationIndex += 1;
            }
            refreshDelegatedListeners();
            var eventData = extractSemanticEventData(nativeEvent, eventKind, targetId, path, target);
            eventData.subscriptionIds = subscriptionIds;
            eventData.handlerIds = handlerIds;
            eventData.defaultPrevented = decision.preventDefault;
            eventData.propagationStopped = decision.stopPropagation;
            eventData.revision = revision;
            var sent = send("event", encodeTaggedValue(eventData), null);
            sent.defaultPrevented = decision.preventDefault;
            sent.propagationStopped = decision.stopPropagation;
            sent.subscriptionIds = subscriptionIds;
            if (endsComposition) {
                completeComposition(targetId, target);
            }
            return sent;
        }

        function completeComposition(nodeId, node) {
            delete compositionsById[nodeId];
            if (own(pendingControlledValuesById, nodeId)) {
                node.value = pendingControlledValuesById[nodeId];
                controlledInputsById[nodeId] = clone(pendingControlledValuesById[nodeId]);
                delete pendingControlledValuesById[nodeId];
            }
        }

        function resizeObserverCallback(entries) {
            var values = Array.isArray(entries) ? entries : [];
            var index = 0;
            while (index < values.length) {
                var entry = values[index];
                var nodeId = readNodeIdentifier(entry.target);
                if (nodeId && resizeObservedById[nodeId] && nodesById[nodeId] === entry.target && state === "ready") {
                    var rectangle = entry.contentRect || {};
                    send("measurement", encodeTaggedValue({
                        kind: "resize",
                        nodeId: nodeId,
                        revision: revision,
                        x: finiteNumber(rectangle.x),
                        y: finiteNumber(rectangle.y),
                        width: finiteNumber(rectangle.width),
                        height: finiteNumber(rectangle.height)
                    }), null);
                }
                index += 1;
            }
        }

        function intersectionObserverCallback(entries) {
            var values = Array.isArray(entries) ? entries : [];
            var index = 0;
            while (index < values.length) {
                var entry = values[index];
                var nodeId = readNodeIdentifier(entry.target);
                if (nodeId && intersectionObservedById[nodeId] && nodesById[nodeId] === entry.target && state === "ready") {
                    var rectangle = entry.boundingClientRect || {};
                    send("measurement", encodeTaggedValue({
                        kind: "intersection",
                        nodeId: nodeId,
                        revision: revision,
                        isIntersecting: entry.isIntersecting === true,
                        ratio: finiteNumber(entry.intersectionRatio),
                        x: finiteNumber(rectangle.x),
                        y: finiteNumber(rectangle.y),
                        width: finiteNumber(rectangle.width),
                        height: finiteNumber(rectangle.height)
                    }), null);
                }
                index += 1;
            }
        }

        function observeResize(nodeId) {
            var node = nodesById[nodeId];
            if (!node || typeof options.ResizeObserver !== "function") {
                return failure("bridge.resize-observer-unavailable");
            }
            if (!resizeObserver) {
                resizeObserver = new options.ResizeObserver(resizeObserverCallback);
            }
            resizeObservedById[nodeId] = true;
            resizeObserver.observe(node);
            return result(true, true, "");
        }

        function observeIntersection(nodeId) {
            var node = nodesById[nodeId];
            if (!node || typeof options.IntersectionObserver !== "function") {
                return failure("bridge.intersection-observer-unavailable");
            }
            if (!intersectionObserver) {
                intersectionObserver = new options.IntersectionObserver(intersectionObserverCallback);
            }
            intersectionObservedById[nodeId] = true;
            intersectionObserver.observe(node);
            return result(true, true, "");
        }

        function unobserveNode(nodeId) {
            var node = nodesById[nodeId];
            var applied = false;
            if (resizeObservedById[nodeId]) {
                if (resizeObserver && node && typeof resizeObserver.unobserve === "function") {
                    resizeObserver.unobserve(node);
                }
                delete resizeObservedById[nodeId];
                applied = true;
            }
            if (intersectionObservedById[nodeId]) {
                if (intersectionObserver && node && typeof intersectionObserver.unobserve === "function") {
                    intersectionObserver.unobserve(node);
                }
                delete intersectionObservedById[nodeId];
                applied = true;
            }
            unregisterVirtualizer(nodeId);
            return result(true, applied, "");
        }

        function calculateVirtualRange(record) {
            var node = nodesById[record.nodeId];
            if (!node) {
                return null;
            }
            var horizontal = record.orientation === "horizontal" || record.orientation === 0;
            var offset = horizontal ? finiteNumber(node.scrollLeft) : finiteNumber(node.scrollTop);
            var viewport = horizontal ? finiteNumber(node.clientWidth) : finiteNumber(node.clientHeight);
            var visibleStart = Math.floor(Math.max(0, offset) / record.itemExtent);
            var visibleEnd = Math.ceil(Math.max(0, offset + viewport) / record.itemExtent);
            if (record.hasTotalCount) {
                visibleStart = Math.min(visibleStart, record.totalCount);
                visibleEnd = Math.min(visibleEnd, record.totalCount);
            }
            var start = Math.max(0, visibleStart - record.overscan);
            var end = visibleEnd + record.overscan;
            if (record.hasTotalCount) {
                end = Math.min(end, record.totalCount);
            }
            return { start: start, count: Math.max(0, end - start), visibleStart: visibleStart, visibleCount: Math.max(0, visibleEnd - visibleStart) };
        }

        function requestVirtualRange(nodeId) {
            var record = virtualizersById[nodeId];
            if (!record || nodesById[nodeId] !== record.node) {
                return failure("bridge.virtualizer-unavailable");
            }
            var range = calculateVirtualRange(record);
            if (!range) {
                return failure("bridge.virtualizer-unavailable");
            }
            if (record.lastStart === range.start && record.lastCount === range.count) {
                return result(true, false, "bridge.virtual-range-unchanged");
            }
            var payload = {
                nodeId: nodeId,
                generation: record.generation,
                start: range.start,
                count: range.count,
                visibleStart: range.visibleStart,
                visibleCount: range.visibleCount,
                sorts: clone(record.sorts),
                filters: clone(record.filters),
                groups: clone(record.groups),
                parentKey: record.parentKey
            };
            record.generation += 1;
            record.lastStart = range.start;
            record.lastCount = range.count;
            return send("virtualizationRange", encodeTaggedValue(payload), null);
        }

        function registerVirtualizer(nodeId, settings) {
            var node = nodesById[nodeId];
            var source = settings || {};
            if (!node || typeof node.addEventListener !== "function" || typeof source.itemExtent !== "number" || !isFinite(source.itemExtent) || source.itemExtent <= 0) {
                return failure("bridge.invalid-virtualizer");
            }
            unregisterVirtualizer(nodeId);
            var listener = function () {
                requestVirtualRange(nodeId);
            };
            virtualizersById[nodeId] = {
                nodeId: nodeId,
                node: node,
                orientation: source.orientation === undefined ? 1 : source.orientation,
                itemExtent: source.itemExtent,
                overscan: isInteger(source.overscan) && source.overscan >= 0 ? source.overscan : 0,
                hasTotalCount: source.hasTotalCount === true || isInteger(source.totalCount),
                totalCount: isInteger(source.totalCount) && source.totalCount >= 0 ? source.totalCount : 0,
                generation: isInteger(source.generation) && source.generation >= 0 ? source.generation : 0,
                sorts: Array.isArray(source.sorts) ? source.sorts : [],
                filters: Array.isArray(source.filters) ? source.filters : [],
                groups: Array.isArray(source.groups) ? source.groups : [],
                parentKey: source.parentKey || null,
                lastStart: -1,
                lastCount: -1,
                listener: listener
            };
            node.addEventListener("scroll", listener, false);
            return result(true, true, "");
        }

        function unregisterVirtualizer(nodeId) {
            var record = virtualizersById[nodeId];
            if (!record) {
                return result(true, false, "");
            }
            if (record.node && typeof record.node.removeEventListener === "function") {
                record.node.removeEventListener("scroll", record.listener, false);
            }
            delete virtualizersById[nodeId];
            return result(true, true, "");
        }

        function disconnectObservers() {
            if (resizeObserver && typeof resizeObserver.disconnect === "function") {
                resizeObserver.disconnect();
            }
            if (intersectionObserver && typeof intersectionObserver.disconnect === "function") {
                intersectionObserver.disconnect();
            }
            var virtualizerIds = Object.keys(virtualizersById);
            var index = 0;
            while (index < virtualizerIds.length) {
                unregisterVirtualizer(virtualizerIds[index]);
                index += 1;
            }
            resizeObserver = null;
            intersectionObserver = null;
            resizeObservedById = {};
            intersectionObservedById = {};
        }

        function captureBehaviorRegistrations() {
            var virtualizers = [];
            var identifiers = Object.keys(virtualizersById).sort();
            var index = 0;
            while (index < identifiers.length) {
                var record = virtualizersById[identifiers[index]];
                virtualizers.push({
                    nodeId: record.nodeId,
                    orientation: record.orientation,
                    itemExtent: record.itemExtent,
                    overscan: record.overscan,
                    hasTotalCount: record.hasTotalCount,
                    totalCount: record.totalCount,
                    generation: record.generation,
                    sorts: clone(record.sorts),
                    filters: clone(record.filters),
                    groups: clone(record.groups),
                    parentKey: record.parentKey
                });
                index += 1;
            }
            return {
                resize: Object.keys(resizeObservedById).sort(),
                intersection: Object.keys(intersectionObservedById).sort(),
                virtualizers: virtualizers
            };
        }

        function restoreBehaviorRegistrations(backup) {
            var index = 0;
            while (index < backup.resize.length) {
                observeResize(backup.resize[index]);
                index += 1;
            }
            index = 0;
            while (index < backup.intersection.length) {
                observeIntersection(backup.intersection[index]);
                index += 1;
            }
            index = 0;
            while (index < backup.virtualizers.length) {
                registerVirtualizer(backup.virtualizers[index].nodeId, backup.virtualizers[index]);
                index += 1;
            }
        }

        function createDomSnapshot() {
            var roots = [];
            var children = domRoot && domRoot.childNodes ? domRoot.childNodes : [];
            var sequenceState = { value: 0 };
            var index = 0;
            while (index < children.length) {
                roots.push(serializeDomNode(children[index], sequenceState));
                index += 1;
            }
            return clone({ sessionId: session, revision: revision, roots: roots });
        }

        function captureDomPreservation(directives) {
            var source = directives || {};
            var captured = { focusedNodeId: null, selection: null, composition: null, scroll: {}, values: {}, media: {}, overlays: [], overlayStates: {} };
            var active = documentObject ? documentObject.activeElement : null;
            var activeId = readNodeIdentifier(active);
            if (source.preserveFocus && active) {
                captured.focusedNodeId = source.focusedNodeId || activeId;
            }
            if (source.preserveSelection && source.hasSelection && isInteger(source.selectionStart) && isInteger(source.selectionEnd)) {
                captured.selection = { start: source.selectionStart, end: source.selectionEnd, direction: source.selectionDirection || "none" };
            } else if (source.preserveSelection && active && isInteger(active.selectionStart) && isInteger(active.selectionEnd)) {
                captured.selection = { start: active.selectionStart, end: active.selectionEnd, direction: active.selectionDirection || "none" };
            }
            if (source.preserveComposition && activeId && compositionsById[activeId]) {
                captured.composition = { nodeId: activeId, state: clone(compositionsById[activeId]) };
            }
            var identifiers = Object.keys(nodesById).sort();
            var index = 0;
            while (index < identifiers.length) {
                var identifier = identifiers[index];
                var node = nodesById[identifier];
                if (source.preserveScroll && (node.scrollLeft || node.scrollTop)) {
                    captured.scroll[identifier] = { x: node.scrollLeft || 0, y: node.scrollTop || 0 };
                }
                if (source.preserveUserEditedFields && "value" in node) {
                    captured.values[identifier] = node.value;
                }
                if (source.preserveMediaPlayback && "currentTime" in node) {
                    captured.media[identifier] = { currentTime: node.currentTime || 0, paused: node.paused !== false };
                }
                if (source.preserveOpenOverlays && ((typeof node.hasAttribute === "function" && node.hasAttribute("open")) || ("open" in node && node.open === true))) {
                    captured.overlays.push(identifier);
                    captured.overlayStates[identifier] = clone(overlaysById[identifier] || {});
                }
                index += 1;
            }
            return captured;
        }

        function restoreDomPreservation(captured) {
            if (!captured) {
                return result(true, false, "");
            }
            var valueIds = Object.keys(captured.values || {});
            var valueIndex = 0;
            while (valueIndex < valueIds.length) {
                if (nodesById[valueIds[valueIndex]]) {
                    nodesById[valueIds[valueIndex]].value = captured.values[valueIds[valueIndex]];
                }
                valueIndex += 1;
            }
            var scrollIds = Object.keys(captured.scroll || {});
            var scrollIndex = 0;
            while (scrollIndex < scrollIds.length) {
                var scrollNode = nodesById[scrollIds[scrollIndex]];
                var offset = captured.scroll[scrollIds[scrollIndex]];
                if (scrollNode) {
                    if (typeof scrollNode.scrollTo === "function") {
                        scrollNode.scrollTo(offset.x, offset.y);
                    } else {
                        scrollNode.scrollLeft = offset.x;
                        scrollNode.scrollTop = offset.y;
                    }
                }
                scrollIndex += 1;
            }
            var mediaIds = Object.keys(captured.media || {});
            var mediaIndex = 0;
            while (mediaIndex < mediaIds.length) {
                var mediaNode = nodesById[mediaIds[mediaIndex]];
                var mediaState = captured.media[mediaIds[mediaIndex]];
                if (mediaNode) {
                    mediaNode.currentTime = mediaState.currentTime;
                    if (!mediaState.paused && typeof mediaNode.play === "function") {
                        mediaNode.play();
                    } else if (mediaState.paused && typeof mediaNode.pause === "function") {
                        mediaNode.pause();
                    }
                }
                mediaIndex += 1;
            }
            var overlayIndex = 0;
            while (overlayIndex < (captured.overlays || []).length) {
                var overlay = nodesById[captured.overlays[overlayIndex]];
                if (overlay) {
                    if (typeof overlay.setAttribute === "function") {
                        overlay.setAttribute("open", "");
                    }
                    overlay.open = true;
                    overlaysById[captured.overlays[overlayIndex]] = clone((captured.overlayStates || {})[captured.overlays[overlayIndex]] || {});
                }
                overlayIndex += 1;
            }
            var focused = nodesById[captured.focusedNodeId];
            if (focused && typeof focused.focus === "function") {
                focused.focus();
                if (captured.selection && typeof focused.setSelectionRange === "function") {
                    focused.setSelectionRange(captured.selection.start, captured.selection.end, captured.selection.direction || "none");
                }
            }
            if (captured.composition && nodesById[captured.composition.nodeId]) {
                compositionsById[captured.composition.nodeId] = clone(captured.composition.state);
            }
            refreshOverlayListeners();
            return result(true, true, "");
        }

        function findMutationParent(mutation) {
            return mutation.parentId ? nodesById[mutation.parentId] : domRoot;
        }

        function unregisterDomTree(node) {
            if (!node) {
                return;
            }
            var identifier = readNodeIdentifier(node);
            if (identifier) {
                unobserveNode(identifier);
                delete nodesById[identifier];
                delete controlledInputsById[identifier];
                delete compositionsById[identifier];
                delete pendingControlledValuesById[identifier];
                delete overlaysById[identifier];
                var subscriptionIds = Object.keys(subscriptionsById);
                var subscriptionIndex = 0;
                while (subscriptionIndex < subscriptionIds.length) {
                    if (subscriptionsById[subscriptionIds[subscriptionIndex]].nodeId === identifier) {
                        delete subscriptionsById[subscriptionIds[subscriptionIndex]];
                    }
                    subscriptionIndex += 1;
                }
            }
            var children = node.childNodes || [];
            var index = 0;
            while (index < children.length) {
                unregisterDomTree(children[index]);
                index += 1;
            }
        }

        function setClassToken(node, token, add) {
            if (node.classList && typeof node.classList.add === "function") {
                if (add) {
                    node.classList.add(token);
                } else {
                    node.classList.remove(token);
                }
                return;
            }
            var tokens = String(typeof node.getAttribute === "function" ? (node.getAttribute("class") || "") : "").split(/\s+/).filter(function (item) { return item; });
            var index = tokens.indexOf(token);
            if (add && index < 0) {
                tokens.push(token);
            } else if (!add && index >= 0) {
                tokens.splice(index, 1);
            }
            node.setAttribute("class", tokens.join(" "));
        }

        function applyDomMutation(mutation) {
            if (!isObject(mutation)) {
                throw new Error("bridge.invalid-mutation");
            }
            var mutationKind = enumToken(mutation.kind, mutationKinds, "");
            if (!isBoundedText(mutationKind, 64, false)) {
                throw new Error("bridge.invalid-mutation");
            }
            var target = mutation.targetId ? nodesById[mutation.targetId] : null;
            var parent;
            var reference;
            if (mutationKind === "insert") {
                parent = findMutationParent(mutation);
                if (!parent || !mutation.node) {
                    throw new Error("bridge.invalid-insert");
                }
                reference = mutation.referenceId ? nodesById[mutation.referenceId] : ((parent.childNodes || [])[mutation.index] || null);
                parent.insertBefore(createDomNode(mutation.node), reference);
                return;
            }
            if (!target) {
                throw new Error("bridge.unknown-mutation-target");
            }
            if (mutationKind === "remove") {
                unregisterDomTree(target);
                target.parentNode.removeChild(target);
            } else if (mutationKind === "move") {
                parent = findMutationParent(mutation);
                var destinationIndex = isInteger(mutation.toIndex) ? mutation.toIndex : mutation.index;
                reference = mutation.referenceId ? nodesById[mutation.referenceId] : ((parent.childNodes || [])[destinationIndex] || null);
                parent.insertBefore(target, reference);
            } else if (mutationKind === "replace") {
                if (!mutation.node || !target.parentNode) {
                    throw new Error("bridge.invalid-replace");
                }
                unregisterDomTree(target);
                target.parentNode.replaceChild(createDomNode(mutation.node), target);
            } else if (mutationKind === "setText") {
                if (target.nodeType === 3 || target.nodeType === 8) {
                    target.nodeValue = mutation.value || "";
                } else {
                    var textChildren = target.childNodes || [];
                    var textIndex = 0;
                    while (textIndex < textChildren.length) {
                        unregisterDomTree(textChildren[textIndex]);
                        textIndex += 1;
                    }
                    target.textContent = mutation.value || "";
                }
            } else if (mutationKind === "setAttribute") {
                var attribute = mutation.attribute || {};
                var attributeName = mutation.name || attribute.name;
                var attributeValue = mutation.value === undefined ? attribute.value : mutation.value;
                if (attribute.namespaceUri && typeof target.setAttributeNS === "function") {
                    target.setAttributeNS(attribute.namespaceUri, attributeName, attributeValue === undefined ? "" : String(attributeValue));
                } else {
                    target.setAttribute(attributeName, attributeValue === undefined ? "" : String(attributeValue));
                }
            } else if (mutationKind === "removeAttribute") {
                var removedAttribute = mutation.attribute || {};
                var removedAttributeName = mutation.name || removedAttribute.name;
                if (removedAttribute.namespaceUri && typeof target.removeAttributeNS === "function") {
                    target.removeAttributeNS(removedAttribute.namespaceUri, removedAttributeName);
                } else {
                    target.removeAttribute(removedAttributeName);
                }
            } else if (mutationKind === "setProperty") {
                var property = mutation.property || {};
                var propertyName = mutation.name || property.name;
                var propertyValue = decodeTaggedValue(own(property, "value") ? property.value : mutation.value);
                if (propertyName === "value" && compositionsById[mutation.targetId]) {
                    pendingControlledValuesById[mutation.targetId] = clone(propertyValue);
                } else {
                    target[propertyName] = propertyValue;
                }
                if (propertyName === "value" || propertyName === "checked" || propertyName === "selectedIndex") {
                    controlledInputsById[mutation.targetId] = clone(propertyValue);
                }
            } else if (mutationKind === "removeProperty") {
                var removedPropertyName = mutation.name || (mutation.property || {}).name;
                target[removedPropertyName] = undefined;
                if (removedPropertyName === "value" || removedPropertyName === "checked" || removedPropertyName === "selectedIndex") {
                    delete controlledInputsById[mutation.targetId];
                }
            } else if (mutationKind === "addClass") {
                setClassToken(target, mutation.name, true);
            } else if (mutationKind === "removeClass") {
                setClassToken(target, mutation.name, false);
            } else if (mutationKind === "setStyle") {
                if (target.style && typeof target.style.setProperty === "function") {
                    target.style.setProperty(mutation.name, mutation.value || "");
                } else {
                    target.style[mutation.name] = mutation.value || "";
                }
            } else if (mutationKind === "removeStyle") {
                if (target.style && typeof target.style.removeProperty === "function") {
                    target.style.removeProperty(mutation.name);
                } else {
                    target.style[mutation.name] = "";
                }
            } else if (mutationKind === "subscribeEvent") {
                var addedSubscription = normalizeSubscription(mutation.eventSubscription || mutation.subscription || {}, mutation.targetId);
                subscriptionsById[addedSubscription.id] = addedSubscription;
            } else if (mutationKind === "unsubscribeEvent") {
                delete subscriptionsById[mutation.name];
            } else {
                throw new Error("bridge.unsupported-mutation");
            }
        }

        function restoreDomBackup(backup) {
            if (!domRoot || !backup) {
                return;
            }
            disconnectObservers();
            clearDomRoot();
            var index = 0;
            while (index < backup.length) {
                domRoot.appendChild(backup[index]);
                index += 1;
            }
            nodesById = {};
            indexDomChildren(domRoot);
        }

        function cloneDomTreeForBackup(node) {
            var copy = node.cloneNode(true);
            copyDomIdentifiers(node, copy);
            return copy;
        }

        function copyDomIdentifiers(source, destination) {
            var identifier = readNodeIdentifier(source);
            if (identifier) {
                destination.__htmlguiId = identifier;
            }
            var sourceChildren = source.childNodes || [];
            var destinationChildren = destination.childNodes || [];
            var index = 0;
            while (index < sourceChildren.length && index < destinationChildren.length) {
                copyDomIdentifiers(sourceChildren[index], destinationChildren[index]);
                index += 1;
            }
        }

        function indexDomChildren(parent) {
            var children = parent && parent.childNodes ? parent.childNodes : [];
            var index = 0;
            while (index < children.length) {
                var child = children[index];
                registerDomNode(child, readNodeIdentifier(child));
                indexDomChildren(child);
                index += 1;
            }
        }

        function applyDomPatch(patch) {
            if (!isObject(patch) || !isInteger(patch.baseRevision) || !isInteger(patch.revision) || patch.revision <= patch.baseRevision || !Array.isArray(patch.mutations)) {
                return failure("bridge.invalid-patch");
            }
            if (patch.baseRevision !== revision) {
                var stale = failure("bridge.revision-mismatch");
                stale.requiresResync = true;
                return stale;
            }
            if (!domRoot) {
                return failure("bridge.dom-unavailable");
            }
            var backup = [];
            var children = domRoot.childNodes || [];
            var backupIndex = 0;
            while (backupIndex < children.length) {
                backup.push(cloneDomTreeForBackup(children[backupIndex]));
                backupIndex += 1;
            }
            var captured = captureDomPreservation(patch.preservation || {});
            var rollbackPreservation = captureDomPreservation({
                preserveFocus: true,
                preserveSelection: true,
                preserveComposition: true,
                preserveScroll: true,
                preserveUserEditedFields: true,
                preserveMediaPlayback: true,
                preserveOpenOverlays: true
            });
            var oldRevision = revision;
            var subscriptionBackup = clone(subscriptionsById);
            var controlledInputBackup = clone(controlledInputsById);
            var compositionBackup = clone(compositionsById);
            var pendingControlledBackup = clone(pendingControlledValuesById);
            var overlayBackup = clone(overlaysById);
            var behaviorBackup = captureBehaviorRegistrations();
            try {
                if (!invokeLifecycleNotifications(patch.lifecycleNotifications, true)) {
                    throw new Error("bridge.lifecycle-handler-failed");
                }
                var mutationIndex = 0;
                while (mutationIndex < patch.mutations.length) {
                    if (own(patch.mutations[mutationIndex], "sequence") && patch.mutations[mutationIndex].sequence !== mutationIndex) {
                        throw new Error("bridge.mutation-out-of-order");
                    }
                    applyDomMutation(patch.mutations[mutationIndex]);
                    mutationIndex += 1;
                }
                revision = patch.revision;
                lastAcknowledgedRevision = patch.revision;
                refreshDelegatedListeners();
                refreshOverlayListeners();
                restoreDomPreservation(captured);
                if (!invokeLifecycleNotifications(patch.lifecycleNotifications, false)) {
                    throw new Error("bridge.lifecycle-handler-failed");
                }
                return result(true, true, "");
            } catch (error) {
                revision = oldRevision;
                lastAcknowledgedRevision = oldRevision;
                subscriptionsById = subscriptionBackup;
                controlledInputsById = controlledInputBackup;
                compositionsById = compositionBackup;
                pendingControlledValuesById = pendingControlledBackup;
                overlaysById = overlayBackup;
                restoreDomBackup(backup);
                restoreBehaviorRegistrations(behaviorBackup);
                refreshOverlayListeners();
                refreshDelegatedListeners();
                restoreDomPreservation(rollbackPreservation);
                var rolledBack = failure("bridge.patch-rolled-back");
                rolledBack.requiresResync = true;
                rolledBack.detail = error && error.message ? String(error.message) : "mutation failed";
                return rolledBack;
            }
        }

        function hydratePublicSnapshot(snapshot, mode) {
            var hydrated = hydrateDom(clone(snapshot), mode || options.hydrationMode);
            if (hydrated.success) {
                state = "ready";
                handoff = clone(snapshot);
            }
            return hydrated;
        }

        function applyPublicDomPatch(patch) {
            var patched = applyDomPatch(clone(patch));
            if (!patched.success && patched.requiresResync && (state === "ready" || state === "resyncing")) {
                var requested = requestResync(patched.code);
                patched.resyncRequested = requested.success;
            }
            return patched;
        }

        function recordError(code) {
            lastError = code;
            return failure(code);
        }

        function sendEnvelope(message, trackAcknowledgement) {
            if (!transport || typeof transport.send !== "function") {
                return recordError("bridge.transport-unavailable");
            }
            if (trackAcknowledgement && pendingAcknowledgements.length >= options.maximumPendingAcknowledgements) {
                return recordError("bridge.acknowledgement-backpressure");
            }
            var sent = transport.send(clone(message));
            if (sent === false || (isObject(sent) && sent.success === false)) {
                state = "faulted";
                return recordError("bridge.transport-send-failed");
            }
            if (trackAcknowledgement) {
                pendingAcknowledgements.push(message.sequence);
            }
            return result(true, true, "");
        }

        function createEnvelope(kind, payload, correlationId) {
            var message = {
                version: negotiatedCapabilities ? negotiatedCapabilities.version : options.version,
                session: session,
                sequence: nextOutboundSequence,
                origin: options.localOrigin,
                kind: kind
            };
            nextOutboundSequence += 1;
            if (correlationId !== null && correlationId !== undefined) {
                message.correlationId = correlationId;
            }
            if (payload !== null && payload !== undefined) {
                message.payload = clone(payload);
            }
            return message;
        }

        function sendAcknowledgement(sequence) {
            var payload = taggedObject({ acknowledgement: taggedInteger(sequence) });
            return sendEnvelope(createEnvelope("acknowledgement", payload, null), false);
        }

        function addRetiredSession(identifier) {
            if (identifier && retiredSessions.indexOf(identifier) < 0) {
                retiredSessions.push(identifier);
            }
            while (retiredSessions.length > options.maximumRetiredSessions) {
                retiredSessions.shift();
            }
        }

        function dispatchMessage(message) {
            if (!options.onMessage) {
                return result(true, true, "");
            }
            var handled = options.onMessage(clone(message));
            if (handled === false || (isObject(handled) && handled.success === false)) {
                return recordError("bridge.handler-failed");
            }
            return result(true, true, "");
        }

        function requestResync(reason) {
            if (state !== "ready" && state !== "resyncing") {
                return recordError("bridge.invalid-state");
            }
            state = "resyncing";
            var correlationId = "resync-" + session + "-" + nextOutboundSequence;
            var request = createEnvelope("request", null, correlationId);
            request.request = {
                correlationId: correlationId,
                operation: "resync",
                arguments: {
                    reason: taggedString(reason || "revision-mismatch"),
                    revision: taggedInteger(lastAcknowledgedRevision)
                },
                timeoutMilliseconds: 0
            };
            return sendEnvelope(request, false);
        }

        function applyRevisionMessage(message) {
            if (message.kind === "renderSnapshot") {
                var snapshotRevision = readTaggedField(message.payload, "revision", "integer");
                if (!isInteger(snapshotRevision) || snapshotRevision < 0) {
                    return recordError("bridge.invalid-snapshot-revision");
                }
                var snapshot = decodeTaggedValue(message.payload);
                if (snapshot && Array.isArray(snapshot.roots) && domRoot) {
                    var hydration = hydrateDom(snapshot, snapshot.hydrationMode || options.hydrationMode);
                    if (!hydration.success) {
                        var hydrationResync = requestResync(hydration.code);
                        hydrationResync.requiresResync = true;
                        hydrationResync.code = hydration.code;
                        return hydrationResync;
                    }
                }
                revision = snapshotRevision;
                lastAcknowledgedRevision = snapshotRevision;
                handoff = clone(message.payload);
                state = "ready";
                return dispatchMessage(message);
            }
            if (message.kind === "domPatch") {
                var baseRevision = readTaggedField(message.payload, "baseRevision", "integer");
                var nextRevision = readTaggedField(message.payload, "revision", "integer");
                if (!isInteger(baseRevision) || !isInteger(nextRevision) || nextRevision <= baseRevision) {
                    return recordError("bridge.invalid-patch-revision");
                }
                if (baseRevision !== revision) {
                    var mismatch = requestResync("revision-mismatch");
                    mismatch.requiresResync = true;
                    return mismatch;
                }
                var patch = decodeTaggedValue(message.payload);
                if (patch && Array.isArray(patch.mutations) && domRoot) {
                    var patched = applyDomPatch(patch);
                    if (!patched.success) {
                        var patchResync = requestResync(patched.code);
                        patchResync.requiresResync = true;
                        patchResync.code = patched.code;
                        return patchResync;
                    }
                } else {
                    revision = nextRevision;
                    lastAcknowledgedRevision = nextRevision;
                }
                return dispatchMessage(message);
            }
            if (message.kind === "focus") {
                var focusResult = applyFocusRequest(decodeTaggedValue(message.payload));
                return focusResult.success ? dispatchMessage(message) : focusResult;
            }
            if (message.kind === "lifecycle") {
                var lifecycleNotification = decodeTaggedValue(message.payload);
                if (!isObject(lifecycleNotification) || !invokeLifecycleNotifications([lifecycleNotification], lifecycleNotification.stage === 3 || lifecycleNotification.stage === 9)) {
                    return failure("bridge.lifecycle-handler-failed");
                }
                return dispatchMessage(message);
            }
            return dispatchMessage(message);
        }

        function acceptInitialize(message) {
            if (message.sequence !== 0) {
                return recordError("bridge.invalid-handshake-sequence");
            }
            if (message.origin !== options.expectedOrigin) {
                return recordError("bridge.origin-mismatch");
            }
            if (retiredSessions.indexOf(message.session) >= 0) {
                return recordError("bridge.stale-session");
            }
            if (state === "ready" && message.session === session && readyEnvelope) {
                var replayed = sendEnvelope(readyEnvelope, false);
                replayed.duplicate = replayed.success;
                replayed.applied = false;
                return replayed;
            }
            var remote = parseHandshake(message.payload);
            if (!remote || remote.nonce.length > 256 || remote.version !== message.version) {
                return recordError("bridge.invalid-handshake-payload");
            }
            var wasReconnect = session.length > 0 && session !== message.session;
            if (wasReconnect) {
                state = "reconnecting";
                addRetiredSession(session);
            }
            session = message.session;
            nonce = remote.nonce;
            nextOutboundSequence = 0;
            lastInboundSequence = message.sequence;
            lastAcknowledgedSequence = -1;
            pendingAcknowledgements = [];
            negotiatedCapabilities = negotiateCapabilities(localCapabilities, remote);
            readyEnvelope = createEnvelope("ready", createHandshakePayload(nonce, negotiatedCapabilities), null);
            var ready = sendEnvelope(readyEnvelope, false);
            if (!ready.success) {
                return ready;
            }
            state = "ready";
            if (wasReconnect) {
                return requestResync("reconnect");
            }
            return result(true, true, "");
        }

        function parseIncoming(input) {
            if (typeof input !== "string") {
                return clone(input);
            }
            if (utf8Length(input) > options.maximumMessageLength) {
                return null;
            }
            try {
                return JSON.parse(input);
            } catch (error) {
                return null;
            }
        }

        function receive(input, metadata) {
            var message = parseIncoming(input);
            if (!message) {
                return recordError("bridge.invalid-json");
            }
            var validation = validateEnvelopeShape(message, options);
            if (validation) {
                return recordError(validation);
            }
            var frame = metadata && own(metadata, "isMainFrame") ? metadata.isMainFrame === true : true;
            if (options.requireMainFrame && !frame) {
                return recordError("bridge.frame-rejected");
            }
            if (message.kind === "initialize") {
                return acceptInitialize(message);
            }
            if (retiredSessions.indexOf(message.session) >= 0) {
                return recordError("bridge.stale-session");
            }
            if (state !== "ready" && state !== "resyncing" && state !== "closing") {
                return recordError("bridge.invalid-state");
            }
            if (message.session !== session) {
                return recordError("bridge.session-mismatch");
            }
            if (message.origin !== options.expectedOrigin) {
                return recordError("bridge.origin-mismatch");
            }
            if (message.sequence <= lastInboundSequence) {
                var duplicate = result(true, false, "");
                duplicate.duplicate = true;
                if (notificationKinds[message.kind]) {
                    sendAcknowledgement(message.sequence);
                }
                return duplicate;
            }
            if (message.sequence !== lastInboundSequence + 1) {
                return recordError("bridge.message-out-of-order");
            }
            lastInboundSequence = message.sequence;
            if (message.kind === "acknowledgement") {
                var acknowledged = readTaggedField(message.payload, "acknowledgement", "integer");
                if (!isInteger(acknowledged) || pendingAcknowledgements.indexOf(acknowledged) < 0) {
                    return recordError("bridge.unknown-acknowledgement");
                }
                var pendingIndex = pendingAcknowledgements.length - 1;
                while (pendingIndex >= 0) {
                    if (pendingAcknowledgements[pendingIndex] <= acknowledged) {
                        pendingAcknowledgements.splice(pendingIndex, 1);
                    }
                    pendingIndex -= 1;
                }
                lastAcknowledgedSequence = acknowledged;
                return result(true, true, "");
            }
            if (message.kind === "heartbeat") {
                if (own(message, "payload")) {
                    return recordError("bridge.unexpected-heartbeat-payload");
                }
                return result(true, true, "");
            }
            if (message.kind === "shutdown") {
                state = "closing";
                var shutdown = sendEnvelope(createEnvelope("shutdown", null, null), false);
                detachDelegatedListeners();
                detachOverlayListeners();
                disconnectObservers();
                if (transport && typeof transport.stop === "function") {
                    transport.stop();
                }
                state = "stopped";
                return shutdown;
            }
            var dispatched = applyRevisionMessage(message);
            if (notificationKinds[message.kind]) {
                var acknowledgedResult = sendAcknowledgement(message.sequence);
                if (!acknowledgedResult.success) {
                    return acknowledgedResult;
                }
            }
            return dispatched;
        }

        function start(nextTransport) {
            if (state !== "stopped") {
                return recordError("bridge.invalid-state");
            }
            if (!nextTransport || typeof nextTransport.start !== "function" || typeof nextTransport.send !== "function" || typeof nextTransport.stop !== "function") {
                return recordError("bridge.invalid-transport");
            }
            transport = nextTransport;
            state = "starting";
            var started = transport.start(function (message, metadata) {
                return receive(message, metadata);
            });
            if (started === false || (isObject(started) && started.success === false)) {
                state = "faulted";
                return recordError("bridge.transport-start-failed");
            }
            return result(true, true, "");
        }

        function send(kind, payload, correlationId) {
            if (state !== "ready") {
                return recordError("bridge.invalid-state");
            }
            if (!notificationKinds[kind]) {
                return recordError("bridge.invalid-notification-kind");
            }
            if (!validatePayload(payload, options)) {
                return recordError("bridge.invalid-payload");
            }
            if (pendingAcknowledgements.length >= options.maximumPendingAcknowledgements) {
                return recordError("bridge.acknowledgement-backpressure");
            }
            return sendEnvelope(createEnvelope(kind, payload, correlationId), true);
        }

        function acknowledge(sequence) {
            if ((state !== "ready" && state !== "resyncing") || !isInteger(sequence) || sequence < 0 || sequence > lastInboundSequence) {
                return recordError("bridge.invalid-acknowledgement");
            }
            return sendAcknowledgement(sequence);
        }

        function stop() {
            if (state === "stopped") {
                return result(true, false, "");
            }
            state = "closing";
            detachDelegatedListeners();
            detachOverlayListeners();
            disconnectObservers();
            var stopped = transport && typeof transport.stop === "function" ? transport.stop() : true;
            if (stopped === false || (isObject(stopped) && stopped.success === false)) {
                state = "faulted";
                return recordError("bridge.transport-stop-failed");
            }
            state = "stopped";
            return result(true, true, "");
        }

        function getState() {
            return clone({
                state: state,
                session: session,
                nonce: nonce,
                nextOutboundSequence: nextOutboundSequence,
                lastInboundSequence: lastInboundSequence,
                lastAcknowledgedSequence: lastAcknowledgedSequence,
                revision: revision,
                lastAcknowledgedRevision: lastAcknowledgedRevision,
                pendingAcknowledgementCount: pendingAcknowledgements.length,
                subscriptionCount: Object.keys(subscriptionsById).length,
                controlledInputCount: Object.keys(controlledInputsById).length,
                compositionCount: Object.keys(compositionsById).length,
                overlayCount: Object.keys(overlaysById).length,
                resizeObservationCount: Object.keys(resizeObservedById).length,
                intersectionObservationCount: Object.keys(intersectionObservedById).length,
                virtualizerCount: Object.keys(virtualizersById).length,
                capabilities: negotiatedCapabilities || localCapabilities,
                handoff: handoff,
                lastError: lastError
            });
        }

        return {
            start: start,
            receive: receive,
            send: send,
            acknowledge: acknowledge,
            requestResync: requestResync,
            createSnapshot: createDomSnapshot,
            hydrate: hydratePublicSnapshot,
            applyPatch: applyPublicDomPatch,
            dispatchEvent: dispatchNativeEvent,
            applyFocus: applyFocusRequest,
            setSelection: applySelectionRequest,
            scroll: applyScrollRequest,
            setOverlay: applyOverlayRequest,
            observeResize: observeResize,
            observeIntersection: observeIntersection,
            unobserve: unobserveNode,
            registerVirtualizer: registerVirtualizer,
            requestVirtualRange: requestVirtualRange,
            unregisterVirtualizer: unregisterVirtualizer,
            notifyLifecycle: notifyLifecycle,
            capturePreservation: captureDomPreservation,
            restorePreservation: restoreDomPreservation,
            stop: stop,
            getState: getState
        };
    }

    function bootstrap(transport, options) {
        var runtime = createRuntime(options);
        var environment = options && options.environment ? options.environment : root;
        runtime.start(transport || detectTransport(environment, options));
        return runtime;
    }

    var api = {
        version: VERSION,
        detectCapabilities: detectCapabilities,
        detectTransport: detectTransport,
        createWebView2Transport: createWebView2Transport,
        createWebKitTransport: createWebKitTransport,
        createAndroidTransport: createAndroidTransport,
        createMessagePortTransport: createMessagePortTransport,
        createPostMessageTransport: createPostMessageTransport,
        createLoopbackTransport: createLoopbackTransport,
        createRuntime: createRuntime,
        bootstrap: bootstrap
    };
    root.htmlgui = typeof Object.freeze === "function" ? Object.freeze(api) : api;
}(typeof globalThis === "object" ? globalThis : this));
