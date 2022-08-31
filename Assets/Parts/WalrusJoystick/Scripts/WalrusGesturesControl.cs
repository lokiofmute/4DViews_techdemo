using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class WalrusGesturesControl : WalrusControl
{
    private Transform _transform;
    private Camera _camera;
    private Canvas _canvas;
    private float _lastTouchTime;
    private readonly List<Touch> _touches = new();
    private readonly List<RectTransform> _gesturePoints = new();
    private bool _isPreviousFrameSentGesture;

    private StringBuilder _sb = new();

    public float MaxTimeToClear = 0.2f;
    public bool ShowTouchDebug = true;
    public bool ShowGestureDebug = true;
    public bool ShowGesturePoints = true;

    public RectTransform GesturePointPrefab;

    // Gesture event handler
    internal EventHandler<WalrusGesture> OnGesture;
    protected void RaiseOnGesture(ref WalrusGesture g)
    {
        OnGesture?.Invoke(this, g);
        _isPreviousFrameSentGesture = true;
    }

    public override void Clear()
    {
        IsChanged = false;
        var touchCount = _touches.Count;
        if ((Time.time > _lastTouchTime + MaxTimeToClear) || (touchCount == 1 && _touches[0].phase == TouchPhase.Ended))
        {
            _touches.Clear();
            HideGesturePoints();
            IsChanged = true;
        }
    }

    public override bool SetPoint(Vector2 p)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, p))
            return false;

        _touches.Add(new Touch { position = p, fingerId = 0, rawPosition = p, phase = TouchPhase.Ended });
        IsChanged = true;
        return true;
    }

    private int GetTouchIndex(int fingerId)
    {
        for (int t = 0; t < _touches.Count; ++t)
            if (_touches[t].fingerId == fingerId)
                return t;
        return -1;
    }

    public override bool SetTouch(ref Touch t)
    {
        switch (t.phase)
        {
            case TouchPhase.Began:
                if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, t.position))
                    return false;
                Debug.Assert(GetTouchIndex(t.fingerId) < 0);
                IsChanged = true;
                _touches.Add(t);
                _lastTouchTime = Time.time;
                return true;
            case TouchPhase.Canceled:
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                {
                    var index = GetTouchIndex(t.fingerId);
                    if (index < 0)
                    {
                        if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, t.position))
                            return false;
                        IsChanged = true;
                        _touches.Add(t);
                        _lastTouchTime = Time.time;
                        return true;
                    }
                    _touches[index] = t;
                    IsChanged = true;
                    _lastTouchTime = Time.time;
                    return true;
                }
            case TouchPhase.Ended:
                {
                    var index = GetTouchIndex(t.fingerId);
                    if (index < 0)
                    {
                        IsChanged = false;
                        return false;
                    }
                    IsChanged = false;
                    _touches.RemoveAt(index);
                    _lastTouchTime = Time.time;
                    return true;
                }
        }

        return false;
    }

    private Vector2 TransformPoint(Vector2 pos)
    {
        Vector2 result;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, pos, _camera, out result);
        result /= _canvas.scaleFactor;
        return result;
    }

    private void RecognizeGestures()
    {
        var touchesCount = _touches.Count;
        if (touchesCount == 1)
        {
            var gesture = new WalrusGesture
            {
                Points = new Vector2[] { _touches[0].position },
                NormalizedPoints = new Vector2[] { TransformPoint(_touches[0].position) },
                Center = _touches[0].position,
                NormalizedCenter = TransformPoint(_touches[0].position)
            };
            RaiseOnGesture(ref gesture);
        }
        else if (touchesCount >= 2)
        {
            var gesture = new WalrusGesture
            {
                Points = new Vector2[touchesCount],
                NormalizedPoints = new Vector2[touchesCount]
            };
            for (int t = 0; t < touchesCount; ++t)
            {
                var pos = _touches[t].position;
                gesture.Center += pos;
                gesture.Points[t] = pos;
                gesture.NormalizedPoints[t] = TransformPoint(pos);
            }
            gesture.Center /= touchesCount;
            gesture.NormalizedCenter = TransformPoint(gesture.Center);
            for (int t = 0; t < touchesCount; ++t)
            {
                var dist = Vector2.Distance(gesture.Center, _touches[t].position);
                gesture.AverageDistance += dist;
            }
            gesture.AverageDistance /= touchesCount;

            RaiseOnGesture(ref gesture);
        }
        else if (_isPreviousFrameSentGesture && touchesCount <= 0)
        {
            // when done, send an empty gesture
            var gesture = new WalrusGesture();
            RaiseOnGesture(ref gesture);
            _isPreviousFrameSentGesture = false;
        }
    }

    private void HideGesturePoints()
    {
        foreach (var p in _gesturePoints)
            p.gameObject.SetActive(false);
    }

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>().rootCanvas;
        Debug.Assert(_canvas != null);
        if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
            _camera = _canvas.worldCamera;

        _lastTouchTime = Time.time;
        Debug.Assert(GesturePointPrefab != null);
        _transform = transform;

        OnGesture += (sender, gesture) =>
        {
            if (ShowGestureDebug)
                Debug.Log($"Gesture event raised : {gesture}");

            if (ShowGesturePoints && !gesture.IsEmpty)
            {
                // make sure we have enough points
                var generateCount = gesture.NormalizedPoints.Length - _gesturePoints.Count + 1;
                if (generateCount >= 0)
                {
                    for (int t = 0; t < generateCount; ++t)
                        _gesturePoints.Add(Instantiate(GesturePointPrefab, _transform));
                }
                else
                    HideGesturePoints();

                // now make the necessary ones visible and locate them
                if (_gesturePoints.Count > 0)
                {
                    _gesturePoints[0].gameObject.SetActive(true);
                    _gesturePoints[0].anchoredPosition = gesture.NormalizedCenter;
                    for (int t = 0; t < gesture.NormalizedPoints.Length; ++t)
                    {
                        var p = _gesturePoints[t + 1];
                        p.gameObject.SetActive(true);
                        p.anchoredPosition = gesture.NormalizedPoints[t];
                    }
                }
            }
        };
    }

    private void LateUpdate()
    {
        if (IsChanged)
        {
            if (ShowTouchDebug)
            {
                _sb.Clear();
                _sb.AppendLine($"time: {Time.time}");
                foreach (var t in _touches)
                    _sb.AppendLine($"   Touch {t.fingerId}: {t.position} -> {TransformPoint(t.position)}, ({t.phase})");
                Debug.Log(_sb);
            }

            RecognizeGestures();
        }
    }
}

public struct WalrusGesture
{
    public Vector2 Center;
    public Vector2[] Points;
    public float AverageDistance;

    public Vector2 NormalizedCenter;
    public Vector2[] NormalizedPoints;

    public bool IsEmpty => Points == null;

    public override string ToString()
    {
        if (IsEmpty)
            return "Empty gesture";

        StringBuilder sb = new($"Gesture({Points.Length} points):{Environment.NewLine}Center = {Center}, AvgDist = {AverageDistance}, Points = ");
        foreach (var p in Points)
            sb.Append($"({p}), ");
        sb.Append(Environment.NewLine);
        sb.Append($"NormCenter = {NormalizedCenter}, NormPoints = ");
        foreach (var p in NormalizedPoints)
            sb.Append($"({p}), ");
        return sb.ToString();
    }
}
