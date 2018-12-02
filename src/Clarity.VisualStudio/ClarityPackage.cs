using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Clarity.VisualStudio.Generation;
using LiveSharp.VisualStudio.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace Clarity.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(ClarityPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class ClarityPackage : AsyncPackage
    {
        public const string PackageGuidString = "452e4725-2467-4e1a-bf1b-d8a3e7ff7c50";

        private VisualStudioWorkspace _workspace;
        string _cachedResult = "";
        
        public ClarityPackage()
        {}

        #region Package Members
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            
            _workspace = componentModel.GetService<VisualStudioWorkspace>();
            
            var workspaceChangeObservable = Observable.FromEventPattern<EventHandler<WorkspaceChangeEventArgs>, WorkspaceChangeEventArgs>(h => _workspace.WorkspaceChanged += h, h => _workspace.WorkspaceChanged -= h);

            workspaceChangeObservable.Buffer(TimeSpan.FromSeconds(3))
                                     .Where(buf => buf.Count > 0)
                                     .Select(_ => Observable.FromAsync(OnWorkspaceChangedAsync))
                                     .Concat()
                                     .Subscribe(_ => { }, e => Debug.WriteLine(e));
        }

        private async Task OnWorkspaceChangedAsync()
        {
            try {
                var generatedFile = _workspace.CurrentSolution
                                              .Projects
                                              .Select(p => p.Documents.FirstOrDefault(d => string.Equals(Path.GetFileName(d.FilePath), "ammy.g.cs", StringComparison.InvariantCultureIgnoreCase)))
                                              .FirstOrDefault(doc => doc != null);

                if (generatedFile == null)
                    return;
            
                var project = generatedFile.Project;
                var compilation = await project.GetCompilationAsync();
                var ns = compilation.GlobalNamespace;
                var metaGenerator = new MetaGenerator();

                walkMembers(ns, s => ResolveCollisions(s, metaGenerator));
                walkMembers(ns, s => BuildSource(s, metaGenerator));
            
                var result = metaGenerator.Generate();

                if (ResultChanged(_cachedResult, result)) {
                    //var references = project.MetadataReferences;
                    //
                    //if (references.Any()) {
                    //    CompileNewBinary(result, references);
                    //    _cachedResult = result;
                    //}
                    File.WriteAllText(generatedFile.FilePath, result);

                    _cachedResult = result;
                }

                void walkMembers(INamespaceOrTypeSymbol sym, Action<ITypeSymbol> action) {
                    if (sym is ITypeSymbol ts)
                        action(ts);

                    foreach (var s in sym.GetMembers().OfType<INamespaceOrTypeSymbol>())
                        walkMembers(s, action);
                }
            }
            catch (Exception) {
                // TODO: Add logger
            }
        }
        
        private static void CompileNewBinary(string source, IEnumerable<MetadataReference> references)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create(
                "Ammy.g.dll",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var emitResult = compilation.Emit("C:\\Projects\\Ammy.g.dll", "C:\\Projects\\Ammy.g.pdb");
            if (!emitResult.Success)
            {}
        }

        private bool ResultChanged(string cachedResult, string result)
        {
            if (cachedResult.Length != result.Length)
                return true;

            return cachedResult != result;
        }

        private static void AppendTypeConstructors(ITypeSymbol bindableObject, ClarityPageGenerator clarityPageGenerator)
        {
            foreach (var ctor in bindableObject.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor)) {
                if (ctor.DeclaredAccessibility != Accessibility.Public)
                    continue;

                var parameters = ctor.Parameters;
                var parameterTypes = parameters.Select(p => p.Type.GetFullyQualifedName()).ToArray();
                var parameterNames = parameters.Select(p => p.Name).ToArray();

                clarityPageGenerator.AddTypeConstructor(bindableObject.GetFullyQualifedName(), bindableObject.Name, parameterTypes, parameterNames);
            }
        }

        private void ResolveCollisions(ITypeSymbol type, MetaGenerator metaGenerator)
        {
            if (IsSuitableBindableObject(type)) {
                var allMembers = type.GetMembers();

                foreach (var member in allMembers) {
                    if (member is IFieldSymbol fs && IsBindableProperty(fs.Type)) {
                        var clrPropertyName = GetNormalPropertyName(fs);
                        metaGenerator.Extensions.AddName(clrPropertyName);
                    } else if (member is IEventSymbol es) {
                        metaGenerator.Extensions.AddName(es.Name);
                    }
                }
            }
        }

        private void BuildSource(ITypeSymbol type, MetaGenerator metaGenerator)
        {
            if (IsSuitableBindableObject(type)) {
                var allMembers = type.GetAllMembers();
                var bindableProperties = allMembers.OfType<IFieldSymbol>()
                                                   .Where(f => IsBindableProperty(f.Type))
                                                   .Where(f => f.ContainingType == type || metaGenerator.Extensions.HasCollisions(GetNormalPropertyName(f)))
                                                   .ToList();
                var events = allMembers.OfType<IEventSymbol>()
                                       .Where(f => f.ContainingType == type || metaGenerator.Extensions.HasCollisions(f.Name))
                                       .ToArray();

                var typeFullName = type.GetFullyQualifedName();
                var typeExtensionsGenerator = metaGenerator.Extensions.GetTypeExtensionsGenerator(typeFullName, type.Name, type.IsSealed);
                
                GenerateExtensionMethods(type, bindableProperties, events, typeExtensionsGenerator);
                GenerateAttachedPropertyExtension(type, bindableProperties, typeExtensionsGenerator);
                
                AppendTypeConstructors(type, metaGenerator.ClarityPage);
            }
        }
        
        private static void GenerateAttachedPropertyExtension(ITypeSymbol type, List<IFieldSymbol> bindableProperties, TypeExtensionsGenerator typeExtensionsGenerator)
        {
            var methods = type.GetAllMembers().OfType<IMethodSymbol>().ToArray(); 

            foreach (var bindableProperty in bindableProperties) {
                var normalPropertyName = GetNormalPropertyName(bindableProperty);
                var getter = methods.FirstOrDefault(m => m.Name == "Get" + normalPropertyName);
                var setter = methods.FirstOrDefault(m => m.Name == "Set" + normalPropertyName);
                
                if (getter != null && setter != null)
                    typeExtensionsGenerator.AddAttachedPropertyExtensions(normalPropertyName, getter.ReturnType.GetFullyQualifedName(), bindableProperty.Name);
            }
        }

        private static void GenerateExtensionMethods(ITypeSymbol type, List<IFieldSymbol> bindableProperties,
            IEventSymbol[] events,
            TypeExtensionsGenerator typeExtensionsGenerator)
        {
            foreach (var bindableProperty in bindableProperties) {
                GenerateBindablePropertyExtension(type, bindableProperty, typeExtensionsGenerator);
            }

            var eventCache = new HashSet<string>();

            foreach (var evt in events) {
                var eventName = GetEventName(evt);

                if (!eventCache.Contains(eventName) && evt.DeclaredAccessibility == Accessibility.Public) {
                    GenerateEventExtension(evt, eventName, typeExtensionsGenerator);
                    eventCache.Add(eventName);
                }
            }
        }

        private static void GenerateEventExtension(IEventSymbol evt, string eventName, TypeExtensionsGenerator typeExtensionsGenerator)
        {
            var eventType = evt.Type;
            
            var invokeMethod = eventType.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => m.Name == "Invoke");
            if (invokeMethod == null)
                throw new Exception("Invoke method not found on " + eventType.Name);

            var parameterNames = invokeMethod.Parameters.Select(p => p.Type.GetFullyQualifedName());
            var parameters = string.Join(", ", parameterNames.ToArray());

            typeExtensionsGenerator.AddEventExtension(invokeMethod.ReturnType.GetFullyQualifedName(), parameters, eventType.GetFullyQualifedName(), eventName);
        }

        private static string GetEventName(IEventSymbol evt)
        {
            var eventName = evt.Name;

            if (eventName.LastIndexOf(".") is var indexOf && indexOf > -1)
                eventName = eventName.Remove(0, indexOf + 1);

            return eventName;
        }

        private static void GenerateBindablePropertyExtension(ITypeSymbol type, IFieldSymbol bindableProperty, TypeExtensionsGenerator typeExtensionsGenerator)
        {
            var clrPropertyName = GetNormalPropertyName(bindableProperty);
            var clrProperty = type.GetAllMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == clrPropertyName);
            
            if (clrProperty != null) {
                var propertyTypeName = clrProperty.Type.GetFullyQualifedName();

                typeExtensionsGenerator.AddBindablePropertyExtension(propertyTypeName, clrPropertyName, bindableProperty.Name);
            }
        }

        private static string GetNormalPropertyName(IFieldSymbol bindableProperty)
        {
            return bindableProperty.Name.Substring(0, bindableProperty.Name.Length - "Property".Length);
        }

        private bool IsSuitableBindableObject(ITypeSymbol type)
        {
            if (!IsOrHasBaseType(type, "BindableObject", "Xamarin.Forms.BindableObject"))
                return false;
            
            var isPublic = type.DeclaredAccessibility == Accessibility.Public;
            var isGeneric = type is INamedTypeSymbol its && its.TypeArguments.Length > 0;
            
            return isPublic && !isGeneric;
        }
        
        private bool IsBindableProperty(ITypeSymbol type)
        {
            return IsOrHasBaseType(type, "BindableProperty", "Xamarin.Forms.BindableProperty");
        }

        private bool IsOrHasBaseType(ITypeSymbol type, string shortTypeName, string fullTypeName)
        {
            do {
                if (type.Name == shortTypeName) {
                    if (type.GetFullyQualifedName() == fullTypeName)
                        return true;
                }
                    
                type = type.BaseType;
            } while (type != null);

            return false;
        }

        #endregion
    }
}
