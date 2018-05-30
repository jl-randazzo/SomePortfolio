using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDeepDuplicator
{
    public class ObjectDeepDuplicator
    {
        MethodInfo _listElementsCopy;
        MethodInfo _entityCopy;

        public ObjectDeepDuplicator()
        {
            this._listElementsCopy = GetType().GetMethod("ListElementsCopy");
            this._entityCopy = GetType().GetMethod("EntityCopy");
        }

        public T EntityCopy<T>(T x) where T : class, new()
        {
            T result = new T();
            Type type = x.GetType();

            if (type.GetInterfaces().Select(z => z.Name).Contains("ICollection`1") && (int)type.GetProperty("Count").GetValue(x) > 0)
            {
                Type listItemsType = type.GenericTypeArguments.ElementAt(0);
                _listElementsCopy.MakeGenericMethod(listItemsType).Invoke(this, new object[] { x, result });
            }

            FieldInfo[] fieldInfo = type.GetFields();
            FieldInfo field;
            List<Type> fieldTypes = new List<Type>();
            foreach (var xField in fieldInfo)
            {
                if (xField.GetValue(x) == null) fieldTypes.Add(null);
                else fieldTypes.Add(xField.GetValue(x).GetType());
            }

            Type fieldType;
            PropertyInfo[] propertyInfo = type.GetProperties();
            List<Type> propertyTypes = new List<Type>();
            foreach (var xProperty in propertyInfo)
            {
                if (xProperty.GetValue(x) == null) propertyTypes.Add(null);
                else propertyTypes.Add(xProperty.GetValue(x).GetType());
            }

            PropertyInfo property;
            Type propertyType;

            for (int i = 0; i < fieldInfo.Length; i++)
            {
                field = fieldInfo.ElementAt(i);
                fieldType = fieldTypes.ElementAt(i);

                if (!fieldType.IsPointer || fieldType.Name == "String")
                {
                    field.SetValue(result, field.GetValue(x));
                }
                else // if current field is a reference type, we need to use recursion to duplicate it as well
                {
                    MethodInfo recursion = _entityCopy.MakeGenericMethod(fieldType);
                    field.SetValue(result, recursion.Invoke(this, new object[] { field.GetValue(x) }));
                }
            }

            for (int i = 0; i < propertyInfo.Length; i++)
            {
                property = propertyInfo.ElementAt(i);
                propertyType = propertyTypes.ElementAt(i);

                if (propertyType == null)
                {
                    property.SetValue(result, null);
                }
                else if (!propertyType.IsPointer && property.CanWrite)
                {
                    property.SetValue(result, property.GetValue(x));
                }
                else if (propertyType.Name.Equals("String") && property.CanWrite)
                {
                    property.SetValue(result, property.GetValue(x));
                }
                else if (property.CanWrite)
                {
                    MethodInfo recursion = _entityCopy.MakeGenericMethod(new Type[] { propertyType });
                    property.SetValue(result, recursion.Invoke(this, new object[] { property.GetValue(x) }));
                }
            }

            return result;
        }

        public void ListElementsCopy<T>(ICollection<T> x, ICollection<T> result)
        {
            int length = (int)x.GetType().GetProperty("Count").GetValue(x);
            Type t = x.ElementAt(0).GetType();

            if (t.IsPointer && t.Name != "String")
            {
                MethodInfo recursion = _entityCopy.MakeGenericMethod(new Type[] { t });
                T current;

                foreach (var listItem in x)
                {
                    current = (T)recursion.Invoke(this, new object[] { listItem });
                    result.Add(current);
                }
            }
            else
            {
                foreach (var listItem in x)
                    result.Add(listItem);
            }
        }
    }
}
