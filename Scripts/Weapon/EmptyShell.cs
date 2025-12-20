using UnityEngine;

public class EmptyShell : MonoBehaviour
{
    [SerializeField] AudioSource sound;
    [SerializeField] AudioClip shellSoundClip;


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 0.1f)
        {
            sound.PlayOneShot(shellSoundClip);
            Invoke(nameof(DeactivateEmptyShell), 3);
        }
    }

    void DeactivateEmptyShell()
    {
        gameObject.SetActive(false);
    }


}
