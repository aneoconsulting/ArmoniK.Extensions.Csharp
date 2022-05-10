using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.Common
{
  /// <summary>
  /// 
  /// </summary>
  public static class IEnumerableExt
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="items"></param>
    /// <param name="maxItems"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,
                                                       int                 maxItems)
      {
        return items.Select((item, inx) => new
                    {
                      item,
                      inx
                    })
                    .GroupBy(x => x.inx / maxItems)
                    .Select(g => g.Select(x => x.item));
      }
    }
}
