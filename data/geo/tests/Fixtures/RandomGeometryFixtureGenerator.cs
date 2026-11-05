using geo;

namespace geo.Tests;

internal class RandomGeometryFixtureGenerator
{
    private readonly Random Random;

    internal RandomGeometryFixtureGenerator(int seed)
    {
        Random = new Random(seed);
    }

    internal Geometry NextValid(GeometryType geometryType)
    {
        GeometryFactory factory = Factory(NextLayout());
        double x = NextOrdinate();
        double y = NextOrdinate();
        if (geometryType == GeometryType.Point)
        {
            return factory.CreatePoint(Coordinate(x, y,
                factory.GetFactoryOptions().CoordinateLayout));
        }
        if (geometryType == GeometryType.LineString)
        {
            return factory.CreateLineString(LineCoordinates(
                x, y, 2 + Random.Next(5),
                factory.GetFactoryOptions().CoordinateLayout));
        }
        if (geometryType == GeometryType.Polygon)
        {
            return ValidPolygon(factory, x, y, Random.Next(2) == 0);
        }
        if (geometryType == GeometryType.MultiPoint)
        {
            return ValidMultiPoint(factory, x, y);
        }
        if (geometryType == GeometryType.MultiLineString)
        {
            return ValidMultiLineString(factory, x, y);
        }
        if (geometryType == GeometryType.MultiPolygon)
        {
            return ValidMultiPolygon(factory, x, y);
        }
        if (geometryType == GeometryType.GeometryCollection)
        {
            return ValidCollection(factory, x, y);
        }
        throw new ArgumentOutOfRangeException(nameof(geometryType));
    }

    internal Geometry NextInvalid(GeometryType geometryType)
    {
        CoordinateLayout layout = NextLayout();
        if (geometryType == GeometryType.Point)
        {
            Coordinate coordinate = Coordinate(1.0, 2.0, layout);
            if (Random.Next(2) == 0) coordinate.X = double.NaN;
            else coordinate.Y = double.PositiveInfinity;
            return PointGeometry(coordinate, layout);
        }
        if (geometryType == GeometryType.LineString)
        {
            return LineGeometry(
                [Coordinate(1.0, 2.0, layout)], layout);
        }
        if (geometryType == GeometryType.Polygon)
        {
            return InvalidPolygon(layout);
        }
        if (geometryType == GeometryType.MultiPoint)
        {
            Coordinate invalid = Coordinate(3.0, 4.0, layout);
            invalid.X = double.NegativeInfinity;
            return new Geometry
            {
                GeometryType = GeometryType.MultiPoint,
                CoordinateLayout = layout,
                Point = null,
                MultiPoint = new MultiPoint
                {
                    Points =
                    [
                        new Point { Coordinate = Coordinate(1.0, 2.0, layout) },
                        new Point { Coordinate = invalid }
                    ]
                }
            };
        }
        if (geometryType == GeometryType.MultiLineString)
        {
            return new Geometry
            {
                GeometryType = GeometryType.MultiLineString,
                CoordinateLayout = layout,
                Point = null,
                MultiLineString = new MultiLineString
                {
                    LineStrings =
                    [
                        new LineString
                        {
                            Coordinates =
                            [
                                Coordinate(0.0, 0.0, layout),
                                Coordinate(1.0, 1.0, layout)
                            ]
                        },
                        new LineString
                        {
                            Coordinates = [Coordinate(2.0, 2.0, layout)]
                        }
                    ]
                }
            };
        }
        if (geometryType == GeometryType.MultiPolygon)
        {
            return InvalidMultiPolygon(layout);
        }
        if (geometryType == GeometryType.GeometryCollection)
        {
            return new Geometry
            {
                GeometryType = GeometryType.GeometryCollection,
                CoordinateLayout = layout,
                Point = null,
                GeometryCollection = new GeometryCollection
                {
                    Geometries =
                    [
                        PointGeometry(Coordinate(0.0, 0.0, layout), layout),
                        LineGeometry([Coordinate(1.0, 1.0, layout)], layout)
                    ]
                }
            };
        }
        throw new ArgumentOutOfRangeException(nameof(geometryType));
    }

