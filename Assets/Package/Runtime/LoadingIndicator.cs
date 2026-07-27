#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Threading;

namespace TSKT
{
    public class LoadingIndicator : MonoBehaviour
    {
        [SerializeField]
        Image value = default!;

        [SerializeField]
        float delay = 1f;

        void Start()
        {
            LoadingProgress.Instance.OperationCount.Where(_ => _ > 0).SubscribeAwait(async (_, ct) =>
            {
                await Show(LoadingProgress.Instance, ct);
            }, AwaitOperation.Drop).RegisterTo(destroyCancellationToken);
        }

        async Awaitable Show(TSKT.LoadingProgress progress, CancellationToken ct)
        {
            try
            {
                await Observable.Timer(System.TimeSpan.FromSeconds(delay)).FirstAsync(ct);

                while (progress.OperationCount.CurrentValue > 0)
                {
                    gameObject.SetActive(true);
                    value.fillAmount = progress.GetProgress();

                    await Awaitable.NextFrameAsync(ct);
                }
            }
            catch (System.OperationCanceledException)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
