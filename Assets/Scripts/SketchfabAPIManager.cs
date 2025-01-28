using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UniGLTF;
using System.IO;
using System.Text;
using UnityEngine.UI;
using TMPro;

public class SketchfabAPIManager : MonoBehaviour
{
    [System.Serializable]
    public class Archives
    {
        public List<Glb> glb;
        public List<Gltf> gltf;
        public List<Source> source;
        public List<Usdz> usdz;
    }

    [System.Serializable]
    public class Avatar
    {
        public string uri;
        public List<Image> images;
    }

    [System.Serializable]
    public class Category
    {
        public string name;
    }

    [System.Serializable]
    public class Cursors
    {
        public string next;
        public object previous;
    }

    [System.Serializable]
    public class Glb
    {
        public int textureCount;
        public int size;
        public string type;
        public int textureMaxResolution;
        public int faceCount;
        public int vertexCount;
    }

    [System.Serializable]
    public class Gltf
    {
        public int textureCount;
        public int size;
        public string type;
        public int textureMaxResolution;
        public int faceCount;
        public int vertexCount;
    }

    [System.Serializable]
    public class Image
    {
        public string uid;
        public int size;
        public int width;
        public string url;
        public int height;
    }

    [System.Serializable]
    public class License
    {
        public string uid;
        public string label;
    }

    [System.Serializable]
    public class Result
    {
        public string uri;
        public string uid;
        public string name;
        public object staffpickedAt;
        public int viewCount;
        public int likeCount;
        public int animationCount;
        public string viewerUrl;
        public string embedUrl;
        public int commentCount;
        public bool isDownloadable;
        public DateTime publishedAt;
        public List<Tag> tags;
        public List<Category> categories;
        public Thumbnails thumbnails;
        public User user;
        public string description;
        public int faceCount;
        public DateTime createdAt;
        public int vertexCount;
        public bool isAgeRestricted;
        public int soundCount;
        public bool isProtected;
        public License license;
        public object price;
        public Archives archives;
    }

    [System.Serializable]
    public class SFSearchResults
    {
        public Cursors cursors;
        public string next;
        public object previous;
        public List<Result> results;
    }

    [System.Serializable]
    public class Source
    {
        public object textureCount;
        public int size;
        public string type;
        public object textureMaxResolution;
        public object faceCount;
        public object vertexCount;
    }

    [System.Serializable]
    public class Tag
    {
        public string name;
        public string slug;
        public string uri;
    }

    [System.Serializable]
    public class Thumbnails
    {
        public List<Image> images;
    }

    [System.Serializable]
    public class Usdz
    {
        public object textureCount;
        public int size;
        public string type;
        public object textureMaxResolution;
        public object faceCount;
        public object vertexCount;
    }

    [System.Serializable]
    public class User
    {
        public string uid;
        public string username;
        public string displayName;
        public string profileUrl;
        public string account;
        public Avatar avatar;
        public string uri;
    }

    [System.Serializable]
    public class ImplicitAccessToken
    {
        public string access_token;
        public int expires_in;
        public string token_type;
        public string scope;
        public string refresh_token;
    }

    [System.Serializable]
    public class _Glb
    {
        public string url;
        public int size;
        public int expires;
    }

    [System.Serializable]
    public class _Gltf
    {
        public string url;
        public int size;
        public int expires;
    }

    [System.Serializable]
    public class _Usdz
    {
        public string url;
        public int size;
        public int expires;
    }

    [System.Serializable]
    public class _ModelDownloadMetaData
    {
        public _Gltf gltf;
        public _Usdz usdz;
        public _Glb glb;
    }


    private GameObject spawnedModel;
    [Header("Mandatory -- Sketchfab Username")] public string username;
    [Header("Mandatory -- Sketchfab Password")] public string password;
    [Space]public string baseUrl;
    public string keywordToSearch;
    public int maximumPageLimit;
    public List<SFSearchResults> searchResults;
     public Button searchBtn;
    public TMP_InputField inputField;
    public GameObject fillbarObject;
    public UnityEngine.UI.Image fillbarForeground;

