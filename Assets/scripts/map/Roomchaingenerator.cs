using System.Collections.Generic;
using UnityEngine;

public class RoomChainGenerator : MonoBehaviour
{
    public enum PlaneNormal { X = 0, Z = 2 }
    public GameObject startingRoomPrefab;
    public GameObject[] roomPrefabs;
    public int numberOfRooms = 10;
    public Vector3 startPosition = Vector3.zero;
    public PlaneNormal planeNormal = PlaneNormal.Z;
    public Vector3 dungeonRotation = new Vector3(-90f, 0f, 0f);
    public Transform player;
    public string playerSpawnName = "Player Spawn";
    public float spawnHeightOffset = 0.5f;
    public GameObject[] enemyPrefabs;
    public bool skipStartRoom = true;
    public float enterMargin = 0.5f;
    public float enemyTriggerDistance = 8f;
    public float enemySpawnHeightOffset = 0.5f;
    public bool addMeshColliders = true;
    public bool allowFlatTurns = true;
    public bool keepOnSamePlane = true;
    public bool snapDoorFaces = true;
    public float overlapPadding = 0.3f;
    public bool verboseLog = true;

    private struct DoorSlot
    {
        public Transform marker;
        public Transform room;
    }

    private static readonly float[] turnAngles = { 0f, 90f, 180f, 270f };

    private readonly List<DoorSlot> availableExits = new List<DoorSlot>();
    private readonly List<Transform> placedRooms = new List<Transform>();

    private int normalIdx;  
    private int axisA;       
    private int axisB;      
    private Vector3 normalVec;
    private Vector3 dirA;
    private Vector3 dirB;
    private float planeLevel;

    private void Start()
    {
        GenerateDungeon();
    }

    private void SetupPlane()
    {
        normalIdx = (int)planeNormal;
        axisA = (normalIdx + 1) % 3;
        axisB = (normalIdx + 2) % 3;

        normalVec = AxisVector(normalIdx);
        dirA = AxisVector(axisA);
        dirB = AxisVector(axisB);
    }

    private static Vector3 AxisVector(int index)
    {
        Vector3 v = Vector3.zero;
        v[index] = 1f;
        return v;
    }

    public void GenerateDungeon()
    {
        availableExits.Clear();
        placedRooms.Clear();
        SetupPlane();

        if (startingRoomPrefab == null)
        {
            Debug.LogError("Starting Room Prefab is not assigned!");
            return;
        }

        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            Debug.LogError("No room prefabs assigned!");
            return;
        }

        GameObject startRoom = Instantiate(
            startingRoomPrefab,
            startPosition,
            startingRoomPrefab.transform.rotation);

        placedRooms.Add(startRoom.transform);

        planeLevel = TryGetBounds(startRoom.transform, out Bounds startBounds)
            ? startBounds.min[normalIdx]
            : startRoom.transform.position[normalIdx];

        if (verboseLog)
        {
            Debug.Log("RoomChainGenerator: laying rooms out in the plane with " +
                "normal axis " + planeNormal + ". Turns happen around " +
                planeNormal + ".");
        }

        AddRoomExits(startRoom.transform, null);

        if (availableExits.Count == 0)
        {
            Debug.LogWarning("The starting room has no Exit markers " +
                "(objects named Exit, Exit1, Exit2...).");
        }

        int placed = 0;

        while (placed < numberOfRooms && availableExits.Count > 0)
        {
            int exitIndex = Random.Range(0, availableExits.Count);
            DoorSlot target = availableExits[exitIndex];
            availableExits.RemoveAt(exitIndex);

            if (TryPlaceRoomAt(target))
                placed++;
        }

        ParentAndOrientDungeon();
        SpawnPlayer();
        SetupEnemyTriggers();

