using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace TSKT
{
    public class WebGLUtil
    {
        // https://docs.unity3d.com/ja/2022.1/Manual/webgl-interactingwithbrowserscripting.html
        // https://docs.unity3d.com/ja/2022.1/Manual/webgl-debugging.html
#if UNITY_WEBGL
        [DllImport("__Internal")]
        public static extern void SyncFS();
#else
        public static void SyncFS()
        {
            // nop
        }
#endif
    }
}
