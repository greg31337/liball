# AVIF and AV1 conformance test matrix

This matrix traces the frozen standards baseline in [conformance.md](conformance.md) to behavioral
tests. Test identifiers use `Class.Method`; the class name maps directly to its source file under
`tests/`. Parameterized cases cover all values declared on the referenced method.

The AVIF inventory is exhaustive for version 1.2.0: it contains all 75 stable `assert-*` anchors in
[`specs/avif-1.2.0.html`](../specs/avif-1.2.0.html). “Should” assertions are included alongside
mandatory assertions. Requirements incorporated by reference from MIAF, HEIF, and AV1 are mapped to
the test which exercises their AVIF-facing effect; their own broader requirement sets are not
silently treated as additional AVIF assertion anchors.

The AV1 specification defines a procedural decoder rather than a finite assertion-anchor list. The
second matrix therefore inventories every decoder requirement family reachable from an AVIF image
item or image-sequence sample. Encoder-only search choices and presentation-system behavior outside
decoded planar output are excluded. Independent differential tests supplement, but do not replace,
the focused normative tests.

## AVIF 1.2 assertions

| Assertion | Section | Requirement | Behavioral test |
| --- | --- | --- | --- |
| `assert-cadb7e29` | 2.1 | AV1 items satisfy the MIAF image-item structure. | `AvifContainerValidationTests.StrictValidationAcceptsAConformingImageItemContainer` |
| `assert-dd521ae6` | 2.1 | Every AV1 item has an `av1C` property. | `AvifContainerValidationTests.RequiredBoxAndPropertyRulesProduceContextualFindings` |
| `assert-8ef3bad2` | 2.1 | Item payload has AV1 sync-sample form. | `AvifContainerValidationTests.PayloadValidationReconcilesAv1ConfigurationAndBitstream` |
| `assert-809b9acc` | 2.1 | Item payload contains exactly one sequence header. | `Av1ItemPropertyTests.ConfigurationObusRequireSizedFirstAndUniqueSequenceHeader` |
| `assert-9c37f45a` | 2.2.1 | `av1C` normally omits a sequence header. | `Av1ItemPropertyTests.ParserExposesAv1ConfigurationSpatialExtentsAndClassicPixelInformation` |
| `assert-a0fd4b2f` | 2.2.1 | An embedded `av1C` sequence header matches the item payload. | `Av1ItemPropertyTests.EmbeddedReducedSequenceHeaderIsParsedAndReconciledWithItemData` |
| `assert-4f2a2770` | 2.2.1 | `av1C` fields match the payload sequence header. | `Av1ItemPropertyTests.ReconciliationRejectsRecordHeaderPayloadAndPixelInformationMismatches` |
| `assert-a631d62f` | 2.2.1 | `av1C` depth/channel count matches `pixi`. | `Av1ItemPropertyTests.ReconciliationRejectsRecordHeaderPayloadAndPixelInformationMismatches` |
| `assert-071d427d` | 2.2.1 | Metadata OBUs agree with equivalent item properties. | `HdrMetadataTests.MatchingObuValuesReconcileAndConflictsLeaveContainerValuesUnchanged` |
| `assert-ebb95262` | 2.2.1 | `av1C` is marked essential. | `AvifContainerValidationTests.EssentialFlagsPixelAspectRatioAndTransformOrderingAreValidated` |
| `assert-62e7d10a` | 2.2.2 | `ispe` matches decoded upscaled width and frame height. | `Av1ItemPropertyTests.ReconciliationRejectsRecordHeaderPayloadAndPixelInformationMismatches` |
| `assert-03d411f7` | 2.2.2 | Without a selector, `ispe` describes the final output of operating point zero. | `Av1ItemPropertyTests.GeneralSequenceHeaderOperatingPointFieldsAreReconciled` |
| `assert-f6c470c1` | 2.2.2 | With `a1op`, `ispe` describes the selected operating point output. | `AdditionalItemPropertyTests.NclxAndOperatingPointSelectorsReconcileWithEmbeddedSequenceHeader` |
| `assert-ecdb0910` | 2.2.3 | `clap` is top-left anchored unless the uncropped image is a visible alternative. | `AvifContainerValidationTests.EssentialFlagsPixelAspectRatioAndTransformOrderingAreValidated` |
| `assert-512ec7a3` | 2.3.1 | A single-operating-point image normally omits `a1op`. | `SingleImageAuthoringTests.AuthorsColorAlphaAndIccWhilePreservingDecodedSamples` |
| `assert-cec6554d` | 2.3.2.1 | Associated `a1op` is essential. | `LayeredImageAuthoringTests.AuthorsProgressiveLayersWithOperatingPointAndSharedPayload` |
| `assert-a878ecf3` | 2.3.2.1 | `a1op.op_index` selects an existing operating point. | `AdditionalItemPropertyTests.NclxAndOperatingPointSelectorsReconcileWithEmbeddedSequenceHeader` |
| `assert-081f689f` | 2.3.2.2 | `lsel.layer_id` is 0–3 or `0xffff`. | `AdditionalItemPropertyTests.Av1LayerPropertiesRejectOutOfRangeAndNonCanonicalLayouts` |
| `assert-421bfe00` | 2.3.2.2 | A selected spatial layer exists in the bitstream. | `Av1LayerSelectionTests.MissingOperatingPointsAndLayersAreReported` |
| `assert-d2e18a5d` | 2.3.2.2 | A selected spatial layer produces an output frame. | `Av1LayerSelectionTests.OutputPolicyChoosesHighestSpatialLayerOrEveryProgressiveLayer` |
| `assert-91b2f163` | 2.3.2.3 | Single-layer items normally omit `a1lx`. | `LayeredImageAuthoringTests.RejectsInvalidLayerSetsWithoutConsumingAnItemId` |
| `assert-08b24f89` | 2.3.2.3 | Associated `a1lx` is non-essential. | `LayeredImageAuthoringTests.AuthorsProgressiveLayersWithOperatingPointAndSharedPayload` |
| `assert-1f4a7944` | 2.3.2.3 | `a1lx` entries following the first zero are zero. | `AdditionalItemPropertyTests.Av1LayerPropertiesRejectOutOfRangeAndNonCanonicalLayouts` |
| `assert-31ec505f` | 2.3.2.3 | Nonzero `a1lx` entries equal the number of layers minus one. | `LayeredImageAuthoringTests.AuthorsProgressiveLayersWithOperatingPointAndSharedPayload` |
| `assert-773464dd` | 3 | AV1 tracks satisfy the MIAF image-sequence structure. | `AvifSequenceValidationTests.ConformingSequencePassesStructuralPayloadAndProfileValidation` |
| `assert-f42bd67a` | 3 | AV1 image-sequence tracks use the `pict` handler. | `AvifSequenceValidationTests.RejectsWrongHandlerMultipleDescriptionsAndInconsistentTiming` |
| `assert-457192a4` | 3 | An AV1 track has exactly one sample description. | `AvifSequenceValidationTests.RejectsWrongHandlerMultipleDescriptionsAndInconsistentTiming` |
| `assert-bfc4d5c2` | 3 | Repeated in-band sequence headers are identical. | `AvifSequenceValidationTests.RejectsNonidenticalInBandSequenceHeaders` |
| `assert-1047902f` | 4.1 | Auxiliary items and tracks satisfy MIAF auxiliary structure. | `AuxiliaryAndAlphaTests.DocumentResolvesAlphaDepthAndArbitraryAuxiliaryAssociations` |
| `assert-1243aeeb` | 4.1 | Auxiliary AV1 sequence headers are monochrome. | `AvifSequenceValidationTests.AuxiliarySequenceMustMatchMasterFrameTimingAndBeMonochrome` |
| `assert-12507edd` | 4.1 | Auxiliary AV1 sequence headers signal full range. | `SequenceAuthoringTests.AuthorsMatchedAlphaTrackAndInfiniteRepetition` |
| `assert-37945cd1` | 4.1 | Alpha and master images use the same bit depth. | `AuxiliaryAndAlphaTests.ContainerValidationReportsAlphaDepthAndResizeMismatches` |
| `assert-620a22d6` | 4.1 | Alpha images normally omit `colr`. | `SingleImageAuthoringTests.AuthorsColorAlphaAndIccWhilePreservingDecodedSamples` |
| `assert-04d1a6f0` | 4.1 | Readers ignore alpha-image `colr` signaling. | `AuxiliaryAndAlphaTests.DocumentResolvesAlphaDepthAndArbitraryAuxiliaryAssociations` |
| `assert-1f1192b2` | 4.2.2 | A tone-map item and its base are preferred alternatives. | `AdvancedDerivedAuthoringTests.AuthorsToneMapWithHiddenGainInputAndPreferredFallbackGroup` |
| `assert-c46f3d7f` | 4.2.2 | A gain-map input is hidden. | `AdvancedDerivedAuthoringTests.AuthorsToneMapWithHiddenGainInputAndPreferredFallbackGroup` |
| `assert-66976029` | 4.2.3 | A sample transform has at least one token. | `SampleTransformTests.ParserRejectsUnsupportedAndMalformedExpressions` |
| `assert-989adc85` | 4.2.3 | Sample-transform tokens evaluate in encoded order. | `SampleTransformTests.EveryDefinedOperatorUsesNormativePostfixSemantics` |
| `assert-1f569fa5` | 4.2.3 | Sample operands reference an available input. | `SampleTransformTests.ParserRejectsUnsupportedAndMalformedExpressions` |
| `assert-6c16d91f` | 4.2.3 | A sample-transform expression is nonempty. | `SampleTransformTests.ParserRejectsUnsupportedAndMalformedExpressions` |
| `assert-98b07e13` | 4.2.3 | Unary operators require one stack operand. | `SampleTransformTests.ParserRejectsUnsupportedAndMalformedExpressions` |
| `assert-75c5cbbc` | 4.2.3 | Binary operators require two stack operands. | `SampleTransformTests.ParserRejectsUnsupportedAndMalformedExpressions` |
| `assert-bac41e3a` | 4.2.3 | Evaluation leaves exactly one stack value. | `SampleTransformTests.ParserRejectsUnsupportedAndMalformedExpressions` |
| `assert-988bf23f` | 5 | Unknown entity-group types are ignored semantically. | `EntityGroupBoxTests.UnknownGroupTypeIsOpaqueAndPreservedIndependently` |
| `assert-bd18acea` | 6.1 | AVIF brands resolve to the registered `image/avif` type and `.avif` convention. | `FileTypeBoxTests.ResolverReportsAvifCapabilitiesMimeTypeAndUnknownBrands` |
| `assert-cb307e76` | 6.2 | The primary item is AV1 or a derivation with only AV1 leaves. | `AvifContainerValidationTests.StrictValidationAcceptsAConformingImageItemContainer` |
| `assert-d15b2e01` | 6.2 | Conforming image-item files advertise `avif`. | `FileTypeBoxTests.ValidatorEnforcesRequiredBrandsAndReportsRecommendedBrands` |
| `assert-05eb121f` | 6.2 | `avio` item dependencies contain only intra frames. | `AvifSequenceValidationTests.EnforcesSequenceProfileAndIntraOnlyBrands` |
| `assert-efe9c102` | 6.3 | An `avis` file contains an AV1 image-sequence track. | `AvifSequenceValidationTests.AvisBrandRequiresAnImageSequenceTrack` |
| `assert-f531b09d` | 6.3 | Conforming image-sequence files advertise `avis`. | `SequenceAuthoringTests.AuthorsVariableDurationsFiniteRepetitionAndDecodableStillFallback` |
| `assert-f9288002` | 6.3 | `avio` sequence samples are sync samples and item constraints also hold. | `AvifSequenceValidationTests.EnforcesSequenceProfileAndIntraOnlyBrands` |
| `assert-7d2f92b6` | 6.3 | A qualifying intra-only sequence advertises `avio`. | `SequenceAuthoringTests.AuthorsVariableDurationsFiniteRepetitionAndDecodableStillFallback` |
| `assert-8fc3f160` | 7 | AVIF files advertise `miaf`. | `FileTypeBoxTests.ValidatorEnforcesRequiredBrandsAndReportsRecommendedBrands` |
| `assert-dff472eb` | 7 | AVIF files advertise `avif` or `avis`. | `FileTypeBoxTests.ValidatorEnforcesRequiredBrandsAndReportsRecommendedBrands` |
| `assert-c4bb0e7d` | 7 | Grid-input derivation chains do not carry transformative properties. | `AvifContainerValidationTests.GridInputsRejectTransformsAcrossTheDependencyChain` |
| `assert-90b9bfa3` | 8.1 | At least one advertised profile decodes the primary item. | `AvifContainerValidationTests.ProfileBrandsEnforceCodedLimitsAndSelfContainment` |
| `assert-951c6d0d` | 8.1 | An advertised sequence profile decodes at least one sequence track. | `FileTypeBoxTests.AdvancedProfileValidationDistinguishesItemsAndSequences` |
| `assert-564c67c4` | 8.1 | A sequence profile accounts for associated auxiliary tracks. | `AvifSequenceValidationTests.EnforcesSequenceProfileAndIntraOnlyBrands` |
| `assert-84233327` | 8.2 | Baseline-profile dependencies are self-contained. | `AvifContainerValidationTests.ProfileBrandsEnforceCodedLimitsAndSelfContainment` |
| `assert-cf4ea772` | 8.2 | Baseline-profile grids stay within profile limits. | `GridAuthoringTests.AuthorsLargeGridPayloadAndRaisesIndependentTileLevel` |
| `assert-2b177d00` | 8.2 | Baseline profile permits at most one primary sequence track. | `AvifSequenceValidationTests.MultiplePrimarySequencesMustBeAlternatesWithMatchedDurations` |
| `assert-7bef4687` | 8.2 | Baseline-profile edit-list restrictions are enforced. | `MovieBoxTests.ParserRejectsInvalidTimescaleDimensionsAndEditDuration` |
| `assert-ff5867a4` | 8.2 | Baseline-profile sequence durations match. | `AvifSequenceValidationTests.MultiplePrimarySequencesMustBeAlternatesWithMatchedDurations` |
| `assert-76bdbeb4` | 8.2 | Baseline uses AV1 Main profile at level 5.1 or lower. | `FileTypeBoxTests.BaselineProfileValidationAcceptsBoundaryAndRejectsExcess` |
| `assert-c2e4549c` | 8.3 | Advanced-profile dependencies are self-contained. | `AvifContainerValidationTests.ProfileBrandsEnforceCodedLimitsAndSelfContainment` |
| `assert-3d9cf5a6` | 8.3 | Advanced-profile grids stay within profile limits. | `GridAuthoringTests.AuthorsLargeGridPayloadAndRaisesIndependentTileLevel` |
| `assert-309f6423` | 8.3 | Advanced profile permits at most one primary sequence track. | `AvifSequenceValidationTests.MultiplePrimarySequencesMustBeAlternatesWithMatchedDurations` |
| `assert-fd768c18` | 8.3 | Advanced-profile edit-list restrictions are enforced. | `MovieBoxTests.ParserRejectsInvalidTimescaleDimensionsAndEditDuration` |
| `assert-53ffd4a0` | 8.3 | Advanced-profile sequence durations match. | `AvifSequenceValidationTests.MultiplePrimarySequencesMustBeAlternatesWithMatchedDurations` |
| `assert-994c71bb` | 8.3 | Advanced still images use High profile at level 6.0 or lower. | `FileTypeBoxTests.AdvancedProfileValidationDistinguishesItemsAndSequences` |
| `assert-7f965b8f` | 8.3 | Advanced sequences use Main or High profile. | `FileTypeBoxTests.AdvancedProfileValidationDistinguishesItemsAndSequences` |
| `assert-d00bacd1` | 8.3 | Advanced Main-profile sequences stay at level 5.1 or lower. | `FileTypeBoxTests.AdvancedProfileValidationDistinguishesItemsAndSequences` |
| `assert-d0390d60` | 8.3 | Advanced High-profile sequences stay at level 5.1 or lower. | `AvifSequenceValidationTests.EnforcesSequenceProfileAndIntraOnlyBrands` |
| `assert-e613e861` | 9.1.1 | Required boxes use permitted versions; unknown nonessential properties remain skippable. | `AvifContainerValidationTests.AlternatePropertyVersionsAreOpaqueUnlessMarkedEssential` |
| `assert-cd562e86` | 9.1.2 | Additional item-related boxes use their prescribed versions unless nonessential. | `ItemPropertiesBoxTests.AssociationParserRejectsUnsupportedVersionsAndFlags` |

