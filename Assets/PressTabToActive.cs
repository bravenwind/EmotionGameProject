using UnityEngine;

public class PressTabToActive : MonoBehaviour
{
    public GameObject go;

    private void Update()
    {
        if (GameSceneUIManager.Instance.currentState == GameSceneUIState.InGame)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                go.SetActive(!go.activeSelf);
                if (Cursor.visible)
                {
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.visible = true;
                }

                Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }
    }
}
