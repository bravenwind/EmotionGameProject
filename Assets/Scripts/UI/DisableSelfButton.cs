using UnityEngine;
using UnityEngine.UI;

public class DisableSelfButton : MonoBehaviour
{
    private void Start()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(DisableSelf);
    }

    public void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}
