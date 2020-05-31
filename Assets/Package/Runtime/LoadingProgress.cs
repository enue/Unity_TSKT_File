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

        readonly List<AsyncOperation> operations = new List<AsyncOperation>();

        LoadingProgress()
        {
            // nop;
        }

        public void Add(AsyncOperation operation)
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
                if (it is ResourceRequest)
                {
                    totalProgress += it.progress;
                }
                else
                {
                    totalProgress += it.progress / 0.9f;
                }
            }

            return totalProgress / operations.Count;
        }
    }
}
