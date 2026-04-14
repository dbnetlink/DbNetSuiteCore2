using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Collections;
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

        public static Type GetTypeFromName(string typeName)
        {
            return String.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
        }

        public static IEnumerable TransformJson(GridModel gridModel, string json)
        {
            var targetType = PluginHelper.GetTypeFromName(gridModel.JsonTransformPluginName);
            object instance = System.Text.Json.JsonSerializer.Deserialize(json, targetType!);

            return (IEnumerable)PluginHelper.InvokeMethod(gridModel.JsonTransformPluginName, nameof(IJsonTransformPlugin.Transform), gridModel, null, instance, instance);
        }

        public static string GetNameFromType(Type type)
        {
            return type != null ? $"{type.FullName}, {type.Assembly.FullName}" : string.Empty;
        }

        public static object InvokeMethod(string typeName, string methodName, ComponentModel componentModel, IEnumerable<object> args = null, object defaultReturn = null, object instance = null) 
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }
            Type type = GetTypeFromName(typeName);
            if (type == null)
            {
                throw new ArgumentException($"Type '{typeName}' could not be found.");
            }
            return InvokeMethod(type, methodName, componentModel, args, defaultReturn, instance);
        }

        public static object InvokeMethod(Type type, string methodName, ComponentModel componentModel, IEnumerable<object> args = null, object defaultReturn = null, object instance = null)
        {
            try
            {
                if (instance == null)
                {
                    instance = Activator.CreateInstance(type);
                }

                if (instance == null)
                {
                    throw new InvalidOperationException($"Unable to create instance of '{type.Name}'.");
                }

                MethodInfo method = type.GetMethod(methodName);

                if (method == null)
                {
                    throw new InvalidOperationException($"Type {type.Name} does not have a public '{methodName}' method.");
                }

                if (args == null)
                {
                    args = Array.Empty<object>();   
                }

                args = (new object[] { componentModel }).Concat(args).ToArray();

                return method.Invoke(instance, args.ToArray());
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