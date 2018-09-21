---------- Subjects ----------

StaticPhysicsCalculations.cs
Accelerator.cs
BasicPlayerMovement.cs
DEMONSTRATES: Anonymous functions and delegates, Observer pattern
understanding, asyncronous events, independently learned API, enumerations,
auto-implemented properties and get/set accessors, non-blocking i/o,
encapsulation, linear algebra/vector multiplication, calculus, 
physics, and a little bit of polymorphism.

This is a personal project that I've been working on with a friend from
University. We thought it would be fun to create a simple platforming game
using the Unity API. Like all game engines (at least those that I know of),
classes in those contexts follow the observer pattern, meaning they (the
subjects) have to perform their essential functions everytime the observer
asks them to.

I wrote all this code here, and the core of it is the 'BasicPlayerMovement.cs'
class. The three observer functions called in that class are Awake(),
Update(), and LateUpdate(). 

The Accelerator class in the Physics folder is also very important, and you'll
see it contains a 'Mutate' method that performs the same general functions
you'd expect from a constructor. I know that in OO, external mutability is
something one really needs to be careful about, but it's actually not uncommon
to expose a number of publically mutable elements in Game programming,
particularly when you're working with a garbage-collected language like C#.
Since the Accelerator class is used as the building block of motion systems
throughout the project, we viewed it as preferable to expose the Mutate method
rather than clog the heap with dozens of dereferenced objects we have no
control over the collection of. We thought about making it a struct, but
mutability was actually important for maintaining flexibility within the data
structure in the face of asyncronous events (player input, collision, etc.).

You also may notice a number of public instance variables. This is part of the
Unity API. Any instance variable marked 'public' is exposed in the editor for
the user to change freely. The values marked in the editor are injected into
the instance at runtime. 

This is absolutely a work in progress, and I plan on updating it regularly,
but it's one of the projects I'm having the most fun working with. 

PS You may be wondering why C++ wasn't used given my blabbering about garbage
collection and whatnot; unfortunately in the free version of the engine, the
C++ API isn't exposed. To be frank, however, the type of project we're working
on isn't demanding enough of memory to truly warrant the memory management
freedom of C++. Object pooling and careful use of Structs are generally
sufficient for keeping any framerate-slowing garbage collection trips at bay.
