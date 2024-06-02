using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InfluenceMapBase : MonoBehaviour
{
    public abstract void AddPoint(Transform point,float influence);
    public abstract void Initialize();
}
