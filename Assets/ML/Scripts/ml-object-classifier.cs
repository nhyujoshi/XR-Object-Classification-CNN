using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.Linq;
using Unity.MLAgents.Sensors;

public class MLObjectClassifier : MonoBehaviour
{
    // Reference to the model asset
    public ModelAsset modelAsset;

    // Reference to our object-word database
    public ObjectWordModel objectWordDatabase;

    // List of possible class names (should match your training classes)
    public List<string> classNames = new List<string>()
    {
        "pineapple", "apple", "ear", "pan",
        "pen", "pie", "pine", "pear"
    };

    private Worker worker;
    private Model runtimeModel;
    private Dictionary<string, string> objectToWordMap;

    private void Awake()
    {
        // Initialize the model
        if (modelAsset != null)
        {
            runtimeModel = ModelLoader.Load(modelAsset);
            worker = new Worker(runtimeModel, BackendType.CPU);
        }
        else
        {
            Debug.LogError("No model asset assigned!");
        }

        // Initialize the object to word mapping
        if (objectWordDatabase != null)
        {
            objectWordDatabase.InitializeDictionary();
        }
        else
        {
            Debug.LogError("No object word database assigned!");
        }
    }

    public string ClassifyObject(Texture2D objectImage)
    {
        if (worker == null)
        {
            Debug.LogError("ML worker not initialized!");
            return null;
        }

        try
        {
            // Preprocess the image
            Texture2D processedImage = PreprocessImage(objectImage);

            // Get input name
            string inputName = runtimeModel.inputs[0].name;

            // Get raw pixel data
            Color[] pixels = processedImage.GetPixels();

            // Create a tensor with the EXACT shape our model expects
            var shape = new TensorShape(1, 224, 224, 3); // NHWC format
            var tensor = new Tensor<float>(shape);

            // Get tensor array for direct manipulation
            float[] tensorData = new float[1 * 224 * 224 * 3];

            // Fill with pixel data in NHWC format
            for (int h = 0; h < 224; h++)
            {
                for (int w = 0; w < 224; w++)
                {
                    Color pixel = pixels[h * 224 + w];

                    // Calculate base index in NHWC format [batch, height, width, channel]
                    int baseIdx = ((0 * 224) + h) * 224 * 3 + w * 3;

                    // Fill R, G, B channels
                    tensorData[baseIdx + 0] = pixel.r;
                    tensorData[baseIdx + 1] = pixel.g;
                    tensorData[baseIdx + 2] = pixel.b;
                }
            }

            // Upload the data
            tensor.Upload(tensorData);

            // Set as input
            worker.SetInput(inputName, tensor);

            // Execute
            worker.Schedule();

            // Get output
            using (var output = worker.PeekOutput() as Tensor<float>)
            {
                // Get the class with the highest probability
                float[] probabilities = output.DownloadToArray();
                int classIndex = GetHighestProbabilityIndex(probabilities);

                if (classIndex < classNames.Count)
                {
                    string objectName = classNames[classIndex];
                    Debug.Log($"Classified object as: {objectName}");
                    return objectName;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during classification: {e.Message}");
        }

        return null;
    }

    private Texture2D PreprocessImage(Texture2D originalImage)
    {
        // Create a target texture with the dimensions expected by the model (224x224 is common)
        Texture2D resizedImage = new Texture2D(224, 224, TextureFormat.RGB24, false);

        // Create a temporary RenderTexture for resizing
        RenderTexture rt = RenderTexture.GetTemporary(224, 224);

        // Copy the original image to the temporary RenderTexture with scaling
        Graphics.Blit(originalImage, rt);

        // Store the active RenderTexture and set it to our temporary one
        RenderTexture previousRT = RenderTexture.active;
        RenderTexture.active = rt;

        // Read the pixels from the RenderTexture to our target texture
        resizedImage.ReadPixels(new Rect(0, 0, 224, 224), 0, 0);
        resizedImage.Apply();

        // Restore the active RenderTexture
        RenderTexture.active = previousRT;
        RenderTexture.ReleaseTemporary(rt);

        return resizedImage;
    }

    private int GetHighestProbabilityIndex(float[] probabilities)
    {
        int maxIndex = 0;
        float maxValue = probabilities[0];

        for (int i = 1; i < probabilities.Length; i++)
        {
            if (probabilities[i] > maxValue)
            {
                maxValue = probabilities[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    // Get the corresponding word for an object
    public string GetDecompositionWord(string objectName)
    {
        if (objectWordDatabase != null)
        {
            return objectWordDatabase.GetWordForObject(objectName);
        }
        return null;
    }

    private void OnDestroy()
    {
        // Clean up resources
        if (worker != null)
        {
            worker.Dispose();
        }
    }
}