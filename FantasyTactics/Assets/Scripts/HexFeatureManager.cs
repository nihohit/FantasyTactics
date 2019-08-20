using UnityEngine;

public class HexFeatureManager : MonoBehaviour {

  public HexFeatureCollection[] plantCollections;

  Transform container;

  public void Clear() {
    if (container) {
      Destroy(container.gameObject);
    }
    container = new GameObject("Features Container").transform;
    container.SetParent(transform, false);
  }

  Transform PickPrefab(
    HexFeatureCollection[] collection, int level, float hash, float choice
  ) {
    if (level == 0) {
      return null;
    }

    return collection[(int) (hash * collection.Length)].Pick(choice);
  }

  private void addFeature(Vector3 position, int level, float hashA, float hashB, float hashC, float hashD, float hashE) {
    var prefab = PickPrefab(
      plantCollections, level, hashA, hashB
    );
    if (!prefab) {
      return;
    }
    var levelInverse = (4 - level);
    var scale = (6f / levelInverse) + (hashD * 2);

    Debug.Log("position is: " + position);
    var adjustedPosition = new Vector3(position.x + hashA, position.y + hashB, position.z + hashC);
    var pertrubrance = HexMetrics.Perturb(adjustedPosition, 16.0f);
    var difference = position - pertrubrance;
    Debug.Log("difference is: " + difference);
    difference.x *= (0.5f - hashA) * 2 * levelInverse;
    difference.z *= (0.5f - hashE) * 2 * levelInverse;
    Debug.Log("modified difference is: " + difference);

    Transform instance = Instantiate(prefab);
    position.y += instance.localScale.y - 1.0f;
    instance.localPosition = pertrubrance;
    instance.localRotation = Quaternion.Euler(0f, 360f * hashC, 0f);
    instance.localScale = new Vector3(scale, scale, scale);
    instance.SetParent(container, false);
  }

  public void AddFeature(HexCell cell) {
    if (cell.PlantLevel == 0) {
      return;
    }
    var position = cell.Position;
    HexHash hash = HexMetrics.SampleHashGrid(position);
    var numberOfItems = 4 - cell.PlantLevel;
    var values = hash.values();

    for (int i = 0; i < numberOfItems; i++) {
      addFeature(position, cell.PlantLevel, values[i % 5], values[(i + 1) % 5], values[(i + 2) % 5], values[(i + 3) % 5], values[(i + 4) % 5]);
    }
  }
}