using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeDetector : MonoBehaviour
{
    private Vector2 fingerDown;
    private Vector2 fingerUp;
    private Vector2 fingerLastDragPos;
    public bool detectSwipeOnlyAfterRelease = true;

    public GameObject UIParent;
    public GameObject Canvas;

    public ServerCommunication scManager;
    public ActiveViewManager acVManager;
    public DataScroll dsManager;

    public float SWIPE_THRESHOLD = 40f;
    public float SCROLL_THRESHOLD = 20f;
    public float moveUIAnimTime = 1.0f;

    private bool canvasIsAnimating = false;

    bool checkSwipe()
    {
        //Check if Horizontal swipe
        if (horizontalValMove() > SWIPE_THRESHOLD && horizontalValMove() > verticalMove())
        {
            //Debug.Log("Horizontal");
            if (fingerDown.x - fingerUp.x > 0)//Right swipe
            {
                OnSwipeRight();
                fingerUp = fingerDown;
                return true;
            }
            else if (fingerDown.x - fingerUp.x < 0)//Left swipe
            {
                OnSwipeLeft();
                fingerUp = fingerDown;
                return true;
            }
        }
        if(Mathf.Sqrt(Mathf.Pow(verticalMove(), 2) + Mathf.Pow(horizontalValMove(), 2)) > SCROLL_THRESHOLD)
        {
            fingerUp = fingerDown;
            return true;
        }
        fingerUp = fingerDown;
        return false;
    }

    float verticalMove()
    {
        return Mathf.Abs(fingerDown.y - fingerUp.y);
    }

    float horizontalValMove()
    {
        return Mathf.Abs(fingerDown.x - fingerUp.x);
    }

    void OnSwipeLeft()
    {
        if(scManager.currentAction == ServerCommunication.UserActionMode.None)
        {
            if (!canvasIsAnimating)
            {
                canvasIsAnimating = true;
                StartCoroutine(hideUI());
            }
        }
    }

    void OnSwipeRight()
    {
        if (scManager.currentAction == ServerCommunication.UserActionMode.None)
        {
            if (!canvasIsAnimating)
            {  
                canvasIsAnimating = true;
                StartCoroutine(showUI());
            }
        }
    }

    private IEnumerator hideUI()
    {
        float canvWidth = Canvas.GetComponent<RectTransform>().rect.width;
        float distance = Vector3.Distance(new Vector3(-canvWidth, 0, 0), UIParent.GetComponent<RectTransform>().anchoredPosition);
        if(distance < 0.1f)
        {
            canvasIsAnimating = false;
        }
        else
        {
            float origTime = Time.realtimeSinceStartup;
            float nowTime = Time.realtimeSinceStartup;
            while (((nowTime - origTime) / moveUIAnimTime) < 1)
            {
                nowTime = Time.realtimeSinceStartup;
                UIParent.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(Vector3.zero, new Vector3(-canvWidth, 0, 0), (nowTime - origTime) / moveUIAnimTime);
                yield return null;
            }
            UIParent.GetComponent<RectTransform>().anchoredPosition = new Vector3(-canvWidth, 0, 0);
            canvasIsAnimating = false;
        }
    }

    private IEnumerator showUI()
    {
        float canvWidth = Canvas.GetComponent<RectTransform>().rect.width;
        float distance = Vector3.Distance(Vector3.zero, UIParent.GetComponent<RectTransform>().anchoredPosition);

        if (distance < 0.1f)
        {
            canvasIsAnimating = false;
        }
        else
        {
            float origTime = Time.realtimeSinceStartup;
            float nowTime = Time.realtimeSinceStartup;
            while (((nowTime - origTime) / moveUIAnimTime) < 1)
            {
                nowTime = Time.realtimeSinceStartup;
                UIParent.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(new Vector3(-canvWidth, 0, 0), Vector3.zero, (nowTime - origTime) / moveUIAnimTime);
                yield return null;
            }
            UIParent.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
            canvasIsAnimating = false;
        }
    }

    public void OnTouchDown()
    {
        if(Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 position = touch.position;

            fingerUp = touch.position;
            fingerDown = touch.position;
            fingerLastDragPos = touch.position;

            if (scManager.currentAction == ServerCommunication.UserActionMode.CreatingConstellation)
            {
                scManager.screenTouchDownConstellationCreation(position);
            }
        }

    }

    public void OnTouchDrag()
    {
        if(Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 position = touch.position;

            if (!detectSwipeOnlyAfterRelease)
            {
                fingerDown = touch.position;
                checkSwipe();
            }

            if (scManager.currentAction == ServerCommunication.UserActionMode.CreatingConstellation)
            {
                scManager.screenDragConstellationCreation(position);
            }
            else if(scManager.currentAction == ServerCommunication.UserActionMode.EditingConstellation)
            {
                scManager.ciManager.screenDragConstellationEditing(position);
            }
            else
            {
                fingerDown = touch.position;
                dsManager.wasScroll(fingerLastDragPos, fingerDown);
                fingerLastDragPos = fingerDown;
            }
        }
    }

    public void OnTouchUp()
    {
        if(Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 position = touch.position;

            fingerDown = touch.position;
            bool wasSwipe = checkSwipe();

            if (scManager.currentAction == ServerCommunication.UserActionMode.CreatingConstellation)
            {
                scManager.screenDragConstellationCreation(position);
            }
            else if (scManager.currentAction == ServerCommunication.UserActionMode.EditingConstellation)
            {
                scManager.ciManager.screenDragConstellationEditing(position);
            }
            else
            {
                if (!wasSwipe)
                {
                    acVManager.TouchUpAt(position);
                }
            }
        }
    }
}
