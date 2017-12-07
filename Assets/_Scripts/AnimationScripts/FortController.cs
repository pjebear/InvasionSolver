using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FortController : MonoBehaviour {

    public Sprite HealthySprite;
    public Sprite DemolishedSprite;
    public Text FortLabel;
    public Image FortImage;
    public HealthBarAnimator HealthBar;
    public LevelDisplay LevelDisplay;
    private int mCurrentLevel;

	// Use this for initialization
	void Start () {
        FortImage.sprite = HealthySprite;
	}
	
    public void Initialize(ushort health, int fortId, int fortLevel)
    {
        FortImage.sprite = HealthySprite;
        FortLabel.text = fortId.ToString();
        LevelDisplay.DisplayLevel(10, 3);
        HealthBar.Initialize(health);
        LevelDisplay.DisplayLevel(fortLevel, 0f);
        mCurrentLevel = fortLevel;
    }

    public void Upgrade(ushort health, int newLevel, float overSeconds)
    {
        Debug.Assert(newLevel >= mCurrentLevel);
        if (newLevel > mCurrentLevel)
        {
            mCurrentLevel = newLevel;
            LevelDisplay.DisplayLevel(newLevel, overSeconds);
            HealthBar.UpdateMaxHealth(health, overSeconds);
        }
    }

    public void Demolish(float overSeconds)
    {
        HealthBar.UpdateCurrentHealth(0, overSeconds);
        StartCoroutine(_Demolish(overSeconds));
    }

    private IEnumerator _Demolish(float overSeconds)
    {
        yield return new WaitForSeconds(overSeconds);
        FortImage.sprite = DemolishedSprite;
    }


}
