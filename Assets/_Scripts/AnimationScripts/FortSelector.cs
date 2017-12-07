using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FortSelector : MonoBehaviour {

    private Color mSelectedFade = Color.white;
    private Color mUnSelectedFade = new Color(1, 1, 1, .5f);
    private Image mSelectorImage;

    public bool Selected;
    private bool mLockSelecting;

    private void Awake()
    {
        mSelectorImage = GetComponent<Image>();
        Selected = false;
        mSelectorImage.color = mUnSelectedFade;
        mLockSelecting = false;
    }

    public void ToggleSelected()
    {
        if (!mLockSelecting)
        {
            Selected = Selected ? false : true;
            mSelectorImage.color = Selected ? mSelectedFade : mUnSelectedFade;
        }
    }

    public void SetSelectable(bool selectable)
    {
        mLockSelecting = !selectable;
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
        mSelectorImage.color = Selected ? mSelectedFade : mUnSelectedFade;
    }
}