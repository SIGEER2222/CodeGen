using SourceGeneratorQuery.Declarations;

namespace RoslynTest.Buildalyzer;
public static class BuildalyzerQuery {
  public static Project GetProjectByProjectName(string projectName) {
    AnalyzerManager manager = new();
    var analyzer = manager.GetProject(projectName);
    AdhocWorkspace workspace = new();
    Project roslynProject = analyzer.AddToWorkspace(workspace);
    return roslynProject;
  }

  public static async Task<IEnumerable<TypeDeclaration>> QueryAllPartialClassByProjectName(Project roslynProject) {
    var documents = await roslynProject.NewQueryAsync();
    var vmClass = documents.SelectMany(x => x.GetAllPartialClasses());
    return vmClass;
  }

  public static async Task<IEnumerable<TypeDeclaration>> QueryAlllassByProjectName(Project roslynProject) {
    var documents = await roslynProject.NewQueryAsync();
    var vmClass = documents.SelectMany(x => x.GetAllClasses());
    return vmClass;
  }
}
