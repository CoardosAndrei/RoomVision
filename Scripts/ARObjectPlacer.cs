using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement;

public enum InteractionMode
{
    None,
    Placement,
    Translation,
    Scaling,
    Rotation,
    Deletion
}

public class ARObjectPlacer : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public ModelLoader modelLoader;

    public float initialScaleFactor = 0.25f;
    public float scaleSpeed = 0.05f;
    public float moveSpeed = 1.0f;
    public float maxScale = 1.0f;
    public float minScale = 0.05f;

    private GameObject lastPlacedObject=null;
    private Vector2 previousMidPoint;
    private bool isMoving=false;

    private float lastTapTime = 0f;
    private const float doubleTapThreshold = 0.3f;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    public InteractionMode currentMode = InteractionMode.None;

    void Update()
    {
        if (!IsReady()) return;

        if(Input.touchCount == 1)
        {
            Touch touch= Input.GetTouch(0);

            if (currentMode == InteractionMode.Placement)
                HandlePlacement(touch);
            else if (currentMode == InteractionMode.Deletion)
                HandleDeletion(touch);
        }
        else if (Input.touchCount == 2 && lastPlacedObject != null)
        {
            if (currentMode == InteractionMode.Translation)
                HandleTranslation(Input.GetTouch(0), Input.GetTouch(1));
            else if (currentMode == InteractionMode.Scaling)
                HandleScaling(Input.GetTouch(0), Input.GetTouch(1));
            else if (currentMode == InteractionMode.Rotation)
                HandleRotation(Input.GetTouch(0), Input.GetTouch(1));
        }
        else
        {
            isMoving = false;
        }
    }

    bool IsReady()
    {
        return raycastManager != null && arCamera != null && modelLoader != null && modelLoader.loadedObject != null;
    }

    void HandlePlacement(Touch touch)
    {
        if (touch.phase != TouchPhase.Began) return;

        float timeSinceLastTap = Time.time - lastTapTime;
        lastTapTime = Time.time;

        if (timeSinceLastTap >= doubleTapThreshold) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            GameObject newObject = Instantiate(modelLoader.loadedObject, hitPose.position, hitPose.rotation);

            if (PersistentPlacedObjects.Instance != null && PersistentPlacedObjects.Instance.Root != null)
            {
                newObject.transform.SetParent(PersistentPlacedObjects.Instance.Root, true);
            }

            newObject.SetActive(true);
            newObject.transform.localScale = Vector3.one * initialScaleFactor;

            AssignTagRecursively(newObject, "Placed Object");

            if (newObject.GetComponentInChildren<Collider>() == null)
            {
                MeshRenderer mesh = newObject.GetComponentInChildren<MeshRenderer>();
                if (mesh != null)
                    mesh.gameObject.AddComponent<BoxCollider>();
                else
                    newObject.AddComponent<BoxCollider>();
            }

            lastPlacedObject = newObject;
            Debug.Log("[AR] Object placed on single tap.");
        }
        else Debug.LogWarning("[AR] Raycast failed - no surface found.");
    }

    void HandleDeletion(Touch touch)
    {
        if (touch.phase != TouchPhase.Began) return;

        float timeSinceLastTap = Time.time - lastTapTime;
        lastTapTime = Time.time;

        if (timeSinceLastTap < doubleTapThreshold)
        {
            Ray ray = arCamera.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.CompareTag("Placed Object"))
                {
                    Destroy(hit.collider.gameObject);
                    Debug.Log("[AR] Object deleted.");
                }
            }
        }
    }

    void HandleTranslation(Touch t0, Touch t1)
    {
        Vector2 currentMid = (t0.position + t1.position) * 0.5f;

        if (!isMoving)
        {
            previousMidPoint= currentMid;
            isMoving= true;
            return;
        }

        Vector2 delta = currentMid - previousMidPoint;
        previousMidPoint = currentMid;

        Vector3 screenPos=arCamera.WorldToScreenPoint(lastPlacedObject.transform.position);
        screenPos += new Vector3(delta.x, delta.y, 0) * moveSpeed;

        if(raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose= hits[0].pose;
            lastPlacedObject.transform.position = hitPose.position;
        }
    }

    void HandleScaling(Touch t0, Touch t1)
    {
        Vector2 prevT0 = t0.position - t0.deltaPosition;
        Vector2 prevT1 = t1.position - t1.deltaPosition;

        float prevDistance = Vector2.Distance(prevT0, prevT1);
        float currentDistance = Vector2.Distance(t0.position, t1.position);
        float delta = currentDistance - prevDistance;

        float scaleChange = delta * scaleSpeed;
        float newScale = Mathf.Clamp(lastPlacedObject.transform.localScale.x + scaleChange, minScale, maxScale);
        lastPlacedObject.transform.localScale = Vector3.one * newScale;
    }

    void HandleRotation(Touch t0, Touch t1)
    {
        Vector2 prevPos0 = t0.position - t0.deltaPosition;
        Vector2 prevPos1 = t1.position - t1.deltaPosition;

        Vector2 currentVector = t1.position - t0.position;
        Vector2 previousVector = prevPos1 - prevPos0;

        float angle = Vector2.SignedAngle(previousVector, currentVector);
        lastPlacedObject.transform.Rotate(0f, -angle, 0f, Space.World);
    }

    void AssignTagRecursively(GameObject obj, string tag)
    {
        obj.tag = tag;
        foreach (Transform child in obj.transform)
        {
            AssignTagRecursively(child.gameObject, tag);
        } 
    }

    public void GoBackToMenu()
    {
        SceneManager.LoadScene("ObjectSelectorScene");
    }

    public void SetModeToPlacement()
    {
        currentMode = InteractionMode.Placement;
    }

    public void SetModeToTranslation()
    { 
        currentMode = InteractionMode.Translation; 
    }

    public void SetModeToScaling()
    {
        currentMode = InteractionMode.Scaling;
    }

    public void SetModeToRotation()
    {
        currentMode = InteractionMode.Rotation;
    }

    public void SetModeToDeletion()
    {
        currentMode= InteractionMode.Deletion;
    }
}