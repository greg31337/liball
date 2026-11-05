using System;

namespace html;

internal enum HtmlInsertionMode
{
    Initial = 0,
    BeforeHtml = 1,
    BeforeHead = 2,
    InHead = 3,
    InHeadNoScript = 4,
    AfterHead = 5,
    InBody = 6,
    Text = 7,
    InTable = 8,
    InTableText = 9,
    InCaption = 10,
    InColumnGroup = 11,
    InTableBody = 12,
    InRow = 13,
    InCell = 14,
    InSelect = 15,
    InSelectInTable = 16,
    InTemplate = 17,
    AfterBody = 18,
    InFrameset = 19,
    AfterFrameset = 20,
    AfterAfterBody = 21,
    AfterAfterFrameset = 22
}

internal enum HtmlTreeBuilderActionKind
{
    Consume = 0,
    Reprocess = 1,
    ProcessUsingMode = 2,
    Stop = 3
}

internal class HtmlTreeBuilderAction
{
    internal HtmlTreeBuilderActionKind kind;
    internal HtmlInsertionMode mode;

    internal HtmlTreeBuilderAction(HtmlTreeBuilderActionKind kind,
        HtmlInsertionMode mode)
    {
        this.kind = kind;
        this.mode = mode;
    }

    internal static HtmlTreeBuilderAction Consume()
    {
        return new HtmlTreeBuilderAction(HtmlTreeBuilderActionKind.Consume,
            HtmlInsertionMode.Initial);
    }

    internal static HtmlTreeBuilderAction Reprocess()
    {
        return new HtmlTreeBuilderAction(HtmlTreeBuilderActionKind.Reprocess,
            HtmlInsertionMode.Initial);
    }

    internal static HtmlTreeBuilderAction ProcessUsingMode(
        HtmlInsertionMode mode)
    {
        ValidateMode(mode);
        return new HtmlTreeBuilderAction(
            HtmlTreeBuilderActionKind.ProcessUsingMode, mode);
    }

    internal static HtmlTreeBuilderAction Stop()
    {
        return new HtmlTreeBuilderAction(HtmlTreeBuilderActionKind.Stop,
            HtmlInsertionMode.Initial);
    }

    internal HtmlTreeBuilderActionKind GetKind()
    {
        return this.kind;
    }

    internal HtmlInsertionMode GetMode()
    {
        return this.mode;
    }

    internal static void ValidateMode(HtmlInsertionMode mode)
    {
        if ((int)mode < (int)HtmlInsertionMode.Initial
            || (int)mode > (int)HtmlInsertionMode.AfterAfterFrameset)
        {
            throw new ArgumentOutOfRangeException("mode", (int)mode,
                "Unknown tree-builder insertion mode.");
        }
    }
}

internal class HtmlInsertionLocation
{
    internal HtmlNode parent;
    internal HtmlNode? beforeChild;

    internal HtmlInsertionLocation(HtmlNode? parent, HtmlNode? beforeChild)
    {
        if (parent == null)
        {
            throw new ArgumentNullException("parent");
        }

        HtmlNodeKind kind = parent.GetKindStorage();
        if (kind != HtmlNodeKind.Document && kind != HtmlNodeKind.Fragment
            && kind != HtmlNodeKind.Element)
        {
            throw new ArgumentException(
                "An insertion location requires a node that can contain children.",
                "parent");
        }

        if (beforeChild != null && beforeChild.GetParentStorage() != parent)
        {
            throw new ArgumentException(
                "The insertion reference must be a child of the location parent.",
                "beforeChild");
        }

        this.parent = parent;
        this.beforeChild = beforeChild;
    }

    internal HtmlNode GetParent()
    {
        return this.parent;
    }

    internal HtmlNode? GetBeforeChild()
    {
        return this.beforeChild;
    }
}

internal class HtmlTreeBuilder
{
    internal const int MaximumTokenDispatches = 64;

    internal HtmlDocument document;
    internal HtmlBudget budget;
    internal HtmlDiagnosticCollector diagnostics;
    internal HtmlOpenElementStack openElements;
    internal HtmlFormattingList formattingElements;
    internal Func<HtmlTreeBuilder, HtmlToken, HtmlTreeBuilderAction>[] modeHandlers;
    internal Func<HtmlTreeBuilder, HtmlToken, HtmlTreeBuilderAction>? foreignHandler;
    internal HtmlInsertionMode insertionMode;
    internal HtmlInsertionMode originalInsertionMode;
    internal HtmlNode? headElement;
    internal HtmlNode? formElement;
    internal bool scriptingEnabled;
    internal bool framesetOk;
    internal bool ignoreNextLineFeed;
    internal HtmlToken?[] pendingTableCharacters;
    internal int pendingTableCharacterCount;
    internal HtmlInsertionMode[] templateInsertionModes;
    internal int templateInsertionModeCount;
    internal HtmlTokenizerState requestedTokenizerState;
    internal bool tokenizerStateRequested;
    internal HtmlNode? rootInsertionTarget;
    internal bool fosterParenting;
    internal bool terminalLimitExceeded;
    internal bool limitDiagnosticReported;
    internal bool stopped;
    internal int lastDispatchCount;
    internal bool lastDispatchUsedForeignRules;
    internal bool[] visitedInsertionModes;
    internal bool[] visitedNamespaces;

