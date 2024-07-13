namespace RoslynTest.Buildalyzer;
internal static class BuildalyzerFile {
  public static async Task Mvvm(string projectPath = default) {
    AnalyzerManager manager = new();
    var analyzer = manager.GetProject(projectPath);
    AdhocWorkspace workspace = new();
    Project roslynProject = analyzer.AddToWorkspace(workspace);
    var documents = await roslynProject.NewQueryAsync();
    var vmClass = documents.SelectMany(x => x.GetAllPartialClasses()).Where(x => x.GetBaseTypes().Any(x => x.Contains("CommonFormVM")));

    string folderPath = Path.Combine(Path.GetDirectoryName(projectPath), "Generated");
    if (folderPath is not null && Directory.Exists(folderPath))
      Directory.Delete(folderPath, true);

    foreach (var vm in vmClass) {
      var classCode = SyntaxFactory.ParseMemberDeclaration("""
/// <summary>
/// </summary>
public partial class __TypeName__ : INotifyPropertyChanged, INotifyPropertyChanging, ISupportServices  {
	private static readonly Lazy<__TypeName__> _instanceOf = new Lazy<__TypeName__>(
		() => new __TypeName__(),
		LazyThreadSafetyMode.None);
	public static __TypeName__ Instance => _instanceOf.Value;

  IServiceContainer? serviceContainer;
  protected IServiceContainer ServiceContainer { get => serviceContainer ??= new ServiceContainer(this); }
  IServiceContainer ISupportServices.ServiceContainer { get => ServiceContainer; }
  protected T? GetService<T>() where T : class => ServiceContainer.GetService<T>();
  protected T GetRequiredService<T>() where T : class => ServiceContainer.GetRequiredService<T>();

  public event PropertyChangedEventHandler? PropertyChanged;
  public event PropertyChangingEventHandler? PropertyChanging;
  protected void RaisePropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
  protected void RaisePropertyChanging(PropertyChangingEventArgs e) => PropertyChanging?.Invoke(this, e);

}
""");

      var path = vm.SourceFile.SyntaxTree.FilePath.Replace("ManuTalent.Mom.Operation.Winform.Host\\", "ManuTalent.Mom.Operation.Winform.Host\\Generated\\").Replace(".cs", ".g.cs");

      var vmNamespace = vm.Namespace;

      var lstCode = new List<string>();

      var motheds = vm.GetMethods().Select(x => x.Name).ToList();

      foreach (var field in vm.GetFields()) {
        var type = field.Type;
        var name = field.Name;
        var newName = RoslynTest.Helps.StringHelp.CapitalizeFirstLetter(name);
        if (newName == name) continue;
        var changedMethod = motheds.Contains($"On{newName}Changed") ? $"On{newName}Changed()" : "";
        var code = $@"
        public {type} {newName} {{
            get => {name};
            set {{
                if(EqualityComparer<{type}>.Default.Equals({name}, value)) return;
                RaisePropertyChanging({name}ChangingEventArgs);
                {name} = value;
                RaisePropertyChanged({name}ChangedEventArgs);
                {changedMethod};
            }}
        }}
        static PropertyChangedEventArgs {name}ChangedEventArgs = new PropertyChangedEventArgs(nameof({newName}));
        static PropertyChangingEventArgs {name}ChangingEventArgs = new PropertyChangingEventArgs(nameof({newName}));
";
        lstCode.Add(code);
      }

      var modifiedClassCode = FluentCSharpSyntaxRewriter
             .Define()
             .WithVisitToken((_, token) => {
               if (token.IsKind(SyntaxKind.IdentifierToken) &&
                         string.Equals(token.ValueText, "__TypeName__", StringComparison.Ordinal))
                 return SyntaxFactory.Identifier(vm.Name).WithTriviaFrom(token);
               return token;
             })
             .Visit(classCode)
             .ParseMemberDeclaration();

      var modifiedClass = FluentCSharpSyntaxRewriter
            .Define()
            .WithVisitClassDeclaration((_, cls) => {
              cls = cls.AddMembers(lstCode.Select(x => SyntaxFactory.ParseMemberDeclaration(x)).ToArray());
              return cls;
            })
            .Visit(modifiedClassCode)
            .ParseMemberDeclaration();



      var namespaceCode = CSharpSyntaxTree.ParseText("""
namespace __ProjectNamespace__ {
}
""").GetRoot();

      var modifiedNamespaceCode = FluentCSharpSyntaxRewriter
            .Define()
            .WithVisitNamespaceDeclaration((_, ns) => {
              ns = ns.AddUsings(vm.SourceFile.Usings).OrderUsings().DistinctUsings().RenameMember(_ => vmNamespace);
              ns = ns.AddMembers(modifiedClass);
              return ns;
            })
            .Visit(namespaceCode)
            .ToFullStringCSharp();

      string directoryPath = Path.GetDirectoryName(path);

      if (!Directory.Exists(directoryPath)) {
        Directory.CreateDirectory(directoryPath);
      }

      File.WriteAllText(path, modifiedNamespaceCode.ToString());
    }
  }
}
