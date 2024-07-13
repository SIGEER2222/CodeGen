
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynTest.SourceGeneratorQuery.Declarations;
public class RemoveSemicolonRewriter : CSharpSyntaxRewriter {
  public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
    var newNode = node.WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken));
    return base.VisitPropertyDeclaration(newNode);
  }
}
