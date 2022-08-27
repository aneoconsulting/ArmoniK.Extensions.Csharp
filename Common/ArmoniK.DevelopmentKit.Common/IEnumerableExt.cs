using System;
using System.Collections.Generic;

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
    /// <param name="source"></param>
    /// <param name="size"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source,
                                                       int                 size)
    {
      if (source == null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      if (size < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(size),
                                              "Should be at least 1");
      }

      return ChunkIterator(source,
                           size);
    }

    private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
    {
      using IEnumerator<TSource> e = source.GetEnumerator();
      while (e.MoveNext())
      {
        TSource[] chunk = new TSource[size];
        chunk[0] = e.Current;

        int i = 1;
        for (; i < chunk.Length && e.MoveNext(); i++)
        {
          chunk[i] = e.Current;
        }

        if (i == chunk.Length)
        {
          yield return chunk;
        }
        else
        {
          Array.Resize(ref chunk,
                       i);
          yield return chunk;
          yield break;
        }
      }
    }
  }
}