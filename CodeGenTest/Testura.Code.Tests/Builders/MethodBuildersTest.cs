using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using NUnit.Framework;
using Testura.Code.Builders;
using Testura.Code.Generators.Common;
using Testura.Code.Models;
using Assert = NUnit.Framework.Assert;

namespace Testura.Code.Tests.Builders;

[TestFixture]
public class MethodBuildersTest {
  [Test]
  public void Build_WhenGivingMethodName_CodeShouldContainName() {
    var builder = new MethodBuilder("MyMethod");
    var method = builder.Build();
    var type = method.GetType();
    Assert.IsTrue(method is (MethodDeclarationSyntax));
    Assert.IsTrue(type == typeof(MethodDeclarationSyntax) || type == typeof(MethodBlockSyntax));
    Assert.IsTrue(builder.Build().ToString().Contains("MyMethod()"));
  }

  [Test]
  public void Build_WhenGivingAttribute_CodeShouldContainAttribute() {
    var builder = new MethodBuilder("MyMethod");
    Assert.IsTrue(builder.WithAttributes(new TesturaAttribute("MyAttribute")).Build().ToString()
        .Contains("[MyAttribute]"));
  }

  [Test]
  public void Build_WhenGivingModifier_CodeShouldContainModifiers() {
    var builder = new MethodBuilder("MyMethod");
    Assert.IsTrue(builder.WithModifiers(Modifiers.Public, Modifiers.Abstract).Build().ToString()
        .Contains("publicabstract"));
  }

  [Test]
  public void Build_WhenGivingParameters_CodeShouldContainParameters() {
    var builder = new MethodBuilder("MyMethod");
    Assert.IsTrue(builder.WithParameters(new Parameter("myParamter", typeof(int))).Build().ToString()
        .Contains("intmyParamter"));
  }

  [Test]
  public void Build_WhenGivingParameterWithModifier_CodeShouldContainParameters() {
    var builder = new MethodBuilder("MyMethod");
    Assert.IsTrue(builder.WithParameters(new Parameter("myParamter", typeof(int), ParameterModifiers.This))
        .Build().ToString().Contains("thisintmyParamter"));
  }

  [Test]
  public void Build_WhenGivingParameterWithAttribute_CodeShouldContainParameters() {
    var builder = new MethodBuilder("MyMethod");

    Assert.IsTrue(builder.WithParameters(new Parameter("myParamter", typeof(int), attributes: new List<TesturaAttribute> { new TesturaAttribute("FromRoute") }))
        .Build().ToString().Contains("[FromRoute]intmyParamter"));
  }

  [Test]
  public void Build_WhenGivingReturnType_CodeShouldContainReturn() {
    var builder = new MethodBuilder("MyMethod");
    Assert.IsTrue(builder.WithReturnType(typeof(int)).Build().ToString().Contains("intMyMethod()"));
  }

  [Test]
  public void Build_WhenGivingNullBody_CodeShouldContainMethodWithSemicolonAtTheEnd() {
    var builder = new MethodBuilder("MyMethod");
    Assert.IsTrue(builder.WithBody(null).Build().ToString().Contains("MyMethod();"));
  }

  [Test]
  public void Build_WhenHavingOperatorOverloading_ShouldGenerateOverloading() {
    var builder = new MethodBuilder("MyMethod")
        .WithModifiers(Modifiers.Public, Modifiers.Static)
        .WithOperatorOverloading(Operators.Equal)
        .WithBody(BodyGenerator.Create());

    StringAssert.Contains("publicstaticMyMethodoperator==(){}", builder.Build().ToString());
  }
}
