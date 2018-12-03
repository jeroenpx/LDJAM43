using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;

public class GridMgr : MonoBehaviour
{
    //
    // TO WORK WITH ORIENTATIONS
    //
    public static Quaternion facingEast = Quaternion.AngleAxis(90, Vector3.up);
    public static Quaternion facingNorth = Quaternion.AngleAxis(0, Vector3.up);
    public static Quaternion facingSouth = Quaternion.AngleAxis(-180, Vector3.up);
    public static Quaternion facingWest = Quaternion.AngleAxis(-90, Vector3.up);

    public static Quaternion[] facings = new Quaternion[] { facingNorth, facingEast, facingSouth, facingWest };
    public static string[] bubbleOptions = new string[] { "N", "E", "S", "W", "wait", "snow" };
    public static int[] diffxFacing = new int[] { 0, 1, 0, -1 };
    public static int[] diffyFacing = new int[] { -1, 0, 1, 0 };

    // FOR STATIC BATCHING
    private List<GameObject> staticWalls = new List<GameObject>();
    private List<GameObject> staticSeparationWalls = new List<GameObject>();

    public enum Orientation
    {
        N, E, S, W
    }

    public string levelFileName = "Level1";

    // Level
    [SerializeField]
    [HideInInspector]
    private int xsize = 0;
    [SerializeField]
    [HideInInspector]
    private int ysize = 0;
    // Ground Level 0 -> 2 -> 4
    // Note: steps of 2 => hill = step of 1
    private int[,] groundLevel;
    // Which orientation is moveable? from a certain tile?
    private ObstacleObject[,] obstacles;
    // For the boundaries...
    private bool[,,] moveableDirection;
    // What is on the ground?
    private GroundObject[,] groundItems;
    // Moveable objects
    private List<MoveableObject> moveObjects = new List<MoveableObject>();


    // Options
    public float tileSize = 50f / 20f;// 20 tiles in 50 units
    public float uvTiles = 20f;// How many tiles in the texture?
    public float stepTime = 1f;// Time One step takes...
    public float shiftDim = 0.3f;
    public float shiftDimUp = 0.3f;
    public MeshFilter ground;
    public GameObject wallBatchContainer;
    public GameObject wall2BatchContainer;
    public GameObject wallPrefab;
    public GameObject golemPrefab;
    public GameObject magicTilePrefab;
    public GameObject wallGroupPrefab;
    public GameObject doorPrefab;
    public GameObject hillPrefab;
    public GameObject buttonPrefab;
    public GameObject exitPrefab;

    public List<GameObject> restartBtns;
    public List<GameObject> nextLevelBtns;
    public List<GameObject> infoMsgs;

    // Current State
    private bool started = false;
    private MoveableObject[,] wasPressure;
    private int[,,] wasX;
    private int[,,] wasY;
    private int[,,] wasLevel;

    // Score
    private int countFinished;

