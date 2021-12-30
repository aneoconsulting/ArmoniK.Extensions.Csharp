using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.GridServer
{
    public class FallBackServerAdder
    {


      public double Add(double a, double b)
      {
        return a + b;
      }

      public double Square(double a)
      {
        return a * a;
      }
    }
}
