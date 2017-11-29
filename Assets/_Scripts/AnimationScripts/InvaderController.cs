using Common.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvaderController : MonoBehaviour {

    private float mMaxHealth;
    public float CurrentHealth;
    public bool CanBeUsed;

    public Text HealthText;
    public HealthBarAnimator HealthBar;
    public Sprite ArcherImage;
    public Sprite SoldierImage;
    public Sprite HealerImage;
    public Sprite DeathImage;
    public Image Image;


    public void Initialize(ushort health, InvaderType type)
    {
        // do stuff with health bar?
        mMaxHealth = CurrentHealth = health;
        CanBeUsed = true;
        HealthBar.Initialize(mMaxHealth);
        switch (type)
        {
            case (InvaderType.Archer):
                Image.sprite = ArcherImage;
                break;
            case (InvaderType.Soldier):
                Image.sprite = SoldierImage;
                break;
            case (InvaderType.Healer):
                Image.sprite = HealerImage;
                break;
        }
    }
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void UpdateHealth(ushort newHealth, float overSeconds)
    {
        CurrentHealth = newHealth;
        if (newHealth == 0)
        {
            StartCoroutine(_ToggleToDeathImage(overSeconds));
        }
        HealthBar.UpdateHealthBar(newHealth, overSeconds);
    }

    private IEnumerator _ToggleToDeathImage(float afterSeconds)
    {
        yield return new WaitForSeconds(afterSeconds);
        Image.sprite = DeathImage;
    }

    public void MoveToPosition(Vector3 localPosition, float inSeconds)
    {
        StartCoroutine(_MoveToPosition(localPosition, inSeconds));
    }

    private IEnumerator _MoveToPosition(Vector3 localPosition, float inSeconds)
    {
        Vector3 delta = localPosition - transform.localPosition;        
        delta /= inSeconds;
        float deltaDistance = delta.magnitude;

        while (true)
        {
            Vector3 remaining = localPosition - transform.localPosition;
            if (remaining.sqrMagnitude <= deltaDistance * Time.deltaTime)
            {
                break;
            }
            else
            {
                transform.localPosition += delta * Time.deltaTime;
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void OnDeath()
    {
        Destroy(gameObject);
    }
}
