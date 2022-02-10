//TODO : remove pragma

using JetBrains.Annotations;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer
{
  public class FallBackServerAdder
    {


      [UsedImplicitly]
      public double Add(double a, double b)
      {
        return a + b;
      }

      [UsedImplicitly]
      public double Square(double a)
      {
        return a * a;
      }
    }
}
