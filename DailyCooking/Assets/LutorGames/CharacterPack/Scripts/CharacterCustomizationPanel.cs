#if UNITY_EDITOR
namespace PrivateLT.CharacterCustomization
{
    /* 
        Note:

        So you're looking at this code thinking, "What in the actual heck is going on here?" 
        Yeah, I feel you—it’s a total dumpster fire. But guess what? I left it like this on purpose. 
        If I can throw this out into the world and you think you can do better, then what’s holding you back? 
        This is your sign to step up, find the courage in yourself, and build something awesome!
        WITHOUT CHILDISH EXCUSES!

        Remember, no idea starts out perfect—just gotta find courage to start it!

        P.S. If you think I'm a terrible coder, just check out my save system and rethink your judgment. 
             This is here to motivate, not to judge. Now go and make something great! <3

        Best Regards,
        Tornike
        Co-Foudner and CEO of Lutor Games
    */

    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Unity.VisualScripting;
    using UnityEditor;
    using UnityEngine;

    public class CharacterCustomizationPanel : EditorWindow
    {
        private static Vector3 defaultPosition = new Vector3(-9000, -9000, 9000);
        private static Vector2 defaultSize = new Vector2(920, 768);
        private static Vector2 previewSize = new Vector2(400, 768);
        private Customization customization;
        private Customization_Data customization_data;
        private Cosmetic currentCosmetic;
        private RenderTexture renderTexture;
        private Texture2D shuffleIcon;
        private Texture2D iconBlock;
        private Texture2D iconNone;
        private Texture2D IconLocked;
        private Texture2D IconUnlocked;
        private Texture2D logo;
        private Camera previewCamera;

        private CosmeticsData currentCosmeticsData;
        private CustomizationPart currentCustomizationPart;

        private float _currecntSelectionLableLength = 0;
        private List<string> _unfittables = new();
        private Vector2 defaultCenter = defaultSize / 2;
        private Rect _logoRect = new(defaultSize.x - 500 + 250, defaultSize.y - 500 + 250, 500, 500);
        private Rect _modelPreviewRect;
        private Rect _saveButtonRect;
        private Rect _shuffleButtonRect;
        private Rect _autoGenerateButtonRect;
        private Rect _inputRect;
        private Rect _labelRect;
        private Rect _copyrightRect;
        private Rect _typeRect;
        private Rect _lockedRect;
        private Rect _selectionPanelRect;
        private Rect _selectionLabelRect;
        private Rect _unfittableRect;
        private Rect _selectionClearRect;
        private Rect _selectionShuffleRect;
        private Rect _selectionLockedRect;
        private Rect _customizationPanelRect;
        private Rect _customizationPanelActiveRect;
        private Rect _switchRect;
        private Rect _previewPanelRect;
        private Rect _mainPreviewRect;
        private Rect _previewLittlePanelRect;
        private List<Texture> typeTextures = new();
        private GUIStyle _saveButtonStyle;
        private GUIStyle _autoGenerateButtonStyle;
        private GUIStyle _boldStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _copyrightStyle;
        private GUIStyle _selectionLableStyle;
        private GUIStyle _selectionClearStyle;
        private GUIStyle _previewLableStyle;

        private List<CustomizationPart> parts = new();
        private List<CosmeticsData> _cosmeticsData = new();

        private Color backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        private Color buttonColor = new Color(0.6f, 0.6f, 0.6f);
        private Color buttonColorDark = new Color(0.4f, 0.4f, 0.4f);
        private Color buttonActive = new Color(1, 0.53f, 0);
        private Color iconActive = new Color(1, 1, 1);
        private Color iconInactive = new Color(0.4f, 0.4f, 0.4f);
        private Color panelColor = new Color(0.2f, 0.2f, 0.2f);
        private string inputPreviousText = "";
        private string inputText = "";
        private int currentListIndex = 0;

