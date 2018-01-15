using InvasionSolver;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultsDisplay : MonoBehaviour {

    public Text SuccessResult;
    public Text NumWavesResult;
    public Text InitialCities;
    public Text FinalCities;
    public Text InitialArmyHealth;
    public Text FinalArmyHealth;
    public GameObject OptimalResultsParent;
    public Text NumSearchStates;
    public Text NumSolutionsFound;
    public Text DifficultyRating;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void DisplayResults(InvasionSolution solution, long numSearchStates, long numSolutionsFound, float difficultyRating)
    {
        DisplaySolution(solution);
        OptimalResultsParent.SetActive(true);
        NumSearchStates.text = numSearchStates.ToString();
        NumSolutionsFound.text = numSolutionsFound.ToString();
        DifficultyRating.text = difficultyRating.ToString("0.00");
    }

    public void DisplayResults(InvasionSolution solution)
    {
        DisplaySolution(solution);
        OptimalResultsParent.SetActive(false);
    }

    private void DisplaySolution(InvasionSolution solution)
    {
        SuccessResult.text = solution.IsCompleteSolution.ToString();
        NumWavesResult.text = solution.NumTurnsToSolve.ToString();
        InitialCities.text = solution.InitialNation.Fortifications.Count.ToString();
        FinalCities.text = solution.FinalNation.NumCitiesRemaining.ToString();
        InitialArmyHealth.text = solution.InitialArmy.Health.ToString();
        FinalArmyHealth.text = solution.FinalArmy.Health.ToString();
    }

}
