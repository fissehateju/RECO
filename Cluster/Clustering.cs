using RR.Intilization;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RR.ui;
using RR.Properties;
using System.Windows.Threading;
using RR.ui.conts;
using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Comuting.computing;
using RR.NetAnimator;
using RR.Comuting.Routing;
using RR.Comuting.SinkClustering;
using RR.RingRouting;
using RR.Models.Energy;
using MP.MergedPath.Routing;

namespace RR.Cluster
{
    internal class Clustering
    {
        private Canvas SensingField;
        private double canvasHeight { get; set; }
        private double canvasWidth { get; set; }
        private double CellRadius { get; set; }
        private double minClusterRad = PublicParamerters.CommunicationRangeRadius * 1.5;

        private Point TopLeft = new Point();
        private Point TopRight = new Point();
        private Point BottomLeft = new Point();
        private Point BottomRight = new Point();
        private static List<Line> RegBorders = new List<Line>();
        private static List<Sensor> netNodes = new List<Sensor>();

        public Clustering(Canvas canv, List<Sensor> net)
        {
            SensingField = canv;
            netNodes = net;
            canvasHeight = SensingField.Height;
            canvasWidth = SensingField.Width;
            getBorderNodes();
            Partitioning_Network_area();
            ChooseHeader();
            //drawNewBorder();
        }

        private void getBorderNodes()
        {
            double smallx = double.MaxValue;
            double smally = double.MaxValue;
            double bigx = 0.0;
            double bigy = 0.0;
            foreach (Sensor sen in netNodes)
            {

                if (sen.CenterLocation.X >= bigx)
                {
                    bigx = sen.CenterLocation.X;
                }
                else if (sen.CenterLocation.X <= smallx)
                {
                    smallx = sen.CenterLocation.X;
                }
                if (sen.CenterLocation.Y >= bigy)
                {
                    bigy = sen.CenterLocation.Y;
                }
                else if (sen.CenterLocation.Y <= smally)
                {
                    smally = sen.CenterLocation.Y;
                }
            }

            PublicParamerters.mostleft = smallx;
            PublicParamerters.mostright = bigx;
            PublicParamerters.mosttop = smally;
            PublicParamerters.mostbottom = bigy;

        }

        private void Partitioning_Network_area()
        {
            double numOfReghorizon = canvasWidth / minClusterRad;
            double numOfRegvert = canvasHeight / minClusterRad;
            
            NetCluster part;
            int id = 1;
            for (int i = 0; i < Math.Floor(numOfReghorizon); i++)
            {
                for (int j = 0; j < Math.Floor(numOfRegvert); j++)
                {

                    TopLeft = new Point(i * (canvasWidth / numOfReghorizon), j * (canvasHeight / numOfRegvert));
                    TopRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), j * (canvasHeight / numOfRegvert));
                    BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), (j + 1) * (canvasHeight / numOfRegvert));
                    BottomRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), (j + 1) * (canvasHeight / numOfRegvert));

                    if (i == Math.Floor(numOfReghorizon) - 1)
                    {
                        TopRight = new Point(PublicParamerters.mostright + 10, j * (canvasHeight / numOfRegvert));
                        BottomRight = new Point(PublicParamerters.mostright + 10, (j + 1) * (canvasHeight / numOfRegvert));
                    }
                    if (j == Math.Floor(numOfRegvert) - 1)
                    {
                        BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), PublicParamerters.mostbottom + 10);
                        BottomRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), PublicParamerters.mostbottom + 10);
                    }
                    if (i == Math.Floor(numOfReghorizon) - 1 && j == Math.Floor(numOfRegvert) - 1)
                    {
                        TopRight = new Point(PublicParamerters.mostright + 10, j * (canvasHeight / numOfRegvert));
                        BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), PublicParamerters.mostbottom + 10);
                        BottomRight = new Point(PublicParamerters.mostright + 10, PublicParamerters.mostbottom + 10);
                    }

                    part = new NetCluster(id, TopLeft, BottomRight);
                    part.width = Operations.DistanceBetweenTwoPoints(TopLeft, TopRight);
                    part.height = Operations.DistanceBetweenTwoPoints(TopLeft, BottomLeft);

                    id += 1;

                    PublicParamerters.listOfRegs.Add(part);
                       
                }
            }

            // Defining the cluster member nodes
            DefineTheClusterElemets();

        }

        private void DefineTheClusterElemets()
        {
            foreach (var Reg in PublicParamerters.listOfRegs)
            {
                foreach (Sensor sen in netNodes)
                {
                    if (sen.CenterLocation.X >= Reg.TopLeft.X && sen.CenterLocation.Y >= Reg.TopLeft.Y &&
                        sen.CenterLocation.X < Reg.BottomRight.X && sen.CenterLocation.Y < Reg.BottomRight.Y)
                    {
                        Reg.MemberNodes.Add(sen);
                        sen.myCluster = Reg;
                    }
                }
            }
        }


        /// <summary>
        /// one time operation at network initialiazation 
        /// afterward cluster will select in distributed way
        /// </summary>

        private void ChooseHeader()
        {
            foreach (var reg in PublicParamerters.listOfRegs)
            {
                double minDs = double.MaxValue;
                Sensor possibleheader = null;
                foreach (Sensor sen in reg.MemberNodes)
                {
                    double dis = Operations.DistanceBetweenTwoPoints(reg.centerPoint, sen.CenterLocation);
                    if (dis < minDs)
                    {
                        minDs = dis;
                        possibleheader = sen;
                    }
                }
                possibleheader.isHeader = true;
                reg.Header = possibleheader;


                Action actionx1 = () => reg.Header.Ellipse_headerIndicator.Visibility = Visibility.Visible;
                reg.Header.Dispatcher.Invoke(actionx1);
            }
           
        }



        private Line linee;
        private void drawNewBorder()
        {

            int n = PublicParamerters.listOfRegs.Count;
            int ctr = n % 3;
            int bWeight;
            foreach (NetCluster regin in PublicParamerters.listOfRegs)
            {
                if (regin.Id > 4)
                {
                    bWeight = regin.Id % 4 + 1;
                }
                else
                {
                    bWeight = regin.Id;
                }
                bWeight = 1;

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.TopLeft.X;
                linee.Y1 = regin.TopLeft.Y;
                linee.X2 = regin.TopLeft.X + regin.width;
                linee.Y2 = regin.TopLeft.Y;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.TopLeft.X;
                linee.Y1 = regin.TopLeft.Y;
                linee.X2 = regin.TopLeft.X;
                linee.Y2 = regin.TopLeft.Y + regin.height;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.TopLeft.X;
                linee.Y1 = regin.TopLeft.Y + regin.height;
                linee.X2 = regin.TopLeft.X + regin.width;
                linee.Y2 = regin.TopLeft.Y + regin.height;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.TopLeft.X + regin.width;
                linee.Y1 = regin.TopLeft.Y;
                linee.X2 = regin.TopLeft.X + regin.width;
                linee.Y2 = regin.TopLeft.Y + regin.height;
                SensingField.Children.Add(linee);
            }
        }
    }

}
