namespace gltf;

internal class DocumentTransaction
{
    internal DocumentCloner Cloner = new DocumentCloner();

    internal GltfDocument CreateWorkingDocument(GltfDocument source)
    {
        GltfEditOptions options = new GltfEditOptions();
        options.CloneJson = true;
        options.CloneResourceBytes = true;
        options.ValidateResult = false;
        return Cloner.CloneDocumentData(source, options);
    }

    internal void CommitWorkingDocument(GltfDocument target, GltfDocument source)
    {
        target.Asset = source.Asset;
        target.Accessors = source.Accessors;
        target.Animations = source.Animations;
        target.Buffers = source.Buffers;
        target.BufferViews = source.BufferViews;
        target.Cameras = source.Cameras;
        target.Images = source.Images;
        target.Materials = source.Materials;
        target.Meshes = source.Meshes;
        target.Nodes = source.Nodes;
        target.Samplers = source.Samplers;
        target.Scenes = source.Scenes;
        target.Skins = source.Skins;
        target.Textures = source.Textures;
        target.HasDefaultScene = source.HasDefaultScene;
        target.DefaultScene = source.DefaultScene;
        target.ExtensionsUsed = source.ExtensionsUsed;
        target.ExtensionsRequired = source.ExtensionsRequired;
        target.PropertyJson = source.PropertyJson;
        target.Resources = source.Resources;
        target.Container = source.Container;
    }
}
