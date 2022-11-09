using RR.Dataplane;
using RR.RingRouting;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RR.Intilization
{
    public class Operations
    {


        public static void DrawPoint(Point i, Brush color)
        {

            Ellipse myEllipse = new Ellipse();
            myEllipse.StrokeThickness = 1;
            myEllipse.Stroke = color;
            myEllipse.Fill = color;
            myEllipse.Width = 1;
            myEllipse.Height = 1;
            myEllipse.Margin = new Thickness(i.X, i.Y, 0, 0);
            PublicParamerters.MainWindow.Canvas_SensingFeild.Children.Add(myEllipse);
        }


        /// <summary>
        /// is sen belongs to List?
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="List"></param>
        /// <returns></returns>
        public static bool FindInAlistbool(Sensor sen, List<Sensor> List)
        {
            foreach (Sensor s in List)
            {
                if (s.ID == sen.ID)
                {
                    return true;
                }
            }
            return false;
        }


        public static double GetAngle(Point i, Point j, Point d)
        {
            System.Windows.Vector u = new System.Windows.Vector(i.X - d.X, i.Y - d.Y);
            System.Windows.Vector v = new System.Windows.Vector(j.X - d.X, j.Y - d.Y);
            double angle = System.Windows.Vector.AngleBetween(u, v);

            return Math.Abs(angle);

        }

        public static Sensor FindInAlist(Sensor sen, List<Sensor> List)
        {
            foreach (Sensor s in List)
            {
                if (s.ID == sen.ID)
                {
                    return sen;
                }
            }
            return null;
        }


        /// <summary>
        /// https://www.geeksforgeeks.org/section-formula-point-divides-line-given-ratio/
        /// m close to the start point. 
        /// n close to the end point.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static Point GetPointMtoNratio(Point p1, Point p2, double d1, double d2)
        {
            double x = ((d2 * p1.X) + (d1 * p2.X)) / (d1 + d2);
            double y = ((d2 * p1.Y) + (d1 * p2.Y)) / (d1 + d2);
            return new Point(x, y);
        }

        /// <summary>
        /// Given a line starts at ns and ends at dest. 
        /// return the the distance from the point nj to the line.
        /// </summary>
        /// <param name="nj">candidiate to be selected along the line </param>
        /// <param name="ns">line stard point</param>
        /// <param name="dest"></param>
        /// <returns>per distance from the point nj to the line </returns>
        public static double Perpendiculardistance(Point nj, Point ns, Point dest)
        {
            double past = Math.Abs(((dest.Y - ns.Y) * nj.X) - ((dest.X - ns.X) * nj.Y) + (dest.X * ns.Y) - (dest.Y * ns.X));
            double sbDis = DistanceBetweenTwoPoints(ns, dest);
            double perDis = past / sbDis;
            return perDis;


        }
        /// <summary>
        /// the angle bettween the two vectors i->j and  i->d
        /// </summary>
        /// <param name="i">currrent node</param>
        /// <param name="j">candidate</param>
        /// <param name="d"> the distination point</param>
        /// <returns></returns>
        public static double AngleDotProdection(Point i, Point j, Point d)
        {
            double axb = (j.X - i.X) * (d.X - i.X) + (j.Y - i.Y) * (d.Y - i.Y);
            double disMul = DistanceBetweenTwoPoints(i, d) * DistanceBetweenTwoPoints(i, j);
            double angale = Math.Acos(axb / disMul); // dot product of the vectors j and d considering i as origin j*d = |j|*|d|cos(theta)
            double norAngle = angale / Math.PI;
            return norAngle;
        }


        public static void DrawLine(Canvas mycanvas, Point from, Point to)
        {
            Line lin = new Line();
            lin.Stroke = Brushes.Black;
            lin.StrokeThickness = 0.1;
            lin.X1 = from.X;
            lin.Y1 = from.Y;
            lin.X2 = to.X;
            lin.Y2 = to.Y;
            mycanvas.Children.Add(lin);
        }

        public static void DrawLine(Canvas mycanvas, Point from, Point to,double StrokeThickness)
        {
            Line lin = new Line();
            lin.Stroke = Brushes.Black;
            lin.StrokeThickness = StrokeThickness;
            lin.X1 = from.X;
            lin.Y1 = from.Y;
            lin.X2 = to.X;
            lin.Y2 = to.Y;
            mycanvas.Children.Add(lin);
        }


        public static double DistanceBetweenTwoSensors(Sensor sensor1, Sensor sensor2)
        {
            if (sensor1 != null && sensor2 != null)
            {
                double dx = (sensor1.CenterLocation.X - sensor2.CenterLocation.X);
                dx *= dx;
                double dy = (sensor1.CenterLocation.Y - sensor2.CenterLocation.Y);
                dy *= dy;
                return Math.Sqrt(dx + dy);
            }
            else
            {
                return double.NegativeInfinity;
            }
        }

        public static double DistanceBetweenTwoPoints(Point p1, Point p2)
        {
            double dx = (p1.X - p2.X);
            dx *= dx;
            double dy = (p1.Y - p2.Y);
            dy *= dy;
            return Math.Sqrt(dx + dy);
        }

        /// <summary>
        /// the communication range is overlapped.
        /// 
        /// </summary>
        /// <param name="sensor1"></param>
        /// <param name="sensor2"></param>
        /// <returns></returns>
        public static bool isOverlapped(Sensor sensor1, Sensor sensor2)
        {
            bool re = false;
            double disttance = DistanceBetweenTwoSensors(sensor1, sensor2);
            if (disttance < (sensor1.ComunicationRangeRadius + sensor2.ComunicationRangeRadius))
            {
                re = true;
            }
            return re;
        }

        internal static double DistanceBetweenTwoPoints(Point centerLocation1, object centerLocation2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// check if j is within the range of i.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static bool isInMySensingRange(Sensor i, Sensor j)
        {
            bool re = false;
            double disttance = DistanceBetweenTwoSensors(i, j);
            if (disttance <= (i.VisualizedRadius))
            {
                re = true;
            }
            return re;
        }

        /// <summary>
        /// commnication=sensing rang*2
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static bool isInMyComunicationRange(Sensor i, Sensor j)
        {
            bool re = false;
            double disttance = DistanceBetweenTwoSensors(i, j);
            if (disttance <= (i.ComunicationRangeRadius))
            {
                re = true;
            }
            return re;
        }

        public static double FindNodeArea(double com_raduos)
        {
            return Math.PI * Math.Pow(com_raduos, 2);
        }

        /// <summary>
        /// n!
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static double Factorial(int n)
        {
            long i, fact;
            fact = n;
            for (i = n - 1; i >= 1; i--)
            {
                fact = fact * i;
            }
            return fact;
        }

        /// <summary>
        /// combination 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static double Combination(int n, int k)
        {
            if (k == 0 || n == k) return 1;
            if (k == 1) return n;
            int dif = n - k;
            int max = Max(dif, k);
            int min = Min(dif, k);

            long i, bast;
            bast = n;
            for (i = n - 1; i > max; i--)
            {
                bast = bast * i;
            }
            double mack = Factorial(min);
            double x = bast / mack;
            return x;
        }

        public static double GetDirectionAngle(Point source, Point destination, Point forwarder)
        {
            double angle = 0;
            double srcForwarder = DistanceBetweenTwoPoints(source, forwarder);
            double srcDest = DistanceBetweenTwoPoints(source, destination);
            double forwarderDest = DistanceBetweenTwoPoints(destination, forwarder);
            double sum = (srcDest * srcDest) + (srcForwarder * srcForwarder) - (forwarderDest * forwarderDest);
            sum /= (2 * srcDest * srcForwarder);
            angle = Math.Acos(sum);

            return angle;
        }
        public static double GetPerpindicularDistance(Point src, Point dest, Point candi)
        {

            double srcAndDesDis = DistanceBetweenTwoPoints(src, dest);
            double dist = (candi.X * (dest.Y - src.Y) - (candi.Y * (dest.X - src.X)) + ((dest.X * src.Y) - (dest.Y * src.X)));
            dist = Math.Abs(dist) / srcAndDesDis;
            return dist;
        }
        public static int isClockwise(Point a, Point b, Point c)
        {
            double value = ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));
            if (value > 0)
            {
                return 1;
            }
            else if (value == 0)
            {
                return 0;
            }
            else { return 2; }

        }
        public static bool PointInPolygon(List<Sensor> poly, Sensor myPoint)
        {
            Sensor anchor = Ring.PointZero;
            bool isInside = false;
            int j = poly.Count - 1;
            Point start = myPoint.CenterLocation;
            Point end = new Point(start.X, 0);

            if (start.Y > anchor.CenterLocation.Y)
            {
                return false;
            }

            for (int i = 0; i < poly.Count; i++)
            {

                Point verticyI = new Point(poly[i].CenterLocation.X, poly[i].CenterLocation.Y);
                Point verticyJ = new Point(poly[j].CenterLocation.X, poly[j].CenterLocation.Y);

                bool intersect = (((isClockwise(start, end, verticyJ) != isClockwise(start, end, verticyI)) && (isClockwise(verticyI, verticyJ, start) != isClockwise(verticyI, verticyJ, end))));

                // bool intersect = ((verticyI.Y > myPoint.Y) != (verticyJ.Y > myPoint.Y))
                // && (myPoint.X < (verticyJ.X - verticyI.X) * (myPoint.Y - verticyI.Y) / (verticyJ.Y - verticyI.Y) + verticyI.X);

                if (intersect)
                {
                    isInside = !isInside;
                }
                j = i;

            }
            return isInside;
        }
        public static int Orientation(Point p1, Point p2, Point p3)
        {
            // See 10th slides from following link  
            // for derivation of the formula 
            double v = (p2.Y - p1.Y) * (p3.X - p2.X) - (p2.X - p1.X) * (p3.Y - p2.Y);
            int val = Convert.ToInt32(v);
            if (val == 0) return 0; // colinear 

            // clock or counterclock wise 
            return (val > 0) ? 1 : 2;
        }
        public static Point GetDirectionToRingNodes(Sensor source)
        {
            Point src = source.CenterLocation;
            Point center = PublicParamerters.networkCenter;
            double xDif = center.X - src.X;
            double yDif = center.Y - src.Y;
            double destX = 0;
            double destY = 0;
            // first up or down 
            if (yDif < 0)
            {
                //it means it down
                destY--;
            }
            else if (yDif > 0)
            {
                //its up
                destY++;
            }
            if (xDif < 0)
            {
                destX++;
            }
            else if (xDif > 0)
            {
                destX--;
            }
            destX = Math.Round(destX * PublicParamerters.clusterRadius);
            destY = Math.Round(destY * PublicParamerters.clusterRadius);
            Point destination = new Point(source.CenterLocation.X + destX, source.CenterLocation.Y + destY);
            return destination;
        }

        public static List<int> PacketPathToIDS(String path)
        {
            String[] strIDS = path.Split('>');
            List<int> ids = new List<int>();

            foreach (String id in strIDS)
            {
                try
                {
                    int x = Int16.Parse(id);
                    ids.Add(x);
                }catch(Exception e)
                {
                    break;
                }
               
            }
            return ids;

        }
        private static int Max(int n1,int n2) { if (n1 > n2) return n1; else return n2; }
        private static int Min(int n1, int n2) { if (n1 < n2) return n1; else return n2; } 
    }
}
