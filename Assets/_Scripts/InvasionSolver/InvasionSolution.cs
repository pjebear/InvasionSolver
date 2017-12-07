
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InvasionSolver
{
    using System.IO;
    using System.Xml.Serialization;
    using Common.Types;

    [XmlRoot("InvasionWave")]
    public class InvasionWave
    {
        [XmlArray("Wave"), XmlArrayItem("AssaultTemplate", typeof(AssaultTemplate))]
        public List<AssaultTemplate> Wave;
        public InvasionWave(List<AssaultTemplate> wave)
        {
            Wave = new List<AssaultTemplate>(wave);
        }
        public InvasionWave()
        {
            Wave = new List<AssaultTemplate>();
        }
    }

    [XmlRoot("InvasionSolution")]
    public class InvasionSolution
    {
        public const string FilePath = "InvasionSolution.xml";

        [XmlElement("InitialArmy", typeof(ArmyBlueprint))]
        public ArmyBlueprint InitialArmy { get; private set; }

        [XmlElement("InitialNation", typeof(NationBlueprint))]
        public NationBlueprint InitialNation { get; private set; }

        [XmlElement("FinalArmy", typeof(ArmyBlueprint))]
        public ArmyBlueprint FinalArmy { get; private set; }

        [XmlElement("FinalNation", typeof(NationBlueprint))]
        public NationBlueprint FinalNation { get; private set; }

        [XmlArray("InvasionOrder"), XmlArrayItem("InvasionWave", typeof(InvasionWave))]
        public List<InvasionWave> InvasionOrder;

        [XmlElement("IsCompleteSolution", typeof(bool))]
        public bool IsCompleteSolution { get; private set; }

        public int NumTurnsToSolve { get { return InvasionOrder.Count; } }

        public InvasionSolution()
        {
            InvasionOrder = null;
            InitialArmy = null;
            InitialNation = null;
        }

        public InvasionSolution(ArmyBlueprint initialArmy, NationBlueprint initialNation,
            ArmyBlueprint finalArmy, NationBlueprint finalNation,
            List<InvasionWave> invasionOrder, bool isCompleteSolution)
        {
            InvasionOrder = invasionOrder;
            InitialArmy = initialArmy;
            InitialNation = initialNation;
            FinalArmy = finalArmy;
            FinalNation = finalNation;
            IsCompleteSolution = isCompleteSolution;
        }

        public void Save(string filePath = FilePath)
        {
            var serializer = new XmlSerializer(typeof(InvasionSolution));
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(fileStream, this);
            }
        }

        public static InvasionSolution Load(string filePath = FilePath)
        {
            var serializer = new XmlSerializer(typeof(InvasionSolution));
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                return serializer.Deserialize(fileStream) as InvasionSolution;
            }
        }
    }
}
