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
            // ロードが始まるまでは隠しておく。非アクティブでも購読は生きている。
            gameObject.SetActive(false);

            LoadingProgress.Instance.OperationCount.Where(_ => _ > 0).SubscribeAwait(async (_, ct) =>
            {
                await Show(LoadingProgress.Instance, ct);
            }, AwaitOperation.Drop).RegisterTo(destroyCancellationToken);
        }

        async Awaitable Show(TSKT.LoadingProgress progress, CancellationToken ct)
        {
            // 表示が終わるたびに破棄する。宣言と逆順に破棄されるので、購読が先に止まってからctsが破棄される。
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            using var subscription = progress.OperationCount.Where(_ => _ == 0).Take(1).DoCancelOnCompleted(cts).Subscribe();

            try
            {
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
