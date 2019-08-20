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

  public void AddFeature(HexCell cell) {
    var position = cell.Position;
    HexHash hash = HexMetrics.SampleHashGrid(position);
    var prefab = PickPrefab(
      plantCollections, cell.PlantLevel, hash.c, hash.d
    );
    if (!prefab) {
      return;
    }

    Transform instance = Instantiate(prefab);
    position.y += instance.localScale.y - 1.0f;
    instance.localPosition = HexMetrics.Perturb(position, 16.0f);
    instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
    var scale = 6f + (hash.b * cell.PlantLevel);
    instance.localScale = new Vector3(scale, scale, scale);
    instance.SetParent(container, false);
  }
}