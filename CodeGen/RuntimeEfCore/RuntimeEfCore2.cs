using System.Reflection;
using System.Runtime.Loader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;
using DbContext = Microsoft.EntityFrameworkCore.DbContext;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Diagnostics;
namespace RoslynTest;

public class RuntimeEfCore2 {
  static string dbPath = Path.Combine(Path.GetTempPath(), "RuntimeDataContext.dll");
  static readonly string mysqlConnection = "User ID=mom;Password=mom;Host=10.10.10.106;Port=31001;Database=mom;";

  public static void CreateDll() {
    var scaffolder = CreateMysqlScaffolder();

    var scaffoldedModelSources = scaffolder.ScaffoldModel(mysqlConnection, new(), new(), new() {
      RootNamespace = "TypedDataContext",
      ContextName = "RuntimeDataContext",
      ContextNamespace = "TypedDataContext.Context",
      ModelNamespace = "TypedDataContext.Models",
      UseDataAnnotations = true,
      SuppressConnectionStringWarning = true
    });

    var sourceFiles = scaffoldedModelSources.AdditionalFiles.Select(f => f.Code).ToList();
    sourceFiles.Add(scaffoldedModelSources.ContextFile.Code);

    var enableLazyLoading = false;

    var result = GenerateCode(sourceFiles, enableLazyLoading).Emit(dbPath);

    if (!result.Success) {
      var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
      var error = failures.FirstOrDefault();
      failures.ToList().ForEach(f => Console.WriteLine(f));
      throw new Exception($"{error?.Id}: {error?.GetMessage()}");
    }
    // peStream.Seek(0, SeekOrigin.Begin);

    // using var fileStream = new FileStream(dbPath, FileMode.Create, FileAccess.ReadWrite);
    // peStream.CopyTo(fileStream);
    // fileStream.Seek(0, SeekOrigin.Begin);
  }

  public static async Task Main2() {
    if (!File.Exists(dbPath)) {
      try {
        CreateDll();
      }
      catch (System.Exception ex) {
        Debug.WriteLine(ex.StackTrace);
      }
    }
    var assemblyLoadContext = new AssemblyLoadContext("DbContext", isCollectible: true);
    var assembly = assemblyLoadContext.LoadFromAssemblyPath(dbPath);

    var type = assembly.GetType("TypedDataContext.Context.RuntimeDataContext");
    _ = type ?? throw new Exception("未找到 DataContext 类型");

    var constr = type.GetConstructor(Type.EmptyTypes);
    _ = constr ?? throw new Exception("未找到 DataContext 构造函数");

    // 创建 DbContext 实例
    DbContext dynamicContext = (DbContext)constr.Invoke(null);

    // 获取所有实体类型（表等）并保存在 entityTypes 列表中
    var entityTypes = dynamicContext.Model.GetEntityTypes();

    var dbContextType = dynamicContext.GetType();

    foreach (var entityType in entityTypes) {
      var tableName = entityType.GetTableName();
      var items = (IQueryable<object>)dynamicContext.Query(entityType.Name);

      Console.WriteLine($"表名: {tableName}, 实体类型: {entityType.Name} 包含 {items.Count()} 条记录");

      foreach (var item in items) {
        var properties = item.GetType().GetProperties();
        foreach (var property in properties) {
          Console.WriteLine($"{property.Name}: {property.GetValue(item)}");
        }
      }
    }

    Console.ReadKey();

  }

  // var connectionString1 = dynamicContext.Database.GetDbConnection().ConnectionString; // 目标连接字符串

  // dynamicContext.Database.EnsureCreated(); // 创建目标数据库
  // dynamicContext.Database.Migrate(); // 将目标数据库模式更新为 "dynamicContext"

  // dynamicContext.Database.GetDbConnection().ConnectionString = connectionString1; // 恢复原始连接字符串

  // 示例代码：获取视图、触发器和存储过程
  /*
  var views = entityTypes
      .Where(x => x.GetProperties().All(p => !p.IsPrimaryKey()))
      .ToList();

  var triggers = entityTypes
      .Where(x => x.GetProperties().All(p => !p.IsPrimaryKey() && !p.IsForeignKey()))
      .ToList();

  var storedProcedures = entityTypes
      .Where(x => x.GetProperties().All(p => p.IsParameter()))
      .ToList();

  foreach (var view in views)
  {
      Console.WriteLine(view);
      var tableName = view.GetTableName();

      // 确定列
      var columns = string.Join(", ", view.GetProperties().Select(p => $"[{p.GetColumnName()}]"));

      var sql = $"CREATE VIEW [{tableName}] ({columns}) AS SELECT {columns} FROM [SourceDatabase].[dbo].[{tableName}]";

      targetDbContext.Database.ExecuteSqlRaw(sql);
  }
  */


