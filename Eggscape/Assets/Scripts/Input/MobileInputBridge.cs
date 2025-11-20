using UnityEngine;

/// <summary>
/// Lightweight bridge to feed touch/UI input into gameplay scripts.
/// UI buttons or virtual joysticks should call these public methods
/// to set the desired input state each frame.
/// </summary>
public class MobileInputBridge : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField]
    private bool enableMobileInput = true;

    /// <summary>
    /// True when this bridge should drive input (typically on mobile).
    /// </summary>
    public bool UseMobileInput => enableMobileInput && Application.isMobilePlatform;

    /// <summary>
    /// Horizontal movement input (-1 to 1) set by a joystick or buttons.
    /// </summary>
    public float Horizontal { get; private set; }

    /// <summary>
    /// Single-frame flags updated by UI events.
    /// </summary>
    public bool JumpPressedThisFrame { get; private set; }
    public bool JumpReleasedThisFrame { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool AttackPressedThisFrame { get; private set; }
    public bool FastFallPressedThisFrame { get; private set; }

    /// <summary>
    /// Called by UI to set horizontal input. Clamp to [-1, 1] for safety.
    /// </summary>
    public void SetMove(float value)
    {
        Horizontal = Mathf.Clamp(value, -1f, 1f);
    }

    /// <summary>
    /// UI button/joystick "press" for jump.
    /// </summary>
    public void JumpPressed()
    {
        JumpPressedThisFrame = true;
        JumpHeld = true;
    }

    /// <summary>
    /// UI button/joystick "release" for jump.
    /// </summary>
    public void JumpReleased()
    {
        JumpReleasedThisFrame = true;
        JumpHeld = false;
    }

    /// <summary>
    /// UI button "attack".
    /// </summary>
    public void AttackPressed()
    {
        AttackPressedThisFrame = true;
    }

    /// <summary>
    /// UI button "fast fall / down".
    /// </summary>
    public void FastFallPressed()
    {
        FastFallPressedThisFrame = true;
    }

    private void LateUpdate()
    {
        // Clear single-frame flags after consumers have read them.
        JumpPressedThisFrame = false;
        JumpReleasedThisFrame = false;
        AttackPressedThisFrame = false;
        FastFallPressedThisFrame = false;
    }
}
