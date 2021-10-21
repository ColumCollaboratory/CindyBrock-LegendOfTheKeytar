using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors
{
    public sealed class NearbyColliderSet
    {
        public NearbyColliderSet(bool[,] colliders, int centerX, int centerY)
        {
            this.colliders = colliders;
            this.centerX = centerX;
            this.centerY = centerY;
        }

        private readonly bool[,] colliders;

        private readonly int centerX;
        private readonly int centerY;


        public bool AnyInside(Vector2Int from, Vector2Int to)
        {
            return AnyInside(from.x, from.y, to.x, to.y);
        }

        public bool AnyInside(int x1, int y1, int x2, int y2)
        {
            int xMin = Mathf.Min(x1, x2);
            int xMax = Mathf.Max(x1, x2);
            int yMin = Mathf.Min(y1, y2);
            int yMax = Mathf.Max(y1, y2);

            for (int x = xMin; x <= xMax; x++)
                for (int y = yMin; y <= yMax; y++)
                    if (this[x, y])
                        return true;
            return false;
        }

        public bool this[int x, int y] => colliders[x + centerX, y + centerY];

        public bool this[Vector2Int tile] => this[tile.x, tile.y];

    }
}
