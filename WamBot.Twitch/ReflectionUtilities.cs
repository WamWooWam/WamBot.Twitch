using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WamBot.Twitch.Api;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace WamBot.Twitch
{
    class ReflectionUtilities
    {
        private static Dictionary<Type, string> _typeKeywords = new Dictionary<Type, string>()
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(object), "object" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };

        public static string GetUsage(MethodInfo info)
        {
            var param = GetMethodParameters(info);
            var b = new StringBuilder();
            foreach (var p in param)
                AppendParameter(param, b, p);

            return b.ToString();
        }

        public static string GetMethodDeclaration(MethodInfo method)
        {
            var builder = new StringBuilder();

            foreach (var attr in method.DeclaringType.CustomAttributes)
            {
                AppendAttribute(builder, false, attr, attr.AttributeType);
            }

            foreach (var attr in method.CustomAttributes)
            {
                AppendAttribute(builder, false, attr, attr.AttributeType);
            }

            if (method.IsPublic)
                builder.Append("public ");
            else if (method.IsPrivate)
                builder.Append("private ");

            if (method.IsStatic)
                builder.Append("static ");

            if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                builder.Append("async ");

            builder.Append(PrettyTypeName(method.ReturnType));
            builder.Append(" ");

            builder.Append(method.Name);
            builder.Append("(");

            var parameters = method.GetParameters().ToArray();
            foreach (var p in parameters)
            {
                AppendParameter(parameters, builder, p, false);
            }
            builder.Append(");");

            return builder.ToString();
        }

        private static void AppendParameter(ParameterInfo[] param, StringBuilder b, ParameterInfo p, bool usage = true)
        {
            foreach (var a in p.CustomAttributes)
            {
                Type at = a.AttributeType;
                AppendAttribute(b, usage, a, at);
            }

            if (p.IsOptional)
            {
                b.Append("[");
            }

            if (p.IsParams())
            {
                b.Append("params ");
            }

            b.Append($"{PrettyTypeName(p.ParameterType)} {p.Name}");

            if (p.IsOptional)
            {
                b.Append($" = {PrettyValue(p.DefaultValue)}]");
            }

            if (p.Position != (usage ? param.Length : param.Length - 1))
            {
                b.Append(", ");
            }
        }

        private static void AppendAttribute(StringBuilder b, bool usage, CustomAttributeData a, Type at)
        {
            if (!at.Namespace.StartsWith("System.Runtime") && at != typeof(DebuggerStepThroughAttribute) && at != typeof(ParamArrayAttribute))
            {
                b.Append("[");

                string name = at.Name;
                b.Append(name.Substring(0, name.Length - 9));

                if (a.ConstructorArguments.Any() || a.NamedArguments.Any())
                {
                    b.Append("(");

                    var cps = a.Constructor.GetParameters();
                    for (int i = 0; i < cps.Length; i++)
                    {
                        var ap = a.ConstructorArguments.ElementAt(i);
                        var cp = cps.ElementAt(i);

                        if (i != 0)
                        {
                            b.Append(", ");
                        }

                        if (usage)
                            b.Append($"{cp.Name}: {PrettyValue(ap.Value)}");
                        else
                            b.Append(PrettyValue(ap.Value));
                    }

                    for (int i = 0; i < a.NamedArguments.Count; i++)
                    {
                        if (i != 0 || a.ConstructorArguments.Any())
                        {
                            b.Append(", ");
                        }

                        var op = a.NamedArguments.ElementAt(i);
                        b.Append($"{op.MemberName} = {PrettyValue(op.TypedValue.Value)}");
                    }

                    b.Append(")");
                }

                b.Append("] ");

                if (!usage)
                    b.AppendLine();
            }
        }

        private static string PrettyValue(object value)
        {
            StringBuilder b = new StringBuilder();

            if (value != null)
            {
                if (value is Array a)
                {
                    b.Append("new[] { ");
                    for (int i = 0; i < a.Length; i++)
                    {
                        if (i != 0)
                        {
                            b.Append(", ");
                        }

                        var o = a.Cast<object>().ElementAt(i);
                        b.Append(PrettyValue(o));
                    }
                    b.Append(" }");
                }

                if (value is ReadOnlyCollection<CustomAttributeTypedArgument> c)
                {
                    b.Append("new[] { ");
                    for (int i = 0; i < c.Count; i++)
                    {
                        if (i != 0)
                        {
                            b.Append(", ");
                        }

                        var o = c.ElementAt(i);
                        b.Append(PrettyValue(o));
                    }
                    b.Append(" }");
                }

                if (value is string s)
                {
                    b.Append($"\"{s}\"");
                }

                if (value is Enum e)
                {
                    b.Append(e.GetType().Name);
                    b.Append(".");
                    b.Append(value);
                }
            }
            else
            {
                return "null";
            }

            return b.Length > 0 ? b.ToString() : value.ToString();
        }

        internal static string PrettyTypeName(Type t)
        {
            if (_typeKeywords.ContainsKey(t))
            {
                return _typeKeywords[t];
            }

            if (t.IsGenericType)
            {
                if (t.GetGenericTypeDefinition() != typeof(Nullable<>))
                {
                    return string.Format(
                        "{0}<{1}>",
                        t.Name.Substring(0, t.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                        string.Join(", ", t.GetGenericArguments().Select(PrettyTypeName)));
                }
                else
                {
                    return $"{PrettyTypeName(t.GetGenericArguments().First())}?";
                }
            }

            if (t.IsArray)
            {
                return $"{PrettyTypeName(t.GetElementType())}[]";
            }

            return t.Name;
        }

        private static ConcurrentDictionary<MethodInfo, ParameterInfo[]> _parameterCache = new ConcurrentDictionary<MethodInfo, ParameterInfo[]>();

        public static ParameterInfo[] GetMethodParameters(MethodInfo method)
        {
            if (_parameterCache.TryGetValue(method, out var m))
            {
                return m;
            }
            else
            {
                var methods = method.GetParameters().Where(p => p.ParameterType != typeof(CommandContext)).ToArray();
                _parameterCache[method] = methods;
                return methods;
            }
        }
    }
}
