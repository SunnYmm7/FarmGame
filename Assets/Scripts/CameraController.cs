using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera _camera = null;
    [SerializeField] private float _moveSpeed = 50f, _moveSmoothing = 10f;
    [SerializeField] private float _zoomSpeed = 100f, _zoomSmoothing = 5f, _zoomMin = 1f, _zoomMax = 20f;
    [SerializeField] private float _mouseScrollSensitivity = 3f, _cameraAngle = 45f, _initialZoom = 5f;
    [SerializeField] private Vector3 _center = Vector3.zero;
    [SerializeField] private float _rightBound = 40f, _leftBound = 40f, _upBound = 40f, _downBound = 40f;

    private Controls _controls;
    private bool _isZooming, _isMoving;
    private float _currentZoom = 8f, _sinCameraAngle, _zoomBaseValue, _zoomBaseDistance;
    private Vector2 _zoomScreenPosition;
    private Vector3 _zoomWorldPosition;
    private Transform _cameraRoot, _cameraPivot, _cameraTarget;
    
    private void Awake()
    {
        _controls = new Controls();
        _cameraRoot = new GameObject("CameraRoot").transform;
        _cameraPivot = new GameObject("CameraPivot").transform;
        _cameraTarget = new GameObject("CameraTarget").transform;
        
        if (_camera == null) _camera = Camera.main;
        _camera.orthographic = true;
        _camera.nearClipPlane = 0.1f;
        _sinCameraAngle = Mathf.Sin(_cameraAngle * Mathf.Deg2Rad);
        
        _cameraPivot.SetParent(_cameraRoot);
        _cameraTarget.SetParent(_cameraPivot);
        _cameraRoot.position = _center;
        _cameraPivot.localEulerAngles = new Vector3(_cameraAngle, 45f, 0);
        _cameraTarget.localPosition = new Vector3(0, 0, -100);
    }
    
    private void Start() => _camera.orthographicSize = _currentZoom = _initialZoom;
    
    private void OnEnable()
    {
        _controls.Enable();
        _controls.Main.Move.started += _ => _isMoving = true;
        _controls.Main.Move.canceled += _ => _isMoving = false;
        _controls.Main.TouchZoom.started += _ => StartZooming();
        _controls.Main.TouchZoom.canceled += _ => _isZooming = false;
    }
    
    private void OnDisable()
    {
        if (_controls != null)
        {
            _controls.Main.Move.started -= _ => _isMoving = true;
            _controls.Main.Move.canceled -= _ => _isMoving = false;
            _controls.Main.TouchZoom.started -= _ => StartZooming();
            _controls.Main.TouchZoom.canceled -= _ => _isZooming = false;
            _controls.Disable();
        }
    }
    
    private void Update()
    {
        // Handle mouse scroll
        if (!Input.touchSupported)
        {
            float scroll = _controls.Main.MouseScroll.ReadValue<float>();
            if (Mathf.Abs(scroll) > 0.01f) _currentZoom -= scroll * _mouseScrollSensitivity * Time.deltaTime;
        }
        
        // Handle touch zoom
        if (_isZooming)
        {
            Vector2 t0 = _controls.Main.TouchPosition0.ReadValue<Vector2>();
            Vector2 t1 = _controls.Main.TouchPosition1.ReadValue<Vector2>();
            float dist = Vector2.Distance(new Vector2(t0.x / Screen.width, t0.y / Screen.height), 
                                        new Vector2(t1.x / Screen.width, t1.y / Screen.height));
            _currentZoom = _zoomBaseValue - (dist - _zoomBaseDistance) * _zoomSpeed;
            Vector3 center = ScreenToWorldPosition(_zoomScreenPosition);
            _cameraRoot.position += _zoomWorldPosition - center;
        }
        // Handle movement
        else if (_isMoving)
        {
            Vector2 move = _controls.Main.MoveDelta.ReadValue<Vector2>();
            if (move.sqrMagnitude > 0.001f)
            {
                move = new Vector2(move.x / Screen.width, move.y / Screen.height) * _moveSpeed;
                _cameraRoot.position -= _cameraRoot.right.normalized * move.x + _cameraRoot.forward.normalized * move.y;
            }
        }
        
        ApplyBounds();
        
        // Smooth camera updates
        _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _currentZoom, Time.deltaTime * _zoomSmoothing);
        _camera.transform.position = Vector3.Lerp(_camera.transform.position, _cameraTarget.position, Time.deltaTime * _moveSmoothing);
        _camera.transform.rotation = _cameraTarget.rotation;
    }
    
    private void StartZooming()
    {
        Vector2 t0 = _controls.Main.TouchPosition0.ReadValue<Vector2>();
        Vector2 t1 = _controls.Main.TouchPosition1.ReadValue<Vector2>();
        _zoomScreenPosition = Vector2.Lerp(t0, t1, 0.5f);
        _zoomWorldPosition = ScreenToWorldPosition(_zoomScreenPosition);
        _zoomBaseValue = _currentZoom;
        _zoomBaseDistance = Vector2.Distance(new Vector2(t0.x / Screen.width, t0.y / Screen.height), 
                                           new Vector2(t1.x / Screen.width, t1.y / Screen.height));
        _isZooming = true;
    }
    
    private void ApplyBounds()
    {
        // Zoom bounds
        float maxZoom = Mathf.Min((_upBound + _downBound) / 2f, (_leftBound + _rightBound) / (2f * _camera.aspect)) * _sinCameraAngle;
        _currentZoom = Mathf.Clamp(_currentZoom, _zoomMin, Mathf.Min(_zoomMax, maxZoom));
        
        // Position bounds
        float h = _currentZoom * 2f / _sinCameraAngle / 2f;
        float w = h * _camera.aspect;
        Vector3 offset = Vector3.zero;
        
        Vector3 tr = _cameraRoot.position + _cameraRoot.right * w + _cameraRoot.forward * h;
        Vector3 tl = _cameraRoot.position - _cameraRoot.right * w + _cameraRoot.forward * h;
        Vector3 bl = _cameraRoot.position - _cameraRoot.right * w - _cameraRoot.forward * h;
        
        if (tr.x > _center.x + _rightBound) offset.x -= tr.x - (_center.x + _rightBound);
        if (tl.x < _center.x - _leftBound) offset.x += (_center.x - _leftBound) - tl.x;
        if (tr.z > _center.z + _upBound) offset.z -= tr.z - (_center.z + _upBound);
        if (bl.z < _center.z - _downBound) offset.z += (_center.z - _downBound) - bl.z;
        
        _cameraRoot.position += offset;
    }
    
    private Vector3 ScreenToWorldPosition(Vector2 screenPos)
    {
        float h = _camera.orthographicSize * 2f;
        float w = h * _camera.aspect;
        Vector3 anchor = _camera.transform.position - _camera.transform.right * (w/2f) - _camera.transform.up * (h/2f);
        return anchor + _camera.transform.right * (screenPos.x / Screen.width * w) + _camera.transform.up * (screenPos.y / Screen.height * h);
    }
    
    // Public API
    public void SetBounds(Vector3 center, float right, float left, float up, float down) 
        => (_center, _rightBound, _leftBound, _upBound, _downBound) = (center, right, left, up, down);
    
    public void SetZoomLimits(float min, float max) 
        => (_zoomMin, _zoomMax) = (Mathf.Max(0.1f, min), Mathf.Max(_zoomMin, max));
    
    public float GetCurrentZoom() => _currentZoom;
    public void SetZoom(float zoom) => _currentZoom = Mathf.Clamp(zoom, _zoomMin, _zoomMax);
    public void MoveTo(Vector3 position) => _cameraRoot.position = position;
}