using BattleRoyalRhythm.Surfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors
{
    /// <summary>
    /// Holds a collection of surfaces and actors that move on those
    /// surfaces. Actors can see other actors in the same Grid World.
    /// </summary>
    [ExecuteInEditMode]
    public sealed class GridWorld : MonoBehaviour
    {

        #region Inspector Fields
        [Tooltip("The first surface centered at local zero which other surfaces will be solved from.")]
        [SerializeField] private Surface rootSurface = null;
        #endregion

        private Matrix4x4 lastTransformMatrix;

        private void Update()
        {
            if (transform.worldToLocalMatrix != lastTransformMatrix)
            {
                lastTransformMatrix = transform.worldToLocalMatrix;
                SolveStitchingConstraints();
            }
        }

        private List<Vector3[]> StitchArrowPolylines;

        private void OnDrawGizmos()
        {
            if (StitchArrowPolylines != null)
            {
                Gizmos.color = Color.yellow;
                foreach (Vector3[] polyline in StitchArrowPolylines)
                    for (int i = 1; i < polyline.Length; i++)
                        Gizmos.DrawLine(polyline[i - 1], polyline[i]);
            }
        }

        /// <summary>
        /// Starting from the root surface, resolves the transform
        /// layout of all surfaces in the scene. Should be called
        /// when a change to a surface might change how the surface
        /// layout is solved.
        /// </summary>
        public void SolveStitchingConstraints()
        {
            StitchArrowPolylines = new List<Vector3[]>();

            // Track which surfaces are solved and are thus
            // locked in place. The root surface is solved
            // by default as it does not depend on anything else.
            List<Surface> solvedSurfaces = new List<Surface>();
            solvedSurfaces.Add(rootSurface);
            // Use a recursive function to traverse the graph
            // of linked surfaces and apply transfrom constraints
            // so that the designer specified seams line up.
            if (rootSurface != null)
                if (rootSurface.surfaceLinks != null)
                    foreach (StitchingConstraint link in rootSurface.surfaceLinks)
                        SolveRestraint(rootSurface, link);
            void SolveRestraint(Surface from, StitchingConstraint restraint)
            {
                // Avoid an overflow if the designer links
                // a surface to itself.
                if (from == restraint.other)
                    return;
                // Ensure that the designer has properly filled in the
                // details for this constraint.
                if (restraint.other != null)
                {
                    Surface to = restraint.other;
                    if (solvedSurfaces.Contains(to))
                    {

                    }

                    // Zero out the transform that we will snap, so
                    // that we don't have to deal with offsets.
                    to.SetTransform(Vector3.zero, Quaternion.identity);
                    // Get the tile weld locations on the x axis.
                    // If the value is negative it is wrapped around.
                    float fromTile = restraint.fromTileOfThisSurface;
                    float toTile = restraint.toTileOfOtherSurface;
                    float addedAngle = 0f;
                    switch (restraint.linkDirection)
                    {
                        case StitchingConstraint.Direction.Right:
                            break;
                        case StitchingConstraint.Direction.Left:
                            fromTile -= 1;
                            toTile += 1;
                            break;
                        case StitchingConstraint.Direction.DoorwayEntrance:
                            fromTile -= 0.5f;
                            addedAngle = -90.0f;
                            break;
                        case StitchingConstraint.Direction.DoorwayExit:
                            toTile += 0.5f;
                            addedAngle = -90.0f;
                            break;
                    }
                    if (fromTile < 0f)
                        fromTile += from.LengthX + 1;
                    if (toTile < 0f)
                        toTile += to.LengthX + 1;

                    // Solve the constraint by first aligning rotation,
                    // then accounting for the offset.
                    Vector2 fromSamplePoint = Vector2.right * fromTile;
                    Vector2 toSamplePoint = Vector2.right * (toTile - 1);
                    // Get the rotation difference between the two
                    // surfaces and use them to solve the rotation.
                    // By default the surfaces are aligned based on
                    // their tangent, designers can add an additional
                    // angle which is applied here using the angle axis.
                    Vector3 fromUp = from.GetUp(fromSamplePoint);
                    Vector3 fromOut = from.GetOutwards(fromSamplePoint);
                    Vector3 toOut = to.GetOutwards(toSamplePoint);
                    to.SetTransform(Vector3.zero,
                        Quaternion.FromToRotation(toOut, fromOut)
                        * Quaternion.AngleAxis(restraint.angle + addedAngle, fromUp));
                    // Account for the offset to weld the surfaces together.
                    Vector3 fromLocation = from.GetLocation(fromSamplePoint)
                        + fromUp * restraint.yStep;
                    Vector3 toLocation = to.GetLocation(toSamplePoint);

                    Vector3 location = fromLocation - toLocation;
                    if (restraint.linkDirection is StitchingConstraint.Direction.DoorwayEntrance)
                    {
                        location += fromOut * 0.5f;
                    }
                    else if (restraint.linkDirection is StitchingConstraint.Direction.DoorwayExit)
                    {
                        location += from.GetRight(fromSamplePoint) * 0.5f;
                    }
                    to.SetTransform(
                        location,
                        to.transform.rotation);




                    // Is the other surface already locked by another
                    // previously solved constraint? If so we should
                    // log an error if the seam does not already
                    // naturally line up.
                    if (solvedSurfaces.Contains(to))
                    {

                    }
                    else
                    {
                        //to.SetTransform(solvedPosition, solvedRotation);
                        solvedSurfaces.Add(to);
                        if (to.surfaceLinks != null)
                            foreach (StitchingConstraint link in to.surfaceLinks)
                                SolveRestraint(to, link);
                    }
                }
            }
            // Recalculate the positioning for all
            // grid actors so that they don't appear
            // skewed if their grid has moved relative
            // to them.
            foreach (GridActor actor in transform.GetComponentsInChildren<GridActor>())
                actor.RefreshPosition();
        }

        /// <summary>
        /// Iterates through surfaces and validates their constraint
        /// values if they are linked to this updated surface.
        /// </summary>
        /// <param name="changedSurface">The surface that was changed.</param>
        public void ValidateLinkedConstraints(Surface changedSurface)
        {
            // Scan through all child surfaces under the grid world.
            Surface[] surfaces = gameObject.GetComponentsInChildren<Surface>();
            foreach (Surface surface in surfaces)
            {
                if (surface.surfaceLinks != null)
                {
                    foreach (StitchingConstraint constraint in surface.surfaceLinks)
                    {
                        if (constraint.other == changedSurface)
                        {
                            // If the surface has a constraint that is linked
                            // to the changed surface, force it to revalidate
                            // the ranges on its links.
                            surface.ValidateConstraints();
                            break;
                        }
                    }
                }
            }
            // Finally revalidate the calling surface.
            changedSurface.ValidateConstraints();
        }

    }
}
