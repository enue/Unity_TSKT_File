using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public class LoadingProgress
    {
        static LoadingProgress instance;
        static public LoadingProgress Instance
        {
            get
            {
                return instance ?? (instance = new LoadingProgress());
            }
        }

        readonly List<(AsyncOperation operation, float max)> operations = new List<(AsyncOperation, float)>();

        LoadingProgress()
        {
            // nop;
        }

        public void Add(AsyncOperation operation, float max = 1f)
        {
            if (operations.TrueForAll(_ => _.operation.isDone))
            {
                operations.Clear();
            }

            operations.Add((operation, max));
        }

        public float GetProgress()
        {
            if (operations.Count == 0)
            {
                return 1f;
            }

            if (operations.TrueForAll(_ => _.operation.isDone))
            {
                operations.Clear();
            }

            if (operations.Count == 0)
            {
                return 1f;
            }

            var totalProgress = 0f;

            foreach (var (operation, max) in operations)
            {
                totalProgress += Mathf.Clamp01(operation.progress / max);
            }

            return totalProgress / operations.Count;
        }
    }
}
