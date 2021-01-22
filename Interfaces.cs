using NESSharp.Core;
using System;

namespace NESSharp.Lib.Compression {
	public interface ICompressionScheme {
		public U8[] Compress(params U8[] data);
		public void Decompress(Action<RegisterA> block);
	}
}
