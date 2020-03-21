using System;

namespace Assets.Code.System.Schemata {
    public class BaseSchema {
        // this is generated at runtime and can be used to track a specific Schema instance
        public readonly Guid id = Guid.NewGuid();
    }
}
