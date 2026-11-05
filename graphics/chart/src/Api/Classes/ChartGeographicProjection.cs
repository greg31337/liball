namespace chart;
/// <summary>
/// Projects spherical longitude and latitude coordinates with a selected geographic formula.
/// </summary>
/// <remarks>
/// Input and inverse coordinates use degrees. Projected coordinates are dimensionless and assume a unit sphere.
/// </remarks>
public class ChartGeographicProjection : IChartGeographicProjection
{
    internal ChartGeographicProjectionKind Kind;
    internal double CentralLongitude;
    internal double CentralLatitude;
    internal double FirstStandardParallel;
    internal double SecondStandardParallel;
    internal double LongitudeOriginRadians;
    internal double LatitudeOriginRadians;
    internal double FirstParallelRadians;
    internal double EquirectangularScale;
    internal double MercatorOrigin;
    internal double AlbersN;
    internal double AlbersC;
    internal double AlbersRhoOrigin;
    /// <summary>
    /// Initializes a geographic projection with its origin and standard parallels.
    /// </summary>
    /// <param name = "kind">The projection formula.</param>
    /// <param name = "centralLongitude">The central longitude in degrees.</param>
    /// <param name = "centralLatitude">The central latitude in degrees.</param>
    /// <param name = "firstStandardParallel">The equirectangular or first Albers-like standard parallel in degrees.</param>
    /// <param name = "secondStandardParallel">The second Albers-like standard parallel in degrees.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when a kind or latitude is outside its supported range.</exception>
    /// <exception cref = "System.ArgumentException">Thrown when the configuration is non-finite or singular.</exception>
    public ChartGeographicProjection(ChartGeographicProjectionKind kind, double centralLongitude, double centralLatitude, double firstStandardParallel, double secondStandardParallel)
    {
        ValidateKind(kind);
        ValidateLongitudeOrigin(centralLongitude);
        ValidateLatitudeValue(centralLatitude, "centralLatitude");
        ValidateLatitudeValue(firstStandardParallel, "firstStandardParallel");
        ValidateLatitudeValue(secondStandardParallel, "secondStandardParallel");
        Kind = kind;
        CentralLongitude = centralLongitude;
        CentralLatitude = centralLatitude;
        FirstStandardParallel = firstStandardParallel;
        SecondStandardParallel = secondStandardParallel;
        LongitudeOriginRadians = DegreesToRadians(centralLongitude);
        LatitudeOriginRadians = DegreesToRadians(centralLatitude);
        FirstParallelRadians = DegreesToRadians(firstStandardParallel);
        EquirectangularScale = 0.0;
        MercatorOrigin = 0.0;
        AlbersN = 0.0;
        AlbersC = 0.0;
        AlbersRhoOrigin = 0.0;
        ConfigureFormula(secondStandardParallel);
    }

    /// <summary>
    /// Projects a longitude and latitude expressed in degrees.
    /// </summary>
    /// <param name = "longitude">The longitude in degrees.</param>
    /// <param name = "latitude">The latitude in degrees.</param>
    /// <returns>A dimensionless projected point.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when a coordinate is non-finite or outside the visible orthographic hemisphere.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when latitude is outside its supported range.</exception>
    public ChartPoint Project(double longitude, double latitude)
    {
        ValidateLongitudeCoordinate(longitude);
        ValidateLatitudeValue(latitude, "latitude");
        double longitudeRadians = DegreesToRadians(longitude);
        double latitudeRadians = DegreesToRadians(latitude);
        double longitudeDifference = NormalizeRadians(longitudeRadians - LongitudeOriginRadians);
        switch (Kind)
        {
            case ChartGeographicProjectionKind.Equirectangular:
                return ProjectEquirectangular(longitudeDifference, latitudeRadians);
            case ChartGeographicProjectionKind.Mercator:
                return ProjectMercator(longitudeDifference, latitudeRadians);
            case ChartGeographicProjectionKind.Orthographic:
                return ProjectOrthographic(longitudeDifference, latitudeRadians);
            case ChartGeographicProjectionKind.AlbersLike:
                return ProjectAlbers(longitudeDifference, latitudeRadians);
            default:
                throw new System.InvalidOperationException("The geographic projection kind is unsupported.");
        }
    }

