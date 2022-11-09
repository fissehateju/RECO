using RR.Computations;
using RR.Dataplane;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RR.Models.Mobility
{
    /* -*- Mode:C++; c-file-style:"gnu"; indent-tabs-mode:nil; -*- */
    /*
     * Copyright (c) 2007 INRIA
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
     * Author: Mathieu Lacage <mathieu.lacage@sophia.inria.fr>
     *       : Ammar Hawbani <anmande@ustc.edu.cn> c# code
     */

    /// <summary>
    /// https://www.nsnam.org/doxygen/position-allocator_8cc_source.html
    /// </summary>
    public class NS3PositionAllocators
    {
        /// <summary>
        /// RandomDiscPositionAllocator
        /// </summary>
        public static class RandomDiscPositionAllocator
        {
            /// <summary>
            /// A random variable which represents the angle (gradients) of a position in a random disc.
            /// ns3::UniformRandomVariable[Min=0.0|Max=6.2830]"
            /// </summary>
            private static double Theta
            {
                get
                {
                    double theta = RandomvariableStream.UniformRandomVariable.GetDoubleValue(0.0, 6.2830);
                    //double theta = RandomvariableStream.UniformRandomVariable.GetDoubleValue(0, 1.5701);
                    return theta;
                }
            }

            /// <summary>
            /// A random variable which represents the radius of a position in a random disc.
            /// ns3::UniformRandomVariable[Min=0.0|Max=200.0]
            /// how long it required to change the direction. smaller values means the sinks change their direction frequently.
            /// </summary>
            private static double Rho
            {
                get
                {
                    double MidRange = (PublicParamerters.CommunicationRangeRadius) * RandomvariableStream.UniformRandomVariable.GetDoubleValue(0, 1);
                   // Console.WriteLine("Range:" + MidRange);
                    double rho = RandomvariableStream.UniformRandomVariable.GetDoubleValue(0, MidRange);
                   // Console.WriteLine("rho:" + rho);
                    return rho;
                }
            }


            /// <summary>
            /// get the next position 
            /// </summary>
            /// <param name="current"></param>
            /// <returns></returns>
            public static Point GetNext(Point current)
            {
                double theta = Theta; // direction
                double rho = Rho; // steps
                //int setx, sety;
                //if (Math.Cos(theta) > 0) setx = 1; else if (Math.Cos(theta) == 0) setx = 0;  else setx = -1;
                //if (Math.Sin(theta) > 0) sety = 1; else if (Math.Sin(theta) == 0) sety = 0; else sety = -1;
                double x = current.X + Math.Cos(theta) * rho;
                double y = current.Y + Math.Sin(theta) * rho;
                Point nextPosition = new Point(x, y);
                return nextPosition;
            }

        }
    }
}
