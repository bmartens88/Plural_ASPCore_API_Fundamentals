using System.Dynamic;
using System.Reflection;

namespace CourseLibrary.API.Helpers;

public static class ObjectExtensions
{
    public static ExpandoObject ShapeData<TSource>(this TSource source,
        string fields)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        var dataShapedObject = new ExpandoObject();

        if (string.IsNullOrWhiteSpace(fields))
        {
            // All public properties should be in the ExpandoObject
            var propertyInfos = typeof(TSource)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propertyInfo in propertyInfos)
            {
                // Get the value of the property on the source object
                var propertyValue = propertyInfo.GetValue(source);

                // Add the field in the ExpandoObject
                ((IDictionary<string, object>) dataShapedObject)
                    .Add(propertyInfo.Name, propertyValue);
            }

            return dataShapedObject;
        }

        // The fields are separated by ",", so we split it
        var fieldsAfterSplit = fields.Split(',');

        foreach (var field in fieldsAfterSplit)
        {
            // Trim each field, as it might contain leading
            // or trailing spaces. Can't trim the var in foreach
            // so use another var
            var propertyName = field.Trim();

            // Use reflection to get the property on the source object
            // we need to include public and instance, b/c specifying a
            // binding flag overwrites the already-existing binding flag
            var propertyInfo = typeof(TSource)
                .GetProperty(propertyName,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo is null)
                throw new Exception($"Property {propertyName} wasn't found on {typeof(TSource)}");

            // Get the value of the property on the source object
            var propertyValue = propertyInfo.GetValue(source);

            // Add the field to the ExpandoObject
            ((IDictionary<string, object>) dataShapedObject)
                .Add(propertyInfo.Name, propertyValue);
        }

        return dataShapedObject;
    }
}