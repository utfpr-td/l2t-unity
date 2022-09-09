using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LatexMathExpressionRender {
    public static class DemoExpressions {
        public static List<string> simple;
        public static List<string> root;
        public static List<string> fraction;
        public static List<string> diverse;
        public static List<string> all;
        static DemoExpressions() {
            simple = new List<string> {
                @"x^y",
                @"x^{y}",
                @"{x}^y",
                @"{x}^{y}",
                @"x^y+a",
                @"x^{y}+a",
                @"{x}^y+a",
                @"{x}^{y}+a",
                @"a+x^y",
                @"a+x^{y}",
                @"a+{x}^y",
                @"a+{x}^{y}",
                @"ax^yb",
                @"ax^{y}b",
                @"a{x}^yb",
                @"a{x}^{y}b",
                @"ax^yb+a",
                @"ax^{y}b+a",
                @"a{x}^yb+a",
                @"a{x}^{y}b+a",
                @"a+ax^yb",
                @"a+ax^{y}b",
                @"a+a{x}^yb",
                @"a+a{x}^{y}b",
                @"123",
                @"123+456",
                @"123+{456}",
                @"{123}+456",
                @"{123}+{456}",
                @"123^456",
                @"123^{456}",
                @"{123}^456",
                @"{123}^{456}",
            };
            root = new List<string> {
                @"1+2+3=6",
            };
            diverse = new List<string> {
                @"5+\frac{\frac{1}{2}}{\frac{1}{2}}",
            };
            fraction = new List<string> {
                @"5+\frac{\frac{1}{2}}{\frac{1}{2}}",
            };
            diverse = new List<string> {
                @"a+\frac{x}{y}-\sqrt{z}=\sqrt{\frac{w}{g}}",
                @"a+\frac{x}{{y}^2}-\sqrt{z}=\sqrt{\frac{w}{g}}",
                @"M+\frac{M}{M}-\sqrt{M}=\sqrt{\frac{M}{M}}",
                @"a+b^{c}+\frac{x}{y}-\sqrt{z}=\sqrt{\frac{w+u^{j}}{g}}",
                @"2x+1-\frac{x}{2}=\sqrt{25}+x^2",
                @"2x+1=7",
                @"y+7=0",
                @"-7",
                @"4",
                @"-1^2",
                @"2^3",
                @"\sqrt{3^2+4^2}",
                @"\sqrt{49}",
                @"-3",
                @"-2",
                @"-\frac{x}{2}=1",
                @"3y-5=7",
                @"-1",
                @"11",
                @"5x+\frac{3x}{7}-30=8",
                @"\frac{y}2+3y+1,5=-30",
                @"-9",
                @"2",
                @"-7",
                @"\sqrt{36}",
                @"x+2=-4",
                @"y+5=-5",
                @"-\sqrt{25}",
                @"-\sqrt{16}",
                @"1^6",
                @"2^2",
                @"-8",
                @"1",
                @"2x+7=1",
                @"2y+5=9",
                @"\sqrt{0}",
                @"\sqrt{36}",
                @"\sqrt{4}",
                @"\sqrt{81}",
                @"-\sqrt[3]{27}",
                @"2^3-1",
                @"\sqrt{16}",
                @"\sqrt{144}",
                @"3^2-5",
                @"2^2+1",
                @"-\sqrt{4}",
                @"2^2",
                @"-9",
                @"10",
            };
            all = new List<string>();
            all.AddRange(simple);
            all.AddRange(root);
            all.AddRange(fraction);
            all.AddRange(diverse);
        }
    }
}
