using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasFocusManager : MonoBehaviour
{
    public float rayDistance = 30f;
    public LayerMask labelLayer;

    private CanvasGroupLabelFader currentLabel;

    void Update()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, labelLayer))
        {
            CanvasGroupLabelFader label = hit.collider.GetComponentInParent<CanvasGroupLabelFader>();

            if (label != null && label != currentLabel)
            {
                if (currentLabel != null)
                    currentLabel.FadeOut();

                currentLabel = label;
                currentLabel.FadeIn();
            }
        }
        else
        {
            if (currentLabel != null)
            {
                currentLabel.FadeOut();
                currentLabel = null;
            }
        }
    }
}


