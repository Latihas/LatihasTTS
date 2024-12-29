using System.IO;
using System.Text.RegularExpressions;

namespace LatihasTTS.Core;

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

public class Main : IActPluginV1 {
	private static Label lab;
	private FormActMain.PlayTtsDelegate oriMethod;
	// internal static int spk;
	internal static string ROOTDIR, ASSETSDIR, TMPDIR;

	internal static void Log(object text) {
		lab.Text += text.ToString() + '\n';
	}

	// ReSharper disable once UnusedMember.Global
	public void InitROOTDIR(string s) {
		ROOTDIR = s;
		ASSETSDIR = ROOTDIR + "/TtsAssets/";
		TMPDIR = ROOTDIR + "/tmp/";
	}


	public void InitPlugin(TabPage page, Label pluginStatusText) {
		try {
			page.Text = "Latihas TTS";
			lab = new Label {
				Text = "",
				Width = 1920,
				Height = 800,
				Location = new Point(0, 64)
			};
			var textinput = new TextBox {
				Text = "温邪上受，首先犯肺，逆传心包。肺主气属卫，心主血属营，辨营卫气血虽与伤寒同，若论治法则与伤寒大异也。",
				Width = 500,
				Height = 64,
				Multiline = true,
				Location = new Point(0, 0)
			};
			var button_test = new Button {
				Text = "Test",
				Width = 128,
				Height = 64,
				Location = new Point(500, 0)
			};
			var button_clear = new Button {
				Text = "清空日志",
				Width = 128,
				Height = 64,
				Location = new Point(500 + 128, 0)
			};
			var button_cleartmp = new Button {
				Text = "清空缓存",
				Width = 128,
				Height = 64,
				Location = new Point(500 + 128 * 2, 0)
			};
			// var selectSpk = new ComboBox {
			// 	Width = 128,
			// 	Height = 64,
			// 	Location = new Point(500+128*2, 0)
			// };
			// for (var i = 0; i <= 282; i++) selectSpk.Items.Add(i.ToString());
			// selectSpk.SelectionChangeCommitted += (_, _) => spk = selectSpk.SelectedIndex;
			button_clear.Click += (_, _) => lab.Text = "";
			button_test.Click += (_, _) => { Speak(textinput.Text); };
			button_cleartmp.Click += (_, _) => {
				if (MessageBox.Show("真的要删除缓存吗", "确认", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
				foreach (var d in Directory.GetFileSystemEntries(TMPDIR))
					File.Delete(d);
			};
			page.Controls.Add(button_test);
			page.Controls.Add(button_clear);
			page.Controls.Add(textinput);
			page.Controls.Add(button_cleartmp);
			// page.Controls.Add(selectSpk);
			page.Controls.Add(lab);
			oriMethod = ActGlobals.oFormActMain.PlayTtsMethod.Clone() as FormActMain.PlayTtsDelegate;
			ActGlobals.oFormActMain.PlayTtsMethod = Speak;
			pluginStatusText.Text = "Plugin Inited.";
			Log("Inited");
			Speak("语音插件加载完成");
		}
		catch (Exception e) {
			pluginStatusText.Text = e.ToString();
		}
	}



	// ReSharper disable once MemberCanBeMadeStatic.Local
	private void _Speak(object message) {
		try {
			var player = new Player();
			var smg = message.ToString();
			if (string.IsNullOrEmpty(smg)) return;
			smg = smg.Replace("AA", ",A,A")
				.Replace("aa", ",a,a")
				.Replace("AOE", "AAOOE")
				.Replace("aoe", "aaooe");
			foreach (var line in Regex.Split(smg, @"[^\u4e00-\u9fa5\w]+")) {
				if (!Alive[0]) return;
				player.Play(TTSEngine.getWav(line),line is "A" or "a");
			}
			Log(message);
		}
		catch (Exception e) {
			Log(e);
		}
	}

	private void Speak(string message) {
		new Thread(_Speak).Start(message);
	}

	internal static readonly bool[] Alive = { true };

	public void DeInitPlugin() {
		Alive[0] = false;
		ActGlobals.oFormActMain.PlayTtsMethod = oriMethod;
	}
}