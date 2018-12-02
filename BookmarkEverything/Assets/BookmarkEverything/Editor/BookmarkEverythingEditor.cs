using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;

namespace ProjectUtility
{
    public class BookmarkEverythingEditor : EditorWindow
    {
        private Settings _currentSettings = new Settings();

        private bool _initialized;
        private const char CHAR_SEPERATOR = ':';
        List<EntryData> _tempLocations = new List<EntryData>();
        private const string SETTINGS_FILENAME = "projectutilitysettings";
        private const string CATEGORY_SCENE = "Scenes";
        private const string CATEGORY_PREFAB = "Prefabs";
        private const string CATEGORY_SCRIPT = "Scripts";
        private const string CATEGORY_SO = "Scriptable Objects";
        private const string CATEGORY_STARRED = "Starred";
        private string[] _projectFinderHeaders = new string[] { CATEGORY_STARRED, CATEGORY_SCENE, CATEGORY_PREFAB, CATEGORY_SCRIPT, CATEGORY_SO };
        [System.Serializable]
        public class EntryData
        {
            public string Path;
            public string Category;
            public int Index;

            public EntryData(string path, string category, int index)
            {
                Path = path;
                Category = category;
                Index = index;
            }
            public EntryData(string path)
            {
                Path = path;
                Category = "default";
            }
            public EntryData(UnityEngine.Object obj)
            {
                Path = AssetDatabase.GetAssetPath(obj);
                if (obj.GetType() == typeof(DefaultAsset))
                {
                    Category = "Folder";
                }
                else
                {
                    string[] s = obj.name.Split(CHAR_SEPERATOR);
                    Category = s[s.Length - 1];
                }
            }
            public static EntryData Clone(EntryData data)
            {
                return new EntryData(data.Path, data.Category, data.Index);
            }
            public static EntryData[] Clone(EntryData[] data)
            {
                EntryData[] newData = new EntryData[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    newData[i] = Clone(data[i]);
                }
                return newData;
            }

            public static implicit operator EntryData(string path)
            {
                if (path == null)
                {
                    return null;
                }
                return new EntryData(path);
            }
            public static implicit operator EntryData(UnityEngine.Object obj)
            {
                return new EntryData(obj);
            }

        }

        [System.Serializable]
        public class Settings
        {
            public List<EntryData> EntryData = new List<EntryData>();
            public PingTypes PingType;
            public bool VisualMode;
            public bool AutoClose;

            public Settings(List<EntryData> entryData, PingTypes pingType, bool visualMode, bool autoClose)
            {
                EntryData = entryData;
                PingType = pingType;
                VisualMode = visualMode;
                AutoClose = autoClose;
            }
            public Settings() { }

            public void Save()
            {
                ClearData(SETTINGS_FILENAME);
                WriteToDisk(SETTINGS_FILENAME, this);
            }
        }

        #region GUI REFERENCES
        GUIStyle _buttonStyle;
        GUIStyle _textFieldStyle;
        GUIStyle _scrollViewStyle;
        GUIStyle _boxStyle;
        GUIStyle _popupStyle;
        GUIStyle _toolbarButtonStyle;

        Texture _editorWindowBackground;
        #endregion

        private List<GUIContent> _headerContents = new List<GUIContent>();
        private List<GUIContent> _projectFinderContents = new List<GUIContent>();

        private PingTypes _pingType;
        private bool _visualMode;
        private bool _autoClose;

        [MenuItem("CriticalShot/Project Utility %h")]
        private static void Init()
        {

            var windows = (BookmarkEverythingEditor[])Resources.FindObjectsOfTypeAll(typeof(BookmarkEverythingEditor));
            if (windows.Length == 0)
            {
                BookmarkEverythingEditor window = (BookmarkEverythingEditor)GetWindow(typeof(BookmarkEverythingEditor));
                window.InitInternal();
            }
            else
            {
                FocusWindowIfItsOpen(typeof(BookmarkEverythingEditor));
            }
        }