    private GeometryFactory Factory(CoordinateLayout layout)
    {
        return new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = layout
        });
    }

    private CoordinateLayout NextLayout()
    {
        return (CoordinateLayout)Random.Next(4);
    }

    private double NextOrdinate()
    {
        return Random.Next(-400, 401) / 4.0;
    }

    private Coordinate Coordinate(
        double x, double y, CoordinateLayout layout)
    {
        Coordinate coordinate = new Coordinate { X = x, Y = y };
        if (layout == CoordinateLayout.XYZ ||
            layout == CoordinateLayout.XYZM)
        {
            coordinate.Z = NextOrdinate();
        }
        if (layout == CoordinateLayout.XYM ||
            layout == CoordinateLayout.XYZM)
        {
            coordinate.M = NextOrdinate();
        }
        return coordinate;
    }

    private Coordinate[] LineCoordinates(
        double x, double y, int count, CoordinateLayout layout)
    {
        Coordinate[] coordinates = new Coordinate[count];
        int index = 0;
        while (index < count)
        {
            coordinates[index] = Coordinate(
                x + (index * 2.0),
                y + index + ((index % 2) * 0.5), layout);
            index++;
        }
        return coordinates;
    }

    private Geometry ValidPolygon(
        GeometryFactory factory, double x, double y, bool withHole)
    {
        CoordinateLayout layout = factory.GetFactoryOptions().CoordinateLayout;
        double width = 8.0 + Random.Next(8);
        double height = 8.0 + Random.Next(8);
        LinearRing shell = RectangleRing(x, y, width, height, layout, false);
        LinearRing[] holes = [];
        if (withHole)
        {
            holes =
            [
                RectangleRing(x + 2.0, y + 2.0,
                    width - 4.0, height - 4.0, layout, true)
            ];
        }
        return factory.CreatePolygon(shell, holes);
    }

    private Geometry ValidMultiPoint(
        GeometryFactory factory, double x, double y)
    {
        CoordinateLayout layout = factory.GetFactoryOptions().CoordinateLayout;
        int count = 2 + Random.Next(5);
        Point[] points = new Point[count];
        int index = 0;
        while (index < count)
        {
            points[index] = new Point
            {
                Coordinate = Coordinate(x + index, y + (index * 2.0), layout)
            };
            index++;
        }
        return factory.CreateMultiPoint(points);
    }

    private Geometry ValidMultiLineString(
        GeometryFactory factory, double x, double y)
    {
        CoordinateLayout layout = factory.GetFactoryOptions().CoordinateLayout;
        int count = 2 + Random.Next(3);
        LineString[] lines = new LineString[count];
        int index = 0;
        while (index < count)
        {
            lines[index] = new LineString
            {
                Coordinates = LineCoordinates(
                    x, y + (index * 10.0), 2 + Random.Next(4), layout)
            };
            index++;
        }
        return factory.CreateMultiLineString(lines);
    }

    private Geometry ValidMultiPolygon(
        GeometryFactory factory, double x, double y)
    {
        CoordinateLayout layout = factory.GetFactoryOptions().CoordinateLayout;
        int count = 2 + Random.Next(3);
        Polygon[] polygons = new Polygon[count];
        int index = 0;
        while (index < count)
        {
            polygons[index] = new Polygon
            {
                Shell = RectangleRing(
                    x + (index * 20.0), y, 8.0, 8.0, layout, false),
                Holes = []
            };
            index++;
        }
        return factory.CreateMultiPolygon(polygons);
    }

    private Geometry ValidCollection(
        GeometryFactory factory, double x, double y)
    {
        CoordinateLayout layout = factory.GetFactoryOptions().CoordinateLayout;
        Geometry point = factory.CreatePoint(Coordinate(x, y, layout));
        Geometry line = factory.CreateLineString(
            LineCoordinates(x + 10.0, y, 3, layout));
        Geometry polygon = factory.CreatePolygon(
            RectangleRing(x + 20.0, y, 7.0, 6.0, layout, false), []);
        return factory.CreateGeometryCollection([point, line, polygon]);
    }

    private Geometry InvalidPolygon(CoordinateLayout layout)
    {
        Coordinate[] coordinates;
        if (Random.Next(2) == 0)
        {
            coordinates =
            [
                Coordinate(0.0, 0.0, layout),
                Coordinate(5.0, 0.0, layout),
                Coordinate(5.0, 5.0, layout),
                Coordinate(0.0, 5.0, layout)
            ];
        }
        else
        {
            coordinates =
            [
                Coordinate(0.0, 0.0, layout),
                Coordinate(5.0, 5.0, layout),
                Coordinate(0.0, 5.0, layout),
                Coordinate(5.0, 0.0, layout),
                Coordinate(0.0, 0.0, layout)
            ];
        }
        return PolygonGeometry(
            new Polygon
            {
                Shell = new LinearRing { Coordinates = coordinates },
                Holes = []
            }, layout);
    }

    private Geometry InvalidMultiPolygon(CoordinateLayout layout)
    {
        Polygon first = new Polygon
        {
            Shell = RectangleRing(0.0, 0.0, 8.0, 8.0, layout, false),
            Holes = []
        };
        Polygon second;
        if (Random.Next(2) == 0)
        {
            second = new Polygon
            {
                Shell = RectangleRing(4.0, 4.0, 8.0, 8.0, layout, false),
                Holes = []
            };
        }
        else
        {
            second = InvalidPolygon(layout).Polygon!;
        }
        return new Geometry
        {
            GeometryType = GeometryType.MultiPolygon,
            CoordinateLayout = layout,
            Point = null,
            MultiPolygon = new MultiPolygon { Polygons = [first, second] }
        };
    }

    private LinearRing RectangleRing(
        double x, double y, double width, double height,
        CoordinateLayout layout, bool clockwise)
    {
        Coordinate lowerLeft = Coordinate(x, y, layout);
        Coordinate lowerRight = Coordinate(x + width, y, layout);
        Coordinate upperRight = Coordinate(x + width, y + height, layout);
        Coordinate upperLeft = Coordinate(x, y + height, layout);
        Coordinate close = Copy(lowerLeft);
        if (clockwise)
        {
            return new LinearRing
            {
                Coordinates =
                    [lowerLeft, upperLeft, upperRight, lowerRight, close]
            };
        }
        return new LinearRing
        {
            Coordinates =
                [lowerLeft, lowerRight, upperRight, upperLeft, close]
        };
    }

    private Coordinate Copy(Coordinate source)
    {
        return new Coordinate
        {
            X = source.X,
            Y = source.Y,
            Z = source.Z,
            M = source.M
        };
    }

    private Geometry PointGeometry(
        Coordinate coordinate, CoordinateLayout layout)
    {
        return new Geometry
        {
            GeometryType = GeometryType.Point,
            CoordinateLayout = layout,
            Point = new Point { Coordinate = coordinate }
        };
    }

    private Geometry LineGeometry(
        Coordinate[] coordinates, CoordinateLayout layout)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LineString,
            CoordinateLayout = layout,
            Point = null,
            LineString = new LineString { Coordinates = coordinates }
        };
    }

    private Geometry PolygonGeometry(
        Polygon polygon, CoordinateLayout layout)
    {
        return new Geometry
        {
            GeometryType = GeometryType.Polygon,
            CoordinateLayout = layout,
            Point = null,
            Polygon = polygon
        };
    }
}
