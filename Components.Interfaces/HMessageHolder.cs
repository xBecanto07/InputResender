using Components.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Components.Interfaces;
public class HMessageHolder {
	[Flags]
	public enum MsgFlags { None = 0, Encrypted = 1 }
	public readonly object Sender, Target;
	private readonly byte[] Data;
	private readonly MsgFlags Flags;

	/// <summary>N-th byte of unpacked message</summary>
	public byte this[int index] => Data[index + 1];
	public bool this[MsgFlags flag] => Flags.HasFlag ( flag );
	public byte[] InnerMsg => Data[1..];
	/// <summary>Length of unpacked message (the inner data)</summary>
	public int Length { get => Data.Length - 1; }
	/// <summary>Length of entire message (including flags)</summary>
	public int Size { get => Data.Length; }

	public ReadOnlySpan<byte> InnerSpan => Data[1..];
	public ReadOnlySpan<byte> Span => Data;
	public static implicit operator ReadOnlyMemory<byte> ( HMessageHolder msg ) => msg.Data;
	/// <summary>Returns clone of entire message (including flags)</summary>
	public static explicit operator byte[] ( HMessageHolder msg ) {
		byte[] res = new byte[msg.Data.Length];
		msg.Data.CopyTo ( res, 0 );
		return res;
	}
	/// <summary>Parse the byte[]</summary>
	public static explicit operator HMessageHolder ( byte[] data ) => new ( data );

	/// <summary>Will add 1byte prefix with flags</summary>
	public HMessageHolder ( MsgFlags flags, byte[] data ) {
		if ( data == null ) throw new ArgumentNullException ( nameof ( data ) );
		Flags = flags;
		Data = new byte[data.Length + 1];
		Data[0] = (byte)flags;
		data.CopyTo ( Data, 1 );
	}

	/// <summary>Will parse 1byte prefix with flags</summary>
	private HMessageHolder ( byte[] data ) {
		if ( data == null ) throw new ArgumentNullException ( nameof ( data ) );
		if ( data.Length < 1 ) throw new ArgumentException ( "Data is empty.", nameof ( data ) );
		Data = data;
		Flags = (MsgFlags)data[0];
		if ( !Enum.IsDefined ( Flags ) ) throw new ArgumentException ( $"Invalid flags value '{Flags}'." );
	}
}