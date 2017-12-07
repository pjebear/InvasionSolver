using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvaderCountSelector : MonoBehaviour {

    public int InvaderCount;
    public Button AddButton;
    public Button RemoveButton;
    public Text Count;
     
	// Use this for initialization
	void Start () {
        RemoveButton.interactable = false;
        Count.text = InvaderCount.ToString();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Add()
    {
        if (InvaderCount == 0)
        {
            RemoveButton.interactable = true;
        }
        Count.text = (++InvaderCount).ToString();
    }

    public void Remove()
    {
        if (InvaderCount == 1)
        {
            RemoveButton.interactable = false;
        }
        Count.text = (--InvaderCount).ToString();
    }
}
