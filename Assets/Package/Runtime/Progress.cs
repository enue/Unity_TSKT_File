using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT.Files
{
    public interface IProgress
    {
        float progress { get; }
        bool isDone { get; }
    }

    public class Progress : System.IProgress<float>, IProgress
    {
        readonly float max;
        public float progress { get; private set; }
        public bool isDone => progress >= 1f;

        public Progress(float max)
        {
            this.max = max;
        }

        public void Report(float value)
        {
            progress = Mathf.Clamp01(value / max);
        }
    }

    public class AsyncOperationProgress : IProgress
    {
        readonly float max;
        readonly AsyncOperation operation;

        public float progress => Mathf.Clamp01(operation.progress / max);
        public bool isDone => operation.isDone;

        public AsyncOperationProgress(AsyncOperation operation, float max)
        {
            this.operation = operation;
            this.max = max;
        }
    }
}
