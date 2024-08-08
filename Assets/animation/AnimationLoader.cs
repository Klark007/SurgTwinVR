using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;


/*
 * AnimationLoader that loads the point cloud data using the timing information provided per JSON file
 */
public class AnimationLoader : MonoBehaviour
{
    static Encoding encA = Encoding.ASCII;

    public bool animationActive; // used to hide the animation via button press
    bool animationPaused;

    string[] files; // stores ply file paths

    const uint start_time_us = 138334544;
    uint time_us = start_time_us;
    Dictionary<string, uint> timingDict;
    public string time_key;

    uint frame = 0; // current index into timing dict / files
    uint async_frame = uint.MaxValue; // frame being loaded asynchronously at the moment
    public bool updated_data; // have we updated our point cloud data 

    Task async_read_task;

    const uint sizePointData = 23000; // should be 23000 for all points;
    Point[] pointData; // potentially switch to NativeArray
    public int maxNrPoints = 10000; // for downsampling, only ever read this many points
    public int nrPoints; // current number of points loaded

    public ComputeBuffer pointBuffer;

    // resolution of temporary texture
    public int res_x = 256*4;
    public int res_y = 256*4;
    int size_rt;
    public ComputeBuffer custom_rt_left;
    public ComputeBuffer custom_rt_right;

    Setup setupScript;

    int qty = 0;

