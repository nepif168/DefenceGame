using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tower : MonoBehaviour {

    //1秒あたりの攻撃回数
    [SerializeField]
    float timeBetweenAttacks;

    //攻撃範囲
    [SerializeField]
    float attackRadius;

    //発射するオブジェクト
    [SerializeField]
    Projectile projectile;

    [SerializeField]
    public UpgradeStatus[] upgradeStatus;

    //発射時の位置調整
    [SerializeField]
    Vector3 shiftProjectilePosition;

    //発射時に回転する部分
    [SerializeField]
    RotationObject[] rotationObject;

    //攻撃対象の敵
    Enemy targetEnemy = null;

    //攻撃の間隔（時間）を保存する
    float attackCounter;

    //攻撃中かどうか
    bool isAttacking = false;

    //初期のアップグレードは配列の0番目なのでインクリメントしたら0になるようにする
    int currentUpgradeIndex = 0;

    int usedMoney = 0;


    //現在のレベル
    public int CurrentLevel
    {
        get
        {
            return currentUpgradeIndex + 1;
        }
    }

    //アップグレードの価格
    public int UpgradeMoney
    {
        get
        {
            if (upgradeStatus.Length > currentUpgradeIndex)
                return upgradeStatus[currentUpgradeIndex].money;
            else
                return -1;
        }
    }

    public int UsedMoney { get { return usedMoney; } }

    //攻撃範囲*0.22 -> radiusImageの範囲
    public float AttackRadius
    {
        get { return attackRadius; }
    }

    void Update () {
        attackCounter -= Time.deltaTime;
        if (targetEnemy == null || targetEnemy.IsDead)
        {
            Enemy nearestEnemy = GetNearestEnemyInRange();  //一番近い敵を取得

            //一番近い敵がいる, その敵が攻撃範囲内のときにターゲットに設定
            if(nearestEnemy != null && Vector3.Distance(MakeUniformHeight(transform.localPosition), MakeUniformHeight(nearestEnemy.transform.localPosition)) <= attackRadius)
            {
                targetEnemy = nearestEnemy;
            }
        }
        else
        {
            if(attackCounter <= 0)  //攻撃の間隔に達したら攻撃
            {
                isAttacking = true;
                attackCounter = timeBetweenAttacks;
            }
            else
            {
                isAttacking = false;
            }

            //攻撃の射程外だったらtargetenemyをfalseにする
            if(Vector3.Distance(MakeUniformHeight(transform.localPosition), MakeUniformHeight(targetEnemy.transform.localPosition)) > attackRadius)
            {
            
                targetEnemy = null;
            }
        }
	}

    private void FixedUpdate()
    {
        if (isAttacking)
            Attack();

        //向きを的に合わせる
        if (targetEnemy != null && rotationObject != null)
            LookAtTargetEnemy(targetEnemy.gameObject);
    }


    public void Attack()
    {
        isAttacking = false;
        Projectile newProjectile = Instantiate(projectile);
        newProjectile.transform.position = transform.position + shiftProjectilePosition;
        if(targetEnemy == null)
        {
            Destroy(newProjectile);
        }
        else
        {
            //発射後の移動
            StartCoroutine(MoveProjectile(newProjectile));
        }
    }

    //発射後の移動
    IEnumerator MoveProjectile(Projectile projectile)
    {
        while (getTargetDistance(targetEnemy) > 0.2f && projectile != null && targetEnemy != null && !targetEnemy.IsDead)
        {
            if (GameManager.Instance.CurrentGameState != GameStatus.play)
                break;
            var dir = targetEnemy.transform.localPosition - transform.localPosition;
            var angleDirection = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.AngleAxis(angleDirection, Vector3.forward);
            projectile.transform.localPosition = Vector3.MoveTowards(projectile.transform.localPosition, targetEnemy.transform.localPosition, Time.deltaTime * projectile.Speed);
            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile.gameObject);
        }
        yield return null;
    }

    public void UpgradeTower()
    {
        //アップグレードが最大以上になっていないか、お金が足りているか
        if (upgradeStatus.Length >= currentUpgradeIndex && GameManager.Instance.Money >= upgradeStatus[currentUpgradeIndex].money)
        {
            timeBetweenAttacks = upgradeStatus[currentUpgradeIndex].timeBetweenAttacks;
            attackRadius = upgradeStatus[currentUpgradeIndex].attackRadius;
            GameManager.Instance.Money -= upgradeStatus[currentUpgradeIndex].money;
            usedMoney += upgradeStatus[currentUpgradeIndex].money; 
            currentUpgradeIndex++;
        }
    }

    public void InitUsedMoney(int money)
    {
        usedMoney += money;
    }

    //targetの方向に向ける
    void LookAtTargetEnemy(GameObject target)
    {
        float _x, _z;
        Quaternion rotate;
        Vector3 _pos;

        foreach (var ro in rotationObject)
        {
            _x = ro.rotateBody.transform.localRotation.x;
            _z = ro.rotateBody.transform.localRotation.z;
            _pos = ro.rotateBody.transform.localPosition;

            ro.rotateBody.transform.LookAt(target.transform);
            if (ro.isOnlyYCoordinate)
            {
                rotate = ro.rotateBody.transform.localRotation;
                rotate.x = _x;
                rotate.z = _z;
                ro.rotateBody.transform.localRotation = rotate;
                ro.rotateBody.transform.localPosition = _pos;
            }
        }
    }

    //targetとの距離を返す
    float getTargetDistance(Enemy enemy)
    {
        //引数がnullだったら一番近い敵を返す
        if (enemy == null)
        {
            enemy = GetNearestEnemyInRange();
            //敵が見つからなかったら0でかえす
            if (enemy == null)
                return 0f;
        }
        //絶対値で返す
        return Mathf.Abs(Vector3.Distance(MakeUniformHeight(transform.localPosition), MakeUniformHeight(enemy.transform.localPosition)));
    }

    //範囲内の敵をリストにして返す
    List<Enemy> GetEnemiesInRange()
    {
        List<Enemy> enemiesInRange = new List<Enemy>();

        foreach(var enemy in GameManager.Instance.EnemyList)
        {
            if(Vector3.Distance(MakeUniformHeight(transform.localPosition), MakeUniformHeight(enemy.transform.localPosition)) <= attackRadius)
            {
                enemiesInRange.Add(enemy);
            }
        }

        return enemiesInRange;
    }

    //一番近い敵を返す
    Enemy GetNearestEnemyInRange()
    {
        Enemy nearestEnemy = null;
        float smallestDistance = float.PositiveInfinity;    //floatを最大に設定しておいて、小さいものと入れ替えていく

        //範囲内の敵を走査
        foreach(var enemy in GetEnemiesInRange())
        {
            if(Vector3.Distance(MakeUniformHeight(transform.localPosition), MakeUniformHeight(enemy.transform.localPosition)) < smallestDistance && !enemy.IsDead)
            {
                smallestDistance = Vector3.Distance(MakeUniformHeight(transform.localPosition), MakeUniformHeight(enemy.transform.localPosition));
                nearestEnemy = enemy;
            }
        }
        return nearestEnemy;
    }

    //高さ0で返す
    Vector3 MakeUniformHeight(Vector3 position)
    {
        Vector3 newPosition = new Vector3(position.x, 0, position.z);

        return newPosition;
    }

    

    [System.Serializable]
    public class RotationObject
    {
        public GameObject rotateBody;
        public bool isOnlyYCoordinate;
    }

    [System.Serializable]
    public class UpgradeStatus
    {
        public float timeBetweenAttacks;
        public float attackRadius;
        public int money;
    }
}
