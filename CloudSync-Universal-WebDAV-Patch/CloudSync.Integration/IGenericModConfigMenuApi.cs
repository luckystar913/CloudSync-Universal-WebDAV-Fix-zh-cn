using System;
using StardewModdingAPI;

namespace CloudSync.Integration;

public interface IGenericModConfigMenuApi
{
	void RegisterModConfig(IManifest mod, Action revertToDefault, Action saveToFile);

	void UnregisterModConfig(IManifest mod);

	void RegisterLabel(IManifest mod, string labelName, string labelDesc);

	void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<bool> optionGet, Action<bool> optionSet);

	void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet);

	void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet);

	void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<SButton> optionGet, Action<SButton> optionSet);

	void RegisterClampedOption(IManifest mod, string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet, int min, int max);
}
