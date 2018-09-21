----README FOR OBJECTDEEPDUPLICATOR.CS----

This small project intially arose out of a desire to force people on the team
I was working with to write better tests. I discovered that several of the
assertions my teammates were making were passing, to be sure, but only because
they were Asserting equality on reference types without a .Equals() override. 

Selfishly, I also wanted to practice with reflection. The idea here was that I
would use generics and reflection to duplicate simple objects. The most
difficult aspect was implementing list functionality. 

I probably don't need to tell anyone that this works in very limited scope. If
I were to continue working on it, I would implement a table of reference
values so that the duplicator can account for circular referencing. As it
stands, if an object contains a reference to itself, for example, it will
ceaselessly create duplicates. 

Time constraints surrounding the project we were working on led me to stop
working on it once it was doing what I needed it to do for the classes we were
testing.
