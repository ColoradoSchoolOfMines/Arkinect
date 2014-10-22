using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ACMX.Games.Arkinect
{
    interface Layer
    {
        List<Block> layOnCanvas(double canvasWidth, double canvasHeight);
    }

    class BlockLayer
    {
        public static List<Block> randomBlocks(double canvasWidth, double canvasHeight)
        {
            Type[] layers = typeof(BlockLayer).GetNestedTypes();
            Layer layer = (Layer)Activator.CreateInstance(layers[new Random().Next(layers.Length)]);
            return layer.layOnCanvas(canvasWidth, canvasHeight);
        }

        public class EvenLayer : Layer
        {
            public List<Block> layOnCanvas(double canvasWidth, double canvasHeight)
            {
                List<Block> blocks = new List<Block>();
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        blocks.Add(new Block(canvasWidth / 7.5, canvasHeight / 20, new Point(canvasWidth / 10 + j * canvasWidth / 5, canvasHeight / 20 + i * canvasHeight / 10), true));
                    }
                }
                return blocks;
            }
        }

        public class StaggeredLayer : Layer
        {
            public List<Block> layOnCanvas(double canvasWidth, double canvasHeight)
            {
                List<Block> blocks = new List<Block>();
                for (int i = 0; i < 5; i++)
                {
                    if (i % 2 == 0)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            blocks.Add(new Block(canvasWidth / 7.5, canvasHeight / 20, new Point(canvasWidth / 10 + j * canvasWidth / 5, canvasHeight / 20 + i * canvasHeight / 10), true));
                        }
                    }
                    else
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            blocks.Add(new Block(canvasWidth / 7.5, canvasHeight / 20, new Point(canvasWidth / 5 + j * canvasWidth / 5, canvasHeight / 20 + i * canvasHeight / 10), true));
                        }
                    }
                }
                return blocks;
            }
        }

    }
}
