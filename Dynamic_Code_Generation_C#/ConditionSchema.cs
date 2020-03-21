using System;
using Assets.Code.System.Schemata;
using UnityEngine;

namespace Assets.Code.System.Schemata {
    public enum ConditionType { And, Or, Atom }
    public enum PrimitiveType { Float, Integer, String, NonPrimitive }
    public enum ComparisonType {
        Greater, GreaterEqual, Less, LessEqual, Equal, True, False, Custom
    }

    [Serializable]
    public class ConditionSchema : BaseSchema {
        [SerializeField]
        public DynamicMethodSchema atomicSchema;
        [SerializeField]
        public ConditionSchema[] children;
        public ConditionType conditionType;
        public ComparisonType comparisonType;

        public string truthMethodName;
        public bool onceMetAlwaysMet;
        public bool checkEveryFrame;

        public PropertySchema targetSchema;
        public PropertySchema[] argumentSchemata;
        public ConditionSchema() {
            conditionType = ConditionType.Atom;
            children = Array.Empty<ConditionSchema>();
            argumentSchemata = Array.Empty<PropertySchema>();
            targetSchema = new PropertySchema();
        }
    }
}
