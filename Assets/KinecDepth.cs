using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Linq;
using System;
using System.Threading;

public class KinecDepth : MonoBehaviour
{

    private KinectSensor _Sensor;
    private DepthFrameReader _Reader;
    private ushort[] _Data;
    System.IO.StreamWriter logFile;
    Windows.Kinect.CoordinateMapper mapper;

    bool newData = false;
    float[,] data;
    float minDist, maxDist, scale;
    TerrainCollider terrCollider;
    TerrainData terrData;
    // Use this for initialization
    void Start()
    {
        terrCollider = GetComponent<TerrainCollider>();
        terrData = terrCollider.terrainData;
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.DepthFrameSource.OpenReader();
            _Reader.FrameArrived += _Reader_FrameArrived;
            _Data = new ushort[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels];
            Debug.LogFormat("Depth:H:{0} x W:{1}", _Sensor.DepthFrameSource.FrameDescription.Height, _Sensor.DepthFrameSource.FrameDescription.Width);
            Debug.LogFormat("TerrData:H:{0} x W:{1}", terrData.heightmapHeight, terrData.heightmapWidth);
            minDist = _Sensor.DepthFrameSource.DepthMinReliableDistance;
            maxDist = 1500f;// _Sensor.DepthFrameSource.DepthMaxReliableDistance;
            scale = 0.1f / (maxDist - minDist);
            Debug.LogFormat("Depth:Min:{0} x Max:{1} (Scale:{2})", minDist, maxDist, scale);
            mapper = _Sensor.CoordinateMapper;
            _Sensor.Open();
        }
        logFile = new System.IO.StreamWriter(new System.IO.FileStream("e:\\log.txt", System.IO.FileMode.OpenOrCreate));
        Debug.LogFormat("Scale:{0}", terrData.heightmapScale);
        data = new float[terrData.heightmapHeight, terrData.heightmapWidth];
    }

    private void _Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
    {
        using (var frame = e.FrameReference.AcquireFrame())
        {
            frame.CopyFrameDataToArray(_Data);
        }

        if (newData) return;
        Debug.Log("Frame:" + Thread.CurrentThread.ManagedThreadId);
        BuildTerrain();
        //new Thread(new ThreadStart(BuildTerrain)).Start();
        //ThreadPool.QueueUserWorkItem(BuildTerrain);
    }

    private void BuildTerrain()
    {
        Debug.Log("Build1:" + Thread.CurrentThread.ManagedThreadId);
        for (int i = 0; i < _Data.Length; i++)
        {
            var curPnt = _Data[i];
            //for (int x = 0; x < _Sensor.DepthFrameSource.FrameDescription.Width; x++)
            //for (int x = 200; x < 300; x++)
            //{
            //for (int y = 0; y < _Sensor.DepthFrameSource.FrameDescription.Height; y++)
            //for (int y = 200; y < 300; y++)
            //{
            //var curVal = (float)_Data[x * y + y];

           // logFile.Write(String.Format("x:{0} y:{1} ", curPnt.X, curPnt.Y));
            var curVal = (float)_Data[i];
            logFile.Write("||" + curVal);
            curVal = curVal > minDist ? curVal : minDist;
            logFile.Write("||" + curVal);
            curVal = curVal > maxDist ? minDist : curVal;
            logFile.Write("||" + curVal);
            curVal -= minDist;
            logFile.Write("||" + curVal);
            curVal *= scale;
            logFile.WriteLine("||" + curVal);
            //logFile.WriteLine(curVal);
            //data[curPnt.X, curPnt.Y] = curVal;
            //}
        }
        newData = true;
        Debug.LogError("FileReady");
    }

    // Update is called once per frame
    void Update()
    {
        if (_Sensor == null || !_Sensor.IsOpen)
        {
            Debug.LogError("Kinect not ready");
            return;
        }
        if (!newData)
        {
            return;
        }
        Debug.Log("Update:" + Thread.CurrentThread.ManagedThreadId);
        terrData.SetHeights(0, 0, data);
        newData = false;
    }

    void OnApplicationQuit()
    {
        if (logFile != null) {
            logFile.Close();
        }
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}
