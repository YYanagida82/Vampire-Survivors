using UnityEngine;

// Trail RendererとParticle Systemの両方を扱うため、これらが必須
[RequireComponent(typeof(ParticleSystem))]
public class ParticleAutoReturnToPool : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Updateの代わりに、ParticleSystemのライフサイクルイベントを利用するのが望ましいが、
    // シンプルにUpdateで終了をチェックする。
    void Update()
    {
        // パーティクルシステムがアクティブでなく、かつ既に再生を停止している場合
        // isAliveで現在粒子が存在するかどうかをチェックする
        if (!ps.IsAlive(true) && ps.isPlaying == false)
        {
            // ObjectPoolに返却
            // このオブジェクトは非アクティブ化され、リセット処理(Clearなど)が実行される
            ObjectPool.instance.Return(gameObject);
        }
    }
}