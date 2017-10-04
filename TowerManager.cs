using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

public class TowerManager : Singleton<TowerManager> {

    [SerializeField]
    GameObject rangeImage;

    [SerializeField]
    Transform child;

    [SerializeField]
    float size;

    [Header("Upgrade")]
    [SerializeField]
    GameObject upgradeUI;

    //ボタン
    [SerializeField]
    Button upgradeButton;
    [SerializeField]
    Button sellButton;

    //テキスト
    [SerializeField]
    Text towerLevelText;
    [SerializeField]
    Text upgradeMoneyText;
    [SerializeField]
    Text sellMoneyText;

    //選択されたタワーを保持
    Tower selectedTower = null;

    public TowerButton PressedTowerButton { get; set; }


    //最初に選択されたら表示するUIを消す
	void Awake(){
        rangeImage.SetActive(false);
        upgradeUI.SetActive(false);

        InitUpgradeUI();
    }
	
	void Update () {
        //使いまわすので先に定義
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        //タワーをクリックした場合に攻撃範囲を表示
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject())
        {
            if (Physics.Raycast(ray, out hit, 1000))
            {
                Tower tower = hit.collider.gameObject.GetComponent<Tower>();

                if (tower != null)
                {
                    //タワーの情報を保持
                    selectedTower = tower;
                    
                    //UI表示
                    SetRangeImage(tower);
                    ShowUpgradeUI(tower);
                }
                else
                {
                    //タワー以外のクリックは非表示
                    rangeImage.SetActive(false);
                    upgradeUI.SetActive(false);

                    InitUpgradeUI();

                    //タワーが選択されてないので破棄
                    selectedTower = null;
                }
            }
        }

        //選択されたタワーの表示
        if (PressedTowerButton != null)
        {
            if (Physics.Raycast(ray, out hit, 1000))
            {
                if (hit.collider.tag == "Sea") return;
                transform.position = hit.collider.transform.position;

                //クリックされたらタワーを生成
                if (Input.GetMouseButtonDown(0) && PressedTowerButton.TowerPrice <= GameManager.Instance.Money && hit.collider.tag == "Map_CanInstallation")
                {
                    //タワーの生成と位置調整
                    var tower = Instantiate(PressedTowerButton.TowerObject.gameObject);
                    tower.transform.position = hit.collider.transform.position;
                    tower.GetComponent<Tower>().InitUsedMoney(PressedTowerButton.TowerPrice);

                    //設置できないように変更
                    hit.collider.tag = "Map_CannnotInstallation";

                    //購入
                    GameManager.Instance.Money -= PressedTowerButton.TowerPrice;

                    //押されたボタンは初期化
                    PressedTowerButton = null;

                    //表示を消す
                    child.gameObject.SetActive(false);
                }
            }
        }

        //Escでタワーの表示をしなくする
        CancelDisplayedTower();

