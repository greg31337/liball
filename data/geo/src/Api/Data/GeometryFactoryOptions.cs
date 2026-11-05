namespace geo;

/// <summary>Specifies default model and topology policies used by geometry construction.</summary>
public class GeometryFactoryOptions
{
    /// <summary>The spatial reference identifier assigned to constructed values.</summary>
    public int SpatialReferenceId = GeometryConstants.UnknownSpatialReferenceId;

    /// <summary>The coordinate layout assigned to constructed values.</summary>
    public CoordinateLayout CoordinateLayout;

    /// <summary>The precision representation applied during construction.</summary>
    public PrecisionModelType PrecisionModelType;

    /// <summary>The preferred orientation for polygon shells.</summary>
    public RingOrientation RingOrientation;

    /// <summary>The boundary-node rule used by topology operations.</summary>
    public BoundaryNodeRule BoundaryNodeRule;

    /// <summary>The fixed precision scale, where one represents unit spacing.</summary>
    public double PrecisionScale = 1.0;

    /// <summary>The fixed precision X origin offset.</summary>
    public double PrecisionOffsetX;

    /// <summary>The fixed precision Y origin offset.</summary>
    public double PrecisionOffsetY;
}
