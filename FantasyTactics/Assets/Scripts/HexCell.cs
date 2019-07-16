using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HexCell : MonoBehaviour {

  public HexCoordinates coordinates;

  public RectTransform uiRect;

  public HexGridChunk chunk;

  public int Index { get; set; }

  public int Elevation {
    get {
      return elevation;
    }
    set {
      if (elevation == value) {
        return;
      }
      int originalViewElevation = ViewElevation;
      elevation = value;
      if (ViewElevation != originalViewElevation) {
        ShaderData.ViewElevationChanged();
      }
      RefreshPosition();

      Refresh();
    }
  }

  public int WaterLevel {
    get {
      return waterLevel;
    }
    set {
      if (waterLevel == value) {
        return;
      }
      int originalViewElevation = ViewElevation;
      waterLevel = value;
      if (ViewElevation != originalViewElevation) {
        ShaderData.ViewElevationChanged();
      }
      Refresh();
    }
  }

  public int ViewElevation {
    get {
      return elevation >= waterLevel ? elevation : waterLevel;
    }
  }

  public bool IsUnderwater {
    get {
      return waterLevel > elevation;
    }
  }

  public Vector3 Position {
    get {
      return transform.localPosition;
    }
  }


  public float StreamBedY {
    get {
      return
        (elevation + HexMetrics.streamBedElevationOffset) *
        HexMetrics.elevationStep;
    }
  }

  public float RiverSurfaceY {
    get {
      return
        (elevation + HexMetrics.waterElevationOffset) *
        HexMetrics.elevationStep;
    }
  }

  public float WaterSurfaceY {
    get {
      return
        (waterLevel + HexMetrics.waterElevationOffset) *
        HexMetrics.elevationStep;
    }
  }

  public int PlantLevel {
    get {
      return plantLevel;
    }
    set {
      if (plantLevel != value) {
        plantLevel = value;
        RefreshSelfOnly();
      }
    }
  }

  public int TerrainTypeIndex {
    get {
      return terrainTypeIndex;
    }
    set {
      if (terrainTypeIndex != value) {
        terrainTypeIndex = value;
        ShaderData.RefreshTerrain(this);
      }
    }
  }

  public bool IsVisible {
    get {
      return visibility > 0 && Explorable;
    }
  }

  public bool IsExplored {
    get {
      return explored && Explorable;
    }
    private set {
      explored = value;
    }
  }

  public bool Explorable { get; set; }

  public int Distance {
    get {
      return distance;
    }
    set {
      distance = value;
    }
  }

  public HexUnit Unit { get; set; }

  public HexCell PathFrom { get; set; }

  public int SearchHeuristic { get; set; }

  public int SearchPriority {
    get {
      return distance + SearchHeuristic;
    }
  }

  public int SearchPhase { get; set; }

  public HexCell NextWithSamePriority { get; set; }

  public HexCellShaderData ShaderData { get; set; }

  int terrainTypeIndex;

  int elevation = int.MinValue;
  int waterLevel;

  int plantLevel;

  int distance;

  int visibility;

  bool explored;

  [SerializeField]
  HexCell[] neighbors;

  public void IncreaseVisibility() {
    visibility += 1;
    if (visibility == 1) {
      IsExplored = true;
      ShaderData.RefreshVisibility(this);
    }
  }

  public void DecreaseVisibility() {
    visibility -= 1;
    if (visibility == 0) {
      ShaderData.RefreshVisibility(this);
    }
  }

  public void ResetVisibility() {
    if (visibility > 0) {
      visibility = 0;
      ShaderData.RefreshVisibility(this);
    }
  }

  public HexCell GetNeighbor(HexDirection direction) {
    return neighbors[(int)direction];
  }

  public void SetNeighbor(HexDirection direction, HexCell cell) {
    neighbors[(int)direction] = cell;
    cell.neighbors[(int)direction.Opposite()] = this;
  }

  public HexEdgeType GetEdgeType(HexDirection direction) {
    return HexMetrics.GetEdgeType(
      elevation, neighbors[(int)direction].elevation
    );
  }

  public HexEdgeType GetEdgeType(HexCell otherCell) {
    return HexMetrics.GetEdgeType(
      elevation, otherCell.elevation
    );
  }

  public int GetElevationDifference(HexDirection direction) {
    int difference = elevation - GetNeighbor(direction).elevation;
    return difference >= 0 ? difference : -difference;
  }

  void RefreshPosition() {
    Vector3 position = transform.localPosition;
    position.y = elevation * HexMetrics.elevationStep;
    position.y +=
      (HexMetrics.SampleNoise(position).y * 2f - 1f) *
      HexMetrics.elevationPerturbStrength;
    transform.localPosition = position;

    Vector3 uiPosition = uiRect.localPosition;
    uiPosition.z = -position.y;
    uiRect.localPosition = uiPosition;
  }

  void Refresh() {
    if (chunk) {
      chunk.Refresh();
      for (int i = 0; i < neighbors.Length; i++) {
        HexCell neighbor = neighbors[i];
        if (neighbor != null && neighbor.chunk != chunk) {
          neighbor.chunk.Refresh();
        }
      }
      if (Unit) {
        Unit.ValidateLocation();
      }
    }
  }

  void RefreshSelfOnly() {
    chunk.Refresh();
    if (Unit) {
      Unit.ValidateLocation();
    }
  }

  public void Save(BinaryWriter writer) {
    writer.Write((byte)terrainTypeIndex);
    writer.Write((byte)(elevation + 127));
    writer.Write((byte)waterLevel);
    writer.Write((byte)plantLevel);
    writer.Write(IsExplored);
  }

  public void Load(BinaryReader reader, int header) {
    terrainTypeIndex = reader.ReadByte();
    ShaderData.RefreshTerrain(this);
    elevation = reader.ReadByte();
    if (header >= 4) {
      elevation -= 127;
    }
    RefreshPosition();
    waterLevel = reader.ReadByte();
    plantLevel = reader.ReadByte();

    IsExplored = header >= 3 ? reader.ReadBoolean() : false;
    ShaderData.RefreshVisibility(this);
  }

  public void SetLabel(string text) {
    UnityEngine.UI.Text label = uiRect.GetComponent<Text>();
    label.text = text;
  }

  public void DisableHighlight() {
    Image highlight = uiRect.GetChild(0).GetComponent<Image>();
    highlight.enabled = false;
  }

  public void EnableHighlight(Color color) {
    Image highlight = uiRect.GetChild(0).GetComponent<Image>();
    highlight.color = color;
    highlight.enabled = true;
  }
}