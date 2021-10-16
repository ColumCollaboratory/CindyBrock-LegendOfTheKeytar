using UnityEngine;

namespace BattleRoyalRhythm.GridActors
{
    // This class acts as both a BoundsInt2D as well
    // as using method chaining to improve the legibility
    // of constructing grid regions (instead of just passing 4 ints).
    /// <summary>
    /// Represents a region of a unit grid.
    /// </summary>
    public struct GridRegion
    {
        #region Fields       | Region Bounds
        private Vector2Int min;
        private Vector2Int max;
        #endregion
        #region Constructors | Default Region
        /// <summary>
        /// Creates a new grid region of 1x1 tiles at the start tile.
        /// </summary>
        /// <param name="startTile">A tile in the region that is the starting reference point.</param>
        public GridRegion(Vector2Int startTile)
        {
            min = startTile;
            max = startTile;
        }
        #endregion
        #region Properties   | Region Bounds, Validated
        /// <summary>
        /// The minimum boundary of the region (bottom left).
        /// </summary>
        public Vector2Int Min
        {
            get => min;
            set
            {
                min = value;
                if (max.x < min.x)
                    max.x = min.x;
                if (max.y < min.y)
                    max.y = min.y;
            }
        }
        /// <summary>
        /// The maximum boundary of the region (top right).
        /// </summary>
        public Vector2Int Max
        {
            get => max;
            set
            {
                max = value;
                if (min.x > max.x)
                    min.x = max.x;
                if (min.y > max.y)
                    min.y = max.y;
            }
        }
        #endregion
        #region Methods      | Region Modification via Chaining
        /// <summary>
        /// Expands the left or right region edge outwards.
        /// </summary>
        /// <param name="tiles">The number of tiles to expand out, the direction is determined by the sign.</param>
        /// <returns>The updated grid region.</returns>
        public GridRegion PushXBound(int tiles)
        {
            if (tiles > 0)
                max.x += tiles;
            else if (tiles < 0)
                min.x += tiles;
            return this;
        }
        /// <summary>
        /// Expands the bottom or top region edge outwards.
        /// </summary>
        /// <param name="tiles">The number of tiles to expand out, the direction is determined by the sign.</param>
        /// <returns>The updated grid region.</returns>
        public GridRegion PushYBound(int tiles)
        {
            if (tiles > 0)
                max.y += tiles;
            else if (tiles < 0)
                min.y += tiles;
            return this;
        }
        #endregion
    }
}
