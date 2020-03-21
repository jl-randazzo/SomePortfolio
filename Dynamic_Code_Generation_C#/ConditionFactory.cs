using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Assets.Code.GameCode.CodeGeneration;
using Assets.Code.GameCode.System.Schemata;
using Assets.Code.System.Schemata;
using UnityEngine;
using UnityEngine.Assertions;
using PrimitiveType = Assets.Code.GameCode.System.Schemata.PrimitiveType;

namespace Assets.Code.System.CodeGeneration {
    public class ConditionFactory {
        Dictionary<string, object> _objDictionary;
        DynamicMethodSchema _methodSchema;

        public ConditionFactory(Dictionary<string, object> objDictionary) {
            _objDictionary = objDictionary;
        }

        public IEnumerable<ICondition> BuildAll(ConditionSchema[] schemata) {
            var watch = Stopwatch.StartNew();
            var dict = GetMethodSchemaDictionary(schemata).ToArray();
            var methodInfo = DynamicCodeFragmentGenerator.GetGeneratedMethods(dict.Select(x => x.Value).ToArray());
            var methodDictionary = new Dictionary<Guid, MethodInfo>();
            for (int i = 0; i < methodInfo.Length; i++) {
                methodDictionary.Add(dict[i].Key, methodInfo[i]);
            }

            var retVal = BuildAll(schemata, methodDictionary);
            watch.Stop();
            Console.WriteLine("Build time: " + watch.ElapsedMilliseconds);
            return retVal;
        }

        public ICondition Build(ConditionSchema schema) {
            return BuildAll(new[] { schema }).ElementAt(0);
        }

        private IEnumerable<ICondition> BuildAll(ConditionSchema[] schemata, Dictionary<Guid, MethodInfo> methodDictionary) {
            foreach (var schema in schemata) {
                yield return Build(schema, methodDictionary);
            }
        }

        private Dictionary<Guid, DynamicMethodSchema> GetMethodSchemaDictionary(ConditionSchema[] schemata) {
            var dictionary = new Dictionary<Guid, DynamicMethodSchema>();
            foreach (var schema in schemata) {
                if (schema.conditionType == ConditionType.Atom) {
                    schema.atomicSchema = schema.atomicSchema == null ? GetMethodSchema(schema) : schema.atomicSchema;
                    dictionary.Add(schema.id, schema.atomicSchema);
                } else {
                    var nestedDictionary = GetMethodSchemaDictionary(schema.children);
                    foreach (var keyValuePair in nestedDictionary) {
                        dictionary.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }
            }
            return dictionary;
        }

        private ICondition Build(ConditionSchema schema, Dictionary<Guid, MethodInfo> methodDictionary) {
            switch (schema.conditionType) {
                case ConditionType.Atom:
                    return BuildAtom(schema, methodDictionary);
                default:
                    return new ConditionsGate(schema.conditionType, BuildAll(schema.children, methodDictionary).ToArray());
            }
        }

        private ICondition BuildAtom(ConditionSchema schema, Dictionary<Guid, MethodInfo> methodDictionary) {
            return AtomFromMethodInfoAndSchema(methodDictionary[schema.id], schema);
        }

        /*
         * Through the use of dynamic delegate types and the switchboard, performance is radically increased from a traditional Invoke() or
         * the DynamicInvoke() call. It makes for a longer method, but the performance gains are worth it.
         */
        private ICondition AtomFromMethodInfoAndSchema(MethodInfo methodInfo, ConditionSchema schema) {
            List<object> argumentList = new List<object>();
            argumentList.Add(_objDictionary[schema.targetSchema.rootObjectKey]);
            for (int i = 0; i < schema.argumentSchemata.Length; i++) {
                var param = schema.argumentSchemata[i];
                if (param.primitiveType == PrimitiveType.NonPrimitive) {
                    argumentList.Add(_objDictionary[param.rootObjectKey]);
                }
            }
            dynamic[] a = argumentList.ToArray();

            List<Type> typeList = methodInfo.GetParameters().Select(x => x.ParameterType).ToList();
            typeList.Add(methodInfo.ReturnType);
            Type delegateType = Expression.GetFuncType(typeList.ToArray());
            var rawDelegate = methodInfo.CreateDelegate(delegateType);
            dynamic del = Convert.ChangeType(rawDelegate, delegateType);

            switch (a.Length) {
                case 2:
                    return new ConditionAtom(() => del(a[0], a[1]), schema.onceMetAlwaysMet);
                case 3:
                    return new ConditionAtom(() => del(a[0], a[1], a[2]), schema.onceMetAlwaysMet);
                case 4:
                    return new ConditionAtom(() => del(a[0], a[1], a[2], a[3]), schema.onceMetAlwaysMet);
                case 5:
                    return new ConditionAtom(() => del(a[0], a[1], a[2], a[3], a[4]), schema.onceMetAlwaysMet);
                case 6:
                    return new ConditionAtom(() => del(a[0], a[1], a[2], a[3], a[4], a[5]), schema.onceMetAlwaysMet);
                case 7:
                    return new ConditionAtom(() => del(a[0], a[1], a[2], a[3], a[4], a[5], a[6]), schema.onceMetAlwaysMet);
                case 8:
                    return new ConditionAtom(() => del(a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7]), schema.onceMetAlwaysMet);
                case 9:
                    return new ConditionAtom(() => del(a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7], a[8]), schema.onceMetAlwaysMet);
                default:
                    return new ConditionAtom(() => del(a[0]), false);
            }
        }

        public static ConditionSchema GetMethodSchemaForAll(ConditionSchema schema) {
            if (schema.conditionType == ConditionType.Atom) {
                schema.atomicSchema = GetMethodSchema(schema);
            } else {
                foreach (var child in schema.children) {
                    GetMethodSchemaForAll(child);
                }
            }
            return schema;
        }

        public static DynamicMethodSchema GetMethodSchema(ConditionSchema schema) {
            DynamicMethodSchema methodSchema = new DynamicMethodSchema();
            var argumentTypeList = new List<Type>();
            var additionalReferencedTypes = new List<Type>();
            argumentTypeList.Add(schema.targetSchema.propertyType);
            if (schema.targetSchema.propertySequence.Last().propertyType != null) {
                additionalReferencedTypes.Add(schema.targetSchema.propertySequence.Last().propertyType);
            }
            foreach (var x in schema.argumentSchemata) {
                if (x.primitiveType == PrimitiveType.NonPrimitive) {
                    argumentTypeList.Add(x.propertyType);
                    if (x.propertySequence.Length > 0 && x.propertySequence.Last().propertyType != null) {
                        additionalReferencedTypes.Add(x.propertySequence.Last().propertyType);
                    }
                }
            }
            methodSchema.argumentData = argumentTypeList.ToArray();
            methodSchema.additionalReferencedTypes = additionalReferencedTypes.ToArray();
            methodSchema.returnType = typeof(bool).AssemblyQualifiedName;
            methodSchema.methodBody = DynamicCodeFragmentGenerator.GetMethodBodyFromConditionSchema(schema);
            return methodSchema;
        }
    }
}
