using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfCanonicalizeTransformTests
{
    [Test]
    public void Canonicalize_PermutedDocumentsConvergeAndRemapEveryReference()
    {
        GltfDocument forwardSource = CreateDocument(false);
        GltfDocument reverseSource = CreateDocument(true);
        GltfTransformReport forwardReport = Transform(forwardSource, false);
        GltfTransformReport reverseReport = Transform(reverseSource, false);
        GltfDocument forward = forwardReport.Document!;
        GltfDocument reverse = reverseReport.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(WriteCanonicalJson(forward), Is.EqualTo(WriteCanonicalJson(reverse)));
            Assert.That(GetResourceOrder(forward), Is.EqualTo(GetResourceOrder(reverse)));
            Assert.That(forwardReport.Steps[0].Remap.Resources, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Accessors, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Animations, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Buffers, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.BufferViews, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Cameras, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Images, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Materials, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Meshes, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Nodes, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Samplers, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Scenes, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Skins, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.Textures, Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.PunctualLights,
                Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.MaterialVariants,
                Has.Count.EqualTo(2));
            Assert.That(forwardReport.Steps[0].Remap.XmpJsonLdPackets,
                Has.Count.EqualTo(2));
            Assert.That(reverseReport.Steps[0].ChangedObjects, Is.Not.Empty);
            Assert.That(forwardSource.Nodes[0].Name, Is.EqualTo("a"));
            Assert.That(reverseSource.Nodes[0].Name, Is.EqualTo("z"));
        });

        AssertReferenceConsistency(forward);
        AssertReferenceConsistency(reverse);
    }

    [Test]
    public void Canonicalize_IsIdempotent()
    {
        GltfTransformReport first = Transform(CreateDocument(true), false);
        string firstJson = WriteCanonicalJson(first.Document!);
        GltfTransformReport second = Transform(first.Document!, false);

        Assert.Multiple(() =>
        {
            Assert.That(WriteCanonicalJson(second.Document!), Is.EqualTo(firstJson));
            Assert.That(second.Steps[0].ChangedObjects, Is.Empty);
            Assert.That(second.Steps[0].ChangedResources, Is.Empty);
        });
    }

    [Test]
    public void Canonicalize_RejectsUnsafeUnknownExtensionsTransactionally()
    {
        GltfDocument source = new GltfDocument();
        source.Samplers.Add(new GltfSampler { Name = "z" });
        source.Samplers.Add(new GltfSampler { Name = "a" });
        source.PropertyJson.Extensions["EXT_unknown"] = new JsonObject();

        Assert.That(
            () => Transform(source, false),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.Contains("cannot safely remap references"));
        Assert.That(source.Samplers[0].Name, Is.EqualTo("z"));
    }

    [Test]
    public void Canonicalize_DefaultValidationAcceptsReorderedSamplers()
    {
        GltfDocument source = new GltfDocument();
        source.Samplers.Add(new GltfSampler { Name = "z" });
        source.Samplers.Add(new GltfSampler { Name = "a" });

        GltfTransformReport report = Transform(source, true);

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Document!.Samplers[0].Name, Is.EqualTo("a"));
            Assert.That(report.Document.Samplers[1].Name, Is.EqualTo("z"));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "samplers/1", "samplers/0" }));
            Assert.That(source.Samplers[0].Name, Is.EqualTo("z"));
        });
    }

    private static GltfTransformReport Transform(GltfDocument document, bool validate)
    {
        GltfTransformStep step = new GltfTransformStep
        {
            Kind = GltfTransformKind.Canonicalize
        };
        GltfTransformOptions options = new GltfTransformOptions
        {
            ValidateBefore = validate,
            ValidateAfter = validate
        };
        return new GltfTransformer().TransformDocument(
            document,
            new GltfTransformPlan { Steps = [step] },
            options);
    }

    private static GltfDocument CreateDocument(bool reverse)
    {
        GltfDocument result = new GltfDocument();
        AddRootExtensions(result, reverse);
        int position = 0;
        while (position < 2)
        {
            string logicalName;
            if (reverse)
            {
                logicalName = position == 0 ? "z" : "a";
            }
            else
            {
                logicalName = position == 0 ? "a" : "z";
            }

            AddChain(result, logicalName, position, reverse);
            position++;
        }

        result.HasDefaultScene = true;
        result.DefaultScene = GetLogicalIndex("z", reverse);
        AddDeclarations(result);
        return result;
    }

    private static void AddChain(
        GltfDocument document,
        string name,
        int index,
        bool reverse)
    {
        byte value = name == "a" ? (byte)1 : (byte)9;
        GltfResource resource = new GltfResource
        {
            Kind = GltfResourceKind.Buffer,
            EffectiveUri = name + ".bin",
            Bytes = [value, 2, 3, 4],
            CacheKey = name
        };
        document.Resources.Resources.Add(resource);
        document.Resources.Cache[name] = resource;
        document.Buffers.Add(new GltfBuffer
        {
            Name = name,
            ByteLength = 4,
            Resolved = true,
            ResolvedBytes = [value, 2, 3, 4],
            HasResource = true,
            Resource = index
        });
        document.BufferViews.Add(new GltfBufferView
        {
            Name = name,
            Buffer = index,
            ByteLength = 4
        });
        document.Accessors.Add(new GltfAccessor
        {
            Name = name,
            HasBufferView = true,
            BufferView = index,
            ComponentType = GltfComponentType.Float,
            Count = 1,
            Type = GltfAccessorType.Scalar
        });
        document.Images.Add(new GltfImage
        {
            Name = name,
            MimeType = GltfMimeTypes.Png,
            HasBufferView = true,
            BufferView = index
        });
        document.Samplers.Add(new GltfSampler { Name = name });
        GltfTexture texture = new GltfTexture
        {
            Name = name,
            HasSampler = true,
            Sampler = index,
            HasSource = true,
            Source = index,
            HasSelectedSource = true,
            SelectedSource = index
        };
        AddBasisu(texture, index);
        document.Textures.Add(texture);
        GltfMaterial material = new GltfMaterial
        {
            Name = name,
            PbrMetallicRoughness = new GltfPbrMetallicRoughness
            {
                BaseColorTexture = new GltfTextureInfo { Index = index }
            }
        };
        AddXmp(material, GetLogicalIndex(name, reverse));
        document.Materials.Add(material);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive
        {
            HasMaterial = true,
            Material = index
        };
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = index
        });
        AddDraco(primitive, index);
        AddVariant(primitive, index, GetLogicalIndex(name, reverse));
        GltfMesh mesh = new GltfMesh { Name = name };
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        document.Cameras.Add(new GltfCamera
        {
            Name = name,
            Type = GltfCameraType.Perspective,
            Perspective = new GltfPerspectiveCamera
            {
                VerticalFieldOfView = name == "a" ? 0.5 : 1.0,
                NearPlane = 0.1
            }
        });
        GltfNode node = new GltfNode
        {
            Name = name,
            HasCamera = true,
            Camera = index,
            HasMesh = true,
            Mesh = index,
            HasSkin = true,
            Skin = index
        };
        if (name == "a")
        {
            node.Children.Add(GetLogicalIndex("z", reverse));
        }

        AddLight(node, GetLogicalIndex(name, reverse));
        document.Nodes.Add(node);
        GltfSkin skin = new GltfSkin
        {
            Name = name,
            HasInverseBindMatrices = true,
            InverseBindMatrices = index,
            HasSkeleton = true,
            Skeleton = index
        };
        skin.Joints.Add(index);
        document.Skins.Add(skin);
        GltfAnimation animation = new GltfAnimation { Name = name };
        animation.Samplers.Add(new GltfAnimationSampler
        {
            Input = index,
            Output = index
        });
        GltfAnimationChannel channel = new GltfAnimationChannel { Sampler = 0 };
        channel.Target.HasNode = true;
        channel.Target.Node = index;
        animation.Channels.Add(channel);
        document.Animations.Add(animation);
        GltfScene scene = new GltfScene { Name = name };
        scene.Nodes.Add(index);
        document.Scenes.Add(scene);
    }

    private static int GetLogicalIndex(string name, bool reverse)
    {
        if (reverse)
        {
            return name == "a" ? 1 : 0;
        }

        return name == "a" ? 0 : 1;
    }

    private static void AssertReferenceConsistency(GltfDocument document)
    {
        int index = 0;
        while (index < 2)
        {
            string name = document.Buffers[index].Name!;
            int nodeIndex = FindNode(document, name);
            int skinIndex = FindSkin(document, name);
            Assert.Multiple(() =>
            {
                Assert.That(document.BufferViews[index].Buffer, Is.EqualTo(index));
                Assert.That(document.Accessors[index].BufferView, Is.EqualTo(index));
                Assert.That(document.Images[index].BufferView, Is.EqualTo(index));
                Assert.That(document.Textures[index].Sampler, Is.EqualTo(index));
                Assert.That(document.Textures[index].Source, Is.EqualTo(index));
                Assert.That(document.Materials[index].PbrMetallicRoughness!
                    .BaseColorTexture!.Index, Is.EqualTo(index));
                Assert.That(document.Meshes[index].Primitives[0].Material,
                    Is.EqualTo(index));
                Assert.That(document.Nodes[nodeIndex].Mesh, Is.EqualTo(index));
                Assert.That(document.Nodes[nodeIndex].Skin, Is.EqualTo(skinIndex));
                Assert.That(document.Skins[skinIndex].Joints,
                    Is.EqualTo(new[] { nodeIndex }));
                Assert.That(document.Animations[index].Samplers[0].Input,
                    Is.EqualTo(index));
                Assert.That(document.Scenes[index].Nodes,
                    Is.EqualTo(new[] { nodeIndex }));
            });
            index++;
        }

        Assert.That(document.DefaultScene, Is.EqualTo(FindScene(document, "z")));
    }

    private static int FindNode(GltfDocument document, string name)
    {
        int index = 0;
        while (document.Nodes[index].Name != name)
        {
            index++;
        }

        return index;
    }

    private static int FindSkin(GltfDocument document, string name)
    {
        int index = 0;
        while (document.Skins[index].Name != name)
        {
            index++;
        }

        return index;
    }

    private static int FindScene(GltfDocument document, string name)
    {
        int index = 0;
        while (document.Scenes[index].Name != name)
        {
            index++;
        }

        return index;
    }

    private static string GetResourceOrder(GltfDocument document)
    {
        return document.Resources.Resources[0].EffectiveUri + "|"
            + document.Resources.Resources[1].EffectiveUri;
    }

    private static string WriteCanonicalJson(GltfDocument document)
    {
        JsonObject json = new GltfJsonWriter().WriteDocument(
            document,
            new GltfWriteOptions());
        return new DeterministicJsonWriter().WriteCanonicalValue(json);
    }

    private static void AddRootExtensions(GltfDocument document, bool reverse)
    {
        GltfLightsPunctualExtension lights = new GltfLightsPunctualExtension();
        GltfMaterialsVariantsExtension variants = new GltfMaterialsVariantsExtension();
        GltfXmpJsonLdExtension xmp = new GltfXmpJsonLdExtension { HasPackets = true };
        int index = 0;
        while (index < 2)
        {
            string name;
            if (reverse)
            {
                name = index == 0 ? "z" : "a";
            }
            else
            {
                name = index == 0 ? "a" : "z";
            }

            lights.Lights.Add(new GltfPunctualLight { Name = name, Type = "point" });
            variants.Variants.Add(new GltfMaterialVariant { Name = name });
            GltfXmpJsonLdPacket packet = new GltfXmpJsonLdPacket();
            packet.Properties["name"] = name;
            xmp.Packets.Add(packet);
            index++;
        }

        GltfTypedExtensionData typedLights = new GltfTypedExtensionData();
        typedLights.LightsPunctual = lights;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual]
            = typedLights;
        GltfTypedExtensionData typedVariants = new GltfTypedExtensionData();
        typedVariants.MaterialsVariants = variants;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrMaterialsVariants]
            = typedVariants;
        GltfTypedExtensionData typedXmp = new GltfTypedExtensionData();
        typedXmp.XmpJsonLd = xmp;
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = typedXmp;
    }

    private static void AddDeclarations(GltfDocument document)
    {
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrLightsPunctual);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrMaterialsVariants);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrXmpJsonLd);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrTextureBasisu);
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrDracoMeshCompression);
    }

    private static void AddBasisu(GltfTexture texture, int image)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.TextureBasisu = new GltfTextureBasisuExtension
        {
            HasSource = true,
            Source = image
        };
        texture.PropertyJson.TypedExtensions[GltfExtensionNames.KhrTextureBasisu] = typed;
    }

    private static void AddXmp(GltfMaterial material, int packet)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.XmpJsonLdAssignment = new GltfXmpJsonLdAssignmentExtension
        {
            HasPacket = true,
            Packet = packet
        };
        material.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = typed;
    }

    private static void AddDraco(GltfMeshPrimitive primitive, int bufferView)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.DracoMeshCompression = new GltfDracoMeshCompressionExtension
        {
            BufferView = bufferView
        };
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrDracoMeshCompression] = typed;
    }

    private static void AddVariant(
        GltfMeshPrimitive primitive,
        int material,
        int variant)
    {
        GltfMaterialVariantMapping mapping = new GltfMaterialVariantMapping
        {
            Material = material
        };
        mapping.Variants.Add(variant);
        GltfMaterialsVariantsPrimitiveExtension extension =
            new GltfMaterialsVariantsPrimitiveExtension();
        extension.Mappings.Add(mapping);
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.MaterialsVariantsPrimitive = extension;
        primitive.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrMaterialsVariants] = typed;
    }

    private static void AddLight(GltfNode node, int light)
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.PunctualLightNode = new GltfPunctualLightNodeExtension { Light = light };
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrLightsPunctual] = typed;
    }
}
