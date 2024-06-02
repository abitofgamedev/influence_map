using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class InfluenceRasterMap : InfluenceMapBase
{
    protected struct PointStruct
    {
        public int ID;
        public Transform Point;
        public Color Color;
    }

    protected struct PointShaderStruct
    {
        public int id;
        public Color color;
        public Vector2 position;
        public float radius;
    }

    [SerializeField] GameObject _InfluenceMapPrefab;
    [SerializeField] Vector2 _MapSize;
    [SerializeField] Vector2Int _GridSize;
    [SerializeField] CustomRenderTexture _InfluenceRenderTexture;
    [SerializeField] float _InflucenceAddition;

    private List<PointStruct> _points=new();
    private List<Color> colors;
    private int[] idArray;
    private List<PointShaderStruct> _dataList = new();
    private CustomRenderTextureUpdateZone[] _zones;
    private List<List<List<PointShaderStruct>>> _pointGrid;
    private void Start()
    {
        idArray = new int[3] { 0, 1, 2};

        colors = new()
        {
            new Color(1, 1, 1, 1),
            new Color(1, 0, 0, 1),
            new Color(0, 1, 0, 1),
            new Color(0, 0, 1, 1),
            new Color(0, 1, 1, 1),
            new Color(1, 1, 0, 1),
        };

        GameObject map = Instantiate(_InfluenceMapPrefab, transform);
        map.transform.localPosition = Vector3.zero;
        map.transform.localScale = new Vector3(_MapSize.x, _MapSize.y, 1);

        _zones = new CustomRenderTextureUpdateZone[1];

        _pointGrid = new();
        for(int i = 0; i < _GridSize.x; i++)
        {
            _pointGrid.Add(new List<List<PointShaderStruct>>());
            for(int j = 0; j < _GridSize.y; j++)
            {
                _pointGrid[i].Add(new List<PointShaderStruct>());
            }
        }

        _InfluenceRenderTexture.Initialize();

        _InfluenceRenderTexture.material.SetInt("_IdSize", idArray.Length);
        ComputeBuffer idBuffer = new ComputeBuffer(idArray.Length, sizeof(int) * idArray.Length);
        idBuffer.SetData(idArray);
        _InfluenceRenderTexture.material.SetBuffer("_Ids", idBuffer);
    }

    IEnumerator Coroutine_UpdateShaderZoneByFrame()
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


            //Find coordinates of the point in the range of 0 to 1;
            float xPos = (_points[i].Point.position.x - (transform.position.x - _MapSize.x / 2f)) / _MapSize.x;
            float yPos = (_points[i].Point.position.z - (transform.position.z - _MapSize.y / 2f)) / _MapSize.y;
            float scale = Mathf.Abs(xPos - ((_points[i].Point.position.x + _InflucenceAddition - 
                (transform.position.x - _MapSize.x / 2f)) / _MapSize.x));

            if (x < 0 || x >= _GridSize.x || y < 0 || y >= _GridSize.y)
            {
                continue;
            }

            PointShaderStruct data = new()
            {
                id = _points[i].ID,
                color = _points[i].Color,
                position = new Vector2(xPos, yPos),
                radius = scale
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
                    //Offset to apply so the zone in the RenderTexture is centered;
                    float offsetX = (1f / (_GridSize.x * 2f));
                    float offsetY = (1f / (_GridSize.y * 2f));

                    //Calculate the zone center coordinates of the RenderTexture in range of 0 to 1
                    float zoneX = ((float)(i) / (float)_GridSize.x) + offsetX;
                    float zoneY = ((float)(_GridSize.y - j) / (float)_GridSize.y) - offsetY;

                    float zoneSizeX = 1f / _GridSize.x;
                    float zoneSizeY = 1f / _GridSize.y;

                    var zone = new CustomRenderTextureUpdateZone()
                    {
                        updateZoneSize = new Vector3(zoneSizeX, zoneSizeY, 1),
                        updateZoneCenter = new Vector3(zoneX, zoneY, 1),
                    };
                    _zones[0] = zone;


                    ComputeBuffer buffer = new ComputeBuffer(_dataList.Count, sizeof(float) * 7 + sizeof(int));
                    buffer.SetData(_dataList);

                    _InfluenceRenderTexture.material.SetInt("_BufferSize", _dataList.Count);
                    _InfluenceRenderTexture.material.SetBuffer("_Buffer", buffer);

                    _InfluenceRenderTexture.SetUpdateZones(_zones);
                    _InfluenceRenderTexture.Update();
                    yield return null;
                }
            }
        }

    }

    IEnumerator Coroutine_UpdateShaderZoneByFrameFromOrigin(Transform changedPoint)
    {
        //Zone where the point was changed;
        int originX = (int)(((changedPoint.position.x + _MapSize.x / 2f) / _MapSize.x) * _GridSize.x);
        int originY = (int)(((changedPoint.position.z + _MapSize.y / 2f) / _MapSize.y) * _GridSize.y);

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

            //Find coordinates of the point in the range of 0 to 1;
            float xPos = (_points[i].Point.position.x - (transform.position.x - _MapSize.x / 2f)) / _MapSize.x;
            float yPos = (_points[i].Point.position.z - (transform.position.z - _MapSize.y / 2f)) / _MapSize.y;
            float scale = Mathf.Abs(xPos - ((_points[i].Point.position.x + _InflucenceAddition - 
                (transform.position.x - _MapSize.x / 2f)) / _MapSize.x));

            if (x < 0 || x >= _GridSize.x || y < 0 || y >= _GridSize.y)
            {
                continue;
            }

            PointShaderStruct data = new()
            {
                id = _points[i].ID,
                color = _points[i].Color,
                position = new Vector2(xPos, yPos),
                radius = scale
            };

            _pointGrid[x][y].Add(data);
        }
        //FOR EACH ZONE
        int size = 0;
        int gridSize = _GridSize.x * _GridSize.y;
        while (gridSize > 0)
        {
            for (int i = originX - size; i <= originX + size; i++)
            {
                for (int j = originY - size; j <= originY + size; j++)
                {
                    if (i < 0 || i >= _GridSize.x || j < 0 || j >= _GridSize.y)
                    {
                        continue;
                    }
                    if ((Mathf.Abs(i - originX) != size && Mathf.Abs(j - originY) != size))
                    {
                        continue;
                    }

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
                        //Offset to apply so the zone in the RenderTexture is centered;
                        float offsetX = (1f / (_GridSize.x * 2f));
                        float offsetY = (1f / (_GridSize.y * 2f));

                        //Calculate the zone center coordinates of the RenderTexture in range of 0 to 1
                        float zoneX = ((float)(i) / (float)_GridSize.x) + offsetX;
                        float zoneY = ((float)(_GridSize.y - j) / (float)_GridSize.y) - offsetY;

                        float zoneSizeX = 1f / _GridSize.x;
                        float zoneSizeY = 1f / _GridSize.y;

                        var zone = new CustomRenderTextureUpdateZone()
                        {
                            updateZoneSize = new Vector3(zoneSizeX, zoneSizeY, 1),
                            updateZoneCenter = new Vector3(zoneX, zoneY, 1),
                        };
                        _zones[0] = zone;


                        ComputeBuffer buffer = new ComputeBuffer(_dataList.Count, sizeof(float) * 7 + sizeof(int));
                        buffer.SetData(_dataList);

                        _InfluenceRenderTexture.material.SetInt("_BufferSize", _dataList.Count);
                        _InfluenceRenderTexture.material.SetBuffer("_Buffer", buffer);

                        _InfluenceRenderTexture.SetUpdateZones(_zones);
                        _InfluenceRenderTexture.Update();
                        yield return null;
                    }
                    gridSize--;
                    if (gridSize == 0)
                    {
                        break;
                    }
                }
            }
            size++;
        }

    }
    public override void AddPoint(Transform point, float influence)
    {
        int randIndex = Random.Range(0, idArray.Length);
        PointStruct p = new PointStruct()
        {
            Color = colors[randIndex],
            ID = randIndex,
            Point = point,
        };
        _points.Add(p);
    }

    bool IsInitialized=false;
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
                StopAllCoroutines();
                StartCoroutine(Coroutine_UpdateShaderZoneByFrame());
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
