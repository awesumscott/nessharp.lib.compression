using NESSharp.Core;
using System;
using System.Collections.Generic;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.Compression {
	public class RLESimple : Module, ICompressionScheme {
		private VByte _temp;

		[Dependencies]
		private void Dependencies() {
			_temp = VByte.New(Ram, $"{nameof(RLESimple)}{nameof(_temp)}");
		}

		public byte[] Compress(params byte[] data) {
			byte cur;
			var len = data.Length;
			var output = new List<byte>();

			void compress(int runLength, byte chr) {
				if (runLength <= 255) {
					output.Add((byte)runLength);
					output.Add(chr);
				} else throw new NotImplementedException(); //TODO: support >255 run lengths
			}

			for (var i = 0; i < len; i++) {
				cur = data[i];

				//Count total # of the same char
				var runLength = 1;
				for (var j = i + 1; j < len; j++) {
					if (cur == data[j])
						runLength++;
					else
						break;
				}

				compress(runLength, cur);
				i += runLength - 1;
			}
			var count = output.Count;
			if (count > 255) throw new NotImplementedException();
			output.Insert(0, (byte)(count + 1)); //max offset from starting value in this data set

			return output.ToArray();
		}

		public void Decompress(Action<RegisterA> block) {
			_temp.Set(TempPtr0[Y.Set(0)]);
			Loop.AscendWhile(Y++, () => Y.LessThan(_temp), _ => {
				Y.State.Unsafe(() => {
					X.Set(A.Set(TempPtr0[Y]));
					Y++;
					Loop.Descend_Post(X, _ => {
						block(A.Set(TempPtr0[Y]));
					});
				});
			});
		}
	}
}
