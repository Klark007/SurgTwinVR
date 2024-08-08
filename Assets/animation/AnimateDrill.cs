using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using Newtonsoft.Json;


public class AnimateDrill : MonoBehaviour
{
    public AnimationLoader animLoader; // provides time key for animation
    public GameObject obj1;
    public GameObject obj2;
    public GameObject obj3;
    public GameObject obj4;

    // https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/#post-1830992
    public static Vector3 ExtractTranslationFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 translate;
        translate.x = matrix.m03;
        translate.y = matrix.m13;
        translate.z = matrix.m23;
        return translate;
    }

    public static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public static Vector3 ExtractScaleFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }
    
    void SetTransformFromMatrix(ref Matrix4x4 matrix)
    {
        // Transform: scale by 0.001, flip about x axis and rotate by 90 deg around x axis, same as the object matrix while rendering the point cloud
        Vector3 pos = ExtractTranslationFromMatrix(ref matrix) * 0.001F;
        transform.position = new Vector3(-pos.x, pos.y, pos.z); // multiply x by -1 and rotate around x by -90 deg

        Vector4 center = matrix * new Vector4(0, 0, 0, 1);
        Vector3 point = new Vector3(-center.x, center.y, center.z) * 0.001F;

        Vector4 t2 = matrix * new Vector4(0, 0, -1, 1);
        Vector3 p2 = Vector3.Normalize(new Vector3(-t2.x, t2.y, t2.z) * 0.001F - point);

        Vector4 t3 = matrix * new Vector4(0, 1, 0, 1);
        Vector3 p3 = Vector3.Normalize(new Vector3(-t3.x, t3.y, t3.z) * 0.001F - point);

        Quaternion rotation = Quaternion.LookRotation(p2, p3);
        transform.rotation = rotation * Quaternion.Euler(90, 0, 0);

        Vector3 scale = ExtractScaleFromMatrix(ref matrix);
        transform.localScale = new Vector3(-scale.x, -scale.y, scale.z) * 0.001F;
    }

    Dictionary<string, Matrix4x4> matrixDict;

    void Start()
    {   
        string jsonString = File.ReadAllText("Assets/animation/tool_poses_000156211512.json");

        Dictionary<string, List<List<float>>> floatArrayDict = JsonConvert.DeserializeObject<Dictionary<string,List<List<float>>>>(jsonString);
        matrixDict = new Dictionary<string, Matrix4x4>();

        // create proper unity matrices
        foreach (KeyValuePair<string, List<List<float>>> entry in  floatArrayDict)
        {
            Matrix4x4 mat = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    mat[i,j] = entry.Value[i][j]; // maybe need to transpose
                }
            }
            matrixDict[entry.Key] = mat;
        }
    }

    // LateUpdate is called once per frame and guaranteed after Update of AnimationLoader
    void LateUpdate()
    {
        if (matrixDict.ContainsKey(animLoader.time_key) && animLoader.animationActive)
        {
            if (animLoader.updated_data)
            {
                GetComponent<Renderer>().enabled = true;
                Matrix4x4 mat = matrixDict[animLoader.time_key];

                Quaternion rot = Quaternion.Euler(-90, 0, 0);
                Matrix4x4 m_rot = Matrix4x4.Rotate(rot);

                mat = m_rot * mat;

                SetTransformFromMatrix(ref mat);
            }

            /*
            // enable for showing coordinate frame
            Vector4 t1 = mat * new Vector4(0, 0, 0, 1);
            obj1.transform.position = new Vector3(-t1.x,t1.y,t1.z) * 0.001F;

            Vector4 t2 = mat * new Vector4(0, 0, -1000, 1);
            obj2.transform.position = new Vector3(-t2.x,t2.y,t2.z) * 0.001F;

            Vector4 t3 = mat * new Vector4(0, 1000, 0, 1);
            obj3.transform.position = new Vector3(-t3.x,t3.y,t3.z) * 0.001F;

            Vector4 t4 = mat * new Vector4(1000, 0, 0, 1);
            obj4.transform.position = new Vector3(-t4.x, t4.y, t4.z) * 0.001F;
            */
        } else
        {
            GetComponent<Renderer>().enabled = false;
        }
    }
}
