using RR.Computations;
using RR.Dataplane;
using RR.Intilization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static RR.Computations.RandomvariableStream;

namespace RR.Models.Mobility
{
    /* -*- Mode:C++; c-file-style:"gnu"; indent-tabs-mode:nil; -*- */
    /*
     * Copyright (c) 2007 INRIA
     *https://www.nsnam.org/doxygen/random-waypoint-mobility-model_8cc_source.html
     * This program is free software; you can redistribute it and/or modify
     * it under the terms of the GNU General Public License version 2 as
     * published by the Free Software Foundation;
     *
     * This program is distributed in the hope that it will be useful,
     * but WITHOUT ANY WARRANTY; without even the implied warranty of
     * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
     * GNU General Public License for more details.
     *
     * You should have received a copy of the GNU General Public License
     * along with this program; if not, write to the Free Software
     * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
     *
     * Author: Mathieu Lacage <mathieu.lacage@sophia.inria.fr>
     *         Ammar Hawbani  <anmande@ustc.edu.cn>  C# code.
     */
    public class RandomWaypointMobilityModel
    {
        private UserControl ObjectToMove; // the object to be moved
        private DispatcherTimer SelectDistinationLocation = new DispatcherTimer();
        private DispatcherTimer MovmentScheduler = new DispatcherTimer();

        /// <summary>
        /// get the position of the object
        /// </summary>
        public Point ObjectPosition
        {
            get
            {
                double x = ObjectToMove.Margin.Left;
                double y = ObjectToMove.Margin.Top;
                Point p = new Point(x, y);
                return p;
            }
            set
            {
                Point p = value;
                ObjectToMove.Margin = new Thickness(p.X, p.Y, 0, 0);
            }
        }

        Point velocityVector;
        Point m_current;
        Point destination;
        double speed;
        double MaxX, MaxY;
       private bool isBound =false ;


        /// <summary>
        /// set the object to be moved.
        /// no Bounds of the area to cruise.
        /// </summary>
        /// <param name="_sink"></param>
        public RandomWaypointMobilityModel(UserControl _sink)
        {
            ObjectToMove = _sink;
            isBound = false;
            ObjectToMove.Focusable = true;

        }
        /// <summary>
        /// MaxX , MaxY are Bounds of the area to cruise.
        /// </summary>
        /// <param name="_sink"></param>
        /// <param name="MaxX"></param>
        /// <param name="MaxY"></param>
        public RandomWaypointMobilityModel(UserControl _sink, double _MaxX, double _MaxY)
        {
            MaxX = _MaxX;
            MaxY = _MaxY;
            isBound = true;
            ObjectToMove = _sink;
        }

        /// <summary>
        /// start moving
        /// </summary>
        public void StartMove()
        {
            SelectDistinationLocation.Tick += SelectDistinationLocation_Tick;
            SelectDistinationLocation.Start();
            MovmentScheduler.Tick += Scheduler_Tick;
        }

        /// <summary>
        /// stop moving
        /// </summary>
        public void StopMoving()
        {
            SelectDistinationLocation.Stop();
            MovmentScheduler.Stop();
        }

        private void Scheduler_Tick(object sender, EventArgs e)
        {
            ScheduleMobility();
        }

        private void SetTravelDelay(TimeSpan timeSpan)
        {
            SelectDistinationLocation.Interval = timeSpan;
            MovmentScheduler.Start();
        }

        private void SelectDistinationLocation_Tick(object sender, EventArgs e)
        {
            BeginWalk();
        }

        /// <summary>
        /// A random variable used to pick the speed of a random waypoint model.
        /// ns3::UniformRandomVariable[Min=0.3|Max=0.7]"
        /// ns3: m_speed
        /// </summary>
        public double Speed
        {
            get
            {
                double speed = UniformRandomVariable.GetDoubleValue(0.3, 0.7);
                return speed;
            }
        }

        /// <summary>
        /// A random variable used to pick the pause of a random waypoint model
        /// ns3::ConstantRandomVariable[Constant=2.0]"
        /// ns3: m_pause
        /// </summary>
        public double Pause => 2.0;

        /// <summary>
        /// The position model used to pick a destination point.
        /// m_position
        /// </summary>
        public Point PositionAllocator(Point current)
        {
            Point next = NS3PositionAllocators.RandomDiscPositionAllocator.GetNext(current);
            if (!isBound)
            {
                return next;
            }
            else
            {
                // is bounded:
                if (IsInside(next))
                {
                    return next;
                }
                else
                {
                    return current; // stop just stay there
                }
            }
        }

        public bool IsInside(Point next)
        {
            if (next.X >= 0 && next.X <= MaxX)
            {
                if (next.Y >= 0 && next.Y <= MaxY)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// get the 
        /// </summary>
        public void BeginWalk()
        {

            
            m_current = ObjectPosition;
            destination = PositionAllocator(m_current); // 

            speed = 5; //Speed; // random distrubiton
            double dx = destination.X - m_current.X;
            double dy = destination.Y - m_current.Y;
            double k = speed / Math.Sqrt((dx * dx) + (dy * dy));//

            if (!double.IsInfinity(k))
            {

                velocityVector = new Point(k * dx, k * dy); // speed +direction
                TimeSpan travelDelay = TimeSpan.FromSeconds(Operations.DistanceBetweenTwoPoints(m_current, destination) / speed);
                SetTravelDelay(travelDelay); // Timer.
            }


            
        }

        public void ScheduleMobility()
        {
            
            MovmentScheduler.Interval = TimeSpan.FromSeconds(1); //TimeSpan.FromSeconds(speed); it was 1
            m_current = ObjectPosition;
            double x = (m_current.X + velocityVector.X);
            double y = (m_current.Y + velocityVector.Y);

            Point moveto= new Point(x, y);


            ObjectPosition = moveto;
            ObjectToMove.Width += 0.00001; // just to trigger an event. dont remove this line.


            // Operations.DrawLine(PublicParamerters.MainWindow.Canvas_SensingFeild, m_current, moveto);

        }
    }
}
