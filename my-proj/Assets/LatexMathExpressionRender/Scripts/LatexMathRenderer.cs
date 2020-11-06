using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LatexMathExpressionRender {
    public class LatexMathRenderer : MonoBehaviour {
        public int baseFontSize;
        public delegate void OnRenderComplete(Texture2D result);
        bool waitL1;
        bool waitL2;
        Color color;
        Node activeNode;
        Text textObject;
        Camera renderCamera;
        Texture2D activeTexture1;
        Texture2D activeTexture2;
        void Start() {
            textObject = GetComponentInChildren<Text>(true);
            renderCamera = GetComponentInChildren<Camera>(true);
            if (baseFontSize == 0) baseFontSize = 50;
        }
        public void Render(string expresion, OnRenderComplete callback) {
            color = textObject.color;
            var node = Parser.parse(expresion);
            node.fontSize = baseFontSize;
            StartCoroutine(Render(node, callback));
        }
        private IEnumerator Render(Node node, OnRenderComplete callback) {
            waitL1 = true;
            StartCoroutine(MakeTextures(node));
            while (waitL1) yield return null;
            node.texture.wrapMode = TextureWrapMode.Clamp;
            callback.Invoke(node.texture);

        }
        IEnumerator MakeTextures(Node n) {
            if (n.nodes != null) {
                if (n.op == "^" || n.op == @"\sqrt" && n.nodes.Count == 2) {
                    n.nodes[0].fontSize = n.fontSize;
                    n.nodes[1].fontSize = Mathf.Max(10, n.fontSize / 2);
                }
                else {
                    for (int i = 0; i < n.nodes.Count; i++) {
                        n.nodes[i].fontSize = n.fontSize;
                    }
                }
                for (int i = 0; i < n.nodes.Count; i++) {
                    if (n.nodes[i].texture == null) {
                        waitL1 = true;
                        StartCoroutine(MakeTextures(n.nodes[i]));
                        while (waitL1) yield return null;
                    }
                }
            }
            switch (n.op) {
                case "()":
                    waitL1 = true;
                    activeNode = n;
                    StartCoroutine(addParenthesis());
                    while (waitL1) yield return null;
                    break;
                case "+":
                case "-":
                case "=":
                case "*":
                    waitL1 = true;
                    StartCoroutine(makeTexture(n, n.fontSize));
                    while (waitL1) yield return null;
                    waitL1 = true;
                    var tempMargin = Mathf.Max(n.nodes[0].bottomMargin, n.nodes[1].bottomMargin) - Mathf.Min(n.nodes[0].bottomMargin, n.nodes[1].bottomMargin);
                    n.bottomMargin = Mathf.Max(n.nodes[0].bottomMargin, n.nodes[1].bottomMargin);
                    if (n.implicitOp) {
                        n.texture = mergeTexturesHorizontal(n.nodes[0].texture, n.nodes[1].texture, n.bottomMargin);
                    }
                    else {
                        n.texture = mergeTexturesHorizontal(n.nodes[0].texture, n.texture, n.nodes[0].bottomMargin);
                        n.texture = mergeTexturesHorizontal(n.texture, n.nodes[1].texture, tempMargin);
                    }
                    waitL1 = false;
                    break;
                case @"\frac":
                    waitL1 = true;
                    n.texture = mergeTexturesVertical(n.nodes[0].texture, n.nodes[1].texture, true);
                    n.bottomMargin = n.nodes[1].texture.height + 4;
                    if (n.sign.Length > 0) {
                        waitL1 = true;
                        activeNode = n;
                        StartCoroutine(addSign());
                        while (waitL1) yield return null;
                    }
                    waitL1 = false;
                    break;
                case "^":
                    //waitL1 = true;
                    //StartCoroutine(makeTexture(n.nodes[1], n.nodes[1].fontSize));
                    //while (waitL1) yield return null;
                    waitL1 = true;
                    n.texture = mergeTexturesPower(n.nodes[0].texture, n.nodes[1].texture);
                    waitL1 = false;
                    break;
                case @"\sqrt":
                    waitL1 = true;
                    n.bottomMargin = n.nodes[0].bottomMargin;
                    switch (n.nodes.Count) {
                        case 1: n.texture = makeTexturesRoot(n.nodes[0].texture, null); break;
                        case 2:
                            waitL1 = true;
                            StartCoroutine(makeTexture(n.nodes[1], n.nodes[1].fontSize));
                            while (waitL1) yield return null;
                            n.texture = makeTexturesRoot(n.nodes[0].texture, n.nodes[1].texture);
                            break;
                        default: throw new System.Exception("No sqrt with 0 or more than 2 params implemented");
                    }
                    if (n.sign.Length > 0) {
                        waitL1 = true;
                        activeNode = n;
                        StartCoroutine(addSign());
                        while (waitL1) yield return null;
                    }
                    waitL1 = false;
                    break;
                default:
                    waitL1 = true;
                    StartCoroutine(makeTexture(n, n.fontSize));
                    while (waitL1) yield return null;
                    break;
            }
            yield return null;
        }
        IEnumerator makeTexture(Node n, int fontSize) {
            textObject.fontSize = fontSize;
            textObject.text = n.op;
            renderCamera.Render();
            yield return new WaitForEndOfFrame();
            n.texture = CapturePixels();
            waitL1 = false;
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
            var result = new Texture2D(t1.width + t2.width, t1.height + Mathf.CeilToInt(t2.height / 2f));
            Fill(result);
            result.SetPixels(0, 0, t1.width, t1.height, t1.GetPixels());
            result.SetPixels(t1.width, t1.height - Mathf.CeilToInt(t2.height / 2f), t2.width, t2.height, t2.GetPixels());
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
                result.SetPixels(0, startPoint / firstSlopeWidth + 3, t2.width, t2.height, t2.GetPixels());
            }


            result.Apply();
            return result;
        }
        IEnumerator addParenthesis() {
            waitL2 = true;
            StartCoroutine(makeTexture("(", activeNode.fontSize, 1));
            while (waitL2) yield return null;

            waitL2 = true;
            StartCoroutine(makeTexture(")", activeNode.fontSize, 2));
            while (waitL2) yield return null;

            activeNode.texture = new Texture2D(activeNode.nodes[0].texture.width + activeTexture1.width + activeTexture2.width, activeNode.nodes[0].texture.height);
            Fill(activeNode.texture);
            activeNode.texture.SetPixels(0, activeNode.nodes[0].texture.height / 2 - activeTexture1.height / 2, activeTexture1.width, activeTexture1.height, activeTexture1.GetPixels());
            activeNode.texture.SetPixels(activeTexture1.width, 0, activeNode.nodes[0].texture.width, activeNode.nodes[0].texture.height, activeNode.nodes[0].texture.GetPixels());
            activeNode.texture.SetPixels(activeTexture1.width + activeNode.nodes[0].texture.width, activeNode.nodes[0].texture.height / 2 - activeTexture1.height / 2, activeTexture2.width, activeTexture2.height, activeTexture2.GetPixels());
            activeNode.texture.Apply();
            waitL1 = false;
            yield return null;
        }
        IEnumerator addSign() {
            int width = activeNode.texture.width, height = activeNode.texture.height;
            Color[] pixels = activeNode.texture.GetPixels();

            waitL2 = true;
            StartCoroutine(makeTexture(activeNode.sign, activeNode.fontSize, 1));
            while (waitL2) yield return null;

            activeNode.texture = new Texture2D(width + activeTexture1.width, height);
            Fill(activeNode.texture);
            activeNode.texture.SetPixels(0, 0, activeTexture1.width, activeTexture1.height, activeTexture1.GetPixels());
            activeNode.texture.SetPixels(activeTexture1.width, 0, width, height, pixels);
            activeNode.texture.Apply();
            waitL1 = false;
            yield return null;
        }
        IEnumerator makeTexture(string text, int fontSize, int activeTexture) {
            textObject.fontSize = fontSize;
            textObject.text = text;
            renderCamera.Render();
            yield return new WaitForEndOfFrame();
            switch (activeTexture) {
                case 1: activeTexture1 = CapturePixels(); break;
                case 2: activeTexture2 = CapturePixels(); break;
                default: throw new System.Exception("Active texture index not implemented");
            }
            waitL2 = false;
            yield return null;
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