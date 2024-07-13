using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynTest.Buildalyzer;
using SourceGeneratorQuery.Declarations;

namespace FluentSyntaxRewriter.Test.Help;
public class ClassFixture : IDisposable {

  public ClassFixture() {
    TypeDeclarations = QueryAllClassByProjectName(TestHelps.winformProjectPath).ToList();
  }
  public void Dispose() {
  }

  public List<TypeDeclaration> TypeDeclarations { get; private set; }

  CSharpCompilation compilation = null;
  public SemanticModel GetSemanticModelBySyntaxTree(SyntaxTree syntaxTree) => compilation.GetSemanticModel(syntaxTree);
  public CSharpCompilation GetCompilation() => compilation;
  public IEnumerable<TypeDeclaration> QueryAllClassByProjectName(string projectName) {
    var project = BuildalyzerQuery.GetProjectByProjectName(projectName);
    compilation = project.GetCompilationAsync().Result as CSharpCompilation;
    return BuildalyzerQuery.QueryAllPartialClassByProjectName(project).Result;
  }

}
