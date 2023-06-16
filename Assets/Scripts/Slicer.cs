
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

        if (Input.GetMouseButtonDown(0)) line_start = _camera.ScreenToViewportPoint(m_pos);
        if (Input.GetMouseButton(0)) line_end = _camera.ScreenToViewportPoint(m_pos);

        if (Input.GetMouseButtonUp(0)) OnRelease();

        _lineRenderer.SetPosition(0, _camera.ViewportPointToRay(line_start).GetPoint(_camera.nearClipPlane));
        _lineRenderer.SetPosition(1, _camera.ViewportPointToRay(line_end).GetPoint(_camera.nearClipPlane));
    }

    [SerializeField] private GameObject debug_cube;

    private void OnRelease()
    {
        // create a ray from the start and end points
        var start_ray = _camera.ViewportPointToRay(line_start);
        var end_ray = _camera.ViewportPointToRay(line_end);

        // get the start and end of the line aligned to the near clipping plane
        var start = start_ray.GetPoint(_camera.nearClipPlane);
        var end = end_ray.GetPoint(_camera.nearClipPlane);
        var depth = end_ray.direction.normalized;

        // Get the tangent of the plane
        var tangent = (end - start).normalized;

        // get the normal vector
        var normal = Vector3.Cross(depth, tangent);

        // create the plane
        var plane = new Plane();

        // I know...
        var objects = FindObjectsOfType<GameObject>();

        // for each object check 
        foreach (var obj in objects)
        {
            // filter tag
            if (obj.CompareTag("Ground")) continue;

            // transform the normal
            var transformed_normal = ((Vector3)(obj.transform.localToWorldMatrix.transpose * normal)).normalized;

            // set the plane in the objects local space
            plane.SetNormalAndPosition(transformed_normal, obj.transform.InverseTransformPoint(start));

            // check that we have the correct components
            if (!TryGetMeshComponents(obj, out var filter, out var renderer)) continue;

            // check plane is intersecting the mesh
            if (!MeshCutter.PlaneIntersectsMesh(plane, filter.mesh)) continue;

            // cut the mesh and out the 2 new meshes 
            MeshCutter.SplitMeshWithPlane(plane, filter.mesh, out var mesh1, out var mesh2);

            // use the new meshes to create new GameObjects
            var t1 = CreateCutGameObject(obj, mesh1);
            t1.position += plane.normal * .01f;
            var t2 = CreateCutGameObject(obj, mesh2);
            t2.position -= plane.normal * .01f;

            Destroy(obj.gameObject);
        }

        // hide line 
        line_start = Vector3.one * 999f;
        line_end = Vector3.one * 999f;
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

        obj.AddComponent<MeshCollider>().convex = true;
        obj.AddComponent<Rigidbody>();

        return obj.transform;
    }

}
//
