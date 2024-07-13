using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace FluentSyntaxRewriter.Test.GeneratorTest.CodeFormat;
public class AssignmentAlignmentTests {
  [Fact]
  public async Task TestAlignAssignmentStatements() {
    var sourceCode = @"
public class TestClass
{
public void TestMethod()
{
int xsasasd = 1;
int yasdad = 10;
int z = 100;
}
}
";
    var newCode = FluentCSharpSyntaxRewriter.Define()
       .RewriteCSharp(SyntaxFactory.ParseSyntaxTree(sourceCode).GetRoot());

    await Verifier.Verify(newCode);
  }
}
