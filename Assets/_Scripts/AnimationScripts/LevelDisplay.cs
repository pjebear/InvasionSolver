using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelDisplay : MonoBehaviour {

    public Text LevelText;
    public Image LevelUpImage;
    private int mCurrentLevel;

	// Use this for initialization
	void Start () {
        LevelUpImage.gameObject.SetActive(false);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void DisplayLevel(int newLevel, float overSeconds)
    {
        if (newLevel > mCurrentLevel)
        {
            StartCoroutine(DisplayLevelupImage(overSeconds));
        }
        mCurrentLevel = newLevel;
        LevelText.text = (newLevel+1).ToString();
    }

    private IEnumerator DisplayLevelupImage(float forSeconds)
    {
        LevelUpImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(forSeconds);
        LevelUpImage.gameObject.SetActive(false);
    }
}
