using System;
using System.Collections.Generic;
using System.Text;

namespace Knotty.Core.Extensions
{
    internal static class QueueExtensions
    {
#if NETSTANDARD2_0 || NET462
        public static bool TryDequeue<T>(this Queue<T> queue, out T result)
        {
            if (queue.Count > 0)
            {
                result = queue.Dequeue ();
                return true;
            }
            result = default!;
            return false;
        }
#endif
    }
}
