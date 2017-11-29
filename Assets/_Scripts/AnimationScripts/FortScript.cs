using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FortScript : MonoBehaviour {

    
    public Sprite HealthySprite;
    public Sprite DemolishedSprite;
    public Text FortLabel;
    public Image FortImage;

	// Use this for initialization
	void Start () {
        FortImage.sprite = HealthySprite;
	}
	
    public void Initialize(ushort health, int fortId)
    {
        FortImage.sprite = HealthySprite;
        FortLabel.text = fortId.ToString();
    }

    public void Demolish()
    {
        FortImage.sprite = DemolishedSprite;
    }
}
