using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentSyntaxRewriter.Test.Help;
public class InvocationCollector : CSharpSyntaxWalker {
  public List<InvocationExpressionSyntax> Invocations { get; } = new List<InvocationExpressionSyntax>();

  public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
    Invocations.Add(node);
    base.VisitInvocationExpression(node); 
  }
}
