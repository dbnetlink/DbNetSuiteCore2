using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
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

        public static string TransformJson(GridModel gridModel, string json)
        {
            var targetType = PluginHelper.GetTypeFromName(gridModel.JsonTransformPluginName);
            object? obj = System.Text.Json.JsonSerializer.Deserialize(json, targetType!);

            obj = PluginHelper.InvokeMethod(gridModel.JsonTransformPluginName, nameof(IJsonTransformPlugin.Transform), gridModel, obj);

            if (obj == null)
            {
                return json;
            }
            return System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions { });
        }

        public static string GetNameFromType(Type? type)
        {
            return type != null ? $"{type.FullName}, {type.Assembly.FullName}" : string.Empty;
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

        public static object? InvokeMethod(Type type, string methodName, ComponentModel componentModel, object? defaultReturn = null)
        {
            try
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

                return method.Invoke(instance, new object[] { componentModel });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is NotImplementedException)
            {
                return null;
            }
            catch (TargetInvocationException ex)
            {
                componentModel.Message = ex.InnerException?.Message ?? $"Error thrown in {type.FullName} - {methodName}";
                componentModel.MessageType = Enums.MessageType.Error;
            }
            catch (Exception ex)
            {
                componentModel.Message = ex.Message;
                componentModel.MessageType = Enums.MessageType.Error;
            }

            return defaultReturn;
        }
    }
}