        public void InitInternal()
        {
            //loads entries from playerprefs
            //Construct main headers(Project Finder, Settings etc.)
            LoadSettings();
            ConstructStyles();
            ConstructMainHeaders();

            ConstructProjectFinderHeaders();
            _initialized = true;
            //constructs all gui element styles

        }
        /// <summary>
        /// Construct tab view of Project Finder
        /// </summary>
        private void ConstructProjectFinderHeaders()
        {
            _projectFinderContents.Add(RetrieveGUIContent(CATEGORY_STARRED, "Favorite"));
            _projectFinderContents.Add(RetrieveGUIContent(CATEGORY_SCENE, ResolveIconNameFromFileExtension("unity")));
            _projectFinderContents.Add(RetrieveGUIContent(CATEGORY_PREFAB, ResolveIconNameFromFileExtension("prefab")));
            _projectFinderContents.Add(RetrieveGUIContent(CATEGORY_SCRIPT, ResolveIconNameFromFileExtension("cs")));
            _projectFinderContents.Add(RetrieveGUIContent(CATEGORY_SO, ResolveIconNameFromFileExtension("asset")));
            if (_projectFinderContents.Count != _projectFinderHeaders.Length)
            {
                Debug.LogError("Inconsistency between Content count and Header count, please add to both of them!");
            }
        }
        private int GetIndexOfCategory(string category)
        {
            for (int i = 0; i < _projectFinderHeaders.Length; i++)
            {
                if (_projectFinderHeaders[i] == category)
                {
                    return i;
                }
            }
            return -1;
        }
        private string GetNameOfCategory(int index)
        {
            if (index >= 0 && index < _projectFinderHeaders.Length)
            {
                return _projectFinderHeaders[index];
            }
            Debug.LogError("No category found with given index of " + index);
            return "";
        }

        /// <summary>
        /// Construct main tab view that is going to be used in <see cref="DrawHeader"/>
        /// </summary>
        private void ConstructMainHeaders()
        {
            _headerContents.Add(RetrieveGUIContent("Project Finder", "UnityEditor.SceneHierarchyWindow"));
            _headerContents.Add(RetrieveGUIContent("Settings", "SettingsIcon"));
        }