    internal HtmlTreeBuilder(HtmlDocument document, HtmlBudget budget,
        HtmlNode? fragmentContext, HtmlNode? rootInsertionTarget)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        ValidateOptionalTarget(document, rootInsertionTarget,
            "rootInsertionTarget");
        this.document = document;
        this.budget = budget;
        this.diagnostics = new HtmlDiagnosticCollector(budget);
        this.openElements = new HtmlOpenElementStack(budget, fragmentContext);
        this.formattingElements = new HtmlFormattingList();
        this.modeHandlers = new Func<HtmlTreeBuilder, HtmlToken,
            HtmlTreeBuilderAction>[
                (int)HtmlInsertionMode.AfterAfterFrameset + 1];
        this.foreignHandler = null;
        this.insertionMode = HtmlInsertionMode.Initial;
        this.originalInsertionMode = HtmlInsertionMode.Initial;
        this.headElement = null;
        this.formElement = null;
        this.scriptingEnabled = false;
        this.framesetOk = true;
        this.ignoreNextLineFeed = false;
        this.pendingTableCharacters = new HtmlToken[8];
        this.pendingTableCharacterCount = 0;
        this.templateInsertionModes = new HtmlInsertionMode[8];
        this.templateInsertionModeCount = 0;
        this.requestedTokenizerState = HtmlTokenizerState.Data;
        this.tokenizerStateRequested = false;
        this.rootInsertionTarget = rootInsertionTarget;
        this.fosterParenting = false;
        this.terminalLimitExceeded = false;
        this.limitDiagnosticReported = false;
        this.stopped = false;
        this.lastDispatchCount = 0;
        this.lastDispatchUsedForeignRules = false;
        this.visitedInsertionModes = new bool[
            (int)HtmlInsertionMode.AfterAfterFrameset + 1];
        this.visitedNamespaces = new bool[(int)HtmlNamespace.Other + 1];
        HtmlTreeBuilderDocumentModes.Configure(this);
        HtmlTreeBuilderInBodyMode.Configure(this);
        HtmlTreeBuilderTableModes.Configure(this);
        HtmlTreeBuilderSelectModes.Configure(this);
        HtmlTreeBuilderTemplateModes.Configure(this);
        HtmlTreeBuilderForeignContent.Configure(this);
        if (!this.budget.TryConsume(HtmlLimitKind.Nodes, 1))
        {
            this.StopForLimit(HtmlDiagnosticCode.ResourceLimitExceeded,
                "The tree-builder node limit was exceeded.",
                document.GetRootStorage().GetSourceSpanStorage());
        }
    }

    internal HtmlDocument GetDocument()
    {
        return this.document;
    }

    internal HtmlOpenElementStack GetOpenElements()
    {
        return this.openElements;
    }

    internal HtmlFormattingList GetFormattingElements()
    {
        return this.formattingElements;
    }

    internal HtmlInsertionMode GetInsertionMode()
    {
        return this.insertionMode;
    }

    internal void SetInsertionMode(HtmlInsertionMode mode)
    {
        ValidateMode(mode);
        this.insertionMode = mode;
    }

    internal HtmlInsertionMode GetOriginalInsertionMode()
    {
        return this.originalInsertionMode;
    }

    internal void SetOriginalInsertionMode(HtmlInsertionMode mode)
    {
        ValidateMode(mode);
        this.originalInsertionMode = mode;
    }

    internal HtmlNode? GetHeadElement()
    {
        return this.headElement;
    }

    internal void SetHeadElement(HtmlNode element)
    {
        if (element != null)
        {
            if (element.GetOwnerDocumentStorage() != this.document
                || element.GetKindStorage() != HtmlNodeKind.Element
                || element.GetNamespaceStorage() != HtmlNamespace.Html
                || element.GetLocalNameStorage() != "head")
            {
                throw new ArgumentException(
                    "The head pointer must reference an HTML head element in this document.",
                    "element");
            }
        }

        this.headElement = element;
    }

    internal HtmlNode? GetFormElement()
    {
        return this.formElement;
    }

    internal void SetFormElement(HtmlNode? element)
    {
        if (element != null)
        {
            if (element.GetOwnerDocumentStorage() != this.document
                || element.GetKindStorage() != HtmlNodeKind.Element
                || element.GetNamespaceStorage() != HtmlNamespace.Html
                || element.GetLocalNameStorage() != "form")
            {
                throw new ArgumentException(
                    "The form pointer must reference an HTML form element in this document.",
                    "element");
            }
        }

        this.formElement = element;
    }

    internal bool IsScriptingEnabled()
    {
        return this.scriptingEnabled;
    }

    internal void SetScriptingEnabled(bool value)
    {
        this.scriptingEnabled = value;
    }

    internal bool IsFramesetOk()
    {
        return this.framesetOk;
    }

    internal void SetFramesetOk(bool value)
    {
        this.framesetOk = value;
    }

    internal void SetIgnoreNextLineFeed(bool value)
    {
        this.ignoreNextLineFeed = value;
    }

    internal bool TakeIgnoreNextLineFeed()
    {
        bool result = this.ignoreNextLineFeed;
        this.ignoreNextLineFeed = false;
        return result;
    }

    internal void ClearPendingTableCharacters()
    {
        int index = 0;
        while (index < this.pendingTableCharacterCount)
        {
            this.pendingTableCharacters[index] = null;
            index = index + 1;
        }

        this.pendingTableCharacterCount = 0;
    }

    internal void AddPendingTableCharacter(HtmlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.GetKind() != HtmlTokenKind.Character)
        {
            throw new ArgumentException(
                "Only character tokens can be buffered for table text.",
                "token");
        }

        if (this.pendingTableCharacterCount
            == this.pendingTableCharacters.Length)
        {
            int capacity = BufferCapacity.GetNext(
                this.pendingTableCharacters.Length,
                this.pendingTableCharacterCount + 1, int.MaxValue);
            HtmlToken[] expanded = new HtmlToken[capacity];
            Array.Copy(this.pendingTableCharacters, expanded,
                this.pendingTableCharacterCount);
            this.pendingTableCharacters = expanded;
        }

        this.pendingTableCharacters[this.pendingTableCharacterCount] = token;
        this.pendingTableCharacterCount =
            this.pendingTableCharacterCount + 1;
    }

    internal HtmlToken[] GetPendingTableCharacters()
    {
        HtmlToken[] result = new HtmlToken[
            this.pendingTableCharacterCount];
        Array.Copy(this.pendingTableCharacters, result,
            this.pendingTableCharacterCount);
        return result;
    }

    internal int GetTemplateInsertionModeCount()
    {
        return this.templateInsertionModeCount;
    }

    internal HtmlInsertionMode[] GetTemplateInsertionModes()
    {
        HtmlInsertionMode[] result = new HtmlInsertionMode[
            this.templateInsertionModeCount];
        Array.Copy(this.templateInsertionModes, result,
            this.templateInsertionModeCount);
        return result;
    }

    internal HtmlInsertionMode GetCurrentTemplateInsertionMode()
    {
        if (this.templateInsertionModeCount == 0)
        {
            throw new InvalidOperationException(
                "The template insertion-mode stack is empty.");
        }

        return this.templateInsertionModes[
            this.templateInsertionModeCount - 1];
    }

    internal void PushTemplateInsertionMode(HtmlInsertionMode mode)
    {
        ValidateMode(mode);
        if (this.templateInsertionModeCount
            == this.templateInsertionModes.Length)
        {
            int capacity = BufferCapacity.GetNext(
                this.templateInsertionModes.Length,
                this.templateInsertionModeCount + 1, int.MaxValue);
            HtmlInsertionMode[] expanded = new HtmlInsertionMode[capacity];
            Array.Copy(this.templateInsertionModes, expanded,
                this.templateInsertionModeCount);
            this.templateInsertionModes = expanded;
        }

        this.templateInsertionModes[this.templateInsertionModeCount] = mode;
        this.templateInsertionModeCount =
            this.templateInsertionModeCount + 1;
    }

    internal HtmlInsertionMode PopTemplateInsertionMode()
    {
        if (this.templateInsertionModeCount == 0)
        {
            throw new InvalidOperationException(
                "The template insertion-mode stack is empty.");
        }

        this.templateInsertionModeCount =
            this.templateInsertionModeCount - 1;
        HtmlInsertionMode result = this.templateInsertionModes[
            this.templateInsertionModeCount];
        this.templateInsertionModes[this.templateInsertionModeCount] =
            HtmlInsertionMode.Initial;
        return result;
    }

    internal void ReplaceCurrentTemplateInsertionMode(
        HtmlInsertionMode mode)
    {
        ValidateMode(mode);
        if (this.templateInsertionModeCount == 0)
        {
            throw new InvalidOperationException(
                "The template insertion-mode stack is empty.");
        }

        this.templateInsertionModes[
            this.templateInsertionModeCount - 1] = mode;
    }

    internal void ResetInsertionModeAppropriately()
    {
        int index = this.openElements.GetCount() - 1;
        while (index >= 0)
        {
            bool last = index == 0;
            HtmlNode node = this.openElements.GetRequiredNode(index);
            if (last && this.openElements.GetFragmentContext() != null)
            {
                HtmlNode? context = this.openElements.GetFragmentContext();
                if (context != null)
                {
                    node = context;
                }
            }

            if (node.GetNamespaceStorage() == HtmlNamespace.Html)
            {
                string name = node.GetLocalNameStorage();
                if (name == "select")
                {
                    this.insertionMode = this.GetSelectResetMode(index);
                    return;
                }

                if (name == "template")
                {
                    this.insertionMode = this.templateInsertionModeCount > 0
                        ? this.GetCurrentTemplateInsertionMode()
                        : HtmlInsertionMode.InTemplate;
                    return;
                }

                HtmlInsertionMode mode = GetResetMode(name,
                    this.headElement == null, last);
                if (mode != HtmlInsertionMode.Initial)
                {
                    this.insertionMode = mode;
                    return;
                }
            }

            if (last)
            {
                this.insertionMode = HtmlInsertionMode.InBody;
                return;
            }

            index = index - 1;
        }

        this.insertionMode = HtmlInsertionMode.InBody;
    }

    internal bool HasTokenizerStateRequest()
    {
        return this.tokenizerStateRequested;
    }

    internal HtmlTokenizerState GetRequestedTokenizerState()
    {
        if (!this.tokenizerStateRequested)
        {
            throw new InvalidOperationException(
                "No tokenizer-state change has been requested.");
        }

        return this.requestedTokenizerState;
    }

    internal void RequestTokenizerState(HtmlTokenizerState state)
    {
        if ((int)state < (int)HtmlTokenizerState.Data
            || (int)state > (int)HtmlTokenizerState.ScriptDataDoubleEscapeEnd)
        {
            throw new ArgumentOutOfRangeException("state", (int)state,
                "Unknown tokenizer state.");
        }

        this.requestedTokenizerState = state;
        this.tokenizerStateRequested = true;
    }

    internal void ClearTokenizerStateRequest()
    {
        this.requestedTokenizerState = HtmlTokenizerState.Data;
        this.tokenizerStateRequested = false;
    }

    internal bool IsFosterParentingEnabled()
    {
        return this.fosterParenting;
    }

    internal void SetFosterParenting(bool value)
    {
        this.fosterParenting = value;
    }

    internal bool HasTerminalLimitExceeded()
    {
        return this.terminalLimitExceeded;
    }

    internal bool IsStopped()
    {
        return this.stopped;
    }

    internal int GetLastDispatchCount()
    {
        return this.lastDispatchCount;
    }

    internal bool DidLastDispatchUseForeignRules()
    {
        return this.lastDispatchUsedForeignRules;
    }

    internal HtmlInsertionMode[] GetVisitedInsertionModes()
    {
        int count = 0;
        int index = 0;
        while (index < this.visitedInsertionModes.Length)
        {
            if (this.visitedInsertionModes[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        HtmlInsertionMode[] result = new HtmlInsertionMode[count];
        index = 0;
        int resultIndex = 0;
        while (index < this.visitedInsertionModes.Length)
        {
            if (this.visitedInsertionModes[index])
            {
                result[resultIndex] = (HtmlInsertionMode)index;
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal HtmlNamespace[] GetVisitedNamespaces()
    {
        int count = 0;
        int index = 0;
        while (index < this.visitedNamespaces.Length)
        {
            if (this.visitedNamespaces[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        HtmlNamespace[] result = new HtmlNamespace[count];
        index = 0;
        int resultIndex = 0;
        while (index < this.visitedNamespaces.Length)
        {
            if (this.visitedNamespaces[index])
            {
                result[resultIndex] = (HtmlNamespace)index;
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal HtmlDiagnostic[] GetDiagnostics()
    {
        return this.diagnostics.GetDiagnostics();
    }

    internal bool WereDiagnosticsTruncated()
    {
        return this.diagnostics.WasTruncated();
    }

    internal long GetDroppedDiagnosticCount()
    {
        return this.diagnostics.GetDroppedCount();
    }

    internal void SetModeHandler(HtmlInsertionMode mode,
        Func<HtmlTreeBuilder, HtmlToken, HtmlTreeBuilderAction> handler)
    {
        ValidateMode(mode);
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        this.modeHandlers[(int)mode] = handler;
    }

    internal void SetForeignHandler(
        Func<HtmlTreeBuilder, HtmlToken, HtmlTreeBuilderAction> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        this.foreignHandler = handler;
    }

    internal bool ProcessToken(HtmlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        this.budget.CheckCancellation();
        if (this.terminalLimitExceeded)
        {
            return false;
        }

        if (this.stopped)
        {
            throw new InvalidOperationException(
                "The tree builder has already stopped.");
        }

        bool forceMode = false;
        HtmlInsertionMode forcedMode = HtmlInsertionMode.Initial;
        this.lastDispatchCount = 0;
        this.lastDispatchUsedForeignRules = false;
        bool running = true;
        while (running)
        {
            this.budget.CheckCancellation();
            this.lastDispatchCount = this.lastDispatchCount + 1;
            if (this.lastDispatchCount > MaximumTokenDispatches)
            {
                this.StopForLimit(HtmlDiagnosticCode.OperationIncomplete,
                    "Tree-builder token reprocessing did not converge.",
                    token.GetSourceSpan());
                return false;
            }

            Func<HtmlTreeBuilder, HtmlToken, HtmlTreeBuilderAction>? handler;
            HtmlInsertionMode dispatchMode = forceMode
                ? forcedMode : this.insertionMode;
            this.RecordDispatch(dispatchMode);
            if (forceMode)
            {
                handler = this.modeHandlers[(int)forcedMode];
            }
            else if (this.ShouldUseHtmlRules(token))
            {
                handler = this.modeHandlers[(int)this.insertionMode];
            }
            else
            {
                handler = this.foreignHandler;
                this.lastDispatchUsedForeignRules = true;
            }

            if (handler == null)
            {
                throw new InvalidOperationException(
                    "The selected tree-builder rules have not been configured.");
            }

            HtmlTreeBuilderAction action = handler(this, token);
            if (action == null)
            {
                throw new InvalidOperationException(
                    "A tree-builder rule must return an action.");
            }

            HtmlTreeBuilderActionKind kind = action.GetKind();
            if (kind == HtmlTreeBuilderActionKind.Consume)
            {
                return true;
            }

            if (kind == HtmlTreeBuilderActionKind.Stop)
            {
                this.stopped = true;
                return true;
            }

            if (kind == HtmlTreeBuilderActionKind.ProcessUsingMode)
            {
                forceMode = true;
                forcedMode = action.GetMode();
            }
            else if (kind == HtmlTreeBuilderActionKind.Reprocess)
            {
                forceMode = false;
            }
            else
            {
                throw new InvalidOperationException(
                    "Unknown tree-builder dispatch action.");
            }
        }

        return false;
    }

    internal void RecordDispatch(HtmlInsertionMode mode)
    {
        this.visitedInsertionModes[(int)mode] = true;
        HtmlNode? adjusted = this.openElements.GetAdjustedCurrentNode();
        HtmlNamespace namespaceKind = adjusted == null
            ? HtmlNamespace.Html : adjusted.GetNamespaceStorage();
        this.visitedNamespaces[(int)namespaceKind] = true;
    }

    internal bool ReportParseError(HtmlToken token, string message)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        return this.ReportParseErrorAtSpan(token.GetSourceSpan(), message);
    }

    internal bool ReportParseErrorAtSpan(HtmlSourceSpan span, string message)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        return this.diagnostics.AddDetails(
            HtmlDiagnosticCode.TreeBuilderUnexpectedToken,
            HtmlDiagnosticSeverity.Error, message, span);
    }

    internal HtmlInsertionLocation GetAdjustedInsertionLocation()
    {
        return this.GetAdjustedInsertionLocationForTarget(null);
    }

    internal HtmlInsertionLocation GetAdjustedInsertionLocationForTarget(
        HtmlNode? overrideTarget)
    {
        if (overrideTarget != null)
        {
            ValidateOptionalTarget(this.document, overrideTarget,
                "overrideTarget");
        }

        HtmlNode? target = overrideTarget;
        if (target == null)
        {
            target = this.openElements.GetCurrentNode();
        }

        if (target == null)
        {
            target = this.document.GetRootStorage();
        }

        HtmlInsertionLocation location;
        if (this.fosterParenting && IsTableInsertionTarget(target))
        {
            location = this.GetFosterParentLocation();
        }
        else
        {
            location = new HtmlInsertionLocation(
                GetTemplateContentsTarget(target), null);
        }

        if (this.rootInsertionTarget != null
            && this.openElements.GetCount() > 0
            && location.GetParent() == this.openElements.GetNode(0))
        {
            location = new HtmlInsertionLocation(
                this.rootInsertionTarget, null);
        }

        return location;
    }

    internal HtmlNode? CreateHtmlElement(HtmlToken token)
    {
        ValidateStartTag(token);
        if (!this.TryConsumeNode(token.GetSourceSpan()))
        {
            return null;
        }

        if (token.GetName() == "template")
        {
            HtmlNode template = this.document.CreateTemplateStorage(
                token.GetAttributes(), token.GetSourceSpan());
            template.SetSourceBackedStorage(true);
            return template;
        }

        HtmlNode element = this.document.CreateElementStorage(HtmlNamespace.Html,
            HtmlNamespaceData.GetUri(HtmlNamespace.Html), token.GetName(),
            token.GetName(), "", token.GetAttributes(),
            token.GetSourceSpan());
        element.SetSourceBackedStorage(true);
        return element;
    }

    internal HtmlNode? CreateGeneratedHtmlElement(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (localName.Length == 0)
        {
            throw new ArgumentException(
                "A generated element name cannot be empty.", "localName");
        }

        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        if (!this.TryConsumeNode(span))
        {
            return null;
        }

        if (localName == "template")
        {
            return this.document.CreateTemplateStorage(
                new HtmlAttribute[0], span);
        }

        return this.document.CreateElementStorage(HtmlNamespace.Html,
            HtmlNamespaceData.GetUri(HtmlNamespace.Html), localName,
            localName, "", new HtmlAttribute[0], span);
    }

    internal HtmlNode? CreateElement(HtmlToken token,
        HtmlNamespace namespaceKind, string namespaceUri, string name,
        string localName, string prefix)
    {
        ValidateStartTag(token);
        if (!this.TryConsumeNode(token.GetSourceSpan()))
        {
            return null;
        }

        HtmlNode element = this.document.CreateElementStorage(namespaceKind,
            namespaceUri,
            name, localName, prefix, token.GetAttributes(),
            token.GetSourceSpan());
        element.SetSourceBackedStorage(true);
        return element;
    }

    internal HtmlNode? CreateDoctype(HtmlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.GetKind() != HtmlTokenKind.Doctype)
        {
            throw new ArgumentException(
                "A document type requires a DOCTYPE token.", "token");
        }

        if (!this.TryConsumeNode(token.GetSourceSpan()))
        {
            return null;
        }

        HtmlNode doctype = this.document.CreateDocumentTypeStorage(
            token.HasName(),
            token.GetName(), token.HasPublicIdentifier(),
            token.GetPublicIdentifier(), token.HasSystemIdentifier(),
            token.GetSystemIdentifier(), token.IsForceQuirks(),
            token.GetSourceSpan());
        doctype.SetSourceBackedStorage(true);
        return doctype;
    }

    internal HtmlNode? InsertHtmlElement(HtmlToken token)
    {
        return this.InsertHtmlElementWithStackOption(token, false);
    }

    internal HtmlNode? InsertHtmlElementWithStackOption(HtmlToken token, bool onlyAddToStack)
    {
        ValidateStartTag(token);
        if (!this.EnsureOpenElementDepth(token.GetSourceSpan()))
        {
            return null;
        }

        HtmlNode? element = this.CreateHtmlElement(token);
        if (element == null)
        {
            return null;
        }

        if (!onlyAddToStack)
        {
            this.InsertNodeAtLocation(element,
                this.GetAdjustedInsertionLocation());
        }

        if (!this.openElements.Push(element))
        {
            this.StopForLimit(HtmlDiagnosticCode.TreeBuilderDepthExceeded,
                "The tree-builder depth limit was exceeded.",
                token.GetSourceSpan());
            return null;
        }

        return element;
    }

    internal HtmlNode? CreateAndInsertHtmlElement(HtmlToken token)
    {
        HtmlNode? element = this.CreateHtmlElement(token);
        if (element == null)
        {
            return null;
        }

        this.InsertNodeAtLocation(element,
            this.GetAdjustedInsertionLocation());
        return element;
    }

    internal HtmlNode? InsertElement(HtmlToken token,
        HtmlNamespace namespaceKind, string namespaceUri, string name,
        string localName, string prefix, bool onlyAddToStack)
    {
        ValidateStartTag(token);
        if (!this.EnsureOpenElementDepth(token.GetSourceSpan()))
        {
            return null;
        }

        HtmlNode? element = this.CreateElement(token, namespaceKind,
            namespaceUri, name, localName, prefix);
        if (element == null)
        {
            return null;
        }

        if (!onlyAddToStack)
        {
            this.InsertNodeAtLocation(element,
                this.GetAdjustedInsertionLocation());
        }

        if (!this.openElements.Push(element))
        {
            this.StopForLimit(HtmlDiagnosticCode.TreeBuilderDepthExceeded,
                "The tree-builder depth limit was exceeded.",
                token.GetSourceSpan());
            return null;
        }

        return element;
    }

    internal HtmlNode? InsertGeneratedHtmlElement(string localName)
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        if (!this.EnsureOpenElementDepth(span))
        {
            return null;
        }

        HtmlNode? element = this.CreateGeneratedHtmlElement(localName);
        if (element == null)
        {
            return null;
        }

        this.InsertNodeAtLocation(element,
            this.GetAdjustedInsertionLocation());
        if (!this.openElements.Push(element))
        {
            this.StopForLimit(HtmlDiagnosticCode.TreeBuilderDepthExceeded,
                "The tree-builder depth limit was exceeded.", span);
            return null;
        }

        return element;
    }

    internal bool PushOpenElement(HtmlNode element, HtmlSourceSpan span)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (!this.EnsureOpenElementDepth(span))
        {
            return false;
        }

        if (!this.openElements.Push(element))
        {
            this.StopForLimit(HtmlDiagnosticCode.TreeBuilderDepthExceeded,
                "The tree-builder depth limit was exceeded.", span);
            return false;
        }

        return true;
    }

    internal bool ReconstructActiveFormattingElements(HtmlSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        bool result = this.formattingElements.Reconstruct(
            this.openElements, token =>
                this.CreateAndInsertHtmlElement(token));
        if (!result && this.openElements.HasExceededDepth())
        {
            this.StopForLimit(HtmlDiagnosticCode.TreeBuilderDepthExceeded,
                "The tree-builder depth limit was exceeded.", span);
        }

        return result;
    }

    internal HtmlNode? InsertCharacter(HtmlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.GetKind() != HtmlTokenKind.Character)
        {
            throw new ArgumentException(
                "Character insertion requires a character token.", "token");
        }

        return this.InsertCharacterData(token.GetData(), token.GetSourceSpan());
    }

    internal HtmlNode? InsertCharacterData(string data, HtmlSourceSpan span)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (data.Length == 0)
        {
            return null;
        }

        HtmlInsertionLocation location = this.GetAdjustedInsertionLocation();
        HtmlNode parent = location.GetParent();
        if (parent.GetKindStorage() == HtmlNodeKind.Document)
        {
            return null;
        }

        HtmlNode? beforeChild = location.GetBeforeChild();
        HtmlNode? previous = beforeChild == null
            ? parent.GetLastChildStorage()
            : beforeChild.GetPreviousSiblingStorage();
        if (previous != null
            && previous.GetKindStorage() == HtmlNodeKind.Text)
        {
            previous.AppendTextDataStorage(data, span);
            previous.SetSourceBackedStorage(true);
            return previous;
        }

        if (!this.TryConsumeNode(span))
        {
            return null;
        }

        HtmlNode text = this.document.CreateTextStorage(data, span);
        text.SetSourceBackedStorage(true);
        this.InsertNodeAtLocation(text, location);
        return text;
    }

    internal HtmlNode? InsertComment(HtmlToken token)
    {
        return this.InsertCommentAtTarget(token, null);
    }

    internal HtmlNode? InsertCommentAtTarget(HtmlToken token, HtmlNode? overrideTarget)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.GetKind() != HtmlTokenKind.Comment)
        {
            throw new ArgumentException(
                "Comment insertion requires a comment token.", "token");
        }

        if (!this.TryConsumeNode(token.GetSourceSpan()))
        {
            return null;
        }

        HtmlNode comment = this.document.CreateCommentStorage(
            token.GetData(), token.GetSourceSpan());
        comment.SetSourceBackedStorage(true);
        this.InsertNodeAtLocation(comment,
            this.GetAdjustedInsertionLocationForTarget(overrideTarget));
        return comment;
    }

    internal HtmlNode? InsertDoctype(HtmlToken token)
    {
        HtmlNode? doctype = this.CreateDoctype(token);
        if (doctype == null)
        {
            return null;
        }

        this.InsertNodeAtLocation(doctype,
            new HtmlInsertionLocation(this.document.GetRootStorage(), null));
        return doctype;
    }

    internal bool InsertNodeAtLocation(HtmlNode node,
        HtmlInsertionLocation location)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (location == null)
        {
            throw new ArgumentNullException("location");
        }

        HtmlNode parent = location.GetParent();
        if (parent.GetKindStorage() == HtmlNodeKind.Document)
        {
            if (!CanInsertIntoDocument(parent, node,
                location.GetBeforeChild()))
            {
                return false;
            }
        }

        parent.InsertChildStorage(node, location.GetBeforeChild());
        return true;
    }

    internal bool MoveNodeAtLocation(HtmlNode node,
        HtmlInsertionLocation location)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (location == null)
        {
            throw new ArgumentNullException("location");
        }

        HtmlNode parent = location.GetParent();
        if (parent.GetKindStorage() == HtmlNodeKind.Document
            && !CanInsertIntoDocument(parent, node,
                location.GetBeforeChild()))
        {
            return false;
        }

        parent.ReparentChildStorage(node, location.GetBeforeChild());
        return true;
    }

    internal bool ShouldUseHtmlRules(HtmlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (this.openElements.GetCount() == 0)
        {
            return true;
        }

        HtmlNode? adjusted = this.openElements.GetAdjustedCurrentNode();
        if (adjusted == null
            || adjusted.GetNamespaceStorage() == HtmlNamespace.Html)
        {
            return true;
        }

        if (IsMathMlTextIntegrationPoint(adjusted))
        {
            if (token.GetKind() == HtmlTokenKind.Character)
            {
                return true;
            }

            if (token.GetKind() == HtmlTokenKind.StartTag
                && token.GetName() != "mglyph"
                && token.GetName() != "malignmark")
            {
                return true;
            }
        }

        if (adjusted.GetNamespaceStorage() == HtmlNamespace.MathMl
            && adjusted.GetLocalNameStorage() == "annotation-xml"
            && token.GetKind() == HtmlTokenKind.StartTag
            && token.GetName() == "svg")
        {
            return true;
        }

        if (IsHtmlIntegrationPoint(adjusted)
            && (token.GetKind() == HtmlTokenKind.StartTag
                || token.GetKind() == HtmlTokenKind.Character))
        {
            return true;
        }

        return token.GetKind() == HtmlTokenKind.EndOfFile;
    }

    internal bool IsCdataAllowed()
    {
        HtmlNode? adjusted = this.openElements.GetAdjustedCurrentNode();
        return adjusted != null
            && adjusted.GetNamespaceStorage() != HtmlNamespace.Html;
    }

    internal HtmlInsertionLocation GetFosterParentLocation()
    {
        HtmlNode? lastTemplate = this.openElements.GetLastHtmlElement(
            "template");
        HtmlNode? lastTable = this.openElements.GetLastHtmlElement("table");
        int templateIndex = lastTemplate == null ? -1
            : this.openElements.GetIndex(lastTemplate);
        int tableIndex = lastTable == null ? -1
            : this.openElements.GetIndex(lastTable);
        if (lastTemplate != null
            && (lastTable == null || templateIndex > tableIndex))
        {
            return new HtmlInsertionLocation(
                lastTemplate.GetTemplateContentStorage(), null);
        }

        if (lastTable == null)
        {
            HtmlNode first = this.openElements.GetCount() == 0
                ? this.document.GetRootStorage()
                : this.openElements.GetRequiredNode(0);
            return new HtmlInsertionLocation(
                GetTemplateContentsTarget(first), null);
        }

        HtmlNode? tableParent = lastTable.GetParentStorage();
        if (tableParent != null)
        {
            return new HtmlInsertionLocation(tableParent, lastTable);
        }

        if (tableIndex <= 0)
        {
            return new HtmlInsertionLocation(
                this.document.GetRootStorage(), null);
        }

        HtmlNode previous = this.openElements.GetRequiredNode(tableIndex - 1);
        return new HtmlInsertionLocation(
            GetTemplateContentsTarget(previous), null);
    }

    internal bool TryConsumeNode(HtmlSourceSpan span)
    {
        if (this.terminalLimitExceeded)
        {
            return false;
        }

        if (this.budget.TryConsume(HtmlLimitKind.Nodes, 1))
        {
            return true;
        }

        this.StopForLimit(HtmlDiagnosticCode.ResourceLimitExceeded,
            "The tree-builder node limit was exceeded.", span);
        return false;
    }

    internal bool EnsureOpenElementDepth(HtmlSourceSpan span)
    {
        if (this.openElements.CanPush())
        {
            return true;
        }

        this.StopForLimit(HtmlDiagnosticCode.TreeBuilderDepthExceeded,
            "The tree-builder depth limit was exceeded.", span);
        return false;
    }

    internal void StopForLimit(HtmlDiagnosticCode code, string message,
        HtmlSourceSpan span)
    {
        this.terminalLimitExceeded = true;
        if (this.limitDiagnosticReported)
        {
            return;
        }

        this.limitDiagnosticReported = true;
        this.diagnostics.AddDetails(code, HtmlDiagnosticSeverity.Error,
            message, span);
    }

    internal static bool IsTableInsertionTarget(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string localName = node.GetLocalNameStorage();
        return localName == "table" || localName == "tbody"
            || localName == "tfoot" || localName == "thead"
            || localName == "tr";
    }

    internal static bool CanInsertIntoDocument(HtmlNode documentNode, HtmlNode node,
        HtmlNode? beforeChild)
    {
        HtmlNodeKind nodeKind = node.GetKindStorage();
        if (nodeKind != HtmlNodeKind.Element
            && nodeKind != HtmlNodeKind.Comment
            && nodeKind != HtmlNodeKind.DocumentType)
        {
            return false;
        }

        HtmlNode[] children = documentNode.GetChildrenStorage();
        int insertionIndex = children.Length;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index] == beforeChild)
            {
                insertionIndex = index;
            }

            HtmlNodeKind childKind = children[index].GetKindStorage();
            if (nodeKind == HtmlNodeKind.Element
                    && childKind == HtmlNodeKind.Element
                || nodeKind == HtmlNodeKind.DocumentType
                    && childKind == HtmlNodeKind.DocumentType)
            {
                return false;
            }

            index = index + 1;
        }

        if (nodeKind == HtmlNodeKind.DocumentType)
        {
            index = 0;
            while (index < insertionIndex)
            {
                if (children[index].GetKindStorage() == HtmlNodeKind.Element)
                {
                    return false;
                }

                index = index + 1;
            }
        }
        else if (nodeKind == HtmlNodeKind.Element)
        {
            index = insertionIndex;
            while (index < children.Length)
            {
                if (children[index].GetKindStorage()
                    == HtmlNodeKind.DocumentType)
                {
                    return false;
                }

                index = index + 1;
            }
        }

        return true;
    }

    internal static HtmlNode GetTemplateContentsTarget(HtmlNode node)
    {
        HtmlNode? content = node.GetTemplateContentStorage();
        return content == null ? node : content;
    }

    internal static bool IsMathMlTextIntegrationPoint(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.MathMl)
        {
            return false;
        }

        string localName = node.GetLocalNameStorage();
        return localName == "mi" || localName == "mo"
            || localName == "mn" || localName == "ms"
            || localName == "mtext";
    }

    internal static bool IsHtmlIntegrationPoint(HtmlNode node)
    {
        HtmlNamespace namespaceKind = node.GetNamespaceStorage();
        string localName = node.GetLocalNameStorage();
        if (namespaceKind == HtmlNamespace.Svg)
        {
            return localName == "foreignObject" || localName == "desc"
                || localName == "title";
        }

        if (namespaceKind != HtmlNamespace.MathMl
            || localName != "annotation-xml")
        {
            return false;
        }

        string? encoding = node.GetAttribute("encoding");
        return encoding != null
            && (Ascii.EqualsIgnoreCase(encoding, "text/html")
                || Ascii.EqualsIgnoreCase(encoding,
                    "application/xhtml+xml"));
    }

    internal static void ValidateStartTag(HtmlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.GetKind() != HtmlTokenKind.StartTag)
        {
            throw new ArgumentException(
                "Element creation requires a start-tag token.", "token");
        }
    }

    internal static void ValidateOptionalTarget(HtmlDocument document,
        HtmlNode? target, string parameterName)
    {
        if (target == null)
        {
            return;
        }

        HtmlNodeKind kind = target.GetKindStorage();
        if (kind != HtmlNodeKind.Document && kind != HtmlNodeKind.Fragment
            && kind != HtmlNodeKind.Element)
        {
            throw new ArgumentException(
                "A tree-builder insertion target must be a container node.",
                parameterName);
        }

        if (target.GetOwnerDocumentStorage() != document)
        {
            throw new ArgumentException(
                "A tree-builder insertion target belongs to another document.",
                parameterName);
        }
    }

    internal static void ValidateMode(HtmlInsertionMode mode)
    {
        if ((int)mode < (int)HtmlInsertionMode.Initial
            || (int)mode > (int)HtmlInsertionMode.AfterAfterFrameset)
        {
            throw new ArgumentOutOfRangeException("mode", (int)mode,
                "Unknown tree-builder insertion mode.");
        }
    }

    internal static HtmlInsertionMode GetResetMode(string localName,
        bool headMissing, bool last)
    {
        if ((localName == "td" || localName == "th") && !last)
        {
            return HtmlInsertionMode.InCell;
        }

        if (localName == "tr")
        {
            return HtmlInsertionMode.InRow;
        }

        if (localName == "tbody" || localName == "thead"
            || localName == "tfoot")
        {
            return HtmlInsertionMode.InTableBody;
        }

        if (localName == "caption")
        {
            return HtmlInsertionMode.InCaption;
        }

        if (localName == "colgroup")
        {
            return HtmlInsertionMode.InColumnGroup;
        }

        if (localName == "table")
        {
            return HtmlInsertionMode.InTable;
        }

        if (localName == "head" && !last)
        {
            return HtmlInsertionMode.InHead;
        }

        if (localName == "body")
        {
            return HtmlInsertionMode.InBody;
        }

        if (localName == "frameset")
        {
            return HtmlInsertionMode.InFrameset;
        }

        if (localName == "html")
        {
            return headMissing ? HtmlInsertionMode.BeforeHead
                : HtmlInsertionMode.AfterHead;
        }

        return HtmlInsertionMode.Initial;
    }

    internal HtmlInsertionMode GetSelectResetMode(int selectIndex)
    {
        int index = selectIndex - 1;
        while (index >= 0)
        {
            HtmlNode node = this.openElements.GetRequiredNode(index);
            if (node.GetNamespaceStorage() == HtmlNamespace.Html)
            {
                string name = node.GetLocalNameStorage();
                if (name == "template")
                {
                    return HtmlInsertionMode.InSelect;
                }

                if (name == "table")
                {
                    return HtmlInsertionMode.InSelectInTable;
                }
            }

            index = index - 1;
        }

        return HtmlInsertionMode.InSelect;
    }
}
