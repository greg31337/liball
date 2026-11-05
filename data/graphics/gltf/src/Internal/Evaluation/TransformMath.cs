using System;
using System.Collections.Generic;
using System.Numerics;

namespace gltf;

internal class NodeTransformEvaluation
{
    internal int Node = GltfConstants.AbsentIndex;
    internal bool IsValid = true;
    internal bool HasLocalTransform = true;
    internal Matrix4x4 LocalTransform = Matrix4x4.Identity;
    internal bool HasWorldTransform;
    internal Matrix4x4 WorldTransform = Matrix4x4.Identity;
    internal bool MatrixDecomposable = true;
    internal bool RotationWasNormalized;
    internal double WorldDeterminant;
    internal bool HasNegativeWorldDeterminant;
    internal bool HasNormalTransform;
    internal Matrix4x4 NormalTransform = Matrix4x4.Identity;
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class SceneTransformAnalysis
{
    internal bool IsValid = true;
    internal SceneGraphAnalysis Graph = new SceneGraphAnalysis();
    internal List<NodeTransformEvaluation> Nodes = new List<NodeTransformEvaluation>();
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class NormalTransformCalculation
{
    internal bool HasTransform;
    internal double Determinant;
    internal Matrix4x4 Transform = Matrix4x4.Identity;
}

internal class TransformMath
{
    internal SceneTransformAnalysis EvaluateDocumentTransforms(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        SceneTransformAnalysis result = new SceneTransformAnalysis();
        result.Graph = new SceneGraphEvaluator().AnalyzeDocument(document);
        result.IsValid = result.Graph.IsValid;
        CopyGraphDiagnostics(result);
        if (document.Nodes == null)
        {
            return result;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            NodeTransformEvaluation nodeResult = EvaluateLocalTransform(
                document.Nodes[nodeIndex],
                nodeIndex);
            result.Nodes.Add(nodeResult);
            nodeIndex++;
        }

        ResolveWorldTransforms(result);
        CollectTransformDiagnostics(result);
        return result;
    }

    internal NodeTransformEvaluation EvaluateLocalTransform(GltfNode node, int nodeIndex)
    {
        NodeTransformEvaluation result = new NodeTransformEvaluation();
        result.Node = nodeIndex;
        if (node == null)
        {
            result.IsValid = false;
            result.HasLocalTransform = false;
            return result;
        }

        string pointer = $"/nodes/{nodeIndex}";
        if (node.HasMatrix)
        {
            if (node.HasTranslation || node.HasRotation || node.HasScale)
            {
                AddNodeTransformIssue(
                    result,
                    GltfIssueCodes.NodeMatrixTrs,
                    GltfValidationSeverity.Error,
                    "A node cannot define both matrix and TRS transforms.",
                    pointer);
            }

            if (!ValidateNumericArray(node.Matrix, 16, result, $"{pointer}/matrix"))
            {
                result.HasLocalTransform = false;
                return result;
            }

            result.LocalTransform = ReadColumnMajorMatrix(node.Matrix);
            result.MatrixDecomposable = IsMatrixDecomposableToTrs(result.LocalTransform);
            if (!result.MatrixDecomposable)
            {
                AddNodeTransformIssue(
                    result,
                    GltfIssueCodes.NodeMatrixNonTrs,
                    GltfValidationSeverity.Error,
                    "The node matrix is not decomposable to translation, rotation, and scale.",
                    $"{pointer}/matrix");
            }

            return result;
        }

        Vector3 translation = Vector3.Zero;
        Vector3 scale = Vector3.One;
        Quaternion rotation = Quaternion.Identity;
        if (node.HasTranslation)
        {
            if (!ValidateNumericArray(node.Translation, 3, result, $"{pointer}/translation"))
            {
                result.HasLocalTransform = false;
                return result;
            }

            translation = new Vector3(
                (float)node.Translation[0],
                (float)node.Translation[1],
                (float)node.Translation[2]);
        }

        if (node.HasScale)
        {
            if (!ValidateNumericArray(node.Scale, 3, result, $"{pointer}/scale"))
            {
                result.HasLocalTransform = false;
                return result;
            }

            scale = new Vector3(
                (float)node.Scale[0],
                (float)node.Scale[1],
                (float)node.Scale[2]);
        }

        if (node.HasRotation)
        {
            if (!ValidateNumericArray(node.Rotation, 4, result, $"{pointer}/rotation"))
            {
                result.HasLocalTransform = false;
                return result;
            }

            double lengthSquared = node.Rotation[0] * node.Rotation[0]
                + node.Rotation[1] * node.Rotation[1]
                + node.Rotation[2] * node.Rotation[2]
                + node.Rotation[3] * node.Rotation[3];
            if (!double.IsFinite(lengthSquared) || lengthSquared == 0.0)
            {
                AddNodeTransformIssue(
                    result,
                    GltfIssueCodes.ValueNotInRange,
                    GltfValidationSeverity.Error,
                    "The node rotation quaternion must have a finite non-zero length.",
                    $"{pointer}/rotation");
                result.HasLocalTransform = false;
                return result;
            }

            double length = Math.Sqrt(lengthSquared);
            if (Math.Abs(lengthSquared - 1.0) > 0.00001)
            {
                AddNodeTransformIssue(
                    result,
                    GltfIssueCodes.RotationNonUnit,
                    GltfValidationSeverity.Error,
                    "The node rotation quaternion is not normalized.",
                    $"{pointer}/rotation");
            }

            if (lengthSquared != 1.0)
            {
                result.RotationWasNormalized = true;
            }

            rotation = new Quaternion(
                (float)(node.Rotation[0] / length),
                (float)(node.Rotation[1] / length),
                (float)(node.Rotation[2] / length),
                (float)(node.Rotation[3] / length));
        }

        result.LocalTransform = Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(translation);
        if (!IsFiniteMatrix(result.LocalTransform))
        {
            AddNodeTransformIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "The node TRS values exceed the transform matrix range.",
                pointer);
            result.HasLocalTransform = false;
            result.LocalTransform = Matrix4x4.Identity;
        }

        return result;
    }

    internal bool ValidateNumericArray(
        double[]? values,
        int expectedLength,
        NodeTransformEvaluation result,
        string pointer)
    {
        if (values == null || values.Length != expectedLength)
        {
            AddNodeTransformIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                $"The transform array must contain {expectedLength} values.",
                pointer);
            return false;
        }

        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            double value = values[valueIndex];
            if (!double.IsFinite(value) || Math.Abs(value) > float.MaxValue)
            {
                AddNodeTransformIssue(
                    result,
                    GltfIssueCodes.ValueNotInRange,
                    GltfValidationSeverity.Error,
                    "The transform value must be finite and representable in a transform matrix.",
                    $"{pointer}/{valueIndex}");
                return false;
            }

            valueIndex++;
        }

