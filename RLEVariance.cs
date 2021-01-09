using NESSharp.Core;
using System;
using System.Collections.Generic;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.Compression {
	public class RLEVariance : Module, ICompressionScheme {
		private VByte _temp;

		[Dependencies]
		private void Dependencies() {
			_temp = VByte.New(Ram, $"{nameof(RLEVariance)}{nameof(_temp)}");
		}

		public byte[] Compress(params byte[] data) {
			byte compressionIndicator = 255;
			byte cur;
			byte? next;
			var len = data.Length;
			var output = new List<byte>();

			void compress(int runLength, byte chr) {
				if (runLength <= 255) {
					output.Add(compressionIndicator);
					output.Add((byte)runLength);
					output.Add(chr);
				} else throw new NotImplementedException(); //TODO: support >255 run lengths
			}

			for (var i = 0; i < len; i++) {
				cur = data[i];
				next = i + 1 >= len ? (byte?)null : data[i + 1];
				if (cur != next && cur != compressionIndicator) {
					output.Add(cur);
					continue;
				}

				//Count total # of the same char
				var runLength = 1;
				for (var j = i + 1; j < len; j++) {
					if (cur == data[j])
						runLength++;
					else
						break;
				}

				//if cur == compressionIndicator, compress regardless of run length
				if (cur == compressionIndicator) {
					compress(runLength, cur);
					i += runLength - 1;
					continue;
				}

				//If run is <= 3, not worth it, add and proceed
				if (runLength <= 3) {
					output.Add(cur);
					continue;
				}
				
				//if >=3, compress and prefix output with compressionIndicator
				compress(runLength, cur);
				i += runLength - 1;
			}
			var count = output.Count;
			if (count > 255) throw new NotImplementedException();
			output.Insert(0, (byte)(count + 1)); //max offset from starting value in this data set

			return output.ToArray();
		}

		public void Decompress(Action<RegisterA> block) {
			byte compressionIndicator = 255;
			_temp.Set(TempPtr0[Y.Set(0)]);
			Loop.AscendWhile(Y++, () => Y.LessThan(_temp), _ => {
				If.Block(c => c
					.True(() => A.Set(TempPtr0[Y]).Equals(compressionIndicator), () => {
						Y.State.Unsafe(() => {
							Y++;
							X.Set(A.Set(TempPtr0[Y]));
							Y++;
							Loop.Descend_Post(X, _ => block(A.Set(TempPtr0[Y])));
						});
					})
					.Else(() => block(A.Set(TempPtr0[Y])))
				);
			});
		}
	}
}
