namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="TrueTypeHintingData"/>.</summary>
public interface ITrueTypeHintingData
{
    /// <summary>Reports whether an <c>fpgm</c> font program is present.</summary>
    /// <returns>True when the preserved font program is nonempty.</returns>
    bool HasFontProgram();

    /// <summary>Gets the preserved <c>fpgm</c> byte count.</summary>
    /// <returns>The font-program byte count.</returns>
    int GetFontProgramLength();

    /// <summary>Copies the unexecuted <c>fpgm</c> font program.</summary>
    /// <returns>A new array containing the program bytes.</returns>
    byte[] CopyFontProgram();

    /// <summary>Reports whether a <c>prep</c> control-value program is present.</summary>
    /// <returns>True when the preserved control-value program is nonempty.</returns>
    bool HasControlValueProgram();

    /// <summary>Gets the preserved <c>prep</c> byte count.</summary>
    /// <returns>The control-value-program byte count.</returns>
    int GetControlValueProgramLength();

    /// <summary>Copies the unexecuted <c>prep</c> control-value program.</summary>
    /// <returns>A new array containing the program bytes.</returns>
    byte[] CopyControlValueProgram();

    /// <summary>Reports whether preserved <c>cvt </c> data is present.</summary>
    /// <returns>True when the control-value data is nonempty.</returns>
    bool HasControlValueData();

    /// <summary>Gets the preserved <c>cvt </c> byte count.</summary>
    /// <returns>The raw control-value-data byte count.</returns>
    int GetControlValueDataLength();

    /// <summary>Copies the instance-adjusted <c>cvt </c> table data.</summary>
    /// <returns>A new array containing the control values as FWORD bytes.</returns>
    byte[] CopyControlValueData();
}
