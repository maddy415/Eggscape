using UnityEngine;
using System.IO;

public class TMPToImage : MonoBehaviour
{
    public Camera captureCamera;
    public RenderTexture renderTexture;
    public string savePath = "Assets/TMPImage.png";

    public void CaptureText()
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        captureCamera.Render();

        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);

        RenderTexture.active = currentRT;
        Debug.Log("Texto salvo em PNG em: " + savePath);
    }
}