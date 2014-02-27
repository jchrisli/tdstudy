#define SERVER_SIDE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;


namespace SocketTestClient
{
    public class Targets
    {
        private int len;
        //private bool[] flags; //a bool array of xGrid * yGrid. True means there is a target at the responding (x * 21 + y) position. False otherwise.
        private Ellipse[] targetEllipses;
        static private int size = 60;
        static private int number = 20;
        static private int xGrid = 21;
        static private int yGrid = 10;
        private Canvas parent;
        private List<Point> positionList;
        Ellipse marker;
        private int currentIndex; //index of the 1-D array (corresponding to the 2-D array)
        private int currentPermutationIndex;
        private int[] permutation;
        private int currentBackupIndex = number;
        static private int[] gridToAvoid = { 0, 1, 19, 20 }; //the grid coordinate of positions where we want to avoid because they are too close to the lamp.
        //for gesturing task
        private int[,] distanceTable;
        private int currentAreaIndex;
        private int areaRadius; //radius of the area to be selected for each round, which is set to the average of the Manhattan distance between targets

        //for participant task
        private bool?[] scheduleTable;
        static private double factor = 2;
        private int scheduleTableIndex = 0;
        private Ellipse oneTarget;
                

        public Targets()
        {
            len = xGrid * yGrid;
            //flags = new bool[len];
            //for (int i = 0; i < len; i++) flags[i] = false;
            targetEllipses = new Ellipse[number];
            for (int i = 0; i < number; i++)
            {
                targetEllipses[i] = new Ellipse();
            }
                foreach (Ellipse e in targetEllipses)
                {

                    SolidColorBrush brush = new SolidColorBrush(Colors.DarkGoldenrod);
                    e.Fill = brush;
                    e.Width = size;
                    e.Height = size;
                    e.Stroke = new SolidColorBrush(Colors.Transparent);
                }
            positionList= new List<Point>();
            marker = new Ellipse();
            marker.Width = 10;
            marker.Height = 10;
            marker.Fill = new SolidColorBrush(Colors.Black);
            marker.Stroke = new SolidColorBrush(Colors.Transparent);

            //initiate a 20 * 20 2d array to store the distances between targets
            distanceTable = new int[number, number];

            //initiate a schedule table containing information of the relative proportion of targets between two sides
            scheduleTable = new bool?[(int)(number * (factor + 1))];
            Random r = new Random();
            for (int i = 0; i < scheduleTable.Length; i++)
            {
                if (i < number) scheduleTable[i] = true;
                else scheduleTable[i] = false;
            }
            for (int i = 0; i < scheduleTable.Length; i++)
            {
                int now = (int)(r.NextDouble() * i);
                bool? temp = scheduleTable[i];
                scheduleTable[i] = scheduleTable[now];
                scheduleTable[now] = temp;
            }

            oneTarget = new Ellipse();
            SolidColorBrush b = new SolidColorBrush(Colors.DarkGoldenrod);
            oneTarget.Fill = b;
            oneTarget.Width = size;
            oneTarget.Height = size;
            oneTarget.Stroke = new SolidColorBrush(Colors.Transparent);
        }

        public void IncrementScheduleTable()
        {
            scheduleTableIndex++;
        }

        public bool? CurrentScheduleTable()
        {
            if (scheduleTableIndex >= scheduleTable.Length) return null;
            else return scheduleTable[scheduleTableIndex];
        }

        /// <summary>
        /// Must be used in a dispatcher
        /// </summary>
        /// <param name="c"></param>
        public void SetCanvas(Canvas c)
        {
            parent = c;
            parent.Children.Add(oneTarget);
            oneTarget.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Increment the index of position list
        /// </summary>
        /// <returns>Return true if all 20 targets have been gone through</returns>
        public bool NextOneTarget()
        {
            currentPermutationIndex++;
            if (currentPermutationIndex >= number)
            {
                return true;
            }
            else
            {
                currentIndex = permutation[currentPermutationIndex];
                return false;
            }
        }

        public void DisplayOneTarget()
        {
            if(parent != null)
            {
                if (currentPermutationIndex < permutation.Length)
                {
                    oneTarget.Visibility = Visibility.Visible;
                    Point nextPosition = positionList[currentPermutationIndex];
                    Canvas.SetLeft(oneTarget, nextPosition.X * size);
                    Canvas.SetTop(oneTarget, nextPosition.Y * size);
                }
                else Console.WriteLine("permutation index error!");
            }
        }

        public void HideOneTarget()
        {
            oneTarget.Visibility = Visibility.Hidden;
        }

        public void Shuffle()
        {
            Random r = new Random();
            permutation = new int[len];
            positionList.Clear();
            for (int i = 0; i < permutation.Length; i++)
            {
                permutation[i] = i;
            }
            for (int i = permutation.Length - 1; i > 0 ; i--)
            {
                int now = (int)(r.NextDouble() * i);
                int temp = permutation[i];
                permutation[i] = permutation[now];
                permutation[now] = temp;
            }
            for (int i = 0; i < number; i++)
            {
                //flags[permutation[i]] = true;
                if (!matchGridToAvoid(permutation[i]))
                    positionList.Add(new Point((permutation[i] % xGrid), (int)(permutation[i] / xGrid)));
                else
                {
                    if(currentBackupIndex == len) permutation[i] = 3;
                    else
                    {
                        do
                        {
                            permutation[i] = permutation[currentBackupIndex];
                            currentBackupIndex++;
                        }
                        while (currentBackupIndex < len && (matchGridToAvoid(permutation[i])));
                    }
                    positionList.Add(new Point((permutation[i] % xGrid), (int)(permutation[i] / xGrid)));
                }
            }
            currentPermutationIndex = 0;
            currentIndex = permutation[currentPermutationIndex];
            currentBackupIndex = number;

            currentAreaIndex = currentIndex;
        }

        
        bool matchGridToAvoid(int position)
        {
            int i = 0;
            while (i < gridToAvoid.Length)
            {
                if (position == gridToAvoid[i])
                    break;
                else i++;
            }
            if (i == gridToAvoid.Length) return false;
            else return true;   
        }

        /// <summary>
        /// Show the targets. This function manipulates ui so it needs to be called in a dispatcher.
        /// </summary>
        /// <param name="parentCanvas">The canvas which is the parent of all the targets</param>
        public void Display(Canvas parentCanvas)
        {
            parent = parentCanvas;
            if (targetEllipses != null)
            {
                int i = 0;
                foreach (Ellipse e in targetEllipses)
                {
                    parentCanvas.Children.Add(e);
                    Point position = positionList[i];
                    Canvas.SetLeft(e, size * position.X);
                    Canvas.SetTop(e, size * position.Y);
                    i++;
                }
                parent.Children.Add(marker);
                marker.Visibility = Visibility.Hidden;
            }
        }

        public bool testTouch(double x, double y)
        {
            int xGridPosition = (int)(x / size);
            int yGridPosition = (int)(y / size);
            int index = yGridPosition * xGrid + xGridPosition;
            try
            {
                if (index != currentIndex) return false;
                else
                {
                    double centerX = xGridPosition * size + size / 2;
                    double centerY = yGridPosition * size + size / 2;
                    if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) < size * size / 4)
                    {
                        return true;
                    }
                    else return false;
                }
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("index out of array length");
                return false;
            }
        }