        private const string characterModelPath = "Assets/LutorGames/CharacterPack/Prefab/PF_CharacterCustomization.prefab";
        private const string iconShufflePath = "Assets/LutorGames/CharacterPack/Art/Edirot Icons/Shuffle.png";
        private const string iconBlockPath = "Assets/LutorGames/CharacterPack/Art/Edirot Icons/Block.png";
        private const string iconLogoPath = "Assets/LutorGames/Logo/Lutor Games Logo.png";
        private const string iconsPath = "Assets/LutorGames/CharacterPack/Art/Edirot Icons/";
        private const string iconCustomizationPath = "Assets/LutorGames/CharacterPack/Art/Edirot Icons/Customization.png";
        private const string iconNonePath = "Assets/LutorGames/CharacterPack/Art/Edirot Icons/None.png";
        private const string iconLockedPath = "Assets/LutorGames/CharacterPack/Art/Edirot Icons/Locked.png";
        private const string iconUnlockedPath = "Assets/LutorGames/CharacterPack/Art/Edirot Icons/Unlocked.png";

        private bool _initialised = false;
        private bool _autoGenerating = false;

        // Set of forbidden characters
        private static readonly HashSet<char> forbiddenChars = new HashSet<char>
        {
            '/', '"', '*', ':', '\\', '|', '?', '.', '<', '>'
        };

        [MenuItem("Tools/Lutor Games/Unique Character Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterCustomizationPanel>("Character Asset Pack");

            var fullSize = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

            window.minSize = defaultSize;
            window.maxSize = defaultSize;

            window.position = new Rect((fullSize.x - defaultSize.x) / 2, (fullSize.y - defaultSize.y) / 2, defaultSize.x, defaultSize.y);

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconCustomizationPath);

            if (icon != null)
            {
                window.titleContent = new GUIContent("Character Asset Pack", icon);
            }

        }
        private void OnGUI()
        {
            if (EditorApplication.isCompiling)
            {
                ClosePanel();
                Close();
                return;
            }

            if (!_initialised) OnGuiInitialise();

            customization.CustomUpdate();

            #region Draw Background

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

            GUI.DrawTexture(_logoRect, logo);

            #endregion

            if (_autoGenerating)
            {
                GUI.enabled = false;
            }

            #region Draw CameraTexture

            EditorGUI.DrawPreviewTexture(_modelPreviewRect, renderTexture);

            #endregion

            #region Draw SavePrefab/Shuffle/AutoGenerate Buttons and InputName Field

            if (GUI.Button(_saveButtonRect, "Save As Prefab", _saveButtonStyle) && !_autoGenerating) SavePrefab();

            if (GUI.Button(_shuffleButtonRect, shuffleIcon) && !_autoGenerating) GeneralShuffle();
            if (GUI.Button(_autoGenerateButtonRect, "Auto Generate", _autoGenerateButtonStyle) && !_autoGenerating) AutoGenerate();

            #endregion

            #region Draw InputName Field

            GUI.Label(_labelRect, "Asset/", _boldStyle);

            inputText = GUI.TextField(_inputRect, inputText, _inputStyle);

            if (inputText != inputPreviousText)
            {
                inputText = RemoveForbiddenCharacters(inputText);
            }

            inputPreviousText = inputText;

            #endregion

            DrawTypeSelection();
            DrawCustomizationSelection();
            DrawPreviewIcons();

            GUI.Label(_copyrightRect, "© 2024 | Asset Pack By Lutor Games LLC, All rights reserved.", _copyrightStyle);

            GUI.enabled = true;

            if (_autoGenerating)
            {
                DrawAutogeneratePanel();
            }

            if (previewCamera == null)
            {
                ClosePanel();
                Close();
            }

            Repaint();
        }

        private void OnDisable()
        {
            ClosePanel();
        }

        private void Initialise()
        {
            renderTexture = new RenderTexture((int)previewSize.x, (int)previewSize.y, 16, RenderTextureFormat.ARGB32);
            shuffleIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconShufflePath);
            iconBlock = AssetDatabase.LoadAssetAtPath<Texture2D>(iconBlockPath);
            logo = AssetDatabase.LoadAssetAtPath<Texture2D>(iconLogoPath);
            IconLocked = AssetDatabase.LoadAssetAtPath<Texture2D>(iconLockedPath);
            IconUnlocked = AssetDatabase.LoadAssetAtPath<Texture2D>(iconUnlockedPath);
            iconNone = AssetDatabase.LoadAssetAtPath<Texture2D>(iconNonePath);

