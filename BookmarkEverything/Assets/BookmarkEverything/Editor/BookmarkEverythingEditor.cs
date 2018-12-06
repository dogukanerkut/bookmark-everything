using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Text;

namespace BookmarkEverything
{
	public class BookmarkEverythingEditor : EditorWindow
    {
        private SaveData _currentSettings = new SaveData();

        private bool _initialized;
        private const char CHAR_SEPERATOR = ':';
        List<EntryData> _tempLocations = new List<EntryData>();
        private const string SETTINGS_FILENAME = "bookmarkeverythingsettings";
        private const string CATEGORY_SCENE = "Scenes";
        private const string CATEGORY_PREFAB = "Prefabs";
        private const string CATEGORY_SCRIPT = "Scripts";
        private const string CATEGORY_SO = "Scriptable Objects";
        private const string CATEGORY_STARRED = "Starred";
        private string[] _projectFinderHeaders = new string[] { CATEGORY_STARRED, CATEGORY_SCENE, CATEGORY_PREFAB, CATEGORY_SCRIPT, CATEGORY_SO };
        [System.Serializable]
        public class EntryData
        {
            public string GUID;
            public string Category;
            public int Index;

            public EntryData(string path, string category, int index)
            {
                GUID = path;
                Category = category;
                Index = index;
            }
            public EntryData(string path)
            {
                GUID = path;
                Category = "default";
            }
            public EntryData(UnityEngine.Object obj)
            {
				//use GetAssetPath+AssetPathToGUID instead of TryGetGUIDAndLocalFileIdentifier because that method is fairly new and not supported in many unity editors
				string path = AssetDatabase.GetAssetPath(obj);
				string guid = AssetDatabase.AssetPathToGUID(path);
                //AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out localId);

                GUID = guid;
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
                return new EntryData(data.GUID, data.Category, data.Index);
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
        public class SaveData
        {
            public List<EntryData> EntryData = new List<EntryData>();
            public PingTypes PingType;
            public bool VisualMode;
            public bool AutoClose;
            public bool ShowFullPath;
            public bool ShowFullPathForFolders = true;
            public SaveData(List<EntryData> entryData, PingTypes pingType, bool visualMode, bool autoClose, bool showFullPath, bool showFullPathForFolders)
            {
                EntryData = entryData;
                PingType = pingType;
                VisualMode = visualMode;
                AutoClose = autoClose;
                ShowFullPath = showFullPath;
                ShowFullPathForFolders = showFullPathForFolders;
            }
            public SaveData() { }

            public void Save()
            {
                IOHelper.ClearData(SETTINGS_FILENAME);
                IOHelper.WriteToDisk(SETTINGS_FILENAME, this);
            }
        }

        #region GUI REFERENCES
        GUIStyle _buttonStyle;
        GUIStyle _textFieldStyle;
        GUIStyle _scrollViewStyle;
        GUIStyle _boxStyle;
        GUIStyle _popupStyle;
        GUIStyle _toolbarButtonStyle;
        GUIStyle _boldLabelStyle;

        Texture _editorWindowBackground;
        #endregion

        private List<GUIContent> _headerContents = new List<GUIContent>();
        private List<GUIContent> _projectFinderContents = new List<GUIContent>();

        private PingTypes _pingType;
        private bool _visualMode;
        private bool _autoClose;
        private bool _showFullPath;
        private bool _showFullPathForFolder;

        [MenuItem("Window/Bookmark Everything %h")]
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

        private void OnEnable() {
            titleContent =RetrieveGUIContent("Bookmark", "CustomSorting");
            _defaultGUIColor = GUI.color;
            minSize = new Vector2(400,400);
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
            
        }
		//Older versions of Unity doesn't like Close() being called in OnGUI
		private void Update()
		{
			if (_autoClose && _reachedToAsset)
			{
				this.Close();
			}
			else if (!_autoClose && _reachedToAsset)
			{
				_reachedToAsset = false;
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
                        List<EntryData> duplicateList = new List<EntryData>();
                        List<EntryData> allowedList = new List<EntryData>();
                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if(!AssetDatabase.Contains(draggedObject))
                            {
                                EditorUtility.DisplayDialog("Bookmark Everything", "Objects from hierarchy is not supported for now. Would you like me to add that? Please e-mail me at dogukanerkut@gmail.com.", "Okay");
                                return;
                            }
                            EntryData entryData = new EntryData(draggedObject);
                            if (_tempLocations.Contains(entryData, new EntryDataGUIDComparer()))
                            {
                                duplicateList.Add(_tempLocations.Find((entry) => entry.GUID == entryData.GUID));
                            }
                            else
                            {
                                allowedList.Add(entryData);
                            }
                        }
                        if(duplicateList.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("\n\n");
                            for (int i = 0; i < duplicateList.Count; i++)
                            {
                                sb.Append(string.Format("{0} in {1} Category\n\n",  GetNameForFile(AssetDatabase.GUIDToAssetPath(duplicateList[i].GUID)), duplicateList[i].Category));
                            }

                            if (EditorUtility.DisplayDialog("Bookmark Everything", string.Format("Duplicate Entries Found: {0} Would you still like to add them ?(Non-duplicates will be added anyway)", sb.ToString()), "Yes", "No"))
                            {
                                duplicateList.AddRange(allowedList);
                                for (int i = 0; i < duplicateList.Count; i++)
                                {
                                    if (_tabIndex == 0)
                                    {
                                        duplicateList[i].Category = GetNameOfCategory(_projectFinderTabIndex);
                                        duplicateList[i].Index = _projectFinderTabIndex;
                                    }
                                    else if (_tabIndex == 1)
                                    {
                                        duplicateList[i].Category = GetNameOfCategory(0);
                                        duplicateList[i].Index = 0;
                                        _lastlyAddedCount++;

                                    }
                                }
                                _tempLocations.AddRange(duplicateList);
                                if (_tabIndex == 0)
                                {
                                    SaveChanges();
                                }
                                // else if (_tabIndex == 1)
                                // {
                                //     _changesMade = true;
                                // }
                            }
                            else
                            {
                                for (int i = 0; i < allowedList.Count; i++)
                                {
                                    if (_tabIndex == 0)
                                    {
                                        allowedList[i].Category = GetNameOfCategory(_projectFinderTabIndex);
                                        allowedList[i].Index = _projectFinderTabIndex;
                                    }
                                    else if (_tabIndex == 1)
                                    {
                                        allowedList[i].Category = GetNameOfCategory(0);
                                        allowedList[i].Index = 0;
                                        _lastlyAddedCount++;
                                    }
                                }
                                _tempLocations.AddRange(allowedList);
                                if (_tabIndex == 0)
                                {
                                    SaveChanges();
                                }
                                // else if (_tabIndex == 1)
                                // {
                                //     _changesMade = true;
                                // }
                            }
                        }
                        else if(allowedList.Count > 0)
                        {
                            for (int i = 0; i < allowedList.Count; i++)
                            {
                                if (_tabIndex == 0)
                                {
                                    allowedList[i].Category = GetNameOfCategory(_projectFinderTabIndex);
                                    allowedList[i].Index = _projectFinderTabIndex;
                                }
                                else if (_tabIndex == 1)
                                {
                                    allowedList[i].Category = GetNameOfCategory(0);
                                    allowedList[i].Index = 0;
                                    _lastlyAddedCount++;

                                }
                            }

                            _tempLocations.AddRange(allowedList);
                            if (_tabIndex == 0)
                            {
                                SaveChanges();
                            }
                            // else if (_tabIndex == 1)
                            // {
                            //     _changesMade = true;
                            // }
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
            _currentSettings = IOHelper.ReadFromDisk<SaveData>(SETTINGS_FILENAME);
            //if nothing is saved, retrieve the default values
            if (_currentSettings == null)
            {
                _currentSettings = new SaveData();
                _currentSettings.PingType = PingTypes.Both;
                _currentSettings.Save();
            }
            _tempLocations.AddRange(EntryData.Clone(_currentSettings.EntryData.ToArray()));

            _pingType = _currentSettings.PingType;
            _visualMode = _currentSettings.VisualMode;
            VisualMode(_visualMode);
            _autoClose = _currentSettings.AutoClose;
            _showFullPath = _currentSettings.ShowFullPath;
            _showFullPathForFolder = _currentSettings.ShowFullPathForFolders;
        }

        #endregion

        #region STRING HELPERS

        private string Capital(string s)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
        }
        private string GetNameForFile(string path)
        {
            if (_currentSettings.ShowFullPath)
            {
                return path;
            }
            string[] s = path.Split('/');
            return s[s.Length - 1];
        }
        private string GetNameForFolder(string path)
        {
            if (_currentSettings.ShowFullPathForFolders)
            {
                return path;
            }
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
#if UNITY_5_1
			Color.TryParseHexString(htmlString, out c);
#else
			ColorUtility.TryParseHtmlString(htmlString, out c);
#endif
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
#if UNITY_5_1
			Color.TryParseHexString(htmlString, out c);
#else
			ColorUtility.TryParseHtmlString(htmlString, out c);
#endif
			return c;
        }

        private void ConstructStyles()
        {

            VisualMode(_visualMode);

        }

        private void VisualMode(bool visualMode)
        {
            _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);

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
        private GUIContent RetrieveGUIContent(string name, string iconName = "", string tooltip = "", bool useIconResolver = false)
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
            if (iconName != null && iconName != "")
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
            if (iconName != null && iconName != "")
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
                    case ButtonTypes.SmallLongHeight:
                    options = new GUILayoutOption[]{ GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + (EditorGUIUtility.singleLineHeight * .5f)), GUILayout.MaxWidth(25) };
                    break;
                    case ButtonTypes.SmallNormalHeight:
                    options = new GUILayoutOption[] { GUILayout.MaxHeight(_standardButtonMaxHeight), GUILayout.MaxWidth(25) };
                    break;
                default:
                    break;
            }

