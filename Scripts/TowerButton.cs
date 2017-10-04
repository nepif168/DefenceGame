using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerButton : MonoBehaviour {

    //設置するタワーオブジェクト
    [SerializeField]
    Tower towerObject;

    //設置するまでに見せるタワー
    [SerializeField]
    GameObject preTowerObject;

    //価格
    [SerializeField]
    int towerPrice;

	public Tower TowerObject
    {
        get { return towerObject; }
    }

    public GameObject PreTowerObject
    {
        get { return preTowerObject; }
    }

    public int TowerPrice
    {
        get { return towerPrice; }
    }

    Button thisButton;

    private void Awake()
    {
        thisButton = GetComponent<Button>();
    }

    //お金がなかったら押せないようにする
    private void Update()
    {
        if (towerPrice > GameManager.Instance.Money)
            thisButton.interactable = false;
        else
            thisButton.interactable = true;
    }
}
