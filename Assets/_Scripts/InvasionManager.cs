
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using Common.Types;
using UnityEngine.UI;
using System.Collections;
using Assets._Scripts.AnimationScripts;
using InvasionSolver;
using System.IO;

class InvasionManager : MonoBehaviour
{
    public bool LoadSimulationFromFile = false;

    public FortScript FortPrefab;
    public ArmyPositionControl ArmyPrefab;
    public Transform ArmyZone;
    public int DefaultArmyNumber = 10;
    private List<ArmyPositionControl> mArmyControllers;
    private ArmyPositionControl mMainArmy;
    private Vector2 mMainArmyZoneSize;
    public float AnimationSpeed = 1f;
    private float mAnimationDuration = 2f;

    public NationController NationController;

    public Text BestSolutionFoundText;
    public Text DefaultSolutionFoundText;
    public Text NumLeafsCreatedText;
    public Text NumberOfSolutionsText;
    public Text BannerText;

    private AndTree mAndTree;
    private SearchResults mSearchResults;
    private bool mSimulationRunning;
    private bool mAnimationStart;

    private void Awake()
    {
        Debug.Assert(DefaultArmyNumber > 0);
        mArmyControllers = new List<ArmyPositionControl>();
        for (int i = 0; i < DefaultArmyNumber; ++i)
        {
            ArmyPositionControl army = Instantiate(ArmyPrefab, ArmyZone).GetComponent<ArmyPositionControl>();
            army.gameObject.name = "Army " + i;
            army.gameObject.SetActive(false);
            mArmyControllers.Add(army);
        }
        mMainArmy = mArmyControllers[0];
    }

    // Use this for initialization
    void Start()
    {
        if (LoadSimulationFromFile && File.Exists(InvasionSolution.FilePath))
        {
            Debug.Log("Loading Invasion From File...");
            InvasionSolution savedSolution = InvasionSolution.Load();

            // Display Army
            mMainArmy.gameObject.SetActive(true);
            mMainArmy.BoundingArea = mMainArmyZoneSize = ArmyZone.GetComponent<RectTransform>().rect.size;
            mMainArmy.Initialize(savedSolution.InitialArmy);

            // Display Nation
            NationController.InitializeNation(savedSolution.InitialNation);
            StartCoroutine(AnimateInvasion(savedSolution));
        }
        else
        {
            Debug.Log("Searching For Optimal Solution...");
            mAndTree = new AndTree();
            NationBlueprint initialNation = new NationBlueprint(6);
            ArmyBlueprint initialArmy = new ArmyBlueprint(2, 2, 3);

            // Display Army
            mMainArmy.gameObject.SetActive(true);
            mMainArmy.BoundingArea = mMainArmyZoneSize = ArmyZone.GetComponent<RectTransform>().rect.size;
            mMainArmy.Initialize(initialArmy);

            // Display Nation
            NationController.InitializeNation(initialNation);
            StartCoroutine(Simulate(initialArmy, initialNation));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!mSimulationRunning)
        {
            BannerText.gameObject.SetActive(false);
        }
        if (mAnimationStart && mSimulationRunning)
        {
            mSimulationRunning = false;
            mAnimationStart = false;
            if (mSearchResults.OptimizedSolution != null)
            {
                InvasionSolution solution = mSearchResults.OptimizedSolution;
                BestSolutionFoundText.text = mSearchResults.OptimizedSolutionWaves.ToString();
                DefaultSolutionFoundText.text = mSearchResults.DefaultSolutionWaves.ToString();
                NumberOfSolutionsText.text = mSearchResults.NumSolutionsFound.ToString();
                NumLeafsCreatedText.text = mSearchResults.NumLeafsCreated.ToString();
                solution.Save();
                StartCoroutine(AnimateInvasion(solution));
            }
            else
            {
                Debug.Log("No Solution");
            }
        }
    }

    IEnumerator Simulate(ArmyBlueprint attackers, NationBlueprint defenders)
    {
        // Update banner saying simulation begun
        yield return new WaitForSeconds(0.5f);
        mSimulationRunning = true;
        mSearchResults = mAndTree.SearchForSolutions(attackers, defenders, true);
        Debug.Log("SearchTime: " + mSearchResults.SearchTimeSeconds);
        Debug.Log("Num Nodes Created: " + mSearchResults.NumLeafsCreated);
        Debug.Log("Num Solutions Found: " + mSearchResults.NumSolutionsFound);
        mAnimationStart = true;
    }

