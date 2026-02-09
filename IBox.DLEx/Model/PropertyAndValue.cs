using IBox.Common.Objects;
using System.Reflection;

namespace IBox.DLEx.Model
{
    public class PropertyAndValue
    {
        private Type? type;
        private string? fieldName;
        private PropertyInfo? propertyInfo;
        private object? value;
        private object? obj;

        public Type? Type { get => type; set => type = value; }
        public string? FieldName { get => fieldName; set => fieldName = value; }
        public PropertyInfo? PropertyInfo { get => propertyInfo; set => propertyInfo = value; }
        public object? Value { get => value; set => this.value = value; }
        public object? Obj { get => obj; set => obj = value; }

        internal PropertyAndValue? GetPropertyInfoAndValue(PropertyAndValue propertyAndValue, int index = 0)
        {
            try
            {
                if (propertyAndValue.Type == null)
                {
                    throw new IboxLog("Type is null", "AppLogs");
                }

                PropertyInfo? propertyInfoX;
                propertyInfoX = propertyAndValue.Type.GetProperty(propertyAndValue.FieldName?.Split(".")[index] ?? "");
                if (propertyInfoX == null)
                {
                    return null;
                }

                if (propertyAndValue.FieldName?.Split(".").Length == index + 1)
                {
                    return new PropertyAndValue()
                    {
                        propertyInfo = propertyInfoX,
                        value = propertyInfoX.GetValue(propertyAndValue.Obj),
                    };
                }

                return GetPropertyInfoAndValue(new PropertyAndValue()
                {
                    Type = propertyInfoX.PropertyType,
                    FieldName = propertyAndValue.FieldName,
                    Obj = propertyInfoX.GetValue(propertyAndValue.Obj),
                }, index + 1);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}