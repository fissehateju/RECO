using RR.Comuting.Routing;
using System;
using System.Collections.Generic;
using System.Windows;

namespace RR.Dataplane.NOS
{
    public enum PacketType
    {
        Data,
        RingConstruction,
        ShareSinkPosition,
        ReportSinkPosition,
        ObtainSinkPosition,
        ResponseSinkPosition,
        ChargingRequest,
        HeaderInfo,
        ReportBatteryLevel,
        ReportSojournPoints,
        AssigningAsHeader
    }

    public enum PacketDropedReasons
    {
        NULL,
        TimeToLive,
        WaitingTime,
        Loop,
        RingNodesError,
        FollowUpMechansim,
        InformationError,
        DeadNode,
        RecoveryPeriodExpired,
        RecoveryNoNewAgentFound
    }

    public class Packet: ICloneable, IDisposable
    {
        //: Packet section:
        public long PID { get; set; } // SEQ ID OF PACKET.
        public PacketType PacketType { get; set; }
        public DateTime generateTime { get; set; }
        public bool isDelivered { get; set; }
        public bool isRecovery
        {
            get
            {
                if(SinkIDsNeedsRecovery==null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool isFollowUp
        {
            get; set;
        }
        public Sensor OldAgentFollowup { get; set; }
        public double PacketLength
        {
            get
            {
                switch (PacketType)
                {
                    case PacketType.Data: return 1024;
                    case PacketType.RingConstruction: return 256;
                    case PacketType.ShareSinkPosition: return 128;
                    case PacketType.ObtainSinkPosition: return 128;
                    case PacketType.ReportSinkPosition: return 128;
                    case PacketType.ResponseSinkPosition: return 128;
                    case PacketType.ChargingRequest: return 128;
                    case PacketType.ReportSojournPoints: return 128;
                    default: return 512;
                }
            }
        }

       
        //public int H2S { get { if (PacketType == PacketType.Data) return Source.HopsToSink; else return Destination.HopsToSink; } }
        public int TimeToLive { get; set; }
        public int Hops { get; set; }
        public string Path { get; set; }
        //public double RoutingDistance { get; set; }
        public double Delay { get; set; }
        public double UsedEnergy_Joule { get; set; }
        public int WaitingTimes { get; set; }
        public int Recharg_WaitingTimes { get; set; }
        public Point DestinationPoint;

        // recharging scheduling param
        public double remainingEnergy_Joule { get; set; }
        public int ChargeTimecount { get; set; }
        public int Rechargingtime { get; set; }
        public double remainingEnergyPercentage
        {
            get { return (remainingEnergy_Joule / PublicParamerters.BatteryIntialEnergy) * 100; }
        }
        public double dataTransmiting_Rate { get; set; }
        public bool isEmergencyAlert { get; set; }
        public AgentsRow agentsRowT { get; set; }

        public List<int> SinkIDsNeedsRecovery = null;// if the agent node did not find its sink. then recovery is rquired for these sinks. this should be set during obtiansinkposition.
        public Sensor Source { get; set; }
        public Sensor Destination { get; set; }
        public List<Sensor> Destinations { get; set; }
        public Sink mySink { get; set; }
        public AgentsRow ReportSinkPosition { get; set; } // ReportSinkPosition
        public PacketDropedReasons PacketDropedReasons { get; set; } 
        public AgentsRow SinksAgentsList { get; set; } // the sinks that should be resonsed to the source that requested
        public PacketDirection PacketDirection { get; set; }
        public Point ClosestPointOnTheDiagonal { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public int ReTransmissionTry { get; set; } // in one hope. this should be =0 when the packet is recived.
    }
}
