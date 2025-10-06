using System;
using UnityEngine;

public class ProjectileWeapon : Weapon
{
    protected float currentAttackInterval;   // 攻撃間隔
    protected int currentAttackCount;   // 攻撃回数

    protected override void Update()
    {
        base.Update();

        if (currentAttackInterval > 0)
        {
            currentAttackInterval -= Time.deltaTime;
            if (currentAttackInterval <= 0) Attack(currentAttackCount);
        }
    }

    public override bool CanAttack()
    {
        if (currentAttackCount > 0) return true;
        return base.CanAttack();
    }

    protected override bool Attack(int attackCount = 1)
    {
        if (!currentStats.projectilePrefab)
        {
            ActivateCooldown(true);
            return false;
        }

        if (!CanAttack()) return false; // 攻撃できない場合はスキップ

        float spawnAngle = GetSpawnAngle(); // 発射角度を取得

        // 発射物生成
        GameObject projectileGO = ObjectPool.instance.Get(
            currentStats.projectilePrefab.gameObject,
            owner.transform.position + (Vector3)GetSpawnOffset(spawnAngle),
            Quaternion.Euler(0, 0, spawnAngle)
        );
        if (!projectileGO) return false;

        Projectile prefab = projectileGO.GetComponent<Projectile>();
        if (!prefab) return false;

        prefab.weapon = this;   // 武器をセット
        prefab.owner = owner;   // 生成元


        ActivateCooldown(true); // 攻撃間隔を更新

        attackCount--;  // 攻撃回数を減らす

        if (attackCount > 0)
        {
            currentAttackCount = attackCount;   // 攻撃回数を更新
            currentAttackInterval = ((WeaponData)data).baseStats.projectileInterval;  // 攻撃間隔を更新
        }

        return true;
    }

    protected virtual float GetSpawnAngle()
    {
        return Mathf.Atan2(movement.lastMovedVector.y, movement.lastMovedVector.x) * Mathf.Rad2Deg;
    }

    protected virtual Vector2 GetSpawnOffset(float spawnAngle = 0)
    {
        return Quaternion.Euler(0, 0, spawnAngle) * new Vector2(
            UnityEngine.Random.Range(currentStats.spawnVariance.xMin, currentStats.spawnVariance.xMax),
            UnityEngine.Random.Range(currentStats.spawnVariance.yMin, currentStats.spawnVariance.yMax)
        );
    }
}
