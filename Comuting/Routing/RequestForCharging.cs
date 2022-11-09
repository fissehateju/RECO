using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.computing;
using RR.Properties;
using RR.RingRouting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RR.Comuting.Routing
{
    public class RequestForCharging
    {
        private LoopMechanizimAvoidance loopMechan = new LoopMechanizimAvoidance();
        private NetworkOverheadCounter counter;
        private string RequestType { get; set; }
        public BaseStation BStation { get; set; }
        public Sink nearCharger;
        public RequestForCharging(Sensor sourceNode, string requestType)
        {
            //counter = new NetworkOverheadCounter();

            BStation = PublicParamerters.BS;
            RequestType = requestType;

            Packet Creq = GeneratePacket(sourceNode);
            sendCHreqPacket(sourceNode, Creq);
            
        }

        //public RequestForCharging()
        //{
        //    counter = new NetworkOverheadCounter();
        //    BStation = PublicParamerters.BS;
        //}

        private void sendCHreqPacket(Sensor sender, Packet pack)
        {
            ReceiveCHreqPacket(sender, pack); // assuming the request received bay the a node close to the baseStation
        }
        private void ReceiveCHreqPacket(Sensor source, Packet pack)
        {
            BStation.SaveToQueue(pack);

            //double minDs = double.MaxValue;
            //for (int i = 0; i < PublicParamerters.MainWindow.mySinks.Count; i++)
            //{
            //    double Ds = Operations.DistanceBetweenTwoPoints(source.CenterLocation, PublicParamerters.MainWindow.mySinks[i].CenterLocation);
            //    if ( Ds < minDs)
            //    {
            //        minDs = Ds;
            //        nearCharger = PublicParamerters.MainWindow.mySinks[i];
            //    }
            //}
            //Point p = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);
            //double DsToBS = Operations.DistanceBetweenTwoPoints(source.CenterLocation, p);
            //if(!nearCharger.isFree && minDs < PublicParamerters.CommunicationRangeRadius) // if charger is out for work
            //{
            //    nearCharger.SaveToQueue(pack);
            //}
            //else
            //{
            //    BStation.SaveToQueue(pack);
            //}

        }

        public Packet GeneratePacket(Sensor sender)
        {
            //This is for the charging request
            PublicParamerters.NumberofGeneratedChargeReqPackets += 1;
            Packet pck = new Packet();
            pck.Source = sender;
            pck.Path = "" + sender.ID;
            pck.Destination = null;
            pck.DestinationPoint = BStation.Position;
            pck.PacketType = PacketType.ChargingRequest;
            pck.PID = PublicParamerters.NumberofGeneratedChargeReqPackets;
            pck.remainingEnergy_Joule = sender.ResidualEnergy;
            pck.dataTransmiting_Rate = sender.transmittingRate;

            if(RequestType == "emergency")
            {
                pck.isEmergencyAlert = true;
            } 
            else
            {
                pck.isEmergencyAlert = false;
            }

            //pck.TimeToLive = Convert.ToInt16((Operations.DistanceBetweenTwoPoints(sender.CenterLocation, pck.DestinationPoint) / (PublicParamerters.CommunicationRangeRadius / 3)));
            //pck.TimeToLive += PublicParamerters.HopsErrorRange;
            //counter.DisplayRefreshAtGenertingPacket(sender, PacketType.ChargingRequest);
            return pck;

        }
    }
}
