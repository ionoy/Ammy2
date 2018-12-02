using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Rocks;

//using Microsoft.CodeAnalysis.MSBuild;

namespace Ammy.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                
                Console.WriteLine("args:");
                foreach (var arg in args) {
                    Console.WriteLine(arg);
                }

                var path = args[0];
                var output = args[1];
                
                //var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                //path = Path.Combine(path, @"..\..\..\libs");

                var fullSource = Generate(path);

                File.WriteAllText(output, fullSource);
            } catch (Exception e) {
                Console.WriteLine("Error: " + Environment.NewLine + e);
            }
        }

        private static string Generate(string path)
        {
            var resolver = new DefaultAssemblyResolver();
            var assembliesToReflect = Directory.GetFiles(path)
                                               .Where(f => string.Equals(Path.GetExtension(f), ".dll", StringComparison.InvariantCultureIgnoreCase));


            var allTypes = assembliesToReflect.SelectMany(a => GetTypes(a, resolver))
                                              .OrderBy(GetBaseTypeCount)
                                              .ToArray();

            var bindableTypes = allTypes.Where(t => HasBaseType(t, "Xamarin.Forms.BindableObject") && !t.IsAbstract && t.IsPublic && !t.HasGenericParameters)
                                        .ToArray();

            var generatedProperties = new ConcurrentDictionary<Type, IReadOnlyList<string>>();
            var generatedExtensions = GenerateExtensionMethods(bindableTypes, generatedProperties);

            return GenerateAll(generatedExtensions, bindableTypes);
        }


        private static bool HasBaseType(TypeDefinition type, string baseTypeName)
        {
            return GetBaseTypes(type).Any(bt => string.Equals(bt.FullName, baseTypeName, StringComparison.InvariantCultureIgnoreCase));
        }

        private static string GenerateAll(IEnumerable<string> generatedExtensions, IReadOnlyList<TypeDefinition> bindableObjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine("namespace Ammy {");

            foreach (var generatedExtension in generatedExtensions)
                sb.AppendLine(generatedExtension);

            sb.AppendLine("  public partial class AmmyPage {");

            foreach (var bindableObject in bindableObjects) {
                AppendTypeConstructors(sb, bindableObject);
            }

            sb.AppendLine("  }" + Environment.NewLine);


            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void AppendTypeConstructors(StringBuilder sb, TypeDefinition bindableObject)
        {
            foreach (var ctor in bindableObject.Methods.Where(m => m.IsConstructor)) {
                if (!ctor.IsPublic)
                    continue;

                var parameters = ctor.Parameters;
                if (parameters.Count == 0) {
                    sb.AppendLine($"    public {bindableObject.FullName} {bindableObject.Name} => new {bindableObject.FullName}();");
                } else {
                    var signature = string.Join(", ", parameters.Select(p => GetTypeName(p.ParameterType.Resolve()) + " " + p.Name));
                    var invocation = string.Join(", ", parameters.Select(p => p.Name));
                    sb.AppendLine($"    public {bindableObject.FullName} {bindableObject.Name}With({signature}) => new {bindableObject.FullName}({invocation});");
                }
            }
        }

        private static IEnumerable<string> GenerateExtensionMethods(TypeDefinition[] types, ConcurrentDictionary<Type, IReadOnlyList<string>> generatedProperties)
        {
            var attachedPropertyExtensions = new List<string>();

            foreach (var type in types) {
                var bindableProperties = type.Fields
                                             .Where(f => HasBaseType(f.FieldType.Resolve(), "Xamarin.Forms.BindableProperty"))
                                             .ToList();
                
                yield return GenerateExtensionMethods(type, bindableProperties, type.Events.ToArray());
                attachedPropertyExtensions.Add(GenerateAttachedPropertyExtension(type, bindableProperties));
            }

            yield return "public static partial class BindableObjectExtensions {";
            yield return string.Join(Environment.NewLine, attachedPropertyExtensions.Where(e => !string.IsNullOrWhiteSpace(e)));
            yield return "}";
        }

        private static string GenerateAttachedPropertyExtension(TypeDefinition type, List<FieldDefinition> bindableProperties)
        {
            var sb = new StringBuilder();
            var methods = type.Methods; 

            foreach (var bindableProperty in bindableProperties) {
                var normalPropertyName = GetNormalPropertyName(bindableProperty);
                var getter = methods.FirstOrDefault(m => m.Name == "Get" + normalPropertyName);
                var setter = methods.FirstOrDefault(m => m.Name == "Set" + normalPropertyName);

                if (getter != null && setter != null) {
                    var valueType = getter.ReturnType;
                    sb.AppendLine($"public static TElement {type.Name}_{normalPropertyName}<TElement>(this TElement instance, {valueType.FullName} val) where TElement : Xamarin.Forms.BindableObject " +
                                  $"=> Helpers.SetAttachedValue(instance, {type.FullName}.{bindableProperty.Name}, val);");
                }
            }
            
            return sb.ToString();
        }

        private static string GenerateExtensionMethods(TypeDefinition type, List<FieldDefinition> bindableProperties, EventDefinition[] events)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"  public static partial class {type.Name}Extensions {{");

            foreach (var bindableProperty in bindableProperties)
                GenerateBindablePropertyExtension(type, sb, bindableProperty);

            foreach (var evt in events)
                GenerateEventExtension(type, sb, evt);

            sb.AppendLine("  }");
            
            return sb.ToString();
        }

        private static void GenerateEventExtension(TypeDefinition type, StringBuilder sb, EventDefinition evt)
        {
            var typeName = GetTypeName(type);
            var eventName = evt.Name;
            var eventType = evt.EventType;
            var resolved = eventType.Resolve();
            
            if (eventType is GenericInstanceType generic) {
                var genericInstanceType = resolved.MakeGenericInstanceType(generic.GenericArguments.ToArray());
                resolved = genericInstanceType.Resolve();
            }

            //eventTypeDefinition.MakeGenericInstanceType()
            var invokeMethod = resolved.Methods.FirstOrDefault(m => m.Name == "Invoke");
            if (invokeMethod == null)
                throw new Exception("Invoke method not found on " + eventType.Name);

            var parameterNames = invokeMethod.Parameters.Select(p => GetTypeName(p.ParameterType.Resolve()));
            var parameters = string.Join(", ", parameterNames.ToArray());
            
            var signature0 = $"this {typeName} obj, System.Action<{parameters}> handler, Disposables disposables";
            var signature1 = $"this {typeName} obj, System.Action<{parameters}> handler, AmmyPage page";
            var signature2 = $"this {typeName} obj, System.Action<{parameters}> handler";
            
            var delegateTypeFullName = GetTypeName(evt.EventType.Resolve());

            sb.AppendLine($"    public static {typeName} {eventName}Event({signature0}) {{ var d = new {delegateTypeFullName}(handler); obj.{eventName} += d; DisposableDummy.Create(() => obj.{eventName} -= d); disposables?.AddTo(disposables); return obj; }}");
            sb.AppendLine($"    public static {typeName} {eventName}Event({signature1}) => obj.{eventName}Event(handler, page.Disposables);");
            sb.AppendLine($"    public static {typeName} {eventName}Event({signature2}) => obj.{eventName}Event(handler, (Disposables)null);");
        }
        
        private static void GenerateBindablePropertyExtension(TypeDefinition type, StringBuilder sb, FieldDefinition bindableProperty)
        {
            var clrPropertyName = GetNormalPropertyName(bindableProperty);
            var clrProperty = type.Properties.FirstOrDefault(p => p.Name == clrPropertyName);
            var typeName = GetTypeName(type);

            if (clrProperty != null) {
                var propertyTypeName = GetTypeName(clrProperty.PropertyType.Resolve());
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

        private static string GetNormalPropertyName(FieldDefinition bindableProperty)
        {
            return bindableProperty.Name.Substring(0, bindableProperty.Name.Length - "Property".Length);
        }
        
        private static string GetTypeName(TypeDefinition type)
        {
            if (type.HasGenericParameters) {
                var genericSpecifierIndex = type.FullName.IndexOf('`');
                var fullName = type.FullName.Substring(0, genericSpecifierIndex);
                return $"{fullName}<{string.Join(", ", type.GenericParameters.Select(gp => GetTypeName(gp.Resolve())))}>";
            } else {
                return type.FullName.Replace('+', '.');
            }
        }

        private static IEnumerable<TypeDefinition> GetTypes(string assemblyPath, DefaultAssemblyResolver resolver)
        {
            using (var stream = File.OpenRead(assemblyPath))
            {
                PEReader reader = new PEReader(stream);
                var metadataReader = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(reader);
                var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                var mvid = metadataReader.GetGuid(mvidHandle);
            }

            using (var liveSharpRuntimeModule = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters {AssemblyResolver = resolver})) {
                return liveSharpRuntimeModule.GetTypes();
            }
            //return Assembly.LoadFile(assemblyPath).GetExportedTypes();
        }

        private static IEnumerable<TypeDefinition> GetBaseTypes(TypeDefinition t)
        {
            for (var baseType = t.BaseType?.Resolve(); baseType != null; baseType = baseType.BaseType?.Resolve())
                yield return baseType;
        }

        private static int GetBaseTypeCount(TypeDefinition t) => GetBaseTypes(t).Count();
    }
}
