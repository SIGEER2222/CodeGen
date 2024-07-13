using Buildalyzer;
using Buildalyzer.Workspaces;
using FluentSyntaxRewriter.Test.Help;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using SourceGeneratorQuery;
using Testura.Code.Generators.Common;
using Testura.Code.Models;

namespace FluentSyntaxRewriter.Test.GeneratorTest;
public class BuildalyzerFileTest {
  private Testura.Code.Builders.ClassBuilder _classBuilder;

  public BuildalyzerFileTest() {
    _classBuilder = new("TestClass", "MyNamespace");
  }

  [Fact]
  public async Task TestName() {
    var code = """
    public partial class Alterationjob
    {
        [Key]
        public string Id { get; set; } = null!;
        public string Id2 { get; set; } = null!;
        public string Id2 { get; set; }
    }
    """;
    var tree = SyntaxFactory.ParseSyntaxTree(code);
    var queryClass = new SourceFile(tree, "");

    var attr = AttributeGenerator.Create(new TesturaAttribute("Requert", new()));

    var modifiedCode = FluentCSharpSyntaxRewriter.Define()
        .WithVisitPropertyDeclaration((_, prop) => {
          if (prop.Initializer is not { Value: ExpressionSyntax }) return prop;
          if (prop.AttributeLists.Any(x => x.Attributes.Any(x => x.Name.ToString() == "Required"))) return prop;
          prop = prop.AddAttributeLists(attr.First());
          return prop;
        })
        .RewriteCSharp(tree.GetRoot());

    await Verifier.Verify(modifiedCode);
  }


  [Theory]
  [InlineData(TestHelps.Efcore)]
  public async Task RenameTest(string projectPath) {
    AnalyzerManager manager = new();
    var analyzer = manager.GetProject(projectPath);
    AdhocWorkspace workspace = new();
    Project roslynProject = analyzer.AddToWorkspace(workspace);
    var solution = roslynProject.Solution;

    var cl = roslynProject.Documents.Skip(2).First();
    var tree = await cl.GetSyntaxTreeAsync();
    var root = await tree.GetRootAsync();
    var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Last();
    var com = await roslynProject.GetCompilationAsync();
    var SemanticModel = com.GetSemanticModel(tree);
    var classSymbol = SemanticModel.GetDeclaredSymbol(classDeclaration);
    var newSln = await RenameSymbols(solution, new List<ReNameSymbol> { new ReNameSymbol(classSymbol, "NewName") });

    var changedDocument = newSln.GetDocument(cl.Id);
    var changedTree = await changedDocument.GetSyntaxTreeAsync();
    var changedRoot = await changedTree.GetRootAsync();
    var changedClassDeclaration = changedRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
    Assert.Equal("NewName", changedClassDeclaration.Identifier.Text);

    var allChangedDocuments = await FindRenamedDocuments(solution, newSln);
    Assert.Contains(cl.Id, allChangedDocuments);
    Assert.Equal(1, allChangedDocuments.Count());
    _ = 1;
  }

  public async Task<IEnumerable<DocumentId>> FindRenamedDocuments(Solution oldSolution, Solution newSolution) {
    var changedDocuments = new List<DocumentId>();

    foreach (var projectId in oldSolution.ProjectIds) {
      var oldProject = oldSolution.GetProject(projectId);
      var newProject = newSolution.GetProject(projectId);

      if (newProject != null) {
        foreach (var documentId in oldProject.DocumentIds) {
          var oldDocument = oldProject.GetDocument(documentId);
          var newDocument = newProject.GetDocument(documentId);

          if (newDocument != null) {
            var oldSyntaxTree = await oldDocument.GetSyntaxTreeAsync();
            var newSyntaxTree = await newDocument.GetSyntaxTreeAsync();
            if (!oldSyntaxTree.IsEquivalentTo(newSyntaxTree)) {
              changedDocuments.Add(documentId);
            }
          }
        }
      }
    }

    return changedDocuments;
  }

  public record ReNameSymbol(ISymbol Symbol, string NewName);

  public async Task<Solution> RenameSymbols(Solution solution, IEnumerable<ReNameSymbol> renames, CancellationToken cancellationToken = default) {
    foreach (var (symbol, newName) in renames) {
      var renameOptions = new SymbolRenameOptions() {
        RenameOverloads = true, // 是否重命名所有重载
        RenameInComments = true, // 是否在注释中重命名
        RenameInStrings = true, // 是否在字符串中重命名
      };
      solution = await Renamer.RenameSymbolAsync(solution, symbol, renameOptions, newName, cancellationToken);
    }

    return solution;
  }

  [Theory]
  [InlineData(TestHelps.Efcore)]
  public async Task Test(string projectPath) {
    AnalyzerManager manager = new();
    var analyzer = manager.GetProject(projectPath);
    AdhocWorkspace workspace = new();
    Project roslynProject = analyzer.AddToWorkspace(workspace);
    var documents = await roslynProject.NewQueryAsync();
    var entities = documents.SelectMany(x => x.GetAllPartialClasses());
    var attr = AttributeGenerator.Create(new TesturaAttribute("Required", new())).First();
    var attrConcurrencyCheck = AttributeGenerator.Create(new TesturaAttribute("ConcurrencyCheck", new())).First();

    var listPath = documents.Select(x => x.FilePath).ToList();
    var listPath2 = documents.Select(x => x.GeneratedPath).ToList();

    foreach (var entity in entities) {
      var root = entity.SourceFile.SyntaxTree.GetRoot();

      root = FluentCSharpSyntaxRewriter.Define()
          .WithVisitPropertyDeclaration((_, prop) => {
            if (prop.AttributeLists.Any(x => x.Attributes.Any(x => x.Name.ToString() == "ConcurrencyCheck"))) return prop;
            if (prop.Identifier.Text.Contains("Concurrencystamp")) return prop.AddAttributeLists(attrConcurrencyCheck);
            return prop;
          })
          .Visit(root);

      root = FluentCSharpSyntaxRewriter.Define()
        .WithVisitPropertyDeclaration((_, prop) => {
          if (prop.Initializer is not { Value: ExpressionSyntax }) return prop;
          if (prop.AttributeLists.Any(x => x.Attributes.Any(x => x.Name.ToString() == "Required"))) return prop;
          prop = prop.AddAttributeLists(attr);
          return prop;
        })
        .Visit(root);

      var code = FluentCSharpSyntaxRewriter.Define().RewriteCSharp(root);

      File.WriteAllText(entity.SourceFile.GeneratedPath, code);
    }

  }

