using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour {

    Slider slider;

    Enemy targetEnemy;

    private void Start()
    {
        slider = GetComponentInChildren<Slider>();
        targetEnemy = GetComponentInParent<Enemy>();
        slider.maxValue = targetEnemy.MaxHP;
    }

    void Update () {
        slider.value = targetEnemy.CurrentHP;
        if (targetEnemy.IsDead)
            gameObject.SetActive(false);
	}
}