            if (iconName != null && iconName != "")
            {

                GUIContent c = new GUIContent(EditorGUIUtility.IconContent(iconName));
                c.text = name;
                return GUILayout.Button(c, _buttonStyle, options);
            }
            else
            {
                return GUILayout.Button(name, options);
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
            if (_tabIndex == 0 && _changesMade)
            {
                    bool save = EditorUtility.DisplayDialog("Bookmark Everything", "You have unsaved changes. Would you like to save them?", "Yes", "No");
                    if (save)
                    {
                        SaveChanges();
                    }
                    else
                    {
                        _lastlyAddedCount = -1;
                        _tempLocations.Clear();
                        _tempLocations.AddRange(EntryData.Clone(_currentSettings.EntryData.ToArray()));
                        _changesMade = false;
                    }
            }
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
        private bool _visualModeChanged;
        private bool _controlVisualMode;
        private bool _controlAutoClose;
        private bool _autoCloseChanged;
        private bool _controlShowFullPath;
        private bool _showFullPathChanged;
        private bool _controlShowFullPathForFolder;
        private bool _showFullPathForFolderChanged;
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
        int _objectIndexToBeRemovedDueToDeletedAsset = -1;
        int _objectIndexToBeRemoved = -1;
        private void DrawProjectFinderEntries(string category)
        {
			bool clicked = false;
            _projectFinderEntriesScroll = EditorGUILayout.BeginScrollView(_projectFinderEntriesScroll, _scrollViewStyle, GUILayout.MaxHeight(position.height));
            for (int i = 0; i < _currentSettings.EntryData.Count; i++)
            {
                if (_currentSettings.EntryData[i].Category == category)
                {
                    string path = AssetDatabase.GUIDToAssetPath(_currentSettings.EntryData[i].GUID);
                    bool exists = IOHelper.Exists(path);
                    bool isFolder = IOHelper.IsFolder(path);
                    GUIContent content;
                    if (exists)
                    {
                        content = ContentWithIcon(isFolder ? GetNameForFolder(path) : GetNameForFile(path), path);
                    }
                    else
                    {
                        content = RetrieveGUIContent((isFolder ? GetNameForFolder(path) : GetNameForFile(path)) + "(File is removed, click to remove)" , "console.erroricon.sml");
                    }
                   EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(content, _buttonStyle, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight + (EditorGUIUtility.singleLineHeight * .5f))))
                    {
                        if (exists)
                        {
                            if (_pingType == PingTypes.Ping)
                            {
                                if (Selection.activeObject)
                                {
                                    Selection.activeObject = null;
                                }
                                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
                            }
                            else if (_pingType == PingTypes.Selection)
                            {
                                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
                            }
                            else if (_pingType == PingTypes.Both)
                            {
                                if (Selection.activeObject)
                                {
                                    Selection.activeObject = null;
                                }
                                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
                                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
                            }
							clicked = true;
                        }
                        else
                        {
                            _objectIndexToBeRemovedDueToDeletedAsset = i;
                        }
                        _reachedToAsset = true;
                    }
                    if (DrawButton("", "ol minus", ButtonTypes.SmallLongHeight))
                    {
                        _objectIndexToBeRemoved = i;
                    }
                   EditorGUILayout.EndHorizontal();
				}

            }
            if (_objectIndexToBeRemovedDueToDeletedAsset != -1)
            {
                _tempLocations.RemoveAt(_objectIndexToBeRemovedDueToDeletedAsset);
                _objectIndexToBeRemovedDueToDeletedAsset = -1;
                SaveChanges();
            }
            if(_objectIndexToBeRemoved != -1)
            {
                 _tempLocations.RemoveAt(_objectIndexToBeRemoved);
                _objectIndexToBeRemoved = -1;
                SaveChanges();
            }
             if (_currentSettings.EntryData.Count == 0)
            {
                EditorGUILayout.LabelField("You can Drag&Drop assets from Project Folder and easily access them here.", _boldLabelStyle);
            }
            EditorGUILayout.EndScrollView();
			
