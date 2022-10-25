using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DataScroll : MonoBehaviour
{
	public GameObject emptyPrefab;
	public Material imgMat;

	public GameObject topLeft;
	public GameObject topRight;
	public GameObject bottomLeft;

	public GameObject column1Start;
	public GameObject column2Start;
	public GameObject column1End;

	public GameObject dataHolder;

	public GameObject vCamera;
	public LayerMask ignoredLayers;

	public GameObject imageShower;
	public GameObject noteText;
	public Texture2D whiteTexture;

	private float column1Height = 0.0f;
	private float column2Height = 0.0f;
	public float imageSeparator = 1.0f;
	public float scrollMult = 0.01f;

	public float viewerWidth = 0.0f;
	public float viewerHeight = 0.0f;

	public float viewerWidthReal = 0.0f;
	public float viewerHeightReal = 0.0f;

	public List<DataObj> stack = new List<DataObj>();
	public bool shouldShowData = false;
	public bool canShowData = true;

	public bool canChangeOwnTransparency = true;

	public float imageSizeMult = 20.0f;
	public float heightAdjust = 0.75f;

	private void Awake()
	{
		viewerWidthReal = Mathf.Abs(topRight.transform.position.x - topLeft.transform.position.x) / imageSizeMult;
		viewerHeightReal = Mathf.Abs(topLeft.transform.position.y - bottomLeft.transform.position.y) / imageSizeMult;

		viewerWidth = Mathf.Abs(topRight.transform.localPosition.x - topLeft.transform.localPosition.x);
		viewerHeight = Mathf.Abs(topLeft.transform.localPosition.y - bottomLeft.transform.localPosition.y);
	}

	public IEnumerator showDataAndWait(Texture texture, DataObj dobj)
    {
		shouldShowData = true;

		imageShower.transform.GetChild(0).GetComponent<MeshRenderer>().material.mainTexture = texture;
		imageShower.transform.localScale = new Vector3(-((float)texture.width / (float)texture.height), -1, 1);
		imageShower.transform.GetChild(0).GetChild(0).localPosition = new Vector3(0, -0.18f, 500);

		if (dobj.type == "noteData")
		{
			imageShower.transform.GetChild(0).GetChild(1).gameObject.GetComponent<TextMeshPro>().text = $"From: {dobj.creator}\n{dobj.text}";
			imageShower.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
		}
        else
        {
			imageShower.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
		}

		for (int i = 0; i < 20; i++)
        {
			imageShower.transform.position = vCamera.transform.position + (14 * vCamera.transform.forward);
			imageShower.transform.rotation = vCamera.transform.rotation;

			Color tmpCol = imageShower.transform.GetChild(0).GetComponent<MeshRenderer>().material.color;
			tmpCol.a = ((float)i) / 20.0f;
			imageShower.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = tmpCol;

			if(dobj.type == "noteData")
            {
				Color tmpCol2 = imageShower.transform.GetChild(0).GetChild(1).GetComponent<TextMeshPro>().faceColor;
				tmpCol2.a = ((float)i) / 20.0f;
				imageShower.transform.GetChild(0).GetChild(1).GetComponent<TextMeshPro>().faceColor = tmpCol2;
			}


			yield return null;
		}
		imageShower.transform.GetChild(0).GetChild(0).localPosition = new Vector3(0, -0.18f, 0);



		while (shouldShowData)
        {
			imageShower.transform.position = vCamera.transform.position + (14 * vCamera.transform.forward);
			imageShower.transform.rotation = vCamera.transform.rotation;

			yield return null;
		}
		imageShower.transform.GetChild(0).GetChild(0).localPosition = new Vector3(0, -0.18f, 500);

		for (int i = 0; i < 20; i++)
		{
			Color tmpCol = imageShower.transform.GetChild(0).GetComponent<MeshRenderer>().material.color;
			tmpCol.a = 1.0f - (((float)i) / 20.0f);
			imageShower.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = tmpCol;

			if (dobj.type == "noteData")
			{
				Color tmpCol2 = imageShower.transform.GetChild(0).GetChild(1).GetComponent<TextMeshPro>().faceColor;
				tmpCol2.a = 1.0f - (((float)i) / 20.0f);
				imageShower.transform.GetChild(0).GetChild(1).GetComponent<TextMeshPro>().faceColor = tmpCol2;
			}

			yield return null;
		}

		imageShower.transform.position = new Vector3(0, -500, 0);
		imageShower.transform.localScale = new Vector3(-1, -1, 1);
		imageShower.transform.GetChild(0).GetChild(0).localPosition = new Vector3(0, -0.18f, 0);
	}

	public void RTouchUpAt(Vector3 point)
    {
		if (shouldShowData)
		{
			shouldShowData = false;
			return;
		}
	}

	public void TouchUpAt(Vector3 point)
    {
        if (shouldShowData)
        {
			shouldShowData = false;
			return;
        }

        if (!canShowData)
        {
			return;
        }

		Vector3 localPoint = gameObject.transform.InverseTransformPoint(point);

		for(int i = 0; i < stack.Count; i++)
        {
			Vector3 objLoc = gameObject.transform.InverseTransformPoint(stack[i].obj.transform.position);
			float xLeft = objLoc.x;
			float xRight = objLoc.x + stack[i].width;
			float yTop = objLoc.y;
			float yBottom = objLoc.y - stack[i].height;

			if(localPoint.x > xLeft && localPoint.x < xRight)
            {
				if(localPoint.y < yTop && localPoint.y > yBottom)
                {
					StartCoroutine(showDataAndWait(stack[i].obj.GetComponent<MeshRenderer>().material.mainTexture, stack[i]));
					
					break;
                }
            }
        }
    }

    private void Update()
    {
		float biggestHeight = column1Height;
		if (column2Height > column1Height)
		{
			biggestHeight = column2Height;
		}

		if(biggestHeight > viewerHeight && canChangeOwnTransparency)
        {

			float topBound = topLeft.transform.localPosition.y;
			float bottomBound = bottomLeft.transform.localPosition.y;

			for(int i = 0; i < stack.Count; i++)
            {
				Vector3 relPos = gameObject.transform.InverseTransformPoint(stack[i].obj.transform.position);
				if(relPos.y > topBound)
                {
					float lessener = 0.0f;
					float byAmt = (relPos.y - topBound) * 50.0f;
					if (byAmt < 10.0f && byAmt > 1.0f)
					{
						lessener = 1.0f / (byAmt);
					}
					if(byAmt <= 1.0f)
                    {
						lessener = 1.0f;
                    }

					if(relPos.y - stack[i].height > topBound)
                    {
						lessener = 0.0f;
                    }
                    else
                    {
						if(lessener < 0.1f)
                        {
							lessener = 0.1f;
                        }
                    }

					Color tmpCol = stack[i].obj.GetComponent<MeshRenderer>().material.color;
					tmpCol.a = lessener;
					stack[i].obj.GetComponent<MeshRenderer>().material.color = tmpCol;

					if (stack[i].type == "noteData")
					{
						stack[i].obj.transform.GetChild(0).GetComponent<TextMeshPro>().text = "";
					}
				}
				else if(relPos.y - stack[i].height < bottomBound)
                {
					float lessener = 0.0f;
					float byAmt = (bottomBound - (relPos.y - stack[i].height)) * 50.0f;
					if(byAmt < 10.0f && byAmt > 1.0f)
					{
						lessener = 1.0f / (byAmt);
					}
					if (byAmt <= 1.0f)
					{
						lessener = 1.0f;
					}

					if(relPos.y < bottomBound)
                    {
						lessener = 0.0f;
					}
                    else
                    {
						if(lessener < 0.1f)
                        {
							lessener = 0.1f;
                        }
                    }

					Color tmpCol = stack[i].obj.GetComponent<MeshRenderer>().material.color;
					tmpCol.a = lessener;
					stack[i].obj.GetComponent<MeshRenderer>().material.color = tmpCol;

					if(stack[i].type == "noteData")
					{
						stack[i].obj.transform.GetChild(0).GetComponent<TextMeshPro>().text = "";
					}
				}
                else
                {
					Color tmpCol = stack[i].obj.GetComponent<MeshRenderer>().material.color;
					tmpCol.a = 1.0f;
					stack[i].obj.GetComponent<MeshRenderer>().material.color = tmpCol;

					if (stack[i].type == "noteData")
					{
						stack[i].obj.transform.GetChild(0).GetComponent<TextMeshPro>().text = $"From: {stack[i].creator}";
					}
				}
            }
		}

        if (canChangeOwnTransparency && biggestHeight <= viewerHeight)
        {
			for (int i = 0; i < stack.Count; i++)
			{
				Color tmpCol = stack[i].obj.GetComponent<MeshRenderer>().material.color;
				tmpCol.a = 1.0f;
				stack[i].obj.GetComponent<MeshRenderer>().material.color = tmpCol;

				if (stack[i].type == "noteData")
				{
					stack[i].obj.transform.GetChild(0).GetComponent<TextMeshPro>().text = $"From: {stack[i].creator}";
				}
			}
		}

	}

	public void ResetAllData()
    {
		for(int i = 0; i < stack.Count; i++)
        {
			UnityEngine.Object.Destroy(stack[i].obj);
			if(stack[i].type == "imageData")
            {
				UnityEngine.Object.Destroy(stack[i].obj.GetComponent<MeshRenderer>().material.mainTexture);
			}
        }
		stack.Clear();
		column1Height = 0.0f;
		column2Height = 0.0f;

		dataHolder.transform.localPosition = Vector3.zero;
    }

    public Vector3 getLocalBasedPositionFromGO(GameObject obj)
    {
		float nx = (obj.transform.localPosition.x - topLeft.transform.localPosition.x) / viewerWidth;
		float ny = (obj.transform.localPosition.y - topLeft.transform.localPosition.y) / viewerHeight;

		return new Vector3(nx, ny, 0);
	}

	public Vector3 getLocalBasedFromPercent(Vector3 vec)
    {
		float nx = (vec.x * viewerWidth) + topLeft.transform.localPosition.x;
		float ny = (vec.y * viewerHeight) + topLeft.transform.localPosition.y;

		return new Vector3(nx, ny, 0);
	}

	public void wasScroll(Vector2 old, Vector2 now)
    {
		Vector3 point = Camera.main.ScreenToWorldPoint(new Vector3(now.x, now.y, Camera.main.nearClipPlane)).normalized;

		Ray ray = new Ray(vCamera.transform.position, (point - vCamera.transform.position));
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit, 10000.0f, ~ignoredLayers))
		{
			if(hit.collider.name == "DataView")
            {
				float ydiff = now.y - old.y;

				Vector3 curPos = dataHolder.transform.localPosition;
				curPos.y += (ydiff * scrollMult * Time.deltaTime);

				float biggestHeight = column1Height;
				if(column2Height > column1Height)
                {
					biggestHeight = column2Height;
                }

				biggestHeight = biggestHeight - viewerHeight;

				if(curPos.y > biggestHeight)
                {
					curPos.y = biggestHeight;

				}


				if (curPos.y < 0)
				{
					curPos.y = 0;
				}

				dataHolder.transform.localPosition = curPos;
            }
		}
	}

	public void BuildNotesData(string creator, string text)
    {
		GameObject newEmpty = Instantiate(emptyPrefab, Vector3.zero, Quaternion.identity);
		newEmpty.transform.parent = dataHolder.transform;

		newEmpty.transform.localPosition = column1Start.transform.localPosition;
		newEmpty.transform.localRotation = Quaternion.identity;
		newEmpty.transform.localScale = Vector3.one;

		newEmpty.AddComponent<MeshFilter>();
		newEmpty.AddComponent<MeshRenderer>();

		GenerateVerticies(newEmpty);

		newEmpty.GetComponent<MeshRenderer>().material.mainTexture = whiteTexture;

		Mesh mesh = newEmpty.GetComponent<MeshFilter>().mesh;
		Vector3[] vertices = mesh.vertices;

		//00, 10, 01, 11

		//top left
		Vector3 localTopLeftPos = getLocalBasedPositionFromGO(column1Start);
		vertices[2] = localTopLeftPos;
		//top right
		Vector3 localTopRightPos = getLocalBasedPositionFromGO(column1End);
		vertices[3] = localTopRightPos;

		float height = ((localTopRightPos.x - localTopLeftPos.x) * ((float)whiteTexture.height)) / ((float)whiteTexture.width) * (viewerWidthReal / viewerHeightReal);

		//bottom left
		vertices[0] = new Vector3(localTopLeftPos.x, -1.0f * height, 0);
		//bottom right
		vertices[1] = new Vector3(localTopRightPos.x, -1.0f * height, 0);

		mesh.vertices = vertices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		float newHeight = 0.0f;
		float newWidth = 0.0f;

		if (column1Height <= column2Height)
		{
			Vector3 tmpCurPos = column1Start.transform.localPosition;
			tmpCurPos.y = tmpCurPos.y - column1Height;
			newEmpty.transform.localPosition = tmpCurPos;

			newHeight = height * viewerHeightReal * heightAdjust;
			newWidth = (localTopRightPos.x - localTopLeftPos.x) * viewerWidthReal;

			column1Height += newHeight + imageSeparator;

			stack.Add(new DataObj(newEmpty, newEmpty.transform.localPosition, newWidth, newHeight, "noteData", creator, text));
		}
		else
		{
			Vector3 tmpCurPos = column2Start.transform.localPosition;
			tmpCurPos.y = tmpCurPos.y - column2Height;
			newEmpty.transform.localPosition = tmpCurPos;

			newHeight = height * viewerHeightReal * heightAdjust;
			newWidth = (localTopRightPos.x - localTopLeftPos.x) * viewerWidthReal;

			column2Height += newHeight + imageSeparator;

			stack.Add(new DataObj(newEmpty, newEmpty.transform.localPosition, newWidth, newHeight, "noteData", creator, text));
		}

		GameObject txtChild = GameObject.Instantiate(noteText);
		txtChild.GetComponent<RectTransform>().SetParent(newEmpty.transform);
		txtChild.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(newWidth / 2, -1 * (newHeight / 2), -0.3f);
		txtChild.GetComponent<RectTransform>().localRotation = Quaternion.identity;
		txtChild.GetComponent<RectTransform>().localScale = new Vector3(0.03100065f, 0.03100065f, 0.03100065f);
		txtChild.GetComponent<TextMeshPro>().text = $"From: {creator}";
	}

	public void BuildImageData(Texture2D image)
	{
		GameObject newEmpty = Instantiate(emptyPrefab, Vector3.zero, Quaternion.identity);
		newEmpty.transform.parent = dataHolder.transform;

		newEmpty.transform.localPosition = column1Start.transform.localPosition;
		newEmpty.transform.localRotation = Quaternion.identity;
		newEmpty.transform.localScale = Vector3.one;
		
		newEmpty.AddComponent<MeshFilter>();
		newEmpty.AddComponent<MeshRenderer>();

		GenerateVerticies(newEmpty);

		newEmpty.GetComponent<MeshRenderer>().material.mainTexture = image;

		Mesh mesh = newEmpty.GetComponent<MeshFilter>().mesh;
		Vector3[] vertices = mesh.vertices;

		//00, 10, 01, 11

		//top left
		Vector3 localTopLeftPos = getLocalBasedPositionFromGO(column1Start);
		vertices[2] = localTopLeftPos;
		//top right
		Vector3 localTopRightPos = getLocalBasedPositionFromGO(column1End);
		vertices[3] = localTopRightPos;

		float height = (((localTopRightPos.x - localTopLeftPos.x) * ((float)image.height)) / ((float)image.width)) * (viewerWidthReal / viewerHeightReal);

		//bottom left
		vertices[0] = new Vector3(localTopLeftPos.x, -1.0f * height, 0);
		//bottom right
		vertices[1] = new Vector3(localTopRightPos.x, -1.0f * height, 0);

		mesh.vertices = vertices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		if (column1Height <= column2Height)
        {
			Vector3 tmpCurPos = column1Start.transform.localPosition;
			tmpCurPos.y = tmpCurPos.y - column1Height;
			newEmpty.transform.localPosition = tmpCurPos;

			float newHeight = height * viewerHeightReal * heightAdjust;
			float newWidth = (localTopRightPos.x - localTopLeftPos.x) * viewerWidthReal;

			column1Height += newHeight + imageSeparator;

			stack.Add(new DataObj(newEmpty, newEmpty.transform.localPosition, newWidth, newHeight));
        }
        else
        {
			Vector3 tmpCurPos = column2Start.transform.localPosition;
			tmpCurPos.y = tmpCurPos.y - column2Height;
			newEmpty.transform.localPosition = tmpCurPos;

			float newHeight = height * viewerHeightReal * heightAdjust;
			float newWidth = (localTopRightPos.x - localTopLeftPos.x) * viewerWidthReal;

			column2Height += newHeight + imageSeparator;

			stack.Add(new DataObj(newEmpty, newEmpty.transform.localPosition, newWidth, newHeight));
		}


	}

	private void GenerateVerticies(GameObject obj)
	{
		Mesh mesh;

		obj.GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "Procedural Grid";

		Vector3[] vertices = new Vector3[(1 + 1) * (1 + 1)];
		Vector2[] uv = new Vector2[vertices.Length];
		Vector4[] tangents = new Vector4[vertices.Length];
		Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
		for (int i = 0, y = 0; y <= 1; y++)
		{
			for (int x = 0; x <= 1; x++, i++)
			{
				vertices[i] = new Vector3(x, y);
				uv[i] = new Vector2((float)x / 1, (float)y / 1);
				tangents[i] = tangent;
			}
		}
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.tangents = tangents;

		int[] triangles = new int[1 * 1 * 6];
		for (int ti = 0, vi = 0, y = 0; y < 1; y++, vi++)
		{
			for (int x = 0; x < 1; x++, ti += 6, vi++)
			{
				triangles[ti] = vi;
				triangles[ti + 3] = triangles[ti + 2] = vi + 1;
				triangles[ti + 4] = triangles[ti + 1] = vi + 1 + 1;
				triangles[ti + 5] = vi + 1 + 2;
			}
		}
		mesh.triangles = triangles;
		mesh.RecalculateNormals();

		obj.GetComponent<MeshRenderer>().material = imgMat;
	}

	[Serializable]
	public class DataObj
    {
		public GameObject obj;
		public Vector3 oglocalPositionTopLeft;
		public float width;
		public float height;
		public float height2;

		public string type;
		public string creator;
		public string text;

		public DataObj(GameObject _obj, Vector3 _oglocalPositionTopLeft, float _width, float _height, string _type = "imageData", string _creator = "", string _text = "")
        {
			obj = _obj;
			oglocalPositionTopLeft = _oglocalPositionTopLeft;
			width = _width;
			height = _height;

			type = _type;

			creator = _creator;
			text = _text;
        }
    }

}