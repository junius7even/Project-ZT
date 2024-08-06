using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZetanStudio.ConditionSystem
{
    [Serializable]
    public class ConditionGroup
    {
        [SerializeField, HideIf("conditions.Length", 0)]
        [Tooltip("条件计算公式\n1、操作数为条件的下标\n2、运算符可使用 \"(\"、\")\"、\"|\"(或)、\"&\"(且)、\"!\"(非)\n" +
            "3、未对非法输入进行处理，需规范填写\n4、例：\"(0 | 1) & !2\" 表示满足条件0或1且不满足条件2\n5、为空时默认进行相互的“且”运算")]
        private string formula;
        /// <summary>
        /// 条件计算公式<br/>
        /// 1、操作数为条件的下标<br/>
        /// 2、运算符可使用 "("、")"、"|"(或)、"&amp;"(且)、"!"(非)<br/>
        /// 3、未对非法输入进行处理，需规范填写<br/>
        /// 4、例：(0 | 1) &amp; !2 表示满足条件0或1且不满足条件2<br/>
        /// 5、为空时默认进行相互的“且”运算<br/>
        /// The formula of condition calculation<br/>
        /// 1、The operands are condition's indices<br/>
        /// 2、The operator can be '('、')'、'|'(or)、'&amp;'(and)、'!'(not)<br/>
        /// 3、Will not handle invalid input, please fill in normatively<br/>
        /// 4、Example: '(0 | 1) &amp; !2' intends that we need to meet condition 0 or 1, and not meet condition 2<br/>
        /// 5、If this is empty, will calculate with 'and' operation mutually.
        /// </summary>
        public string Formula => formula;

        [SerializeReference, PolymorphismList("GetGroup", "GetName")]
        private Condition[] conditions = { };
        public ReadOnlyCollection<Condition> Conditions => new ReadOnlyCollection<Condition>(conditions);

        public bool IsValid => conditions.All(c => c != null && c.IsValid);

        public bool IsMet()
        {
            if (!IsValid) return false;
            if (string.IsNullOrEmpty(Formula)) return conditions.All(c => c.IsMet());
            if (conditions.Length > 0)
            {
                //删除所有空格才开始计算
                //Delete all empty chars before starting
                var cr = Regex.Replace(Formula, @"[ \t]", "").ToCharArray();
                //逆波兰表达式
                //Reverse Polish notation
                List<string> RPN = new List<string>();
                //数字串
                //Index string
                string indexStr = string.Empty;
                //运算符栈
                //Operator stack
                Stack<char> optStack = new Stack<char>();
                for (int i = 0; i < cr.Length; i++)
                {
                    char c = cr[i];
                    string item;
                    if (c < '0' || c > '9')
                    {
                        if (!string.IsNullOrEmpty(indexStr))
                        {
                            item = indexStr;
                            indexStr = string.Empty;
                            getRPNItem(item);
                        }
                        if (c == '(' || c == ')' || c == '|' || c == '&' || c == '!')
                        {
                            item = c + "";
                            getRPNItem(item);
                        }
                        //既不是数字也不是运算符，直接放弃计算
                        //Give up when char is neither a number nor an operator
                        else break;
                    }
                    else
                    {
                        //拼接数字
                        //Splice numbers
                        indexStr += c;
                        if (i + 1 >= cr.Length)
                        {
                            item = indexStr;
                            indexStr = string.Empty;
                            getRPNItem(item);
                        }
                    }
                }
                while (optStack.Count > 0)
                    RPN.Add(optStack.Pop() + "");
                Stack<bool> values = new Stack<bool>();
                foreach (var item in RPN)
                {
                    if (int.TryParse(item, out int index))
                    {
                        if (index >= 0 && index < conditions.Length)
                        {
                            values.Push(conditions[index].IsMet());
                        }
                        else return true;
                    }
                    else if (values.Count > 0)
                    {
                        if (item == "!") values.Push(!values.Pop());
                        else if (values.Count > 1)
                        {
                            bool right = values.Pop();
                            bool left = values.Pop();
                            if (item == "|") values.Push(left | right);
                            else if (item == "&") values.Push(left & right);
                        }
                    }
                }
                if (values.Count == 1) return values.Pop();

                void getRPNItem(string item)
                {
                    //遇到运算符
                    //Meet operator
                    if (item == "!" || item == "&" || item == "|")
                    {
                        char opt = item[0];
                        //栈空则直接入栈
                        //Push directly when the stack is empty
                        if (optStack.Count < 1) optStack.Push(opt);
                        //栈不空则出栈所有优先级大于或等于opt的运算符后才入栈opt
                        //If not, then pop all operators that priority greater than 'opt' before push 'opt'
                        else while (optStack.Count > 0)
                            {
                                char top = optStack.Peek();
                                if (top + "" == item || top == '!' || top == '&' && opt == '|')
                                {
                                    RPN.Add(optStack.Pop() + "");
                                    if (optStack.Count < 1)
                                    {
                                        optStack.Push(opt);
                                        break;
                                    }
                                }
                                else
                                {
                                    optStack.Push(opt);
                                    break;
                                }
                            }
                    }
                    else if (item == "(") optStack.Push('(');
                    else if (item == ")")
                    {
                        while (optStack.Count > 0)
                        {
                            char opt = optStack.Pop();
                            if (opt == '(') break;
                            else RPN.Add(opt + "");
                        }
                    }
                    //遇到数字
                    //Meet number
                    else if (int.TryParse(item, out _)) RPN.Add(item);
                }
            }
            foreach (Condition con in conditions)
                if (!con.IsMet()) return false;
            return true;
        }

        public static implicit operator bool(ConditionGroup obj) => obj != null;
    }
}