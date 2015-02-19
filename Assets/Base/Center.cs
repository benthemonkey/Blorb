﻿using UnityEngine;
using System.Collections;

public class Center : MonoBehaviour {

    private const float pixelOffset = 750.0F;
    private const float placementOffset = 0.725f;

	private float health;
	private float resourcesInternal;

    public float speed;

	public float blorbAmount
	{ 
		//made property so updates text dynamically
		get {return resourcesInternal;}
		set {resourcesInternal = value; 
			resourcePoolText.text = ((int)resourcesInternal).ToString();
			WorldManager.UpdateTowerGUI(blorbAmount);
		}
	}
	public bool collectingFromResource = false;
	public TextMesh resourcePoolText;

    public Transform turret;
    public Transform wall;
	public Transform collector;
    public Transform bottom;
    public Transform placement;
	public Transform healthbar;

    public bool[] TakenSpot
    {
        get { return takenSpots; }
        set { takenSpots = value;  }
    }

    private bool[] takenSpots = new bool[16];

    public bool isActive;

	void GameStart () {
		enabled = true;

		health = 100f;
		healthbar.localScale = new Vector2 (health * 1.5f, 1f);

		blorbAmount = 100;
		//resourcePoolText.text = resourcePool.ToString ();
		collectingFromResource = false;

		this.renderer.enabled = true;
        this.transform.FindChild("Player").renderer.enabled = true;
        isActive = true;
	}

	void OnTriggerEnter (Collider collision) {
		gameObject.SetActive (false);
	}

	// Use this for initialization
	void Start () {
		GameEventManager.GameStart += GameStart;
		enabled = false;
		this.renderer.enabled = false;
        this.transform.FindChild("Player").renderer.enabled = false;
		healthbar = this.transform.Find ("GUI/HUD/Health");
		resourcePoolText.renderer.sortingLayerName = "UI";
		resourcePoolText.renderer.sortingOrder = 1;

        for (int i = 0; i < takenSpots.Length; i++) takenSpots[i] = false;
	}

    public void FindAllPossiblePlacements()
    {
        //start with the center and iterate through children
        for (int i = 0; i < takenSpots.Length; i++)
        {
            if (!takenSpots[i])
            {
                //place down a placement spot
                SetPlacement(BuildDirection.ToDirFromSpot(i), this.transform);
            }
        }

        foreach (Transform child in this.transform)
        {
            if (child.GetComponent<Attachments>() != null)
            {
                child.GetComponent<Attachments>().FindAllPossiblePlacements(this);
            }
        }
    }

    public void RemoveAllPossiblePlacements()
    {
        GameObject[] placements = GameObject.FindGameObjectsWithTag("Placement");

        foreach (GameObject placement in placements)
        {
            Destroy(placement);
        }
    }

    public void RecalculateAllPossiblePlacements()
    {
        RemoveAllPossiblePlacements();
        FindAllPossiblePlacements();
    }

    public void SetPlacement(float[] dir, Transform parent)
    {
        //placement pieces cost 0
        PlacePiece("Placement", dir, parent, 0, true);

        //check if there is a valid path to the center
        /*
        PlacePiece("TestPiece", dir, parent, true);
        if (HasPathToCenter())
        {
            Debug.Log("HAS PATH");
            RemovePiece(dir, parent);
            PlacePiece("Placement", dir, parent, true);
        }
        else
        {
            RemovePiece(dir, parent);
            PlacePiece("Placement", dir, parent, false);
        }
         * */
        //Debug.Log("Has Path: " + HasPathToCenter());
        
    }

    void FixedUpdate()
    {
        if (!isActive) return;
        
        float inputHorizontal = Input.GetAxis("Horizontal");
        float inputVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(inputHorizontal, inputVertical, 0.0f);

        this.transform.position += movement * speed * Time.deltaTime;
        
    }

