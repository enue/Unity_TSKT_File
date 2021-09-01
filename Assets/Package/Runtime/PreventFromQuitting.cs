#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public class PreventFromQuitting : System.IDisposable
    {
        readonly System.Action? action;

        public PreventFromQuitting(System.Action? action)
        {
            this.action = action;
            Application.wantsToQuit += Prevent;
        }

        public void Dispose()
        {
            Application.wantsToQuit -= Prevent;
        }

        bool Prevent()
        {
            action?.Invoke();
            return false;
        }
    }
}
