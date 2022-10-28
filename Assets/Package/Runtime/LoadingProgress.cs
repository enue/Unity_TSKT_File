#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if TSKT_FILE_UNIRX_SUPPORT
using UniRx;
#endif
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

        class ProgressItem : IItem
        {
            public float Progress { get; private set; }
            public bool IsDone => Progress >= 1f;

            public ProgressItem(System.Progress<float> progress)
            {
                progress.ProgressChanged += (_, value) =>
                {
                    Progress = value;
                };
            }
        }

        static LoadingProgress? instance;
        public static LoadingProgress Instance => instance ??= new();

        readonly List<IItem> operations = new();
        float fixedTotalProgress = 0f;
        float fixedProgress = 0f;
#if TSKT_FILE_UNIRX_SUPPORT
        public ReactiveProperty<int> OperationCount { get; } = new(0);
#endif
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
            var result = new System.Progress<float>();
            var item = new ProgressItem(result);
            Add(item);
            return result;
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
#if TSKT_FILE_UNIRX_SUPPORT
            OperationCount.Value = operations.Count;
#endif
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
#if TSKT_FILE_UNIRX_SUPPORT
                OperationCount.Value = 0;
#endif

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
