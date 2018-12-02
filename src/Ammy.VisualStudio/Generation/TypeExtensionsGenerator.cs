using System.Collections.Generic;
using System.Text;

namespace Ammy.VisualStudio.Generation
{
    internal class TypeExtensionsGenerator
    {
        public string TypeFullName { get; }
        public string TypeShortName { get; }
        public bool IsSealed { get; }

        public List<(string, string, string)> BindablePropertyExtensions { get; } = new List<(string, string, string)>();
        public List<(string, string, string, string)> EventExtensions { get; } = new List<(string, string, string, string)>();

        private readonly StringBuilder _attachedProperties = new StringBuilder();
        private readonly ExtensionsGenerator _extensionsGenerator;

        public TypeExtensionsGenerator(string typeFullName, string typeShortName, bool isSealed,
            ExtensionsGenerator extensionsGenerator)
        {
            _extensionsGenerator = extensionsGenerator;
            TypeFullName = typeFullName;
            TypeShortName = typeShortName;
            IsSealed = isSealed;
        }

        public void AddBindablePropertyExtension(string propertyTypeName, string clrPropertyName, string bindablePropertyName)
        {
            BindablePropertyExtensions.Add((propertyTypeName, clrPropertyName, bindablePropertyName));
        }

        public void AddEventExtension(string returnType, string parms, string eventType, string eventName)
        {
            EventExtensions.Add((returnType, parms, eventType, eventName));
        }

        public void Generate(StringBuilder sb)
        {
            var tempBuilder = new StringBuilder();

            GenerateBindablePropertyExtensions(tempBuilder);
            GenerateEventExtensions(tempBuilder);
            tempBuilder.AppendLine(_attachedProperties.ToString());

            if (tempBuilder.Length > 0) {
                sb.AppendLine($"  [System.Runtime.CompilerServices.CompilerGenerated]public static partial class {TypeShortName}Extensions {{");
                sb.AppendLine(tempBuilder.ToString());
                sb.AppendLine("  }");
            }
        }

        private void GenerateEventExtensions(StringBuilder sb)
        {
            foreach (var (returnType, parms, eventType, eventName) in EventExtensions) {
                var systemDelegateName = "Action";
                var parameters = parms;

                if (returnType != "System.Void") {
                    parameters += ", " + returnType;
                    systemDelegateName = "Func";
                }

                if (IsSealed || _extensionsGenerator.HasCollisions(eventName)) {
                    var signature0 = $"this {TypeFullName} obj, System.{systemDelegateName}<{parameters}> handler, Disposables disposables";
                    var signature1 = $"this {TypeFullName} obj, System.{systemDelegateName}<{parameters}> handler, AmmyPage page";
                    var signature2 = $"this {TypeFullName} obj, System.{systemDelegateName}<{parameters}> handler";
                
                    sb.AppendLine($"    public static {TypeFullName} {eventName}Event({signature0}) {{ var d = new {eventType}(handler); obj.{eventName} += d; DisposableDummy.Create(() => obj.{eventName} -= d); disposables?.AddTo(disposables); return obj; }}");
                    sb.AppendLine($"    public static {TypeFullName} {eventName}Event({signature1}) => obj.{eventName}Event(handler, page.Disposables);");
                    sb.AppendLine($"    public static {TypeFullName} {eventName}Event({signature2}) => obj.{eventName}Event(handler, (Disposables)null);");
                } else {
                    var signature0 = $"this TElement obj, System.{systemDelegateName}<{parameters}> handler, Disposables disposables";
                    var signature1 = $"this TElement obj, System.{systemDelegateName}<{parameters}> handler, AmmyPage page";
                    var signature2 = $"this TElement obj, System.{systemDelegateName}<{parameters}> handler";
               
                    sb.AppendLine($"    public static TElement {eventName}Event<TElement>({signature0}) where TElement : {TypeFullName} {{ var d = new {eventType}(handler); obj.{eventName} += d; DisposableDummy.Create(() => obj.{eventName} -= d); disposables?.AddTo(disposables); return obj; }}");
                    sb.AppendLine($"    public static TElement {eventName}Event<TElement>({signature1}) where TElement : {TypeFullName} => obj.{eventName}Event(handler, page.Disposables);");
                    sb.AppendLine($"    public static TElement {eventName}Event<TElement>({signature2}) where TElement : {TypeFullName} => obj.{eventName}Event(handler, (Disposables)null);");
                }                
            }
        }

