using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

/// <summary>
/// Utility for drawing wire‑frame colliders at runtime.
/// Call these inside OnRenderObject() or via a custom renderer.
/// </summary>
public static class RuntimeColliderDrawer
{
    // A simple unlit material that works with GL.LINES.
    private static Material _lineMaterial;
    private static Material LineMaterial => _lineMaterial = _lineMaterial != null ? _lineMaterial : CreateLineMaterial();

    private static Material CreateLineMaterial()
    {
        // Use Unity's built‑in “Lines/Colored” shader (doesn’t need lighting).
        var mat = new Material(Shader.Find("Hidden/Internal-Colored"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
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

    #region Get Vertices Methods
    /* ------------------------------------------------------------------ */
    /* Capsule ---------------------------------------------------------- */
    /* ------------------------------------------------------------------ */
    /// <param name="center">World position of the capsule’s center.</param>
    /// <param name="rotation">
    /// Quaternion that defines the capsule’s orientation (its local + Y axis becomes the capsule axis).
    /// </param>
    /// <param name="radius">Radius of the capsule.</param>
    /// <param name="height">Total height of the capsule (including caps).</param>
    /// <param name="segments">
    /// Number of segments around each circle. Must be even for a clean hemispherical division.
    /// </param>
    /// <param name="verts">Out‑array containing all vertex positions.</param>
    /// <param name="edges">Out‑array containing index pairs that define lines.</param>
    public static void GetCapsuleVertices(
        Vector3 center,
        Quaternion rotation,
        float radius,
        float height,
        int segments,
        out Vector3[] verts,
        out (int, int)[] edges)
    {
        // Height of the cylindrical part (excluding hemispheres)
        float cylHeight = Mathf.Max(0f, height - 2f * radius);

        // Pre‑compute rotation that will be used to place rings
        Quaternion rot = rotation;

        var vertList = new List<Vector3>();
        var edgeList = new List<(int, int)>();

        /* ---------- Top & bottom ring (cylinder ends) ---------- */
        for (int i = 0; i <= segments; i++)
        {
            float theta = 2f * Mathf.PI * i / segments;
            Vector3 dir = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
            Vector3 offset = rot * dir * radius;

            // Top ring (index 0 … segments)
            vertList.Add(center + rot * (Vector3.up * (cylHeight * 0.5f)) + offset);

            // Bottom ring (index segments+1 … 2*segments+1)
            vertList.Add(center - rot * (Vector3.up * (cylHeight * 0.5f)) + offset);
        }

        /* ---------- Cylinder side lines ---------- */
        for (int i = 0; i < segments; i++)
        {
            edgeList.Add((i, i + 1));                     // top ring
            edgeList.Add((segments + i + 1, segments + i + 2));// bottom ring
            edgeList.Add((i, segments + i + 1));           // side edge
        }
        // close the two circles
        edgeList.Add((0, segments));
        edgeList.Add((segments, 2 * segments + 1));

        /* ---------- Helper to add a hemisphere ---------- */
        void AddHemisphere(bool upper)
        {
            int startIndex = vertList.Count;
            float sign = upper ? 1f : -1f;   // + for top, – for bottom

            // Build vertices
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
                        sign * cosTheta,
                        sinTheta * Mathf.Sin(phi)
                    );
                    vertList.Add(center
                                 + rot * (Vector3.up * (cylHeight * 0.5f) * sign)
                                 + rot * localPos * radius);
                }
            }

            int ringCount = segments + 1;

            // Edges inside the hemisphere
            for (int lat = 0; lat < segments / 2; lat++)
            {
                int startA = startIndex + lat * ringCount;
                int startB = startIndex + (lat + 1) * ringCount;
                for (int lon = 0; lon < ringCount - 1; lon++)
                {
                    edgeList.Add((startA + lon, startA + lon + 1));
                    edgeList.Add((startB + lon, startB + lon + 1));
                    edgeList.Add((startA + lon, startB + lon));
                }
            }

            // Connect the base ring of the cap to the cylinder ring
            int baseIndex = startIndex + (segments / 2) * ringCount;
            if (upper)
            {
                for (int i = 0; i <= segments; i++)
                    edgeList.Add((i, baseIndex + i)); // top ring → upper cap
            }
            else
            {
                for (int i = 0; i <= segments; i++)
                    edgeList.Add((segments + 1 + i, baseIndex + i)); // bottom ring → lower cap
            }
        }

        /* ---------- Upper & lower hemispheres ---------- */
        AddHemisphere(true);   // top
        AddHemisphere(false);  // bottom

        verts = vertList.ToArray();
        edges = edgeList.ToArray();
    }