        /// <summary>
        /// Clear everyting in the list. Needs to be used with a dispatcher.
        /// </summary>
        public void Clear()
        {
            if (parent != null)
            {
                if (targetEllipses != null)
                {
                    foreach (Ellipse e in targetEllipses)
                    {
                        parent.Children.Remove(e);
                    }
                }
                if (marker != null)
                    parent.Children.Remove(marker);
            }
        }

        public bool NextMarker()
        {
            currentPermutationIndex++;
            if (currentPermutationIndex == number)
            {
                return true; //so one round is ended
            }
            else
            {
                currentIndex = permutation[currentPermutationIndex];
                return false;
            }
        }

        /// <summary>
        /// Move the marker that indicates which of the ellipses is the current target. Must be used in dispatcher.
        /// </summary>
        public void MoveMarker()
        {
            if (marker.Visibility == Visibility.Hidden) marker.Visibility = Visibility.Visible;
            if (currentPermutationIndex < permutation.Length)
            {
                Point nextPosition = positionList[currentPermutationIndex];
                Canvas.SetLeft(marker, nextPosition.X * size + size / 2 - marker.Width / 2);
                Canvas.SetTop(marker, nextPosition.Y * size + size / 2 - marker.Height / 2);
            }
            else Console.WriteLine("permutation index error!");
        }

        public void CurrentTargetGrid(ref int x, ref int y)
        {
            x = ((int)positionList[currentPermutationIndex - 1].X);
            y = ((int)positionList[currentPermutationIndex - 1].Y);

        }

        /// <summary>
        /// Calculate the Manhattan distance between two targets
        /// </summary>
        /// <param name="indexA">position of target A</param>
        /// <param name="indexB">position of target B</param>
        private int ManhattanDistance(Point pointA, Point pointB)
        {
            return (int)(Math.Abs(pointA.X - pointB.X) + Math.Abs(pointA.Y - pointB.Y));
        }

        /// <summary>
        /// Generate a graph, whose links between nodes are marked by their manhanttan distance
        /// </summary>
        private void DistanceGraph()
        {
            int sum = 0;
            int count = 0;
            for (int i = 0; i < number; i++)
            {
                int gridPositionX1 = permutation[i] % xGrid;
                int gridPositionY1 = permutation[i] / xGrid;
                for (int j = i; j < number; j++)
                {
                    int gridPositionX2 = permutation[j] % xGrid;
                    int gridPositionY2 = permutation[i] / xGrid;
                    int distance = ManhattanDistance(new Point(gridPositionX1, gridPositionY1), new Point(gridPositionX2, gridPositionY2));
                    distanceTable[i, j] = distance;
                    distanceTable[j, i] = distanceTable[i, j];
                    sum += distance;
                    if (i != j) count++;
                }
            }
            areaRadius = sum / count;
        }

        /// <summary>
        /// Find the targets around a certain target
        /// </summary>
        /// <param name="centerIndex">The index of the center target in the permutation list</param>
        /// <param name="radius">The radius within which targets will be selected</param>
        /// <returns>Return a list of target indice</returns>
        private List<int> AdjacentTargets(int centerIndex, int radius)
        {
            List<int> indexOfTargetsFound = new List<int>();
            for (int j = 0; j < number; j++)
            {
                if (distanceTable[centerIndex, j] < radius)
                    indexOfTargetsFound.Add(j);
            }
            return indexOfTargetsFound;
        }

        private bool NextAreaOfTargets()
        {
            if (currentPermutationIndex++ < number)
            {
                currentIndex = permutation[currentPermutationIndex];
                List<int> targetGroup = AdjacentTargets(currentIndex, areaRadius);
                return true;
            }
            else return true;

        }
    }
}
