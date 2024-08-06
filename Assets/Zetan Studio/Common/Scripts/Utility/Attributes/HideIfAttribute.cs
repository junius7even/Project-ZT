using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZetanStudio
{
    public interface ICheckValueAttribute
    {
        public string[] Paths { get; }
        public object[] Values { get; }
        public bool And { get; }
        public string Formula { get; }
        public Type DeclarationType { get; }

        public bool Check(object declarant)
        {
            if (Paths.Length < 1) return false;
            if (DeclarationType != null) return check0();
            else if (string.IsNullOrEmpty(Formula)) return check1();
            else return check2();

            bool check(string p, object v)
            {
                if (p == "typeof(this)") return (v as Type).IsAssignableFrom(declarant.GetType());
                bool type = p.StartsWith("typeof(");
                if (type) p = p.Replace("typeof(", "").Replace(")", "");
                if (UtilityZT.TryGetValue(p, declarant, out var value, out _))
                {
                    if (type) return value != null && value.GetType().IsAssignableFrom(v as Type);
                    else if (Equals(value, v) || value == null && v == null) return true;
                    else return false;
                }
                else
                {
                    Debug.LogWarning($"找不到路径：{declarant.GetType().Name}.{p}");
                    return false;
                }
            }

            bool check0()
            {
                return declarant.GetType().Equals(DeclarationType);
            }

            bool check1()
            {
                bool hide = check(Paths[0], Values[0]);
                for (int i = 1; i < Paths.Length; i++)
                {
                    if (And) hide &= check(Paths[i], Values[i]);
                    else hide |= check(Paths[i], Values[i]);
                }
                return hide;
            }

            bool check2()
            {
                bool calFailed = false;
                if (Paths.Length < 1) calFailed = true;
                else
                {
                    var cr = Formula.Replace(" ", "").ToCharArray();
                    List<string> RPN = new List<string>();
                    string indexStr = string.Empty;
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
                                GetRPNItem(item);
                            }
                            if (c == '(' || c == ')' || c == '|' || c == '&' || c == '!')
                            {
                                item = c + "";
                                GetRPNItem(item);
                            }
                            else
                            {
                                calFailed = true;
                                break;
                            }
                        }
                        else
                        {
                            indexStr += c;//拼接数字
                            if (i + 1 >= cr.Length)
                            {
                                item = indexStr;
                                indexStr = string.Empty;
                                GetRPNItem(item);
                            }
                        }
                    }
                    while (optStack.Count > 0)
                        RPN.Add(optStack.Pop() + "");
                    Stack<bool> values = new Stack<bool>();
                    foreach (var item in RPN)
                    {
                        //Debug.Log(item);
                        if (int.TryParse(item, out int index))
                        {
                            if (index >= 0 && index < Paths.Length)
                                values.Push(check(Paths[index], Values[index]));
                            else
                            {
                                //Debug.Log("return 1");
                                return true;
                            }
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
                    if (values.Count == 1)
                    {
                        //Debug.Log("return 2");
                        return values.Pop();
                    }

                    void GetRPNItem(string item)
                    {
                        //Debug.Log(item);
                        if (item == "|" || item == "&" || item == "!")
                        {
                            char opt = item[0];
                            if (optStack.Count < 1) optStack.Push(opt);
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
                        else if (int.TryParse(item, out _)) RPN.Add(item);
                    }
                }
                if (!calFailed)
                {
                    //Debug.Log("return 3");
                    return true;
                }
                else
                {
                    //Debug.Log("return 4");
                    return check1();
                }
            }
        }
    }

    public class HideIfAttribute : EnhancedPropertyAttribute, ICheckValueAttribute
    {
        public string[] Paths { get; }
        public object[] Values { get; }
        public bool And { get; }
        public string Formula { get; }
        public Type DeclarationType { get; }

        public readonly bool readOnly;

        public HideIfAttribute(Type declarationType, bool readOnly = false)
        {
            DeclarationType = declarationType;
            this.readOnly = readOnly;
        }
        /// <param name="path">路径</param>
        /// <param name="value">值</param>
        /// <param name="readOnly">只否只读而不是隐藏</param>
        public HideIfAttribute(string path, object value, bool readOnly = false)
        {
            Paths = new string[] { path };
            Values = new object[] { value };
            this.readOnly = readOnly;
        }
        /// <summary>
        /// 两个数组长度必须一致
        /// </summary>
        /// <param name="paths">路径列表</param>
        /// <param name="values">值列表</param>
        /// <param name="and">是否进行与操作</param>
        /// <param name="readOnly">只否只读而不是隐藏</param>
        public HideIfAttribute(string[] paths, object[] values, bool and = true, bool readOnly = false)
        {
            Paths = paths;
            Values = values;
            And = and;
            this.readOnly = readOnly;
        }
        /// <summary>
        /// 参数格式：(是否进行与操作，路径1，值1，路径2，值2，……，路径n，值n)
        /// </summary>
        /// <param name="and">是否进行与操作</param>
        /// <param name="readOnly">只否只读而不是隐藏</param>
        /// <param name="pathValuePair">径值对</param>
        public HideIfAttribute(bool and, bool readOnly, object path, object value)
        {
            And = and;
            this.readOnly = readOnly;
            Paths = new string[] { path as string };
            Values = new object[] { value };
        }
        /// <summary>
        /// 参数格式：(是否进行与操作，路径1，值1，路径2，值2，……，路径n，值n)
        /// </summary>
        /// <param name="and">是否进行与操作</param>
        /// <param name="readOnly">只否只读而不是隐藏</param>
        /// <param name="pathValuePair">径值对</param>
        public HideIfAttribute(bool and, bool readOnly, params object[] pathValuePair)
        {
            And = and;
            this.readOnly = readOnly;
            Paths = new string[pathValuePair.Length / 2];
            Values = new object[pathValuePair.Length / 2];
            for (int i = 0; i < pathValuePair.Length; i += 2)
            {
                Paths[i / 2] = pathValuePair[i] as string;
                Values[i / 2] = pathValuePair[i + 1];
            }
        }
        /// <summary>
        /// 带关系表达式
        /// </summary>
        /// <param name="paths">路径列表</param>
        /// <param name="values">值列表</param>
        /// <param name="relational">关系表达式，支持括号、与(&amp;)、或(|)、非(!)</param>
        /// <param name="readOnly">只否只读而不是隐藏</param>
        public HideIfAttribute(string[] paths, object[] values, string relational, bool readOnly = false)
        {
            Paths = paths;
            Values = values;
            Formula = relational;
            this.readOnly = readOnly;
        }
    }
}