using Common.Types;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.IO;

namespace InvasionSolver
{
    [XmlRoot("SearchResults")]
    public class SearchResults
    {
        public const string FILE_PATH = "InvasionResults.xml";
        // add optimization bools
        public float SearchTimeSeconds;
        public long NumLeafsCreated { get; private set; }
        public long NumSolutionsFound { get; private set; }
        public InvasionSolution DefaultSolution { get; private set; }
        public InvasionSolution OptimizedSolution { get; private set; }

        public SearchResults()
        {
            // empty for serialization
        }

        public SearchResults(long numLeafsCreated, long numSolutionsFound, 
             InvasionSolution defaultSolution,
             InvasionSolution optimizedSolution)
        {
            SearchTimeSeconds = -1;
            NumLeafsCreated = numLeafsCreated;
            NumSolutionsFound = numSolutionsFound;
            DefaultSolution = defaultSolution;
            OptimizedSolution = optimizedSolution;
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(SearchResults));
            using (var fileStream = new FileStream(FILE_PATH, FileMode.Create))
            {
                serializer.Serialize(fileStream, this);
            }
        }

        public static SearchResults Load()
        {
            var serializer = new XmlSerializer(typeof(SearchResults));
            using (var fileStream = new FileStream(FILE_PATH, FileMode.Open))
            {
                return serializer.Deserialize(fileStream) as SearchResults;
            }
        }
    }

    class AndTree
    {
        private int mMaxDepthOfTree;
        private bool mPruneSolutions; // optimization

        public long OpenLeafCount { get { return mOpenLeafs.Count; } }
        public long SolutionsFound { get { return mNumSolutionsFound; } }

        private LinkedList<SearchState> mOpenLeafs;
        private long mNumLeafsCreated;
        private long mNumSolutionsFound;

        private float mBestPartialSolutionValue; // used to record non successful invasions 
        private SearchState mBestPartialSolution;

        private float mBestSolutionArmyValue;
        private int mBestSolutionDepth;
        private InvasionSolution mBestSolution;

        private static bool mIsOptimized;

        private ArmyBlueprint mInitialArmy;
        private NationBlueprint mInitialNation;

        private float mCurrentCompletionPercent;
        private float mPercentTier;

        public AndTree()
        {
            mOpenLeafs = new  LinkedList<SearchState>();
        }

        public SearchResults SearchForSolutions(ArmyBlueprint attackers, NationBlueprint defenders, bool optimize)
        {
            mInitialArmy = attackers;
            mInitialNation = defenders;
            mIsOptimized = true; /* = optimize; Replace after data collection*/

            // Initialize start variables and start invasion search
            SearchState initialState = new SearchState(attackers, defenders, 0, null, null);
            mOpenLeafs.Clear();
                // orrient the nations layout relative to the army. TODO: allow for dynamic invasion direction
            initialState.Prob.NationBlueprint.BeginInvasion(Vector2.right); 

            // Do the default search keeping the units together
            mOpenLeafs.AddLast(initialState);
            mPruneSolutions = true; // only care about the best solution with army grouped up. Only record 
            mMaxDepthOfTree = mBestSolutionDepth = -1; // max depth = -1 until a solution is found creating a limit to depth

            // Search for solutions with linear invasion strategy
            while (mOpenLeafs.Count > 0)
            {
                SearchState pop = mOpenLeafs.Last.Value;
                mOpenLeafs.RemoveLast();
                Search(pop, true);
            }
            // record default invasion stats
            InvasionSolution linearSolution = null;
            if (mBestSolution == null) // no solution was found
            {
                // record the best result possible 
                linearSolution = ToSolution(mBestPartialSolution, mInitialArmy, mInitialNation);
            }
            else
            {
                linearSolution = mBestSolution;
            }
            
            int bestLinearDepth = linearSolution != null ? mBestSolutionDepth : -1;
            // update the max depth of the optimized search to be the depth of the default time
            mMaxDepthOfTree = bestLinearDepth;
            //Debug.Log("Best Case Group Scenario took " + mMaxDepthOfTree + " turns to complete, needing " + (mMaxDepthOfTree - initialState.Prob.NationBlueprint.NumCitiesRemaining) + " turns to heal");
            // reset search variables for the optimized search
            mPruneSolutions = optimize;
            mOpenLeafs.Clear();
            mBestSolution = null;
            mBestPartialSolution = null;
            
            mOpenLeafs.AddLast(initialState);
            // search for parallel attack strategy solutions
            while (mOpenLeafs.Count > 0)
            {
                SearchState pop = mOpenLeafs.Last.Value;
                mOpenLeafs.RemoveLast();
                Search(pop, false);
            }
            

            // Record optimized Solution
            InvasionSolution parallelSolution = null;
            if (mBestSolution == null)
            {
                parallelSolution = ToSolution(mBestPartialSolution, mInitialArmy, mInitialNation);
            }
            else
            {
                parallelSolution = mBestSolution;
            }

            int bestOptimizedDepth = parallelSolution != null ? mBestSolutionDepth : -1;
            return new SearchResults(mNumLeafsCreated, mNumSolutionsFound, 
                 linearSolution, parallelSolution);
        }

        /* 
         * Driver for search logic.
         * i) Check if the chosen state is solved, and if so update solution records accordiningly 
         * ii) If not solved, check for preliminary qualities that would disqualify this sub problem from being searched
         * iii) If not disqualified, run a recursive division on the invading army into all possible invasion groupings
         *      Each combination found will be added as a new leaf on the tree
         */
        public void Search(SearchState chosenState, bool keepArmyTogether)
        {
            if (chosenState.IsSolved)
            {
                mNumLeafsCreated++;

                #region SolutionFound
                // Check if the solution is relevant depending on which optimizations are being used and solution quality
                bool updateSolution = false;
                bool recordSolution = false;

                int numStepsInInvasion = chosenState.Depth;

                if (mBestSolution == null) // havent found a solution yet
                {
                    recordSolution = updateSolution = true;
                    if (mIsOptimized)
                    {
                        // only accept solutions that occur in as many turns as the new solution
                        mMaxDepthOfTree = numStepsInInvasion;
                    }
                }
                else if (numStepsInInvasion < mBestSolutionDepth ) // found a quicker invasion strategy
                {
                    recordSolution = updateSolution = true;
                    mBestSolution = null;
                    // reset solution tracking
                    if (mIsOptimized)
                    {
                        mNumSolutionsFound = 0;
                        mMaxDepthOfTree = numStepsInInvasion;
                    }
                }
                else if (mBestSolutionDepth == numStepsInInvasion) // occurs in the same number of turns as previous solutions
                {
                    recordSolution = true;
                    if (chosenState.Prob.InvadingArmy.CalculateArmyValue() > mBestSolutionArmyValue)
                    {
                        updateSolution = true;
                    }
                }
                else // worse quality of solution
                {
                    recordSolution = !mIsOptimized;
                }
                
                if (recordSolution)
                {
                    mNumSolutionsFound++;
                }

                // Create and store new solution 
                if (updateSolution)
                {
                    mBestSolution = ToSolution(chosenState, mInitialArmy, mInitialNation);
                    mBestSolutionDepth = numStepsInInvasion;
                    mBestSolutionArmyValue = chosenState.Prob.InvadingArmy.CalculateArmyValue();
                }
                #endregion
            }
            else // sub problem is not solved. Check if sub problem can be divided
            {
                if (mBestSolution == null) // record partial solutions until an actual solution is found
                {
                    if (mBestPartialSolution == null)
                    {
                        mBestPartialSolution = chosenState;
                        mBestPartialSolutionValue = chosenState.Prob.ProblemValue;
                    }
                    else
                    {
                        // only update partial solution if it was able to take more cities than the previous, regardless of better army value
                        if (chosenState.Prob.NationBlueprint.NumCitiesRemaining < mBestPartialSolution.Prob.NationBlueprint.NumCitiesRemaining)
                        {
                            float probValue = chosenState.Prob.ProblemValue;
                            if (probValue < mBestPartialSolutionValue)
                            {
                                mBestPartialSolution = chosenState;
                                mBestPartialSolutionValue = probValue;
                            }
                        }
                    }
                }

                // check for disqualifying features of invasion state
                bool validState = true;
                if (chosenState.ParentProblem != null)
                {
                    if (chosenState.ParentProblem.StateValue == chosenState.StateValue) // army did not heal nor was a city taken
                    {
                        // do nothing, this state didn't progress the invasion
                        validState = false;
                    }
                }
                // took more turns to complete than our max turn limit
                if (mMaxDepthOfTree != -1 && mIsOptimized && chosenState.Depth >= mMaxDepthOfTree)
                {
                    validState = false;
                }

                // valid subproblem. Add all new leafs possible from this state
                if (validState)
                {
                    List<SearchState> subProblems = Fdiv(chosenState, keepArmyTogether);
                    mNumLeafsCreated += subProblems.Count;
                    foreach (SearchState subProblem in subProblems)
                    {
                        mOpenLeafs.AddLast(subProblem);
                    }
                }
                else
                {
                    mNumLeafsCreated++;
                }
            }
        }

        /*
         * Driver for recursive division of invading army into all possible sub army invasions
         * Does future checks to see if there will ever be a valid solution coming from this leaf.
         *      example: Checks if the army will ever be able to capture the strongest city
         */
        public List<SearchState> Fdiv(SearchState state, bool keepArmyTogether)
        {
            //Debug.Assert(!state.IsSolved, "Attempting to apply Div on a solved problem");

            // Get all attackable forts
            List<Fortification> fortifications = new List<Fortification>(state.Prob.NationBlueprint.GetBorderCities());
            ArmyBlueprint invadingArmy = state.Prob.InvadingArmy;
            fortifications.Add(new Fortification()); // add a place holder fort for units to be held in reserve

            List<SearchState> problemDivisions = new List<SearchState>();

            // TODO: pick most difficult city to capture. Temp patch just check the first
            Fortification hardestCity = fortifications[0];
            // Since damage doesn't increase over time, if the army can't take the city now it will never be able to
            if (!mIsOptimized && invadingArmy.Damage < hardestCity.Defense)
            {
                return problemDivisions;
            }
            // If the army is strong enough to take the city but doesn't have enough health, 
            // and it either can't heal, or it's max health even after healing wont be enough to take the city,
            // abandon this search
            else if (!mIsOptimized && invadingArmy.Health <= hardestCity.Offense
                && !(invadingArmy.CanHeal && invadingArmy.MaxHealth > hardestCity.Offense))
            {
                return problemDivisions;
            }

            // come up with all possible transitions through recursing through all army combinations
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

        /*
         * Recurses through all possible army combinations into sub armies to attack each border city provided.
         * Checks if the sub army will be able to capture the assigned fort, and if not discards the combination
         * Sub Army combinations are cached to prevent situations such as 3 soldiers with the same health being split into armies of size 1 and being treated as distinct
         * Caching system uses a similarity value defined in Common Constants. 
         * TODO: the similarity factor is too simplistic and should be scaled based on percent health, and how close to death the unit is, rather than a fixed value
         */
        private static void RecurseFdiv(ArmyBlueprint armyToSubdivide, int currentGroup, List<Fortification> toCapture, InvasionWave currentWave, List<InvasionWave> possibleInvasionWaves, bool keepArmyTogether)
        {
            if (currentGroup == toCapture.Count - 1) // last fort to capture
            {
                if (!armyToSubdivide.IsEmpty) // units left over to subdivide
                {
                    // put all units in last invasion group
                    // ASSUMPTION: Last fort is always a place holder fort
                    currentWave.Wave.Add(AssaultTemplate.GetAssualtTemplate(toCapture[currentGroup], armyToSubdivide));
                }
                // add invasion wave to possible solutions
                possibleInvasionWaves.Add(currentWave);
            }
            else if (armyToSubdivide.IsEmpty) // No units left to allocate to the remaining sub armies
            {
                // add invasion wave up to this point to return list
                possibleInvasionWaves.Add(currentWave);
            }
            else
            {
                int armySize = armyToSubdivide.Size; // num remaining units to subdivide
                long numDifferentCombinations = (long)Mathf.Pow(2, armySize);
                IteratorCache invasionCache = new IteratorCache();

                // iterate through all possible bitwise combinations of the subarmy
                // Combinations begin with all units being added as it orrients the leaf list to have a higher chance of 
                // a successful solution and arriving at optimization values quicker.
                // Doing in reverse order (ie combinationNumber = 0, combinationNumber < NumDifferentCombinations) will
                // bias all units to be in reserve and take longer to arrive at a solution
                for (long combinationNumber = numDifferentCombinations - 1; combinationNumber >= 0; combinationNumber--)
                {
                    ArmyBlueprint subArmy = new ArmyBlueprint();
                    ArmyBlueprint remainingArmy = new ArmyBlueprint();

                    #region Create SubArmy from combination number
                    BitArray combinationArray = new BitArray(System.BitConverter.GetBytes(combinationNumber));
                    for (int j = 0; j < armySize; ++j)
                    {
                        if (j < armyToSubdivide.Soldiers.Count) // bit represents a soldier
                        {
                            if (combinationArray[j]) // put soldier in sub army
                            {
                                subArmy.Soldiers.Add(armyToSubdivide.Soldiers[j]);
                            }
                            else // keep soldier in reserve
                            {
                                remainingArmy.Soldiers.Add(armyToSubdivide.Soldiers[j]);
                            }
                        }
                        else if (j < armyToSubdivide.Soldiers.Count + armyToSubdivide.Healers.Count) // bit is a healer
                        {
                            if (combinationArray[j]) // put healer in sub army
                            {
                                subArmy.Healers.Add(armyToSubdivide.Healers[j - armyToSubdivide.Soldiers.Count]);
                            }
                            else // keep healer in reserve
                            {
                                remainingArmy.Healers.Add(armyToSubdivide.Healers[j - armyToSubdivide.Soldiers.Count]);
                            }
                        }
                        else // bit represents an archer
                        {
                            if (combinationArray[j]) // put archer in sub army
                            {
                                subArmy.Archers.Add(armyToSubdivide.Archers[j - armyToSubdivide.Soldiers.Count - armyToSubdivide.Healers.Count]);
                            }
                            else // keep archer in reserve
                            {
                                remainingArmy.Archers.Add(armyToSubdivide.Archers[j - armyToSubdivide.Soldiers.Count - armyToSubdivide.Healers.Count]);
                            }
                        }
                    }
                    #endregion

                    // Only consider iteration if it can capture the city it was tasked to take
                    if (combinationNumber == 0) // Empty sub army. 
                    {
                        InvasionWave recursiveTransitionCopy = new InvasionWave(currentWave.Wave);
                        RecurseFdiv(remainingArmy, currentGroup + 1, toCapture, recursiveTransitionCopy, possibleInvasionWaves, keepArmyTogether);
                    }
                    else if (!mIsOptimized || !invasionCache.IsCached(subArmy))
                    {
                        // check is assualt will be successful and record values in assault template
                        AssaultTemplate assaultTemplate = AssaultTemplate.GetAssualtTemplate(toCapture[currentGroup], subArmy);
                        if (assaultTemplate != null /* is valid assault */)
                        {
                            invasionCache.Cache(subArmy);
                            InvasionWave recursiveTransitionCopy = new InvasionWave(currentWave.Wave);
                            recursiveTransitionCopy.Wave.Add(assaultTemplate);
                            // recurse with remaining units not included in this sub army
                            RecurseFdiv(remainingArmy, currentGroup + 1, toCapture, recursiveTransitionCopy, possibleInvasionWaves, keepArmyTogether);
                        }
                        // else discard combination
                    }
                    
                    // Used for linear invasion strategies to prevent unneccessary combination checks. 
                    // jump to putting the army in reserve if it wasn't assigned to a fort
                    if (keepArmyTogether && combinationNumber == numDifferentCombinations - 1) // first sub army created
                    {
                        combinationNumber = 1; // will become 0 next loop iteration and second army option will be empty
                    }
                }
            }
        }

        /*
         * Perform all calculations that occur during a sub armies invasion of a city.
         * Assault template records some calculated values used in the FDiv process to prevent redundant calculations
         * Any sub army in the invasion is assumed to be strong enough to capture the corresponding fort
         * Once the fort is captured, the army will have it's health reduced, and if any healers are left alive, will heal
         * Once each army has performed attack/healing, they will be merged into a new army. 
         * Remaining forts are merged into a new nation
         * New army/nation objects are created to prevent deep/shallow copy issues
         */
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
                // was there an unneccessary step in keeping units in reserve. Was not able to heal and could have contributed to the taking of a fort
                if (assaultTemplate.ToAssault.IsPlaceHolderFort && assaultTemplate.Attackers.CalculateArmyValue() == afterAssault.CalculateArmyValue())
                {
                    // assert false;
                    return null;
                }
                if (!assaultTemplate.ToAssault.IsPlaceHolderFort) // fort used to simulate units held in reserve
                {
                    captured.Add(assaultTemplate.ToAssault);
                }
            }
          
            return new SearchState(ArmyBlueprint.MergeSubArmies(armiesAfterAttack), new NationBlueprint(initialState.Prob.NationBlueprint, captured), initialState.Depth + 1, initialState, transition);
        }

        // Helper function to convert the invasion stack into a solution
        public static InvasionSolution ToSolution(SearchState state, ArmyBlueprint initialArmy, NationBlueprint initialNation)
        {
            SearchState currentState = state;
            List<InvasionWave> invasionOrder = new List<InvasionWave>();
            while (currentState.TransitionFromParent != null)
            {
                invasionOrder.Add(currentState.TransitionFromParent);
                currentState = currentState.ParentProblem;
            }
            invasionOrder.Reverse();
            return new InvasionSolution(initialArmy, initialNation,
                state.Prob.InvadingArmy, state.Prob.NationBlueprint,
                invasionOrder, state.IsSolved);
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
                    {
                        return true;
                    }
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





