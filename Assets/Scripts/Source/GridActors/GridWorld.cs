using System.Collections.Generic;
using UnityEngine;
using BattleRoyalRhythm.Surfaces;

namespace BattleRoyalRhythm.GridActors
{
    /// <summary>
    /// Holds a collection of surfaces and actors that move on those
    /// surfaces. Actors can see other actors in the same Grid World.
    /// </summary>
    public sealed class GridWorld : MonoBehaviour
    {

        #region Inspector Fields
        [Tooltip("The first surface centered at local zero which other surfaces will be solved from.")]
        [SerializeField] private Surface rootSurface = null;
        #endregion


#if UNITY_EDITOR
        private List<Vector3[]> stitchArrowPolylines;

        private void OnDrawGizmos()
        {
            // Draw the stitching arrows if they have
            // been generated.
            if (stitchArrowPolylines != null)
            {
                Gizmos.color = Color.green;
                foreach (Vector3[] polyline in stitchArrowPolylines)
                    for (int i = 1; i < polyline.Length; i++)
                        Gizmos.DrawLine(polyline[i - 1], polyline[i]);
            }
        }
#endif

        /// <summary>
        /// Starting from the root surface, resolves the transform
        /// layout of all surfaces in the scene. Should be called
        /// when a change to a surface might change how the surface
        /// layout is solved.
        /// </summary>
        public void SolveStitchingConstraints()
        {
#if UNITY_EDITOR
            // Reset the scene polylines. These are generated
            // as each stitch is completed and cached until
            // the next validation call.
            stitchArrowPolylines = new List<Vector3[]>();
#endif
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
                        ProcessConstraint(rootSurface, link.other, link);
            // NOTE this is a long ass function and certainly
            // poor practice. Tried to organize it as best as
            // possible- as I believe extracted functions would
            // just clutter the scope.
            void ProcessConstraint(Surface from, Surface to, StitchingConstraint constraint)
            {
                #region Invalid Cases Check
                // Break the recursive function if this surface links
                // to itself (stack overflow), or if the specified
                // surface is null (not yet specified).
                if (constraint.other == null || from == constraint.other)
                    return;
                #endregion
                #region Circular Constraint Setup
                // Grab the initial state of the transform
                // to solve so we can check if it matches previously
                // solved constraints in a circular loop.
                Vector3 expectedPosition = to.transform.position;
                Quaternion expectedRotation = to.transform.rotation;
                #endregion
                #region Direction Conditions Setup
                // Get the sampling points for merging these two
                // surfaces together, as well as an angle offset and
                // added translation for the link type.
                Vector2 fromSample = Vector2.right * constraint.fromTileOfThisSurface;
                Vector2 toSample = Vector2.right * constraint.toTileOfOtherSurface;
                if (fromSample.x < 0f)
                    fromSample.x += from.LengthX + 1;
                if (toSample.x < 0f)
                    toSample.x += to.LengthX + 1;
                // Store more info for offsets caused by doorways
                // that consist of intersecting surfaces.
                float addedAngle = 0f;
                Vector3 offsetTranslation = Vector3.zero;
                switch (constraint.linkDirection)
                {
                    case StitchingConstraint.Direction.Left:
                        fromSample.x -= 1.0f;
                        break;
                    case StitchingConstraint.Direction.Right:
                        toSample.x -= 1.0f;
                        break;
                    case StitchingConstraint.Direction.ExitLeft:
                        fromSample.x -= 1.0f;
                        toSample.x -= 0.5f;
                        addedAngle = 90.0f;
                        offsetTranslation = -from.GetRight(fromSample) * 0.5f;
                        break;
                    case StitchingConstraint.Direction.ExitRight:
                        toSample.x -= 0.5f;
                        addedAngle = -90.0f;
                        offsetTranslation = from.GetRight(fromSample) * 0.5f;
                        break;
                    case StitchingConstraint.Direction.EntranceLeftFacing:
                        fromSample.x -= 0.5f;
                        addedAngle = 90.0f;
                        offsetTranslation = from.GetOutwards(fromSample) * 0.5f;
                        break;
                    case StitchingConstraint.Direction.EntranceRightFacing:
                        fromSample.x -= 0.5f;
                        toSample.x -= 1.0f;
                        addedAngle = -90.0f;
                        offsetTranslation = from.GetOutwards(fromSample) * 0.5f;
                        break;
                }
                #endregion
                #region Solve Constraint Transform
                // Zero out the transform that we will snap, so
                // that we don't have to deal with offsets.
                to.SetTransform(Vector3.zero, Quaternion.identity);
                // Get the rotation difference between the two
                // surfaces and use them to solve the rotation.
                // By default the surfaces are aligned based on
                // their tangent, designers can add an additional
                // angle which is applied here using the angle axis.
                Vector3 fromUp = from.GetUp(fromSample);
                Vector3 fromOut = from.GetOutwards(fromSample);
                Vector3 toOut = to.GetOutwards(toSample);
                to.SetTransform(Vector3.zero,
                    Quaternion.FromToRotation(toOut, fromOut)
                    * Quaternion.AngleAxis(constraint.angle + addedAngle, fromUp));
                // Now that the surface is oriented use it's
                // location function to properly weld the two
                // seams together.
                to.SetTransform(
                    from.GetLocation(fromSample) - to.GetLocation(toSample)
                        + fromUp * constraint.yStep + offsetTranslation,
                    to.transform.rotation);
                #endregion
                #region Circular Constraint Check
                // If this surface has already been locked
                // by another constraint, compare the outcome
                // of the two constraints.
                if (solvedSurfaces.Contains(to))
                {
                    // Post a message if the result of this constraint
                    // fights the previous constraint's lock on the
                    // to surface.
                    if (Vector3.SqrMagnitude(to.transform.position - expectedPosition) > 0.1f
                        || Quaternion.Angle(to.transform.rotation, expectedRotation) > 1.0f)
                    {
                        Debug.Log(
                            $"Constraint from {from.gameObject.name} to {to.gameObject.name} " +
                            $"was ignored because another constraint already defines {to.gameObject.name}. " +
                            $"The link will still work, but there is a considerable gap between the surfaces.");
                    }
                    // Override to the previous constraint value.
                    to.SetTransform(expectedPosition, expectedRotation);
                }
                else
                {
                    // Otherwise continue traversing the
                    // constraint tree.
                    solvedSurfaces.Add(to);
                    if (to.surfaceLinks != null)
                        foreach (StitchingConstraint link in to.surfaceLinks)
                            ProcessConstraint(to, link.other, link);
                }
                #endregion
#if UNITY_EDITOR
                #region Generate Gizmo Arrows
                float ARROW_SIZE = 0.25f;
                // Get the range to draw the arrows in,
                // accounting for the y shift.
                int yMin = Mathf.Max(0, constraint.yStep);
                int yMax = Mathf.Min(from.LengthY, to.LengthY + constraint.yStep);
                // TODO this section is quite a mess; not sure of an
                // easy way to cram this code down- it is hard to generalize.
                for (int y = yMin; y < yMax; y++)
                {
                    float fromY = y + 0.5f;
                    float toY = fromY - constraint.yStep;
                    Vector3[] line;
                    Vector2 tileFrom, tileTo;
                    switch (constraint.linkDirection)
                    {
                        case StitchingConstraint.Direction.Left:
                            // Draw the linking segment.
                            line = new Vector3[3];
                            tileFrom = fromSample + new Vector2(0.5f, fromY);
                            tileTo = toSample + new Vector2(-0.5f, toY);
                            line[0] = from.GetLocation(tileFrom);
                            line[1] = from.GetLocation(fromSample + new Vector2(0f, fromY));
                            line[2] = to.GetLocation(tileTo);
                            stitchArrowPolylines.Add(line);
                            // Draw the left arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyToOther)
                            {
                                Vector3[] leftArrow = new Vector3[3];
                                Vector3 up = from.GetUp(tileFrom);
                                Vector3 right = from.GetRight(tileFrom);
                                leftArrow[0] = line[0] + ARROW_SIZE * (up - right);
                                leftArrow[1] = line[0];
                                leftArrow[2] = line[0] + ARROW_SIZE * (-right - up);
                                stitchArrowPolylines.Add(leftArrow);
                            }
                            // Draw the right arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyFromOther)
                            {
                                Vector3[] rightArrow = new Vector3[3];
                                Vector3 up = to.GetUp(tileTo);
                                Vector3 right = to.GetRight(tileTo);
                                rightArrow[0] = line[2] + ARROW_SIZE * (up + right);
                                rightArrow[1] = line[2];
                                rightArrow[2] = line[2] + ARROW_SIZE * (-up + right);
                                stitchArrowPolylines.Add(rightArrow);
                            }
                            break;
                        case StitchingConstraint.Direction.Right:
                            // Draw the linking segment.
                            line = new Vector3[3];
                            tileFrom = fromSample + new Vector2(-0.5f, fromY);
                            tileTo = toSample + new Vector2(0.5f, toY);
                            line[0] = from.GetLocation(tileFrom);
                            line[1] = from.GetLocation(fromSample + new Vector2(0f, fromY));
                            line[2] = to.GetLocation(tileTo);
                            stitchArrowPolylines.Add(line);
                            // Draw the left arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyToOther)
                            {
                                Vector3[] leftArrow = new Vector3[3];
                                Vector3 up = from.GetUp(tileFrom);
                                Vector3 right = from.GetRight(tileFrom);
                                leftArrow[0] = line[0] + ARROW_SIZE * (right + up);
                                leftArrow[1] = line[0];
                                leftArrow[2] = line[0] + ARROW_SIZE * (right - up);
                                stitchArrowPolylines.Add(leftArrow);
                            }
                            // Draw the right arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyFromOther)
                            {
                                Vector3[] rightArrow = new Vector3[3];
                                Vector3 up = to.GetUp(tileTo);
                                Vector3 right = to.GetRight(tileTo);
                                rightArrow[0] = line[2] + ARROW_SIZE * (up - right);
                                rightArrow[1] = line[2];
                                rightArrow[2] = line[2] + ARROW_SIZE * (-up - right);
                                stitchArrowPolylines.Add(rightArrow);
                            }
                            break;
                        case StitchingConstraint.Direction.ExitLeft:
                            // Draw the linking segment.
                            line = new Vector3[2];
                            tileFrom = fromSample + new Vector2(0.5f, fromY);
                            tileTo = toSample + new Vector2(0f, toY);
                            line[0] = from.GetLocation(tileFrom);
                            line[1] = to.GetLocation(tileTo);
                            stitchArrowPolylines.Add(line);
                            // Draw the left arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyToOther)
                            {
                                Vector3[] leftArrow = new Vector3[3];
                                Vector3 up = from.GetUp(tileFrom);
                                Vector3 right = from.GetRight(tileFrom);
                                leftArrow[0] = line[0] + ARROW_SIZE * (up - right);
                                leftArrow[1] = line[0];
                                leftArrow[2] = line[0] + ARROW_SIZE * (-right - up);
                                stitchArrowPolylines.Add(leftArrow);
                            }
                            // Draw the right arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyFromOther)
                            {
                                Vector3[] rightArrow = new Vector3[5];
                                Vector3 up = to.GetUp(tileTo);
                                Vector3 right = to.GetRight(tileTo);
                                rightArrow[0] = line[1] + ARROW_SIZE * up;
                                rightArrow[1] = line[1] + ARROW_SIZE * right;
                                rightArrow[2] = line[1] - ARROW_SIZE * up;
                                rightArrow[3] = line[1] - ARROW_SIZE * right;
                                rightArrow[4] = rightArrow[0];
                                stitchArrowPolylines.Add(rightArrow);
                            }
                            break;
                        case StitchingConstraint.Direction.ExitRight:
                            // Draw the linking segment.
                            line = new Vector3[2];
                            tileFrom = fromSample + new Vector2(-0.5f, fromY);
                            tileTo = toSample + new Vector2(0f, toY);
                            line[0] = from.GetLocation(tileFrom);
                            line[1] = to.GetLocation(tileTo);
                            stitchArrowPolylines.Add(line);
                            // Draw the left arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyToOther)
                            {
                                Vector3[] leftArrow = new Vector3[3];
                                Vector3 up = from.GetUp(tileFrom);
                                Vector3 right = from.GetRight(tileFrom);
                                leftArrow[0] = line[0] + ARROW_SIZE * (up + right);
                                leftArrow[1] = line[0];
                                leftArrow[2] = line[0] + ARROW_SIZE * (right - up);
                                stitchArrowPolylines.Add(leftArrow);
                            }
                            // Draw the right arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyFromOther)
                            {
                                Vector3[] rightArrow = new Vector3[5];
                                Vector3 up = to.GetUp(tileTo);
                                Vector3 right = to.GetRight(tileTo);
                                rightArrow[0] = line[1] + ARROW_SIZE * up;
                                rightArrow[1] = line[1] + ARROW_SIZE * right;
                                rightArrow[2] = line[1] - ARROW_SIZE * up;
                                rightArrow[3] = line[1] - ARROW_SIZE * right;
                                rightArrow[4] = rightArrow[0];
                                stitchArrowPolylines.Add(rightArrow);
                            }
                            break;
                        case StitchingConstraint.Direction.EntranceLeftFacing:
                            // Draw the linking segment.
                            line = new Vector3[2];
                            tileFrom = fromSample + new Vector2(0f, fromY);
                            tileTo = toSample + new Vector2(-0.5f, toY);
                            line[0] = from.GetLocation(tileFrom);
                            line[1] = to.GetLocation(tileTo);
                            stitchArrowPolylines.Add(line);
                            // Draw the left arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyToOther)
                            {
                                Vector3[] leftArrow = new Vector3[5];
                                Vector3 up = from.GetUp(tileFrom);
                                Vector3 right = from.GetRight(tileFrom);
                                leftArrow[0] = line[0] + ARROW_SIZE * up;
                                leftArrow[1] = line[0] + ARROW_SIZE * right;
                                leftArrow[2] = line[0] - ARROW_SIZE * up;
                                leftArrow[3] = line[0] - ARROW_SIZE * right;
                                leftArrow[4] = leftArrow[0];
                                stitchArrowPolylines.Add(leftArrow);
                            }
                            // Draw the right arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyFromOther)
                            {
                                Vector3[] rightArrow = new Vector3[3];
                                Vector3 up = to.GetUp(tileTo);
                                Vector3 right = to.GetRight(tileTo);
                                rightArrow[0] = line[1] + ARROW_SIZE * (up + right);
                                rightArrow[1] = line[1];
                                rightArrow[2] = line[1] + ARROW_SIZE * (right - up);
                                stitchArrowPolylines.Add(rightArrow);
                            }
                            break;
                        case StitchingConstraint.Direction.EntranceRightFacing:
                            // Draw the linking segment.
                            line = new Vector3[2];
                            tileFrom = fromSample + new Vector2(0f, fromY);
                            tileTo = toSample + new Vector2(0.5f, toY);
                            line[0] = from.GetLocation(tileFrom);
                            line[1] = to.GetLocation(tileTo);
                            stitchArrowPolylines.Add(line);
                            // Draw the left arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyToOther)
                            {
                                Vector3[] leftArrow = new Vector3[5];
                                Vector3 up = from.GetUp(tileFrom);
                                Vector3 right = from.GetRight(tileFrom);
                                leftArrow[0] = line[0] + ARROW_SIZE * up;
                                leftArrow[1] = line[0] + ARROW_SIZE * right;
                                leftArrow[2] = line[0] - ARROW_SIZE * up;
                                leftArrow[3] = line[0] - ARROW_SIZE * right;
                                leftArrow[4] = leftArrow[0];
                                stitchArrowPolylines.Add(leftArrow);
                            }
                            // Draw the right arrow if needed.
                            if (constraint.actorTraversal != StitchingConstraint.Traversal.OnlyFromOther)
                            {
                                Vector3[] rightArrow = new Vector3[3];
                                Vector3 up = to.GetUp(tileTo);
                                Vector3 right = to.GetRight(tileTo);
                                rightArrow[0] = line[1] + ARROW_SIZE * (up - right);
                                rightArrow[1] = line[1];
                                rightArrow[2] = line[1] + ARROW_SIZE * (-right - up);
                                stitchArrowPolylines.Add(rightArrow);
                            }
                            break;
                    }
                }
                #endregion
#endif
            }
            // Finally update the position and orientation of
            // any grid actors in the editor, to match the new
            // solved layout.
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
