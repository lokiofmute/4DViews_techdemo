using UnityEngine;

public class WalrusInput : MonoBehaviour
{
    private WalrusControl[] _controls;

    private void Awake()
    {
        //Input.multiTouchEnabled = true;
        Debug.Log($"Joystick {name} input is {(Input.multiTouchEnabled ? "MultiTouch" : "SingleTouch")}");

        _controls = GetComponentsInChildren<WalrusControl>();
        Debug.Assert(_controls.Length > 0);
        Debug.Log($"WalrusInput detected {_controls.Length} controls");
    }

    //private StringBuilder _sb = new StringBuilder();

    private void Update()
    {
        //_sb.Clear();
        ClearAllControls();

        var touchCount = Input.touchCount;
        if (touchCount > 0)
        {
            //_sb.Append($"# Touches = {touchCount}, ");
            for (int t = 0; t < touchCount; ++t)
            {
                var touch = Input.GetTouch(t);
                //_sb.Append($"Touch {touch.fingerId}, phase = {touch.phase}, pos = {touch.position}");
                DistributeTouch(ref touch);
            }
            //_sb.AppendLine();
        }
        else if (Input.GetMouseButton(0))
        {
            //_sb.Append($"Mouse {Input.mousePosition}");
            DistributePoint(Input.mousePosition);
            //_sb.AppendLine();
        }

        //if (_sb.Length > 0)
        //    Debug.Log(_sb.ToString());
    }

    private void DistributePoint(Vector2 p)
    {
        foreach (var c in _controls)
            if (c.SetPoint(p))
            {
            //    _sb.Append($" -> {c.name}");
            }
    }

    private void DistributeTouch(ref Touch t)
    {
        foreach (var c in _controls)
            if (c.SetTouch(ref t))
            {
            //    _sb.Append($" -> {c.name}");
            }
    }

    private void ClearAllControls()
    {
        foreach (var c in _controls)
            c.Clear();
    }
}
