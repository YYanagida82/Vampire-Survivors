using UnityEngine;

public class ClockLancetWeapon : ProjectileWeapon
{
    public const int NUMBER_OF_ANGLES = 12;
    protected float currentAngle = 90; // -90度は12時の方向を指す

    // この武器が1回撃つごとに回転する角度(30度刻み)
    protected static float turnAngle = -360f / NUMBER_OF_ANGLES;

    protected override bool Attack(int attackCount = 1)
    {
        // 攻撃が成功した場合、現在の角度を進める
        if(base.Attack(1))
        {
            currentAngle += turnAngle;

            // 180より大きいか、-180より小さい場合
            if(Mathf.Abs(currentAngle) > 180f)
                // -180から180の間に変換
                currentAngle = -Mathf.Sign(currentAngle) * (360f - Mathf.Abs(currentAngle));

            return true;
        }
        return false;
    }

    // 武器の発射方向をオーバーライドして、現在の角度を返す
    protected override float GetSpawnAngle() { return currentAngle; }
}
