using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class PrefabFocusManager : MonoBehaviour
{
    public float rayDistance = 50f;         // Customizable detection distance
    public LayerMask prefabLayer;           // Assigned in Inspector (should match prefab root objects)

    private IFocusablePrefab currentFocused;
    private IFocusablePrefab[] allPrefabs;

    void Start()
    {
        // ✔️ Caches all prefabs in the scene that implement IFocusablePrefab
        allPrefabs = FindObjectsOfType<MonoBehaviour>().OfType<IFocusablePrefab>().ToArray();
    }

    void Update()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, prefabLayer))
        {
            IFocusablePrefab hitPrefab = hit.collider.GetComponentInParent<MonoBehaviour>() as IFocusablePrefab;

            if (hitPrefab != null)
            {
                // ✔️ Show only the prefab we are looking at
                foreach (var prefab in allPrefabs)
                    prefab.SetDescriptionVisible(prefab.ObjectCode == hitPrefab.ObjectCode);

                currentFocused = hitPrefab;
            }
        }
        else
        {
            // ✔️ Hide all labels if nothing is focused
            foreach (var prefab in allPrefabs)
                prefab.SetDescriptionVisible(true);

            currentFocused = null;
        }
    }
}



