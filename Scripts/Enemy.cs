using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO: projectileに破壊されたときの破壊判定

public class Enemy : MonoBehaviour {

    //道順を順番に入れる
    [System.NonSerialized]
    public Transform[] wayPoints;

    //最終ポイント
    [System.NonSerialized]
    public Transform exitPoint;
    
    //何秒に1回移動するか
    [SerializeField]
    float navigationUpdate;

    //hp
    [SerializeField]
    int hp;

    //移動速度
    [SerializeField]
    float moveSpeed = 1;

    [SerializeField]
    float destroyDelay = 5;

    //死んだときにもらえるお金
    [SerializeField]
    int reward;

    //移動ターゲットのindex
    int targetWayPointIndex = 0;

    //死んでいるかの確認
    bool isDead = false;

    Collider enemyCollider;
    Rigidbody enemyRigidbody;
    Animator animator;
    Animation animation;
    float navigationTime = 0;

    int maxHp;

    //プロパティ

    public bool IsDead
    {
        get { return isDead; }
    }

    public int CurrentHP
    {
        get { return hp; }
    }

    public int MaxHP
    {
        get { return maxHp; }
    }

	void Awake () {
        //コンポーネントの取得
        enemyCollider = GetComponent<Collider>();
        enemyRigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        animation = GetComponent<Animation>();

        //EnemyListに登録
        GameManager.Instance.RegisterEnemy(this);

        maxHp = hp;
    }

    private void Start()
    {
        //出現時に向きを移動ターゲットに向ける
        transform.LookAt(wayPoints[targetWayPointIndex]);
    }

    void Update () {
        if (wayPoints != null && !isDead)
        {
            navigationTime += Time.deltaTime;
            if (navigationTime > navigationUpdate)
            {
                //移動処理
                if (targetWayPointIndex < wayPoints.Length)
                {
                    transform.position = Vector3.MoveTowards(transform.position, wayPoints[targetWayPointIndex].position, moveSpeed * Time.deltaTime);
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, exitPoint.position, moveSpeed * Time.deltaTime);
                }

                //タイマーリセット
                navigationTime = 0;
            }
        }
	}

    private void OnTriggerEnter(Collider other)
    {
        //target pointの変更と向きの操作
        ChangeWayPointTargetWhenOnTriggerEnter(other);

        //最終ポイントまで到達したら
        if (other.gameObject.name == exitPoint.gameObject.name)
        {
            //脱出した数+1
            GameManager.Instance.RoundEscaped += 1;
            GameManager.Instance.TotalEscaped += 1;

            //敵（これ）の削除
            GameManager.Instance.UnregesterEnemy(this);

            //ゲーム終了条件を満たしていないかチェック
            GameManager.Instance.CheckWaveOver();
        }
        else if(other.tag == "Projectile")
        {
            Projectile newProjectile = other.gameObject.GetComponent<Projectile>();
            EnemyHit(newProjectile.AttackLength);
            Destroy(other.gameObject);
        }
    }

    public void EnemyHit(int hitPoints)
    {
        //hpを減らす or 死んだか確認
        if(hp - hitPoints > 0)
        {
            hp -= hitPoints;
        }
        else
        {
            if (animator != null)
                animator.SetTrigger("Die");
            else if (animation != null)
                animation.Play("Dead");

            die();
        }
    }

    //死んだ判定（アニメーション等）を行う
    public void die()
    {
        hp = 0;
        if (!isDead)
        {
            //死んだことにする
            isDead = true;

            //重力を無効にする
            enemyRigidbody.useGravity = false;

            //コライダーをfalseにして当たり判定をなくす
            enemyCollider.enabled = false;

            GameManager.Instance.RoundKilled += 1;

            Invoke("DestroyThisEnemy", destroyDelay);
        }
    }

    void DestroyThisEnemy()
    {
        GameManager.Instance.UnregesterEnemy(this);

        GameManager.Instance.Money += reward;

        //ゲーム終了条件を確認
        GameManager.Instance.CheckWaveOver();
    }

    void ChangeWayPointTargetWhenOnTriggerEnter(Collider other)
    {
        //ターゲットのIndexがLength以上になる場合、条件式内の配列のIndex参照でエラーになるので先に回避
        if (targetWayPointIndex < wayPoints.Length - 1)
        {
            //向きが変わると再度当たり判定になるため、waypointそれぞれのgameobjectを判断材料にする(Tagだと再度当たり判定で+2)
            if (other.gameObject.name == wayPoints[targetWayPointIndex].gameObject.name)
            {
                targetWayPointIndex++;
                transform.LookAt(wayPoints[targetWayPointIndex]);
            }
        }
        //exitpointに向かう
        else if (other.gameObject.name == wayPoints[wayPoints.Length - 1].gameObject.name)
        {
            targetWayPointIndex++;
            transform.LookAt(exitPoint);
        }

    }

    private void OnDestroy()
    {
        GameManager.Instance.SetInfoText();
        GameManager.Instance.CheckWaveOver();
    }
}
