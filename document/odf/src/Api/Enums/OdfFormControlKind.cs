namespace odf;

/// <summary>
/// Identifies a standard form or control kind.
/// </summary>
public enum OdfFormControlKind
{
    /// <summary>
    /// The form-control kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A form container.
    /// </summary>
    Form = 1,
    /// <summary>
    /// A push button.
    /// </summary>
    Button = 2,
    /// <summary>
    /// A single-line text control.
    /// </summary>
    Text = 3,
    /// <summary>
    /// A multi-line text control.
    /// </summary>
    TextArea = 4,
    /// <summary>
    /// A check box.
    /// </summary>
    CheckBox = 5,
    /// <summary>
    /// A radio button.
    /// </summary>
    RadioButton = 6,
    /// <summary>
    /// A list box.
    /// </summary>
    ListBox = 7,
    /// <summary>
    /// A combo box.
    /// </summary>
    ComboBox = 8,
    /// <summary>
    /// A numeric value-range control.
    /// </summary>
    ValueRange = 9,
    /// <summary>
    /// An image control.
    /// </summary>
    Image = 10,
    /// <summary>
    /// A file-selection control.
    /// </summary>
    File = 11,
    /// <summary>
    /// A date control.
    /// </summary>
    Date = 12,
    /// <summary>
    /// A time control.
    /// </summary>
    Time = 13,
    /// <summary>
    /// A frame control.
    /// </summary>
    Frame = 14,
    /// <summary>
    /// A hidden control.
    /// </summary>
    Hidden = 15,
    /// <summary>
    /// A label control.
    /// </summary>
    Label = 16,
    /// <summary>
    /// A formatted-text control.
    /// </summary>
    FormattedText = 17,
    /// <summary>
    /// A numeric control.
    /// </summary>
    Number = 18,
    /// <summary>
    /// A password control.
    /// </summary>
    Password = 19,
    /// <summary>
    /// An image-frame control.
    /// </summary>
    ImageFrame = 20,
    /// <summary>
    /// A grid control.
    /// </summary>
    Grid = 21,
    /// <summary>
    /// A control whose implementation is application-defined.
    /// </summary>
    GenericControl = 22,
    /// <summary>
    /// A grid column containing one or more column controls.
    /// </summary>
    Column = 23
}
