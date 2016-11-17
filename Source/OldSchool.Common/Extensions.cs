using System;
using System.Collections.Generic;

namespace OldSchool.Common
{
    public static class Extensions
    {
        public static void Each<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
                action(item);
        }
    }
}