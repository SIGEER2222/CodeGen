using FluentSyntaxRewriter.Test.Help;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynTest.Buildalyzer;

namespace FluentSyntaxRewriter.Test.GeneratorTest;
public class CopyTest {

  [Theory]
  [InlineData(TestHelps.BusinessRule)]
  [InlineData(TestHelps.Facade)]
  public async Task CopyMethodTest(string projectName) {
    var project = BuildalyzerQuery.GetProjectByProjectName(projectName);
    var compilation = (await project.GetCompilationAsync()) as CSharpCompilation;
    var allClass = await BuildalyzerQuery.QueryAlllassByProjectName(project);
    var namespaceName = project.Name + ".Generated";

    var copyToClasses = allClass.Where(x => x.GetMethods()
      .Any(x => x.ExpressionStatementSyntaxs
        .Any(x => x.ToString()
          .Contains(".CopyTo("))));

    List<CopyDto> keyValuePairs = new();

    foreach (var cl in copyToClasses) {
      var semanticModel = compilation.GetSemanticModel(cl.SyntaxNode.SyntaxTree);
      var Invocations = cl.SyntaxNode.DescendantNodes().OfType<InvocationExpressionSyntax>()
        .Where(x => x.ToString().Contains(".CopyTo(")).ToList();
      var arguments = Invocations.SelectMany(x => x.DescendantNodes().OfType<IdentifierNameSyntax>()).ToList();

      var listType = arguments.Select(x => semanticModel.GetTypeInfo(x).Type).ToList();

      var method = string.Empty;

      if (listType.Count % 3 == 0) {
        var groupCount = listType.Count / 3;
        for (int i = 0; i < groupCount; i++) {
          var source = listType[i * 3];
          var target = listType[i * 3 + 2];
          var prop1 = RoslynHelpers.GetProperties(source).ToList();
          var prop2 = RoslynHelpers.GetProperties(target).ToList();

          var prop3 = prop2.Where(x => prop1.Any(y => y.Name == x.Name)).ToList();

          var assignments = prop3.Select(x => $"target.{x.Name} = source.{x.Name};{Environment.NewLine}").Aggregate((x, y) => x + y);

          var value = new CopyDto() {
            SourceName = source.ToString(),
            TargetName = target.ToString()
          };
          if (!keyValuePairs.Any(x => x.SourceName == value.SourceName && x.TargetName == value.TargetName)) {
            keyValuePairs.Add(value);
          }
          else {
            continue;
          }

          method += $@"
  /// <summary>
  /// Generated
  /// </summary>
  public static {target.ToString()} CopyTo (this {source.ToString()} source, {target.ToString()} target) {{
      {assignments}
      return target;
    }}{Environment.NewLine}";
        }


        var code = @$"
  /// <summary>
  /// Generated
  /// </summary>
  namespace {namespaceName};

  public static partial class  CopeTo   {{
    {method}
  }}
  ";
        var newCode = FluentCSharpSyntaxRewriter.Define()
           .RewriteCSharp(SyntaxFactory.ParseSyntaxTree(code).GetRoot());

        var path = cl.SourceFile.GeneratedPath.Replace($"{project.Name}\\", $"{project.Name}\\Generated\\").Replace(".cs", ".g.cs");

        string directoryPath = Path.GetDirectoryName(path);

        if (!Directory.Exists(directoryPath)) {
          Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(path, newCode);
        // var listType2 = arguments.Select(x => {
        //   var symbolInfo = semanticModel.GetSymbolInfo(x);
        //   if (symbolInfo.Symbol is null) {
        //     return null;
        //   }
        //   var symbolType = SymbolHelp.GetTypeSymbol(symbolInfo.Symbol);
        //   return symbolType;
        // }).ToList();
      }
    }
  }
}

public class CopyDto {
  public string SourceName { get; set; }
  public string TargetName { get; set; }
}



public static class RoslynHelpers {
  public static IEnumerable<IPropertySymbol> GetProperties(ITypeSymbol typeSymbol) {
    if (typeSymbol == null) {
      throw new ArgumentNullException(nameof(typeSymbol));
    }

    if (typeSymbol.TypeKind != TypeKind.Class) {
      throw new ArgumentException("The provided symbol is not a class.", nameof(typeSymbol));
    }

    var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
    if (namedTypeSymbol == null) {
      throw new InvalidOperationException("The provided symbol is not a named type symbol.");
    }

    var properties = new List<IPropertySymbol>();
    CollectProperties(namedTypeSymbol, properties);
    return properties;
  }

  private static void CollectProperties(INamedTypeSymbol typeSymbol, List<IPropertySymbol> properties) {
    if (typeSymbol == null) {
      return;
    }
    properties.AddRange(typeSymbol.GetMembers().OfType<IPropertySymbol>());
    CollectProperties(typeSymbol.BaseType as INamedTypeSymbol, properties);
  }
}
