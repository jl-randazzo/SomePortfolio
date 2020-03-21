AUTHOR: Luke Randazzo

This is part of a much, much larger project. I was designing a GUI for an animation project that would allow users to dynamically inspect
properties of a given object without having the write any actual code. The initial system that underlied this was essential a complex series
of reflection calls that could hitch performance. I decided to try a different approach: dynamically generate code based on
user input, compile them in EDIT mode, and then store the dynamic code fragments in a system hash that could be recalled at any time.

This implementation also needed to be thread-safe, as multiple threads are operating in the same space with some frequency. Every time
an object is trying to construct its dynamic components, it needs to claim the DynamicMethodCache. Each dynamic method generates a distinct SHA-256
hash based on its arguments, return type, and the internals of the code. The DynamicCodeFragmentGenerator takes in an array of DynamicMethodSchema
objects, and duplicate code is registered and linked to the same exact dynamic method. If the method doesn't exist, a placeholder is indexed
for each of the methods, and then the compiler is launched, the assembly is generated, and a dictionary containing key/value pairs of 
hash/MethodInfo is populated using the new assembly. Additionally, a json object enumerating these relationships is updated.

All of this is done in the context of the EDIT mode. Once the user actually runs the simulation, all of the dynamic assemblies have already
been generated; the json asset is loaded, and any ConditionGate or other dynamic object requiring a lightning-fast snippet of code
retrieves the necessary code from the DynamicMethodCache.

PERFORMANCE

I've included a unit test file called ConditionAtomConstructorTest.cs that demonstrates the asynchronous logic functioning. It also 
writes performance statistics to the console. Making use of both code generation and C#'s dynamic callsites, I managed to reduce
the time it takes to run dynamic methods by over 80% for simple methods and over 90% for complex methods when compared with the reflection-
based solution I was using before. 
