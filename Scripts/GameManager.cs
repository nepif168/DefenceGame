using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

public enum GameStatus
{
    next, play, gameover, win
}

public class GameManager : Singleton<GameManager> {

    //Stage: ステージ（シーンごと）
    //Wave : ステージ内で切り替わる

    [SerializeField]
    Button playButton;

    [SerializeField]
    GameObject spawnPoint;

    [Header("ウェーブの数とパターン")]
    [SerializeField]
    Wave[] waves;

    [Header("合計で出てもいい許容エネミー数")]
    [SerializeField]
    int limitedTotalEnemies;

    //スポーンまでの時間
    [SerializeField]
    float spawnDelay = 0.1f;

    //1度にスポーンする敵の数
    [SerializeField]
    float enemyPerSpawn = 1f;

    [SerializeField]
    int firstMoney = 10;

    //所持金のテキスト
    [SerializeField]
    Text moneyText;

    [SerializeField]
    Text enemyInfo;

    [SerializeField]
    Text waveText;

    //エネミーの設定
    [SerializeField]
    Transform[] wayPoints;

    [SerializeField]
    Transform exitPoint;

    //お金
    int money;

    //逃げた一人キルした数
    int roundEscaped = 0;
    int roundKilled = 0;

    int totalEscaped = 0;

    int currentWave = 1;    //1からカウントなので注意

    int[] enemiesPattern;

    GameStatus currentGameState = GameStatus.play;

    [HideInInspector]
    public List<Enemy> EnemyList = new List<Enemy>();

    int wavePerSpawn;
    int enemiesToSpawn = 0;

    //ズームのための変数
    Vector3 _cameraPos;

    float touchPointsDistance;

    Vector3 focus = Vector3.zero;

    //移動のための変数
    Vector3 touchedPos;

    //プロパティ

    //所持金が変化したらテキストも変更する
    public int Money
    {
        get { return money; }
        set
        {
            money = value;
            moneyText.text = money.ToString() + "G";
        }
    }

    public int RoundEscaped { get { return roundEscaped; } set { roundEscaped = value; } }
    public int TotalEscaped { get { return totalEscaped; } set { totalEscaped = value; } }
    public GameStatus CurrentGameState { get { return currentGameState; } }
    public int RoundKilled { get { return roundKilled; } set { roundKilled = value; } }

    private void Start()
    { 
        //メニュー表示
        ShowMenu();
        //初期のお金を設定
        Money = firstMoney;

        //UIのTextをセット
        SetInfoText();

        _cameraPos = Camera.main.transform.position;
    }

    
    private void Update()
    {
        //ズームの処理
        if (Input.touchCount >= 2 && !TowerManager.IsPointerOverUIObject())
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            if (t2.phase == TouchPhase.Began)
            {
                touchPointsDistance = Vector2.Distance(t1.position, t2.position);

                Ray ray = Camera.main.ScreenPointToRay((t1.position + t2.position) / 2);
                RaycastHit hit;
                if(Physics.Raycast(ray, out hit, 1000)){
                    focus = hit.point;
                    focus.y = 30;
                }
            }
            else if (t1.phase == TouchPhase.Moved && t2.phase == TouchPhase.Moved)
            {
                float newDist = Vector2.Distance(t1.position, t2.position);

                if (newDist > touchPointsDistance)
                {
                    //広がったとき
                    Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position, focus, Time.deltaTime * 200);
                }
                else
                {
                    Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position, _cameraPos, Time.deltaTime * 200);
                }

