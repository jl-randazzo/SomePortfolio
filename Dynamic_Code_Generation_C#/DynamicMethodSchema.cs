using System;
using System.Linq;
using System.Security.Cryptography;
using Assets.Code.GameCode.CodeGeneration;
using Assets.Code.GameCode.System.Schemata;
using UnityEngine;

namespace Assets.Code.System.Schemata {
    /*
     * The hash is left unserialized because it is calculated at runtime; the same is true for the _methodName;
     */
    [Serializable]
    public class DynamicMethodSchema : BaseSchema {
        [NonSerialized]
        private HashArrayWrapper _hash;
        public HashArrayWrapper hash { get => _hash; set => _hash = value; }
        [NonSerialized]
        private string _methodName;
        public string methodName { get => _methodName; set => _methodName = value; }

        public string methodBody;
        // return type is the assembly-qualified type name. Use typeof(x).AssemblyQualifiedName
        public string returnType;

        public Type[] argumentData {
            get {
                return _argDataStringTypes.Select(x => Type.GetType(x)).ToArray();
            }
            set {
                _argDataStringTypes = value.Select(x => x.AssemblyQualifiedName).ToArray();
            }
        }

        [SerializeField]
        private string[] _argDataStringTypes = Array.Empty<string>();

        public Type[] additionalReferencedTypes {
            get {
                return _addRefStringTypes.Select(x => Type.GetType(x)).ToArray();
            }
            set {
                _addRefStringTypes = value.Select(x => x.AssemblyQualifiedName).ToArray();
            }
        }

        [SerializeField]
        private string[] _addRefStringTypes = Array.Empty<string>();
        public override string ToString() {
            string accumulator = "";
            accumulator += methodBody + returnType;
            foreach (var type in argumentData) {
                accumulator += type.Name;
            }
            return accumulator;
        }
        public DynamicMethodSchema() {
            argumentData = Array.Empty<Type>();
            additionalReferencedTypes = Array.Empty<Type>();
        }
    }
}