    //IEnumerator UpdateLabelsThroughoutSimulation()
    //{
    //    while (mSimulationRunning)
    //    {
    //        BestSolutionFoundText.text = mAndTree.BestSolutionTime.ToString();
    //        DefaultSolutionFoundText.text = mAndTree.DefaultSolutionTime.ToString();
    //        NumberOfSolutionsText.text = mAndTree.NumSolutionsFound.ToString();
    //        OpenLeafsText.text = mAndTree.OpenLeafs.Count.ToString();
    //        yield return new WaitForFixedUpdate();
    //    }
    //    BestSolutionFoundText.text = mAndTree.BestSolutionTime.ToString();
    //    DefaultSolutionFoundText.text = mAndTree.DefaultSolutionTime.ToString();
    //    NumberOfSolutionsText.text = mAndTree.NumSolutionsFound.ToString();
    //    OpenLeafsText.text = mAndTree.OpenLeafs.Count.ToString();
    //}

    IEnumerator AnimateInvasion(InvasionSolution solution)
    {
        float subArmyAnimationDuration = mAnimationDuration * AnimationSpeed;
        float assaultHealthDuration = mAnimationDuration * AnimationSpeed;
        float reformAnimationDuration = mAnimationDuration * AnimationSpeed;

        List<InvasionWave> bestSolution = solution.InvasionOrder;
        for (int i = 0; i < bestSolution.Count; ++i)
        {
            BannerText.text = "WAVE " + i;
            List<AssaultTemplate> attackWave = bestSolution[i].Wave;
            Debug.Assert(DefaultArmyNumber >= attackWave.Count);
            List<ArmyBlueprint> subArmies = new List<ArmyBlueprint>();
            foreach (var wave in attackWave)
            {
                subArmies.Add(wave.Attackers);
            }
            FormSubArmies(subArmies, subArmyAnimationDuration);
            yield return new WaitForSeconds(subArmyAnimationDuration * 1.5f);

            // display "Attack wave #"
            int subArmyIdx = 0;
            foreach (AssaultTemplate attack in attackWave)
            {
                ArmyBlueprint afterAttack = attack.AssaultFortification(false);
                mArmyControllers[subArmyIdx].UpdateHealth(afterAttack, assaultHealthDuration);
                if (!attack.ToAssault.IsPlaceHolderFort)
                    NationController.DestroyFort(attack.ToAssault.FortificationId);
                yield return new WaitForSeconds(assaultHealthDuration * 1.5f);
                afterAttack.Heal();
                mArmyControllers[subArmyIdx].UpdateHealth(afterAttack, assaultHealthDuration);
                yield return new WaitForSeconds(assaultHealthDuration * 1.5f);

                subArmyIdx++;
            }

            MergeSubArmies(reformAnimationDuration);
            yield return new WaitForSeconds(reformAnimationDuration * 1.5f);
        }
        BannerText.text = "Invasion Complete!";
    }

    private void FormSubArmies(List<ArmyBlueprint> subArmies, float animationSpeed)
    {
        int numSubArmies = subArmies.Count;
        float gridSize = Mathf.CeilToInt(Mathf.Sqrt(numSubArmies));
        Vector2 cellSize = mMainArmyZoneSize / gridSize;

        Debug.Assert(mMainArmy.gameObject.activeInHierarchy);
        var splitArmy = mArmyControllers[0].SplitIntoSubArmies(subArmies);
        int subArmyIdx = 0;
        float columnNum = gridSize - 1;
        float rowNum = 0;
        for (int i = 0; i < subArmies.Count; ++i)
        {
            // set Position
            mArmyControllers[subArmyIdx].transform.localPosition = new Vector3(cellSize.x * columnNum, -cellSize.y * rowNum++);
            mArmyControllers[subArmyIdx].BoundingArea = cellSize;

            mArmyControllers[subArmyIdx].gameObject.SetActive(true);
            mArmyControllers[subArmyIdx++].Initialize(splitArmy[i], animationSpeed);
            if (rowNum == gridSize)
            {
                rowNum = 0;
                columnNum--;
            }
        }
    }

    private void UpdateHealth(List<ArmyBlueprint> subArmies, float animationSpeed)
    {
        int subArmyIdx = 0;
        foreach (var subarmy in subArmies)
        {
            Debug.Assert(mArmyControllers[subArmyIdx].gameObject.activeInHierarchy);
            mArmyControllers[subArmyIdx++].UpdateHealth(subarmy, animationSpeed);
        }
    }

    private void MergeSubArmies(float animationSpeed)
    {
        int subArmyIdx = 0;
        List<ArmyPositionControl> controls = new List<ArmyPositionControl>();
        foreach (var subarmy in mArmyControllers)
        {
            if (subarmy.gameObject.activeInHierarchy)
            {
                controls.Add(subarmy);
                if (subArmyIdx++ > 0)
                {
                    subarmy.gameObject.SetActive(false);
                }
            }
        }
        mMainArmy.BoundingArea = mMainArmyZoneSize;
        mMainArmy.transform.localPosition = Vector3.zero;
        mMainArmy.Initialize(controls, animationSpeed);
    }
}



