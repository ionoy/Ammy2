using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.LanguageServices;

namespace LiveSharp.VisualStudio.Infrastructure
{
    public static class RoslynExtensions
    {
        public static string GetFullyQualifedName(this ITypeSymbol ts)
        {
            if (ts is IArrayTypeSymbol ats)
                return GetFullyQualifedName(ats.ElementType) + "[]";

            return GetFullMetadataName(ts);
        }

        private static string GetFullMetadataName(INamespaceOrTypeSymbol s)
        {
            if (s == null || IsRootNamespace(s))
                return string.Empty;

            var typeName = s.MetadataName;
            var last = s;

            if (s is INamedTypeSymbol its && its.TypeArguments.Length > 0) {
                var args = string.Join(",", its.TypeArguments.Select(t => t.GetFullyQualifedName()));
                if (typeName.IndexOf("`") is var delimiterIndex)
                    typeName = typeName.Remove(delimiterIndex);
                typeName += "<" + args + ">";
            }

            var sb = new StringBuilder(typeName);
            var nsOrType = s.ContainingSymbol;

            while (!(nsOrType is INamespaceOrTypeSymbol) && nsOrType != null)
                nsOrType = nsOrType.ContainingSymbol;

            s = nsOrType as INamespaceOrTypeSymbol;

            while (!IsRootNamespace(s)) {
                if (s is ITypeSymbol && last is ITypeSymbol) sb.Insert(0, '.');
                else sb.Insert(0, '.');

                sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                s = (INamespaceOrTypeSymbol)s.ContainingSymbol;
            }

            return sb.ToString();
        }

        private static bool IsRootNamespace(ISymbol symbol)
        {
            INamespaceSymbol s = null;
            return ((s = symbol as INamespaceSymbol) != null) && s.IsGlobalNamespace;
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol ts)
        {
            var type = ts;

            while ((type = type.BaseType) != null)
                yield return type;
        }

        public static IReadOnlyList<ISymbol> GetAllMembers(this ITypeSymbol type)
        {
            return new[] { type }.Concat(type.GetBaseTypes())
                                 .SelectMany(t => t.GetMembers())
                                 .ToArray();
        }
    }
}
