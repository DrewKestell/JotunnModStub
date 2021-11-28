using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public class ZPackage
{
	private MemoryStream m_stream = new MemoryStream();

	private BinaryWriter m_writer;

	private BinaryReader m_reader;

	public ZPackage()
	{
		m_writer = new BinaryWriter(m_stream);
		m_reader = new BinaryReader(m_stream);
	}

	public ZPackage(string base64String)
	{
		m_writer = new BinaryWriter(m_stream);
		m_reader = new BinaryReader(m_stream);
		if (!string.IsNullOrEmpty(base64String))
		{
			byte[] array = Convert.FromBase64String(base64String);
			m_stream.Write(array, 0, array.Length);
			m_stream.Position = 0L;
		}
	}

	public ZPackage(byte[] data)
	{
		m_writer = new BinaryWriter(m_stream);
		m_reader = new BinaryReader(m_stream);
		m_stream.Write(data, 0, data.Length);
		m_stream.Position = 0L;
	}

	public ZPackage(byte[] data, int dataSize)
	{
		m_writer = new BinaryWriter(m_stream);
		m_reader = new BinaryReader(m_stream);
		m_stream.Write(data, 0, dataSize);
		m_stream.Position = 0L;
	}

	public void Load(byte[] data)
	{
		Clear();
		m_stream.Write(data, 0, data.Length);
		m_stream.Position = 0L;
	}

	public void Write(ZPackage pkg)
	{
		byte[] array = pkg.GetArray();
		m_writer.Write(array.Length);
		m_writer.Write(array);
	}

	public void WriteCompressed(ZPackage pkg)
	{
		byte[] array = Utils.Compress(pkg.GetArray());
		m_writer.Write(array.Length);
		m_writer.Write(array);
	}

	public void Write(byte[] array)
	{
		m_writer.Write(array.Length);
		m_writer.Write(array);
	}

	public void Write(byte data)
	{
		m_writer.Write(data);
	}

	public void Write(sbyte data)
	{
		m_writer.Write(data);
	}

	public void Write(char data)
	{
		m_writer.Write(data);
	}

	public void Write(bool data)
	{
		m_writer.Write(data);
	}

	public void Write(int data)
	{
		m_writer.Write(data);
	}

	public void Write(short data)
	{
		m_writer.Write(data);
	}

	public void Write(uint data)
	{
		m_writer.Write(data);
	}

	public void Write(ulong data)
	{
		m_writer.Write(data);
	}

	public void Write(long data)
	{
		m_writer.Write(data);
	}

	public void Write(float data)
	{
		m_writer.Write(data);
	}

	public void Write(double data)
	{
		m_writer.Write(data);
	}

	public void Write(string data)
	{
		m_writer.Write(data);
	}

	public void Write(ZDOID id)
	{
		m_writer.Write(id.userID);
		m_writer.Write(id.id);
	}

	public void Write(Vector3 v3)
	{
		m_writer.Write(v3.x);
		m_writer.Write(v3.y);
		m_writer.Write(v3.z);
	}

	public void Write(Vector2i v2)
	{
		m_writer.Write(v2.x);
		m_writer.Write(v2.y);
	}

	public void Write(Quaternion q)
	{
		m_writer.Write(q.x);
		m_writer.Write(q.y);
		m_writer.Write(q.z);
		m_writer.Write(q.w);
	}

	public ZDOID ReadZDOID()
	{
		return new ZDOID(m_reader.ReadInt64(), m_reader.ReadUInt32());
	}

	public bool ReadBool()
	{
		return m_reader.ReadBoolean();
	}

	public char ReadChar()
	{
		return m_reader.ReadChar();
	}

	public byte ReadByte()
	{
		return m_reader.ReadByte();
	}

	public sbyte ReadSByte()
	{
		return m_reader.ReadSByte();
	}

	public short ReadShort()
	{
		return m_reader.ReadInt16();
	}

	public int ReadInt()
	{
		return m_reader.ReadInt32();
	}

	public uint ReadUInt()
	{
		return m_reader.ReadUInt32();
	}

	public long ReadLong()
	{
		return m_reader.ReadInt64();
	}

	public ulong ReadULong()
	{
		return m_reader.ReadUInt64();
	}

	public float ReadSingle()
	{
		return m_reader.ReadSingle();
	}

	public double ReadDouble()
	{
		return m_reader.ReadDouble();
	}

	public string ReadString()
	{
		return m_reader.ReadString();
	}

	public Vector3 ReadVector3()
	{
		Vector3 result = default(Vector3);
		result.x = m_reader.ReadSingle();
		result.y = m_reader.ReadSingle();
		result.z = m_reader.ReadSingle();
		return result;
	}

	public Vector2i ReadVector2i()
	{
		Vector2i result = default(Vector2i);
		result.x = m_reader.ReadInt32();
		result.y = m_reader.ReadInt32();
		return result;
	}

	public Quaternion ReadQuaternion()
	{
		Quaternion result = default(Quaternion);
		result.x = m_reader.ReadSingle();
		result.y = m_reader.ReadSingle();
		result.z = m_reader.ReadSingle();
		result.w = m_reader.ReadSingle();
		return result;
	}

	public ZPackage ReadCompressedPackage()
	{
		int count = m_reader.ReadInt32();
		return new ZPackage(Utils.Decompress(m_reader.ReadBytes(count)));
	}

	public ZPackage ReadPackage()
	{
		int count = m_reader.ReadInt32();
		return new ZPackage(m_reader.ReadBytes(count));
	}

	public void ReadPackage(ref ZPackage pkg)
	{
		int count = m_reader.ReadInt32();
		byte[] array = m_reader.ReadBytes(count);
		pkg.Clear();
		pkg.m_stream.Write(array, 0, array.Length);
		pkg.m_stream.Position = 0L;
	}

	public byte[] ReadByteArray()
	{
		int count = m_reader.ReadInt32();
		return m_reader.ReadBytes(count);
	}

	public string GetBase64()
	{
		return Convert.ToBase64String(GetArray());
	}

	public byte[] GetArray()
	{
		m_writer.Flush();
		m_stream.Flush();
		return m_stream.ToArray();
	}

	public void SetPos(int pos)
	{
		m_stream.Position = pos;
	}

	public int GetPos()
	{
		return (int)m_stream.Position;
	}

	public int Size()
	{
		m_writer.Flush();
		m_stream.Flush();
		return (int)m_stream.Length;
	}

	public void Clear()
	{
		m_writer.Flush();
		m_stream.SetLength(0L);
		m_stream.Position = 0L;
	}

	public byte[] GenerateHash()
	{
		byte[] array = GetArray();
		return SHA512.Create().ComputeHash(array);
	}
}
