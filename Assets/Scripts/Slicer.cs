
// System
using System;
using System.Collections;
using System.Collections.Generic;

// Unity 
using UnityEngine;

//
public class Slicer : MonoBehaviour
{
    // 

    [SerializeField] private GameObject target;

    private Plane _plane;

    private void Start()
    {
        _plane = new Plane(Vector3.left, Vector3.zero);
    }

    private void Update()
    {
        var click = Input.GetMouseButtonDown(0);
        if (click) OnClick();
    }

    private void OnClick()
    {
        // check that we have the correct components
        if (!TryGetMeshComponents(target, out var filter, out var renderer)) return;

        // check plane is intersecting the mesh
        if (!MeshCutter.PlaneIntersectsMesh(_plane, filter.mesh, target.transform)) return;

        // cut the mesh and out the 2 new meshes 
        MeshCutter.SplitMeshWithPlane(target.transform, _plane, filter.mesh, out var mesh1, out var mesh2);

        // use the new meshes to create new GameObjects
        var t1 = CreateCutGameObject(target, mesh1);
        var t2 = CreateCutGameObject(target, mesh2);

        target.gameObject.SetActive(false); // todo DEstroy < < < < < < < 
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

        return obj.transform;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(55, 0, 0, .75f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(.001f, 2f, 2f));
        DrawPlane(Vector3.zero, _plane.normal);
    }

    private void DrawPlane(Vector3 position,Vector3 normal)
    {

        var v3 = new Vector3();

        if (normal.normalized != Vector3.forward)
            v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
        else
            v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude; ;

        var corner0 = position + v3;
        var corner2 = position - v3;

        var q = Quaternion.AngleAxis(90.0f, normal);
        v3 = q * v3;
        var corner1 = position + v3;
        var corner3 = position - v3;

        Debug.DrawLine(corner0, corner2, Color.green);
        Debug.DrawLine(corner1, corner3, Color.green);
        Debug.DrawLine(corner0, corner1, Color.green);
        Debug.DrawLine(corner1, corner2, Color.green);
        Debug.DrawLine(corner2, corner3, Color.green);
        Debug.DrawLine(corner3, corner0, Color.green);
        Debug.DrawRay(position, normal, Color.red);
    }
}
//
