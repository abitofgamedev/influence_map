using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfluenceVectorMap : InfluenceMapBase
{
    private struct PointStruct
    {
        public int ID;
        public Transform Point;
        public Color Color;
    }

    private struct PointShaderStruct
    {
        public int id;
        public Color color;
        public Vector3 position;
        public float radius;
    }

    [SerializeField] Renderer _InfluenceMapPrefab;
    [SerializeField] Vector2 _MapSize;
    [SerializeField] Vector2Int _GridSize;
    [SerializeField] float _InflucenceAddition;

    protected Renderer[][] _maps;

    private List<PointStruct> _points = new();
    private List<Color> colors;
    private int[] idArray;
    private List<PointShaderStruct> _dataList = new();
    private List<List<List<PointShaderStruct>>> _pointGrid;
    private void Start()
    {
        idArray = new int[3] { 0, 1, 2 };

        colors = new()
        {
            new Color(1, 1, 1, 1),
            new Color(1, 0, 0, 1),
            new Color(0, 1, 0, 1),
            new Color(0, 0, 1, 1),
            new Color(0, 1, 1, 1),
            new Color(1, 1, 0, 1),
        };

        ComputeBuffer idBuffer = new ComputeBuffer(idArray.Length, sizeof(int) * idArray.Length);
        idBuffer.SetData(idArray);

        float subMapSizeX = _MapSize.x / _GridSize.x;
        float subMapSizeY = _MapSize.y / _GridSize.y;
        _maps = new Renderer[_GridSize.x][];
        for (int i = 0; i < _GridSize.x; i++)
        {
            _maps[i] = new Renderer[_GridSize.y];
            for(int j = 0; j < _GridSize.y; j++)
            {
                _maps[i][j]= Instantiate(_InfluenceMapPrefab, transform);
                float xPos = subMapSizeX/2f+ (transform.position.x - _MapSize.x / 2f) + subMapSizeX * i;
                float yPos = subMapSizeY/2f+ (transform.position.z - _MapSize.y / 2f) + subMapSizeY * j;
                _maps[i][j].transform.localPosition =new Vector3(xPos, yPos,0);
                _maps[i][j].transform.localScale = new Vector3(subMapSizeX, subMapSizeY, 1);
                _maps[i][j].material.SetInt("_IdSize", idArray.Length);
                _maps[i][j].material.SetBuffer("_Ids", idBuffer);
            }
        }

        _pointGrid = new();
        for (int i = 0; i < _GridSize.x; i++)
        {
            _pointGrid.Add(new List<List<PointShaderStruct>>());
            for (int j = 0; j < _GridSize.y; j++)
            {
                _pointGrid[i].Add(new List<PointShaderStruct>());
            }
        }
    }

    void UpdateInfluenceMap()
    {
        for (int i = 0; i < _GridSize.x; i++)
        {
            for (int j = 0; j < _GridSize.y; j++)
            {
                _pointGrid[i][j].Clear();
            }
        }
        for (int i = 0; i < _points.Count; i++)
        {
            //Find the coordinates of the zone the point belongs to in the range of O to _GridSize
            int x = (int)(((_points[i].Point.position.x + _MapSize.x / 2f) / _MapSize.x) * _GridSize.x);
            int y = (int)(((_points[i].Point.position.z + _MapSize.y / 2f) / _MapSize.y) * _GridSize.y);

            if (x < 0 || x >= _GridSize.x || y < 0 || y >= _GridSize.y)
            {
                continue;
            }

            PointShaderStruct data = new()
            {
                id = _points[i].ID,
                color = _points[i].Color,
                position = _points[i].Point.position,
                radius = _points[i].Point.localScale.x * _InflucenceAddition
            };

            _pointGrid[x][y].Add(data);
        }

        //FOR EACH ZONE
        for (int i = 0; i < _GridSize.x; i++)
        {
            for (int j = 0; j < _GridSize.y; j++)
            {
                _dataList.Clear();

                //Combine the data from the zone and every one of its neighbours
                for (int p = i - 1; p <= i + 1; p++)
                {
                    for (int q = j - 1; q <= j + 1; q++)
                    {
                        if (p >= 0 && p < _GridSize.x && q >= 0 && q < _GridSize.y)
                        {
                            _dataList.AddRange(_pointGrid[p][q]);
                        }
                    }
                }

                if (_dataList.Count > 0)
                {
                    ComputeBuffer buffer = new ComputeBuffer(_dataList.Count, sizeof(float) * 8 + sizeof(int));
                    buffer.SetData(_dataList);

                    _maps[i][j].material.SetInt("_BufferSize", _dataList.Count);
                    _maps[i][j].material.SetBuffer("_Buffer", buffer);
                }
            }
        }
    }

    public override void AddPoint(Transform point, float influence)
    {
        int randIndex = Random.Range(0, idArray.Length);
        PointStruct p = new PointStruct()
        {
            Color = colors[randIndex],
            ID = idArray[randIndex],
            Point = point,
        };
        _points.Add(p);
    }

    bool IsInitialized = false;
    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }
        foreach (var point in _points)
        {
            if (point.Point.hasChanged)
            {
                UpdateInfluenceMap();
                foreach (var p in _points)
                {
                    p.Point.hasChanged = false;
                }
                break;
            }
        }
    }

    public override void Initialize()
    {
        IsInitialized = true;
    }
}
