using UnityEngine;

public class PickupMagnet : Pickup
{
    [Header("Magnet Settings")]
    public float magnetRange = 20f; // 効果範囲
    public float magnetSpeed = 15f; // 吸い寄せ速度
    public AudioClip magnetSound;

    private void OnDrawGizmosSelected()
    {
        // ギズモの色を設定（目立つ赤色に設定）
        Gizmos.color = Color.red;

        // ギズモの描画処理は、このコンポーネントがアタッチされているオブジェクト（Nukeアイテム）の位置を基準に行う
        Vector3 position = transform.position;

        // ワイヤーフレームの球体（2Dゲームなので円として表示される）を描画する
        Gizmos.DrawWireSphere(position, magnetRange);
    }

    protected override void OnDisable()
    {
        if (target == null) return;

        // 全Pickupを探す
        Pickup[] allPickups = FindObjectsByType<Pickup>(FindObjectsSortMode.None);

        foreach (Pickup p in allPickups)
        {
            if (p.type == PickupType.Experience)
            {
                p.Collect(target, magnetSpeed);
            }
        }

        if (magnetSound) AudioSource.PlayClipAtPoint(magnetSound, transform.position);

        base.OnDisable();
    }
}
