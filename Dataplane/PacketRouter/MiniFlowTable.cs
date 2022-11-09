using RR.Dataplane.NOS;

namespace RR.Dataplane.PacketRouter
{
    public enum FlowAction { Forward, Drop }

    /*
    public class MiniFlowTableEntry
    {
        public int ID { get { return NeighborEntry.Sensor.ID; } }
        public double UpLinkPriority { get; set; }
        public FlowAction UpLinkAction { get; set; }
        public double UpLinkStatistics { get; set; }  

        public double DownLinkPriority { get; set; }
        public FlowAction DownLinkAction { get; set; }
        public double DownLinkStatistics { get; set; }

        public SensorState SensorState { get { return NeighborEntry.Sensor.CurrentSensorState; } }
        public double Statistics { get { return UpLinkStatistics + DownLinkStatistics; } }
        public  NeighborEntry NeighborEntry { get; set; } 

    }*/
}
