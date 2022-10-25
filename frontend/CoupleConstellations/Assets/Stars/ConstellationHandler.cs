using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine;

public class ConstellationHandler : MonoBehaviour
{
    public string uid = "";
    public string cname = "";
    public List<GameObject> stars = new List<GameObject>();

    public GameObject testPrefab;
    public GameObject testEmpty;

    public GameObject activeViewManager;
    public GameObject lineRendererPrefab;
    private GameObject currentLineRenderer;

    public enum status
    {
        CREATION,
        COMPLETION,
        WAIT_ONE
    };
    public status currentStat = status.CREATION;

    //public Vector3[] drawPoints = null;
    public GameObject constellationCollider;

    public Vector3[] starPositions
    {
        get
        {
            Vector3[] ret = new Vector3[stars.Count];
            for(int i = 0; i < stars.Count; i++)
            {
                ret[i] = stars[i].transform.position;
            }
            return ret;
        }
    }

    public Vector3 averageStarPos
    {
        get
        {
            Vector3 avgPosition = new Vector3(
                stars.Average(x => x.transform.position.x),
                stars.Average(x => x.transform.position.y),
                stars.Average(x => x.transform.position.z));

            return avgPosition;
        }
    }

    static Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle) {
        var finalPos  = point - pivot;
        //Center the point around the origin
        finalPos = angle * finalPos;
        //Rotate the point.


        finalPos += pivot;
        //Move the point back to its original offset. 
        return finalPos;

    }

    public GameObject getCollider()
    {
        Vector3 avgPosition = new Vector3(
            stars.Average(x => x.transform.position.x),
            stars.Average(x => x.transform.position.y),
            stars.Average(x => x.transform.position.z));

        float avgDirectionMagnitude = (avgPosition - Camera.main.transform.position).magnitude;

        Vector3[] pointsWMagnitude = new Vector3[stars.Count];
        Vector3[] pointsNormalized = new Vector3[stars.Count];

        float biggestZ = 0;
        Vector3 biggestZVec = Vector3.zero;

        for (int i = 0; i < pointsWMagnitude.Length; i++)
        {
            pointsWMagnitude[i] = ((stars[i].transform.position - Camera.main.transform.position).normalized) * avgDirectionMagnitude;
            pointsNormalized[i] = RotateAroundPoint(pointsWMagnitude[i], new Vector3(0, 0, 0), Quaternion.Inverse(Quaternion.LookRotation(avgPosition)) * Quaternion.LookRotation(Vector3.forward));

            if (pointsNormalized[i].z > biggestZ)
            {
                biggestZ = pointsNormalized[i].z;
                biggestZVec = pointsNormalized[i];
            }

        }

        Vector3[] fixedPoints = new Vector3[stars.Count];
        List<KeyValuePair<double, Vector2>> toTriangulate = new List<KeyValuePair<double, Vector2>>();

        for (int i = 0; i < stars.Count; i++)
        {
            fixedPoints[i] = pointsNormalized[i];
            float myZ = fixedPoints[i].z;

            float necessaryMult = biggestZ / myZ;

            fixedPoints[i] *= necessaryMult;

            Vector2 vec = new Vector2(fixedPoints[i].x, fixedPoints[i].y);
            double tanVal = Math.Atan(((double)vec.y) / ((double)vec.x)) * (180 / Math.PI);

            if (vec.x > 0.0f && vec.y > 0.0f)
            {
                //dont change
            }
            if (vec.x < 0.0f && vec.y > 0.0f)
            {
                tanVal = 180.0 + tanVal;
            }
            if (vec.x < 0.0f && vec.y < 0.0f)
            {
                tanVal = 180.0 + tanVal;
            }
            if (vec.x > 0.0f && vec.y < 0.0f)
            {
                tanVal = 360.0 + tanVal;
            }

            toTriangulate.Add(new KeyValuePair<double, Vector2>(tanVal, vec));

        }

        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < toTriangulate.Count; i++)
        {
            double lowestVal = 400.0;
            Vector2 pnt = Vector2.zero;
            for (int j = 0; j < toTriangulate.Count; j++)
            {
                if (toTriangulate[j].Key < lowestVal && !points.Contains(toTriangulate[j].Value))
                {
                    lowestVal = toTriangulate[j].Key;
                    pnt = toTriangulate[j].Value;
                }
            }
            points.Add(pnt);
        }


        Triangulator tr = new Triangulator(points.ToArray());

        int[] indices = tr.Triangulate();

        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[points.Count];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(points[i].x, points[i].y, 0);
        }

        GameObject newTest = GameObject.Instantiate(testEmpty, Vector3.zero, Quaternion.identity);

        // Create the mesh
        Mesh msh = new Mesh();
        msh.vertices = vertices;
        msh.triangles = indices;
        msh.RecalculateNormals();
        msh.RecalculateBounds();

        // Set up game object with mesh;
        //newTest.AddComponent(typeof(MeshRenderer));
        MeshFilter filter = newTest.AddComponent(typeof(MeshFilter)) as MeshFilter;
        filter.mesh = msh;

        newTest.transform.position = Vector3.forward * avgDirectionMagnitude;
        newTest.transform.position = RotateAroundPoint(Vector3.forward * avgDirectionMagnitude, new Vector3(0, 0, 0), Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward)) * Quaternion.LookRotation(avgPosition));
        newTest.transform.rotation = Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward)) * Quaternion.LookRotation(avgPosition);

        newTest.AddComponent(typeof(MeshCollider));

        newTest.layer = 8;

        newTest.transform.parent = transform;
        newTest.transform.localScale = new Vector3(1.18f, 1.18f, 1.18f);
        return newTest;
    }

    public Vector3[] getDrawPoints()
    {
        Vector3 avgPosition = new Vector3(
            stars.Average(x => x.transform.position.x),
            stars.Average(x => x.transform.position.y),
            stars.Average(x => x.transform.position.z));


        int depth2 = 2 * (int)(SuperficieIrregularPolygon() / 20.0f);
        if(depth2 < 2)
        {
            depth2 = 2;
        }
        int denom = (depth2 * 2) + 1;
        List<Vector3> vecList = new List<Vector3>();

        for (int i = 0; i < stars.Count; i++)
        {
            for (int j = 0; j < stars.Count; j++)
            {
                if (i != j)
                {
                    if (Math.Abs(i - j) == 1 || (i == 0 && j == stars.Count - 1) || (j == 0 && i == stars.Count - 1))
                    {
                        for (int k = 1; k <= depth2; k++)
                        {
                            float percentage = (((float)k) / ((float)denom));
                            float nx = (percentage * stars[i].transform.position.x) + ((1 - percentage) * stars[j].transform.position.x);
                            float ny = (percentage * stars[i].transform.position.y) + ((1 - percentage) * stars[j].transform.position.y);
                            float nz = (percentage * stars[i].transform.position.z) + ((1 - percentage) * stars[j].transform.position.z);
                            vecList.Add(new Vector3(nx, ny, nz));
                        }
                    }
                }
            }
        }

        for(int i = 0; i < stars.Count; i++)
        {
            vecList.Add(stars[i].transform.position);
        }

        Vector3[] origStarPositions = vecList.ToArray();

        int depth = 4 * (int)(SuperficieIrregularPolygon() / 20.0f);
        if(depth < 4)
        {
            depth = 4;
        }

        Vector3[][] positionMesh = new Vector3[origStarPositions.Length][];
        for (int i = 0; i < origStarPositions.Length; i++)
        {
            positionMesh[i] = new Vector3[depth];
            //orig
            positionMesh[i][0] = origStarPositions[i];

            for (int j = 1; j < depth; j++)
            {
                float nx = (((((float)j) / ((float)depth))) * avgPosition.x) + (((((float)(depth - j)) / ((float)depth))) * origStarPositions[i].x);
                float ny = (((((float)j) / ((float)depth))) * avgPosition.y) + (((((float)(depth - j)) / ((float)depth))) * origStarPositions[i].y);
                float nz = (((((float)j) / ((float)depth))) * avgPosition.z) + (((((float)(depth - j)) / ((float)depth))) * origStarPositions[i].z);
                positionMesh[i][j] = new Vector3(nx, ny, nz);
            }
        }

        Vector3[] ret1 = new Vector3[(positionMesh.Length * depth) + 1];
        for (int i = 0; i < positionMesh.Length; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                ret1[(i * depth) + j] = positionMesh[i][j];
            }
        }

        ret1[ret1.Length - 1] = avgPosition;

        return ret1;
    }

    public GameObject lastStar
    {
        get
        {
            if (stars.Count == 0)
            {
                return null;
            }
            return stars.Last();
        }
    }

    public GameObject firstStar
    {
        get
        {
            if(stars.Count == 0)
            {
                return null;
            }
            return stars.First();
        }
    }

    public float SuperficieIrregularPolygon()
    {
        float temp = 0;
        int i = 0;
        for (; i < stars.Count; i++)
        {
            if (i != stars.Count - 1)
            {
                float mulA = stars[i].transform.position.x * stars[i + 1].transform.position.z;
                float mulB = stars[i + 1].transform.position.x * stars[i].transform.position.z;
                temp = temp + (mulA - mulB);
            }
            else
            {
                float mulA = stars[i].transform.position.x * stars[0].transform.position.z;
                float mulB = stars[0].transform.position.x * stars[i].transform.position.z;
                temp = temp + (mulA - mulB);
            }
        }
        temp *= 0.5f;
        return Mathf.Abs(temp);
    }

    public void Undo()
    {
        if(currentStat != status.CREATION)
        {
            return;
        }
        
        if(lastStar != null)
        {
            UnityEngine.Object.Destroy(lastStar);
            stars.Remove(lastStar);

            UpdateLineRenderer();
        }
    }

    public void AddToStars(GameObject star)
    {
        if(currentStat != status.CREATION)
        {
            if(currentStat == status.WAIT_ONE)
            {
                currentStat = status.CREATION;
            }
            return;
        }

        stars.Add(star);

        Vector3[] points = new Vector3[stars.Count];
        for(int i = 0; i < stars.Count; i++)
        {
            points[i] = stars.ElementAt(i).transform.position;
        }

        currentLineRenderer.GetComponent<LineRenderer>().positionCount = points.Length;
        currentLineRenderer.GetComponent<LineRenderer>().SetPositions(points);
    }

    public void UpdateLineRenderer()
    {
        if (currentStat != status.CREATION)
        {
            if (currentStat == status.WAIT_ONE)
            {
                currentStat = status.CREATION;
            }

            if(currentStat == status.COMPLETION)
            {
                Vector3[] pointsA = new Vector3[stars.Count + 1];
                for (int i = 0; i < stars.Count; i++)
                {
                    pointsA[i] = stars.ElementAt(i).transform.position;
                }
                pointsA[pointsA.Length - 1] = stars.ElementAt(0).transform.position;

                currentLineRenderer.GetComponent<LineRenderer>().positionCount = pointsA.Length;
                currentLineRenderer.GetComponent<LineRenderer>().SetPositions(pointsA);

                //drawPoints = getDrawPoints();
                constellationCollider = getCollider();
            }
            return;
        }

        Vector3[] points = new Vector3[stars.Count];
        for (int i = 0; i < stars.Count; i++)
        {
            points[i] = stars.ElementAt(i).transform.position;
        }

        currentLineRenderer.GetComponent<LineRenderer>().positionCount = points.Length;
        currentLineRenderer.GetComponent<LineRenderer>().SetPositions(points);
    }

    public void initNec()
    {
        currentLineRenderer = Instantiate(lineRendererPrefab, Vector3.zero, Quaternion.identity);
        currentLineRenderer.transform.parent = transform;

        currentLineRenderer.GetComponent<LineRenderer>().positionCount = 0;

        activeViewManager = GameObject.FindGameObjectWithTag("activeViewManager");
    }

    public void removeConstellationCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            //i am not error handling for this
        }
    }

    public void deleteFromServer(ServerCommunication sc)
    {
        RemoveConstellationObj obj = new RemoveConstellationObj();
        obj.clientConnectCode = sc.currentUserInf[1];
        obj.secretJoinCode = sc.currentUserInf[2];
        obj.constellationUID = uid;

        string json = JsonUtility.ToJson(obj);

        string[] exts = { "removeConstellation" };
        string uri = ServerCommunication.createUri(exts);

        StartCoroutine(sc.PostRequest(uri, json, removeConstellationCallback));

        UnityEngine.Object.Destroy(gameObject);


    }

    public void editConstellationCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            //i am not error handling for this
        }
    }

    public void UpdateConstellationPointsToServer(ServerCommunication sc)
    {
        EditConstellationPointsObj obj = new EditConstellationPointsObj();
        obj.clientConnectCode = sc.currentUserInf[1];
        obj.secretJoinCode = sc.currentUserInf[2];
        obj.constellationUID = uid;

        SimplePoint[] spoints = new SimplePoint[stars.Count];

        for(int i = 0; i < stars.Count; i++)
        {
            spoints[i] = new SimplePoint();
            spoints[i].x = stars[i].transform.position.x;
            spoints[i].y = stars[i].transform.position.y;
            spoints[i].z = stars[i].transform.position.z;
        }

        obj.points = spoints;

        string json = JsonUtility.ToJson(obj);

        string[] exts = { "editconstellation", "editPoints" };
        string uri = ServerCommunication.createUri(exts);

        StartCoroutine(sc.PostRequest(uri, json, editConstellationCallback));
    }

    public void createConstellationCallback(UnityWebRequest uwr)
    {
        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            RetCreateConstellationObj ret = JsonUtility.FromJson<RetCreateConstellationObj>(uwr.downloadHandler.text);

            if(ret.exists == 1)
            {
                uid = ret.constellationUID;
            }
            else
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
        }
    }

    public void addToServer(ServerCommunication sc)
    {
        SimplePoint[] points = new SimplePoint[stars.Count];

        for(int i = 0; i < stars.Count; i++)
        {
            points[i] = new SimplePoint();
            points[i].x = stars[i].transform.position.x;
            points[i].y = stars[i].transform.position.y;
            points[i].z = stars[i].transform.position.z;
        }

        CreateConstellationObj obj = new CreateConstellationObj();
        obj.clientConnectCode = sc.currentUserInf[1];
        obj.secretJoinCode = sc.currentUserInf[2];
        obj.constellationName = cname;
        obj.points = points;

        string json = JsonUtility.ToJson(obj);

        string[] exts = { "createConstellation" };
        string uri = ServerCommunication.createUri(exts);

        StartCoroutine(sc.PostRequest(uri, json, createConstellationCallback));
    }

    [Serializable]
    public class SimplePoint
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class CreateConstellationObj
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public string constellationName;
        public SimplePoint[] points;
    }

    [Serializable]
    public class RetCreateConstellationObj
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public int exists;
        public string constellationUID;
        public string constellationName;
    }

    [Serializable]
    public class RemoveConstellationObj
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public string constellationUID;
    }

    [Serializable]
    public class EditConstellationPointsObj
    {
        public string clientConnectCode;
        public string secretJoinCode;
        public string constellationUID;
        public SimplePoint[] points;
    }
}
