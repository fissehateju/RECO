using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RR.Intilization;
using System.Threading;
using RR.Computations;

namespace RR.Forwarding
{
    /// <summary>
    /// generate anumber between 0- max:
    /// </summary>
    public static class UnformRandomNumberGenerator
    {
        public static double GetUniform(double max) 
        {
            return max * RandomeNumberGenerator.GetUniform();
        }

        public static double GetUniformSleepSec(double max)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            return max * RandomeNumberGenerator.GetUniform();
        }


        public static double GetUniformSleepSec(double min, double max)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            double re= RandomvariableStream.UniformRandomVariable.GetDoubleValue(min, max);
            return re;
        }

    }
}
