#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using R3;

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
        float start = 0f;
        float normalizedStart = 0f;
        readonly ReactiveProperty<int> operationCount = new(0);
        public ReadOnlyReactiveProperty<int> OperationCount => operationCount;

        LoadingProgress()
        {
            // nop;
        }

        public void Observe(AsyncOperation operation, float max = 1f)
        {
            Observe(new AsyncOperationItem(operation, max));
        }

        public System.IProgress<float> Add()
        {
            var result = new System.Progress<float>();
            var item = new ProgressItem(result);
            Observe(item);
            return result;
        }
 
        void Observe(IItem item)
        {
            if (TryGetProgress(out var normalized, out var total))
            {
                normalizedStart = normalized;
                start = total;
            }
            else
            {
                normalizedStart = 0f;
                start = 0f;
            }

            operations.Add(item);
            operationCount.Value = operations.Count;
        }

        bool TryGetProgress(out float normalized, out float total)
        {
            if (operations.Count == 0)
            {
                normalized = 0f;
                total = 0f;
                return false;
            }
            if (operations.TrueForAll(_ => _.IsDone))
            {
                operations.Clear();
                operationCount.Value = 0;

                normalized = 0f;
                total = 0f;
                return false;
            }

            total = operations.Sum(_ => _.Progress);

            var min = start;
            var max = operations.Count;
            var t = Mathf.InverseLerp(min, max, total);
            normalized = Mathf.Lerp(normalizedStart, 1f, t);
            return true;
        }

        public float GetProgress()
        {
            if (TryGetProgress(out var normalized, out _))
            {
                return normalized;
            }
            return 1f;
        }
    }
}
