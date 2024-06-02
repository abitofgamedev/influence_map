using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleScript : MonoBehaviour
{
    [SerializeField] InfluenceMapBase _InfluenceMap;
    [SerializeField] int _PointCount;
    [SerializeField] int _MapSize;
    [SerializeField] GameObject _PointPrefab;
    private void Start()
    {
        for(int i = 0; i < _PointCount; i++)
        {
            Transform point = Instantiate(_PointPrefab).transform;
            point.position = new Vector3(Random.Range(-_MapSize / 2f, _MapSize / 2f), 0, Random.Range(-_MapSize / 2f, _MapSize / 2f));
            _InfluenceMap.AddPoint(point, 400);
        }
        _InfluenceMap.Initialize();
    }
}
