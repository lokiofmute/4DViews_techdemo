using UnityEngine;

public class SceneSteering : MonoBehaviour
{
    private Transform _transform;
    private float? _prevGestureAverageDistance;
    private Vector2? _prevGestureCenter;
    private Vector2 _rotationEuler;
    private Vector2 _rotationEulerVelocity;

    public WalrusGesturesControl WalrusGesturesControl;
    public WalrusJoystick WalrusJoystick;
    public Transform CameraTransform;
    public Vector2 MinRotationZ = new(-25, 25);
    public float RotationDampening = 1.0f;

    public float MoveSpeed = 0.1f;
    public float MaxRotationSpeed = 10;
    public float PinchZoomSpeed = 50;
    public float CameraBlendSpeed = 1;
    public float CameraOffset = 10;
    public Vector2 CameraOffsetLimit = new Vector2(5, 20);

    private void Awake()
    {
        _transform = transform;
        Debug.Assert(CameraTransform != null);
        Debug.Assert(WalrusJoystick != null);
        Debug.Assert(WalrusGesturesControl != null);
        WalrusGesturesControl.OnGesture += HandleOnGesture;
        CameraOffset = CameraTransform.localPosition.x;
    }

    private void HandleOnGesture(object sender, WalrusGesture gesture)
    {
        // receive the ending or idle gesture
        if (gesture.IsEmpty)
        {
            _prevGestureAverageDistance = null;
            _prevGestureCenter = null;
            return;
        }

        // receive the opening gesture
        switch (gesture.Points.Length)
        {
            case 1:
                // single touch -> move
                if (_prevGestureCenter == null)
                {
                    _prevGestureCenter = gesture.Center;
                }
                else
                {
                    var diff = gesture.Center - _prevGestureCenter.Value;
                    _rotationEulerVelocity += diff * Time.deltaTime * MoveSpeed;
                    _rotationEulerVelocity.x = Mathf.Clamp(_rotationEulerVelocity.x, -MaxRotationSpeed, MaxRotationSpeed);
                    _rotationEulerVelocity.y = Mathf.Clamp(_rotationEulerVelocity.y, -MaxRotationSpeed, MaxRotationSpeed);
                    _prevGestureCenter = gesture.Center;
                }
                break;
            default:
                if (_prevGestureAverageDistance == null)
                {
                    _prevGestureAverageDistance = gesture.AverageDistance;
                }
                else
                {
                    var diff = gesture.AverageDistance - _prevGestureAverageDistance.Value;
                    _prevGestureAverageDistance = gesture.AverageDistance;
                    CameraOffset = Mathf.Clamp(CameraOffset + diff * Time.deltaTime * PinchZoomSpeed, CameraOffsetLimit.x, CameraOffsetLimit.y);
                    var pos = CameraTransform.localPosition;
                    CameraTransform.localPosition = Vector3.Lerp(pos, new Vector3(CameraOffset, pos.y, pos.z), CameraBlendSpeed * Time.deltaTime);
                }
                break;
        }

    }

    private void Update()
    {
        _rotationEuler.x += _rotationEulerVelocity.x;
        _rotationEuler.y = Mathf.Clamp(_rotationEuler.y + _rotationEulerVelocity.y, MinRotationZ.x, MinRotationZ.y);
        _transform.rotation = Quaternion.Euler(0, _rotationEuler.x, _rotationEuler.y);

        _rotationEulerVelocity = Vector2.Lerp(_rotationEulerVelocity, Vector2.zero, 1.0f - Mathf.Clamp01(Time.deltaTime * RotationDampening));
    }
}
