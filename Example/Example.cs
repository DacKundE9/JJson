using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

// Support Generic Dictionary
// Support nullable field
// Support any unity version
// Support UnityEngine.ISerializationCallbackReceiver
// Support C# project

// Easy use
// string JJson.ToJson(object obj)
// T JJson.FromJson<T>(string jsonStr)

namespace JFramework
{
	public enum MyEnum { A, B, C, D, E }

	[Serializable]
	public struct MySerializableStruct
	{
		public int a;
		public string str;
	}

	public struct MyNonSerializableStruct
	{
		public int a;
		public string str;
	}

	[Serializable]
	public class ExampleClass :
#if UNITY_EDITOR
		UnityEngine.ISerializationCallbackReceiver
#else
		JJson.ISerializationCallbackReceiver
#endif
	{
		// can serialize public field
		public int pulbicField = 1;

		// jjson ignore private field
		private int privateField = 2;

#if UNITY_EDITOR
		// this field is private, but can serialize
		[SerializeField]
		private int serializablePrivateField = 3;
#endif

		// public field, but can't serialize
		[NonSerialized]
		public int nonSerializedPublicField = 4;

		// support Array & List
		public float[] arr = new float[] { 1.0f, 1.2f, 3.0f };
		public List<float> list = new List<float>() { 5.0f, 3.2f, 124.2f };

		// Class & Struct must has System.Serializable Attribute
		public MySerializableStruct		serializableStruct		= new MySerializableStruct()	{ a = 10, str = "serializableStruct" };	// can serialize
		public MyNonSerializableStruct	nonSerializableStruct	= new MyNonSerializableStruct() { a = 12, str = "nonSerializableStruct" };	// can't serialize

		// Support Dictionary!!!
		public Dictionary<string, int> myDic = new Dictionary<string, int>() { { "a", 1 }, { "b", 2 }, { "c", 3 } };

		// JJson.ToJson can select enum parse type
		// JJson.FromJson can parse number value and string value
		public MyEnum myEnum1 = MyEnum.B;
		public MyEnum myEnum2 = MyEnum.E;

		// Support nullable field
		public bool? nullable		= null;
		public bool? nullableBool	= true;

		// UnityEngine.ISerializationCallbackReceiver & JJson.ISerializationCallbackReceiver
		public void OnBeforeSerialize()
		{

		}

		public void OnAfterDeserialize()
		{

		}
	}



#if UNITY_EDITOR
	public class Example : MonoBehaviour
	{
		// Can't see nullable and Dictionary in Inspector Window
		public ExampleClass exampleClass;
		public TextAsset jsonSource;
	}

	[CustomEditor(typeof(Example))]
	public class ExampleEditor : Editor
	{
		public new Example target { get { return base.target as Example; } }

		public static JJson.EnumConvertType enumConvertType = JJson.EnumConvertType.String;
		public static bool					niceToLook		= true;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			enumConvertType = (JJson.EnumConvertType)EditorGUILayout.EnumPopup("Enum Convert Type", enumConvertType);
			niceToLook		= EditorGUILayout.Toggle("Nice To Look", niceToLook);

			if(GUILayout.Button("Log exampleClass to json"))
			{
				Debug.Log(JJson.ToJson(this.target.exampleClass, enumConvertType, niceToLook));
			}

			if (GUILayout.Button("Deserialize jsonSource to my exampleClass field"))
			{
				this.target.exampleClass = JJson.FromJson<ExampleClass>(this.target.jsonSource.text);
			}
		}
	}
#endif
}