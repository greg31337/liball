using System;
using System.Collections.Generic;

namespace odf;

internal static class XFormsDocumentModel
{
    const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

    internal static void LoadXForms(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for XForms materialization.");
        }
        List<OdfXFormsModel> models = new List<OdfXFormsModel>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? root = document.Parts[partIndex].Root;
            if (root != null)
            {
                FindModels(document, root, models);
            }
            partIndex = partIndex + 1;
        }
        document.XFormsModels = models.ToArray();
        ResolveReferences(document);
        XFormsDocumentState state = new XFormsDocumentState();
        state.Models = document.XFormsModels;
        document.XFormsState = state;
    }

    internal static void SynchronizeXForms(OdfDocument document)
    {
        if (document == null || document.XFormsModels == null)
        {
            throw new OdfDocumentException(
                "A document and XForms models are required for writing.");
        }
        int index = 0;
        while (index < document.XFormsModels.Length)
        {
            OdfXFormsModel model = document.XFormsModels[index];
            if (model == null)
            {
                throw new OdfDocumentException(
                    "XForms model collections cannot contain null entries.");
            }
            SynchronizeModel(document, model);
            index = index + 1;
        }
        ResolveReferences(document);
        if (document.XFormsState == null)
        {
            document.XFormsState = new XFormsDocumentState();
        }
        document.XFormsState.Models = document.XFormsModels;
    }

    static void FindModels(
        OdfDocument document,
        OdfNode node,
        List<OdfXFormsModel> models)
    {
        if (IsElement(node, OdfNamespaces.XForms, "model"))
        {
            models.Add(LoadModel(document, node));
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            FindModels(document, node.Children[index], models);
            index = index + 1;
        }
    }

    static OdfXFormsModel LoadModel(OdfDocument document, OdfNode node)
    {
        OdfXFormsModel result = new OdfXFormsModel();
        result.Id = ReadCommon(node, "id");
        result.Functions = ReadCommon(node, "functions");
        result.Schema = ReadCommon(node, "schema");
        result.Version = ReadCommon(node, "version");
        result.Attributes = node.Attributes;
        result.ChildNodes = node.Children;
        result.PartPath = node.Location.PartPath;
        result.Node = node;
        List<OdfXFormsInstance> instances =
            new List<OdfXFormsInstance>();
        List<OdfXFormsBind> binds = new List<OdfXFormsBind>();
        List<OdfXFormsSubmission> submissions =
            new List<OdfXFormsSubmission>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.XForms, "instance"))
            {
                instances.Add(LoadInstance(document, child));
            }
            else if (IsElement(child, OdfNamespaces.XForms, "bind"))
            {
                binds.Add(LoadBind(child, null));
            }
            else if (IsElement(child, OdfNamespaces.XForms, "submission"))
            {
                submissions.Add(LoadSubmission(child));
            }
            index = index + 1;
        }
        result.Instances = instances.ToArray();
        result.Binds = binds.ToArray();
        result.Submissions = submissions.ToArray();
        result.SchemaReferences = LoadSchemaReferences(
            document,
            result,
            node);
        CaptureModelOriginals(result);
        return result;
    }

    static OdfXFormsInstance LoadInstance(
        OdfDocument document,
        OdfNode node)
    {
        OdfXFormsInstance result = new OdfXFormsInstance();
        result.Id = ReadCommon(node, "id");
        result.Source = ReadCommon(node, "src");
        result.Resource = ReadCommon(node, "resource");
        result.InlineNodes = node.Children;
        result.InlineRoot = FirstElement(node.Children);
        result.Attributes = node.Attributes;
        result.PartPath = node.Location.PartPath;
        result.Node = node;
        ResolveInstanceLocation(document, result);
        result.OriginalId = result.Id;
        result.OriginalSource = result.Source;
        result.OriginalResource = result.Resource;
        return result;
    }

    static OdfXFormsBind LoadBind(
        OdfNode node,
        OdfXFormsBind? parent)
    {
        OdfXFormsBind result = new OdfXFormsBind();
        result.Id = ReadCommon(node, "id");
        result.NodeSet = ReadCommon(node, "nodeset");
        result.Type = ReadCommon(node, "type");
        result.ReadOnlyExpression = ReadCommon(node, "readonly");
        result.RelevantExpression = ReadCommon(node, "relevant");
        result.RequiredExpression = ReadCommon(node, "required");
        result.ConstraintExpression = ReadCommon(node, "constraint");
        result.CalculateExpression = ReadCommon(node, "calculate");
        result.P3PType = ReadCommon(node, "p3ptype");
        result.Parent = parent;
        result.Attributes = node.Attributes;
        result.Node = node;
        List<OdfXFormsBind> children = new List<OdfXFormsBind>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, OdfNamespaces.XForms, "bind"))
            {
                children.Add(LoadBind(child, result));
            }
            index = index + 1;
        }
        result.Children = children.ToArray();
        CaptureBindOriginals(result);
        return result;
    }

    static OdfXFormsSubmission LoadSubmission(OdfNode node)
    {
        OdfXFormsSubmission result = new OdfXFormsSubmission();
        result.Id = ReadCommon(node, "id");
        result.Ref = ReadCommon(node, "ref");
        result.Bind = ReadCommon(node, "bind");
        result.Resource = ReadCommon(node, "resource");
        result.Action = ReadCommon(node, "action");
        result.Mode = ReadCommon(node, "mode");
        result.Method = ReadCommon(node, "method");
        result.Validate = ReadCommon(node, "validate");
        result.Relevant = ReadCommon(node, "relevant");
        result.Serialization = ReadCommon(node, "serialization");
        result.Version = ReadCommon(node, "version");
        result.Indent = ReadCommon(node, "indent");
        result.MediaType = ReadCommon(node, "mediatype");
        result.Encoding = ReadCommon(node, "encoding");
        result.OmitXmlDeclaration = ReadCommon(
            node,
            "omit-xml-declaration");
        result.Standalone = ReadCommon(node, "standalone");
        result.CDataSectionElements = ReadCommon(
            node,
            "cdata-section-elements");
        result.Replace = ReadCommon(node, "replace");
        result.Instance = ReadCommon(node, "instance");
        result.TargetRef = ReadCommon(node, "targetref");
        result.Separator = ReadCommon(node, "separator");
        result.IncludeNamespacePrefixes = ReadCommon(
            node,
            "includenamespaceprefixes");
        result.ResourceNode = FindDirectChild(
            node,
            OdfNamespaces.XForms,
            "resource");
        result.ChildNodes = node.Children;
        result.Attributes = node.Attributes;
        result.Node = node;
        CaptureSubmissionOriginals(result);
        return result;
    }

    static OdfXFormsSchemaReference[] LoadSchemaReferences(
        OdfDocument document,
        OdfXFormsModel model,
        OdfNode node)
    {
        List<OdfXFormsSchemaReference> result =
            new List<OdfXFormsSchemaReference>();
        string[] locations = model.Schema.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < locations.Length)
        {
            OdfXFormsSchemaReference reference =
                new OdfXFormsSchemaReference();
            reference.Location = locations[index];
            reference.Node = node;
            ResolveSchemaLocation(document, model.PartPath, reference);
            reference.OriginalLocation = reference.Location;
            result.Add(reference);
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, XmlSchemaNamespace, "schema"))
            {
                OdfXFormsSchemaReference reference =
                    new OdfXFormsSchemaReference();
                reference.IsInline = true;
                reference.Node = child;
                result.Add(reference);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ResolveReferences(OdfDocument document)
    {
        int modelIndex = 0;
        while (modelIndex < document.XFormsModels.Length)
        {
            OdfXFormsModel model = document.XFormsModels[modelIndex];
            int submissionIndex = 0;
            while (submissionIndex < model.Submissions.Length)
            {
                ResolveSubmissionReferences(
                    model,
                    model.Submissions[submissionIndex]);
                submissionIndex = submissionIndex + 1;
            }
            modelIndex = modelIndex + 1;
        }
        int controlIndex = 0;
        while (controlIndex < document.FormControls.Length)
        {
            ResolveControlBinding(
                document,
                document.FormControls[controlIndex].Binding);
            controlIndex = controlIndex + 1;
        }
    }

    static void ResolveSubmissionReferences(
        OdfXFormsModel model,
        OdfXFormsSubmission submission)
    {
        submission.BindTarget = FindBindInModel(model, submission.Bind);
        submission.InstanceTarget = FindInstanceInModel(
            model,
            submission.Instance);
        submission.IsBindReferenceBroken = submission.Bind.Length > 0
            && submission.BindTarget == null;
        submission.IsInstanceReferenceBroken = submission.Instance.Length > 0
            && submission.InstanceTarget == null;
    }

    static void ResolveControlBinding(
        OdfDocument document,
        OdfFormBinding binding)
    {
        binding.XFormsBindTarget = FindBindInDocument(
            document,
            binding.XFormsBind);
        binding.XFormsListSourceTarget = FindBindInDocument(
            document,
            binding.XFormsListSource);
        binding.XFormsSubmissionTarget = FindSubmissionInDocument(
            document,
            binding.XFormsSubmission);
        binding.IsXFormsBindBroken = binding.XFormsBind.Length > 0
            && binding.XFormsBindTarget == null;
        binding.IsXFormsListSourceBroken =
            binding.XFormsListSource.Length > 0
            && binding.XFormsListSourceTarget == null;
        binding.IsXFormsSubmissionBroken =
            binding.XFormsSubmission.Length > 0
            && binding.XFormsSubmissionTarget == null;
    }

    static OdfXFormsBind? FindBindInDocument(
        OdfDocument document,
        string identifier)
    {
        if (identifier.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < document.XFormsModels.Length)
        {
            OdfXFormsBind? result = FindBindInModel(
                document.XFormsModels[index],
                identifier);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfXFormsBind? FindBindInModel(
        OdfXFormsModel model,
        string identifier)
    {
        if (identifier.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < model.Binds.Length)
        {
            OdfXFormsBind? result = FindBindTree(
                model.Binds[index],
                identifier);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfXFormsBind? FindBindTree(
        OdfXFormsBind bind,
        string identifier)
    {
        if (bind.Id == identifier)
        {
            return bind;
        }
        int index = 0;
        while (index < bind.Children.Length)
        {
            OdfXFormsBind? result = FindBindTree(
                bind.Children[index],
                identifier);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfXFormsInstance? FindInstanceInModel(
        OdfXFormsModel model,
        string identifier)
    {
        if (identifier.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < model.Instances.Length)
        {
            if (model.Instances[index].Id == identifier)
            {
                return model.Instances[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfXFormsSubmission? FindSubmissionInDocument(
        OdfDocument document,
        string identifier)
    {
        if (identifier.Length == 0)
        {
            return null;
        }
        int modelIndex = 0;
        while (modelIndex < document.XFormsModels.Length)
        {
            OdfXFormsSubmission[] submissions =
                document.XFormsModels[modelIndex].Submissions;
            int index = 0;
            while (index < submissions.Length)
            {
                if (submissions[index].Id == identifier)
                {
                    return submissions[index];
                }
                index = index + 1;
            }
            modelIndex = modelIndex + 1;
        }
        return null;
    }

    static void SynchronizeModel(
        OdfDocument document,
        OdfXFormsModel model)
    {
        OdfNode? node = model.Node;
        if (node == null)
        {
            return;
        }
        bool schemaReferencesChanged = SchemaReferencesChanged(
            model.SchemaReferences);
        if (schemaReferencesChanged)
        {
            model.Schema = SchemaReferenceText(model.SchemaReferences);
        }
        WriteCommonChanged(node, "id", model.Id, model.OriginalId);
        WriteCommonChanged(
            node,
            "functions",
            model.Functions,
            model.OriginalFunctions);
        WriteCommonChanged(
            node,
            "schema",
            model.Schema,
            model.OriginalSchema);
        WriteCommonChanged(
            node,
            "version",
            model.Version,
            model.OriginalVersion);
        int index = 0;
        while (index < model.Instances.Length)
        {
            SynchronizeInstance(document, model.Instances[index]);
            index = index + 1;
        }
        index = 0;
        while (index < model.Binds.Length)
        {
            SynchronizeBind(model.Binds[index]);
            index = index + 1;
        }
        index = 0;
        while (index < model.Submissions.Length)
        {
            SynchronizeSubmission(model.Submissions[index]);
            index = index + 1;
        }
        index = 0;
        while (index < model.SchemaReferences.Length)
        {
            OdfXFormsSchemaReference reference =
                model.SchemaReferences[index];
            if (!reference.IsInline)
            {
                ResolveSchemaLocation(
                    document,
                    model.PartPath,
                    reference);
                reference.OriginalLocation = reference.Location;
            }
            index = index + 1;
        }
        model.Attributes = node.Attributes;
        model.ChildNodes = node.Children;
        CaptureModelOriginals(model);
    }

    static void SynchronizeInstance(
        OdfDocument document,
        OdfXFormsInstance instance)
    {
        OdfNode? node = instance.Node;
        if (node == null)
        {
            return;
        }
        WriteCommonChanged(node, "id", instance.Id, instance.OriginalId);
        WriteCommonChanged(
            node,
            "src",
            instance.Source,
            instance.OriginalSource);
        WriteCommonChanged(
            node,
            "resource",
            instance.Resource,
            instance.OriginalResource);
        instance.Attributes = node.Attributes;
        instance.InlineNodes = node.Children;
        instance.InlineRoot = FirstElement(node.Children);
        ResolveInstanceLocation(document, instance);
        instance.OriginalId = instance.Id;
        instance.OriginalSource = instance.Source;
        instance.OriginalResource = instance.Resource;
    }

    static void SynchronizeBind(OdfXFormsBind bind)
    {
        OdfNode? node = bind.Node;
        if (node == null)
        {
            return;
        }
        WriteCommonChanged(node, "id", bind.Id, bind.OriginalId);
        WriteCommonChanged(
            node,
            "nodeset",
            bind.NodeSet,
            bind.OriginalNodeSet);
        WriteCommonChanged(node, "type", bind.Type, bind.OriginalType);
        WriteCommonChanged(node, "readonly", bind.ReadOnlyExpression, bind.OriginalReadOnlyExpression);
        WriteCommonChanged(node, "relevant", bind.RelevantExpression, bind.OriginalRelevantExpression);
        WriteCommonChanged(node, "required", bind.RequiredExpression, bind.OriginalRequiredExpression);
        WriteCommonChanged(node, "constraint", bind.ConstraintExpression, bind.OriginalConstraintExpression);
        WriteCommonChanged(node, "calculate", bind.CalculateExpression, bind.OriginalCalculateExpression);
        WriteCommonChanged(node, "p3ptype", bind.P3PType, bind.OriginalP3PType);
        int index = 0;
        while (index < bind.Children.Length)
        {
            SynchronizeBind(bind.Children[index]);
            index = index + 1;
        }
        bind.Attributes = node.Attributes;
        CaptureBindOriginals(bind);
    }

    static void SynchronizeSubmission(OdfXFormsSubmission submission)
    {
        OdfNode? node = submission.Node;
        if (node == null)
        {
            return;
        }
        WriteCommonChanged(node, "id", submission.Id, submission.OriginalId);
        WriteCommonChanged(node, "ref", submission.Ref, submission.OriginalRef);
        WriteCommonChanged(node, "bind", submission.Bind, submission.OriginalBind);
        WriteCommonChanged(node, "resource", submission.Resource, submission.OriginalResource);
        WriteCommonChanged(node, "action", submission.Action, submission.OriginalAction);
        WriteCommonChanged(node, "mode", submission.Mode, submission.OriginalMode);
        WriteCommonChanged(node, "method", submission.Method, submission.OriginalMethod);
        WriteCommonChanged(node, "validate", submission.Validate, submission.OriginalValidate);
        WriteCommonChanged(node, "relevant", submission.Relevant, submission.OriginalRelevant);
        WriteCommonChanged(node, "serialization", submission.Serialization, submission.OriginalSerialization);
        WriteCommonChanged(node, "version", submission.Version, submission.OriginalVersion);
        WriteCommonChanged(node, "indent", submission.Indent, submission.OriginalIndent);
        WriteCommonChanged(node, "mediatype", submission.MediaType, submission.OriginalMediaType);
        WriteCommonChanged(node, "encoding", submission.Encoding, submission.OriginalEncoding);
        WriteCommonChanged(node, "omit-xml-declaration", submission.OmitXmlDeclaration, submission.OriginalOmitXmlDeclaration);
        WriteCommonChanged(node, "standalone", submission.Standalone, submission.OriginalStandalone);
        WriteCommonChanged(node, "cdata-section-elements", submission.CDataSectionElements, submission.OriginalCDataSectionElements);
        WriteCommonChanged(node, "replace", submission.Replace, submission.OriginalReplace);
        WriteCommonChanged(node, "instance", submission.Instance, submission.OriginalInstance);
        WriteCommonChanged(node, "targetref", submission.TargetRef, submission.OriginalTargetRef);
        WriteCommonChanged(node, "separator", submission.Separator, submission.OriginalSeparator);
        WriteCommonChanged(node, "includenamespaceprefixes", submission.IncludeNamespacePrefixes, submission.OriginalIncludeNamespacePrefixes);
        submission.Attributes = node.Attributes;
        submission.ChildNodes = node.Children;
        submission.ResourceNode = FindDirectChild(
            node,
            OdfNamespaces.XForms,
            "resource");
        CaptureSubmissionOriginals(submission);
    }

    static void ResolveInstanceLocation(
        OdfDocument document,
        OdfXFormsInstance instance)
    {
        instance.EffectiveSource = instance.Source.Length > 0
            ? instance.Source
            : instance.Resource;
        instance.IsExternal = false;
        instance.PackagePath = "";
        instance.PackageEntryExists = false;
        string location = instance.EffectiveSource;
        if (location.Length == 0)
        {
            return;
        }
        if (IsExternalLocation(document, location))
        {
            instance.IsExternal = true;
            return;
        }
        string path = ResolvePackagePath(instance.PartPath, location);
        instance.PackagePath = path;
        instance.PackageEntryExists = PackageEntryExists(document, path);
    }

    static void ResolveSchemaLocation(
        OdfDocument document,
        string partPath,
        OdfXFormsSchemaReference reference)
    {
        reference.IsExternal = false;
        reference.PackagePath = "";
        reference.PackageEntryExists = false;
        if (reference.Location.Length == 0)
        {
            return;
        }
        if (IsExternalLocation(document, reference.Location))
        {
            reference.IsExternal = true;
            return;
        }
        string path = ResolvePackagePath(partPath, reference.Location);
        reference.PackagePath = path;
        reference.PackageEntryExists = PackageEntryExists(document, path);
    }

    static bool IsExternalLocation(
        OdfDocument document,
        string location)
    {
        if (OdfIriResolver.IsAbsoluteIri(location)
            || location.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }
        return document.PackageKind == OdfPackageKind.Flat
            && !location.StartsWith("#", StringComparison.Ordinal);
    }

    static string ResolvePackagePath(string partPath, string location)
    {
        if (!OdfIriResolver.IsPackageRelativeReference(location))
        {
            return "";
        }
        string path = RemoveLocationSuffix(location);
        if (path.Length == 0)
        {
            path = partPath;
        }
        else
        {
            path = PackagePathRules.ResolveRelativeReference(
                partPath,
                path);
        }
        return path;
    }

    static string RemoveLocationSuffix(string location)
    {
        int end = location.Length;
        int query = location.IndexOf('?');
        if (query >= 0 && query < end)
        {
            end = query;
        }
        int fragment = location.IndexOf('#');
        if (fragment >= 0 && fragment < end)
        {
            end = fragment;
        }
        return location.Substring(0, end);
    }

    static bool PackageEntryExists(OdfDocument document, string path)
    {
        if (path.Length == 0)
        {
            return false;
        }
        OdfPackageEntry[] entries = document.PackageEntries;
        if (document.Package != null)
        {
            entries = document.Package.Entries;
        }
        int index = 0;
        while (index < entries.Length)
        {
            string entryPath = entries[index].CanonicalPath;
            if (entryPath.Length == 0)
            {
                entryPath = entries[index].Path;
            }
            if (entryPath == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool SchemaReferencesChanged(
        OdfXFormsSchemaReference[] references)
    {
        int index = 0;
        while (index < references.Length)
        {
            OdfXFormsSchemaReference reference = references[index];
            if (!reference.IsInline
                && reference.Location != reference.OriginalLocation)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string SchemaReferenceText(
        OdfXFormsSchemaReference[] references)
    {
        string result = "";
        int index = 0;
        while (index < references.Length)
        {
            OdfXFormsSchemaReference reference = references[index];
            if (!reference.IsInline && reference.Location.Length > 0)
            {
                if (result.Length > 0)
                {
                    result = result + " ";
                }
                result = result + reference.Location;
            }
            index = index + 1;
        }
        return result;
    }

    static void CaptureModelOriginals(OdfXFormsModel model)
    {
        model.OriginalId = model.Id;
        model.OriginalFunctions = model.Functions;
        model.OriginalSchema = model.Schema;
        model.OriginalVersion = model.Version;
    }

    static void CaptureBindOriginals(OdfXFormsBind bind)
    {
        bind.OriginalId = bind.Id;
        bind.OriginalNodeSet = bind.NodeSet;
        bind.OriginalType = bind.Type;
        bind.OriginalReadOnlyExpression = bind.ReadOnlyExpression;
        bind.OriginalRelevantExpression = bind.RelevantExpression;
        bind.OriginalRequiredExpression = bind.RequiredExpression;
        bind.OriginalConstraintExpression = bind.ConstraintExpression;
        bind.OriginalCalculateExpression = bind.CalculateExpression;
        bind.OriginalP3PType = bind.P3PType;
    }

    static void CaptureSubmissionOriginals(
        OdfXFormsSubmission submission)
    {
        submission.OriginalId = submission.Id;
        submission.OriginalRef = submission.Ref;
        submission.OriginalBind = submission.Bind;
        submission.OriginalResource = submission.Resource;
        submission.OriginalAction = submission.Action;
        submission.OriginalMode = submission.Mode;
        submission.OriginalMethod = submission.Method;
        submission.OriginalValidate = submission.Validate;
        submission.OriginalRelevant = submission.Relevant;
        submission.OriginalSerialization = submission.Serialization;
        submission.OriginalVersion = submission.Version;
        submission.OriginalIndent = submission.Indent;
        submission.OriginalMediaType = submission.MediaType;
        submission.OriginalEncoding = submission.Encoding;
        submission.OriginalOmitXmlDeclaration =
            submission.OmitXmlDeclaration;
        submission.OriginalStandalone = submission.Standalone;
        submission.OriginalCDataSectionElements =
            submission.CDataSectionElements;
        submission.OriginalReplace = submission.Replace;
        submission.OriginalInstance = submission.Instance;
        submission.OriginalTargetRef = submission.TargetRef;
        submission.OriginalSeparator = submission.Separator;
        submission.OriginalIncludeNamespacePrefixes =
            submission.IncludeNamespacePrefixes;
    }

    static OdfNode? FirstElement(OdfNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index].Kind == OdfNodeKind.Element)
            {
                return nodes[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectChild(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsElement(node.Children[index], namespaceName, localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static string ReadCommon(OdfNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName.Length == 0
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.XForms
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static void WriteCommonChanged(
        OdfNode node,
        string localName,
        string value,
        string original)
    {
        if (value == original)
        {
            return;
        }
        OdfAttribute? existing = FindCommonAttribute(node, localName);
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(node, "", localName);
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.XForms,
                localName);
            return;
        }
        if (existing != null)
        {
            existing.Value = value;
            return;
        }
        StyleDocumentModel.WriteAttribute(node, "", localName, "", value);
    }

    static OdfAttribute? FindCommonAttribute(
        OdfNode node,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if ((attribute.Name.NamespaceName.Length == 0
                    || attribute.Name.NamespaceName == OdfNamespaces.XForms)
                && attribute.Name.LocalName == localName)
            {
                return attribute;
            }
            index = index + 1;
        }
        return null;
    }
}