    public void PlacePiece(string tag, float[] direction, Transform selectedBasePiece, int cost, bool isOkay = true)
    {
        if (!isActive) return;

        float xDirection = (float)direction[0];
        float yDirection = (float)direction[1];

        //get the location of the top of the base
        float offset = GetOffset(selectedBasePiece);
        Transform piece = placement;
        Transform tempBottom = bottom;
        Color color = new Color(255.0f, 255.0f, 255.0f); //normal color

        switch (tag)
        {
            case "Placement":
                tempBottom = placement;
                color = (isOkay ? new Color(0.516f, 0.886f, 0.882f, 0) : new Color(1f, 0.0f, 0.0f, 1.0f));
                break;
            case "Turret":
            case "TestPiece":
                piece = turret;
                break;
            case "Wall":
                piece = wall;
                break;
            case "Collector":
                piece = collector;
                break;
        }

        Transform childBottom = Instantiate(tempBottom) as Transform;
        childBottom.transform.parent = selectedBasePiece;
        childBottom.transform.localScale = Vector3.one;
        childBottom.GetComponent<SpriteRenderer>().color = color;
        childBottom.tag = tag;

        if (tag != "Placement")
            childBottom.GetComponent<Attachments>().spot = BuildDirection.ToSpotFromDir(direction);
        if (tag == "Placement")
            childBottom.GetComponent<PlacementBottom>().spot = BuildDirection.ToSpotFromDir(direction);

        if (tag != "Placement")
        {
           
            //reserve spot taken for self
            if (selectedBasePiece == this.transform) //if we're actually the center
            {
                ReserveSpot(BuildDirection.ToSpotFromDir(direction), selectedBasePiece.GetComponent<Center>());
                ReserveSpot((BuildDirection.ToSpotFromDir(direction) + 1) % takenSpots.Length, selectedBasePiece.GetComponent<Center>());
                ReserveSpot((BuildDirection.ToSpotFromDir(direction) - 1) % takenSpots.Length, selectedBasePiece.GetComponent<Center>());

                if (BuildDirection.IsMid(direction)) //we may need to reserve the neighbors
                    ReserveSpot(BuildDirection.GetDiagonal(BuildDirection.ToSpotFromDir(direction)), selectedBasePiece.GetComponent<Center>());
            }
            else
            {
                ReserveSpot(BuildDirection.ToSpotFromDir(direction), selectedBasePiece.GetComponent<Attachments>());
                ReserveSpot((BuildDirection.ToSpotFromDir(direction) + 1) % takenSpots.Length, selectedBasePiece.GetComponent<Attachments>());
                ReserveSpot((BuildDirection.ToSpotFromDir(direction) - 1) % takenSpots.Length, selectedBasePiece.GetComponent<Attachments>());

                if (BuildDirection.IsMid(direction))
                    ReserveSpot(BuildDirection.GetDiagonal(BuildDirection.ToSpotFromDir(direction)), selectedBasePiece.GetComponent<Attachments>());
            }

            //reserve spot taken for the just placed piece (cannot build back on parent)
            ReserveSpot(BuildDirection.OppositeSpot(BuildDirection.ToSpotFromDir(direction)), childBottom.GetComponentInChildren<Attachments>());
            ReserveSpot(BuildDirection.OppositeSpot((BuildDirection.ToSpotFromDir(direction) + 1) % takenSpots.Length), childBottom.GetComponentInChildren<Attachments>());
            ReserveSpot(BuildDirection.OppositeSpot((BuildDirection.ToSpotFromDir(direction) - 1) % takenSpots.Length), childBottom.GetComponentInChildren<Attachments>());

            if (BuildDirection.IsMid(direction))
                ReserveSpot(BuildDirection.OppositeSpot(BuildDirection.GetDiagonal(BuildDirection.ToSpotFromDir(direction))), childBottom.GetComponentInChildren<Attachments>());

            //here we need a switch for different towers
            Transform childTower = Instantiate(piece) as Transform;
            childTower.transform.parent = childBottom;
            childTower.transform.localScale = Vector3.one;
            childTower.tag = tag;
        }

        float xOffset = (offset + placementOffset) * (float)xDirection;
        float yOffset = (offset + placementOffset) * (float)yDirection;

        childBottom.transform.position = new Vector3(selectedBasePiece.position.x + xOffset, selectedBasePiece.position.y + yOffset);

		blorbAmount -= cost;
		//resourcePoolText.text = resourcePool.ToString ();
    }

