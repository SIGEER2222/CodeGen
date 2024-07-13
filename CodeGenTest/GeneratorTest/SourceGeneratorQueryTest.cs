using Buildalyzer;
using Buildalyzer.Workspaces;
using FluentSyntaxRewriter.Test.Help;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorQuery;

namespace FluentSyntaxRewriter.Test;
public class SourceGeneratorQueryTest {
  [Fact]
  public void QueryProperties() {
    var code = @"
namespace TheProject;
public class TestClass {
  public int id;
  public int ID => id;
  public string Name { get; set; } = ""Name"";
}
        ";
    var tree = SyntaxFactory.ParseSyntaxTree(code);
    var sourceFile = new SourceFile(tree, "");
    var classes = sourceFile.GetAllClasses();
    var members = classes.First().GetProperties();
    Assert.Equal(2, members.Count());
    Assert.Single(members.Where(x => x.DefaultValue != null));
  }

  [Fact]
  public void QueryIdentifiers() {
    var code = @"
public partial class Form1 : DevExpress.XtraEditors.XtraForm, IViewBind<Form1ViewModel> {
  public Form1() {
    InitializeComponent();
    InitBinding();
  }
  public void BindText() {
    textEdit1.BindText(ViewModel.Text1);
  }
  public void BindCommand() {
    button1.Bind(ViewModel.SomeTask());
  }
  public void BindGridView() {
    gridControl1
      .BindData(ViewModel.SourceData)
      .BindSelected(ViewModel.SourceDataSelected);
  }
}
";
    var tree = SyntaxFactory.ParseSyntaxTree(code);
    var sourceFile = new SourceFile(tree, "");
    var classes = sourceFile.GetAllClasses();
    var method = classes.First().GetMethods();
    var Modifiers = method.Select(x => x.Identifiers);
    Assert.Equal(3, Modifiers.Count());

    var identifiers = method.Select(x => x.Name);
  }

  [Fact]
  public async Task QueryProjectDocuments() {
    // Given
    AnalyzerManager manager = new();
    var analyzer = manager.GetProject(TestHelps.winformProjectPath);
    AdhocWorkspace workspace = new();
    Project roslynProject = analyzer.AddToWorkspace(workspace);
    var documents = await roslynProject.NewQueryAsync();
    // When
    var vmClass = documents.SelectMany(x => x.GetAllClasses()).Where(x => x.Attributes.Any(x => x.Name.Contains("GenerateViewModel")));
    // Then
    Assert.Equal(3, vmClass.Count());
  }

  [Fact]
  public void QueryNamespaces() {
    var code = SyntaxFactory.ParseSyntaxTree("""
      using ManuTalent.Mom.Semi.FrontEnd.Enums.COM;
      namespace ManuTalent.Mom.Operation.Winform.Host.CommonSourceGenerator;
      [GenerateViewModel(ImplementINotifyPropertyChanging = true, ImplementIDataErrorInfo = true)]
      public partial class ComboxEquipment : CommonFormVM
      {
          [GenerateProperty] string shop;
          [GenerateProperty] string eqpType;
          [GenerateProperty] string eqpName;
          [GenerateProperty] string scheduleType;
      }
      """);
    var namespaces = code.GetRoot().ChildNodes().OfType<FileScopedNamespaceDeclarationSyntax>(); ;
    Assert.Single(namespaces);
  }

  [Fact]
  public void QueryStaticUsings() {
    var syntaxTree = CSharpSyntaxTree.ParseText(
"""
using static ManuTalent.Mom.Semi.FrontEnd.Enums.COM;
using  ManuTalent.Mom.Semi.FrontEnd.Enums.COM;
namespace ManuTalent.Mom.Operation.Winform.Host.CommonSourceGenerator;
[GenerateViewModel(ImplementINotifyPropertyChanging = true, ImplementIDataErrorInfo = true)]
public partial class ComboxEquipment : CommonFormVM<LotInformationDto>
{
}
"""
);

    var sourceFile = new SourceFile(syntaxTree, "");
    var classs = sourceFile.GetAllClasses().FirstOrDefault();
    var baseClass = classs.GetBaseTypes();
    Assert.Single(baseClass);
    var root = syntaxTree.GetRoot();
    var usingStaticDirectives = root.DescendantNodes()
    .OfType<UsingDirectiveSyntax>();
    Assert.Single(usingStaticDirectives);
  }
}
