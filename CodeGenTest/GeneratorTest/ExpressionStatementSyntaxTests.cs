using FluentRoslyn.CSharp.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FluentSyntaxRewriter.Test.GeneratorTest;
public class ExpressionStatementSyntaxTests {
  public ExpressionStatementSyntaxTests() {
    VerifySettings settings = new VerifySettings();
  }
  [Fact]
  public void TestExpressionStatementSyntaxCreationFromString() {
    string expectedCode = "textEdit1.BindText(ViewModel.Text1);";
    var expressionStatement = SyntaxFactory.ParseStatement(expectedCode) as ExpressionStatementSyntax;
    Assert.NotNull(expressionStatement);
    Assert.Equal(expectedCode, expressionStatement.ToFullString());
  }

  [Fact]
  public async Task Test() {
    bool machineOrUser = true;

    var codeTemplate = (MethodDeclarationSyntax?)SyntaxFactory.ParseMemberDeclaration(
        """
    public IReadOnlyDictionary<string, object> LookupPolicy(RegistryValueOptions registryValueOptions = default, RegistryView registryView = default) {
        /* REPLACE2: Update */
        var f = "true";
        /* REPLACE2: Update2 */
        return default;
    }
    """);

    codeTemplate = codeTemplate.RenameMember(_ => machineOrUser ? "LookupPolicyForMachine" : "LookupPolicyForUser");

    var result = codeTemplate.ReplacePlaceholders(
        new Dictionary<string, Func<string>>
        {
        { "Update", () => $"string a = \"{!machineOrUser}\";" },
        { "Update2", () => $"string b = \"{machineOrUser}\";" },
        }, "REPLACE2:")
        .ToFullStringCSharp();
    await Verify(result.ToString());
  }

  [Fact]
  public async Task RemoveNodesFromClass() {
    SyntaxTree tree = CSharpSyntaxTree.ParseText(TestHelpers.PMS);
    var root = tree.GetRoot();

    ClassDeclarationSyntax classDeclaration = root.DescendantNodes()
      .OfType<ClassDeclarationSyntax>()
      .FirstOrDefault();

    var members = CSharpSyntaxTree.ParseText(TestHelpers.code2).GetRoot().DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .ToList();

    Assert.NotNull(classDeclaration);

    var nodesToRemove = classDeclaration.DescendantNodes().OfType<ExpressionStatementSyntax>();

    var modifiedCode = FluentCSharpSyntaxRewriter.Define()
       .WithVisitClassDeclaration((_, cls) => {
         return cls.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepDirectives);
       })
        .WithVisitMethodDeclaration((_, d) => {
          return d.RenameMember(s => $"Modified_{s}")

                  .AddStatements("return 123;");
        })
       .Visit(root);
    var modifiedCode2 = FluentCSharpSyntaxRewriter.Define()
        .WithVisitClassDeclaration((_, cls) => {
          return cls.AddMembers(members.ToArray());
        })
        .RewriteCSharp(modifiedCode);

    await Verify(modifiedCode2.ToString());
  }
}