    [ContextMenu("Cleanup")]
    void Cleanup()
    {
        ground.mesh = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.tag != "Keep")
            {
                GameObject.DestroyImmediate(child.gameObject);
                i--;
            }
        }
        xsize = 0;
        ysize = 0;
        groundLevel = null;
        obstacles = null;
        moveableDirection = null;
        groundItems = null;
        moveObjects = new List<MoveableObject>();
    }

    [ContextMenu("Init")]
    void Init()
    {
        Cleanup();
        InitLevel(levelFileName);
    }

    // Use this for initialization
    void Start()
    {
        foreach (GameObject restart in restartBtns)
        {
            restart.SetActive(false);
        }
        foreach (GameObject infoMsg in infoMsgs)
        {
            infoMsg.SetActive(true);
        }
        foreach (GameObject nextLevelBtn in nextLevelBtns)
        {
            nextLevelBtn.SetActive(false);
        }
        countFinished = 0;


        //InitLevel("Level1");

        ReloadLevelDetail();

        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                if (groundItems[x, y] != null)
                {
                    groundItems[x, y].change += StartLevel;
                }
            }
        }

        foreach (MoveableObject moveObj in moveObjects)
        {
            moveObj.exit += delegate ()
            {
                moveObjects.Remove(moveObj);
                CountReachFinishIncrease();
            };
        }

        // Initialize the buttons - if already pressed...
        wasPressure = new MoveableObject[xsize, ysize];
        foreach (MoveableObject moveObj in moveObjects)
        {
            wasPressure[moveObj.X, moveObj.Y] = moveObj;
            if (groundItems[moveObj.X, moveObj.Y] != null)
            {
                groundItems[moveObj.X, moveObj.Y].PressureActivate();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void CountReachFinishIncrease()
    {
        foreach (GameObject nextLevelBtn in nextLevelBtns)
        {
            nextLevelBtn.SetActive(true);
        }
        countFinished++;
    }

    void StartLevel()
    {
        if (!started)
        {
            started = true;

            foreach (GameObject infoMsg in infoMsgs)
            {
                infoMsg.SetActive(false);
            }

            StartCoroutine(GameLoop());
        }
    }

    IEnumerator GameLoop()
    {
        float timeSinceStart = 0f;
        while (started)
        {
            Step();
            // Correct Facing Direction
            foreach (MoveableObject obj in moveObjects)
            {
                if (obj.Orientation != -1)
                {
                    obj.AnimSetRotation(facings[obj.Orientation]);
                }
                obj.InitAnim();
            }

            float time = 0;
            while (time < stepTime)
            {
                foreach (MoveableObject obj in moveObjects)
                {
                    if (obj.MoveDirection != -1 || obj.Level != wasLevel[obj.X, obj.Y, obj.Level/2])
                    {
                        Vector3 toPos = new Vector3((obj.X + 0.5f) * tileSize, obj.Level / 2f * tileSize, -(obj.Y + .5f) * tileSize);
                        Vector3 fromPos = new Vector3((wasX[obj.X, obj.Y, obj.Level / 2] + 0.5f) * tileSize, wasLevel[obj.X, obj.Y, obj.Level / 2] / 2f * tileSize, -(wasY[obj.X, obj.Y, obj.Level / 2] + .5f) * tileSize);

                        Debug.DrawLine(fromPos, toPos, Color.red);
                        obj.AnimMove(fromPos, toPos, time / stepTime);
                    }
                }
                yield return 0;
                time += Time.deltaTime;
                timeSinceStart += Time.deltaTime;

                if (timeSinceStart > 2)
                {
                    foreach (GameObject restart in restartBtns)
                    {
                        restart.SetActive(true);
                    }
                    timeSinceStart = -10000;
                }
            }
        }
    }

    void Step()
    {
        // 1. Calculate current heights
        // = groundLevel + boxes

        // Unblock everything
        foreach (MoveableObject moveObj in moveObjects)
        {
            moveObj.BeforeStep();
        }

        // Trigger btns
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                if (wasPressure[x, y] != null)
                {
                    if (groundItems[x, y] != null)
                    {
                        groundItems[x, y].PressureTick(wasPressure[x, y]);
                    }
                }
                else
                {
                    if (groundItems[x, y] != null)
                    {
                        groundItems[x, y].NoPressureTick();
                    }
                }
            }
        }

        // If player cannot move there, block him immediatelly
        foreach (MoveableObject moveObj in moveObjects)
        {
            if (moveObj.MoveDirection != -1 && !CanMoveThere(moveObj, moveObj.MoveDirection))
            {
                moveObj.MoveDirection = -1;
            }
        }



        for (int l = 5 - 1; l >= 0; l--)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                // Collect all "boxes" (non-moving things)
                MoveableObject[,] allMoveables = new MoveableObject[xsize, ysize];
                MoveableObject[,] boxes = new MoveableObject[xsize, ysize];
                foreach (MoveableObject moveObj in moveObjects)
                {
                    if (moveObj.Level/2 != l) {
                        // This one is one a different level... We will do it in a different iteration.
                        continue;
                    }
                    if (moveObj.MoveDirection == -1)
                    {
                        boxes[moveObj.X, moveObj.Y] = moveObj;
                    }
                    allMoveables[moveObj.X, moveObj.Y] = moveObj;
                }

                // Try to move things forward and see which positions will be taken
                int[,] moveToOccupied = new int[xsize, ysize];
                // From which position we tried to access
                bool[,,] sourceOrientation = new bool[xsize, ysize, 4];
                foreach (MoveableObject moveObj in moveObjects)
                {
                    if (moveObj.Level / 2 != l)
                    {
                        // This one is one a different level... We will do it in a different iteration.
                        continue;
                    }
                    if (moveObj.MoveDirection != -1)
                    {
                        int gotoX = moveObj.X + diffxFacing[moveObj.MoveDirection];
                        int gotoY = moveObj.Y + diffyFacing[moveObj.MoveDirection];
                        moveToOccupied[gotoX, gotoY]++;
                        sourceOrientation[gotoX, gotoY, moveObj.MoveDirection] = true;
                        // Also push all the boxes
                        while (boxes[gotoX, gotoY] != null)
                        {
                            MoveableObject box = boxes[gotoX, gotoY];
                            if (CanMoveThere(box, moveObj.MoveDirection))
                            {
                                gotoX += diffxFacing[moveObj.MoveDirection];
                                gotoY += diffyFacing[moveObj.MoveDirection];
                                moveToOccupied[gotoX, gotoY]++;
                                sourceOrientation[gotoX, gotoY, moveObj.MoveDirection] = true;
                            }
                            else
                            {
                                // box cannot move there... This whole "trail" will become blocked...
                                moveToOccupied[box.X, box.Y]++;
                                break;
                            }
                        }
                    }
                }

                for (int x = 0; x < xsize; x++)
                {
                    for (int y = 0; y < ysize; y++)
                    {
                        if (moveToOccupied[x, y] > 1)
                        {
                            // Two things tried to move to the same spot
                            // recursively disable all things pushing to this...
                            changed |= DisableOrigins(x, y, ref sourceOrientation, ref allMoveables);
                        }
                    }
                }
                // TODO: block the lower level we are walking on. (we know which blocks will move where on the parent level)
            }
        }

        // Final result known...
        // Only now do we set the MoveDirection for the boxes!
        for (int l = 5 - 1; l >= 0; l--)
        {
            MoveableObject[,] boxes = new MoveableObject[xsize, ysize];
            foreach (MoveableObject moveObj in moveObjects)
            {
                if (moveObj.Level / 2 != l)
                {
                    // This one is one a different level... We will do it in a different iteration.
                    continue;
                }
                if (moveObj.MoveDirection == -1)
                {
                    boxes[moveObj.X, moveObj.Y] = moveObj;
                }
            }
            foreach (MoveableObject moveObj in moveObjects)
            {
                if (moveObj.Level / 2 != l)
                {
                    // This one is one a different level... We will do it in a different iteration.
                    continue;
                }
                if (boxes[moveObj.X, moveObj.Y] == null)
                {
                    int gotoX = moveObj.X + diffxFacing[moveObj.MoveDirection];
                    int gotoY = moveObj.Y + diffyFacing[moveObj.MoveDirection];
                    // Also push all the boxes
                    while (boxes[gotoX, gotoY] != null)
                    {
                        boxes[gotoX, gotoY].MoveDirection = moveObj.MoveDirection;
                        gotoX += diffxFacing[moveObj.MoveDirection];
                        gotoY += diffyFacing[moveObj.MoveDirection];
                    }
                }
            }
        }

        // Already Move Everyone in Memory
        MoveableObject[,] pressure = new MoveableObject[xsize, ysize];
        wasX = new int[xsize, ysize, 5];
        wasY = new int[xsize, ysize, 5];
        wasLevel = new int[xsize, ysize, 5];
        foreach (MoveableObject moveObj in moveObjects)
        {
            int wasXthis = moveObj.X;
            int wasYthis = moveObj.Y;
            int wasLevelthis = moveObj.Level;
            bool touchingGround = true;
            if (moveObj.MoveDirection != -1)
            {
                moveObj.X += diffxFacing[moveObj.MoveDirection];
                moveObj.Y += diffyFacing[moveObj.MoveDirection];

                // Calculate the new Level
                if (moveObj.Orientation != -1)
                {
                    int newLevel = groundLevel[moveObj.X, moveObj.Y];
                    if (!IsBoxToWalkOnAt(moveObj.X, moveObj.Y, moveObj.Level))
                    {
                        moveObj.Level = newLevel;
                    }
                }
                else
                {
                    // Don't change the level if moving the block over a cliff!
                }
            }
            else
            {
                // We are not moving... Falling?
                if (moveObj.Orientation == -1)
                {
                    // We are a box...
                    int newLevel = groundLevel[moveObj.X, moveObj.Y];
                    if (newLevel < moveObj.Level)
                    {
                        moveObj.Level = Mathf.Max(moveObj.Level - 2, newLevel);
                        touchingGround = false;
                    }
                }
            }

            // Keep track of previous positions for animation...
            wasX[moveObj.X, moveObj.Y, moveObj.Level/2] = wasXthis;
            wasY[moveObj.X, moveObj.Y, moveObj.Level / 2] = wasYthis;
            wasLevel[moveObj.X, moveObj.Y, moveObj.Level / 2] = wasLevelthis;

            // Calculate the pressure
            if (touchingGround)
            {
                pressure[moveObj.X, moveObj.Y] = moveObj;
            }
        }

        // Activate & disactivate btns
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                if (pressure[x, y] != null && wasPressure[x, y] == null)
                {
                    if (groundItems[x, y] != null)
                    {
                        groundItems[x, y].PressureActivate();
                    }
                }
                if (pressure[x, y] == null && wasPressure[x, y] != null)
                {
                    if (groundItems[x, y] != null)
                    {
                        groundItems[x, y].PressureRelease();
                    }
                }
            }
        }
        wasPressure = pressure;
    }

    bool InGrid(int x, int y)
    {
        return x >= 0 && x < xsize && y >= 0 && y < ysize;
    }

    // TODO: precalculate??? Like a "block Stack height"
    // INNEFFICIENT!!!
    bool IsBoxToWalkOnAt(int gotoX, int gotoY, int myLvl) {
        int destGroundLevel = InGrid(gotoX, gotoY) ? groundLevel[gotoX, gotoY] : 100;
        foreach (MoveableObject boxBelow in moveObjects)
        {
            if (boxBelow.Orientation == -1 && boxBelow.X == gotoX && boxBelow.Y == gotoY && boxBelow.Level == myLvl - 2 && boxBelow.Level == destGroundLevel)
            {
                return true;
            }
        }
        return false;
    }

    bool CanMoveThere(MoveableObject box, int direction)
    {
        int gotoX = box.X + diffxFacing[direction];
        int gotoY = box.Y + diffyFacing[direction];
        int curGroundLevel = groundLevel[box.X, box.Y]+(IsBoxToWalkOnAt(box.X, box.Y, box.Level)?2:0);
        int destGroundLevel = InGrid(gotoX, gotoY) ? groundLevel[gotoX, gotoY] : 100;
        if (destGroundLevel < 0)
        {
            destGroundLevel = 100;
        }

        /*Debug.Log("---");
        Debug.Log("A: "+ (box.Level == curGroundLevel));
        Debug.Log("B: " + ((destGroundLevel >= curGroundLevel - box.GetMaxStepDown() && destGroundLevel <= curGroundLevel + box.GetMaxStepUp()) || IsBoxToWalkOnAt(gotoX, gotoY, box.Level)));
        Debug.Log("C: " + moveableDirection[box.X, box.Y, direction]);
        Debug.Log("D: " + !box.Waiting);
        Debug.Log("E: " + (obstacles[gotoX, gotoY] == null || obstacles[gotoX, gotoY].CanEnter(box, direction)));*/

        return box.Level == curGroundLevel && ((destGroundLevel >= curGroundLevel - box.GetMaxStepDown() && destGroundLevel <= curGroundLevel + box.GetMaxStepUp()) || IsBoxToWalkOnAt(gotoX, gotoY, box.Level)) && moveableDirection[box.X, box.Y, direction] && !box.Waiting && (obstacles[gotoX, gotoY] == null || obstacles[gotoX, gotoY].CanEnter(box, direction));
    }

    /**
     * Returns true if something changed
     */
    bool DisableOrigins(int x, int y, ref bool[,,] sourceOrientation, ref MoveableObject[,] allMoveables)
    {
        bool changed = false;
        for (int i = 0; i < 4; i++)
        {
            if (sourceOrientation[x, y, i])
            {
                int fromX = x - diffxFacing[i];
                int fromY = y - diffyFacing[i];
                if (allMoveables[fromX, fromY].MoveDirection != -1)
                {
                    allMoveables[fromX, fromY].MoveDirection = -1;
                    changed = true;
                }
                changed |= DisableOrigins(fromX, fromY, ref sourceOrientation, ref allMoveables);
            }
        }
        return changed;
    }

    public List<GameObject> store_moveObjects;
    public List<GameObject> store_obstacles;
    public List<GameObject> store_groundItems;
    public List<bool> store_moveableDirection;
    public List<int> store_groundLevel;

    private void StoreLevelDetail()
    {
        store_moveObjects = new List<GameObject>();
        store_obstacles = new List<GameObject>();
        store_groundItems = new List<GameObject>();
        store_moveableDirection = new List<bool>();
        store_groundLevel = new List<int>();

        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                store_groundLevel.Add(groundLevel[x, y]);
            }
        }
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                for (int d = 0; d < 4; d++)
                {
                    store_moveableDirection.Add(moveableDirection[x, y, d]);
                }
            }
        }
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                if (groundItems[x, y] != null)
                {
                    store_groundItems.Add(groundItems[x, y].GameObj);
                }
                else
                {
                    store_groundItems.Add(null);
                }
            }
        }
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                if (obstacles[x, y] != null)
                {
                    store_obstacles.Add(obstacles[x, y].GameObj);
                }
                else
                {
                    store_obstacles.Add(null);
                }
            }
        }
        for (int i = 0; i < moveObjects.Count; i++)
        {
            store_moveObjects.Add(moveObjects[i].GameObj);
        }
    }

    private void ReloadLevelDetail()
    {
        groundLevel = new int[xsize, ysize];
        moveableDirection = new bool[xsize, ysize, 4];
        groundItems = new GroundObject[xsize, ysize];
        obstacles = new ObstacleObject[xsize, ysize];
        moveObjects = new List<MoveableObject>();

        int i = 0;
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                groundLevel[x, y] = store_groundLevel[i];
                i++;
            }
        }
        i = 0;
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                for (int d = 0; d < 4; d++)
                {
                    moveableDirection[x, y, d] = store_moveableDirection[i];
                    i++;
                }
            }
        }
        i = 0;
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                if (store_groundItems[i] != null)
                {
                    groundItems[x, y] = store_groundItems[i].GetComponent<GroundObject>();
                }
                i++;
            }
        }
        i = 0;
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                if (store_obstacles[i] != null)
                {
                    obstacles[x, y] = store_obstacles[i].GetComponent<ObstacleObject>();
                }
                i++;
            }
        }
        for (i = 0; i < store_moveObjects.Count; i++)
        {
            moveObjects.Add(store_moveObjects[i].GetComponent<MoveableObject>());
        }
    }

    //
    // Level Reading
    //
    private void InitLevel(string levelName)
    {
        // Init random
        Random.InitState(1);

        XDocument doc = XDocument.Load("Assets/Resources/Levels/" + levelName + ".tmx");

        // Get width & height
        xsize = int.Parse(doc.Root.Attribute("width").Value);
        ysize = int.Parse(doc.Root.Attribute("height").Value);

        // Init the level
        groundLevel = new int[xsize, ysize];
        moveableDirection = new bool[xsize, ysize, 4];
        groundItems = new GroundObject[xsize, ysize];
        obstacles = new ObstacleObject[xsize, ysize];
        ActivateableObject[,] activateableObjects = new ActivateableObject[xsize, ysize];
        Button[,] buttons = new Button[xsize, ysize];
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                groundItems[x, y] = null;
                groundLevel[x, y] = 0;
                for (int d = 0; d < 4; d++)
                {
                    moveableDirection[x, y, d] = true;
                }
            }
        }
        for (int x = 0; x < xsize; x++)
        {
            moveableDirection[x, 0, (int)Orientation.W] = false;
            moveableDirection[x, ysize - 1, (int)Orientation.E] = false;
        }
        for (int y = 0; y < xsize; y++)
        {
            moveableDirection[0, y, (int)Orientation.N] = false;
            moveableDirection[xsize - 1, y, (int)Orientation.S] = false;
        }

        // Find Layers
        IEnumerable<XElement> objects = null;
        IEnumerable<XElement> heightmap = null;
        List<List<XElement>> opt = new List<List<XElement>>();
        foreach (XElement layer in doc.Root.Descendants("layer"))
        {
            XAttribute nameAttr = layer.Attribute("name");
            string name = nameAttr.Value;
            IEnumerable<XElement> children = layer.Descendants("data").Descendants("tile");
            if (name == "Objects")
            {
                objects = children;
            }
            if (name == "Height")
            {
                heightmap = children;
            }
            if (name == "Opt1")
            {
                opt.Add(new List<XElement>(children));
            }
            if (name == "Opt2")
            {
                opt.Add(new List<XElement>(children));
            }
            if (name == "Opt3")
            {
                opt.Add(new List<XElement>(children));
            }
            if (name == "Opt4")
            {
                opt.Add(new List<XElement>(children));
            }
            if (name == "Opt5")
            {
                opt.Add(new List<XElement>(children));
            }
        }

        int index;
        // Fill level height
        index = 0;
        foreach (XElement tile in heightmap)
        {
            int y = index / xsize;
            int x = index - y * xsize;
            int type = (int.Parse(tile.Attribute("gid").Value) - 1);
            int groundLevelType = type / 8;
            if (type < 0) { groundLevelType = -1; }
            groundLevel[x, y] = groundLevelType * 2;

            index++;
        }

        // Spawn Walls & stuff
        index = 0;
        foreach (XElement tile in objects)
        {
            int y = index / xsize;
            int x = index - y * xsize;
            int type = int.Parse(tile.Attribute("gid").Value) - 1;
            int region = type % 8;
            int subtype = type / 8;
            int thisLevel = groundLevel[x, y];
            Vector3 localPosition = new Vector3((x + .5f) * tileSize, thisLevel / 2 * tileSize, -(y + .5f) * tileSize);

            if (region == 1)
            {
                // Spawn people
                GameObject golem = GameObject.Instantiate(golemPrefab);
                MoveableObject golemMove = golem.GetComponent<MoveableObject>();
                golem.transform.parent = transform;
                golem.transform.localPosition = localPosition;
                golemMove.X = x;
                golemMove.Y = y;
                golemMove.Level = thisLevel;
                if (subtype < 4)
                {
                    golem.transform.localRotation = facings[subtype];
                    golemMove.Orientation = subtype;
                }
                else
                {
                    golem.transform.localRotation = facings[Random.Range(0, 4)];
                    golemMove.Orientation = -1;
                    golem.GetComponent<Golem>().Iced(true);
                }
                moveObjects.Add(golemMove);
            }
            if (region == 2 && subtype == 0)
            {
                // Button
                GameObject buttonTile = GameObject.Instantiate(buttonPrefab);
                GroundObject groundObject = buttonTile.GetComponent<GroundObject>();
                buttonTile.transform.parent = transform;
                buttonTile.transform.localPosition = localPosition;
                groundItems[x, y] = groundObject;
                buttons[x, y] = buttonTile.GetComponent<Button>();
            }
            if (region == 2 && (subtype == 1 || subtype == 2))
            {
                // Magic Tile
                bool[] optsEnabled = new bool[bubbleOptions.Length];
                int defaultOpt = -1;
                for (int i = 0; i < opt.Count; i++)
                {
                    int optValue = int.Parse(opt[i][index].Attribute("gid").Value) - 1;
                    if (optValue >= 0)
                    {
                        int optVal = optValue / 8;
                        optsEnabled[optVal] = true;
                        if (i == 0)
                        {
                            defaultOpt = optVal;
                        }
                    }
                }
                List<string> availableOptions = new List<string>();
                for (int i = 0; i < bubbleOptions.Length; i++)
                {
                    if (optsEnabled[i])
                    {
                        availableOptions.Add(bubbleOptions[i]);
                    }
                }
                GameObject magicTile = GameObject.Instantiate(magicTilePrefab);
                magicTile.transform.parent = transform;
                magicTile.transform.localPosition = localPosition;
                GroundObject groundObject = magicTile.GetComponent<GroundObject>();
                magicTile.GetComponent<ConfigureTile>().SetAvailableOptions(availableOptions);
                if (subtype == 2)
                {
                    magicTile.GetComponent<ConfigureTile>().DisableConfig();
                    if (defaultOpt != -1)
                    {
                        magicTile.GetComponent<ConfigureTile>().Select(bubbleOptions[defaultOpt]);
                    }
                }
                groundItems[x, y] = groundObject;

            }
            if (region == 5)
            {
                // Exit
                GameObject exit = GameObject.Instantiate(exitPrefab);
                GroundObject groundObject = exit.GetComponent<GroundObject>();
                exit.transform.parent = transform;
                exit.transform.localPosition = localPosition;
                exit.transform.localRotation = facings[subtype];
                groundItems[x, y] = groundObject;
            }
            if (region == 3 && subtype < 4)
            {
                // Hills
                GameObject hill = GameObject.Instantiate(hillPrefab);
                ObstacleObject hillObstactle = hill.GetComponent<ObstacleObject>();
                hill.transform.parent = transform;
                hill.transform.localPosition = localPosition;
                hill.transform.localRotation = facings[subtype];
                hill.GetComponent<Hill>().direction = subtype;
                obstacles[x, y] = hillObstactle;
                groundLevel[x, y] += 1;
            }
            if (region == 3 && subtype == 4)
            {
                // Wall
                {
                    GameObject wallTile = GameObject.Instantiate(wallGroupPrefab);
                    ObstacleObject obstacleObj = wallTile.GetComponent<ObstacleObject>();
                    wallTile.transform.parent = transform;
                    wallTile.transform.localPosition = localPosition;
                    wallTile.transform.localRotation = facingNorth;
                    staticSeparationWalls.Add(wallTile);
                    obstacles[x, y] = obstacleObj;
                }
                {
                    GameObject wallTile = GameObject.Instantiate(wallGroupPrefab);
                    wallTile.transform.parent = transform;
                    wallTile.transform.localPosition = localPosition;
                    wallTile.transform.localRotation = facingEast;
                    staticSeparationWalls.Add(wallTile);
                }
            }
            if (region == 3 && subtype == 5)
            {
                // Horiz Door
                GameObject doorTile = GameObject.Instantiate(doorPrefab);
                ObstacleObject obstacleObj = doorTile.GetComponent<ObstacleObject>();
                doorTile.transform.parent = transform;
                doorTile.transform.localPosition = localPosition;
                doorTile.transform.localRotation = facingNorth;
                staticSeparationWalls.Add(doorTile);
                obstacles[x, y] = obstacleObj;
                activateableObjects[x, y] = doorTile.GetComponent<ActivateableObject>();

            }
            if (region == 3 && subtype == 6)
            {
                // Vertical Door
                GameObject doorTile = GameObject.Instantiate(doorPrefab);
                ObstacleObject obstacleObj = doorTile.GetComponent<ObstacleObject>();
                doorTile.transform.parent = transform;
                doorTile.transform.localPosition = localPosition;
                doorTile.transform.localRotation = facingEast;
                staticSeparationWalls.Add(doorTile);
                obstacles[x, y] = obstacleObj;
                activateableObjects[x, y] = doorTile.GetComponent<ActivateableObject>();

            }
            if (region == 2 && subtype == 4)
            {
                // Horiz Wall
                GameObject wallTile = GameObject.Instantiate(wallGroupPrefab);
                ObstacleObject obstacleObj = wallTile.GetComponent<ObstacleObject>();
                wallTile.transform.parent = transform;
                wallTile.transform.localPosition = localPosition;
                wallTile.transform.localRotation = facingNorth;
                staticSeparationWalls.Add(wallTile);
                obstacles[x, y] = obstacleObj;
            }
            if (region == 2 && subtype == 5)
            {
                // Vertical Wall
                GameObject wallTile = GameObject.Instantiate(wallGroupPrefab);
                ObstacleObject obstacleObj = wallTile.GetComponent<ObstacleObject>();
                wallTile.transform.parent = transform;
                wallTile.transform.localPosition = localPosition;
                wallTile.transform.localRotation = facingEast;
                staticSeparationWalls.Add(wallTile);
                obstacles[x, y] = obstacleObj;
            }

            index++;
        }

        // Link buttons to actions...
        IEnumerable<XElement> links = doc.Root.Descendants("objectgroup").Descendants("object").Descendants("polyline");
        foreach (XElement link in links)
        {
            int pixelx = Mathf.FloorToInt(float.Parse(link.Parent.Attribute("x").Value));
            int pixely = Mathf.FloorToInt(float.Parse(link.Parent.Attribute("y").Value));
            string[] points = link.Attribute("points").Value.Split(new char[] { ' ' });
            List<Button> btns = new List<Button>();
            List<ActivateableObject> activ = new List<ActivateableObject>();
            foreach (string point in points)
            {
                string[] parts = point.Split(new char[] { ',' });
                int x = (pixelx + int.Parse(parts[0])) / 20;
                int y = (pixely + int.Parse(parts[1])) / 20;
                if (activateableObjects[x, y] != null)
                {
                    activ.Add(activateableObjects[x, y]);
                }
                if (buttons[x, y] != null)
                {
                    btns.Add(buttons[x, y]);
                }
            }
            foreach (Button btn in btns)
            {
                foreach (ActivateableObject a in activ)
                {
                    btn.objects.Add(a.GameObj);
                }
            }
        }

        float[,] shiftEast = new float[xsize + 1, ysize + 1];
        float[,] shiftNorth = new float[xsize + 1, ysize + 1];
        float[,] shiftUp = new float[xsize + 1, ysize + 1];
        for (int x = 0; x < xsize + 1; x++)
        {
            for (int y = 0; y < ysize + 1; y++)
            {
                shiftEast[x, y] = Random.Range(-shiftDim, shiftDim);
                shiftNorth[x, y] = Random.Range(-shiftDim, shiftDim);
                shiftUp[x, y] = Random.Range(-shiftDimUp, shiftDimUp);
            }
        }

        // Make ground mesh
        Mesh m = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> norms = new List<Vector3>();
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                if (groundLevel[x, y] >= 0)
                {
                    float height = groundLevel[x, y] / 2 * tileSize;

                    int v = vertices.Count;
                    uvs.Add(new Vector2(x / uvTiles, y / uvTiles));
                    vertices.Add(new Vector3(x * tileSize + shiftEast[x, y], height + shiftUp[x, y], -y * tileSize + shiftNorth[x, y]));
                    uvs.Add(new Vector2((x + 1) / uvTiles, y / uvTiles));
                    vertices.Add(new Vector3((x + 1) * tileSize + shiftEast[x + 1, y], height + shiftUp[x + 1, y], -y * tileSize + shiftNorth[x + 1, y]));
                    uvs.Add(new Vector2((x + 1) / uvTiles, (y + 1) / uvTiles));
                    vertices.Add(new Vector3((x + 1) * tileSize + shiftEast[x + 1, y + 1], height + shiftUp[x + 1, y + 1], -(y + 1) * tileSize + shiftNorth[x + 1, y + 1]));
                    uvs.Add(new Vector2(x / uvTiles, (y + 1) / uvTiles));
                    vertices.Add(new Vector3(x * tileSize + shiftEast[x, y + 1], height + shiftUp[x, y + 1], -(y + 1) * tileSize + shiftNorth[x, y + 1]));
                    norms.Add(Vector3.up);
                    norms.Add(Vector3.up);
                    norms.Add(Vector3.up);
                    norms.Add(Vector3.up);
                    triangles.Add(v);
                    triangles.Add(v + 1);
                    triangles.Add(v + 2);
                    triangles.Add(v);
                    triangles.Add(v + 2);
                    triangles.Add(v + 3);
                }
            }
        }
        m.vertices = vertices.ToArray();
        m.uv = uvs.ToArray();
        m.normals = norms.ToArray();
        m.triangles = triangles.ToArray();
        ground.mesh = m;

        // Make walls
        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                int northLevel = y == 0 ? -2 : groundLevel[x, y - 1];
                int southLevel = y == ysize - 1 ? -2 : groundLevel[x, y + 1];
                int eastLevel = x == xsize - 1 ? -2 : groundLevel[x + 1, y];
                int westLevel = x == 0 ? -2 : groundLevel[x - 1, y];
                int thisLevel = groundLevel[x, y] / 2 * 2;

                // Wall in the north
                for (int i = thisLevel; i + 1 < northLevel; i += 2)
                {
                    MakeWall(x + 0f, y - .5f, i / 2, facingSouth);
                }
                // Wall in the south
                for (int i = thisLevel; i + 1 < southLevel; i += 2)
                {
                    MakeWall(x + 0f, y + .5f, i / 2, facingNorth);
                }
                // Wall in the east
                for (int i = thisLevel; i + 1 < eastLevel; i += 2)
                {
                    MakeWall(x + .5f, y + 0f, i / 2, facingWest);
                }
                // Wall in the west
                for (int i = thisLevel; i + 1 < westLevel; i += 2)
                {
                    MakeWall(x - .5f, y + 0f, i / 2, facingEast);
                }
            }
        }

        // Do Static Batching
        //StaticBatchingUtility.Combine(staticWalls.ToArray(), wallBatchContainer);
        //StaticBatchingUtility.Combine(staticSeparationWalls.ToArray(), wall2BatchContainer);

        // Serialize some data
        StoreLevelDetail();
    }

    private void MakeWall(float x, float y, int level, Quaternion facing)
    {
        GameObject wall = GameObject.Instantiate(wallPrefab);
        wall.transform.parent = transform;
        wall.transform.localPosition = new Vector3((x + .5f) * tileSize, level * tileSize, -(y + .5f) * tileSize);
        wall.transform.localRotation = facing;
        staticWalls.Add(wall);
    }
}
