using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using MongoDB.Bson;
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

        public static string TransformJson(string jsonString, Type targetType, GridModel gridModel)
        {
            object? obj = JsonSerializer.Deserialize(jsonString, targetType);

            if (obj == null)
            {
                throw new ArgumentException("Failed to deserialize JSON string.");
            }

            MethodInfo? transformMethod = targetType.GetMethod(nameof(IJsonTransformPlugin.Transform));

            if (transformMethod == null)
            {
                throw new InvalidOperationException($"Type {targetType.Name} does not have a public 'Transform' method.");
            }

            try
            {
                object? transformedData = transformMethod.Invoke(obj, new object[] { gridModel });
                return JsonSerializer.Serialize(transformedData, new JsonSerializerOptions { });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is NotImplementedException)
            {
                return jsonString;
            }
            /*
            catch (TargetInvocationException ex)
            {
                gridModel.Message = ex.InnerException?.Message ?? $"Error thrown in {targetType.FullName} - {nameof(IJsonTransformPlugin.Transform)}";
                return string.Empty;
            }
            */
        }


        public static object? InvokeMethod(string typeName, string methodName, ComponentModel componentModel, object? defaultReturn = null)
        {
            Type? type = GetTypeFromName(typeName);
            if (type == null)
            {
                throw new ArgumentException($"Type '{typeName}' could not be found.");
            }
            return InvokeMethod(type, methodName, componentModel, defaultReturn);
        }

        public static object? InvokeMethod(Type type, string methodName, ComponentModel componentModel, object? defaultReturn)
        {
            object? instance = Activator.CreateInstance(type);

            if (instance == null)
            {
                throw new InvalidOperationException($"Unable to create instance of '{type.Name}'.");
            }

            MethodInfo? method = type.GetMethod(methodName);

            if (method == null)
            {
                throw new InvalidOperationException($"Type {type.Name} does not have a public '{methodName}' method.");
            }

            try
            {
                return method.Invoke(instance, new object[] { componentModel });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is NotImplementedException)
            {
                return  new NotImplementedException(); ;
            }
            catch (TargetInvocationException ex)
            {
                componentModel.Message = ex.InnerException?.Message ?? $"Error thrown in {type.FullName} - {methodName}";
            }

            return defaultReturn;
        }
    }
}