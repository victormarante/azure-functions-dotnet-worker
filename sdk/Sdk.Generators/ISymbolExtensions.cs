using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class ISymbolExtensions
    {
        internal static string GetFullName(this ISymbol symbol)
        {
            if (symbol == null || IsRootNamespace(symbol))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(symbol.MetadataName);
            symbol = symbol.ContainingSymbol;

            while (!IsRootNamespace(symbol))
            {
                sb.Insert(0, '.');
                sb.Insert(0, symbol.MetadataName);
                symbol = symbol.ContainingSymbol;
            }

            return sb.ToString();
        }

        private static bool IsRootNamespace(ISymbol symbol)
        {
            if (symbol is INamespaceSymbol namespaceSymbol)
            {
                return namespaceSymbol.IsGlobalNamespace;
            }

            return false;
        }
    }
}
