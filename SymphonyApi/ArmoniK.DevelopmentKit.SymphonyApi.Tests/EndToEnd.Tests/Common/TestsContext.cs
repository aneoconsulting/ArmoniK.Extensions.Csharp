#nullable enable
using System;
using System.Reflection;

namespace ArmoniK.EndToEndTests.Common
{

  public class TestContext
  {
    public Type ClassCLient { get; set; }
    public Type ClassServiceContainer { get; set; }
    public String NameSpaceTest { get; set; }
    public object? ClientClassInstance { get; set; }
    public MethodInfo[] MethodTests { get; set; }
  }
}