using Common.Enums;
using Common.Types;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmyPositionControl : MonoBehaviour
{
    public InvaderController InvaderPrefab;
    public Vector2 BoundingArea;
   
    private Dictionary<InvaderType, List<InvaderController>> mInvaderControllers;
    private Vector3 mInvaderControllerDimensions;

    private void Awake()
    {

    }

    public void ResetArmy()
    {
        if (mInvaderControllers != null)
        {
            foreach (List<InvaderController> controllers in mInvaderControllers.Values)
            {
                foreach (InvaderController controller in controllers)
                {
                    Destroy(controller.gameObject);
                }
            }
            mInvaderControllers.Clear();
        }
       
    }

    // Should only be called once when army is first being created
    public void Initialize(ArmyBlueprint army)
    {
        Initialize();
        foreach (ushort archer in army.Archers)
        {
            InvaderController controller = Instantiate(InvaderPrefab, transform.parent);
            controller.Initialize(archer, InvaderType.Archer);
            mInvaderControllers[InvaderType.Archer].Add(controller);
        }
        foreach (ushort healer in army.Healers)
        {
            InvaderController controller = Instantiate(InvaderPrefab, transform.parent);
            controller.Initialize(healer, InvaderType.Healer);
            mInvaderControllers[InvaderType.Healer].Add(controller);
        }
        foreach (ushort soldier in army.Soldiers)
        {
            InvaderController controller = Instantiate(InvaderPrefab, transform.parent);
            controller.Initialize(soldier, InvaderType.Soldier);
            mInvaderControllers[InvaderType.Soldier].Add(controller);
        }
        LayoutArmy(); // create at position in army
    }

    public void Initialize(List<ArmyPositionControl> subArmies, float overSeconds)
    {
        Debug.Assert(overSeconds > 0);
        Dictionary<InvaderType, List<InvaderController>> newArmy = new Dictionary<InvaderType, List<InvaderController>>();
        newArmy.Add(InvaderType.Soldier, new List<InvaderController>());
        newArmy.Add(InvaderType.Archer, new List<InvaderController>());
        newArmy.Add(InvaderType.Healer, new List<InvaderController>());

        foreach (ArmyPositionControl subArmy in subArmies)
        {
            foreach (var invaderGroupPair in subArmy.mInvaderControllers)
            {
                foreach (InvaderController controller in invaderGroupPair.Value)
                {
                    if (controller.CurrentHealth > 0)
                    {
                        newArmy[invaderGroupPair.Key].Add(controller);
                        controller.CanBeUsed = true;
                    }
                    else
                    {
                        controller.OnDeath();
                    }
                }
            }
        }

        newArmy[InvaderType.Archer].Sort((InvaderController lhs, InvaderController rhs)=>
        {
            return lhs.CurrentHealth.CompareTo(rhs.CurrentHealth);
        });
        newArmy[InvaderType.Healer].Sort((InvaderController lhs, InvaderController rhs) =>
        {
            return lhs.CurrentHealth.CompareTo(rhs.CurrentHealth);
        });
        newArmy[InvaderType.Soldier].Sort((InvaderController lhs, InvaderController rhs) =>
        {
            return lhs.CurrentHealth.CompareTo(rhs.CurrentHealth);
        });

        mInvaderControllers = newArmy;
        LayoutArmy(overSeconds); // move each soldier to its position
    }

    public void Initialize(Dictionary<InvaderType, List<InvaderController>> army, float overSeconds)
    {
        Debug.Assert(overSeconds > 0);
        Initialize();
        mInvaderControllers = army;
        foreach (List<InvaderController> controllers in mInvaderControllers.Values)
        {
            foreach (InvaderController controller in controllers)
            {
                controller.CanBeUsed = true;
            }
        }
        LayoutArmy(overSeconds);
    }

    private void Initialize()
    {
        mInvaderControllers = new Dictionary<InvaderType, List<InvaderController>>();
        mInvaderControllers.Add(InvaderType.Soldier, new List<InvaderController>());
        mInvaderControllers.Add(InvaderType.Archer, new List<InvaderController>());
        mInvaderControllers.Add(InvaderType.Healer, new List<InvaderController>());
    }

    private void LayoutArmy(float overSeconds = 0)
    {
        mInvaderControllerDimensions = InvaderPrefab.GetComponent<RectTransform>().rect.size;
        mInvaderControllerDimensions += Vector3.one * 20 * 2f;
        float prefabScale = 1f;
        float unitsPerColumn = 0;
        int numArchers = mInvaderControllers[InvaderType.Archer].Count;
        int numHealers = mInvaderControllers[InvaderType.Healer].Count;
        int numSoldiers = mInvaderControllers[InvaderType.Soldier].Count;
        int errorBreak = 10;
        while (errorBreak-- > 0)
        {
            unitsPerColumn = Mathf.FloorToInt(BoundingArea.y / mInvaderControllerDimensions.y);
            if (unitsPerColumn == 0)
            {
                unitsPerColumn = 1;
                prefabScale = mInvaderControllerDimensions.y / BoundingArea.y;
                mInvaderControllerDimensions *= prefabScale;
            }
            float numColumns = Mathf.Ceil(numArchers / unitsPerColumn);
            numColumns += Mathf.Ceil(numHealers / unitsPerColumn);
            numColumns += Mathf.Ceil(numSoldiers / unitsPerColumn);

            float totalWidthNeeded = numColumns * mInvaderControllerDimensions.x;
            float difference = (BoundingArea.x - totalWidthNeeded) / BoundingArea.x;
            if (difference < 0)
            {
                prefabScale /= 1 + (Mathf.Abs(difference) / 2f);
                mInvaderControllerDimensions /= 1 + (Mathf.Abs(difference) / 2f);
            }
            else
            {
                break;
            }
        }
        // place soldiers
        int count = 0;
        int columnCount = 0;
        InvaderType[] invaderOrderings = { InvaderType.Archer, InvaderType.Healer, InvaderType.Soldier };
        for (int typeIdx = 0; typeIdx < invaderOrderings.Length; typeIdx++ )
        {
            List<InvaderController> controllers = mInvaderControllers[invaderOrderings[typeIdx]];
            foreach (InvaderController controller in controllers)
            {
                if (count == unitsPerColumn)
                {
                    count = 0;
                    columnCount++;
                }
                controller.transform.localScale = Vector3.one * prefabScale;
                Vector3 positionInArmy = new Vector3(columnCount * mInvaderControllerDimensions.x, -count * mInvaderControllerDimensions.y);
                if (overSeconds > 0)
                {
                    controller.MoveToPosition(transform.position + positionInArmy, overSeconds);
                    //controller.transform.localPosition = positionInArmy;
                }
                else // set at position
                {
                    controller.transform.position = transform.position + positionInArmy;
                }

                count++;
            }
            // started a new column but didn't finish it. Add rest to next wave
            if (count > 0)
            {
                count = 0;
                columnCount++;
            }  
        }
    }

    public List<Dictionary<InvaderType, List<InvaderController>>> SplitIntoSubArmies(List<ArmyBlueprint> armyCompositions)
    {
        List<Dictionary<InvaderType, List<InvaderController>>> subArmyControllers = new List<Dictionary<InvaderType, List<InvaderController>>>();
        foreach (ArmyBlueprint subarmy in armyCompositions)
        {
            Dictionary<InvaderType, List<InvaderController>> controllers = new Dictionary<InvaderType, List<InvaderController>>();
            List<InvaderController> invaderControllers = new List<InvaderController>();
            foreach (ushort archer in subarmy.Archers)
            {
                InvaderController match = mInvaderControllers[InvaderType.Archer].Find((x) =>
                { return x.CurrentHealth == archer && x.CanBeUsed; });
                Debug.Assert(match != null);
                match.CanBeUsed = false;
                invaderControllers.Add(match);
            }
            controllers.Add(InvaderType.Archer, invaderControllers);

            invaderControllers = new List<InvaderController>();
            foreach (ushort healer in subarmy.Healers)
            {
                InvaderController match = mInvaderControllers[InvaderType.Healer].Find((x) =>
                { return x.CurrentHealth == healer && x.CanBeUsed; });
                Debug.Assert(match != null);
                match.CanBeUsed = false;
                invaderControllers.Add(match);
            }
            controllers.Add(InvaderType.Healer, invaderControllers);

            invaderControllers = new List<InvaderController>();
            foreach (ushort soldier in subarmy.Soldiers)
            {
                InvaderController match = mInvaderControllers[InvaderType.Soldier].Find((x) =>
                { return x.CurrentHealth == soldier && x.CanBeUsed; });
                Debug.Assert(match != null);
                match.CanBeUsed = false;
                invaderControllers.Add(match);
            }
            controllers.Add(InvaderType.Soldier, invaderControllers);
            subArmyControllers.Add(controllers);
        }
        // assert
        foreach (List<InvaderController> controllers in mInvaderControllers.Values)
        {
            foreach (InvaderController controller in controllers)
            {
                Debug.Assert(!controller.CanBeUsed);
            }
        }
        return subArmyControllers;
    }

    public void UpdateHealth(ArmyBlueprint afterAssault, float overSeconds)
    {
        for (int i = 0; i < afterAssault.Archers.Count; ++i)
        {
            InvaderController controller = mInvaderControllers[InvaderType.Archer][i];
            controller.UpdateHealth(afterAssault.Archers[i], overSeconds);
        }

        for (int i = 0; i < afterAssault.Healers.Count; ++i)
        {
            mInvaderControllers[InvaderType.Healer][i].UpdateHealth(afterAssault.Healers[i], overSeconds);
        }

        for (int i = 0; i < afterAssault.Soldiers.Count; ++i)
        {
            mInvaderControllers[InvaderType.Soldier][i].UpdateHealth(afterAssault.Soldiers[i], overSeconds);
        }
    }

	// Update is called once per frame
	void Update () {
		
	}
}
