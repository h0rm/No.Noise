GMCS_FLAGS = -unsafe

SOURCES =  \
	Banshee.NoNoise/BansheeLibraryAnalyzer.cs \
	Banshee.NoNoise/NoNoiseClutterSourceContents.cs \
	Banshee.NoNoise/NoNoiseHelpDialog.cs \
	Banshee.NoNoise/NoNoiseSchemas.cs \
	Banshee.NoNoise/NoNoiseService.cs \
	MathLib/Collection.cs \
	MathLib/Combinatorics.cs \
	MathLib/Complex.cs \
	MathLib/Constants.cs \
	MathLib/Distributions/Continuous/BetaDistribution.cs \
	MathLib/Distributions/Continuous/CauchyLorentzDistribution.cs \
	MathLib/Distributions/Continuous/ChiDistribution.cs \
	MathLib/Distributions/Continuous/ChiSquareDistribution.cs \
	MathLib/Distributions/Continuous/ContinuousUniformDistribution.cs \
	MathLib/Distributions/Continuous/ErlangDistribution.cs \
	MathLib/Distributions/Continuous/ExponentialDistribution.cs \
	MathLib/Distributions/Continuous/FisherSnedecorDistribution.cs \
	MathLib/Distributions/Continuous/GammaDistribution.cs \
	MathLib/Distributions/Continuous/LaplaceDistribution.cs \
	MathLib/Distributions/Continuous/LognormalDistribution.cs \
	MathLib/Distributions/Continuous/NormalDistribution.cs \
	MathLib/Distributions/Continuous/ParetoDistribution.cs \
	MathLib/Distributions/Continuous/StableDistribution.cs \
	MathLib/Distributions/Continuous/StandardDistribution.cs \
	MathLib/Distributions/Continuous/StudentsTDistribution.cs \
	MathLib/Distributions/Continuous/TriangularDistribution.cs \
	MathLib/Distributions/ContinuousDistribution.cs \
	MathLib/Distributions/CumulativeNormalDistribution.cs \
	MathLib/Distributions/Discrete/ArbitraryDistribution.cs \
	MathLib/Distributions/Discrete/BernoulliDistribution.cs \
	MathLib/Distributions/Discrete/BinomialDistribution.cs \
	MathLib/Distributions/Discrete/DiscreteUniformDistribution.cs \
	MathLib/Distributions/Discrete/GeometricDistribution.cs \
	MathLib/Distributions/Discrete/HypergeometricDistribution.cs \
	MathLib/Distributions/Discrete/PoissonDistribution.cs \
	MathLib/Distributions/Discrete/ZipfDistribution.cs \
	MathLib/Distributions/DiscreteDistribution.cs \
	MathLib/Distributions/IContinuousGenerator.cs \
	MathLib/Distributions/IContinuousProbabilityDistribution.cs \
	MathLib/Distributions/IDiscreteGenerator.cs \
	MathLib/Distributions/IDiscreteProbabilityDistribution.cs \
	MathLib/Distributions/InvCumulativeNormalDistribution.cs \
	MathLib/IRealFunction.cs \
	MathLib/LinearAlgebra/CholeskyDecomposition.cs \
	MathLib/LinearAlgebra/ComplexMatrix.cs \
	MathLib/LinearAlgebra/ComplexVector.cs \
	MathLib/LinearAlgebra/EigenvalueDecomposition.cs \
	MathLib/LinearAlgebra/IMatrix.cs \
	MathLib/LinearAlgebra/IVector.cs \
	MathLib/LinearAlgebra/LUDecomposition.cs \
	MathLib/LinearAlgebra/Matrix.cs \
	MathLib/LinearAlgebra/NamespaceDoc.cs \
	MathLib/LinearAlgebra/QRDecomposition.cs \
	MathLib/LinearAlgebra/SingularValueDecomposition.cs \
	MathLib/LinearAlgebra/Vector.cs \
	MathLib/Natural.cs \
	MathLib/Number.cs \
	MathLib/OnDemandComputation.cs \
	MathLib/Polynomial.cs \
	MathLib/Properties/Resources.Designer.cs \
	MathLib/Quaternion.cs \
	MathLib/RandomSources/AdditiveLaggedFibonacciRandomSource.cs \
	MathLib/RandomSources/MersenneTwisterRandomSource.cs \
	MathLib/RandomSources/RandomSource.cs \
	MathLib/RandomSources/SystemRandomSource.cs \
	MathLib/RandomSources/XorShiftRandomSource.cs \
	MathLib/Rational.cs \
	MathLib/Ratios.cs \
	MathLib/SampleList.cs \
	MathLib/Set.cs \
	MathLib/SiConstants.cs \
	MathLib/SiPrefixes.cs \
	MathLib/Sorting.cs \
	MathLib/SpecialFunctions.cs \
	MathLib/Statistics/Accumulator.cs \
	MathLib/Statistics/Histogram.cs \
	MathLib/Trigonometry.cs \
	MathLib/Tuple.cs \
	Mirage/Analyzer.cs \
	Mirage/AudioDecoder.cs \
	Mirage/Dbg.cs \
	Mirage/Matrix.cs \
	Mirage/Mfcc.cs \
	Mirage/Scms.cs \
	Mirage/ScmsConfiguration.cs \
	Mirage/Vector.cs \
	NoNoise/Data/DatabaseException.cs \
	NoNoise/Data/DataEntry.cs \
	NoNoise/Data/DataParser.cs \
	NoNoise/Data/NoNoiseDBHandler.cs \
	NoNoise/PCA/PCAnalyzer.cs \
	NoNoise/Visualization/Gui/Button.cs \
	NoNoise/Visualization/Gui/InfoBox.cs \
	NoNoise/Visualization/Gui/MainGui.cs \
	NoNoise/Visualization/Gui/StatusBox.cs \
	NoNoise/Visualization/Gui/StyleSheet.cs \
	NoNoise/Visualization/Gui/ToolbarButton.cs \
	NoNoise/Visualization/Gui/ToolbarToggleButton.cs \
	NoNoise/Visualization/Gui/ZoomButton.cs \
	NoNoise/Visualization/SelectionActor.cs \
	NoNoise/Visualization/SongActor.cs \
	NoNoise/Visualization/SongActorManager.cs \
	NoNoise/Visualization/SongGroup.cs \
	NoNoise/Visualization/SongPoint.cs \
	NoNoise/Visualization/SongPointManager.cs \
	NoNoise/Visualization/Util/Point.cs \
	NoNoise/Visualization/Util/QuadTree.cs \
	NoNoise/Visualization/View.cs
	NoNoise/Visualization/Gui/MainGui.cs \
	NoNoise/Visualization/SongActor.cs \
	NoNoise/Visualization/SongActorManager.cs \
	NoNoise/Visualization/SongGroup.cs \
	NoNoise/Visualization/SongPoint.cs \
	NoNoise/Visualization/SongPointManager.cs \
	NoNoise/Visualization/Util/ClutterHelper.cs \
	NoNoise/Visualization/Util/Point.cs \
	NoNoise/Visualization/Util/QuadTree.cs \
	NoNoise/Visualization/View.cs

LINK =  \
	$(BANSHEE_LIBS) \
	$(CLUTTER_SHARP_LIBS) \
	 -r:System.Data -r:Mono.Data.Sqlite

ASSEMBLY = Banshee.NoNoise

RESOURCES =  \
	Mirage/Resources/dct.filter \
	Mirage/Resources/filterweights.filter \
	NoNoise.addin.xml \
	Resources/tool_menu.xml

#EXTRA_DIST = \
#	Data/airport_locations.tsv

include $(top_srcdir)/build/build.mk
