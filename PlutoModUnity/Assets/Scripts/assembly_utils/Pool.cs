using System.Collections.Generic;

public class Pool<T> where T : new()
{
	private static Stack<T> _available = new Stack<T>();

	public static T Create()
	{
		lock (_available)
		{
			if (_available.Count > 0)
			{
				return _available.Pop();
			}
			return new T();
		}
	}

	public static void Release(T obj)
	{
		if (obj == null)
		{
			return;
		}
		lock (_available)
		{
			_available.Push(obj);
		}
	}
}
