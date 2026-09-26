using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameplayCanvas : MonoBehaviour
{
    [SerializeField] private CharacterCtrl characterController;
    [SerializeField] private UIButton kickButton;
    [SerializeField] private UIButton autoKickButton;
    [FormerlySerializedAs("ResetButton")]
    [SerializeField] private UIButton resetButton;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = FindObjectOfType<CharacterCtrl>();
        }

        if (kickButton != null)
        {
            kickButton._OnClick += _OnKickButtonClick;
        }

        if (autoKickButton != null)
        {
            autoKickButton._OnClick += _OnAutoKickButtonClick;
        }

        if (resetButton != null)
        {
            resetButton._OnClick += _OnResetButtonClick;
        }

        if (characterController != null)
        {
            characterController.BallProximityChanged += SetKickButtonVisible;
        }

        SetKickButtonVisible(
            characterController != null && characterController.IsBallInFront);
    }

    private void _OnResetButtonClick(int obj)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (kickButton != null)
        {
            kickButton._OnClick -= _OnKickButtonClick;
        }

        if (characterController != null)
        {
            characterController.BallProximityChanged -= SetKickButtonVisible;
        }

        if (autoKickButton != null)
        {
            autoKickButton._OnClick -= _OnAutoKickButtonClick;
        }

        if (resetButton != null)
        {
            resetButton._OnClick -= _OnResetButtonClick;
        }
    }

    private void _OnKickButtonClick(int obj)
    {
        characterController?.TryKick();
    }

    private void _OnAutoKickButtonClick(int obj)
    {
        characterController?.TryAutoKick();
    }

    private void SetKickButtonVisible(bool isVisible)
    {
        if (kickButton != null)
        {
            kickButton.gameObject.SetActive(isVisible);
        }
    }

}