    /* ------------------------------------------------------------------ */
    /* Box -------------------------------------------------------------- */
    /* ------------------------------------------------------------------ */
    public static void GetBoxVertices(
        Vector3 center,          // local center
        Quaternion rotation,     // orientation
        Vector3 halfExtents,     // size/2
        out Vector3[] verts,
        out (int, int)[] edges)
    {
        // ---- Vertices -------------------------------------------------
        List<Vector3> verticesList = new List<Vector3>(8);

        // 8 corners of a unit cube in local space (same order as your code)
        Vector3[] localCorners = {
        new Vector3(-1,-1,-1), new Vector3( 1,-1,-1),
        new Vector3(-1, 1,-1), new Vector3( 1, 1,-1),
        new Vector3(-1,-1, 1), new Vector3( 1,-1, 1),
        new Vector3(-1, 1, 1), new Vector3( 1, 1, 1)
    };

        foreach (var lc in localCorners)
        {
            // Scale by halfExtents, rotate and translate
            verticesList.Add(center + rotation * Vector3.Scale(lc, halfExtents));
        }

        // ---- Edges ----------------------------------------------------
        List<(int, int)> edgesList = new List<(int, int)>(12);

        // Bottom face (indices 0‑3)
        edgesList.Add((0, 1));
        edgesList.Add((1, 3));
        edgesList.Add((3, 2));
        edgesList.Add((2, 0));

        // Top face (indices 4‑7)
        edgesList.Add((4, 5));
        edgesList.Add((5, 7));
        edgesList.Add((7, 6));
        edgesList.Add((6, 4));

        // Vertical edges connecting bottom ↔ top
        edgesList.Add((0, 4));
        edgesList.Add((1, 5));
        edgesList.Add((2, 6));
        edgesList.Add((3, 7));

        verts = verticesList.ToArray();
        edges = edgesList.ToArray();
    }


    /* ------------------------------------------------------------------ */
    /* Sphere ----------------------------------------------------------- */
    /* ------------------------------------------------------------------ */
    public static void GetSphereVertices(
        Vector3 center,
        Quaternion rotation,     // optional – sphere is symmetric
        float radius,
        int segments,            // number of points per latitude circle (>= 2)
        out Vector3[] verts,
        out (int, int)[] edges)
    {
        // ---------- Vertices ----------
        List<Vector3> verticesList = new List<Vector3>((segments + 1) * segments);

        for (int lat = 0; lat <= segments; lat++)
        {
            float theta = Mathf.PI * lat / segments;          // polar angle

            for (int lon = 0; lon < segments; lon++)
            {
                float phi = 2f * Mathf.PI * lon / segments;   // azimuthal angle

                Vector3 local = new Vector3(
                    radius * Mathf.Sin(theta) * Mathf.Cos(phi),
                    radius * Mathf.Sin(theta) * Mathf.Sin(phi),
                    radius * Mathf.Cos(theta));

                verticesList.Add(center + rotation * local);      // world position
            }
        }

        // ---------- Edges ----------
        List<(int, int)> edgesList = new List<(int, int)>(segments * (2 * segments + 1));

        // Helper that gives the index of a vertex in the flat list.
        int Index(int lat, int lon) => lat * segments + lon;

        // Horizontal edges – wrap around each latitude ring
        for (int lat = 0; lat <= segments; lat++)
            for (int lon = 0; lon < segments; lon++)
                edgesList.Add((Index(lat, lon), Index(lat, (lon + 1) % segments)));

        // Vertical edges – connect adjacent latitude rings
        for (int lat = 0; lat < segments; lat++)
            for (int lon = 0; lon < segments; lon++)
                edgesList.Add((Index(lat, lon), Index(lat + 1, lon)));

        verts = verticesList.ToArray();
        edges = edgesList.ToArray();
    }

    #endregion

    #region Common Rendering Helper

    public static void RenderLines(Vector3[] verts, (int, int)[] edges, Color color)
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