            if (customization == null)
            {
                customization = LoadCustomizationPrefab();
                customization_data = customization.CustomizationData;
                _cosmeticsData = customization_data.CosmeticDatas;

                customization.transform.position = defaultPosition;

                customization.Initialise(this);

            }

            if (previewCamera == null)
            {
                previewCamera = customization.Camera;
                previewCamera.targetTexture = renderTexture;
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = backgroundColor;
            }


            _modelPreviewRect = new Rect(0, 0, previewSize.x, previewSize.y);

            _saveButtonRect = new Rect(defaultCenter.x - 125, defaultSize.y - 70, 250, 40);
            _shuffleButtonRect = new Rect(_saveButtonRect.xMax - 40, _saveButtonRect.y - 35, 40, 30);
            _autoGenerateButtonRect = new Rect(_saveButtonRect.x, _shuffleButtonRect.y, _saveButtonRect.width - _shuffleButtonRect.width - 5, 30);

            _inputRect = new Rect(_saveButtonRect.x + 70, _shuffleButtonRect.y - 35f, _saveButtonRect.width - 70, 25);
            _labelRect = new Rect(_saveButtonRect.x, _inputRect.y, 100, 25);

            _copyrightRect = new Rect(5, defaultSize.y - 25, 600, 25);

            _typeRect = new Rect(defaultSize.x - 465, 40, 55, 55);
            _lockedRect = new Rect(0, 0, 15, 15);

            _selectionPanelRect = new Rect(defaultSize.x - 500, 160, 450, 450);
            _selectionLabelRect = new Rect(_selectionPanelRect.x + 10, _selectionPanelRect.y + 10, 300, 50);
            _selectionClearRect = new Rect(_selectionPanelRect.xMax - 90, _selectionPanelRect.y + 10, 80, 35);

            _unfittableRect = new Rect(_selectionLabelRect.x + _currecntSelectionLableLength * 20, _selectionLabelRect.y + 10, 24, 24);
            _selectionShuffleRect = new Rect(_selectionClearRect.x - _selectionClearRect.height - 5, _selectionClearRect.y, _selectionClearRect.height, _selectionClearRect.height);
            _selectionLockedRect = new Rect(_selectionShuffleRect.x - _selectionClearRect.height - 5, _selectionClearRect.y, _selectionClearRect.height, _selectionClearRect.height);

            _customizationPanelRect = new Rect(0, 0, 92.5f, 92.5f);
            _customizationPanelActiveRect = new Rect(0, 0, _customizationPanelRect.width + 10, _customizationPanelRect.height + 10);
            _switchRect = new Rect(_selectionPanelRect.center.x + 40, _selectionPanelRect.yMax - 55, 80, 40);

            _previewPanelRect = new Rect(50, 20, 290, 200);
            _mainPreviewRect = new Rect(_previewPanelRect.x + 10, _previewPanelRect.y + _previewPanelRect.height - 160, 150, 150);

            _previewLittlePanelRect = new Rect(0, 0, 30, 30);

            for (int i = 0; i < _cosmeticsData.Count; i++)
            {
                typeTextures.Add(GetTypeIcon(_cosmeticsData[i].Type));
            }
        }

        private void OnGuiInitialise()
        {
            Initialise();
            InitialiseStyles();
            SetCosmeticType(customization_data.CosmeticDatas[0]);

            foreach (var data in customization_data.CosmeticDatas)
            {
                data.Locked = false;
            }

            _initialised = true;
        }

        private void InitialiseStyles()
        {
            _saveButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _autoGenerateButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _boldStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 20
            };

