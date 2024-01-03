using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public GameObject target;
    public Vector3 offset;

    [Header("FOV")]
    public float rotationSpeed = 10f;
    public float minAngle = -60f;
    public float maxAngle = 60f;
    
    [Header("Zoom")]
    public float zoomSpeed = 10f;
    public float minZoomDistance = 5f;
    public float maxZoomDistance = 50f;

    private float _currentAngle;
    private Vector3 _velocity;
    public bool isShaking;

    private void Start()
    {
        _currentAngle = transform.eulerAngles.x;
        UpdateCameraPosition();
    }

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            // Input logic for FOV
            float inputFov = Input.GetAxis("Mouse ScrollWheel");
            if (inputFov != 0)
            {
                _currentAngle -= -inputFov * rotationSpeed;
                _currentAngle = Mathf.Clamp(_currentAngle, minAngle, maxAngle); 
            }

            // Input logic for Zoom
            //float inputZoom = Input.GetAxis("Vertical");
            //offset.z += inputZoom * zoomSpeed;
            //offset.z = Mathf.Clamp(offset.z, -maxZoomDistance, -minZoomDistance); // note the negatives as we're working with a negative offset.z
        }
        
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        // Convert the current angle into radians
        float radianAngle = _currentAngle * Mathf.Deg2Rad;

        // Calculate height and depth based on trigonometry using the vertical component of the offset
        float height = Mathf.Abs(offset.z) * Mathf.Sin(radianAngle);
        float depth = Mathf.Abs(offset.z) * Mathf.Cos(radianAngle);

        // Determine the camera's new position
        Vector3 newCameraPosition = target.transform.position + new Vector3(0, height, -depth) + new Vector3(0, 0, offset.z); // Add back the original Z offset
        transform.position = newCameraPosition;

        // Always look at the target
        transform.LookAt(target.transform.position);
    }
    
    public IEnumerator Shake(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0.0f;
        Vector3 originalPosition = transform.position; // Store the original camera position

        while (elapsed < duration)
        {
            var targetPos = target.transform.position;
            
            Vector3 randomPoint = targetPos + (Random.insideUnitSphere * magnitude);
            randomPoint.z = originalPosition.z; // Keep the original Z position constant

            var transformPos = transform.position;
            transformPos = Vector3.Lerp(transformPos, randomPoint, Time.deltaTime * 2f);
            
            transformPos = Vector3.Lerp(transformPos, targetPos + offset, Time.deltaTime * 2f);

            transform.position = transformPos;
            
            elapsed += Time.deltaTime;
            isShaking = false;
            yield return null;
        }
    }

}
