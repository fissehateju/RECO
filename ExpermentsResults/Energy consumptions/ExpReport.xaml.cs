using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Properties;
using RR.ui;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace RR.ExpermentsResults.Energy_consumptions
{
    class ResultsObject
    {
        public double AverageEnergyConsumption { get; set; }
        public double AverageHops { get; set; }
        public double AverageWaitingTime { get; set; }
        public double AverageRedundantTransmissions { get; set; }
        public double AverageRoutingDistance { get; set; }
        public double AverageTransmissionDistance { get; set; }
    }

    public class ValParPair
    {
        public string Par { get; set; }
        public string Val { get; set; }
    }

    /// <summary>
    /// Interaction logic for ExpReport.xaml
    /// </summary>
    public partial class ExpReport : Window
    {

        public ExpReport(MainWindow _mianWind)
        {
            InitializeComponent();

            List<ValParPair> List = new List<ValParPair>();

            List.Add(new ValParPair() { Par = "Network", Val = Settings.Default.NetworkName });
            List.Add(new ValParPair() { Par = "Number of Nodes", Val = PublicParamerters.NumberofNodes.ToString() });
            List.Add(new ValParPair() { Par = "Network SideLength m^2", Val = PublicParamerters.NetworkSquareSideLength.ToString() });
            List.Add(new ValParPair() { Par = "# Sinks", Val = PublicParamerters.SinkCount.ToString() });
            List.Add(new ValParPair() { Par = "Communication Range Radius", Val = PublicParamerters.CommunicationRangeRadius.ToString() + " m" });

            // Time settings:
            List.Add(new ValParPair() { Par = "Simulation Time", Val = Settings.Default.StopSimlationWhen.ToString() + " s" });
            List.Add(new ValParPair() { Par = "Start up time", Val = PublicParamerters.MacStartUp.ToString() + " s" });
            List.Add(new ValParPair() { Par = "Active Time", Val = PublicParamerters.Periods.ActivePeriod.ToString() + " s" });
            List.Add(new ValParPair() { Par = "Sleep Time", Val = PublicParamerters.Periods.SleepPeriod.ToString() + " s" });
            List.Add(new ValParPair() { Par = "Queue Time", Val = PublicParamerters.QueueTime.Seconds.ToString() });
            List.Add(new ValParPair() { Par = "Lifetime (s)", Val = PublicParamerters.SimulationTime.ToString() });


            //Energy:
            List.Add(new ValParPair() { Par = "Initial Energy (J)", Val = PublicParamerters.BatteryIntialEnergy.ToString() });
            List.Add(new ValParPair() { Par = "Total Energy Consumption (J)", Val = PublicParamerters.TotalEnergyConsumptionJoule.ToString() });
            List.Add(new ValParPair() { Par = "Total Energy Consumption for Data Packets (J)", Val = PublicParamerters.EnergyConsumedForDataPackets.ToString() });
            List.Add(new ValParPair() { Par = "Total Energy Consumption for Control Packets (J)", Val = PublicParamerters.EnergyComsumedForControlPackets.ToString() });
            List.Add(new ValParPair() { Par = "Total Wasted Energy  (J)", Val = PublicParamerters.TotalWastedEnergyJoule.ToString() });


            // Hops:
            List.Add(new ValParPair() { Par = "Average Hops/path", Val = PublicParamerters.AverageHops.ToString() });

            // Distances:
            List.Add(new ValParPair() { Par = "Average Routing Distance (m)/path", Val = PublicParamerters.AverageRoutingDistance.ToString() });
            List.Add(new ValParPair() { Par = "Average Transmission Distance (m)/hop", Val = PublicParamerters.AverageTransmissionDistance.ToString() });


            // Delay:
            List.Add(new ValParPair() { Par = "Average Delay (s)/path", Val = PublicParamerters.AverageDelay.ToString() });
            List.Add(new ValParPair() { Par = "Average Waiting Time/path", Val = PublicParamerters.AverageWaitingTimes.ToString() });
            List.Add(new ValParPair() { Par = "Average Transmission Delay (s)/path", Val = PublicParamerters.AverageTransmissionDelay.ToString() });
            List.Add(new ValParPair() { Par = "Average Queuing Delay (s)/path", Val = PublicParamerters.AverageQueueDelay.ToString() });

            // Packets:
            List.Add(new ValParPair() { Par = "Packet Rate", Val = Settings.Default.PacketRate.ToString() + " s" });
            List.Add(new ValParPair() { Par = "# Generated Packet", Val = PublicParamerters.NumberofGeneratedPackets.ToString() });
            List.Add(new ValParPair() { Par = "# Deliverd Packet", Val = PublicParamerters.NumberofDeliveredPacket.ToString() });
            List.Add(new ValParPair() { Par = "# Droped Packet", Val = PublicParamerters.NumberofDropedPacket.ToString() });
            List.Add(new ValParPair() { Par = "Success %", Val = PublicParamerters.DeliveredRatio.ToString() });
            List.Add(new ValParPair() { Par = "Droped %", Val = PublicParamerters.DropedRatio.ToString() });
            List.Add(new ValParPair() { Par = "# Data Pcket", Val = PublicParamerters.NumberOfDataPacket.ToString() }); ;
            List.Add(new ValParPair() { Par = "# Obtain Sink Position Packet", Val = PublicParamerters.NumberOfObtainSinkPositionPacket.ToString() });
            List.Add(new ValParPair() { Par = "# Response Sink Position Packet ", Val = PublicParamerters.NumberOfResponseSinkPositionPacket.ToString() });
            List.Add(new ValParPair() { Par = "# Report Sink Position Packet ", Val = PublicParamerters.NumberOfReportSinkPositionPacket.ToString() });
            List.Add(new ValParPair() { Par = "# Diagonal Virtual Line Construction Packet", Val = PublicParamerters.NumberOfSojournPointsPacket.ToString() });
            List.Add(new ValParPair() { Par = "Sinks start at center", Val = Settings.Default.SinksStartAtNetworkCenter.ToString() });
            // others:

            List.Add(new ValParPair() { Par = "Protocol", Val = "RR" });
            dg_data.ItemsSource = List;
        }
    }
}
