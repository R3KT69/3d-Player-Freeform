using UnityEngine;

public class Mouse : MonoBehaviour
{
    public GameObject playerCamera;
    public float mouseSensitivity = 2f;
    public float maxLookDegree = 75f;
    private float xRotation = 0f;

    void Update()
    {
        if (!Menu.inMenu)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            // Clamp vertical rotation
            xRotation -= mouseY; 
            xRotation = Mathf.Clamp(xRotation, -maxLookDegree, maxLookDegree);

            // Apply rotations
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); 
            transform.Rotate(Vector3.up * mouseX); 
        }
        
    }
}
