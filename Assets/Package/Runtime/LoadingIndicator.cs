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
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                progress.OperationCount.Where(_ => _ == 0).TakeUntil(ct).Take(1).DoCancelOnCompleted(cts);

                await Observable.Timer(System.TimeSpan.FromSeconds(delay)).FirstAsync(cts.Token);

                gameObject.SetActive(true);

                while (true)
                {
                    value.fillAmount = progress.GetProgress();
                    await Awaitable.NextFrameAsync(cts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // nop
            }
            gameObject.SetActive(false);
        }
    }
}
