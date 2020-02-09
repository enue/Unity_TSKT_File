using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public readonly struct Option<T>
    {
        public readonly bool hasValue;
        public readonly T value;

        Option(bool hasValue, T value)
        {
            this.hasValue = hasValue;
            this.value = value;
        }

        public Option(T value)
        {
            hasValue = true;
            this.value = value;
        }

        public static Option<T> Empty => new Option<T>(false, default);
    }
}