    /// <summary>
    /// Inverts a dimensionless projected point to longitude and latitude in degrees.
    /// </summary>
    /// <param name = "point">The projected point.</param>
    /// <returns>A point whose x value is longitude and whose y value is latitude, in degrees.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>point</c> is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentException">Thrown when the point is non-finite or outside the projection domain.</exception>
    public ChartPoint Invert(ChartPoint point)
    {
        ValidateProjectedPoint(point);
        switch (Kind)
        {
            case ChartGeographicProjectionKind.Equirectangular:
                return InvertEquirectangular(point);
            case ChartGeographicProjectionKind.Mercator:
                return InvertMercator(point);
            case ChartGeographicProjectionKind.Orthographic:
                return InvertOrthographic(point);
            case ChartGeographicProjectionKind.AlbersLike:
                return InvertAlbers(point);
            default:
                throw new System.InvalidOperationException("The geographic projection kind is unsupported.");
        }
    }

    /// <summary>Returns the configured projection kind.</summary>
    /// <returns>The projection kind.</returns>
    public ChartGeographicProjectionKind GetKind()
    {
        return Kind;
    }

    /// <summary>Returns the central longitude in degrees.</summary>
    /// <returns>The central longitude.</returns>
    public double GetCentralLongitude()
    {
        return CentralLongitude;
    }

    /// <summary>Returns the central latitude in degrees.</summary>
    /// <returns>The central latitude.</returns>
    public double GetCentralLatitude()
    {
        return CentralLatitude;
    }

    /// <summary>Returns the first standard parallel in degrees.</summary>
    /// <returns>The first standard parallel.</returns>
    public double GetFirstStandardParallel()
    {
        return FirstStandardParallel;
    }

    /// <summary>Returns the second standard parallel in degrees.</summary>
    /// <returns>The second standard parallel.</returns>
    public double GetSecondStandardParallel()
    {
        return SecondStandardParallel;
    }

    internal void ConfigureFormula(double secondStandardParallel)
    {
        if (Kind == ChartGeographicProjectionKind.Equirectangular)
        {
            EquirectangularScale = System.Math.Cos(FirstParallelRadians);
            if (System.Math.Abs(EquirectangularScale) <= 0.000000000001)
            {
                throw new System.ArgumentException("The equirectangular standard parallel cannot be a pole.");
            }
        }
        else if (Kind == ChartGeographicProjectionKind.Mercator)
        {
            ValidateMercatorLatitude(LatitudeOriginRadians, "centralLatitude");
            MercatorOrigin = System.Math.Log(System.Math.Tan(System.Math.PI / 4.0 + LatitudeOriginRadians / 2.0));
        }
        else if (Kind == ChartGeographicProjectionKind.AlbersLike)
        {
            double secondParallelRadians = DegreesToRadians(secondStandardParallel);
            double firstSine = System.Math.Sin(FirstParallelRadians);
            if (System.Math.Abs(FirstParallelRadians - secondParallelRadians) <= 0.000000000001)
            {
                AlbersN = firstSine;
            }
            else
            {
                AlbersN = (firstSine + System.Math.Sin(secondParallelRadians)) / 2.0;
            }

            if (System.Math.Abs(AlbersN) <= 0.000000000001)
            {
                throw new System.ArgumentException("The Albers-like standard parallels produce a singular cone.");
            }

            double firstCosine = System.Math.Cos(FirstParallelRadians);
            AlbersC = firstCosine * firstCosine + 2.0 * AlbersN * firstSine;
            double originRadicand = AlbersC - 2.0 * AlbersN * System.Math.Sin(LatitudeOriginRadians);
            if (originRadicand < 0.0)
            {
                throw new System.ArgumentException("The Albers-like origin is outside the projection domain.");
            }

            AlbersRhoOrigin = System.Math.Sqrt(originRadicand) / AlbersN;
        }
    }

    internal ChartPoint ProjectEquirectangular(double longitudeDifference, double latitudeRadians)
    {
        return new ChartPoint(longitudeDifference * EquirectangularScale, latitudeRadians - LatitudeOriginRadians);
    }

