using System.Collections.Generic;
using UnityEngine;

public class LightningRingWeapon : ProjectileWeapon
{
    // シーン上にいる攻撃対象の全敵を保持するリスト
    List<EnemyStats> allSelectedEnemies = new List<EnemyStats>();
    // 物理演算の結果を格納するためのリストを用意（GCスパイク対策）
    private Collider2D[] overlapResults = new Collider2D[100];

    protected override bool Attack(int attackCount = 1)
    {
        if (!currentStats.hitEffect)
        {
            ActivateCooldown(true);
            return false;
        }

        if (!CanAttack()) return false;

        // 新しい攻撃の前にカウンターをリセット
        if (currentCooldown <= 0)
        {
            // パフォーマンス向上のため、FindObjectsByTypeの使用を避ける
            allSelectedEnemies.Clear();
            foreach (var enemy in EnemyStats.activeEnemies)
            {
                Renderer r = enemy.GetComponent<Renderer>();
                if (r && r.isVisible)
                {
                    allSelectedEnemies.Add(enemy);
                }
            }

            ActivateCooldown();
            currentAttackCount = attackCount;
        }

        EnemyStats target = PickEnemy();    // ターゲット選択
        // ターゲットが見つかったら範囲攻撃とエフェクトを生成
        if (target)
        {
            DamageArea(target.transform.position, GetArea(), GetDamage());
            ObjectPool.instance.Get(currentStats.hitEffect.gameObject, target.transform.position, Quaternion.identity);
        }

        if (attackCount > 0)
        {
            currentAttackCount = attackCount - 1;
            currentAttackInterval = currentStats.projectileInterval;
        }

        return true;
    }

    EnemyStats PickEnemy()
    {
        EnemyStats target = null;
        // 有効なターゲットが見つかるかリストが空になるまでループ
        while (!target && allSelectedEnemies.Count > 0)
        {
            // リストからランダムに1体選ぶ
            int idx = Random.Range(0, allSelectedEnemies.Count);
            target = allSelectedEnemies[idx];

            // 敵が既に破壊されるなどしてnullになっていたらリストから除外してやり直し
            if (!target)
            {
                allSelectedEnemies.RemoveAt(idx);
                continue;
            }

            // 敵が画面に表示されていなければターゲットにしない
            Renderer r = target.GetComponent<Renderer>();
            if (!r || !r.isVisible)
            {
                allSelectedEnemies.RemoveAt(idx);
                target = null;
                continue;
            }
        }

        // 一度ターゲットに選んだ敵はリストから削除し、連続で選ばれないようにする
        allSelectedEnemies.Remove(target);
        return target;
    }

    void DamageArea(Vector2 position, float radius, float damage)
    {
        // GC負荷を避けるため、OverlapCircleNonAllocを使用
        int numTargets = Physics2D.OverlapCircleNonAlloc(
            position, 
            radius, 
            overlapResults,
            int.MaxValue // LayerMask (全てのレイヤー)
        );
        for (int i = 0; i < numTargets; i++)
        {
            Collider2D t = overlapResults[i];
            EnemyStats es = t.GetComponent<EnemyStats>();
            if (es != null) // Unityのオブジェクト参照としての有効性をチェック
            {
                // さらにGameObjectアクティブか確認
                if (es.gameObject.activeInHierarchy)
                {
                    es.TakeDamage(damage, transform.position); // 敵ならダメージを与える
                    ApplyBuffs(es); // バフを適用
                }
            }
        }
    }
}
