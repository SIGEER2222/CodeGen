using FluentSyntaxRewriter.Test.Help;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace FluentSyntaxRewriter.Test;
public class VMToBindTest : IClassFixture<ClassFixture> {
  ClassFixture _fixture;
  public VMToBindTest(ClassFixture fixture) {
    _fixture = fixture;
  }

  /// <summary>
  /// 这个是针对形如		fluent.SetBinding(cboEqp, c => c.Text, x => x.EqpName); 这样的语句进行筛选的，关键词是SetBinding
  /// 找到这个语句，然后找到对应的控件，绑定的属性，绑定的ViewModel的属性
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task BindCommond() {
    var classes = _fixture.TypeDeclarations.Where(x => x.Name == "frmPMSMain");
    var methods = classes.SelectMany(x => x.GetMethods()).Where(x => x.Name != "Dispose" && x.Name != "InitializeComponent");
    var argumentLists = methods.SelectMany(x => x.ExpressionStatementSyntaxs);
    var strings = argumentLists.Select(x => x.ToString());
    var bingdings = argumentLists.Where(x => x.ToString().Contains("SetBinding")).ToList();

    var lstBingding = new List<BingdingSyntaxNode>();
    var semanticModel = _fixture.GetSemanticModelBySyntaxTree(classes.First().SyntaxNode.SyntaxTree);

    foreach (var item in bingdings) {
      var argus = item.DescendantNodes().OfType<ArgumentSyntax>();
      var argu = argus.First().ChildNodes().First();
      var symbolInfo = semanticModel.GetSymbolInfo(argu);
      var symbolType = SymbolHelp.GetTypeSymbol(symbolInfo.Symbol);

      var bingding = new BingdingSyntaxNode() {
        Node = item,
        SymbolTypeName = symbolType.Name,
        ControlName = argus.First().ToString(),
        BindName = argus.Skip(1).First().ToString(),
        VMName = argus.Skip(2).First().ToString()
      };

      bingding.BingdingType = symbolType.Name switch {
        var x when x.Contains("CheckedComboBoxEdit") => BingdingType.CheckedComboBoxEdit,
        var x when x.Contains("TextEdit") => BingdingType.TextEdit,
        var x when x.Contains("Button") => BingdingType.Button,
        var x when x.Contains("GridControl") => BingdingType.GridControl,
        _ => BingdingType.Other
      };

      lstBingding.Add(bingding);
    }

    var tree = methods.FirstOrDefault().SyntaxNode.SyntaxTree.GetRoot();

    string code2 = @"
public class Test{
public void BindText() {
  textEdit1.BindText(ViewModel.Text1);
}

public void BinButton() {
  button1.BinButton(ViewModel.SomeTask());
}

public void BindGridView() {
  gridControl1
  .BindData(ViewModel.SourceData)
  .BindSelected(ViewModel.SourceDataSelected)
  ;
}

public void BindComboBox() {
  cboArea.BindComboBox();
}

public void InitSomeBind() {

}
}
";

    var syntaxTree = CSharpSyntaxTree.ParseText(code2);

    var members = syntaxTree.GetRoot().DescendantNodes()
    .OfType<MethodDeclarationSyntax>()
    .ToList();

    var textEdit = lstBingding.Where(x => x.BingdingType == BingdingType.TextEdit).ToList();
    var button = lstBingding.Where(x => x.BingdingType == BingdingType.Button).ToList();
    var gridControl = lstBingding.Where(x => x.BingdingType == BingdingType.GridControl).ToList();
    var comboBox = lstBingding.Where(x => x.BingdingType == BingdingType.CheckedComboBoxEdit).ToList();

    var lstTextEdit = textEdit.Select(x => $"{x.ControlName}.BindText(ViewModel.{x.BindName});")
        .Select(x => SyntaxFactory.ParseStatement(x) as ExpressionStatementSyntax);
    var lstButton = button.Select(x => $"{x.ControlName}.Bind(ViewModel.{x.BindName}());")
        .Select(x => SyntaxFactory.ParseStatement(x) as ExpressionStatementSyntax);
    var lstGridControl = gridControl.Select(x => $"{x.ControlName}.BindData(ViewModel.{x.BindName}).BindSelected(ViewModel.{x.BindName}Selected);")
        .Select(x => SyntaxFactory.ParseStatement(x) as ExpressionStatementSyntax);
    var lstComboBox = comboBox.Select(x => $"{x.ControlName}.Bind(ViewModel.{x.VMName}());")
        .Select(x => SyntaxFactory.ParseStatement(x) as ExpressionStatementSyntax);

    var removeNodes = lstBingding.Where(x => x.BingdingType != BingdingType.Other).Select(x => x.Node);

    var code = FluentCSharpSyntaxRewriter.Define()
              .WithVisitClassDeclaration((_, cls) => {
                var tree = cls.SyntaxTree;
                var removeNodes = cls.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(x => x.Identifier.Text == "InitSomeBind");
                var newTree = tree.WithRootAndOptions(cls.RemoveNodes(removeNodes, SyntaxRemoveOptions.KeepNoTrivia), tree.Options).ToString();
                var otherTree = tree.WithRootAndOptions(cls.RemoveNodes(removeNodes, SyntaxRemoveOptions.KeepNoTrivia), tree.Options).ToString();
                return cls.AddMembers(members.ToArray());
              })
               .WithVisitMethodDeclaration((_, d) => {
                 var methodName = d.GetMemberName();
                 var lstExpressionStatementSyntax = methodName switch {
                   "BindText" => lstTextEdit,
                   "BinButton" => lstButton,
                   "BindGridView" => lstGridControl,
                   "BindComboBox" => lstComboBox,
                   _ => new List<ExpressionStatementSyntax>()
                 };
                 return d.AddBodyStatements(lstExpressionStatementSyntax.ToArray());
               }).Visit(tree);

    var code3 = FluentCSharpSyntaxRewriter.Define()
           .WithVisitClassDeclaration((_, cls) => {
             return cls.RemoveNodes(removeNodes, SyntaxRemoveOptions.KeepDirectives);
           })
          .RewriteCSharp(tree);

    await Verify(code3.ToString());

    Assert.Single(methods);
    Assert.Equal(2, classes.Count());
  }

