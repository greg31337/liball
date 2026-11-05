using System.Collections.Generic;

namespace iso20022;

internal class GenerationModel
{
    internal List<GenerationSource> Sources = new List<GenerationSource>();
    internal List<GenerationConcept> Concepts = new List<GenerationConcept>();
    internal List<GenerationFacet> Facets = new List<GenerationFacet>();
}
