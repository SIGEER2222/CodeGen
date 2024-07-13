using System.Reactive.Linq;
using DynamicRun.Builder;

namespace RoslynTest.DynamicRun;
public class DynamicRunTest
{
    public static async Task Test()
    {

        var sourcesPath = Path.Combine(Environment.CurrentDirectory, "Sources");

        Console.WriteLine($"Running from: {Environment.CurrentDirectory}");
        Console.WriteLine($"Sources from: {sourcesPath}");
        Console.WriteLine("Modify the sources to compile and run it!");

        using var watcher = new ObservableFileSystemWatcher(c => { c.Path = @$"D:\桌面\文档\DynamicRun"; });
        var changeDispose = watcher.Changed
            .Throttle(TimeSpan.FromSeconds(.5))
            .Where(c => c.FullPath.EndsWith("DynamicRun.txt"))
            .Select(c => c.FullPath)
            .Do(_ => Console.WriteLine("Changed"))
            .Subscribe(filepath => Runner.Execute(Compiler.Compile(filepath), new[] { "France" })); ;

        watcher.Start();

        Console.ReadLine();

        watcher.Stop();
    }
}