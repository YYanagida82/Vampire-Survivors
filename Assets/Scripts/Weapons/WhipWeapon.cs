using UnityEngine;

public class WhipWeapon : ProjectileWeapon
{
    int currentSpawnCount;  // 鞭を生成するカウント
    float currentSpawnYOffset;  // 鞭を生成するY座標のオフセット

    protected override bool Attack(int attackCount = 1)
    {
        if (!currentStats.projectilePrefab)
        {
            ActivateCooldown(true);
            return false;
        }

        if (!CanAttack()) return false;

        // 新しい攻撃の前にカウンターをリセット
        if (currentCooldown <= 0)
        {
            currentSpawnCount = 0;
            currentSpawnYOffset = 0f;
        }

        // 鞭を振る方向を決定(プレイヤーの向き X  偶数or奇数 で交互に攻撃)
        float spawnDir = Mathf.Sign(movement.lastMovedVector.x) * (currentSpawnCount % 2 != 0 ? -1 : 1);
        // 鞭の生成位置を計算
        Vector2 spawnOffset = new Vector2(
            spawnDir * UnityEngine.Random.Range(currentStats.spawnVariance.xMin, currentStats.spawnVariance.xMax),
            currentSpawnYOffset
        );

        // 鞭を生成
        GameObject projectileGO = ObjectPool.instance.Get(
            currentStats.projectilePrefab.gameObject,
            owner.transform.position + (Vector3)spawnOffset,
            Quaternion.identity
        );
        if (!projectileGO) return false;

        Projectile prefab = projectileGO.GetComponent<Projectile>();
        if (!prefab) return false;
        
        prefab.owner = owner;

        // 鞭が左向きならスプライトを水平反転
        if (spawnDir < 0)
        {
            prefab.transform.localScale = new Vector3(
                -Mathf.Abs(prefab.transform.localScale.x),
                prefab.transform.localScale.y,
                prefab.transform.localScale.z
            );
        }

        prefab.weapon = this;
        ActivateCooldown(true);   // 攻撃間隔を更新
        attackCount--;

        currentSpawnCount++;    // カウンター更新(次の攻撃に備える)
        // 2回攻撃するごとに、次の鞭の生成位置を少し上にずらす
        if (currentSpawnCount > 1 && currentSpawnCount % 2 == 0)
            currentSpawnYOffset += 1;

        // 連続攻撃処理
        if (attackCount > 0)
        {
            currentAttackCount = attackCount;
            currentAttackInterval = ((WeaponData)data).baseStats.projectileInterval;
        }

        return true;
    }
}
