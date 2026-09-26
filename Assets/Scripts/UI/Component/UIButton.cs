
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UIButton : MonoBehaviour
{

    public Action<int> _OnClick;
    public enum STATE
    {
        DISABLE = 0,
        OPENING = 1,
        SELECTING = 2,
        CLOSING = 3,
    }

    //Index follow State
    [SerializeField]
    protected int indexID;
    [SerializeField]
    protected Button button;
    [SerializeField]
    protected Transform tf;
    [SerializeField]
    protected TMP_Text textButton;

    protected STATE state;
    public int IndexID => indexID;
    public STATE State => state;
    public Transform Tf => tf;
    public RectTransform RectTf => (RectTransform)tf;
    protected virtual void Awake()
    {
        button.onClick.AddListener(OnClick);
    }
    protected virtual void OnDestroy()
    {
        button.onClick.RemoveListener(OnClick);
    }
    public virtual void SetData(string text)
    {
        if (textButton != null)
            textButton.text = text;
    }

    public virtual void SetInteractable(bool state)
    {
        button.interactable = state;
    }

    protected virtual void OnClick()
    {

        _OnClick?.Invoke(indexID);
    }

}