using UnityEngine;

public class DisableAllChild : MonoBehaviour
{
    public static DisableAllChild Instance;

    private void Awake()
    {
        Instance = this;
        DisableAllLineRenderer();
    }

    public void DisableAll()
    {
        Transform[] childs = GetComponentsInChildren<Transform>();

        foreach (Transform child in childs)
        {
            child.gameObject.SetActive(false);
        }
    }

    public void DisableAllLineRenderer()
    {
        LineRenderer[] lines = GetComponentsInChildren<LineRenderer>();
        foreach (LineRenderer line in lines)
        {
            line.enabled = false;
        }
    }
}

