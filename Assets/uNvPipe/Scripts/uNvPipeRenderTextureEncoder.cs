using UnityEngine;
using UnityEngine.Assertions;

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

    Texture2D texture2d_;
    float t_ = 0f;

    void Start()
    {
        InitTexture();
        InitEncoder();
    }

    void InitEncoder()
    {
        Assert.IsNotNull(encoder, "Please set encoder.");
    }

    void InitTexture()
    {
        Assert.IsNotNull(texture, "Please set texture.");

        if (!texture) return;
        
        texture2d_ = new Texture2D(
            texture.width,
            texture.height,
            TextureFormat.RGBA32,
            false,
            false);
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
}

}
