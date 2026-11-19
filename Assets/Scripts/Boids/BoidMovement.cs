
using UnityEngine;

public class BoidMovement : MonoBehaviour
{
    private Vector3 velocity;

    private float maxSpeed;

    public Vector3 Velocity => velocity;
    public ref Vector3 GetVelocityReference()
    {
        return ref velocity;
    }

    public void Initialize(float speed)
    {
        maxSpeed = speed;
        InitializeMovement();
    }

    public void InitializeMovement()
    {
        Vector3 direction = Random.insideUnitSphere;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;

        velocity = direction.normalized * maxSpeed;
    }

    public void ApplySteering(Vector3 steering)
    {
        velocity += steering * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        transform.position += velocity * Time.deltaTime;

        transform.position = new Vector3(
            transform.position.x,
            1f,
            transform.position.z
        );

        if (velocity.sqrMagnitude > 0.001f)
            transform.forward = velocity.normalized;
    }

    public void StopMovement()
    {
        velocity = Vector3.zero;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = new Vector3(
            position.x,
            1f,
            position.z
        );
    }
}

