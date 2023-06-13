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

        // position used for side checks
        var w = target.transform.position;

        // Prealocate indices
        var i0 = 0;
        var i1 = 0;
        var i2 = 0;

        // Prealocate vertices
        Vector3 v0;
        Vector3 v1;
        Vector3 v2;

        // Preallocate intersecting points
        Vector3? p0;
        Vector3? p1;
        Vector3? p2;


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
            if (TriangleIntersectsPlane(v0 + w, v1 + w, v2 + w, plane))
            {
                // Calculate the intersection points between the triangle edges and the cutting plane
                CalculateTrianglePlaneIntersections(v0, v1, v2, plane, out p0, out p1, out p2);

                if(!p0.HasValue)
                {
                    var s1 = plane.GetSide(v2) ? md1 : md2;

                    s1.vertices.Add(v2);
                    s1.vertices.Add(p2.Value);
                    s1.vertices.Add(p1.Value);

                    s1.triangles.Add(s1.vertices.Count - 3);
                    s1.triangles.Add(s1.vertices.Count - 2);
                    s1.triangles.Add(s1.vertices.Count - 1);

                    var s2 = !plane.GetSide(v2) ? md1 : md2;

                    s2.vertices.Add(v0);
                    s2.vertices.Add(v1);
                    s2.vertices.Add(p2.Value);
                     
                    s2.triangles.Add(s2.vertices.Count - 3);
                    s2.triangles.Add(s2.vertices.Count - 2);
                    s2.triangles.Add(s2.vertices.Count - 1);
                     
                    s2.vertices.Add(v1);
                    s2.vertices.Add(p1.Value);
                    s2.vertices.Add(p2.Value);
                     
                    s2.triangles.Add(s2.vertices.Count - 3);
                    s2.triangles.Add(s2.vertices.Count - 2);
                    s2.triangles.Add(s2.vertices.Count - 1);
                }

                if (!p1.HasValue)
                {
                    var s1 = plane.GetSide(v0) ? md1 : md2;

                    s1.vertices.Add(v0);
                    s1.vertices.Add(p0.Value);
                    s1.vertices.Add(p2.Value);

                    s1.triangles.Add(s1.vertices.Count - 3);
                    s1.triangles.Add(s1.vertices.Count - 2);
                    s1.triangles.Add(s1.vertices.Count - 1);

                    var s2 = !plane.GetSide(v0) ? md1 : md2;

                    s2.vertices.Add(p0.Value);
                    s2.vertices.Add(v2);
                    s2.vertices.Add(p2.Value);

                    s2.triangles.Add(s2.vertices.Count - 3);
                    s2.triangles.Add(s2.vertices.Count - 2);
                    s2.triangles.Add(s2.vertices.Count - 1);

                    s2.vertices.Add(v2);
                    s2.vertices.Add(p0.Value);
                    s2.vertices.Add(v1);

                    s2.triangles.Add(s2.vertices.Count - 3);
                    s2.triangles.Add(s2.vertices.Count - 2);
                    s2.triangles.Add(s2.vertices.Count - 1);
                }

                continue;
            }

            // Triangle does not intersect add it to list based on if it is above or below the plane
            var side = plane.GetSide(v0 + target.position) ? md1 : md2;
                   
            side.vertices.Add(v0);
            side.vertices.Add(v1);
            side.vertices.Add(v2);

            side.triangles.Add(side.vertices.Count - 3);
            side.triangles.Add(side.vertices.Count - 2);
            side.triangles.Add(side.vertices.Count - 1);

        }

        // Set the vertices and triangles for mesh1 and mesh2
        mesh1 = new Mesh
        {
            vertices = md1.vertices.ToArray(),
            triangles = md1.triangles.ToArray(),
        };
        mesh1.RecalculateNormals();

        mesh2 = new Mesh
        {
            vertices = md2.vertices.ToArray(),
            triangles = md2.triangles.ToArray(),
        };
        mesh2.RecalculateNormals();
    }

    private static void CalculateTrianglePlaneIntersections(Vector3 v0, Vector3 v1, Vector3 v2, Plane plane, out Vector3? point0, out Vector3? point1, out Vector3? point2)
    {
        point0 = null;
        point1 = null;
        point2 = null;

        // Calculate the distances from each vertex to the plane
        var distance0 = plane.GetDistanceToPoint(v0);
        var distance1 = plane.GetDistanceToPoint(v1);
        var distance2 = plane.GetDistanceToPoint(v2);

        // Check if each vertex is on a different side of the plane
        var isIntersecting01 = (distance0 * distance1) < 0;
        var isIntersecting12 = (distance1 * distance2) < 0;
        var isIntersecting20 = (distance2 * distance0) < 0;

        // Calculate intersection points between the triangle edges and the cutting plane
        if (isIntersecting01)
        {
            point0 = CalculateIntersectionPoint(v0, v1, plane);
        }

        if (isIntersecting12)
        {
            point1 = CalculateIntersectionPoint(v1, v2, plane);
        }

        if (isIntersecting20)
        {
            point2 = CalculateIntersectionPoint(v2, v0, plane);
        }
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


    public static bool PlaneIntersectsMesh(Plane plane, Mesh mesh)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;

        for (var i = 0; i < triangles.Length; i += 3)
        {
            var vertexA = vertices[triangles[i]];
            var vertexB = vertices[triangles[i + 1]];
            var vertexC = vertices[triangles[i + 2]];

            if (TriangleIntersectsPlane(vertexA, vertexB, vertexC, plane))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TriangleIntersectsPlane(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC, Plane plane)
    {
        /*var distanceA = plane.GetDistanceToPoint(vertexA);
        var distanceB = plane.GetDistanceToPoint(vertexB);
        var distanceC = plane.GetDistanceToPoint(vertexC);

        // Check if the three vertices are on different sides of the plane
        var isIntersecting = (distanceA > 0 && distanceB < 0 && distanceC < 0) ||
                              (distanceA < 0 && distanceB > 0 && distanceC > 0);

        return isIntersecting;*/

        var a = plane.GetSide(vertexA);
        var b = plane.GetSide(vertexB);
        var c = plane.GetSide(vertexC);

        return a != b || c != a || c != b;
    }

}
