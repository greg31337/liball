using System;
using htmlgui;

namespace htmlgui.Tests;

public class TableTemplateExpanderTests
{
    [Test]
    public void ExpandsNativeTableCaptionColumnsHeadersRowsAndCellTemplates()
    {
        UiTableColumn name = Column("name", "Name");
        name.FieldKey = "personName";
        name.Width = new UiLength { HasValue = true, Value = 8, Unit = UiLengthUnit.Rem };
        name.IsSortable = true;
        name.SortDirection = UiSortDirection.Ascending;
        UiTableColumn language = Column("language", "Language");
        UiTableCell nameCell = Cell("name", "Ada");
        nameCell.IsHeader = true;
        UiTableCell languageCell = Cell("language", "ignored");
        languageCell.Content = TextNode("language-content", "language-content-id", "C#");
        UiTableRow row = new UiTableRow
        {
            Key = "ada",
            Cells = new UiTableCell[] { languageCell, nameCell }
        };
        UiNode node = TableControl("table", new UiTableColumn[] { name, language }, new UiTableRow[] { row });
        node.Table!.Caption = "People";
        node.State.Version = 4;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode columnGroup = result.Children[1];
        DomNode head = result.Children[2];
        DomNode bodyRow = result.Children[3].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Table));
            Assert.That(result.Key, Is.EqualTo("table-node"));
            Assert.That(result.Id, Is.EqualTo("table-node-id"));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-table"));
            Assert.That(AttributeValue(result, "data-table-key"), Is.EqualTo("people"));
            Assert.That(result.Children[0].Name, Is.EqualTo(HtmlElements.Caption));
            Assert.That(result.Children[0].Children[0].Text, Is.EqualTo("People"));
            Assert.That(columnGroup.Name, Is.EqualTo(HtmlElements.ColumnGroup));
            Assert.That(columnGroup.Children[0].Name, Is.EqualTo(HtmlElements.Column));
            Assert.That(AttributeValue(columnGroup.Children[0], HtmlAttributes.Style), Is.EqualTo("width:8rem;"));
            Assert.That(head.Name, Is.EqualTo(HtmlElements.TableHead));
            Assert.That(head.Children[0].Children[0].Name, Is.EqualTo(HtmlElements.TableHeader));
            Assert.That(AttributeValue(head.Children[0].Children[0], HtmlAttributes.Scope), Is.EqualTo("col"));
            Assert.That(AttributeValue(head.Children[0].Children[0], AriaAttributes.Sort), Is.EqualTo("ascending"));
            Assert.That(bodyRow.Name, Is.EqualTo(HtmlElements.TableRow));
            Assert.That(bodyRow.Key, Is.EqualTo("ada"));
            Assert.That(bodyRow.Children[0].Key, Is.EqualTo("name"));
            Assert.That(bodyRow.Children[0].Name, Is.EqualTo(HtmlElements.TableHeader));
            Assert.That(bodyRow.Children[0].Children[0].Text, Is.EqualTo("Ada"));
            Assert.That(bodyRow.Children[1].Key, Is.EqualTo("language"));
            Assert.That(bodyRow.Children[1].Children[0].Text, Is.EqualTo("C#"));
            Assert.That(bodyRow.Version, Is.EqualTo(4));
        });
    }

    [Test]
    public void ExpandsInteractiveGridSelectionEditingFrozenColumnsAndDetails()
    {
        UiTableColumn name = Column("name", "Name");
        name.IsFrozen = true;
        name.IsSortable = true;
        name.SortDirection = UiSortDirection.Descending;
        name.IsResizable = true;
        name.IsReorderable = true;
        UiTableColumn score = Column("score", "Score");
        UiTableCell firstName = Cell("name", "Ada");
        firstName.IsHeader = true;
        UiTableRow first = new UiTableRow
        {
            Key = "first",
            Cells = new UiTableCell[] { firstName, Cell("score", "10") },
            DetailContent = TextNode("details", "details-id", "First details"),
            IsSelected = true
        };
        UiTableCell edited = Cell("score", "20");
        edited.IsEditable = true;
        UiTableRow second = new UiTableRow
        {
            Key = "second",
            Cells = new UiTableCell[] { Cell("name", "Grace"), edited },
            IsDisabled = true
        };
        UiNode node = TableControl("data-grid", new UiTableColumn[] { name, score }, new UiTableRow[] { first, second });
        node.Table!.IsInteractive = true;
        node.Table.HasRowDetails = true;
        node.Table.Selection = new UiSelection
        {
            Mode = UiSelectionMode.Multiple,
            SelectedKeys = new string[] { "first" },
            ActiveKey = "second"
        };
        node.Table.EditState = new UiEditState
        {
            RowKey = "second",
            ColumnKey = "score",
            OriginalValue = IntegerValue(20),
            CurrentValue = IntegerValue(21),
            IsDirty = true,
            IsPending = true,
            IsValid = false
        };
        node.State.IsEnabled = false;
        node.State.IsVisible = false;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode header = result.Children[1].Children[0].Children[0];
        DomNode body = result.Children[2];
        DomNode firstResult = body.Children[0];
        DomNode detail = body.Children[1];
        DomNode secondResult = body.Children[2];
        DomNode editedResult = secondResult.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Grid));
            Assert.That(AttributeValue(result, "data-selection-mode"), Is.EqualTo("multiple"));
            Assert.That(AttributeValue(result, "data-edit-row"), Is.EqualTo("second"));
            Assert.That(AttributeValue(result, "data-edit-column"), Is.EqualTo("score"));
            Assert.That(AttributeValue(result, AriaAttributes.ColumnCount), Is.EqualTo("2"));
            Assert.That(AttributeValue(result, AriaAttributes.RowCount), Is.EqualTo("4"));
            Assert.That(AttributeValue(result, AriaAttributes.MultiSelectable), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.ActiveDescendant), Is.EqualTo(secondResult.Id));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(FindAttribute(result, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(header, HtmlAttributes.Role), Is.EqualTo(AriaRoles.ColumnHeader));
            Assert.That(AttributeValue(result.Children[1].Children[0], AriaAttributes.RowIndex), Is.EqualTo("1"));
            Assert.That(AttributeValue(header, AriaAttributes.ColumnIndex), Is.EqualTo("1"));
            Assert.That(AttributeValue(header, "data-frozen"), Is.EqualTo("true"));
            Assert.That(AttributeValue(header, "data-resizable"), Is.EqualTo("true"));
            Assert.That(AttributeValue(header, "data-reorderable"), Is.EqualTo("true"));
            Assert.That(AttributeValue(header, "draggable"), Is.EqualTo("true"));
            Assert.That(AttributeValue(header, AriaAttributes.Sort), Is.EqualTo("descending"));
            Assert.That(AttributeValue(firstResult, AriaAttributes.RowIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(firstResult.Children[0], AriaAttributes.ColumnIndex), Is.EqualTo("1"));
            Assert.That(AttributeValue(firstResult.Children[1], AriaAttributes.ColumnIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(firstResult, AriaAttributes.Selected), Is.EqualTo("true"));
            Assert.That(AttributeValue(firstResult, AriaAttributes.Expanded), Is.EqualTo("false"));
            Assert.That(AttributeValue(firstResult, AriaAttributes.Controls), Is.EqualTo(detail.Id));
            Assert.That(FindAttribute(detail, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(detail, AriaAttributes.RowIndex), Is.EqualTo("3"));
            Assert.That(AttributeValue(detail.Children[0], AriaAttributes.ColumnSpan), Is.EqualTo("2"));
            Assert.That(detail.Children[0].Children[0].Text, Is.EqualTo("First details"));
            Assert.That(AttributeValue(secondResult, AriaAttributes.RowIndex), Is.EqualTo("4"));
            Assert.That(AttributeValue(secondResult, "data-active"), Is.EqualTo("true"));
            Assert.That(AttributeValue(secondResult, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(AttributeValue(editedResult, "data-editing"), Is.EqualTo("true"));
            Assert.That(AttributeValue(editedResult, "data-edit-original"), Does.Contain("20"));
            Assert.That(AttributeValue(editedResult, "data-edit-current"), Does.Contain("21"));
            Assert.That(AttributeValue(editedResult, "data-edit-dirty"), Is.EqualTo("true"));
            Assert.That(AttributeValue(editedResult, AriaAttributes.ColumnIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(editedResult, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(editedResult, AriaAttributes.Invalid), Is.EqualTo("true"));
            Assert.That(AttributeValue(editedResult, AriaAttributes.ReadOnly), Is.EqualTo("false"));
        });
    }

    [TestCase(UiSelectionMode.None, "none", false, false, false)]
    [TestCase(UiSelectionMode.Single, "single", false, true, false)]
    [TestCase(UiSelectionMode.Multiple, "multiple", true, true, true)]
    [TestCase(UiSelectionMode.Range, "range", true, true, true)]
    public void ProjectsEveryGridSelectionModeWithAccessibleRowAndColumnIndices(UiSelectionMode mode, string token, bool multiSelectable, bool firstSelected, bool secondSelected)
    {
        UiTableColumn name = Column("name", "Name");
        UiNode node = TableControl(
            "data-grid",
            new UiTableColumn[] { name },
            new UiTableRow[] { Row("first", Cell("name", "First")), Row("second", Cell("name", "Second")) });
        string[] selectedKeys = Array.Empty<string>();
        if (firstSelected)
        {
            selectedKeys = secondSelected ? new string[] { "first", "second" } : new string[] { "first" };
        }

        node.Table!.IsInteractive = true;
        node.Table.Selection = new UiSelection
        {
            Mode = mode,
            SelectedKeys = selectedKeys,
            AnchorKey = mode == UiSelectionMode.Range ? "first" : null
        };

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode headerRow = result.Children[1].Children[0];
        DomNode body = result.Children[2];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-selection-mode"), Is.EqualTo(token));
            Assert.That(HasAttribute(result, AriaAttributes.MultiSelectable), Is.EqualTo(multiSelectable));
            Assert.That(AttributeValue(result, AriaAttributes.RowCount), Is.EqualTo("3"));
            Assert.That(AttributeValue(headerRow, AriaAttributes.RowIndex), Is.EqualTo("1"));
            Assert.That(AttributeValue(headerRow.Children[0], AriaAttributes.ColumnIndex), Is.EqualTo("1"));
            Assert.That(AttributeValue(body.Children[0], AriaAttributes.RowIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(body.Children[1], AriaAttributes.RowIndex), Is.EqualTo("3"));
            Assert.That(AttributeValue(body.Children[0].Children[0], AriaAttributes.ColumnIndex), Is.EqualTo("1"));
            Assert.That(AttributeValue(body.Children[0], AriaAttributes.Selected), Is.EqualTo(firstSelected ? "true" : "false"));
            Assert.That(AttributeValue(body.Children[1], AriaAttributes.Selected), Is.EqualTo(secondSelected ? "true" : "false"));
        });
    }

    [Test]
    public void ExpandsFilteringGroupingSummariesHiddenColumnsAndSpans()
    {
        UiTableColumn name = Column("name", "Name");
        name.FieldKey = "personName";
        UiTableColumn amount = Column("amount", "Amount");
        amount.IsHidden = true;
        UiTableColumn notes = Column("notes", "Notes");
        UiTableCell spanning = Cell("name", "Combined");
        spanning.HasColumnSpan = true;
        spanning.ColumnSpan = 2;
        UiTableRow alpha = Row("alpha", spanning, Cell("notes", "A note"));
        UiTableRow beta = Row("beta", Cell("name", "Beta"), Cell("amount", "2"), Cell("notes", "B note"));
        UiTableRow other = Row("other", Cell("name", "Other"));
        UiNode node = TableControl("table", new UiTableColumn[] { name, amount, notes }, new UiTableRow[] { alpha, beta, other });
        node.Table!.Filters = new UiFilter[]
        {
            new UiFilter { Key = "personName", Operator = "contains", Value = TextValue("a"), IgnoreCase = true }
        };
        node.Table.Groups = new UiGroup[]
        {
            new UiGroup { Key = "letters", Label = "Letters", ItemKeys = new string[] { "beta", "alpha" }, IsExpanded = false }
        };
        node.Table.Summaries = new UiTableSummary[]
        {
            new UiTableSummary { ColumnKey = "amount", Label = "Total", Value = IntegerValue(2) }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode collapsed = expander.ExpandNode(node);
        DomNode groupBody = collapsed.Children[2];
        DomNode ungroupedBody = collapsed.Children[3];
        DomNode foot = collapsed.Children[4];
        node.Table.Groups[0].IsExpanded = true;
        DomNode expanded = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(collapsed, "data-filter-0-key"), Is.EqualTo("personName"));
            Assert.That(AttributeValue(collapsed, "data-filter-0-operator"), Is.EqualTo("contains"));
            Assert.That(AttributeValue(collapsed, "data-filter-0-ignore-case"), Is.EqualTo("true"));
            Assert.That(AttributeValue(collapsed, "data-filter-0-value"), Does.Contain("a"));
            Assert.That(FindAttribute(collapsed.Children[0].Children[1], HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(groupBody, "data-group-key"), Is.EqualTo("letters"));
            Assert.That(AttributeValue(groupBody, "data-expanded"), Is.EqualTo("false"));
            Assert.That(groupBody.Children[0].Children[0].Children[0].Text, Is.EqualTo("Letters"));
            Assert.That(groupBody.Children[1].Key, Is.EqualTo("beta"));
            Assert.That(groupBody.Children[2].Key, Is.EqualTo("alpha"));
            Assert.That(FindAttribute(groupBody.Children[1], HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(groupBody.Children[2].Children[0].Name, Is.EqualTo(HtmlElements.TableData));
            Assert.That(AttributeValue(groupBody.Children[2].Children[0], HtmlAttributes.ColumnSpan), Is.EqualTo("2"));
            Assert.That(groupBody.Children[2].Children[1].Key, Is.EqualTo("notes"));
            Assert.That(ungroupedBody.Children[0].Key, Is.EqualTo("other"));
            Assert.That(foot.Name, Is.EqualTo(HtmlElements.TableFoot));
            Assert.That(foot.Children[0].Children[1].Children[0].Text, Is.EqualTo("Total: 2"));
            Assert.That(HasAttribute(expanded.Children[2].Children[1], HtmlAttributes.Hidden), Is.False);
            Assert.That(expanded.Children[2].Children[2].Id, Is.EqualTo(groupBody.Children[2].Id));
            Assert.That(node.Table.Rows[0], Is.SameAs(alpha));
            Assert.That(node.Table.Groups[0].ItemKeys, Is.EqualTo(new string[] { "beta", "alpha" }));
        });
    }

    [Test]
    public void ProjectsAccessibleIndicesForInteractiveGroupsEmptyCellsAndSummaries()
    {
        UiTableColumn name = Column("name", "Name");
        UiTableColumn amount = Column("amount", "Amount");
        UiTableRow row = Row("member", Cell("name", "Member"));
        UiNode node = TableControl("data-grid", new UiTableColumn[] { name, amount }, new UiTableRow[] { row });
        node.Table!.IsInteractive = true;
        node.Table.Groups = new UiGroup[]
        {
            new UiGroup { Key = "group", Label = "Group", ItemKeys = new string[] { "member" } }
        };
        node.Table.Summaries = new UiTableSummary[]
        {
            new UiTableSummary { ColumnKey = "amount", Label = "Total", Value = IntegerValue(1) }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode groupBody = result.Children[2];
        DomNode groupRow = groupBody.Children[0];
        DomNode dataRow = groupBody.Children[1];
        DomNode emptyCell = dataRow.Children[1];
        DomNode summaryRow = result.Children[3].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, AriaAttributes.RowCount), Is.EqualTo("4"));
            Assert.That(AttributeValue(groupBody, HtmlAttributes.Role), Is.EqualTo(AriaRoles.RowGroup));
            Assert.That(AttributeValue(groupRow, AriaAttributes.RowIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(groupRow.Children[0], HtmlAttributes.Role), Is.EqualTo(AriaRoles.RowHeader));
            Assert.That(AttributeValue(groupRow.Children[0], AriaAttributes.ColumnSpan), Is.EqualTo("2"));
            Assert.That(AttributeValue(groupRow.Children[0], AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(AttributeValue(dataRow, AriaAttributes.RowIndex), Is.EqualTo("3"));
            Assert.That(AttributeValue(dataRow.Children[0], AriaAttributes.ColumnIndex), Is.EqualTo("1"));
            Assert.That(AttributeValue(emptyCell, AriaAttributes.ColumnIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(emptyCell, AriaAttributes.ReadOnly), Is.EqualTo("true"));
            Assert.That(AttributeValue(emptyCell, HtmlAttributes.Role), Is.EqualTo(AriaRoles.GridCell));
            Assert.That(AttributeValue(summaryRow, AriaAttributes.RowIndex), Is.EqualTo("4"));
            Assert.That(AttributeValue(summaryRow.Children[0], AriaAttributes.ColumnIndex), Is.EqualTo("1"));
            Assert.That(AttributeValue(summaryRow.Children[1], AriaAttributes.ColumnIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(summaryRow.Children[1], AriaAttributes.ReadOnly), Is.EqualTo("true"));
            Assert.That(summaryRow.Children[1].Children[0].Text, Is.EqualTo("Total: 1"));
        });
    }

    [Test]
    public void KeepsRowAndCellIdentitiesStableAcrossSourceReordering()
    {
        UiTableColumn name = Column("name", "Name");
        UiTableColumn score = Column("score", "Score");
        UiTableRow alpha = Row("alpha", Cell("name", "Alpha"), Cell("score", "1"));
        UiTableRow beta = Row("beta", Cell("score", "2"), Cell("name", "Beta"));
        UiNode node = TableControl("table", new UiTableColumn[] { name, score }, new UiTableRow[] { alpha, beta });
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode firstBody = first.Children[2];
        node.Table!.Rows = new UiTableRow[] { beta, alpha };
        DomNode reordered = expander.ExpandNode(node);
        DomNode reorderedBody = reordered.Children[2];
        firstBody.Children[0].Children[0].Children[0].Text = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(firstBody.Children[0].Id, Is.EqualTo(reorderedBody.Children[1].Id));
            Assert.That(firstBody.Children[0].Children[0].Id, Is.EqualTo(reorderedBody.Children[1].Children[0].Id));
            Assert.That(firstBody.Children[1].Id, Is.EqualTo(reorderedBody.Children[0].Id));
            Assert.That(reorderedBody.Children[1].Children[0].Children[0].Text, Is.EqualTo("Alpha"));
            Assert.That(beta.Cells[0].ColumnKey, Is.EqualTo("score"));
        });
    }

    [Test]
    public void RejectsMalformedTableColumnRowAndCellDefinitions()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "data-grid",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode blankKey = TableControl("table", Array.Empty<UiTableColumn>(), Array.Empty<UiTableRow>());
        blankKey.Table!.Key = " ";
        UiNode nullColumns = TableControl("table", Array.Empty<UiTableColumn>(), Array.Empty<UiTableRow>());
        nullColumns.Table!.Columns = null!;
        UiNode duplicateColumns = TableControl("table", new UiTableColumn[] { Column("same", "One"), Column("same", "Two") }, Array.Empty<UiTableRow>());
        UiTableColumn unsortableColumn = Column("name", "Name");
        unsortableColumn.SortDirection = UiSortDirection.Ascending;
        UiNode unsortable = TableControl("table", new UiTableColumn[] { unsortableColumn }, Array.Empty<UiTableRow>());
        UiTableColumn invalidDirectionColumn = Column("name", "Name");
        invalidDirectionColumn.IsSortable = true;
        invalidDirectionColumn.SortDirection = (UiSortDirection)999;
        UiNode invalidDirection = TableControl("table", new UiTableColumn[] { invalidDirectionColumn }, Array.Empty<UiTableRow>());
        UiNode duplicateRows = TableControl("table", new UiTableColumn[] { Column("name", "Name") }, new UiTableRow[] { Row("same", Cell("name", "A")), Row("same", Cell("name", "B")) });
        UiNode unknownCell = TableControl("table", new UiTableColumn[] { Column("name", "Name") }, new UiTableRow[] { Row("row", Cell("missing", "A")) });
        UiTableCell overlappingFirst = Cell("name", "A");
        overlappingFirst.HasColumnSpan = true;
        overlappingFirst.ColumnSpan = 2;
        UiNode overlapping = TableControl("table", new UiTableColumn[] { Column("name", "Name"), Column("value", "Value") }, new UiTableRow[] { Row("row", overlappingFirst, Cell("value", "B")) });
        UiTableCell badSpanCell = Cell("name", "A");
        badSpanCell.HasRowSpan = true;
        badSpanCell.RowSpan = 0;
        UiNode badSpan = TableControl("table", new UiTableColumn[] { Column("name", "Name") }, new UiTableRow[] { Row("row", badSpanCell) });
        UiTableRow unsupportedDetailRow = Row("row", Cell("name", "A"));
        unsupportedDetailRow.DetailContent = TextNode("detail", "detail-id", "Detail");
        UiNode unsupportedDetail = TableControl("table", new UiTableColumn[] { Column("name", "Name") }, new UiTableRow[] { unsupportedDetailRow });
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankKey));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullColumns));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateColumns));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unsortable));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidDirection))!.ParamName, Is.EqualTo("sortDirection"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateRows));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownCell));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(overlapping));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(badSpan));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unsupportedDetail));
        });
    }

    [Test]
    public void RejectsMalformedTableQuerySelectionGroupingSummaryAndEditState()
    {
        UiTableColumn name = Column("name", "Name");
        UiTableCell editableCell = Cell("name", "A");
        editableCell.IsEditable = true;
        UiTableRow row = Row("row", editableCell);
        UiNode nullFilters = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row });
        nullFilters.Table!.Filters = null!;
        UiNode unknownFilter = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row });
        unknownFilter.Table!.Filters = new UiFilter[] { new UiFilter { Key = "missing", Operator = "equals" } };
        UiNode unknownGroup = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row });
        unknownGroup.Table!.Groups = new UiGroup[] { new UiGroup { Key = "group", ItemKeys = new string[] { "missing" } } };
        UiNode duplicateGroupRows = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row });
        duplicateGroupRows.Table!.Groups = new UiGroup[]
        {
            new UiGroup { Key = "one", ItemKeys = new string[] { "row" } },
            new UiGroup { Key = "two", ItemKeys = new string[] { "row" } }
        };
        UiNode unknownSummary = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row });
        unknownSummary.Table!.Summaries = new UiTableSummary[] { new UiTableSummary { ColumnKey = "missing" } };
        UiNode selectedWithoutMode = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row });
        selectedWithoutMode.Table!.Selection.SelectedKeys = new string[] { "row" };
        UiNode multipleSingle = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row, Row("other", Cell("name", "B")) });
        multipleSingle.Table!.Selection = new UiSelection { Mode = UiSelectionMode.Single, SelectedKeys = new string[] { "row", "other" } };
        UiNode wrongAnchor = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row });
        wrongAnchor.Table!.Selection = new UiSelection { Mode = UiSelectionMode.Single, AnchorKey = "row" };
        UiNode unknownEdit = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { row });
        unknownEdit.Table!.EditState = new UiEditState { RowKey = "missing", ColumnKey = "name" };
        UiNode noneditable = TableControl("table", new UiTableColumn[] { name }, new UiTableRow[] { Row("row", Cell("name", "A")) });
        noneditable.Table!.EditState = new UiEditState { RowKey = "row", ColumnKey = "name" };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullFilters));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownFilter));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownGroup));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateGroupRows));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownSummary));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(selectedWithoutMode));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(multipleSingle));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(wrongAnchor));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unknownEdit));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(noneditable));
        });
    }

    private static UiNode TableControl(string name, UiTableColumn[] columns, UiTableRow[] rows)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = "table-node",
                SiblingIdentity = "table-node",
                StableId = "table-node-id"
            },
            Table = new UiTable { Key = "people", Columns = columns, Rows = rows }
        };
    }

    private static UiTableColumn Column(string key, string header)
    {
        return new UiTableColumn { Key = key, Header = header };
    }

    private static UiTableRow Row(string key, params UiTableCell[] cells)
    {
        return new UiTableRow { Key = key, Cells = cells };
    }

    private static UiTableCell Cell(string columnKey, string value)
    {
        return new UiTableCell { ColumnKey = columnKey, Value = TextValue(value) };
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue IntegerValue(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiNode TextNode(string key, string id, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Text = text
        };
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        foreach (DomAttribute attribute in node.Attributes)
        {
            if (attribute.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    private static DomAttribute FindAttribute(DomNode node, string name)
    {
        foreach (DomAttribute attribute in node.Attributes)
        {
            if (attribute.Name == name)
            {
                return attribute;
            }
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }
}