    void Start()
    {
        string jsonString = File.ReadAllText("Assets/animation/timings_000156211512.json");
        timingDict = JsonConvert.DeserializeObject<Dictionary<string, uint>>(jsonString);

        files = Directory.GetFiles("Assets/pointclouds/animation", "*.ply");

        pointData = new Point[sizePointData];
        pointBuffer = new ComputeBuffer((int) sizePointData, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Point)));

        size_rt = res_x * res_y;
        custom_rt_left = new ComputeBuffer(size_rt, sizeof(uint));
        custom_rt_right = new ComputeBuffer(size_rt, sizeof(uint));

        animationActive = false;
        animationPaused = false;

        setupScript = FindObjectOfType<Setup>();
    }

    float point_updates = 0;

    void updateFrame()
    {
        uint dt_us = (uint)(Time.deltaTime * 1_000_000);
        time_us += dt_us;

        // find frame that fits
        while (frame < (uint)files.Length && time_us > timingDict.ElementAt((int)frame).Value)
        {
            frame++;
        }

        // next frame that fits is at beginning
        if (frame >= (uint)(files.Length))
        {
            // want a small break before reseting 
            time_us = start_time_us - 2500000;
            frame = 0;
            point_updates = 0;

            while (frame < (uint)files.Length && time_us > timingDict.ElementAt((int)frame).Value)
            {
                frame++;
            }

            Debug.Assert(!(frame >= (uint)(files.Length)));
        }

        time_key = timingDict.ElementAt((int)frame).Key;
    }

    private void LateUpdate()
    {
        OVRInput.Update();
    }

    async void Update()
    {
        if (setupScript.videoState == Setup.VideoState.Unloaded && OVRInput.GetDown(OVRInput.Button.Two))
        {
            animationActive = !animationActive;
        }

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            animationPaused = !animationPaused;
        }

        if (animationActive)
        {
            ++qty;

            uint old_frame = frame;
            if (!animationPaused)
            {
                updateFrame();
            }

            Profiler.BeginSample("Better read file");
            updated_data = frame != old_frame;
            if (updated_data)
            {
                if (frame == async_frame)
                {
                    await async_read_task;
                }
                else
                {
                    readFile(files[frame]);
                }

                pointBuffer.SetData(pointData);
            }
            Profiler.EndSample();

            point_updates = (point_updates * (qty - 1) + (frame - old_frame)) / qty;
            if (qty % 30 == 0)
            {
                Debug.Log(point_updates);
            }

            async_frame = (frame + 1) % ((uint)files.Length);
            async_read_task = asyncReadFile(async_frame);
        }
    }

    void OnDestroy()
    {
        pointBuffer.Release();
        custom_rt_left.Release();
        custom_rt_right.Release();
    }

    private Task asyncReadFile(uint frame)
    {
        return Task.Run(() =>
        {
            readFile(files[frame]);
        });
    }

    private void readFile(string path)
    {
        // read files: if issues https://stackoverflow.com/questions/4246392/c-sharp-loading-binary-files
        //Profiler.BeginSample("Read file");
        byte[] bytesFile = File.ReadAllBytes(path);
        //Profiler.EndSample();

        int head = 0;

        string line0 = readLineString(ref bytesFile, ref head);
        Debug.Assert(line0 == "ply");

        // should also support format ascii 1.0 and format binary_big_endian 1.0
        string line1 = readLineString(ref bytesFile, ref head);
        Debug.Assert(line1 == "format binary_little_endian 1.0");

        // check header
        bool foundHeader = false;
        nrPoints = -1;
        PropertyFlags propertyFlags = new PropertyFlags();
        while ((char)bytesFile[head] != 3 && !foundHeader)
        {
            string line = readLineString(ref bytesFile, ref head);
            switch (firstWord(line))
            {
                case "comment":
                    break;
                case "element":
                    string[] line_split = line.Split();
                    Debug.Assert(line_split[1] == "vertex"); // expect vertices
                    nrPoints = int.Parse(line_split[2]);
                    break;
                case "property":
                    checkProperty(line, ref propertyFlags);
                    break;
                case "end_header":
                    foundHeader = true;
                    break;
                default:
                    Debug.LogError("File header unknown line:" + line);
                    break;
            }
        }

        Debug.Assert(propertyFlags.check());
        Debug.Assert(nrPoints != -1);

        nrPoints = Math.Min((int) sizePointData, Math.Min(nrPoints, maxNrPoints)); // for performance reasons
        

        // read points into file
        //Profiler.BeginSample("Parse file");

        int start = head;
        Parallel.For(0, nrPoints, i =>
        {
            Point p = readLineBinary(ref bytesFile, start + i * (sizeof(double) * 3 + 3));
            pointData[i] = p;
        });

        //Profiler.EndSample();
    }

    private void checkProperty(string property, ref PropertyFlags flags)
    {
        // we only care about position and color, we assume the format from open3d
        string[] line_split = property.Split();
        Debug.Assert(line_split[1] == "double" || line_split[1] == "uchar");

        switch (line_split[2])
        {
            case "x":
                flags.hasX = true;
                break;
            case "y":
                flags.hasY = true;
                break;
            case "z":
                flags.hasZ = true;
                break;
            case "red":
                flags.hasR = true;
                break;
            case "green":
                flags.hasG = true;
                break;
            case "blue":
                flags.hasB = true;
                break;
        }
    }

    private string firstWord(string line)
    {
        return line.Split()[0];
    }

    private string readLineString(ref byte[] bytesFile, ref int head)
    {
        int start = head;
        while (((char)bytesFile[head] != '\n') && ((char)bytesFile[head] != 3))
        {
            head++;
        }
        int end = head;
        if ((char)bytesFile[head] != 3)
        {
            head++;
        }

        return encA.GetString(bytesFile, start, end - start);
    }

    private Point readLineBinary(ref byte[] bytesFile, int head)
    {
        int start = head;

        // narowing conversion
        float x = (float)BitConverter.ToDouble(bytesFile, start);
        float y = (float)BitConverter.ToDouble(bytesFile, start + sizeof(double));
        float z = (float)BitConverter.ToDouble(bytesFile, start + 2 * sizeof(double));
        Vector3 pos = new Vector3(x, y, z);

        // skip 3 doubles (position) to get the color attributes
        // throw all 3 color attributes into a single int 
        int colorOffset = sizeof(double) * 3;
        byte r = bytesFile[start + colorOffset];
        byte g = bytesFile[start + colorOffset + 1];
        byte b = bytesFile[start + colorOffset + 2];

        return new Point(pos, r, g, b);
    }

}
struct PropertyFlags
{
    public bool hasX;
    public bool hasY;
    public bool hasZ;
    public bool hasR;
    public bool hasG;
    public bool hasB;

    public bool check()
    {
        return hasX && hasY && hasZ && hasR && hasG && hasB;
    }
}
