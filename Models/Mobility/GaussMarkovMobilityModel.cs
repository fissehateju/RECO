using RR.Computations;
using RR.Dataplane;
using RR.Intilization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RR.Models.Mobility
{

    /* -*- Mode:C++; c-file-style:"gnu"; indent-tabs-mode:nil; -*- */
    /*
     * Copyright (c) 2009 Dan Broyles
     *
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
     * Author: Dan Broyles <dbroyl01@ku.edu>
     *       : Ammar Hawbani <anmande@ustc.edu.cn> c# code.
     */


    /// <summary>
    /// 
    /// </summary>
    class GaussMarkovMobilityModel
    {
        private UserControl ObjectToMove; // the object to be moved
        private DispatcherTimer SelectDistinationLocation = new DispatcherTimer();
        private DispatcherTimer MovmentScheduler = new DispatcherTimer();
        double MaxX, MaxY;
        private bool isBound = false;
        Point velocityVector;
        double m_meanVelocity;
        double m_meanDirection;
        double m_meanPitch;
        double m_Velocity;
        double m_Direction;
        double m_Pitch;

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

        public GaussMarkovMobilityModel(UserControl _sink)
        {
            m_meanVelocity = 0.0;
            m_meanDirection = 0.0;
            m_meanPitch = 0.0;
            ObjectToMove = _sink;
            isBound = false;
        }
        /// <summary>
        /// MaxX , MaxY are Bounds of the area to cruise.
        /// </summary>
        /// <param name="_sink"></param>
        /// <param name="MaxX"></param>
        /// <param name="MaxY"></param>
        public GaussMarkovMobilityModel(UserControl _sink, double _MaxX, double _MaxY)
        {

            m_meanVelocity = 0.0;
            m_meanDirection = 0.0;
            m_meanPitch = 0.0;
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

        

       

        private void SelectDistinationLocation_Tick(object sender, EventArgs e)
        {
            StartWalk();
        }

        /// <summary>
        /// Change current direction and speed after moving for this time.
        /// m_timeStep
        /// </summary>
        private double TimeStep
        {
            get { return 2; } // was 1
        }

        /// <summary>
        /// A constant representing the tunable parameter in the Gauss-Markov model.
        /// defualt 1.0
        /// m_alpha
        /// </summary>
        private double  Alpha
        {
            get { return 1; }
        }

        /// <summary>
        /// A random variable used to assign the average velocity.
        /// ns3::UniformRandomVariable[Min=0.0|Max=1.0]
        /// m_rndMeanVelocity
        /// </summary>
        private double MeanVelocity
        {
            get
            {
                return RandomvariableStream.UniformRandomVariable.GetDoubleValue(0, 1);
            }
        }

        /// <summary>
        /// A random variable used to assign the average direction.
        /// ns3::UniformRandomVariable[Min=0.0|Max=6.283185307]"
        /// m_rndMeanDirection
        /// </summary>
        private double MeanDirection
        {
            get
            {
                return RandomvariableStream.UniformRandomVariable.GetDoubleValue(0, 6.283185307);
            }
        }

        /// <summary>
        /// A random variable used to assign the average pitch.
        /// ns3::ConstantRandomVariable[Constant=0.0]"
        /// m_rndMeanPitch
        /// </summary>
        private double MeanPitch
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// A gaussian random variable used to calculate the next velocity value.
        /// ns3::NormalRandomVariable[Mean=0.0|Variance=1.0|Bound=10.0]"
        /// Defaults to zero mean, and std dev = 1, and bound to +-10 of the mean
        /// https://www.nsnam.org/doxygen/random-variable-stream_8cc_source.html
        /// m_normalVelocity
        /// </summary>
        private double NormalVelocity
        {
            get
            {
                return RandomvariableStream.NormalRandomVariable.GetValue(0, 1);
            }
        }

        /// <summary>
        /// A gaussian random variable used to calculate the next direction value.
        /// m_normalDirection
        /// ns3::NormalRandomVariable[Mean=0.0|Variance=1.0|Bound=10.0]
        /// m_normalDirection
        /// </summary>
        public double NormalDirection
        {
            get
            {
                return RandomvariableStream.NormalRandomVariable.GetValue(0, 1);
            }
        }

        /// <summary>
        /// A gaussian random variable used to calculate the next pitch value.
        /// m_normalPitch
        /// </summary>
        public double NormalPitch
        {
            get
            {
                return RandomvariableStream.NormalRandomVariable.GetValue(0, 1);
            }
        }

       

        public void StartWalk()
        {
            if (m_meanVelocity == 0.0)
            {
                //Initialize the mean velocity, direction, and pitch variables
                m_meanVelocity = MeanVelocity;
                m_meanDirection = MeanDirection;
                m_meanPitch = MeanPitch;

                double cosD = Math.Cos(m_meanDirection);
                double cosP = Math.Cos(m_meanPitch);
                double sinD = Math.Sin(m_meanDirection);
                double sinP = Math.Sin(m_meanPitch);

                //Initialize the starting velocity, direction, and pitch to be identical to the mean ones
                m_Velocity = m_meanVelocity;
                m_Direction = m_meanDirection;
                m_Pitch = m_meanPitch;
                //Set the velocity vector to give to the constant velocity helper
                velocityVector = new Point(m_Velocity * cosD * cosP, m_Velocity * sinD * cosP);
                SetTravelDelay(TimeSpan.FromSeconds(TimeStep)); // Timer.

            }
            else
            {
                //Get the next values from the gaussian distributions for velocity, direction, and pitch
                double rv = NormalVelocity;
                double rd = NormalDirection;
                double rp = NormalPitch;

                //Calculate the NEW velocity, direction, and pitch values using the Gauss-Markov formula:
                //newVal = alpha*oldVal + (1-alpha)*meanVal + sqrt(1-alpha^2)*rv
                //where rv is a random number from a normal (gaussian) distribution
                double m_alpha = Alpha;
                double one_minus_alpha = 1 - m_alpha;
                double sqrt_alpha = Math.Sqrt(1 - m_alpha * m_alpha);

                m_Velocity = m_alpha * m_Velocity + one_minus_alpha * m_meanVelocity + sqrt_alpha * NormalVelocity;
                m_Direction = m_alpha * m_Direction + one_minus_alpha * m_meanDirection + sqrt_alpha * NormalDirection;
                m_Pitch = m_alpha * m_Pitch + one_minus_alpha * m_meanPitch + sqrt_alpha * NormalPitch;

                //Calculate the linear velocity vector to give to the constant velocity helper

                double cosDir = Math.Cos(m_Direction);
                double cosPit = Math.Cos(m_Pitch);
                double sinDir = Math.Sin(m_Direction);
                double sinPit = Math.Sin(m_Pitch);

                double vx = m_Velocity * cosDir * cosPit;
                double vy = m_Velocity * sinDir * cosPit;

                velocityVector = new Point(vx, vy);

                SetTravelDelay(TimeSpan.FromSeconds(TimeStep)); // Timer.
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

        private void SetTravelDelay(TimeSpan timeSpan)
        {
            SelectDistinationLocation.Interval = TimeSpan.FromSeconds(10);
            MovmentScheduler.Interval = TimeSpan.FromSeconds(0.01);
            MovmentScheduler.Start();
        }

        private void Scheduler_Tick(object sender, EventArgs e)
        {
           ScheduleMobility(TimeSpan.FromSeconds(1)); // 10 steps:
        }

        /// <summary>
        /// this like time steps
        /// </summary>
        /// <param name="delayLeft"></param>
        public void ScheduleMobility(TimeSpan delayLeft)
        {
       

            Point position = ObjectPosition;
            Point speed = velocityVector;
            Point nextPosition = new Point();

            nextPosition.X = position.X + (speed.X * delayLeft.Seconds);
            nextPosition.Y = position.Y + (speed.Y * delayLeft.Seconds);

            if (delayLeft.Seconds < 0.0) delayLeft = TimeSpan.FromSeconds(1);

            // Make sure that the position by the next time step is still within the boundary.
            // If out of bounds, then alter the velocity vector and average direction to keep the position in bounds

            if (IsInside(nextPosition))
            {
               
                ObjectPosition = nextPosition;
                Operations.DrawLine(PublicParamerters.MainWindow.Canvas_SensingFeild, position, nextPosition);
            }
            else
            {

            }



        }





    }
}
