using System;
using System.Collections.Generic;

namespace htmlgui;

internal class AccessibilityValidator
{
    internal DiagnosticSink Diagnostics;
    internal List<UiNode> Nodes;
    internal List<string> Locations;
    internal Dictionary<string, string> Identifiers;

    internal AccessibilityValidator(DiagnosticSink diagnostics, List<UiNode> nodes, List<string> locations, Dictionary<string, string> identifiers)
    {
        Diagnostics = diagnostics;
        Nodes = nodes;
        Locations = locations;
        Identifiers = new Dictionary<string, string>(identifiers, StringComparer.Ordinal);
    }

    internal void ValidateNodes()
    {
        RegisterElementIdentifiers();
        int index = 0;
        while (index < Nodes.Count)
        {
            ValidateNodeAccessibility(Nodes[index], Locations[index]);
            index++;
        }
    }

    internal void RegisterElementIdentifiers()
    {
        int nodeIndex = 0;
        while (nodeIndex < Nodes.Count)
        {
            UiNode node = Nodes[nodeIndex];
            DomAttribute[]? attributes = node.Attributes;
            int attributeIndex = 0;
            while (attributes != null && attributeIndex < attributes.Length)
            {
                DomAttribute? attribute = attributes[attributeIndex];
                if (attribute != null && attribute.Name == HtmlAttributes.Id && !string.IsNullOrEmpty(attribute.Value))
                {
                    string identity = string.Empty;
                    if (node.Identity != null)
                    {
                        identity = node.Identity.StableId;
                    }

                    string location = Locations[nodeIndex] + "/attributes/" + attributeIndex + "/value";
                    if (Identifiers.ContainsKey(attribute.Value) && attribute.Value != identity)
                    {
                        AddAccessibilityError("accessibility.duplicate-id", "The element identifier is already used by another model object.", location);
                    }
                    else if (!Identifiers.ContainsKey(attribute.Value))
                    {
                        Identifiers.Add(attribute.Value, location);
                    }
                }

                attributeIndex++;
            }

            nodeIndex++;
        }
    }

    internal void ValidateNodeAccessibility(UiNode node, string location)
    {
        string? role = GetEffectiveRole(node);
        string? suppliedRole = GetSuppliedRole(node);
        string roleLocation = GetRoleLocation(node, location);
        if (suppliedRole != null)
        {
            if (!IsSupportedRole(suppliedRole))
            {
                AddAccessibilityError("accessibility.invalid-role", "The supplied accessibility role is not a supported canonical role.", roleLocation);
                role = null;
            }
            else
            {
                string? implicitRole = GetImplicitRole(node);
                if (implicitRole != null && suppliedRole == implicitRole)
                {
                    AddAccessibilityWarning("accessibility.redundant-role", "The supplied role duplicates the node's native semantics.", roleLocation);
                }
            }
        }

        ValidateRelationships(node, location);
        ValidateSupportHooks(node, location);
        ValidateProhibitedAria(node, role, location);
        ValidateRequiredContext(node, role, roleLocation);
        ValidateRequiredOwnedNodes(node, role, roleLocation);
        ValidateRequiredProperties(node, role, roleLocation);
        ValidateAccessibleName(node, role, roleLocation);
        ValidateHiddenFocus(node, location);
        ValidateKeyboardAccess(node, role, location);
    }

    internal string? GetSuppliedRole(UiNode node)
    {
        if (node.Accessibility != null && node.Accessibility.Role != null)
        {
            return node.Accessibility.Role;
        }

        return GetAttributeValue(node, HtmlAttributes.Role);
    }

    internal string GetRoleLocation(UiNode node, string location)
    {
        if (node.Accessibility != null && node.Accessibility.Role != null)
        {
            return location + "/accessibility/role";
        }

        int index = FindAttributeIndex(node, HtmlAttributes.Role);
        if (index >= 0)
        {
            return location + "/attributes/" + index + "/value";
        }

        return location;
    }

    internal string? GetEffectiveRole(UiNode node)
    {
        string? supplied = GetSuppliedRole(node);
        if (supplied != null)
        {
            return supplied;
        }

        return GetImplicitRole(node);
    }