        return true;
    }

    internal Matrix4x4 ReadColumnMajorMatrix(double[] values)
    {
        return new Matrix4x4(
            (float)values[0], (float)values[1], (float)values[2], (float)values[3],
            (float)values[4], (float)values[5], (float)values[6], (float)values[7],
            (float)values[8], (float)values[9], (float)values[10], (float)values[11],
            (float)values[12], (float)values[13], (float)values[14], (float)values[15]);
    }

    internal bool IsMatrixDecomposableToTrs(Matrix4x4 matrix)
    {
        if (!IsFiniteMatrix(matrix)
            || Math.Abs(matrix.M14) > 0.00001f
            || Math.Abs(matrix.M24) > 0.00001f
            || Math.Abs(matrix.M34) > 0.00001f
            || Math.Abs(matrix.M44 - 1.0f) > 0.00001f)
        {
            return false;
        }

        double firstLengthSquared = (double)matrix.M11 * matrix.M11
            + (double)matrix.M12 * matrix.M12
            + (double)matrix.M13 * matrix.M13;
        double secondLengthSquared = (double)matrix.M21 * matrix.M21
            + (double)matrix.M22 * matrix.M22
            + (double)matrix.M23 * matrix.M23;
        double thirdLengthSquared = (double)matrix.M31 * matrix.M31
            + (double)matrix.M32 * matrix.M32
            + (double)matrix.M33 * matrix.M33;
        double firstSecondDot = (double)matrix.M11 * matrix.M21
            + (double)matrix.M12 * matrix.M22
            + (double)matrix.M13 * matrix.M23;
        double firstThirdDot = (double)matrix.M11 * matrix.M31
            + (double)matrix.M12 * matrix.M32
            + (double)matrix.M13 * matrix.M33;
        double secondThirdDot = (double)matrix.M21 * matrix.M31
            + (double)matrix.M22 * matrix.M32
            + (double)matrix.M23 * matrix.M33;
        return AreRowsOrthogonal(firstSecondDot, firstLengthSquared, secondLengthSquared)
            && AreRowsOrthogonal(firstThirdDot, firstLengthSquared, thirdLengthSquared)
            && AreRowsOrthogonal(secondThirdDot, secondLengthSquared, thirdLengthSquared);
    }

    internal bool AreRowsOrthogonal(
        double dotProduct,
        double firstLengthSquared,
        double secondLengthSquared)
    {
        if (firstLengthSquared == 0.0 || secondLengthSquared == 0.0)
        {
            return true;
        }

        double scale = Math.Sqrt(firstLengthSquared * secondLengthSquared);
        return Math.Abs(dotProduct) <= 0.00001 * scale;
    }

    internal void ResolveWorldTransforms(SceneTransformAnalysis result)
    {
        int[] states = new int[result.Nodes.Count];
        int nodeIndex = 0;
        while (nodeIndex < result.Nodes.Count)
        {
            if (states[nodeIndex] == 0)
            {
                ResolveWorldTransformChain(result, states, nodeIndex);
            }

            nodeIndex++;
        }
    }

    internal void ResolveWorldTransformChain(
        SceneTransformAnalysis result,
        int[] states,
        int startIndex)
    {
        List<int> chain = new List<int>();
        int currentIndex = startIndex;
        while (currentIndex != GltfConstants.AbsentIndex
            && states[currentIndex] == 0)
        {
            states[currentIndex] = 1;
            chain.Add(currentIndex);
            if (result.Graph.CycleNodes[currentIndex]
                || result.Graph.ParentReferences[currentIndex].Count > 1)
            {
                break;
            }

            currentIndex = result.Graph.Parents[currentIndex];
        }

        int chainPosition = chain.Count - 1;
        while (chainPosition >= 0)
        {
            int nodeIndex = chain[chainPosition];
            NodeTransformEvaluation nodeResult = result.Nodes[nodeIndex];
            int parentIndex = result.Graph.Parents[nodeIndex];
            if (!nodeResult.HasLocalTransform
                || result.Graph.CycleNodes[nodeIndex]
                || result.Graph.ParentReferences[nodeIndex].Count > 1)
            {
                nodeResult.IsValid = false;
                nodeResult.HasWorldTransform = false;
            }
            else if (parentIndex == GltfConstants.AbsentIndex)
            {
                SetWorldTransform(nodeResult, nodeResult.LocalTransform);
            }
            else if (states[parentIndex] == 2
                && result.Nodes[parentIndex].HasWorldTransform)
            {
                SetWorldTransform(
                    nodeResult,
                    nodeResult.LocalTransform * result.Nodes[parentIndex].WorldTransform);
            }
            else
            {
                nodeResult.IsValid = false;
                nodeResult.HasWorldTransform = false;
            }

            states[nodeIndex] = 2;
            chainPosition--;
        }
    }

    internal void SetWorldTransform(
        NodeTransformEvaluation result,
        Matrix4x4 worldTransform)
    {
        if (!IsFiniteMatrix(worldTransform))
        {
            AddNodeTransformIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "The composed world transform exceeds the matrix range.",
                $"/nodes/{result.Node}");
            result.HasWorldTransform = false;
            return;
        }

        result.HasWorldTransform = true;
        result.WorldTransform = worldTransform;
        NormalTransformCalculation normal = CalculateNormalTransform(worldTransform);
        result.WorldDeterminant = normal.Determinant;
        result.HasNegativeWorldDeterminant = normal.Determinant < 0.0;
        result.HasNormalTransform = normal.HasTransform;
        result.NormalTransform = normal.Transform;
    }

    internal NormalTransformCalculation CalculateNormalTransform(Matrix4x4 worldTransform)
    {
        NormalTransformCalculation result = new NormalTransformCalculation();
        double a = worldTransform.M11;
        double b = worldTransform.M12;
        double c = worldTransform.M13;
        double d = worldTransform.M21;
        double e = worldTransform.M22;
        double f = worldTransform.M23;
        double g = worldTransform.M31;
        double h = worldTransform.M32;
        double i = worldTransform.M33;
        double cofactor11 = e * i - f * h;
        double cofactor12 = f * g - d * i;
        double cofactor13 = d * h - e * g;
        double cofactor21 = c * h - b * i;
        double cofactor22 = a * i - c * g;
        double cofactor23 = b * g - a * h;
        double cofactor31 = b * f - c * e;
        double cofactor32 = c * d - a * f;
        double cofactor33 = a * e - b * d;
        result.Determinant = a * cofactor11 + b * cofactor12 + c * cofactor13;
        if (!double.IsFinite(result.Determinant) || result.Determinant == 0.0)
        {
            return result;
        }

        double normal11 = cofactor11 / result.Determinant;
        double normal12 = cofactor12 / result.Determinant;
        double normal13 = cofactor13 / result.Determinant;
        double normal21 = cofactor21 / result.Determinant;
        double normal22 = cofactor22 / result.Determinant;
        double normal23 = cofactor23 / result.Determinant;
        double normal31 = cofactor31 / result.Determinant;
        double normal32 = cofactor32 / result.Determinant;
        double normal33 = cofactor33 / result.Determinant;
        if (!IsNormalValueRepresentable(normal11)
            || !IsNormalValueRepresentable(normal12)
            || !IsNormalValueRepresentable(normal13)
            || !IsNormalValueRepresentable(normal21)
            || !IsNormalValueRepresentable(normal22)
            || !IsNormalValueRepresentable(normal23)
            || !IsNormalValueRepresentable(normal31)
            || !IsNormalValueRepresentable(normal32)
            || !IsNormalValueRepresentable(normal33))
        {
            return result;
        }

        result.HasTransform = true;
        result.Transform = new Matrix4x4(
            (float)normal11, (float)normal12, (float)normal13, 0.0f,
            (float)normal21, (float)normal22, (float)normal23, 0.0f,
            (float)normal31, (float)normal32, (float)normal33, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);
        return result;
    }

    internal bool IsNormalValueRepresentable(double value)
    {
        return double.IsFinite(value) && Math.Abs(value) <= float.MaxValue;
    }

    internal bool IsFiniteMatrix(Matrix4x4 matrix)
    {
        return float.IsFinite(matrix.M11)
            && float.IsFinite(matrix.M12)
            && float.IsFinite(matrix.M13)
            && float.IsFinite(matrix.M14)
            && float.IsFinite(matrix.M21)
            && float.IsFinite(matrix.M22)
            && float.IsFinite(matrix.M23)
            && float.IsFinite(matrix.M24)
            && float.IsFinite(matrix.M31)
            && float.IsFinite(matrix.M32)
            && float.IsFinite(matrix.M33)
            && float.IsFinite(matrix.M34)
            && float.IsFinite(matrix.M41)
            && float.IsFinite(matrix.M42)
            && float.IsFinite(matrix.M43)
            && float.IsFinite(matrix.M44);
    }

    internal void CopyGraphDiagnostics(SceneTransformAnalysis result)
    {
        int diagnosticIndex = 0;
        while (diagnosticIndex < result.Graph.Diagnostics.Count)
        {
            result.Diagnostics.Add(result.Graph.Diagnostics[diagnosticIndex]);
            diagnosticIndex++;
        }
    }

    internal void CollectTransformDiagnostics(SceneTransformAnalysis result)
    {
        int nodeIndex = 0;
        while (nodeIndex < result.Nodes.Count)
        {
            NodeTransformEvaluation nodeResult = result.Nodes[nodeIndex];
            if (!nodeResult.IsValid)
            {
                result.IsValid = false;
            }

            int diagnosticIndex = 0;
            while (diagnosticIndex < nodeResult.Diagnostics.Count)
            {
                result.Diagnostics.Add(nodeResult.Diagnostics[diagnosticIndex]);
                diagnosticIndex++;
            }

            nodeIndex++;
        }
    }

    internal void AddNodeTransformIssue(
        NodeTransformEvaluation result,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            result.IsValid = false;
        }
    }
}
