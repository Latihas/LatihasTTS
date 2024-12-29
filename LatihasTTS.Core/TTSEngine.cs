using System.Text;

namespace LatihasTTS.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.ML.OnnxRuntime;
using NAudio.Wave;

public static class TTSEngine {
	private static class PaddleTextTokenizer {
		private static readonly Dictionary<string, string> vocab = new();
		private static readonly Dictionary<string, string> pinyin = new();
		private static readonly Dictionary<string, long> symbol = new();

		static PaddleTextTokenizer() {
			using (var sr = File.OpenText(Main.ASSETSDIR + "vocab.txt")) {
				while (sr.ReadLine() is { } nextLine) {
					try {
						var array = nextLine.Split(':');
						vocab[array[0]] = array[1];
					}
					catch (Exception) {
						// ignored
					}
				}
			}
			using (var sr = File.OpenText(Main.ASSETSDIR + "pinyin.txt")) {
				while (sr.ReadLine() is { } nextLine) {
					try {
						var array = nextLine.Split(':');
						pinyin[array[0]] = array[1];
					}
					catch (Exception) {
						// ignored
					}
				}
			}
			using (var sr = File.OpenText(Main.ASSETSDIR + "symbol.txt")) {
				while (sr.ReadLine() is { } nextLine) {
					try {
						var array = nextLine.Split(' ');
						symbol[array[0]] = long.Parse(array[1]);
					}
					catch (Exception) {
						// ignored
					}
				}
			}
		}

		public static long[] encode(string text) {
			var list = new List<long>();
			foreach (var t in py(text)) {
				if (symbol.TryGetValue(t, out var value)) list.Add(value);
			}
			return list.ToArray();
		}

		private static List<string> py(string text) {
			var list = new List<string>();
			for (int i = text.Length, start = 0; i > start; i--) {
				var ss = text.Substring(start, i - start);
				if (!vocab.TryGetValue(ss, out var zhTone)) continue;
				start = i;
				i = text.Length + 1;
				if (zhTone == null) continue;
				if (zhTone.Length == 1) {
					var cz = zhTone[0];
					if (cz is <= 'Z' and >= 'A') {
						if (!pinyin.TryGetValue(zhTone, out var what)) {
							Main.Log($"{zhTone} is not pinyin.");
							continue;
						}
						list.Add(what);
					}
				}
				foreach (var x in zhTone.Split(' ')) {
					var key = Regex.Replace(x, "\\d+$", "");
					if (!pinyin.TryGetValue(key, out var what)) {
						Main.Log($"{key} is not pinyin.");
						continue;
					}
					what += x.Substring(x.Length - 1);
					list.Add(what);
				}
			}

			var res = new List<string>();
			for (var i = 0; i < list.Count; i++) {
				var ci = list[i];
				if (i != list.Count - 1) {
					var cj = list[i + 1];
					var ci_last = ci[ci.Length - 1];
					var cj_last = cj[cj.Length - 1];
					if (ci_last == '3' && cj_last == '3')
						ci = ci.Substring(0, ci.Length - 1) + '2';
					if (ci == "b u4" && cj_last == '4') ci = "b u2";
					if (ci == "^ i1") {
						ci = cj_last switch {
							'1' or '3' => "^ i4",
							'4' => "^ i2",
							_ => ci
						};
					}
				}
				res.AddRange(ci.Split(' '));
			}
			return res;
		}
	}

	private static class GenTTS {
		private static readonly InferenceSession session_fastspeech, session_vcoder;
		private static readonly RunOptions runOptions = new();

		static GenTTS() {
			var options = new SessionOptions();
			options.AddSessionConfigEntry("session.load_model_format", "ORT");
			session_fastspeech = new InferenceSession(Main.ASSETSDIR + "a.ort", options);
			session_vcoder = new InferenceSession(Main.ASSETSDIR + "v.ort", options);
		}

		public static float[] forward(long[] ids) {
			if (ids.Length == 0) return Array.Empty<float>();
			// Main.Log(string.Join(" ", session_fastspeech.InputNames));
			// Main.Log(string.Join(" ", session_vcoder.InputNames));
			using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(ids, new long[] { ids.Length });
			var inputs1 = new Dictionary<string, OrtValue> {
				{ "text", inputOrtValue },
				// { "spk_id", OrtValue.CreateTensorValueFromMemory(new long[] { Main.spk }, new long[] { 1 }) }
			};
			using var outputs1 = session_fastspeech.Run(runOptions, inputs1, session_fastspeech.OutputNames);
			var inputs2 = new Dictionary<string, OrtValue> { { "logmel", outputs1.First() } };
			using var results = session_vcoder.Run(runOptions, inputs2, session_vcoder.OutputNames);
			return results.First().GetTensorMutableDataAsSpan<float>().ToArray();
		}
	}

	internal static MemoryStream getWav(string text) {
		if (string.IsNullOrEmpty(text)) return new MemoryStream();
		var tmpfp = Main.TMPDIR + text + ".wav";
		if (File.Exists(tmpfp)) {Main.Log("Cached: "+text);
			using var fileStream = new FileStream(tmpfp, FileMode.Open, FileAccess.Read);
			var mStream = new MemoryStream();
			fileStream.CopyTo(mStream);
			mStream.Position = 0;
			return mStream;
		}
		var a = GenTTS.forward(PaddleTextTokenizer.encode(text));
		var ad = new byte[2 * a.Length];
		var iter = 0;
		foreach (var da in a) ad[iter++] = ad[iter++] = (byte)(da * 128 * 1.25);
		new Thread(() => {
			try {
				if (!Directory.Exists(Main.TMPDIR)) Directory.CreateDirectory(Main.TMPDIR);
				using var fostream = new MemoryStream(ad);
				using var outputStream = new FileStream(tmpfp, FileMode.Create, FileAccess.Write);
				fostream.CopyTo(outputStream);
			}
			catch (Exception e) {
				Main.Log(e);
			}
		}).Start();
		return new MemoryStream(ad);
	}
}