using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
#if EDITOR
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
#endif

namespace Assets.Code.CodeGeneration {
    internal class DynamicMethodCache {
        public static DynamicMethodCache instance { get; private set; }
        public volatile int cacheMisses;
        public volatile int cacheHits;
        private int _dllIndex;
        public int dllIndex { get => _dllIndex++; }

#if EDITOR

        static DynamicMethodCache() {
            instance = new DynamicMethodCache();
        }

        private static readonly string _dynamicHashAssetName = "DynamicMethodHashMap.asset";
        private static readonly string _mapPath = "Assets\\Plugins\\Dynamic\\" + _dynamicHashAssetName;

        private bool _hashDirty = false;

        private DynamicMethodCache() {
            UnityEngine.Debug.Log("Datapath: " + _mapPath);
            var asset = (TextAsset)AssetDatabase.LoadAssetAtPath(_mapPath, typeof(TextAsset));
            if (asset != null) {
                UnityEngine.Debug.Log("Dynamic method hash exists");
                _hashAsset = JsonUtility.FromJson<DynamicMethodHashMap>(asset.text);
                PopulateDictionary();
            } else {
                UnityEngine.Debug.Log("Dynamic method hash asset does not exist");
                _hashAsset = new DynamicMethodHashMap();
            }
            _dllIndex = GetDllIndex();
        }

        private int GetDllIndex() {
            var files = Directory.EnumerateFiles(Application.dataPath + "\\Plugins\\Dynamic\\", "*.dll");
            return files.Count();
        }

        private void Save() {
            TextAsset asset = new TextAsset(JsonUtility.ToJson(_hashAsset));
            AssetDatabase.CreateAsset(asset, _mapPath);
            AssetDatabase.SaveAssets();
            _hashDirty = false;
        }

        public void TriggerSave() {
            if (_hashDirty)
                Save();
        }
#else
        private DynamicMethodCache(TextAsset cacheAsset) {
            _hashAsset = JsonUtility.FromJson<DynamicMethodHashMap>(cacheAsset.text);
            instance = this;
            instance.PopulateDictionary();
        }

        public static void Initialize(TextAsset cacheAsset) {
            if (instance != null) {
                UnityEngine.Debug.LogError("Dynamic method cache instance already exists");
            } else {
                instance = new DynamicMethodCache(cacheAsset);
            }
        }
#endif

        private void PopulateDictionary() {
            foreach (var entry in _hashAsset.hashes) {
                Type type = Type.GetType(entry.assemblyQalifiedName);
                MethodInfo method = type.GetMethod(entry.methodName);
                _dynamicMethodHashmap[new HashArrayWrapper(entry.hash)] = method;
            }
        }

        private readonly DynamicMethodHashMap _hashAsset;
        // critical resource
        private readonly ConcurrentDictionary<HashArrayWrapper, MethodInfo> _dynamicMethodHashmap = new ConcurrentDictionary<HashArrayWrapper, MethodInfo>();
        private static readonly Mutex mut = new Mutex();
        private MethodInfo GetMethodInfo(HashArrayWrapper hashKey) {
            var retVal = _dynamicMethodHashmap.ContainsKey(hashKey) ? _dynamicMethodHashmap[hashKey] : null;
            return retVal;
        }
        private void SetMethodInfo(HashArrayWrapper hashKey, MethodInfo value) {
            _dynamicMethodHashmap[hashKey] = value;
#if EDITOR
            // add if the value is not equal to the placeholder method
            if (value != typeof(DynamicCodeFragmentGenerator).GetMethod("GetGeneratedMethods")) {
                _hashAsset.hashes.Add(new SerializedMethodHash() { hash = hashKey.bytes, methodName = value.Name, assemblyQalifiedName = value.DeclaringType.AssemblyQualifiedName });
                _hashDirty = true;
            }
#endif
        }

        public void Claim() {
            mut.WaitOne();
            Console.WriteLine("access granted to cache");
        }
        public void Release() {
            mut.ReleaseMutex();
        }

        public MethodInfo this[HashArrayWrapper hashKey] {
            get => GetMethodInfo(hashKey);
            set => SetMethodInfo(hashKey, value);
        }
    }

    public struct HashArrayWrapper : IEquatable<HashArrayWrapper> {
        private readonly byte[] _bytes;
        public byte[] bytes => _bytes;
        public HashArrayWrapper(byte[] bytes) {
            _bytes = bytes;
        }

        public override int GetHashCode() {
            int accumulator = 0;
            foreach (var @byte in bytes) {
                accumulator += @byte;
            }
            return accumulator;
        }

        public override bool Equals(object obj) {
            return obj is HashArrayWrapper other && Equals(other);
        }

        public bool Equals(HashArrayWrapper other) {
            return _bytes.SequenceEqual(other.bytes);
        }
    }

    [Serializable]
    internal class DynamicMethodHashMap {
        public List<SerializedMethodHash> hashes = new List<SerializedMethodHash>();
    }

    [Serializable]
    internal class SerializedMethodHash {
        public byte[] hash;
        public string methodName;
        public string assemblyQalifiedName;
    }
}
