using System.Collections.Generic;
using UnityEngine;

public class Aura : WeaponEffect    // オーラ系武器クラス
{
    // 現在オーラ内にいる敵とその敵が次にダメージを受けるまでの時間を管理する変数
    Dictionary<EnemyStats, float> affectedTargets = new Dictionary<EnemyStats, float>();

    // オーラから出た敵をリストから安全に削除するために一時的に保持するリスト
    List<EnemyStats> targetsToUnaffect = new List<EnemyStats>();

    void Update()
    {
        // コピーを作成
        Dictionary<EnemyStats, float> affectedTargetsCopy = new Dictionary<EnemyStats, float>(affectedTargets);

        // 現在オーラ内にいる全ての敵に対して処理を行う
        foreach (KeyValuePair<EnemyStats, float> pair in affectedTargetsCopy)
        {
            affectedTargets[pair.Key] -= Time.deltaTime;    // ダメージを受けるまでの時間を減らす
            // ダメージ処理
            if (pair.Value <= 0)
            {
                // 敵がオーラから出ている場合
                if (targetsToUnaffect.Contains(pair.Key))
                {
                    // ダメージを与えずにリストから完全削除
                    affectedTargets.Remove(pair.Key);
                    targetsToUnaffect.Remove(pair.Key);
                }
                else  // まだオーラ内にいる場合
                {
                    Weapon.Stats stats = weapon.GetStats(); // 武器ステータス取得

                    // タイマーを武器のクールダウン時間でリセットする (次のダメージタイミングを設定)
                    affectedTargets[pair.Key] = stats.cooldown * Owner.Stats.cooldown;
                    // 敵にダメージとノックバックを与える
                    pair.Key.TakeDamage(GetDamage(), transform.position, stats.knockback);
                    weapon.ApplyBuffs(pair.Key);  // バフを適用
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 接触したのが敵の場合
        if (other.TryGetComponent(out EnemyStats es))
        {
            // リストに登録されていない敵の場合
            if (!affectedTargets.ContainsKey(es))
            {
                affectedTargets.Add(es, 0); // リストに追加。タイマー0で即ダメージが入るようにする
            }
            else
            {
                // 登録済みの敵が一度範囲外に出てすぐ戻ってきた場合の保険処理
                if (targetsToUnaffect.Contains(es))
                {
                    targetsToUnaffect.Remove(es);
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out EnemyStats es))
        {
            // リストにいる敵が範囲外に出たら
            if (affectedTargets.ContainsKey(es))
            {
                // 削除予定リストに追加する (すぐには消さない)
                targetsToUnaffect.Add(es);
            }
        }
    }
}