            _inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft
            };

            _copyrightStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Normal,
                fontSize = 12
            };

            _selectionLableStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1, 1, 1) },
                hover = { textColor = new Color(1, 1, 1) },
                active = { textColor = new Color(1, 1, 1) },
                alignment = TextAnchor.UpperLeft,
            };

            _selectionClearStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 25,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                alignment = TextAnchor.MiddleCenter,
            };

            _previewLableStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1, 1, 1) },
                hover = { textColor = new Color(1, 1, 1) },
                active = { textColor = new Color(1, 1, 1) },
                alignment = TextAnchor.UpperCenter,
            };
        }

        private Customization LoadCustomizationPrefab()
        {
            GameObject loadedModel = AssetDatabase.LoadAssetAtPath<GameObject>(characterModelPath);

            if (loadedModel != null)
            {
                return Instantiate(loadedModel).GetComponent<Customization>();
            }

            Debug.LogWarning($"Customization can't be found: {characterModelPath}");

            return null;
        }

        private void SavePrefab()
        {
            string path = $"Assets/{inputText}.prefab";

            var character = customization.GetCharacter();

            PrefabUtility.SaveAsPrefabAsset(character, path);

            DestroyImmediate(character);

            Debug.Log($"Prefab saved at {path}");
        }

        private string RemoveForbiddenCharacters(string text)
        {
            var result = new System.Text.StringBuilder();

            foreach (var c in text)
            {
                if (!forbiddenChars.Contains(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private void ClosePanel()
        {
            if (customization != null)
            {
                customization.Close();
            }
            if (previewCamera != null)
            {
                DestroyImmediate(previewCamera.gameObject);
            }
            if (customization != null)
            {
                DestroyImmediate(customization.gameObject);
            }
            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }
        }

        private void DrawTypeSelection()
        {
            GUI.backgroundColor = buttonColor;

            for (int i = 0; i < _cosmeticsData.Count; i++)
            {
                var data = _cosmeticsData[i];

                var line = Mathf.Clamp01(Mathf.Floor(i / 6f));

                var rawAmount = line == 0 ? 6 : 7;

                _typeRect.x = defaultSize.x - 465 + ((i % rawAmount) * 65) - (line * 55 / 2);
                _typeRect.y = 20 + (line * 65);

                _lockedRect.x = _typeRect.x + (data.Locked ? 2 : 5);
                _lockedRect.y = _typeRect.y + _typeRect.height - _lockedRect.height - 5;

                if (GUI.Button(_lockedRect, IconLocked) && !_autoGenerating)
                {
                    data.Locked = !data.Locked;
                }

                GUI.contentColor = currentCosmeticsData.Type == data.Type ? iconActive : iconInactive;

                if (GUI.Button(_typeRect, typeTextures[i]) && !_autoGenerating)
                {
                    SetCosmeticType(data);
                    RestartListSection();
                }

                var iconLock = data.Locked ? IconLocked : IconUnlocked;

                GUI.color = data.Locked ? iconActive : iconInactive;

                GUI.DrawTexture(_lockedRect, iconLock);

                GUI.color = Color.white;
                GUI.contentColor = Color.white;
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawCustomizationSelection()
        {
            #region Draw Panel

            EditorGUI.DrawRect(_selectionPanelRect, panelColor);

            #endregion

            #region Draw Lable

            GUI.Label(_selectionLabelRect, currentCosmeticsData.Type, _selectionLableStyle);

            #endregion

            #region Draw Unfittables

            _unfittableRect.x = _selectionLabelRect.x + _currecntSelectionLableLength + 10;

            if (!string.IsNullOrWhiteSpace(_unfittables[0]))
            {
                for (int i = 0; i < _unfittables.Count; i++)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                    GUI.DrawTexture(_unfittableRect, GetTypeIcon(_unfittables[i]));
                    GUI.color = Color.white;
                    GUI.DrawTexture(_unfittableRect, iconBlock);

                    _unfittableRect.x += _unfittableRect.width + 5f;
                }
            }

            #endregion

            #region Draw ClearButton

            GUI.backgroundColor = buttonColorDark;

            if (GUI.Button(_selectionClearRect, "") && !_autoGenerating)
            {
                currentCosmeticsData.OnClear?.Invoke();
            }

            GUI.backgroundColor = Color.white;

            _selectionClearStyle.fontStyle = FontStyle.Normal;

            GUI.Label(_selectionClearRect, "Clear", _selectionClearStyle);

            #endregion

            #region Draw LockedToggle

            GUI.backgroundColor = buttonColorDark;
            GUI.contentColor = currentCosmeticsData.Locked ? iconActive : iconInactive;

            if (GUI.Button(_selectionLockedRect, currentCosmeticsData.Locked ? IconLocked : IconUnlocked) && !_autoGenerating)
            {
                currentCosmeticsData.Locked = !currentCosmeticsData.Locked;
            }

            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;

            #endregion

            #region Draw ShuffleButton

            GUI.backgroundColor = buttonColorDark;

            if (GUI.Button(_selectionShuffleRect, shuffleIcon) && !_autoGenerating)
            {
                currentCosmeticsData.OnShuffle?.Invoke();
            }

            GUI.backgroundColor = Color.white;

            #endregion

            #region Draw CosmeticList

            _selectionClearStyle.fontStyle = FontStyle.Bold;

            var listSectionCount = _cosmeticsData.Count / 12;

            var startIndex = currentListIndex * 12;

            for (int i = startIndex; i < startIndex + 12; i++)
            {
                if (currentCosmeticsData.Cosmetics.Count <= i) continue;

                var cosmetic = currentCosmeticsData.Cosmetics[i];

                _customizationPanelRect.x = (_selectionPanelRect.x + 10) + (((i - startIndex) % 4) * (_customizationPanelRect.width + 20));
                _customizationPanelRect.y = (_selectionPanelRect.y + 60) + (Mathf.Floor((i - startIndex) / 4f) * (_customizationPanelRect.height + 20));

                var isActive = currentCustomizationPart.Index == i;

                _customizationPanelActiveRect.x = _customizationPanelRect.x - 5;
                _customizationPanelActiveRect.y = _customizationPanelRect.y - 5;

                if (isActive)
                {
                    EditorGUI.DrawRect(_customizationPanelActiveRect, buttonActive);
                }

                GUI.backgroundColor = buttonColorDark;
                if (GUI.Button(_customizationPanelRect, cosmetic.Icon.texture) && !_autoGenerating)
                {
                    ChooseCustom(i);
                }
                GUI.backgroundColor = Color.white;
            }

            #endregion

            #region Draw SwithButtons

            _selectionClearStyle.fontStyle = FontStyle.Bold;

            _switchRect.x = _selectionPanelRect.center.x + 40;
            //Right
            GUI.backgroundColor = buttonColorDark;
            if (GUI.Button(_switchRect, "") && !_autoGenerating)
            {
                currentListIndex = (currentListIndex + 1) % (listSectionCount + 1);
            }
            GUI.backgroundColor = Color.white;

            GUI.Label(_switchRect, ">", _selectionClearStyle);


            //Left
            _switchRect.x -= 160;

            GUI.backgroundColor = buttonColorDark;
            if (GUI.Button(_switchRect, "") && !_autoGenerating)
            {
                currentListIndex = Mathf.Clamp((currentListIndex - 1) % (listSectionCount + 1), 0, listSectionCount + 1);
            }
            GUI.backgroundColor = Color.white;

            GUI.Label(_switchRect, "<", _selectionClearStyle);

            //Counter
            _selectionClearStyle.fontStyle = FontStyle.Normal;
            _selectionClearStyle.fontSize = 20;

            _switchRect.x = _selectionPanelRect.center.x - 35;

            GUI.Label(_switchRect, $"{currentListIndex}/{listSectionCount}", _selectionClearStyle);
            #endregion
        }

        private void DrawPreviewIcons()
        {
            #region Draw Panel

            EditorGUI.DrawRect(_previewPanelRect, panelColor);

            #endregion

            #region Draw Lable

            GUI.Label(_previewPanelRect, currentCosmeticsData.Type, _previewLableStyle);

            #endregion

            #region Draw MainPreview

            var mainIcon = currentCosmetic != null ? currentCosmetic.Icon.texture : iconNone;

            GUI.DrawTexture(_mainPreviewRect, mainIcon);

            #endregion

            #region Draw SmallPreviews

            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];

                var icon = part.CurrentCosmetic != null ? part.CurrentCosmetic.Icon.texture : iconNone;

                var additionalX = Mathf.FloorToInt(i / 4f) * 40;
                var additionalY = (i % 4) * 40;

                _previewLittlePanelRect.x = _previewPanelRect.x + (_mainPreviewRect.width + 20) + additionalX;
                _previewLittlePanelRect.y = _previewPanelRect.y + _previewPanelRect.height - (_mainPreviewRect.width + 10) + additionalY;

                GUI.DrawTexture(_previewLittlePanelRect, icon);
            }

            #endregion
        }

        private void ChooseCustom(int index)
        {
            currentCosmeticsData.SetMesh(index);

            UpdateCurrentCosmetic();
        }

        private void SetCosmeticType(CosmeticsData data)
        {
            currentCosmeticsData = data;

            _currecntSelectionLableLength = _selectionLableStyle.CalcSize(new GUIContent(currentCosmeticsData.Type)).x;

            _unfittables = currentCosmeticsData.UnfittableTypes
               .Split(',')
               .Select(s => s.Trim())
               .ToList();

            currentCustomizationPart = customization.CustomizationParts.FirstOrDefault(x => x.Type == currentCosmeticsData.Type);

            currentCosmetic = currentCustomizationPart.CurrentCosmetic;

            parts.Clear();

            foreach (var part in customization.CustomizationParts)
            {
                parts.Add(part);
            }

            parts.Remove(currentCustomizationPart);
        }

        private void GeneralShuffle()
        {
            customization.Shuffle();
            UpdateCurrentCosmetic();
        }

        private void AutoGenerate()
        {
            _autoGenerating = true;
        }

        private string amountInput = "1";
        private void DrawAutogeneratePanel()
        {
            var panelWidth = 350;
            var panelHeight = 200;
            var autoGeneratePanel = new Rect(defaultCenter.x - panelWidth / 2, defaultCenter.y - panelHeight / 2, panelWidth, panelHeight);

            EditorGUI.DrawRect(new Rect(0, 0, defaultSize.x, defaultSize.y), new Color(0, 0, 0, 0.5f));

            EditorGUI.DrawRect(autoGeneratePanel, panelColor);

            GUI.backgroundColor = buttonColorDark;
            if (GUI.Button(new Rect(autoGeneratePanel.xMax - 40, autoGeneratePanel.y + 10, 30, 30), "X"))
            {
                _autoGenerating = false;
            }
            GUI.backgroundColor = Color.white;

            var titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 25 };
            GUI.Label(new Rect(autoGeneratePanel.x, autoGeneratePanel.y + 10, panelWidth, 30), "Autogenerate Panel", titleStyle);

            var inputWidth = 50;
            var inputHeight = 25;
            var labelWidth = 70;
            var inputX = autoGeneratePanel.x + 50;
            var inputY = autoGeneratePanel.center.y + 5;

            var generateIAmountLableStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 15 };
            var generateAmountLableRect = new Rect(inputX, inputY, labelWidth, inputHeight);
            GUI.Label(generateAmountLableRect, "Amount:", generateIAmountLableStyle);

            var amountInputRect = new Rect(inputX + labelWidth, inputY, inputWidth, inputHeight);
            string newAmountInput = GUI.TextField(amountInputRect, amountInput, _inputStyle);
            amountInput = Regex.Replace(newAmountInput, "[^0-9]", "");

            generateAmountLableRect.width = 100;
            generateAmountLableRect.y = autoGeneratePanel.center.y - 5 - generateAmountLableRect.height;
            GUI.Label(generateAmountLableRect, "Name Prefix:", generateIAmountLableStyle);

            amountInputRect.y = generateAmountLableRect.y;
            amountInputRect.width = 150;
            amountInputRect.x = inputX + generateAmountLableRect.width;
            inputText = GUI.TextField(amountInputRect, inputText, _inputStyle);

            var buttonWidth = 120;
            var buttonHeight = 30;
            var buttonX = autoGeneratePanel.x + (panelWidth - buttonWidth) / 2;
            var buttonY = autoGeneratePanel.yMax - 50;

            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "Generate", _autoGenerateButtonStyle))
            {
                var amount = int.Parse(amountInput);
                if (amount <= 0) return;

                string name = inputText;
                for (int i = 0; i < amount; i++)
                {
                    inputText = $"{name}_{i}";
                    GeneralShuffle();
                    SavePrefab();
                }
                inputText = name;

                _autoGenerating = false;
            }
        }

        private void UpdateCurrentCosmetic()
        {
            currentCosmetic = currentCustomizationPart.CurrentCosmetic;
        }

        private Texture GetTypeIcon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconsPath + $"{name}.png");
        }

        private void RestartListSection()
        {
            currentListIndex = 0;
        }
    }
}

#endif
