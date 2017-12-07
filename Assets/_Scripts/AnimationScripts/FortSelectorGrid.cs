using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FortSelectorGrid : MonoBehaviour {

    public FortSelector FortSelectorPrefab;
    private int mDefaultGridSize = 7;
    public int NumElementsInRow;
    public int MaxGridSize;
    public int MinGridSize;

    public FortSelector[,] FortSelectors;
    private Vector3 SelectorDimensions;

    private void Awake()
    {
        NumElementsInRow = mDefaultGridSize;
        MaxGridSize = mDefaultGridSize / 2 + 1;
        MinGridSize = 1;

        SelectorDimensions = FortSelectorPrefab.GetComponent<RectTransform>().rect.size;

        FortSelectors = new FortSelector[mDefaultGridSize, mDefaultGridSize];

        int middle = mDefaultGridSize / 2; // assumes odd size
        for (int row = 0; row < mDefaultGridSize; ++row)
        {
            for (int column = 0; column < mDefaultGridSize; column++)
            {
                FortSelector selector = Instantiate(FortSelectorPrefab, transform);
                selector.transform.localPosition = new Vector3((column - middle) * SelectorDimensions.x, (middle - row) * SelectorDimensions.y, 0);
                FortSelectors[row, column] = selector;
                selector.gameObject.SetActive(true);
            }
        }
    }

    public void SetGridToShow(int sizeToShow)
    {
        if (sizeToShow >= MinGridSize && sizeToShow <= MaxGridSize )
        {
            int offset = MaxGridSize - sizeToShow;
            SetSelectorsActive(offset);
        }
        else
        {
            Debug.Log("FortSelectorGrid.SetGridToShow(): Invalid Size: " + sizeToShow);
        }
    }

    private void SetSelectorsActive(int offset)
    {
        for (int row = 0; row < mDefaultGridSize; ++row)
        {
            for (int column = 0; column < mDefaultGridSize; column++)
            {
                bool active = row >= offset && row <= mDefaultGridSize - offset - 1
                    && column >= offset && column <= mDefaultGridSize - offset - 1;
                FortSelectors[row,column].gameObject.SetActive(active);
            }
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