  /// <summary>
  ///
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task TestEventToCommandBindings() {
    var classes = _fixture.TypeDeclarations.Where(x => x.Name == "frmPMSMain");
    var methods = classes.SelectMany(x => x.GetMethods()).Where(x => x.Name != "Dispose" && x.Name != "InitializeComponent");

    var invocations = methods.SelectMany(x => x.InvocationExpressionSyntaxs);
    var semanticModel = _fixture.GetSemanticModelBySyntaxTree(classes.First().SyntaxNode.SyntaxTree);
    var lstBingding = new List<BingdingSyntaxNode>();

    foreach (var invocation in invocations) {
      if (invocation.Expression is { } expression && (expression.ToString().Contains("WithEvent") && expression.ToString().Contains("EventToCommand"))) {
        var bingding = new BingdingSyntaxNode();
        var arguments = invocation.DescendantNodes().OfType<ArgumentSyntax>();

        foreach (var argu in arguments) {
          if (argu.Expression is IdentifierNameSyntax) {
            var symbolInfo = semanticModel.GetSymbolInfo(argu.Expression);
            var symbolType = SymbolHelp.GetTypeSymbol(symbolInfo.Symbol);
            bingding.SymbolTypeName = symbolType.Name;
            bingding.ControlName = argu.Expression.ToString();
          }
          else if (argu.Expression is MemberAccessExpressionSyntax) {  //btnQuery.Click
            bingding.BindName = argu.Expression.ToString().Split('.').Last();
          }
          else if (argu.Expression is SimpleLambdaExpressionSyntax) {  //x => x.BtnQuery
            bingding.VMName = argu.Expression.ToString().Split('.').Last();
          }
        }

        lstBingding.Add(bingding);
      }
    }
    Assert.Contains(lstBingding, binding => binding.ControlName == "btnQuery" && binding.BindName == "Click" && binding.VMName == "BtnQuery");
  }

  [Fact]
  public async Task Replace() {
  }

}

public class EventToCommandSyntaxNode {
  public string ControlName { get; set; }
  public string EventName { get; set; }
  public string CommandName { get; set; }
  public string ControlType { get; set; }
  public string CommandType { get; set; }
}

public class BingdingSyntaxNode {
  public SyntaxNode Node { get; set; }
  public string SymbolTypeName { get; set; }
  public BingdingType BingdingType { get; set; }
  public string ControlName { get; set; }
  public string BindName { get; set; }
  public string VMName { get; set; }
}

public class BingdingClass {
  public List<BingdingSyntaxNode> Nodes { get; set; }
  public string VMName { get; set; }
  public string VName { get; set; }
}

public enum BingdingType {
  Other,
  CheckedComboBoxEdit,
  TextEdit,
  Button,
  GridControl,
}
