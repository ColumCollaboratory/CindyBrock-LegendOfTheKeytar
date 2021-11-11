using BattleRoyalRhythm.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.GridActors.Hazards
{
    [RequireComponent(typeof(LineRenderer))]
    /// <summary>
    /// A linear hazard that deals damage to actors.
    /// </summary>
    public sealed class LaserHazard : GridActor
    {
        private enum LaserDirection : byte
        {
            Horizontal,
            Vertical
        }

        [SerializeField] private BeatService beatService = null;

        [Header("Laser Positioning")]
        [SerializeField] private LaserDirection direction = LaserDirection.Horizontal;
        // TODO this should be inferred.
        [SerializeField] private int laserLength = 3;

        [Header("Laser Behaviour")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private int beatsOn = 1;
        [SerializeField] private int beatsOff = 2;

        private bool isOn;
        private int beatIndex;
        private LineRenderer renderer;
        private Vector2Int span;

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            // Draw the laser. TODO needs to account for curvature.
            if (CurrentSurface != null)
            {
                Vector2 begin;
                Vector2 offset;
                if (direction is LaserDirection.Horizontal)
                {
                    begin = Location + new Vector2(-1f, -0.5f);
                    offset = Vector2.right * laserLength;
                }
                else
                {
                    begin = Location + new Vector2(-0.5f, -1f);
                    offset = Vector2.up * laserLength;
                }
                Vector3 start = CurrentSurface.GetLocation(begin);
                Vector3 end = CurrentSurface.GetLocation(begin + offset);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(start, end);
            }
        }
#endif


        // Start is called before the first frame update
        private void Start()
        {
            // Get the line renderer.
            renderer = gameObject.GetComponent<LineRenderer>();
            // Set the line renderer to the range.
            // TODO make this DRY
            Vector2 begin;
            Vector2 offset;
            if (direction is LaserDirection.Horizontal)
            {
                begin = Location + new Vector2(-1f, -0.5f);
                offset = Vector2.right * laserLength;
                span = Vector2Int.right * laserLength;
            }
            else
            {
                begin = Location + new Vector2(-0.5f, -1f);
                offset = Vector2.up * laserLength;
                span = Vector2Int.up * laserLength;
            }
            Vector3 start = CurrentSurface.GetLocation(begin);
            Vector3 end = CurrentSurface.GetLocation(begin + offset);
            renderer.SetPositions(new Vector3[] { start, end });

            // Subscribe to the beat service.
            beatService.BeatElapsed += OnBeatElapsed;
            isOn = true;
            beatIndex = 0;
        }

        private void OnBeatElapsed(float beatTime)
        {
            beatIndex++;
            if (isOn && beatIndex >= beatsOn)
            {
                beatIndex = 0;
                isOn = false;
                renderer.enabled = false;
            }
            if (!isOn && beatIndex >= beatsOff)
            {
                beatIndex = 0;
                isOn = true;
                renderer.enabled = true;
            }

            // Check for damageable actors inside.
            if (isOn)
            {
                List<GridActor> intersectingActors =
                    World.GetIntersectingActors(CurrentSurface,
                    Tile.x, Tile.y, Tile.x + span.x, Tile.y + span.y);
                foreach (GridActor actor in intersectingActors)
                    if (actor is IDamageable damageable)
                        damageable.ApplyDamage(damage);
            }
        }
    }
}
