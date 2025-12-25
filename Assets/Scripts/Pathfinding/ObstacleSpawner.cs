using UnityEngine;

// Test-only: randomly spawn 1x1x1 obstacles tagged "Obstacle" on the grid.
// Note: Ensure you create a Tag named "Obstacle" in Unity's Tag Manager.
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Area (Grid)")]
    public int width = 300;
    public int height = 300;
    public int offsetX = -150;
    public int offsetZ = -150;

    [Header("Randomization")]
    [Range(0f, 1f)] public float density = 0.35f; // probability per cell, thousands overall
    public int seed = 12345;

    [Header("Obstacle Settings")]
    public Material obstacleMaterial; // optional visual

    void Start()
    {
        Random.InitState(seed);
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (Random.value <= density)
                {
                    SpawnPillar(offsetX + x, offsetZ + z);
                }
            }
        }
    }

    void SpawnPillar(int gx, int gz)
    {
        // Stack 3 cubes vertically: y, y+1, y+2
        float baseY = transform.position.y;
        for (int i = 0; i < 3; i++)
        {
            SpawnObstacle(gx, gz, baseY + i);
        }
    }

    void SpawnObstacle(int gx, int gz, float y)
    {
        Vector3 pos = new Vector3(gx, y, gz);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1f, 1f, 1f); // strict 1x1x1
        go.name = $"Obstacle_{gx}_{gz}_{y:0}";
        go.tag = "Obstacle";
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && obstacleMaterial != null)
        {
            mr.material = obstacleMaterial;
        }
        var col = go.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = false;
            col.size = Vector3.one; // ensure 1x1x1
            col.center = Vector3.zero;
        }
        go.transform.SetParent(transform);
    }
}
