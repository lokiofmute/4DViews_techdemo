
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class WalrusControl : MonoBehaviour
{
    private RectTransform _rectTransform;

    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : (_rectTransform = GetComponent<RectTransform>());

    public bool IsChanged { get; set; }
    public Vector2 Position { get; set; }

    public virtual void Clear()
    {
        IsChanged = false;
        Position = Vector2.zero;
    }

    public virtual bool SetPoint(Vector2 p)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, p))
            return false;

        Position = p;
        IsChanged = true;
        return true;
    }

    public virtual bool SetTouch(ref Touch t)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, t.position))
            return false;

        Position = t.position;
        IsChanged = true;
        return true;
    }
}
