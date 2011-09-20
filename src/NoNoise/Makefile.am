SOURCES =  \
	Banshee.NoNoise/NoNoiseSource.cs \
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
	$(CLUTTER_SHARP_LIBS) 

ASSEMBLY = Banshee.NoNoise

RESOURCES = NoNoise.addin.xml

#EXTRA_DIST = \
#	Data/airport_locations.tsv

include $(top_srcdir)/build/build.mk