    void RemovePiece(float[] dir, Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.GetComponent<Attachments>() != null &&
                child.GetComponent<Attachments>().spot == BuildDirection.ToSpotFromDir(dir))
            {
                Destroy(child.gameObject);
                break;
            }
        }
    }

    private void ReserveSpot(int dir, Attachments obj)
    {
        obj.TakenSpot[dir] = true;
    }

    private void ReserveSpot(int dir, Center obj)
    {
        obj.TakenSpot[dir] = true;
    }

    private float GetOffset(Transform selectedBasePiece)
    {
        SpriteRenderer bottomRenderer = selectedBasePiece.GetComponentInChildren<SpriteRenderer>();;
        return (bottomRenderer.sprite.rect.height / pixelOffset); //height or width works because they're equivalent
    }

    public void takeDamage(float damage)
    {
        health -= damage;
		healthbar.localScale = new Vector2 (health * 1.5f, 1f);

		if (health <= 0f) {
			GameEventManager.TriggerGameOver();
		}
    }

	public void receiveBlorb(int blorb)
	{
		blorbAmount += blorb;
		//resourcePoolText.text = resourcePool.ToString ();
	}

    private bool HasPathToCenter()
    {
        Pathfinding2D finder = this.gameObject.GetComponent<Pathfinding2D>();
        finder.FindPath(new Vector3(20.0f + this.transform.position.x, 20.0f + this.transform.position.y, 0.0f), this.transform.position);
        Debug.DrawLine(new Vector3(20.0f + this.transform.position.x, 20.0f + this.transform.position.y, 0.0f),
                       this.transform.position, Color.red, 100.0f, false);

        if (finder.Path.Count > 0)
            return true;

        return false;
    }

    public bool HasEnoughResources(int cost)
    {
		return blorbAmount >= cost;
    }

    void OnCollisionEnter(Collision other)
    {
		if (other.gameObject.tag == "Enemy") { //only hit the player
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
			if (!enemy.instantDamage) {
                enemy.target = this;
				enemy.isHitting = true; //stop jittery movement
			} else {
				takeDamage(10 * enemy.hitDamage);
				Destroy (enemy.gameObject);
			}
		}
    }

    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.tag == "Enemy")
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            enemy.isHitting = false;
        }
    }
}

public abstract class BuildDirection
{
    public static float[] Up = new float[3] { 0, 1f, 0 };  //0
    public static float[] UpMid = new float[3] { 0.5f, 1f, 1f }; //1
    public static float[] UpRight = new float[3] { 1f, 1f, 2f }; //2
    public static float[] UpRightMid = new float[3] { 1f, 0.5f, 3f }; //3
    public static float[] Right = new float[3] { 1f, 0, 4f };  //4
    public static float[] RightMid = new float[3] { 1f, -0.5f, 5f }; //5
    public static float[] RightDown = new float[3] { 1f, -1f, 6f }; //6
    public static float[] RightDownMid = new float[3] { 0.5f, -1f, 7f }; //7
    public static float[] Down = new float[3] { 0, -1f, 8f };  //8
    public static float[] DownMid = new float[3] { -0.5f, -1f, 9f }; //9
    public static float[] DownLeft = new float[3] { -1f, -1f, 10f }; //10
    public static float[] DownLeftMid = new float[3] { -1f, -0.5f, 11f }; //11
    public static float[] Left = new float[3] { -1f, 0, 12f };  //12
    public static float[] LeftMid = new float[3] { -1f, 0.5f, 13f }; //13
    public static float[] LeftUp = new float[3] { -1f, 1f, 14f }; //14
    public static float[] LeftUpMid = new float[3] { -0.5f, 1f, 15f }; //15

    private static ArrayList directions = 
        new ArrayList() { Up, UpMid, UpRight, UpRightMid,
                          Right, RightMid, RightDown, RightDownMid,
                          Down, DownMid, DownLeft, DownLeftMid, 
                          Left, LeftMid, LeftUp, LeftUpMid };

    public enum Neighbor { Left, Right };
    
    public static int OppositeSpot(int spot)
    {
        return (spot + 8) % 16;
    }

    public static int ToSpotFromDir(float[] dir)
    {
        return (int)dir[2];
    }

    public static float[] ToDirFromSpot(int spot)
    {
        return (float [])directions[spot];
    }

    public static bool IsMid(float[] dir)
    {
        return (ToSpotFromDir(dir) % 2 != 0);
    }

    public static int GetDiagonal(int spot)
    {
        if (spot == 1 || spot == 5 || spot == 9 || spot == 13)
            return (spot + 2) % 16;

        if (spot == 3 || spot == 7 || spot == 11 || spot == 15)
            return (spot - 2) % 16;

        return spot; //not needed
    }
}
