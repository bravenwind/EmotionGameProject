using UnityEngine;

public class EnableGameObjectButton : MonoBehaviour
{
    public void EnableGameObject(GameObject go)
    {
        go.SetActive(true);
    }
}
