using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(ParticleSystem))]
public class ObjectPoolReturner : MonoBehaviour
{
    // ParticleSystemの再生が終了したときにUnityから自動で呼ばれるコールバック
    void OnParticleSystemStopped()
    {
        // ObjectPoolのインスタンスが存在し、かつ自身がアクティブな状態の場合にプールに戻す
        if (ObjectPool.instance && gameObject.activeInHierarchy)
        {
            ObjectPool.instance.Return(gameObject);
        }
    }
}