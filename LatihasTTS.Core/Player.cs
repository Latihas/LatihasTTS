using System.Collections.Generic;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace LatihasTTS.Core;

public class Player {
	private readonly Queue<WavInfo> streams = new();

	private class WavInfo {
		internal readonly MemoryStream stream;
		internal readonly bool longDelay;

		public WavInfo(MemoryStream memoryStream, bool b) {
			stream = memoryStream;
			longDelay = b;
		}
	}

	internal Player() {
		new Thread(() => {
			while (Main.Alive[0]) {
				if (streams.Count > 0) {
					var mStream = streams.Dequeue();
					PlayWav(mStream.stream);
					mStream.stream.Close();
					if (mStream.longDelay) Thread.Sleep(75);
				}
				else Thread.Sleep(50);
			}
		}).Start();
	}

	internal void Play(MemoryStream stream, bool longDelay = false) {
		streams.Enqueue(new WavInfo(stream, longDelay));
	}

	private static void PlayWav(MemoryStream stream) {
		const int sr = 30000;
		var rawStream = new RawSourceWaveStream(stream, new WaveFormat(sr, 16, 1));
		var waveOut = new WaveOut();
		waveOut.Init(rawStream);
		waveOut.Play();
		while (waveOut.PlaybackState == PlaybackState.Playing) Thread.Sleep(250);
	}
}