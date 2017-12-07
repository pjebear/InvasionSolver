
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using Common.Types;
using UnityEngine.UI;
using System.Collections;
using Assets._Scripts.AnimationScripts;
using InvasionSolver;


public class AnimationManager : MonoBehaviour
{
    public bool LoadSimulationFromFile = false;

    public GameObject ButtonParent;

    public FortController FortPrefab;
    public NationController NationController;

    public ArmyPositionControl ArmyPrefab;
    public Transform ArmyZone;
    public int DefaultArmyNumber = 10;
    private List<ArmyPositionControl> mArmyControllers;
    private ArmyPositionControl mMainArmy;
    private Vector2 mMainArmyZoneSize;

    public float AnimationSpeed = 1f;
    private float mAnimationDuration = 2f;

    public GameObject SimulationButtonParent;
    public Button AutoContinueButton;
    public Button ContinueButton;

    public GameObject AnimationNavigationParent;

    private bool mIsAutoSimulation;
    private bool mIsWaitingOnContinue;

    public Text BannerText;


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
        SimulationButtonParent.gameObject.SetActive(false);
        AnimationNavigationParent.SetActive(false);
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BeginAnimation(InvasionSolution defaultSolution, InvasionSolution optimizedSolution)
    {
        mMainArmy.BoundingArea = mMainArmyZoneSize = ArmyZone.GetComponent<RectTransform>().rect.size;
        StartCoroutine(_BeginAnimation(defaultSolution, optimizedSolution));   
    }

    private IEnumerator _BeginAnimation(InvasionSolution defaultSolution, InvasionSolution optimizedSolution)
    {
        mMainArmy.gameObject.SetActive(true);

        mMainArmy.ResetArmy();
        NationController.ResetNation();
        mMainArmy.Initialize(defaultSolution.InitialArmy);
        NationController.InitializeNation(defaultSolution.InitialNation);
        yield return AnimateInvasion(defaultSolution);
        mMainArmy.ResetArmy();
        mMainArmy.Initialize(optimizedSolution.InitialArmy);
        NationController.ResetNation();
        NationController.InitializeNation(optimizedSolution.InitialNation);
        yield return AnimateInvasion(optimizedSolution);
        SimulationButtonParent.SetActive(false);
        AnimationNavigationParent.SetActive(true);
    }

    IEnumerator AnimateInvasion(InvasionSolution solution)
    {
        yield return new WaitForFixedUpdate();
        AnimationNavigationParent.SetActive(false);
        SimulationButtonParent.gameObject.SetActive(true);
        ToggleAutoContinue(true);
        ContinueButton.interactable = false;
        mIsWaitingOnContinue = false;
        
        float subArmyAnimationDuration = mAnimationDuration * AnimationSpeed;
        float assaultHealthDuration = mAnimationDuration * AnimationSpeed;
        float reformAnimationDuration = mAnimationDuration * AnimationSpeed;

        List<InvasionWave> bestSolution = solution.InvasionOrder;
        for (int i = 0; i < bestSolution.Count; ++i)
        {
            BannerText.text = "WAVE " + (i + 1);
            List<AssaultTemplate> attackWave = bestSolution[i].Wave;
            Debug.Assert(DefaultArmyNumber >= attackWave.Count);
            List<ArmyBlueprint> subArmies = new List<ArmyBlueprint>();
            foreach (var wave in attackWave)
            {
                subArmies.Add(wave.Attackers);
            }
            FormSubArmies(subArmies, subArmyAnimationDuration);
            yield return _WaitForInputAfterAnimation(subArmyAnimationDuration * 1.5f);

            // display "Attack wave #"
            int subArmyIdx = 0;
            foreach (AssaultTemplate attack in attackWave)
            {
                ArmyBlueprint afterAttack = attack.AssaultFortification(false);
                mArmyControllers[subArmyIdx].UpdateHealth(afterAttack, assaultHealthDuration);
                if (!attack.ToAssault.IsPlaceHolderFort)
                {
                    NationController.DestroyFort(attack.ToAssault, assaultHealthDuration);
                    yield return _WaitForInputAfterAnimation(assaultHealthDuration * 1.5f);
                }
               
                afterAttack.Heal();
                mArmyControllers[subArmyIdx].UpdateHealth(afterAttack, assaultHealthDuration);
                yield return _WaitForInputAfterAnimation(assaultHealthDuration * 1.5f);

                subArmyIdx++;
            }

            MergeSubArmies(reformAnimationDuration);
            NationController.UpgradeForts(reformAnimationDuration);
            yield return _WaitForInputAfterAnimation(reformAnimationDuration * 1.5f);
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

    public void ToggleAutoContinue()
    {
        ToggleAutoContinue(!(mIsAutoSimulation ^ false));
    }

    private void ToggleAutoContinue(bool isAutoContinue)
    {
        mIsAutoSimulation = isAutoContinue;
        AutoContinueButton.GetComponentInChildren<Text>().text = "AutoContinue: " + (mIsAutoSimulation ? "ON" : "OFF");
    }

    public void ContinueAnimation()
    {
        mIsWaitingOnContinue = false;
    }

    private IEnumerator _WaitForInputAfterAnimation(float animationTime)
    {
        yield return new WaitForSeconds(animationTime);

        if (!mIsAutoSimulation)
        {
            ContinueButton.interactable = true;
            mIsWaitingOnContinue = true;
            yield return new WaitWhile(() => { return mIsWaitingOnContinue && !mIsAutoSimulation; });
            ContinueButton.interactable = false;
            mIsWaitingOnContinue = false;
        }
    }

    private void WaitForInputAfterAnimation(float animationTime)
    {
        while ((animationTime -= Time.deltaTime) > 0) { /* wait*/ }
        float timeout = 20f;
        if (!mIsAutoSimulation)
        {
            ContinueButton.interactable = true;
            mIsWaitingOnContinue = true;
            while (mIsWaitingOnContinue && !mIsAutoSimulation && (timeout -= Time.deltaTime) > 0) { /* wait*/ }
            ContinueButton.interactable = false;
            mIsWaitingOnContinue = false;
        }
    }
}



