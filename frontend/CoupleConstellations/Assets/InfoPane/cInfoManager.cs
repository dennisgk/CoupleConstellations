using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using NativeGalleryNamespace;
using System.IO;

public class cInfoManager : MonoBehaviour
{
    public enum cInfoActions
    {
        ADD,
        SETTINGS,
        IMAGE_VIEW,
        NOTES_VIEW
    }

    public enum LoadedData
    {
        IMAGE_DATA,
        NOTES_DATA
    }

    public GameObject plusButton;
    public GameObject settingsButton;
    public GameObject imagesButton;
    public GameObject notesButton;
    public GameObject refreshButton;
    public GameObject pinButton;
    public GameObject galaxyParent;

    public GameObject textPrompt;
    public InputField textPromptInput;
    public Text textPromptDesc;

    public GameObject notesDataPrompt;
    public InputField notesDataInputField;

    public ServerCommunication scManager;
    public DataScroll dsManager;

    public TextMeshPro constellationNameText;

    public float dataSizeModifier = 0.2f;
    public float nextImageDistance = 2.0f;

    public float popoutZs = -1.1f;
    public float popinZs = 0.0f;

    public GameObject errorPopup;
    public Text errorDescription;

    private string lastLoadedConstellationUID = "";
    public LoadedData lastLoadedData = LoadedData.IMAGE_DATA;

    public string curConstellationUID = "";
    public LoadedData curLoadedData = LoadedData.IMAGE_DATA;

    public cInfoActions curAction = cInfoActions.IMAGE_VIEW;
    public string curConstellationName = "";

    public GameObject imageDataPrefab;
    public GameObject dataHolder;

    private string dataCreationConstellationUID = "";
    private string deletionConstellationUID = "";
    private string editingConstellationUID = "";

