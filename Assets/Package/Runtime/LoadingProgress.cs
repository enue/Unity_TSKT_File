using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TSKT
{
    public class LoadingProgress
    {
        static LoadingProgress instance;
        static public LoadingProgress Instance => instance ?? (instance = new LoadingProgress());

        readonly List<(AsyncOperation operation, float max)> operations = new List<(AsyncOperation, float)>();
        float fixedTotalProgress = 0f;
        float fixedProgress = 0f;

        LoadingProgress()
        {
            // nop;
        }

        public void Add(AsyncOperation operation, float max = 1f)
        {
            fixedProgress = GetProgress(out var totalProgress);
            if (fixedProgress == 1f)
            {
                fixedProgress = 0f;
            }
            fixedTotalProgress = totalProgress;

            operations.Add((operation, max));
        }

        float GetProgress(out float totalProgress)
        {
            if (operations.Count == 0)
            {
                totalProgress = 0f;
                return 1f;
            }
            if (operations.TrueForAll(_ => _.operation.isDone))
            {
                operations.Clear();

                totalProgress = 0f;
                return 1f;
            }

            totalProgress = operations.Sum(_ => Mathf.Clamp01(_.operation.progress / _.max));

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
