using UnityEngine;
using UnityEngine.UI;

public class DynamicButtonLoader : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform buttonParent;
    public UIInteraction uiInteraction;

    [System.Serializable]
    public class ObjectData
    {
        public string name;
        public string category;
        public string url;
    }

    public ObjectData[] mockObjects;

    void Start()
    {
        foreach (var obj in mockObjects)
        {
            GameObject buttonGO = Instantiate(buttonPrefab, buttonParent);
            buttonGO.GetComponentInChildren<Text>().text = obj.name;

            Button button = buttonGO.GetComponent<Button>();
            string name= obj.name;
            string url= obj.url;
            string category = obj.category;

            button.onClick.AddListener(() =>
            {
                uiInteraction.OnObjectSelected(url, name, category);
            });
        }
    }
}