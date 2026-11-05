using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxConnectorOperations"/>.
/// </summary>
public interface IPptxConnectorOperations
{
    /// <summary>
    /// Returns the preset geometry without changing the connector.
    /// </summary>
    /// <param name="connectorType">The connector type value to use.</param>
    /// <returns>The resulting text.</returns>
    string GetPresetGeometry(string connectorType);
    /// <summary>
    /// Reports whether the connector is connector preset.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsConnectorPreset(string presetGeometry);
}