        private void OnGUI()
        {
            if (!_initialized)
            {
                InitInternal();
            }
            if (_visualMode)
            {
                GUI.DrawTexture(new Rect(0, 0, EditorGUIUtility.currentViewWidth, position.height), _editorWindowBackground);
            }
            DrawHeader();

            DropAreaGUI();
            if (_autoClose && _reachedToAsset)
            {
                this.Close();
            }
        }
        public void DropAreaGUI()
        {
            Event evt = Event.current;
            Rect drop_area = new Rect(0, 0, EditorGUIUtility.currentViewWidth, position.height);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            EntryData entryData = new EntryData(draggedObject);
                            if (_tabIndex == 0)
                            {
                                entryData.Category = GetNameOfCategory(_projectFinderTabIndex);
                                entryData.Index = _projectFinderTabIndex;
                            }
                            else if (_tabIndex == 1)
                            {
                                entryData.Category = GetNameOfCategory(0);
                                entryData.Index = 0;
                            }
                            _tempLocations.Add(entryData);
                            SaveChanges();
                        }
                    }
                    break;
            }
        }

        #region HELPERS

        #region LOAD DATA

        private void LoadSettings()
        {
            //attempt to load the entries
            _currentSettings = ReadFromDisk<Settings>(SETTINGS_FILENAME);
            //if nothing is saved, retrieve the default values
            if (_currentSettings == null)
            {
                _currentSettings = new Settings();
                //_currentSettings.EntryData.Add(new EntryData(scenesPath, CATEGORY_SCENE, GetIndexOfCategory(CATEGORY_SCENE)));
                //_currentSettings.EntryData.Add(new EntryData(prefabsPath, CATEGORY_PREFAB, GetIndexOfCategory(CATEGORY_PREFAB)));
                //_currentSettings.EntryData.Add(new EntryData(scriptsPath, CATEGORY_SCRIPT, GetIndexOfCategory(CATEGORY_SCRIPT)));
                _currentSettings.PingType = PingTypes.Both;
                _currentSettings.Save();
            }
            _tempLocations.AddRange(EntryData.Clone(_currentSettings.EntryData.ToArray()));

            _pingType = _currentSettings.PingType;
            _visualMode = _currentSettings.VisualMode;
            VisualMode(_visualMode);
            _autoClose = _currentSettings.AutoClose;
        }

        #endregion

        #region STRING HELPERS

        private string Capital(string s)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
        }
        private string GetLastNameFromPath(string path)
        {
            string[] s = path.Split('/');
            return s[s.Length - 1];
        }

        private GUIContent ContentWithIcon(string name, string path)
        {

            GUIContent c = new GUIContent(name, AssetDatabase.GetCachedIcon(path));
            return c;
        }
        /// <summary>
        /// Assumes that the name is actually type and tries to resolve icon from name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private GUIContent ContentWithIcon(string name)
        {
            GUIContent c = EditorGUIUtility.IconContent(ResolveIconNameFromFileExtension(name));
            c.text = name;
            return c;
        }
        private string ResolveFileExtensionFromHeaderName(string header)
        {
            switch (header)
            {
                case CATEGORY_SCENE:
                    return "unity";
                case CATEGORY_PREFAB:
                    return "prefab";
                case CATEGORY_SCRIPT:
                    return "cs";
                case CATEGORY_SO:
                    return "asset";
                case CATEGORY_STARRED:
                    return "Favorite";
                default:
                    return "default";
            }
        }
        private string ResolveIconNameFromFileExtension(string fileExtension)
        {
            switch (fileExtension)
            {
                case "unity":
                    return "SceneAsset Icon";
                case "prefab":
                    return "PrefabNormal Icon";
                case "mat":
                    return "Material Icon";
                case "cs":
                    return "cs Script Icon";
                case "wav":
                    return "AudioClip Icon";
                case "mp3":
                    return "AudioClip Icon";
                case "flac":
                    return "AudioClip Icon";
                case "folder":
                    return "Folder Icon";
                case "dll":
                    return "dll Script Icon";
                case "fbx":
                    return "PrefabModel Icon";
                case "asset":
                    return "ScriptableObject Icon";
                case "txt":
                    return "TextAsset Icon";
                case "controller":
                    return "UnityEditor.Graphs.AnimatorControllerTool";
                case "Favorite":
                    return "Favorite";




                default:
                    return "DefaultAsset Icon";
            }
        }

        #endregion

        #region GUI STYLE ARRRANGEMENT
        /// <summary>
        /// Creates a single pixel Texture2D and paints it with given color. We can't directly edit GUIStyle's color so we do this.
        /// </summary>
        private Texture2D CreateColorForEditor(string htmlString)
        {
            Texture2D t = new Texture2D(1, 1);
            Color c;
            ColorUtility.TryParseHtmlString(htmlString, out c);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
        /// <summary>
        /// Creates a color from given HTML string.
        /// </summary>
        /// <param name="htmlString"></param>
        /// <returns></returns>
        private Color CreateColor(string htmlString)
        {
            Color c;
            ColorUtility.TryParseHtmlString(htmlString, out c);
            return c;
        }

        private void ConstructStyles()
        {

            VisualMode(_visualMode);

        }

        private void VisualMode(bool visualMode)
        {
            if (visualMode)
            {
                _buttonStyle = new GUIStyle(EditorStyles.miniButton);
                _textFieldStyle = new GUIStyle(EditorStyles.textField);
                _scrollViewStyle = new GUIStyle();
                _boxStyle = new GUIStyle(EditorStyles.helpBox);
                _popupStyle = new GUIStyle(EditorStyles.popup);
                _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);

                _editorWindowBackground = CreateColorForEditor("#362914");

                _buttonStyle.normal.background = CreateColorForEditor("#EACA93");
                _buttonStyle.active.background = CreateColorForEditor("#5A4B31");
                _buttonStyle.active.textColor = CreateColor("#ecf0f1");
                _buttonStyle.focused.background = CreateColorForEditor("#EACA93");
                _buttonStyle.alignment = TextAnchor.MiddleLeft;

                _scrollViewStyle.normal.background = CreateColorForEditor("#231703");

                _textFieldStyle.normal.background = CreateColorForEditor("#EACA93");
                _textFieldStyle.active.background = CreateColorForEditor("#EACA93");
                _textFieldStyle.focused.background = CreateColorForEditor("#EACA93");

                _boxStyle.normal.background = CreateColorForEditor("#EACA93");

                _popupStyle.normal.background = CreateColorForEditor("#EACA93");
                _popupStyle.focused.background = CreateColorForEditor("#EACA93");

                _toolbarButtonStyle.normal.background = CreateColorForEditor("#EACA93");
                _toolbarButtonStyle.alignment = TextAnchor.MiddleLeft;
            }
            else
            {
                _buttonStyle = new GUIStyle(EditorStyles.miniButton);
                _textFieldStyle = new GUIStyle(EditorStyles.textField);
                _scrollViewStyle = new GUIStyle();
                _boxStyle = new GUIStyle(EditorStyles.helpBox);
                _popupStyle = new GUIStyle(EditorStyles.popup);
                _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);

                _buttonStyle.alignment = TextAnchor.MiddleLeft;
                _toolbarButtonStyle.alignment = TextAnchor.MiddleLeft;
            }
        }
        #endregion

        #region IOHELPER
        private static void WriteToDisk(string fileName, object serializeObject)
        {
            string str = JsonUtility.ToJson(serializeObject);
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
            File.AppendAllText(path, str + Environment.NewLine);
        }
        private static T ReadFromDisk<T>(string fileName)
        {
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
            T returnObject = default(T);
            if (File.Exists(path))
            {
                using (StreamReader streamReader = new StreamReader(path))
                {
                    string line;
                    while (!string.IsNullOrEmpty(line = streamReader.ReadLine()))
                    {
                        returnObject = Deserialize<T>(line);
                    }
                }
            }
            return returnObject;
        }
        private static T Deserialize<T>(string text)
        {
            text = text.Trim();
            Type typeFromHandle = typeof(T);
            object obj = null;
            try
            {
                obj = JsonUtility.FromJson<T>(text);
            }
            catch (Exception ex)
            {
                Debug.LogError("Cannot deserialize to type " + typeFromHandle.ToString() + ": " + ex.Message + ", Json string: " + text);
            }
            if (obj != null && obj.GetType() == typeFromHandle)
            {
                return (T)obj;
            }
            return default(T);
        }
        private static void ClearData(string fileName)
        {
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
            if (File.Exists(path))
            {
                using (FileStream fileStream = File.Open(path, FileMode.Open))
                {
                    fileStream.SetLength(0L);
                }
            }
        }
        #endregion

        #endregion


        #region ELEMENT DRAWERS

        #region GUICONTENT

        private GUIContent[] RetrieveGUIContent(string[] entries)
        {
            GUIContent[] c = new GUIContent[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                c[i] = RetrieveGUIContent(entries[i], ResolveIconNameFromFileExtension(ResolveFileExtensionFromHeaderName(entries[i])));
            }
            return c;
        }

        /// <summary>
        /// Easily create GUIContent
        /// </summary>
        /// <param name="name"></param>
        /// <param name="iconName"></param>
        /// <param name="tooltip"></param>
        /// <returns></returns>
        private GUIContent RetrieveGUIContent(string name, string iconName = "", string tooltip = "")
        {
            if (iconName != null || iconName != "")
            {

                GUIContent c = new GUIContent(EditorGUIUtility.IconContent(iconName));
                c.text = name;
                c.tooltip = tooltip;
                return c;
            }
            else
            {
                return new GUIContent(name);
            }
        }
        #endregion

        #region BUTTON

        private const float _standardButtonMaxWidth = 25;
        private const float _standardButtonMaxHeight = 18;
        private const float _bigButtonMaxHeight = 30;
        private bool DrawButton(string name, string iconName = "", string tooltip = "")
        {
            if (iconName != null || iconName != "")
            {

                GUIContent c = new GUIContent(EditorGUIUtility.IconContent(iconName));
                c.text = name;
                c.tooltip = tooltip;
                return GUILayout.Button(c);
            }
            else
            {
                return GUILayout.Button(name);
            }

        }
        private bool DrawButton(string name, string iconName = "", params GUILayoutOption[] options)
        {
            if (iconName != null || iconName != "")
            {

                GUIContent c = new GUIContent(EditorGUIUtility.IconContent(iconName));
                c.text = name;
                return GUILayout.Button(c, options);
            }
            else
            {
                return GUILayout.Button(name, options);
            }

        }
        private bool DrawButton(string name, string iconName, ButtonTypes type)
        {
            GUILayoutOption[] options = null;

            switch (type)
            {
                case ButtonTypes.Standard:
                    options = new GUILayoutOption[] { GUILayout.MaxHeight(_standardButtonMaxHeight) };
                    break;
                case ButtonTypes.Big:
                    options = new GUILayoutOption[] { GUILayout.MaxHeight(_bigButtonMaxHeight) };
                    break;
                default:
                    break;
            }

            if (iconName != null || iconName != "")
            {

                GUIContent c = new GUIContent(EditorGUIUtility.IconContent(iconName));
                c.text = name;
                return GUILayout.Button(c, _buttonStyle, options);
            }
            else
            {
                Debug.LogError("Icon Name was null!");
                return false;
            }


        }

        #endregion

        #region READ-ONLY TEXT FIELD

        void ReadOnlyTextField(string label, string text)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                EditorGUILayout.SelectableLabel(text, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #endregion

        #region CONTENT

        #region MAIN HEADER DRAWERS

        private int _tabIndex = 0;
        private void DrawHeader()
        {
            _tabIndex = GUILayout.Toolbar(_tabIndex, _headerContents.ToArray());
            switch (_tabIndex)
            {
                case 0:
                    DrawProjectFinder();
                    break;
                case 1:
                    DrawSettings();
                    break;
                default:
                    break;
            }
        }
        private int _projectFinderTabIndex = 0;
        private bool _projectFinderSettingsFoldout;
        private bool _visualModeChanged;
        private bool _controlVisualMode;
        private bool _controlAutoClose;
        private bool _autoCloseChanged;
        private void DrawProjectFinder()
        {
            _projectFinderTabIndex = GUILayout.Toolbar(_projectFinderTabIndex, _projectFinderContents.ToArray(), _toolbarButtonStyle, GUILayout.ExpandHeight(false));
            switch (_projectFinderTabIndex)
            {
                case 0://starred
                    DrawProjectFinderEntries(CATEGORY_STARRED);
                    break;
                case 1://scenes
                    DrawProjectFinderEntries(CATEGORY_SCENE);

                    break;
                case 2://prefab
                    DrawProjectFinderEntries(CATEGORY_PREFAB);

                    break;
                case 3://script
                    DrawProjectFinderEntries(CATEGORY_SCRIPT);

                    break;
                case 4://so
                    DrawProjectFinderEntries(CATEGORY_SO);
                    break;
                default:
                    break;
            }
        }
        private bool _reachedToAsset;
        Vector2 _projectFinderEntriesScroll;
        private void DrawProjectFinderEntries(string category)
        {
            _projectFinderEntriesScroll = EditorGUILayout.BeginScrollView(_projectFinderEntriesScroll, _scrollViewStyle, GUILayout.MaxHeight(position.height));
            for (int i = 0; i < _currentSettings.EntryData.Count; i++)
            {
                if (_currentSettings.EntryData[i].Category == category)
                {
                    if (GUILayout.Button(ContentWithIcon(GetLastNameFromPath(_currentSettings.EntryData[i].Path), _currentSettings.EntryData[i].Path), _buttonStyle, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + (EditorGUIUtility.singleLineHeight * .5f))))
                    {
                        if (_pingType == PingTypes.Ping)
                        {
                            if (Selection.activeObject)
                            {
                                Selection.activeObject = null;
                            }

                            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(_currentSettings.EntryData[i].Path));
                        }
                        else if (_pingType == PingTypes.Selection)
                        {
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(_currentSettings.EntryData[i].Path);
                        }
                        else if (_pingType == PingTypes.Both)
                        {
                            if (Selection.activeObject)
                            {
                                Selection.activeObject = null;
                            }

                            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(_currentSettings.EntryData[i].Path));
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(_currentSettings.EntryData[i].Path);

                        }
                        EditorUtility.FocusProjectWindow();
                        _reachedToAsset = true;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

        }

        Vector2 _settingScrollPos;
        bool _changesMade = false;
        private void DrawSettings()
        {
            int toBeRemoved = -1;
            UnityEngine.Object pingedObject = null;
            _settingScrollPos = EditorGUILayout.BeginScrollView(_settingScrollPos, _scrollViewStyle, GUILayout.MaxHeight(position.height));
            //Iterate all found entries - key is path value is type
            for (int i = 0; i < _tempLocations.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        GUILayout.Space(4);
                        EditorGUILayout.SelectableLabel(_tempLocations[i].Path, _textFieldStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                    EditorGUILayout.EndVertical();
                    if (DrawButton("", "ViewToolOrbit", ButtonTypes.Standard))
                    {
                        pingedObject = AssetDatabase.LoadMainAssetAtPath(_tempLocations[i].Path);
                        if (Selection.activeObject)
                        {
                            Selection.activeObject = null;
                        }

                        EditorGUIUtility.PingObject(pingedObject);
                    }

                    if (DrawButton("Assign Selected Object", "TimeLinePingPong", ButtonTypes.Standard))
                    {
                        string s = AssetDatabase.GetAssetPath(Selection.activeObject);
                        if (s == "" || s == null || Selection.activeObject == null)
                        {
                            EditorUtility.DisplayDialog("Empty Selection", "Please select an item from Project Hierarchy.", "Okay");
                        }
                        else
                        {
                            _tempLocations[i] = Selection.activeObject;
                            _changesMade = true;
                        }
                        GUI.FocusControl(null);
                    }
                    //çatecori
                    ///*int categoryIndex*/ = GetIndexOfCategory(_tempPlayerPrefLocations[i].Category);
                    _tempLocations[i].Index = EditorGUILayout.Popup(_tempLocations[i].Index, RetrieveGUIContent(_projectFinderHeaders), _popupStyle, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight));

                    _tempLocations[i].Category = _projectFinderHeaders[_tempLocations[i].Index];

                    //Remove Button
                    if (DrawButton("", "ol minus", ButtonTypes.Standard))
                    {
                        toBeRemoved = i;
                    }
                }
                EditorGUILayout.EndHorizontal();

            }//endfor
            EditorGUILayout.EndScrollView();

            DrawInnerSettings();

            //Focus to Project window if a ping object is selected. Causes an error if it is directly made within the for loop
            if (pingedObject != null)
            {
                EditorUtility.FocusProjectWindow();
            }
            //Remove item
            if (toBeRemoved != -1)
            {
                _tempLocations.RemoveAt(toBeRemoved);
            }
            //--
            //Add

            if (DrawButton("Add", "ol plus", ButtonTypes.Big))
            {
                if (Selection.activeObject != null)
                {
                    _tempLocations.Add(Selection.activeObject);
                }
                else
                {
                    _tempLocations.Add("Assets");

                }
                GUI.FocusControl(null);
            }

            //Save
            if (DrawButton("Save", "redLight", ButtonTypes.Big))
            {
                SaveChanges();
            }
            //detect if any change occured, if not reverse the HelpBox
            if (_currentSettings.EntryData.Count != _tempLocations.Count)
            {
                _changesMade = true;
            }
            else
            {
                for (int i = 0; i < _currentSettings.EntryData.Count; i++)
                {
                    if (_currentSettings.EntryData[i].Path != _tempLocations[i].Path || _currentSettings.EntryData[i].Category != _tempLocations[i].Category)
                    {
                        _changesMade = true;
                        break;
                    }
                    if (i == _currentSettings.EntryData.Count - 1)
                    {
                        _changesMade = false;
                    }
                }
            }
            //Show info about saving
            if (_changesMade)
            {
                EditorGUILayout.HelpBox("Changes are made, you should save changes if you want to keep them.", MessageType.Info);
                if (DrawButton("Revert Changes", "TimeLineLoop", ButtonTypes.Standard))
                {
                    _tempLocations.Clear();
                    _tempLocations.AddRange(EntryData.Clone(_currentSettings.EntryData.ToArray()));
                }

            }
        }

        private void SaveChanges()
        {
            _currentSettings.EntryData.Clear();
            _currentSettings.EntryData.AddRange(EntryData.Clone(_tempLocations.ToArray()));
            //PlayerPrefs.SetString(PLAYERPREF_PATH, MergeStrings(_currentSettings.EntryData.Select(item => item.Path).ToArray()));
            //PlayerPrefs.SetString(PLAYERPREF_TYPE, MergeStrings(_currentSettings.EntryData.Select(item => item.Category).ToArray()));

            _currentSettings.Save();
            _changesMade = false;
        }
        #endregion

        #endregion

        private void DrawInnerSettings()
        {
            _projectFinderSettingsFoldout = EditorGUILayout.Foldout(_projectFinderSettingsFoldout, "Settings");
            if (_projectFinderSettingsFoldout)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                string label = "Current Ping Type : ";
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(label.Length * 6));
                if (_pingType == PingTypes.Ping)
                {
                    if (GUILayout.Button("Ping", _buttonStyle, GUILayout.ExpandWidth(false)))
                    {
                        _pingType = PingTypes.Selection;
                        _currentSettings.PingType = _pingType;
                        _currentSettings.Save();
                    }
                }
                else if (_pingType == PingTypes.Selection)
                {
                    if (GUILayout.Button("Selection", _buttonStyle, GUILayout.ExpandWidth(false)))
                    {
                        _pingType = PingTypes.Both;
                        _currentSettings.PingType = _pingType;
                        _currentSettings.Save();
                    }
                }
                else if (_pingType == PingTypes.Both)
                {
                    if (GUILayout.Button("Both", _buttonStyle, GUILayout.ExpandWidth(false)))
                    {
                        _pingType = PingTypes.Ping;
                        _currentSettings.PingType = _pingType;
                        _currentSettings.Save();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _controlAutoClose = _autoClose;
                _autoClose = EditorGUILayout.Toggle("Auto Close : ", _autoClose);

                if (_controlAutoClose != _autoClose)
                {
                    _autoCloseChanged = true;
                }
                if (_autoCloseChanged)
                {
                    _currentSettings.AutoClose = _autoClose;
                    _currentSettings.Save();
                    _autoCloseChanged = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                label = "Visual Mode(Experimental!) : ";
                //EditorGUILayout.LabelField(label, GUILayout.MaxWidth(label.Length * 6));
                _controlVisualMode = _visualMode;
                _visualMode = EditorGUILayout.Toggle(label, _visualMode);

                if (_controlVisualMode != _visualMode)
                {
                    _visualModeChanged = true;
                }
                if (_visualModeChanged)
                {
                    VisualMode(_visualMode);
                    _currentSettings.VisualMode = _visualMode;
                    _currentSettings.Save();
                    _visualModeChanged = false;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();


            }
        }
    }
}


public enum MainHeaders
{
    Scenes,
    Prefabs,
    Scripts
}
public enum ButtonTypes
{
    Standard,
    Big
}
public enum PingTypes
{
    Ping,
    Selection,
    Both
}