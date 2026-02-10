using UnityEngine;
using UnityEngine.UI;

public class DisableSelfButton : MonoBehaviour
{
    public void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}