                touchPointsDistance = Vector2.Distance(t1.position, t2.position);
            }
        }

        //移動の処理
        if (Input.touchCount == 1 && !TowerManager.IsPointerOverUIObject())
        {
            Touch t1 = Input.GetTouch(0);
            if (t1.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(t1.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000))
                {
                    touchedPos = hit.point;
                    touchedPos.y = Camera.main.transform.position.y;
                }
            }
            else if (t1.phase == TouchPhase.Moved)
            {
                Ray ray = Camera.main.ScreenPointToRay(t1.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000))
                {
                    Vector3 pos = hit.point;
                    pos.y = Camera.main.transform.position.y;
                    Camera.main.transform.position = (touchedPos - pos) * 0.5f + Camera.main.transform.position;

                    touchedPos = pos;
                }
            }
        }
    }

    //敵を出現させる
    IEnumerator Spawn()
    {
        //現在マップ上にいる敵が制限を上回ってない、合計のスポーン数が出現設定数を上回っていない
        if (EnemyList.Count < limitedTotalEnemies && enemiesToSpawn < enemiesPattern.Length)
        {
            for(int i = 0; i < enemyPerSpawn; i++)
            {
                //for文で2対一気に出現の可能性があるので内側でもう一度チェック
                if (EnemyList.Count < limitedTotalEnemies && enemiesToSpawn < enemiesPattern.Length)
                {
                    //エネミーのインスタンスの生成
                    Enemy newEnemy = Instantiate(waves[currentWave - 1].spawnOrders[enemiesPattern[enemiesToSpawn]].enemy);
                    newEnemy.transform.localPosition = spawnPoint.transform.localPosition;
                    newEnemy.wayPoints = wayPoints;
                    newEnemy.exitPoint = exitPoint;

                    enemiesToSpawn++;
                }
            }
        }

        yield return new WaitForSeconds(spawnDelay);
        StartCoroutine(Spawn());
    }

    public void SetCurrentGameState()
    {
        //10人以上抜け出してる -> Gameover
        if (TotalEscaped >= 10)
            currentGameState = GameStatus.gameover;
        //ウェーブ数が全部のウェーブと同じか上回った -> win
        else if (currentWave >= waves.Length)
            currentGameState = GameStatus.win;
        //ウェーブ数0(最初) -> play(ゲーム開始)
        else if (currentWave == 0)
            currentGameState = GameStatus.play;
        //それ以外 -> next(次のウェーブ)
        else
            currentGameState = GameStatus.next;
    }

    public void ShowMenu()
    {
        //現在の状態によってボタンに表示される文字を変えるだけ
        switch (currentGameState)
        {
            case GameStatus.gameover:
                playButton.GetComponentInChildren<Text>().text = "Play again";
                break;
            case GameStatus.next:
                playButton.GetComponentInChildren<Text>().text = "Next wave";
                break;
            case GameStatus.play:
                playButton.GetComponentInChildren<Text>().text = "Play";
                break;
            case GameStatus.win:
                playButton.GetComponentInChildren<Text>().text = "Win";
                break;
        }

        //ボタンをアクティブにする
        playButton.gameObject.SetActive(true);
    }

    public void PlayButton()
    {
        switch (currentGameState)
        {
            case GameStatus.next:
                currentWave += 1;
                limitedTotalEnemies += currentWave;
                currentGameState = GameStatus.play;
                break;
            case GameStatus.gameover:
                SceneManager.LoadScene(0);
                break;
            case GameStatus.win:
                SceneManager.LoadScene(0);
                break;
            default:
                currentWave = 1;
                break;
        }

        roundKilled = 0;
        roundEscaped = 0;
        enemiesToSpawn = 0;

        //すべての敵を削除
        DestroyAllEnemies();

        //敵の順番を生成
        enemiesPattern = CreateEnemiesOrder();

        //コルーチンを止めてSpawnを開始
        StopAllCoroutines();
        StartCoroutine(Spawn());

        //表示を更新
        SetInfoText();

        //ボタンの表示を消す
        playButton.gameObject.SetActive(false);
    }

    public void SetInfoText()
    {
        enemyInfo.text = TotalEscaped + "/" + "10";
        waveText.text = currentWave + "/" + waves.Length;
    }

    //ウェーブがなくなったかのチェック
    public void CheckWaveOver()
    {
        if (roundEscaped + roundKilled == enemiesPattern.Length)
        {
            SetCurrentGameState();
            ShowMenu();
        }
        else if (TotalEscaped >= 10)
        {
            SetCurrentGameState();
            ShowMenu();
        }
    }

    /// <summary>
    /// 敵をリストに登録
    /// </summary>
    /// <param name="enemy">対象の敵</param>
    public void RegisterEnemy(Enemy enemy)
    {
        EnemyList.Add(enemy);
    }

    /// <summary>
    /// 指定した敵を1対削除
    /// </summary>
    /// <param name="enemy">対象の敵</param>
    public void UnregesterEnemy(Enemy enemy)
    {
        EnemyList.Remove(enemy);
        Destroy(enemy.gameObject);
    }

    /// <summary>
    /// 敵をすべて削除
    /// </summary>
    public void DestroyAllEnemies()
    {
        foreach(var enemy in EnemyList)
        {
            Destroy(enemy.gameObject);
        }

        EnemyList.Clear();
    }

    //スポーンの順番ぎめ
    int[] CreateEnemiesOrder()
    {
        int indexLength = 0;
        for (int i = 0; i < waves[currentWave - 1].spawnOrders.Length; i++)
        {
            indexLength += waves[currentWave - 1].spawnOrders[i].amount;
        }

        int[] pattern = new int[indexLength];

        int count = 0;

        for (int i = 0; i < waves[currentWave - 1].spawnOrders.Length; i++)
        {
            for (int j = 0; j < waves[currentWave - 1].spawnOrders[i].amount; j++, count++)
            {
               pattern[count] = i;

            }
        }

        int[] result = pattern.OrderBy(i => Guid.NewGuid()).ToArray();

        return result;
    }


    [Serializable]
    public class SpawnOrder
    {
        public Enemy enemy;
        public int amount;
    }

    [Serializable]
    public class Wave
    {
        public SpawnOrder[] spawnOrders;
    }

    public static void Shuffle(int[] ary)
    {
        //Fisher-Yatesアルゴリズムでシャッフルする
        System.Random rng = new System.Random();
        int n = ary.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int tmp = ary[k];
            ary[k] = ary[n];
            ary[n] = tmp;
        }
    }
}


