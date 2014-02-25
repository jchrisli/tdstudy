using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SocketTestClient
{
    public class TrackingEventArgs : EventArgs
    {
        double x;
        double y;

        public TrackingEventArgs(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double X
        {
            get { return this.x; }
            set { this.x = value; }
        }

        public double Y
        {
            get { return this.y; }
            set { this.y = value; }
        }

    }
}
