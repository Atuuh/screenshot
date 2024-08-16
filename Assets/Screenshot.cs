using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Screenshot : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [Range(0f, 100f)][SerializeField] private float _similarity;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CaptureImage();
            Debug.Log("Wrote screenshot");
        }
    }

    private void CaptureImage()
    {
        var originalBackground = _camera.backgroundColor;
        var originalRT = RenderTexture.active;
        var cameraData = _camera.GetComponent<UniversalAdditionalCameraData>();

        var renderTexture = _camera.activeTexture;
        RenderTexture.active = renderTexture;

        var firstPass = GetPixels(renderTexture, originalBackground);
        var secondPass = GetPixels(renderTexture, RotateColour(originalBackground, 180f));
        var combinedPixels = firstPass.Zip(secondPass, (a, b) =>
        {
            if (a == Color.clear && b == Color.clear)
            {
                return Color.clear;
            }
            else if (a == Color.clear)
            {
                return b;
            }
            else if (b == Color.clear)
            {
                return a;
            }
            else
            {
                return b;
            }
        });
        var texture = new Texture2D(renderTexture.width, renderTexture.height);
        texture.SetPixels(combinedPixels.ToArray());
        SaveImage(texture, "render");

        var image = RTImage(_camera);
        SaveImage(image, "copy");

        _camera.backgroundColor = originalBackground;
        RenderTexture.active = originalRT;
    }

    private void SaveImage(Texture2D texture, string name)
    {
        File.WriteAllBytes($"{Application.dataPath}/{name}.png", texture.EncodeToPNG());
    }

    Texture2D RTImage(Camera camera)
    {
        // The Render Texture in RenderTexture.active is the one
        // that will be read by ReadPixels.
        var currentRT = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;

        // Render the camera's view.
        camera.Render();

        // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
        image.Apply();

        // Replace the original active Render Texture.
        RenderTexture.active = currentRT;
        return image;
    }

    private Color[] GetPixels(RenderTexture renderTexture, Color clearColour)
    {
        _camera.backgroundColor = clearColour;
        _camera.Render();
        var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        var pixels = texture.GetPixels();
        return pixels.Select(p => AreColoursSimilar(p, clearColour) ? Color.clear : p).ToArray();
    }

    private bool AreColoursSimilar(Color c1, Color c2)
    {
        var r = Math.Abs(c1.r - c2.r);
        var g = Math.Abs(c1.g - c2.g);
        var b = Math.Abs(c1.b - c2.b);

        return (r + g + b) <= _similarity;
    }

    private Color RotateColour(Color colour, float degrees)
    {
        Color.RGBToHSV(colour, out var h, out var s, out var v);
        var rotatedHue = degrees / 360f + h % 1f;
        return Color.HSVToRGB(rotatedHue, s, v);
    }
}