  // Scaffold 构建器（逆向工程）用于从数据库中生成实体类的配置方法。
  [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "We need it")]
  static IReverseEngineerScaffolder CreateMysqlScaffolder() =>
      new ServiceCollection()
          .AddEntityFrameworkMySql() // 注册 MySQL 数据库提供程序
          .AddLogging() // 注册日志服务
          .AddEntityFrameworkDesignTimeServices() // 注册 EF Core 设计时服务
                                                  // 确保只创建一个实例
          .AddSingleton<LoggingDefinitions, Pomelo.EntityFrameworkCore.MySql.Diagnostics.Internal.MySqlLoggingDefinitions>() // Scaffold 构建器的日志服务
          .AddSingleton<IRelationalTypeMappingSource, Pomelo.EntityFrameworkCore.MySql.Storage.Internal.MySqlTypeMappingSource>() // 提供关系型数据类型映射
          .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>() // 为 EF Core 实体生成代码
          .AddSingleton<IDatabaseModelFactory, Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal.MySqlDatabaseModelFactory>() // 提供数据库模型生成器
          .AddSingleton<IProviderConfigurationCodeGenerator, Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal.MySqlCodeGenerator>() // 提供 EF Core 配置生成器
          .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>() // 提供 Scaffold 模型生成器
          .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>() // 提供 EF Core 复数化功能
                                                                                       // 当你想要模型化一个名为 "Person" 的数据库表时，
                                                                                       // Scaffold 构建器将生成名为 "People" 的实体类。
          .AddSingleton<ProviderCodeGeneratorDependencies>() // 提供 EF Core 提供程序所需的依赖项
          .AddSingleton<AnnotationCodeGeneratorDependencies>() // 提供 EF Core 实体所需的依赖项
          .BuildServiceProvider() // ServiceProvider 是一个自动生成组件所需依赖项的结构。
          .GetRequiredService<IReverseEngineerScaffolder>(); // 获取所需的 IReverseEngineerScaffolder 服务



  //Scaffold oluşturucuyu (Reverse Engineering) oluşturmak için kullanılan bir metottur.
  //veritabanından varlık sınıflarının oluşturulmasını mümkün kılan ayarlar yappılır.
  [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "We need it")]
  static IReverseEngineerScaffolder CreateMssqlScaffolder() =>
      new ServiceCollection()
         .AddEntityFrameworkSqlServer() //SQL Server veritabanı sağlayıcısını kaydeder
         .AddLogging() //Kayıt kaydeder
         .AddEntityFrameworkDesignTimeServices() //EF Core tasarım zamanı hizmetlerini kaydeder
                                                 //Yalnıca bir örneğin oluşmasını sağlar
         .AddSingleton<LoggingDefinitions, SqlServerLoggingDefinitions>() //Scaffold oluşturucusunun kayıt hizmetleri
         .AddSingleton<IRelationalTypeMappingSource, SqlServerTypeMappingSource>() //ilişkisel veri türü eşlemesini sağlar.
         .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>() //EF Core varlıkları için kod oluşturur.
         .AddSingleton<IDatabaseModelFactory, SqlServerDatabaseModelFactory>() // veritabanı modeli oluşturucularını sağlar
         .AddSingleton<IProviderConfigurationCodeGenerator, SqlServerCodeGenerator>() //EF Core sağlayıcı yapılandırması
         .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>() //Scaffold modeli oluşturucularını sağlar
         .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>() //EF Core çoğullama özelliğini sağlar
                                                                                      //bir veritabanındaki "Person" adlı bir tabloyu
                                                                                      //modellemek istediğinizde, Scaffold oluşturucusu
                                                                                      //"People" adlı bir varlık sınıfı oluşturacaktır.
         .AddSingleton<ProviderCodeGeneratorDependencies>() //gerekli bağımlılıkları sağlar. -EF Core sağlayıcı-
         .AddSingleton<AnnotationCodeGeneratorDependencies>() // erekli bağımlılıkları sağlar. -EF Core varlık-
         .BuildServiceProvider() //ServiceProvider=bir bileşenin ihtiyaç duyduğu diğer bileşenleri otomatik olarak oluşturan yapıdır.
         .GetRequiredService<IReverseEngineerScaffolder>();



  //derleme işlemi sırasında kullanılacak referansların oluşturulduğu bölümdür
  static List<MetadataReference> CompilationReferences(bool enableLazyLoading) {
    var refs = new List<MetadataReference>(); // referansları listeler
    var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
    refs.AddRange(referencedAssemblies.Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location)));


    // referanslar belirtilen türlerin ait olduğu DLL dosyalarının yerlerinden elde edilir
    refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
    refs.Add(MetadataReference.CreateFromFile(typeof(BackingFieldAttribute).Assembly.Location));
    refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location));
    refs.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
    refs.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));
    refs.Add(MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.KeyAttribute).Assembly.Location));
    refs.Add(MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute).Assembly.Location));

    if (enableLazyLoading) {
      refs.Add(MetadataReference.CreateFromFile(typeof(ProxiesExtensions).Assembly.Location)); // ekledi
    }

    return refs;
  }

  private static CSharpCompilation GenerateCode(List<string> sourceFiles, bool enableLazyLoading) {
    var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);

    var parsedSyntaxTrees = sourceFiles.Select(f => SyntaxFactory.ParseSyntaxTree(f, options));

    return CSharpCompilation.Create($"DataContext.dll",
        parsedSyntaxTrees,
        references: CompilationReferences(enableLazyLoading),
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release,
            assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
  }
}

public static class DynamicContextExtensions {
  public static IQueryable Query(this DbContext context, string entityName) =>
      context.Query(entityName, context.Model.FindEntityType(entityName).ClrType);

  static readonly MethodInfo SetMethod =
      typeof(DbContext).GetMethod(nameof(DbContext.Set), 1, new[] { typeof(string) }) ??
      throw new Exception($"Type not found: DbContext.Set");

  public static IQueryable Query(this DbContext context, string entityName, Type entityType) =>
      (IQueryable)SetMethod.MakeGenericMethod(entityType)?.Invoke(context, new[] { entityName }) ??
      throw new Exception($"Type not found: {entityType.FullName}");
}
