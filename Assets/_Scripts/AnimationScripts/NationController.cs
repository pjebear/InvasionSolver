using Common.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets._Scripts.AnimationScripts
{
    class NationController : MonoBehaviour
    {
        public FortScript FortPrefab;
        private Vector2 mFortificationDimensions;

        private Dictionary<int, FortScript> mFortLookup;

        private void Awake()
        {
            mFortLookup = new Dictionary<int, FortScript>();
            mFortificationDimensions = FortPrefab.GetComponent<RectTransform>().rect.size + Vector2.one * 40f; // padding
        }


        public void InitializeNation(NationBlueprint nation)
        {
            Vector3 currentScreenPosition = transform.position;
            currentScreenPosition.x -= mFortificationDimensions.x * ((nation.NationSize-1) / 2f);
            transform.position = currentScreenPosition;
            foreach (Fortification fort in nation.Fortifications)
            {
                FortScript script = Instantiate(FortPrefab, transform);
                Vector2 positionInNation = fort.PositionInNation(nation.NationSize);
                script.transform.localPosition = new Vector2(positionInNation.x * mFortificationDimensions.x, positionInNation.y * mFortificationDimensions.y);
                script.Initialize(fort.Defense, fort.FortificationId);
                mFortLookup.Add(fort.FortificationId, script);
            }
        }

        public void DestroyFort(int fortId)
        {
            mFortLookup[fortId].Demolish();
        }

        public void DestroyForts(List<int> fortIds)
        {
            foreach (int fortId in fortIds)
            {
                mFortLookup[fortId].Demolish();
            }
        }
    }
}
