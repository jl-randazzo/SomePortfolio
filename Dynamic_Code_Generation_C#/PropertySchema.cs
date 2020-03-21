using System;

namespace Assets.Code.System.Schemata {
    [Serializable]
    public class PropertySchema : BaseSchema {
        public PrimitiveType primitiveType;
        public long primInt;
        public float primFloat;
        public string primString;

        public string rootObjectKey;
        public string propertyTypeName;
        public Type propertyType { get => Type.GetType(propertyTypeName); set => propertyTypeName = value.AssemblyQualifiedName; }
        public PropertyNode[] propertySequence;
        public PropertySchema() {
            propertySequence = new PropertyNode[0];
            primitiveType = PrimitiveType.NonPrimitive;
        }

        public Type destinationType {
            get {
                Type type;
                if (propertySequence.Length > 0) {
                    type = propertySequence[propertySequence.Length - 1].propertyType;
                } else {
                    type = propertyType;
                }
                return type;
            }
        }
    }

    [Serializable]
    public struct PropertyNode {
        public string propertyName;
        public Type propertyType {
            get => Type.GetType(propertyTypeName); set => propertyTypeName = value.AssemblyQualifiedName;
        }

        public string propertyTypeName;


        public PropertyNode(string propname, string typename) {
            propertyName = propname;
            propertyTypeName = typename;
        }

        public PropertyNode(string propname, Type type) {
            propertyName = propname;
            propertyTypeName = type.AssemblyQualifiedName;
        }
    }
}
