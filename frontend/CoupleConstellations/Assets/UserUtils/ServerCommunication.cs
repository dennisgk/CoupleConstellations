using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class ServerCommunication : MonoBehaviour
{
    
    #region Instance
    private static ServerCommunication instance;
    public static ServerCommunication Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ServerCommunication>();
                if (instance == null)
                {
                    instance = new GameObject("Spawned ServerCommunication", typeof(ServerCommunication)).GetComponent<ServerCommunication>();
                }
            }

            return instance;
        }
        set
        {
            instance = value;
        }
    }
    #endregion

    public enum UserActionMode
    {
        None,
        CreatingConstellation,
        NamingConstellation,
        EditingConstellation,
        RemovingConstellation
    }

    public UserActionMode currentAction = UserActionMode.None;

    public GameObject galaxyParent;
    public GameObject constellationParentPrefab;
    public GameObject starPrefab;
    public GameObject eulaPopup;
    public Text greetingPanelText;

    public GameObject bottomTextHelper;
    public GameObject undoButton;
    public GameObject doneButton;

    public Text galaxyNameText;

    public Text mGalaxyNameText;
    public Text mClientConnectCode;
    public Text mSecretJoinCode;
    public Text mCreator;

    public GameObject newPlayerPopup;
    public GameObject greetingPopup;
    public GameObject joinGalaxyPopup;
    public GameObject createGalaxyPopup;

    public InputField textPromptInputField;
    public Text textPromptDescription;
    public GameObject textPromptPopup;

    public GameObject galaxyInfoPopup;
    public GameObject constellationsPopup;

    public InputField cgName;
    public InputField cgCode;
    public GameObject cgErrorText;

    public InputField jgID;
    public InputField jgCode;
    public GameObject jgErrorText;

    public InputField newbieName;
    public GameObject newbieErrorMessage;

    public GameObject errorPopup;
    public Text errorDescription;
    public GameObject helpPopup;
    public Text helpDescription;

    public GameObject notesDataPrompt;

    public SwipeDetector sdManager;
    public cInfoManager ciManager;
    private GameObject currentConstellation = null;
    private GameObject currentNamingConstellation = null;
    private GameObject currentDeletingConstellation = null;
    public GameObject constellationPrefab;

    private static string serverUrl = "https://constellations.kountouris.org";

    private bool automaticConstellationGenerationForNew = true;
    public GameObject checkGeneration;

    public bool isUserEnabled = false;
    public string currentUsername = "";

    public delegate void handleRequestResponse(UnityWebRequest uwr);

    public string[] currentUserInf;

    private static string cleanStr(string str)
    {
        string ret = str;
        while (ret[0] == '/')
        {
            ret = ret.Substring(1);
        }

        while (ret[ret.Length - 1] == '/')
        {
            ret = ret.Substring(0, ret.Length - 1);
        }

        return ret;
    }

    public void ToggleAutomaticConstellationGen()
    {
        automaticConstellationGenerationForNew = !automaticConstellationGenerationForNew;
        checkGeneration.SetActive(automaticConstellationGenerationForNew);
    }

    public static string createUri(string[] extensions)
    {
        string ret = cleanStr(serverUrl) + "/";
        for (int i = 0; i < extensions.Length; i++)
        {
            ret = ret + extensions[i] + "/";
        }
        ret = cleanStr(ret);
        return ret;
    }

    public string[] getUserInfo()
    {
        string username = "";
        string joinedGalaxyID = "";
        string joinedGalaxySecretCode = "";

        if (PlayerPrefs.HasKey("UserName"))
        {
            username = PlayerPrefs.GetString("UserName");
            currentUsername = username;
        }
        else
        {
            return null;
        }

        if (PlayerPrefs.HasKey("JoinedGalaxyCode"))
        {
            joinedGalaxyID = PlayerPrefs.GetString("JoinedGalaxyCode");
        }
        else
        {
            return null;
        }

        if (PlayerPrefs.HasKey("JoinedGalaxySecretCode"))
        {
            joinedGalaxySecretCode = PlayerPrefs.GetString("JoinedGalaxySecretCode");
        }
        else
        {
            return null;
        }

        string[] ret = new string[3];
        ret[0] = username;
        ret[1] = joinedGalaxyID;
        ret[2] = joinedGalaxySecretCode;

        return ret;
    }

    public void ExitJoinGalaxyPopup()
    {
        joinGalaxyPopup.SetActive(false);
    }

    public void ExitCreateGalaxyPopup()
    {
        createGalaxyPopup.SetActive(false);
    }

    public void OpenJoinGalaxyPopup()
    {
        joinGalaxyPopup.SetActive(true);
        jgErrorText.SetActive(false);
    }

    public void OpenCreateGalaxyPopup()
    {
        createGalaxyPopup.SetActive(true);
        cgErrorText.SetActive(false);
    }

    public void HideErrorPopup()
    {
        errorPopup.SetActive(false);
    }

    public void HideHelpPopup()
    {
        helpPopup.SetActive(false);
    }

    public void SubmitTextPromptPopup()
    {
        if (currentAction == UserActionMode.NamingConstellation)
        {
            currentNamingConstellation.GetComponent<ConstellationHandler>().cname = textPromptInputField.text;
            currentNamingConstellation.GetComponent<ConstellationHandler>().addToServer(this);
            currentNamingConstellation = null;
            currentAction = UserActionMode.None;
        }

        if (currentAction == UserActionMode.RemovingConstellation)
        {
            ciManager.CarryOutDeletion(textPromptInputField.text);
            currentAction = UserActionMode.None;
        }

        textPromptPopup.SetActive(false);
    }

    public void ShowErrorPopup(string error)
    {
        errorDescription.text = " - " + error;
        errorPopup.SetActive(true);
    }

    public static bool isAlphanumeric(string strToCheck)
    {
        foreach (char c in strToCheck)
        {
            if (!(char.IsLetterOrDigit(c) || c == ' '))
            {
                return false;
            }
        }
        return true;
    }

    public void NewPlayerPanelContinued()
    {
        if (newbieName.text.Length < 1 || newbieName.text.Length > 19)
        {
            newbieErrorMessage.SetActive(true);
        }
        else
        {
            if (!isAlphanumeric(newbieName.text))
            {
                newbieErrorMessage.SetActive(true);
            }
            else
            {
                PlayerPrefs.SetString("UserName", newbieName.text);
                currentUsername = newbieName.text;

                greetingPopup.SetActive(true);
                newPlayerPopup.SetActive(false);
            }
        }
    }

    public void joinGalaxyCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            CommunicationObjects.GalaxyExists ret = JsonUtility.FromJson<CommunicationObjects.GalaxyExists>(uwr.downloadHandler.text);

            if (ret.exists == 1)
            {
                PlayerPrefs.SetString("JoinedGalaxyCode", ret.clientConnectCode);
                PlayerPrefs.SetString("JoinedGalaxySecretCode", ret.secretJoinCode);

                greetingPopup.SetActive(false);

                initUser();
            }
            else
            {
                jgErrorText.SetActive(true);
            }

        }
    }

    public void buildGalaxyCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            CommunicationObjects.GalaxyConstruction ret = JsonUtility.FromJson<CommunicationObjects.GalaxyConstruction>(uwr.downloadHandler.text);

            if (ret.exists == 1)
            {
                //Debug.Log(uwr.downloadHandler.text);

                galaxyNameText.text = ret.galaxyName;
                mGalaxyNameText.text = "Galaxy Name: " + ret.galaxyName;
                mClientConnectCode.text = "ID: " + ret.clientConnectCode;
                mSecretJoinCode.text = "Code: " + ret.secretJoinCode;
                mCreator.text = "Creator: " + ret.galaxyCreator;

                for (int i = 0; i < ret.constellations.Length; i++)
                {
                    GameObject newConstellation = (GameObject)Instantiate(constellationParentPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    newConstellation.transform.parent = galaxyParent.transform;

                    newConstellation.GetComponent<ConstellationHandler>().initNec();
                    newConstellation.GetComponent<ConstellationHandler>().uid = ret.constellations[i].uid;
                    newConstellation.GetComponent<ConstellationHandler>().cname = ret.constellations[i].name;
                    for (int j = 0; j < ret.constellations[i].stars.Length; j++)
                    {
                        GameObject newStar = (GameObject)Instantiate(starPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                        newStar.transform.parent = newConstellation.transform;

                        newStar.transform.position = new Vector3(ret.constellations[i].stars[j].x, ret.constellations[i].stars[j].y, ret.constellations[i].stars[j].z);
                        newConstellation.GetComponent<ConstellationHandler>().AddToStars(newStar);
                    }

                    newConstellation.GetComponent<ConstellationHandler>().currentStat = ConstellationHandler.status.COMPLETION;
                    newConstellation.GetComponent<ConstellationHandler>().UpdateLineRenderer();
                }
            }
            else
            {
                Debug.Log(ret.exists);
                ShowErrorPopup("Galaxy no longer exists");
            }

        }
    }

    public ConstellationHandler[] CreateRandomConstellations(int numOfAutoGens = 8, int minNumStarsPerConstellation = 4, int maxNumStarsPerConstellation = 8)
    {

        System.Random rnd = new System.Random();

        ConstellationHandler[] handlers = new ConstellationHandler[numOfAutoGens];

        for (int i = 0; i < numOfAutoGens; i++)
        {
            GameObject newConst = (GameObject)Instantiate(constellationPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            newConst.transform.parent = galaxyParent.transform;

            newConst.GetComponent<ConstellationHandler>().initNec();

            handlers[i] = newConst.GetComponent<ConstellationHandler>();


            int numStars = rnd.Next(minNumStarsPerConstellation, maxNumStarsPerConstellation + 1);

            bool xPN = rnd.NextDouble() >= 0.5f;
            bool yPN = rnd.NextDouble() >= 0.5f;
            bool zPN = rnd.NextDouble() >= 0.5f;
            float randXPos = ((float)rnd.NextDouble() * 30.0f) + 5.0f;
            float randXNeg = ((float)rnd.NextDouble() * -30.0f) - 5.0f;
            float randYPos = ((float)rnd.NextDouble() * 30.0f) + 5.0f;
            float randYNeg = ((float)rnd.NextDouble() * -30.0f) - 5.0f;
            float randZPos = ((float)rnd.NextDouble() * 30.0f) + 5.0f;
            float randZNeg = ((float)rnd.NextDouble() * -30.0f) - 5.0f;
            Vector3 aroundPointT = new Vector3((xPN ? randXPos : randXNeg), (yPN ? randYPos : randYNeg), (zPN ? randZPos : randZNeg));
            Vector3 dir = (aroundPointT - Vector3.zero).normalized;
            Vector3 aroundPoint = dir * 35;

            for (int j = 0; j < numStars; j++)
            {
                bool xPNM = rnd.NextDouble() >= 0.5f;
                bool yPNM = rnd.NextDouble() >= 0.5f;
                bool zPNM = rnd.NextDouble() >= 0.5f;
                float randXPosM = ((float)rnd.NextDouble() * 4.0f) + 1.5f;
                float randXNegM = ((float)rnd.NextDouble() * -4.0f) - 1.5f;
                float randYPosM = ((float)rnd.NextDouble() * 4.0f) + 1.5f;
                float randYNegM = ((float)rnd.NextDouble() * -4.0f) - 1.5f;
                float randZPosM = ((float)rnd.NextDouble() * 4.0f) + 1.5f;
                float randZNegM = ((float)rnd.NextDouble() * -4.0f) - 1.5f;
                Vector3 starPntT = new Vector3(aroundPoint.x + (xPNM ? randXPosM : randXNegM), aroundPoint.y + (yPNM ? randYPosM : randYNegM), aroundPoint.z + (zPNM ? randZPosM : randZNegM));
                Vector3 starDir = (starPntT - Vector3.zero).normalized;
                Vector3 nPoint = starDir * 35;

                GameObject newStar = Instantiate(starPrefab, nPoint, Quaternion.identity);
                newStar.transform.parent = newConst.transform;

                newConst.GetComponent<ConstellationHandler>().AddToStars(newStar);
            }

            handlers[i].currentStat = ConstellationHandler.status.COMPLETION;
            handlers[i].cname = "auto";
            handlers[i].UpdateLineRenderer();
        }

        return handlers;
    }

    public void CreateGalaxy()
    {
        if (isAlphanumeric(cgName.text) && cgName.text.Length < 20 && isAlphanumeric(cgCode.text) && cgCode.text.Length < 20)
        {
            //must talk to server
            CommunicationObjects.CreateGalaxyReq req = new CommunicationObjects.CreateGalaxyReq();
            req.galaxyName = cgName.text;
            req.galaxySecretCode = cgCode.text;
            req.galaxyCreator = currentUsername;

            string json = JsonUtility.ToJson(req);

            string[] exts = { "createGalaxy" };
            string uri = createUri(exts);

            StartCoroutine(PostRequestCreateGalaxy(uri, json));
        }
        else
        {
            cgErrorText.SetActive(true);
            return;
        }


        greetingPopup.SetActive(false);
    }

    public void JoinGalaxy()
    {
        //must talk to server
        CommunicationObjects.GalaxyCredentials creds = new CommunicationObjects.GalaxyCredentials();
        creds.clientConnectCode = jgID.text;
        creds.secretJoinCode = jgCode.text;

        string json = JsonUtility.ToJson(creds);

        string[] exts = { "doesGalaxyExist" };
        string uri = createUri(exts);
        StartCoroutine(PostRequest(uri, json, joinGalaxyCallback));
    }

    public void ToggleGalaxyInfoPopup()
    {
        if (isUserEnabled)
        {
            galaxyInfoPopup.SetActive(!galaxyInfoPopup.activeInHierarchy);
            if (galaxyInfoPopup.activeInHierarchy)
            {
                constellationsPopup.SetActive(false);
            }
        }
    }

    public void ToggleConstellationPopup()
    {
        if (isUserEnabled)
        {
            constellationsPopup.SetActive(!constellationsPopup.activeInHierarchy);
            if (constellationsPopup.activeInHierarchy)
            {
                galaxyInfoPopup.SetActive(false);
            }
        }
    }

    public void onCreateConstellation()
    {
        if (isUserEnabled)
        {
            if (currentAction == UserActionMode.None)
            {
                currentAction = UserActionMode.CreatingConstellation;
                bottomTextHelper.SetActive(true);

                helpPopup.SetActive(true);
                helpDescription.text = "- Begin by tapping anywhere to add the first star\n- There must be at least 3 stars in a constellation to complete it\n- The stars will automatically connect one after another\n- Complete the constellation by tapping on the first node again\n- Swipe left to undo";

                undoButton.SetActive(true);
            }
        }
    }

    public void completeConstellation(GameObject constellationParent)
    {
        constellationParent.GetComponent<ConstellationHandler>().currentStat = ConstellationHandler.status.COMPLETION;
        constellationParent.GetComponent<ConstellationHandler>().UpdateLineRenderer();

        currentAction = UserActionMode.NamingConstellation;

        currentConstellation = null;
        undoButton.SetActive(false);

        textPromptDescription.text = "What would you like to name the constellation?";
        textPromptInputField.text = "";
        textPromptPopup.SetActive(true);

        currentNamingConstellation = constellationParent;

    }

    public GameObject screenTouchDownConstellationCreation(Vector2 screenPos)
    {
        if (currentAction != UserActionMode.CreatingConstellation)
        {
            return null;
        }

        if (currentConstellation == null)
        {
            currentConstellation = (GameObject)Instantiate(constellationPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            currentConstellation.transform.parent = galaxyParent.transform;

            currentConstellation.GetComponent<ConstellationHandler>().initNec();
        }

        Vector3 point = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane)).normalized;
        Vector3 point2 = point * 35;

        if (currentConstellation.GetComponent<ConstellationHandler>().firstStar != null)
        {
            if (Vector3.Distance(point2, currentConstellation.GetComponent<ConstellationHandler>().firstStar.transform.position) < 2.5f)
            {
                if (currentConstellation.GetComponent<ConstellationHandler>().stars.Count > 2)
                {
                    completeConstellation(currentConstellation);
                    return null;
                }
                else
                {
                    //must have at least three stars to complete
                    currentConstellation.GetComponent<ConstellationHandler>().currentStat = ConstellationHandler.status.WAIT_ONE;
                    return null;
                }
            }
        }

        GameObject newStar = Instantiate(starPrefab, point2, Quaternion.identity);
        newStar.transform.parent = currentConstellation.transform;

        currentConstellation.GetComponent<ConstellationHandler>().AddToStars(newStar);

        return newStar;
    }

    public void screenDragConstellationCreation(Vector2 screenPos)
    {
        if (currentAction != UserActionMode.CreatingConstellation)
        {
            return;
        }

        Vector3 point = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane)).normalized;

        if (currentConstellation.GetComponent<ConstellationHandler>().currentStat == ConstellationHandler.status.CREATION)
        {
            currentConstellation.GetComponent<ConstellationHandler>().lastStar.transform.position = point * 35;
        }

        currentConstellation.GetComponent<ConstellationHandler>().UpdateLineRenderer();
    }

    public void OnUndo()
    {
        if (currentAction == UserActionMode.CreatingConstellation)
        {
            if (currentConstellation != null)
            {
                currentConstellation.GetComponent<ConstellationHandler>().Undo();
            }
        }
    }

    public void OnDoneTapped()
    {
        if(currentAction == UserActionMode.EditingConstellation)
        {
            ciManager.DoneEditingConstellation();
            currentAction = UserActionMode.None;
        }
    }

    public void HideGalaxyInfoPopup()
    {
        galaxyInfoPopup.SetActive(false);
    }

    public void HideConstellationsPopup()
    {
        constellationsPopup.SetActive(false);
    }

    public void BuildGalaxy()
    {
        CommunicationObjects.GalaxyCredentials creds = new CommunicationObjects.GalaxyCredentials();
        creds.clientConnectCode = currentUserInf[1];
        creds.secretJoinCode = currentUserInf[2];

        string json = JsonUtility.ToJson(creds);

        string[] exts = { "buildGalaxy" };
        string uri = createUri(exts);
        StartCoroutine(PostRequest(uri, json, buildGalaxyCallback));
    }

    public void clearScene()
    {
        //MUST IMPLEMENT LATER REMOVE OTHER GALAXIES
        int children = galaxyParent.transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            GameObject.Destroy(galaxyParent.transform.GetChild(i).gameObject);
        }
    }

    public void acceptEULAP()
    {
        PlayerPrefs.SetString("acceptedEULA3", "true");
        initUser();
    }

    void initUser()
    {
        newPlayerPopup.SetActive(false);
        greetingPopup.SetActive(false);
        joinGalaxyPopup.SetActive(false);
        createGalaxyPopup.SetActive(false);
        galaxyInfoPopup.SetActive(false);
        constellationsPopup.SetActive(false);
        errorPopup.SetActive(false);
        helpPopup.SetActive(false);
        undoButton.SetActive(false);
        doneButton.SetActive(false);
        textPromptPopup.SetActive(false);
        notesDataPrompt.SetActive(false);


        if (PlayerPrefs.HasKey("acceptedEULA3"))
        {
            eulaPopup.SetActive(false);
        }
        else
        {
            eulaPopup.SetActive(true);
            return;
        }

        //bottomTextHelper.SetActive(false);

        clearScene();

        currentUserInf = getUserInfo();

        if (currentUserInf == null)
        {
            newPlayerPopup.SetActive(true);
            newbieErrorMessage.SetActive(false);
            return;
        }

        isUserEnabled = true;

        //must load stuff

        BuildGalaxy();

        //PlayerPrefs.DeleteAll();
    }

    void Start()
    {
        initUser();
    }

    public IEnumerator PostRequest(string url, string json, handleRequestResponse callback)
    {
        var uwr = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        callback(uwr);
    }

    public IEnumerator PostRequestCreateGalaxy(string url, string json)
    {
        var uwr = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            yield break;
        }
        else
        {
            CommunicationObjects.GalaxyCredentials creds = JsonUtility.FromJson<CommunicationObjects.GalaxyCredentials>(uwr.downloadHandler.text);

            PlayerPrefs.SetString("JoinedGalaxyCode", creds.clientConnectCode);
            PlayerPrefs.SetString("JoinedGalaxySecretCode", creds.secretJoinCode);

            initUser();
        }

        ConstellationHandler[] consts = new ConstellationHandler[0];
        if (automaticConstellationGenerationForNew)
        {
            consts = CreateRandomConstellations();
        }

        for (int i = 0; i < consts.Length; i++)
        {
            CommunicationObjects.StarCoordinate[] points = new CommunicationObjects.StarCoordinate[consts[i].stars.Count];

            for (int j = 0; j < consts[i].stars.Count; j++)
            {
                points[j] = new CommunicationObjects.StarCoordinate();
                points[j].x = consts[i].stars[j].transform.position.x;
                points[j].y = consts[i].stars[j].transform.position.y;
                points[j].z = consts[i].stars[j].transform.position.z;
            }

            CommunicationObjects.CreateConstellationObj obj = new CommunicationObjects.CreateConstellationObj();
            obj.clientConnectCode = currentUserInf[1];
            obj.secretJoinCode = currentUserInf[2];
            obj.constellationName = "auto";
            obj.points = points;

            string json2 = JsonUtility.ToJson(obj);

            string[] exts2 = { "createConstellation" };
            string uri2 = ServerCommunication.createUri(exts2);

            var uwr2 = new UnityWebRequest(uri2, "POST");
            byte[] jsonToSend2 = new System.Text.UTF8Encoding().GetBytes(json2);
            uwr2.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend2);
            uwr2.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr2.SetRequestHeader("Content-Type", "application/json");

            //Send the request then wait here until it returns
            yield return uwr2.SendWebRequest();

            consts[i].createConstellationCallback(uwr2);
        }
    }

    private class CommunicationObjects
    {
        [Serializable]
        public class CreateGalaxyReq
        {
            public string galaxyName;
            public string galaxySecretCode;
            public string galaxyCreator;
        }

        [Serializable]
        public class GalaxyCredentials
        {
            public string clientConnectCode;
            public string secretJoinCode;
        }

        [Serializable]
        public class GalaxyExists
        {
            public string clientConnectCode;
            public string secretJoinCode;
            public int exists; //exists == 0 galxy connect code doesnt exist, exists == 1 galaxy exists, exists == 2 galaxy has incorrect secret join code
        }


        [Serializable]
        public class StarCoordinate
        {
            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        public class Constellation
        {
            public string uid;
            public string name;
            public StarCoordinate[] stars;
        }

        [Serializable]
        public class GalaxyConstruction
        {
            public string clientConnectCode;
            public string secretJoinCode;
            public string galaxyName;
            public string galaxyCreator;
            public int exists; //exists == 0 galxy connect code doesnt exist, exists == 1 galaxy exists, exists == 2 galaxy has incorrect secret join code, exists == 3 bad file read
            public Constellation[] constellations;
        }

        [Serializable]
        public class CreateConstellationObj
        {
            public string clientConnectCode;
            public string secretJoinCode;
            public string constellationName;
            public StarCoordinate[] points;
        }
    }

}
