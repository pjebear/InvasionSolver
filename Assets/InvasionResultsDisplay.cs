using InvasionSolver;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvasionResultsDisplay : MonoBehaviour {

    public GameObject ResultsParent;
    public ResultsDisplay DefaultDisplay;
    public ResultsDisplay OptimizedDisplay;

    public GameObject ProgressParent;
    public Text OpenLeafsText;
    public Text SolutionsFoundText;

    private bool mDisplayingProgress;


	// Use this for initialization
	void Start () {
        mDisplayingProgress = false;

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void UpdateSearch(long numOpenLeafs, long numSolutionsFound)
    {
        if (!mDisplayingProgress)
        {
            mDisplayingProgress = true;
            ResultsParent.SetActive(false);
            ProgressParent.SetActive(true);
        }
        
        OpenLeafsText.text = numOpenLeafs.ToString();
        SolutionsFoundText.text = numSolutionsFound.ToString();
    }

    public void DisplayResults(SearchResults results)
    {
        if (mDisplayingProgress)
        {
            mDisplayingProgress = false;
            ResultsParent.SetActive(true);
            ProgressParent.SetActive(false);
        }
        ProgressParent.SetActive(false);
        ResultsParent.SetActive(true);
        DefaultDisplay.DisplayResults(results.DefaultSolution);
        OptimizedDisplay.DisplayResults(results.OptimizedSolution, 
            results.NumLeafsCreated, results.NumSolutionsFound, results.NumSolutionsFound / (float)results.NumLeafsCreated );
    }
}
