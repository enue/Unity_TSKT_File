#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if TSKT_FILE_UNIRX_SUPPORT
using UniRx;
#endif

namespace TSKT
{
    public class LoadingIndicator : MonoBehaviour
    {
        [SerializeField]
        GameObject window = default!;

        [SerializeField]
        Image value = default!;

        [SerializeField]
        float delay = 1f;

        float? time;

#if TSKT_FILE_UNIRX_SUPPORT
        void Start()
        {
            LoadingProgress.Instance.OperationCount.Subscribe(_ =>
            {
                gameObject.SetActive(_ > 0);
                if (_ == 0)
                {
                    time = null;
                }
            }).AddTo(this);
        }
        void Update()
        {
            var progress = LoadingProgress.Instance.GetProgress();
            if (progress > 0f)
            {
                if (!time.HasValue)
                {
                    time = Time.realtimeSinceStartup;
                }
                var elapsed = Time.realtimeSinceStartup - time;
                if (elapsed > delay)
                {
                    value.fillAmount = progress;
                }
            }
            else
            {
                time = null;
            }
        }
#endif
    }
}
