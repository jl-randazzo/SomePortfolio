using System;
using System.Numerics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Schema;
using Assets.Code.GameCode.CodeGeneration;
using Assets.Code.GameCode.System.CodeGeneration;
using Assets.Code.GameCode.System.Schemata;
using Assets.Code.Physics.Collision;
using Assets.Code.System.Schemata;
using NUnit.Framework;
using PrimitiveType = Assets.Code.GameCode.System.Schemata.PrimitiveType;

namespace Assets.Code.Tests.System.Schemata {
    [TestFixture]
    public class ConditionAtomConstructorTest {
        private ConditionSchema GetWrapperConditionSchema() {
            ConditionSchema schema = new ConditionSchema() {
                conditionType = ConditionType.Atom,
                comparisonType = ComparisonType.Custom,
                truthMethodName = "Contains",
                targetSchema = new PropertySchema() {
                    primitiveType = PrimitiveType.NonPrimitive,
                    rootObjectKey = "wrapper",
                    propertyTypeName = typeof(VectorRangeWrapperWrapper).AssemblyQualifiedName,
                    propertySequence = new PropertyNode[]
                    {
                        new PropertyNode(){ propertyName = "wrapper", propertyTypeName = typeof(VectorRangeWrapper).AssemblyQualifiedName },
                        new PropertyNode(){ propertyName = "range", propertyTypeName = typeof(VectorRange).AssemblyQualifiedName }
                    }
                },
                argumentSchemata = new PropertySchema[]
                {
                    new PropertySchema()
                    {
                        primitiveType = PrimitiveType.NonPrimitive,
                        rootObjectKey = "vecWrapper",
                        propertyTypeName = typeof(VectorWrapper).AssemblyQualifiedName,
                        propertySequence = new PropertyNode[]
                        {
                            new PropertyNode(){ propertyName = "vec", propertyTypeName = typeof(Vector2).AssemblyQualifiedName }
                        }
                    }
                }
            };
            return schema;

        }

