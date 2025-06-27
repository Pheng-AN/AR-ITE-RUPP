using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFocusablePrefab
{
    string ObjectCode { get; }
    void SetDescriptionVisible(bool visible);
}
