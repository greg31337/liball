using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class FinderGrouperTests
{
    [Test]
    public void GroupsVersionOneGeometryAndPreservesFamilyAmbiguity()
    {
        FinderCandidate topLeft = CreateCandidate(10, 10, 2, 0.95);
        FinderCandidate topRight = CreateCandidate(38, 10, 2, 0.9);
        FinderCandidate bottomLeft = CreateCandidate(10, 38, 2, 0.85);

        FinderGroup[] groups = FinderGrouper.Group(
            new FinderCandidate[] { bottomLeft, topRight, topLeft },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(groups, Has.Length.EqualTo(1));
            Assert.That(groups[0].TopLeft, Is.SameAs(topLeft));
            Assert.That(groups[0].TopRight, Is.SameAs(topRight));
            Assert.That(groups[0].BottomLeft, Is.SameAs(bottomLeft));
            Assert.That(groups[0].EstimatedBottomRight.X, Is.EqualTo(38));
            Assert.That(groups[0].EstimatedBottomRight.Y, Is.EqualTo(38));
            Assert.That(groups[0].EstimatedVersion, Is.EqualTo(1));
            Assert.That(groups[0].EstimatedDimension, Is.EqualTo(21));
            Assert.That(groups[0].EstimatedSymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(groups[0].Model2Possible, Is.True);
            Assert.That(groups[0].LegacyModel1Possible, Is.True);
            Assert.That(groups[0].RightAngleCosine, Is.EqualTo(0).Within(0.001));
            Assert.That(groups[0].OrientationRadians, Is.EqualTo(0).Within(0.001));
            Assert.That(groups[0].Confidence, Is.GreaterThan(0.9));
        });
    }

    [Test]
    public void OrdersRotatedFindersByPositiveImageSpaceOrientation()
    {
        FinderCandidate anchor = CreateCandidate(30, 10, 2, 0.9);
        FinderCandidate clockwiseArm = CreateCandidate(50, 30, 2, 0.9);
        FinderCandidate counterArm = CreateCandidate(10, 30, 2, 0.9);

        FinderGroup[] groups = FinderGrouper.Group(
            new FinderCandidate[] { counterArm, anchor, clockwiseArm },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(groups, Has.Length.EqualTo(1));
            Assert.That(groups[0].TopLeft, Is.SameAs(anchor));
            Assert.That(groups[0].TopRight, Is.SameAs(clockwiseArm));
            Assert.That(groups[0].BottomLeft, Is.SameAs(counterArm));
            Assert.That(groups[0].OrientationRadians,
                Is.EqualTo(global::System.Math.PI / 4.0).Within(0.001));
            Assert.That(groups[0].EstimatedDimension, Is.EqualTo(21));
        });
    }

    [Test]
    public void EstimatesLaterModel2VersionWithoutLegacyPossibility()
    {
        FinderCandidate topLeft = CreateCandidate(10, 10, 1, 0.9);
        FinderCandidate topRight = CreateCandidate(100, 10, 1, 0.9);
        FinderCandidate bottomLeft = CreateCandidate(10, 100, 1, 0.9);

        FinderGroup[] groups = FinderGrouper.Group(
            new FinderCandidate[] { topLeft, topRight, bottomLeft },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(groups, Has.Length.EqualTo(1));
            Assert.That(groups[0].EstimatedVersion, Is.EqualTo(20));
            Assert.That(groups[0].EstimatedDimension, Is.EqualTo(97));
            Assert.That(groups[0].Model2Possible, Is.True);
            Assert.That(groups[0].LegacyModel1Possible, Is.False);
        });
    }

    [Test]
    public void HonorsLegacyOnlyVersionRangeAndProvisionalKind()
    {
        QrDetectionOptions legacyOnly = new QrDetectionOptions
        {
            AllowModel2 = false,
            AllowLegacyModel1 = true
        };
        FinderCandidate topLeft = CreateCandidate(10, 10, 1, 0.9);
        FinderCandidate version14Right = CreateCandidate(76, 10, 1, 0.9);
        FinderCandidate version14Bottom = CreateCandidate(10, 76, 1, 0.9);
        FinderCandidate version15Right = CreateCandidate(80, 10, 1, 0.9);
        FinderCandidate version15Bottom = CreateCandidate(10, 80, 1, 0.9);

        FinderGroup[] version14 = FinderGrouper.Group(
            new FinderCandidate[] { topLeft, version14Right, version14Bottom },
            legacyOnly);
        FinderGroup[] version15 = FinderGrouper.Group(
            new FinderCandidate[] { topLeft, version15Right, version15Bottom },
            legacyOnly);

        Assert.Multiple(() =>
        {
            Assert.That(version14, Has.Length.EqualTo(1));
            Assert.That(version14[0].EstimatedVersion, Is.EqualTo(14));
            Assert.That(version14[0].EstimatedSymbolKind, Is.EqualTo(QrSymbolKind.LegacyModel1));
            Assert.That(version14[0].Model2Possible, Is.False);
            Assert.That(version14[0].LegacyModel1Possible, Is.True);
            Assert.That(version15, Is.Empty);
        });
    }

    [Test]
    public void RejectsNonRightAnglesModuleMismatchAndPolarityConflicts()
    {
        FinderCandidate anchor = CreateCandidate(10, 10, 2, 0.9);
        FinderCandidate right = CreateCandidate(38, 10, 2, 0.9);
        FinderCandidate shallow = CreateCandidate(35, 18, 2, 0.9);
        FinderCandidate mismatched = CreateCandidate(10, 38, 5, 0.9);
        FinderCandidate inconsistentDimension = CreateCandidate(10, 52, 2, 0.9);
        FinderCandidate bottom = CreateCandidate(10, 38, 2, 0.9);
        anchor.Polarity = QrPolarity.DarkOnLight;
        right.Polarity = QrPolarity.DarkOnLight;
        bottom.Polarity = QrPolarity.LightOnDark;

        FinderGroup[] angle = FinderGrouper.Group(
            new FinderCandidate[] { anchor, right, shallow },
            new QrDetectionOptions());
        FinderGroup[] module = FinderGrouper.Group(
            new FinderCandidate[] { anchor, right, mismatched },
            new QrDetectionOptions());
        FinderGroup[] dimension = FinderGrouper.Group(
            new FinderCandidate[] { anchor, right, inconsistentDimension },
            new QrDetectionOptions());
        FinderGroup[] polarity = FinderGrouper.Group(
            new FinderCandidate[] { anchor, right, bottom },
            new QrDetectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(angle, Is.Empty);
            Assert.That(module, Is.Empty);
            Assert.That(dimension, Is.Empty);
            Assert.That(polarity, Is.Empty);
        });
    }

    [Test]
    public void BoundsAndRanksGeneratedGroupsAtConfiguredLimit()
    {
        FinderCandidate highTopLeft = CreateCandidate(10, 10, 2, 0.95);
        FinderCandidate highTopRight = CreateCandidate(38, 10, 2, 0.95);
        FinderCandidate highBottomLeft = CreateCandidate(10, 38, 2, 0.95);
        FinderCandidate lowTopLeft = CreateCandidate(70, 10, 2, 0.5);
        FinderCandidate lowTopRight = CreateCandidate(98, 10, 2, 0.5);
        FinderCandidate lowBottomLeft = CreateCandidate(70, 38, 2, 0.5);
        FinderGroup[] groups = FinderGrouper.Group(
            new FinderCandidate[]
            {
                highTopLeft,
                highTopRight,
                highBottomLeft,
                lowTopLeft,
                lowTopRight,
                lowBottomLeft
            },
            new QrDetectionOptions
            {
                MaximumCandidates = 6
            });

        Assert.Multiple(() =>
        {
            Assert.That(groups.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(groups.Length, Is.LessThanOrEqualTo(6));
            Assert.That(groups[0].Confidence, Is.GreaterThan(groups[1].Confidence));
            Assert.That(groups[0].TopLeft, Is.SameAs(highTopLeft));
        });
    }

    [Test]
    public void RejectsInvalidInputsAndCandidateLimits()
    {
        FinderCandidate valid = CreateCandidate(10, 10, 2, 0.9);
        FinderCandidate invalid = CreateCandidate(10, 10, 2, double.NaN);
        FinderCandidate unvalidated = new FinderCandidate
        {
            CenterX = 10,
            CenterY = 10,
            ModuleSize = 2,
            Confidence = 0.9
        };
        QrDetectionOptions noCandidates = new QrDetectionOptions
        {
            MaximumCandidates = 0
        };
        QrDetectionOptions twoCandidates = new QrDetectionOptions
        {
            MaximumCandidates = 2
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => FinderGrouper.Group(null!, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderGrouper.Group(new FinderCandidate[0], null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderGrouper.Group(new FinderCandidate[] { valid }, noCandidates),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderGrouper.Group(new FinderCandidate[] { null! }, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderGrouper.Group(new FinderCandidate[] { invalid }, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderGrouper.Group(new FinderCandidate[] { unvalidated }, new QrDetectionOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FinderGrouper.Group(
                new FinderCandidate[] { valid, valid, valid },
                twoCandidates), Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static FinderCandidate CreateCandidate(
        double centerX,
        double centerY,
        double moduleSize,
        double confidence)
    {
        return new FinderCandidate
        {
            CenterX = centerX,
            CenterY = centerY,
            ModuleSize = moduleSize,
            Confidence = confidence,
            RegionValidated = true,
            RegionConfidence = confidence,
            CenterRegionArea = 9,
            OuterRegionArea = 24,
            FinderCorners = new QrQuadrilateral
            {
                TopLeft = new QrPoint { X = centerX - 3.5 * moduleSize, Y = centerY - 3.5 * moduleSize },
                TopRight = new QrPoint { X = centerX + 3.5 * moduleSize, Y = centerY - 3.5 * moduleSize },
                BottomRight = new QrPoint { X = centerX + 3.5 * moduleSize, Y = centerY + 3.5 * moduleSize },
                BottomLeft = new QrPoint { X = centerX - 3.5 * moduleSize, Y = centerY + 3.5 * moduleSize }
            }
        };
    }
}
