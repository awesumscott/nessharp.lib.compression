using NESSharp.Core;
using System;
using System.Collections.Generic;

namespace NESSharp.Lib.Compression;

public class RLESimple : Module, ICompressionScheme {
	private VByte _temp;

	[Dependencies]
	private void Dependencies() {
		_temp = VByte.New(Ram, $"{nameof(RLESimple)}{nameof(_temp)}");
	}

	public U8[] Compress(params U8[] data) {
		U8 cur;
		var len = data.Length;
		var output = new List<U8>();

		void compress(int runLength, byte chr) {
			if (runLength <= 255) {
				output.Add(runLength);
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
		output.Insert(0, count + 1); //max offset from starting value in this data set

		return output.ToArray();
	}

	public void Decompress(Action<RegisterA> block) {
		_temp.Set(AL.TempPtr0[CPU.Y.Set(0)]);
		Loop.While_PostCondition_PostInc(CPU.Y.Inc(), () => CPU.Y.LessThan(_temp), _ => {
			CPU.Y.State.Unsafe(() => {
				CPU.X.Set(CPU.A.Set(AL.TempPtr0[CPU.Y]));
				CPU.Y.Inc();
				Loop.Descend_PostCondition_PostDec(CPU.X, _ => {
					block(CPU.A.Set(AL.TempPtr0[CPU.Y]));
				});
			});
		});
	}
}
