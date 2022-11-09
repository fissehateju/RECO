using RR.Computations;
using RR.Dataplane;
using RR.Intilization;
using RR.Comuting.Routing;
using RR.Models.Mobility;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using RR.Dataplane.NOS;

namespace RR.Models.Charging
{
    internal class RechargingModel
    {
        public DispatcherTimer timer_Recharging = new DispatcherTimer(); // Making this static will restric the values inside the fuction called by Tick
        private Packet tobeCharged;
        public Sink charger;
        public RechargingModel(Sink charg, Packet pack)
        {
            charger = charg;
            tobeCharged = pack;
        }

        public void startRecharing()
        {
            double senConsummedE = PublicParamerters.BatteryIntialEnergy - tobeCharged.Source.ResidualEnergy;
            tobeCharged.Rechargingtime = 1 + (int)Math.Ceiling(senConsummedE / PublicParamerters.chargingRate);
            tobeCharged.ChargeTimecount = 0;
            PublicParamerters.TotalTransferredEnergy += senConsummedE;

            timer_Recharging.Interval = TimeSpan.FromSeconds(1);
            timer_Recharging.Start();
            timer_Recharging.Tick += Recharging;
        }
        private void Recharging(Object sender, EventArgs e)
        {

            tobeCharged.ChargeTimecount++;           

            if (tobeCharged.ChargeTimecount == tobeCharged.Rechargingtime)
            {
                tobeCharged.Source.ResidualEnergy = PublicParamerters.BatteryIntialEnergy;
                PublicParamerters.TotalNumChargedSensors += 1;

                timer_Recharging.Stop();
                
                tobeCharged.Source.rechargeReqCount = 0;
                PublicParamerters.requestedList.Remove(tobeCharged.Source);

                if (!tobeCharged.Source.ChargingRequestInitiate.Equals(PublicParamerters.defualtDateValue))  // != "1/1/0001 12:00:00 AM"
                {
                    var delay = DateTime.Now - tobeCharged.Source.ChargingRequestInitiate;
                    PublicParamerters.ChargingDelayInSecond += delay.TotalSeconds;
                    tobeCharged.Source.ChargingRequestInitiate = PublicParamerters.defualtDateValue;
                }

                DateTime currentdate = DateTime.Now;
                int s = currentdate.Second;
                int m = currentdate.Minute;
                int h = currentdate.Hour;
                int d = currentdate.Day;
                TimeSpan currentTime = new TimeSpan(h, m, s);
                currentTime = currentdate.Subtract(currentdate.Date); // is also possible

                if(charger.agentsRow != null && charger.agentsRow.agentAtTimeT.Keys.Contains(tobeCharged.Source))
                {
                    if (TimeSpan.Compare(currentTime, charger.agentsRow.agentAtTimeT[tobeCharged.Source]) == -1)
                    {
                        tobeCharged.ChargeTimecount = 0;
                        timer_Recharging.Start();
                    }
                    else
                    {
                        charger.SetMobility();
                    }
                }
                else
                {
                    charger.SetMobility();
                }


            }
            else
            {
                //System.Console.WriteLine("Still recharging sensor {0}", tobeCharged.Source.ID);
                if (tobeCharged.ChargeTimecount > tobeCharged.Rechargingtime)
                {
                        tobeCharged.ChargeTimecount = tobeCharged.Rechargingtime;
                        timer_Recharging.Stop();                    
                }
            }
        }       

    }
}
