using SourceGeneratorQuery.Declarations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynTest.SourceGeneratorQuery.Declarations; 
public class ExpressionStatementDeclaration {
  MethodDeclaration _method;
  public ExpressionStatementDeclaration(MethodDeclaration method) {
    _method = method;
  }
}
