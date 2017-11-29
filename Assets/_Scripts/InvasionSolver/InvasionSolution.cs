
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

        [XmlElement("ArmyBlueprint", typeof(ArmyBlueprint))]
        public ArmyBlueprint InitialArmy { get; private set; }

        [XmlElement("NationBlueprint", typeof(NationBlueprint))]
        public NationBlueprint InitialNation { get; private set; }

        [XmlArray("InvasionOrder"), XmlArrayItem("InvasionWave", typeof(InvasionWave))]
        public List<InvasionWave> InvasionOrder;

        public InvasionSolution()
        {
            InvasionOrder = null;
            InitialArmy = null;
            InitialNation = null;
        }

        public InvasionSolution(ArmyBlueprint initialArmy, NationBlueprint initialNation, List<InvasionWave> invasionOrder)
        {
            InvasionOrder = invasionOrder;
            InitialArmy = initialArmy;
            InitialNation = initialNation;
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
