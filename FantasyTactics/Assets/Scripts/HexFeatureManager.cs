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
    HexFeatureCollection[] collection,
    int level, float hash, float choice
  ) {
    if (level == 0) {
      return null;
    }

    float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
    for (int i = 0; i < thresholds.Length; i++) {
      if (hash < thresholds[i]) {
        return collection[i].Pick(choice);
      }
    }
    return null;
  }

  public void AddFeature(HexCell cell, Vector3 position) {
    HexHash hash = HexMetrics.SampleHashGrid(position);
    var prefab = PickPrefab(
      plantCollections, cell.PlantLevel, hash.c, hash.d
    );
    if (!prefab) {
      return;
    }

    Transform instance = Instantiate(prefab);
    position.y += instance.localScale.y * 0.5f;
    instance.localPosition = HexMetrics.Perturb(position);
    instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
    instance.SetParent(container, false);
  }
}