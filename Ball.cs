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

        public void limitedMove(double limit)
        {
            double ratio = limit / Math.Sqrt(vel.X * vel.X + vel.Y * vel.Y);
            ratio = Math.Min(1, ratio);
            move(vel.X * ratio, vel.Y * ratio);
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

        public void collideInside(Rect obj)
        {
            if (loc.X < obj.Left + Ball.BALL_RADIUS)
            {
                vel.Offset(-2 * vel.X, 0);
                reflectX(Ball.BALL_RADIUS);
            }
            if (loc.Y < obj.Top + Ball.BALL_RADIUS)
            {
                vel.Offset(0, -2 * vel.Y);
                reflectY(Ball.BALL_RADIUS);
            }
            if (loc.X > obj.Right - Ball.BALL_RADIUS)
            {
                vel.Offset(-2 * vel.X, 0);
                reflectX(obj.Right - Ball.BALL_RADIUS);
            }
            if (loc.Y > obj.Bottom - Ball.BALL_RADIUS)
            {
                vel.Offset(0, -2 * vel.Y);
                reflectY(obj.Bottom - Ball.BALL_RADIUS);
            }
        }

        public bool collideOutside(Rect obj)
        {
            Rect selfRect = new Rect(loc.X - BALL_RADIUS, loc.Y - BALL_RADIUS, 2 * BALL_RADIUS, 2 * BALL_RADIUS);
            Rect intersect = Rect.Intersect(obj, selfRect);
            if (intersect.IsEmpty)
            {
                return false;
            }
            if (intersect.Width < intersect.Height)
            {
                vel.Offset(-2 * vel.X, 0);
            }
            else
            {
                vel.Offset(0, -2 * vel.Y);
            }
            return true;
        }
    }
}
