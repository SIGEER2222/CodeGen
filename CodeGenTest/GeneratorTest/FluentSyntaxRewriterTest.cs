using FluentRoslyn.CSharp.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FluentSyntaxRewriter.Test;
public class FluentSyntaxRewriterTest {
  [Fact]
  public async Task ChangeMethodName_AddStatements() {

    var members = CSharpSyntaxTree.ParseText(TestHelpers.code2).GetRoot().DescendantNodes()
    .OfType<MethodDeclarationSyntax>()
    .ToList();

    var parsedCode = CSharpSyntaxTree.ParseText(TestHelpers.PMS).GetRoot();

    var node = parsedCode.DescendantNodes().OfType<ExpressionStatementSyntax>();

    var modifiedCode = FluentCSharpSyntaxRewriter.Define()
        .WithVisitClassDeclaration((_, cls) => {
          return cls.AddMembers(members.ToArray()).RemoveNodes(node, SyntaxRemoveOptions.KeepDirectives);
        })
        .WithVisitMethodDeclaration((_, d) => {
          return d.RenameMember(s => $"Modified_{s}")
                  .AddStatements("return 123;");
        })
        .RewriteCSharp(parsedCode);

    // assert
    await Verify(modifiedCode.ToString());
    Assert.Contains("Modified_Hello", modifiedCode, StringComparison.Ordinal);
    Assert.Contains("return 123;", modifiedCode, StringComparison.Ordinal);
  }

  [Fact]
  public void ChangeIdentifierTokens() {
    var parsedCode = CSharpSyntaxTree.ParseText("""
/// <summary>
/// Indicates that a specific value entry should be deleted from the registry.
/// </summary>
public sealed class __TypeName__ {
	private static readonly Lazy<__TypeName__> _instanceOf = new Lazy<__TypeName__>(
		() => new __TypeName__(),
		LazyThreadSafetyMode.None);

	public static __TypeName__ Instance => _instanceOf.Value;

	internal __TypeName__() :
		base()
	{ }
}
""").GetRoot();

    var modifiedCode = FluentCSharpSyntaxRewriter
        .Define()
        .WithVisitToken((_, token) => {
          if (token.IsKind(SyntaxKind.IdentifierToken) &&
                    string.Equals(token.ValueText, "__TypeName__", StringComparison.Ordinal))
            return SyntaxFactory.Identifier("AType").WithTriviaFrom(token);
          return token;
        })
        .Visit(parsedCode)
        .ToFullStringCSharp();

    Assert.DoesNotContain("__TypeName__", modifiedCode, StringComparison.Ordinal);
  }

  [Fact]
  public async Task CombineCodes_RenameMember_ManipulateUsings() {
    var classCode = SyntaxFactory.ParseMemberDeclaration("""
/// <summary>
/// </summary>
public sealed class __TypeName__ {
	private static readonly Lazy<__TypeName__> _instanceOf = new Lazy<__TypeName__>(
		() => new __TypeName__(),
		LazyThreadSafetyMode.None);

	public static __TypeName__ Instance => _instanceOf.Value;

	internal __TypeName__() :
		base()
	{ }
}
""");

    var modifiedClassCode = FluentCSharpSyntaxRewriter
        .Define()
        .WithVisitToken((_, token) => {
          if (token.IsKind(SyntaxKind.IdentifierToken) &&
                    string.Equals(token.ValueText, "__TypeName__", StringComparison.Ordinal))
            return SyntaxFactory.Identifier("AType").WithTriviaFrom(token);

          return token;
        })
        .Visit(classCode)
        .ParseMemberDeclaration();

    var namespaceCode = CSharpSyntaxTree.ParseText("""
namespace __ProjectNamespace__ {
}
""").GetRoot();

    var modifiedNamespaceCode = FluentCSharpSyntaxRewriter
        .Define()
        .WithVisitNamespaceDeclaration((_, ns) => {
          ns = ns.AddUsings("System", "System.Collections.Generic").OrderUsings().DistinctUsings().RenameMember(_ => "TheProject");
          ns = ns.AddMembers(modifiedClassCode);
          return ns;
        })
        .WithVisitClassDeclaration((_, cls) => {
          return cls.RenameMember(_ => "TheClass");
        })
        .Visit(namespaceCode)
        .ToFullStringCSharp();

    await Verify(modifiedNamespaceCode.ToString());
  }

  [Fact]
  public void AddXmlDocTest() {
    var field = SyntaxFactory.ParseMemberDeclaration(
        """
			    static int z = 0;
			    """);

    var code = field.AddXmlDocumentation(summary: "Test")
        .ToFullStringCSharp();

    Assert.Contains($"/// <summary>Test</summary>{Environment.NewLine}", code, StringComparison.Ordinal);
  }

