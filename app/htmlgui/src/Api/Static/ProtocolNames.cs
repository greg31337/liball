namespace htmlgui;

/// <summary>Provides canonical DOM event names exchanged with the browser runtime.</summary>
public static class DomEventNames
{
    /// <summary>The <c>click</c> event.</summary>
    public const string Click = "click";
    /// <summary>The <c>dblclick</c> event.</summary>
    public const string DoubleClick = "dblclick";
    /// <summary>The <c>contextmenu</c> event.</summary>
    public const string ContextMenu = "contextmenu";
    /// <summary>The <c>pointerdown</c> event.</summary>
    public const string PointerDown = "pointerdown";
    /// <summary>The <c>pointerup</c> event.</summary>
    public const string PointerUp = "pointerup";
    /// <summary>The <c>pointermove</c> event.</summary>
    public const string PointerMove = "pointermove";
    /// <summary>The <c>pointerenter</c> event.</summary>
    public const string PointerEnter = "pointerenter";
    /// <summary>The <c>pointerleave</c> event.</summary>
    public const string PointerLeave = "pointerleave";
    /// <summary>The <c>pointercancel</c> event.</summary>
    public const string PointerCancel = "pointercancel";
    /// <summary>The <c>mousedown</c> event.</summary>
    public const string MouseDown = "mousedown";
    /// <summary>The <c>mouseup</c> event.</summary>
    public const string MouseUp = "mouseup";
    /// <summary>The <c>mousemove</c> event.</summary>
    public const string MouseMove = "mousemove";
    /// <summary>The <c>mouseenter</c> event.</summary>
    public const string MouseEnter = "mouseenter";
    /// <summary>The <c>mouseleave</c> event.</summary>
    public const string MouseLeave = "mouseleave";
    /// <summary>The <c>wheel</c> event.</summary>
    public const string Wheel = "wheel";
    /// <summary>The <c>keydown</c> event.</summary>
    public const string KeyDown = "keydown";
    /// <summary>The <c>keyup</c> event.</summary>
    public const string KeyUp = "keyup";
    /// <summary>The <c>focus</c> event.</summary>
    public const string Focus = "focus";
    /// <summary>The <c>blur</c> event.</summary>
    public const string Blur = "blur";
    /// <summary>The <c>focusin</c> event.</summary>
    public const string FocusIn = "focusin";
    /// <summary>The <c>focusout</c> event.</summary>
    public const string FocusOut = "focusout";
    /// <summary>The <c>beforeinput</c> event.</summary>
    public const string BeforeInput = "beforeinput";
    /// <summary>The <c>input</c> event.</summary>
    public const string Input = "input";
    /// <summary>The <c>change</c> event.</summary>
    public const string Change = "change";
    /// <summary>The <c>compositionstart</c> event.</summary>
    public const string CompositionStart = "compositionstart";
    /// <summary>The <c>compositionupdate</c> event.</summary>
    public const string CompositionUpdate = "compositionupdate";
    /// <summary>The <c>compositionend</c> event.</summary>
    public const string CompositionEnd = "compositionend";
    /// <summary>The <c>selectionchange</c> event.</summary>
    public const string SelectionChange = "selectionchange";
    /// <summary>The <c>submit</c> event.</summary>
    public const string Submit = "submit";
    /// <summary>The <c>reset</c> event.</summary>
    public const string Reset = "reset";
    /// <summary>The <c>dragstart</c> event.</summary>
    public const string DragStart = "dragstart";
    /// <summary>The <c>drag</c> event.</summary>
    public const string Drag = "drag";
    /// <summary>The <c>dragenter</c> event.</summary>
    public const string DragEnter = "dragenter";
    /// <summary>The <c>dragover</c> event.</summary>
    public const string DragOver = "dragover";
    /// <summary>The <c>dragleave</c> event.</summary>
    public const string DragLeave = "dragleave";
    /// <summary>The <c>drop</c> event.</summary>
    public const string Drop = "drop";
    /// <summary>The <c>dragend</c> event.</summary>
    public const string DragEnd = "dragend";
    /// <summary>The <c>copy</c> event.</summary>
    public const string Copy = "copy";
    /// <summary>The <c>cut</c> event.</summary>
    public const string Cut = "cut";
    /// <summary>The <c>paste</c> event.</summary>
    public const string Paste = "paste";
    /// <summary>The <c>scroll</c> event.</summary>
    public const string Scroll = "scroll";
    /// <summary>The <c>resize</c> event.</summary>
    public const string Resize = "resize";
    /// <summary>The <c>visibilitychange</c> event.</summary>
    public const string VisibilityChange = "visibilitychange";
    /// <summary>The <c>toggle</c> event.</summary>
    public const string Toggle = "toggle";
}

/// <summary>Provides canonical JSON member and token names used by the bridge protocol.</summary>
public static class BridgeProtocolNames
{
    /// <summary>The protocol-version member.</summary>
    public const string Version = "version";
    /// <summary>The session member.</summary>
    public const string Session = "session";
    /// <summary>The session nonce member.</summary>
    public const string Nonce = "nonce";
    /// <summary>The sequence-number member.</summary>
    public const string Sequence = "sequence";
    /// <summary>The correlation identifier member.</summary>
    public const string CorrelationId = "correlationId";
    /// <summary>The message-origin member.</summary>
    public const string Origin = "origin";
    /// <summary>The main-frame member.</summary>
    public const string MainFrame = "mainFrame";
    /// <summary>The message-kind member.</summary>
    public const string Kind = "kind";
    /// <summary>The payload member.</summary>
    public const string Payload = "payload";
    /// <summary>The capabilities member.</summary>
    public const string Capabilities = "capabilities";
    /// <summary>The revision member.</summary>
    public const string Revision = "revision";
    /// <summary>The base-revision member.</summary>
    public const string BaseRevision = "baseRevision";
    /// <summary>The acknowledgement member.</summary>
    public const string Acknowledgement = "acknowledgement";
    /// <summary>The success member.</summary>
    public const string Success = "success";
    /// <summary>The error member.</summary>
    public const string Error = "error";
    /// <summary>The error-code member.</summary>
    public const string ErrorCode = "errorCode";
    /// <summary>The error-message member.</summary>
    public const string ErrorMessage = "errorMessage";
    /// <summary>The chunk identifier member.</summary>
    public const string ChunkId = "chunkId";
    /// <summary>The chunk-index member.</summary>
    public const string ChunkIndex = "chunkIndex";
    /// <summary>The chunk-count member.</summary>
    public const string ChunkCount = "chunkCount";
    /// <summary>The chunk data member.</summary>
    public const string ChunkData = "chunkData";
    /// <summary>The checksum member.</summary>
    public const string Checksum = "checksum";
    /// <summary>The host endpoint origin token.</summary>
    public const string HostOrigin = "host";
    /// <summary>The browser endpoint origin token.</summary>
    public const string BrowserOrigin = "browser";
}

/// <summary>Provides supported numeric bridge protocol versions.</summary>
public static class BridgeProtocolVersions
{
    /// <summary>The earliest protocol version accepted by this library.</summary>
    public const int MinimumSupported = 1;
    /// <summary>The protocol version emitted by this library.</summary>
    public const int Current = 1;
}
