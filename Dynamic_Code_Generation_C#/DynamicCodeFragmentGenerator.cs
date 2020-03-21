using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Code.GameCode.System.Schemata;
using Assets.Code.System.Schemata;
using JetBrains.Annotations;
using Microsoft.CSharp;
using UnityEngine;
using PrimitiveType = Assets.Code.GameCode.System.Schemata.PrimitiveType;

namespace Assets.Code.CodeGeneration {
    public static class DynamicCodeFragmentGenerator {
        private static DynamicMethodCache _dynamicMethodCache => DynamicMethodCache.instance;
        public static int cacheMisses => _dynamicMethodCache.cacheMisses;
        public static int cacheHits => _dynamicMethodCache.cacheHits;

        // Code & Assembly Data
        private static readonly string opener =
            @"namespace DynamicallyGeneratedCodeFragments {
                public static partial class Fragments {";
        private static readonly string closer = "}}";

        private static int _methodCount = 0;
        private static string autoChangingMethodName => "DynamicMethod_" + _methodCount++;

        private static readonly MethodInfo _placeHolderMethod = typeof(DynamicCodeFragmentGenerator).GetMethod("GetGeneratedMethods");

        private static readonly List<Tuple<string, string>> _usingStatementsAndDlls = new List<Tuple<string, string>>();

        private static readonly SHA256 SHA = SHA256.Create();
        public static void CalculateAndStoreHashes([NotNull] DynamicMethodSchema[] schemata) {
            foreach (var schema in schemata) {
                schema.hash = schema.hash.bytes == null ? new HashArrayWrapper(SHA.ComputeHash(Encoding.ASCII.GetBytes(schema.ToString()))) : schema.hash;
            }
        }

        public static MethodInfo GetGeneratedMethod([NotNull] DynamicMethodSchema schema) {
            return GetGeneratedMethods(new[] { schema })[0];
        }

        public static MethodInfo[] GetGeneratedMethods([NotNull] DynamicMethodSchema[] schemata) {
            StringBuilder classBuilder = new StringBuilder();
            StringBuilder classBodyBuilder = new StringBuilder();
            MethodInfo[] methods = new MethodInfo[schemata.Length];

            _dynamicMethodCache.Claim();
            CalculateAndStoreHashes(schemata);
            int index = 0, toCompile = 0;
            foreach (var schema in schemata) {
                if (_dynamicMethodCache[schema.hash] == null) {
                    _dynamicMethodCache.cacheMisses++;
                    classBodyBuilder.Append(GetStaticMethodString(schema));
                    var newUsingStatements = CollectNewUsingStatements(schema);
                    AppendNewUsingStatements(newUsingStatements, classBuilder);

                    // This is a placeholder in case our schemata contain duplicates.
                    _dynamicMethodCache[schema.hash] = _placeHolderMethod;
                    toCompile++;
                } else {
                    methods[index] = _placeHolderMethod;
                    _dynamicMethodCache.cacheHits++;
                }
                index++;
            }

            Type @class = null;
            if (toCompile > 0) {
                classBuilder.Append(opener);
                classBuilder.Append(classBodyBuilder.ToString());
                classBuilder.Append(closer);
                string sourceCode = classBuilder.ToString();

                var watch = Stopwatch.StartNew();
                Assembly assembly = Compile(sourceCode, _usingStatementsAndDlls);
                watch.Stop();
                Console.WriteLine("Compilation time: " + watch.ElapsedMilliseconds);
                _dynamicMethodCache.TriggerSave(); // save the asset map

                @class = assembly.GetType("DynamicallyGeneratedCodeFragments.Fragments");
            }

            // populate method info array. Those that hold a placeholder triggered a cache hit 
            for (int i = 0; i < methods.Length; i++) {
                if (methods[i] != _placeHolderMethod) {
                    methods[i] = @class.GetMethod(schemata[i].methodName);
                    _dynamicMethodCache[schemata[i].hash] = methods[i];
                } else {
                    methods[i] = _dynamicMethodCache[schemata[i].hash];
                    schemata[i].methodName = methods[i].Name;
                }
            }

            _dynamicMethodCache.Release();
            return methods;
        }

        private static readonly CodeDomProvider _codeProvider = CSharpCodeProvider.CreateProvider("C#");
        private static Assembly Compile(string sourceCode, List<Tuple<string, string>> usingStatementsAndDlls) {

            var parameters = new CompilerParameters(usingStatementsAndDlls.Select(x => x.Item2).Append(typeof(MonoBehaviour).Assembly.Location).ToArray());
            parameters.GenerateExecutable = false;
            parameters.GenerateInMemory = true;
            parameters.OutputAssembly = Application.dataPath + "\\Plugins\\Dynamic\\DynamicCode_" + DynamicMethodCache.instance.dllIndex.ToString("D5") + ".dll";

            var results = _codeProvider.CompileAssemblyFromSource(parameters, sourceCode);
            if (results.Errors.HasErrors) {
                UnityEngine.Debug.LogWarning("Errant code: " + sourceCode);
                foreach (CompilerError resultsError in results.Errors) {
                    UnityEngine.Debug.LogError(resultsError.ErrorText);
                }
            }
            return results.CompiledAssembly;
        }

