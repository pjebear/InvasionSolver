using Common.Types;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace InvasionSolver
{
    class SearchResults
    {
        // add optimization bools
        public float SearchTimeSeconds { get; private set; }
        public long NumLeafsCreated { get; private set; }
        public long NumSolutionsFound { get; private set; }
        public int DefaultSolutionWaves { get; private set; }
        public InvasionSolution DefaultSolution { get; private set; }
        public int OptimizedSolutionWaves { get; private set; }
        public InvasionSolution OptimizedSolution { get; private set; }

        public SearchResults()
        {
            // empty for serialization
        }

        public SearchResults(float searchTimeSeconds, long numLeafsCreated, long numSolutionsFound, 
            int defaultSolutionWaves, InvasionSolution defaultSolution,
            int optimizedSolutionWaves, InvasionSolution optimizedSolution)
        {
            SearchTimeSeconds = searchTimeSeconds;
            NumLeafsCreated = numLeafsCreated;
            NumSolutionsFound = numSolutionsFound;
            DefaultSolutionWaves = defaultSolutionWaves;
            DefaultSolution = defaultSolution;
            OptimizedSolutionWaves = optimizedSolutionWaves;
            OptimizedSolution = optimizedSolution;
        }
    }

    class AndTree
    {
        private int mMaxDepthOfTree;
        private bool mPruneSolutions; // optimization

        private Stack<SearchState> mOpenLeafs;
        private long mNumLeafsCreated;
        private long mNumSolutionsFound;

        private float mBestSolutionArmyValue;
        private int mBestSolutionDepth;
        private InvasionSolution mBestSolution;

        private ArmyBlueprint mInitialArmy;
        private NationBlueprint mInitialNation;

        public AndTree()
        {
            mOpenLeafs = new  Stack<SearchState>();
        }

        public SearchResults SearchForSolutions(ArmyBlueprint attackers, NationBlueprint defenders, bool keepOnlyBest)
        {
            mInitialArmy = attackers;
            mInitialNation = defenders;

            // Initialize start variables and start invasion search
            SearchState initialState = new SearchState(attackers, defenders, 0, null, null);
            mOpenLeafs.Clear();
            initialState.Prob.NationBlueprint.BeginInvasion(Vector2.right);

            // Do the default search keeping the units together
            mOpenLeafs.Push(initialState);
            // only care about the best solution with army grouped up
            mPruneSolutions = true;
            mMaxDepthOfTree = -1;
            while (mOpenLeafs.Count > 0)
            {
                Search(mOpenLeafs.Pop(), true);
            }
            // record default invasion stats
            InvasionSolution defaultSolution = mBestSolution;
            int bestDefaultDepth = defaultSolution != null ? mBestSolutionDepth : -1;
            // update the max depth of the optimized search to be the depth of the default time
            mMaxDepthOfTree = mBestSolutionDepth;
            Debug.Log("Best Case Group Scenario took " + mMaxDepthOfTree + " turns to complete, needing " + (mMaxDepthOfTree - initialState.Prob.NationBlueprint.NumCitiesRemaining) + " turns to heal");
			
            // reset search variables for the optimized search
            mPruneSolutions = keepOnlyBest;
            mOpenLeafs.Clear();
            mBestSolution = null;

            float searchStartTime = Time.realtimeSinceStartup;
            mOpenLeafs.Push(initialState);
            mNumLeafsCreated = 1;
            while (mOpenLeafs.Count > 0)
            {
				Search(mOpenLeafs.Pop(), false);
            }
            float optimizedSearchDuration = Time.realtimeSinceStartup - searchStartTime;
            InvasionSolution optimizedSolution = mBestSolution;
            int bestOptimizedDepth = optimizedSolution != null ? mBestSolutionDepth : -1;
            return new SearchResults(optimizedSearchDuration, mNumLeafsCreated, mNumSolutionsFound, 
                bestDefaultDepth, defaultSolution, bestOptimizedDepth, optimizedSolution);
        }

        public void Search(SearchState chosenState, bool keepArmyTogether)
        {
            if (chosenState.IsSolved)
            {
                #region SolutionFound
                bool updateSolution = false;
                bool recordSolution = false;

                int numStepsInInvasion = chosenState.Depth;

                if (mBestSolution == null)
                {
                    recordSolution = updateSolution = true;
                    if (mPruneSolutions)
                    {
                        mMaxDepthOfTree = numStepsInInvasion;
                    }
                }
                else if (numStepsInInvasion < mBestSolutionDepth ) // found a better tier of solutions
                {
                    recordSolution = updateSolution = true;
                    mBestSolution = null;
                    // reset solution tracking
                    if (mPruneSolutions)
                    {
                        mNumSolutionsFound = 0;
                        mMaxDepthOfTree = numStepsInInvasion;
                    }
                }
                else if (mBestSolutionDepth == numStepsInInvasion) // at the same tier. Check if better
                {
                    recordSolution = true;
                    if (chosenState.Prob.InvadingArmy.CalculateArmyValue() > mBestSolutionArmyValue)
                    {
                        updateSolution = true;
                    }
                }
                else // worse tier
                {
                    recordSolution = !mPruneSolutions;
                }
                
                if (recordSolution)
                {
                    mNumSolutionsFound++;
                }

                // Create and store new solution 
                if (updateSolution)
                {
                    List <InvasionWave> invasionOrder = new List<InvasionWave>();
                    while (chosenState.TransitionFromParent != null)
                    {
                        invasionOrder.Add(chosenState.TransitionFromParent);
                        chosenState = chosenState.ParentProblem;
                    }
                    invasionOrder.Reverse();
                    InvasionSolution newSolution = new InvasionSolution(mInitialArmy, mInitialNation, invasionOrder);
                   
                    mBestSolution = newSolution;
                    mBestSolutionDepth = numStepsInInvasion;
                    mBestSolutionArmyValue = chosenState.Prob.InvadingArmy.CalculateArmyValue();
                }
                #endregion
            }
            else
            {
                bool validState = true;
                if (chosenState.ParentProblem != null)
                {
                    if (chosenState.ParentProblem.StateValue == chosenState.StateValue)
                    {
                        // do nothing, this state didn't progress the invasion
                        validState = false;
                    }
                }
                // past our max depth
                if (mMaxDepthOfTree != -1 && chosenState.Depth >= mMaxDepthOfTree)
                {
                    validState = false;
                }
                // Add all new leafs possible from this state
                if (validState)
                {
                    List<SearchState> subProblems = Fdiv(chosenState, keepArmyTogether);
                    mNumLeafsCreated += subProblems.Count;
                    foreach (SearchState subProblem in subProblems)
                    {
                        mOpenLeafs.Push(subProblem);
                    }
                }
            }
        }

        public SearchState Ftrans(SearchState initialState, InvasionWave transition)
        {
            List<ArmyBlueprint> armiesAfterAttack = new List<ArmyBlueprint>();
            List<Fortification> captured = new List<Fortification>();
            for (int subArmyIdx = 0; subArmyIdx < transition.Wave.Count; ++subArmyIdx)
            {
                AssaultTemplate assaultTemplate = transition.Wave[subArmyIdx];
                // run assault simulation. Capture fort, deal damage to invaders, and heal up after if possible
                ArmyBlueprint afterAssault = assaultTemplate.AssaultFortification(); 
                armiesAfterAttack.Add(afterAssault);
                // was there an unneccessary step in keeping units in reserve
                if (assaultTemplate.ToAssault.IsPlaceHolderFort && assaultTemplate.Attackers.CalculateArmyValue() == afterAssault.CalculateArmyValue())
                {
                    return null;
                }
                if (!assaultTemplate.ToAssault.IsPlaceHolderFort)
                {
                    captured.Add(assaultTemplate.ToAssault);
                }
            }
          
            return new SearchState(ArmyBlueprint.MergeSubArmies(armiesAfterAttack), new NationBlueprint(initialState.Prob.NationBlueprint, captured), initialState.Depth + 1, initialState, transition);
        }

        public List<SearchState> Fdiv(SearchState state, bool keepArmyTogether)
        {
            Debug.Assert(!state.IsSolved, "Attempting to apply Div on a solved problem");

            List<Fortification> fortifications = new List<Fortification>(state.Prob.NationBlueprint.GetBorderCities());
            ArmyBlueprint invadingArmy = state.Prob.InvadingArmy;
            fortifications.Add(new Fortification());

            List<SearchState> problemDivisions = new List<SearchState>();

            // pick most difficult city to capture
            // all cities are of same dificulty for now so just choose the first
            Fortification hardestCity = fortifications[0];
            // Since damage doesn't increase over time, if the army can't take the city now it will never be able to
            if (invadingArmy.Damage < hardestCity.Defense) 
            {
                return problemDivisions;
            }
            // If the army is strong enough to take the city but doesn't have enough health, 
            // and it either can't heal, or it's max health even after healing wont be enough to take the city,
            // abandon this search
            else if (invadingArmy.Health <= hardestCity.Offense
                && !(invadingArmy.CanHeal && invadingArmy.MaxHealth > hardestCity.Offense))
            {

                return problemDivisions;
            }
            // come up with all possible transitions
            else
            {
                List<InvasionWave> armyDistributions = new List<InvasionWave>();
                RecurseFdiv(
                    new ArmyBlueprint(invadingArmy.Soldiers, invadingArmy.Healers, invadingArmy.Archers),
                    0, fortifications, new InvasionWave(), armyDistributions, keepArmyTogether);

                foreach (InvasionWave transition in armyDistributions)
                {
                    SearchState nextState = Ftrans(state, transition);
                    // if the transition proved to be wasteful, discard
                    if (nextState != null)
                    {
                        problemDivisions.Add(nextState);
                    }
                }
            }
            return problemDivisions;
        }

        private static void RecurseFdiv(ArmyBlueprint armyToSubdivide, int currentGroup, List<Fortification> toCapture, InvasionWave currentWave, List<InvasionWave> possibleInvasionWaves, bool keepArmyTogether)
        {
            if (currentGroup == toCapture.Count - 1) // last fort to capture
            {
                if (!armyToSubdivide.IsEmpty)
                {
					currentWave.Wave.Add(AssaultTemplate.GetAssualtTemplate(toCapture[currentGroup], armyToSubdivide));
                }
				possibleInvasionWaves.Add(currentWave);
            }
            else if (armyToSubdivide.IsEmpty) // No units left to allocate to the remaining sub armies
            {
				possibleInvasionWaves.Add(currentWave);
            }
            else
            {
                int armySize = armyToSubdivide.Size;
                long numDifferentCombinations = (long)Mathf.Pow(2, armySize);
                IteratorCache invasionCache = new IteratorCache();

                for (long combinationNumber = numDifferentCombinations - 1; combinationNumber >= 0; combinationNumber--)
                {
                    ArmyBlueprint subArmy = new ArmyBlueprint();
                    ArmyBlueprint remainingArmy = new ArmyBlueprint();

                    #region Create SubArmy from combination number
                    BitArray combinationArray = new BitArray(System.BitConverter.GetBytes(combinationNumber));
                    for (int j = 0; j < armySize; ++j)
                    {
                        if (j < armyToSubdivide.Soldiers.Count)
                        {
                            if (combinationArray[j])
                            {
                                subArmy.Soldiers.Add(armyToSubdivide.Soldiers[j]);
                            }
                            else
                            {
                                remainingArmy.Soldiers.Add(armyToSubdivide.Soldiers[j]);
                            }
                        }
                        else if (j < armyToSubdivide.Soldiers.Count + armyToSubdivide.Healers.Count)
                        {
                            if (combinationArray[j])
                            {
                                subArmy.Healers.Add(armyToSubdivide.Healers[j - armyToSubdivide.Soldiers.Count]);
                            }
                            else
                            {
                                remainingArmy.Healers.Add(armyToSubdivide.Healers[j - armyToSubdivide.Soldiers.Count]);
                            }
                        }
                        else
                        {
                            if (combinationArray[j])
                            {
                                subArmy.Archers.Add(armyToSubdivide.Archers[j - armyToSubdivide.Soldiers.Count - armyToSubdivide.Healers.Count]);
                            }
                            else
                            {
                                remainingArmy.Archers.Add(armyToSubdivide.Archers[j - armyToSubdivide.Soldiers.Count - armyToSubdivide.Healers.Count]);
                            }
                        }
                    }
                    #endregion

                    // Only consider iteration if it can capture the city it was tasked to take

                    if (combinationNumber == 0) // Empty sub army
                    {
                        InvasionWave recursiveTransitionCopy = new InvasionWave(currentWave.Wave);
                        RecurseFdiv(remainingArmy, currentGroup + 1, toCapture, recursiveTransitionCopy, possibleInvasionWaves, keepArmyTogether);
                    }
                    else if (!invasionCache.IsCached(subArmy))
                    {
                        AssaultTemplate assaultTemplate = AssaultTemplate.GetAssualtTemplate(toCapture[currentGroup], subArmy);
                        if (assaultTemplate != null /* is valid assault */)
                        {
                            invasionCache.Cache(subArmy);
                            InvasionWave recursiveTransitionCopy = new InvasionWave(currentWave.Wave);
                            recursiveTransitionCopy.Wave.Add(assaultTemplate);
                            RecurseFdiv(remainingArmy, currentGroup + 1, toCapture, recursiveTransitionCopy, possibleInvasionWaves, keepArmyTogether);
                        }
                    } 
             

                    if (keepArmyTogether && combinationNumber == numDifferentCombinations - 1) // first sub army created
                    {
                        combinationNumber = 1; // will become 0 next loop iteration and second army option will be empty
                    }
                }
            }
        }
    }

    class SearchState
    {
        private static long sStateNumber = 0;
        private long mStateNumber;
        public int Depth;
        public Prob Prob;
        public InvasionWave TransitionFromParent;
        public bool IsSolved { get { return Prob.IsSolved(); } }
        public float StateValue { get { return Prob.ProblemValue; } }

        public SearchState ParentProblem;

        public SearchState(ArmyBlueprint army, NationBlueprint defender, int depth, SearchState parent, InvasionWave fromParent)
        {
            if (depth == 0)
            {
                sStateNumber = 0;
            }
            mStateNumber = sStateNumber++;
            Depth = depth;
            Prob = new Prob(army, defender);
            ParentProblem = parent;
            TransitionFromParent =  fromParent != null ? new InvasionWave(fromParent.Wave) : null;
        }

        public override string ToString()
        {
            return string.Format("[{0}](pr,{1})\n", mStateNumber, IsSolved ? "yes" : "?") +  Prob.ToString();
        }
    }

    class Prob
    {
        public ArmyBlueprint InvadingArmy;
        public NationBlueprint NationBlueprint;
        public float ProblemValue
        {
            get
            {
                return ArmyBlueprint.InitialArmyValue - InvadingArmy.CalculateArmyValue() // minimal if army has not lost units/health
                    + NationBlueprint.GetNationValue(); // minimal if all nations have been captured
            }
        }
        public Prob(ArmyBlueprint army, NationBlueprint defender)
        {
            InvadingArmy = army;
            NationBlueprint = defender;
        }

        public bool IsSolved()
        {
            return NationBlueprint.IsDefeated;
        }

        public override string ToString()
        {
            return NationBlueprint.ToString() + "\n " + InvadingArmy.ToString();
        }
    }

    class IteratorCache
    {
        Dictionary<int, HashSet<ArmyBlueprint>> mPriorityCache;
        public IteratorCache()
        {
            mPriorityCache = new Dictionary<int, HashSet<ArmyBlueprint>>();
        }

        public bool IsCached(ArmyBlueprint invasionGroup)
        {
            if (mPriorityCache.ContainsKey(invasionGroup.Size))
            {
                foreach (ArmyBlueprint skeleton in mPriorityCache[invasionGroup.Size])
                {
                    if (invasionGroup.Equals(skeleton))
                        return true;
                }
            }
            return false;
        }

        public void Cache(ArmyBlueprint invasionGroup)
        {
            if (!mPriorityCache.ContainsKey(invasionGroup.Size))
            {
                HashSet<ArmyBlueprint> toAdd = new HashSet<ArmyBlueprint>();
                toAdd.Add(invasionGroup);
                mPriorityCache.Add(invasionGroup.Size, toAdd);
            }
            else
            {
                mPriorityCache[invasionGroup.Size].Add(invasionGroup);
            }
        }
    }
}





