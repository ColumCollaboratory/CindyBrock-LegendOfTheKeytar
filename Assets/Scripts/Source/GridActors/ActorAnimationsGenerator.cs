using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors
{
    #region Animation Delegates
    /// <summary>
    /// Provides a relative movement location based on an interpolant between 0-1.
    /// </summary>
    /// <param name="t">The interpolant in the animation.</param>
    /// <returns>A relative offset from the origin starting location.</returns>
    public delegate Vector2 ActorAnimationPath(float t);
    #endregion

    /// <summary>
    /// Contains utilities for generating animation paths for
    /// grid actors to follow over the course of a beat.
    /// </summary>
    public static class ActorAnimationsGenerator
    {
        #region Linear Path Generators
        /// <summary>
        /// Creates a linear walk path across tiles.
        /// </summary>
        /// <param name="moveX">The x distance to move.</param>
        /// <param name="moveY">The y distance to move.</param>
        /// <returns>An animation path.</returns>
        public static ActorAnimationPath CreateWalkPath(int moveX, int moveY = 0)
        {
            // Store the end location and lerp towards it.
            Vector2 end = new Vector2(moveX, moveY);
            return (float t) =>
            {
                // Lerp to the end position.
                return end * t;
            };
        }
        #endregion
        #region Parabolic Jump Path Generators
        /// <summary>
        /// Creates a jump path based on the given offsets that meets the jump height
        /// in the first path and finishes the jump in the second path.
        /// </summary>
        /// <param name="endX">The x offset of the jump.</param>
        /// <param name="endY">The y offset of the jump.</param>
        /// <param name="jumpHeight">The height of the jump apex.</param>
        /// <returns>A collection of two animation paths for jumping and falling.</returns>
        public static List<ActorAnimationPath> CreateJumpPaths(int endX, int endY, int jumpHeight)
        {
            // Redirect to in place jump if applicable.
            if (endX == 0 && endY == 0)
                return CreateJumpPaths(jumpHeight);
            // Precalculate variables for the curve.
            // The curve is generated from left to right and flipped
            // at call time based on whether x is negative.
            float midX = Mathf.Round(0.5f * Mathf.Abs(endX));
            // Calculate the coefficiencts for both segments of the arc.
            float coef1 = -jumpHeight / (midX * midX);
            float coef2 = (-jumpHeight + endY) / ((Mathf.Abs(endX) - midX) * (Mathf.Abs(endX) - midX));
            // Create the animation arc code.
            List<ActorAnimationPath> animations = new List<ActorAnimationPath>
            {
                (float t) =>
                {
                    float x = midX * t;
                    return new Vector2(x * (endX > 0 ? 1f : -1f),
                        coef1 * (x - midX) * (x - midX) + jumpHeight
                    );
                },
                (float t) =>
                {
                    float x = Mathf.Lerp(midX, Mathf.Abs(endX), t);
                    return new Vector2((x - midX) * (endX > 0 ? 1f : -1f),
                        coef2 * (x - midX) * (x - midX)
                    );
                }
            };
            return animations;
        }
        /// <summary>
        /// Creates a jump path based on the given height that meets the jump height
        /// in the first path and finishes the jump in the second path.
        /// </summary>
        /// <param name="jumpHeight">The height of the jump apex.</param>
        /// <returns>A collection of two animation paths for jumping and falling.</returns>
        public static List<ActorAnimationPath> CreateJumpPaths(int jumpHeight)
        {
            // Create the animation arc code.
            List<ActorAnimationPath> animations = new List<ActorAnimationPath>();
            // Left hand side of a scaled projectile motion arc.
            animations.Add((float t) =>
                new Vector2(0f,
                    jumpHeight - jumpHeight * (1f - t) * (1f - t)
                ));
            // Right hand side of a scaled projectile motion arc.
            animations.Add((float t) =>
                new Vector2(0f, -jumpHeight * t * t));
            return animations;
        }
        /// <summary>
        /// Creates an animation path that jumps up to a location.
        /// </summary>
        /// <param name="moveY">The number of tiles to jump up.</param>
        /// <returns>An animation path for jumping up.</returns>
        public static ActorAnimationPath CreateJumpUpPath(int moveY)
        {
            // Left hand side of the projectile motion equation.
            return (float t) =>
                new Vector2(0f,
                    moveY - moveY * (1f - t) * (1f - t)
                );
        }
        /// <summary>
        /// Creates an animation path that drops down to a location.
        /// </summary>
        /// <param name="moveY">The number of tiles to drop (should be negative).</param>
        /// <returns>An animation path for dropping down.</returns>
        public static ActorAnimationPath CreateDropDownPath(int moveY)
        {
            // Use gravity fall equation to create drop.
            return (float t) =>
                new Vector2(0f, moveY * t * t);
        }
        #endregion
        #region Ledge Interaction Path Generators
        /// <summary>
        /// Creates an animation path that pulls up onto a ledge.
        /// </summary>
        /// <param name="ledgeFacesRight">Whether to shift left or right when pulling up.</param>
        /// <param name="pullHeight">The number of tiles to pull up.</param>
        /// <returns>An animation path pulling up.</returns>
        public static ActorAnimationPath CreatePullUpPath(bool ledgeFacesRight, int pullHeight)
        {
            // Create the animation segments.
            Vector2 segment1 = Vector2.up * pullHeight;
            Vector2 segment2 = ledgeFacesRight ? Vector2.left : Vector2.right;
            // TODO this sub-interpolant part could be better.
            float pullUpSegment = pullHeight / (pullHeight + 1f);
            return (float t) =>
            {
                if (t == 1f)
                    return segment1 + segment2 * ((t - pullUpSegment) / (1f - pullUpSegment));
                else
                    return Vector2.zero;
                /*
                if (t < pullUpSegment)
                    return segment1 * (t / pullUpSegment);
                else
                    return segment1 + segment2 * ((t - pullUpSegment) / (1f - pullUpSegment));
                */
            };
        }
        /// <summary>
        /// Creates an animation path that hangs down from a ledge.
        /// </summary>
        /// <param name="ledgeFacesRight">Whether to shift left or right when pulling up.</param>
        /// <param name="hangHeight">The number of tiles to drop down.</param>
        /// <returns>An animation path hanging down.</returns>
        public static ActorAnimationPath CreateHangDownPath(bool ledgeFacesRight, int hangHeight)
        {
            // Create the animation segments.
            Vector2 segment1 = ledgeFacesRight ? Vector2.right : Vector2.left;
            Vector2 segment2 = Vector2.down * hangHeight;
            // TODO this sub-interpolant part could be better.
            float slideOutSegment = 1f - hangHeight / (hangHeight + 1f);
            return (float t) =>
            {
                if (t < slideOutSegment)
                    return segment1 * (t / slideOutSegment);
                else
                    return segment1 + segment2 * ((t - slideOutSegment) / (1f - slideOutSegment));
            };
        }
        #endregion
    }
}