        CheckUpgradeButton();
	}

    //範囲を表示
    void SetRangeImage(Tower tower)
    {
        rangeImage.transform.position = new Vector3(tower.transform.position.x, -27.7f, tower.transform.position.z);
        rangeImage.transform.localScale = new Vector3(tower.AttackRadius * size, tower.AttackRadius * size, tower.AttackRadius * size);
        rangeImage.SetActive(true);
    }

    //UpgradeUIの表示
    void ShowUpgradeUI(Tower tower)
    {
        upgradeUI.transform.position = (tower.transform.position + Camera.main.transform.position) / 2;
        //upgradeUI.transform.SetParent(tower.transform);
        //upgradeUI.transform.localPosition = upgradeUIPosition;
        UpdateUpgradeUI();
        upgradeUI.SetActive(true);
    }

    //ボタンが押されたときの処理
    public void SelectedTower(TowerButton selectedTowerButton)
    {
        if (selectedTowerButton != PressedTowerButton)
        {
            //押されたボタンの設定
            PressedTowerButton = selectedTowerButton;

            //子オブジェクトがあったら先に削除
            foreach (Transform n in child.transform)
            {
                Destroy(n.gameObject);
            }

            //PreObjectの生成 -> 登録(位置調整等)
            var pre = Instantiate(PressedTowerButton.PreTowerObject);
            pre.transform.SetParent(child);
            pre.transform.localPosition = Vector3.one;


            //前回のが残っていた場合、ボタンを押したとき前回の場所に出現するので場外に移動させる
            transform.position = new Vector3(1000, 0, 1000);

            //表示する
            child.gameObject.SetActive(true);
        }
        else
        {
            PressedTowerButton = null;
            child.gameObject.SetActive(false);
        }
    }

    //仮で表示されてるオブジェクトを非表示
    void CancelDisplayedTower()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PressedTowerButton = null;
            child.gameObject.SetActive(false);
        }
    }

    //アップグレードのボタン
    public void OnClickUpgradeButton()
    {
        if (selectedTower == null) return;  //無いと思うけど念のため

        selectedTower.UpgradeTower();

        UpdateUpgradeUI();
        SetRangeImage(selectedTower);
    }

    //売るボタン
    public void OnClickSellButton()
    {
        if (selectedTower == null) return;

        //売るお金はかけたお金の1/2
        GameManager.Instance.Money += Mathf.CeilToInt(selectedTower.UsedMoney / 2f);

        //タワー下のタイルを設置可能に戻す
        RaycastHit hit;
        if (Physics.Raycast(origin: selectedTower.transform.position, direction: Vector3.down, maxDistance: 100, hitInfo: out hit))
        {
            if (hit.collider.tag == "Map_CannnotInstallation")
            {
                hit.collider.tag = "Map_CanInstallation";
            }
        }

        //タワー削除
        Destroy(selectedTower.gameObject);

        //タワーUIの表示をけす
        rangeImage.SetActive(false);
        upgradeUI.SetActive(false);
    }

    //UIの更新
    void UpdateUpgradeUI()
    {
        upgradeButton.interactable = sellButton.interactable = true;
        if (selectedTower.UpgradeMoney > 0)
            upgradeMoneyText.text = string.Format("Upgrade\r\n{0}G", selectedTower.UpgradeMoney);
        else
        {
            upgradeButton.interactable = false;
            upgradeMoneyText.text = "MaxLevel";
        }

        sellMoneyText.text = string.Format("　Sell　\r\n{0}G", Mathf.CeilToInt(selectedTower.UsedMoney / 2f));
        towerLevelText.text = "Lv" + selectedTower.CurrentLevel.ToString();
    }

    void CheckUpgradeButton()
    {
        if (selectedTower == null) return;
        if (selectedTower.UpgradeMoney > GameManager.Instance.Money)
            upgradeButton.interactable = false;
        else
            upgradeButton.interactable = true;
    }

    public void InitUpgradeUI()
    {
        upgradeMoneyText.text = sellMoneyText.text = "Select Tower";
        upgradeButton.interactable = sellButton.interactable = false;
    }

    //UI上をクリックしているかどうか

    /// <summary>
    /// Cast a ray to test if Input.mousePosition is over any UI object in EventSystem.current. This is a replacement
    /// for IsPointerOverGameObject() which does not work on Android in 4.6.0f3
    /// </summary>
    public static bool IsPointerOverUIObject()
    {
        // Referencing this code for GraphicRaycaster https://gist.github.com/stramit/ead7ca1f432f3c0f181f
        // the ray cast appears to require only eventData.position.
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    /// <summary>
    /// Cast a ray to test if screenPosition is over any UI object in canvas. This is a replacement
    /// for IsPointerOverGameObject() which does not work on Android in 4.6.0f3
    /// </summary>
    private bool IsPointerOverUIObject(Canvas canvas, Vector2 screenPosition)
    {
        // Referencing this code for GraphicRaycaster https://gist.github.com/stramit/ead7ca1f432f3c0f181f
        // the ray cast appears to require only eventData.position.
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = screenPosition;

        GraphicRaycaster uiRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
        List<RaycastResult> results = new List<RaycastResult>();
        uiRaycaster.Raycast(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}
