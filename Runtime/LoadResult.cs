using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public static class LoadResult
    {
        public enum State
        {
            Succeeded,
            FailedOpen,
            NotFound,
            FailedDeserialize,
        }
    }

    public readonly struct LoadResult<T>
    {
        public readonly LoadResult.State state;
        public readonly T value;

        public LoadResult(T value, LoadResult.State state)
        {
            this.state = state;
            this.value = value;
        }

        public LoadResult(T value)
        {
            state = LoadResult.State.Succeeded;
            this.value = value;
        }

        public LoadResult<S> CreateInvalid<S>()
        {
            Debug.Assert(state != LoadResult.State.Succeeded);
            return new LoadResult<S>(default, state);
        }

        public static LoadResult<T> FileNotFound => new LoadResult<T>(default, LoadResult.State.NotFound);
        public static LoadResult<T> FailedOpen => new LoadResult<T>(default, LoadResult.State.FailedOpen);
        public static LoadResult<T> FailedDeserialize => new LoadResult<T>(default, LoadResult.State.FailedDeserialize);
    }
}
