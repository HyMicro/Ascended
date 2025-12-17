using UnityEngine;
using System;
using System.Collections.Generic; // Dibutuhkan jika ingin pakai List di luar

[Serializable]
public struct BiomeData
{
    [Header("Biome Info")]
    public string biomeName;
    
    [Tooltip("Titik Y di mana player akan respawn saat reset ke Biome ini.")]
    public float biomeStartY;
    
    [Tooltip("Batas Y: Jika player jatuh di bawah Y ini, dia akan direset ke Biome SEBELUMNYA.")]
    public float fallResetY;
}