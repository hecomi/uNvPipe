using UnityEngine;
using UnityEngine.Assertions;
using System.IO;
using System.Runtime.InteropServices;

namespace uNvPipe.Examples
{

public class uNvPipeRenderTextureEncoder : MonoBehaviour
{
    [SerializeField]
    uNvPipeEncoder encoder = null;

    [SerializeField]
    bool forceIframe = false;

    [SerializeField]
    RenderTexture texture = null;

    [SerializeField]
    bool outputToFile = false;

    [SerializeField]
    string filePath = "test.h264";

    Texture2D texture2d_;
    float t_ = 0f;

    FileStream fileStream_;
    BinaryWriter binaryWriter_;

    void Start()
    {
        Assert.IsNotNull(encoder, "Please set encoder.");
        Assert.IsNotNull(texture, "Please set texture.");

        if (outputToFile)
        {
            fileStream_ = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            binaryWriter_ = new BinaryWriter(fileStream_);
        }

        if (encoder)
        {
            encoder.onEncoded.AddListener(OnEncoded);
        }

        if (texture)
        {
            texture2d_ = new Texture2D(
                texture.width,
                texture.height,
                TextureFormat.RGBA32,
                false,
                false);
        }
    }

    void OnApplicationQuit()
    {
        if (binaryWriter_ != null) 
        {
            binaryWriter_.Close();
        }

        if (fileStream_ != null) 
        {
            fileStream_.Close();
        }
    }

    void Update()
    {
        if (encoder.fps == Application.targetFrameRate)
        {
            Encode();
        }
        else
        {
            var T = 1f / encoder.fps;
            t_ += Time.deltaTime;

            if (t_ >= T)
            {
                t_ -= T;
                Encode();
            }
        }
    }

    void Encode()
    {
        if (!texture || !encoder) return;

		var activeRenderTexture = RenderTexture.active;
		RenderTexture.active = texture;

        var area = new Rect(0f, 0f, texture2d_.width, texture2d_.height);
		texture2d_.ReadPixels(area, 0, 0);
		texture2d_.Apply();

		RenderTexture.active = activeRenderTexture;

        encoder.Encode(texture2d_, forceIframe);
    }

    void OnEncoded(System.IntPtr ptr, int size)
    {
        if (!outputToFile) return;

        var bytes = new byte[size];
        Marshal.Copy(ptr, bytes, 0, size);
        binaryWriter_.Write(bytes);
    }
}

}
