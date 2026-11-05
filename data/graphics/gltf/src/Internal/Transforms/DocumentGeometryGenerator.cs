using System;
using System.Collections.Generic;

namespace gltf;

internal class NormalGenerationData
{
    internal double[] Values = [];
    internal int DegenerateTriangles;
    internal int UndefinedVertices;
    internal int ValidTriangles;
}

internal class DocumentGeometryGenerator
{
    internal AccessorBoundsCalculator Bounds = new AccessorBoundsCalculator();
    internal AccessorCodecEngine Accessors = new AccessorCodecEngine();
    internal AccessorEncoder Encoder = new AccessorEncoder();
    internal DocumentCloner Cloner = new DocumentCloner();
    internal DocumentCompactor Mapping = new DocumentCompactor();
    internal DocumentVertexTransformer Vertices = new DocumentVertexTransformer();
    internal BufferViewTargetCleaner Targets = new BufferViewTargetCleaner();
    internal List<int> SourceBufferViews = new List<int>();

    internal TransformStepExecution GenerateGeometry(
        GltfDocument document,
        GltfTransformStep step)
    {
        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        execution.Result.Remap = Mapping.CreateIdentityRemap(document);
        bool changed;
        if (step.Kind == GltfTransformKind.GenerateBounds)
        {
            changed = RegenerateAccessorBounds(document, execution.Result);
        }
        else
        {
            changed = GenerateDocumentNormals(document, execution.Result);
        }

        if (changed && document.Container != null)
        {
            document.Container = null;
            execution.Result.ChangedObjects.Add("container");
        }

        return execution;
    }

    internal bool RegenerateAccessorBounds(
        GltfDocument document,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int accessorIndex = 0;
        while (accessorIndex < document.Accessors.Count)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            if (IsBoundsShape(accessor.Type))
            {
                AccessorBoundsResult calculated = Bounds.CalculateAccessorBounds(
                    document,
                    accessorIndex);
                if (calculated.Available
                    && calculated.HasValues
                    && calculated.ValuesFinite
                    && (!BoundsEqual(accessor.Minimum, calculated.Minimum)
                        || !BoundsEqual(accessor.Maximum, calculated.Maximum)))
                {
                    accessor.Minimum = calculated.Minimum;
                    accessor.Maximum = calculated.Maximum;
                    result.ChangedObjects.Add($"accessors/{accessorIndex}");
                    changed = true;
                }
            }

            accessorIndex++;
        }

