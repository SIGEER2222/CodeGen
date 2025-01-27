using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static SourceGeneratorQuery.Declarations.MethodDeclarationExtentions;
#nullable disable

namespace SourceGeneratorQuery.Declarations;
public class MethodDeclaration {
  public MethodDeclaration(BaseMethodDeclarationSyntax node, TypeDeclaration parent) {
    this.SyntaxNode = node;
    Parent = parent;
  }

  public readonly BaseMethodDeclarationSyntax SyntaxNode;
  public readonly TypeDeclaration Parent;

  public bool IsPublic {
    get {
      return Modifiers.Contains("public");
    }
  }
  public bool IsProtected =>
      Modifiers.Contains("protected");
  public bool IsInternal =>
      Modifiers.Contains("internal");
  public bool IsPrivate => Modifiers.Contains("private") ||
      !IsPublic && !IsProtected && !IsInternal;
  public bool IsReadonly =>
      Modifiers.Contains("readonly");
  public bool IsStatic =>
      Modifiers.Contains("static");
  public bool IsAbstract =>
      Modifiers.Contains("abstract");
  public bool IsAsync =>
      Modifiers.Contains("async");
  public IEnumerable<string> Modifiers =>
      SyntaxNode.Modifiers.Select(x => x.Text);

  public IEnumerable<string> Identifiers => SyntaxNode.DescendantNodes().OfType<IdentifierNameSyntax>().Select(x => x.Identifier.Text);

  public IEnumerable<ArgumentListSyntaxNode> ArgumentLists => SyntaxNode.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(invocation => new ArgumentListSyntaxNode {
    Name = invocation.Expression.ToString(),
    ArgumentList = invocation.ArgumentList
  });

  public IEnumerable<InvocationExpressionSyntax> InvocationExpressionSyntaxs => SyntaxNode.DescendantNodes().OfType<InvocationExpressionSyntax>();

  public IEnumerable<ExpressionStatementSyntax> ExpressionStatementSyntaxs => SyntaxNode.DescendantNodes().OfType<ExpressionStatementSyntax>();

  public string Name {
    get {
      switch (SyntaxNode) {
        case MethodDeclarationSyntax method:
          return method.Identifier.ToString();
      }
      switch (SyntaxNode) {
        case ConstructorDeclarationSyntax method:
          return method.Identifier.ToString();
      }

      throw new ArgumentNullException();
    }
    set {
      switch (SyntaxNode) {
        case MethodDeclarationSyntax method:
          method.WithIdentifier(SyntaxFactory.Identifier(""));
          break;
      }
      throw new NotImplementedException();
    }
  }
  public string ReturnType {
    get {
      switch (SyntaxNode) {
        case MethodDeclarationSyntax method:
          return method.ReturnType.ToString();
      }
      return "void";
    }
  }
  public bool ReturnTypeIsNullable {
    get {
      switch (SyntaxNode) {
        case MethodDeclarationSyntax method:
          return method.ReturnType.IsNotNull;
      }
      return false;
    }
  }
  public IEnumerable<AttributeDeclaration> Attributes => SyntaxNode.AttributeLists
      .SelectMany(x => x.Attributes.Select(a => new AttributeDeclaration(a)));
  public IEnumerable<ParameterDeclaration> Parameters =>
      SyntaxNode.ParameterList.Parameters.Select(p => new ParameterDeclaration(p));
  public string Body =>
      SyntaxNode.Body.GetText().ToString();
}

public static class MethodDeclarationExtentions {
  public static IEnumerable<MethodDeclaration> WithName(this IEnumerable<MethodDeclaration> source,
      Func<string, bool> predicate) {
    return source.Where(x => predicate(x.Name));
  }
  public static IEnumerable<MethodDeclaration> WithAttribute(this IEnumerable<MethodDeclaration> source,
      Func<AttributeDeclaration, bool> predicate) {
    return source.Where(x => x.Attributes.Any(a => predicate(a)));
  }
  public static IEnumerable<MethodDeclaration> WithAttribute(this IEnumerable<MethodDeclaration> source,
      string name) {
    return source.Where(x => x.Attributes.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
  }
  public static IEnumerable<MethodDeclaration> WithPublic(this IEnumerable<MethodDeclaration> source) {
    return source.Where(x => x.IsPublic);
  }
  public static IEnumerable<MethodDeclaration> WithProtected(this IEnumerable<MethodDeclaration> source) {
    return source.Where(x => x.IsProtected);
  }
  public static IEnumerable<MethodDeclaration> WithInternal(this IEnumerable<MethodDeclaration> source) {
    return source.Where(x => x.IsInternal);
  }
  public static IEnumerable<MethodDeclaration> WithPrivate(this IEnumerable<MethodDeclaration> source) {
    return source.Where(x => x.IsPrivate);
  }
  public static IEnumerable<MethodDeclaration> WithReadonly(this IEnumerable<MethodDeclaration> source) {
    return source.Where(x => x.IsReadonly);
  }
  public static IEnumerable<MethodDeclaration> WithStatic(this IEnumerable<MethodDeclaration> source) {
    return source.Where(x => x.IsStatic);
  }
  public static IEnumerable<MethodDeclaration> WithAbstract(this IEnumerable<MethodDeclaration> source) {
    return source.Where(x => x.IsAbstract);
  }

  public class ArgumentListSyntaxNode {
    public string Name { get; set; }
    public ArgumentListSyntax ArgumentList { get; set; }
  }
}
