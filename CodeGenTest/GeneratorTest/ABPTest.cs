
using Buildalyzer;
using Buildalyzer.Workspaces;

namespace FluentSyntaxRewriter.Test.GeneratorTest;
public class ABPTest {

  [Fact]
  public async Task GetAllProjects() {
    var path = @"D:\MyWork\MOM新版\mom.solution.sln\momwinform\host\ManuTalent.Mom.Operation.Winform.Host\ManuTalent.Mom.Operation.Winform.Host.csproj";
    var project = BuildalyzerQuery.GetProjectByProjectName(path);
    var sln = project.Solution;
    var projects = sln.Projects.Select(x => x.FilePath).ToList();
    _ = 1;
  }

  [Fact]
  public async Task TestSln() {
    StringWriter log = new StringWriter();
    AnalyzerManager manager = new(@"D:\MyWork\测试项目\LearnAndTest.sln", new AnalyzerManagerOptions() {
      LogWriter = log
    });
    var projects = manager.Projects;

    var allDocuments = projects.Select(x => x.Value).SelectMany(x => x.Build()).ToList();
    var workspace = manager.GetWorkspace();
    _ = 1;
  }

  [Fact]
  public async Task GenerateBase() {
    var basePath = @"D:\MyWork\MOM新版";

    var strNamespace = "ManuTalent.Mom.Semi.FrontEnd.Generated";
    var domain = $$"""
      namespace {{strNamespace}};
      public partial class GeneratedDomainService : SemiFrontEndDomainBase{

      }
    """;
    domain = AddUsingsToCompilationUnit(domain, " ManuTalent.Mom.Semi.FrontEnd.Dto.Generated", "  System.Collections.Generic",
   " ManuTalent.Mom.Semi.FrontEnd.EntityFrameworkCore.Models");
    var partDomainPath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\domain\ManuTalent.Mom.Semi.FrontEnd.Domain\Domain", "Generated", "DomainServices");

    var facade = $$"""
      namespace {{strNamespace}};
      using ManuTalent.Mom.Semi.FrontEnd;
      public partial class GeneratedFacade : SemiFrontEndFacadeBase, IGeneratedFacade{
        protected GeneratedDomainService GeneratedDomainService => LazyServiceProvider.LazyGetRequiredService<GeneratedDomainService>();
      }
    """;
    var facadeInterface = $$"""
      namespace {{strNamespace}};
      public interface IGeneratedFacade{
      }
    """;
    var facadePath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\impl\ManuTalent.Mom.Semi.FrontEnd.Facade.Impl",
   "Generated");

