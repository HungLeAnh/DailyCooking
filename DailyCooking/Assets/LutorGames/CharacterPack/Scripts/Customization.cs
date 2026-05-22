#if UNITY_EDITOR
namespace PrivateLT.CharacterCustomization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public class CustomizationPart
    {
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public CosmeticsData CosmeticsData { get; set; }
        public Customization_Data CustomizationData { get; set; }
        public string Type => CosmeticsData.Type;
        public int Index => _index;
        public Cosmetic CurrentCosmetic => _index >= 0 ? CosmeticsData.Cosmetics[_index] : null;
        public List<Cosmetic> Cosmetics => CosmeticsData.Cosmetics;

        private int _index;

        public void Initialise(Customization_Data customizationData)
        {
            CustomizationData = customizationData;
            CosmeticsData = CustomizationData.CosmeticDatas.First(x => x.Type == SkinnedMeshRenderer.name.Split('_')[0]);
            CosmeticsData.OnShuffle += Shuffle;
            CosmeticsData.OnRefresh += Refresh;
            CosmeticsData.OnClear += Clear;
            CosmeticsData.OnSetMeshWithIndex += SetMesh;
        }

        public void Close()
        {
            CosmeticsData.OnShuffle -= Shuffle;
            CosmeticsData.OnRefresh -= Refresh;
            CosmeticsData.OnClear -= Clear;
            CosmeticsData.OnSetMeshWithIndex -= SetMesh;
        }

        public void Refresh()
        {
            SetMesh(0);
        }

        public void Clear()
        {
            SetMesh(null);
        }

        public void Shuffle()
        {
            var randomIndex = UnityEngine.Random.Range(0, Cosmetics.Count);

            if (_index == randomIndex)
            {
                Shuffle();
                return;
            }

            SetMesh(randomIndex);
        }

        public void SetMesh(int index)
        {
            SetMesh(Cosmetics[index]);
            CosmeticsData.CurrentMeshIndex = index;
        }

        public void SetMesh(Cosmetic cosmetic)
        {
            if (cosmetic == null)
            {
                _index = -1;
                CosmeticsData.CurrentMeshIndex = -1;
                SkinnedMeshRenderer.sharedMesh = null;
                return;
            }

            SkinnedMeshRenderer.sharedMesh = cosmetic.Mesh;

            _index = Cosmetics.IndexOf(cosmetic);

            ClearFromUnfittables();
        }

        private void ClearFromUnfittables()
        {
            CustomizationData.ClearFromUnfittables(CosmeticsData.UnfittableTypes);
        }

    }

    public class Customization : MonoBehaviour
    {
        public Transform Rotatable;
        public Camera Camera;
        public CharacterCustomizationPanel CustomizationPanel { set; get; }

        public Customization_Data CustomizationData;
        [SerializeField] private GameObject _character;
        [SerializeField] private List<CustomizationPart> _customizationParts;

        public List<CustomizationPart> CustomizationParts => _customizationParts;

        private float rotationSpeed = 25f;

        public void Initialise(CharacterCustomizationPanel customizationPanel)
        {
            foreach (var part in _customizationParts)
            {
                part.Initialise(CustomizationData);
            }

            CustomizationPanel = customizationPanel;

            Shuffle();
        }

        public void Close()
        {
            foreach (var part in _customizationParts)
            {
                part.Close();
            }
        }

        static Customization()
        {

            EditorApplication.update += EditorUpdate;
        }

        private static void EditorUpdate()
        {
            Customization[] scripts = FindObjectsOfType<Customization>();

            foreach (var script in scripts)
            {
                if (script.CustomizationPanel == null)
                {
                    DestroyImmediate(script.gameObject);
                }
            }
        }

        public void CustomUpdate()
        {
            Rotate();
        }

        public void Rotate()
        {
            Rotatable.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
        }

        public GameObject GetCharacter()
        {
            return Instantiate(_character, Vector3.zero, Quaternion.identity);
        }

        public void Shuffle(List<string> alreadyUsedParts = null)
        {
            CustomizationPart chosenPart;
            List<CustomizationPart> parts = new();

            alreadyUsedParts ??= new();

            foreach (var customizationPart in _customizationParts)
            {
                if (customizationPart.CosmeticsData.Locked)
                {
                    alreadyUsedParts.Add(customizationPart.CosmeticsData.Type);

                    if (customizationPart.CosmeticsData.CurrentMeshIndex >= 0)
                    {
                        var unfittables = customizationPart.CosmeticsData.UnfittableTypes
                            .Split(',')
                            .Select(s => s.Trim())
                            .ToList();

                        unfittables.ForEach(x => alreadyUsedParts.Add(x));
                    }
                }

                parts.Add(customizationPart);
            }

            if (alreadyUsedParts != null)
            {
                parts.RemoveAll(c => alreadyUsedParts.Contains(c.Type));
            }

            if (parts.Count <= 0) return;

            chosenPart = parts[UnityEngine.Random.Range(0, parts.Count)];

            chosenPart.Shuffle();

            alreadyUsedParts.Add(chosenPart.Type);


            var unfittableTypeList = chosenPart.CosmeticsData.UnfittableTypes
           .Split(',')
           .Select(s => s.Trim())
           .ToList();

            foreach (var unfittable in unfittableTypeList)
            {
                if (alreadyUsedParts.Contains(unfittable)) continue;

                alreadyUsedParts.Add(unfittable);
            }

            if (alreadyUsedParts.Count >= _customizationParts.Count) return;

            Shuffle(alreadyUsedParts);
        }
    }


}

#endif

