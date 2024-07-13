using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using RoslynTest.Buildalyzer;

var file = new Argument<FileInfo>("script", "测试").ExistingOnly();
var watch = new Option<bool>(new[] { "--watch", "-w" }, "进行中");
var verbose = new Option<bool>(new[] { "--verbose", "-v" }, "进行中");

var command = new RootCommand("从项目进行代码生成") { file, watch, verbose };
command.SetHandler<FileInfo, bool, bool>(Run, file, watch, verbose);

return await command.InvokeAsync(args);

static async Task Run(FileInfo script, bool watch, bool verbose) {
  var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
  await BuildalyzerFile.Mvvm(script.FullName);
}
