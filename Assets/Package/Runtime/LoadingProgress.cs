#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public class LoadingProgress
    {
        interface IItem
        {
            float Progress { get; }
            bool IsDone { get; }
        }

        class AsyncOperationItem : IItem
        {
            readonly AsyncOperation operation;
            readonly float max;

            public AsyncOperationItem(AsyncOperation operation, float max)
            {
                this.operation = operation;
                this.max = max;
            }
            public float Progress => Mathf.Clamp01(operation.progress / max);
            public bool IsDone => operation.isDone;
        }

        class ProgressItem : System.IProgress<float>, IItem
        {
            public float Progress { get; private set; }
            public bool IsDone => Progress >= 1f;

            public void Report(float value)
            {
                if (Progress < value)
                {
                    Progress = value;
                }
            }
        }

        static LoadingProgress? instance;
        static public LoadingProgress Instance => instance ??= new LoadingProgress();

        readonly List<IItem> operations = new List<IItem>();
        float fixedTotalProgress = 0f;
        float fixedProgress = 0f;

        LoadingProgress()
        {
            // nop;
        }

        public void Add(AsyncOperation operation, float max = 1f)
        {
            Add(new AsyncOperationItem(operation, max));
        }

        public System.IProgress<float> Add()
        {
            var item = new ProgressItem();
            Add(item);
            return item;
        }
 
        void Add(IItem item)
        {
            fixedProgress = GetProgress(out var totalProgress);
            if (fixedProgress == 1f)
            {
                fixedProgress = 0f;
            }
            fixedTotalProgress = totalProgress;

            operations.Add(item);
        }

        float GetProgress(out float totalProgress)
        {
            if (operations.Count == 0)
            {
                totalProgress = 0f;
                return 1f;
            }
            if (operations.TrueForAll(_ => _.IsDone))
            {
                operations.Clear();

                totalProgress = 0f;
                return 1f;
            }

            totalProgress = operations.Sum(_ => _.Progress);

            var min = fixedTotalProgress;
            var max = operations.Count;
            var t = Mathf.InverseLerp(min, max, totalProgress);
            return Mathf.Lerp(fixedProgress, 1f, t);
        }

        public float GetProgress()
        {
            return GetProgress(out _);
        }
    }
}
