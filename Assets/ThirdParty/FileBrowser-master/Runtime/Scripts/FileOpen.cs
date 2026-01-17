using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using UnityEngine.Events;

#if !UNITY_EDITOR && UNITY_WEBGL
using Netherlands3D.JavascriptConnection;
#endif

public class FileOpen : MonoBehaviour
{
    private Button button;

    [DllImport("__Internal")]
    [UsedImplicitly]
    private static extern void BrowseForFile(string inputFieldName);

    [Tooltip("Allowed file input selections")] [SerializeField]
    private string fileExtentions = "csv";

    [Tooltip("Allowed selection multiple files")] [SerializeField]
    private bool multiSelect = false;

    public UnityEvent<string> onFilesSelected = new();

#if !UNITY_EDITOR && UNITY_WEBGL
    private string fileInputName;
    private FileInputIndexedDB javaScriptFileInputHandler;
#endif

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        javaScriptFileInputHandler = FindObjectOfType<FileInputIndexedDB>(true);
        if (javaScriptFileInputHandler == null)
        {
            GameObject go = new GameObject("UserFileUploads");
            javaScriptFileInputHandler = go.AddComponent<FileInputIndexedDB>();
        }

        // Set file input name with generated id to avoid html conflicts
        fileInputName += "_" + gameObject.GetInstanceID();
        name = fileInputName;

        DrawHTMLOverCanvas javascriptInput = gameObject.AddComponent<DrawHTMLOverCanvas>();
        javascriptInput.SetupInput(fileInputName, fileExtentions, multiSelect);

        // if button is null, no visual element is attached and we should prevent the DrawHTMLOverCanvas from actually
        // drawing something over the whole canvas. We still need the HTML input element though as that triggers the
        // file upload dialog in `OpenFile()`
        javascriptInput.AlignObjectID(fileInputName, button != null);
#else
        if (button) button.onClick.AddListener(OpenFile);
#endif
    }

#if !UNITY_EDITOR && UNITY_WEBGL
    public void ClickNativeButton()
    {
        javaScriptFileInputHandler.SetCallbackAddress(SendResults);
    }
#endif

    /// <summary>
    /// Opens the File browser to pick a file to import
    /// </summary>
    public void OpenFile()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        BrowseForFile("_" + gameObject.GetInstanceID());
#else
        string[] fileExtentionNames = fileExtentions.Split(',');
        ExtensionFilter[] extentionfilters = new ExtensionFilter[1];

        extentionfilters[0] = new ExtensionFilter(fileExtentionNames[0], fileExtentionNames);

        string[] filenames = SFB.StandaloneFileBrowser.OpenFilePanel("select file(s)", "", extentionfilters, multiSelect);
        string resultingFiles = "";
        for (int i = 0; i < filenames.Length; i++)
        {
            string destinationFolder = Application.persistentDataPath;
            string originalFileName = System.IO.Path.GetFileName(filenames[i]);
            string destinationPath = System.IO.Path.Combine(destinationFolder, originalFileName);

            int counter = 1;
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(originalFileName);
            string fileExtension = System.IO.Path.GetExtension(originalFileName);

            while (System.IO.File.Exists(destinationPath))
            {
                // Create a new filename with a counter appended
                string newFileName = $"{fileNameWithoutExtension}({counter}){fileExtension}";
                destinationPath = System.IO.Path.Combine(destinationFolder, newFileName);
                counter++;
            }

            System.IO.File.Copy(filenames[i], destinationPath, true);
            resultingFiles += System.IO.Path.GetFileName(destinationPath) + ",";
        }

        SendResults(resultingFiles);
#endif
    }

    public void SendResults(string filePaths)
    {
        Debug.Log("button received: " + filePaths);
        onFilesSelected.Invoke(filePaths);
    }
}