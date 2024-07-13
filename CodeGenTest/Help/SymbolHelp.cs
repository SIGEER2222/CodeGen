using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentSyntaxRewriter.Test.Help;
internal static class SymbolHelp {
  public static ITypeSymbol GetTypeSymbol(ISymbol symbol) => symbol switch {
    ILocalSymbol localSymbol => localSymbol.Type,
    IParameterSymbol parameterSymbol => parameterSymbol.Type,
    IFieldSymbol fieldSymbol => fieldSymbol.Type,
    IPropertySymbol propertySymbol => propertySymbol.ContainingType,
    null => throw new ArgumentNullException(nameof(symbol)),
    _ => symbol.ContainingType
  };
}
