#nullable enable
using System;
using System.Reflection;

namespace ArmoniK.EndToEndTests.Common
{

  public class TestContext
  {
    public Type? ClassClient { get; set; }
    public string? NameSpaceTest { get; set; }
    public object? ClientClassInstance { get; set; }
    public MethodInfo[]? MethodTests { get; set; }
  }
}