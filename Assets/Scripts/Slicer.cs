
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
        if (!MeshCutter.PlaneIntersectsMesh(_plane, filter.mesh)) return;

        // cut the mesh and out the 2 new meshes 
        MeshCutter.SplitMeshWithPlane(target.transform, _plane, filter.mesh, out var mesh1, out var mesh2);

        // use the new meshes to create new GameObjects
        CreateCutGameObject(target, mesh1);
        CreateCutGameObject(target, mesh2);

        target.gameObject.SetActive(false); // todo DEstroy < < < < < < < 
    }

    private bool TryGetMeshComponents(GameObject target, out MeshFilter filter, out MeshRenderer renderer)
    {
        target.TryGetComponent(out filter);
        target.TryGetComponent(out renderer);
        return !(filter == null || renderer == null);
    }

    private void CreateCutGameObject(GameObject target, Mesh mesh)
    {
        var obj = new GameObject(target.name + " piece");
        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = target.GetComponent<MeshRenderer>().material;
    }

}
//
