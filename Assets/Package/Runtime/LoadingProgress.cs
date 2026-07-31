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
            float IndicatorMax { get; }
        }

        class AsyncOperationItem : IItem
        {
            readonly AsyncOperation operation;
            public float IndicatorMax { get; }

            public AsyncOperationItem(AsyncOperation operation, float max)
            {
                this.operation = operation;
                IndicatorMax = max;
            }
            public float Progress => operation.progress;
            public bool IsDone => operation.isDone;
        }

        class ProgressItem : IItem
        {
            public float Progress { get; private set; }
            public bool IsDone => Progress >= 1f;
            public float IndicatorMax => 1f;

            public ProgressItem(System.Progress<float> progress)
            {
                progress.ProgressChanged += (_, value) =>
                {
                    Progress = value;
                };
            }
        }

        static LoadingProgress? instance;
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            instance?.operationCount.Dispose();
            instance = null;
        }
#endif
        public static LoadingProgress Instance => instance ??= new();

        readonly List<IItem> operations = new();
        float start = 0f;
        float normalizedStart = 0f;
        float previousNormalizedValue = 0f;
        float previousTotalValue = 0f;

        readonly ReactiveProperty<int> operationCount = new(0);
        public ReadOnlyReactiveProperty<int> OperationCount => operationCount;

        LoadingProgress()
        {
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
            TryClear();

            normalizedStart = previousNormalizedValue;
            start = previousTotalValue;

            operations.Add(item);
            operationCount.Value = operations.Count;
        }

        bool TryClear()
        {
            if (operations.TrueForAll(_ => _.IsDone) && operations.Any(_ => _.IndicatorMax >= 1f))
            {
                operations.Clear();
                operationCount.Value = 0;

                previousNormalizedValue = 0f;
                previousTotalValue = 0f;

                return true;
            }
            return false;
        }


        public float GetProgress()
        {
            if (operations.Count == 0)
            {
                previousNormalizedValue = 0f;
                previousTotalValue = 0f;

                return 1f;
            }
            if (TryClear())
            {
                return 1f;
            }

            var total = operations.Sum(_ => _.Progress);
            previousTotalValue = total;

            var indicatorMax = operations.Max(_ => _.IndicatorMax);

            var min = start;
            var max = operations.Count;
            var t = Mathf.InverseLerp(min, max, total);
            var normalized = Mathf.Lerp(normalizedStart, indicatorMax, t);
            previousNormalizedValue = normalized;

            return normalized;
        }
    }
}