        [Test]
        public void TestVectorRangeBuilt() {
            string comparisonMethodName = "Contains";
            Type rootType = typeof(VectorRange);
            Type argType = typeof(Vector2);

            VectorWrapper wrapper = new VectorWrapper(new Vector2(1, 1));
            VectorRangeWrapperWrapper rangeWrapper = new VectorRangeWrapperWrapper(new VectorRangeWrapper(VectorRange.Any));

            var condAtom2 = new ConditionAtom(() => rangeWrapper.wrapper.range.Contains(wrapper.vec), false);
            dynamic rangeWrapper_dyn = rangeWrapper;
            dynamic wrapper_dyn = wrapper;
            var condAtom3 = new ConditionAtom(() => rangeWrapper_dyn.wrapper.range.Contains(wrapper_dyn.vec), false);

            Console.WriteLine(typeof(Vector2).Name);
            var schema = GetWrapperConditionSchema();

            Dictionary<string, object> objDictionary = new Dictionary<string, object>();
            objDictionary.Add("wrapper", rangeWrapper);
            objDictionary.Add("vecWrapper", wrapper);

            var condAtom = new ConditionFactory(objDictionary).Build(schema);
            Assert.True(condAtom.Met());
            condAtom2.Met();
            condAtom3.Met();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 1000000; i++) {
                condAtom.Met();
            }
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds for dynamically compiled method: " + watch.ElapsedMilliseconds);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < 1000000; i++) {
                condAtom2.Met();
            }
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds for hard-coded delegate: " + watch.ElapsedMilliseconds);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < 1000000; i++) {
                condAtom3.Met();
            }
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds for dynamically-typed, hard-coded delegate: " + watch.ElapsedMilliseconds);
        }

        [Test]
        public void NestedComparisonTest() {
            var wrapperA = new FloatClassWrapper<Vector2>.FloatWrapperA<Tuple<string, string>, float>(2);
            FloatWrapperB wrapperB = new FloatWrapperB(2);
            Dictionary<string, object> objDictionary = new Dictionary<string, object>();
            objDictionary.Add("floatWrapper_a", wrapperA);
            objDictionary.Add("floatWrapper_b", wrapperB);
            var schema = FloatComparisonNestedSchemata();

            var condGate = new ConditionFactory(objDictionary).Build(schema);
            Assert.True(condGate.Met());
            schema.conditionType = ConditionType.And;
            condGate = new ConditionFactory(objDictionary).Build(schema);
            Assert.False(condGate.Met());
            wrapperB.f = 1;
            Assert.True(condGate.Met());

            Stopwatch watch = new Stopwatch();
            condGate.Met(); // clearing for cache and JiT
            watch.Start();
            for (int i = 0; i < 1000000; i++) {
                condGate.Met();
            }
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds for dynamically compiled method: " + watch.ElapsedMilliseconds);
            ICondition hardCodedConditionGate = new ConditionsGate(ConditionType.And, new ConditionAtom(() => wrapperA.f >= 2, false), new ConditionAtom(() => wrapperB.f < 2, false));
            watch.Reset();
            hardCodedConditionGate.Met(); // clearing cache and JiT
            watch.Start();
            for (int i = 0; i < 1000000; i++) {
                hardCodedConditionGate.Met();
            }
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds for hard-coded delegate: " + watch.ElapsedMilliseconds);

            dynamic dyn_wrapperA = wrapperA;
            dynamic dyn_wrapperB = wrapperB;
            ICondition dynamicConditionGate = new ConditionsGate(ConditionType.And, new ConditionAtom(() => dyn_wrapperA.f >= 2, false), new ConditionAtom(() => dyn_wrapperB.f < 2, false));
            watch.Reset();
            dynamicConditionGate.Met(); // clearing cache and JiT
            watch.Start();
            for (int i = 0; i < 1000000; i++) {
                dynamicConditionGate.Met();
            }
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds for dynamically-typed, hard-coded delegate: " + watch.ElapsedMilliseconds);
        }

        [Test]
        public void TestDoubleDuplicateMethods() {
            var wrapperA = new FloatWrapperB(2);
            var wrapperB = new FloatWrapperB(2);
            var schema = FloatComparisonNestedSchemata();
            schema.children[0].targetSchema.propertyTypeName = (typeof(FloatWrapperB).AssemblyQualifiedName);
            schema.children[0].comparisonType = ComparisonType.Equal;
            schema.children[1].comparisonType = ComparisonType.Equal;

            Dictionary<string, object> objDictionary = new Dictionary<string, object>();
            objDictionary.Add("floatWrapper_a", wrapperA);
            objDictionary.Add("floatWrapper_b", wrapperB);
            ICondition cond = new ConditionFactory(objDictionary).Build(schema);
            Assert.True(cond.Met());
            wrapperA.f = 1;
            Assert.True(cond.Met());
            wrapperB.f = 3;
            Assert.False(cond.Met());
        }

        [Test]
        public void MutualExclusionAsyncTest() {
            DynamicCodeFragmentGenerator.CalculateAndStoreHashes(new DynamicMethodSchema[] { new DynamicMethodSchema() });
            int dynamicCacheHits = DynamicCodeFragmentGenerator.cacheHits;
            int dynamicCacheMisses = DynamicCodeFragmentGenerator.cacheMisses;
            var ret = TaskRunner();
            ret.Wait();
            object[] array = (object[])ret.Result;
            ICondition[] conds = array.Cast<ICondition>().ToArray();
            foreach (var cond in conds) {
                Assert.True(cond.Met());
            }
            int endCacheHits = DynamicCodeFragmentGenerator.cacheHits;
            int endCacheMisses = DynamicCodeFragmentGenerator.cacheMisses;
            Console.Write("Misses: " + endCacheMisses + " " + dynamicCacheMisses);
            Console.Write("Hits: " + endCacheHits + " " + dynamicCacheHits);
            Assert.AreEqual(2, endCacheMisses - dynamicCacheMisses);
            Assert.AreEqual(4, endCacheHits - dynamicCacheHits);
        }

        private async Task<object> TaskRunner() {
            var wrapperA = new FloatClassWrapper<Vector2>.FloatWrapperA<Tuple<string, string>, float>(2);
            var wrapperB = new FloatWrapperB(2);
            var schema = FloatComparisonNestedSchemata();
            var schema_2 = FloatComparisonNestedSchemata();
            var schema_3 = FloatComparisonNestedSchemata();
            Dictionary<string, object> objDictionary = new Dictionary<string, object>();
            objDictionary.Add("floatWrapper_a", wrapperA);
            objDictionary.Add("floatWrapper_b", wrapperB);

            var a = AsyncTest(objDictionary, schema);
            var b = AsyncTest(objDictionary, schema_2);
            var c = Task.Run(async () => new ConditionFactory(objDictionary).Build(schema_3));
            return await Task.WhenAll(a, b, c);
        }

        private Task<ICondition> AsyncTest(Dictionary<string, object> dict, ConditionSchema schema) {
            return Task.Run(() => new ConditionFactory(dict).Build(schema));
        }

        [Test]
        public void MatchingHashTest() {
            SHA256 SHA = SHA256.Create();
            byte[] randomBytes = Encoding.ASCII.GetBytes("test string");
            string lastHash = Encoding.ASCII.GetString(SHA.ComputeHash(randomBytes));
            for (int i = 0; i < 10; i++) {
                SHA = SHA256.Create();
                string currentHash = Encoding.ASCII.GetString(SHA.ComputeHash(randomBytes));
                Assert.AreEqual(currentHash, lastHash);
                lastHash = currentHash;
            }
        }

        private ConditionSchema FloatComparisonNestedSchemata() {
            var condSchema = new ConditionSchema();
            condSchema.conditionType = ConditionType.Or;
            condSchema.children = new ConditionSchema[]
            {
                new ConditionSchema() {
                    targetSchema =  new PropertySchema() {
                        rootObjectKey = "floatWrapper_a",
                        propertyTypeName = typeof(FloatClassWrapper<Vector2>.FloatWrapperA<Tuple<string,string>, float>).AssemblyQualifiedName,
                        propertySequence = new PropertyNode[]
                        {
                            new PropertyNode() { propertyName = "f", propertyTypeName = typeof(float).AssemblyQualifiedName }
                        },
                        primitiveType = PrimitiveType.NonPrimitive
                    },
                    argumentSchemata = new PropertySchema[]
                    {
                        new PropertySchema()
                        {
                            primitiveType = PrimitiveType.Float,
                            primFloat = 2
                        }
                    },
                    comparisonType = ComparisonType.GreaterEqual,
                    conditionType = ConditionType.Atom
                },
                new ConditionSchema()
                {
                    targetSchema =  new PropertySchema()
                    {
                        rootObjectKey = "floatWrapper_b",
                        propertyTypeName = typeof(FloatWrapperB).AssemblyQualifiedName,
                        propertySequence = new PropertyNode[]
                        {
                            new PropertyNode()
                            {
                                propertyName = "f", propertyTypeName = typeof(float).AssemblyQualifiedName
                            }
                        },
                        primitiveType = PrimitiveType.NonPrimitive
                    },
                    argumentSchemata = new PropertySchema[]
                    {
                        new PropertySchema()
                        {
                            primitiveType = PrimitiveType.Float,
                            primFloat = 2
                        }
                    },
                    comparisonType = ComparisonType.Less,
                    conditionType = ConditionType.Atom
                }
            };
            return condSchema;
        }

        public class FloatClassWrapper<TypeParam> {
            public class FloatWrapperA<TypeParam, TypeParamB> {
                public float f { get; set; }

                public FloatWrapperA(float f) {
                    this.f = f;
                }
            }

        }


        public class FloatWrapperB {
            public float f { get; set; }

            public FloatWrapperB(float f) {
                this.f = f;
            }
        }


        public class VectorWrapper {
            public Vector2 vec { get; private set; }
            public VectorWrapper(Vector2 vec) {
                this.vec = vec;
            }

        }

        public class VectorRangeWrapperWrapper {
            public VectorRangeWrapper wrapper { get; private set; }

            public VectorRangeWrapperWrapper() { }
            public VectorRangeWrapperWrapper(VectorRangeWrapper wrapper) {
                this.wrapper = wrapper;
            }
        }

        public class VectorRangeWrapper {
            public VectorRange range { get; private set; }
            public VectorRangeWrapper() { }
            public VectorRangeWrapper(VectorRange range) {
                this.range = range;
            }
        }
    }
}
