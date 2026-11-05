namespace gltf.Tests.Api.Static;

[TestFixture]
public class GltfExtensionNamesTests
{
    [Test]
    public void Constants_CoverTheRegistrySnapshotWithoutDuplicates()
    {
        string[] expected =
        [
            "ADOBE_materials_clearcoat_specular",
            "ADOBE_materials_clearcoat_tint",
            "ADOBE_materials_thin_transparency",
            "AGI_articulations",
            "AGI_stk_metadata",
            "CESIUM_primitive_outline",
            "EXT_lights_ies",
            "EXT_lights_image_based",
            "EXT_mesh_gpu_instancing",
            "EXT_mesh_manifold",
            "EXT_mesh_primitive_edge_visibility",
            "EXT_mesh_primitive_restart",
            "EXT_meshopt_compression",
            "EXT_texture_astc",
            "EXT_texture_procedurals_mx_1_39",
            "EXT_texture_webp",
            "FB_geometry_metadata",
            "GODOT_single_root",
            "GRIFFEL_bim_data",
            "KHR_accessor_float64",
            "KHR_animation_pointer",
            "KHR_audio_graph",
            "KHR_collision_shapes",
            "KHR_draco_mesh_compression",
            "KHR_gaussian_splatting",
            "KHR_interactivity",
            "KHR_lights_punctual",
            "KHR_materials_anisotropy",
            "KHR_materials_clearcoat",
            "KHR_materials_diffuse_transmission",
            "KHR_materials_dispersion",
            "KHR_materials_emissive_strength",
            "KHR_materials_ior",
            "KHR_materials_iridescence",
            "KHR_materials_pbrSpecularGlossiness",
            "KHR_materials_sheen",
            "KHR_materials_specular",
            "KHR_materials_subsurface",
            "KHR_materials_transmission",
            "KHR_materials_unlit",
            "KHR_materials_variants",
            "KHR_materials_volume",
            "KHR_mesh_quantization",
            "KHR_meshopt_compression",
            "KHR_node_hoverability",
            "KHR_node_selectability",
            "KHR_node_visibility",
            "KHR_physics_rigid_bodies",
            "KHR_techniques_webgl",
            "KHR_texture_basisu",
            "KHR_texture_procedurals",
            "KHR_texture_transform",
            "KHR_texture_video",
            "KHR_xmp",
            "KHR_xmp_json_ld",
            "MPEG_accessor_timed",
            "MPEG_animation_timing",
            "MPEG_audio_spatial",
            "MPEG_buffer_circular",
            "MPEG_media",
            "MPEG_mesh_linking",
            "MPEG_scene_dynamic",
            "MPEG_texture_video",
            "MPEG_viewport_recommended",
            "MSFT_lod",
            "MSFT_packing_normalRoughnessMetallic",
            "MSFT_packing_occlusionRoughnessMetallic",
            "MSFT_texture_dds",
            "NV_materials_mdl"
        ];

        Assert.That(StaticStringData.ReadConstants(typeof(GltfExtensionNames)), Is.EqualTo(expected));
    }
}