        private static void AppendNewUsingStatements(List<Tuple<string, string>> newUsingStatements, StringBuilder classBuilder) {
            foreach (var str in newUsingStatements) {
                if (null != str) {
                    _usingStatementsAndDlls.Add(str);
                    classBuilder.Append(str.Item1 + "\n");
                }
            }
        }

        private static string GetStaticMethodString(DynamicMethodSchema schema) {
            StringBuilder methodBuilder = new StringBuilder();

            schema.methodName = DynamicCodeFragmentGenerator.autoChangingMethodName;
            string methodText = GenerateMethodText(schema);

            methodBuilder.Append(methodText);

            return methodBuilder.ToString();
        }

        private static string GenerateMethodText(DynamicMethodSchema schema) {

            Type returnType = Type.GetType(schema.returnType);
            string declaration = "public static " + GetFullyConstructedTypeName(returnType) + " " + schema.methodName;
            string arguments = "( ";

            int i = 0;
            foreach (var type in schema.argumentData) {
                arguments += GetFullyConstructedTypeName(type) + " a" + i++.ToString() + ",";
            }
            arguments = arguments.Insert(arguments.Length - 1, ")");
            arguments = arguments.Substring(0, arguments.Length - 1);

            string methodScopeAndBody = "{" + schema.methodBody + "}";

            return declaration + arguments + methodScopeAndBody;
        }

        private static List<Tuple<string, string>> CollectNewUsingStatements(DynamicMethodSchema schema) {
            var newUsingStatements = new List<Tuple<string, string>>();
            var oldUsingStatements = new List<Tuple<string, string>>();
            oldUsingStatements.AddRange(_usingStatementsAndDlls);

            Type returnType = Type.GetType(schema.returnType);
            newUsingStatements.AddRange(GetUsingStatementsForTypeAndGenericArguments(oldUsingStatements, returnType));

            int i = 0;
            foreach (var type in schema.additionalReferencedTypes) {
                newUsingStatements.AddRange(GetUsingStatementsForTypeAndGenericArguments(oldUsingStatements, type));
            }
            foreach (var type in schema.argumentData) {
                newUsingStatements.AddRange(GetUsingStatementsForTypeAndGenericArguments(oldUsingStatements, type));
            }
            return newUsingStatements;
        }

        private static List<Tuple<string, string>> GetUsingStatementsForTypeAndGenericArguments(List<Tuple<string, string>> oldUsingStatementsAndDlls, Type type) {
            var retList = new List<Tuple<string, string>>();
            retList.Add(GenerateUsingStatement(oldUsingStatementsAndDlls, type));
            foreach (var t in type.GetGenericArguments()) {
                retList.AddRange(GetUsingStatementsForTypeAndGenericArguments(oldUsingStatementsAndDlls, t));
            }
            return retList;
        }

        private static Tuple<string, string> GenerateUsingStatement(List<Tuple<string, string>> oldUsingStatementsAndDlls, Type type) {
            string usingStatement = "using " + type.Namespace + ";";
            string dll = type.Assembly.Location;
            Tuple<string, string> usingStatementAndDll = new Tuple<string, string>(usingStatement, dll);

            if (oldUsingStatementsAndDlls.Contains(usingStatementAndDll) || string.IsNullOrEmpty(type.Namespace)) {
                return null;
            } else {
                oldUsingStatementsAndDlls.Add(usingStatementAndDll);
                return usingStatementAndDll;
            }
        }

