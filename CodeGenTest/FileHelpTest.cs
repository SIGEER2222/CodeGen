using FluentSyntaxRewriter.Test.Help;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentSyntaxRewriter.Test;
public class FileHelpTest {
  [Fact]
  public void FilePathTest() {
    var path = TestHelps.winformProjectPath;
    var projectName = Path.GetFileNameWithoutExtension(path);
    var fileName = Path.GetFileName(path);
    var fileType = Path.GetExtension(path);
    var generatedPath = Path.Combine(Path.GetDirectoryName(path), "Generated");
    var generatedFilePath = Path.Combine(generatedPath, projectName + ".Generated.cs");

    Assert.Equal(@"D:\MyWork\MOM新版\mom.solution.sln\momwinform\host\ManuTalent.Mom.Operation.Winform.Host\Generated\ManuTalent.Mom.Operation.Winform.Host.Generated.cs", generatedFilePath);
    Assert.Equal("ManuTalent.Mom.Operation.Winform.Host", projectName);
    Assert.Equal(@"D:\MyWork\MOM新版\mom.solution.sln\momwinform\host\ManuTalent.Mom.Operation.Winform.Host\Generated", generatedPath);
    Assert.Equal("ManuTalent.Mom.Operation.Winform.Host.csproj", fileName);
    Assert.Equal(".csproj", fileType);
  }

  [Theory]
  [InlineData(@"D:\MyWork\Mom开发\mom.solution.sln")]
  public void DeleteObjBinDirectoriesTest(string directoryPath) {
    string[] subDirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);

    foreach (string subDir in subDirectories) {
      string directoryName = new DirectoryInfo(subDir).Name.ToLower();

      if (directoryName == "obj" || directoryName == "bin") {
        Directory.Delete(subDir, true);
      }
    }
  }
}
