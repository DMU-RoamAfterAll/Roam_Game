using UnityEngine;
using System;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour {
    public List<(string, int)> skillList = new List<(string, int)>();
    public List<(string, int)> itemList = new List<(string, int)>();
    public List<(string, int)> weaponList = new List<(string, int)>();
}