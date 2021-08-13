using System;
using UnityEngine;

namespace BattleRoyalRhythm.Surfaces
{
    /// <summary>
    /// Defines the parameters for stitching a traversal seam
    /// between two surfaces. Used by designers with the GridWorld
    /// stitching system.
    /// </summary>
    [Serializable]
    public sealed class StitchingConstraint
    {
        #region Enums
        /// <summary>
        /// The direction of traversal across a seam.
        /// </summary>
        public enum Direction : byte
        {
            Right,
            Left,
            DoorwayEntrance,
            DoorwayExit
        }
        /// <summary>
        /// The ways which the link can be traversed.
        /// </summary>
        public enum Traversal : byte
        {
            TwoWay,
            OnlyToOther,
            OnlyFromOther
        }
        #endregion
        #region Inspector Fields
        [HideInInspector] public string inspectorName = "New Link";
        [Tooltip("The other surface to stitch to.")]
        [SerializeField] public Surface other = null;
        [Header("Traversal")]
        [Tooltip("Determines which direction actors have to step across this link from this surface.")]
        [SerializeField] public Direction linkDirection = Direction.Right;
        [Tooltip("Sets restraints on the direction of travel across the seam.")]
        [SerializeField] public Traversal actorTraversal = Traversal.TwoWay;
        [Header("Seam Location")]
        [Tooltip("The x tile location on this surface to step from." +
            "Positive values are relative to the left. " +
            "Negative values are relative to the right.")]
        [SerializeField] public int fromTileOfThisSurface = 1;
        [Tooltip("The x tile location on the other surface to step to." +
            "Positive values are relative to the left. " +
            "Negative values are relative to the right.")]
        [SerializeField] public int toTileOfOtherSurface = 1;
        [Tooltip("Adds an offset along the vertical axis to step the surface up/down.")]
        [SerializeField] public int yStep = 0;
        [Header("Seam Angle")]
        [Tooltip("Determines the angle at the intersection of these two surfaces.")]
        [SerializeField][Range(-180f, 180f)] public float angle = 0f;
        #endregion
    }
}