    internal ChartPoint ProjectMercator(double longitudeDifference, double latitudeRadians)
    {
        ValidateMercatorLatitude(latitudeRadians, "latitude");
        double y = System.Math.Log(System.Math.Tan(System.Math.PI / 4.0 + latitudeRadians / 2.0)) - MercatorOrigin;
        return new ChartPoint(longitudeDifference, y);
    }

    internal ChartPoint ProjectOrthographic(double longitudeDifference, double latitudeRadians)
    {
        double originSine = System.Math.Sin(LatitudeOriginRadians);
        double originCosine = System.Math.Cos(LatitudeOriginRadians);
        double latitudeSine = System.Math.Sin(latitudeRadians);
        double latitudeCosine = System.Math.Cos(latitudeRadians);
        double differenceCosine = System.Math.Cos(longitudeDifference);
        double visibility = originSine * latitudeSine + originCosine * latitudeCosine * differenceCosine;
        if (visibility < -0.000000000001)
        {
            throw new System.ArgumentException("The coordinate is outside the visible orthographic hemisphere.");
        }

        double x = latitudeCosine * System.Math.Sin(longitudeDifference);
        double y = originCosine * latitudeSine - originSine * latitudeCosine * differenceCosine;
        return new ChartPoint(x, y);
    }

    internal ChartPoint ProjectAlbers(double longitudeDifference, double latitudeRadians)
    {
        double radicand = AlbersC - 2.0 * AlbersN * System.Math.Sin(latitudeRadians);
        if (radicand < -0.000000000001)
        {
            throw new System.ArgumentException("The coordinate is outside the Albers-like projection domain.");
        }

        if (radicand < 0.0)
        {
            radicand = 0.0;
        }

        double rho = System.Math.Sqrt(radicand) / AlbersN;
        double theta = AlbersN * longitudeDifference;
        return new ChartPoint(rho * System.Math.Sin(theta), AlbersRhoOrigin - rho * System.Math.Cos(theta));
    }

    internal ChartPoint InvertEquirectangular(ChartPoint point)
    {
        double longitude = LongitudeOriginRadians + point.X / EquirectangularScale;
        double latitude = LatitudeOriginRadians + point.Y;
        return CoordinateFromRadians(longitude, latitude);
    }

    internal ChartPoint InvertMercator(ChartPoint point)
    {
        double longitude = LongitudeOriginRadians + point.X;
        double latitude = System.Math.Asin(System.Math.Tanh(point.Y + MercatorOrigin));
        return CoordinateFromRadians(longitude, latitude);
    }

    internal ChartPoint InvertOrthographic(ChartPoint point)
    {
        double radiusSquared = point.X * point.X + point.Y * point.Y;
        if (radiusSquared > 1.000000000002)
        {
            throw new System.ArgumentException("The point is outside the orthographic projection disk.");
        }

        if (radiusSquared <= 0.000000000000000000000001)
        {
            return new ChartPoint(NormalizeDegrees(CentralLongitude), CentralLatitude);
        }

        if (radiusSquared > 1.0)
        {
            radiusSquared = 1.0;
        }

        double radius = System.Math.Sqrt(radiusSquared);
        double angularDistance = System.Math.Asin(radius);
        double distanceSine = System.Math.Sin(angularDistance);
        double distanceCosine = System.Math.Cos(angularDistance);
        double originSine = System.Math.Sin(LatitudeOriginRadians);
        double originCosine = System.Math.Cos(LatitudeOriginRadians);
        double latitudeArgument = distanceCosine * originSine + point.Y * distanceSine * originCosine / radius;
        latitudeArgument = ClampUnit(latitudeArgument);
        double latitude = System.Math.Asin(latitudeArgument);
        double longitude = LongitudeOriginRadians + System.Math.Atan2(point.X * distanceSine, radius * originCosine * distanceCosine - point.Y * originSine * distanceSine);
        return CoordinateFromRadians(longitude, latitude);
    }

