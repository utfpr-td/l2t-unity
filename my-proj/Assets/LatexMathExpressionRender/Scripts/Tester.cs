using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using UnityEngine.Rendering;
using System;
using UnityEngine.InputSystem;

namespace LatexMathExpressionRender {
    public class Tester : MonoBehaviour {
        public int fontSize = 50;
        private int lastSize;
        LatexMathRenderer mathRenderer;
        Image targetSpriteImage;
        RectTransform targetSpriteRect;
        InputField textCustomExp;
        Dropdown dropdown;
        //float axis;
        bool axis;
        void Start() {
            Load();
        }
        private void Awake() {
            Load();
        }
        private void Update() {
            //if (Input.GetAxis("Submit") > 0 && axis == 0) {
            //    OnTxtCustomExpEndEdit("self");
            //    axis = Input.GetAxis("Submit");
            //}
            //else if (Input.GetAxis("Submit") == 0 && axis > 0) {
            //    axis = 0;
            //}

            if (Keyboard.current.enterKey.isPressed && !axis) {
                OnTxtCustomExpEndEdit("self");
                axis = true;
            }
            else if (!Keyboard.current.enterKey.isPressed && axis) {
                axis = false;
            }
            if (lastSize != fontSize) {
                mathRenderer.Render(textCustomExp.text, RenderCallback, fontSize);
                lastSize = fontSize;
            }
        }
        public void OnDropdownValueChanged(int param) {
            var index = dropdown.value;
            if (index >= 0) {
                textCustomExp.text = DemoExpressions.all[index];
                mathRenderer.Render(textCustomExp.text, RenderCallback, fontSize);
                lastSize = fontSize;
            }
        }
        public void OnTxtCustomExpEndEdit(string val) {
            if (val == "self") {
                PlayerPrefs.SetString("lastExp", textCustomExp.text);
                textCustomExp.ActivateInputField();
                textCustomExp.caretPosition = textCustomExp.text.Length;
                mathRenderer.Render(textCustomExp.text, RenderCallback, fontSize);
                lastSize = fontSize;
            }
        }
        public void OnBtnMoveClick(int delta) {
            switch (delta) {
                case -1:
                    dropdown.value = dropdown.value == 0 ? dropdown.options.Count - 1 : dropdown.value - 1;
                    break;
                case 1:
                    dropdown.value = dropdown.value == dropdown.options.Count - 1 ? 0 : dropdown.value + 1;
                    break;
                default: throw new Exception("Not implemented move delta");
            }
        }
        private void RenderCallback(Texture2D result) {
            targetSpriteImage.sprite = Sprite.Create(result, new Rect(0, 0, result.width, result.height), Vector2.zero);
            targetSpriteImage.sprite.name = "Procedural Sprite";
            targetSpriteRect.sizeDelta = new Vector2(result.width, result.height);
        }
        private void Load() {
            var img = GameObject.Find("TargetImage");
            textCustomExp = GameObject.Find("CustomExp").GetComponent<InputField>();
            dropdown = GameObject.Find("Dropdown").GetComponent<Dropdown>();
            mathRenderer = GameObject.Find("LatexMathExpressionRenderer").GetComponent<LatexMathRenderer>();
            targetSpriteImage = img.GetComponent<Image>();
            targetSpriteRect = img.GetComponent<RectTransform>();
            textCustomExp.text = PlayerPrefs.GetString("lastExp");
            dropdown.options = new List<Dropdown.OptionData>();
            dropdown.ClearOptions();
            for (int i = 0; i < DemoExpressions.all.Count; i++) dropdown.options.Add(new Dropdown.OptionData(DemoExpressions.all[i]));
        }
    }
}