using System;
using htmlgui;

namespace htmlgui.Tests;

public class ExactHtmlCoverageTests
{
    [TestCase(UiLayoutKind.Flow, "flow", "div", "align-items:flex-start;display:block;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.Inline, "inline", "span", "align-items:flex-start;display:inline;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.VerticalStack, "vertical-stack", "div", "align-items:flex-start;display:flex;flex-direction:column;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.HorizontalStack, "horizontal-stack", "div", "align-items:flex-start;display:flex;flex-direction:row;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.FlexRow, "flex-row", "div", "align-items:flex-start;display:flex;flex-direction:row;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.FlexColumn, "flex-column", "div", "align-items:flex-start;display:flex;flex-direction:column;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.Wrap, "wrap", "div", "align-items:flex-start;display:flex;flex-wrap:wrap;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.Grid, "grid", "div", "align-items:flex-start;display:grid;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.UniformGrid, "uniform-grid", "div", "align-items:flex-start;display:grid;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.Dock, "dock", "div", "align-items:flex-start;display:grid;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.Overlay, "overlay", "div", "align-items:flex-start;display:grid;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.Absolute, "absolute", "div", "align-items:flex-start;justify-content:flex-start;position:relative;")]
    [TestCase(UiLayoutKind.Split, "split", "div", "align-items:flex-start;display:flex;flex-direction:row;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.Scroll, "scroll", "div", "align-items:flex-start;display:block;justify-content:flex-start;")]
    [TestCase(UiLayoutKind.ResponsiveShell, "responsive-shell", "div", "align-items:flex-start;display:grid;justify-content:flex-start;")]
    public void SerializesEveryLayoutKindExactly(UiLayoutKind kind, string token, string element, string style)
    {
        UiNode node = Node(UiNodeKind.Layout, "layout");
        node.Layout = new UiLayout
        {
            Kind = kind,
            Children = new UiNode[] { Text("child") }
        };

        string html = Serialize(node);

        Assert.That(html, Is.EqualTo("<" + element + " class=\"htmlgui-layout htmlgui-layout-" + token + "\" data-layout=\"" + token + "\" style=\"" + style + "\">child</" + element + ">"));
    }

    [TestCase(UiInputKind.Text, "text", "text", "value")]
    [TestCase(UiInputKind.Password, "password", "password", "value")]
    [TestCase(UiInputKind.Search, "search", "search", "value")]
    [TestCase(UiInputKind.Email, "email", "email", "value")]
    [TestCase(UiInputKind.Url, "url", "url", "value")]
    [TestCase(UiInputKind.Telephone, "telephone", "tel", "value")]
    [TestCase(UiInputKind.Number, "number", "number", "7")]
    [TestCase(UiInputKind.Range, "range", "range", "7")]
    [TestCase(UiInputKind.Date, "date", "date", "2026-09-15")]
    [TestCase(UiInputKind.Time, "time", "time", "12:30")]
    [TestCase(UiInputKind.Month, "month", "month", "2026-09")]
    [TestCase(UiInputKind.Week, "week", "week", "2026-W38")]
    [TestCase(UiInputKind.LocalDateTime, "local-date-time", "datetime-local", "2026-09-15T12:30")]
    [TestCase(UiInputKind.Color, "color", "color", "#123456")]
    [TestCase(UiInputKind.Hidden, "hidden", "hidden", "value")]
    public void SerializesEveryNativeScalarInputKindExactly(UiInputKind kind, string token, string type, string value)
    {
        UiNode node = Input(kind, value);

        string html = Serialize(node);

        Assert.That(html, Is.EqualTo("<input class=\"htmlgui-input htmlgui-input-" + token + "\" data-update-trigger=\"change\" name=\"field\" type=\"" + type + "\" value=\"" + value + "\">"));
    }

    [Test]
    public void SerializesMultilineFileAndReadOnlyInputKindsExactly()
    {
        UiNode multiline = Input(UiInputKind.Multiline, "line one\nline two");
        UiNode file = Input(UiInputKind.File, string.Empty);
        file.Input!.Value = new UiValue();
        UiNode readOnly = Input(UiInputKind.ReadOnly, "display");

        Assert.Multiple(() =>
        {
            Assert.That(Serialize(multiline), Is.EqualTo("<textarea class=\"htmlgui-input htmlgui-input-multiline\" data-update-trigger=\"change\" name=\"field\">line one\nline two</textarea>"));
            Assert.That(Serialize(file), Is.EqualTo("<input class=\"htmlgui-input htmlgui-input-file\" data-update-trigger=\"change\" name=\"field\" type=\"file\">"));
            Assert.That(Serialize(readOnly), Is.EqualTo("<output class=\"htmlgui-input htmlgui-input-read-only\" data-update-trigger=\"change\" name=\"field\">display</output>"));
        });
    }

    [TestCase(UiButtonKind.Push, "push", "button", "")]
    [TestCase(UiButtonKind.Submit, "submit", "submit", "")]
    [TestCase(UiButtonKind.Reset, "reset", "reset", "")]
    [TestCase(UiButtonKind.Toggle, "toggle", "button", " aria-pressed=\"false\"")]
    [TestCase(UiButtonKind.Command, "command", "button", " data-command-id=\"save\"")]
    public void SerializesOrdinaryButtonKindsExactly(UiButtonKind kind, string token, string type, string extra)
    {
        UiNode node = Node(UiNodeKind.Control, "button");
        node.Name = "button";
        node.Button = new UiButton { Kind = kind, Label = "Save", CommandId = kind == UiButtonKind.Command ? "save" : null };

        string html = Serialize(node);
        string attributes = " class=\"htmlgui-button htmlgui-button-" + token + "\" type=\"" + type + "\"";
        if (kind == UiButtonKind.Toggle)
        {
            attributes = extra + attributes;
        }
        else if (kind == UiButtonKind.Command)
        {
            attributes = " class=\"htmlgui-button htmlgui-button-" + token + "\"" + extra + " type=\"" + type + "\"";
        }

        Assert.That(html, Is.EqualTo("<button" + attributes + ">Save</button>"));
    }

    [Test]
    public void SerializesIconAndSplitButtonKindsExactly()
    {
        UiNode icon = Node(UiNodeKind.Control, "icon");
        icon.Name = "icon-button";
        icon.Button = new UiButton { Kind = UiButtonKind.Icon, Label = "Close", IconKey = "close" };
        UiNode split = Node(UiNodeKind.Control, "split");
        split.Name = "split-button";
        split.Button = new UiButton
        {
            Kind = UiButtonKind.Split,
            Label = "Export",
            Menu = new UiMenu { Key = "export", IsOpen = true }
        };

        Assert.Multiple(() =>
        {
            Assert.That(Serialize(icon), Is.EqualTo("<button aria-label=\"Close\" class=\"htmlgui-button htmlgui-button-icon\" type=\"button\"><span aria-hidden=\"true\" class=\"htmlgui-button-icon\" data-icon-key=\"close\"></span></button>"));
            Assert.That(Serialize(split), Is.EqualTo("<div class=\"htmlgui-split-button\" role=\"group\"><button class=\"htmlgui-split-primary\" type=\"button\">Export</button><button aria-expanded=\"true\" aria-haspopup=\"menu\" aria-label=\"Export menu\" class=\"htmlgui-split-toggle\" data-menu-key=\"export\" type=\"button\">▾</button></div>"));
        });
    }

    [TestCase(UiTextKind.Plain, "<span class=\"htmlgui-text\">Content</span>")]
    [TestCase(UiTextKind.Heading, "<h1 class=\"htmlgui-heading\">Content</h1>")]
    [TestCase(UiTextKind.Paragraph, "<p class=\"htmlgui-paragraph\">Content</p>")]
    [TestCase(UiTextKind.Label, "<label class=\"htmlgui-label\">Content</label>")]
    [TestCase(UiTextKind.Code, "<code class=\"htmlgui-code\">Content</code>")]
    [TestCase(UiTextKind.Preformatted, "<pre class=\"htmlgui-preformatted\">Content</pre>")]
    [TestCase(UiTextKind.Quote, "<blockquote class=\"htmlgui-quote\">Content</blockquote>")]
    [TestCase(UiTextKind.Link, "<a class=\"htmlgui-link\" href=\"/target\">Content</a>")]
    [TestCase(UiTextKind.Icon, "<span aria-hidden=\"true\" class=\"htmlgui-icon\">Content</span>")]
    [TestCase(UiTextKind.Image, "<img alt=\"Alternative\" class=\"htmlgui-image\" src=\"/image.png\">")]
    [TestCase(UiTextKind.Figure, "<figure class=\"htmlgui-figure\"><figcaption class=\"htmlgui-figure-caption\">Content</figcaption></figure>")]
    [TestCase(UiTextKind.Badge, "<span class=\"htmlgui-badge\">Content</span>")]
    [TestCase(UiTextKind.Chip, "<span class=\"htmlgui-chip\">Content</span>")]
    [TestCase(UiTextKind.Status, "<span class=\"htmlgui-status\" role=\"status\">Content</span>")]
    [TestCase(UiTextKind.Landmark, "<section class=\"htmlgui-landmark\" role=\"region\">Content</section>")]
    [TestCase(UiTextKind.Localized, "<span class=\"htmlgui-localized-text\" data-localization-key=\"catalog.item\">Content</span>")]
    public void SerializesEverySemanticTextControlExactly(UiTextKind kind, string expected)
    {
        UiNode node = Node(UiNodeKind.Control, "content");
        node.Name = "text";
        node.TextContent = new UiText
        {
            Kind = kind,
            Text = "Content",
            Url = kind == UiTextKind.Link ? "/target" : kind == UiTextKind.Image ? "/image.png" : null,
            AlternativeText = kind == UiTextKind.Image ? "Alternative" : null,
            LocalizationKey = kind == UiTextKind.Localized ? "catalog.item" : null
        };

        Assert.That(Serialize(node), Is.EqualTo(expected));
    }

    [Test]
    public void SerializesEveryChoiceControlConstructExactly()
    {
        string[] names = new string[]
        {
            "checkbox",
            "switch",
            "radio-group",
            "select",
            "multi-select",
            "listbox",
            "combo-box",
            "autocomplete",
            "editable-combo",
            "segmented-choice"
        };
        string[] expected = new string[]
        {
            "<input checked class=\"htmlgui-choice htmlgui-checkbox\" data-update-trigger=\"change\" name=\"field\" type=\"checkbox\" value=\"true\">",
            "<input aria-checked=\"true\" checked class=\"htmlgui-choice htmlgui-switch\" data-update-trigger=\"change\" name=\"field\" role=\"switch\" type=\"checkbox\" value=\"true\">",
            "<div class=\"htmlgui-choice-group htmlgui-radio-group\" data-field-name=\"field\" data-update-trigger=\"change\" role=\"radiogroup\"><label class=\"htmlgui-radio-choice\"><input checked class=\"htmlgui-radio-input\" name=\"field\" type=\"radio\" value=\"1\">One</label></div>",
            "<select class=\"htmlgui-choice htmlgui-select\" data-update-trigger=\"change\" name=\"field\"><option data-choice-key=\"one\" selected value=\"1\">One</option></select>",
            "<select class=\"htmlgui-choice htmlgui-multi-select\" data-update-trigger=\"change\" multiple name=\"field\"><option data-choice-key=\"one\" selected value=\"1\">One</option></select>",
            "<div class=\"htmlgui-choice htmlgui-listbox\" data-field-name=\"field\" data-update-trigger=\"change\" role=\"listbox\" tabindex=\"0\"><div aria-selected=\"true\" class=\"htmlgui-choice-option\" data-value=\"1\" role=\"option\" tabindex=\"-1\">One</div></div>",
            "<div class=\"htmlgui-choice htmlgui-combo-box\" data-field-name=\"field\"><button aria-controls=\"ui-choice-cf8061b7699088fe9b5b\" aria-expanded=\"false\" class=\"htmlgui-combo-trigger\" data-field-name=\"field\" data-update-trigger=\"change\" role=\"combobox\" type=\"button\">One</button><div class=\"htmlgui-combo-popup\" hidden role=\"listbox\"><div aria-selected=\"true\" class=\"htmlgui-choice-option\" data-value=\"1\" role=\"option\" tabindex=\"-1\">One</div></div></div>",
            "<div class=\"htmlgui-choice htmlgui-autocomplete\" data-field-name=\"field\"><input aria-autocomplete=\"list\" aria-controls=\"ui-choice-cf8061b7699088fe9b5b\" aria-expanded=\"false\" class=\"htmlgui-combo-editor\" data-update-trigger=\"change\" name=\"field\" role=\"combobox\" type=\"text\" value=\"1\"><div class=\"htmlgui-combo-popup\" hidden role=\"listbox\"><div aria-selected=\"true\" class=\"htmlgui-choice-option\" data-value=\"1\" role=\"option\" tabindex=\"-1\">One</div></div></div>",
            "<div class=\"htmlgui-choice htmlgui-editable-combo\" data-field-name=\"field\"><input aria-autocomplete=\"both\" aria-controls=\"ui-choice-cf8061b7699088fe9b5b\" aria-expanded=\"false\" class=\"htmlgui-combo-editor\" data-update-trigger=\"change\" name=\"field\" role=\"combobox\" type=\"text\" value=\"1\"><div class=\"htmlgui-combo-popup\" hidden role=\"listbox\"><div aria-selected=\"true\" class=\"htmlgui-choice-option\" data-value=\"1\" role=\"option\" tabindex=\"-1\">One</div></div></div>",
            "<div class=\"htmlgui-choice-group htmlgui-segmented-choice\" data-field-name=\"field\" role=\"radiogroup\"><button aria-checked=\"true\" class=\"htmlgui-segment\" data-update-trigger=\"change\" data-value=\"1\" role=\"radio\" tabindex=\"0\" type=\"button\">One</button></div>"
        };

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < names.Length)
            {
                UiNode node = Choice(names[index]);
                Assert.That(Serialize(node), Is.EqualTo(expected[index]), names[index]);
                index++;
            }
        });
    }

    [Test]
    public void SerializesCollectionTableTreeMenuTabsAndNavigationControlsExactly()
    {
        UiNode collection = Control("list");
        collection.Collection = new UiCollection
        {
            Key = "items",
            Items = new UiCollectionItem[]
            {
                new UiCollectionItem { Key = "one", Value = TextValue("Item") }
            }
        };
        UiNode table = Control("table");
        table.Table = new UiTable
        {
            Key = "people",
            Columns = new UiTableColumn[] { new UiTableColumn { Key = "name", Header = "Name" } },
            Rows = new UiTableRow[]
            {
                new UiTableRow
                {
                    Key = "greg",
                    Cells = new UiTableCell[] { new UiTableCell { ColumnKey = "name", Value = TextValue("Greg") } }
                }
            }
        };
        UiNode tree = Control("tree");
        tree.Tree = new UiTree
        {
            Key = "topics",
            Items = new UiTreeItem[] { new UiTreeItem { Key = "one", Label = "One" } }
        };
        UiNode menu = Control("menu");
        menu.Menu = new UiMenu
        {
            Key = "actions",
            Label = "Actions",
            Items = new UiMenuItem[]
            {
                new UiMenuItem { Key = "save", Kind = UiMenuItemKind.Command, Label = "Save", CommandId = "save" }
            }
        };
        UiNode tabs = Control("tabs");
        tabs.Tabs = new UiTabs
        {
            Key = "views",
            ActiveTabKey = "first",
            Tabs = new UiTab[]
            {
                new UiTab { Key = "first", Label = "First", IsSelected = true, Panel = Text("Panel") }
            }
        };
        UiNode toolbar = Control("toolbar");
        toolbar.Toolbar = new UiToolbar
        {
            Key = "tools",
            Label = "Tools",
            Tools = new UiTool[]
            {
                new UiTool { Key = "save", Kind = "command", Label = "Save", CommandId = "save" }
            }
        };
        UiNode breadcrumb = Control("breadcrumb");
        breadcrumb.Breadcrumb = new UiBreadcrumb
        {
            Key = "crumbs",
            HasCurrentIndex = true,
            CurrentIndex = 0,
            Items = new UiChoice[]
            {
                new UiChoice { Key = "home", Label = "Home", Url = "/", Value = TextValue("home") }
            }
        };
        UiNode pagination = Control("pagination");
        pagination.Pagination = new UiPagination
        {
            Key = "pages",
            PageIndex = 0,
            PageSize = 10,
            HasPageCount = true,
            PageCount = 2,
            HasNext = true
        };
        UiNode[] controls = new UiNode[] { collection, table, tree, menu, tabs, toolbar, breadcrumb, pagination };
        string[] names = new string[] { "collection", "table", "tree", "menu", "tabs", "toolbar", "breadcrumb", "pagination" };
        string[] expected = new string[]
        {
            "<ul class=\"htmlgui-list htmlgui-unordered-list\" data-collection-key=\"items\" data-collection-state=\"ready\" data-page-count=\"1\" data-page-end=\"false\" data-page-start=\"0\" data-selection-mode=\"none\"><li class=\"htmlgui-list-item\" data-item-key=\"one\">Item</li></ul>",
            "<table class=\"htmlgui-table\" data-selection-mode=\"none\" data-table-key=\"people\"><colgroup><col class=\"htmlgui-table-column\" data-column-key=\"name\"></colgroup><thead><tr class=\"htmlgui-table-header-row\"><th class=\"htmlgui-table-header\" data-column-key=\"name\" scope=\"col\">Name</th></tr></thead><tbody class=\"htmlgui-table-body\"><tr class=\"htmlgui-table-row\" data-row-key=\"greg\"><td class=\"htmlgui-table-cell\" data-column-key=\"name\">Greg</td></tr></tbody></table>",
            "<div class=\"htmlgui-tree\" data-range-count=\"1\" data-range-start=\"0\" data-selection-mode=\"none\" data-tree-key=\"topics\" data-typeahead-buffer=\"\" data-typeahead-generation=\"0\" data-typeahead-ignore-case=\"true\" role=\"tree\" tabindex=\"0\"><div aria-level=\"1\" aria-posinset=\"1\" aria-selected=\"false\" aria-setsize=\"1\" class=\"htmlgui-tree-item\" data-flat-index=\"0\" data-item-key=\"one\" role=\"treeitem\" tabindex=\"-1\">One</div></div>",
            "<div aria-label=\"Actions\" aria-orientation=\"vertical\" class=\"htmlgui-menu htmlgui-menu-popup\" data-dismiss-escape=\"true\" data-dismiss-outside=\"true\" data-menu-key=\"actions\" data-menu-kind=\"popup\" data-open=\"false\" data-typeahead-buffer=\"\" data-typeahead-generation=\"0\" data-typeahead-ignore-case=\"true\" data-wrap-focus=\"true\" hidden role=\"menu\"><div class=\"htmlgui-menu-item htmlgui-menu-item-command\" data-command-id=\"save\" data-item-key=\"save\" role=\"menuitem\" tabindex=\"0\"><span class=\"htmlgui-menu-label\">Save</span></div></div>",
            "<div class=\"htmlgui-tabs\" data-automatic-activation=\"true\" data-tabs-key=\"views\"><div aria-orientation=\"horizontal\" class=\"htmlgui-tab-list\" role=\"tablist\"><button aria-controls=\"ui-tabs-c3df0915006639204bfb\" aria-selected=\"true\" class=\"htmlgui-tab\" data-tab-key=\"first\" role=\"tab\" tabindex=\"0\" type=\"button\"><span class=\"htmlgui-tab-label\">First</span></button></div><div aria-labelledby=\"ui-tabs-6c9c686466b95bab68e4\" class=\"htmlgui-tab-panel\" data-lazy=\"false\" data-materialized=\"true\" data-tab-key=\"first\" role=\"tabpanel\" tabindex=\"0\">Panel</div></div>",
            "<div aria-label=\"Tools\" aria-orientation=\"horizontal\" class=\"htmlgui-toolbar\" data-toolbar-key=\"tools\" role=\"toolbar\"><button class=\"htmlgui-tool htmlgui-tool-command\" data-command-id=\"save\" data-tool-key=\"save\" tabindex=\"0\" type=\"button\">Save</button></div>",
            "<nav aria-label=\"Breadcrumb\" class=\"htmlgui-navigation htmlgui-navigation-breadcrumb\" data-navigation-key=\"crumbs\" data-navigation-kind=\"breadcrumb\"><ol class=\"htmlgui-navigation-items\"><li class=\"htmlgui-navigation-item\"><a aria-current=\"page\" class=\"htmlgui-navigation-link\" data-item-key=\"home\" href=\"/\">Home</a></li></ol></nav>",
            "<nav aria-label=\"Pagination\" class=\"htmlgui-pagination\" data-page-count=\"2\" data-page-index=\"0\" data-page-size=\"10\" data-pagination-key=\"pages\"><button aria-label=\"Previous\" class=\"htmlgui-page-button\" data-target-page=\"-1\" disabled type=\"button\">Previous</button><button aria-current=\"page\" aria-label=\"1\" class=\"htmlgui-page-button\" data-target-page=\"0\" disabled type=\"button\">1</button><button aria-label=\"2\" class=\"htmlgui-page-button\" data-target-page=\"1\" type=\"button\">2</button><button aria-label=\"Next\" class=\"htmlgui-page-button\" data-target-page=\"1\" type=\"button\">Next</button></nav>"
        };
        string[] actual = Array.ConvertAll(controls, Serialize);

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < actual.Length)
            {
                Assert.That(actual[index], Is.EqualTo(expected[index]), names[index]);
                index++;
            }
        });
    }

    [TestCase("progress", "<progress aria-live=\"off\" class=\"htmlgui-progress\" data-severity=\"information\">Message</progress>")]
    [TestCase("meter", "<meter aria-live=\"off\" class=\"htmlgui-meter\" data-severity=\"information\">Message</meter>")]
    [TestCase("spinner", "<div aria-busy=\"true\" aria-live=\"off\" class=\"htmlgui-spinner\" data-severity=\"information\" role=\"status\">Message</div>")]
    [TestCase("skeleton", "<div aria-hidden=\"true\" class=\"htmlgui-skeleton\" data-severity=\"information\">Message</div>")]
    [TestCase("empty", "<section aria-live=\"off\" class=\"htmlgui-empty-state\" data-severity=\"information\" role=\"status\">Message</section>")]
    [TestCase("error", "<section aria-live=\"off\" class=\"htmlgui-error-state\" data-severity=\"information\" role=\"alert\">Message</section>")]
    [TestCase("loading", "<section aria-busy=\"true\" aria-live=\"off\" class=\"htmlgui-loading-state\" data-severity=\"information\" role=\"status\">Message</section>")]
    [TestCase("alert", "<div aria-live=\"off\" class=\"htmlgui-alert\" data-severity=\"information\" role=\"alert\">Message</div>")]
    [TestCase("toast", "<div aria-live=\"off\" class=\"htmlgui-toast\" data-severity=\"information\" role=\"status\">Message</div>")]
    [TestCase("status", "<output aria-live=\"off\" class=\"htmlgui-status\" data-severity=\"information\">Message</output>")]
    public void SerializesEveryFeedbackControlConstructExactly(string kind, string expected)
    {
        UiNode node = Control(kind);
        node.Feedback = new UiFeedback { Kind = kind, Message = "Message" };

        Assert.That(Serialize(node), Is.EqualTo(expected));
    }

    [Test]
    public void SerializesLiveAnnouncementAndToastOverlayConstructsExactly()
    {
        UiNode live = Control("live-announcements");
        live.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "saved", Message = "Saved", Mode = UiAriaLiveMode.Polite, Atomic = true }
        };
        UiNode toast = Control("toast");
        toast.Toast = new UiToast
        {
            Key = "saved",
            Message = "Saved",
            LiveMode = UiAriaLiveMode.Assertive,
            State = new UiOverlayState { IsOpen = true },
            HasDurationMilliseconds = true,
            DurationMilliseconds = 3000
        };

        Assert.Multiple(() =>
        {
            Assert.That(Serialize(live), Is.EqualTo("<div class=\"htmlgui-live-announcements\"><div aria-atomic=\"true\" aria-live=\"polite\" class=\"htmlgui-live-announcement\" role=\"status\">Saved</div></div>"));
            Assert.That(Serialize(toast), Is.EqualTo("<div aria-live=\"assertive\" class=\"htmlgui-toast\" data-duration-milliseconds=\"3000\" data-severity=\"information\" data-toast-key=\"saved\" role=\"alert\">Saved</div>"));
        });
    }

    [Test]
    public void SerializesDialogPopoverTooltipAndEveryMediaConstructExactly()
    {
        UiNode dialog = Control("dialog");
        dialog.Dialog = new UiDialog
        {
            Key = "dialog",
            Kind = UiDialogKind.Modal,
            Title = "Title",
            Content = new UiNode[] { Text("Body") }
        };
        UiNode popover = Control("popover");
        popover.Popover = new UiPopover
        {
            Key = "popover",
            Anchor = new UiAnchor { TargetId = "anchor", Placement = "bottom" },
            Content = new UiNode[] { Text("Popover") }
        };
        UiNode tooltip = Control("tooltip");
        tooltip.Tooltip = new UiTooltip
        {
            Key = "tooltip",
            Text = "Tooltip",
            Anchor = new UiAnchor { TargetId = "anchor", Placement = "top" }
        };
        UiNode audio = Media("audio", "/sound.mp3", "Audio");
        UiNode video = Media("video", "/movie.mp4", "Video");
        UiNode embed = Media("embed", "https://example.test/embed", "Embed");
        UiNode inlineSvg = Media("inline-svg", string.Empty, "Graphic");
        UiNode path = Node(UiNodeKind.Element, "path");
        path.Name = "path";
        path.NamespaceUri = "http://www.w3.org/2000/svg";
        inlineSvg.Children = new UiNode[] { path };
        string[] names = new string[] { "dialog", "popover", "tooltip", "audio", "video", "embed", "inline-svg" };
        string[] actual = new string[]
        {
            Serialize(dialog),
            Serialize(popover),
            Serialize(tooltip),
            SerializeWithCapabilities(audio),
            SerializeWithCapabilities(video),
            SerializeWithCapabilities(embed),
            SerializeWithCapabilities(inlineSvg)
        };
        string[] expected = new string[]
        {
            "<div class=\"htmlgui-overlay htmlgui-overlay-dialog\" data-backdrop=\"false\" data-dialog-kind=\"modal\" data-dismiss-escape=\"true\" data-dismiss-outside=\"true\" data-modal=\"true\" data-open=\"false\" data-stack-order=\"0\" data-trap-focus=\"false\" hidden><dialog aria-labelledby=\"ui-overlay-43d07f140df33413d002\" aria-modal=\"true\" class=\"htmlgui-dialog htmlgui-dialog-modal\" data-dialog-key=\"dialog\" data-dialog-kind=\"modal\"><h2 class=\"htmlgui-dialog-title\">Title</h2><div class=\"htmlgui-dialog-body\">Body</div><div class=\"htmlgui-dialog-actions\" role=\"group\"></div></dialog></div>",
            "<div aria-modal=\"false\" class=\"htmlgui-overlay htmlgui-overlay-popover\" data-allow-flip=\"true\" data-allow-shift=\"true\" data-anchor-target=\"anchor\" data-backdrop=\"false\" data-dismiss-escape=\"true\" data-dismiss-outside=\"true\" data-modal=\"false\" data-open=\"false\" data-placement=\"bottom\" data-popover-key=\"popover\" data-stack-order=\"0\" data-trap-focus=\"false\" hidden popover=\"manual\" role=\"dialog\">Popover</div>",
            "<div class=\"htmlgui-overlay htmlgui-overlay-tooltip\" data-allow-flip=\"true\" data-allow-shift=\"true\" data-anchor-target=\"anchor\" data-backdrop=\"false\" data-dismiss-escape=\"true\" data-dismiss-outside=\"true\" data-modal=\"false\" data-open=\"false\" data-placement=\"top\" data-stack-order=\"0\" data-tooltip-key=\"tooltip\" data-trap-focus=\"false\" hidden role=\"tooltip\">Tooltip</div>",
            "<audio aria-label=\"Audio\" class=\"htmlgui-audio\" controls src=\"/sound.mp3\"></audio>",
            "<video aria-label=\"Video\" class=\"htmlgui-video\" controls src=\"/movie.mp4\"></video>",
            "<iframe aria-label=\"Embed\" class=\"htmlgui-embed\" sandbox=\"\" src=\"https://example.test/embed\" title=\"Embed\"></iframe>",
            "<svg xmlns=\"http://www.w3.org/2000/svg\" aria-label=\"Graphic\" class=\"htmlgui-inline-svg\" role=\"img\"><path></path></svg>"
        };

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < actual.Length)
            {
                Assert.That(actual[index], Is.EqualTo(expected[index]), names[index]);
                index++;
            }
        });
    }

    [Test]
    public void SerializesDistinctCollectionCompositeMenuTabAndNavigationVariantsExactly()
    {
        UiNode ordered = CollectionVariant("ordered-list");
        UiNode description = CollectionVariant("description-list");
        UiNode interactive = CollectionVariant("interactive-list");
        interactive.Collection!.Selection = new UiSelection
        {
            Mode = UiSelectionMode.Single,
            SelectedKeys = new string[] { "one" },
            ActiveKey = "one"
        };
        UiNode grid = TableVariant("data-grid");
        grid.Table!.IsInteractive = true;
        UiNode treeGrid = TreeVariant("tree-grid");
        treeGrid.Tree!.IsTreeGrid = true;
        UiNode menuBar = MenuVariant("menu-bar");
        UiNode contextMenu = MenuVariant("context-menu");
        UiNode menuButton = MenuVariant("menu-button");
        UiNode stepper = TabsVariant("stepper");
        UiNode rail = NavigationVariant("navigation-rail");
        UiNode sidebar = NavigationVariant("navigation-sidebar");
        UiNode[] controls = new UiNode[] { ordered, description, interactive, grid, treeGrid, menuBar, contextMenu, menuButton, stepper, rail, sidebar };
        string[] names = new string[] { "ordered", "description", "interactive", "grid", "tree-grid", "menu-bar", "context-menu", "menu-button", "stepper", "rail", "sidebar" };
        string[] expected = new string[]
        {
            "<ol class=\"htmlgui-list htmlgui-ordered-list\" data-collection-key=\"items\" data-collection-state=\"ready\" data-page-count=\"1\" data-page-end=\"false\" data-page-start=\"0\" data-selection-mode=\"none\"><li class=\"htmlgui-list-item\" data-item-key=\"one\">One</li></ol>",
            "<dl class=\"htmlgui-list htmlgui-description-list\" data-collection-key=\"items\" data-collection-state=\"ready\" data-page-count=\"1\" data-page-end=\"false\" data-page-start=\"0\" data-selection-mode=\"none\"><div class=\"htmlgui-description-item\" data-item-key=\"one\"><dt class=\"htmlgui-description-term\">One</dt><dd class=\"htmlgui-description-details\"></dd></div></dl>",
            "<div aria-activedescendant=\"ui-collection-15a87ae97019117971d3\" class=\"htmlgui-list htmlgui-interactive-list\" data-collection-key=\"items\" data-collection-state=\"ready\" data-page-count=\"1\" data-page-end=\"false\" data-page-start=\"0\" data-selection-mode=\"single\" role=\"listbox\" tabindex=\"0\"><div aria-posinset=\"1\" aria-selected=\"true\" class=\"htmlgui-list-item\" data-active=\"true\" data-item-key=\"one\" role=\"option\" tabindex=\"-1\">One</div></div>",
            "<table aria-colcount=\"1\" aria-rowcount=\"2\" class=\"htmlgui-table htmlgui-data-grid\" data-selection-mode=\"none\" data-table-key=\"table\" role=\"grid\" tabindex=\"0\"><colgroup><col class=\"htmlgui-table-column\" data-column-key=\"name\"></colgroup><thead><tr aria-rowindex=\"1\" class=\"htmlgui-table-header-row\" role=\"row\"><th aria-colindex=\"1\" class=\"htmlgui-table-header\" data-column-key=\"name\" role=\"columnheader\" scope=\"col\">Name</th></tr></thead><tbody class=\"htmlgui-table-body\" role=\"rowgroup\"><tr aria-rowindex=\"2\" aria-selected=\"false\" class=\"htmlgui-table-row\" data-row-key=\"one\" role=\"row\" tabindex=\"-1\"><td aria-colindex=\"1\" aria-readonly=\"true\" class=\"htmlgui-table-cell\" data-column-key=\"name\" role=\"gridcell\">One</td></tr></tbody></table>",
            "<div aria-colcount=\"1\" aria-rowcount=\"-1\" class=\"htmlgui-tree htmlgui-tree-grid\" data-range-count=\"1\" data-range-start=\"0\" data-selection-mode=\"none\" data-tree-key=\"tree\" data-typeahead-buffer=\"\" data-typeahead-generation=\"0\" data-typeahead-ignore-case=\"true\" role=\"treegrid\" tabindex=\"0\"><div aria-level=\"1\" aria-posinset=\"1\" aria-rowindex=\"1\" aria-selected=\"false\" aria-setsize=\"1\" class=\"htmlgui-tree-row\" data-flat-index=\"0\" data-item-key=\"one\" role=\"row\" tabindex=\"-1\"><div aria-colindex=\"1\" class=\"htmlgui-tree-grid-cell\" role=\"gridcell\">One</div></div></div>",
            "<div aria-label=\"Menu\" aria-orientation=\"vertical\" class=\"htmlgui-menu htmlgui-menu-bar\" data-dismiss-escape=\"true\" data-dismiss-outside=\"true\" data-menu-key=\"menu\" data-menu-kind=\"bar\" data-open=\"false\" data-typeahead-buffer=\"\" data-typeahead-generation=\"0\" data-typeahead-ignore-case=\"true\" data-wrap-focus=\"true\" role=\"menubar\"><div class=\"htmlgui-menu-item htmlgui-menu-item-command\" data-item-key=\"one\" role=\"menuitem\" tabindex=\"0\"><span class=\"htmlgui-menu-label\">One</span></div></div>",
            "<div aria-label=\"Menu\" aria-orientation=\"vertical\" class=\"htmlgui-menu htmlgui-menu-context\" data-dismiss-escape=\"true\" data-dismiss-outside=\"true\" data-menu-key=\"menu\" data-menu-kind=\"context\" data-open=\"false\" data-typeahead-buffer=\"\" data-typeahead-generation=\"0\" data-typeahead-ignore-case=\"true\" data-wrap-focus=\"true\" hidden role=\"menu\"><div class=\"htmlgui-menu-item htmlgui-menu-item-command\" data-item-key=\"one\" role=\"menuitem\" tabindex=\"0\"><span class=\"htmlgui-menu-label\">One</span></div></div>",
            "<div class=\"htmlgui-menu-button\"><button aria-controls=\"ui-menu-d9f632ced1b2b97575ed\" aria-expanded=\"false\" aria-haspopup=\"menu\" class=\"htmlgui-menu-button-trigger\" data-menu-key=\"menu\" tabindex=\"0\" type=\"button\">Menu</button><div aria-label=\"Menu\" aria-orientation=\"vertical\" class=\"htmlgui-menu htmlgui-menu-button-popup\" data-dismiss-escape=\"true\" data-dismiss-outside=\"true\" data-menu-key=\"menu\" data-menu-kind=\"button-popup\" data-open=\"false\" data-typeahead-buffer=\"\" data-typeahead-generation=\"0\" data-typeahead-ignore-case=\"true\" data-wrap-focus=\"true\" hidden role=\"menu\"><div class=\"htmlgui-menu-item htmlgui-menu-item-command\" data-item-key=\"one\" role=\"menuitem\" tabindex=\"0\"><span class=\"htmlgui-menu-label\">One</span></div></div></div>",
            "<div aria-orientation=\"horizontal\" class=\"htmlgui-stepper\" data-stepper-key=\"tabs\"><ol class=\"htmlgui-step-list\"><li class=\"htmlgui-step-item\"><button aria-controls=\"ui-tabs-139bebbf5e95d01be37b\" aria-current=\"step\" class=\"htmlgui-step\" data-step-key=\"one\" tabindex=\"0\" type=\"button\"><span class=\"htmlgui-step-label\">One</span></button></li></ol><div aria-labelledby=\"ui-tabs-22adc5aabd2e465ae2df\" class=\"htmlgui-step-panel\" data-lazy=\"false\" data-materialized=\"true\">Panel</div></div>",
            "<nav aria-label=\"Breadcrumb\" class=\"htmlgui-navigation htmlgui-navigation-rail\" data-navigation-key=\"navigation\" data-navigation-kind=\"rail\"><ul class=\"htmlgui-navigation-items\"><li class=\"htmlgui-navigation-item\"><a aria-current=\"true\" class=\"htmlgui-navigation-link\" data-item-key=\"one\" href=\"/one\">One</a></li></ul></nav>",
            "<nav aria-label=\"Breadcrumb\" class=\"htmlgui-navigation htmlgui-navigation-sidebar\" data-navigation-key=\"navigation\" data-navigation-kind=\"sidebar\"><ul class=\"htmlgui-navigation-items\"><li class=\"htmlgui-navigation-item\"><a aria-current=\"true\" class=\"htmlgui-navigation-link\" data-item-key=\"one\" href=\"/one\">One</a></li></ul></nav>"
        };
        string[] actual = Array.ConvertAll(controls, Serialize);

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < actual.Length)
            {
                Assert.That(actual[index], Is.EqualTo(expected[index]), names[index]);
                index++;
            }
        });
    }

    [Test]
    public void SerializesGenericContainerControlConstructsExactly()
    {
        UiNode spacer = Control("spacer");
        UiNode separator = Control("separator");
        UiNode card = Control("card", "child");
        UiNode groupBox = Control("group-box", "child");
        UiNode fieldSet = Control("field-set", "child");
        UiNode disclosure = Control("disclosure");
        disclosure.Disclosure = new UiDisclosure { Key = "details", Summary = "More", IsExpanded = true };
        UiNode collapsible = Control("collapsible-region");
        collapsible.Disclosure = new UiDisclosure
        {
            Key = "region",
            Summary = "Options",
            IsExpanded = false,
            Content = new UiNode[] { Text("hidden content") }
        };
        UiNode resizable = Control("resizable-panel", "child");
        resizable.Layout = new UiLayout { ResizeOrientation = UiOrientation.Vertical };

        Assert.Multiple(() =>
        {
            Assert.That(Serialize(spacer), Is.EqualTo("<div aria-hidden=\"true\" class=\"htmlgui-spacer\"></div>"));
            Assert.That(Serialize(separator), Is.EqualTo("<hr class=\"htmlgui-separator\">"));
            Assert.That(Serialize(card), Is.EqualTo("<article class=\"htmlgui-card\">child</article>"));
            Assert.That(Serialize(groupBox), Is.EqualTo("<section class=\"htmlgui-group-box\" role=\"group\">child</section>"));
            Assert.That(Serialize(fieldSet), Is.EqualTo("<fieldset class=\"htmlgui-field-set\">child</fieldset>"));
            Assert.That(Serialize(disclosure), Is.EqualTo("<details class=\"htmlgui-disclosure\" open><summary class=\"htmlgui-disclosure-summary\">More</summary></details>"));
            Assert.That(Serialize(collapsible), Is.EqualTo("<section class=\"htmlgui-collapsible-region\"><button aria-controls=\"ui-control-0d234a3af3fb60116baf\" aria-expanded=\"false\" class=\"htmlgui-collapsible-toggle\" type=\"button\">Options</button><div aria-labelledby=\"ui-control-e4ca6d60c91dd97c6857\" class=\"htmlgui-collapsible-content\" hidden role=\"region\">hidden content</div></section>"));
            Assert.That(Serialize(resizable), Is.EqualTo("<div class=\"htmlgui-resizable-panel\" data-resize=\"vertical\" style=\"overflow:auto;resize:vertical;\">child</div>"));
        });
    }

    [Test]
    public void SerializesEveryLowLevelAndLogicalNodeKindExactly()
    {
        UiNode element = Node(UiNodeKind.Element, "element");
        element.Name = HtmlElements.Span;
        element.Attributes = new DomAttribute[] { new DomAttribute { Name = "title", Value = "A & B" } };
        element.Children = new UiNode[] { Text("element") };
        UiNode comment = Node(UiNodeKind.Comment, "comment");
        comment.Text = "note";
        UiNode fragment = Node(UiNodeKind.Fragment, "fragment");
        fragment.Children = new UiNode[] { Text("fragment") };
        UiNode component = Node(UiNodeKind.Component, "component");
        UiNode componentSlot = Node(UiNodeKind.Slot, "component-slot");
        componentSlot.Name = "content";
        componentSlot.Children = new UiNode[] { Text("fallback") };
        component.Children = new UiNode[] { componentSlot };
        component.Slots = new UiSlot[] { new UiSlot { Name = "content", Content = new UiNode[] { Text("projected") } } };
        UiNode control = Node(UiNodeKind.Control, "control");
        control.Name = "custom-control";
        control.Children = new UiNode[] { Text("control") };
        UiNode conditional = Node(UiNodeKind.Conditional, "conditional");
        conditional.Conditional = new UiConditional
        {
            Condition = true,
            WhenTrue = new UiNode[] { Text("true") },
            WhenFalse = new UiNode[] { Text("false") }
        };
        UiNode repeat = Node(UiNodeKind.Repeat, "repeat");
        repeat.Repeat = new UiRepeat
        {
            Items = new UiCollectionItem[]
            {
                new UiCollectionItem { Key = "one", Content = Text("first") },
                new UiCollectionItem { Key = "two", Content = Text("second") }
            }
        };
        UiNode slot = Node(UiNodeKind.Slot, "slot");
        slot.Name = "standalone";
        slot.Children = new UiNode[] { Text("slot fallback") };
        UiNode portal = Node(UiNodeKind.Portal, "portal");
        portal.Portal = new UiPortal
        {
            Key = "notice",
            TargetRegion = "overlay",
            Content = new UiNode[] { Text("portal") }
        };
        UiNode raw = Node(UiNodeKind.RawContent, "raw");
        raw.Text = "<em>trusted</em>";
        raw.RawContentPolicy = new UiRawContentPolicy { IsTrusted = true, PolicyId = "reviewed" };

        Assert.Multiple(() =>
        {
            Assert.That(Serialize(element), Is.EqualTo("<span title=\"A &amp; B\">element</span>"));
            Assert.That(Serialize(Text("text & value")), Is.EqualTo("text &amp; value"));
            Assert.That(Serialize(comment), Is.EqualTo("<!--note-->"));
            Assert.That(Serialize(fragment), Is.EqualTo("fragment"));
            Assert.That(Serialize(component), Is.EqualTo("projected"));
            Assert.That(Serialize(control), Is.EqualTo("control"));
            Assert.That(Serialize(conditional), Is.EqualTo("true"));
            Assert.That(Serialize(repeat), Is.EqualTo("firstsecond"));
            Assert.That(Serialize(slot), Is.EqualTo("slot fallback"));
            Assert.That(Serialize(portal), Is.EqualTo("portal"));
            Assert.That(Serialize(raw), Is.EqualTo("<em>trusted</em>"));
        });
    }

    [Test]
    public void SerializesFormConstraintsAndAriaProjectionExactly()
    {
        UiNode input = Input(UiInputKind.Text, "greg");
        input.Input!.AutoComplete = "username";
        input.Input.EnterKeyHint = "next";
        input.Input.InputMode = "text";
        input.Input.HasMaximumLength = true;
        input.Input.MaximumLength = 40;
        input.Input.HasMinimumLength = true;
        input.Input.MinimumLength = 2;
        input.Input.Pattern = "[A-Za-z]+";
        input.Input.Placeholder = "User name";
        input.Input.IsRequired = true;
        input.Input.HasSpellCheck = true;
        input.Input.SpellCheck = false;
        UiNode form = Node(UiNodeKind.Element, "form");
        form.Name = HtmlElements.Form;
        form.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = "method", Value = "post" },
            new DomAttribute { Name = "action", Value = "/submit" },
            new DomAttribute { Name = "novalidate", IsBoolean = true }
        };
        form.Children = new UiNode[] { input };

        Assert.That(
            Serialize(form),
            Is.EqualTo("<form action=\"/submit\" method=\"post\" novalidate><input autocomplete=\"username\" class=\"htmlgui-input htmlgui-input-text\" data-update-trigger=\"change\" enterkeyhint=\"next\" inputmode=\"text\" maxlength=\"40\" minlength=\"2\" name=\"field\" pattern=\"[A-Za-z]+\" placeholder=\"User name\" required spellcheck=\"false\" type=\"text\" value=\"greg\"></form>"));

        UiNode accessible = Node(UiNodeKind.Control, "accessible");
        accessible.Name = "custom-control";
        accessible.Children = new UiNode[] { Text("content") };
        accessible.State = new UiNodeState
        {
            IsVisible = false,
            IsEnabled = false,
            IsBusy = true,
            HasTabIndex = true,
            TabIndex = -1,
            HasPressed = true,
            IsPressed = false,
            HasSelected = true,
            IsSelected = true,
            HasExpanded = true,
            IsExpanded = false,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Mixed,
            HasCurrentKind = true,
            CurrentKind = UiAriaCurrentKind.Page
        };
        accessible.Accessibility = new UiAccessibility
        {
            Role = AriaRoles.Region,
            Label = "Direct label",
            Description = "Direct description",
            LiveMode = UiAriaLiveMode.Assertive,
            Relationships = new UiAriaRelationship[]
            {
                Relationship(AriaAttributes.ActiveDescendant, "active"),
                Relationship(AriaAttributes.Controls, "panel"),
                Relationship(AriaAttributes.DescribedBy, "help"),
                Relationship(AriaAttributes.Details, "details"),
                Relationship(AriaAttributes.ErrorMessage, "error"),
                Relationship(AriaAttributes.FlowTo, "next"),
                Relationship(AriaAttributes.LabelledBy, "label"),
                Relationship(AriaAttributes.Owns, "popup")
            }
        };

        Assert.That(
            Serialize(accessible),
            Is.EqualTo("<section aria-activedescendant=\"active\" aria-busy=\"true\" aria-checked=\"mixed\" aria-controls=\"panel\" aria-current=\"page\" aria-describedby=\"help error\" aria-details=\"details\" aria-disabled=\"true\" aria-errormessage=\"error\" aria-expanded=\"false\" aria-flowto=\"next\" aria-invalid=\"true\" aria-labelledby=\"label\" aria-live=\"assertive\" aria-owns=\"popup\" aria-pressed=\"false\" aria-selected=\"true\" hidden tabindex=\"-1\">content</section>"));
    }

    [Test]
    public void SerializesNamespacesVoidAndRawTextRulesExactly()
    {
        UiNode svg = Node(UiNodeKind.Element, "svg");
        svg.Name = "svg";
        svg.NamespaceUri = "http://www.w3.org/2000/svg";
        svg.Attributes = new DomAttribute[]
        {
            new DomAttribute
            {
                NamespaceUri = "http://www.w3.org/1999/xlink",
                Prefix = "xlink",
                Name = "href",
                Value = "#icon"
            }
        };
        UiNode path = Node(UiNodeKind.Element, "path");
        path.Name = "path";
        path.NamespaceUri = "http://www.w3.org/2000/svg";
        svg.Children = new UiNode[] { path };
        UiNode image = Node(UiNodeKind.Element, "image");
        image.Name = HtmlElements.Image;
        image.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.AlternativeText, Value = "A & B" } };
        UiNode script = Node(UiNodeKind.Element, "script");
        script.Name = HtmlElements.Script;
        script.Children = new UiNode[] { Text("const end = '</script>';") };
        UiNode title = Node(UiNodeKind.Element, "title");
        title.Name = HtmlElements.Title;
        title.Children = new UiNode[] { Text("A < B & C") };

        Assert.Multiple(() =>
        {
            Assert.That(Serialize(svg), Is.EqualTo("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xlink:href=\"#icon\"><path></path></svg>"));
            Assert.That(Serialize(image), Is.EqualTo("<img alt=\"A &amp; B\">"));
            Assert.That(Serialize(script), Is.EqualTo("<script>const end = '\\x3C/script>';</script>"));
            Assert.That(Serialize(title), Is.EqualTo("<title>A &lt; B &amp; C</title>"));
        });
    }

    [Test]
    public void SerializesEveryMetadataResourceOptionAndBothOutputModesExactly()
    {
        UiWindow window = new UiWindow
        {
            Key = "window",
            Id = "window-id",
            Document = new UiDocumentMetadata { Language = "en", TextDirection = "ltr" }
        };
        UiPage page = new UiPage
        {
            Key = "page",
            Id = "page-id",
            Title = "Exact <page>",
            Metadata = new UiDocumentMetadata
            {
                CharacterSet = "utf-8",
                Viewport = "width=device-width",
                BaseUrl = "/app/",
                Description = "Description & details",
                ApplicationName = "HTML GUI",
                Values = new UiValueEntry[]
                {
                    new UiValueEntry { Key = "theme-color", Value = new UiValue { Kind = UiValueKind.String, StringValue = "#123456" } }
                }
            },
            Resources = new UiResource[]
            {
                new UiResource
                {
                    Key = "preload",
                    Url = "/font.woff2",
                    Relationship = "preload",
                    MimeType = "font/woff2",
                    Media = "screen",
                    Integrity = "sha256-YWJjZA==",
                    CrossOrigin = "anonymous",
                    ReferrerPolicy = "no-referrer",
                    Attributes = new UiValueEntry[]
                    {
                        new UiValueEntry { Key = "as", Value = new UiValue { Kind = UiValueKind.String, StringValue = "font" } },
                        new UiValueEntry { Key = "fetchpriority", Value = new UiValue { Kind = UiValueKind.String, StringValue = "high" } }
                    }
                },
                new UiResource
                {
                    Key = "module",
                    Url = "/app.js",
                    Relationship = "script",
                    IsModule = true,
                    Attributes = new UiValueEntry[]
                    {
                        new UiValueEntry { Key = "defer", Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true } }
                    }
                }
            },
            ContentView = new UiView { Key = "content", Id = "content-id", ContentRoot = Text("Hello & goodbye") }
        };
        UiCssCompilation css = new UiCssCompilation { Success = true, Css = ".exact{color:red;}" };
        HtmlOutputComposer composer = new HtmlOutputComposer();

        HtmlOutputComposition document = composer.ComposePageOutput(
            window,
            page,
            css,
            null,
            new UiRenderOptions { FullDocument = true, IncludeHydration = false, IncludeBootstrap = false, ContentSecurityPolicyNonce = "nonce_1" });
        HtmlOutputComposition fragment = composer.ComposePageOutput(
            window,
            page,
            css,
            null,
            new UiRenderOptions { FullDocument = false, IncludeHydration = false, IncludeBootstrap = false, ContentSecurityPolicyNonce = "nonce_1" });

        Assert.Multiple(() =>
        {
            Assert.That(document.Html, Is.EqualTo("<!doctype html><html dir=\"ltr\" lang=\"en\"><head><meta charset=\"utf-8\"><meta content=\"width=device-width\" name=\"viewport\"><title>Exact &lt;page&gt;</title><base href=\"/app/\"><meta content=\"Description &amp; details\" name=\"description\"><meta content=\"HTML GUI\" name=\"application-name\"><meta content=\"#123456\" name=\"theme-color\"><link as=\"font\" crossorigin=\"anonymous\" fetchpriority=\"high\" href=\"/font.woff2\" integrity=\"sha256-YWJjZA==\" media=\"screen\" referrerpolicy=\"no-referrer\" rel=\"preload\" type=\"font/woff2\"><script defer=\"true\" nonce=\"nonce_1\" src=\"/app.js\" type=\"module\"></script><style nonce=\"nonce_1\">.exact{color:red;}</style></head><body>Hello &amp; goodbye</body></html>"));
            Assert.That(fragment.Html, Is.EqualTo("<link as=\"font\" crossorigin=\"anonymous\" fetchpriority=\"high\" href=\"/font.woff2\" integrity=\"sha256-YWJjZA==\" media=\"screen\" referrerpolicy=\"no-referrer\" rel=\"preload\" type=\"font/woff2\"><script defer=\"true\" nonce=\"nonce_1\" src=\"/app.js\" type=\"module\"></script><style nonce=\"nonce_1\">.exact{color:red;}</style>Hello &amp; goodbye"));
        });
    }

    private static UiNode Input(UiInputKind kind, string value)
    {
        UiValue inputValue = new UiValue { Kind = UiValueKind.String, StringValue = value };
        if (kind == UiInputKind.Number || kind == UiInputKind.Range)
        {
            inputValue = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 7 };
        }

        UiNode node = Node(UiNodeKind.Control, "input-" + kind.ToString());
        node.Name = "input";
        node.Input = new UiInput { Kind = kind, Name = "field", Value = inputValue };
        return node;
    }

    private static UiNode Choice(string name)
    {
        UiChoice choice = new UiChoice
        {
            Key = "one",
            Label = "One",
            Value = new UiValue { Kind = UiValueKind.String, StringValue = "1" },
            IsSelected = true
        };
        UiValue value = new UiValue { Kind = UiValueKind.String, StringValue = "1" };
        if (name == "checkbox" || name == "switch")
        {
            value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true };
        }

        UiNode node = Node(UiNodeKind.Control, "choice");
        node.Name = name;
        node.Input = new UiInput
        {
            Name = "field",
            Value = value,
            Choices = name == "checkbox" || name == "switch" ? Array.Empty<UiChoice>() : new UiChoice[] { choice }
        };
        return node;
    }

    private static UiNode Media(string kind, string source, string alternative)
    {
        UiNode node = Control(kind);
        node.Media = new UiMedia
        {
            Kind = kind,
            Source = source,
            AlternativeText = alternative,
            ShowControls = kind == "audio" || kind == "video"
        };
        return node;
    }

    private static UiNode CollectionVariant(string name)
    {
        UiNode node = Control(name);
        node.Collection = new UiCollection
        {
            Key = "items",
            Items = new UiCollectionItem[]
            {
                new UiCollectionItem { Key = "one", Value = TextValue("One") }
            }
        };
        return node;
    }

    private static UiNode TableVariant(string name)
    {
        UiNode node = Control(name);
        node.Table = new UiTable
        {
            Key = "table",
            Columns = new UiTableColumn[] { new UiTableColumn { Key = "name", Header = "Name" } },
            Rows = new UiTableRow[]
            {
                new UiTableRow
                {
                    Key = "one",
                    Cells = new UiTableCell[] { new UiTableCell { ColumnKey = "name", Value = TextValue("One") } }
                }
            }
        };
        return node;
    }

    private static UiNode TreeVariant(string name)
    {
        UiNode node = Control(name);
        node.Tree = new UiTree
        {
            Key = "tree",
            Items = new UiTreeItem[] { new UiTreeItem { Key = "one", Label = "One" } }
        };
        return node;
    }

    private static UiNode MenuVariant(string name)
    {
        UiNode node = Control(name);
        node.Menu = new UiMenu
        {
            Key = "menu",
            Label = "Menu",
            Items = new UiMenuItem[] { new UiMenuItem { Key = "one", Kind = UiMenuItemKind.Command, Label = "One" } }
        };
        return node;
    }

    private static UiNode TabsVariant(string name)
    {
        UiNode node = Control(name);
        node.Tabs = new UiTabs
        {
            Key = "tabs",
            ActiveTabKey = "one",
            Tabs = new UiTab[] { new UiTab { Key = "one", Label = "One", IsSelected = true, Panel = Text("Panel") } }
        };
        return node;
    }

    private static UiNode NavigationVariant(string name)
    {
        UiNode node = Control(name);
        node.Breadcrumb = new UiBreadcrumb
        {
            Key = "navigation",
            HasCurrentIndex = true,
            CurrentIndex = 0,
            Items = new UiChoice[] { new UiChoice { Key = "one", Label = "One", Url = "/one", Value = TextValue("one") } }
        };
        return node;
    }

    private static UiNode Control(string name)
    {
        return Control(name, null);
    }

    private static UiNode Control(string name, string? childText)
    {
        UiNode node = Node(UiNodeKind.Control, name);
        node.Name = name;
        if (childText != null)
        {
            node.Children = new UiNode[] { Text(childText) };
        }

        return node;
    }

    private static UiNode Text(string text)
    {
        UiNode node = Node(UiNodeKind.Text, "text");
        node.Text = text;
        return node;
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiNode Node(UiNodeKind kind, string key)
    {
        return new UiNode
        {
            Kind = kind,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = key + "-id" }
        };
    }

    private static UiAriaRelationship Relationship(string name, string target)
    {
        return new UiAriaRelationship { Name = name, TargetIds = new string[] { target } };
    }

    private static string Serialize(UiNode node)
    {
        DomNode expanded = new SemanticExpander().ExpandNode(node);
        return new HtmlSerializer().SerializeNode(expanded);
    }

    private static string SerializeWithCapabilities(UiNode node)
    {
        SemanticExpander expander = new SemanticExpander();
        expander.ResourceCapabilities = new string[] { "media" };
        return new HtmlSerializer().SerializeNode(expander.ExpandNode(node));
    }
}
