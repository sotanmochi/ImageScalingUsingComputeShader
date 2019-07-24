// Copyright (c) 2019 Soichiro Sugimoto
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEditor;
using UnityEngine;

public class ImageScaling : MonoBehaviour
{
    public enum InterpolationType
    {
        Bilinear,
        Lanczos
    }

    [SerializeField] ComputeShader _computeShader;

    [SerializeField] Texture2D _srcImage;
    [SerializeField] float _upsampleScale = 2;
    [SerializeField] InterpolationType _type = InterpolationType.Lanczos;

    [SerializeField] Material _inputImageMaterial;
    [SerializeField] Material _outputImageMaterial;

    Vector2Int _gpuThreads = new Vector2Int(16, 16);

    RenderTexture _dstRT;

    void Start()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("Compute Shader is not Support!!");
            return;
        }
        if (_computeShader == null)
        {
            Debug.LogError("Compute Shader has not been assigned!!");
            return;
        }

        Initialize();

        InvokeResize();
        _inputImageMaterial.mainTexture = _srcImage;
        _outputImageMaterial.mainTexture = _dstRT;

        SaveAsPng(_dstRT);
    }

    void Initialize()
    {
        int width = (int)(_upsampleScale * _srcImage.width);
        int height = (int)(_upsampleScale * _srcImage.height);

        _dstRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _dstRT.enableRandomWrite = true;
        _dstRT.Create();
    }

    void InvokeResize()
    {
        int srcWidth = _srcImage.width;
        int srcHeight = _srcImage.height;
        int dstWidth = _dstRT.width;
        int dstHeight = _dstRT.height;

        if(_type == InterpolationType.Bilinear)
        {
            int kernelID = _computeShader.FindKernel("ResizeBilinear");
            _computeShader.SetInt("_DstWidth", dstWidth);
            _computeShader.SetInt("_DstHeight", dstHeight);
            _computeShader.SetTexture(kernelID, "_SrcImage", _srcImage);
            _computeShader.SetTexture(kernelID, "_PixelValueWrite", _dstRT);
            _computeShader.Dispatch(kernelID, Mathf.CeilToInt((float)dstWidth / _gpuThreads.x), 
                                            Mathf.CeilToInt((float)dstHeight / _gpuThreads.y), 1);
        }
        else if(_type == InterpolationType.Lanczos)
        {
            int kernelID = _computeShader.FindKernel("ResizeLanczos");
            _computeShader.SetInt("_SrcWidth", srcWidth);
            _computeShader.SetInt("_SrcHeight", srcHeight);
            _computeShader.SetInt("_DstWidth", dstWidth);
            _computeShader.SetInt("_DstHeight", dstHeight);
            _computeShader.SetTexture(kernelID, "_SrcImage", _srcImage);
            _computeShader.SetTexture(kernelID, "_PixelValueWrite", _dstRT);
            _computeShader.Dispatch(kernelID, Mathf.CeilToInt((float)dstWidth / _gpuThreads.x), 
                                            Mathf.CeilToInt((float)dstHeight / _gpuThreads.y), 1);
        }
    }

    void SaveAsPng(RenderTexture rt)
    {
        RenderTexture.active = rt;

        Texture2D tex2d = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, false);
        tex2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2d.Apply();

        RenderTexture.active = null;

        var pngImage = tex2d.EncodeToPNG();
        string filePath = EditorUtility.SaveFilePanel("Save Texture", "", "rt.png", "png");
        if (filePath.Length > 0)
        {
            File.WriteAllBytes(filePath, pngImage);
        }
    }
}
