﻿using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour {

	const int mapFileVersion = 4;

	public Text menuLabel, actionButtonLabel;

	public InputField nameInput;

	public RectTransform listContent;

	public SaveLoadItem itemPrefab;

	public HexGrid hexGrid;

	public bool preloadMap;

	bool saveMode;
	bool firstFrame = true;

	private void Start() {
		Debug.Log(Application.persistentDataPath);
	}

	public void Update() {
		if (!firstFrame) {
			return;
		}

		if (preloadMap) {
			var map = Resources.Load<TextAsset>("bob");
			Load(new MemoryStream(map.bytes));
		}

		firstFrame = false;
		gameObject.active = false;
	}

	public void Open(bool saveMode) {
		this.saveMode = saveMode;
		if (saveMode) {
			menuLabel.text = "Save Map";
			actionButtonLabel.text = "Save";
		} else {
			menuLabel.text = "Load Map";
			actionButtonLabel.text = "Load";
		}
		FillList();
		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close() {
		gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}

	public void Action() {
		string path = GetSelectedPath();
		if (path == null) {
			return;
		}
		if (saveMode) {
			Save(path);
		} else {
			Load(path);
		}
		Close();
	}

	public void SelectItem(string name) {
		nameInput.text = name;
	}

	public void Delete() {
		string path = GetSelectedPath();
		if (path == null) {
			return;
		}
		if (File.Exists(path)) {
			File.Delete(path);
		}
		nameInput.text = "";
		FillList();
	}

	void FillList() {
		for (int i = 0; i < listContent.childCount; i++) {
			Destroy(listContent.GetChild(i).gameObject);
		}
		string[] paths =
			Directory.GetFiles(Application.persistentDataPath, "*.map");
		Array.Sort(paths);
		for (int i = 0; i < paths.Length; i++) {
			SaveLoadItem item = Instantiate(itemPrefab);
			item.menu = this;
			item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
			item.transform.SetParent(listContent, false);
		}
	}

	string GetSelectedPath() {
		string mapName = nameInput.text;
		if (mapName.Length == 0) {
			return null;
		}
		return Path.Combine(Application.persistentDataPath, mapName + ".map");
	}

	void Save(string path) {
		using(
			var writer = new BinaryWriter(File.Open(path, FileMode.Create))
		) {
			writer.Write(mapFileVersion);
			hexGrid.Save(writer);
		}
	}

	void Load(string path) {
		if (!File.Exists(path)) {
			Debug.LogError("File does not exist " + path);
			return;
		}
		Load(File.OpenRead(path));
	}

	void Load(Stream stream) {
		using(var reader = new BinaryReader(stream)) {
			int header = reader.ReadInt32();
			if (header <= mapFileVersion) {
				hexGrid.Load(reader, header);
				HexMapCamera.ValidatePosition();
			} else {
				Debug.LogWarning("Unknown map format " + header);
			}
		}
	}
}