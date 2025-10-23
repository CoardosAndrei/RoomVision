using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentPlacedObjects : MonoBehaviour
{
    public static PersistentPlacedObjects Instance { get; private set; }
    public Transform Root => _root != null ? _root.transform : null;

    [SerializeField] private GameObject _root;

    private void Awake()
    {
        if (Instance != null) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if(_root == null)
        {
            _root = new GameObject("PlacedObjectsRoot");
            DontDestroyOnLoad(_root);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) 
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        bool inAR = s.name == "ARPlacementScene";
        if(_root != null) 
            _root.SetActive(inAR);
    }

    public void ClearAllPlaced()
    {
        if (_root != null) 
            return;
        for(int i=_root.transform.childCount -1; i>=0;i--)
            Destroy(_root.transform.GetChild(i).gameObject);
    }
}