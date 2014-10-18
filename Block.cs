using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ACMX.Games.Arkinect
{
    class Block
    {
        public double width { get; private set;}
        public double height { get; private set; }

        public bool isDestroyable { get; private set; }

        public Point loc { get; set; }

        public Block(double width, double height, Point loc, bool isDestroyable)
        {
            this.width = width;
            this.height = height;
            this.loc = loc;
            this.isDestroyable = isDestroyable;
        }

        public Rect getCollisionBox()
        {
            return new Rect(loc.X - width / 2, loc.Y - height / 2, width, height);
        }
    }
}
