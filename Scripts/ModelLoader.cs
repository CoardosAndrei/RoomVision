using UnityEngine;
using System.Threading.Tasks;
using GLTFast;
using System.Linq;

public class ModelLoader : MonoBehaviour
{
    public GameObject loadedObject;
    private Material futuristicMaterial;

    void Awake()
    {
        futuristicMaterial = Resources.Load<Material>("Materials/FuturisticMaterial");
        if (futuristicMaterial == null)
            Debug.LogWarning("[ModelLoader] FuturisticMaterial not found!");
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(SelectedObjectData.objectUrl))
        {
            LoadModelFromUrl(SelectedObjectData.objectUrl);
        }
    }

    public async void LoadModelFromUrl(string modelUrl)
    {
        Debug.Log("[ModelLoader] Loading model from: " + modelUrl);

        var gltf = new GltfImport();
        bool success = await gltf.Load(modelUrl);

        if (!success)
        {
            Debug.LogError("[ModelLoader] Failed to load model from URL.");
            return;
        }

        if (loadedObject != null)
            Destroy(loadedObject);

        GameObject go = new GameObject("LoadedModel");
        success = await gltf.InstantiateMainSceneAsync(go.transform);

        if (!success)
        {
            Debug.LogError("[ModelLoader] Failed to instantiate model.");
            return;
        }

        loadedObject = go;
        loadedObject.SetActive(false);

        ApplyCustomMaterial(loadedObject);
        AssignTagRecursive(loadedObject, "Placed Object");
        AddColliderRecursive(loadedObject);

        Debug.Log("[ModelLoader] Model loaded and ready.");
    }

    private void ApplyCustomMaterial(GameObject target)
    {
        if (futuristicMaterial == null) return;

        foreach(var renderer in target.GetComponentsInChildren<Renderer>())
        {
            renderer.materials = Enumerable.Repeat(futuristicMaterial, renderer.sharedMaterials.Length).ToArray();
        }
    }

    private void AssignTagRecursive(GameObject obj, string tag)
    {
        obj.tag = tag; 
        foreach(Transform child in obj.transform)
        {
            AssignTagRecursive(child.gameObject, tag);
        }
    }

    private void AddColliderRecursive(GameObject root)
    {
        int count = 0;
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            var obj=mf.gameObject;
            if(obj.GetComponent<Collider>() == null)
            {
                var mc = obj.AddComponent<MeshCollider>();
                mc.convex = true;
                count++;
            }
        }

        if (count == 0)
        {
            root.AddComponent<BoxCollider>();
            Debug.LogWarning("[ModelLoader] No Mesh Filters found. Added box collider.");
        }
        else
        {
            Debug.Log("[ModelLoader] Added " + count + "MeshColliders.");
        }
    }
}