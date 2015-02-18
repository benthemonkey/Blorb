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
		set {resourcesInternal = value; resourcePoolText.text = ((int)resourcesInternal).ToString();}
	}
	public bool collectingFromResource = false;
	public TextMesh resourcePoolText;

    public Transform turret;
    public Transform wall;
	public Transform collector;
    public Transform bottom;
    public Transform placement;
	public Transform healthbar;

    public bool[] takenSpots = new bool[8] {false, false, false, false, false, false, false, false};

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

    public void SetPlacement(int[] dir, Transform parent)
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

    public void PlacePiece(string tag, int[] direction, Transform selectedBasePiece, int cost, bool isOkay = true)
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
                
                color = (isOkay ? new Color(0.516f, 0.886f, 0.882f, 0.5f) : new Color(1f, 0.0f, 0.0f, 0.0f));
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
           
            if (selectedBasePiece == this.transform) //if we're actually the center
                ReserveSpot(BuildDirection.ToSpotFromDir(direction), selectedBasePiece.GetComponent<Center>());
            else
                ReserveSpot(BuildDirection.ToSpotFromDir(direction), selectedBasePiece.GetComponent<Attachments>());

            ReserveSpot(BuildDirection.OppositeDirection(BuildDirection.ToSpotFromDir(direction)), childBottom.GetComponentInChildren<Attachments>());

            //here we need a switch for different towers
            Transform childTower = Instantiate(piece) as Transform;
            childTower.transform.parent = childBottom;
            childTower.transform.localScale = Vector3.one;
            childTower.tag = tag;
        }

        float xOffset = (offset + placementOffset) * (float)xDirection;
        float yOffset = (offset + placementOffset) * (float)yDirection;

        childBottom.transform.position = new Vector3(selectedBasePiece.position.x + xOffset, selectedBasePiece.position.y + yOffset);

        //now check if there is a path to the center still...
        /*
        if (tag != "Placement")
        {
            //Debug.Log("ATTACHMENTS: " + GameObject.FindObjectsOfType<Attachments>().Length);
            if (GameObject.FindObjectsOfType<Attachments>().Length > 3)
            {
                if (HasPathToCenter())
                {
                    Debug.Log("HAS PATH");
                    //RemovePiece(dir, parent);
                    //PlacePiece("Placement", dir, parent, true);
                }
                else
                {
                    Debug.Log("HAS NO PATH");
                    //RemovePiece(dir, parent);
                    //PlacePiece("Placement", dir, parent, false);
                }
            }
        }
        */

		blorbAmount -= cost;
		//resourcePoolText.text = resourcePool.ToString ();
    }

    void RemovePiece(int[] dir, Transform parent)
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
        obj.takenSpots[dir] = true;
    }

    private void ReserveSpot(int dir, Center obj)
    {
        obj.takenSpots[dir] = true;
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
    public static int[] Up = new int[2] { 0, 1 };  //0
    public static int[] Right = new int[2] { 1, 0 };  //1
    public static int[] Down = new int[2] { 0, -1 };  //2
    public static int[] Left = new int[2] { -1, 0 };  //3
    public static int[] UpRight = new int[2] { 1, 1 }; //4
    public static int[] DownRight = new int[2] { 1, -1 }; //5
    public static int[] DownLeft = new int[2] { -1, -1 }; //6
    public static int[] UpLeft = new int[2] { -1, 1 }; //7

    public static bool IsSameDir(int[] l, int[] r)
    {
        if (l[0] == r[0] && l[1] == r[1]) 
           return true;

        return false;
    }
    
    public static int OppositeDirection(int dir)
    {
        return (dir + 10) % 8;
    }

    public static int ToSpotFromDir(int[] dir)
    {
        if (IsSameDir(dir, Up))
        {
            return 0;
        }
        else if (IsSameDir(dir, Right))
        {
            return 1;
        }
        else if (IsSameDir(dir, Down))
        {
            return 2;
        }
        else if (IsSameDir(dir, Left))
        {
            return 3;
        }
        else if (IsSameDir(dir, UpRight))
        {
            return 4;
        }
        else if (IsSameDir(dir, DownRight))
        {
            return 5;
        }
        else if (IsSameDir(dir, DownLeft))
        {
            return 6;
        }

        return 7;
    }

    public static int[] ToDirFromSpot(int spot)
    {
        if (spot == 0)
        {
            return BuildDirection.Up;
        }
        else if (spot == 1)
        {
            return BuildDirection.Right;
        }
        else if (spot == 2)
        {
            return BuildDirection.Down;
        }
        else if (spot == 3)
        {
            return BuildDirection.Left;
        }
        else if (spot == 4)
        {
            return BuildDirection.UpRight;
        }
        else if (spot == 5)
        {
            return BuildDirection.DownRight;
        }
        else if (spot == 6)
        {
            return BuildDirection.DownLeft;
        }
        
        return BuildDirection.UpLeft;
    }
}
