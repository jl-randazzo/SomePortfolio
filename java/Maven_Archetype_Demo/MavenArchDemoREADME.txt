JOSIAH 'LUKE' RANDAZZO
MAVEN_DEMO_ARCHETYPE

This was an extracurricular activity.

This archetype was designed to give me a quick start on homework assignments
for my Computer Graphics class. For some reason, my professor really wanted to
use the LWJGL (Lightweight Java Gaming Library) to interface with OpenGL
rather than C or C++. Since I knew I'd be needing these two classes
(Basic.java and InputInfo.java) for all of my assignments, I decided it would
be fun to put together a maven archetype to neatly start off all of my
projects. 

Beyond that, I configured the archetype's plugins to include the jar-builder
(designating Basic.java as the home of the main method in the manifest) and
Apache's handy shade plugin. The plugin upgrades the .jar file to an uber-jar
during the 'mvn package' command. All dependencies are included, so the jar
can be run from the command line on any system with JRE 1.8. 

java -jar Maven_Archetype_Demo-1.0-SNAPSHOT.jar

The output is bland, just a red window with some basic input detection. It's 
only supposed to serve as the basis for projects interfacing with OpenGL through LWJGL.

Please look through the pom.xml if you're interested.
