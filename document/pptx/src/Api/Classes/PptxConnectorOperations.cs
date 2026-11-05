using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxConnector values.
/// </summary>
public class PptxConnectorOperations : IPptxConnectorOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxConnectorOperations()
    {
    }

    /// <summary>
    /// Returns the preset geometry without changing the connector.
    /// </summary>
    /// <param name="connectorType">The connector type value to use.</param>
    /// <returns>The resulting text.</returns>
    public string GetPresetGeometry(string connectorType)
    {
        return PptxConnectorCore.GetPresetGeometry(connectorType);
    }

    /// <summary>
    /// Reports whether the connector is connector preset.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsConnectorPreset(string presetGeometry)
    {
        return PptxConnectorCore.IsConnectorPreset(presetGeometry);
    }
}
