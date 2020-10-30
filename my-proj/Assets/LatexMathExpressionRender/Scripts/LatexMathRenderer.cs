using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LatexMathExpressionRender {
    public class LatexMathRenderer : MonoBehaviour {
        public delegate void OnRenderComplete(Texture2D result);
        bool wait;
        Color color;
        Text textObject;
        Camera renderCamera;
        static Texture2D opParanthesis;
        static Texture2D edParanthesis;
        static Texture2D signPlus;
        static Texture2D signMinu;
        void Start() {
            Load();
        }
        private void Load() {
            textObject = GetComponentInChildren<Text>(true);
            renderCamera = GetComponentInChildren<Camera>(true);
        }
        public void Render(string expresion, OnRenderComplete callback) {
            this.color = textObject.color;
            StartCoroutine(Render(Parser.parse(expresion), callback));
        }
        private IEnumerator Render(Node node, OnRenderComplete callback) {
            //if (opParanthesis == null) {
                wait = true;
                StartCoroutine(makeTexture("(", 50, '('));
                while (wait) yield return null;
                wait = true;
                StartCoroutine(makeTexture(")", 50, ')'));
                while (wait) yield return null;
                wait = true;
                StartCoroutine(makeTexture("+", 50, '+'));
                while (wait) yield return null;
                wait = true;
                StartCoroutine(makeTexture("-", 50, '-'));
                while (wait) yield return null;
            //}
            wait = true;
            StartCoroutine(MakeTextures(node));
            while (wait) yield return null;
            node.texture.wrapMode = TextureWrapMode.Clamp;
            callback.Invoke(node.texture);

        }
        IEnumerator MakeTextures(Node n) {
            if (n.nodes != null) {
                for (int i = 0; i < n.nodes.Count; i++) {
                    if (n.nodes[i].texture == null) {
                        wait = true;
                        StartCoroutine(MakeTextures(n.nodes[i]));
                        while (wait) yield return null;
                    }
                }
            }
            switch (n.op) {
                case "+":
                case "-":
                case "=":
                case "*":
                    wait = true;
                    StartCoroutine(makeTexture(n, 50));
                    while (wait) yield return null;
                    wait = true;
                    var tempMargin = Mathf.Max(n.nodes[0].bottomMargin, n.nodes[1].bottomMargin) - Mathf.Min(n.nodes[0].bottomMargin, n.nodes[1].bottomMargin);
                    n.bottomMargin = Mathf.Max(n.nodes[0].bottomMargin, n.nodes[1].bottomMargin);
                    if (n.implicitOp) {
                        n.texture = mergeTexturesHorizontal(n.nodes[0].texture, n.nodes[1].texture, n.bottomMargin);
                    }
                    else {
                        n.texture = mergeTexturesHorizontal(n.nodes[0].texture, n.texture, n.nodes[0].bottomMargin);
                        n.texture = mergeTexturesHorizontal(n.texture, n.nodes[1].texture, tempMargin);
                    }
                    if (n.hasParenthesis) n.texture = addParenthesis(n.texture);
                    wait = false;
                    break;
                case @"\frac":
                    wait = true;
                    n.texture = mergeTexturesVertical(n.nodes[0].texture, n.nodes[1].texture, true);
                    n.bottomMargin = n.nodes[1].texture.height + 4;
                    if (n.sign.Length > 0) n.texture = addSign(n.texture, n.sign == "+" ? signPlus : signMinu, n.bottomMargin);
                    if (n.hasParenthesis) n.texture = addParenthesis(n.texture);
                    wait = false;
                    break;
                case "^":
                    wait = true;
                    StartCoroutine(makeTexture(n.nodes[1], 25));
                    while (wait) yield return null;
                    wait = true;
                    n.texture = mergeTexturesPower(n.nodes[0].texture, n.nodes[1].texture);
                    if (n.hasParenthesis) n.texture = addParenthesis(n.texture);
                    wait = false;
                    break;
                case @"\sqrt":
                    wait = true;
                    n.bottomMargin = n.nodes[0].bottomMargin;
                    switch (n.nodes.Count) {
                        case 1: n.texture = makeTexturesRoot(n.nodes[0].texture, null); break;
                        case 2:
                            wait = true;
                            StartCoroutine(makeTexture(n.nodes[1], 25));
                            while (wait) yield return null;
                            n.texture = makeTexturesRoot(n.nodes[0].texture, n.nodes[1].texture);
                            break;
                        default: throw new System.Exception("No sqrt with 0 or more than 2 params implemented");
                    }
                    if (n.sign.Length > 0) n.texture = addSign(n.texture, n.sign == "+" ? signPlus : signMinu);
                    if (n.hasParenthesis) n.texture = addParenthesis(n.texture);
                    wait = false;
                    break;
                default:
                    wait = true;
                    StartCoroutine(makeTexture(n, 50));
                    while (wait) yield return null;
                    break;
            }
            yield return null;
        }
        IEnumerator makeTexture(string text, int fontSize, char target) {
            textObject.fontSize = fontSize;
            textObject.text = text;
            renderCamera.Render();
            yield return new WaitForEndOfFrame();
            var texture = CapturePixels();
            switch (target) {
                case '(': opParanthesis = texture; break;
                case ')': edParanthesis = texture; break;
                case '+': signPlus = texture; break;
                case '-': signMinu = texture; break;
                default: break;
            }
            wait = false;
            yield return null;
        }
        IEnumerator makeTexture(Node n, int fontSize) {
            textObject.fontSize = fontSize;
            textObject.text = n.op;
            renderCamera.Render();
            yield return new WaitForEndOfFrame();
            n.texture = CapturePixels();
            if (n.hasParenthesis) n.texture = addParenthesis(n.texture);
            wait = false;
            yield return null;
        }
        Texture2D mergeTexturesHorizontal(Texture2D t1, Texture2D t2, int basin) {
            int height = 0;
            if (basin > 0) {
                if (t1.height > t2.height) {
                    height = Mathf.Max(t2.height + basin, t1.height);
                }
                else if (t1.height < t2.height) {
                    height = Mathf.Max(t1.height + basin, t2.height);
                }
                else {
                    height = t1.height;
                }
            }
            else height = Mathf.Max(t1.height, t2.height);
            var result = new Texture2D(t1.width + t2.width, height);
            Fill(result);
            int t1Offset = 0, t2Offset = 0;
            if (t1.height > t2.height) {
                t2Offset = basin;
            }
            else if (t1.height < t2.height) {
                t1Offset = basin;
            }
            result.SetPixels(0, t1Offset, t1.width, t1.height, t1.GetPixels());
            result.SetPixels(t1.width, t2Offset, t2.width, t2.height, t2.GetPixels());
            result.Apply();
            return result;
        }
        Texture2D mergeTexturesVertical(Texture2D t1, Texture2D t2, bool separator = false) {
            var separatorThickness = 4;
            var result = new Texture2D(Mathf.Max(t1.width, t2.width), t1.height + t2.height + (separator ? separatorThickness : 0));
            Fill(result);
            int t1Offset = 0, t2Offset = 0;
            if (t1.width > t2.width) {
                t2Offset = Mathf.RoundToInt((t1.width - t2.width) / 2);
            }
            else if (t1.width < t2.width) {
                t1Offset = Mathf.RoundToInt((t2.width - t1.width) / 2);

            }
            result.SetPixels(t1Offset, t2.height + (separator ? separatorThickness : 0), t1.width, t1.height, t1.GetPixels());
            if (separator) {
                var sepArray = new Color[result.width * separatorThickness];
                for (int i = 0; i < sepArray.Length; i++) {
                    sepArray[i] = color;
                }
                result.SetPixels(0, t2.height, result.width, separatorThickness, sepArray);
            }
            result.SetPixels(t2Offset, 0, t2.width, t2.height, t2.GetPixels());
            result.Apply();
            return result;
        }
        Texture2D mergeTexturesPower(Texture2D t1, Texture2D t2) {
            var result = new Texture2D(t1.width + t2.width, t1.height + t2.height / 2);
            Fill(result);
            result.SetPixels(0, 0, t1.width, t1.height, t1.GetPixels());
            result.SetPixels(t1.width, t1.height - t2.height / 2, t2.width, t2.height, t2.GetPixels());
            result.Apply();
            return result;
        }
        Texture2D makeTexturesRoot(Texture2D t1, Texture2D t2) {
            var bottomMargin = 10;
            var topMargin = 2;
            var prefixWidth = 15;
            var topPadding = 4;
            var firstSlopeWidth = 3;
            var secondSlopeWidth = 6;
            var thirdSlopeWidth = 6;
            var t2offset = 0;
            var t2hoffset = 0;
            var startPoint = (t1.height + topPadding) * firstSlopeWidth / 2;
            if (t2 != null) {
                t2offset = t2.width;
                t2hoffset = t2.height - (t1.height + topPadding - startPoint / firstSlopeWidth);
            }
            var result = new Texture2D(t1.width + prefixWidth + t2offset, t1.height + topPadding + t2hoffset + 3);
            Fill(result);
            result.SetPixels(t2offset + prefixWidth, 0, t1.width, t1.height, t1.GetPixels());
            var firstSlope = new Color[(t1.height + topPadding) * firstSlopeWidth];
            var secondSlope = new Color[(t1.height + topPadding) * secondSlopeWidth];
            var thirdSlope = new Color[(t1.height + topPadding) * thirdSlopeWidth];
            var topCover = new Color[topPadding * t1.width];
            var transparent = new Color(0, 0, 0, 0);
            for (int i = 0; i < firstSlope.Length; i++) firstSlope[i] = transparent;
            for (int i = 0; i < secondSlope.Length; i++) secondSlope[i] = transparent;
            for (int i = 0; i < thirdSlope.Length; i++) thirdSlope[i] = transparent;
            for (int i = 0; i < topPadding; i++) {
                var c = (i < 2) ? color : transparent;
                for (int j = 0; j < t1.width; j++) topCover[i * t1.width + j] = c;
            }
            firstSlope[startPoint] = color;
            firstSlope[startPoint + 1] = color;
            firstSlope[startPoint + firstSlopeWidth + 1] = color;
            firstSlope[startPoint + firstSlopeWidth + 2] = color;
            firstSlope[startPoint + 2 * firstSlopeWidth + 2] = color;

            int lineLength;
            var secondStartPoint = (t1.height + topPadding) * secondSlopeWidth / 2 + 2 * secondSlopeWidth;
            lineLength = (t1.height + topPadding - topMargin - bottomMargin) / (2 * secondSlopeWidth);
            for (int i = 0; i < secondSlopeWidth; i++) {
                for (int j = 0; j < lineLength; j++) secondSlope[secondStartPoint - j * secondSlopeWidth] = color;
                secondStartPoint = secondStartPoint - secondSlopeWidth * (lineLength) + 1;
            }
            var thirdStartPoint = thirdSlopeWidth * bottomMargin;
            lineLength = (t1.height + topPadding - topMargin - bottomMargin) / thirdSlopeWidth;
            for (int i = 0; i < thirdSlopeWidth; i++) {
                for (int j = 0; j < lineLength; j++) thirdSlope[thirdStartPoint + j * thirdSlopeWidth] = color;
                thirdStartPoint = thirdStartPoint + thirdSlopeWidth * (lineLength) + 1;
            }

            result.SetPixels(t2offset, 0, firstSlopeWidth, t1.height + topPadding, firstSlope);
            result.SetPixels(t2offset + firstSlopeWidth, 0, secondSlopeWidth, t1.height + topPadding, secondSlope);
            result.SetPixels(t2offset + firstSlopeWidth + secondSlopeWidth, 0, thirdSlopeWidth, t1.height + topPadding, thirdSlope);
            result.SetPixels(t2offset + prefixWidth, t1.height, t1.width, topPadding, topCover);
            if (t2offset > 0) {
                result.SetPixels(0,  startPoint/firstSlopeWidth + 3, t2.width, t2.height, t2.GetPixels());
            }


            result.Apply();
            return result;
        }
        Texture2D addParenthesis(Texture2D t1) {
            var result = new Texture2D(t1.width + 2 * opParanthesis.width + edParanthesis.width, t1.height);
            Fill(result);
            result.SetPixels(0, t1.height / 2 - opParanthesis.height / 2, opParanthesis.width, opParanthesis.height, opParanthesis.GetPixels());
            result.SetPixels(opParanthesis.width, 0, t1.width, t1.height, t1.GetPixels());
            result.SetPixels(opParanthesis.width + t1.width, t1.height / 2 - opParanthesis.height / 2, edParanthesis.width, edParanthesis.height, edParanthesis.GetPixels());
            result.Apply();
            return result;
        }
        Texture2D addSign(Texture2D t1, Texture2D sign, int bottomMargin = 0) {
            var result = new Texture2D(t1.width + sign.width, t1.height);
            Fill(result);
            result.SetPixels(0, bottomMargin, sign.width, sign.height, sign.GetPixels());
            result.SetPixels(sign.width, 0, t1.width, t1.height, t1.GetPixels());
            result.Apply();
            return result;
        }
        Texture2D addParenthesisAdjustableHeight(Texture2D t1) {
            var symbolWidth = 10;
            var result = new Texture2D(t1.width + 2 * symbolWidth, t1.height);
            Fill(result);
            var opArea = result.GetPixels(0, 0, symbolWidth, t1.height);
            var edArea = result.GetPixels(t1.width - symbolWidth, 0, symbolWidth, t1.height);
            for (int i = 0; i < t1.height / 2; i++) {
                var pDistance = (t1.height / 2f - i) / (t1.height / 2f);
                opArea[i * symbolWidth + Mathf.FloorToInt(symbolWidth * Mathf.Sin(90f * pDistance * Mathf.PI / 180))] = Color.black;
            }
            for (int i = t1.height / 2; i < t1.height; i++) {
                opArea[i * symbolWidth + Mathf.FloorToInt(symbolWidth * ((i - t1.height / 2f) / (t1.height / 2f)))] = Color.black;
            }
            result.SetPixels(0, 0, symbolWidth, t1.height, opArea);
            result.SetPixels(symbolWidth, 0, t1.width, t1.height, t1.GetPixels());
            result.SetPixels(symbolWidth + t1.width, 0, symbolWidth, t1.height, edArea);
            result.Apply();
            return result;
        }

        private static void Fill(Texture2D result) {
            var array = result.GetPixels();
            var color = new Color(0, 0, 0, 0);
            for (int i = 0; i < array.Length; i++) {
                array[i] = color;
            }
            result.SetPixels(array);
        }

        public Texture2D CapturePixels() {
            int width = Mathf.FloorToInt(textObject.rectTransform.rect.width);
            int height = Mathf.FloorToInt(textObject.rectTransform.rect.height);
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            RenderTexture.active = renderCamera.targetTexture;
            var ap = textObject.rectTransform.anchoredPosition;
            texture.ReadPixels(new Rect(ap.x, ap.y, textObject.rectTransform.rect.width, textObject.rectTransform.rect.height), 0, 0);
            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++) {
                if (pixels[i] == Color.white) pixels[i] = new Color(255, 0, 0, 0);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        public Texture2D CreateTextureByPixel() {
            int width = 100;
            int height = 100;

            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Point;

            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    texture.SetPixel(j, height - 1 - i, Color.red);
                }
            }
            texture.Apply();
            return texture;
        }
    }
}