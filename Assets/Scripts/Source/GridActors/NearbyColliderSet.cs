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


        public bool this[int x, int y] => colliders[x + centerX, y + centerY];

        public bool this[Vector2Int tile] => this[tile.x, tile.y];

    }
}
