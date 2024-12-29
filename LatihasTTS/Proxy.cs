using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Label = System.Windows.Forms.Label;

namespace LatihasTTS;

public class Proxy : IActPluginV1 {
	private Assembly Core;
	private dynamic RealPlugin;
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr LoadLibrary(string lpFileName);

	public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
		var text = Path.Combine(ActGlobals.oFormActMain.PluginGetSelfData(this).pluginFile.DirectoryName!, "libs");
		Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + text);
		Core = Load(Path.Combine(text, "LatihasTTS.Core.dll"));
		LoadLibrary(Path.Combine(text, "onnxruntime.dll"));
		RealPlugin = Core.CreateInstance("LatihasTTS.Core.Main");
		RealPlugin!.InitPlugin(pluginScreenSpace, pluginStatusText);
		RealPlugin.InitROOTDIR(ActGlobals.oFormActMain.PluginGetSelfData(this).pluginFile.DirectoryName);
	}

	public void DeInitPlugin() {
		RealPlugin.DeInitPlugin();
		RealPlugin.Dispose();
		Core = null;
	}

	private static Assembly Load(string path) {
		return Assembly.Load(File.ReadAllBytes(path));
	}
}