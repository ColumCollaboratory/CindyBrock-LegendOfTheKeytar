using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyalRhythm.Surfaces
{

    public sealed class FlatSurface : Surface
    {


        public override Mesh GetTileMesh()
        {
            // Generate the flat vertices.
            int index = 0;
            Vector3[] vertices = new Vector3[(LengthX + 1) * (LengthY + 1)];
            for (int y = 0; y <= LengthY; y++)
            {
                for (int x = 0; x <= LengthX; x++)
                {
                    vertices[index] = new Vector3(x, y);
                    index++;
                }
            }
            // Generate the triangles.
            index = 0;
            int[] triangles = new int[LengthX * LengthY * 6];
            for (int y = 0; y < LengthY; y++)
            {
                for (int x = 0; x < LengthX; x++)
                {
                    int a = y * (LengthX + 1) + x;
                    int b = a + 1;
                    int c = (y + 1) * (LengthX + 1) + x;
                    int d = c + 1;
                    triangles[index] = a;
                    triangles[index + 1] = d;
                    triangles[index + 2] = b;
                    triangles[index + 3] = d;
                    triangles[index + 4] = a;
                    triangles[index + 5] = c;
                    index += 6;
                }
            }
            // Generate the UVs.
            Vector2[] uvs = new Vector2[vertices.Length];
            float uvTileScale = 1f / UVUnit;
            for (index = 0; index < uvs.Length; index++)
                uvs[index] = vertices[index] * uvTileScale;

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            return mesh;
        }

        protected override Vector3[][] GetWireFramePolylines()
        {
            Vector3[][] wireframe = new Vector3[LengthX + LengthY + 2][];
            int wireIndex = 0;
            for (int x = 0; x < LengthX + 1; x++)
            {
                wireframe[wireIndex] = new Vector3[]
                {
                    new Vector3(x, 0f, 0f),
                    new Vector3(x, LengthY, 0f)
                };
                wireIndex++;
            }
            for (int y = 0; y < LengthY + 1; y++)
            {
                wireframe[wireIndex] = new Vector3[]
                {
                    new Vector3(0f, y, 0f),
                    new Vector3(LengthX, y, 0f)
                };
                wireIndex++;
            }
            return wireframe;
        }

        protected override bool TryLinecastLocal(Vector3 start, Vector3 end, out Vector2 hitLocation)
        {
            // Eliminate cases where the line does not
            // intersect the surface in the correct direction.
            if (start.z > 0f || end.z < 0f)
            {
                hitLocation = default;
                return false;
            }
            // Find the local intersection on the surface.
            Vector3 direction = (end - start).normalized;
            hitLocation = start + direction * (Mathf.Abs(start.z) / direction.z);
            return true;
        }

        protected override Vector3 GetLocationLocal(Vector2 surfaceLocation)
        {
            return surfaceLocation;
        }

        protected override Vector3 GetOutwardsLocal(Vector2 surfaceLocation)
        {
            return Vector3.forward;
        }

        protected override Vector3 GetUpLocal(Vector2 surfaceLocation)
        {
            return Vector3.up;
        }

        protected override Vector3 GetRightLocal(Vector2 surfaceLocation)
        {
            return Vector3.right;
        }
    }
}
