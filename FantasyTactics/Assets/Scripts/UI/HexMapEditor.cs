﻿using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

  public HexGrid hexGrid;

  public Material terrainMaterial;

  int activeElevation;
  int activeWaterLevel;

  int activePlantLevel;

  int activeTerrainTypeIndex;

  int brushSize;

  bool applyElevation = true;
  bool applyWaterLevel = true;

  bool applyPlantLevel;

  enum OptionalToggle {
    Ignore,
    Yes,
    No
  }

  HexDirection dragDirection;
  HexCell previousCell;

  public void SetTerrainTypeIndex(int index) {
    activeTerrainTypeIndex = index;
  }

  public void SetApplyElevation(bool toggle) {
    applyElevation = toggle;
  }

  public void SetElevation(float elevation) {
    activeElevation = (int) elevation;
  }

  public void SetApplyWaterLevel(bool toggle) {
    applyWaterLevel = toggle;
  }

  public void SetWaterLevel(float level) {
    activeWaterLevel = (int) level;
  }

  public void SetApplyPlantLevel(bool toggle) {
    applyPlantLevel = toggle;
  }

  public void SetPlantLevel(float level) {
    activePlantLevel = (int) level;
  }

  public void SetBrushSize(float size) {
    brushSize = (int) size;
  }

  public void SetEditMode(bool toggle) {
    enabled = toggle;
  }

  public void ShowGrid(bool visible) {
    if (visible) {
      terrainMaterial.EnableKeyword("GRID_ON");
    } else {
      terrainMaterial.DisableKeyword("GRID_ON");
    }
  }

  void Awake() {
    terrainMaterial.DisableKeyword("GRID_ON");
    Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
    SetEditMode(true);
  }

  void Update() {
    if (!EventSystem.current.IsPointerOverGameObject()) {
      if (Input.GetMouseButton(0)) {
        HandleInput();
        return;
      }
      if (Input.GetKeyDown(KeyCode.U)) {
        if (Input.GetKey(KeyCode.LeftShift)) {
          DestroyUnit();
        } else {
          CreateUnit();
        }
        return;
      }
    }
    previousCell = null;
  }

  HexCell GetCellUnderCursor() {
    return
    hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
  }

  void CreateUnit() {
    HexCell cell = GetCellUnderCursor();
    if (cell && !cell.Unit) {
      hexGrid.AddUnit(
        Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f)
      );
    }
  }

  void DestroyUnit() {
    HexCell cell = GetCellUnderCursor();
    if (cell && cell.Unit) {
      hexGrid.RemoveUnit(cell.Unit);
    }
  }

  void HandleInput() {
    HexCell currentCell = GetCellUnderCursor();
    if (currentCell) {
      if (previousCell && previousCell != currentCell) {
        ValidateDrag(currentCell);
      }
      EditCells(currentCell);
      previousCell = currentCell;
    } else {
      previousCell = null;
    }
  }

  void ValidateDrag(HexCell currentCell) {
    for (
      dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++
    ) {
      if (previousCell.GetNeighbor(dragDirection) == currentCell) {
        return;
      }
    }
  }

  void EditCells(HexCell center) {
    int centerX = center.coordinates.X;
    int centerZ = center.coordinates.Z;

    for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
      for (int x = centerX - r; x <= centerX + brushSize; x++) {
        EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
      }
    }
    for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
      for (int x = centerX - brushSize; x <= centerX + r; x++) {
        EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
      }
    }
  }

  void EditCell(HexCell cell) {
    if (cell) {
      if (activeTerrainTypeIndex >= 0) {
        cell.TerrainTypeIndex = activeTerrainTypeIndex;
      }
      if (applyElevation) {
        cell.Elevation = activeElevation;
      }
      if (applyWaterLevel) {
        cell.WaterLevel = activeWaterLevel;
      }
      if (applyPlantLevel) {
        cell.PlantLevel = activePlantLevel;
      }
    }
  }
}