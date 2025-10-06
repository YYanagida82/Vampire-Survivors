using UnityEngine;

public class PickupNuke : Pickup
{
    [Header("Nuke Settings")]
    public float nukeRange = 30f; // 効果範囲
    public AudioClip nukeSound;
    public ParticleSystem explosionEffect;
    
    private void OnDrawGizmosSelected()
    {
        // ギズモの色を設定（目立つ赤色に設定）
        Gizmos.color = Color.red;

        // ギズモの描画処理は、このコンポーネントがアタッチされているオブジェクト（Nukeアイテム）の位置を基準に行う
        Vector3 position = transform.position;

        // ワイヤーフレームの球体（2Dゲームなので円として表示される）を描画する
        Gizmos.DrawWireSphere(position, nukeRange);
    }

    protected override void OnDisable()
    {
        if (target == null) return;

        // 敵のレイヤーマスクを取得
        int enemyLayer = LayerMask.GetMask("Enemy");

        // 範囲内のコライダーを取得
        Collider2D[] colliders = Physics2D.OverlapCircleAll(target.transform.position, nukeRange, enemyLayer);

        foreach (var hit in colliders)
        {
            // EnemyStatsコンポーネントを取得
            EnemyStats e = hit.GetComponent<EnemyStats>();
            if (e == null) continue;

            // 処理落ちを避けるため、Kill()を直接呼ぶ
            e.Kill();
        }

        if (explosionEffect)
            Instantiate(explosionEffect, target.transform.position, Quaternion.identity);

        if (nukeSound)
            AudioSource.PlayClipAtPoint(nukeSound, target.transform.position);

        base.OnDisable();
    }
}
