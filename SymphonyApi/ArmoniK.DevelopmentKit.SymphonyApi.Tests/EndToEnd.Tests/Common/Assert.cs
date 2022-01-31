using System;

namespace ArmoniK.EndToEndTests.Common
{
  public static class Assert
  {
    public static void AreEqual<T>(T expected, T value)
    {
      if (!expected.Equals(value))
        throw new ArgumentException($"Excpected {expected}\nBut was: {value}");
    }
  }
}