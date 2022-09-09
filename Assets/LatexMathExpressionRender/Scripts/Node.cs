using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LatexMathExpressionRender {
    public class Node {
        public int fontSize;
        public int bottomMargin;
        public bool implicitOp;
        public string op;
        public string sign;
        public string expression;
        public Texture2D texture;
        public List<Node> nodes;
        public bool hasFrac {
            get {
                if (nodes == null || nodes.Count == 0) return op == @"\frac";
                if (op == @"\frac") return true;
                for (int i = 0; i < nodes.Count; i++) if (nodes[i].hasFrac) return true;
                return false;
            }
        }
        public override string ToString() {
            if (nodes != null && nodes.Count > 0) {
                if (nodes.Count == 2) {
                    return string.Format("L: ({0}); Op: {1}; R: ({2});", nodes[0].ToString(), op, nodes[1].ToString());
                }
                else if (nodes.Count == 1) {
                    return string.Format("Val: ({0}); Op: {1}", nodes[0].ToString(), op);
                }
                return base.ToString();
            }
            return string.Format("Val: {0}", op);
        }
    }
}