        return changed;
    }

    internal bool IsBoundsShape(GltfAccessorType type)
    {
        return type == GltfAccessorType.Scalar
            || type == GltfAccessorType.Vector2
            || type == GltfAccessorType.Vector3
            || type == GltfAccessorType.Vector4;
    }

    internal bool BoundsEqual(double[]? current, double[] calculated)
    {
        if (current == null || current.Length != calculated.Length)
        {
            return false;
        }

        int index = 0;
        while (index < current.Length)
        {
            if (current[index] != calculated[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool GenerateDocumentNormals(
        GltfDocument document,
        GltfTransformStepResult result)
    {
        SourceBufferViews.Clear();
        bool changed = false;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                string pointer = $"/meshes/{meshIndex}/primitives/{primitiveIndex}";
                bool primitiveChanged = GeneratePrimitiveNormals(
                    document,
                    primitive,
                    pointer,
                    result);
                if (primitiveChanged)
                {
                    result.ChangedObjects.Add(
                        $"meshes/{meshIndex}/primitives/{primitiveIndex}");
                    changed = true;
                }

                primitiveIndex++;
            }

            meshIndex++;
        }

        if (changed)
        {
            Targets.ClearUnusedTargets(document, result, SourceBufferViews);
        }

        return changed;
    }

    internal bool GeneratePrimitiveNormals(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        string pointer,
        GltfTransformStepResult result)
    {
        if (!IsTriangleMode(primitive.Mode))
        {
            return false;
        }

        PrimitiveVertexData? vertexData = Vertices.ReadPrimitiveData(
            document,
            primitive);
        if (vertexData == null)
        {
            return false;
        }

        int positionAttribute = FindAttribute(primitive, "POSITION");
        if (positionAttribute == GltfConstants.AbsentIndex)
        {
            return false;
        }

        int positionAccessor = primitive.Attributes[positionAttribute].Accessor;
        GltfAccessor position = document.Accessors[positionAccessor];
        if (position.Type != GltfAccessorType.Vector3)
        {
            return false;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = positionAccessor;
        GltfAccessorData decoded = Accessors.ReadFloatingData(request);
        if (decoded.Status != GltfOperationStatus.Success
            || decoded.FloatingValues.Length != vertexData.VertexCount * 3
            || !ValuesFinite(decoded.FloatingValues))
        {
            return false;
        }

        NormalGenerationData generated = CalculateAreaWeightedNormals(
            decoded.FloatingValues,
            vertexData.Indices,
            primitive.Mode,
            vertexData.VertexCount);
        if (generated.DegenerateTriangles > 0 || generated.UndefinedVertices > 0)
        {
            AddDegenerateDiagnostic(result, pointer, generated);
        }

        if (generated.ValidTriangles == 0)
        {
            return false;
        }

        int normalAttribute = FindAttribute(primitive, "NORMAL");
        int sourceAccessor = GltfConstants.AbsentIndex;
        if (normalAttribute != GltfConstants.AbsentIndex)
        {
            sourceAccessor = primitive.Attributes[normalAttribute].Accessor;
            RecordSourceBufferView(document, sourceAccessor);
        }

        AccessorEncodingResult encoding = Encoder.EncodeFloatingAccessor(
            generated.Values,
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            false);
        AccessorCreationResult created = Encoder.CreateAccessorStorage(
            document,
            encoding,
            true);
        GltfBufferView createdView = document.BufferViews[created.BufferView];
        createdView.HasTarget = true;
        createdView.Target = GltfBufferViewTarget.ArrayBuffer;
        if (sourceAccessor >= 0 && sourceAccessor < document.Accessors.Count)
        {
            CopyAccessorMetadata(
                document.Accessors[sourceAccessor],
                document.Accessors[created.Accessor]);
        }

        if (normalAttribute == GltfConstants.AbsentIndex)
        {
            GltfAttribute normal = new GltfAttribute();
            normal.Semantic = "NORMAL";
            normal.Accessor = created.Accessor;
            primitive.Attributes.Add(normal);
        }
        else
        {
            primitive.Attributes[normalAttribute].Accessor = created.Accessor;
        }

        return true;
    }

    internal bool IsTriangleMode(GltfPrimitiveMode mode)
    {
        return mode == GltfPrimitiveMode.Triangles
            || mode == GltfPrimitiveMode.TriangleStrip
            || mode == GltfPrimitiveMode.TriangleFan;
    }

    internal int FindAttribute(GltfMeshPrimitive primitive, string semantic)
    {
        int index = 0;
        while (index < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[index];
            if (attribute.Semantic == semantic)
            {
                return index;
            }

            index++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal bool ValuesFinite(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!double.IsFinite(values[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal NormalGenerationData CalculateAreaWeightedNormals(
        double[] positions,
        uint[] indices,
        GltfPrimitiveMode mode,
        int vertexCount)
    {
        NormalGenerationData result = new NormalGenerationData();
        result.Values = new double[vertexCount * 3];
        if (mode == GltfPrimitiveMode.Triangles)
        {
            int index = 0;
            while (index + 2 < indices.Length)
            {
                IncludeTriangle(
                    positions,
                    result,
                    indices[index],
                    indices[index + 1],
                    indices[index + 2]);
                index += 3;
            }
        }
        else if (mode == GltfPrimitiveMode.TriangleStrip)
        {
            int index = 0;
            while (index + 2 < indices.Length)
            {
                if (index % 2 == 0)
                {
                    IncludeTriangle(
                        positions,
                        result,
                        indices[index],
                        indices[index + 1],
                        indices[index + 2]);
                }
                else
                {
                    IncludeTriangle(
                        positions,
                        result,
                        indices[index + 1],
                        indices[index],
                        indices[index + 2]);
                }

                index++;
            }
        }
        else
        {
            int index = 1;
            while (index + 1 < indices.Length)
            {
                IncludeTriangle(
                    positions,
                    result,
                    indices[0],
                    indices[index],
                    indices[index + 1]);
                index++;
            }
        }

        NormalizeGeneratedNormals(result);
        return result;
    }

    internal void IncludeTriangle(
        double[] positions,
        NormalGenerationData result,
        uint first,
        uint second,
        uint third)
    {
        if (first == second || first == third || second == third)
        {
            result.DegenerateTriangles++;
            return;
        }

        int firstOffset = (int)first * 3;
        int secondOffset = (int)second * 3;
        int thirdOffset = (int)third * 3;
        double firstX = positions[secondOffset] - positions[firstOffset];
        double firstY = positions[secondOffset + 1] - positions[firstOffset + 1];
        double firstZ = positions[secondOffset + 2] - positions[firstOffset + 2];
        double secondX = positions[thirdOffset] - positions[firstOffset];
        double secondY = positions[thirdOffset + 1] - positions[firstOffset + 1];
        double secondZ = positions[thirdOffset + 2] - positions[firstOffset + 2];
        double normalX = firstY * secondZ - firstZ * secondY;
        double normalY = firstZ * secondX - firstX * secondZ;
        double normalZ = firstX * secondY - firstY * secondX;
        double lengthSquared = normalX * normalX
            + normalY * normalY
            + normalZ * normalZ;
        if (!double.IsFinite(lengthSquared) || lengthSquared == 0.0)
        {
            result.DegenerateTriangles++;
            return;
        }

        AddNormal(result.Values, firstOffset, normalX, normalY, normalZ);
        AddNormal(result.Values, secondOffset, normalX, normalY, normalZ);
        AddNormal(result.Values, thirdOffset, normalX, normalY, normalZ);
        result.ValidTriangles++;
    }

    internal void AddNormal(
        double[] normals,
        int offset,
        double x,
        double y,
        double z)
    {
        normals[offset] += x;
        normals[offset + 1] += y;
        normals[offset + 2] += z;
    }

    internal void NormalizeGeneratedNormals(NormalGenerationData result)
    {
        int offset = 0;
        while (offset + 2 < result.Values.Length)
        {
            double x = result.Values[offset];
            double y = result.Values[offset + 1];
            double z = result.Values[offset + 2];
            double length = Math.Sqrt(x * x + y * y + z * z);
            if (double.IsFinite(length) && length > 0.0)
            {
                result.Values[offset] = x / length;
                result.Values[offset + 1] = y / length;
                result.Values[offset + 2] = z / length;
            }
            else
            {
                result.Values[offset] = 0.0;
                result.Values[offset + 1] = 0.0;
                result.Values[offset + 2] = 1.0;
                result.UndefinedVertices++;
            }

            offset += 3;
        }
    }

    internal void AddDegenerateDiagnostic(
        GltfTransformStepResult result,
        string pointer,
        NormalGenerationData generated)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = GltfIssueCodes.AccessorIndexTriangleDegenerate;
        issue.Severity = GltfValidationSeverity.Information;
        issue.Message = $"Normal generation encountered {generated.DegenerateTriangles} degenerate triangles and {generated.UndefinedVertices} vertices without an accumulated normal.";
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
    }

    internal void RecordSourceBufferView(GltfDocument document, int accessorIndex)
    {
        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            return;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (accessor.HasBufferView
            && accessor.BufferView >= 0
            && accessor.BufferView < document.BufferViews.Count
            && !SourceBufferViews.Contains(accessor.BufferView))
        {
            SourceBufferViews.Add(accessor.BufferView);
        }
    }

    internal void CopyAccessorMetadata(GltfAccessor source, GltfAccessor target)
    {
        target.Name = source.Name;
        Cloner.Clones.Clear();
        Cloner.CloneJson = true;
        Cloner.CloneResourceBytes = true;
        target.PropertyJson = (GltfPropertyJson)(Cloner.CloneValue(source.PropertyJson)
            ?? new GltfPropertyJson());
    }
}
