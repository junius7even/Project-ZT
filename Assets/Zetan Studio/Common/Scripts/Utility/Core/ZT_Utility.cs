using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ZetanStudio
{
    public static partial class UtilityZT
    {
        #region 杂项
        public static Scene GetActiveScene() => SceneManager.GetActiveScene();

        public static T LoadResource<T>() where T : Object
        {
            try
            {
                var asset = Resources.LoadAll<T>("")[0];
                Resources.UnloadUnusedAssets();
                return asset;
            }
            catch
            {
                return null;
            }
        }
        public static Object LoadResource(Type type)
        {
            try
            {
                var asset = Resources.LoadAll("", type)[0];
                Resources.UnloadUnusedAssets();
                return asset;
            }
            catch
            {
                return null;
            }
        }
        public static int CompareStringNumbericSuffix(string x, string y)
        {
            var m1 = Regex.Match(x, @"(\w+) *(\d+)");
            var m2 = Regex.Match(y, @"(\w+) *(\d+)");
            if (m1.Success && m2.Success)
            {
                var pre1 = m1.Groups[1].Value;
                var pre2 = m2.Groups[1].Value;
                if (pre1 != pre2) return string.Compare(x, y);
                var num1 = int.Parse(m1.Groups[2].Value);
                var num2 = int.Parse(m2.Groups[2].Value);
                if (num1 < num2) return -1;
                else if (num1 > num2) return 1;
                else return 0;
            }
            else return string.Compare(x, y);
        }

        public static void Stopwatch(Action action)
        {
            if (action == null) throw new ArgumentNullException();
            System.Diagnostics.Stopwatch sw = new();
            sw.Start(); action?.Invoke(); sw.Stop();
            Debug.Log(sw.ElapsedMilliseconds + " ms");
        }
        public static T Stopwatch<T>(Func<T> action)
        {
            if (action == null) throw new ArgumentNullException();
            System.Diagnostics.Stopwatch sw = new();
            sw.Start(); var @return = action.Invoke(); sw.Stop();
            Debug.Log(sw.ElapsedMilliseconds + " ms");
            return @return;
        }

        public static IList<T> RandomOrder<T>(IList<T> list)
        {
            var result = Activator.CreateInstance(list.GetType()) as IList<T>;
            var indices = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                indices.Add(i);
            }
            while (result.Count < list.Count)
            {
                var index = Random.Range(0, indices.Count);
                if (index < 0 || index >= indices.Count) break;
                result.Add(list[indices[index]]);
                indices.RemoveAt(index);
            }
            return result;
        }
        public static T[] RandomOrder<T>(T[] array)
        {
            var result = new T[array.Length];
            var indices = new List<int>();
            for (int i = 0; i < array.Length; i++)
            {
                indices.Add(i);
            }
            int take = 0;
            while (take < array.Length)
            {
                var index = Random.Range(0, indices.Count);
                if (index < 0 || index >= indices.Count) break;
                result[take] = array[indices[index]];
                indices.RemoveAt(index);
                take++;
            }
            return result;
        }

        /// <summary>
        /// 将所给下标处的数据移动到指定下标，若数据位于目标位置下方，则插入插入其下方，而非目标位置<br/>
        /// Move all elements indexed by <i><paramref name="indices"/></i> to <i><paramref name="insertAtIndex"/></i>, if the data is below <i><paramref name="insertAtIndex"/></i>, then insert the data below it instead of itself.
        /// </summary>
        /// <returns>是否发生了移动<br/>
        /// Have any moving happened.
        /// </returns>
        public static bool MoveElements<T>(IList<T> data, int[] indices, int insertAtIndex, out int[] newIndices)
        {
            newIndices = null;
            if (data is not null)
            {
                while (insertAtIndex < 0)
                {
                    insertAtIndex++;
                }
                while (insertAtIndex > data.Count + 1)
                {
                    insertAtIndex--;
                }
                var position = new Dictionary<int, int>();
                for (int i = 0; i < indices.Length; i++)
                {
                    position[indices[i]] = i;
                }
                Array.Sort(indices);
                var cache = new T[indices.Length];
                var upperIndices = new HashSet<int>();
                var belowIndices = new HashSet<int>();
                for (int i = 0; i < indices.Length; i++)
                {
                    int index = indices[i];
                    cache[i] = data[index];
                    if (index < insertAtIndex) upperIndices.Add(index);
                    else belowIndices.Add(index);
                }

                var remainder = new List<T>(insertAtIndex - upperIndices.Count);
                int start, junction;
                bool hasMoved = false;
                if (upperIndices.Count > 0)
                {
                    newIndices ??= new int[indices.Length];

                    for (int i = indices[0]; i < insertAtIndex; i++)
                    {
                        if (!upperIndices.Contains(i))
                        {
                            remainder.Add(data[i]);
                        }
                    }
                    hasMoved = remainder.Count > 0;
                    start = indices[0];
                    junction = start + remainder.Count;
                    for (int i = start; i < insertAtIndex; i++)
                    {
                        if (i < junction) data[i] = remainder[i - start];//放置被上移填充的项
                        else
                        {
                            data[i] = cache[i - junction];//放置被插入的项
                            newIndices[position[indices[i - junction]]] = i;
                        }
                    }
                }
                if (belowIndices.Count > 0)
                {
                    newIndices ??= new int[indices.Length];

                    remainder = new List<T>(indices[^1] + 1 - insertAtIndex - belowIndices.Count);
                    for (int i = insertAtIndex; i < indices[^1]; i++)
                    {
                        if (!belowIndices.Contains(i))
                        {
                            remainder.Add(data[i]);
                        }
                    }
                    hasMoved |= remainder.Count > 0;
                    start = insertAtIndex;
                    junction = start + belowIndices.Count;
                    for (int i = start; i <= indices[^1]; i++)
                    {
                        if (i < junction)
                        {
                            data[i] = cache[upperIndices.Count + (i - start)];//放置被插入的项
                            newIndices[position[indices[upperIndices.Count + (i - start)]]] = i;
                        }
                        else data[i] = remainder[i - junction];//放置被下移填充的项
                    }
                }

                return hasMoved;
            }
            return false;
        }

        public static string ConvertToAssetsPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            path = path.Replace("\\", "/");
            return path.Replace(Application.dataPath, "Assets");
        }
        #endregion

        #region 文本
        public static string ColorText(string text, Color color)
        {
            if (!color.Equals(Color.clear)) return string.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGB(color), text);
            else return text;
        }
        public static string ColorText(object content, Color color)
        {
            if (!color.Equals(Color.clear)) return string.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGB(color), content?.ToString() ?? "Null");
            else return content?.ToString() ?? "Null";
        }
        public static string BoldText(string text) => $"<b>{text}</b>";
        public static string ItalicText(string text) => $"<i>{text}</i>";
        public static string RemoveTags(string text)
        {
            return Regex.Replace(text,
                @"<color=?>|<color=[a-z]+>|<color=""[a-z]+"">|<color='[a-z]+'>|<color=#[a-f\d]{6}>|<color=#[a-f\d]{8}>|<\/color>|<size>|<size=\d{0,3}>|<\/size>|<b>|<\/b>|<i>|<\/i>",
                "", RegexOptions.IgnoreCase);
        }
        #endregion

        #region 游戏对象
        public static void SetActive(GameObject gameObject, bool value)
        {
            if (!gameObject) return;
            if (gameObject.activeSelf != value) gameObject.SetActive(value);
        }
        public static void SetActive(Component component, bool value)
        {
            if (!component) return;
            SetActive(component.gameObject, value);
        }
        #endregion

        #region 反射相关
        public static bool TryGetValue(string path, object target, out object value, out MemberInfo memberInfo)
        {
            value = default;
            memberInfo = null;
            string[] fields = path.Split('.');
            object mv = target;
            if (mv == null) return false;
            var mType = mv.GetType();
            for (int i = 0; i < fields.Length; i++)
            {
                memberInfo = mType?.GetField(fields[i], CommonBindingFlags);
                if (memberInfo is FieldInfo field)
                {
                    mv = field.GetValue(mv);
                    mType = mv?.GetType();
                }
                else
                {
                    memberInfo = mType?.GetProperty(fields[i], CommonBindingFlags);
                    if (memberInfo is PropertyInfo property)
                    {
                        mv = property.GetValue(mv);
                        mType = mv?.GetType();
                    }
                    else return false;
                }
            }
            if (memberInfo != null)
            {
                value = mv;
                return true;
            }
            else return false;
        }

        public const BindingFlags CommonBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        #endregion

        #region Debug.Log相关
        public static T Log<T>(T message)
        {
            LogInternal(LogType.Log, message);
            return message;
        }
        public static void Log(object message, params object[] messages)
        {
            var msg = new object[messages.Length + 1];
            msg[0] = message;
            for (int i = 0; i < messages.Length; i++)
            {
                msg[i + 1] = messages[i];
            }
            LogInternal(LogType.Log, msg);
        }

        public static T LogWarning<T>(T message)
        {
            LogInternal(LogType.Warning, message);
            return message;
        }
        public static void LogWarning(object message, params object[] messages)
        {
            var msg = new object[messages.Length + 1];
            msg[0] = message;
            for (int i = 0; i < messages.Length; i++)
            {
                msg[i + 1] = messages[i];
            }
            LogInternal(LogType.Warning, msg);
        }

        public static T LogError<T>(T message)
        {
            LogInternal(LogType.Error, message);
            return message;
        }
        public static void LogError(object message, params object[] messages)
        {
            var msg = new object[messages.Length + 1];
            msg[0] = message;
            for (int i = 0; i < messages.Length; i++)
            {
                msg[i + 1] = messages[i];
            }
            LogInternal(LogType.Error, msg);
        }

        private static void LogInternal(LogType logType, params object[] messages)
        {
            StringBuilder sb = new StringBuilder();
            if (messages != null)
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    sb.Append(messages[i] is null ? "Null" : messages[i]);
                    if (i != messages.Length - 1) sb.Append(", ");
                }
            }
            else sb.Append("Null");
            LogInternal(logType, sb.ToString(), new System.Diagnostics.StackTrace(2, true));
        }
        private static void LogInternal(LogType logType, object message, System.Diagnostics.StackTrace trace)
        {
            StringBuilder sb = new StringBuilder(message is null ? "Null" : message.ToString());
            sb.Append('\n');
            switch (logType)
            {
                case LogType.Error:
                    sb.Append("UnityEngine.Debug:LogError (object)\n");
                    break;
                case LogType.Warning:
                    sb.Append("UnityEngine.Debug:LogWarning (object)\n");
                    break;
                case LogType.Log:
                    sb.Append("UnityEngine.Debug:Log (object)\n");
                    break;
                default:
                    break;
            }
            foreach (var frame in trace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                sb.Append(method.ReflectedType);
                sb.Append(':');
                sb.Append(method.Name);
                sb.Append(" (");
                var pars = method.GetParameters();
                for (int j = 0; j < pars.Length; j++)
                {
                    sb.Append(pars[j].ParameterType);
                    if (j != pars.Length - 1) sb.Append(',');
                }
                sb.Append(")");
                var file = ConvertToAssetsPath(frame.GetFileName());
                var line = frame.GetFileLineNumber();
                if (!string.IsNullOrEmpty(file))
                {
                    sb.Append(" (at <a href=\"");
                    sb.Append(file);
                    sb.Append("\" line=\":");
                    sb.Append(line);
                    sb.Append("\">");
                    sb.Append(file);
                    sb.Append(":");
                    sb.Append(line);
                    sb.Append("</a>)");
                }
                sb.Append('\n');
            }
            var oldTrace = Application.GetStackTraceLogType(logType);
            Application.SetStackTraceLogType(logType, StackTraceLogType.None);
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(sb);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(sb);
                    break;
                case LogType.Log:
                    Debug.Log(sb);
                    break;
                default:
                    break;
            }
            Application.SetStackTraceLogType(logType, oldTrace);
        }
        #endregion

        #region Vector相关
        public static Vector2 ScreenCenter => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        public static Vector3 MousePositionToWorld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Pointer.current.position.ReadValue());