    private string _nextUrl;
    private int currentPageNumber;
    private bool shouldLoadNextPage;
    private string clientId = "INPUT_HERE";
    private string clientSecret = "INPUT_HERE";
    [SerializeField] private ImplicitAccessToken IAT;

    private void Start()
    {
        searchBtn.onClick.AddListener(() => _PerformSearch());
       // Invoke("callI", 2.0f);
    }

    public void callI()
    {
        string write_path = Application.persistentDataPath + "/" + "humanface.zip";
        DisplayModel(write_path);
    }

    private IEnumerator AuthenticateUser()
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Username And/Or Password are empty!");
            yield return null;
        }
        else
        {
            string _Url = "https://sketchfab.com/oauth2/token/";


            // string s = "";
            WWWForm form = new WWWForm();
            form.AddField("grant_type", "password");
            form.AddField("username", username);
            form.AddField("password", password);

            using (UnityWebRequest www = UnityWebRequest.Post(_Url, form))
            {
                string cred = Convert.ToBase64String(Encoding.UTF8.GetBytes (clientId + ":" + clientSecret));

                www.SetRequestHeader("Authorization", "Basic " + cred);
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    // Error
                    Debug.Log(System.Text.Encoding.UTF8.GetString(www.downloadHandler.data));
                    Debug.Log(www.error);
                }
                else
                {
                    if (www.isDone)
                    {
                        string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                        IAT =  (ImplicitAccessToken)JsonUtility.FromJson(jsonResult, typeof(ImplicitAccessToken));
                    }
                }
            }
        }
    }

    private IEnumerator PerformSearch()
    {
        fillbarObject.SetActive(false);
        _nextUrl = string.Empty;
        searchResults = new List<SFSearchResults>();
        currentPageNumber = 0;
        shouldLoadNextPage = true;

        keywordToSearch = inputField.text;

        if (keywordToSearch.Equals(""))
        {
            Debug.Log("Please enter a valid keyword!");
            yield return null;
        }
        else
        {
            if (spawnedModel != null)
            {
                DestroyImmediate(spawnedModel);
            }

            //DeleteFiles();

           // string _Url = baseUrl + "tags=" + keywordToSearch + "&downloadable=true&archives_flavours=true";
            string _Url = "https://api.sketchfab.com/v3/search?type=models&q=" + keywordToSearch + "&animated=false&downloadable=true&archives_flavours=true";
            _Url = Uri.EscapeUriString(_Url);
            using (UnityWebRequest www = UnityWebRequest.Get(_Url))
            {
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    // Error
                    Debug.Log(System.Text.Encoding.UTF8.GetString(www.downloadHandler.data));
                    Debug.Log(www.error);
                }
                else
                {
                    if (www.isDone)
                    {
                        string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                        SFSearchResults _searchResults =  (SFSearchResults)JsonUtility.FromJson(jsonResult, typeof(SFSearchResults));
                        searchResults.Add(_searchResults);
                        currentPageNumber++;
                        if (!string.IsNullOrEmpty(_searchResults.next))
                        {
                            _nextUrl = _searchResults.next;

                            while (currentPageNumber < maximumPageLimit && shouldLoadNextPage)
                            {
                                shouldLoadNextPage = false;
                                StartCoroutine(LoadNextPages(_nextUrl));

                                while (!shouldLoadNextPage)
                                    yield return null;
                            }
                        }

                        // Get model download details
                        int randomElementIndex = 0;//UnityEngine.Random.Range(0, maximumPageLimit);
                        int randomResultObjectIndex = 0;//UnityEngine.Random.Range(0, 24);
                        StartCoroutine(GetModelDetails(searchResults[randomElementIndex].results[randomResultObjectIndex].uid));
                    }
                }
            }
        }
    }

    private IEnumerator LoadNextPages(string url)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                // Error
                Debug.Log(System.Text.Encoding.UTF8.GetString(www.downloadHandler.data));
                Debug.Log(www.error);
            }
            else
            {
                if (www.isDone)
                {
                    string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                    SFSearchResults _searchResults =  (SFSearchResults)JsonUtility.FromJson(jsonResult, typeof(SFSearchResults));
                    searchResults.Add(_searchResults);
                    _nextUrl = _searchResults.next;
                    currentPageNumber++;
                    shouldLoadNextPage = true;
                }
            }
        }
    }

    private IEnumerator GetModelDetails(string UID)
    {
       // UID = "INSERT_HERE";
        if (string.IsNullOrEmpty(IAT.access_token))
        {
            StartCoroutine(AuthenticateUser());
            yield return new WaitForSeconds(2f);
            StartCoroutine(GetSpecificModelDetails(UID));
        }
        else
        {
            StartCoroutine(GetSpecificModelDetails(UID));
            yield return null;
        }
    }

    private IEnumerator GetSpecificModelDetails(string UID)
    {
        string downloadUrl = "https://api.sketchfab.com/v3/models/" + UID + "/download";
        using (UnityWebRequest www = UnityWebRequest.Get(downloadUrl))
        {
            www.SetRequestHeader("Authorization", "Bearer " + IAT.access_token);
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                // Error
                Debug.Log(System.Text.Encoding.UTF8.GetString(www.downloadHandler.data));
                Debug.Log("Error fetching details: " + www.error);
            }
            else
            {
                if (www.isDone)
                {
                    string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                    _ModelDownloadMetaData modelMetaData = (_ModelDownloadMetaData)JsonUtility.FromJson(jsonResult, typeof(_ModelDownloadMetaData));
                    StartCoroutine(BeginDownloadProcess(modelMetaData.gltf.url));
                }
            }
        }
    }

    private IEnumerator BeginDownloadProcess(string url)
    {
        Debug.Log("Downloading from: " + url);
        fillbarObject.SetActive(true);
        fillbarForeground.fillAmount = 0;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                float progress = www.downloadProgress * 100 / 100.0f;
                fillbarForeground.fillAmount = progress;
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                fillbarObject.SetActive(false);
                Debug.Log(System.Text.Encoding.UTF8.GetString(www.downloadHandler.data));
                Debug.Log("Download err: " + www.error);
            }
            else
            {
                if (operation.isDone)
                {
                    fillbarObject.SetActive(false);
                    string modelName = keywordToSearch + ".zip";
                    string write_path = Application.persistentDataPath + "/" + modelName;
                    Debug.Log("Path: " + write_path);
                    System.IO.File.WriteAllBytes(write_path, www.downloadHandler.data);
                    DisplayModel(write_path);
                }
            }
        }
    }

    
    public float size;

    private void DisplayModel(string path)
    {        
        var context = gltfImporter.Load(path);
        context.ShowMeshes();
        spawnedModel = context.Root;
        spawnedModel.transform.position = Vector3.zero;
        spawnedModel.transform.localEulerAngles = new Vector3(0, 30, 0);

        MeshRenderer collider = spawnedModel.GetComponentInChildren<MeshRenderer>();
        SkinnedMeshRenderer _smr = spawnedModel.GetComponentInChildren<SkinnedMeshRenderer>();

        Vector3 targetSize = Vector3.one * size;
        
        Bounds meshBounds;

        if (collider != null)
        {
            meshBounds = collider.bounds;
        }
        else
        {
            meshBounds = _smr.bounds;
        }

        Vector3 meshSize = meshBounds.size;
        float xScale = targetSize.x / meshSize.x;
        float yScale = targetSize.y / meshSize.y;
        float zScale = targetSize.z / meshSize.z;
        spawnedModel.transform.localScale = new Vector3(xScale, yScale, zScale);
    }

    public void DeleteFiles()
    {
        searchResults = new List<SFSearchResults>();
        IAT = null;
        string[] filePaths = Directory.GetFiles(Application.persistentDataPath);
        foreach (string filePath in filePaths)
            File.Delete(filePath); 
    }

    public void _PerformSearch()
    {
        StartCoroutine(PerformSearch());
    }

    public void _AuthenticateUser()
    {
        StartCoroutine(AuthenticateUser());
    }
}
