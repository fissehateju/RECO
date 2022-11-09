using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Intilization;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RR.NetAnimator
{
   public class MyNetAnimator
    {
        private Sensor sender;
        public MyNetAnimator(Sensor _sender)
        {
            sender = _sender;
        }

        /// <summary>
        /// show or hide the arrow in seperated thread.
        /// </summary>
        /// <param name="id"></param>
        private void ShowOrHideArrow(int id)
        {
            Thread thread = new Thread(() =>
            {
                lock (sender.MyArrows)
                {
                    Arrow ar = GetArrow(id);
                    if (ar != null)
                    {
                        lock (ar)
                        {
                            if (ar.Visibility == Visibility.Visible)
                            {
                                Action action = () => ar.Visibility = Visibility.Hidden;
                                sender.Dispatcher.Invoke(action);
                            }
                            else
                            {
                                Action action = () => ar.Visibility = Visibility.Visible;
                                sender.Dispatcher.Invoke(action);
                            }
                        }
                    }
                }
            }
            );
            thread.Start();
        }


        // get arrow by ID.
        private Arrow GetArrow(int EndPointID)
        {
            foreach (Arrow arr in sender.MyArrows) { if (arr.To.ID == EndPointID) return arr; }
            return null;
        }

        public void StartAnimate(int reciverID, PacketType packetType )
        {
            Thread thread = new Thread(() =>
            {
                lock (sender.MyArrows)
                {
                    Arrow ar = GetArrow(reciverID);
                   
                    if (ar != null)
                    {
                        lock (ar)
                        {
                            if (ar.Visibility == Visibility.Hidden)
                            {
                                if (Settings.Default.ShowAnimation)
                                {
                                    Action actionx = () => ar.BeginAnimation(packetType);
                                    sender.Dispatcher.Invoke(actionx);
                                    Action action1 = () => ar.Visibility = Visibility.Visible;
                                    sender.Dispatcher.Invoke(action1);
                                }
                                else
                                {
                                    Action action1 = () => ar.Visibility = Visibility.Visible;
                                    sender.Dispatcher.Invoke(action1);
                                    sender.Dispatcher.Invoke(() => ar.Stroke = new SolidColorBrush(Colors.Black));
                                    sender.Dispatcher.Invoke(() => ar.StrokeThickness = 1);
                                    sender.Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    sender.Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                            }
                            else
                            {
                                if (Settings.Default.ShowAnimation)
                                {
                                    Action actionx = () => ar.BeginAnimation(packetType);
                                    sender.Dispatcher.Invoke(actionx);
                                    sender.Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    sender.Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                                else
                                {
                                    sender.Dispatcher.Invoke(() => ar.Stroke = new SolidColorBrush(Colors.Black));
                                    sender.Dispatcher.Invoke(() => ar.StrokeThickness = 1);
                                    sender.Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    sender.Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                            }
                        }
                    }
                }
            }
           );
            thread.Start();
            thread.Priority = ThreadPriority.Highest;
        }
    }
}
