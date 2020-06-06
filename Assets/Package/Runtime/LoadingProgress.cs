using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public class LoadingProgress
    {
        static LoadingProgress instance;
        static public LoadingProgress Instance => instance ?? (instance = new LoadingProgress());

        readonly List<Files.IProgress> operations = new List<Files.IProgress>();

        LoadingProgress()
        {
            // nop;
        }

        public void Add(Files.IProgress operation)
        {
            if (operations.TrueForAll(_ => _.isDone))
            {
                operations.Clear();
            }

            operations.Add(operation);
        }

        public float GetProgress()
        {
            if (operations.Count == 0)
            {
                return 1f;
            }

            if (operations.TrueForAll(_ => _.isDone))
            {
                operations.Clear();
            }

            if (operations.Count == 0)
            {
                return 1f;
            }

            var totalProgress = 0f;

            foreach (var it in operations)
            {
                totalProgress += it.progress;
            }

            return totalProgress / operations.Count;
        }
    }
}