  [Fact]
  public async Task GetCompilationUnitTest() {
    var s = CSharpSyntaxTree.ParseText(
        """
                namespace A
                {
                    public class B
                    {
                    }
                }
                """);

    Assert.NotNull(s.GetCompilationUnitRoot());
    Assert.NotNull(await s.GetCompilationUnitSyntaxAsync());
    Assert.True(s.TryGetCompilationUnitSyntax(out var syntax));
    Assert.NotNull(syntax);
  }

  [Fact]
  public void ChainedVisitMethods() {
    var typeName = "DeleteValueType";

    var template = SyntaxFactory.ParseMemberDeclaration(
        """
			public sealed class __TypeName__ {
				private static readonly Lazy<__TypeName__> _instanceOf = new Lazy<__TypeName__>(
					() => new __TypeName__(),
					LazyThreadSafetyMode.None);

				public static __TypeName__ Instance => _instanceOf.Value;

				internal __TypeName__() :
					base()
				{ }
			}
			""");

    var code = FluentCSharpSyntaxRewriter
        .Define()
        .WithVisitToken((_, token) => {
          if (token.IsKind(SyntaxKind.IdentifierToken) &&
                    string.Equals(token.ValueText, "__TypeName__", StringComparison.Ordinal))
            return SyntaxFactory.Identifier(typeName).WithTriviaFrom(token);

          return token;
        })
        .WithVisitClassDeclaration((_, token) => {
          token = token.AddXmlDocumentation(
                    summary: "Indicates that a specific value entry should be deleted from the registry.");
          return token;
        })
        .Visit(template)
        .ToFullStringCSharp();

    Assert.Contains("DeleteValueType", code);
    Assert.Contains($"/// <summary>Indicates that a specific value entry should be deleted from the registry.</summary>{Environment.NewLine}", code, StringComparison.Ordinal);
  }

  [Fact]
  public void PlaceholderReplaceTest() {
    bool machineOrUser = true;

    var codeTemplate = (MethodDeclarationSyntax?)SyntaxFactory.ParseMemberDeclaration(
        """
        public IReadOnlyDictionary<string, object> LookupPolicy(RegistryValueOptions registryValueOptions = default, RegistryView registryView = default) {
            /* REPLACE: Update */
            var f = "true";
            /* REPLACE: Update2 */
            return default;
        }
        """);

    codeTemplate = codeTemplate.RenameMember(_ => machineOrUser ? "LookupPolicyForMachine" : "LookupPolicyForUser");

    var result = codeTemplate.ReplacePlaceholders(
        new Dictionary<string, Func<string>>
        {
                    { "Update", () => $"string a = \"{!machineOrUser}\";" },
                    { "Update2", () => $"string b = \"{machineOrUser}\";" },
        })
        .ToFullStringCSharp();

    Assert.Contains("LookupPolicyForMachine", result, StringComparison.Ordinal);
    Assert.Contains($"string a = \"{!machineOrUser}\";", result, StringComparison.Ordinal);
    Assert.Contains($"string b = \"{machineOrUser}\";", result, StringComparison.Ordinal);
  }

  [Fact]
  public async Task ReplaceMVVM() {
    var code = CSharpSyntaxTree.ParseText("""
namespace ManuTalent.Mom.Operation.Winform.Host.Forms.PMS;
[GenerateViewModel(ImplementINotifyPropertyChanging = true, ImplementIDataErrorInfo = true)]
public partial class frmPMSVM : CommonFormVM
{
    [GenerateProperty] BindingList<PMSEqpWorkScheduleDto> _dtScheduleInfo = new();
    [GenerateProperty] PMSEqpWorkScheduleDto _dtScheduleInfo_Selected;
    [GenerateProperty] BindingList<PMSWorkOrderDto> _dtWorkOrderInfo = new();
    [GenerateProperty] ComboxEquipment _comboxEquipment = ComboxEquipment.Copy();
    [GenerateProperty] string eqpName;
    [GenerateProperty] bool hasValue = false;

    [GenerateCommand]
    public async Task LoadAsync()
    {

    }
}
""").GetRoot();
    FluentCSharpSyntaxRewriter.Define()
            .WithVisitMethodDeclaration((_, d) => {
              if (d.GetContainingNamespace() != "MyNamespace")
                return d;
              if (d.GetContainingTypeName() != "MyExtension")
                return d;
              if (d.GetMemberName() != "Hello")
                return d;
              return d
                        .RenameMember(s => $"Modified_{s}")
                        .AddStatements("return 123;");
            })
            .RewriteCSharp(code);
    await Verify(code.ToFullString());

  }
}
