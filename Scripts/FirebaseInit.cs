using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;

public class FirebaseInit : MonoBehaviour
{
    private static bool _initialized = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if(_initialized) return;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var result= task.Result;
            if(result == DependencyStatus.Available)
            {
                var _ = FirebaseFirestore.DefaultInstance;

                _initialized = true;
                Debug.Log("Firebase is ready (initialized once).");
            }
            else
            {
                Debug.LogError("Firebase not ready: " + result);
            }
        });
    }

    private async void OnApplicationFocus(bool hasFocus)
    {
        if(!hasFocus | !_initialized) return;
        try
        {
            var db = FirebaseFirestore.DefaultInstance; ;
            await db.EnableNetworkAsync();
            await db.Collection("_warmup_ignore").Limit(1).GetSnapshotAsync(Source.Server);
        }
        catch { }
    }
}