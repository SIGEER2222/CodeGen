using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynTest.Helps;
public static class StringHelp {

  public static string CapitalizeFirstLetter(string input) {
    if (string.IsNullOrEmpty(input)) {
      return input;
    }

    char firstChar = input[0];
    if (firstChar == '_') {
      return char.ToUpper(input[1]) + input.Substring(2);
    }
    else {
      return char.ToUpper(firstChar) + input.Substring(1);
    }
  }
}