        private void GenerateBindablePropertyExtensions(StringBuilder sb)
        {
            var start = "Helpers.SetPropertyValue(";
            var end = ");";

            foreach (var (propertyTypeName, clrPropertyName, bindablePropertyName) in BindablePropertyExtensions) {
                var baseArgs = $"obj, {TypeFullName}.{bindablePropertyName}, value";

                if (IsSealed || _extensionsGenerator.HasCollisions(clrPropertyName)) {
                    var signature = $"this {TypeFullName} obj, {propertyTypeName} value";
                    var bindableSignature =
                        $"this {TypeFullName} obj, BindableValue<{propertyTypeName}> value, Xamarin.Forms.BindingMode mode = Xamarin.Forms.BindingMode.Default";
                    var bindableWithExprSignature =
                        $"this {TypeFullName} obj, BindableValue<TFrom> value, System.Func<TFrom, {propertyTypeName}> selector";

                    sb.AppendLine(
                        $"    public static {TypeFullName} {clrPropertyName}({signature}) => {start + baseArgs + end}");
                    sb.AppendLine(
                        $"    public static {TypeFullName} {clrPropertyName}({bindableSignature}) => {start + baseArgs}, mode{end}");
                    sb.AppendLine(
                        $"    public static {TypeFullName} {clrPropertyName}<TFrom>({bindableWithExprSignature}) => {start + baseArgs}, selector{end}");

                    if (propertyTypeName == "System.Windows.Input.ICommand") {
                        var commandBody =
                            $"Helpers.SetPropertyValue(obj, {TypeFullName}.{bindablePropertyName}, new Xamarin.Forms.Command(function));";
                        sb.AppendLine(
                            $" public static {TypeFullName} {clrPropertyName}(this {TypeFullName} obj, System.Action function) => {commandBody}");
                    }
                }
                else {
                    var signature = $"this TElement obj, {propertyTypeName} value";
                    var bindableSignature =
                        $"this TElement obj, BindableValue<{propertyTypeName}> value, Xamarin.Forms.BindingMode mode = Xamarin.Forms.BindingMode.Default";
                    var bindableWithExprSignature =
                        $"this TElement obj, BindableValue<TFrom> value, System.Func<TFrom, {propertyTypeName}> selector";

                    sb.AppendLine(
                        $"    public static TElement {clrPropertyName}<TElement>({signature}) where TElement : {TypeFullName} => {start + baseArgs + end}");
                    sb.AppendLine(
                        $"    public static TElement {clrPropertyName}<TElement>({bindableSignature}) where TElement : {TypeFullName} => {start + baseArgs}, mode{end}");
                    sb.AppendLine(
                        $"    public static TElement {clrPropertyName}<TFrom, TElement>({bindableWithExprSignature}) where TElement : {TypeFullName} => {start + baseArgs}, selector{end}");

                    if (propertyTypeName == "System.Windows.Input.ICommand") {
                        var commandBody =
                            $"Helpers.SetPropertyValue(obj, {TypeFullName}.{bindablePropertyName}, new Xamarin.Forms.Command(function));";
                        sb.AppendLine(
                            $" public static TElement {clrPropertyName}<TElement>(this TElement obj, System.Action function) where TElement : {TypeFullName} => {commandBody}");
                    }
                }
            }
        }

        public void AddAttachedPropertyExtensions(string normalPropertyName, string valueTypeName, string bindablePropertyName)
        {
            _attachedProperties.AppendLine($"public static TElement {TypeShortName}_{normalPropertyName}<TElement>(this TElement instance, {valueTypeName} val) where TElement : Xamarin.Forms.BindableObject " +
                          $"=> Helpers.SetAttachedValue(instance, {TypeFullName}.{bindablePropertyName}, val);");
        }
    }
}