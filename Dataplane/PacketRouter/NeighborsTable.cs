using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.Dataplane.PacketRouter
{
    /*
    /// <summary>
    /// TABLE 2: NEIGHBORING NODES INFORMATION TABLE (NEIGHBORS-TABLE)
    /// </summary>
    public class NeighborEntry
    {
        public int ID { get { return Sensor.ID; } } // id of candidate.
        public double R { get; set; }// 
        public System.Windows.Point CenterLocation { get { return Sensor.CenterLocation; } }
        public Sensor Sensor { get; set; }
    }*/

    public class CoordinationEntry
    {
        public double Priority { get; set; }
        public int SensorID { get { return Sensor.ID; } }
        public Sensor Sensor { get; set; }

    }


}
