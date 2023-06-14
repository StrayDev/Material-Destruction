
// System
using System;
using System.Collections;
using System.Collections.Generic;

// Unity 
using UnityEngine;

//
public class Slicer : MonoBehaviour
{
    // References
    [SerializeField] private Transform plane_transform;

    // Components
    private LineRenderer _lineRenderer;
    private Camera _camera;

    // Members
    private Vector3 line_start = default;
    private Vector3 line_end = default;

    private void Start()
    {
        if (!TryGetComponent(out _lineRenderer))
        {
            Debug.LogWarning($"Adding missing Line Renderer to : {gameObject.name}");

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
        }

        _camera = Camera.main;
    }

    private void Update()
    {
        var m_pos = Input.mousePosition;
        m_pos.z = 1;

        if (Input.GetMouseButtonDown(0)) line_start = _camera.ScreenToWorldPoint(m_pos);
        if (Input.GetMouseButton(0)) line_end = _camera.ScreenToWorldPoint(m_pos);

        if (Input.GetMouseButtonUp(0)) OnRelease();

        _lineRenderer.SetPosition(0, line_start);
        _lineRenderer.SetPosition(1, line_end);
    }

    private void OnRelease()
    {
        var world_plane = new Plane(line_start, line_end, line_end + _camera.transform.forward * 500);
        var center = _camera.transform.position / 3;

        var plane = ConvertToObjectToWorld(world_plane, _camera.transform);

        plane_transform.up = world_plane.normal;
        plane_transform.position = center;

        // I know...
        var objects = FindObjectsOfType<GameObject>();

        // for each object check 
        foreach (var obj in objects)
        {
            if (obj.CompareTag("Ground")) continue;
            
            // check that we have the correct components
            if (!TryGetMeshComponents(obj, out var filter, out var renderer)) continue;

            // 
            var local_plane = ConvertToWorldToLocal(plane, obj.transform);

            // check plane is intersecting the mesh
            if (!MeshCutter.PlaneIntersectsMesh(local_plane, filter.mesh, obj.transform)) continue;

            // cut the mesh and out the 2 new meshes 
            MeshCutter.SplitMeshWithPlane(obj.transform, plane, filter.mesh, out var mesh1, out var mesh2);

            // use the new meshes to create new GameObjects
            var t1 = CreateCutGameObject(obj, mesh1);
            t1.position += world_plane.normal * .05f;
            var t2 = CreateCutGameObject(obj, mesh2);
            t2.position -= world_plane.normal * .05f;

            Destroy(obj.gameObject);
        }

        // hide line 
/*        line_start = Vector3.one * 999f;
        line_end = Vector3.one * 999f;*/
    }

    private Plane ConvertToObjectToWorld(Plane objectPlane, Transform objectTransform)
    {
        // Get the world-to-local matrix
        Matrix4x4 worldToLocalMatrix = objectTransform.localToWorldMatrix;

        // Transform the plane normal and distance from object to world space
        Vector3 worldNormal = worldToLocalMatrix.MultiplyVector(objectPlane.normal);
        float worldDistance = objectPlane.distance + Vector3.Dot(worldNormal, objectTransform.position);

        // Create the world plane
        Plane worldPlane = new Plane(worldNormal, worldDistance);

        return worldPlane;
    }

    private Plane ConvertToWorldToLocal(Plane worldPlane, Transform localTransform)
    {
        // Get the local-to-world matrix
        Matrix4x4 localToWorldMatrix = localTransform.worldToLocalMatrix;

        // Transform the plane normal and distance from world to local space
        Vector3 localNormal = localToWorldMatrix.MultiplyVector(worldPlane.normal);
        float localDistance = worldPlane.distance - Vector3.Dot(localNormal, localTransform.position);

        // Create the local plane
        Plane localPlane = new Plane(localNormal, localDistance);

        return localPlane;
    }

    private Vector3 GetMousePosition()
    {
        return _camera.ScreenToWorldPoint(Input.mousePosition);
    }

    private bool TryGetMeshComponents(GameObject target, out MeshFilter filter, out MeshRenderer renderer)
    {
        target.TryGetComponent(out filter);
        target.TryGetComponent(out renderer);
        return !(filter == null || renderer == null);
    }

    private Transform CreateCutGameObject(GameObject target, Mesh mesh)
    {
        var obj = new GameObject(target.name + " piece");

        obj.transform.position = target.transform.position;
        obj.transform.rotation = target.transform.rotation;

        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = target.GetComponent<MeshRenderer>().material;

       // obj.AddComponent<MeshCollider>().convex = true;
        //obj.AddComponent<Rigidbody>();

        return obj.transform;
    }

}
//