#else
                return Camera.main.ScreenToWorldPoint(Input.mousePosition);
#endif
            }
        }

        public static Vector3 PositionToGrid(Vector3 originalPos, float gridSize = 1.0f, float offset = 1.0f)
        {
            Vector3 newPos = originalPos;
            newPos -= Vector3.one * offset;
            newPos /= gridSize;
            newPos = new Vector3(Mathf.Round(newPos.x), Mathf.Round(newPos.y), Mathf.Round(newPos.z));
            newPos *= gridSize;
            newPos += Vector3.one * offset;
            return newPos;
        }
        public static Vector2 PositionToGrid(Vector2 originalPos, float gridSize = 1.0f, float offset = 1.0f)
        {
            Vector2 newPos = originalPos;
            newPos -= Vector2.one * offset;
            newPos /= gridSize;
            newPos = new Vector2(Mathf.Round(newPos.x), Mathf.Round(newPos.y));
            newPos *= gridSize;
            newPos += Vector2.one * offset;
            return newPos;
        }

        public static float Slope(Vector3 from, Vector3 to)
        {
            float height = Mathf.Abs(from.y - to.y);//高程差
            float length = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));//水平差
            return Mathf.Atan(height / length) * Mathf.Rad2Deg;
        }

        public static bool Vector3LessThan(Vector3 v1, Vector3 v2)
        {
            return v1.x < v2.x && v1.y <= v2.y && v1.z <= v2.z || v1.x <= v2.x && v1.y < v2.y && v1.z <= v2.z || v1.x <= v2.x && v1.y <= v2.y && v1.z < v2.z;
        }

        public static bool Vector3LargeThan(Vector3 v1, Vector3 v2)
        {
            return v1.x > v2.x && v1.y >= v2.y && v1.z >= v2.z || v1.x >= v2.x && v1.y > v2.y && v1.z >= v2.z || v1.x >= v2.x && v1.y >= v2.y && v1.z > v2.z;
        }

        public static Vector3 CenterBetween(Vector3 point1, Vector3 point2)
        {
            float x = point1.x - (point1.x - point2.x) * 0.5f;
            float y = point1.y - (point1.y - point2.y) * 0.5f;
            float z = point1.z - (point1.z - point2.z) * 0.5f;
            return new Vector3(x, y, z);
        }
        public static Vector2 CenterBetween(Vector2 point1, Vector2 point2)
        {
            float x = point1.x - (point1.x - point2.x) * 0.5f;
            float y = point1.y - (point1.y - point2.y) * 0.5f;
            return new Vector2(x, y);
        }

        public static Vector3 SizeBetween(Vector3 point1, Vector3 point2)
        {
            return new Vector3(Mathf.Abs(point1.x - point2.x), Mathf.Abs(point1.y - point2.y), Mathf.Abs(point1.z - point2.z));
        }

        public static Vector2 GetVectorFromAngle(float angle)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        }
        #endregion

        #region 文件和安全
        public static FileStream OpenFile(string path, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite)
        {
            try
            {
                return new FileStream(path, fileMode, fileAccess);
            }
            catch
            {
                return null;
            }
        }

        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }
        public static string GetFileDirectory(string path)
        {
            try
            {
                return Path.GetDirectoryName(path);
            }
            catch
            {
                return string.Empty;
            }
        }
        public static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }
        public static string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// 加密字符串，多用于JSON
        /// </summary>
        /// <param name="unencryptText">待加密明文</param>
        /// <param name="key">密钥</param>
        /// <returns>密文</returns>
        public static string Encrypt(string unencryptText, string key)
        {
            if (key.Length != 32 && key.Length != 16) return unencryptText;
            //密钥
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            //待加密明文数组
            byte[] unencryptBytes = Encoding.UTF8.GetBytes(unencryptText);

            //Rijndael加密算法
            RijndaelManaged rDel = new RijndaelManaged
            {
                Key = keyBytes,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            ICryptoTransform cTransform = rDel.CreateEncryptor();

            //返回加密后的密文
            byte[] resultBytes = cTransform.TransformFinalBlock(unencryptBytes, 0, unencryptBytes.Length);
            return Convert.ToBase64String(resultBytes, 0, resultBytes.Length);
        }
        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="encrytedText">待解密密文</param>
        /// <param name="key">密钥</param>
        /// <returns>明文</returns>
        public static string Decrypt(string encrytedText, string key)
        {
            if (key.Length != 32 && key.Length != 16) return encrytedText;
            //解密密钥
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            //待解密密文数组
            byte[] encryptBytes = Convert.FromBase64String(encrytedText);

            //Rijndael解密算法
            RijndaelManaged rDel = new RijndaelManaged
            {
                Key = keyBytes,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            ICryptoTransform cTransform = rDel.CreateDecryptor();

            //返回解密后的明文
            byte[] resultBytes = cTransform.TransformFinalBlock(encryptBytes, 0, encryptBytes.Length);
            return Encoding.UTF8.GetString(resultBytes);
        }

        public static MemoryStream Encrypt(Stream unencryptStream, string key)
        {
            if (key.Length != 32 && key.Length != 16) return null;
            if (unencryptStream == null) return null;
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            //加密过程
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, new RijndaelManaged
            {
                Key = keyBytes,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            }.CreateEncryptor(), CryptoStreamMode.Write);
            byte[] buffer = new byte[1024];
            unencryptStream.Position = 0;
            int bytesRead;
            do
            {
                bytesRead = unencryptStream.Read(buffer, 0, 1024);
                cs.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);
            cs.FlushFinalBlock();

            byte[] resultBytes = ms.ToArray();
            unencryptStream.SetLength(0);
            unencryptStream.Write(resultBytes, 0, resultBytes.Length);
            return ms;
        }
        public static MemoryStream Decrypt(Stream encryptedStream, string key)
        {
            if (key.Length != 32 && key.Length != 16) return null;
            if (encryptedStream == null) return null;
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            //解密过程
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(encryptedStream, new RijndaelManaged
            {
                Key = keyBytes,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            }.CreateDecryptor(), CryptoStreamMode.Read);
            byte[] buffer = new byte[1024];
            int bytesRead;
            do
            {
                bytesRead = cs.Read(buffer, 0, 1024);
                ms.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);

            MemoryStream result = new MemoryStream(ms.GetBuffer());
            return result;
        }

        public static string GetMD5(string fileName)
        {
            try
            {
                using FileStream file = new FileStream(fileName, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] bytes = md5.ComputeHash(file);
                file.Close();
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        public static bool CompareMD5(string fileName, string md5hashToCompare)
        {
            try
            {
                using FileStream file = new FileStream(fileName, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] bytes = md5.ComputeHash(file);
                file.Close();
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString() == md5hashToCompare;
            }
            catch
            {
                return false;
            }
        }

        public static string GetMD5(FileStream file)
        {
            try
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] bytes = md5.ComputeHash(file);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        public static bool CompareMD5(FileStream file, string md5hashToCompare)
        {
            try
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] bytes = md5.ComputeHash(file);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString() == md5hashToCompare;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }

    public class EqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> equals;
        private readonly Func<T, int> hashCode;

        public EqualityComparer(Func<T, T, bool> equals, Func<T, int> hashCode)
        {
            this.equals = equals ?? throw new ArgumentNullException(nameof(equals));
            this.hashCode = hashCode ?? throw new ArgumentNullException(nameof(hashCode));
        }

        public bool Equals(T x, T y)
        {
            return equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return hashCode(obj);
        }
    }
    public class Comparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> comparison;

        public Comparer(Func<T, T, int> comparison)
        {
            this.comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
        }

        public int Compare(T x, T y)
        {
            return comparison(x, y);
        }
    }
    public class NumbericSuffixStringComparer : IComparer<string>
    {
        public int Compare(string x, string y) => UtilityZT.CompareStringNumbericSuffix(x, y);
    }

    public enum UpdateMode
    {
        Update,
        LateUpdate,
        FixedUpdate
    }
}