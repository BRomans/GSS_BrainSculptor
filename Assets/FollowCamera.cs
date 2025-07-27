using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Camera targetCamera;
    public Vector3 offset = new Vector3(0, 0, 5);
    public bool faceCamera = true;

    void LateUpdate()
    {
        if (targetCamera != null)
        {
            Vector3 desiredPosition = targetCamera.transform.position + targetCamera.transform.TransformDirection(offset);
            transform.position = desiredPosition;

            if (faceCamera)
            {
                // Make the object look at the camera
                transform.LookAt(targetCamera.transform.position);
                Vector3 directionToCamera = targetCamera.transform.position - transform.position;
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
}