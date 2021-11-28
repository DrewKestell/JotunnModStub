using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Localization
{
	private static Localization m_instance;

	private char[] m_endChars = " (){}[]+-!?/\\\\&%,.:-=<>\n".ToCharArray();

	private Dictionary<string, string> m_translations = new Dictionary<string, string>();

	private List<string> m_languages = new List<string>();

	public static Localization instance
	{
		get
		{
			if (m_instance == null)
			{
				Initialize();
			}
			return m_instance;
		}
	}

	private static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new Localization();
		}
	}

	private Localization()
	{
		m_languages = LoadLanguages();
		SetupLanguage("English");
		string @string = PlayerPrefs.GetString("language", "");
		if (@string != "")
		{
			SetupLanguage(@string);
		}
	}

	public void SetLanguage(string language)
	{
		PlayerPrefs.SetString("language", language);
	}

	public string GetSelectedLanguage()
	{
		return PlayerPrefs.GetString("language", "English");
	}

	public string GetNextLanguage(string lang)
	{
		for (int i = 0; i < m_languages.Count; i++)
		{
			if (m_languages[i] == lang)
			{
				if (i + 1 < m_languages.Count)
				{
					return m_languages[i + 1];
				}
				return m_languages[0];
			}
		}
		return m_languages[0];
	}

	public string GetPrevLanguage(string lang)
	{
		for (int i = 0; i < m_languages.Count; i++)
		{
			if (m_languages[i] == lang)
			{
				if (i - 1 >= 0)
				{
					return m_languages[i - 1];
				}
				return m_languages[m_languages.Count - 1];
			}
		}
		return m_languages[0];
	}

	public void Localize(Transform root)
	{
		Text[] componentsInChildren = root.gameObject.GetComponentsInChildren<Text>(includeInactive: true);
		foreach (Text text in componentsInChildren)
		{
			text.text = Localize(text.text);
		}
	}

	public string Localize(string text, params string[] words)
	{
		string text2 = Localize(text);
		return InsertWords(text2, words);
	}

	private string InsertWords(string text, string[] words)
	{
		for (int i = 0; i < words.Length; i++)
		{
			string newValue = words[i];
			text = text.Replace("$" + (i + 1), newValue);
		}
		return text;
	}

	public string Localize(string text)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		string word;
		int wordStart;
		int wordEnd;
		while (FindNextWord(text, num, out word, out wordStart, out wordEnd))
		{
			stringBuilder.Append(text.Substring(num, wordStart - num));
			stringBuilder.Append(Translate(word));
			num = wordEnd;
		}
		stringBuilder.Append(text.Substring(num));
		return stringBuilder.ToString();
	}

	private bool FindNextWord(string text, int startIndex, out string word, out int wordStart, out int wordEnd)
	{
		if (startIndex >= text.Length - 1)
		{
			word = null;
			wordStart = -1;
			wordEnd = -1;
			return false;
		}
		wordStart = text.IndexOf('$', startIndex);
		if (wordStart != -1)
		{
			int num = text.IndexOfAny(m_endChars, wordStart);
			if (num != -1)
			{
				word = text.Substring(wordStart + 1, num - wordStart - 1);
				wordEnd = num;
			}
			else
			{
				word = text.Substring(wordStart + 1);
				wordEnd = text.Length;
			}
			return true;
		}
		word = null;
		wordEnd = -1;
		return false;
	}

	private string Translate(string word)
	{
		if (word.StartsWith("KEY_"))
		{
			string bindingName = word.Substring(4);
			return GetBoundKeyString(bindingName);
		}
		if (m_translations.TryGetValue(word, out var value))
		{
			return value;
		}
		return "[" + word + "]";
	}

	public string GetBoundKeyString(string bindingName)
	{
		string boundKeyString = ZInput.instance.GetBoundKeyString(bindingName);
		if (boundKeyString.Length > 0 && boundKeyString[0] == '$' && m_translations.TryGetValue(boundKeyString.Substring(1), out var value))
		{
			return value;
		}
		return boundKeyString;
	}

	private void AddWord(string key, string text)
	{
		m_translations.Remove(key);
		m_translations.Add(key, text);
	}

	private void Clear()
	{
		m_translations.Clear();
	}

	private string StripCitations(string s)
	{
		if (s.StartsWith("\""))
		{
			s = s.Remove(0, 1);
			if (s.EndsWith("\""))
			{
				s = s.Remove(s.Length - 1, 1);
			}
		}
		return s;
	}

	public bool SetupLanguage(string language)
	{
		if (!LoadCSV("localization", language))
		{
			return false;
		}
		LoadCSV("localization_extra", language);
		return true;
	}

	public bool LoadCSV(string fileName, string language)
	{
		TextAsset textAsset = Resources.Load(fileName, typeof(TextAsset)) as TextAsset;
		if (textAsset == null)
		{
			ZLog.Log("Failed to load language file " + fileName);
			return false;
		}
		StringReader stringReader = new StringReader(textAsset.text);
		string[] array = stringReader.ReadLine().Split(',');
		int num = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if (StripCitations(array[i]) == language)
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			ZLog.LogWarning("Failed to find language:" + language);
			return false;
		}
		foreach (List<string> item in DoQuoteLineSplit(stringReader))
		{
			if (item.Count == 0)
			{
				continue;
			}
			string text = item[0];
			if (!text.StartsWith("//") && text.Length != 0 && item.Count > num)
			{
				string text2 = item[num];
				if (string.IsNullOrEmpty(text2) || text2[0] == '\r')
				{
					text2 = item[1];
				}
				AddWord(text, text2);
			}
		}
		ZLog.Log("Loaded localization CSV:" + fileName + " language:" + language);
		return true;
	}

	private List<List<string>> DoQuoteLineSplit(StringReader reader)
	{
		List<List<string>> list = new List<List<string>>();
		List<string> list2 = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		while (true)
		{
			int num = reader.Read();
			switch (num)
			{
			case -1:
				list2.Add(stringBuilder.ToString());
				list.Add(list2);
				return list;
			case 34:
				flag = !flag;
				continue;
			case 44:
				if (!flag)
				{
					list2.Add(stringBuilder.ToString());
					stringBuilder.Length = 0;
					continue;
				}
				break;
			}
			if (num == 10 && !flag)
			{
				list2.Add(stringBuilder.ToString());
				stringBuilder.Length = 0;
				list.Add(list2);
				list2 = new List<string>();
			}
			else
			{
				stringBuilder.Append((char)num);
			}
		}
	}

	public List<string> GetLanguages()
	{
		return m_languages;
	}

	private List<string> LoadLanguages()
	{
		string[] array = new StringReader((Resources.Load("localization", typeof(TextAsset)) as TextAsset).text).ReadLine().Split(',');
		List<string> list = new List<string>();
		for (int i = 1; i < array.Length; i++)
		{
			list.Add(StripCitations(array[i]));
		}
		return list;
	}
}
