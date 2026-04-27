using NESSharp.Core;
using System;
using System.Collections.Generic;

namespace NESSharp.Lib.Compression;

public class RLEVariance : Module, ICompressionScheme {
	private VByte _temp;

	[Dependencies]
	private void Dependencies() {
		_temp = VByte.New(Ram, $"{nameof(RLEVariance)}{nameof(_temp)}");
	}

	public U8[] Compress(params U8[] data) {
		U8 compressionIndicator = 255;
		U8 cur;
		U8? next;
		var len = data.Length;
		var output = new List<U8>();

		void compress(int runLength, U8 chr) {
			if (runLength <= 255) {
				output.Add(compressionIndicator);
				output.Add(runLength);
				output.Add(chr);
			} else throw new NotImplementedException(); //TODO: support >255 run lengths
		}

		for (var i = 0; i < len; i++) {
			cur = data[i];
			next = i + 1 >= len ? null : data[i + 1];
			if (cur != next && cur != compressionIndicator) {
				output.Add(cur);
				continue;
			}

			//Count total # of the same char
			var runLength = 1;
			for (var j = i + 1; j < len; j++) {
				if (cur == data[j] && runLength < 255)
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
		output.Insert(0, count + 1); //max offset from starting value in this data set

		return output.ToArray();
	}

	public void Decompress(Action<RegisterA> block) {
		U8 compressionIndicator = 255;
		_temp.Set(AL.TempPtr0[CPU.Y.Set(0)]);
		Loop.While_PostCondition_PostInc(CPU.Y.Inc(), () => CPU.Y.LessThan(_temp), _ => {
			If.Block(c => c
				.True(() => CPU.A.Set(AL.TempPtr0[CPU.Y]).Equals(compressionIndicator), () => {
					CPU.Y.State.Unsafe(() => {
						CPU.Y.Inc();
						CPU.X.Set(CPU.A.Set(AL.TempPtr0[CPU.Y]));
						CPU.Y.Inc();
						Loop.Descend_PostCondition_PostDec(CPU.X, _ => block(CPU.A.Set(AL.TempPtr0[CPU.Y])));
					});
				})
				.Else(() => block(CPU.A.Set(AL.TempPtr0[CPU.Y])))
			);
		});
	}
}
