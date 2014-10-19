using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ACMX.Games.Arkinect
{
    class Ball
    {
        public const int BALL_RADIUS = 20;

        public Point loc;
        public Point vel;

        public Ball(Point l, Point v)
        {
            loc = l;
            vel = v;
        }

        public void move(double dx, double dy)
        {
            loc.Offset(dx, dy);
        }

        public void move()
        {
            loc.Offset(vel.X, vel.Y);
        }

        public void reflectX(double axis)
        {
            loc.Offset(-2 * (loc.X - axis), 0);
        }

        public void reflectY(double axis)
        {
            loc.Offset(0, -2 * (loc.Y - axis));
        }
    }
}
