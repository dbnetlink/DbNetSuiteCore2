namespace DbNetSuiteCore.CustomisationHelpers
{
    using System;
    using System.Text.Json;
    using System.Reflection;
    using DbNetSuiteCore.CustomisationHelpers.Interfaces;

    public class JsonTransformer
    {
        public static string TransformJson(string jsonString, Type targetType)
        {
            object originalObject = JsonSerializer.Deserialize(jsonString, targetType);

            if (originalObject == null)
            {
                throw new ArgumentException("Failed to deserialize JSON string.");
            }

            MethodInfo transformMethod = targetType.GetMethod(nameof(IJsonTransform.Transform));

            if (transformMethod == null)
            {
                throw new InvalidOperationException($"Type {targetType.Name} does not have a public 'Transform' method.");
            }

            object transformedData = transformMethod.Invoke(originalObject, null);

            return JsonSerializer.Serialize(transformedData, new JsonSerializerOptions {});
        }
    }
}
