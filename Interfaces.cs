using NESSharp.Core;
using System;

namespace NESSharp.Lib.Compression {
	public interface ICompressionScheme {
		public byte[] Compress(params byte[] data);
		public void Decompress(Action<RegisterA> block);
	}
}