    // Start is called before the first frame update
    void Start()
    {
        ImagesButtonPressed();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void addToImageData(Texture2D image)
    {
        dsManager.BuildImageData(image);

    }

    Texture2D rotateTexture(Texture2D originalTexture, bool clockwise)
    {
        Color32[] original = originalTexture.GetPixels32();
        Color32[] rotated = new Color32[original.Length];
        int w = originalTexture.width;
        int h = originalTexture.height;

        int iRotated, iOriginal;

        for (int j = 0; j < h; ++j)
        {
            for (int i = 0; i < w; ++i)
            {
                iRotated = (i + 1) * h - j - 1;
                iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                rotated[iRotated] = original[iOriginal];
            }
        }

        Texture2D rotatedTexture = new Texture2D(h, w);
        rotatedTexture.SetPixels32(rotated);
        rotatedTexture.Apply();
        return rotatedTexture;
    }

    public void DownloadImagesCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            if (uwr.responseCode != 404)
            {
                Texture2D txt2d = new Texture2D(0, 0);
                txt2d.LoadImage(uwr.downloadHandler.data);

                addToImageData(txt2d);
            }
        }
    }

    public void LoadDataImagesCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            GetConstellationImagesRet ret = JsonUtility.FromJson<GetConstellationImagesRet>(uwr.downloadHandler.text);

            if(ret.exists == 1)
            {
                for (int i = 0; i < ret.extraData.Length; i++)
                {
                    GetConstellationImagesFromStructReq req = new GetConstellationImagesFromStructReq();
                    req.clientConnectCode = scManager.currentUserInf[1];
                    req.secretJoinCode = scManager.currentUserInf[2];
                    req.constellationUID = curConstellationUID;
                    req.imagePath = ret.extraData[i].imagePath;

                    string json = JsonUtility.ToJson(req);

                    string[] exts = { "getConstellationImageFromStruct" };
                    string uri = ServerCommunication.createUri(exts);
                    StartCoroutine(scManager.PostRequest(uri, json, DownloadImagesCallback));
                }
            }

        }
    }

    public void DownloadNotesCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            GetConstellationNotesRet ret = JsonUtility.FromJson<GetConstellationNotesRet>(uwr.downloadHandler.text);

            if(ret.exists == 1)
            {
                for(int i = 0; i < ret.extraData.Length; i++)
                {
                    dsManager.BuildNotesData(ret.extraData[i].creator, ret.extraData[i].text);
                }
            }
        }
    }

    public void LoadData()
    {
        if (curConstellationUID == lastLoadedConstellationUID && curLoadedData == lastLoadedData)
        {
            //everything is already loaded
        }
        else
        {
            dsManager.ResetAllData();

            lastLoadedConstellationUID = curConstellationUID;
            lastLoadedData = curLoadedData;
            constellationNameText.text = curConstellationName;

            if (curLoadedData == LoadedData.IMAGE_DATA)
            {
                GetConstellationDataReq req = new GetConstellationDataReq();
                req.clientConnectCode = scManager.currentUserInf[1];
                req.secretJoinCode = scManager.currentUserInf[2];
                req.constellationUID = curConstellationUID;
                req.dataType = "imageData";

                string json = JsonUtility.ToJson(req);

                string[] exts = { "getConstellationData" };
                string uri = ServerCommunication.createUri(exts);
                StartCoroutine(scManager.PostRequest(uri, json, LoadDataImagesCallback));
            }
            if(curLoadedData == LoadedData.NOTES_DATA)
            {
                GetConstellationDataReq req = new GetConstellationDataReq();
                req.clientConnectCode = scManager.currentUserInf[1];
                req.secretJoinCode = scManager.currentUserInf[2];
                req.constellationUID = curConstellationUID;
                req.dataType = "noteData";

                string json = JsonUtility.ToJson(req);

                string[] exts = { "getConstellationData" };
                string uri = ServerCommunication.createUri(exts);
                StartCoroutine(scManager.PostRequest(uri, json, DownloadNotesCallback));
            }
        }
    }

    public void SetZ(GameObject obj, float val)
    {
        Vector3 ogPos = obj.transform.localPosition;
        ogPos.z = val;
        obj.transform.localPosition = ogPos;
    }

    public void ImagesButtonPressed()
    {
        curAction = cInfoActions.IMAGE_VIEW;

        SetZ(imagesButton, popinZs);
        SetZ(notesButton, popoutZs);

        curLoadedData = LoadedData.IMAGE_DATA;
        LoadData();
    }

    public void NotesDataPromptClosed()
    {
        dsManager.canShowData = true;
        notesDataPrompt.SetActive(false);
    }

    private void UploadNotesCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            //not error handling
            RefreshButtonPressed();
        }
    }

    public void NotesDataPromptSubmitted()
    {
        string text = notesDataInputField.text;
        string constUID = dataCreationConstellationUID;
        string creator = scManager.currentUserInf[0];

        UploadNoteDataReq req = new UploadNoteDataReq();

        req.clientConnectCode = scManager.currentUserInf[1];
        req.secretJoinCode = scManager.currentUserInf[2];

        req.constellationUID = constUID;
        req.creator = creator;
        req.noteText = text;

        string json = JsonUtility.ToJson(req);

        string[] exts = { "editConstellation", "uploadNoteData" };
        string uri = ServerCommunication.createUri(exts);

        StartCoroutine(scManager.PostRequest(uri, json, UploadNotesCallback));
        notesDataPrompt.SetActive(false);
        dsManager.canShowData = true;
    }

    private void UploadImageCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            //not error handling
            RefreshButtonPressed();
        }
    }

    IEnumerator Uploadimage(string url, string _headerKey, string path, ServerCommunication.handleRequestResponse callback)
    {
        string[] tmpUploadNameArr = path.Split(new char[] { '.' });
        string tmpUploadName = "upl." + tmpUploadNameArr[tmpUploadNameArr.Length - 1];

        string _headerValue = "_asdj12avdspfhasdjf01238fbse9a7fnx2380fzas8dfh20h30f09adsod118h342ho4bqef8se0qaewfh0832bfvzcxjvas3230l23ajewfj290390a9dsufq0h2309";

        WWWForm formrequest = new WWWForm();
        formrequest.AddBinaryData("upload", File.ReadAllBytes(path), tmpUploadName, "application/octet-stream");

        UnityWebRequest www = UnityWebRequest.Post(url, formrequest);


        //set jwt
        if (_headerKey != null)
        {
            www.SetRequestHeader(_headerKey, _headerValue);
        }

        yield return www.SendWebRequest();

        callback(www);
    }


    public void mediaCallback(string path)
    {
        if(path == null)
        {
            errorDescription.text = "No image selected";
            errorPopup.SetActive(true);
        }
        else
        {

            string clientConnectCode = $"clientConnectCode={scManager.currentUserInf[1]}";
            string secretJoinCode = $"secretJoinCode={scManager.currentUserInf[2]}";
            string constellationUID = $"constellationUID={dataCreationConstellationUID}";
            string creator = $"creator={scManager.currentUserInf[0]}";

            string appendedArgs = $"?{clientConnectCode}&{secretJoinCode}&{constellationUID}&{creator}";

            string[] exts = { "editConstellation", "uploadImageData", appendedArgs };
            string uri = ServerCommunication.createUri(exts);

            StartCoroutine(Uploadimage(uri, "upload", path, UploadImageCallback));

            dsManager.canShowData = true;
            
        }

        dsManager.canShowData = true;
    }

    public void PlusButtonPressed()
    {
        dsManager.canShowData = false;
        if(curLoadedData == LoadedData.IMAGE_DATA)
        {
            dataCreationConstellationUID = curConstellationUID;

            NativeGallery.Permission perm = NativeGallery.CheckPermission(NativeGallery.PermissionType.Read);
            if(perm == NativeGallery.Permission.Granted)
            {
                NativeGallery.GetImageFromGallery(mediaCallback);
            }
            else if(perm == NativeGallery.Permission.ShouldAsk)
            {
                NativeGallery.RequestPermission(NativeGallery.PermissionType.Read);
                NativeGallery.GetImageFromGallery(mediaCallback);
            }
            else
            {
                errorDescription.text = "Permission not enabled";
                errorPopup.SetActive(true);
            }
        }
        if(curLoadedData == LoadedData.NOTES_DATA)
        {
            notesDataPrompt.SetActive(true);
            dataCreationConstellationUID = curConstellationUID;
        }
    }

    public void CogButtonPressed()
    {

    }

    public void NotesButtonPressed()
    {
        curAction = cInfoActions.NOTES_VIEW;

        SetZ(imagesButton, popoutZs);
        SetZ(notesButton, popinZs);

        curLoadedData = LoadedData.NOTES_DATA;
        LoadData();
    }

    public void RefreshButtonPressed()
    {
        lastLoadedConstellationUID = "";
        LoadData();
    }

    public void DeleteConstellation()
    {
        deletionConstellationUID = curConstellationUID;
        textPrompt.SetActive(true);
        textPromptDesc.text = "Type 'yes' to delete. Type 'no' to cancel.";
        textPromptInput.text = "";
        scManager.currentAction = ServerCommunication.UserActionMode.RemovingConstellation;

    }

    public void CarryOutDeletion(string input)
    {
        if(input.ToLower() == "yes")
        {
            //delete from deletionConstellationUID and build galaxy when done
            for(int i = 0; i < galaxyParent.transform.childCount; i++)
            {
                ConstellationHandler h = galaxyParent.transform.GetChild(i).GetComponent<ConstellationHandler>();
                if(h.uid == deletionConstellationUID)
                {
                    h.deleteFromServer(scManager);
                    return;
                }
            }
        }
    }
    public void EditConstellation()
    {
        scManager.currentAction = ServerCommunication.UserActionMode.EditingConstellation;

        scManager.helpDescription.text = "- Drag on the points to move them around\n- Tap the Done button when you are done";
        scManager.helpPopup.SetActive(true);

        scManager.doneButton.SetActive(true);

        editingConstellationUID = curConstellationUID;

        gameObject.SetActive(false);
    }

    public ConstellationHandler FindConstellation(string constellationUID)
    {
        for(int i = 0; i < galaxyParent.transform.childCount; i++)
        {
            ConstellationHandler h = galaxyParent.transform.GetChild(i).GetComponent<ConstellationHandler>();
            if(h.uid == constellationUID)
            {
                return h;
            }
        }

        return null;
    }

    public void screenDragConstellationEditing(Vector2 screenPos)
    {
        if (scManager.currentAction != ServerCommunication.UserActionMode.EditingConstellation)
        {
            return;
        }

        Vector3 point = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane)).normalized;

        ConstellationHandler curHandler = FindConstellation(editingConstellationUID);

        if(curHandler != null)
        {
            if (curHandler.currentStat == ConstellationHandler.status.COMPLETION)
            {
                Vector3 outVec = point * 35;
                float closestDist = 10000.0f;
                GameObject closestStar = null;

                for(int i = 0; i < curHandler.stars.Count; i++)
                {
                    float dist = Vector3.Distance(outVec, curHandler.stars[i].transform.position);
                    if(dist < closestDist)
                    {
                        closestDist = dist;
                        closestStar = curHandler.stars[i];
                    }
                }

                closestStar.transform.position = point * 35;
            }

            curHandler.UpdateLineRenderer();
        }


    }

    public void DoneEditingConstellation()
    {

        scManager.doneButton.SetActive(false);
        scManager.helpPopup.SetActive(false);

        //get points from constellation + update

        gameObject.SetActive(true);

        ConstellationHandler curHandler = FindConstellation(editingConstellationUID);
        curHandler.UpdateConstellationPointsToServer(scManager);

    }

    [System.Serializable]
    public class GetConstellationDataReq
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public string constellationUID;
        public string dataType;
    }

    [System.Serializable]
    public class ImageRet
    {
        public string creator;
        public string imagePath;
        public string sourceHolderFile;
    }

    [System.Serializable]
    public class GetConstellationImagesRet
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public int exists;
        public string constellationUID;
        public string constellationName;
        public string dataType;
        public ImageRet[] extraData;
    }



    [System.Serializable]
    public class NotesRet
    {
        public string creator;
        public string text;
        public string sourceHolderFile;
    }

    [System.Serializable]
    public class GetConstellationNotesRet
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public int exists;
        public string constellationUID;
        public string constellationName;
        public string dataType;
        public NotesRet[] extraData;
    }



    [System.Serializable]
    public class GetConstellationImagesFromStructReq
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public string constellationUID;
        public string imagePath;
    }



    [System.Serializable]
    public class UploadNoteDataReq
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public string constellationUID;
        public string creator;
        public string noteText;
    }



    [System.Serializable]
    public class RemoveConstellationReq
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public string constellationUID;
    }

}