    internal string? GetImplicitRole(UiNode node)
    {
        if (node.Button != null)
        {
            return AriaRoles.Button;
        }

        if (node.Input != null)
        {
            return GetInputRole(node);
        }

        if (node.Table != null)
        {
            if (node.Table.IsInteractive)
            {
                return AriaRoles.Grid;
            }

            return AriaRoles.Table;
        }

        if (node.Tree != null)
        {
            if (node.Tree.IsTreeGrid)
            {
                return AriaRoles.TreeGrid;
            }

            return AriaRoles.Tree;
        }

        if (node.Menu != null)
        {
            return AriaRoles.Menu;
        }

        if (node.Tabs != null)
        {
            return AriaRoles.TabList;
        }

        if (node.Toolbar != null)
        {
            return AriaRoles.ToolBar;
        }

        if (node.Dialog != null)
        {
            return AriaRoles.Dialog;
        }

        if (node.Tooltip != null)
        {
            return AriaRoles.ToolTip;
        }

        if (node.TextContent != null)
        {
            if (node.TextContent.Kind == UiTextKind.Heading)
            {
                return AriaRoles.Heading;
            }

            if (node.TextContent.Kind == UiTextKind.Link)
            {
                return AriaRoles.Link;
            }

            if (node.TextContent.Kind == UiTextKind.Image)
            {
                return AriaRoles.Image;
            }
        }

        switch (node.Name)
        {
            case HtmlElements.Button:
            case HtmlElements.Summary:
                return AriaRoles.Button;
            case HtmlElements.Input:
                return GetNativeInputRole(node);
            case HtmlElements.TextArea:
                return AriaRoles.TextBox;
            case HtmlElements.Select:
                if (GetAttributeValue(node, HtmlAttributes.Multiple) != null)
                {
                    return AriaRoles.ListBox;
                }

                return AriaRoles.ComboBox;
            case HtmlElements.Option:
                return AriaRoles.Option;
            case HtmlElements.Anchor:
                if (GetAttributeValue(node, HtmlAttributes.HyperlinkReference) != null)
                {
                    return AriaRoles.Link;
                }

                return null;
            case HtmlElements.Image:
                return AriaRoles.Image;
            case HtmlElements.Navigation:
                return AriaRoles.Navigation;
            case HtmlElements.Search:
                return AriaRoles.Search;
            case HtmlElements.Main:
                return AriaRoles.Main;
            case HtmlElements.Aside:
                return AriaRoles.Complementary;
            case HtmlElements.Header:
                return AriaRoles.Banner;
            case HtmlElements.Footer:
                return AriaRoles.ContentInfo;
            case HtmlElements.OrderedList:
            case HtmlElements.UnorderedList:
                return AriaRoles.List;
            case HtmlElements.ListItem:
                return AriaRoles.ListItem;
            case HtmlElements.Table:
                return AriaRoles.Table;
            case HtmlElements.TableRow:
                return AriaRoles.Row;
            case HtmlElements.TableHeader:
                return AriaRoles.ColumnHeader;
            case HtmlElements.TableData:
                return AriaRoles.Cell;
            case HtmlElements.Progress:
                return AriaRoles.ProgressBar;
            case HtmlElements.Output:
                return AriaRoles.Status;
            case HtmlElements.HorizontalRule:
                return AriaRoles.Separator;
            case HtmlElements.Dialog:
                return AriaRoles.Dialog;
            case HtmlElements.Details:
            case HtmlElements.FieldSet:
                return AriaRoles.Group;
            case HtmlElements.Heading1:
            case HtmlElements.Heading2:
            case HtmlElements.Heading3:
            case HtmlElements.Heading4:
            case HtmlElements.Heading5:
            case HtmlElements.Heading6:
                return AriaRoles.Heading;
            default:
                return null;
        }
    }

    internal string? GetInputRole(UiNode node)
    {
        switch (node.Name)
        {
            case "checkbox":
            case "checkbox-input":
                return AriaRoles.Checkbox;
            case "switch":
            case "switch-input":
                return AriaRoles.Switch;
            case "radio":
            case "radio-input":
            case "radio-group":
            case "radiogroup":
                return AriaRoles.RadioGroup;
            case "select":
            case "select-input":
            case "combo-box":
            case "combobox":
            case "combobox-input":
            case "autocomplete":
            case "autocomplete-input":
            case "editable-combo":
            case "editable-combo-box":
            case "editable-combobox":
                return AriaRoles.ComboBox;
            case "multi-select":
            case "multiselect":
            case "multi-select-input":
            case "listbox":
            case "list-box":
            case "listbox-input":
                return AriaRoles.ListBox;
        }

        UiInput? input = node.Input;
        if (input == null)
        {
            return null;
        }

        switch (input.Kind)
        {
            case UiInputKind.Hidden:
            case UiInputKind.Color:
            case UiInputKind.Date:
            case UiInputKind.Time:
            case UiInputKind.Month:
            case UiInputKind.Week:
            case UiInputKind.LocalDateTime:
            case UiInputKind.File:
                return null;
            case UiInputKind.Number:
                return AriaRoles.SpinButton;
            case UiInputKind.Range:
                return AriaRoles.Slider;
            case UiInputKind.Search:
                return AriaRoles.SearchBox;
            default:
                return AriaRoles.TextBox;
        }
    }

    internal string? GetNativeInputRole(UiNode node)
    {
        string? type = GetAttributeValue(node, HtmlAttributes.Type);
        switch (type)
        {
            case HtmlInputTypes.Hidden:
            case HtmlInputTypes.Color:
            case HtmlInputTypes.Date:
            case HtmlInputTypes.Time:
            case HtmlInputTypes.Month:
            case HtmlInputTypes.Week:
            case HtmlInputTypes.LocalDateTime:
            case HtmlInputTypes.File:
                return null;
            case HtmlInputTypes.Checkbox:
                return AriaRoles.Checkbox;
            case HtmlInputTypes.Radio:
                return AriaRoles.Radio;
            case HtmlInputTypes.Range:
                return AriaRoles.Slider;
            case HtmlInputTypes.Number:
                return AriaRoles.SpinButton;
            case HtmlInputTypes.Search:
                return AriaRoles.SearchBox;
            case HtmlInputTypes.Button:
            case HtmlInputTypes.Submit:
            case HtmlInputTypes.Reset:
                return AriaRoles.Button;
            default:
                return AriaRoles.TextBox;
        }
    }

