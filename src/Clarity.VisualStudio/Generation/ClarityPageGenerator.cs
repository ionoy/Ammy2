using System;
using System.Linq;
using System.Text;

namespace Clarity.VisualStudio.Generation
{
    internal class ClarityPageGenerator
    {
        readonly StringBuilder _internal = new StringBuilder();

        public void AddTypeConstructor(string typeFullName, string typeShortName, string[] parameterTypes, string[] parameterNames)
        {
            if (parameterTypes.Length == 0) {
                _internal.AppendLine($"    public {typeFullName} {typeShortName} => new {typeFullName}();");
            } else {
                var signature = string.Join(", ", parameterTypes.Zip(parameterNames, (type, name) => new { type, name })
                    .Select(t => t.type + " " + t.name));
                var invocation = string.Join(", ", parameterNames);

                _internal.AppendLine($"    public {typeFullName} {typeShortName}With({signature}) => new {typeFullName}({invocation});");
            }
        }

        public void Generate(StringBuilder sb)
        {
            sb.AppendLine("  [System.Runtime.CompilerServices.CompilerGenerated]public partial class ClarityPage {");

            sb.AppendLine(_internal.ToString());

            sb.AppendLine("  }" + Environment.NewLine);
        }
    }
}