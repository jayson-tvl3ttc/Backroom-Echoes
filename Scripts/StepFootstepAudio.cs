using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepFootstepAudio : MonoBehaviour
{
    public AudioSource footstepAudio;
    public Transform xrRigTransform;

    public float minStepDistance = 0.25f; // Step interval for fast movement
    public float maxStepDistance = 0.5f;  // Step interval for slow movement

    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;

    void Start()
    {
        if (xrRigTransform == null)
            xrRigTransform = this.transform;

        lastPosition = xrRigTransform.position;
    }

    void Update()
    {
        Vector3 currentPosition = xrRigTransform.position;
        float deltaDistance = Vector3.Distance(currentPosition, lastPosition);
        accumulatedDistance += deltaDistance;

        // Calculate current movement speed
        float movementSpeed = deltaDistance / Time.deltaTime;

        // Use speed to determine step interval (fast ¡ú small interval, slow ¡ú large interval)
        float t = Mathf.Clamp01(movementSpeed / 1.5f); // Normalized speed (adjustable)
        float currentStepDistance = Mathf.Lerp(maxStepDistance, minStepDistance, t);

        
        // Play footstep sound after accumulating a certain distance
        if (accumulatedDistance >= currentStepDistance)
        {
            footstepAudio.PlayOneShot(footstepAudio.clip);
            accumulatedDistance = 0f;
        }

        lastPosition = currentPosition;
    }
}
