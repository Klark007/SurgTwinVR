using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Linq;

public class SortPoints : MonoBehaviour
{
    public ComputeShader sorting_shader;

    int sorting_kernel;
    int keying_kernel;
    int shuffle_kernel;
    uint thread_group_size;

    ComputeBuffer keyBuffer;

    void Start()
    {
        sorting_kernel = sorting_shader.FindKernel("BitonicSort");
        keying_kernel = sorting_shader.FindKernel("ComputeKeys");
        shuffle_kernel = sorting_shader.FindKernel("ComputeKeys");

        sorting_shader.GetKernelThreadGroupSizes(sorting_kernel, out thread_group_size, out _, out _);
    }

    void dispatch(uint n, uint h)
    {
        sorting_shader.SetInt("h", (int)h);

        sorting_shader.Dispatch(sorting_kernel, (int)MathF.Ceiling((float)n / thread_group_size / 2), 1, 1);
    }
    
    void local_bitonic_sort(uint n)
    {
        sorting_shader.SetInt("mode", 0);
        sorting_shader.Dispatch(sorting_kernel, (int)MathF.Ceiling((float)n / thread_group_size / 2), 1, 1);
    }
    
    void local_crossover(uint n, uint h)
    {
        //Debug.Log("Local crossover:" + h.ToString());
        sorting_shader.SetInt("mode", 1);
        dispatch(n, h);
    }
    
    void global_flip(uint n, uint h)
    {
        //Debug.Log("Global flip:" + h.ToString());
        sorting_shader.SetInt("mode", 2);
        dispatch(n, h);
    }

    void global_crossover(uint n, uint h)
    {
        //Debug.Log("Global crossover:" + h.ToString());
        sorting_shader.SetInt("mode", 3);
        dispatch(n, h);
    }

    public void createKeyBuffer(uint n)
    {
        keyBuffer = new ComputeBuffer((int)n, sizeof(uint));
    }

    public void sortArray(ComputeBuffer pointBuffer, uint n)
    {
        uint next_power = 1;
        while (next_power < n)
        {
            next_power *= 2;
        }

        // setup buffers
        sorting_shader.SetBuffer(sorting_kernel, "values", pointBuffer);
        sorting_shader.SetBuffer(sorting_kernel, "keys", keyBuffer);

        sorting_shader.SetBuffer(keying_kernel, "values", pointBuffer);
        sorting_shader.SetBuffer(keying_kernel, "keys", keyBuffer);

        sorting_shader.SetBuffer(shuffle_kernel, "values", pointBuffer);


        sorting_shader.SetInt("n", (int)n);

        sorting_shader.Dispatch(keying_kernel, (int)MathF.Ceiling((float)n / thread_group_size), 1, 1);

        // actual sort start
        local_bitonic_sort(n);

        for (uint h = thread_group_size * 4; h <= next_power; h *= 2)
        {
            global_flip(next_power, h);

            for (uint hh = h / 2; hh > 1; hh /= 2)
            {
                if (hh <= thread_group_size * 2)
                {
                    local_crossover(next_power, hh);
                }
                else
                {
                    global_crossover(next_power, hh);
                }
            }
        }
        // actual sort end

        // shuffle
        // sorting_shader.Dispatch(shuffle_kernel, (int)MathF.Ceiling((float)n / 8 / 2), 1, 1);
    }

    void OnDestroy()
    {
        keyBuffer.Dispose();
    }
}