## AV1 decoder requirement families

| AV1 decoder requirement family | Behavioral tests |
| --- | --- |
| Low-overhead OBU headers, sizes, extensions, temporal units, and frame units | `Av1ObuParserTests.LowOverheadParsingReadsHeadersExtensionsPayloadsAndTemporalUnits`; `Av1ObuParserTests.LengthDelimitedParsingBuildsTemporalAndFrameUnitHierarchy` |
| Annex-B/length-delimited bounds, delimiter placement, and trailing bits | `Av1ObuParserTests.LengthDelimitedParsingEnforcesNestedBoundsAndDelimiterPlacement`; `Av1ObuParserTests.TrailingBitValidationAcceptsPaddingAndRejectsMalformedPatterns` |
| Sequence-header timing, decoder models, operating points, and tool flags | `Av1SequenceHeaderTests.GeneralHeaderRetainsTimingModelsOperatingPointsAndTools` |
| Reduced still-picture header inferred values | `Av1SequenceHeaderTests.ReducedHeaderInfersNormativeDefaults` |
| Main, High, and Professional profile depth/chroma syntax | `Av1SequenceHeaderTests.ProfileColorBranchesProduceExpectedDepthAndSubsampling`; `Av1SemanticValidatorTests.SequenceProfilesFrameIdWidthsAndChromaRulesAreValidated` |
| Uncompressed frame headers, show-existing frames, references, and order hints | `Av1FrameHeaderTests.InterFrameParsesReferencesMotionAndPredictedFilmGrain`; `Av1FrameHeaderTests.ShowExistingFrameRetainsPresentationTimeAndRejectsBadInput` |
| Frame IDs, random access, error resilience, and reference validity | `Av1SemanticValidatorTests.FrameIdsAndReferenceAvailabilityAreValidatedAcrossFrames`; `Av1SemanticValidatorTests.RandomAccessAndShowExistingRequirementsAreEnforced` |
| Frame dimensions, super-resolution syntax, and tile-layout semantics | `Av1SemanticValidatorTests.DimensionsSuperResolutionAndTileLayoutsAreValidated` |
| Tile-group ranges, tile-size syntax, scheduling, and complete coverage | `Av1TileGroupTests.ParserSplitsLittleEndianTileSizesAndOwnsPayload`; `Av1TileGroupTests.SchedulerAbortsGappedGroupsAndReleasesEveryReservation` |
| Large-scale tile lists and camera-tile output composition | `Av1LargeScaleTileTests.TileListModeIsOptInAndRejectsMalformedOrLimitedLists`; `Av1LargeScaleTileTests.DecodedTilesAreComposedInListOrderAndLeaveHolesUntouched` |
| Arithmetic-symbol decoding, literals, non-symmetric values, and CDF adaptation | `Av1SymbolDecoderTests.BooleanLiteralAndNonSymmetricPathsDecodeKnownVectors`; `Av1SymbolDecoderTests.AdaptiveSymbolsUpdateOnlyTheSelectedCdfRow` |
| Default CDFs, tile contexts, primary-reference loading, and frame-end updates | `Av1CdfContextTests.DefaultContextsContainEveryPersistentTableAndQuantizerSlice`; `Av1CdfContextTests.TileContextsAreIndependentAndDesignatedTileUpdatesFrameReferences` |
| Partition legality, recursive traversal, frame edges, and tile boundaries | `Av1PartitionTraversalTests.PartitionLegalityMatchesEverySquareSizeAndRectangularSubsize`; `Av1PartitionTraversalTests.FrameEdgesForcePartitionsAndClipVisibleBlockGeometry` |
| Block modes, segmentation, skip, delta quantizer/filter, and transform splitting | `Av1BlockModeInfoTests.SegmentationFeaturesControlSkipReferenceQuantizerAndLoopFilter`; `Av1BlockModeInfoTests.TransformModeSelectSplitsByDepthAndRejectsExcessDepth` |
| Coefficient tokens, scan order, end-of-block, Golomb extension, dequantization, and matrices | `Av1CoefficientDecoderTests.TransformClassesPlaceCoefficientsInTheirScanOrder`; `Av1CoefficientDecoderTests.BaseRangeAndGolombExtensionDecodeLargeLevels`; `Av1CoefficientDecoderTests.QuantizerMatricesWeightAcCoefficients` |
| Inverse transforms, lossless WHT, reduced coefficient domains, reconstruction, and clipping | `Av1InverseTransformTests.FourByFourSupportsEveryDeclaredTransformType`; `Av1InverseTransformTests.SixtyFourPixelTransformsPadTheReducedCoefficientDomain`; `Av1InverseTransformTests.ReconstructionAddsResidualAndClipsAtEveryBitDepth` |
| Intra DC, directional, smooth, Paeth, filter-intra, CfL, palette, and intra-block-copy prediction | `Av1IntraPredictionTests.EveryDirectionalModeSupportsAngleDeltasAndRectangularBlocks`; `Av1IntraPredictionTests.CflSubsamples420LumaSubtractsAverageAndAppliesSignedAlpha`; `Av1IntraPredictionTests.IntraBlockCopyUsesOnlyPreviouslyDecodedNonOverlappingRegions` |
| Inter subpixel filters, scaling, warp, OBMC, chroma precision, and high-bit-depth clipping | `Av1InterPredictionTests.EverySwitchableSubpixelKernelUsesItsNormativeCoefficients`; `Av1InterPredictionTests.GlobalTranslationAndLocalAffineWarpMapReferenceCoordinates`; `Av1InterPredictionTests.ChromaMotionVectorsRetainOneSixteenthSamplePrecision` |
| Compound average, distance, difference-weighted, wedge, and inter-intra prediction | `Av1CompoundPredictionTests.DistanceWeightsFollowReferenceOrderHintRatios`; `Av1CompoundPredictionTests.WedgeMasksUseAllCodebooksAndOppositeSignsAreComplements`; `Av1CompoundPredictionTests.InterIntraPredictionBlendsIntraTowardItsRelevantEdge` |
| Spatial/temporal motion-vector candidates, projection, precision, clipping, and global models | `Av1MotionVectorTests.ProjectionHandlesSignsScalingClippingAndZeroDenominators`; `Av1MotionVectorTests.TemporalCandidateProjectsStoredMotionUsingOrderHints`; `Av1MotionVectorTests.GlobalTranslationAndAffineModelsProduceBlockCenterVectors` |
| Deblocking level derivation, narrow/wide filters, transform edges, chroma, and high depth | `Av1LoopFilterTests.LevelSelectionCombinesBlockSegmentReferenceAndModeDeltas`; `Av1LoopFilterTests.EightTapWideFilterUsesTheFlatRegionKernel`; `Av1LoopFilterTests.ThresholdsAndFilteringScaleForHighBitDepth` |
| CDEF direction search, constrain function, primary/secondary taps, skip, chroma, and edges | `Av1CdefFilterTests.DirectionSearchFindsCardinalAndDiagonalPatterns`; `Av1CdefFilterTests.SecondaryFilterProducesTheNormativeBoundedResult`; `Av1CdefFilterTests.PartialEdgeBlocksAreFilteredWithoutReadingOutsideTheFrame` |
| Super-resolution normative filter, phases, denominators, edges, odd dimensions, and chroma | `Av1SuperResolutionTests.EightBitRampMatchesNormativeVectorAndReplicatesVisibleEdges`; `Av1SuperResolutionTests.EveryDenominatorPreservesConstantColorAcrossChromaLayouts`; `Av1SuperResolutionTests.OddDimensionsUseCeilingChromaPlaneSizes` |
| Wiener, self-guided, switchable, stripe-boundary, and restoration-unit processing | `Av1LoopRestorationTests.WienerImpulseMatchesNormativeRounding`; `Av1LoopRestorationTests.EverySelfGuidedSetPreservesConstantsAtEveryBitDepth`; `Av1LoopRestorationTests.WienerStripeBoundaryUsesDeblockedSecondContextLine` |
| Decode wrap-up order, motion-field storage, segmentation inheritance, refresh, and output | `Av1DecodeWrapupTests.MotionFieldIsStoredBeforeReferenceRefresh`; `Av1DecodeWrapupTests.SegmentationMapIsInheritedFromPrimaryReference`; `Av1DecodeWrapupTests.HiddenKeyShowExistingAndShownInterStayInDecodeOrder` |
| Reference slots, show-existing identity, output ownership, and decoded-resource limits | `Av1FrameBufferTests.RefreshFlagsShareOneFrameAndOutputsRemainInDecodeOrder`; `Av1FrameBufferTests.OutputOwnershipKeepsOverwrittenFrameAliveUntilRelease`; `Av1FrameBufferTests.DecodedByteAndFrameLimitsFailWithoutTakingPlaneOwnership` |
| Film-grain random process, scaling, chroma-from-luma, overlap, range, and bit depths | `Av1FilmGrainTests.RandomGeneratorAndScalingLookupMatchNormativeVectors`; `Av1FilmGrainTests.ColorChromaFromLumaSupportsEveryLayoutAndBitDepth`; `Av1FilmGrainTests.RestrictedRangeUsesIdentityChromaEndpoint` |
| Metadata OBUs for HDR, scalability, ITU-T T.35/HDR10+, timecode, and unknown payloads | `Av1MetadataTests.HdrMetadataParsesContentLightAndMasteringDisplayValues`; `Av1MetadataTests.ScalabilityStructureRetainsLayersAndTemporalGroups`; `Av1MetadataTests.ItuT35RecognizesHdr10PlusAndUnknownMetadataStaysOpaque` |
| Operating-point filtering, temporal/spatial layer selection, and progressive output policy | `Av1LayerSelectionTests.AutomaticAndExplicitOperatingPointSelectionResolveLayerCaps`; `Av1LayerSelectionTests.SpatialTemporalFilteringKeepsGlobalAndSelectedObus`; `Av1LayerSelectionTests.OutputPolicyChoosesHighestSpatialLayerOrEveryProgressiveLayer` |
| Normative block/transform constants, scans, quantizer matrices, and default CDF tables | `Av1SyntaxTableTests.SyntaxConstantsAndEnumsMatchNormativeValues`; `Av1SyntaxTableTests.ScanTablesMatchKnownVectorsAndEveryScanIsAPermutation`; `Av1SyntaxTableTests.EveryNormativeDefaultCdfIsPresentAndDefensivelyReturned` |
| Exact 8-, 10-, and 12-bit planar reconstruction across legal chroma formats | `Av1EncoderReconstructionTests.ScriptedAndArithmeticDecodersAgreeForEveryFormat`; `Av1PixelConformanceTests.ExactComparisonCoversOddSubsampledTenBitPlanes` |
| Complete normative post-filter ordering | `Av1DecodeWrapupTests.HiddenKeyShowExistingAndShownInterStayInDecodeOrder`; `Av1EncoderFilterDecisionTests.ReconstructionMatchesDirectDecoderFilterComposition` |
| Malformed syntax, semantic failures, truncation, atomic failure, and configured bounds | `Av1ObuParserTests.LowOverheadParsingRejectsIllegalHeadersSizesAndTruncation`; `Av1SequenceHeaderTests.InvalidLocalSemanticsTruncationAndTrailingBitsAreRejected`; `Av1PixelConformanceTests.ValidationRejectsIllegalFormatsLayoutsAndSampleRanges` |
| End-to-end conformance-vector extraction and independent plane comparison | `ImportedVectorTests.OfficialAvifVectorsProbeValidateAndExposeExpectedItemModel`; `ReferenceCodecDifferentialTests.ManagedFormatValidationMatchesLibaomAndDav1d` |

## Maintenance rule

When the frozen standards baseline changes, regenerate the AVIF assertion-anchor inventory and
review every AV1 decoder family before accepting the baseline change. A requirement may map to more
than one test, but no row may be left without a behavioral test. Renaming or deleting a referenced
test requires updating this matrix in the same change.