        public static string GetMethodBodyFromConditionSchema(ConditionSchema schema) {
            StringBuilder sb_method = new StringBuilder();
            int argumentNumber = 0;
            sb_method.Append("return ");
            sb_method.Append("a" + argumentNumber++);
            foreach (var node in schema.targetSchema.propertySequence) {
                sb_method.Append("." + node.propertyName);
            }
            StringBuilder[] sb_args = new StringBuilder[schema.argumentSchemata.Length];
            for (int i = 0; i < sb_args.Length; i++) {
                var sb = new StringBuilder();
                sb_args[i] = sb;
                switch (schema.argumentSchemata[i].primitiveType) {
                    case PrimitiveType.NonPrimitive:
                        sb.Append("a" + (argumentNumber++));
                        foreach (var node in schema.argumentSchemata[i].propertySequence) {
                            sb.Append("." + node.propertyName);
                        }
                        break;
                    case PrimitiveType.Float:
                        sb.Append(schema.argumentSchemata[i].primFloat.ToString());
                        break;
                    case PrimitiveType.String:
                        sb.Append(schema.argumentSchemata[i].primString);
                        break;
                    case PrimitiveType.Integer:
                        sb.Append(schema.argumentSchemata[i].primInt.ToString());
                        break;
                }
            }
            switch (schema.comparisonType) {
                case ComparisonType.Custom:
                    sb_method.Append("." + schema.truthMethodName + "(");
                    for (int i = 0; i < sb_args.Length; i++) {
                        sb_method.Append(sb_args[i].ToString());
                        if (i + 1 < sb_args.Length) {
                            sb_method.Append(",");
                        }
                    }
                    sb_method.Append(")");
                    break;
                case ComparisonType.GreaterEqual:
                    sb_method.Append(" >= " + sb_args[0].ToString());
                    break;
                case ComparisonType.Equal:
                    sb_method.Append(".Equals(" + sb_args[0].ToString() + ")");
                    break;
                case ComparisonType.LessEqual:
                    sb_method.Append(" <= " + sb_args[0].ToString());
                    break;
                case ComparisonType.Less:
                    sb_method.Append(" < " + sb_args[0].ToString());
                    break;
                case ComparisonType.False:
                    sb_method.Append(" == false");
                    break;
                case ComparisonType.True:
                    sb_method.Append(" == true");
                    break;
            }
            sb_method.Append(";");

            return sb_method.ToString();
        }

        public static string GetFullyConstructedTypeName(Type type) {
            return GetFullyConstructedTypeName(type.ToString());
        }

        // This method converts from the string representation of types to one that can be written into code
        // The regular expressions remove the artifacts constructed by Generic type parameters
        public static string GetFullyConstructedTypeName(string fullName) {
            var parameterListMatch = Regex.Match(fullName, @"\[.*\]");

            if (parameterListMatch.Success) {
                Queue<string> typeParameters = new Queue<string>();
                Queue<string> nestedTypeQueue = new Queue<string>();

                // Recursive replacement for nested generic types
                string subTypeList = fullName.Substring(parameterListMatch.Index + 1, parameterListMatch.Length - 2);
                string constructedSubType = GetFullyConstructedTypeName(subTypeList);
                fullName = fullName.Remove(parameterListMatch.Index + 1, parameterListMatch.Length - 2);
                fullName = fullName.Insert(parameterListMatch.Index + 1, constructedSubType);

                parameterListMatch = Regex.Match(fullName, @"\[.*\]");
                if (parameterListMatch.Success) {
                    string parameterList = parameterListMatch.ToString();
                    fullName = fullName.Remove(parameterListMatch.Index, parameterListMatch.Length);

                    var nestedType = Regex.Match(parameterList, @"<.*>");
                    while (nestedType.Success) {
                        parameterList = parameterList.Remove(nestedType.Index, nestedType.Length);
                        parameterList = parameterList.Insert(nestedType.Index, "*");
                        nestedTypeQueue.Enqueue(nestedType.ToString());
                        nestedType = Regex.Match(parameterList, @"<.*>");
                    }

                    parameterList = Regex.Replace(parameterList, @"[\[\] ]", "");
                    parameterList.Split(',').ToList().ForEach(x => typeParameters.Enqueue(x));
                }

                var genericOperatorMatch = Regex.Match(fullName, @"`[0-9]+");
                while (genericOperatorMatch.Success) {
                    int paramCount = Int32.Parse(fullName.Substring(genericOperatorMatch.Index + 1, genericOperatorMatch.Length - 1));
                    StringBuilder genericTypeParametersBuilder = new StringBuilder();
                    genericTypeParametersBuilder.Append('<');
                    for (int i = 0; i < paramCount; i++) {
                        genericTypeParametersBuilder.Append(typeParameters.Dequeue());
                        if (i + 1 < paramCount)
                            genericTypeParametersBuilder.Append(',');
                    }
                    genericTypeParametersBuilder.Append('>');
                    fullName = fullName.Remove(genericOperatorMatch.Index, genericOperatorMatch.Length);
                    fullName = fullName.Insert(genericOperatorMatch.Index, genericTypeParametersBuilder.ToString());
                    genericOperatorMatch = Regex.Match(fullName, @"`[0-9]+");
                }

                var nestedTypeSymbol = Regex.Match(fullName, @"\*");
                while (nestedTypeSymbol.Success) {
                    fullName = fullName.Remove(nestedTypeSymbol.Index, 1);
                    fullName = fullName.Insert(nestedTypeSymbol.Index, nestedTypeQueue.Dequeue());
                    nestedTypeSymbol = Regex.Match(fullName, @"\*");
                }
            }

            return fullName.Replace('+', '.'); //replacing sub-type descriptor
        }
    }
}
