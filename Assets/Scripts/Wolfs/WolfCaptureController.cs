
using UnityEngine;

public class WolfCaptureController : MonoBehaviour
{
    public void CaptureHunter(HunterAgent hunter)
    {
        if (hunter == null)
            return;

        Debug.Log(
            $"[Wolf] {gameObject.name} capturó al Hunter."
        );

        hunter.KillHunter();
    }
}
