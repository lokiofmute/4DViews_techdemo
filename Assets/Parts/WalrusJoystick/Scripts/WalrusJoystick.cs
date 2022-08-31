// Vincent email adres: trnmntacc@gmail.com


using System;
using UnityEngine;

[Flags]
public enum AxisKind
{
    None = 0,
    Horizontal = 1 << 0,
    Vertical = 1 << 1,
    Both = Horizontal | Vertical
}

public class WalrusJoystick : WalrusControl
{
    private Camera _camera;
    private Canvas _canvas;
    private Vector2 _backgroundRadius;
    private Vector2 _anchorFactor;
    private int _fingerId = -1;

    public RectTransform Handle;
    public float Min;
    public float Max;
    public float Radius = 1.0f;
    public bool IsSticky = false;
    public AxisKind AxisKind = AxisKind.Both;
    public Vector2 Direction { get; private set; }

    private void Awake()
    {
        Debug.Assert(Handle != null);

        _canvas = GetComponentInParent<Canvas>().rootCanvas;
        Debug.Assert(_canvas != null);
        if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
            _camera = _canvas.worldCamera;

        _backgroundRadius = RectTransform.sizeDelta * 0.5f;
        _anchorFactor = Radius * _backgroundRadius;

        CenterHandle();
    }

    public override bool SetTouch(ref Touch t)
    {
        switch(t.phase)
        {
            case TouchPhase.Began:
                if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, t.position))
                    return false;
                Position = t.position;
                IsChanged = true;
                _fingerId = t.fingerId;
                return true;
            case TouchPhase.Canceled:
                if (_fingerId < 0)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, t.position))
                        return false;
                }
                else if (_fingerId != t.fingerId)
                    return false;               
                Position = t.position;
                IsChanged = true;
                _fingerId = t.fingerId;
                return true;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if(_fingerId == t.fingerId)
                {
                    Position = t.position;
                    IsChanged = true;
                    return true;
                }
                return false;
            case TouchPhase.Ended:
                if(t.fingerId == _fingerId)
                {
                    _fingerId = -1;
                    if (!IsSticky)
                    {
                        IsChanged = false;
                        Position = Vector2.zero;
                        CenterHandle();
                    }
                    return true;
                }
                return false;
        }

        return false;
    }

    private void LateUpdate()
    {
        if (IsChanged)
            SetDirection(TransformPoint(Position));
        else if(!IsSticky)
            CenterHandle();
    }

    private void SetDirection(Vector2 pos)
    {
        Handle.anchoredPosition = pos * _anchorFactor;
        float rescale = (Max - Min) * .5f;
        Direction = new Vector2((pos.x + 1) * rescale + Min,
                                (pos.y + 1) * rescale + Min);
    }

    private void CenterHandle() => SetDirection(Vector2.zero);    

    private Vector2 TransformPoint(Vector2 pos)
    {
        Vector2 result;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, pos, _camera, out result);
        result /= _backgroundRadius * _canvas.scaleFactor;
        if (result.sqrMagnitude > 1.0f)
            result.Normalize();
        if (AxisKind != AxisKind.Both)
        {
            if (AxisKind == AxisKind.Vertical)
                result.x = 0;
            else if (AxisKind == AxisKind.Horizontal)
                result.y = 0;
        }
        return result;
    }

}