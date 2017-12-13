using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NationLayoutController : MonoBehaviour {

    public Button IncreaseButton;
    public Button DecreaseButton;
    public Text SizeCounterText;
    public Text NationSizeText;
    private int mCurrentNationSize;

    public const int
        SQUARE_LAYOUT = 0,
        DIAMOND_LAYOUT = 1,
        COMPACT_LAYOUT = 2,
        X_LAYOUT = 3,
        CUSTOM_LAYOUT = 4,
        NUM_LAYOUTS = 5;

    private int mCurrentLayout;

    public Button[] NationLayoutSelectors;
    public FortSelectorGrid FortGrid;

	// Use this for initialization
	void Start () {
        Debug.Assert(NationLayoutSelectors.Length == NUM_LAYOUTS);
        DecreaseButton.interactable = false;
        mCurrentNationSize = FortGrid.MinGridSize;
        SizeCounterText.text = mCurrentNationSize.ToString();
        UpdateGridSize();
        mCurrentLayout = -1;
        UpdateLayout(SQUARE_LAYOUT);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public bool[,] GetNationLayout()
    {
        int startingPos = FortGrid.MaxGridSize - mCurrentNationSize;
        int maxPosition = FortGrid.NumElementsInRow - startingPos;
        int gridSize = maxPosition - startingPos;

        bool[,] boolGrid = new bool[gridSize, gridSize];
        for (int row = startingPos; row < maxPosition; row++)
        {
            for (int column = startingPos; column < maxPosition; column++)
            {
                boolGrid[row - startingPos, column - startingPos] = FortGrid.FortSelectors[row, column].Selected;
            }
        }
        return boolGrid;
    }

    public void IncreaseNationSize()
    {
        if (mCurrentNationSize == FortGrid.MaxGridSize - 1)
        {
            IncreaseButton.interactable = false;
        }
        else if (mCurrentNationSize == FortGrid.MinGridSize)
        {
            DecreaseButton.interactable = true;
        }
        mCurrentNationSize++;
        SizeCounterText.text = mCurrentNationSize.ToString();
        UpdateGridSize();
        UpdateLayout(mCurrentLayout);
    }

    public void DecreaseNationSize()
    {
        if (mCurrentNationSize == FortGrid.MaxGridSize)
        {
            IncreaseButton.interactable = true;
        }
        else if (mCurrentNationSize == FortGrid.MinGridSize + 1)
        {
            DecreaseButton.interactable = false;
        }
        mCurrentNationSize--;
        SizeCounterText.text = mCurrentNationSize.ToString();
        UpdateGridSize();
        UpdateLayout(mCurrentLayout);
    }

    private void UpdateGridSize()
    {
        FortGrid.SetGridToShow(mCurrentNationSize);
    }

    public void UpdateLayout(int newLayout)
    {
        bool fromResize = mCurrentLayout == newLayout;
        if (!fromResize)
        {
            mCurrentLayout = newLayout;
            for (int layout = 0; layout < NUM_LAYOUTS; layout++)
            {
                NationLayoutSelectors[layout].GetComponent<Image>().color =
                    layout == mCurrentLayout ? Color.white : Color.white - new Color(0, 0, 0, .5f);
            }
        }

        switch (mCurrentLayout)
        {
            case (SQUARE_LAYOUT):
                _UpdateSquareLayout();
                break;

            case (DIAMOND_LAYOUT):
                _UpdateDiamondLayout();
                break;

            case (COMPACT_LAYOUT):
                _UpdateCompactLayout();
                break;

            case (X_LAYOUT):
                _UpdateXLayout();
                break;

            case (CUSTOM_LAYOUT):
                _UpdateCustomLayout(fromResize);
                break;
        }
    }

    private void _UpdateSquareLayout()
    {
        int startingPos = FortGrid.MaxGridSize - mCurrentNationSize;
        int maxPosition = FortGrid.NumElementsInRow - startingPos;

        for (int row = 0; row < FortGrid.NumElementsInRow; ++row)
        {
            for (int column = 0; column < FortGrid.NumElementsInRow; column++)
            {
                FortGrid.FortSelectors[row, column].SetSelectable(false);
                if (row >= startingPos && row < maxPosition && column >= startingPos && column < maxPosition)
                {
                    if (row == startingPos || row == maxPosition - 1
                    || column == startingPos || column == maxPosition - 1)
                    {
                        FortGrid.FortSelectors[row, column].SetSelected(true);
                    }
                    else
                    {
                        FortGrid.FortSelectors[row, column].SetSelected(false);
                    }
                }
                else
                {
                    FortGrid.FortSelectors[row, column].SetSelected(false);
                }
            }
        }
    }

    private void _UpdateCompactLayout()
    {
        int startingPos = FortGrid.MaxGridSize - mCurrentNationSize;
        int maxPosition = FortGrid.NumElementsInRow - startingPos;

        for (int row = 0; row < FortGrid.NumElementsInRow; ++row)
        {
            for (int column = 0; column < FortGrid.NumElementsInRow; column++)
            {
                FortGrid.FortSelectors[row, column].SetSelectable(false);
                if (row >= startingPos && row < maxPosition && column >= startingPos && column < maxPosition)
                {
                    FortGrid.FortSelectors[row, column].SetSelected(true);
                }
                else
                {
                    FortGrid.FortSelectors[row, column].SetSelected(false);
                }
            }
        }
    }

    private void _UpdateCustomLayout(bool fromResize)
    {
        int startingPos = FortGrid.MaxGridSize - mCurrentNationSize;
        int maxPosition = FortGrid.MaxGridSize * 2 - 1 - startingPos;

        for (int row = startingPos; row < maxPosition; ++row)
        {
            for (int column = startingPos; column < maxPosition; column++)
            {
                FortGrid.FortSelectors[row, column].SetSelectable(true);
                if (!fromResize)
                {
                    FortGrid.FortSelectors[row, column].SetSelected(false);
                }
            }
        }
    }

    private void _UpdateXLayout()
    {
        int startingPos = FortGrid.MaxGridSize - mCurrentNationSize;
        int maxPosition = FortGrid.NumElementsInRow - startingPos;

        for (int row = 0; row < FortGrid.NumElementsInRow; ++row)
        {
            for (int column = 0; column < FortGrid.NumElementsInRow; ++column)
            {
                FortGrid.FortSelectors[row, column].SetSelectable(false);
                if (row >= startingPos && row < maxPosition && column >= startingPos && column < maxPosition)
                {
                    if (row == column || FortGrid.NumElementsInRow - row - 1 == column)
                    {
                        FortGrid.FortSelectors[row, column].SetSelected(true);
                    }
                    else
                    {
                        FortGrid.FortSelectors[row, column].SetSelected(false);
                    }
                }
                else
                {
                    FortGrid.FortSelectors[row, column].SetSelected(false);
                }
            }
        }
    }

    private void _UpdateDiamondLayout()
    {
        int startingPos = FortGrid.MaxGridSize - mCurrentNationSize;
        int maxPosition = FortGrid.NumElementsInRow - startingPos;
        int middlePosition = FortGrid.NumElementsInRow / 2;

        for (int row = 0; row < FortGrid.NumElementsInRow; ++row)
        {
            for (int column = 0; column < FortGrid.NumElementsInRow; ++column)
            {
                FortGrid.FortSelectors[row, column].SetSelectable(false);
                if (row >= startingPos && row < maxPosition && column >= startingPos && column < maxPosition)
                {
                    int distanceFromCenter = Mathf.Abs(middlePosition - column) + startingPos;
                    if (distanceFromCenter == row || FortGrid.NumElementsInRow - row - 1 == distanceFromCenter)
                    {
                        FortGrid.FortSelectors[row, column].SetSelected(true);
                    }
                    else
                    {
                        FortGrid.FortSelectors[row, column].SetSelected(false);
                    }



                    //if (Mathf.Abs(column - row)  == mCurrentNationSize - 1 || Mathf.Abs(column + row) == mCurrentNationSize - 1)
                    //{
                    //    FortGrid.FortSelectors[row, column].SetSelected(true);
                    //}
                    //else
                    //{
                    //    FortGrid.FortSelectors[row, column].SetSelected(false);
                    //}
                }
                else
                {
                    FortGrid.FortSelectors[row, column].SetSelected(false);
                }
            }
        }
    }
}
