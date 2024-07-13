using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynTest.Helps;
public static class RoslynFileHelps {
  public static string ProjectPath;
  public static string projectName => Path.GetFileNameWithoutExtension(ProjectPath);
  public static string fileName => Path.GetFileName(ProjectPath);
  public static string fileType => Path.GetExtension(ProjectPath);
  public static string GeneratedPath => Path.Combine(Path.GetDirectoryName(ProjectPath), "Generated");
  public static string GetProjectPath(DirectoryInfo path) {
    if (path is null) {
      Console.WriteLine("Path is null");
      return string.Empty;
    }
    var fileName = Path.GetFileName(path.FullName);
    var fileType = Path.GetExtension(path.FullName);

    if (fileType != ".csproj") {
      Console.WriteLine("File type is not .csproj");
      return string.Empty;
    }

    var projectName = Path.GetFileNameWithoutExtension(path.FullName);
    var generatedPath = Path.Combine(Path.GetDirectoryName(path.FullName), "Generated");
    ProjectPath = fileName;
    return fileName;
  }
}
