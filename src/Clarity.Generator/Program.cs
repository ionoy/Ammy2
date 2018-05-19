using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
//using Microsoft.CodeAnalysis.MSBuild;

namespace Clarity.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                var fullSource = Generate();
                File.WriteAllText(@"Clarity.g.cs", fullSource);
            } catch (Exception e) {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static string Generate()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, @"..\..\..\libs");
            var assembliesToReflect = Directory.GetFiles(path)
                                               .Where(f => Path.GetFileName(f).StartsWith("Xamarin.") && 
                                                           string.Equals(Path.GetExtension(f), ".dll", StringComparison.InvariantCultureIgnoreCase));

            var allTypes = assembliesToReflect.SelectMany(GetTypes)
                                              .OrderBy(GetBaseTypeCount)
                                              .ToArray();
            var bindableObjectType = allTypes.FirstOrDefault(t => t.FullName == "Xamarin.Forms.BindableObject");

            if (bindableObjectType == null)
                throw new Exception("Couldn't find BindableObject type");

            var bindablePropertyType = allTypes.FirstOrDefault(t => t.FullName == "Xamarin.Forms.BindableProperty");

            if (bindablePropertyType == null)
                throw new Exception("Couldn't find BindableProperty type");
        
            var bindableTypes = allTypes.Where(t => bindableObjectType.IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic && !t.IsGenericType)
                                        .ToArray();
            var generatedProperties = new ConcurrentDictionary<Type, IReadOnlyList<string>>();

            var generatedExtensions = GenerateExtensionMethods(bindableTypes, bindablePropertyType, generatedProperties);
            return GenerateAll(generatedExtensions, bindableTypes);
        }
        
        private static string GenerateAll(IEnumerable<string> generatedExtensions, IReadOnlyList<Type> bindableObjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine("namespace Clarity {");

            foreach (var generatedExtension in generatedExtensions)
                sb.AppendLine(generatedExtension);

            sb.AppendLine("  public partial class ClarityBase {");

            foreach (var bindableObject in bindableObjects) {
                AppendTypeConstructors(sb, bindableObject);
            }

            sb.AppendLine("  }" + Environment.NewLine);


            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void AppendTypeConstructors(StringBuilder sb, Type bindableObject)
        {
            foreach (var ctor in bindableObject.GetConstructors()) {
                if (!ctor.IsPublic)
                    continue;

                var parameters = ctor.GetParameters();
                if (parameters.Length == 0) {
                    sb.AppendLine($"    public {bindableObject.FullName} {bindableObject.Name} => new {bindableObject.FullName}();");
                } else {
                    var signature = string.Join(", ", parameters.Select(p => GetTypeName(p.ParameterType) + " " + p.Name));
                    var invocation = string.Join(", ", parameters.Select(p => p.Name));
                    sb.AppendLine($"    public {bindableObject.FullName} {bindableObject.Name}With({signature}) => new {bindableObject.FullName}({invocation});");
                }
            }
        }

        private static IEnumerable<string> GenerateExtensionMethods(Type[] types, Type bindablePropertyType, ConcurrentDictionary<Type, IReadOnlyList<string>> generatedProperties)
        {
            foreach (var type in types)
                yield return GenerateExtensionMethods(type, bindablePropertyType, generatedProperties);
        }

        private static string GenerateExtensionMethods(Type type, Type bindablePropertyType, ConcurrentDictionary<Type, IReadOnlyList<string>> generatedProperties)
        {
            var sb = new StringBuilder();
            var baseTypes = GetBaseTypes(type);
            var bindableProperties = type.GetFields(BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public)
                                         .Where(f => f.FieldType == bindablePropertyType)
                                         //.Where(f => !IsGeneratedAlready(generatedProperties, f.Name, baseTypes))
                                         .ToList();

            sb.AppendLine($"  public static partial class {type.Name}Extensions {{");

            foreach (var bindableProperty in bindableProperties) {
                var clrPropertyName = bindableProperty.Name.Substring(0, bindableProperty.Name.Length - "Property".Length);
                var clrProperty = type.GetProperty(clrPropertyName);
                var typeName = GetTypeName(type);

                if (clrProperty != null) {
                    var propertyTypeName = GetTypeName(clrProperty.PropertyType);
                    var signature = $"this {typeName} obj, {propertyTypeName} value";
                    var bindableSignature = $"this {typeName} obj, BindableValue<{propertyTypeName}> value, Xamarin.Forms.BindingMode mode = Xamarin.Forms.BindingMode.Default";
                    var bindableWithExprSignature = $"this {typeName} obj, BindableValue<TFrom> value, System.Func<TFrom, {propertyTypeName}> selector";
                    var baseArgs = $"obj, {type.FullName}.{bindableProperty.Name}, value";
                    var start = "Helpers.SetPropertyValue(";
                    var end = ");";

                    sb.AppendLine($"    public static {typeName} {clrPropertyName}({signature}) => {start + baseArgs + end}");
                    sb.AppendLine($"    public static {typeName} {clrPropertyName}({bindableSignature}) => {start + baseArgs}, mode{end}");
                    sb.AppendLine($"    public static {typeName} {clrPropertyName}<TFrom>({bindableWithExprSignature}) => {start + baseArgs}, selector{end}");

                    if (propertyTypeName == "System.Windows.Input.ICommand") {
                        var commandBody = $"Helpers.SetPropertyValue(obj, {type.FullName}.{bindableProperty.Name}, new Xamarin.Forms.Command(function));";
                        sb.AppendLine($" public static {typeName} {clrPropertyName}(this {typeName} obj, System.Action function) => {commandBody}");
                    }
                }
            }

            sb.AppendLine("  }");

            //generatedProperties[type] = bindableProperties.Select(fi => fi.Name).ToArray();

            return sb.ToString();
        }

        private static bool IsGeneratedAlready(ConcurrentDictionary<Type, IReadOnlyList<string>> generatedProperties, string propertyName, IEnumerable<Type> baseTypes)
        {
            foreach (var bt in baseTypes)
                if (generatedProperties.TryGetValue(bt, out var propertyList) && propertyList.Contains(propertyName))
                    return true;
            
            return false;
        }

        private static string GetTypeName(Type type)
        {
            if (type.IsGenericType) {
                var genericSpecifierIndex = type.FullName.IndexOf('`');
                var fullName = type.FullName.Substring(0, genericSpecifierIndex);
                return $"{fullName}<{string.Join(", ", type.GetGenericArguments().Select(GetTypeName))}>";
            } else {
                return type.FullName.Replace('+', '.');
            }
        }

        private static IEnumerable<Type> GetTypes(string assemblyPath)
        {
            return Assembly.LoadFile(assemblyPath).GetExportedTypes();
        }

        private static IEnumerable<Type> GetBaseTypes(Type t)
        {
            for (var baseType = t.BaseType; baseType != null; baseType = baseType.BaseType)
                yield return baseType;
        }

        private static int GetBaseTypeCount(Type t) => GetBaseTypes(t).Count();
    }
}
