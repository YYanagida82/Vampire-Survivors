using UnityEngine;

public class AuraWeapon : Weapon
{
    protected Aura currentAura;

    protected override void Update() { }

    public override void OnEquip()  // オーラを装備
    {
        if (currentStats.auraPrefab)
        {
            if (currentAura)    // 既に装備していたら破棄
            {
                ObjectPool.instance.Return(currentAura.gameObject);
                currentAura = null;
            }
            // オーラを生成
            GameObject auraObject = ObjectPool.instance.Get(
                currentStats.auraPrefab.gameObject, 
                transform.position, // 位置はプレイヤーと同じ
                Quaternion.identity // 回転はなし
            );
            currentAura = auraObject.GetComponent<Aura>();
            currentAura.transform.SetParent(this.transform);    // オーラをプレイヤーに追従させる

            // 生成したオーラに武器とプレイヤー情報を渡す
            currentAura.weapon = this;
            currentAura.owner = owner;
            float area = GetArea();
            currentAura.transform.localScale = new Vector3(area, area, area);
        }
    }

    public override void OnUnequip()    // 装備を解除
    {
        if (currentAura)
        {
            ObjectPool.instance.Return(currentAura.gameObject);  // オーラを破棄
            currentAura = null; // 参照をクリア
        }
    }

    public override bool DoLevelUp()    // レベルアップ処理
    {
        if (!base.DoLevelUp()) return false;

        if (currentAura)
        {
            // 新しいレベルのステータスに合わせてオーラエフェクトのサイズを更新
            currentAura.transform.localScale = new Vector3(currentStats.area, currentStats.area, currentStats.area);
        }
        return true;
    }
}