    internal bool IsSupportedRole(string role)
    {
        switch (role)
        {
            case AriaRoles.Alert:
            case AriaRoles.AlertDialog:
            case AriaRoles.Application:
            case AriaRoles.Banner:
            case AriaRoles.Button:
            case AriaRoles.Cell:
            case AriaRoles.Checkbox:
            case AriaRoles.ColumnHeader:
            case AriaRoles.ComboBox:
            case AriaRoles.Complementary:
            case AriaRoles.ContentInfo:
            case AriaRoles.Dialog:
            case AriaRoles.Document:
            case AriaRoles.Form:
            case AriaRoles.Grid:
            case AriaRoles.GridCell:
            case AriaRoles.Group:
            case AriaRoles.Heading:
            case AriaRoles.Image:
            case AriaRoles.Link:
            case AriaRoles.List:
            case AriaRoles.ListBox:
            case AriaRoles.ListItem:
            case AriaRoles.Log:
            case AriaRoles.Main:
            case AriaRoles.Menu:
            case AriaRoles.MenuBar:
            case AriaRoles.MenuItem:
            case AriaRoles.MenuItemCheckbox:
            case AriaRoles.MenuItemRadio:
            case AriaRoles.Navigation:
            case AriaRoles.None:
            case AriaRoles.Note:
            case AriaRoles.Option:
            case AriaRoles.Presentation:
            case AriaRoles.ProgressBar:
            case AriaRoles.Radio:
            case AriaRoles.RadioGroup:
            case AriaRoles.Region:
            case AriaRoles.Row:
            case AriaRoles.RowGroup:
            case AriaRoles.RowHeader:
            case AriaRoles.ScrollBar:
            case AriaRoles.Search:
            case AriaRoles.SearchBox:
            case AriaRoles.Separator:
            case AriaRoles.Slider:
            case AriaRoles.SpinButton:
            case AriaRoles.Status:
            case AriaRoles.Switch:
            case AriaRoles.Tab:
            case AriaRoles.Table:
            case AriaRoles.TabList:
            case AriaRoles.TabPanel:
            case AriaRoles.TextBox:
            case AriaRoles.Timer:
            case AriaRoles.ToolBar:
            case AriaRoles.ToolTip:
            case AriaRoles.Tree:
            case AriaRoles.TreeGrid:
            case AriaRoles.TreeItem:
                return true;
            default:
                return false;
        }
    }

    internal void ValidateRequiredContext(UiNode node, string? role, string location)
    {
        if (role == null)
        {
            return;
        }

        string[] accepted = Array.Empty<string>();
        switch (role)
        {
            case AriaRoles.Option:
                accepted = new string[] { AriaRoles.ListBox };
                break;
            case AriaRoles.MenuItem:
            case AriaRoles.MenuItemCheckbox:
            case AriaRoles.MenuItemRadio:
                accepted = new string[] { AriaRoles.Menu, AriaRoles.MenuBar };
                break;
            case AriaRoles.Radio:
                accepted = new string[] { AriaRoles.RadioGroup };
                break;
            case AriaRoles.Tab:
                accepted = new string[] { AriaRoles.TabList };
                break;
            case AriaRoles.TreeItem:
                accepted = new string[] { AriaRoles.Tree, AriaRoles.Group };
                break;
            case AriaRoles.Row:
                accepted = new string[] { AriaRoles.Grid, AriaRoles.TreeGrid, AriaRoles.RowGroup, AriaRoles.Table };
                break;
            case AriaRoles.Cell:
            case AriaRoles.GridCell:
            case AriaRoles.RowHeader:
            case AriaRoles.ColumnHeader:
                accepted = new string[] { AriaRoles.Row };
                break;
        }

        if (accepted.Length > 0 && !HasRequiredContext(node, accepted))
        {
            AddAccessibilityError("accessibility.required-context", "The role is missing its required owning context.", location);
        }
    }

