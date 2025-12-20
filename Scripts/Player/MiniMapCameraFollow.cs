using UnityEngine;

public class MiniMapCameraFollow : MonoBehaviour
{
    Vector3 Position;

    private void LateUpdate()
    {
        Position = Player.instance.transform.position;
        Position.y = transform.position.y;
        transform.SetPositionAndRotation(Position, Quaternion.Euler(90f, Player.instance.transform.eulerAngles.y, 0f));
    }
}
