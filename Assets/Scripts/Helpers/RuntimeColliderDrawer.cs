using UnityEngine;

/// <summary>
/// Utility for drawing wire‑frame colliders at runtime.
/// Call these inside OnRenderObject() or via a custom renderer.
/// </summary>
public static class RuntimeColliderDrawer
{
    // A simple unlit material that works with GL.LINES.
    private static Material _lineMaterial;
    private static Material LineMaterial => _lineMaterial ??= CreateLineMaterial();

    private static Material CreateLineMaterial()
    {
        // Use Unity's built‑in “Lines/Colored” shader (doesn’t need lighting).
        var mat = new Material(Shader.Find("Hidden/Internal-Colored"));
        mat.hideFlags = HideFlags.HideAndDontSave;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mat.SetInt("_ZWrite", 0);
        return mat;
    }

    #region Box

    /// <summary>
    /// Draws a wireframe box.
    /// </summary>
    /// <param name="center">Center of the box in world space.</param>
    /// <param name="size">Size of the box (width, height, depth).</param>
    /// <param name="color">Line color.</param>
    public static void DrawBox(Vector3 center, Vector3 size, Color color)
    {
        // 8 corners of a unit cube
        var half = size * 0.5f;
        var verts = new[]
        {
            center + new Vector3(-half.x, -half.y, -half.z),
            center + new Vector3( half.x, -half.y, -half.z),
            center + new Vector3( half.x, -half.y,  half.z),
            center + new Vector3(-half.x, -half.y,  half.z),

            center + new Vector3(-half.x,  half.y, -half.z),
            center + new Vector3( half.x,  half.y, -half.z),
            center + new Vector3( half.x,  half.y,  half.z),
            center + new Vector3(-half.x,  half.y,  half.z)
        };

        var edges = new[]
        {
            // Bottom
            (0,1),(1,2),(2,3),(3,0),
            // Top
            (4,5),(5,6),(6,7),(7,4),
            // Sides
            (0,4),(1,5),(2,6),(3,7)
        };

        RenderLines(verts, edges, color);
    }

    #endregion

    #region Sphere

    /// <summary>
    /// Draws a wireframe sphere approximated with latitude/longitude lines.
    /// </summary>
    public static void DrawSphere(Vector3 center, float radius, Color color,
                                  int segments = 12)
    {
        var verts = new System.Collections.Generic.List<Vector3>();

        // Latitude circles (horizontal rings)
        for (int lat = 0; lat <= segments; lat++)
        {
            float theta = Mathf.PI * lat / segments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= segments; lon++)
            {
                float phi = 2f * Mathf.PI * lon / segments;
                float x = sinTheta * Mathf.Cos(phi);
                float y = cosTheta;
                float z = sinTheta * Mathf.Sin(phi);
                verts.Add(center + new Vector3(x, y, z) * radius);
            }
        }

        var edges = new System.Collections.Generic.List<(int, int)>();

        // Connect vertices in each latitude ring
        for (int lat = 0; lat < segments; lat++)
        {
            int start = lat * (segments + 1);
            for (int lon = 0; lon < segments; lon++)
            {
                edges.Add((start + lon, start + lon + 1));
            }
        }

        // Connect vertices between latitude rings
        for (int lat = 0; lat < segments - 1; lat++)
        {
            int startA = lat * (segments + 1);
            int startB = (lat + 1) * (segments + 1);
            for (int lon = 0; lon <= segments; lon++)
            {
                edges.Add((startA + lon, startB + lon));
            }
        }

        RenderLines(verts.ToArray(), edges.ToArray(), color);
    }

    #endregion

    #region Capsule

    /// <summary>
    /// Draws a complete capsule wireframe at runtime.
    /// </summary>
    public static void DrawCapsuleFull(Vector3 center, Vector3 axis,
                                       float radius, float height, Color color,
                                       int segments = 12)
    {
        // Clamp height so that the cylinder part is never negative.
        float halfHeight = Mathf.Max(0f, (height - 2f * radius) * 0.5f);
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, axis.normalized);

        // ----- Cylinder vertices ------------------------------------------------
        var vertsTop = new Vector3[segments + 1];
        var vertsBottom = new Vector3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float theta = 2f * Mathf.PI * i / segments;
            Vector3 dir = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
            Vector3 offset = rot * dir * radius;

            vertsTop[i] = center + axis.normalized * halfHeight + offset;
            vertsBottom[i] = center - axis.normalized * halfHeight + offset;
        }

        var verts = new System.Collections.Generic.List<Vector3>();
        verts.AddRange(vertsTop);
        verts.AddRange(vertsBottom);

        // ----- Cylinder side lines ---------------------------------------------
        var edges = new System.Collections.Generic.List<(int, int)>();
        for (int i = 0; i < segments; i++)
        {
            edges.Add((i, i + 1));                     // top ring
            edges.Add((segments + i, segments + i + 1));// bottom ring
            edges.Add((i, segments + i));               // side edge
        }
        edges.Add((0, segments));          // close top circle
        edges.Add((segments, 2 * segments)); // close bottom circle

        // ----- Upper hemisphere -----------------------------------------------
        AddHemisphere(center + axis.normalized * halfHeight, true);
        // ----- Lower hemisphere -----------------------------------------------
        AddHemisphere(center - axis.normalized * halfHeight, false);

        RenderLines(verts.ToArray(), edges.ToArray(), color);
        return;

        // ------------------------------------------------------------------------
        void AddHemisphere(Vector3 sphereCenter, bool upper)
        {
            int startIndex = verts.Count;
            for (int lat = 0; lat <= segments / 2; lat++)
            {
                float theta = Mathf.PI * lat / segments;          // 0 … π/2
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= segments; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / segments;
                    Vector3 localPos = new Vector3(
                        sinTheta * Mathf.Cos(phi),
                        upper ? cosTheta : -cosTheta,
                        sinTheta * Mathf.Sin(phi)
                    );
                    verts.Add(sphereCenter + rot * localPos * radius);
                }
            }

            int ringCount = segments + 1;
            for (int lat = 0; lat < segments / 2; lat++)
            {
                int startA = startIndex + lat * ringCount;
                int startB = startIndex + (lat + 1) * ringCount;
                for (int lon = 0; lon < ringCount - 1; lon++)
                {
                    edges.Add((startA + lon, startA + lon + 1));
                    edges.Add((startB + lon, startB + lon + 1));
                    edges.Add((startA + lon, startB + lon));
                }
            }
        }
    }



    #endregion

    #region Common Rendering Helper

    private static void RenderLines(Vector3[] verts, (int, int)[] edges, Color color)
    {
        if (verts == null || verts.Length == 0) return;

        LineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity); // world space
        GL.Begin(GL.LINES);
        GL.Color(color);

        foreach (var e in edges)
        {
            GL.Vertex(verts[e.Item1]);
            GL.Vertex(verts[e.Item2]);
        }

        GL.End();
        GL.PopMatrix();
    }
    #endregion
}