    internal bool HasRequiredContext(UiNode node, string[] acceptedRoles)
    {
        string? parentId = null;
        string nodeId = string.Empty;
        if (node.Identity != null)
        {
            parentId = node.Identity.ParentStableId;
            nodeId = node.Identity.StableId;
        }

        int index = 0;
        while (index < Nodes.Count)
        {
            UiNode possibleOwner = Nodes[index];
            string ownerId = string.Empty;
            if (possibleOwner.Identity != null)
            {
                ownerId = possibleOwner.Identity.StableId;
            }

            if ((ownerId == parentId || IsAncestorOfNode(possibleOwner, node) || OwnsIdentifier(possibleOwner, nodeId)) && ContainsRole(acceptedRoles, GetEffectiveRole(possibleOwner)))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool ContainsRole(string[] roles, string? role)
    {
        int index = 0;
        while (role != null && index < roles.Length)
        {
            if (roles[index] == role)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void ValidateRequiredOwnedNodes(UiNode node, string? role, string location)
    {
        if (role == null || HasGeneratedOwnedNodes(node, role))
        {
            return;
        }

        string[] accepted = Array.Empty<string>();
        switch (role)
        {
            case AriaRoles.ListBox:
                accepted = new string[] { AriaRoles.Option };
                break;
            case AriaRoles.Menu:
            case AriaRoles.MenuBar:
                accepted = new string[] { AriaRoles.MenuItem, AriaRoles.MenuItemCheckbox, AriaRoles.MenuItemRadio };
                break;
            case AriaRoles.RadioGroup:
                accepted = new string[] { AriaRoles.Radio };
                break;
            case AriaRoles.TabList:
                accepted = new string[] { AriaRoles.Tab };
                break;
            case AriaRoles.Tree:
                accepted = new string[] { AriaRoles.TreeItem };
                break;
            case AriaRoles.Grid:
            case AriaRoles.TreeGrid:
                accepted = new string[] { AriaRoles.Row };
                break;
            case AriaRoles.Row:
                accepted = new string[] { AriaRoles.Cell, AriaRoles.GridCell, AriaRoles.RowHeader, AriaRoles.ColumnHeader };
                break;
        }

        if (accepted.Length > 0 && !HasOwnedRole(node, accepted))
        {
            AddAccessibilityError("accessibility.required-owned", "The role does not own a required child role.", location);
        }
    }

    internal bool HasGeneratedOwnedNodes(UiNode node, string role)
    {
        if ((role == AriaRoles.ListBox || role == AriaRoles.RadioGroup) && node.Input != null && node.Input.Choices != null && node.Input.Choices.Length > 0)
        {
            return true;
        }

        if ((role == AriaRoles.Menu || role == AriaRoles.MenuBar) && node.Menu != null && node.Menu.Items != null && node.Menu.Items.Length > 0)
        {
            return true;
        }

        if (role == AriaRoles.TabList && node.Tabs != null && node.Tabs.Tabs != null && node.Tabs.Tabs.Length > 0)
        {
            return true;
        }

        if ((role == AriaRoles.Tree || role == AriaRoles.TreeGrid) && node.Tree != null && node.Tree.Items != null && node.Tree.Items.Length > 0)
        {
            return true;
        }

        if ((role == AriaRoles.Grid || role == AriaRoles.Table) && node.Table != null && node.Table.Rows != null && node.Table.Rows.Length > 0)
        {
            return true;
        }

        return false;
    }

    internal bool HasOwnedRole(UiNode owner, string[] acceptedRoles)
    {
        string ownerId = string.Empty;
        if (owner.Identity != null)
        {
            ownerId = owner.Identity.StableId;
        }

        int index = 0;
        while (index < Nodes.Count)
        {
            UiNode candidate = Nodes[index];
            string? parentId = null;
            string candidateId = string.Empty;
            if (candidate.Identity != null)
            {
                parentId = candidate.Identity.ParentStableId;
                candidateId = candidate.Identity.StableId;
            }

            if ((parentId == ownerId || IsDescendantOfIdentifier(candidate, ownerId) || OwnsIdentifier(owner, candidateId)) && ContainsRole(acceptedRoles, GetEffectiveRole(candidate)))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool OwnsIdentifier(UiNode owner, string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return false;
        }

        string? owns = GetAttributeValue(owner, AriaAttributes.Owns);
        if (ContainsIdentifierToken(owns, identifier))
        {
            return true;
        }

        UiAccessibility? accessibility = owner.Accessibility;
        UiAriaRelationship[]? relationships = null;
        if (accessibility != null)
        {
            relationships = accessibility.Relationships;
        }

        int relationshipIndex = 0;
        while (relationships != null && relationshipIndex < relationships.Length)
        {
            UiAriaRelationship? relationship = relationships[relationshipIndex];
            if (relationship != null && relationship.Name == AriaAttributes.Owns)
            {
                string[]? targets = relationship.TargetIds;
                int targetIndex = 0;
                while (targets != null && targetIndex < targets.Length)
                {
                    if (targets[targetIndex] == identifier)
                    {
                        return true;
                    }

                    targetIndex++;
                }
            }

            relationshipIndex++;
        }

        return false;
    }

    internal void ValidateRequiredProperties(UiNode node, string? role, string location)
    {
        if (role == null)
        {
            return;
        }

        if ((role == AriaRoles.Checkbox || role == AriaRoles.Radio || role == AriaRoles.Switch || role == AriaRoles.MenuItemCheckbox || role == AriaRoles.MenuItemRadio) && !HasCheckedValue(node))
        {
            AddAccessibilityError("accessibility.required-property", "The role requires a checked state.", location);
        }

        if (role == AriaRoles.ComboBox && !HasExpandedValue(node))
        {
            AddAccessibilityError("accessibility.required-property", "The combobox role requires an expanded state.", location);
        }

        if (role == AriaRoles.ComboBox && GetAttributeValue(node, AriaAttributes.Controls) == null && !HasRelationship(node, AriaAttributes.Controls) && node.Input == null)
        {
            AddAccessibilityError("accessibility.required-property", "The combobox role requires a controls relationship.", location);
        }

        if (role == AriaRoles.Tab && !HasSelectedValue(node))
        {
            AddAccessibilityError("accessibility.required-property", "The tab role requires a selected state.", location);
        }

        if (role == AriaRoles.Option && node.Name != HtmlElements.Option && !HasSelectedValue(node))
        {
            AddAccessibilityError("accessibility.required-property", "The option role requires a selected state.", location);
        }

        if (role == AriaRoles.Heading && !HasHeadingLevel(node))
        {
            AddAccessibilityError("accessibility.required-property", "The heading role requires a heading level.", location);
        }

        if ((role == AriaRoles.ScrollBar || role == AriaRoles.Slider || role == AriaRoles.SpinButton) && !HasValueNow(node))
        {
            AddAccessibilityError("accessibility.required-property", "The range role requires a current value.", location);
        }

        if (role == AriaRoles.ScrollBar && GetAttributeValue(node, AriaAttributes.Controls) == null && !HasRelationship(node, AriaAttributes.Controls))
        {
            AddAccessibilityError("accessibility.required-property", "The scrollbar role requires a controls relationship.", location);
        }
    }

    internal bool HasCheckedValue(UiNode node)
    {
        if (node.State != null && node.State.HasCheckState)
        {
            return true;
        }

        if (GetAttributeValue(node, AriaAttributes.Checked) != null || GetAttributeValue(node, HtmlAttributes.Checked) != null)
        {
            return true;
        }

        return node.Input != null;
    }

    internal bool HasExpandedValue(UiNode node)
    {
        if (node.State != null && node.State.HasExpanded)
        {
            return true;
        }

        if (GetAttributeValue(node, AriaAttributes.Expanded) != null)
        {
            return true;
        }

        return node.Input != null;
    }

    internal bool HasSelectedValue(UiNode node)
    {
        if (node.State != null && node.State.HasSelected)
        {
            return true;
        }

        return GetAttributeValue(node, AriaAttributes.Selected) != null;
    }

    internal bool HasHeadingLevel(UiNode node)
    {
        if (node.Accessibility != null && node.Accessibility.HasHeadingLevel)
        {
            return true;
        }

        if (node.TextContent != null && node.TextContent.HasHeadingLevel)
        {
            return true;
        }

        if (GetAttributeValue(node, AriaAttributes.Level) != null)
        {
            return true;
        }

        switch (node.Name)
        {
            case HtmlElements.Heading1:
            case HtmlElements.Heading2:
            case HtmlElements.Heading3:
            case HtmlElements.Heading4:
            case HtmlElements.Heading5:
            case HtmlElements.Heading6:
                return true;
            default:
                return false;
        }
    }

    internal bool HasValueNow(UiNode node)
    {
        if (GetAttributeValue(node, AriaAttributes.ValueNow) != null || GetAttributeValue(node, HtmlAttributes.Value) != null)
        {
            return true;
        }

        if (node.Input != null && node.Input.Value != null)
        {
            return true;
        }

        return node.Feedback != null && node.Feedback.HasValue;
    }

    internal void ValidateRelationships(UiNode node, string location)
    {
        UiAccessibility? accessibility = node.Accessibility;
        UiAriaRelationship[]? relationships = null;
        if (accessibility != null)
        {
            relationships = accessibility.Relationships;
        }

        int relationshipIndex = 0;
        while (relationships != null && relationshipIndex < relationships.Length)
        {
            UiAriaRelationship? relationship = relationships[relationshipIndex];
            string relationshipLocation = location + "/accessibility/relationships/" + relationshipIndex;
            if (relationship != null)
            {
                if (!IsRelationshipName(relationship.Name))
                {
                    AddAccessibilityError("accessibility.prohibited-aria", "The relationship name is not a supported ARIA ID-reference property.", relationshipLocation + "/name");
                }
                else
                {
                    ValidateRelationshipTargets(relationship.TargetIds, relationshipLocation + "/targetIds");
                }
            }

            relationshipIndex++;
        }

        DomAttribute[]? attributes = node.Attributes;
        int attributeIndex = 0;
        while (attributes != null && attributeIndex < attributes.Length)
        {
            DomAttribute? attribute = attributes[attributeIndex];
            if (attribute != null && IsRelationshipName(attribute.Name))
            {
                ValidateAttributeTargets(attribute.Value, location + "/attributes/" + attributeIndex + "/value");
            }

            attributeIndex++;
        }
    }

    internal void ValidateRelationshipTargets(string[]? targetIds, string location)
    {
        if (targetIds == null || targetIds.Length == 0)
        {
            AddAccessibilityError("accessibility.broken-reference", "An ARIA relationship requires at least one target identifier.", location);
            return;
        }

        int index = 0;
        while (index < targetIds.Length)
        {
            string? target = targetIds[index];
            if (target == null || target.Length == 0 || ContainsWhiteSpace(target) || !Identifiers.ContainsKey(target))
            {
                AddAccessibilityError("accessibility.broken-reference", "The ARIA relationship target does not identify a model object.", location + "/" + index);
            }

            index++;
        }
    }

    internal void ValidateAttributeTargets(string? value, string location)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddAccessibilityError("accessibility.broken-reference", "An ARIA relationship requires at least one target identifier.", location);
            return;
        }

        string[] targets = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (targets.Length == 0)
        {
            AddAccessibilityError("accessibility.broken-reference", "An ARIA relationship requires at least one target identifier.", location);
            return;
        }

        int index = 0;
        while (index < targets.Length)
        {
            if (!Identifiers.ContainsKey(targets[index]))
            {
                AddAccessibilityError("accessibility.broken-reference", "The ARIA relationship target does not identify a model object.", location);
            }

            index++;
        }
    }

    internal bool IsRelationshipName(string name)
    {
        switch (name)
        {
            case AriaAttributes.ActiveDescendant:
            case AriaAttributes.Controls:
            case AriaAttributes.DescribedBy:
            case AriaAttributes.Details:
            case AriaAttributes.ErrorMessage:
            case AriaAttributes.FlowTo:
            case AriaAttributes.LabelledBy:
            case AriaAttributes.Owns:
                return true;
            default:
                return false;
        }
    }

    internal bool HasRelationship(UiNode node, string name)
    {
        UiAccessibility? accessibility = node.Accessibility;
        UiAriaRelationship[]? relationships = null;
        if (accessibility != null)
        {
            relationships = accessibility.Relationships;
        }

        int index = 0;
        while (relationships != null && index < relationships.Length)
        {
            UiAriaRelationship? relationship = relationships[index];
            if (relationship != null && relationship.Name == name && relationship.TargetIds != null && relationship.TargetIds.Length > 0)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void ValidateProhibitedAria(UiNode node, string? role, string location)
    {
        if (role != AriaRoles.None && role != AriaRoles.Presentation)
        {
            return;
        }

        if (HasSemanticAria(node))
        {
            AddAccessibilityError("accessibility.prohibited-aria", "Presentational roles cannot be combined with ARIA states, properties, or an accessible name.", location);
        }
    }

    internal void ValidateSupportHooks(UiNode node, string location)
    {
        UiAccessibility? accessibility = node.Accessibility;
        if (accessibility == null)
        {
            return;
        }

        UiAccessibilityFeature[]? features = accessibility.Features;
        if (features == null)
        {
            AddAccessibilityError("accessibility.invalid-feature", "The accessibility feature collection cannot be null.", location + "/accessibility/features");
            return;
        }

        bool targetSize = false;
        bool nonColorStatus = false;
        int index = 0;
        while (index < features.Length)
        {
            switch (features[index])
            {
                case UiAccessibilityFeature.ForcedColors:
                case UiAccessibilityFeature.Zoom:
                case UiAccessibilityFeature.Reflow:
                case UiAccessibilityFeature.FocusVisible:
                case UiAccessibilityFeature.ReducedMotion:
                case UiAccessibilityFeature.ReducedTransparency:
                case UiAccessibilityFeature.ScreenReaderOnly:
                case UiAccessibilityFeature.Busy:
                    break;
                case UiAccessibilityFeature.TargetSize:
                    targetSize = true;
                    break;
                case UiAccessibilityFeature.NonColorStatus:
                    nonColorStatus = true;
                    break;
                default:
                    AddAccessibilityError("accessibility.invalid-feature", "The accessibility feature is not supported.", location + "/accessibility/features/" + index);
                    break;
            }

            index++;
        }

        if (targetSize && accessibility.MinimumTargetSizePixels < 24)
        {
            AddAccessibilityError("accessibility.target-size", "Minimum accessibility target sizes cannot be less than 24 CSS pixels.", location + "/accessibility/minimumTargetSizePixels");
        }

        if (nonColorStatus && string.IsNullOrWhiteSpace(accessibility.StatusText))
        {
            AddAccessibilityError("accessibility.non-color-status", "Non-color status hooks require non-empty status text.", location + "/accessibility/statusText");
        }
    }

    internal bool HasSemanticAria(UiNode node)
    {
        UiAccessibility? accessibility = node.Accessibility;
        if (accessibility != null)
        {
            if (accessibility.Label != null || accessibility.Description != null || accessibility.HasHeadingLevel || accessibility.LiveMode != UiAriaLiveMode.Off || accessibility.HasCurrentKind)
            {
                return true;
            }

            if (accessibility.Relationships != null && accessibility.Relationships.Length > 0)
            {
                return true;
            }

            if (ContainsSemanticSupportFeature(accessibility.Features))
            {
                return true;
            }
        }

        DomAttribute[]? attributes = node.Attributes;
        int index = 0;
        while (attributes != null && index < attributes.Length)
        {
            DomAttribute? attribute = attributes[index];
            if (attribute != null && attribute.Name != null && attribute.Name != HtmlAttributes.Role && attribute.Name.StartsWith("aria-", StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool ContainsSemanticSupportFeature(UiAccessibilityFeature[]? features)
    {
        int index = 0;
        while (features != null && index < features.Length)
        {
            if (features[index] == UiAccessibilityFeature.Busy || features[index] == UiAccessibilityFeature.NonColorStatus)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void ValidateAccessibleName(UiNode node, string? role, string location)
    {
        if (role != null && RequiresAccessibleName(role) && !HasAccessibleName(node))
        {
            AddAccessibilityError("accessibility.missing-name", "The role requires an accessible name.", location);
        }
    }

    internal bool RequiresAccessibleName(string role)
    {
        switch (role)
        {
            case AriaRoles.AlertDialog:
            case AriaRoles.Button:
            case AriaRoles.Checkbox:
            case AriaRoles.ComboBox:
            case AriaRoles.Dialog:
            case AriaRoles.Grid:
            case AriaRoles.Image:
            case AriaRoles.Link:
            case AriaRoles.ListBox:
            case AriaRoles.Menu:
            case AriaRoles.MenuBar:
            case AriaRoles.MenuItem:
            case AriaRoles.MenuItemCheckbox:
            case AriaRoles.MenuItemRadio:
            case AriaRoles.Radio:
            case AriaRoles.RadioGroup:
            case AriaRoles.SearchBox:
            case AriaRoles.Slider:
            case AriaRoles.SpinButton:
            case AriaRoles.Switch:
            case AriaRoles.Tab:
            case AriaRoles.TextBox:
            case AriaRoles.ToolBar:
            case AriaRoles.Tree:
            case AriaRoles.TreeGrid:
            case AriaRoles.TreeItem:
                return true;
            default:
                return false;
        }
    }

    internal bool HasAccessibleName(UiNode node)
    {
        if (node.Accessibility != null && !string.IsNullOrWhiteSpace(node.Accessibility.Label))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(GetAttributeValue(node, AriaAttributes.Label)) || !string.IsNullOrWhiteSpace(GetAttributeValue(node, AriaAttributes.LabelledBy)) || !string.IsNullOrWhiteSpace(GetAttributeValue(node, HtmlAttributes.Title)) || !string.IsNullOrWhiteSpace(GetAttributeValue(node, HtmlAttributes.AlternativeText)))
        {
            return true;
        }

        if (node.Button != null && !string.IsNullOrWhiteSpace(node.Button.Label))
        {
            return true;
        }

        if (node.TextContent != null && (!string.IsNullOrWhiteSpace(node.TextContent.Text) || !string.IsNullOrWhiteSpace(node.TextContent.AlternativeText)))
        {
            return true;
        }

        if (node.Media != null && !string.IsNullOrWhiteSpace(node.Media.AlternativeText))
        {
            return true;
        }

        if (node.Menu != null && !string.IsNullOrWhiteSpace(node.Menu.Label))
        {
            return true;
        }

        if (node.Toolbar != null && !string.IsNullOrWhiteSpace(node.Toolbar.Label))
        {
            return true;
        }

        if (node.Dialog != null && !string.IsNullOrWhiteSpace(node.Dialog.Title))
        {
            return true;
        }

        if (node.Tooltip != null && !string.IsNullOrWhiteSpace(node.Tooltip.Text))
        {
            return true;
        }

        if (node.Feedback != null && !string.IsNullOrWhiteSpace(node.Feedback.Message))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            return true;
        }

        return HasChildText(node.Children, 0);
    }

    internal bool HasChildText(UiNode[]? children, int depth)
    {
        if (children == null || depth > 32)
        {
            return false;
        }

        int index = 0;
        while (index < children.Length)
        {
            UiNode? child = children[index];
            if (child != null)
            {
                if (!string.IsNullOrWhiteSpace(child.Text))
                {
                    return true;
                }

                if (child.TextContent != null && !string.IsNullOrWhiteSpace(child.TextContent.Text))
                {
                    return true;
                }

                if (HasChildText(child.Children, depth + 1))
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }

    internal void ValidateHiddenFocus(UiNode node, string location)
    {
        if (IsHidden(node) && IsFocusable(node))
        {
            AddAccessibilityError("accessibility.hidden-focus", "A node hidden from presentation or accessibility APIs cannot remain focusable.", location + "/state");
        }
    }

    internal bool IsHidden(UiNode node)
    {
        if (node.State != null && !node.State.IsVisible)
        {
            return true;
        }

        if (node.Accessibility != null && node.Accessibility.IsHidden)
        {
            return true;
        }

        string? ariaHidden = GetAttributeValue(node, AriaAttributes.Hidden);
        if (ariaHidden == "true")
        {
            return true;
        }

        return GetAttributeValue(node, HtmlAttributes.Hidden) != null;
    }

    internal bool IsFocusable(UiNode node)
    {
        if (node.State != null && !node.State.IsEnabled)
        {
            return false;
        }

        if (node.Button != null && !node.Button.IsEnabled || node.Input != null && node.Input.IsDisabled || GetAttributeValue(node, HtmlAttributes.Disabled) != null)
        {
            return false;
        }

        if (node.State != null && (node.State.IsFocusable || node.State.HasTabIndex && node.State.TabIndex >= 0))
        {
            return true;
        }

        string? tabIndex = GetAttributeValue(node, HtmlAttributes.TabIndex);
        if (tabIndex != null && (tabIndex.Length == 0 || tabIndex[0] != '-'))
        {
            return true;
        }

        if (node.Button != null || node.Input != null && node.Input.Kind != UiInputKind.Hidden)
        {
            return true;
        }

        switch (node.Name)
        {
            case HtmlElements.Button:
            case HtmlElements.Select:
            case HtmlElements.TextArea:
                return true;
            case HtmlElements.Input:
                return GetAttributeValue(node, HtmlAttributes.Type) != HtmlInputTypes.Hidden;
            case HtmlElements.Anchor:
                return GetAttributeValue(node, HtmlAttributes.HyperlinkReference) != null;
            default:
                return false;
        }
    }

    internal void ValidateKeyboardAccess(UiNode node, string? role, string location)
    {
        if (IsHidden(node) || node.State != null && !node.State.IsEnabled || node.Button != null && !node.Button.IsEnabled || node.Input != null && node.Input.IsDisabled)
        {
            return;
        }

        bool hasPointerAction = HasEventKind(node, UiEventKind.Click) || HasEventKind(node, UiEventKind.DoubleClick) || HasEventKind(node, UiEventKind.PointerDown) || HasEventKind(node, UiEventKind.PointerUp) || HasEventKind(node, UiEventKind.MouseDown) || HasEventKind(node, UiEventKind.MouseUp);
        bool interactiveRole = role != null && IsInteractiveRole(role);
        if (!hasPointerAction && role == AriaRoles.Option && IsOwnedByFocusableComposite(node))
        {
            return;
        }

        if ((hasPointerAction || interactiveRole) && !IsNativelyKeyboardAccessible(node) && (!IsFocusable(node) || hasPointerAction && !HasKeyboardBinding(node)))
        {
            AddAccessibilityError("accessibility.keyboard-access", "An interactive node requires focusability and keyboard access.", location + "/eventBindings");
        }
    }

    internal bool IsOwnedByFocusableComposite(UiNode node)
    {
        string? parentId = null;
        string nodeId = string.Empty;
        if (node.Identity != null)
        {
            parentId = node.Identity.ParentStableId;
            nodeId = node.Identity.StableId;
        }

        int index = 0;
        while (index < Nodes.Count)
        {
            UiNode owner = Nodes[index];
            string ownerId = string.Empty;
            if (owner.Identity != null)
            {
                ownerId = owner.Identity.StableId;
            }

            if ((ownerId == parentId || IsAncestorOfNode(owner, node) || OwnsIdentifier(owner, nodeId)) && GetEffectiveRole(owner) == AriaRoles.ListBox && IsFocusable(owner))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool IsAncestorOfNode(UiNode possibleAncestor, UiNode node)
    {
        string ancestorId = string.Empty;
        if (possibleAncestor.Identity != null)
        {
            ancestorId = possibleAncestor.Identity.StableId;
        }

        return IsDescendantOfIdentifier(node, ancestorId);
    }

    internal bool IsDescendantOfIdentifier(UiNode node, string ancestorId)
    {
        if (string.IsNullOrEmpty(ancestorId) || node.Identity == null)
        {
            return false;
        }

        string? parentId = node.Identity.ParentStableId;
        int depth = 0;
        while (parentId != null && parentId.Length > 0 && depth <= Nodes.Count)
        {
            if (parentId == ancestorId)
            {
                return true;
            }

            UiNode? parent = FindNodeByIdentifier(parentId);
            if (parent == null || parent.Identity == null)
            {
                return false;
            }

            parentId = parent.Identity.ParentStableId;
            depth++;
        }

        return false;
    }

    internal UiNode? FindNodeByIdentifier(string identifier)
    {
        int index = 0;
        while (index < Nodes.Count)
        {
            UiNode candidate = Nodes[index];
            if (candidate.Identity != null && candidate.Identity.StableId == identifier)
            {
                return candidate;
            }

            index++;
        }

        return null;
    }

    internal bool IsInteractiveRole(string role)
    {
        switch (role)
        {
            case AriaRoles.Button:
            case AriaRoles.Checkbox:
            case AriaRoles.ComboBox:
            case AriaRoles.Link:
            case AriaRoles.ListBox:
            case AriaRoles.MenuItem:
            case AriaRoles.MenuItemCheckbox:
            case AriaRoles.MenuItemRadio:
            case AriaRoles.Option:
            case AriaRoles.Radio:
            case AriaRoles.ScrollBar:
            case AriaRoles.SearchBox:
            case AriaRoles.Slider:
            case AriaRoles.SpinButton:
            case AriaRoles.Switch:
            case AriaRoles.Tab:
            case AriaRoles.TextBox:
            case AriaRoles.TreeItem:
                return true;
            default:
                return false;
        }
    }

    internal bool IsNativelyKeyboardAccessible(UiNode node)
    {
        if (node.Button != null || node.Input != null && node.Input.Kind != UiInputKind.Hidden)
        {
            return true;
        }

        switch (node.Name)
        {
            case HtmlElements.Button:
            case HtmlElements.Select:
            case HtmlElements.TextArea:
                return true;
            case HtmlElements.Input:
                return GetAttributeValue(node, HtmlAttributes.Type) != HtmlInputTypes.Hidden;
            case HtmlElements.Anchor:
                return GetAttributeValue(node, HtmlAttributes.HyperlinkReference) != null;
            default:
                return false;
        }
    }

    internal bool HasKeyboardBinding(UiNode node)
    {
        return HasEventKind(node, UiEventKind.KeyDown) || HasEventKind(node, UiEventKind.KeyUp);
    }

    internal bool HasEventKind(UiNode node, UiEventKind kind)
    {
        UiEventBinding[]? bindings = node.EventBindings;
        int index = 0;
        while (bindings != null && index < bindings.Length)
        {
            UiEventBinding? binding = bindings[index];
            if (binding != null && binding.Kind == kind)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal string? GetAttributeValue(UiNode node, string name)
    {
        DomAttribute[]? attributes = node.Attributes;
        int index = 0;
        while (attributes != null && index < attributes.Length)
        {
            DomAttribute? attribute = attributes[index];
            if (attribute != null && attribute.Name == name)
            {
                return attribute.Value;
            }

            index++;
        }

        return null;
    }

    internal int FindAttributeIndex(UiNode node, string name)
    {
        DomAttribute[]? attributes = node.Attributes;
        int index = 0;
        while (attributes != null && index < attributes.Length)
        {
            DomAttribute? attribute = attributes[index];
            if (attribute != null && attribute.Name == name)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal bool ContainsIdentifierToken(string? values, string identifier)
    {
        if (values == null)
        {
            return false;
        }

        string[] tokens = values.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == identifier)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool ContainsWhiteSpace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsWhiteSpace(value[index]))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void AddAccessibilityError(string code, string message, string location)
    {
        Diagnostics.Add(DiagnosticLevel.Error, code, message, location);
    }

    internal void AddAccessibilityWarning(string code, string message, string location)
    {
        Diagnostics.Add(DiagnosticLevel.Warning, code, message, location);
    }
}
