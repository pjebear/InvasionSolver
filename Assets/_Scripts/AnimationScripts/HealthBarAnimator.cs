using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarAnimator : MonoBehaviour {

    private float mCurrentHealth;
    private float mMaxHealth;
    
    public Text HealthText;
    public Text DeltaText;
    public Transform CurrentHealthBar;

    public void Initialize(float maxHealth)
    {
        mCurrentHealth = mMaxHealth = maxHealth;
        DeltaText.gameObject.SetActive(false);
        DisplayHealthBar();
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void UpdateHealthBar(float newHealth, float overSeconds)
    {
        if (newHealth != mCurrentHealth)
        {
            StartCoroutine(_UpdateHealthBar(newHealth, overSeconds));
        }
    }

    private IEnumerator _UpdateHealthBar(float newHealth, float overSeconds)
    {
        float deltaHealth = newHealth - mCurrentHealth;
        DeltaText.gameObject.SetActive(true);
        DeltaText.text = (deltaHealth > 0 ? "+" : "") + ((int)deltaHealth).ToString();
        DeltaText.color = deltaHealth < 0 ? Color.red : Color.green;
        deltaHealth /= overSeconds;
        while (true)
        {
            if (!(deltaHealth < 0 ^ mCurrentHealth <= newHealth))
            {
                mCurrentHealth = newHealth;
                DisplayHealthBar();
                DeltaText.gameObject.SetActive(false);
                break;
            }
            else
            {
                DeltaText.gameObject.SetActive(true);
                mCurrentHealth += deltaHealth * Time.deltaTime;
                DisplayHealthBar();
                yield return new WaitForFixedUpdate();
            }
        }
    }

    private void DisplayHealthBar()
    {
        HealthText.text = string.Format("{0} / {1}", mCurrentHealth.ToString("n0"), mMaxHealth.ToString("n0"));
        CurrentHealthBar.localScale = new Vector3( mCurrentHealth / mMaxHealth, 1,1);
    }

}
