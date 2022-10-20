// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ArmoniK.DevelopmentKit.Common.Extensions;

/// <summary>
///   Add Chunk Method for dotnet version which doesn't have the Method
///   Chunk split the IEnumerable in several IEnumerable
///   Original source code : https://github.com/dotnet/runtime/blob/main/src/libraries/System.Linq/src/System/Linq/Chunk.cs
/// </summary>
public static class IEnumerableExt
{
  /// <summary>
  ///   Split the elements of a sequence into chunks of size at most <paramref name="size" />.
  /// </summary>
  /// <remarks>
  ///   Every chunk except the last will be of size <paramref name="size" />.
  ///   The last chunk will contain the remaining elements and may be of a smaller size.
  /// </remarks>
  /// <param name="source">
  ///   An <see cref="IEnumerable{T}" /> whose elements to chunk.
  /// </param>
  /// <param name="size">
  ///   Maximum size of each chunk.
  /// </param>
  /// <typeparam name="TSource">
  ///   The type of the elements of source.
  /// </typeparam>
  /// <returns>
  ///   An <see cref="IEnumerable{T}" /> that contains the elements the input sequence split into chunks of size
  ///   <paramref name="size" />.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  ///   <paramref name="source" /> is null.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  ///   <paramref name="size" /> is below 1.
  /// </exception>
  public static IEnumerable<TSource[]> ToChunk<TSource>(this IEnumerable<TSource> source,
                                                        int                       size)
  {
    if (source == null)
    {
      throw new ArgumentNullException(nameof(source));
    }

    if (size < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(size));
    }

    return ChunkIterator(source,
                         size);
  }

  private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source,
                                                               int                  size)
  {
    using var e = source.GetEnumerator();

    // Before allocating anything, make sure there's at least one element.
    if (e.MoveNext())
    {
      // Now that we know we have at least one item, allocate an initial storage array. This is not
      // the array we'll yield.  It starts out small in order to avoid significantly overallocating
      // when the source has many fewer elements than the chunk size.
      var arraySize = Math.Min(size,
                               4);
      int i;
      do
      {
        var array = new TSource[arraySize];

        // Store the first item.
        array[0] = e.Current;
        i        = 1;

        if (size != array.Length)
        {
          // This is the first chunk. As we fill the array, grow it as needed.
          for (; i < size && e.MoveNext(); i++)
          {
            if (i >= array.Length)
            {
              arraySize = (int)Math.Min((uint)size,
                                        2 * (uint)array.Length);
              Array.Resize(ref array,
                           arraySize);
            }

            array[i] = e.Current;
          }
        }
        else
        {
          // For all but the first chunk, the array will already be correctly sized.
          // We can just store into it until either it's full or MoveNext returns false.
          var local = array; // avoid bounds checks by using cached local (`array` is lifted to iterator object as a field)
          Debug.Assert(local.Length == size);
          for (; (uint)i < (uint)local.Length && e.MoveNext(); i++)
          {
            local[i] = e.Current;
          }
        }

        if (i != array.Length)
        {
          Array.Resize(ref array,
                       i);
        }

        yield return array;
      } while (i >= size && e.MoveNext());
    }
  }

  /// <summary>
  ///   Convert IEnumerable byte to IEnumerable double
  /// </summary>
  /// <param name="arr"></param>
  /// <returns></returns>
  public static IEnumerable<double> ConvertToArray(this IEnumerable<byte> arr)
  {
    var bytes = arr as byte[] ?? arr.ToArray();

    var values = new double[bytes.Count() / sizeof(double)];

    for (var i = 0; i < values.Length; i++)
    {
      values[i] = BitConverter.ToDouble(bytes,
                                        i * 8);
    }

    return values;
  }

  /// <summary>
  ///   returns an HashSet. Useful to align missing method other than dotnet core
  /// </summary>
  /// <param name="source">The IEnumarable source to convert a new HashSet object</param>
  /// <param name="comparer">The comparer element</param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static HashSet<T> ToHashSet<T>(this IEnumerable<T>  source,
                                        IEqualityComparer<T> comparer = null)
    => new(source,
           comparer);
}
