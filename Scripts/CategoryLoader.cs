using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;

public class CategoryLoader : MonoBehaviour
{
    public TMP_Dropdown categoryDropdown;
    public ObjectButtonSpawner objectSpawner;

    private FirebaseFirestore db;
    private bool initialized = false;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Firebase dependency check failed: " + task.Exception);
                return;
            }

            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                LoadCategories();
            }
            else
            {
                Debug.LogError("Firebase dependencies not available: " + dependencyStatus);
            }
        });
    }

    void LoadCategories()
    {
        db.Collection("catalog").GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(serverTask =>
        {
            if (!serverTask.IsCompletedSuccessfully)
            {
                Debug.LogWarning("[CategoryLoader] Server read failed, trying cache: " + serverTask.Exception);
                db.Collection("catalog").GetSnapshotAsync(Source.Cache).ContinueWithOnMainThread(cacheTask =>
                {
                    if (!cacheTask.IsCompletedSuccessfully)
                    {
                        Debug.LogError("[CategoryLoader] Cache read also failed: " + cacheTask.Exception);
                        return;
                    }
                    PopulateDropdown(cacheTask.Result);
                });
                return;
            }

            PopulateDropdown(serverTask.Result);
        });
    }

    private void PopulateDropdown(QuerySnapshot snapshot)
    {
        HashSet<string> categories = new();

        foreach (var doc in snapshot.Documents)
        {
            if (doc.ContainsField("Category"))
            {
                string cat = doc.GetValue<string>("Category");
                if (!string.IsNullOrEmpty(cat))
                {
                    categories.Add(cat);
                    Debug.Log("[CategoryLoader] Found category: " + cat);
                }
            }
        }

        List<string> categoryList= new(categories);
        categoryDropdown.onValueChanged.RemoveListener(OnCategoryChanged);
        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(categoryList);
        categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        initialized = true;

        if (categoryList.Count > 0)
        {
            Debug.Log("[CategoryLoader] Selected category: " + categoryList[0]);
            OnCategoryChanged(0);
        }
        else
        {
            Debug.LogWarning("[CategoryLoader] No categories found.");
        }
    }

    void OnCategoryChanged(int index)
    {
        if (!initialized)
        {
            Debug.LogWarning("[CategoryLoader] Ignored OnCategpryChanged because initialization not complete.");
            return;
        }

        if(categoryDropdown == null || categoryDropdown.options == null || categoryDropdown.options.Count == 0)
        {
            Debug.LogError("[CategoryLoader] Dropdown or options are missing.");
            return;
        }

        string selectedCategory = categoryDropdown.options[index].text;
        Debug.Log("[CategoryLoader] Selected category: " + selectedCategory);

        if (objectSpawner != null) 
        { 
            objectSpawner.LoadObjectsForCategory(selectedCategory);
        }
        else
        {
            Debug.LogError("[CategoryLoader] ObjectButtonSpawner is not assigned!");
        }
    }

    private async void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && db != null)
        {
            try { await db.EnableNetworkAsync(); } catch { }
        }
    }
}