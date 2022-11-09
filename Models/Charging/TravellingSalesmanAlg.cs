using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Energy;
using RR.Intilization;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace RR.Models.Charging
{
    public class Route
    {
        public List<Location> Locations { get; }
        public List<int> Routes { get; }
        public int Length { get { return length; } }
        private int length;
        private void calculateLength()
        {
            length = 0;
            if (Locations.Count <= 1)
                return;

            for (int i = 0; i < Locations.Count - 1; i++)
                length += Locations[i].GetDistanceTo(Locations[i + 1]);
        }

        public void Print()
        {
            Console.WriteLine(string.Join("->", Locations.Select(x => x.ID)) + $" {Length} m.");

        }

        public Route(List<Location> locations)
        {
            Locations = locations;
            calculateLength();
        }

        public Route TwoOptSwap(int i, int k)
        {
            //reverse the subpart from index i to k
            var locations = new List<Location>(Locations);
            locations.Reverse(i, k - i + 1);
            return new Route(locations);
        }
    }
        public class Location
        {
        public int ID { get; }
        public Point Address { get; }
        public Dictionary<Location, int> distancesToDic { get; set; }
        public int GetDistanceTo(Location to)
        {
            return distancesToDic[to];
        }

        public Location FindNearestUnvisitedNeighbor(HashSet<Location> visitedSet)
        {
            var nearestUnvisitedNeighbour = distancesToDic.Where(entry => !visitedSet.Contains(entry.Key)).OrderBy(d => d.Value).First().Key;
            return nearestUnvisitedNeighbour;
        }

        public Location(int pack, Point address)
        {
            ID = pack;
            Address = address;
        }

        }

    public class TravellingSalesmanAlg
    {
        public Point startPoint = new Point();
        public List<Dictionary<Packet, double>> distancesToDic = new List<Dictionary<Packet, double>>();
        public Dictionary<Packet, double> distances { get; set; }
        private List<Location> locations;
        public TravellingSalesmanAlg(List<Location> locations)
        {
            this.locations = locations;
            Console.WriteLine("Distances:");
            foreach (var location in locations)
            {
                location.distancesToDic = new Dictionary<Location, int>();
                foreach (var otherLocation in locations.Where(x => x != location))
                {
                    var distance = (int)Operations.DistanceBetweenTwoPoints(location.Address, otherLocation.Address);
                    location.distancesToDic.Add(otherLocation, distance);
                    //Console.WriteLine($"{location.Name} -> {otherLocation.Name} : {distance} m.");
                }
            }
        }

        private Route initializeNearestNeighborRoute()
        {
            var nnVisitedSet = new HashSet<Location>();
            var nnRouteLocations = new List<Location>();
            nnRouteLocations.Add(locations[0]);
            nnVisitedSet.Add(locations[0]);
            for (int i = 0; i < locations.Count - 1; i++)
            {
                var nearestUnvisitedNeighbor = nnRouteLocations[i].FindNearestUnvisitedNeighbor(nnVisitedSet);
                nnRouteLocations.Add(nearestUnvisitedNeighbor);
                nnVisitedSet.Add(nearestUnvisitedNeighbor);
            }

            return new Route(new List<Location>(nnRouteLocations));
        }


        public Route Solve()
        {
            var startTime = DateTime.Now;

            var existingRoute = initializeNearestNeighborRoute();
            var count = locations.Count;
            //Console.Write($"Initial route:");
            //existingRoute.Print();

            var anyImprovement = true;
            while (anyImprovement)
            {
                anyImprovement = false;
                for (int i = 1; i < count - 1; i++)
                {
                    for (int k = i + 1; k < count; k++)
                    {
                        //Console.Write($"trying {i}, {k} ");
                        var newRoute = existingRoute.TwoOptSwap(i, k);
                        //newRoute.Print();
                        if (newRoute.Length < existingRoute.Length)
                        {
                            anyImprovement = true;
                            existingRoute = newRoute;
                            //Console.Write($"Improved route ({i},{k}):");
                            //newRoute.Print();
                            break;
                        }
                    }
                    if (anyImprovement)
                        break;
                }
            }

            //https://en.wikipedia.org/wiki/2-opt
            //repeat until no improvement is made {
            //    start_again:
            //    best_distance = calculateTotalDistance(existing_route)
            //    for (i = 1; i < number of nodes eligible to be swapped - 1; i++) {
            //        for (k = i + 1; k < number of nodes eligible to be swapped; k++) {
            //            new_route = 2optSwap(existing_route, i, k)
            //            new_distance = calculateTotalDistance(new_route)
            //            if (new_distance < best_distance)
            //            {
            //              existing_route = new_route
            //              goto start_again
            //            }
            //        }
            //    }
            //}

            //Console.Write("Result: ");
            //existingRoute.Print();
            var finishTime = DateTime.Now;
            var duration = finishTime - startTime;
            Console.WriteLine($"Started at: {startTime}, finished at {finishTime}, took {duration.Seconds} seconds");
            return existingRoute;
        }
    }
}
