using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public enum SpriteSheetSize
    {
        Unknown = 0,
        FiveByFive = 1,
        TenByTen = 2
    }

    public static class SpriteSheetExtensions
    {
        public static int SpriteSheetTiles(this SpriteSheetSize spriteSheetSize, out int horizontalTiles, out int verticalTiles)
        {
            horizontalTiles = 1;
            verticalTiles = 1;
            switch (spriteSheetSize)
            {
                case SpriteSheetSize.FiveByFive:
                    horizontalTiles = 5;
                    verticalTiles = 5;
                    break;

                case SpriteSheetSize.TenByTen:
                    horizontalTiles = 10;
                    verticalTiles = 10;
                    break;
            }

            return (horizontalTiles * verticalTiles);
        }
    }
}
