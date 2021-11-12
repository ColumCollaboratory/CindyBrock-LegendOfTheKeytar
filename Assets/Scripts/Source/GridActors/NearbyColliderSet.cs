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
        public NearbyColliderSet(int centerX, int centerY, bool[,] colliders, CollisionDirectionMask[,] directionMasks)
        {
            this.centerX = centerX;
            this.centerY = centerY;
            this.colliders = colliders;
            this.directionMasks = directionMasks;
        }

        private readonly int centerX;
        private readonly int centerY;
        private readonly bool[,] colliders;
        private readonly CollisionDirectionMask[,] directionMasks;



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

        public bool this[Vector2Int tile] => this[tile.x, tile.y];
        public bool this[int x, int y] => colliders[x + centerX, y + centerY];

        public bool this[Vector2Int tile, CollisionDirectionMask direction] => this[tile.x, tile.y, direction];
        public bool this[int x, int y, CollisionDirectionMask direction] =>
            colliders[x + centerX, y + centerY] && ((directionMasks[x + centerX, y + centerY] & direction) > 0);
    }
}
