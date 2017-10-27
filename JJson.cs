using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace JFramework
{
    /// <summary>
    /// Version 1.0.0
    /// </summary>
    public static class JJson
    {
        #region Define
        private const BindingFlags  DEFAULT_BINDING_FLAGS   = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string        NULL_STR                = "null";
        private const string        JSON_TAB                = "  ";

        public enum ObjectType      { Value, Array, Class }
        public enum EnumConvertType { Number, String }

        public interface ISerializationCallbackReceiver { void OnBeforeSerialize(); void OnAfterDeserialize(); }

        public abstract class IJson
        {
            public abstract ObjectType type { get; }

            public abstract IJson this[int idx]     { get; set; }
            public abstract IJson this[string key]  { get; set; }
        }
        public abstract class JsonCollection<TCollection, TValue> : IJson, ICollection<TValue> where TCollection : ICollection<TValue>, new()
        {
            #region Field, Property
            private		TCollection _collection = new TCollection();
			protected	TCollection collection	{ get { return this._collection; } }
            #endregion

            #region ICollection
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            public virtual int                  Count                                   { get { return this.collection.Count; } }
            public virtual bool                 IsReadOnly                              { get { return this.collection.IsReadOnly; } }
            public virtual void                 Add(TValue item)                        { this.collection.Add(item); }
            public virtual void                 Clear()                                 { this.collection.Clear(); }
            public virtual bool                 Contains(TValue item)                   { return this.collection.Contains(item); }
            public virtual void                 CopyTo(TValue[] array, int arrayIndex)  { this.collection.CopyTo(array, arrayIndex); }
            public virtual IEnumerator<TValue>  GetEnumerator()                         { return this.collection.GetEnumerator(); }
            public virtual bool                 Remove(TValue item)                     { return this.collection.Remove(item); }
            #endregion
        }
        public sealed   class JsonValue : IJson
        {
            #region Constructor
            public JsonValue(string val, bool isQuotesRemoved)      { this.val = val; this.isQuotesRemoved = isQuotesRemoved; }
            public JsonValue(string val, Type valType, bool isNull) { this.val = val; this.valType = valType; this.isQuotesRemoved = !isNull; }
            #endregion

            #region Field, Property
            public bool     isQuotesRemoved { get; private set; }
            public string   val             { get; private set; }
            public Type     valType         { get; private set; }
            public bool     isNullVal       { get { return !this.isQuotesRemoved && this.val == NULL_STR; } }
            #endregion

            #region IJson
            public override ObjectType type			{ get { return ObjectType.Value; } }
            public override IJson this[int idx]     { get { throw new JJsonWrongIndexerUseException(this); } set { throw new JJsonWrongIndexerUseException(this); } }
            public override IJson this[string key]  { get { throw new JJsonWrongIndexerUseException(this); } set { throw new JJsonWrongIndexerUseException(this); } }
            #endregion
        }
        public sealed   class JsonArray : JsonCollection<List<IJson>, IJson>, IList<IJson>
        {
            #region IJson
            public override ObjectType type			{ get { return ObjectType.Array; } }
            public override IJson this[string key]  { get { throw new JJsonWrongIndexerUseException(this); } set { throw new JJsonWrongIndexerUseException(this); } }
            #endregion

            #region IList
            public override IJson    this[int index]				{ get { return this.collection[index]; } set { this.collection[index] = value; } }
            public			int      IndexOf(IJson item)			{ return this.collection.IndexOf(item); }
            public			void     Insert(int index, IJson item)	{ this.collection.Insert(index, item); }
            public			void     RemoveAt(int index)			{ this.collection.RemoveAt(index); }
            #endregion
        }
        public sealed   class JsonClass : JsonCollection<Dictionary<string, IJson>, KeyValuePair<string, IJson>>, IDictionary<string, IJson>
        {
            #region IJson
            public override ObjectType type		{ get { return ObjectType.Class; } }
            public override IJson this[int idx] { get { throw new JJsonWrongIndexerUseException(this); } set { throw new JJsonWrongIndexerUseException(this); } }
            #endregion

            #region IDictionary
            public override IJson				this[string key]						{ get { return this.collection[key]; } set { this.collection[key] = value; } }
            public			ICollection<string>	Keys									{ get { return this.collection.Keys; } }
            public			ICollection<IJson>	Values									{ get { return this.collection.Values; } }
            public			void				Add(string key, IJson value)			{ this.collection.Add(key, value); }
            public			bool				ContainsKey(string key)					{ return this.collection.ContainsKey(key); }
            public			bool				Remove(string key)						{ return this.collection.Remove(key); }
            public			bool				TryGetValue(string key, out IJson value){ return this.collection.TryGetValue(key, out value); }
            #endregion
        }
        #endregion

        #region Attribute
        [AttributeUsage(AttributeTargets.Field)]
        public sealed class SerializeFieldAttribute : Attribute { }
        #endregion

        #region Exception
        public  abstract    class JJsonException : System.Exception
        {
            public          string msg      = null;
            public override string Message  { get { return this.msg; } }
        }
        private sealed      class JJsonFormatException : JJsonException
        {
            public JJsonFormatException(Type type, string val) { this.msg = string.Format("Type - {0}, Value - {1}", type, val); }
        }
        private sealed      class JJsonWrongIndexerUseException : JJsonException
        {
            public JJsonWrongIndexerUseException(IJson iJson) { this.msg = iJson.GetType().ToString(); }
        }
        private sealed      class JJsonFormatBracketException : JJsonException
        {
            public const string TOO_MANY_BRACKET    = "Too many bracket.";
            public const string UNMATCHED_BRACKET   = "Unmatched bracket.";
            public enum ExceptionType
            {
                ManyBracket,
                UnmatchedBracket,
            }

            public JJsonFormatBracketException(ExceptionType type, char    wrongFormat) : this(type, wrongFormat.ToString()) { }
            public JJsonFormatBracketException(ExceptionType type, string  wrongFormat)
            {
                string str = null;
                IJson j = new JsonValue("", true);
                j[0] = j;
                switch(type)
                {
                    case ExceptionType.ManyBracket:     str = TOO_MANY_BRACKET; break;
                    case ExceptionType.UnmatchedBracket:str = UNMATCHED_BRACKET;break;
                }

                this.msg = string.Format("{0} - \"{1}\"", str, wrongFormat);
            }
        }
        private sealed      class JJsonFormatKeyException : JJsonException
        {
            public JJsonFormatKeyException()            { this.msg = "Class must have key."; }
            public JJsonFormatKeyException(string key)  { this.msg = string.Format("Array element hasn't any key. : Unnecessary Key - {0}", key); }
        }
        private sealed      class JJsonUnsupportedTypeException : JJsonException
        {
            public JJsonUnsupportedTypeException(Type type) { this.msg = string.Format("This type unsupport parsing from string. : {0}", type); }
        }
        private sealed      class JJsonDeserializeException : JJsonException
        {
            public JJsonDeserializeException(Type type, string val) { this.msg = string.Format("Type - {0}, Value - {1}", type, val); }
        }
        private sealed      class JJsonCreateException : JJsonException
        {
            public JJsonCreateException(object obj, Type type) { this.msg = string.Format("Object - {0}, Type - {1}", obj, type); }
        }
        #endregion

        #region From Json
        public static class ParseMachine
        {
            #region Field, Property
            private static StringBuilder   token       = new StringBuilder();
            private static StringBuilder   key         = new StringBuilder();
            private static Stack<IJson>    jsonStack   = new Stack<IJson>();
            private static Stack<char>     signStack   = new Stack<char>();
            private static bool            isTokenEmpty{ get { return token.Length <= 0; } }
            private static IJson           topNode     { get { return jsonStack.Count <= 0 ? null : jsonStack.Peek(); } }
            #endregion

            #region Interface
            public static JsonClass Parse(string jsonStr)
            {
                Reset();
                bool isQuotesOpen   = false;
                bool isRemovedQuotes= false;
                IJson baseNode      = null;

                for (int i = 0 ; i < jsonStr.Length ; ++i)
                {
                    switch (jsonStr[i])
                    {
                        case '\r': case '\n':
                            break;

                        case ' ': case '\t':
                            if (isQuotesOpen)
                                token.Append(jsonStr[i]);

                            break;

                        case '[':
                            if (isQuotesOpen)
                            {
                                token.Append(jsonStr[i]);
                                break;
                            }

                            JsonArray jsonArr = new JsonArray();
                            AddNode(jsonArr);
                            jsonStack.Push(jsonArr);
                            signStack.Push(jsonStr[i]);
                            break;

                        case '{':
                            if (isQuotesOpen)
                            {
                                token.Append(jsonStr[i]);
                                break;
                            }

                            JsonClass jsonCls = new JsonClass();
                            AddNode(jsonCls);
                            jsonStack.Push(jsonCls);
                            signStack.Push(jsonStr[i]);
                            break;

                        case ']':
                            if (isQuotesOpen)
                            {
                                token.Append(jsonStr[i]);
                                break;
                            }

                            CheckCloseBracketException(jsonStr[i], '[');

                            if (!isTokenEmpty)
                            {
                                isRemovedQuotes = RemoveTokenQuotes();
                                AddNode(new JsonValue(token.ToString(), isRemovedQuotes));
                            }

                            baseNode = jsonStack.Pop();
                            token.Length = 0;
                            break;

                        case '}':
                            if (isQuotesOpen)
                            {
                                token.Append(jsonStr[i]);
                                break;
                            }

                            CheckCloseBracketException(jsonStr[i], '{');

                            if (!isTokenEmpty)
                            {
                                isRemovedQuotes = RemoveTokenQuotes();
                                AddNode(new JsonValue(token.ToString(), isRemovedQuotes));
                            }

                            baseNode = jsonStack.Pop();
                            token.Length = 0;
                            break;

                        case ',':
                            if (isQuotesOpen)
                            {
                                token.Append(jsonStr[i]);
                                break;
                            }

                            if (isTokenEmpty)
                                break;

                            isRemovedQuotes = RemoveTokenQuotes();
                            AddNode(new JsonValue(token.ToString(), isRemovedQuotes));
                            token.Length = 0;
                            break;

                        case ':':
                            if (isQuotesOpen)
                                token.Append(jsonStr[i]);
                            else
                            {
                                RemoveTokenQuotes();
                                key.Append(token.ToString());
                                token.Length = 0;
                            }

                            break;

                        case '"':
                            isQuotesOpen ^= true;
                            token.Append(jsonStr[i]);
                            break;

                        case '\\':
                            ++i;

                            if (!isQuotesOpen)
                                break;

                            switch (jsonStr[i])
                            {
                                case 't': token.Append('\t'); break;
                                case 'r': token.Append('\r'); break;
                                case 'n': token.Append('\n'); break;
                                case 'b': token.Append('\b'); break;
                                case 'f': token.Append('\f'); break;
                                case 'u':
                                    token.Append((char)int.Parse(jsonStr.Substring(i + 1, 4), System.Globalization.NumberStyles.AllowHexSpecifier));
                                    i += 4;
                                    break;

                                default:
                                    token.Append(jsonStr[i]);
                                    break;
                            }

                            break;

                        default:
                            token.Append(jsonStr[i]);
                            break;
                    }
                }

                CheckRemainSignException();
                return baseNode as JsonClass;
            }
            #endregion

            #region Function
            private static void Reset()
            {
                token.Length   = 0;
                key.Length     = 0;
                jsonStack      .Clear();
                signStack      .Clear();
            }
            private static void AddNode(IJson node)
            {
                if (topNode == null)
                    return;

                switch(topNode.type)
                {
                    case ObjectType.Array:
                        if (key.Length > 0)
                            throw new JJsonFormatKeyException(key.ToString());

                        (topNode as JsonArray).Add(node);
                        break;

                    case ObjectType.Class:
                        if (key.Length <= 0)
                            throw new JJsonFormatKeyException();

                        (topNode as JsonClass).Add(key.ToString(), node);
                        key.Length = 0;
                        break;
                }
            }
            private static bool RemoveTokenQuotes()
            {
                if (isTokenEmpty || token[0] != '"')
                    return false;

                token.Remove(0, 1);
                token.Remove(token.Length - 1, 1);
                return true;
            }
            #endregion

            #region Debug
            private static void CheckCloseBracketException(char closeBracket, char openBracket)
            {
                if (jsonStack.Count <= 0)
                    throw new JJsonFormatBracketException(JJsonFormatBracketException.ExceptionType.ManyBracket, closeBracket);

                if (signStack.Peek() != openBracket)
                    throw new JJsonFormatBracketException(JJsonFormatBracketException.ExceptionType.UnmatchedBracket, string.Format("{0}, {1}", signStack.Pop(), closeBracket));

                signStack.Pop();
            }
            private static void CheckRemainSignException()
            {
                if (signStack.Count > 0)
                {
                    StringBuilder   sb      = new StringBuilder();
                    Stack<char>     stack   = new Stack<char>();

                    while (signStack.Count > 0)
                        stack.Push(signStack.Pop());

                    while (stack.Count > 0)
                    {
                        sb.Append(stack.Pop());
                        if (stack.Count > 0)
                            sb.Append(", ");
                    }

                    throw new JJsonFormatBracketException(JJsonFormatBracketException.ExceptionType.ManyBracket, sb.ToString());
                }
            }
            #endregion
        }

        public  static T        FromJson<T>(string jsonStr)         { return (T)FromJson(jsonStr, typeof(T)); }
        public  static object   FromJson(string jsonStr, Type type)
		{
			if (!type.IsDefined(typeof(SerializableAttribute), false))
				return null;

			return DeserializeJsonClass(ParseMachine.Parse(jsonStr), type);
		}
        public  static object   DeserializeJsonClass(JsonClass jsonCls, Type type)
        {
            object obj = Deserialize(jsonCls, type);

            if (obj is ISerializationCallbackReceiver)
                (obj as ISerializationCallbackReceiver).OnAfterDeserialize();
#if UNITY_3 || UNITY_4 || UNITY_5 || UNITY_2017_1_OR_NEWER
			else if (obj is UnityEngine.ISerializationCallbackReceiver)
                (obj as UnityEngine.ISerializationCallbackReceiver).OnAfterDeserialize();
#endif

            return obj;
        }
        private static object   ParseString(string val, Type type)
        {
            #region Primitive Type
            if (type == typeof(bool))
            {
                bool b;
                if (bool.TryParse(val, out b))
                    return b;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(byte))
            {
                byte b;
                if (byte.TryParse(val, out b))
                    return b;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(sbyte))
            {
                sbyte sb;
                if (sbyte.TryParse(val, out sb))
                    return sb;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(short))
            {
                short s;
                if (short.TryParse(val, out s))
                    return s;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(int))
            {
                int i;
                if (int.TryParse(val, out i))
                    return i;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(long))
            {
                long l;
                if (long.TryParse(val, out l))
                    return l;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(ushort))
            {
                ushort us;
                if (ushort.TryParse(val, out us))
                    return us;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(uint))
            {
                uint ui;
                if (uint.TryParse(val, out ui))
                    return ui;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(ulong))
            {
                ulong ul;
                if (ulong.TryParse(val, out ul))
                    return ul;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(float))
            {
                float f;
                if (float.TryParse(val, out f))
                    return f;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(double))
            {
                double d;
                if (double.TryParse(val, out d))
                    return d;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(decimal))
            {
                decimal dec;
                if (decimal.TryParse(val, out dec))
                    return dec;

                throw new JJsonDeserializeException(type, val);
            }

            if (type == typeof(char))
            {
                char c;
                if (char.TryParse(val, out c))
                    return c;

                throw new JJsonDeserializeException(type, val);
            }
            #endregion

            if (type == typeof(string))
                return val;

            if (type == typeof(StringBuilder))
                return new StringBuilder(val);

            if(type.IsEnum)
            {
                long l;
                if (Enum.IsDefined(type, val) || long.TryParse(val, out l))
                    return Enum.Parse(type, val, true);

                throw new JJsonDeserializeException(type, val);
            }

            if (IsNullable(type))
                return Nullable.GetUnderlyingType(type).IsEnum && string.IsNullOrEmpty(val) ? null : ParseString(val, Nullable.GetUnderlyingType(type));

            throw new JJsonUnsupportedTypeException(type);
        }
        private static object   Deserialize(IJson iJson, Type type)
        {
            switch(iJson.type)
            {
                case ObjectType.Value: return Deserialize(iJson as JsonValue, type);
                case ObjectType.Array: return Deserialize(iJson as JsonArray, type);
                case ObjectType.Class: return Deserialize(iJson as JsonClass, type);
            }

            throw new JJsonDeserializeException(type, iJson.ToString());
        }
        private static object   Deserialize(JsonValue jsonVal, Type type)
        {
            if(jsonVal.isNullVal)
                return null;

            if ((type == typeof(string) || type == typeof(StringBuilder)) &&
                !jsonVal.isQuotesRemoved
				)
            {
                throw new JJsonFormatException(type, jsonVal.val);
            }

            return ParseString(jsonVal.val, type);
        }
        private static object   Deserialize(JsonArray jsonArr, Type type)
        {
            Type elemType = GetElementType(type);

            if (elemType == null)
                throw new JJsonDeserializeException(type, jsonArr.ToString());

            if (type.IsArray)
            {
                Array arr = Array.CreateInstance(elemType, jsonArr.Count);
                for (int i = 0 ; i < arr.Length ; ++i)
                    arr.SetValue(Deserialize(jsonArr[i], elemType), i);

                return arr;
            }
            else
            {
                IList list = Activator.CreateInstance(type) as IList;
                foreach (var v in jsonArr)
                    list.Add((Deserialize(v, elemType)));

                return list;
            }
        }
        private static object   Deserialize(JsonClass jsonCls, Type type)
        {
            if (type.IsPrimitive || type.IsEnum)
                throw new JJsonDeserializeException(type, jsonCls.ToString());

            if(IsNullable(type))
                return Activator.CreateInstance(type, Deserialize(jsonCls, type.GetGenericArguments()[0]));

            object inst = Activator.CreateInstance(type, true);

            if(IsGenericDictionary(type))
            {
                Type[] genericTypes = type.GetGenericArguments();

                foreach (var v in jsonCls)
                    (inst as IDictionary)[ParseString(v.Key, genericTypes[0])] = Deserialize(v.Value, genericTypes[1]);

                return inst;
            }

            FieldInfo[] fieldInfos = type.GetFields(DEFAULT_BINDING_FLAGS);

            for (int i = 0 ; i < fieldInfos.Length ; ++i)
            {
                if (fieldInfos[i].IsJJsonSerializable() && jsonCls.ContainsKey(fieldInfos[i].Name))
                    fieldInfos[i].SetValue(inst, Deserialize(jsonCls[fieldInfos[i].Name], fieldInfos[i].FieldType));
            }

            return inst;
        }
        #endregion

        #region To Json
        public  static string       ToJson(object obj, EnumConvertType enumConvertType = EnumConvertType.String, bool niceToLook = true)
        {
			if (!obj.GetType().IsDefined(typeof(SerializableAttribute), false))
				return niceToLook ? "{\n\t\n}" : "{}";

            if (obj is ISerializationCallbackReceiver)
                (obj as ISerializationCallbackReceiver).OnBeforeSerialize();
#if UNITY_3 || UNITY_4 || UNITY_5 || UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
			else if (obj is UnityEngine.ISerializationCallbackReceiver)
                (obj as UnityEngine.ISerializationCallbackReceiver).OnBeforeSerialize();
#endif

			JsonClass		jsonCls = CreateJsonClass(obj, obj.GetType());
            StringBuilder   jsonStr = new StringBuilder();
            AppendJsonString(jsonStr, jsonCls, enumConvertType, niceToLook, 0);

            return jsonStr.ToString();
        }
        private static JsonArray    CreateJsonArray(object obj, Type type)
        {
            if (obj == null)
                return new JsonArray();

            JsonArray   jsonArr = new JsonArray();
            Type        elemType= GetElementType(type);

            if (elemType == null)
                throw new JJsonCreateException(obj, type);

            foreach(var v in obj as IList)
                jsonArr.Add(ParseField(v, elemType));

            return jsonArr;
        }
        private static JsonClass    CreateJsonClass(object obj, Type type)
        {
            if (obj == null)
                return new JsonClass();

            FieldInfo[] fieldInfos  = type.GetFields(DEFAULT_BINDING_FLAGS);
            JsonClass   jsonCls     = new JsonClass();

            for (int i = 0 ; i < fieldInfos.Length ; ++i)
            {
                if (fieldInfos[i].IsJJsonSerializable())
                    jsonCls.Add(fieldInfos[i].Name, ParseField(fieldInfos[i].GetValue(obj), fieldInfos[i].FieldType));
            }

            return jsonCls;
        }
        private static IJson        ParseField(object obj, Type type)
        {
            if (obj == null)
                return new JsonValue(NULL_STR, type, true);

            if (IsCanParseToStringImmediately(type))
                return new JsonValue(PrimitiveValueToString(obj), type, false);

            if(IsNullable(type))
            {
                if (IsCanParseToStringImmediately(Nullable.GetUnderlyingType(type)))
                    return new JsonValue(PrimitiveValueToString(obj), type, false);

                return CreateJsonClass(obj, Nullable.GetUnderlyingType(type));
            }

            if (type.IsArray || IsGenericList(type))
                return CreateJsonArray(obj, type);

            if (IsGenericDictionary(type))
            {
                JsonClass   jsonCls     = new JsonClass();
                Type[]      genericTypes= type.GetGenericArguments();

                foreach (DictionaryEntry w in obj as IDictionary)
                    jsonCls.Add(w.Key.ToString(), ParseField(w.Value, genericTypes[1]));

                return jsonCls;
            }

            return CreateJsonClass(obj, type);
        }
        private static string       PrimitiveValueToString(object obj)      { return obj is bool ? obj.ToString().ToLower() : obj.ToString(); }
        private static bool         IsCanParseToStringImmediately(Type type){ return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(StringBuilder); }
        private static void         AppendJsonString(StringBuilder jsonStr, IJson iJson, EnumConvertType enumConvertType, bool niceToLook, int tabCnt)
        {
            switch(iJson.type)
            {
                case ObjectType.Value: AppendJsonString(jsonStr, iJson as JsonValue, enumConvertType, niceToLook);			break;
                case ObjectType.Array: AppendJsonString(jsonStr, iJson as JsonArray, enumConvertType, niceToLook, tabCnt);	break;
                case ObjectType.Class: AppendJsonString(jsonStr, iJson as JsonClass, enumConvertType, niceToLook, tabCnt);	break;
            }
        }
        private static void         AppendJsonString(StringBuilder jsonStr, JsonValue jsonVal, EnumConvertType enumConvertType, bool niceToLook)
        {
            if (jsonVal.valType.IsPrimitive ||
                jsonVal.isNullVal ||
                (IsNullable(jsonVal.valType) && Nullable.GetUnderlyingType(jsonVal.valType).IsPrimitive))
            {
                jsonStr.Append(jsonVal.val);
                return;
            }

			if (jsonVal.valType.IsEnum && enumConvertType == EnumConvertType.Number)
			{
				jsonStr.Append((int)Enum.Parse(jsonVal.valType, jsonVal.val));
				return;
			}

            jsonStr.Append('"');
            jsonStr.Append(MakeEscape(jsonVal.val));
            jsonStr.Append('"');
        }
        private static void         AppendJsonString(StringBuilder jsonStr, JsonArray jsonArr, EnumConvertType enumConvertType, bool niceToLook, int tabCnt)
        {
            jsonStr.Append('[');
            AppendLine(jsonStr, niceToLook);
            ++tabCnt;
            bool isNeedComma = false;

            foreach(var v in jsonArr)
            {
                if (isNeedComma)
                    AppendComma(jsonStr, niceToLook);

                AppendJsonTab(jsonStr, tabCnt, niceToLook);
                AppendJsonString(jsonStr, v, enumConvertType, niceToLook, tabCnt);
                isNeedComma = true;
            }

            AppendLine(jsonStr, niceToLook);
            AppendJsonTab(jsonStr, --tabCnt, niceToLook);
            jsonStr.Append(']');
        }
        private static void         AppendJsonString(StringBuilder jsonStr, JsonClass jsonCls, EnumConvertType enumConvertType, bool niceToLook, int tabCnt)
        {
            jsonStr.Append('{');
            AppendLine(jsonStr, niceToLook);
            ++tabCnt;
            bool isNeedComma = false;

            foreach(var v in jsonCls)
            {
                if (isNeedComma)
                    AppendComma(jsonStr, niceToLook);

                AppendJsonTab(jsonStr, tabCnt, niceToLook);

                jsonStr.Append('"');
                jsonStr.Append(v.Key);
                jsonStr.Append('"');
                jsonStr.Append(':');
                if(niceToLook)
                    jsonStr.Append(' ');

                AppendJsonString(jsonStr, v.Value, enumConvertType, niceToLook, tabCnt);
                isNeedComma = true;
            }

            AppendLine(jsonStr, niceToLook);
            AppendJsonTab(jsonStr, --tabCnt, niceToLook);
            jsonStr.Append('}');
        }
        private static void         AppendJsonTab(StringBuilder jsonStr, int tabCnt, bool niceToLook)
        {
            if (!niceToLook)
                return;

            for (int i = 0 ; i < tabCnt ; ++i)
                jsonStr.Append(JSON_TAB);
        }
        private static void         AppendComma(StringBuilder jsonStr, bool niceToLook)
        {
            jsonStr.Append(',');
            AppendLine(jsonStr, niceToLook);
        }
        private static void         AppendLine(StringBuilder jsonStr, bool niceToLook)
        {
            if (niceToLook)
                jsonStr.AppendLine();
        }
        private static string       MakeEscape(string str)
        {
            StringBuilder sb = new StringBuilder();

            foreach(char c in str)
            {
                switch (c)
                {
                    case '\\':  sb.Append("\\\\");  break;
                    case '\"':  sb.Append("\\\"");  break;
                    case '\n':  sb.Append("\\n");   break;
                    case '\r':  sb.Append("\\r");   break;
                    case '\t':  sb.Append("\\t");   break;
                    case '\b':  sb.Append("\\b");   break;
                    case '\f':  sb.Append("\\f");   break;
                    default:    sb.Append(c);       break;
                }
            }

            return sb.ToString();
        }
        #endregion

        #region Function
        private static bool IsNullable(Type type) { return Nullable.GetUnderlyingType(type) != null; }
        private static bool IsGenericList(Type type)
        {
            return (!type.IsGenericType || type.GetGenericArguments().Length != 1) ?
                false :
                type == typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
        }
        private static bool IsGenericDictionary(Type type)
        {
            return (!type.IsGenericType || type.GetGenericArguments().Length != 2) ?
                false :
                type == typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments()[0], type.GetGenericArguments()[1]);
        }
        private static Type GetElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (IsGenericList(type))
                return type.GetGenericArguments()[0];

            return null;
        }
        #endregion

        #region Expantion Method
        /// <summary>
        /// Check field can serialize in JJson.
        /// </summary>
        /// <param name="info">field info</param>
        /// <returns></returns>
        public static bool IsJJsonSerializable(this FieldInfo info)
        {
			if (info.IsDefined(typeof(NonSerializedAttribute), true))
				return false;

			bool b = info.FieldType.IsClass;
			if (!info.FieldType.IsEnum && !info.FieldType.IsDefined(typeof(SerializableAttribute), false))
				return false;

			if (info.IsPublic)
				return true;

			if (info.IsDefined(typeof(SerializeFieldAttribute), true))
				return true;

#if UNITY_3 || UNITY_4 || UNITY_5 || UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
			if (info.IsDefined(typeof(UnityEngine.SerializeField), true))
				return true;
#endif
			return false;
        }
        #endregion
    }
}