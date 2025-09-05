namespace DbNetSuiteCore.Helpers
{
    using System;
    using System.Text.Json;
    using System.Reflection;

    public class ReflectionHelper
    {
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
