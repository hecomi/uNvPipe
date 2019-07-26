using UnityEngine;
using UnityEngine.Assertions;

namespace uNvPipe.Examples
{

public class EncoderToDecoder : MonoBehaviour
{
    [SerializeField]
    uNvPipeEncoder encoder = null;

    [SerializeField]
    uNvPipeDecoder decoder = null;

    void Start()
    {
        Assert.IsNotNull(encoder, "Please set encoder to EncoderToDecoder.");
        Assert.IsNotNull(decoder, "Please set decoder to EncoderToDecoder.");

        if (encoder)
        {
            encoder.onEncoded.AddListener(OnEncoded);
        }
    }

    void OnEncoded(System.IntPtr ptr, int size)
    {
        if (decoder)
        {
            decoder.Decode(ptr, size);
        }
    }
}

}