    var iFacadePath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\interface\ManuTalent.Mom.Semi.FrontEnd.Facade.Interface\FacadeInterface",
       "Generated");
    Directory.CreateDirectory(iFacadePath);

    File.WriteAllText(Path.Combine(partDomainPath, "GeneratedDomainService.cs"), FormatCode(domain));
    File.WriteAllText(Path.Combine(facadePath, "GeneratedFacade.cs"), FormatCode(facade));
    File.WriteAllText(Path.Combine(iFacadePath, "IGeneratedFacade.cs"), FormatCode(facadeInterface));
  }

  [Fact]
  public async Task TestABP() {
    var project = BuildalyzerQuery.GetProjectByProjectName(@"D:\MyWork\测试项目\Reactive\src\MomContext\MomContext.csproj");
    var sln = project.Solution;
    var projects = sln.Projects.Select(x => x.FilePath).ToList();
    var compilation = (await project.GetCompilationAsync()) as CSharpCompilation;
    var allClass = await BuildalyzerQuery.QueryAlllassByProjectName(project);
    var cl = allClass.FirstOrDefault(x => x.Name == "FabTransformSetting");
    var className = cl.Name;
    var strNamespace = "ManuTalent.Mom.Semi.FrontEnd.Generated";
    var basePath = @"D:\MyWork\MOM新版";

    var members = allClass.FirstOrDefault(x => x.Name == "FabTransformSetting").SyntaxNode.Members
            .Where(member => member.IsKind(SyntaxKind.MethodDeclaration) || member.IsKind(SyntaxKind.PropertyDeclaration))
            .Where(member => member.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)))
            .ToList();

    var baseCode = FluentCSharpSyntaxRewriter.Define()
       .WithVisitClassDeclaration((_, cls) => {
         BaseTypeSyntax baseClass = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("SemiEntity"));
         BaseTypeSyntax interfaces = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("I" + className));
         return cls.AddBaseListTypes(baseClass).AddBaseListTypes(interfaces);
       })
       .WithVisitNamespaceDeclaration((_, ns) => {
         ns = ns.AddUsings("ManuTalent.Mom.Semi.FrontEnd.IEntities.Generated", "Semi.Domain.Entities").OrderUsings().DistinctUsings().RenameMember(_ => strNamespace);
         return ns;
       })
       .RewriteCSharp(cl.SourceFile.SyntaxTree.GetRoot());
    var tree = SyntaxFactory.ParseSyntaxTree(baseCode.ToString());
    baseCode = AddUsingsToCompilationUnit(tree.GetCompilationUnitRoot(), " ManuTalent.Mom.Semi.FrontEnd.IEntities.Generated", " Semi.Domain.Entities").ToString();
    var repName = className.Replace("Fab", "") + "Repository";

    // File.WriteAllText(Path.Combine(repositoryPath + "\\Generated", "I" + repName + ".cs"), iRep);
    var rep = $$"""
        using ManuTalent.Mom.Domain.EfCoreImpl;
        using ManuTalent.Mom.Semi.FrontEnd.BusinessRules.WIP.LotRule.Entities;
        using ManuTalent.Mom.Semi.FrontEnd.Domain.WIP.LotRule.IRepositories;
        using Semi.Domain.Repositories;
        using Volo.Abp.Caching;
        using Volo.Abp.EntityFrameworkCore;
        using ManuTalent.Mom.Semi.FrontEnd.EntityFrameworkCore.Models;
        using ManuTalent.Mom.Semi.FrontEnd.Generated;
        namespace ManuTalent.Mom.Semi.FrontEnd.EntityFrameworkCore.Repositories;

        public class {{repName}} : SemiRepositoryBase<{{className}}>, I{{repName}}
        {
            public {{repName}}(IDbContextProvider<IMomDbContext> dbContextProvider, IDistributedCache<{{className}}> cache) : base(dbContextProvider, cache)
            {
            }
        }
      """;

    var entityPath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\domain\ManuTalent.Mom.Semi.FrontEnd.Domain\Domain",
       "Generated", "Entities");
    Directory.CreateDirectory(entityPath);

    var member = cl.GetPropertiesWithoutAttributesOrDefaults().Select(x => x.ToString()).Aggregate((x, y) => x + Environment.NewLine + y);
    var iEntity = $$"""
      namespace ManuTalent.Mom.Semi.FrontEnd.IEntities.Generated;
      public interface I{{className}}
      {
        {{member}}
      }
    """;

    var propertiesWithoutCertainAttributes = cl.SyntaxNode.ChildNodes().OfType<PropertyDeclarationSyntax>()
        .Select(property => property
            .WithAttributeLists(
                new SyntaxList<AttributeListSyntax>(
                    property.AttributeLists.SelectMany(
                       al => al.Attributes.Where(
                            attr => attr.Name.ToString() != "Key" &&
                                    !(attr.Name.ToString() == "Column" && attr.ArgumentList.Arguments.Count > 0)
                        )
                        .Select(attr => SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr)))
                        .Select(al => al.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
                    )
                )
            )
            .WithInitializer(null)
            .WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.None))
        )
        .Select(x => x.ToString())
        .Aggregate((x, y) => x + Environment.NewLine + y);

    var dto = $$"""
      namespace ManuTalent.Mom.Semi.FrontEnd.Dto.Generated;
      public class {{className.Replace("Fab", "")}}Dto
      {
        {{propertiesWithoutCertainAttributes}}
      }
    """;
    dto = FormatCode(dto);
    var dtoPath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\shared\ManuTalent.Mom.Semi.FrontEnd.Shared\IEntities",
       "Generated", "Dto");

    var mapPath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\shared\ManuTalent.Mom.Semi.FrontEnd.Shared\Helpers", "Generated", "Maps");
    Directory.CreateDirectory(dtoPath);
    Directory.CreateDirectory(mapPath);

    var iEntityPath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\shared\ManuTalent.Mom.Semi.FrontEnd.Shared\IEntities",
       "Generated", "IEntities");
    Directory.CreateDirectory(iEntityPath);

    var repPath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\impl\ManuTalent.Mom.Semi.FrontEnd.EfCoreImpl\EntityFrameworkCore", "Generated");
    Directory.CreateDirectory(repPath);

    var domainServicePath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\domain\ManuTalent.Mom.Semi.FrontEnd.Domain\Domain",
       "Generated", @"DomainServices");
    Directory.CreateDirectory(domainServicePath);

    var iRep = $$"""
        using ManuTalent.Mom.Semi.FrontEnd.EntityFrameworkCore.Models;
        using ManuTalent.Mom.Semi.FrontEnd.BusinessRules.WIP.LotRule.Entities;
        using Semi.Domain.Repositories;
        namespace {{strNamespace}};
        public interface I{{repName}} : ISemiRepository<{{className}}>{}
      """;

    var iRepositoryPath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\domain\ManuTalent.Mom.Semi.FrontEnd.Domain\Domain",
       "Generated", @"IRepositories");
    Directory.CreateDirectory(iRepositoryPath);

    var domain = $$"""
      namespace {{strNamespace}};
      public partial class GeneratedDomainService : SemiFrontEndDomainBase{

      }
    """;
    var partDomainPath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\domain\ManuTalent.Mom.Semi.FrontEnd.Domain\Domain", "Generated", "DomainServices");
    var code3 = SyntaxFactory.ParseMemberDeclaration($$"""
      protected I{{repName}} {{repName}} => LazyServiceProvider.LazyGetRequiredService<I{{repName}}>();
      public async Task<List<{{className.Replace("Fab", "") + "Dto"}}>> Get{{className.Replace("Fab", "")}}Async()
      {
          var query = await {{repName}}.QueryAllAsync();
          return ObjectMapper.Map<List<{{className}}>, List<{{className.Replace("Fab", "") + "Dto"}}>>(query);
      }
    """);
    domain = AddUsingsToCompilationUnit(domain, " ManuTalent.Mom.Semi.FrontEnd.Dto.Generated", "  System.Collections.Generic",
    " ManuTalent.Mom.Semi.FrontEnd.EntityFrameworkCore.Models");
    domain = FluentCSharpSyntaxRewriter.Define()
       .WithVisitClassDeclaration((_, cls) => {
         return cls.AddMembers(code3);
       })
       .RewriteCSharp(SyntaxFactory.ParseSyntaxTree(domain.ToString()).GetRoot());

    var facade = $$"""
      namespace {{strNamespace}};
      using System.Collections.Generic;
      using System.Threading.Tasks;
      using ManuTalent.Mom.Semi.FrontEnd.Dto.Generated;

      using ManuTalent.Mom.Semi.FrontEnd;
      public partial class GeneratedFacade : SemiFrontEndFacadeBase, IGeneratedFacade{
        protected GeneratedDomainService GeneratedDomainService => LazyServiceProvider.LazyGetRequiredService<GeneratedDomainService>();
      }
    """;
    var facadeInterface = $$"""
      namespace {{strNamespace}};
      using System.Collections.Generic;
      using System.Threading.Tasks;
      using ManuTalent.Mom.Semi.FrontEnd.Dto.Generated;
      public interface IGeneratedFacade{
      }
    """;
    var facadePath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\impl\ManuTalent.Mom.Semi.FrontEnd.Facade.Impl",
   "Generated");

    var iFacadePath = Path.Combine(basePath, @"mom.solution.sln\momsolution\src\interface\ManuTalent.Mom.Semi.FrontEnd.Facade.Interface\FacadeInterface",
       "Generated");
    Directory.CreateDirectory(iFacadePath);

    var baseRule = $$"""
      namespace ManuTalent.Mom.Semi.FrontEnd.Generated;
      public abstract partial class GeneratedAdhocBusinessRule : SemiFrontEndAdhocBusinessRuleBase
      {
        protected IGeneratedFacade GeneratedFacade => ServiceProvider.LazyGetRequiredService<IGeneratedFacade>();
      }
    """;

    File.WriteAllText(Path.Combine(entityPath, cl.Name + ".cs"), FormatCode(baseCode.ToString()));
    File.WriteAllText(Path.Combine(iEntityPath, "I" + cl.Name + ".cs"), FormatCode(iEntity.ToString()));
    File.WriteAllText(Path.Combine(repPath, repName + ".cs"), FormatCode(rep));
    File.WriteAllText(Path.Combine(iRepositoryPath, "I" + repName + ".cs"), FormatCode(iRep));
    // File.WriteAllText(Path.Combine(basePath, @"mom.solution.sln\momsolution\src\core\domain\ManuTalent.Mom.Semi.FrontEnd.Domain",
    //   "Generated", "SemiFrontEndDomainBase.cs"), FormatCode(SemiFrontEndDomainBaseCode));
    File.WriteAllText(Path.Combine(partDomainPath, "GeneratedDomainService.cs"), FormatCode(domain));
    File.WriteAllText(Path.Combine(dtoPath, className.Replace("Fab", "") + "Dto.cs"), FormatCode(dto));
    File.WriteAllText(Path.Combine(facadePath, "GeneratedFacade.cs"), FormatCode(facade));
    File.WriteAllText(Path.Combine(iFacadePath, "IGeneratedFacade.cs"), FormatCode(facadeInterface));
    File.WriteAllText(Path.Combine(basePath, @"mom.solution.sln\momsolution\src\businessrule\ManuTalent.Mom.Semi.FrontEnd.BusinessRule\Generated\BusinessRules", "GeneratedAdhocBusinessRule.cs"), FormatCode(baseRule));
    _ = 1;
  }

  [Fact]
  public async Task GeneratedBaseRule() {

    var baseRule = $$"""
    namespace ManuTalent.Mom.Semi.FrontEnd.Generated;

    public class GetTransformSetting : Generated.GeneratedAdhocBusinessRule
    {
        [BusinessRule(SemiFrontEndAccessStringConsts.OperationQueryEQPGetEqpStateByCurrentState)]
        public async Task<SemiFrontEndAdhocRuleResponseBase<Dto.Generated.TransformSettingDto>> DoGet(GetTransformSettingBusinessRequest request)
        {
            var trans = await GeneratedFacade.GetTransformSettingAsync();

            return new SemiFrontEndAdhocRuleResponseBase<Dto.Generated.TransformSettingDto>()
            {
                ListValue = trans
            };
        }
    }
    """;
    var basePath = @"D:\MyWork\MOM新版\mom.solution.sln\momsolution\src\businessrule\ManuTalent.Mom.Semi.FrontEnd.BusinessRule\Generated\BusinessRules\QueryRule";

  }


  public static SyntaxTree AddUsingsToCompilationUnit(CompilationUnitSyntax compilationUnit, params string[] namespacesToAdd) {
    if (namespacesToAdd == null || namespacesToAdd.Length < 1)
      return compilationUnit.SyntaxTree;

    var list = compilationUnit.Usings;
    foreach (var eachNamespace in namespacesToAdd)
      list = list.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(eachNamespace)));
    return compilationUnit.WithUsings(list).SyntaxTree;
  }

  public static string AddUsingsToCompilationUnit(string code, params string[] namespacesToAdd) {
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    return AddUsingsToCompilationUnit(syntaxTree.GetCompilationUnitRoot(), namespacesToAdd).ToString();
  }
  public static string FormatCode(string code) {
    var newCode = FluentCSharpSyntaxRewriter.Define()
       .RewriteCSharp(CSharpSyntaxTree.ParseText(code).GetRoot());
    return newCode.ToString();
  }

}
