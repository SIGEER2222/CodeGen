using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace RoslynTest.FluentSyntaxRewriter;

public class AlignEqualsRewriter : CSharpSyntaxRewriter {
  public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) {

    var variableNameLengths = node.Declaration.Variables
    .Select(variable => variable.Identifier.Text.Length)
    .ToList();

    var maxVariableNameLength = variableNameLengths.Any() ? variableNameLengths.Max() : 0;

    // 对齐等号
    var alignedVariables = node.Declaration.Variables
    .Select(variable => {
      var variableNameLength = variable.Identifier.Text.Length;
      var paddingSize = maxVariableNameLength - variableNameLength;
      var padding = new string(' ', paddingSize);
      var initializer = variable.Initializer;
      if (initializer != null) {
        var newInitializer = initializer.WithLeadingTrivia(SyntaxFactory.Whitespace(padding));
        return variable.WithInitializer(newInitializer);
      }
      return variable;
    });

    var alignedDeclaration = node.Declaration.WithVariables(SyntaxFactory.SeparatedList(alignedVariables));
    return node.WithDeclaration(alignedDeclaration);
  }
}


public static class CSharpFormateOptions {
  public static OptionSet Option(OptionSet Options) {
    var formatOptionSet = Options
    .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, false)
    .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, 2)
    .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\n")
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, false)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, false)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, false)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAccessors, false)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, false)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, false)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, false)
    .WithChangedOption(CSharpFormattingOptions.NewLineForElse, true)
    .WithChangedOption(CSharpFormattingOptions.NewLineForCatch, true)
    .WithChangedOption(CSharpFormattingOptions.NewLineForFinally, true)
    .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true)
    .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, true);
    return formatOptionSet;
  }
}
