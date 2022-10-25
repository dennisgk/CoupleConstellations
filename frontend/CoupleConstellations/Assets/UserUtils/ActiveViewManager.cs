using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActiveViewManager : MonoBehaviour
{
    public enum ViewModes
    {
        ATTACH_TO_GALAXY,
        FOLLOW_CAMERA
    }

    public ViewModes currentViewMode = ViewModes.ATTACH_TO_GALAXY;

    public ServerCommunication scManager;
    public SwipeDetector sdManager;

    public GameObject vCamera;
    public float hoverDistance = 10.0f;
    
    public GameObject cInfo;
    public GameObject[] cInfoTransparencyChanges;
    public float cInfoSpeed = 5.0f;
    public float cInfoDistFromStar = 6.0f;

    public GameObject testPrefab;
    public GameObject optionsSubMenu;

    private float cInfoTransparency = 0.0f;
    public float transparencyChangeSpeed = 10.0f;
    public cInfoManager ciManager;
    public DataScroll dsManager;

    public LayerMask ignoredLayers;
    public LayerMask ignoredLayersForConstellations;

    public Vector3 cameraForward35
    {
        get
        {
            return (vCamera.transform.forward * 35);
        }
    }

    private void lerpCInfo(ConstellationHandler closestConstellation, Vector3 raycastPoint)
    {

        /*Vector3 avgLoc = closestConstellation.averageStarPos;
        Vector3 highMidpoint = Vector3.Lerp(vCamera.transform.position, avgLoc, 0.5f) + (vCamera.transform.up * 5);

        Plane plane = new Plane(vCamera.transform.position, avgLoc, highMidpoint);

        Vector3[] starsPos = closestConstellation.starPositions;

        Vector3 displayLoc = avgLoc + (-1 * cInfoDistFromStar * plane.normal);
        float curFarthestDist = Vector3.Distance(avgLoc, displayLoc);

        for (int i = 0; i < starsPos.Length; i++)
        {
            if (!plane.GetSide(starsPos[i]))
            {
                //position is on left
                Vector3 tmpDisplayLoc = (starsPos[i] + (-1 * cInfoDistFromStar * plane.normal));
                float dist = Vector3.Distance(avgLoc, tmpDisplayLoc);
                if (dist > curFarthestDist)
                {
                    curFarthestDist = dist;
                    displayLoc = tmpDisplayLoc;
                }
            }

        }

        displayLoc = displayLoc + (cInfo.transform.forward * -12.0f);*/

        Vector3 onCollider = closestConstellation.averageStarPos;
        onCollider = onCollider + (-11.0f * cInfo.transform.forward) + (15.5f * cInfo.transform.right);

        cInfo.transform.position = Vector3.Lerp(cInfo.transform.position, onCollider, cInfoSpeed * Time.deltaTime);
        cInfo.transform.rotation = vCamera.transform.rotation;
    }

    private void lerpCInfoCam()
    {

        Vector3 displayLoc = vCamera.transform.position + (vCamera.transform.forward * 21.5f) + (vCamera.transform.right * 15.0f);

        cInfo.transform.position = Vector3.Lerp(cInfo.transform.position, displayLoc, cInfoSpeed * Time.deltaTime);
        cInfo.transform.rotation = vCamera.transform.rotation;
    }

    private void Start()
    {
        optionsSubMenu.SetActive(false);
        for(int i = 0; i < cInfoTransparencyChanges.Length; i++)
        {
            if (cInfoTransparencyChanges[i].GetComponent<CanGoTransparent>().transparencyCanChange == 1)
            {
                Color tmpCol = cInfoTransparencyChanges[i].GetComponent<Renderer>().material.color;
                tmpCol.a = cInfoTransparency;
                cInfoTransparencyChanges[i].GetComponent<Renderer>().material.color = tmpCol;
            }
            if (cInfoTransparencyChanges[i].GetComponent<CanGoTransparent>().transparencyCanChange == 2)
            {
                if (cInfoTransparency >= 0.5f)
                {
                    cInfoTransparencyChanges[i].SetActive(true);
                }
                else
                {
                    cInfoTransparencyChanges[i].SetActive(false);
                }
            }
        }

    }

    public void TouchUpAt(Vector2 screenPos)
    {
        Vector3 point = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane)).normalized;

        Ray ray = new Ray(vCamera.transform.position, (point - vCamera.transform.position));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10000.0f, ~ignoredLayers))
        {
            if (hit.collider.gameObject.activeInHierarchy)
            {
                if (hit.collider.name == "PlusButton")
                {
                    ciManager.PlusButtonPressed();
                }
                else if (hit.collider.name == "CogButton")
                {
                    //ciManager.CogButtonPressed();
                    optionsSubMenu.SetActive(!optionsSubMenu.activeInHierarchy);
                }
                else if (hit.collider.name == "ImagesButton")
                {
                    ciManager.ImagesButtonPressed();
                }
                else if (hit.collider.name == "NotesButton")
                {
                    ciManager.NotesButtonPressed();
                }
                else if (hit.collider.name == "RefreshButton")
                {
                    ciManager.RefreshButtonPressed();
                }
                else if (hit.collider.name == "DeleteButton")
                {
                    optionsSubMenu.SetActive(false);

                    ciManager.DeleteConstellation();
                }
                else if (hit.collider.name == "EditPointsButton")
                {
                    optionsSubMenu.SetActive(false);
                    ciManager.EditConstellation();
                }
                else if (hit.collider.name == "FlagConstellationButton")
                {
                    optionsSubMenu.SetActive(false);
                    scManager.helpDescription.text = "- This constellation has been flagged as inappropriate\n- It will be reported to the servers and admin";
                    scManager.helpPopup.SetActive(true);
                }
                else if (hit.collider.name == "DataView")
                {
                    dsManager.TouchUpAt(hit.point);
                }
                else if (hit.collider.name == "PinButton")
                {
                    if (currentViewMode == ViewModes.ATTACH_TO_GALAXY)
                    {
                        if (!string.IsNullOrEmpty(ciManager.curConstellationUID))
                        {
                            currentViewMode = ViewModes.FOLLOW_CAMERA;
                            ciManager.SetZ(ciManager.pinButton, ciManager.popinZs);
                        }
                    }
                    else if (currentViewMode == ViewModes.FOLLOW_CAMERA)
                    {
                        currentViewMode = ViewModes.ATTACH_TO_GALAXY;
                        ciManager.SetZ(ciManager.pinButton, ciManager.popoutZs);
                    }
                }
                else
                {
                    dsManager.RTouchUpAt(hit.point);
                }
            }
            else
            {
                dsManager.RTouchUpAt(hit.point);
            }

        }
        else
        {
            dsManager.RTouchUpAt(hit.point);
        }
    }

    private void UpdateCInfoTransparency()
    {
        for (int i = 0; i < cInfoTransparencyChanges.Length; i++)
        {
            if (cInfoTransparencyChanges[i].GetComponent<CanGoTransparent>().transparencyCanChange == 1)
            {
                float transparencyChange = transparencyChangeSpeed * Time.deltaTime;

                Color tmpCol = cInfoTransparencyChanges[i].GetComponent<Renderer>().material.color;
                if (tmpCol.a > cInfoTransparency)
                {
                    if (tmpCol.a >= transparencyChange)
                    {
                        tmpCol.a -= transparencyChange;
                    }
                    else
                    {
                        tmpCol.a = 0.0f;
                    }
                }

                if (tmpCol.a < cInfoTransparency)
                {
                    if ((1.0f - tmpCol.a) >= transparencyChange)
                    {
                        tmpCol.a += transparencyChange;
                    }
                    else
                    {
                        tmpCol.a = 1.0f;
                    }
                }

                cInfoTransparencyChanges[i].GetComponent<Renderer>().material.color = tmpCol;
            }
            if (cInfoTransparencyChanges[i].GetComponent<CanGoTransparent>().transparencyCanChange == 2)
            {
                if (cInfoTransparency >= 0.5f)
                {
                    cInfoTransparencyChanges[i].SetActive(true);
                }
                else
                {
                    cInfoTransparencyChanges[i].SetActive(false);
                }
            }
        }

        for(int i = 0; i < dsManager.stack.Count; i++)
        {

            Color tmpCol = dsManager.stack[i].obj.GetComponent<MeshRenderer>().material.color;
            if (cInfoTransparency < 0.5f)
            {
                tmpCol.a = 0.0f;
            }

            if (dsManager.stack[i].type == "noteData")
            {
                if (cInfoTransparency >= 0.5f)
                {
                    if(dsManager.stack[i].obj.transform.GetChild(0).GetComponent<TextMeshPro>().text != $"From: {dsManager.stack[i].creator}")
                    {
                        dsManager.stack[i].obj.transform.GetChild(0).GetComponent<TextMeshPro>().text = $"From: {dsManager.stack[i].creator}";
                    }
                }
                else
                {
                    if (dsManager.stack[i].obj.transform.GetChild(0).GetComponent<TextMeshPro>().text != "")
                    {
                        dsManager.stack[i].obj.transform.GetChild(0).GetComponent<TextMeshPro>().text = "";
                    }
                }
            }

            dsManager.stack[i].obj.GetComponent<MeshRenderer>().material.color = tmpCol;
        }

        if (cInfoTransparency >= 0.5f)
        {
            dsManager.canChangeOwnTransparency = true;
        }
        else
        {
            dsManager.canChangeOwnTransparency = false;
        }

    }

    private void Update()
    {
        if (currentViewMode == ViewModes.ATTACH_TO_GALAXY)
        {

            Ray ray = new Ray(vCamera.transform.position, vCamera.transform.forward);
            RaycastHit hit;

            GameObject[] constellationParents = GameObject.FindGameObjectsWithTag("constellationParent");
            ConstellationHandler closestConstellation = null;

            if (Physics.Raycast(ray, out hit, 10000.0f, ~ignoredLayersForConstellations))
            {
                for(int i = 0; i < constellationParents.Length; i++)
                {
                    if(hit.collider.gameObject == constellationParents[i].GetComponent<ConstellationHandler>().constellationCollider)
                    {
                        closestConstellation = constellationParents[i].GetComponent<ConstellationHandler>();
                        break;
                    }
                }
            }

            if (closestConstellation != null)
            {
                ciManager.curConstellationUID = closestConstellation.uid;
                ciManager.curConstellationName = closestConstellation.cname;
                ciManager.LoadData();
                cInfoTransparency = 0.65f;
                lerpCInfo(closestConstellation, hit.point);
            }
            else
            {
                ciManager.curConstellationUID = "";
                cInfoTransparency = 0.0f;
            }

            /*GameObject[] constellationParents = GameObject.FindGameObjectsWithTag("constellationParent");
            float closestConstellationVal = 9999.0f;
            Vector3 closestHoverPoint = Vector3.zero;
            ConstellationHandler closestConstellation = null;

            Vector3 currentHover = cameraForward35;

            for (int i = 0; i < constellationParents.Length; i++)
            {
                Vector3[] possibleHovers = constellationParents[i].GetComponent<ConstellationHandler>().drawPoints;
                float closestVal = 10000.0f;
                Vector3 closestHover = Vector3.zero;

                for (int k = 0; k < possibleHovers.Length; k++)
                {
                    float distance = Vector3.Distance(currentHover, possibleHovers[k]);
                    if (distance < closestVal)
                    {
                        closestVal = distance;
                        closestHover = possibleHovers[k];
                    }
                }

                if (closestVal < closestConstellationVal)
                {
                    closestConstellationVal = closestVal;
                    closestConstellation = constellationParents[i].GetComponent<ConstellationHandler>();
                    closestHoverPoint = closestHover;
                }
            }

            if (closestConstellation != null)
            {
                if (closestConstellationVal < hoverDistance)
                {
                    ciManager.curConstellationUID = closestConstellation.uid;
                    ciManager.curConstellationName = closestConstellation.cname;
                    ciManager.LoadData();
                    cInfoTransparency = 0.65f;
                    lerpCInfo(closestHoverPoint, closestConstellation, currentHover);
                }
                else
                {
                    ciManager.curConstellationUID = "";
                    cInfoTransparency = 0.0f;
                }
            }
            else
            {
                ciManager.curConstellationUID = "";
                cInfoTransparency = 0.0f;
            }*/
        }
        else if(currentViewMode == ViewModes.FOLLOW_CAMERA)
        {
            if (string.IsNullOrEmpty(ciManager.curConstellationUID))
            {
                cInfoTransparency = 0.0f;
            }
            else
            {
                lerpCInfoCam();
                cInfoTransparency = 0.65f;
            }
        }

        UpdateCInfoTransparency();
    }


}