        if (placed < numberOfRooms)
        {
            Debug.LogWarning("Only placed " + placed + " of " + numberOfRooms +
                " rooms. Check the Console for the reason on each exit.");
        }
    }

    private void ParentAndOrientDungeon()
    {
        GameObject root = new GameObject("Generated Dungeon");
        root.transform.position = startPosition;

        foreach (Transform room in placedRooms)
        {
            if (addMeshColliders)
                EnsureColliders(room);

            room.SetParent(root.transform, true);
        }

        root.transform.rotation = Quaternion.Euler(dungeonRotation);
    }

    private Transform resolvedPlayer;

    // Adds a RoomEnemyTrigger to every room so enemies appear when the player walk in
    private void SetupEnemyTriggers()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            if (verboseLog)
                Debug.Log("No Enemy Prefabs assigned, so no enemies will spawn.");
            return;
        }

        if (resolvedPlayer == null)
        {
            Debug.LogWarning("No player found, so enemy triggers were not set up.");
            return;
        }

        GameObject container = new GameObject("Spawned Enemies");

        for (int i = 0; i < placedRooms.Count; i++)
        {
            if (i == 0 && skipStartRoom)
                continue;

            RoomEnemyTrigger trigger =
                placedRooms[i].gameObject.AddComponent<RoomEnemyTrigger>();

            trigger.Setup(
                resolvedPlayer,
                enemyPrefabs,
                container.transform,
                enterMargin,
                enemySpawnHeightOffset);
        }
    }

    // Moves the player onto the spawn marker.
    private void SpawnPlayer()
    {
        Transform p = player;

        if (p == null)
        {
            GameObject found = null;

            try { found = GameObject.FindGameObjectWithTag("Player"); }
            catch (UnityException) { } 

            if (found == null)
                found = GameObject.Find("Player");

            if (found != null)
                p = found.transform;
        }

        if (p == null)
        {
            Debug.LogWarning("No player found. Assign one to the Player field.");
            return;
        }

        resolvedPlayer = p;

        Transform marker = FindMarker(playerSpawnName, out Transform markerRoom);
        if (marker == null)
        {
            Debug.LogWarning("No marker named '" + playerSpawnName +
                "' found in the generated rooms.");
            return;
        }

        // Click these Console messages to highlight the marker in the Hierarchy.
        if (markerRoom != placedRooms[0])
        {
            Debug.LogWarning(
                "The starting room has no '" + playerSpawnName + "' marker, so the " +
                "player was sent to the one in " + markerRoom.name + ". Set Starting " +
                "Room Prefab to the room that holds the marker, and make sure the " +
                "marker is inside the prefab asset itself, not only in the scene.",
                marker);
        }
        else if (verboseLog)
        {
            Debug.Log("Player teleported to '" + marker.name + "' in the starting room (" +
                markerRoom.name + ").", marker);
        }

        if (markerRoom != placedRooms[0])
        {
            Debug.LogWarning(
                "The starting room has no '" + playerSpawnName + "' marker, so the " +
                "player was sent to the one in " + markerRoom.name + ". Make sure " +
                "Starting Room Prefab is the room that holds the marker, and that the " +
                "marker is inside the prefab asset itself, not only in the scene copy.",
                marker);
        }
        else if (verboseLog)
        {
            Debug.Log("Player teleported to the starting room's '" +
                marker.name + "'.", marker);
        }

        Vector3 spawnPosition = marker.position + Vector3.up * spawnHeightOffset;

        CharacterController controller = p.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        p.position = spawnPosition;

        Rigidbody body = p.GetComponent<Rigidbody>();
        if (body != null)
            body.position = spawnPosition;

        if (controller != null)
            controller.enabled = true;

        Physics.SyncTransforms();
    }

    // Gives every mesh in the room a Mesh Collider if it has none.
    private void EnsureColliders(Transform room)
    {
        foreach (MeshFilter filter in room.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;

            if (filter.GetComponent<Collider>() != null)
                continue;

            MeshCollider meshCollider = filter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filter.sharedMesh;
        }
    }

    // Searches the starting room first
    private Transform FindMarker(string markerName, out Transform markerRoom)
    {
        string wanted = markerName.Replace(" ", "");

        foreach (Transform room in placedRooms)
        {
            foreach (Transform t in room.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Replace(" ", "").Equals(
                    wanted, System.StringComparison.OrdinalIgnoreCase))
                {
                    markerRoom = room;
                    return t;
                }
            }
        }

        markerRoom = null;
        return null;
    }

    private bool TryPlaceRoomAt(DoorSlot target)
    {
        Vector3 exitDir = GetDoorDirection(target.marker, target.room);
        if (exitDir == Vector3.zero)
        {
            if (verboseLog)
                Debug.LogWarning("Could not work out which way '" +
                    target.marker.name + "' of " + target.room.name + " faces.");
            return false;
        }

        // The new room's Entrance has to face the opposite way.
        Vector3 neededEntranceDir = -exitDir;

        int wrongSide = 0;
        int overlapped = 0;
        int noEntrance = 0;

        List<GameObject> candidates = new List<GameObject>(roomPrefabs);
        Shuffle(candidates);

        foreach (GameObject prefab in candidates)
        {
            if (prefab == null)
                continue;

            GameObject newRoom = Instantiate(
                prefab, Vector3.zero, prefab.transform.rotation);

            Transform entrance = FindEntrance(newRoom.transform);
            if (entrance == null)
            {
                noEntrance++;
                Destroy(newRoom);
                continue;
            }

            bool facingFound = false;
            bool placedThisRoom = false;

            foreach (float turn in turnAngles)
            {
                if (!allowFlatTurns && turn != 0f)
                    break;

                Vector3 euler = prefab.transform.eulerAngles;
                euler[normalIdx] += turn;
                Quaternion rotation = Quaternion.Euler(euler);

                newRoom.transform.SetPositionAndRotation(Vector3.zero, rotation);

                // Does the Entrance face the Exit in this turn?
                if (GetDoorDirection(entrance, newRoom.transform) != neededEntranceDir)
                    continue;

                facingFound = true;

                // 1) Put the Entrance on the Exit (inside the plane only).
                Vector3 offset = target.marker.position - entrance.position;
                offset[normalIdx] = 0f;
                newRoom.transform.position += offset;

                // 2) Slide along the door direction so the doorway walls touch.
                if (snapDoorFaces &&
                    TryGetBounds(target.room, out Bounds targetBounds) &&
                    TryGetBounds(newRoom.transform, out Bounds newBounds))
                {
                    float targetFace = FaceCoordinate(targetBounds, exitDir);
                    float newNearFace = FaceCoordinate(newBounds, neededEntranceDir);

                    newRoom.transform.position +=
                        exitDir * (targetFace - newNearFace);
                }

                // 3) Same depth as the starting room.
                if (keepOnSamePlane &&
                    TryGetBounds(newRoom.transform, out Bounds planeBounds))
                {
                    newRoom.transform.position +=
                        normalVec * (planeLevel - planeBounds.min[normalIdx]);
                }

                // 4) Reject if it runs into an existing room.
                if (OverlapsExistingRooms(newRoom.transform))
                {
                    overlapped++;
                    continue;
                }

                placedThisRoom = true;
                break;
            }

            if (placedThisRoom)
            {
                placedRooms.Add(newRoom.transform);
                AddRoomExits(newRoom.transform, entrance);
                return true;
            }

            if (!facingFound)
                wrongSide++;

            Destroy(newRoom);
        }

        if (verboseLog)
        {
            Debug.LogWarning(
                "Exit '" + target.marker.name + "' of " + target.room.name +
                " faces " + exitDir + ". No room fit. Rooms whose Entrance could not " +
                "face it: " + wrongSide + ". Placements rejected for overlap: " +
                overlapped + ". Rooms without an Entrance marker: " + noEntrance + ".");
        }

        return false;
    }

    private float FaceCoordinate(Bounds b, Vector3 dir)
    {
        int idx = Mathf.Abs(dir[axisA]) > 0.5f ? axisA : axisB;
        return dir[idx] > 0f ? b.max[idx] : b.min[idx];
    }

    private bool OverlapsExistingRooms(Transform newRoom)
    {
        MeshRenderer[] newRenderers =
            newRoom.GetComponentsInChildren<MeshRenderer>(true);

        foreach (Transform existing in placedRooms)
        {
            MeshRenderer[] oldRenderers =
                existing.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer a in newRenderers)
            {
                foreach (MeshRenderer b in oldRenderers)
                {
                    if (BoundsOverlapInPlane(a.bounds, b.bounds))
                        return true;
                }
            }
        }

        return false;
    }

    private bool BoundsOverlapInPlane(Bounds a, Bounds b)
    {
        float overlapA = Mathf.Min(a.max[axisA], b.max[axisA]) -
                         Mathf.Max(a.min[axisA], b.min[axisA]);
        float overlapB = Mathf.Min(a.max[axisB], b.max[axisB]) -
                         Mathf.Max(a.min[axisB], b.min[axisB]);

        return overlapA > overlapPadding && overlapB > overlapPadding;
    }

    private void Shuffle(List<GameObject> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GameObject tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private bool TryGetBounds(Transform room, out Bounds bounds)
    {
        MeshRenderer[] renderers = room.GetComponentsInChildren<MeshRenderer>(true);

        if (renderers.Length == 0)
        {
            bounds = new Bounds();
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    private Transform FindEntrance(Transform room)
    {
        foreach (Transform t in room.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Equals("Entrance",
                System.StringComparison.OrdinalIgnoreCase))
                return t;
        }
        return null;
    }

    private void AddRoomExits(Transform room, Transform usedEntrance)
    {
        foreach (Transform t in room.GetComponentsInChildren<Transform>(true))
        {
            if (t == usedEntrance)
                continue;

            if (IsExitName(t.name))
                availableExits.Add(new DoorSlot { marker = t, room = room });
        }
    }

    private bool IsExitName(string markerName)
    {
        if (markerName.Equals("Exit",
            System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (markerName.StartsWith("Exit",
            System.StringComparison.OrdinalIgnoreCase))
        {
            string suffix = markerName.Substring(4);
            return suffix.Length > 0 && int.TryParse(suffix, out _);
        }

        return false;
    }

    private Vector3 GetDoorDirection(Transform marker, Transform room)
    {
        if (!TryGetBounds(room, out Bounds bounds))
            return Vector3.zero;

        Vector3 p = marker.position;

        float best = Mathf.Abs(bounds.max[axisA] - p[axisA]);
        Vector3 dir = dirA;

        float d = Mathf.Abs(p[axisA] - bounds.min[axisA]);
        if (d < best) { best = d; dir = -dirA; }

        d = Mathf.Abs(bounds.max[axisB] - p[axisB]);
        if (d < best) { best = d; dir = dirB; }

        d = Mathf.Abs(p[axisB] - bounds.min[axisB]);
        if (d < best) { best = d; dir = -dirB; }

        return dir;
    }
}