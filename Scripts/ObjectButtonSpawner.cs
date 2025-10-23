using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ObjectButtonSpawner : MonoBehaviour
{
    public Transform contentGrid;
    public GameObject objectButtonPrefab;
    [SerializeField] private ModelLoader modelLoader;

    private FirebaseFirestore db;

    void Awake()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    private async void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && db != null)
        {
            try { await db.EnableNetworkAsync(); } catch { }
        }
    }

    public void LoadObjectsForCategory(string category) 
    {
        if(db == null)
        {
            Debug.LogError("[ObjectSpawner] Firestore not initialized,");
            return;
        }

        foreach(Transform child in contentGrid)
            Destroy(child.gameObject);

        var query= db.Collection("catalog").WhereEqualTo("Category", category);

        query.GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(serverTask =>
        {
            if (serverTask.IsFaulted || serverTask.IsCanceled)
            {
                Debug.LogWarning("[ObjectSpawner] Server fetch failed, trying cache: " + serverTask.Exception);
                query.GetSnapshotAsync(Source.Cache).ContinueWithOnMainThread(cacheTask =>
                {
                    if (cacheTask.IsFaulted || cacheTask.IsCanceled)
                    {
                        Debug.LogError("[ObjectSpawner] Cache fetch failed: " + cacheTask.Exception);
                        return;
                    }

                    BuildButtons(category, cacheTask.Result);
                });
                return;
            }
            BuildButtons(category, serverTask.Result);
        });
    }

    private void BuildButtons(string category, QuerySnapshot snapshot)
    {
        Debug.Log("[ObjectSpawner] Total objects for " + category + snapshot.Count);

        foreach(DocumentSnapshot doc in snapshot.Documents)
        {
            string nume = doc.ContainsField("Nume") ? doc.GetValue<string>("Nume") : doc.Id;
            string url = doc.ContainsField("url") ? doc.GetValue<string>("url") : "";

            GameObject go = Instantiate(objectButtonPrefab, contentGrid);
            go.name = nume;

            TMP_Text tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null) 
            {
                tmp.text = nume;
                Debug.Log("[ObjectSpawner] Set TMP text: " + nume);
            }
            else
            {
                Text legacyText = go.GetComponentInChildren<Text>();
                if (legacyText != null) 
                {
                    legacyText.text = nume;
                }
                else
                {
                    Debug.LogWarning("[ObjectSpawner] No text component founct in prefab " +go.name);
                }
            }

            Button btn = go.GetComponent<Button>();
            if (btn != null) 
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log("[ObjectSpawner] Selected: " + nume + "URL: " + url);
                    SelectedObjectData.objectName = nume;
                    SelectedObjectData.objectUrl = url;
                    SceneManager.LoadScene("ARPlacementScene");
                });
            }
            go.SetActive(true);
        }
    }
}