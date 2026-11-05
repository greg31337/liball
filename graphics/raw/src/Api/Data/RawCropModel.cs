namespace raw;

/// <summary>Models the non-destructive stored, active, default, and caller-selected crop hierarchy.</summary>
public class RawCropModel
{
    /// <summary>The complete stored image bounds.</summary>
    public RawArea? StoredArea;
    /// <summary>The usable sensor area.</summary>
    public RawArea? ActiveArea;
    /// <summary>The camera-recommended crop, or <c>null</c>.</summary>
    public RawArea? DefaultCrop;
    /// <summary>The caller-selected crop, or <c>null</c>.</summary>
    public RawArea? UserCrop;
    /// <summary>The effective crop selected in user, default, active, then stored precedence order.</summary>
    public RawArea? SelectedArea;

    /// <summary>Creates a crop hierarchy from image metadata and an optional caller crop.</summary>
    /// <param name="info">Stored image geometry and camera-defined areas.</param>
    /// <param name="userCrop">An optional caller crop in stored image coordinates.</param>
    /// <exception cref="RawFormatException">The areas are invalid or a visible crop escapes the active area.</exception>
    public RawCropModel(RawImageInfo info, RawArea? userCrop)
    {
        if (info == null || info.StoredArea == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Image metadata with stored bounds is required for crop modeling.", -1);
        }
        if (!info.StoredArea.Validate(info.Width, info.Height).IsValid() || info.ActiveArea != null && !info.ActiveArea.Validate(info.Width, info.Height).IsValid() || info.DefaultCrop != null && !info.DefaultCrop.Validate(info.Width, info.Height).IsValid() || userCrop != null && !userCrop.Validate(info.Width, info.Height).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "Every crop must be nonempty and contained in the stored image.", -1);
        }
        if (info.StoredArea.Kind != RawAreaKind.StoredImage || info.ActiveArea != null && info.ActiveArea.Kind != RawAreaKind.Active || info.DefaultCrop != null && info.DefaultCrop.Kind != RawAreaKind.DefaultCrop || userCrop != null && userCrop.Kind != RawAreaKind.UserCrop)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Stored, active, default, and user areas require their matching semantic kinds.", -1);
        }
        RawArea visible = info.ActiveArea == null ? info.StoredArea : info.ActiveArea;
        if (info.DefaultCrop != null && !Contains(visible, info.DefaultCrop) || userCrop != null && !Contains(visible, userCrop))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "Default and user crops must be contained in the active sensor area.", -1);
        }
        StoredArea = Copy(info.StoredArea);
        ActiveArea = Copy(info.ActiveArea);
        DefaultCrop = Copy(info.DefaultCrop);
        UserCrop = Copy(userCrop);
        RawArea? selected = UserCrop == null ? DefaultCrop == null ? ActiveArea == null ? StoredArea : ActiveArea : DefaultCrop : UserCrop;
        SelectedArea = Copy(selected);
    }

    /// <summary>Returns whether one area is completely contained by another.</summary>
    /// <param name="outer">The containing area.</param>
    /// <param name="inner">The candidate contained area.</param>
    /// <returns><c>true</c> when every coordinate in the inner area lies in the outer area.</returns>
    internal static bool Contains(RawArea? outer, RawArea inner)
    {
        if (outer == null || inner == null || outer.Width <= 0 || outer.Height <= 0 || inner.Width <= 0 || inner.Height <= 0)
        {
            return false;
        }
        return inner.X >= outer.X && inner.Y >= outer.Y && (long)inner.X + inner.Width <= (long)outer.X + outer.Width && (long)inner.Y + inner.Height <= (long)outer.Y + outer.Height;
    }

    /// <summary>Validates the crop hierarchy against complete stored image dimensions.</summary>
    /// <param name="imageWidth">The stored image width.</param>
    /// <param name="imageHeight">The stored image height.</param>
    /// <returns>All crop-model validation findings.</returns>
    internal RawValidationReport Validate(int imageWidth, int imageHeight)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (StoredArea == null || StoredArea.Kind != RawAreaKind.StoredImage || StoredArea.X != 0 || StoredArea.Y != 0 || StoredArea.Width != imageWidth || StoredArea.Height != imageHeight || !StoredArea.Validate(imageWidth, imageHeight).IsValid())
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "CropModel.StoredArea", "The stored area must exactly match the complete image.");
        }
        if (ActiveArea != null && (ActiveArea.Kind != RawAreaKind.Active || !ActiveArea.Validate(imageWidth, imageHeight).IsValid()))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "CropModel.ActiveArea", "The active area must be bounded and have active semantics.");
        }
        if (DefaultCrop != null && (DefaultCrop.Kind != RawAreaKind.DefaultCrop || !DefaultCrop.Validate(imageWidth, imageHeight).IsValid()))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "CropModel.DefaultCrop", "The default crop must be bounded and have default-crop semantics.");
        }
        if (UserCrop != null && (UserCrop.Kind != RawAreaKind.UserCrop || !UserCrop.Validate(imageWidth, imageHeight).IsValid()))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "CropModel.UserCrop", "The user crop must be bounded and have user-crop semantics.");
        }
        RawArea? visible = ActiveArea == null ? StoredArea : ActiveArea;
        if (DefaultCrop != null && !Contains(visible, DefaultCrop) || UserCrop != null && !Contains(visible, UserCrop))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "CropModel.VisibleAreas", "Default and user crops must be contained in the active sensor area.");
        }
        RawArea? expected = UserCrop == null ? DefaultCrop == null ? ActiveArea == null ? StoredArea : ActiveArea : DefaultCrop : UserCrop;
        if (!Same(SelectedArea, expected))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "CropModel.SelectedArea", "The selected crop must follow user, default, active, then stored precedence.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static RawArea? Copy(RawArea? area)
    {
        if (area == null)
        {
            return null;
        }
        return new RawArea(area.Kind, area.X, area.Y, area.Width, area.Height);
    }

    internal static bool Same(RawArea? first, RawArea? second)
    {
        if (first == null || second == null)
        {
            return first == second;
        }
        return first.Kind == second.Kind && first.X == second.X && first.Y == second.Y && first.Width == second.Width && first.Height == second.Height;
    }
}
