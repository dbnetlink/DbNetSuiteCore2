using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using System;
using System.Reflection;
using System.Text.Json;

namespace DbNetSuiteCore.Helpers
{

    public static class PluginHelper
    {
        public static bool DoesTypeImplementInterface<TInterface>(string typeName)
        {
            var type = GetTypeFromName(typeName);
            return typeof(TInterface).IsAssignableFrom(type) && !type.IsInterface;
        }

        public static Type? GetTypeFromName(string typeName)
        {
            return String.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
        }

        public static string GetNameFromType(Type? type)
        {
            return type != null ? $"{type.FullName}, {type.Assembly.FullName}" : string.Empty;
        }

        public static string TransformJson(string jsonString, Type targetType, object[]? args = null)
        {
            object instance = JsonSerializer.Deserialize(jsonString, targetType);

            if (instance == null)
            {
                throw new ArgumentException("Failed to deserialize JSON string.");
            }

            MethodInfo transformMethod = targetType.GetMethod(nameof(IJsonTransformPlugin.Transform));

            if (transformMethod == null)
            {
                throw new InvalidOperationException($"Type {targetType.Name} does not have a public 'Transform' method.");
            }

            object transformedData = transformMethod.Invoke(instance, args);

            return JsonSerializer.Serialize(transformedData, new JsonSerializerOptions { });
        }

        public static object? InvokeMethod(string typeName, string methodName, object[]? args = null)
        {
            return InvokeMethod(GetTypeFromName(typeName), methodName, args);
        }

        public static object? InvokeMethod(Type type, string methodName, object[]? args = null)
        {
            object instance = Activator.CreateInstance(type);

            MethodInfo method = type.GetMethod(methodName);

            if (method == null)
            {
                throw new InvalidOperationException($"Type {type.Name} does not have a public '{methodName}' method.");
            }

            return method.Invoke(instance, args);
        }
    }
}