    internal ChartPoint InvertAlbers(ChartPoint point)
    {
        double vertical = AlbersRhoOrigin - point.Y;
        double sign = 1.0;
        if (AlbersN < 0.0)
        {
            sign = -1.0;
        }

        double rhoMagnitude = System.Math.Sqrt(point.X * point.X + vertical * vertical);
        if (rhoMagnitude <= 0.000000000001)
        {
            throw new System.ArgumentException("The Albers-like cone apex does not have a unique inverse longitude.");
        }

        double rho = sign * rhoMagnitude;
        double theta = System.Math.Atan2(sign * point.X, sign * vertical);
        double latitudeArgument = (AlbersC - rho * rho * AlbersN * AlbersN) / (2.0 * AlbersN);
        if (latitudeArgument < -1.000000000001 || latitudeArgument > 1.000000000001)
        {
            throw new System.ArgumentException("The point is outside the Albers-like projection domain.");
        }

        latitudeArgument = ClampUnit(latitudeArgument);
        double latitude = System.Math.Asin(latitudeArgument);
        double longitude = LongitudeOriginRadians + theta / AlbersN;
        return CoordinateFromRadians(longitude, latitude);
    }

    internal static ChartPoint CoordinateFromRadians(double longitude, double latitude)
    {
        if (latitude < -System.Math.PI / 2.0 - 0.000000000001 || latitude > System.Math.PI / 2.0 + 0.000000000001)
        {
            throw new System.ArgumentException("The projected point inverts to a latitude outside the geographic domain.");
        }

        double longitudeDegrees = NormalizeDegrees(RadiansToDegrees(longitude));
        double latitudeDegrees = RadiansToDegrees(latitude);
        return new ChartPoint(longitudeDegrees, latitudeDegrees);
    }

    internal static double NormalizeRadians(double value)
    {
        while (value > System.Math.PI)
        {
            value = value - 2.0 * System.Math.PI;
        }

        while (value < -System.Math.PI)
        {
            value = value + 2.0 * System.Math.PI;
        }

        return value;
    }

    internal static double NormalizeDegrees(double value)
    {
        while (value > 180.0)
        {
            value = value - 360.0;
        }

        while (value < -180.0)
        {
            value = value + 360.0;
        }

        return value;
    }

    internal static double DegreesToRadians(double value)
    {
        return value * System.Math.PI / 180.0;
    }

    internal static double RadiansToDegrees(double value)
    {
        return value * 180.0 / System.Math.PI;
    }

    internal static double ClampUnit(double value)
    {
        if (value < -1.0)
        {
            return -1.0;
        }

        if (value > 1.0)
        {
            return 1.0;
        }

        return value;
    }

    internal static void ValidateKind(ChartGeographicProjectionKind kind)
    {
        if (kind != ChartGeographicProjectionKind.Equirectangular && kind != ChartGeographicProjectionKind.Mercator && kind != ChartGeographicProjectionKind.Orthographic && kind != ChartGeographicProjectionKind.AlbersLike)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }

    internal static void ValidateLongitudeOrigin(double longitude)
    {
        if (double.IsNaN(longitude) || double.IsInfinity(longitude))
        {
            throw new System.ArgumentException("Central longitude must be finite.", "centralLongitude");
        }

        if (longitude < -180.0 || longitude > 180.0)
        {
            throw new System.ArgumentOutOfRangeException("centralLongitude", "Central longitude must be between -180 and 180 degrees.");
        }
    }

    internal static void ValidateLongitudeCoordinate(double longitude)
    {
        if (double.IsNaN(longitude) || double.IsInfinity(longitude))
        {
            throw new System.ArgumentException("Longitude must be finite.", "longitude");
        }
    }

    internal static void ValidateLatitudeValue(double latitude, string name)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude))
        {
            throw new System.ArgumentException("Latitude must be finite.", name);
        }

        if (latitude < -90.0 || latitude > 90.0)
        {
            throw new System.ArgumentOutOfRangeException(name, "Latitude must be between -90 and 90 degrees.");
        }
    }

    internal static void ValidateMercatorLatitude(double latitudeRadians, string name)
    {
        if (latitudeRadians <= -System.Math.PI / 2.0 || latitudeRadians >= System.Math.PI / 2.0)
        {
            throw new System.ArgumentOutOfRangeException(name, "Mercator latitude must be strictly between -90 and 90 degrees.");
        }
    }

    internal static void ValidateProjectedPoint(ChartPoint point)
    {
        if (point == null)
        {
            throw new System.ArgumentNullException("point");
        }

        if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
        {
            throw new System.ArgumentException("Projected points must be finite.", "point");
        }
    }
}
