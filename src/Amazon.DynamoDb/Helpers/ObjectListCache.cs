﻿// Based on .NET Source code

using System.Collections.Generic;

namespace System.Text
{
    internal static class ObjectListCache<T>
    {
        private static List<List<T>> pool = new List<List<T>>();
        private static object lockObject = new object();

        internal struct Handle : IDisposable
        {
            public List<T> Value;

            public Handle(List<T> value)
            {
                Value = value;
            }

            public void Dispose()
            {
                Release(Value);
            }
        }

        public static Handle AcquireHandle()
        {
            lock (lockObject)
            {
                if (pool.Count == 0)
                {
                    return new Handle(new List<T>());
                }
                else
                {
                    var handle = new Handle(pool[pool.Count - 1]);
                    pool.RemoveAt(pool.Count - 1);
                    return handle;
                }
            }
        }

        public static void Release(List<T> list)
        {
            list.Clear();

            lock (lockObject)
            {
                pool.Add(list);
            }
        }
    }
}