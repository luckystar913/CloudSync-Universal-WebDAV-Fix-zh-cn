using System;
using System.Collections.Generic;
using System.Reflection;
using CloudSync.Sync;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace CloudSync.Patches;

internal static class SaveFileSlotPatch
{
	private static CloudSaveChecker? Checker;

	private static IMonitor? Monitor;

	private static Texture2D? CloudTexture;

	private static Rectangle CloudSourceRect;

	private static bool SpriteReady;

	private static bool DebugLogged;

	public static void Init(CloudSaveChecker checker, IMonitor monitor)
	{
		Checker = checker;
		Monitor = monitor;
	}

	public static void Apply(Harmony harmony)
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		try
		{
			Type nestedType = typeof(LoadGameMenu).GetNestedType("SaveFileSlot", BindingFlags.Public | BindingFlags.NonPublic);
			if (nestedType == null)
			{
				IMonitor? monitor = Monitor;
				if (monitor != null)
				{
					monitor.Log("CloudSync: SaveFileSlot type not found, skipping patch", (LogLevel)3);
				}
				return;
			}
			MethodInfo method = nestedType.GetMethod("Draw", new Type[2]
			{
				typeof(SpriteBatch),
				typeof(int)
			});
			if (method == null)
			{
				IMonitor? monitor2 = Monitor;
				if (monitor2 != null)
				{
					monitor2.Log("CloudSync: SaveFileSlot.Draw method not found, skipping patch", (LogLevel)3);
				}
				return;
			}
			HarmonyMethod val = new HarmonyMethod(typeof(SaveFileSlotPatch).GetMethod("DrawPostfix", BindingFlags.Static | BindingFlags.NonPublic));
			harmony.Patch((MethodBase)method, (HarmonyMethod)null, val, (HarmonyMethod)null, (HarmonyMethod)null);
			IMonitor? monitor3 = Monitor;
			if (monitor3 != null)
			{
				monitor3.Log("CloudSync: SaveFileSlot.Draw patch applied", (LogLevel)1);
			}
		}
		catch (Exception ex)
		{
			IMonitor? monitor4 = Monitor;
			if (monitor4 != null)
			{
				monitor4.Log("CloudSync: failed to apply SaveFileSlot patch: " + ex.Message, (LogLevel)4);
			}
		}
	}

	public static void LoadSprite()
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Dictionary<string, string> dictionary = Game1.content.Load<Dictionary<string, string>>("Data\\Furniture");
			string text = null;
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				string text2 = item.Value.Split('/')[0];
				if (text2.Equals("Cloud Decal", StringComparison.OrdinalIgnoreCase))
				{
					text = item.Key;
					break;
				}
			}
			if (text == null)
			{
				IMonitor? monitor = Monitor;
				if (monitor != null)
				{
					monitor.Log("CloudSync: Cloud Decal not found in furniture data, using fallback sprite", (LogLevel)1);
				}
				UseFallbackSprite();
				return;
			}
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(F)" + text);
			CloudTexture = dataOrErrorItem.GetTexture();
			CloudSourceRect = dataOrErrorItem.GetSourceRect(0, (int?)null);
			SpriteReady = true;
			IMonitor? monitor2 = Monitor;
			if (monitor2 != null)
			{
				monitor2.Log($"CloudSync: loaded Cloud Decal sprite (ID: {text}, rect: {CloudSourceRect})", (LogLevel)1);
			}
		}
		catch (Exception ex)
		{
			IMonitor? monitor3 = Monitor;
			if (monitor3 != null)
			{
				monitor3.Log("CloudSync: failed to load Cloud Decal sprite: " + ex.Message, (LogLevel)3);
			}
			UseFallbackSprite();
		}
	}

	private static void UseFallbackSprite()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CloudTexture = Game1.mouseCursors;
			CloudSourceRect = new Rectangle(346, 392, 8, 8);
			SpriteReady = true;
			IMonitor? monitor = Monitor;
			if (monitor != null)
			{
				monitor.Log("CloudSync: using fallback cloud sprite", (LogLevel)1);
			}
		}
		catch
		{
			SpriteReady = false;
		}
	}

	private static void DrawPostfix(object __instance, SpriteBatch b, int i, LoadGameMenu ___menu)
	{
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		if (!SpriteReady || Checker == null || !Checker.IsLoaded || ___menu == null)
		{
			if (!DebugLogged)
			{
				IMonitor? monitor = Monitor;
				if (monitor != null)
				{
					monitor.Log($"CloudSync DrawPostfix: early exit (sprite={SpriteReady}, checker={Checker != null}, loaded={Checker?.IsLoaded}, menu={___menu != null})", (LogLevel)1);
				}
				DebugLogged = true;
			}
			return;
		}
		try
		{
			FieldInfo field = __instance.GetType().GetField("Farmer");
			if (field == null)
			{
				if (!DebugLogged)
				{
					IMonitor? monitor2 = Monitor;
					if (monitor2 != null)
					{
						monitor2.Log("CloudSync DrawPostfix: Farmer field not found", (LogLevel)1);
					}
					DebugLogged = true;
				}
				return;
			}
			object? value = field.GetValue(__instance);
			Farmer val = (Farmer)((value is Farmer) ? value : null);
			if (val == null)
			{
				return;
			}
			long uniqueMultiplayerID = val.UniqueMultiplayerID;
			if (!DebugLogged)
			{
				IMonitor? monitor3 = Monitor;
				if (monitor3 != null)
				{
					monitor3.Log($"CloudSync DrawPostfix: farmer='{((Character)val).Name}', multiplayerId={uniqueMultiplayerID}, match={Checker.HasCloudSaveForFarmer(uniqueMultiplayerID)}, cloud saves=[{string.Join(", ", Checker.GetAllSaves())}]", (LogLevel)1);
				}
				DebugLogged = true;
			}
			if (Checker.HasCloudSaveForFarmer(uniqueMultiplayerID) && i >= 0 && i < ___menu.slotButtons.Count && i < ___menu.deleteButtons.Count)
			{
				Rectangle bounds = ((ClickableComponent)___menu.deleteButtons[i]).bounds;
				float num = 1.5f;
				int num2 = (int)((float)CloudSourceRect.Width * num);
				int num3 = (int)((float)CloudSourceRect.Height * num);
				int num4 = bounds.X - num2 - 8;
				int num5 = bounds.Y + (bounds.Height - num3) / 2 - 2;
				b.Draw(CloudTexture, new Vector2((float)num4, (float)num5), (Rectangle?)CloudSourceRect, Color.White * 0.8f, 0f, Vector2.Zero, num, (SpriteEffects)0, 0.99f);
				Rectangle val2 = new Rectangle(num4, num5, num2, num3);
			if (val2.Contains(Game1.getMouseX(), Game1.getMouseY()))
				{
					IClickableMenu.drawHoverText(b, "已同步到云端", Game1.smallFont, 0, 0, -1, (string)null, -1, (string[])null, (Item)null, 0, (string)null, -1, -1, -1, 1f, (CraftingRecipe)null, (IList<Item>)null, (Texture2D)null, (Rectangle?)null, (Color?)null, (Color?)null, 1f, -1, -1);
				}
			}
		}
		catch
		{
		}
	}
}
