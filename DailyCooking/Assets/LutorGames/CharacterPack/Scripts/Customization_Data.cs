#if UNITY_EDITOR
namespace PrivateLT.CharacterCustomization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [Serializable]
    public class Cosmetic
    {
        public string Name;
        public Sprite Icon;
        public Mesh Mesh;
    }

    [Serializable]
    public class CosmeticsData
    {
        public string Type;
        public string UnfittableTypes;
        public List<Cosmetic> Cosmetics;

        public bool Locked { set; get; }
        public int CurrentMeshIndex { set; get; }
        public Cosmetic CurrentMesh => CurrentMeshIndex >= 0 ? Cosmetics[CurrentMeshIndex] : null;

        public Action OnRefresh;
        public Action OnClear;
        public Action OnShuffle;
        public Action<int> OnSetMeshWithIndex;

        public void Clear()
        {
            OnClear?.Invoke();
            CurrentMeshIndex = -1;
        }

        public void SetMesh(int index)
        {
            OnSetMeshWithIndex?.Invoke(index);
            CurrentMeshIndex = index;
        }

        public CosmeticsData(string type, List<Cosmetic> cosmetics)
        {
            Type = type;
            Cosmetics = cosmetics;
        }
    }

    public class Customization_Data : ScriptableObject
    {
        public List<CosmeticsData> CosmeticDatas;

        public void AddCosmetic(string type, Cosmetic cosmetic)
        {
            var cosmeticData = CosmeticDatas.FirstOrDefault(x => x.Type == type);

            if (cosmeticData == null)
            {
                cosmeticData = new CosmeticsData(type, new());
                CosmeticDatas.Add(cosmeticData);
            }

            var cosmeticInfo = cosmeticData.Cosmetics.FirstOrDefault(x => x.Name == cosmetic.Name);

            int index = 0;

            if (cosmeticInfo != null)
            {
                index = cosmeticData.Cosmetics.IndexOf(cosmeticInfo);
                cosmeticData.Cosmetics.Remove(cosmeticInfo);
            }

            cosmeticData.Cosmetics.Insert(index, cosmetic);
        }

        public void ClearFromUnfittables(string unfittableNames)
        {
            var unfittableNameList = unfittableNames
                               .Split(',')
                               .Select(s => s.Trim())
                               .ToList();

            var unfittables = CosmeticDatas.Where(x => unfittableNameList.Any(y => y == x.Type)).ToList();

            foreach (var unfittable in unfittables)
            {
                unfittable.Clear();
            }
        }

        public bool Contains(Cosmetic newCosmetic, out bool isSame)
        {
            isSame = false;

            foreach (var cosmeticData in CosmeticDatas)
            {
                foreach (var cosmetic in cosmeticData.Cosmetics)
                {
                    if (cosmetic.Name != newCosmetic.Name) continue;

                    if (Equals(cosmetic, newCosmetic)) isSame = true;

                    return true;
                }
            }

            return false;
        }

        private bool Equals(Cosmetic cosmetic1, Cosmetic cosmetic2)
        {
            return (cosmetic1.Name == cosmetic2.Name) && (cosmetic1.Mesh == cosmetic2.Mesh) && (cosmetic1.Icon == cosmetic2.Icon);
        }

    }
}

#endif