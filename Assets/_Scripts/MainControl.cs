using Common.Types;
using InvasionSolver;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MainControl : MonoBehaviour {

    public GameObject NewOrLoadButtonParent;
    public Button LoadButton;

    public GameObject InvasionInputParent;
    public InvaderCountSelector SoldierCounter;
    public InvaderCountSelector ArcherCounter;
    public InvaderCountSelector HealerCounter;
    public NationLayoutController NationLayoutController;

    public GameObject InvasionResultsParent;
    public InvasionResultsDisplay ResultsDisplay;

    public GameObject InvasionAnimationParent;
    public AnimationManager AnimationManager;

    private AndTree mAndTree;
    private SearchResults mSearchResults;

    // Use this for initialization
    void Start()
    {
        NewOrLoadButtonParent.gameObject.SetActive(true);
        InvasionInputParent.gameObject.SetActive(false);
        InvasionAnimationParent.gameObject.SetActive(false);
        InvasionResultsParent.SetActive(false);
        LoadButton.interactable = File.Exists(SearchResults.FILE_PATH);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void NewSimulation()
    {
        NewOrLoadButtonParent.gameObject.SetActive(false);
        InvasionInputParent.gameObject.SetActive(true);
        InvasionAnimationParent.gameObject.SetActive(false);
        InvasionResultsParent.SetActive(false);
    }

    public void BeginSimulation()
    {
        InvasionInputParent.gameObject.SetActive(false);
        InvasionResultsParent.SetActive(false);
        InvasionAnimationParent.gameObject.SetActive(true);

        ArmyBlueprint army = new ArmyBlueprint(SoldierCounter.InvaderCount, HealerCounter.InvaderCount, ArcherCounter.InvaderCount);
        NationBlueprint nation = new NationBlueprint(NationLayoutController.GetNationLayout());

        mAndTree = new AndTree();
        mSearchResults = mAndTree.SearchForSolutions(army, nation, true);

        InvasionResultsParent.SetActive(true);
        ResultsDisplay.DisplayResults(mSearchResults);
        mSearchResults.Save();
    }

    public void BeginAnimation()
    {
        InvasionResultsParent.SetActive(false);
        AnimationManager.BeginAnimation(mSearchResults.DefaultSolution, mSearchResults.OptimizedSolution);
    }

    public void LoadSimulation()
    {
        mSearchResults = SearchResults.Load();
        NewOrLoadButtonParent.gameObject.SetActive(false);
        InvasionAnimationParent.gameObject.SetActive(true);
        BeginAnimation();
    }
}
