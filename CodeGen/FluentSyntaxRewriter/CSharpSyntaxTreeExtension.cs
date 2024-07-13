using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using RoslynTest.FluentSyntaxRewriter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSyntaxRewriter {
  /// <summary>
  /// Extension methods for <see cref="SyntaxNode"/> and <see cref="SyntaxTree"/> related to C# syntax.
  /// </summary>
  public static class CSharpSyntaxTreeExtension {

    public static BlockSyntax AddStatements(this BlockSyntax block, params string[] statements) {
      if (statements == null || statements.Length < 1)
        return block;

      foreach (var eachStatement in statements) {
        var parsedStatement = SyntaxFactory.ParseStatement(string.Concat(eachStatement, Environment.NewLine));
        block = block.WithStatements(block.Statements.Add(parsedStatement));
      }

      return block;
    }


    public static MethodDeclarationSyntax AddStatements(this MethodDeclarationSyntax methodNode, params string[] statements) {
      var body = methodNode.Body;
      if (body != null) {
        var newBody = AddStatements(body, statements);
        return methodNode.WithBody(newBody);
      }
      return methodNode;
    }


    public static PropertyDeclarationSyntax AddStatements(this PropertyDeclarationSyntax propertyNode, params string[] statements) {
      var accessors = propertyNode.AccessorList.Accessors;
      if (accessors != null) {
        var newAccessors = new SyntaxList<AccessorDeclarationSyntax>(accessors.Cast<AccessorDeclarationSyntax>().Select(accessor => {
          if (accessor.Body != null) {
            var newBody = AddStatements(accessor.Body, statements);
            return accessor.WithBody(newBody);
          }
          return accessor;
        }));
        return propertyNode.WithAccessorList(SyntaxFactory.AccessorList(newAccessors));
      }
      return propertyNode;
    }

    public static EventDeclarationSyntax AddStatements(this EventDeclarationSyntax eventNode, params string[] statements) {
      var accessors = eventNode.AccessorList.Accessors;
      if (accessors != null) {
        var newAccessors = new SyntaxList<AccessorDeclarationSyntax>(accessors.Cast<AccessorDeclarationSyntax>().Select(accessor => {
          if (accessor.Body != null) {
            var newBody = AddStatements(accessor.Body, statements);
            return accessor.WithBody(newBody);
          }
          return accessor;
        }));
        return eventNode.WithAccessorList(SyntaxFactory.AccessorList(newAccessors));
      }
      return eventNode;
    }

    public static string GetContainingTypeName<TSyntaxNode>(this TSyntaxNode node)
        where TSyntaxNode : SyntaxNode {
      var typeNames = new List<string>();
      var current = (SyntaxNode)node;

      while (current != null) {
        if (current is TypeDeclarationSyntax typeNode)
          typeNames.Insert(0, typeNode.Identifier.Text);

        current = current.Parent;
      }

      if (typeNames.Count > 0)
        return string.Join(".", typeNames);

      return null;
    }

    public static string GetContainingNamespace<TSyntaxNode>(this TSyntaxNode node)
        where TSyntaxNode : SyntaxNode {
      var current = (SyntaxNode)node;

      while (current != null && !(current is NamespaceDeclarationSyntax))
        current = current.Parent;

      if (current is NamespaceDeclarationSyntax namespaceNode)
        return namespaceNode.Name.ToString();

      return string.Empty;
    }

    public static string GetMemberName<TSyntaxNode>(this TSyntaxNode node)
        where TSyntaxNode : SyntaxNode {
      switch (node) {
        case MethodDeclarationSyntax methodNode:
          return methodNode.Identifier.Text;
        case PropertyDeclarationSyntax propertyNode:
          return propertyNode.Identifier.Text;
        case EventDeclarationSyntax eventNode:
          return eventNode.Identifier.Text;
        case FieldDeclarationSyntax fieldNode:
          var variable = fieldNode.Declaration.Variables.FirstOrDefault();
          return variable.Identifier.Text;
        case VariableDeclaratorSyntax variableNode:
          return variableNode.Identifier.Text;
        case TypeDeclarationSyntax typeNode:
          return typeNode.Identifier.Text;
        case NamespaceDeclarationSyntax namespaceNode:
          return namespaceNode.Name.ToString();
        default:
          return null;
      }
    }

    public static MethodDeclarationSyntax RenameMember(this MethodDeclarationSyntax methodNode, Func<string, string> nameReplacer) {
      var currentName = methodNode.Identifier.Text;
      var newName = nameReplacer.Invoke(currentName);

      if (string.IsNullOrWhiteSpace(newName) || string.Equals(currentName, newName, StringComparison.Ordinal))
        return methodNode;

      return methodNode.WithIdentifier(SyntaxFactory.Identifier(newName));
    }

    public static PropertyDeclarationSyntax RenameMember(this PropertyDeclarationSyntax propertyNode, Func<string, string> nameReplacer) {
      var currentName = propertyNode.Identifier.Text;
      var newName = nameReplacer.Invoke(currentName);

      if (string.IsNullOrWhiteSpace(newName) || string.Equals(currentName, newName, StringComparison.Ordinal))
        return propertyNode;

      return propertyNode.WithIdentifier(SyntaxFactory.Identifier(newName));
    }

    public static EventDeclarationSyntax RenameMember(this EventDeclarationSyntax eventNode, Func<string, string> nameReplacer) {
      var currentName = eventNode.Identifier.Text;
      var newName = nameReplacer.Invoke(currentName);

      if (string.IsNullOrWhiteSpace(newName) || string.Equals(currentName, newName, StringComparison.Ordinal))
        return eventNode;

      return eventNode.WithIdentifier(SyntaxFactory.Identifier(newName));
    }

    public static FieldDeclarationSyntax RenameMember(this FieldDeclarationSyntax fieldNode, Func<string, string> nameReplacer) {
      var variable = fieldNode.Declaration.Variables.FirstOrDefault();
      if (variable == null)
        return fieldNode;

      var currentName = variable.Identifier.Text;
      var newName = nameReplacer.Invoke(currentName);

      if (string.IsNullOrWhiteSpace(newName) || string.Equals(currentName, newName, StringComparison.Ordinal))
        return fieldNode;

      var newVariable = variable.WithIdentifier(SyntaxFactory.Identifier(newName));
      var newDeclaration = fieldNode.Declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(newVariable));
      return fieldNode.WithDeclaration(newDeclaration);
    }

    public static TypeDeclarationSyntax RenameMember(this TypeDeclarationSyntax typeNode, Func<string, string> nameReplacer) {
      var currentName = typeNode.Identifier.Text;
      var newName = nameReplacer.Invoke(currentName);

      if (string.IsNullOrWhiteSpace(newName) || string.Equals(currentName, newName, StringComparison.Ordinal))
        return typeNode;

      return typeNode.WithIdentifier(SyntaxFactory.Identifier(newName));
    }

    public static NamespaceDeclarationSyntax RenameMember(this NamespaceDeclarationSyntax namespaceNode, Func<string, string> nameReplacer) {
      var currentName = namespaceNode.Name.ToString();
      var newName = nameReplacer.Invoke(currentName);

      if (string.IsNullOrWhiteSpace(newName) || string.Equals(currentName, newName, StringComparison.Ordinal))
        return namespaceNode;

      var newNamespaceName = SyntaxFactory.ParseName(newName);
      return namespaceNode.WithName(newNamespaceName);
    }


    public static string RewriteCSharp(this FluentCSharpSyntaxRewriter rewriter,
        SyntaxNode syntaxNode,
        bool useTabs = false, int indentationSize = 4,
        FormattingOptions.IndentStyle indentStyle = default,
        string newLine = default)
        => rewriter.Visit(syntaxNode).ToFullStringCSharp(useTabs, indentationSize, indentStyle, newLine);

    public static string ToFullStringCSharp(this SyntaxNode syntaxNode,
        bool useTabs = false, int indentationSize = 4,
        FormattingOptions.IndentStyle indentStyle = default,
        string newLine = default) {
      using (var workspace = new AdhocWorkspace()) {
        var options = CSharpFormateOptions.Option(workspace.Options)
            .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, useTabs)
            .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, indentationSize)
            .WithChangedOption(FormattingOptions.SmartIndent, LanguageNames.CSharp, indentStyle)
            .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, newLine ?? Environment.NewLine);
        var formattedRoot = Formatter.Format(syntaxNode, workspace, options);
        var newRoot = new AlignEqualsRewriter().Visit(formattedRoot);
        return newRoot.ToFullString();
      }
    }

    public static string GetIdentifier<TNameSyntax>(this TNameSyntax nameSyntax)
        where TNameSyntax : NameSyntax {
      switch (nameSyntax) {
        case IdentifierNameSyntax identifierName:
          return identifierName.Identifier.Text;

        case QualifiedNameSyntax qualifiedName:
          var left = GetIdentifier(qualifiedName.Left);
          var right = GetIdentifier(qualifiedName.Right);
          return string.Join(".", new string[] { left, right, });

        default:
          return string.Empty;
      }
    }

    public static TBaseNamespaceDeclarationSyntax AddUsings<TBaseNamespaceDeclarationSyntax>(this TBaseNamespaceDeclarationSyntax ns, params string[] namespacesToAdd)
        where TBaseNamespaceDeclarationSyntax : BaseNamespaceDeclarationSyntax {
      if (namespacesToAdd == null || namespacesToAdd.Length < 1)
        return ns;

      var list = ns.Usings;
      foreach (var eachNamespace in namespacesToAdd)
        list = list.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseTypeName(eachNamespace)));

      return (TBaseNamespaceDeclarationSyntax)ns.WithUsings(list);
    }

    public static TBaseNamespaceDeclarationSyntax AddUsings<TBaseNamespaceDeclarationSyntax>(this TBaseNamespaceDeclarationSyntax ns, IEnumerable<string> namespacesToAdd)
       where TBaseNamespaceDeclarationSyntax : BaseNamespaceDeclarationSyntax {
      if (namespacesToAdd == null || namespacesToAdd.Count() < 1)
        return ns;

      var list = ns.Usings;
      foreach (var eachNamespace in namespacesToAdd)
        list = list.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseTypeName(eachNamespace)));

      return (TBaseNamespaceDeclarationSyntax)ns.WithUsings(list);
    }

    public static TBaseNamespaceDeclarationSyntax RemoveUsings<TBaseNamespaceDeclarationSyntax>(this TBaseNamespaceDeclarationSyntax ns, params string[] namespacesToRemove)
        where TBaseNamespaceDeclarationSyntax : BaseNamespaceDeclarationSyntax {
      if (namespacesToRemove == null || namespacesToRemove.Length < 1)
        return ns;

      var newList = new SyntaxList<UsingDirectiveSyntax>();

      foreach (var eachNamespace in ns.Usings) {
        var text = eachNamespace.Name.GetIdentifier();

        if (namespacesToRemove.Contains(text, StringComparer.Ordinal))
          continue;

        newList = newList.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseTypeName(text)));
      }

      return (TBaseNamespaceDeclarationSyntax)ns.WithUsings(newList);
    }

    public static TBaseNamespaceDeclarationSyntax DistinctUsings<TBaseNamespaceDeclarationSyntax>(this TBaseNamespaceDeclarationSyntax ns)
        where TBaseNamespaceDeclarationSyntax : BaseNamespaceDeclarationSyntax {
      return (TBaseNamespaceDeclarationSyntax)ns.WithUsings(new SyntaxList<UsingDirectiveSyntax>(
          Helpers.DistinctBy(ns.Usings, x => x.Name.GetIdentifier(), StringComparer.Ordinal)));
    }

    public static TBaseNamespaceDeclarationSyntax OrderUsings<TBaseNamespaceDeclarationSyntax>(this TBaseNamespaceDeclarationSyntax ns)
        where TBaseNamespaceDeclarationSyntax : BaseNamespaceDeclarationSyntax {
      return (TBaseNamespaceDeclarationSyntax)ns.WithUsings(new SyntaxList<UsingDirectiveSyntax>(
          ns.Usings.OrderBy(x => x.Name.GetIdentifier(), StringComparer.Ordinal)));
    }

    public static TBaseNamespaceDeclarationSyntax OrderUsingsDescending<TBaseNamespaceDeclarationSyntax>(this TBaseNamespaceDeclarationSyntax ns)
        where TBaseNamespaceDeclarationSyntax : BaseNamespaceDeclarationSyntax {
      return (TBaseNamespaceDeclarationSyntax)ns.WithUsings(new SyntaxList<UsingDirectiveSyntax>(
          ns.Usings.OrderByDescending(x => x.Name.GetIdentifier(), StringComparer.Ordinal)));
    }

    public static MemberDeclarationSyntax ParseMemberDeclaration<TSyntaxNode>(this TSyntaxNode node, int offset = 0, ParseOptions options = default, bool consumeFullText = true)
        where TSyntaxNode : SyntaxNode
        => SyntaxFactory.ParseMemberDeclaration(node.ToFullString(), offset, options, consumeFullText);

    public static TMemberDeclarationSyntax AddXmlDocumentation<TMemberDeclarationSyntax>(this TMemberDeclarationSyntax member,
        string summary = default, string remarks = default, string returns = default,
        IReadOnlyDictionary<string, string> parameters = default)
        where TMemberDeclarationSyntax : MemberDeclarationSyntax {
      var list = new List<XmlNodeSyntax>();

      if (summary != null)
        list.Add(SyntaxFactory.XmlSummaryElement(SyntaxFactory.XmlText(summary)));

      if (remarks != null)
        list.Add(SyntaxFactory.XmlRemarksElement(SyntaxFactory.XmlText(remarks)));

      if (parameters != null && parameters.Count > 0) {
        foreach (var eachParameter in parameters)
          list.Add(SyntaxFactory.XmlParamElement(eachParameter.Key, SyntaxFactory.XmlText(eachParameter.Value)));
      }

      if (returns != null)
        list.Add(SyntaxFactory.XmlReturnsElement(SyntaxFactory.XmlText(returns)));

      if (list.Count < 1)
        return member;

      return member.WithLeadingTrivia(member.GetLeadingTrivia().AddRange(new SyntaxTrivia[]
      {
                SyntaxFactory.Trivia(SyntaxFactory.DocumentationComment(list.ToArray())),
                SyntaxFactory.ElasticLineFeed,
      }));
    }

    public static CompilationUnitSyntax GetCompilationUnitSyntax<TSyntaxTree>(this TSyntaxTree syntaxTree,
        CancellationToken cancellationToken = default)
        where TSyntaxTree : SyntaxTree
        => syntaxTree?.GetRoot(cancellationToken) as CompilationUnitSyntax;

    public static async Task<CompilationUnitSyntax> GetCompilationUnitSyntaxAsync<TSyntaxTree>(this TSyntaxTree syntaxTree,
        CancellationToken cancellationToken = default)
        where TSyntaxTree : SyntaxTree
        => (syntaxTree != null) ? await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax : null;

    public static bool TryGetCompilationUnitSyntax<TSyntaxTree>(this TSyntaxTree syntaxTree,
        out CompilationUnitSyntax compilationUnitSyntax)
        where TSyntaxTree : SyntaxTree {
      if (syntaxTree == null || !syntaxTree.TryGetRoot(out SyntaxNode root) || root == null) {
        compilationUnitSyntax = null;
        return false;
      }
      else {
        compilationUnitSyntax = root as CompilationUnitSyntax;
        return (compilationUnitSyntax != null);
      }
    }

    public static CompilationUnitSyntax ReplacePlaceholders<TSyntaxNode>(this TSyntaxNode targetNode, Dictionary<string, Func<string>> maps, string prefix = default)
        where TSyntaxNode : SyntaxNode {
      if (targetNode == null)
        throw new ArgumentNullException(nameof(targetNode));

      if (maps == null || maps.Count < 1)
        return CSharpSyntaxTree.ParseText(targetNode.ToFullString()).GetCompilationUnitRoot();

      if (string.IsNullOrWhiteSpace(prefix))
        prefix = "REPLACE:";

      var trimmingCharacters = new char[] { '/', '*', ' ', '\t', };
      var spanList = new Dictionary<TextSpan, string>(maps.Count);

      var sourceCode = FluentCSharpSyntaxRewriter.Define(true)
          .WithVisitTrivia((_, x) => {
            var key = x.ToString().Trim(trimmingCharacters).Replace(prefix, string.Empty).Trim();
            if (maps.ContainsKey(key))
              spanList.Add(x.Span, maps[key].Invoke());
            return x;
          })
          .Visit(targetNode)
          .ToFullString();

      var buffer = new StringBuilder();
      var lastIndex = 0;

      foreach (var eachSpan in spanList) {
        buffer.Append(sourceCode.Substring(lastIndex, eachSpan.Key.Start - lastIndex));
        buffer.Append(eachSpan.Value);
        lastIndex = eachSpan.Key.End;
      }
      buffer.Append(sourceCode.Substring(lastIndex));

      return CSharpSyntaxTree.ParseText(buffer.ToString()).GetCompilationUnitRoot();
    }
  }
}