			//Older version of unity has issues with FocusProjectWindow being in the middle of the run(before EndXViews).
			if (clicked)
			{
				EditorUtility.FocusProjectWindow();
			}
		}

        Vector2 _settingScrollPos;
        bool _changesMade = false;
        int _lastlyAddedCount = -1;
        Color _defaultGUIColor;
        private void DrawSettings()
        {
            DrawInnerSettings();

            int toBeRemoved = -1;
            UnityEngine.Object pingedObject = null;
            _settingScrollPos = EditorGUILayout.BeginScrollView(_settingScrollPos, _scrollViewStyle, GUILayout.MaxHeight(position.height));
            //Iterate all found entries - key is path value is type
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Manage Registered Assets", _boldLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            for (int i = 0; i < _tempLocations.Count; i++)
            {
                bool exists = IOHelper.Exists(_tempLocations[i].GUID, ExistentialCheckStrategy.GUID);
                if (_lastlyAddedCount != -1 && i >= _tempLocations.Count - _lastlyAddedCount -1)
                {
                    GUI.color = Color.green;
                }
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        string fullPath = exists ? AssetDatabase.GUIDToAssetPath(_tempLocations[i].GUID) : "(Removed)" + AssetDatabase.GUIDToAssetPath(_tempLocations[i].GUID);
                        GUILayout.Space(4);
                        EditorGUILayout.SelectableLabel(fullPath, _textFieldStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                    EditorGUILayout.EndVertical();
                    if (!exists)
                    {
                        GUI.enabled = false;
                    }
                    if (DrawButton("", "ViewToolOrbit", ButtonTypes.SmallNormalHeight))
                    {
                        pingedObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(_tempLocations[i].GUID));
                        if (Selection.activeObject)
                        {
                            Selection.activeObject = null;
                        }

                        if (_pingType == PingTypes.Ping)
                            {
                                EditorGUIUtility.PingObject(pingedObject);
                            }
                            else if (_pingType == PingTypes.Selection)
                            {
                                Selection.activeObject = pingedObject;
                            }
                            else if (_pingType == PingTypes.Both)
                            {
                                EditorGUIUtility.PingObject(pingedObject);
                                Selection.activeObject = pingedObject;
                            }
                    }

                    // if (DrawButton("Assign Selected Object", "TimeLinePingPong", ButtonTypes.Standard))
                    // {
                    //     string s = AssetDatabase.GetAssetPath(Selection.activeObject);
                    //     if (s == "" || s == null || Selection.activeObject == null)
                    //     {
                    //         EditorUtility.DisplayDialog("Empty Selection", "Please select an item from Project Hierarchy.", "Okay");
                    //     }
                    //     else
                    //     {
                    //         _tempLocations[i] = Selection.activeObject;
                    //         _changesMade = true;
                    //     }
                    //     GUI.FocusControl(null);
                    // }
                    //çatecori
                    ///*int categoryIndex*/ = GetIndexOfCategory(_tempPlayerPrefLocations[i].Category);
                    _tempLocations[i].Index = EditorGUILayout.Popup(_tempLocations[i].Index, RetrieveGUIContent(_projectFinderHeaders), _popupStyle, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(150));

                    _tempLocations[i].Category = _projectFinderHeaders[_tempLocations[i].Index];
                   
                    if (!exists)
                    {
                        GUI.enabled = true;
                    }
                    //Remove Button
                    if (DrawButton("", "ol minus", ButtonTypes.SmallNormalHeight))
                    {
                        if(_lastlyAddedCount != -1 && i >= _tempLocations.Count - _lastlyAddedCount -1)
                        {
                            _lastlyAddedCount--;
                        }
                        toBeRemoved = i;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (_lastlyAddedCount != -1 && i >= _tempLocations.Count - _lastlyAddedCount - 1)
                {
                    GUI.color = _defaultGUIColor;
                }
            }//endfor
             if (_tempLocations.Count == 0 && _currentSettings.EntryData.Count == 0)
            {
                EditorGUILayout.LabelField("Start dragging some assets from Project Folder!", _boldLabelStyle);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

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

            // if (DrawButton("Add", "ol plus", ButtonTypes.Big))
            // {
            //     if (Selection.activeObject != null)
            //     {
            //         _tempLocations.Add(Selection.activeObject);
            //     }
            //     else
            //     {
            //         EditorUtility.DisplayDialog("Empty Selection", "Please select an item from Project Hierarchy.", "Okay");
            //     }
            //     GUI.FocusControl(null);
            // }

            //Save
           
            //detect if any change occured, if not reverse the HelpBox
            if (_currentSettings.EntryData.Count != _tempLocations.Count)
            {
                _changesMade = true;
            }
            else
            {
                for (int i = 0; i < _currentSettings.EntryData.Count; i++)
                {
                    if (_currentSettings.EntryData[i].GUID != _tempLocations[i].GUID || _currentSettings.EntryData[i].Category != _tempLocations[i].Category)
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
                if (DrawButton("Save", "redLight", ButtonTypes.Big))
                {
                    SaveChanges();
                }
                EditorGUILayout.HelpBox("Changes are made, you should save changes if you want to keep them.", MessageType.Info);
                if (DrawButton("Discard Changes", "", ButtonTypes.Standard))
                {
                    _lastlyAddedCount = -1;
                    _tempLocations.Clear();
                    _tempLocations.AddRange(EntryData.Clone(_currentSettings.EntryData.ToArray()));
                    _changesMade  = false;
                }

            }
           
        }

        private void SaveChanges()
        {
            _currentSettings.EntryData.Clear();
            _currentSettings.EntryData.AddRange(EntryData.Clone(_tempLocations.ToArray()));
            _lastlyAddedCount = -1;

            _currentSettings.Save();
            _changesMade = false;
        }
#endregion

#endregion

        private void DrawInnerSettings()
        {
                EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("General Settings", _boldLabelStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.BeginHorizontal();
                string label = "Current Ping Type : ";
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(label.Length * 7.3f));
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
                label = "Show Full Path : ";
                _controlShowFullPath = _showFullPath;
                _showFullPath = EditorGUILayout.Toggle(label, _showFullPath);

                if (_controlShowFullPath != _showFullPath)
                {
                    _showFullPathChanged = true;
                }
                if (_showFullPathChanged)
                {
                    _currentSettings.ShowFullPath = _showFullPath;
                    _currentSettings.Save();
                    _showFullPathChanged = false;
                }
                
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                label = "Show Full Path(Folders) : ";
                _controlShowFullPathForFolder = _showFullPathForFolder;
                _showFullPathForFolder = EditorGUILayout.Toggle(label, _showFullPathForFolder);

                if (_controlShowFullPathForFolder != _showFullPathForFolder)
                {
                    _showFullPathForFolderChanged = true;
                }
                if (_showFullPathForFolderChanged)
                {
                    _currentSettings.ShowFullPathForFolders = _showFullPathForFolder;
                    _currentSettings.Save();
                    _showFullPathForFolderChanged = false;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                label = "Visual Mode(Experimental!) : ";
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


public enum MainHeaders
{
    Scenes,
    Prefabs,
    Scripts
}
public enum ButtonTypes
{
    Standard,
    Big,
    SmallLongHeight,
    SmallNormalHeight
}
public enum PingTypes
{
    Ping,
    Selection,
    Both
}