  [Theory]
  [InlineData(TestHelps.winformProjectPath)]
  public async Task GenerateFileTest(string projectPath) {
    AnalyzerManager manager = new();
    var analyzer = manager.GetProject(projectPath);
    AdhocWorkspace workspace = new();
    Project roslynProject = analyzer.AddToWorkspace(workspace);
    var documents = await roslynProject.NewQueryAsync();
    var vmClass = documents.SelectMany(x => x.GetAllPartialClasses()).Where(x => x.GetBaseTypes().Any(x => x.Contains("CommonFormVM")));

    string folderPath = Path.Combine(Path.GetDirectoryName(projectPath), "Generated");
    if (folderPath is not null && Directory.Exists(folderPath))
      Directory.Delete(folderPath, true);

    var classCode = SyntaxFactory.ParseMemberDeclaration("""
/// <summary>
/// </summary>
public partial class __TypeName__ : INotifyPropertyChanged, INotifyPropertyChanging {
	private static readonly Lazy<__TypeName__> _instanceOf = new Lazy<__TypeName__>(
		() => new __TypeName__(),
		LazyThreadSafetyMode.None);
	public static __TypeName__ Instance => _instanceOf.Value;

  public event PropertyChangedEventHandler? PropertyChanged;
  public event PropertyChangingEventHandler? PropertyChanging;
  protected void RaisePropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
  protected void RaisePropertyChanging(PropertyChangingEventArgs e) => PropertyChanging?.Invoke(this, e);

}
""");

    foreach (var vm in vmClass) {

      var path = vm.SourceFile.SyntaxTree.FilePath.Replace("ManuTalent.Mom.Operation.Winform.Host\\", "ManuTalent.Mom.Operation.Winform.Host\\Generated\\").Replace(".cs", ".g.cs");

      var vmNamespace = vm.Namespace;

      var lstCode = new List<string>();

      var motheds = vm.GetMethods().Select(x => x.Name).ToList();

      foreach (var field in vm.GetFields()) {
        var type = field.Type;
        var name = field.Name;
        var newName = RoslynTest.Helps.StringHelp.CapitalizeFirstLetter(name);
        if (newName == name) continue;
        var changedMethod = motheds.Contains($"On{newName}Changed") ? $"On{newName}Changed()" : "";
        var code = $@"
        public {type} {newName} {{
            get => {name};
            set {{
                if(EqualityComparer<{type}>.Default.Equals({name}, value)) return;
                RaisePropertyChanging({name}ChangingEventArgs);
                {name} = value;
                RaisePropertyChanged({name}ChangedEventArgs);
                {changedMethod};
            }}
        }}
        static PropertyChangedEventArgs {name}ChangedEventArgs = new PropertyChangedEventArgs(nameof({newName}));
        static PropertyChangingEventArgs {name}ChangingEventArgs = new PropertyChangingEventArgs(nameof({newName}));
";
        lstCode.Add(code);
      }

      var modifiedClassCode = FluentCSharpSyntaxRewriter
             .Define()
             .WithVisitToken((_, token) => {
               if (token.IsKind(SyntaxKind.IdentifierToken) &&
                         string.Equals(token.ValueText, "__TypeName__", StringComparison.Ordinal))
                 return SyntaxFactory.Identifier(vm.Name).WithTriviaFrom(token);
               return token;
             })
             .Visit(classCode)
             .ParseMemberDeclaration();

      var modifiedClass = FluentCSharpSyntaxRewriter
            .Define()
            .WithVisitClassDeclaration((_, cls) => {
              cls = cls.AddMembers(lstCode.Select(x => SyntaxFactory.ParseMemberDeclaration(x)).ToArray());
              return cls;
            })
            .Visit(modifiedClassCode)
            .ParseMemberDeclaration();



      var namespaceCode = CSharpSyntaxTree.ParseText("""
namespace __ProjectNamespace__ {
}
""").GetRoot();

      var modifiedNamespaceCode = FluentCSharpSyntaxRewriter
            .Define()
            .WithVisitNamespaceDeclaration((_, ns) => {
              ns = ns.AddUsings(vm.SourceFile.Usings).OrderUsings().DistinctUsings().RenameMember(_ => vmNamespace);
              ns = ns.AddMembers(modifiedClass);
              return ns;
            })
            .Visit(namespaceCode)
            .ToFullStringCSharp();

      string directoryPath = Path.GetDirectoryName(path);

      if (!Directory.Exists(directoryPath)) {
        Directory.CreateDirectory(directoryPath);
      }

      File.WriteAllText(path, modifiedNamespaceCode.ToString());
    }
  }
}
