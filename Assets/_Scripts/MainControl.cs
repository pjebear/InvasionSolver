using Common.Types;
using InvasionSolver;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

    private Thread mSimulationThread;
    private AndTree mAndTree;
    private NationBlueprint mInitialNation;
    private ArmyBlueprint mInitialInvaders;
    private SearchResults mSearchResults;
    private bool mSearchComplete;

    // Use this for initialization
    void Start()
    {
        mSearchComplete = true;
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
        Debug.Assert(mSearchComplete);
        InvasionInputParent.gameObject.SetActive(false);
        InvasionAnimationParent.gameObject.SetActive(false);
        InvasionResultsParent.SetActive(true);

        mInitialInvaders = new ArmyBlueprint(SoldierCounter.InvaderCount, HealerCounter.InvaderCount, ArcherCounter.InvaderCount);
        mInitialNation = new NationBlueprint(NationLayoutController.GetNationLayout());
        StartCoroutine(_BeginSimulation());
    }

    private IEnumerator _BeginSimulation()
    {
        mAndTree = new AndTree();
        mSearchComplete = false;
        mSimulationThread = new Thread(new ThreadStart(SimulationThread));
        float searchStartTime = Time.realtimeSinceStartup;
        mSimulationThread.Start();
        StartCoroutine(_DisplaySimulationProgress());
        yield return new WaitWhile(() => { return !mSearchComplete; });
        float searchDuration = Time.realtimeSinceStartup - searchStartTime;
        mSearchResults.SearchTimeSeconds = searchDuration;

        InvasionResultsParent.SetActive(true);
        ResultsDisplay.DisplayResults(mSearchResults);
        mSearchResults.Save();
    }

    private IEnumerator _DisplaySimulationProgress()
    {
        do
        {
            ResultsDisplay.UpdateSearch(mAndTree.OpenLeafCount, mAndTree.SolutionsFound);
            yield return new WaitForSeconds(0.5f);
        }
        while (!mSearchComplete);
    }

    public void BeginAnimation()
    {
        InvasionResultsParent.SetActive(false);
        InvasionAnimationParent.gameObject.SetActive(true);
        AnimationManager.BeginAnimation(mSearchResults.DefaultSolution, mSearchResults.OptimizedSolution);
    }

    public void LoadSimulation()
    {
        mSearchResults = SearchResults.Load();
        NewOrLoadButtonParent.gameObject.SetActive(false);
        InvasionAnimationParent.gameObject.SetActive(true);
        BeginAnimation();
    }

    private void SimulationThread()
    {
        mSearchResults = mAndTree.SearchForSolutions(mInitialInvaders, mInitialNation, true);
        mSearchComplete = true;
    }

    private void OnApplicationQuit()
    {
        if (mSimulationThread != null)
        {
            mSimulationThread.Abort();
        }
    }
}
