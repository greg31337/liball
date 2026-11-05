using uspto;

namespace uspto.Tests;

public class ContinuityGraphBuilderTests
{
    [Test]
    public void BuildCanonicalizesIdentifiersSuppressesDuplicatesAndRepresentsMissingNodes()
    {
        PatentContinuity root = new PatentContinuity
        {
            ApplicationNumber = "14/412,875",
            Provenance = new PatentDataProvenance { Source = DataProvenance.Odp, DatasetIdentifier = "continuity-root", IsAuthoritative = true },
            Parents = new PatentParentContinuity[]
            {
                new PatentParentContinuity
                {
                    ParentApplicationNumber = "12/312,313",
                    ChildApplicationNumber = "14/412,875",
                    ParentPatentNumber = "8968299",
                    RelationshipCode = "PRO",
                    RelationshipDescription = "Provisional priority",
                    ParentApplicationStatusCode = 159,
                    HasParentApplicationStatusCode = true,
                    ParentApplicationStatusDescription = "Expired",
                    ParentApplicationFilingDate = new DateTimeOffset(2012, 5, 23, 0, 0, 0, TimeSpan.Zero),
                    HasParentApplicationFilingDate = true
                },
                new PatentParentContinuity
                {
                    ParentApplicationNumber = "12312313",
                    ChildApplicationNumber = "14412875",
                    RelationshipCode = "pro",
                    RelationshipDescription = "Duplicate source edge"
                }
            },
            Children = new PatentChildContinuity[]
            {
                new PatentChildContinuity
                {
                    ParentApplicationNumber = "14/412,875",
                    ChildApplicationNumber = "14/853,719",
                    ChildPatentNumber = "9704967",
                    RelationshipCode = "DIV",
                    RelationshipDescription = "Divisional",
                    ChildApplicationStatusCode = 150,
                    HasChildApplicationStatusCode = true,
                    ChildApplicationStatusDescription = "Patented Case",
                    ChildApplicationFilingDate = new DateTimeOffset(2015, 9, 14, 0, 0, 0, TimeSpan.Zero),
                    HasChildApplicationFilingDate = true
                }
            }
        };
        PatentContinuity hydratedChild = new PatentContinuity
        {
            ApplicationNumber = "14853719",
            Provenance = new PatentDataProvenance { Source = DataProvenance.Odp, DatasetIdentifier = "continuity-child", FromCache = true, IsAuthoritative = true },
            Parents = new PatentParentContinuity[]
            {
                new PatentParentContinuity
                {
                    ParentApplicationNumber = "14412875",
                    ChildApplicationNumber = "14853719",
                    RelationshipCode = "DIV"
                }
            }
        };

        PatentContinuityGraph graph = ContinuityGraphBuilder.Build(new PatentContinuity[] { root, hydratedChild });

        Assert.Multiple(() =>
        {
            Assert.That(graph.RootApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(graph.Nodes, Has.Length.EqualTo(3));
            Assert.That(graph.Edges, Has.Length.EqualTo(2));
            Assert.That(graph.MissingNodeCount, Is.EqualTo(1));
            Assert.That(graph.DuplicateEdgeCount, Is.EqualTo(2));
            Assert.That(graph.HasCycle, Is.False);
            Assert.That(graph.Nodes[0].ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(graph.Nodes[0].IsRoot, Is.True);
            Assert.That(graph.Nodes[0].IsMissing, Is.False);
            Assert.That(graph.Nodes[1].ApplicationNumber, Is.EqualTo("14853719"));
            Assert.That(graph.Nodes[1].IsMissing, Is.False);
            Assert.That(graph.Nodes[2].ApplicationNumber, Is.EqualTo("12312313"));
            Assert.That(graph.Nodes[2].IsMissing, Is.True);
            Assert.That(graph.Edges[0].ParentApplicationNumber, Is.EqualTo("12312313"));
            Assert.That(graph.Edges[0].ChildApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(graph.Edges[0].RelationshipCode, Is.EqualTo("PRO"));
            Assert.That(graph.Edges[1].ParentApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(graph.Edges[1].ChildApplicationNumber, Is.EqualTo("14853719"));
            Assert.That(graph.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(graph.Provenance.IsAuthoritative, Is.False);
            Assert.That(graph.Provenance.FieldSources.Count(item => item.FieldPath == "Nodes"), Is.EqualTo(2));
            Assert.That(graph.Provenance.FieldSources.Single(item => item.FieldPath == "Nodes" && item.DatasetIdentifier == "continuity-child").Delivery, Is.EqualTo(DataProvenance.Cache));
        });

        PatentContinuityGraphNode parentNode = graph.Nodes[2];
        PatentContinuityGraphNode childNode = graph.Nodes[1];
        Assert.Multiple(() =>
        {
            Assert.That(parentNode.PatentNumber, Is.EqualTo("8968299"));
            Assert.That(parentNode.ApplicationStatusCode, Is.EqualTo(159));
            Assert.That(parentNode.HasApplicationStatusCode, Is.True);
            Assert.That(parentNode.ApplicationStatusDescription, Is.EqualTo("Expired"));
            Assert.That(parentNode.FilingDate, Is.EqualTo(new DateTimeOffset(2012, 5, 23, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(parentNode.HasFilingDate, Is.True);
            Assert.That(childNode.PatentNumber, Is.EqualTo("9704967"));
            Assert.That(childNode.ApplicationStatusCode, Is.EqualTo(150));
            Assert.That(childNode.HasApplicationStatusCode, Is.True);
            Assert.That(childNode.ApplicationStatusDescription, Is.EqualTo("Patented Case"));
            Assert.That(childNode.HasFilingDate, Is.True);
            Assert.That(root.ApplicationNumber, Is.EqualTo("14/412,875"));
        });
    }

    [Test]
    public void BuildMarksCycleEdgesWithoutMarkingAnInboundTail()
    {
        PatentContinuity tail = ChildRecord("12/000,001", "12/000,001", "12/000,002", "CON");
        PatentContinuity first = ChildRecord("12/000,002", "12/000,002", "12/000,003", "CON");
        PatentContinuity second = ChildRecord("12/000,003", "12/000,003", "12/000,004", "CON");
        PatentContinuity third = ChildRecord("12/000,004", "12/000,004", "12/000,002", "CON");

        PatentContinuityGraph graph = ContinuityGraphBuilder.Build(new PatentContinuity[] { tail, first, second, third });

        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes, Has.Length.EqualTo(4));
            Assert.That(graph.Edges, Has.Length.EqualTo(4));
            Assert.That(graph.MissingNodeCount, Is.Zero);
            Assert.That(graph.HasCycle, Is.True);
            Assert.That(graph.Edges[0].IsCycle, Is.False);
            Assert.That(graph.Edges[1].IsCycle, Is.True);
            Assert.That(graph.Edges[2].IsCycle, Is.True);
            Assert.That(graph.Edges[3].IsCycle, Is.True);
        });
    }

    [Test]
    public void DistinctRelationshipCodesRemainSeparateAndSelfLinkIsACycle()
    {
        PatentContinuity continuity = new PatentContinuity
        {
            ApplicationNumber = "12/000,001",
            Children = new PatentChildContinuity[]
            {
                new PatentChildContinuity { ChildApplicationNumber = "12/000,002", RelationshipCode = "CON" },
                new PatentChildContinuity { ChildApplicationNumber = "12/000,002", RelationshipCode = "DIV" },
                new PatentChildContinuity { ChildApplicationNumber = "12/000,001", RelationshipCode = "CON" }
            }
        };

        PatentContinuityGraph graph = ContinuityGraphBuilder.Build(new PatentContinuity[] { continuity });

        Assert.Multiple(() =>
        {
            Assert.That(graph.Edges, Has.Length.EqualTo(3));
            Assert.That(graph.DuplicateEdgeCount, Is.Zero);
            Assert.That(graph.HasCycle, Is.True);
            Assert.That(graph.Edges[0].IsCycle, Is.False);
            Assert.That(graph.Edges[1].IsCycle, Is.False);
            Assert.That(graph.Edges[2].IsCycle, Is.True);
        });
    }

    [Test]
    public void MissingRelationshipEndpointsUseTheContainingApplication()
    {
        PatentContinuity continuity = new PatentContinuity
        {
            ApplicationNumber = "12/000,002",
            Parents = new PatentParentContinuity[]
            {
                new PatentParentContinuity { ParentApplicationNumber = "12/000,001", RelationshipCode = "CON" },
                new PatentParentContinuity()
            },
            Children = new PatentChildContinuity[]
            {
                new PatentChildContinuity { ChildApplicationNumber = "12/000,003", RelationshipCode = "DIV" },
                new PatentChildContinuity()
            }
        };

        PatentContinuityGraph graph = ContinuityGraphBuilder.Build(new PatentContinuity[] { continuity });

        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes, Has.Length.EqualTo(3));
            Assert.That(graph.Edges, Has.Length.EqualTo(2));
            Assert.That(graph.Edges[0].ParentApplicationNumber, Is.EqualTo("12000001"));
            Assert.That(graph.Edges[0].ChildApplicationNumber, Is.EqualTo("12000002"));
            Assert.That(graph.Edges[1].ParentApplicationNumber, Is.EqualTo("12000002"));
            Assert.That(graph.Edges[1].ChildApplicationNumber, Is.EqualTo("12000003"));
            Assert.That(graph.MissingNodeCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void ContinuityParserAttachesAOneHopGraph()
    {
        string response = """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "14/412,875",
                  "parentContinuityBag": [
                    {
                      "parentApplicationNumberText": "12/312,313",
                      "childApplicationNumberText": "14/412,875",
                      "parentApplicationStatusCode": 159,
                      "claimParentageTypeCode": "PRO"
                    }
                  ],
                  "childContinuityBag": [
                    {
                      "parentApplicationNumberText": "14/412,875",
                      "childApplicationNumberText": "14/853,719",
                      "claimParentageTypeCode": "DIV"
                    }
                  ]
                }
              ]
            }
            """;

        PatentContinuity continuity = OdpPatentContinuityPriorityParser.ParseContinuity(response, "14412875", ResponseMetadata());

        Assert.Multiple(() =>
        {
            Assert.That(continuity.Graph.RootApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(continuity.Graph.Nodes, Has.Length.EqualTo(3));
            Assert.That(continuity.Graph.Edges, Has.Length.EqualTo(2));
            Assert.That(continuity.Graph.MissingNodeCount, Is.EqualTo(2));
            Assert.That(continuity.Graph.DuplicateEdgeCount, Is.Zero);
            Assert.That(continuity.Graph.HasCycle, Is.False);
            Assert.That(continuity.Graph.Nodes[0].IsRoot, Is.True);
            Assert.That(continuity.Graph.Nodes[1].ApplicationStatusCode, Is.EqualTo(159));
            Assert.That(continuity.Graph.Nodes[1].HasApplicationStatusCode, Is.True);
        });
    }

    [Test]
    public void EmptyInputAndGraphModelsHaveStableDefaults()
    {
        PatentContinuityGraph graph = ContinuityGraphBuilder.Build(Array.Empty<PatentContinuity>());
        PatentContinuity continuity = new PatentContinuity();
        PatentContinuityGraphNode node = new PatentContinuityGraphNode();
        PatentContinuityGraphEdge edge = new PatentContinuityGraphEdge();

        Assert.Multiple(() =>
        {
            Assert.That(graph.RootApplicationNumber, Is.Null);
            Assert.That(graph.Nodes, Is.Empty);
            Assert.That(graph.Edges, Is.Empty);
            Assert.That(graph.MissingNodeCount, Is.Zero);
            Assert.That(graph.DuplicateEdgeCount, Is.Zero);
            Assert.That(graph.HasCycle, Is.False);
            Assert.That(continuity.Graph, Is.Not.Null);
            Assert.That(continuity.Graph.Nodes, Is.Empty);
            Assert.That(node.IsMissing, Is.False);
            Assert.That(node.IsRoot, Is.False);
            Assert.That(node.HasApplicationStatusCode, Is.False);
            Assert.That(node.HasFilingDate, Is.False);
            Assert.That(edge.IsCycle, Is.False);
        });
    }

    [Test]
    public void InvalidGraphInputsReportTheResponsibleField()
    {
        ValidationException missingCollection = Assert.Throws<ValidationException>(() => ContinuityGraphBuilder.Build(null!));
        ValidationException nullRecord = Assert.Throws<ValidationException>(() => ContinuityGraphBuilder.Build(new PatentContinuity[] { null! }));
        ValidationException missingApplication = Assert.Throws<ValidationException>(() => ContinuityGraphBuilder.Build(new PatentContinuity[] { new PatentContinuity() }));
        ValidationException invalidApplication = Assert.Throws<ValidationException>(() => ContinuityGraphBuilder.Build(new PatentContinuity[] { new PatentContinuity { ApplicationNumber = "bad" } }));
        ValidationException nullParents = Assert.Throws<ValidationException>(() => ContinuityGraphBuilder.Build(new PatentContinuity[] { new PatentContinuity { ApplicationNumber = "12/000,001", Parents = null! } }));
        ValidationException nullChildren = Assert.Throws<ValidationException>(() => ContinuityGraphBuilder.Build(new PatentContinuity[] { new PatentContinuity { ApplicationNumber = "12/000,001", Children = null! } }));
        ValidationException nullParent = Assert.Throws<ValidationException>(() => ContinuityGraphBuilder.Build(new PatentContinuity[] { new PatentContinuity { ApplicationNumber = "12/000,001", Parents = new PatentParentContinuity[] { null! } } }));
        ValidationException invalidRelated = Assert.Throws<ValidationException>(() => ContinuityGraphBuilder.Build(new PatentContinuity[] { new PatentContinuity { ApplicationNumber = "12/000,001", Children = new PatentChildContinuity[] { new PatentChildContinuity { ChildApplicationNumber = "bad" } } } }));

        Assert.Multiple(() =>
        {
            Assert.That(missingCollection.FieldName, Is.EqualTo("continuities"));
            Assert.That(nullRecord.FieldName, Is.EqualTo("continuities[0]"));
            Assert.That(missingApplication.FieldName, Is.EqualTo("continuities[0].ApplicationNumber"));
            Assert.That(invalidApplication.FieldName, Is.EqualTo("continuities[0].ApplicationNumber"));
            Assert.That(nullParents.FieldName, Is.EqualTo("continuities[0].Parents"));
            Assert.That(nullChildren.FieldName, Is.EqualTo("continuities[0].Children"));
            Assert.That(nullParent.FieldName, Is.EqualTo("continuities[0].Parents[0]"));
            Assert.That(invalidRelated.FieldName, Is.EqualTo("ChildApplicationNumber"));
        });
    }

    private static PatentContinuity ChildRecord(string applicationNumber, string parentNumber, string childNumber, string relationshipCode)
    {
        return new PatentContinuity
        {
            ApplicationNumber = applicationNumber,
            Children = new PatentChildContinuity[]
            {
                new PatentChildContinuity
                {
                    ParentApplicationNumber = parentNumber,
                    ChildApplicationNumber = childNumber,
                    RelationshipCode = relationshipCode
                }
            }
        };
    }

    private static HttpResponseMetadata ResponseMetadata()
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/14412875/continuity"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json
        };
    }
}
