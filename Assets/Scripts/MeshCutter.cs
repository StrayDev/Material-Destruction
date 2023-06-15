using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCutter
{
    //

    public static void SplitMeshWithPlane(Transform target, Plane plane, Mesh mesh, out Mesh mesh1, out Mesh mesh2)
    {
        // allocate meshes 
        mesh1 = new Mesh();
        mesh2 = new Mesh();

        // New lists for each new mesh
        var md1 = new MeshData();
        var md2 = new MeshData();

        // Cache mesh data
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;

        // Prealocate indices
        var i0 = 0;
        var i1 = 0;
        var i2 = 0;

        // Prealocate vertices
        Vector3 v0;
        Vector3 v1;
        Vector3 v2;

        // Preallocate intersecting points
        Vector3 p0;
        Vector3 p1;
        Vector3 p2;

        // keep all of th eintersecting pairs to create the fill
        var intersection_pairs = new List<Vector3>();

        // 
        //Plane planeInObjectSpace = TransformPlaneToMatrix(plane, target.worldToLocalMatrix);

        // Iterate over each triangle in the mesh
        for (var i = 0; i < triangles.Length; i += 3)
        {
            // Retrieve the indices of the triangle vertices
            i0 = triangles[i];
            i1 = triangles[i + 1];
            i2 = triangles[i + 2];

            // Retrieve the vertices of the triangle using the indices
            v0 = vertices[i0];
            v1 = vertices[i1];
            v2 = vertices[i2];

            // Check if the triangle intersects the cutting plane
            if (TriangleIntersectsPlane(plane, v0, v1, v2))
            {
                // Calculate the intersection points between the triangle edges and the cutting plane
                CalculateTrianglePlaneIntersections(v0, v1, v2, plane, out p0, out p1, out p2);

                Vector3 intersect1 = default;
                Vector3 intersect2 = default;

                if (p0 == Vector3.zero)
                {
                    intersect1 = p2;
                    intersect2 = p1;
                }
                if (p1 == Vector3.zero)
                {
                    intersect1 = p0;
                    intersect2 = p2;
                }
                if (p2 == Vector3.zero)
                {
                    intersect1 = p1;
                    intersect2 = p0;
                }

                // store these for later
                intersection_pairs.Add(intersect1);
                intersection_pairs.Add(intersect2);

                // get sides 
                var v0_side = plane.GetSide(v0);
                var v1_side = plane.GetSide(v1);
                var v2_side = plane.GetSide(v2);

                Vector3 solo;
                Vector3 pair1;
                Vector3 pair2;

                if (v0_side != v1_side)
                {
                    if (v0_side != v2_side)
                    {
                        solo = v0;
                        pair1 = v1;
                        pair2 = v2;
                    }
                    else
                    {
                        solo = v1;
                        pair1 = v2;
                        pair2 = v0;
                    }
                }
                else
                {
                    solo = v2;
                    pair1 = v0;
                    pair2 = v1;
                }

                // create the solo triangle
                var side1 = plane.GetSide(solo) ? md1 : md2;

                side1.vertices.Add(solo);
                side1.vertices.Add(intersect1);
                side1.vertices.Add(intersect2);

                side1.triangles.Add(side1.vertices.Count - 3);
                side1.triangles.Add(side1.vertices.Count - 2);
                side1.triangles.Add(side1.vertices.Count - 1);

                // create the pair of triangles
                var side2 = plane.GetSide(pair1) ? md1 : md2;

                side2.vertices.Add(pair1);
                side2.vertices.Add(intersect2);
                side2.vertices.Add(intersect1);

                side2.triangles.Add(side2.vertices.Count - 3);
                side2.triangles.Add(side2.vertices.Count - 2);
                side2.triangles.Add(side2.vertices.Count - 1);

                side2.vertices.Add(pair2);
                side2.vertices.Add(intersect2);
                side2.vertices.Add(pair1);

                side2.triangles.Add(side2.vertices.Count - 3);
                side2.triangles.Add(side2.vertices.Count - 2);
                side2.triangles.Add(side2.vertices.Count - 1);

                continue;
            }

            // Triangle does not intersect add it to list based on if it is above or below the plane
            var centre = (v0 + v1 + v2) / 3;
            var side = plane.GetSide(centre) ? md1 : md2;

            side.vertices.Add(v0);
            side.vertices.Add(v1);
            side.vertices.Add(v2);

            side.triangles.Add(side.vertices.Count - 3);
            side.triangles.Add(side.vertices.Count - 2);
            side.triangles.Add(side.vertices.Count - 1);
        }

        // get the center point
        var center_point = Vector3.zero;
        foreach (var p in intersection_pairs)
        {
            center_point += p;
        }
        center_point /= intersection_pairs.Count;

        // fill missing area 
        for (int i = 0; i < intersection_pairs.Count / 2; i++)
        {
            var index = i * 2;
            var pair1 = intersection_pairs[index];
            var pair2 = intersection_pairs[index + 1];

            md1.vertices.Add(center_point);
            md1.vertices.Add(pair1);
            md1.vertices.Add(pair2);

            md1.triangles.Add(md1.vertices.Count - 3);
            md1.triangles.Add(md1.vertices.Count - 2);
            md1.triangles.Add(md1.vertices.Count - 1);

            md1.vertices.Add(pair1);
            md1.vertices.Add(center_point);
            md1.vertices.Add(pair2);

            md1.triangles.Add(md1.vertices.Count - 3);
            md1.triangles.Add(md1.vertices.Count - 2);
            md1.triangles.Add(md1.vertices.Count - 1);

            // 
            md2.vertices.Add(center_point);
            md2.vertices.Add(pair1);
            md2.vertices.Add(pair2);

            md2.triangles.Add(md2.vertices.Count - 3);
            md2.triangles.Add(md2.vertices.Count - 2);
            md2.triangles.Add(md2.vertices.Count - 1);

            md2.vertices.Add(pair1);
            md2.vertices.Add(center_point);
            md2.vertices.Add(pair2);

            md2.triangles.Add(md2.vertices.Count - 3);
            md2.triangles.Add(md2.vertices.Count - 2);
            md2.triangles.Add(md2.vertices.Count - 1);
        }

        // Set the vertices and triangles for mesh1 and mesh2
        mesh1 = new Mesh();
        mesh1.vertices = md1.vertices.ToArray();
        mesh1.triangles = md1.triangles.ToArray();
        mesh1.RecalculateNormals();

        mesh2 = new Mesh();
        mesh2.vertices = md2.vertices.ToArray();
        mesh2.triangles = md2.triangles.ToArray();
        mesh2.RecalculateNormals();
    }

    private static void CalculateTrianglePlaneIntersections(Vector3 v0, Vector3 v1, Vector3 v2, Plane plane, out Vector3 p0, out Vector3 p1, out Vector3 p2)
    {
        p0 = Vector3.zero;
        p1 = Vector3.zero;
        p2 = Vector3.zero;

        // Calculate the distances from each vertex to the plane
        var d0 = plane.GetDistanceToPoint(v0);
        var d1 = plane.GetDistanceToPoint(v1);
        var d2 = plane.GetDistanceToPoint(v2);

        // Check if each vertex is on a different side of the plane
        var result0 = (d0 * d1) < 0;
        var result1 = (d1 * d2) < 0;
        var result2 = (d2 * d0) < 0;

        // Calculate intersection points between the triangle edges and the cutting plane
        if (result0) p0 = CalculateIntersectionPoint(v0, v1, plane);
        if (result1) p1 = CalculateIntersectionPoint(v1, v2, plane);
        if (result2) p2 = CalculateIntersectionPoint(v2, v0, plane);
    }

    private static Vector3 CalculateIntersectionPoint(Vector3 vertex1, Vector3 vertex2, Plane plane)
    {
        Vector3 lineDirection = vertex2 - vertex1;
        float lineMagnitude = lineDirection.magnitude;
        Vector3 lineDirectionNormalized = lineDirection / lineMagnitude;

        float distance = plane.GetDistanceToPoint(vertex1);
        float dotProduct = Vector3.Dot(lineDirectionNormalized, plane.normal);

        float intersectionDistance = -distance / dotProduct;
        return vertex1 + lineDirectionNormalized * intersectionDistance;
    }


    public static bool PlaneIntersectsMesh(Plane plane, Mesh mesh, Transform target)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;

        for (var i = 0; i < triangles.Length; i += 3)
        {
            var v0 = vertices[triangles[i]];
            var v1 = vertices[triangles[i + 1]];
            var v2 = vertices[triangles[i + 2]];

            if (TriangleIntersectsPlane(plane, v0, v1, v2))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TriangleIntersectsPlane(Plane plane, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        // Check if any two vertices are on opposite sides of the plane
        bool intersects = plane.GetSide(v0) != plane.GetSide(v1) ||
                          plane.GetSide(v0) != plane.GetSide(v2) ||
                          plane.GetSide(v1) != plane.GetSide(v2);

        if (!intersects)
        {
            // Check for degenerate triangle (all vertices are collinear)
            if (Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), plane.normal) == 0f)
            {
                return false;
            }

            // Check for self-intersecting triangle
            bool intersectsEdges = TriangleIntersectsEdge(v0, v1, v2, plane) ||
                                   TriangleIntersectsEdge(v1, v2, v0, plane) ||
                                   TriangleIntersectsEdge(v2, v0, v1, plane);

            intersects = intersectsEdges;
        }

        return intersects;
    }

    private static bool TriangleIntersectsEdge(Vector3 v0, Vector3 v1, Vector3 v2, Plane plane)
    {
        // Check if the edge (v0-v1) intersects the plane
        return (plane.GetSide(v0) != plane.GetSide(v1)) && IsPointBetweenPlanes(v0, v1, v2, plane);
    }

    private static bool IsPointBetweenPlanes(Vector3 p0, Vector3 p1, Vector3 p2, Plane plane)
    {
        // Check if a point (p2) is between two planes defined by (p0-p1)
        return Vector3.Dot(plane.normal, p2 - p0) * Vector3.Dot(plane.normal, p2 - p1) <= 0f;
    }



}
