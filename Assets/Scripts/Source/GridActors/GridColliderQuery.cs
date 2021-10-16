using System.Collections.Generic;
using UnityEngine;
using BattleRoyalRhythm.Surfaces;
using System.Linq;

namespace BattleRoyalRhythm.GridActors
{
    // This class helps to abstract away the stitching
    // mechanism of the grid world. Wrapping over surfaces
    // and handling out of bounds requests is handled internally
    // in this class so that outside of the class the collision
    // space can be thought of simply as a 2D grid relative to
    // the center of the query.
    /// <summary>
    /// A collider query lets you check for surrounding colliders
    /// relative to an actor on the grid.
    /// </summary>
    public sealed class GridColliderQuery
    {
        #region Fields
        private readonly GridActor actor;
        #endregion
        #region Constructor | Initializes query collections and properties
        /// <summary>
        /// Creates a new grid collider query based on a target grid actor.
        /// </summary>
        /// <param name="actor">The actor that the query is centered on.</param>
        public GridColliderQuery(GridActor actor)
        {
            this.actor = actor;
            IgnoredActors = new List<GridActor>();
            actor.SurfaceChanged += OnSurfaceChanged;
            IgnoreStaticColliders = false;
            DumpCachedState();
        }
        #endregion

        private void OnSurfaceChanged(Surface newSurface) => DumpCachedState();

        /// <summary>
        /// Removes any cached collider data from previous queries.
        /// This should be called every time colliders are expected to
        /// change for that are relavent to the query. This is called
        /// automatically when the target surface changes.
        /// </summary>
        public void DumpCachedState()
        {
            cachedStateRoot = new CachedSurfaceNode(actor.CurrentSurface);
        }

        private CachedSurfaceNode cachedStateRoot;

        private sealed class CachedSurfaceNode
        {
            public CachedSurfaceNode(Surface surface)
            {
                this.surface = surface;
                cachedLeftLinks = new CachedSurfaceNode[surface.LengthY];
                cachedRightLinks = new CachedSurfaceNode[surface.LengthY];
                actorColliders = new List<GridActor>[surface.LengthX, surface.LengthY];
            }

            private readonly Surface surface;

            public bool StaticColliderAt(Vector2Int tile)
            {
                // Is the requested point on this surface node?
                if (tile.x >= 1 && tile.x <= surface.LengthX && tile.y >= 1 && tile.y <= surface.LengthY)
                {
                    return World.surfaceColliders[surface][tile.x, tile.y];
                }
                else
                {

                }
            }

            public List<GridActor> ActorCollidersAt(Vector2Int tile)
            {

            }


            private readonly CachedSurfaceNode[] cachedLeftLinks;
            private readonly CachedSurfaceNode[] cachedRightLinks;

            private readonly List<GridActor>[,] actorColliders;
        }

        private bool this[int x, int y]
        {
            get
            {
                int localX = x - actor.Tile.x;
                int localY = y - actor.Tile.y;

            }
        }

        private readonly bool[,] colliders;

        public bool IgnoreStaticColliders { get; set; }

        public List<GridActor> IgnoredActors { get; }

        public void IgnoreActorsOfType<T>()
        {
            foreach (GridActor otherActor in actor.World.Actors)
                if (otherActor is T && !IgnoredActors.Contains(otherActor))
                    IgnoredActors.Add(otherActor);

        }


        public bool TileObstructed(Vector2Int tile) => TileObstructed(tile.x, tile.y);
        public bool TileObstructed(int x, int y) => this[x, y];


        public bool AnyTileObstructedIn(GridRegion region)
        {
            for (int x = region.Min.x; x <= region.Max.x; x++)
                for (int y = region.Min.y; y <= region.Max.y; y++)
                    if (this[x, y])
                        return true;
            return false;
        }

        public bool EveryTileObstructedIn(GridRegion region)
        {
            for (int x = region.Min.x; x <= region.Max.x; x++)
                for (int y = region.Min.y; y <= region.Max.y; y++)
                    if (!this[x, y])
                        return false;
            return true;
        }


    }
}
