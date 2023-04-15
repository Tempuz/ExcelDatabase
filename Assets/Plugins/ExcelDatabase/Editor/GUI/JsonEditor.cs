using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExcelDatabase.Editor.GUI
{
    public class JsonEditor : EditorWindow
    {
        private string _jsonPath;
        private Dictionary<string, string>[] _json;

        public static void Open(string jsonPath)
        {
            var window = GetWindow<JsonEditor>();
            window.titleContent = new GUIContent("Excel Database Json Editor");
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            window._json = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(json.text);
            window._jsonPath = jsonPath;
            window.ListIDs();
        }

        public void CreateGUI()
        {
            ApplyUI();
            RegisterButton();
            ListIDs();
        }

        private void ApplyUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Plugins/ExcelDatabase/Editor/GUI/JsonEditor.uxml"
            );
            rootVisualElement.Add(visualTree.Instantiate());

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Plugins/ExcelDatabase/Editor/GUI/JsonEditor.uss"
            );
            rootVisualElement.styleSheets.Add(styleSheet);
        }

        private void RegisterButton()
        {
            rootVisualElement.Q<Button>("save-button").RegisterCallback<ClickEvent>(HandleSave);

            void HandleSave(ClickEvent _)
            {
                var json = JsonConvert.SerializeObject(_json, Formatting.Indented);
                File.WriteAllText(_jsonPath, json);
            }
        }

        private void ListIDs()
        {
            var idList = rootVisualElement.Q<ListView>("id-list");
            idList.itemsSource = _json;
            idList.makeItem = MakeItem;
            idList.bindItem = BindItem;
            idList.onSelectionChange += HandleSelectionChange;

            VisualElement MakeItem()
            {
                var label = new Label();
                label.AddToClassList("list-label");
                return label;
            }

            void BindItem(VisualElement e, int i)
            {
                if (e is Label label)
                {
                    label.text = _json[i]["ID"];
                }
            }

            void HandleSelectionChange(IEnumerable<object> selection)
            {
                var columns = selection.First() as IEnumerable<KeyValuePair<string, string>>;
                ListColumns(columns.Skip(1));
            }
        }

        private void ListColumns(IEnumerable<KeyValuePair<string, string>> columns)
        {
            var columnList = rootVisualElement.Q<ListView>("column-list");
            columnList.Clear();
            columnList.itemsSource = columns.ToList();
            columnList.makeItem = MakeItem;
            columnList.bindItem = BindItem;
            columnList.onSelectionChange += HandleSelectionChange;

            VisualElement MakeItem()
            {
                var label = new Label();
                label.AddToClassList("list-label");
                return label;
            }

            void BindItem(VisualElement e, int i)
            {
                if (e is Label label)
                {
                    label.text = $"{columns.ElementAt(i).Key}: {columns.ElementAt(i).Value}";
                }
            }

            void HandleSelectionChange(IEnumerable<object> selection) { }
        }
    }
}
