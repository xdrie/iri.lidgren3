using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lidgren.Network
{
    public static class BitBufferReflectionExtensions
    {
        // TODO: optimize reflection (generic compiled expressions?)

        public const BindingFlags DefaultBindingFlags =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static Dictionary<Type, MethodInfo> ReadMethods { get; } = new Dictionary<Type, MethodInfo>();
        private static Dictionary<Type, MethodInfo> WriteMethods { get; } = new Dictionary<Type, MethodInfo>();

        static BitBufferReflectionExtensions()
        {
            var inMethods = typeof(NetIncomingMessage).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo method in inMethods)
            {
                if (method.GetParameters().Length == 0 &&
                    method.Name.StartsWith("Read", StringComparison.InvariantCulture) &&
                    method.Name.Substring(4) == method.ReturnType.Name)
                    ReadMethods[method.ReturnType] = method;
            }

            var outMethods = typeof(NetOutgoingMessage).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo method in outMethods)
            {
                if (method.Name.Equals("Write", StringComparison.InvariantCulture))
                {
                    ParameterInfo[] pis = method.GetParameters();
                    if (pis.Length == 1)
                        WriteMethods[pis[0].ParameterType] = method;
                }
            }
        }

        /// <summary>
        /// Reads all fields with the specified binding of the object in alphabetical order using reflection.
        /// </summary>
        public static void ReadAllFields(
            this IBitBuffer buffer, object target, BindingFlags flags = DefaultBindingFlags)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();
            FieldInfo[] fields = type.GetFields(flags);
            NetUtility.SortMembersList(fields);

            foreach (FieldInfo fi in fields)
            {
                // find read method
                if (!ReadMethods.TryGetValue(fi.FieldType, out var readMethod))
                    throw new LidgrenException("Failed to find read method for type " + fi.FieldType);

                // read and set value
                var value = readMethod.Invoke(buffer, null);
                fi.SetValue(target, value);
            }
        }

        /// <summary>
        /// Reads all fields with the specified binding of the object in alphabetical order using reflection.
        /// </summary>
        public static void ReadAllProperties(
            this IBitBuffer buffer, object target, BindingFlags flags = DefaultBindingFlags)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();
            PropertyInfo[] fields = type.GetProperties(flags);
            NetUtility.SortMembersList(fields);
            foreach (PropertyInfo fi in fields)
            {
                // find read method
                if (!ReadMethods.TryGetValue(fi.PropertyType, out var readMethod))
                    throw new LidgrenException("Failed to find read method for type " + fi.PropertyType);

                // read and set value
                var value = readMethod.Invoke(buffer, null);
                fi.SetMethod?.Invoke(target, new[] { value });
            }
        }

        /// <summary>
        /// Writes all fields with specified binding in alphabetical order using reflection.
        /// </summary>
        public static void WriteAllFields(
            this IBitBuffer buffer, object? source, BindingFlags flags = DefaultBindingFlags)
        {
            if (source == null)
                return;

            Type tp = source.GetType();
            FieldInfo[] fields = tp.GetFields(flags);
            NetUtility.SortMembersList(fields);

            foreach (FieldInfo fi in fields)
            {

                // find the appropriate Write method
                if (!WriteMethods.TryGetValue(fi.FieldType, out var writeMethod))
                    throw new LidgrenException("Failed to find write method for type " + fi.FieldType);

                // get and write value
                var value = fi.GetValue(source);
                writeMethod.Invoke(buffer, new[] { value });
            }
        }

        /// <summary>
        /// Writes all properties with specified binding in alphabetical order using reflection.
        /// </summary>
        public static void WriteAllProperties(
            this IBitBuffer buffer, object? source, BindingFlags flags = DefaultBindingFlags)
        {
            if (source == null)
                return;

            Type type = source.GetType();
            PropertyInfo[] fields = type.GetProperties(flags);
            NetUtility.SortMembersList(fields);

            foreach (PropertyInfo fi in fields)
            {
                var getMethod = fi.GetMethod;
                if (getMethod == null)
                    continue;

                // find the appropriate Write method
                if (!WriteMethods.TryGetValue(fi.PropertyType, out var writeMethod))
                    throw new LidgrenException("Failed to find write method for type " + fi.PropertyType);

                // get and write value
                var value = getMethod.Invoke(source, null);
                writeMethod.Invoke(buffer, new[] { value });
            }
        }
    }
}
