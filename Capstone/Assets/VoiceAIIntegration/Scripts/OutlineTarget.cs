using UnityEngine;

public class OutlineTarget : MonoBehaviour
{
    private Outline outline;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
    }

    public void Highlight(float duration = 3f)
    {
        if (outline == null) return;

        outline.enabled = true;
        CancelInvoke();
        Invoke(nameof(Disable), duration);
    }

    private void Disable()
    {
        if (outline != null)
            outline.enabled = false;
    }
}
