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
            NotFound,
            Error,
            FailedDeserialize,
        }
    }

    public readonly struct LoadResult<T>
    {
        public readonly LoadResult.State state;
        public readonly T value;
        public readonly System.Exception exception;

        public LoadResult(T value, LoadResult.State state, System.Exception exception)
        {
            this.state = state;
            this.value = value;
            this.exception = exception;
        }

        public LoadResult(T value)
        {
            state = LoadResult.State.Succeeded;
            this.value = value;
            exception = null;
        }

        public LoadResult<S> CreateFailed<S>()
        {
            Debug.Assert(!Succeeded);
            return new LoadResult<S>(default, state, exception);
        }

        public bool Succeeded => state == LoadResult.State.Succeeded;

        public static LoadResult<T> CreateNotFound(System.Exception ex = null)
        {
            return new LoadResult<T>(default, LoadResult.State.NotFound, ex);
        }
        public static LoadResult<T> CreateFailedDeserialize(System.Exception ex = null)
        {
            return new LoadResult<T>(default, LoadResult.State.FailedDeserialize, ex);
        }
        public static LoadResult<T> CreateError(System.Exception ex = null)
        {
            return new LoadResult<T>(default, LoadResult.State.Error, ex);
        }
    }
}
