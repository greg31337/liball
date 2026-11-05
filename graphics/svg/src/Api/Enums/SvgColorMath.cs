namespace svg;

/// <summary>Defines stable color operation failures.</summary>
public enum SvgColorOperationError
{
    /// <summary>No operation failure occurred.</summary>
    None = 0,

    /// <summary>An argument or mutable color state was invalid.</summary>
    InvalidArgument = 1,

    /// <summary>A current-color placeholder must be resolved before the operation.</summary>
    CurrentColorUnresolved = 2,

    /// <summary>The operation produced a non-finite channel.</summary>
    NonFiniteResult = 3
}
