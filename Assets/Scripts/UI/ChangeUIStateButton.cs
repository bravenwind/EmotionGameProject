using UnityEngine;

public class ChangeUIStateButton : MonoBehaviour
{
    public void ChangeUIState(UIStateComponent uiStateComponent)
    {
        GameSceneUIState uiState = uiStateComponent.uiState;
        GameSceneUIManager.Instance.SetState(uiState);
    }
}
