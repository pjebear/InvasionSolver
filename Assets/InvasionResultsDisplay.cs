using InvasionSolver;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvasionResultsDisplay : MonoBehaviour {

    public ResultsDisplay DefaultDisplay;
    public ResultsDisplay OptimizedDisplay;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void DisplayResults(SearchResults results)
    {
        DefaultDisplay.DisplayResults(results.DefaultSolution);
        OptimizedDisplay.DisplayResults(results.OptimizedSolution, 
            results.NumLeafsCreated, results.NumSolutionsFound, results.NumSolutionsFound / (float)results.NumLeafsCreated );
    }
}
